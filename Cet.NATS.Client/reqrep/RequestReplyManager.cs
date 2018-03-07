
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Interface for the INBOX subscription adapters.
    /// </summary>
    internal interface IRequestReplayFeedback
    {
        void ProcessMessage(MsgOut message, long reqId);
    }


    /// <summary>
    /// This is request-reply manager
    /// </summary>
    internal sealed class RequestReplyManager
        : IRequestReplayFeedback, IDisposable
    {

        //the interval of the watchdog clock
        private static readonly TimeSpan WatchdogClockInterval = TimeSpan.FromSeconds(1);


        /// <summary>
        /// Creates an instance of <see cref="RequestReplyManager"/>
        /// </summary>
        /// <param name="owner"></param>
        internal RequestReplyManager(Connection owner)
        {
            this._owner = owner;
            this._options = owner.OptionsInternal;
            this._connmgr = owner.ConnMgr;

            if (this._options.UseOldRequestStyle)
            {
                this._rnd = new Random(Guid.NewGuid().GetHashCode());
            }
        }


        private readonly Connection _owner;
        private readonly ClientOptions _options;
        private readonly ConnectionManager _connmgr;
        private readonly Random _rnd;

        private InboxSubscription _inbox;
        private long _reqId;

        private readonly object _reqlocker = new object();
        private readonly Dictionary<long, InFlightRequest> _requestRegister = new Dictionary<long, InFlightRequest>();

        private Thread _watchdogThread;
        private CancellationTokenSource _watchdogCts;


        /// <summary>
        /// Gets the current "INBOX subject" generated for this NATS client
        /// </summary>
        internal string InboxSubject => this._inboxSubject;
        private string _inboxSubject;


        #region Life-cycle state machine

        private const int LifeCycleStateIdle = 0;
        private const int LifeCycleStateStarting = 1;
        private const int LifeCycleStateRunning = 2;
        private const int LifeCycleStateStopping = 3;

        private int _lifeCycleState = LifeCycleStateIdle;


        /// <summary>
        /// Triggers the start of the state-machine
        /// </summary>
        internal void Start()
        {
            //if IDLE then STARTING else exit
            if (Interlocked.CompareExchange(ref this._lifeCycleState, LifeCycleStateStarting, LifeCycleStateIdle) == LifeCycleStateIdle)
            {
                this._inboxSubject = this.NewInbox();
                this._inbox = this._owner.SubPool.SubscribeInbox(this._inboxSubject + ".*", this);

                this._watchdogCts = new CancellationTokenSource();
                this._watchdogThread = new Thread(this.WatchdogWorker);
                this._watchdogThread.Start();

                this._lifeCycleState = LifeCycleStateRunning;
            }
        }


        /// <summary>
        /// Stops the state-machine and waits for its completion
        /// </summary>
        /// <param name="disposing"></param>
        internal void Stop(bool disposing)
        {
            if (disposing)
            {
                this._lifeCycleState = LifeCycleStateStopping;
                StopCore();
            }
            else
            {
                //if RUNNING then STOPPING else exit
                if (Interlocked.CompareExchange(ref this._lifeCycleState, LifeCycleStateStopping, LifeCycleStateRunning) == LifeCycleStateRunning)
                {
                    StopCore();
                }
            }

            void StopCore()
            {
                if (this._watchdogThread !=null)
                {
                    this._watchdogCts.Cancel();
                    this._watchdogThread.Join();
                    this._watchdogCts = null;
                    this._watchdogThread = null;
                }
                //this._watchdogClock.Dispose();
                this._inbox.Dispose();
                this._lifeCycleState = LifeCycleStateIdle;
            }
        }

        #endregion


        /// <summary>
        /// Sets an <see cref="InFlightRequest"/> instance up,
        /// as proxy for a request-reply process.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal InFlightRequest SetupRequest(MsgIn message)
        {
            InFlightRequest request;
            lock (this._reqlocker)
            {
                request = new InFlightRequest(this, ++this._reqId);
                this._requestRegister.Add(request.ReqId, request);
            }

            message.InboxRequest = request;
            return request;
        }


        /// <summary>
        /// Removes the <see cref="InFlightRequest"/> from the local queue.
        /// </summary>
        /// <param name="request"></param>
        internal void DropRequest(InFlightRequest request)
        {
            lock (this._reqlocker)
            {
                this._requestRegister.Remove(request.ReqId);
            }
        }


        /// <summary>
        /// Gets a new INBOX string, but DOES NOT alter the current instance setting.
        /// </summary>
        /// <returns>The new generated INBOX-subject</returns>
        internal string NewInbox()
        {
            if (this._options.UseOldRequestStyle)
            {
                //old request style
                byte[] buf = new byte[13];
                this._rnd.NextBytes(buf);
                return InternalConstants.inboxPrefix + BitConverter.ToString(buf).Replace("-", "");
            }
            else
            {
                //new request style
                return InternalConstants.inboxPrefix + Guid.NewGuid().ToString("N");
            }
        }


        /// <summary>
        /// Handler of the received <see cref="MsgOut"/> message from the subscription.
        /// The message is quicky dispatched to the proper <see cref="InFlightRequest"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="reqId"></param>
        void IRequestReplayFeedback.ProcessMessage(MsgOut message, long reqId)
        {
            InFlightRequest request;
            lock (this._reqlocker)
            {
                this._requestRegister.TryGetValue(reqId, out request);
            }
            if (request != null)
            {
                request.Resolve(message);
            }
        }


        /// <summary>
        /// Global watchdog worker.
        /// Acts as a heartbeat for any pending <see cref="InFlightRequest"/> in the locale queue,
        /// which waits the response within a certain timeout.
        /// </summary>
        private void WatchdogWorker()
        {
            while (this._watchdogCts.IsCancellationRequested == false)
            {
                if (Helpers.CancelableDelay(WatchdogClockInterval, this._watchdogCts.Token))
                {
                    //delay wa scanceled
                    break;
                }

                lock (this._reqlocker)
                {
                    foreach (var pair in this._requestRegister)
                    {
                        InFlightRequest request = pair.Value;
                        if (request.WatchdogClock(WatchdogClockInterval))
                        {
                            request.Reject(
                                new NATSTimeoutException("Timeout occurred while waiting for the reply.")
                                );
                        }
                    }
                }
            }
        }


        public void Dispose()
        {
            this.Stop(disposing: true);
        }

    }
}
