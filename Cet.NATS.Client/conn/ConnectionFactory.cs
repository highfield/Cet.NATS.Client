
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
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Provides factory methods to create connections to NATS Servers.
    /// </summary>
    public sealed class ConnectionFactory
    {

        /// <summary>
        /// Creates a new instance of <see cref="ConnectionFactory"/> using
        /// an instance of <see cref="ClientOptions"/> with its default parameters.
        /// </summary>
        public ConnectionFactory()
        {
            this._defaultOptions = new ClientOptions();
        }


        /// <summary>
        /// Creates an instance of <see cref="ConnectionFactory"/> using the
        /// provided <see cref="ClientOptions"/> as parameters.
        /// </summary>
        /// <param name="options"></param>
        public ConnectionFactory(ClientOptions options)
        {
            this._defaultOptions = options ?? throw new ArgumentNullException(nameof(options));
        }


        /// <summary>
        /// Retrieves the default set of client options.
        /// </summary>
        public IClientOptions DefaultOptions => this._defaultOptions;
        private ClientOptions _defaultOptions;


        /// <summary>
        /// Create a connection to the NATs server using the <see cref="ConnectionFactory"/> options.
        /// </summary>
        /// <remarks>
        /// This method returns immediately, before the actual connection setup 
        /// and the successful completion of the handshake
        /// </remarks>
        /// <returns>An <see cref="IConnection"/> object connected to the NATS server.</returns>
        /// <exception cref="NATSNoServersException">No connection to a NATS Server could be established.</exception>
        /// <exception cref="NATSConnectionException"><para>A timeout occurred connecting to a NATS Server.</para>
        /// <para>-or-</para>
        /// <para>An exception was encountered while connecting to a NATS Server. See <see cref="System.Exception.InnerException"/> for more
        /// details.</para></exception>
        public IConnection CreateConnection()
        {
            if (this._defaultOptions == null)
            {
                throw new Exception();  //TODO refine expection
            }
            return this.CreateConnection(this._defaultOptions);
        }


        /// <summary>
        /// Create a connection to the NATs server using the <see cref="ConnectionFactory"/> options.
        /// </summary>
        /// <remarks>
        /// This method returns only after the actual connection setup 
        /// and the successful completion of the handshake.
        /// </remarks>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IConnection"/> object connected to the NATS server.</returns>
        /// <exception cref="NATSNoServersException">No connection to a NATS Server could be established.</exception>
        /// <exception cref="NATSConnectionException"><para>A timeout occurred connecting to a NATS Server.</para>
        /// <para>-or-</para>
        /// <para>An exception was encountered while connecting to a NATS Server. See <see cref="System.Exception.InnerException"/> for more
        /// details.</para></exception>
        public async Task<IConnection> CreateConnectionAsync(CancellationToken token)
        {
            if (this._defaultOptions == null)
            {
                throw new Exception();  //TODO refine expection
            }
            return await this.CreateConnectionAsync(this._defaultOptions, token);
        }


        /// <summary>
        /// Create a connection to a NATS Server defined by the given options.
        /// </summary>
        /// <remarks>
        /// This method returns immediately, before the actual connection setup 
        /// and the successful completion of the handshake
        /// </remarks>
        /// <param name="options">A <see cref="ClientOptions"/> instance with the proper parameters.</param>
        /// <returns>An <see cref="IConnection"/> object connected to the NATS server.</returns>
        /// <exception cref="NATSNoServersException">No connection to a NATS Server could be established.</exception>
        /// <exception cref="NATSConnectionException"><para>A timeout occurred connecting to a NATS Server.</para>
        /// <para>-or-</para>
        /// <para>An exception was encountered while connecting to a NATS Server. See <see cref="System.Exception.InnerException"/> for more
        /// details.</para></exception>
        public IConnection CreateConnection(ClientOptions options)
        {
            var nc = new Connection(options);
            nc.Start();
            return nc;
        }


        /// <summary>
        /// Create a connection to a NATS Server defined by the given options.
        /// </summary>
        /// <remarks>
        /// This method returns only after the actual connection setup 
        /// and the successful completion of the handshake.
        /// </remarks>
        /// <param name="options">A <see cref="ClientOptions"/> instance with the proper parameters.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IConnection"/> object connected to the NATS server.</returns>
        /// <exception cref="NATSNoServersException">No connection to a NATS Server could be established.</exception>
        /// <exception cref="NATSConnectionException"><para>A timeout occurred connecting to a NATS Server.</para>
        /// <para>-or-</para>
        /// <para>An exception was encountered while connecting to a NATS Server. See <see cref="System.Exception.InnerException"/> for more
        /// details.</para></exception>
        public async Task<IConnection> CreateConnectionAsync(ClientOptions options, CancellationToken token)
        {
            var nc = new Connection(options);
            await nc.StartAsync(token);
            return nc;
        }

    }
}
