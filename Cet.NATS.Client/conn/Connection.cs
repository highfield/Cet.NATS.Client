
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
    internal enum ClientProtcolVersion
    {
        /// <summary>
        /// ClientProtoZero is the original client protocol from 2009.
        /// </summary>
        /// <remarks>
        /// see: http://nats.io/documentation/internals/nats-protocol/
        /// </remarks>
        ClientProtoZero = 0,

        /// <summary>
        /// ClientProtoInfo signals a client can receive more then the original INFO block.
        /// This can be used to update clients on other cluster members, etc.
        /// </summary>
        ClientProtoInfo = 1,
    }


    /// <summary>
    /// <see cref="Connection"/> represents a bare connection to a NATS server.
    /// Users should create an <see cref="IConnection"/> instance using
    /// <see cref="ConnectionFactory"/> rather than directly using this class.
    /// </summary>
    internal sealed partial class Connection
        : IConnection, IDisposable
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class
        /// with the specified <see cref="ClientOptions"/>.
        /// </summary>
        /// <param name="options">The configuration options to use for this 
        /// <see cref="Connection"/>.</param>
        internal Connection(ClientOptions options)
        {
            this._options = new ClientOptions(options);
            this._connmgr = new ConnectionManager(this);
            this._subPool = new SubscriptionPool(this);
            this._rrmgr = new RequestReplyManager(this);
        }


        internal ConnectionManager ConnMgr => this._connmgr;
        private readonly ConnectionManager _connmgr;


        internal SubscriptionPool SubPool => this._subPool;
        private readonly SubscriptionPool _subPool;


        internal RequestReplyManager RequestReplyManager => this._rrmgr;
        private readonly RequestReplyManager _rrmgr;


        /// <summary>
        /// Gets a copy of the configuration options for this instance.
        /// </summary>
        public IClientOptions Options => this._options;
        internal ClientOptions OptionsInternal => this._options;
        private readonly ClientOptions _options;


        /// <summary>
        /// Gets the current state of the <see cref="Connection"/>.
        /// </summary>
        /// <seealso cref="ConnState"/>
        public ConnState State => this._connmgr.State;


        /// <summary>
        /// Gets the endpoint of the NATS server to which this instance
        /// is connected, otherwise <c>null</c>.
        /// </summary>
        public ServerEndPoint ConnectedServer => this._connmgr.SrvPool.CurrentServer?.EndPoint;


        /// <summary>
        /// Gets the server ID of the NATS server to which this instance
        /// is connected, otherwise <c>null</c>.
        /// </summary>
        public string ConnectedId => this._connmgr.ConnectedId;


        /// <summary>
        /// Gets the maximum size in bytes of a payload sent
        /// to the connected NATS Server.
        /// </summary>
        public long MaxPayload => this._connmgr.MaxPayload;


        /// <summary>
        /// Gets an array of known server URIs for this instance.
        /// </summary>
        /// <remarks><see cref="Servers"/> also includes any additional
        /// servers discovered after a connection has been established.</remarks>
        public IReadOnlyCollection<ServerEndPoint> GetKnownServers() => this._connmgr.SrvPool.GetServerList(implicitOnly: false);


        /// <summary>
        /// Gets an array of server URIs that were discovered after this
        /// instance connected.
        /// </summary>
        public IReadOnlyCollection<ServerEndPoint> GetDiscoveredServers() => this._connmgr.SrvPool.GetServerList(implicitOnly: true);


        /// <summary>
        /// Gets the statistics tracked for the <see cref="Connection"/>.
        /// </summary>
        /// <seealso cref="ResetStats"/>
        public ConnectionStatsInfo GetStats() => this._connmgr.GetStats();


        /// <summary>
        /// Resets the associated statistics for the <see cref="Connection"/>.
        /// </summary>
        /// <seealso cref="GetStats"/>
        public void ResetStats() => this._connmgr.ResetStats();


        /// <summary>
        /// Starts the connection handshake against the NATS server.
        /// </summary>
        public void Start() => this._connmgr.Start();


        /// <summary>
        /// Starts asynchronously the connection handshake against the NATS server.
        /// </summary>
        public Task StartAsync(CancellationToken token) => this._connmgr.StartAsync(token);


        /// <summary>
        /// Stops the job of the client, and also shuts down the connection.
        /// </summary>
        public void Stop() => this._connmgr.Stop(disposing: false);


        /// <summary>
        /// Stops asynchronously the job of the client, and also shuts down the connection.
        /// </summary>
        public Task StopAsync() => this._connmgr.StopAsync();


        #region IDisposable Support

        // To detect redundant calls
        private bool disposedValue = false;

        /// <summary>
        /// Closes the connection and optionally releases the managed resources.
        /// </summary>
        /// <remarks>In derived classes, do not override the <see cref="Close"/> method, instead
        /// put all of the <seealso cref="Connection"/> cleanup logic in your Dispose override.</remarks>
        /// <param name="disposing"><c>true</c> to release both managed
        /// and unmanaged resources; <c>false</c> to release only unmanaged 
        /// resources.</param>
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;

                if (disposing)
                {
                    this._rrmgr.Dispose();
                    this._subPool.Dispose();
                    this._connmgr.Dispose();
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Connection"/>.
        /// </summary>
        /// <remarks>This method disposes the connection, by clearing all pending
        /// operations, and closing the connection to release resources.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
