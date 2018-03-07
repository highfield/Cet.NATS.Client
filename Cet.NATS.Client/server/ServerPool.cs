
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Used to host the server endpoints pool, and to connect to one of them.
    /// </summary>
    internal sealed class ServerPool
    {
        private ServerPool(ClientOptions options)
        {
            this._options = options;
        }


        /// <summary>
        /// Creates an instance of <see cref="ServerPool"/>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        internal static ServerPool Create(
            ClientOptions options
            )
        {
            //collects the defined server endpoints
            List<ServerEndPoint> definedEndPoints = options.Servers
                .Distinct()
                .ToList();

            if (definedEndPoints.Count == 0)
            {
                //none was defined, so adds the default one
                definedEndPoints.Add(ServerEndPoint.Default());
            }
            else if (options.NoRandomize == false)
            {
                //gives a shuffle to the list
                Shuffle(definedEndPoints);
            }

            //creates the main instance with the effective collection
            var instance = new ServerPool(options);
            foreach (ServerEndPoint ep in definedEndPoints)
            {
                instance._servers.AddLast(
                    new ServerConnectionStatus(ep, isImplicit: false)
                    );
            }
            return instance;
        }


        private readonly ClientOptions _options;
        private readonly LinkedList<ServerConnectionStatus> _servers = new LinkedList<ServerConnectionStatus>();
        private readonly object _locker = new object();


        /// <summary>
        /// Exposes the current server status
        /// </summary>
        internal ServerConnectionStatus CurrentServer => this._currentServer;
        private ServerConnectionStatus _currentServer = null;


        /// <summary>
        /// Callback to update the stats
        /// </summary>
        internal Action IncrementStatsCounter;


        /// <summary>
        /// Main entry-point to connect to a server
        /// </summary>
        /// <param name="initialPresentation">Indicates whether it's the very first attempt to connect,
        /// or rather some retry due to a connection drop.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>The <see cref="TcpConnection"/> useful to exchange data with the server.</returns>
        internal TcpConnection ConnectToAServer(
            bool initialPresentation,
            CancellationToken token
            )
        {
            //select an iterator against the proper server list
            IEnumerator<ServerConnectionStatus> iter;
            lock (this._locker)
            {
                if (initialPresentation)
                {
                    iter = this._servers.GetEnumerator();
                }
                else
                {
                    iter = new ServerStatusEnumerator(this);
                }
            }

            //task cancelation will raise an exception
            token.ThrowIfCancellationRequested();

            Exception lastex = null;
            while (iter.MoveNext())
            {
                ServerConnectionStatus s = iter.Current;
                try
                {
                    //connection attempt
                    var tcp = TcpConnection.Create(this._options, s, token);
                    s.DidConnect = true;
                    s.ReconnectionCount = 0;
                    this._currentServer = s;
                    return tcp;
                }
                catch (OperationCanceledException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                }

                if (initialPresentation == false)
                {
                    //manage stats
                    s.ReconnectionCount++;
                    this.IncrementStatsCounter?.Invoke();

                    //waits before retrying again
                    Helpers.CancelableDelay(this._options.ReconnectWait, token);
                }
            }

            if (lastex != null)
            {
                throw lastex;
            }

            this._currentServer = null;
            return null;
        }


        #region Custom enumerator

        private class ServerStatusEnumerator
            : IEnumerator<ServerConnectionStatus>
        {
            public ServerStatusEnumerator(ServerPool owner)
            {
                this._owner = owner;
            }


            private readonly ServerPool _owner;


            private ServerConnectionStatus _cursor;
            public ServerConnectionStatus Current => this._cursor;
            object IEnumerator.Current => this._cursor;


            public bool MoveNext()
            {
                lock (this._owner._locker)
                {
                    ServerConnectionStatus s = this._cursor;
                    if (s == null) return false;

                    LinkedList<ServerConnectionStatus> servers = this._owner._servers;

                    //remove the current server.
                    servers.Remove(s);

                    if (0 < this._owner._options.MaxReconnect &&
                        s.ReconnectionCount < this._owner._options.MaxReconnect
                        )
                    {
                        //if we haven't surpassed max reconnects, add it to try again.
                        servers.AddLast(s);
                    }

                    this._cursor = servers.FirstOrDefault();
                    return this._cursor != null;
                }
            }


            public void Reset()
            {
                this._cursor = this._owner._currentServer;
            }


            public void Dispose()
            {
                this._cursor = null;
            }
        }

        #endregion


        /// <summary>
        /// Pop the current server and put onto the end of the list. 
        /// Select head of list as long as number of reconnect attempts under MaxReconnect.
        /// </summary>
        /// <returns>The next server endpoint, or null</returns>
        internal ServerConnectionStatus SelectNextServer()
        {
            lock (this._locker)
            {
                ServerConnectionStatus s = this._currentServer;
                if (s == null)
                {
                    return null;
                }

                //remove the current server.
                this._servers.Remove(s);

                if (this._options.MaxReconnect > 0 &&
                    s.ReconnectionCount < this._options.MaxReconnect
                    )
                {
                    //if we haven't surpassed max reconnects, add it to try again.
                    this._servers.AddLast(s);
                }

                this._currentServer = this._servers.FirstOrDefault();
                return this._currentServer;
            }
        }


        /// <summary>
        /// Returns a copy of the list to ensure threadsafety.
        /// </summary>
        /// <param name="implicitOnly">Indicates how to filter the original collection.</param>
        /// <returns></returns>
        internal IReadOnlyCollection<ServerEndPoint> GetServerList(bool implicitOnly)
        {
            lock (this._locker)
            {
                return this._servers
                    .Where(_ => _.IsImplicit || implicitOnly == false)
                    .Select(_ => _.EndPoint)
                    .ToList();
            }
        }


        /// <summary>
        /// Adds a series of endpoints as "implicit" to the official list.
        /// </summary>
        /// <remarks>
        /// Duplicates are not added.
        /// </remarks>
        /// <param name="collection">The "implicit" endpoints to be added.</param>
        /// <returns>Indicates whether any item was actually added.</returns>
        internal bool AddAsImplicit(
            IEnumerable<ServerEndPoint> collection
            )
        {
            lock (this._locker)
            {
                bool added = false;
                foreach (ServerEndPoint ep in collection)
                {
                    if (this._servers.Any(_ => _.Equals(ep))) continue;

                    added = true;
                    this._servers.AddLast(
                        new ServerConnectionStatus(ep, isImplicit: true)
                        );
                }
                return added;
            }
        }


        /// <summary>
        /// Shuffles a generic collection of items
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        internal static void Shuffle<T>(IList<T> list)
        {
            if (list == null) return;

            int n = list.Count;
            if (n <= 1) return;

            var rnd = new Random(DateTime.Now.Millisecond ^ new object().GetHashCode());
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

    }
}
