
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
    /// <see cref="ReactiveSubscription"/> asynchronously delivers messages to listeners
    /// </summary>
    internal sealed class ReactiveSubscription
        : SubscriptionBase, IReactiveSubscription, ISubscription
    {

        /// <summary>
        /// Creates an instance of <see cref="ReactiveSubscription"/>
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="sid"></param>
        /// <param name="subject"></param>
        /// <param name="queue"></param>
        /// <param name="syncHandler"></param>
        /// <param name="asyncHandler"></param>
        internal ReactiveSubscription(
            SubscriptionPool owner,
            long sid,
            string subject,
            string queue,
            Action<MsgOut, CancellationToken> syncHandler,
            Func<MsgOut, CancellationToken, Task> asyncHandler
            )
            : base(owner, sid, subject, queue, useCache: true)
        {
            this._syncHandler = syncHandler;
            this._asyncHandler = asyncHandler;
            this._cts = new CancellationTokenSource();
        }


        private readonly Action<MsgOut, CancellationToken> _syncHandler;
        private readonly Func<MsgOut, CancellationToken, Task> _asyncHandler;
        private readonly CancellationTokenSource _cts;


        /// <summary>
        /// Indicates whether any pending message is waiting to be delivered
        /// </summary>
        internal bool AnyPending;


        /// <summary>
        /// Processe the dispatched message
        /// </summary>
        /// <param name="message"></param>
        protected override void ProcessOutgoingMessage(MsgOut message)
        {
            this.Owner.ReactiveSubscriptionSignal(this);
        }


        /// <summary>
        /// Dispatches all the pending messages to the listener
        /// </summary>
        internal void ForwardPendingMessages()
        {
            if (this._syncHandler != null)
            {
                //dequeues, and delivers in the sync-fashion
                while (
                    this._cts.IsCancellationRequested == false &&
                    this.TryDequeueSinglePendingMessage(out MsgOut message)
                    )
                {
                    try
                    {
                        this._syncHandler(message, this._cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        //do nothing: it's up to the handler to catch exceptions
                    }
                };
            }
            else if (this._cts.IsCancellationRequested == false)
            {
                //starts a fire-and-forget task, to dequeue and deliver the pending messages
                Task.Factory.StartNew(this.ForwardPendingMessagesAsync)
                    .GetAwaiter()
                    .GetResult();
            }
        }


        /// <summary>
        /// Dispatches all the pending messages to the listener
        /// </summary>
        /// <remarks>
        /// The sequence of the messages is preserved within a single dequeuing session,
        /// that is, each task like the following.
        /// However, there's no guarantee to preserve the original publisher sequence,
        /// because it may span here across many different parallel tasks.
        /// </remarks>
        /// <returns></returns>
        private async Task ForwardPendingMessagesAsync()
        {
            //dequeues, and delivers in the async-fashion
            while (
                this._cts.IsCancellationRequested == false &&
                this.TryDequeueSinglePendingMessage(out MsgOut message)
                )
            {
                try
                {
                    await this._asyncHandler(message, this._cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    //do nothing: it's up to the handler to catch exceptions
                }
            };
        }


        /// <summary>
        /// Custom disposal of the local resources
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._cts.Cancel();
            }
            base.Dispose(disposing);
        }

    }
}
