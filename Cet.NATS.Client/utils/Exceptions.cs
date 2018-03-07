
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

namespace Cet.NATS.Client
{
    /// <summary>
    /// The exception that is thrown when there is a NATS error condition.  All
    /// NATS exception inherit from this class.
    /// </summary>
    public class NATSException : Exception
    {
        internal NATSException() : base() { }
        internal NATSException(string err) : base (err) {}
        internal NATSException(string err, Exception innerEx) : base(err, innerEx) { }
    }


    /// <summary>
    /// The exception that is thrown when there is a connection error.
    /// </summary>
    public class NATSConnectionException : NATSException
    {
        internal NATSConnectionException(string err) : base(err) { }
        internal NATSConnectionException(string err, Exception innerEx) : base(err, innerEx) { }
    }


    /// <summary>
    /// This exception that is thrown when there is an internal error with
    /// the NATS protocol.
    /// </summary>
    public class NATSProtocolException : NATSException
    {
        internal NATSProtocolException(string err) : base(err) { }
    }


    /// <summary>
    /// The exception that is thrown when a connection cannot be made
    /// to any server.
    /// </summary>
    public class NATSNoServersException : NATSException
    {
        internal NATSNoServersException(string err) : base(err) { }
    }


    /// <summary>
    /// The exception that is thrown when a secure connection is requested,
    /// but not required.
    /// </summary>
    public class NATSSecureConnWantedException : NATSException
    {
        internal NATSSecureConnWantedException() : base("A secure connection is requested.") { }
    }


    /// <summary>
    /// The exception that is thrown when a secure connection is required.
    /// </summary>
    public class NATSSecureConnRequiredException : NATSException
    {
        internal NATSSecureConnRequiredException() : base("A secure connection is required.") { }
        internal NATSSecureConnRequiredException(String s) : base(s) { }
    }


    /// <summary>
    /// The exception that is thrown when a an operation is performed on
    /// a connection that is closed.
    /// </summary>
    public class NATSConnectionClosedException : NATSException
    {
        internal NATSConnectionClosedException() : base("Connection is closed.") { }
    }


    /// <summary>
    /// The exception that is thrown when a consumer (subscription) is slow.
    /// </summary>
    public class NATSSlowConsumerException : NATSException
    {
        internal NATSSlowConsumerException() : base("Consumer is too slow.") { }
    }


    /// <summary>
    /// The exception that is thrown when an operation occurs on a connection
    /// that has been determined to be stale.
    /// </summary>
    public class NATSStaleConnectionException : NATSException
    {
        internal NATSStaleConnectionException() : base("Connection is stale.") { }
    }


    /// <summary>
    /// The exception that is thrown when a message payload exceeds what
    /// the maximum configured.
    /// </summary>
    public class NATSMaxPayloadException : NATSException
    {
        internal NATSMaxPayloadException() : base("Maximum payload size has been exceeded") { }
        internal NATSMaxPayloadException(string err) : base(err) { }
    }


    /// <summary>
    /// The exception that is thrown when a subscriber has exceeded the maximum
    /// number of messages that has been configured.
    /// </summary>
    public class NATSMaxMessagesException : NATSException
    {
        internal NATSMaxMessagesException() : base("Maximum number of messages have been exceeded.") { }
    }

    
    /// <summary>
    /// The exception that is thrown when a subscriber operation is performed on
    /// an invalid subscriber.
    /// </summary>
    public class NATSBadSubscriptionException : NATSException
    {
        internal NATSBadSubscriptionException() : base("Subcription is not valid.") { }
    }


    /// <summary>
    /// The exception that is thrown when a NATS operation times out.
    /// </summary>
    public class NATSTimeoutException : NATSException
    {
        internal NATSTimeoutException(string message) : base(message) { }
        internal NATSTimeoutException(string message, Exception inner) : base(message, inner) { }
    }
}