
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
using System.Net;

namespace Cet.NATS.Client
{
    /// <summary>
    /// This class contains default values for fields used throughout NATS.
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Client version
        /// </summary>
        public const string Version = "0.0.1";

        /// <summary>
        /// The default NATS connect url ("nats://localhost:4222")
        /// </summary>
        public static readonly IPAddress Address = IPAddress.Parse("127.0.0.1");
        //public static readonly Uri Uri = new Uri("nats://localhost:4222");

        /// <summary>
        /// The default NATS connect port. (4222)
        /// </summary>
        public const int Port = 4222;

        /// <summary>
        /// Default number of times to attempt a reconnect. (60)
        /// </summary>
        public const int MaxReconnect = 60;

        /// <summary>
        /// Default ReconnectWait time
        /// </summary>
        public static readonly TimeSpan ReconnectWait = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Default timeout
        /// </summary>
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        /// <summary>
        ///  Default ping interval (2 minutes);
        /// </summary>
        public static readonly TimeSpan PingInterval = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Default MaxPingOut value (2);
        /// </summary>
        public const int MaxPingOut = 2;

        /// <summary>
        /// Language string of this client, ".NET"
        /// </summary>
        public const string LangString = ".NET";

        /// <summary>
        /// Default subscriber pending messages limit.
        /// </summary>
        public const int SubPendingMsgsLimit = 65536;

        /// <summary>
        /// Default subscriber pending bytes limit.
        /// </summary>
        public const long SubPendingBytesLimit = 65536 * 1024;

        /*
         * Namespace level defaults
         */

        // The size of the bufio writer on top of the socket.
        internal const int defaultBufSize = 32768;

        // The read size from the network stream.
        internal const int defaultReadLength = 20480;

    }
}
