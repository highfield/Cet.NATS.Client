
/********************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright 2018+ Cet Electronics.
 * 
 * Based on the original work by Apcera Inc.
 * Copyright (c) 2012-2015 Apcera Inc.
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
    partial class Connection
    {

        #region Passive

        /// <summary>
        /// Expresses interest in the given <paramref name="subject"/> to the NATS Server.
        /// </summary>
        /// <remarks>
        /// <para>The passive fashion requires some external actor to "pull" the incoming <see cref="MsgOut"/>.</para>
        /// </remarks>
        /// <param name="subject">The subject on which to listen for messages.
        /// The subject can have wildcards (partial: <c>*</c>, full: <c>&gt;</c>).</param>
        /// <returns>An <see cref="IPassiveSubscription"/> to use to read any messages received
        /// from the NATS Server on the given <paramref name="subject"/>.</returns>
        /// <seealso cref="ISubscription.Subject"/>
        public IPassiveSubscription SubscribePassive(
            string subject
            )
        {
            return this._subPool.SubscribePassive(
                subject, 
                null
                );
        }


        /// <summary>
        /// Creates a synchronous queue subscriber on the given <paramref name="subject"/>.
        /// </summary>
        /// <remarks>
        /// <para>The passive fashion requires some external actor to "pull" the incoming <see cref="MsgOut"/>.</para>
        /// <para>All subscribers with the same queue name will form the queue group and
        /// only one member of the group will be selected to receive any given message.</para>
        /// </remarks>
        /// <param name="subject">The subject on which to listen for messages.</param>
        /// <param name="queue">The name of the queue group in which to participate.</param>
        /// <returns>An <see cref="IPassiveSubscription"/> to use to read any messages received
        /// from the NATS Server on the given <paramref name="subject"/>, as part of 
        /// the given queue group.</returns>
        /// <seealso cref="ISubscription.Subject"/>
        /// <seealso cref="ISubscription.Queue"/>
        public IPassiveSubscription SubscribePassive(
            string subject,
            string queue
            )
        {
            return this._subPool.SubscribePassive(
                subject,
                queue
                );
        }

        #endregion


        #region Reactive

        /// <summary>
        /// Expresses interest in the given <paramref name="subject"/> to the NATS Server, and begins delivering
        /// messages to the given event handler.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="IReactiveSubscription"/> returned will start delivering messages
        /// to the event handler as soon as they are received.</para>
        /// <para>The reactive fashion expects some external handler to "fire" the incoming <see cref="MsgOut"/>.</para>
        /// </remarks>
        /// <param name="subject">The subject on which to listen for messages.
        /// The subject can have wildcards (partial: <c>*</c>, full: <c>&gt;</c>).</param>
        /// <param name="handler">The <see cref="Action{MsgOut, CancellationToken}"/> invoked when messages are received 
        /// on the returned <see cref="IReactiveSubscription"/>.</param>
        /// <returns>An <see cref="IReactiveSubscription"/> to use to read any messages received
        /// from the NATS Server on the given <paramref name="subject"/>.</returns>
        public IReactiveSubscription SubscribeReactive(
            string subject,
            Action<MsgOut, CancellationToken> handler
            )
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return this._subPool.SubscribeReactive(
                subject,
                queue: null,
                syncHandler: handler,
                asyncHandler: null
                );
        }


        /// <summary>
        /// Expresses interest in the given <paramref name="subject"/> to the NATS Server, and begins delivering
        /// messages to the given event handler.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="IReactiveSubscription"/> returned will start delivering messages
        /// to the event handler as soon as they are received.</para>
        /// <para>The reactive fashion expects some external handler to "fire" the incoming <see cref="MsgOut"/>.</para>
        /// </remarks>
        /// <param name="subject">The subject on which to listen for messages.
        /// The subject can have wildcards (partial: <c>*</c>, full: <c>&gt;</c>).</param>
        /// <param name="queue">The name of the queue group in which to participate.</param>
        /// <returns>An <see cref="IReactiveSubscription"/> to use to read any messages received
        /// from the NATS Server on the given <paramref name="subject"/>.</param>
        /// <param name="handler">The <see cref="Action{MsgOut, CancellationToken}"/> invoked when messages are received 
        /// on the returned <see cref="IReactiveSubscription"/>.</param>
        /// <returns>An <see cref="IReactiveSubscription"/> to use to read any messages received
        /// from the NATS Server on the given <paramref name="subject"/>.</returns>
        public IReactiveSubscription SubscribeReactive(
            string subject, 
            string queue,
            Action<MsgOut, CancellationToken> handler
            )
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return this._subPool.SubscribeReactive(
                subject, 
                queue, 
                syncHandler: handler,
                asyncHandler: null
                );
        }


        /// <summary>
        /// Expresses interest in the given <paramref name="subject"/> to the NATS Server, and begins delivering
        /// messages to the given event handler.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="IReactiveSubscription"/> returned will start delivering messages
        /// to the event handler as soon as they are received.</para>
        /// <para>The reactive fashion expects some external handler to "fire" the incoming <see cref="MsgOut"/>.</para>
        /// </remarks>
        /// <param name="subject">The subject on which to listen for messages.
        /// The subject can have wildcards (partial: <c>*</c>, full: <c>&gt;</c>).</param>
        /// <param name="handler">The <see cref="Func{MsgOut, CancellationToken, Task}"/> invoked when messages are received 
        /// on the returned <see cref="IReactiveSubscription"/>.</param>
        /// <returns>An <see cref="IReactiveSubscription"/> to use to read any messages received
        /// from the NATS Server on the given <paramref name="subject"/>.</returns>
        public IReactiveSubscription SubscribeAsyncReactive(
            string subject,
            Func<MsgOut, CancellationToken, Task> handler
            )
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return this._subPool.SubscribeReactive(
                subject,
                queue: null,
                syncHandler: null,
                asyncHandler: handler
                );
        }


        /// <summary>
        /// Expresses interest in the given <paramref name="subject"/> to the NATS Server, and begins delivering
        /// messages to the given event handler.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="IReactiveSubscription"/> returned will start delivering messages
        /// to the event handler as soon as they are received.</para>
        /// <para>The reactive fashion expects some external handler to "fire" the incoming <see cref="MsgOut"/>.</para>
        /// </remarks>
        /// <param name="subject">The subject on which to listen for messages.
        /// The subject can have wildcards (partial: <c>*</c>, full: <c>&gt;</c>).</param>
        /// <param name="queue">The name of the queue group in which to participate.</param>
        /// <returns>An <see cref="IReactiveSubscription"/> to use to read any messages received
        /// from the NATS Server on the given <paramref name="subject"/>.</param>
        /// <param name="handler">The <see cref="Func{MsgOut, CancellationToken, Task}"/> invoked when messages are received 
        /// on the returned <see cref="IReactiveSubscription"/>.</param>
        /// <returns>An <see cref="IReactiveSubscription"/> to use to read any messages received
        /// from the NATS Server on the given <paramref name="subject"/>.</returns>
        public IReactiveSubscription SubscribeAsyncReactive(
            string subject, 
            string queue, 
            Func<MsgOut, CancellationToken, Task> handler
            )
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return this._subPool.SubscribeReactive(
                subject, 
                queue, 
                syncHandler: null,
                asyncHandler: handler
                );
        }

        #endregion


        /// <summary>
        /// Creates an inbox string which can be used for directed replies from subscribers.
        /// </summary>
        /// <remarks>
        /// The returned inboxes are guaranteed to be unique, but can be shared and subscribed
        /// to by others.
        /// </remarks>
        /// <returns>A unique inbox string.</returns>
        public string NewInbox() => this._rrmgr.NewInbox();

    }
}
