
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
    /// State of the <see cref="IConnection"/>.
    /// </summary>
    public enum ConnState
    {
        /// <summary>
        /// The <see cref="IConnection"/> is disconnected.
        /// </summary>
        DISCONNECTED = 0,

        /// <summary>
        /// The <see cref="IConnection"/> is connected to a NATS Server.
        /// </summary>
        CONNECTED,

        /// <summary>
        /// The <see cref="IConnection"/> has been closed.
        /// </summary>
        CLOSED,

        /// <summary>
        /// The <see cref="IConnection"/> is currently reconnecting
        /// to a NATS Server.
        /// </summary>
        RECONNECTING,

        /// <summary>
        /// The <see cref="IConnection"/> is currently connecting
        /// to a NATS Server.
        /// </summary>
        CONNECTING
    }


    /// <summary>
    /// Represents a connection to the NATS server.
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Gets the configuration options for this instance.
        /// </summary>
        IClientOptions Options { get; }


        /// <summary>
        /// Gets the endpoint of the NATS server to which this instance
        /// is connected, otherwise <c>null</c>.
        /// </summary>
        ServerEndPoint ConnectedServer { get; }


        /// <summary>
        /// Gets the server ID of the NATS server to which this instance
        /// is connected, otherwise <c>null</c>.
        /// </summary>
        string ConnectedId { get; }


        /// <summary>
        /// Gets an array of known server endpoints for this instance.
        /// </summary>
        /// <remarks><see cref="Servers"/> also includes any additional
        /// servers discovered after a connection has been established.</remarks>
        IReadOnlyCollection<ServerEndPoint> GetKnownServers();


        /// <summary>
        /// Gets an array of server endpoints that were discovered after this
        /// instance connected.
        /// </summary>
        IReadOnlyCollection<ServerEndPoint> GetDiscoveredServers();


        /// <summary>
        /// Publishes a <see cref="MsgIn"/> instance, which includes the subject, an optional reply, and an
        /// optional data field.
        /// </summary>
        /// <remarks>
        /// <para>NATS implements a publish-subscribe message distribution model. NATS publish subscribe is a
        /// one-to-many communication. A publisher sends a message on a subject. Any active subscriber listening
        /// on that subject receives the message. Subscribers can register interest in wildcard subjects.</para>
        /// <para>In the basic NATS platfrom, if a subscriber is not listening on the subject (no subject match),
        /// or is not acive when the message is sent, the message is not received. NATS is a fire-and-forget
        /// messaging system. If you need higher levels of service, you can either use NATS Streaming, or build the
        /// additional reliability into your client(s) yourself.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        void Publish(MsgIn message, CancellationToken token);


        /// <summary>
        /// Publishes a <see cref="MsgIn"/> instance, which includes the subject, an optional reply, and an
        /// optional data field.
        /// </summary>
        /// <remarks>
        /// <para>NATS implements a publish-subscribe message distribution model. NATS publish subscribe is a
        /// one-to-many communication. A publisher sends a message on a subject. Any active subscriber listening
        /// on that subject receives the message. Subscribers can register interest in wildcard subjects.</para>
        /// <para>In the basic NATS platfrom, if a subscriber is not listening on the subject (no subject match),
        /// or is not acive when the message is sent, the message is not received. NATS is a fire-and-forget
        /// messaging system. If you need higher levels of service, you can either use NATS Streaming, or build the
        /// additional reliability into your client(s) yourself.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="timeout">Represents the time to wait for the connection going stable.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        void Publish(MsgIn message, TimeSpan timeout, CancellationToken token);


        /// <summary>
        /// Sends a request payload and returns the response <see cref="MsgOut"/>, or throws
        /// <see cref="NATSTimeoutException"/> if the <paramref name="timeout"/> expires.
        /// </summary>
        /// <remarks>
        /// <para>NATS supports two flavors of request-reply messaging: point-to-point or one-to-many. Point-to-point
        /// involves the fastest or first to respond. In a one-to-many exchange, you set a limit on the number of 
        /// responses the requestor may receive and instead must use a subscription (<see cref="ISubscription.AutoUnsubscribe(int)"/>).
        /// In a request-response exchange, publish request operation publishes a message with a reply subject expecting
        /// a response on that reply subject.</para>
        /// <para><see cref="Request(MsgIn, CancellationToken)"/> will create an unique inbox for this request, sharing a single
        /// subscription for all replies to this <see cref="Connection"/> instance. However, if 
        /// <see cref="ClientOptions.UseOldRequestStyle"/> is set, each request will have its own underlying subscription. 
        /// The old behavior is not recommended as it may cause unnecessary overhead on connected NATS servers.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A <see cref="MsgOut"/> with the response from the NATS server.</returns>
        MsgOut Request(MsgIn message, CancellationToken token);


        /// <summary>
        /// Sends a request payload and returns the response <see cref="MsgOut"/>, or throws
        /// <see cref="NATSTimeoutException"/> if the <paramref name="timeout"/> expires.
        /// </summary>
        /// <remarks>
        /// <para>NATS supports two flavors of request-reply messaging: point-to-point or one-to-many. Point-to-point
        /// involves the fastest or first to respond. In a one-to-many exchange, you set a limit on the number of 
        /// responses the requestor may receive and instead must use a subscription (<see cref="ISubscription.AutoUnsubscribe(int)"/>).
        /// In a request-response exchange, publish request operation publishes a message with a reply subject expecting
        /// a response on that reply subject.</para>
        /// <para><see cref="Request(MsgIn, TimeSpan, CancellationToken)"/> will create an unique inbox for this request, sharing a single
        /// subscription for all replies to this <see cref="Connection"/> instance. However, if 
        /// <see cref="ClientOptions.UseOldRequestStyle"/> is set, each request will have its own underlying subscription. 
        /// The old behavior is not recommended as it may cause unnecessary overhead on connected NATS servers.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="timeout">Represents the overall time to wait for the connection going stable, if not, and for the message loopback.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A <see cref="MsgOut"/> with the response from the NATS server.</returns>
        MsgOut Request(MsgIn message, TimeSpan timeout, CancellationToken token);


        /// <summary>
        /// Sends a request payload and returns the response <see cref="MsgOut"/>, or throws
        /// <see cref="NATSTimeoutException"/> if the <paramref name="timeout"/> expires.
        /// </summary>
        /// <remarks>
        /// <para>NATS supports two flavors of request-reply messaging: point-to-point or one-to-many. Point-to-point
        /// involves the fastest or first to respond. In a one-to-many exchange, you set a limit on the number of 
        /// responses the requestor may receive and instead must use a subscription (<see cref="ISubscription.AutoUnsubscribe(int)"/>).
        /// In a request-response exchange, publish request operation publishes a message with a reply subject expecting
        /// a response on that reply subject.</para>
        /// <para><see cref="RequestAsync(MsgIn, CancellationToken)"/> will create an unique inbox for this request, sharing a single
        /// subscription for all replies to this <see cref="Connection"/> instance. However, if 
        /// <see cref="ClientOptions.UseOldRequestStyle"/> is set, each request will have its own underlying subscription. 
        /// The old behavior is not recommended as it may cause unnecessary overhead on connected NATS servers.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the <see cref="Task{TResult}.Result"/>
        /// parameter contains a <see cref="MsgOut"/> with the response from the NATS server.</returns>
        Task<MsgOut> RequestAsync(MsgIn message, CancellationToken token);


        /// <summary>
        /// Sends a request payload and returns the response <see cref="MsgOut"/>, or throws
        /// <see cref="NATSTimeoutException"/> if the <paramref name="timeout"/> expires.
        /// </summary>
        /// <remarks>
        /// <para>NATS supports two flavors of request-reply messaging: point-to-point or one-to-many. Point-to-point
        /// involves the fastest or first to respond. In a one-to-many exchange, you set a limit on the number of 
        /// responses the requestor may receive and instead must use a subscription (<see cref="ISubscription.AutoUnsubscribe(int)"/>).
        /// In a request-response exchange, publish request operation publishes a message with a reply subject expecting
        /// a response on that reply subject.</para>
        /// <para><see cref="RequestAsync(MsgIn, TimeSpan, CancellationToken)"/> will create an unique inbox for this request, sharing a single
        /// subscription for all replies to this <see cref="Connection"/> instance. However, if 
        /// <see cref="ClientOptions.UseOldRequestStyle"/> is set, each request will have its own underlying subscription. 
        /// The old behavior is not recommended as it may cause unnecessary overhead on connected NATS servers.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="timeout">Represents the overall time to wait for the connection going stable, if not, and for the message loopback.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the <see cref="Task{TResult}.Result"/>
        /// parameter contains a <see cref="MsgOut"/> with the response from the NATS server.</returns>
        Task<MsgOut> RequestAsync(MsgIn message, TimeSpan timeout, CancellationToken token);


        /// <summary>
        /// Creates an inbox string which can be used for directed replies from subscribers.
        /// </summary>
        /// <remarks>
        /// The returned inboxes are guaranteed to be unique, but can be shared and subscribed
        /// to by others.
        /// </remarks>
        /// <returns>A unique inbox string.</returns>
        string NewInbox();


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
        IPassiveSubscription SubscribePassive(string subject);


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
        IPassiveSubscription SubscribePassive(string subject, string queue);


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
        IReactiveSubscription SubscribeReactive(string subject, Action<MsgOut, CancellationToken> handler);


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
        IReactiveSubscription SubscribeReactive(string subject, string queue, Action<MsgOut, CancellationToken> handler);


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
        IReactiveSubscription SubscribeAsyncReactive(string subject, Func<MsgOut, CancellationToken, Task> handler);


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
        IReactiveSubscription SubscribeAsyncReactive(string subject, string queue, Func<MsgOut, CancellationToken, Task> handler);


        /// <summary>
        /// Starts the connection handshake against the NATS server.
        /// </summary>
        /// <remarks>
        /// This method returns immediately, before the actual connection setup 
        /// and the successful completion of the handshake
        /// </remarks>
        void Start();


        /// <summary>
        /// Starts asynchronously the connection handshake against the NATS server.
        /// </summary>
        /// <remarks>
        /// This method returns only after the actual connection setup 
        /// and the successful completion of the handshake.
        /// </remarks>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>The <see cref="Task"/> to await.</returns>
        Task StartAsync(CancellationToken token);


        /// <summary>
        /// Stops the job of the client, and also shuts down the connection.
        /// </summary>
        /// <remarks>
        /// This method returns only after the stop process has been completed.
        /// </remarks>
        void Stop();


        /// <summary>
        /// Stops asynchronously the job of the client, and also shuts down the connection.
        /// </summary>
        /// <remarks>
        /// This method returns only after the stop process has been completed.
        /// </remarks>
        /// <returns>The <see cref="Task"/> to await.</returns>
        Task StopAsync();


        /// <summary>
        /// Gets the current state of the <see cref="IConnection"/>.
        /// </summary>
        /// <seealso cref="ConnState"/>
        ConnState State { get; }


        /// <summary>
        /// Gets the statistics tracked for the <see cref="IConnection"/>.
        /// </summary>
        /// <seealso cref="ResetStats"/>
        ConnectionStatsInfo GetStats();


        /// <summary>
        /// Resets the associated statistics for the <see cref="IConnection"/>.
        /// </summary>
        /// <seealso cref="GetStats"/>
        void ResetStats();


        /// <summary>
        /// Gets the maximum size in bytes of any payload sent
        /// to the connected NATS Server.
        /// </summary>
        long MaxPayload { get; }

    }
}
