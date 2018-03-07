
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
    /// Tracks individual backend servers.
    /// </summary>
    internal sealed class ServerConnectionStatus
    {
        /// <summary>
        /// Creates an instance of <see cref="ServerConnectionStatus"/>
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="isImplicit"></param>
        internal ServerConnectionStatus(
            ServerEndPoint endPoint,
            bool isImplicit
            )
        {
            this.EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.IsImplicit = isImplicit;
            this.UpdateLastAttempt();
        }


        /// <summary>
        /// The server connection endpoint
        /// </summary>
        internal ServerEndPoint EndPoint { get; }


        /// <summary>
        /// Indicates whether this server endpoint was discovered by the NATS broker.
        /// </summary>
        internal bool IsImplicit { get; }
        internal DateTime LastAttempt { get; private set; }


        /// <summary>
        /// Indicates whether the server was connected at least once
        /// </summary>
        internal bool DidConnect = false;


        /// <summary>
        /// Number of attempt to reconnect the server
        /// </summary>
        internal int ReconnectionCount = 0;


        /// <summary>
        /// Elapsed time since last reconnection attempt
        /// </summary>
        internal TimeSpan TimeSinceLastAttempt => DateTime.Now - this.LastAttempt;


        /// <summary>
        /// Updates the <see cref="LastAttempt"/> field
        /// </summary>
        internal void UpdateLastAttempt()
        {
            this.LastAttempt = DateTime.Now;
        }

    }
}

