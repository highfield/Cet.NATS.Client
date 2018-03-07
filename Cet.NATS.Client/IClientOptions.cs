
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
using System.Net.Security;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Used to expose a read-only version of the NATS client configuration
    /// </summary>
    public interface IClientOptions
    {
        /// <summary>
        /// Represents the method that will handle an event raised
        /// whenever the client connection state changes.
        /// </summary>
        Action<StateChangedEventArgs> StateChangedEventHandler { get; set; }


        /// <summary>
        /// Represents the method that will handle an event raised
        /// whenever a new server has joined the cluster.
        /// </summary>
        Action<ServerDiscoveredEventArgs> ServerDiscoveredEventHandler { get; set; }


        /// <summary>
        /// Represents the method that will handle an event raised 
        /// when an error occurs out of band.
        /// </summary>
        Action<AsyncErrorEventArgs> AsyncErrorEventHandler { get; set; }


        /// <summary>
        /// Overrides the default NATS RemoteCertificationValidationCallback.
        /// </summary>
        /// <remarks>
        /// The default callback simply checks if there were any protocol
        /// errors. Overriding this callback is useful during testing, or accepting self
        /// signed certificates.
        /// </remarks>
        RemoteCertificateValidationCallback TLSRemoteCertificationValidationCallback { get; set; }


        /// <summary>
        /// Gets the array of servers that the NATs client will connect to.
        /// </summary>
        IReadOnlyCollection<ServerEndPoint> Servers { get; }


        /// <summary>
        /// Gets a value indicating whether or not the server chosen for connection
        /// should not be selected randomly.
        /// </summary>
        bool NoRandomize { get; }


        /// <summary>
        /// Gets the name of this client.
        /// </summary>
        string Name { get; }


        /// <summary>
        /// Gets a value indicating whether or not logging information should be verbose.
        /// </summary>
        bool Verbose { get; }


        /// <summary>
        /// This option is not used by the NATS Client.
        /// </summary>
        bool Pedantic { get; }


        /// <summary>
        /// Gets a value indicating whether or not the old
        /// request pattern should be used.
        /// </summary>
        /// <remarks>
        /// The old request pattern involved a separate subscription
        /// per request inbox. The new style (default) involves creating
        /// a single inbox subscription per connection, upon the first
        /// request, and mapping outbound requests over that one
        /// subscription.
        /// </remarks>
        bool UseOldRequestStyle { get; }


        /// <summary>
        /// Gets the maxmimum number of times a connection will
        /// attempt to reconnect.
        /// </summary>
        int MaxReconnect { get; }


        /// <summary>
        /// Gets the amount of time, in milliseconds, the client will 
        /// wait before attempting a reconnection.
        /// </summary>
        TimeSpan ReconnectWait { get; }


        /// <summary>
        /// Getsthe interval, in milliseconds, pings will be sent to the server.
        /// </summary>
        /// <remarks>
        /// Take care to coordinate this value with the server's interval.
        /// </remarks>
        TimeSpan PingInterval { get; }


        /// <summary>
        /// Gets the timeout, in milliseconds, when connecting to a NATS server.
        /// </summary>
        TimeSpan Timeout { get; }


        /// <summary>
        /// Gets the maximum number of outstanding pings before
        /// terminating a connection.
        /// </summary>
        int MaxPingsOut { get; }


        /// <summary>
        /// Gets the maximum number of message pending in the subscribers' delivery queue
        /// </summary>
        int PendingMessagesLimit { get; }


        /// <summary>
        /// Gets the maximum number of bytes pending in the subscribers' delivery queue
        /// </summary>
        long PendingBytesLimit { get; }

    }
}
