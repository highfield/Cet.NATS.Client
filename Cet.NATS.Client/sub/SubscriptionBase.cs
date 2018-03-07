
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
using System.Threading;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Represents interest in a NATS topic.
    /// </summary>
    internal class SubscriptionBase
        : ISubscription, IDisposable
    {
        /// <summary>
        /// Constructor for an instance of <see cref="SubscriptionBase"/>
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="sid"></param>
        /// <param name="subject"></param>
        /// <param name="queue"></param>
        /// <param name="useCache"></param>
        protected SubscriptionBase(
            SubscriptionPool owner,
            long sid,
            string subject,
            string queue,
            bool useCache
            )
        {
            this._owner = owner;
            this.SubId = sid;
            this._subject = subject;
            this._queue = queue;
            this._useCache = useCache;

            if (useCache)
            {
                this._deliveringQueue = new Queue<MsgOut>();
                this._deliveringLocker = new object();

                this.UpdateActualLimits();
            }
        }


        private readonly bool _useCache;
        private readonly Queue<MsgOut> _deliveringQueue;
        private readonly object _deliveringLocker;

        private int _actualPendingMessagesLimit;
        private long _actualPendingBytesLimit;


        /// <summary>
        /// A read-only copy of the subscriber ID
        /// </summary>
        internal readonly long SubId;


        internal SubscriptionPool Owner => this._owner;
        private readonly SubscriptionPool _owner;


        /// <summary>
        /// Gets the subject for this subscription.
        /// </summary>
        /// <remarks>
        /// Subject that represents this subscription. This can be different
        /// than the received subject inside a Msg if this is a wildcard.
        /// </remarks>
        public string Subject => this._subject;
        private readonly string _subject = null;


        /// <summary>
        /// Gets the optional queue group name.
        /// </summary>
        /// <remarks>
        /// If present, all subscriptions with the same name will form a distributed queue, and each message will only
        /// be processed by one member of the group.
        /// Optional queue group name. If present, all subscriptions with the
        /// same name will form a distributed queue, and each message will
        /// only be processed by one member of the group.
        /// </remarks>
        public string Queue => this._queue;
        private readonly string _queue;


        /// <summary>
        /// Gets the <see cref="IConnection"/> associated with this instance.
        /// </summary>
        public IConnection Connection => this._owner.Connection;


        /// <summary>
        /// Gets or sets the number of messages to manage before an
        /// unsubscription will take place automatically
        /// </summary>
        internal long AutoUnsubscribeMessageCount
        {
            get => Interlocked.Read(ref this._autoUnsubscribeMessageCount);
            set => Interlocked.Exchange(ref this._autoUnsubscribeMessageCount, value);
        }
        private long _autoUnsubscribeMessageCount;


        /// <summary>
        /// Gets a value indicating whether or not the <see cref="SubscriptionBase"/> is still valid.
        /// </summary>
        public bool IsValid => this._isValid;
        private bool _isValid = true;


        /// <summary>
        /// Gets whether the <see cref="MsgOut"/> host consumer is too slow
        /// to manage the incoming messages
        /// </summary>
        public bool IsSlowConsumer => this._isSlowConsumer;
        private volatile bool _isSlowConsumer;


        /// <summary>
        /// Gets the current maximum number of messages can be pending in the local queue
        /// </summary>
        public int PendingMessagesLimit => this._pendingMessagesLimit;
        private int _pendingMessagesLimit;


        /// <summary>
        /// Gets the current maximum number of bytes can be pending in the local queue
        /// </summary>
        public long PendingBytesLimit => Interlocked.Read(ref this._pendingBytesLimit);
        private long _pendingBytesLimit;


        /// <summary>
        /// Returns a string whose prototype of any publish-message
        /// </summary>
        internal string ProtoMessageSub => string.Format(
            InternalConstants.subProto, 
            this._subject, 
            this._queue, 
            this.SubId
            );


        /// <summary>
        /// Processes the <see cref="MsgOut"/> as they are given from the <see cref="Parser"/>
        /// </summary>
        /// <param name="message"></param>
        internal virtual void ProcessMessage(
            MsgOut message
            )
        {
            if (this._useCache)
            {
                //indirect delivery by enqueuing messages
                bool shouldUnsubscribe;
                lock (this._statsLocker)
                {
                    // Subscription internal stats
                    this._totalProcessedMessages++;
                    this._totalProcessedBytes += message.PayloadLength;

                    shouldUnsubscribe =
                        this._autoUnsubscribeMessageCount > 0 &&
                        this._totalProcessedMessages >= this._autoUnsubscribeMessageCount;

                    this._pendingMessages++;
                    if (this._pendingMessages > this._pendingMessagesHighestCount)
                    {
                        this._pendingMessagesHighestCount = this._pendingMessages;
                    }

                    this._pendingBytes += message.PayloadLength;
                    if (this._pendingBytes > this._pendingBytesHighestCount)
                    {
                        this._pendingBytesHighestCount = this._pendingBytes;
                    }

                    //detect for slow-consumer condition
                    bool isSlowConsumerOld = this._isSlowConsumer;
                    this._isSlowConsumer =
                        (this._actualPendingMessagesLimit > 0) && (this._pendingMessages > this._actualPendingMessagesLimit) ||
                        (this._actualPendingBytesLimit > 0) && (this._pendingBytes > this._actualPendingBytesLimit);

                    if (this._isSlowConsumer &&
                        isSlowConsumerOld == false
                        )
                    {
                        //TODO what to do when the consumer is too slow?
                        //possibly notify the client about the lost messages?
                        //maybe writes the lost messages on the disk?
                        //Console.WriteLine("slow consumer");
                    }
                }

                if (this._isSlowConsumer == false)
                {
                    lock (this._deliveringLocker)
                    {
                        this._deliveringQueue.Enqueue(message);
                        //Debug.Assert(this._pendingMessages == this._deliveringQueue.Count);
                    }
                }

                //yield to the specific message processing
                this.ProcessOutgoingMessage(message);

                //consider to unsubscribe, whereas necessary
                if (shouldUnsubscribe)
                {
                    this.Unsubscribe();
                }
            }
            else
            {
                //direct delivery
                this.ProcessOutgoingMessage(message);
            }
        }


        /// <summary>
        /// Yield to the specific message processing
        /// </summary>
        /// <param name="message"></param>
        protected virtual void ProcessOutgoingMessage(MsgOut message) { }


        /// <summary>
        /// Tries to dequeue a single <see cref="MsgOut"/> from the local queue.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Indicates whether the extraction was successful.</returns>
        protected bool TryDequeueSinglePendingMessage(out MsgOut message)
        {
            bool success = false;
            lock (this._deliveringLocker)
            {
                if (this._deliveringQueue.Count != 0)
                {
                    message = this._deliveringQueue.Dequeue();
                    Debug.Assert(message != null);
                    success = true;
                }
                else
                {
                    message = null;
                }
            }

            if (success)
            {
                lock (this._statsLocker)
                {
                    this._totalDeliveredMessages++;
                    this._pendingMessages--;
                    this._pendingBytes -= message.PayloadLength;
                }
            }
            return success;
        }


        /// <summary>
        /// Removes interest in the <see cref="Subject"/>.
        /// </summary>
        /// <exception cref="NATSBadSubscriptionException">There is no longer an associated <see cref="Connection"/>
        /// for this <see cref="ISubscription"/>.</exception>
        public void Unsubscribe()
        {
            this._owner.Unsubscribe(this, 0);
        }


        /// <summary>
        /// Issues an automatic call to <see cref="Unsubscribe"/> when <paramref name="max"/> messages have been
        /// received.
        /// </summary>
        /// <remarks>This can be useful when sending a request to an unknown number of subscribers.
        /// <see cref="Connection"/>'s Request methods use this functionality.</remarks>
        /// <param name="max">The maximum number of messages to receive on the subscription before calling
        /// <see cref="Unsubscribe"/>. Values less than or equal to zero (<c>0</c>) unsubscribe immediately.</param>
        /// <exception cref="NATSBadSubscriptionException">There is no longer an associated <see cref="Connection"/>
        /// for this <see cref="ISubscription"/>.</exception>
        public virtual void AutoUnsubscribe(int max)
        {
            this._owner.Unsubscribe(this, max);
        }


        /// <summary>
        /// Sets the limits for pending messages and bytes for this instance.
        /// </summary>
        /// <remarks>Zero (<c>0</c>) is not allowed. Negative values indicate that the
        /// given metric is not limited.</remarks>
        /// <param name="messageLimit">The maximum number of pending messages.</param>
        /// <param name="bytesLimit">The maximum number of pending bytes of payload.</param>
        public void SetPendingLimits(
            int messageLimit,
            long bytesLimit
            )
        {
            lock (this._statsLocker)
            {
                this._pendingMessagesLimit = Math.Max(messageLimit, 0);
                this._pendingBytesLimit = Math.Max(bytesLimit, 0);
                this.UpdateActualLimits();
            }
        }


        private void UpdateActualLimits()
        {
            ClientOptions options = this._owner.Connection.OptionsInternal;
            lock (this._statsLocker)
            {
                this._actualPendingMessagesLimit = this._pendingMessagesLimit != 0
                    ? this._pendingMessagesLimit
                    : options.PendingMessagesLimit;

                this._actualPendingBytesLimit = this._pendingBytesLimit != 0
                    ? this._pendingBytesLimit
                    : options.PendingBytesLimit;
            }
        }


        #region Statistics

        private long _totalProcessedBytes;
        private long _totalProcessedMessages;
        private long _totalDeliveredMessages;
        private long _totalDroppedMessages;

        private int _pendingMessages;
        private long _pendingBytes;
        private int _pendingMessagesHighestCount;
        private long _pendingBytesHighestCount;

        private readonly object _statsLocker = new object();


        public SubscriptionStatsInfo GetStats()
        {
            lock (this._statsLocker)
            {
                return new SubscriptionStatsInfo
                {
                    TotalProcessedMessages = this._totalProcessedMessages,
                    TotalProcessedBytes = this._totalProcessedBytes,
                    TotalDeliveredMessages = this._totalDeliveredMessages,
                    TotalDroppedMessages = this._totalDroppedMessages,
                    PendingMessagesHighestCount = this._pendingMessagesHighestCount,
                    PendingBytesHighestCount = this._pendingBytesHighestCount
                };
            }
        }


        public void ResetStats()
        {
            lock (this._statsLocker)
            {
                this._totalProcessedMessages = 0;
                this._totalProcessedBytes = 0;
                this._totalDeliveredMessages = 0;
                this._totalDroppedMessages = 0;
                this._pendingMessages = 0;
                this._pendingBytes = 0;
                this._pendingMessagesHighestCount = 0;
                this._pendingBytesHighestCount = 0;
            }
        }

        #endregion


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Unsubscribes the subscription and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed
        /// and unmanaged resources; <c>false</c> to release only unmanaged 
        /// resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            this._isValid = false;

            if (!disposedValue)
            {
                try
                {
                    this.Unsubscribe();
                }
                catch (Exception)
                {
                    // We we get here with normal usage, for example when
                    // auto unsubscribing, so ignore.
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SubscriptionBase"/>.
        /// </summary>
        /// <remarks>This method unsubscribes from the subject, to release resources.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}