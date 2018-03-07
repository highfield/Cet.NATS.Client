
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
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Manager and dispatcher for all the subscriptions of a client
    /// </summary>
    internal sealed class SubscriptionPool
        : IDisposable
    {
        /// <summary>
        /// Creates an instance of <see cref="SubscriptionPool"/>
        /// </summary>
        /// <param name="owner"></param>
        internal SubscriptionPool(Connection owner)
        {
            this._owner = owner;
            this._options = owner.OptionsInternal;
            this._connmgr = owner.ConnMgr;

            this._reactThread = new Thread(this.ReactiveSubscriptionWorker);
            this._reactThread.Start();
        }


        private readonly ClientOptions _options;
        private readonly ConnectionManager _connmgr;

        private long _ssid;

        private readonly object _sublocker = new object();
        private readonly Dictionary<long, SubscriptionBase> _subRegister = new Dictionary<long, SubscriptionBase>();

        private Thread _reactThread;
        private CancellationTokenSource _reactCts = new CancellationTokenSource();
        private SemaphoreSlim _reactSemaphore = new SemaphoreSlim(0);
        private readonly LinkedList<ReactiveSubscription> _reactPendingQueue = new LinkedList<ReactiveSubscription>();
        private readonly object _reactProcessorLocker = new object();


        /// <summary>
        /// Gets the <see cref="Connection"/> associated with this instance.
        /// </summary>
        internal Connection Connection => this._owner;
        private readonly Connection _owner;


        /// <summary>
        /// Entry-point to register a reactive subscription
        /// </summary>
        /// <remarks>
        /// You should specify only one handler: either sync or async.
        /// </remarks>
        /// <param name="subject">The subject to subscribe to.</param>
        /// <param name="queue">The queue group name. Specify null or empty if not used.</param>
        /// <param name="syncHandler">The sync callback to handle the messages.</param>
        /// <param name="asyncHandler">The async callback to handle the messages.</param>
        /// <returns>An instance of <see cref="ReactiveSubscription"/>.</returns>
        internal IReactiveSubscription SubscribeReactive(
            string subject,
            string queue,
            Action<MsgOut, CancellationToken> syncHandler,
            Func<MsgOut, CancellationToken, Task> asyncHandler
            )
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new NATSBadSubscriptionException();
            }

            lock (this._sublocker)
            {
                //creates a new instance of subscription proxy
                this._ssid++;
                var sub = new ReactiveSubscription(
                    this,
                    this._ssid,
                    subject,
                    queue,
                    syncHandler,
                    asyncHandler
                    );

                //then register it
                this._subRegister.Add(this._ssid, sub);

                //finally, enqueues the request of subscription to be
                //sent to the NATS broker.
                this._connmgr.EnqueueSubscriptionOperation(sub.ProtoMessageSub);
                return sub;
            }
        }


        /// <summary>
        /// Entry-point to register an active subscription
        /// </summary>
        /// <param name="subject">The subject to subscribe to.</param>
        /// <param name="queue">The queue group name. Specify null or empty if not used.</param>
        /// <returns>An instance of <see cref="PassiveSubscription"/>.</returns>
        internal IPassiveSubscription SubscribePassive(
            string subject,
            string queue
            )
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new NATSBadSubscriptionException();
            }

            lock (this._sublocker)
            {
                //creates a new instance of subscription proxy
                this._ssid++;
                var sub = new PassiveSubscription(this, this._ssid, subject, queue);

                //then register it
                this._subRegister.Add(this._ssid, sub);

                //finally, enqueues the request of subscription to be
                //sent to the NATS broker.
                this._connmgr.EnqueueSubscriptionOperation(sub.ProtoMessageSub);
                return sub;
            }
        }


        /// <summary>
        /// Entry-point to register a special INBOX subscription
        /// internally used for the request-reply mechanism.
        /// </summary>
        /// <param name="subject">The subject to subscribe to.</param>
        /// <param name="feedback">The reference to the reply message handler.</param>
        /// <returns>An instance of <see cref="InboxSubscription"/>.</returns>
        internal InboxSubscription SubscribeInbox(
            string subject,
            IRequestReplayFeedback feedback
            )
        {
            lock (this._sublocker)
            {
                //creates a new instance of subscription proxy
                this._ssid++;
                var sub = new InboxSubscription(this, this._ssid, subject, feedback);

                //then register it
                this._subRegister.Add(this._ssid, sub);

                //finally, enqueues the request of subscription to be
                //sent to the NATS broker.
                this._connmgr.EnqueueSubscriptionOperation(sub.ProtoMessageSub);
                return sub;
            }
        }


        /// <summary>
        /// Processes the <see cref="MsgOut"/> as they are given from the <see cref="Parser"/>
        /// </summary>
        /// <param name="message"></param>
        internal void ProcessMessage(
            MsgOut message
            )
        {
            SubscriptionBase sub;
            lock (this._sublocker)
            {
                //retrieves the subscription proxy to dispatch the message to
                this._subRegister.TryGetValue(message.SubId, out sub);
            }

            if (sub != null)
            {
                //dispatches the message to the proxy
                message.ArrivalSubcription = sub;
                sub.ProcessMessage(message);
            }
        }


        /// <summary>
        /// Signals the local dispatcher about new messages related 
        /// to the specified subscription
        /// </summary>
        /// <param name="sub"></param>
        internal void ReactiveSubscriptionSignal(ReactiveSubscription sub)
        {
            if (sub.AnyPending == false)
            {
                lock (this._reactProcessorLocker)
                {
                    this._reactPendingQueue.AddLast(sub);
                    sub.AnyPending = true;
                }

                this._reactSemaphore.Release(1);
            }
        }


        /// <summary>
        /// Local worker that acts as a dispatcher: waits for a signal,
        /// then dispatches the messages to the proper handler
        /// </summary>
        private void ReactiveSubscriptionWorker()
        {
            while (true)
            {
                //waits for a signal to continue
                try
                {
                    this._reactSemaphore.Wait(this._reactCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                while (true)
                {
                    //dequeues the next subscription reference
                    ReactiveSubscription sub;
                    lock (this._reactProcessorLocker)
                    {
                        if (this._reactPendingQueue.Count > 0)
                        {
                            sub = this._reactPendingQueue.First.Value;
                            this._reactPendingQueue.RemoveFirst();
                            sub.AnyPending = false;
                        }
                        else
                        {
                            break;
                        }
                    }

                    //yields the proxy to forward its pending messages
                    sub.ForwardPendingMessages();
                };
            }
        }


        /// <summary>
        /// Unsubscribe performs the low level unsubscribe to the server.
        /// </summary>
        /// <remarks>
        /// Use <see cref="SubscriptionBase.Unsubscribe"/>
        /// </remarks>
        /// <param name="sub">The reference to unsubscribe.</param>
        /// <param name="max">The number of message to count before self-unsubscribe.
        /// A non-positive value will unsubscribe immediately.</param>
        internal void Unsubscribe(SubscriptionBase sub, long max)
        {
            lock (this._sublocker)
            {
                if (this._subRegister.ContainsKey(sub.SubId))
                {
                    if (max > 0)
                    {
                        //do not unsubscribe yet, rather set the number of message
                        //to receive before self-unsubscribe
                        sub.AutoUnsubscribeMessageCount = max;
                    }
                    else
                    {
                        //unsubscribe immediately
                        this._subRegister.Remove(sub.SubId);
                        if (sub is ReactiveSubscription react)
                        {
                            lock (this._reactProcessorLocker)
                            {
                                this._reactPendingQueue.Remove(react);
                            }
                        }

                        //enqueues a message of unsubscription to be sent
                        //to the NATS broker
                        this._connmgr.EnqueueSubscriptionOperation(
                            string.Format(InternalConstants.unsubProto, sub.SubId, max)
                            );

                        sub.Dispose();
                    }
                }
            }
        }


        /// <summary>
        /// Unsubscribe all, that is, removes all the existent subscriptions.
        /// </summary>
        private void UnsubscribeAll()
        {
            lock (this._sublocker)
            {
                lock (this._reactProcessorLocker)
                {
                    this._reactPendingQueue.Clear();
                }

                var subcoll = new List<SubscriptionBase>(this._subRegister.Values);
                foreach (SubscriptionBase sub in subcoll)
                {
                    this.Unsubscribe(sub, 0);
                }
            }
        }


        public void Dispose()
        {
            this._reactCts.Cancel();
            this._reactThread.Join();
            this._reactCts.Dispose();
            this._reactSemaphore.Dispose();
            this.UnsubscribeAll();
        }

    }
}
