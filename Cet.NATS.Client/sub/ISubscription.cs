﻿
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

namespace Cet.NATS.Client
{
    /// <summary>
    /// Represents interest in a NATS topic.
    /// </summary>
    /// <remarks>
    /// <para>Subscriptions represent interest in a topic on a NATS Server or cluster of
    /// NATS Servers. Subscriptions can be exact or include wildcards. A subscriber can
    /// process a NATS message synchronously (<see cref="IPassiveSubscription"/>) or asynchronously
    /// (<see cref="IReactiveSubscription"/>).</para>
    /// </remarks>
    /// <seealso cref="IPassiveSubscription"/>
    /// <seealso cref="IReactiveSubscription"/>
    public interface ISubscription
    {
        /// <summary>
        /// Gets the subject for this subscription.
        /// </summary>
        /// <remarks><para>Subject names, including reply subject (INBOX) names, are case-sensitive
        /// and must be non-empty alphanumeric strings with no embedded whitespace, and optionally
        /// token-delimited using the dot character (<c>.</c>), e.g.: <c>FOO</c>, <c>BAR</c>,
        /// <c>foo.BAR</c>, <c>FOO.BAR</c>, and <c>FOO.BAR.BAZ</c> are all valid subject names, while:
        /// <c>FOO. BAR</c>, <c>foo. .bar</c> and <c>foo..bar</c> are <em>not</em> valid subject names.</para>
        /// <para>NATS supports the use of wildcards in subject subscriptions.</para>
        /// <list>
        /// <item>The asterisk character (<c>*</c>) matches any token at any level of the subject.</item>
        /// <item>The greater than symbol (<c>&gt;</c>), also known as the <em>full wildcard</em>, matches
        /// one or more tokens at the tail of a subject, and must be the last token. The wildcard subject
        /// <c>foo.&gt;</c> will match <c>foo.bar</c> or <c>foo.bar.baz.1</c>, but not <c>foo</c>.</item>
        /// <item>Wildcards must be separate tokens (<c>foo.*.bar</c> or <c>foo.&gt;</c> are syntactically
        /// valid; <c>foo*.bar</c>, <c>f*o.b*r</c> and <c>foo&gt;</c> are not).</item>
        /// </list>
        /// <para>For example, the wildcard subscrpitions <c>foo.*.quux</c> and <c>foo.&gt;</c> both match
        /// <c>foo.bar.quux</c>, but only the latter matches <c>foo.bar.baz</c>. With the full wildcard,
        /// it is also possible to express interest in every subject that may exist in NATS (<c>&gt;</c>).</para>
        /// </remarks>
        string Subject { get; }


        /// <summary>
        /// Gets the optional queue group name.
        /// </summary>
        /// <remarks>
        /// <para>If present, all subscriptions with the same name will form a distributed queue, and each message will only
        /// be processed by one member of the group. Although queue groups have multiple subscribers,
        /// each message is consumed by only one.</para>
        /// </remarks>
        string Queue { get; }


        /// <summary>
        /// Gets the <see cref="IConnection"/> associated with this instance.
        /// </summary>
        IConnection Connection { get; }


        /// <summary>
        /// Gets a value indicating whether or not the <see cref="ISubscription"/> is still valid.
        /// </summary>
        bool IsValid { get; }


        /// <summary>
        /// Gets whether the <see cref="MsgOut"/> host consumer is too slow
        /// to manage the incoming messages
        /// </summary>
        bool IsSlowConsumer { get; }


        /// <summary>
        /// Removes interest in the <see cref="Subject"/>.
        /// </summary>
        void Unsubscribe();


        /// <summary>
        /// Issues an automatic call to <see cref="Unsubscribe"/> when <paramref name="max"/> messages have been
        /// received.
        /// </summary>
        /// <remarks>This can be useful when sending a request to an unknown number of subscribers.
        /// <see cref="Connection"/>'s Request methods use this functionality.</remarks>
        /// <param name="max">The maximum number of messages to receive on the subscription before calling
        /// <see cref="Unsubscribe"/>. Values less than or equal to zero (<c>0</c>) unsubscribe immediately.</param>
        void AutoUnsubscribe(int max);


        /// <summary>
        /// Sets the limits for pending messages and bytes for this instance.
        /// </summary>
        /// <remarks>Zero (<c>0</c>) is not allowed. Negative values indicate that the
        /// given metric is not limited.</remarks>
        /// <param name="messageLimit">The maximum number of pending messages.</param>
        /// <param name="bytesLimit">The maximum number of pending bytes of payload.</param>
        void SetPendingLimits(int messageLimit, long bytesLimit);


        /// <summary>
        /// Gets a <see cref="SubscriptionStatsInfo"/> block with the stats
        /// </summary>
        /// <returns></returns>
        SubscriptionStatsInfo GetStats();


        /// <summary>
        /// Resets the stats
        /// </summary>
        void ResetStats();

    }
}
