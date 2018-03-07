
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
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Convenience class representing the TCP connection to prevent 
    /// managing two variables throughout the NATs client code.
    /// </summary>
    internal sealed class TcpConnection
        : IDisposable
    {
        /// A note on the use of streams.  .NET provides a BufferedStream
        /// that can sit on top of an IO stream, in this case the network
        /// stream. It increases performance by providing an additional
        /// buffer.
        /// 
        /// So, here's what we have for writing:
        ///     Client code
        ///          ->BufferedStream (bw)
        ///              ->NetworkStream/SslStream (srvStream)
        ///                  ->TCPClient (srvClient);
        ///                  
        ///  For reading:
        ///     Client code
        ///          ->NetworkStream/SslStream (srvStream)
        ///              ->TCPClient (srvClient);
        /// 
        private const int InitialSocketSendTimeout = 2; //ms
        private const int InitialSocketReceiveTimeout = 0; //ms


        private TcpConnection() { }


        /// <summary>
        /// Creates an instance of <see cref="TcpConnection"/> using the
        /// specified <see cref="ServerConnectionStatus"/> as parameters.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="s"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal static TcpConnection Create(
            ClientOptions options,
            ServerConnectionStatus s,
            CancellationToken token
            )
        {
            var instance = new TcpConnection();
            instance._options = options;

            var client = new TcpClient();
            try
            {
                //it seems there's no way to better make a connection attempt
                //within a certain timeout.
                //the solution is connecting in the async-fashion, then
                //controlling the task timeout and cancellation in the sync-fashion
                bool success = client
                    .ConnectAsync(s.EndPoint.Address, s.EndPoint.Port)
                    .Wait((int)options.Timeout.TotalMilliseconds, token);

                if (success)
                {
                    //set-up the socket
                    client.NoDelay = false;
                    client.ReceiveBufferSize = Defaults.defaultBufSize * 2;
                    client.SendBufferSize = Defaults.defaultBufSize;
                    client.SendTimeout = InitialSocketSendTimeout;
                    client.ReceiveTimeout = InitialSocketReceiveTimeout;
                    instance._client = client;
                    instance._netStream = client.GetStream();

                    // save off the hostname
                    instance._hostName = s.EndPoint.Address.ToString();

                    instance.CreateReaderWriter();
                }
                else
                {
                    throw new NATSConnectionException("timeout");
                }
            }
            catch (Exception ex)
            {
                client.Dispose();
                throw ex;
            }
            //Console.WriteLine("socket connected");
            return instance;
        }


        private ClientOptions _options;
        private TcpClient _client = null;

        private NetworkStream _netStream = null;
        private SslStream _sslStream = null;
        private Stream _actualReadBufferedStream;
        private Stream _actualWriteBufferedStream;

        private readonly byte[] _buffer = new byte[Defaults.defaultReadLength];

        private string _hostName = null;


        /// <summary>
        /// Indicates if the socket is connected or not
        /// </summary>
        internal bool Connected => this._client?.Connected ?? false;


        /// <summary>
        /// Gets or sets the send-timeout of the socket
        /// </summary>
        internal int SocketSendTimeout
        {
            get => this._client.SendTimeout;
            set => this._client.SendTimeout = value;
        }


        /// <summary>
        /// Gets or sets the receive-timeout of the socket
        /// </summary>
        internal int SocketReceiveTimeout
        {
            get => this._client.ReceiveTimeout;
            set => this._client.ReceiveTimeout = value;
        }


        /// <summary>
        /// Makes the connection TLS.
        /// That is, wraps the main stream with a <see cref="SslStream"/>.
        /// </summary>
        internal void MakeTLS()
        {
            if (this._netStream == null)
            {
                throw new NATSException("Internal error:  Cannot create SslStream from null stream.");
            }

            //requires the host to give a valid certificate
            RemoteCertificateValidationCallback cb = this._options.TLSRemoteCertificationValidationCallback ?? RemoteCertificateValidation;

            this._sslStream = new SslStream(
                this._netStream,
                false,
                cb,
                null,
                EncryptionPolicy.RequireEncryption
                );

            try
            {
                this.CreateReaderWriter();

                //authentication
                SslProtocols protocol = (SslProtocols)Enum.Parse(typeof(SslProtocols), "Tls12");
                this._sslStream.AuthenticateAsClientAsync(
                    this._hostName,
                    this._options.CertificateCollection,
                    protocol,
                    true
                    ).Wait();
            }
            catch (Exception ex)
            {

                this.Dispose();
                throw new NATSConnectionException("TLS Authentication error", ex);
            }
        }


        /// <summary>
        /// Reads a byte-array from the incoming stream.
        /// </summary>
        /// <param name="timeout">The amount of millisecs to wait before exiting.
        /// A value of zero returns immediately, and a negative value waits endlessly.</param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal ArraySegment<byte> ReadSegment(
            int timeout,
            CancellationToken token
            )
        {
            int len = 0;
            if (timeout < 0)
            {
                //waits for incoming data, but with no timeout or cancellation
                len = this._actualReadBufferedStream.Read(this._buffer, 0, Defaults.defaultReadLength);
            }
            else if (timeout == 0)
            {
                //tests for any cached data, then returns immediately
                if (this._netStream.DataAvailable)
                {
                    len = this._actualReadBufferedStream.Read(this._buffer, 0, Defaults.defaultReadLength);
                }
            }
            else
            {
                //waits for incoming data, with timeout and cancellation
                Task<int> t = this._actualReadBufferedStream.ReadAsync(this._buffer, 0, Defaults.defaultReadLength);
                bool success = t.Wait(timeout, token);
                len = success ? t.Result : 0;
            }
            return new ArraySegment<byte>(this._buffer, 0, len);
        }


        /// <summary>
        /// Creates both the incoming data (read) and the outgoing data (write) streams.
        /// </summary>
        private void CreateReaderWriter()
        {
            this._actualReadBufferedStream = (Stream)this._sslStream ?? this._netStream;
            this._actualWriteBufferedStream = new BufferedStream(this._actualReadBufferedStream, Defaults.defaultBufSize);
        }


        /// <summary>
        /// Sends a string over the socket, serialized as UTF-8
        /// </summary>
        /// <param name="value"></param>
        internal void WriteString(string value)
        {
            this.WriteByteArray(
                Encoding.UTF8.GetBytes(value ?? string.Empty)
                );
        }


        /// <summary>
        /// Sends a simple byte-array over the socket
        /// </summary>
        /// <param name="buffer"></param>
        internal void WriteByteArray(byte[] buffer)
        {
            this._actualWriteBufferedStream.Write(buffer, 0, buffer.Length);
        }


        /// <summary>
        /// Sends a segment of byte-array over the socket
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        internal void WriteByteArray(
            byte[] buffer,
            int offset,
            int count
            )
        {
            this._actualWriteBufferedStream.Write(buffer, offset, count);
        }


        /// <summary>
        /// Sends a segment of byte-array over the socket
        /// </summary>
        /// <param name="segment"></param>
        internal void WriteByteArray(ArraySegment<byte> segment)
        {
            this._actualWriteBufferedStream.Write(segment.Array, segment.Offset, segment.Count);
        }


        /// <summary>
        /// Forces any pending cached data to be physically sent over the socket.
        /// </summary>
        internal void WriteFlush()
        {
            this._actualWriteBufferedStream.Flush();
        }


        /// <summary>
        /// Validates the certificate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private static bool RemoteCertificateValidation(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors
            )
        {
            return sslPolicyErrors == SslPolicyErrors.None;
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                this._sslStream?.Dispose();
                this._netStream?.Dispose();
                this._client?.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
