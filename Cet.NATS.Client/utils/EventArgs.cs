
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
    /// Provides the details when the state of a <see cref="IConnection"/>
    /// changes.
    /// </summary>
    public sealed class StateChangedEventArgs
        : EventArgs
    {
        internal StateChangedEventArgs(Connection conn)
        {
            this._conn = conn;
        }


        /// <summary>
        /// Gets the <see cref="IConnection"/> associated with the event.
        /// </summary>
        public IConnection Connection => this._conn;
        private Connection _conn;

        /// <summary>
        /// Gets the current state of the <see cref="IConnection"/>.
        /// </summary>
        /// <seealso cref="ConnState"/>
        public ConnState State { get; internal set; }
    }


    /// <summary>
    /// Provides the details when the state of a <see cref="IConnection"/>
    /// changes.
    /// </summary>
    public sealed class ServerDiscoveredEventArgs
        : EventArgs
    {
        internal ServerDiscoveredEventArgs() { }

        /// <summary>
        /// Gets the <see cref="IConnection"/> associated with the event.
        /// </summary>
        public IConnection Connection { get; internal set; }

        /// <summary>
        /// Gets the current state of the <see cref="IConnection"/>.
        /// </summary>
        /// <seealso cref="ConnState"/>
        public ConnState State { get; internal set; }
    }


    /// <summary>
    /// Provides the details when the state of a <see cref="IConnection"/>
    /// changes.
    /// </summary>
    public sealed class AsyncErrorEventArgs
        : EventArgs
    {
        internal AsyncErrorEventArgs() { }

        /// <summary>
        /// Gets the <see cref="IConnection"/> associated with the event.
        /// </summary>
        public IConnection Connection { get; internal set; }

        /// <summary>
        /// Gets the current state of the <see cref="IConnection"/>.
        /// </summary>
        /// <seealso cref="ConnState"/>
        public ConnState State { get; internal set; }

        /// <summary>
        /// Gets the error message associated with the event.
        /// </summary>
        public string Error { get; internal set; }

        /// <summary>
        /// Gets the error message associated with the event.
        /// </summary>
        public Exception Exc { get; internal set; }
    }
}
