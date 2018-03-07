
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
using System.Security.Cryptography.X509Certificates;

namespace Cet.NATS.Client
{
    /// <summary>
    /// This class is used to setup all NATs client options.
    /// </summary>
    public sealed class ClientOptions
        : IClientOptions
    {

        /// <summary>
        /// Options can only be publicly created through <see cref="ConnectionFactory.GetDefaultOptions"/>
        /// </summary>
        public ClientOptions() { }


        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="o"></param>
        internal ClientOptions(ClientOptions o)
        {
            this.AsyncErrorEventHandler = o.AsyncErrorEventHandler;
            this.StateChangedEventHandler = o.StateChangedEventHandler;
            this.ServerDiscoveredEventHandler = o.ServerDiscoveredEventHandler;
            this.MaxPingsOut = o.MaxPingsOut;
            this.MaxReconnect = o.MaxReconnect;
            this.Name = o.Name;
            this.NoRandomize = o.NoRandomize;
            this.Pedantic = o.Pedantic;
            this.UseOldRequestStyle = o.UseOldRequestStyle;
            this.PingInterval = o.PingInterval;
            this.ReconnectWait = o.ReconnectWait;
            this.Verbose = o.Verbose;
            this._servers.AddRange(o.Servers);
            this.PendingMessagesLimit = o.PendingMessagesLimit;
            this.PendingBytesLimit = o.PendingBytesLimit;
            this.Timeout = o.Timeout;
            this.TLSRemoteCertificationValidationCallback = o.TLSRemoteCertificationValidationCallback;

            if (o.CertificateCollection != null)
            {
                this._certificateCollection = new X509Certificate2Collection(o.CertificateCollection);
            }
        }


        private X509Certificate2Collection _certificateCollection;
        internal X509Certificate2Collection CertificateCollection
        {
            get
            {
                if (this._certificateCollection == null)
                {
                    this._certificateCollection = new X509Certificate2Collection();
                }
                return this._certificateCollection;
            }
        }


        /// <summary>
        /// Represents the method that will handle an event raised
        /// whenever the client connection state changes.
        /// </summary>
        public Action<StateChangedEventArgs> StateChangedEventHandler { get; set; }


        /// <summary>
        /// Represents the method that will handle an event raised
        /// whenever a new server has joined the cluster.
        /// </summary>
        public Action<ServerDiscoveredEventArgs> ServerDiscoveredEventHandler { get; set; }


        /// <summary>
        /// Represents the method that will handle an event raised 
        /// when an error occurs out of band.
        /// </summary>
        public Action<AsyncErrorEventArgs> AsyncErrorEventHandler { get; set; }


        /// <summary>
        /// Gets the array of servers that the NATs client will connect to.
        /// </summary>
        public IReadOnlyCollection<ServerEndPoint> Servers => this._servers;
        private readonly List<ServerEndPoint> _servers = new List<ServerEndPoint>();


        /// <summary>
        /// Gets or sets a value indicating whether or not the server chosen for connection
        /// should not be selected randomly.
        /// </summary>
        public bool NoRandomize { get; set; }


        /// <summary>
        /// Gets or sets the name of this client.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Gets or sets a value indicating whether or not logging information should be verbose.
        /// </summary>
        public bool Verbose { get; set; }


        /// <summary>
        /// This option is not used by the NATS Client.
        /// </summary>
        public bool Pedantic { get; set; }


        /// <summary>
        /// Gets or sets a value indicating whether or not the old
        /// request pattern should be used.
        /// </summary>
        /// <remarks>
        /// The old request pattern involved a separate subscription
        /// per request inbox. The new style (default) involves creating
        /// a single inbox subscription per connection, upon the first
        /// request, and mapping outbound requests over that one
        /// subscription.
        /// </remarks>
        public bool UseOldRequestStyle { get; set; }


        /// <summary>
        /// Gets or sets the maxmimum number of times a connection will
        /// attempt to reconnect.
        /// </summary>
        public int MaxReconnect { get; set; } = Defaults.MaxReconnect;


        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, the client will 
        /// wait before attempting a reconnection.
        /// </summary>
        public TimeSpan ReconnectWait { get; set; } = Defaults.ReconnectWait;


        /// <summary>
        /// Gets or sets the interval, in milliseconds, pings will be sent to the server.
        /// </summary>
        /// <remarks>
        /// Take care to coordinate this value with the server's interval.
        /// </remarks>
        public TimeSpan PingInterval { get; set; } = Defaults.PingInterval;


        /// <summary>
        /// Gets or sets the timeout, in milliseconds, when connecting to a NATS server.
        /// </summary>
        public TimeSpan Timeout
        {
            get => this._timeout;
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("Timeout must be zero or greater.");
                }

                this._timeout = value;
            }
        }
        private TimeSpan _timeout = Defaults.Timeout;


        /// <summary>
        /// Gets or sets the maximum number of outstanding pings before
        /// terminating a connection.
        /// </summary>
        public int MaxPingsOut { get; set; } = Defaults.MaxPingOut;


        /// <summary>
        /// Gets or sets the maximum number of message pending in the subscribers' delivery queue
        /// </summary>
        public int PendingMessagesLimit { get; set; } = Defaults.SubPendingMsgsLimit;


        /// <summary>
        /// Gets or sets the maximum number of bytes pending in the subscribers' delivery queue
        /// </summary>
        public long PendingBytesLimit { get; set; } = Defaults.SubPendingBytesLimit;


        /// <summary>
        /// Add an explicit server endpoint to the connectable pool
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public ClientOptions AddServer(ServerEndPoint endPoint)
        {
            this._servers.Add(endPoint);
            return this;
        }


        /// <summary>
        /// Adds an X.509 certifcate from a file for use with a secure connection.
        /// </summary>
        /// <param name="fileName">Path to the certificate file to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <c>null</c>.</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">An error with the certificate
        /// ocurred. For example:
        /// <list>
        /// <item>The certificate file does not exist.</item>
        /// <item>The certificate is invalid.</item>
        /// <item>The certificate's password is incorrect.</item></list></exception>
        public ClientOptions AddCertificate(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            X509Certificate2 cert = new X509Certificate2(fileName);
            return this.AddCertificate(cert);
        }


        /// <summary>
        /// Adds an X.509 certifcate for use with a secure connection.
        /// </summary>
        /// <param name="certificate">An X.509 certificate represented as an <see cref="X509Certificate2"/> object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <c>null</c>.</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">An error with the certificate
        /// ocurred. For example:
        /// <list>
        /// <item>The certificate file does not exist.</item>
        /// <item>The certificate is invalid.</item>
        /// <item>The certificate's password is incorrect.</item></list></exception>
        public ClientOptions AddCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }
            this.CertificateCollection.Add(certificate);
            return this;
        }


        /// <summary>
        /// Overrides the default NATS RemoteCertificationValidationCallback.
        /// </summary>
        /// <remarks>
        /// The default callback simply checks if there were any protocol
        /// errors. Overriding this callback is useful during testing, or accepting self
        /// signed certificates.
        /// </remarks>
        public RemoteCertificateValidationCallback TLSRemoteCertificationValidationCallback { get; set; }

    }
}

