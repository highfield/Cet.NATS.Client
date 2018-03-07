
/********************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright 2018+ Cet Electronics.
 * 
 * Based on the original work by Apcera Inc.
 * https://github.com/nats-io/csharp-nats
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*********************************************************************************/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Handles in-flight requests when using the new-style request/reply behavior
    /// </summary>
    internal sealed class InFlightRequest
        : IDisposable
    {
        private const int StateIdle = 0;
        private const int StateActive = 1;
        private const int StateComplete = 2;

        /// <summary>
        /// Idle-state grace timeout
        /// </summary>
        /// <remarks>
        /// The idle state starts to live from the creation of the <see cref="InFlightRequest"/>,
        /// up to the subsequent call to <see cref="InFlightRequest.GetReply(int, CancellationToken)"/>
        /// or <see cref="InFlightRequest.GetReplyAsync(int, CancellationToken)"/>.
        /// This value gives a limit of time for this state.
        /// </remarks>
        /// <seealso cref="Connection.RequestCore(MsgIn, int, CancellationToken)"/>
        /// <seealso cref="Connection.RequestCoreAsync(MsgIn, int, CancellationToken)"/>
        private static readonly TimeSpan IdleGraceTimeout = TimeSpan.FromSeconds(3);


        /// <summary>
        /// Creates an instance of <see cref="InFlightRequest"/>.
        /// </summary>
        /// <param name="rrmgr"></param>
        /// <param name="reqId"></param>
        internal InFlightRequest(
            RequestReplyManager rrmgr,
            long reqId
            )
        {
            this._rrmgr = rrmgr;
            this.ReqId = reqId;
            this._evt = new SemaphoreSlim(0);
        }


        private readonly RequestReplyManager _rrmgr;
        private SemaphoreSlim _evt;
        private int _status = StateIdle;
        private readonly object _wdtLocker = new object();
        private bool _waitForever;
        private TimeSpan _timeToExpire = IdleGraceTimeout;

        private MsgOut _resolveValue;
        private Exception _rejectValue;

        internal readonly long ReqId;


        /// <summary>
        /// Gets the "reply-to" subject to indicate in the <see cref="MsgIn"/>.
        /// </summary>
        internal string ReplyToSubject => this._rrmgr.InboxSubject + "." + Converters.Int64ToString(this.ReqId);


        /// <summary>
        /// Indicates whether the <see cref="MsgOut"/> is available for the host app.
        /// </summary>
        /// <seealso cref="GetReply(int, CancellationToken)"/>
        /// <seealso cref="GetReplyAsync(int, CancellationToken)"/>
        internal bool IsComplete => this._status == StateComplete;


        /// <summary>
        /// Waits for the <see cref="MsgOut"/> as reply.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for. 
        /// A negative value will wait forever.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>The received <see cref="MsgOut"/>.</returns>
        internal MsgOut GetReply(TimeSpan timeout, CancellationToken token)
        {
            try
            {
                int sts = Interlocked.CompareExchange(ref this._status, StateActive, StateIdle);
                if (sts != StateActive)
                {
                    lock (this._wdtLocker)
                    {
                        this._timeToExpire = timeout;
                        this._waitForever = timeout < TimeSpan.Zero;
                    }
                    this._evt.Wait(token);
                    return this._resolveValue ?? throw this._rejectValue;
                }
                else
                {
                    throw new InvalidOperationException("Invalid state in order to access this method: " + sts);
                }
            }
            finally
            {
                this.Dispose();
            }
        }


        /// <summary>
        /// Waits for the <see cref="MsgOut"/> as reply, async-fashion.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for. 
        /// A negative value will wait forever.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>The received <see cref="MsgOut"/>.</returns>
        internal async Task<MsgOut> GetReplyAsync(TimeSpan timeout, CancellationToken token)
        {
            try
            {
                int sts = Interlocked.CompareExchange(ref this._status, StateActive, StateIdle);
                if (sts != StateActive)
                {
                    lock (this._wdtLocker)
                    {
                        this._timeToExpire = timeout;
                        this._waitForever = timeout < TimeSpan.Zero;
                    }
                    await this._evt.WaitAsync(token);
                    return this._resolveValue ?? throw this._rejectValue;
                }
                else
                {
                    throw new InvalidOperationException("Invalid state in order to access this method: " + sts);
                }
            }
            finally
            {
                this.Dispose();
            }
        }


        /// <summary>
        /// Tells this instance the request-reply process has been completed with success,
        /// and feeds the <see cref="MsgOut"/> received.
        /// </summary>
        /// <param name="message"></param>
        internal void Resolve(MsgOut message)
        {
            int sts = Interlocked.Exchange(ref this._status, StateComplete);
            if (sts != StateComplete)
            {
                this._resolveValue = message;
                this._evt.Release(1);
            }
        }


        /// <summary>
        /// Tells this instance the request-reply process has been completed with errors,
        /// and feeds the <see cref="Exception"/> found.
        /// </summary>
        /// <param name="ex"></param>
        internal void Reject(Exception ex)
        {
            int sts = Interlocked.Exchange(ref this._status, StateComplete);
            if (sts != StateComplete)
            {
                this._rejectValue = ex;
                this._evt.Release(1);
            }
        }


        /// <summary>
        /// Manages the watchdog timer for calculating the timeout.
        /// </summary>
        /// <param name="elapsed">The amount of time elapsed since the last call.</param>
        /// <returns>Indicates whether the timeout has been expired.</returns>
        internal bool WatchdogClock(TimeSpan elapsed)
        {
            lock (this._wdtLocker)
            {
                if (this._waitForever)
                {
                    return false;
                }
                else
                {
                    this._timeToExpire -= elapsed;
                    return this._timeToExpire <= TimeSpan.Zero;
                }
            }
        }


        public void Dispose()
        {
            this._evt.Dispose();
            this._rrmgr.DropRequest(this);
        }

    }
}
