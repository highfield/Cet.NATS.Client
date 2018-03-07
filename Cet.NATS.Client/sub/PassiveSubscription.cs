
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
    /// <see cref="PassiveSubscription"/> provides messages for a subject through
    /// an external host awaiting (sync or async) for them.
    /// </summary>
    internal sealed class PassiveSubscription
        : SubscriptionBase, IPassiveSubscription    //, ISubscription
    {

        /// <summary>
        /// Creates an instance of <see cref="PassiveSubscription"/>
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="sid"></param>
        /// <param name="subject"></param>
        /// <param name="queue"></param>
        internal PassiveSubscription(
            SubscriptionPool owner,
            long sid,
            string subject,
            string queue
            )
            : base(owner, sid, subject, queue, useCache: true)
        {
            this._mainSem = new SemaphoreSlim(0);
        }


        private SemaphoreSlim _mainSem;
        private int _disposed;


        /// <summary>
        /// Returns the next <see cref="MsgOut"/> available to a subscriber, 
        /// or blocks up to a given timeout until the next one is available.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the next message.</param>
        /// <returns>The next <see cref="MsgOut"/> available to a subscriber.</returns>
        public MsgOut NextMessage(TimeSpan timeout)
        {
            return this.NextMessage(timeout, CancellationToken.None);
        }


        /// <summary>
        /// Returns the next <see cref="MsgOut"/> available to a subscriber
        /// </summary>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>The next <see cref="MsgOut"/> available to a subscriber.</returns>
        public MsgOut NextMessage(CancellationToken token)
        {
            return this.NextMessage(Timeout.InfiniteTimeSpan, token);
        }


        /// <summary>
        /// Returns the next <see cref="MsgOut"/> available to a subscriber,
        /// or block up to a given timeout until the next one is available.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the next message.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>The next <see cref="MsgOut"/> available to a subscriber.</returns>
        public MsgOut NextMessage(TimeSpan timeout, CancellationToken token)
        {
            if (this._disposed != 0)
            {
                throw new ObjectDisposedException(nameof(PassiveSubscription));
            }

            MsgOut message = null;
            try
            {
                while (
                    this._disposed == 0 &&
                    this.TryDequeueSinglePendingMessage(out message) == false &&
                    this._mainSem.Wait(timeout, token)
                    )
                {
                    //do nothing
                };
            }
            finally
            {
                //check for disposal in progress
                if (this._disposed != 0)
                {
                    throw new NATSBadSubscriptionException();
                }
            }


            return message;
        }


        /// <summary>
        /// Returns a task to await the next <see cref="MsgOut"/> available to a subscriber
        /// or block up to a given timeout until the next one is available.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the next message.</param>
        /// <returns>A task to await the next <see cref="MsgOut"/> available to a subscriber.</returns>
        public async Task<MsgOut> NextMessageAsync(TimeSpan timeout)
        {
            return await this.NextMessageAsync(timeout, CancellationToken.None);
        }


        /// <summary>
        /// Returns a task to await the next <see cref="MsgOut"/> available to a subscriber
        /// </summary>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A task to await the next <see cref="MsgOut"/> available to a subscriber.</returns>
        public async Task<MsgOut> NextMessageAsync(CancellationToken token)
        {
            return await this.NextMessageAsync(Timeout.InfiniteTimeSpan, token);
        }


        /// <summary>
        /// Returns a task to await the next <see cref="MsgOut"/> available to a subscriber
        /// or block up to a given timeout until the next one is available.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the next message.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A task to await the next <see cref="MsgOut"/> available to a subscriber.</returns>
        public async Task<MsgOut> NextMessageAsync(TimeSpan timeout, CancellationToken token)
        {
            if (this._disposed != 0)
            {
                throw new ObjectDisposedException(nameof(PassiveSubscription));
            }

            MsgOut message = null;
            try
            {
                while (
                    this._disposed == 0 &&
                    this.TryDequeueSinglePendingMessage(out message) == false &&
                    await this._mainSem.WaitAsync(timeout, token)
                    )
                {
                    //do nothing
                };
            }
            finally
            {
                //check for disposal in progress
                if (this._disposed != 0)
                {
                    throw new NATSBadSubscriptionException();
                }
            }

            return message;
        }


        /// <summary>
        /// Processes the dispatched message
        /// </summary>
        /// <param name="message"></param>
        protected override void ProcessOutgoingMessage(MsgOut message)
        {
            //release the semaphore
            this._mainSem.Release();
        }


        /// <summary>
        /// Custom disposal of the local resources
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.CompareExchange(ref this._disposed, 1, 0) == 0)
                {
                    //here we release the semaphore, but WON'T dispose it.
                    //that's because there could be a task waiting for it,
                    //the release signal is not immediate, but the disposal.
                    //that might lead to an unexpected behavior, such as the
                    //task would "wait" a disposed semaphore before realizing
                    //that's actually released.
                    this._mainSem.Release();
                }
            }
            base.Dispose(disposing);
        }

    }
}