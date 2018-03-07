
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Main connection manager, and raw-data dispatcher
    /// </summary>
    /// <remarks>
    /// Each <see cref="Connection"/> instance hosts a single <see cref="ConnectionManager"/> instance.
    /// This instance lives and die together with the <see cref="Connection"/> one.
    /// </remarks>
    internal sealed class ConnectionManager
        : IDisposable
    {

        private const int StageServerInitialConnection = 0;
        private const int StageWaitInfo = 1;
        private const int StageSendConnectVerbose = 2;
        private const int StageSendConnectNormal = 3;
        private const int StageLogicallyConnected = 4;
        private const int StageServerReconnection = 5;


        private const int ReadTimeout = 5000;   //ms


        /// <summary>
        /// Creates an instance of <see cref="ConnectionManager"/>
        /// </summary>
        /// <param name="owner"></param>
        internal ConnectionManager(Connection owner)
        {
            this._owner = owner;
            this._options = owner.OptionsInternal;

            //create the server pool manager
            this._srvPool = ServerPool.Create(this._options);
            this._srvPool.IncrementStatsCounter = () =>
            {
                Interlocked.Increment(ref this._statsReconnections);
            };

            //create the incoming data parser
            this._parser = new Parser()
            {
                NotifyOperation = this.NotifyOperationHandler,
                NotifyMessage = this.NotifyMessageHandler,
                NotifyError = this.NotifyErrorHandler
            };

            //prepare a single instance of args to pass when the StateChanged event is raised:
            //that's for performance reasons
            this._stateChangedEventArgs = new StateChangedEventArgs(this._owner);
        }


        private Connection _owner;
        private ClientOptions _options;
        private Thread _worker;

        private Parser _parser;

        private int _workerStage;
        private CancellationTokenSource _outerTokenSource;
        private CancellationTokenSource _compositeTokenSource;
        private readonly StateChangedEventArgs _stateChangedEventArgs;

        private readonly object _writeLocker = new object();
        private volatile bool _writePending;

        private readonly List<string> _subQueue = new List<string>();
        private volatile bool _subPending;


        internal TcpConnection Tcp => this._tcp;
        private TcpConnection _tcp;


        internal ServerPool SrvPool => this._srvPool;
        private ServerPool _srvPool;


        internal ServerInfo SvrInfo => this._info;
        private ServerInfo _info;


        internal long MaxPayload => this._info?.MaxPayload ?? 0;


        internal string ConnectedId => this._info?.ServerId ?? InternalConstants._EMPTY_;


        internal bool IsLogicalStarted => this._isLogicalStarted;
        private bool _isLogicalStarted;


        private Exception _beforeLogicalStartError;


        #region Life-cycle state machine

        private const int LifeCycleStateIdle = 0;
        private const int LifeCycleStateStarting = 1;
        private const int LifeCycleStateRunning = 2;
        private const int LifeCycleStateStopping = 3;

        private int _lifeCycleState = LifeCycleStateIdle;


        /// <summary>
        /// Triggers a start to the connection state-machine.
        /// </summary>
        /// <remarks>
        /// This method will return immediately.
        /// </remarks>
        internal void Start()
        {
            //if IDLE then STARTING else exit
            if (Interlocked.CompareExchange(ref this._lifeCycleState, LifeCycleStateStarting, LifeCycleStateIdle) == LifeCycleStateIdle)
            {
                this.StartCore(null, CancellationToken.None);
            }
        }


        /// <summary>
        /// Starts the connection state-machine waiting for the complete handshake
        /// </summary>
        /// <remarks>
        /// The task returned by this method will be released upon the completion of the handshake,
        /// or any exception
        /// </remarks>
        /// <param name="token"></param>
        /// <returns></returns>
        internal Task StartAsync(CancellationToken token)
        {
            //if IDLE then STARTING else exit
            if (Interlocked.CompareExchange(ref this._lifeCycleState, LifeCycleStateStarting, LifeCycleStateIdle) == LifeCycleStateIdle)
            {
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                this.StartCore(tcs, token);
                return tcs.Task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }


        /// <summary>
        /// Common entry-point to start the connection state-machine
        /// </summary>
        /// <param name="tcs"></param>
        /// <param name="token"></param>
        private void StartCore(TaskCompletionSource<object> tcs, CancellationToken token)
        {
            System.Diagnostics.Debug.Assert(this._lifeCycleState == LifeCycleStateStarting);

            this._outerTokenSource = new CancellationTokenSource();
            this._compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this._outerTokenSource.Token, token);
            var wkctx = new WorkerContext()
            {
                TaskCompletion = tcs,
                CancToken = this._compositeTokenSource.Token,
            };

            this._isLogicalStarted = false;
            this._beforeLogicalStartError = null;

            this._worker = new Thread(this.OuterWorker);
            this._worker.Start(wkctx);
            this._lifeCycleState = LifeCycleStateRunning;
        }


        /// <summary>
        /// Stops the connection state-machine synchronously
        /// </summary>
        /// <remarks>
        /// This method blocks until the stop process is complete.
        /// </remarks>
        /// <param name="disposing"></param>
        internal void Stop(bool disposing)
        {
            if (disposing)
            {
                this._lifeCycleState = LifeCycleStateStopping;
                this.StopCore(null);
            }
            else
            {
                //if RUNNING then STOPPING else exit
                if (Interlocked.CompareExchange(ref this._lifeCycleState, LifeCycleStateStopping, LifeCycleStateRunning) == LifeCycleStateRunning)
                {
                    this.StopCore(null);
                }
            }
        }


        /// <summary>
        /// Stops the connection state-machine asynchronously
        /// </summary>
        /// <remarks>
        /// This method blocks (async-await) until the stop process is complete.
        /// </remarks>
        /// <returns></returns>
        internal Task StopAsync()
        {
            //if RUNNING then STOPPING else exit
            if (Interlocked.CompareExchange(ref this._lifeCycleState, LifeCycleStateStopping, LifeCycleStateRunning) == LifeCycleStateRunning)
            {
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                this.StopCore(tcs);
                return tcs.Task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }


        /// <summary>
        /// Common entry-point to stop the connection state-machine
        /// </summary>
        /// <param name="tcs"></param>
        private void StopCore(TaskCompletionSource<object> tcs)
        {
            System.Diagnostics.Debug.Assert(this._lifeCycleState == LifeCycleStateStopping);
            try
            {
                this._tcp?.WriteFlush();
            }
            catch { }
            this._outerTokenSource?.Cancel();
            this._worker?.Join();
            tcs?.SetResult(null);
            this._compositeTokenSource?.Dispose();
            this._compositeTokenSource = null;
            this._outerTokenSource?.Dispose();
            this._outerTokenSource = null;
            this._worker = null;
            this._lifeCycleState = LifeCycleStateIdle;
        }


        internal ConnState State => this._status;
        private ConnState _status = ConnState.CLOSED;


        /// <summary>
        /// Updates the official connection status
        /// </summary>
        /// <param name="status"></param>
        private void SetStatus(ConnState status)
        {
            if (this._status != status)
            {
                this._status = status;

                //notify the event's subscribers
                var handler = this._options.StateChangedEventHandler;
                if (handler != null)
                {
                    this._stateChangedEventArgs.State = this._status;
                    handler(this._stateChangedEventArgs);
                }
            }
        }

        #endregion


        /// <summary>
        /// Halts the caller until the NATS handshake is completed successfully.
        /// </summary>
        /// <param name="timeout">The amount of time to wait before breaking the stop.
        /// A negative value will wait endlessly.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        internal void WaitLogicalStart(
            TimeSpan timeout,
            CancellationToken token
            )
        {
            var delay = TimeSpan.FromMilliseconds(100);
            bool waitForever = timeout < TimeSpan.Zero;
            while (this._isLogicalStarted == false)
            {
                //the timed-out semaphore is much better and responsive than any other classic timer!
                using (var sem = new SemaphoreSlim(0))
                {
                    sem.Wait(delay, token);
                }

                if (this._beforeLogicalStartError != null)
                {
                    throw this._beforeLogicalStartError;
                }
                else if (waitForever == false)
                {
                    timeout -= delay;
                    if (timeout <= TimeSpan.Zero)
                    {
                        throw new NATSTimeoutException("Timeout occurred while waiting for the logical connection.");
                    }
                }
            }
        }


        private class WorkerContext
        {
            public TaskCompletionSource<object> TaskCompletion;
            public CancellationToken CancToken;
        }


        /// <summary>
        /// The "outer" worker, as direct thread handler
        /// </summary>
        /// <param name="state"></param>
        private void OuterWorker(object state)
        {
            var ctx = (WorkerContext)state;
            this._workerStage = StageServerInitialConnection;

            int attempt = 1;
            bool completed;
            do
            {
                if (ctx.CancToken.IsCancellationRequested) break;

                //sets the NATS connection status upon an initial connection,
                //or an attempt of a reconnection after a drop
                if (this._workerStage == StageServerInitialConnection)
                {
                    this.SetStatus(ConnState.CONNECTING);
                }
                else
                {
                    if (Helpers.CancelableDelay(this._options.ReconnectWait, ctx.CancToken)) break;
                    this.SetStatus(ConnState.RECONNECTING);
                }

                try
                {
                    //call the inner worker
                    this.InnerWorker(ctx);
                }
                catch (Exception ex)
                {
                    if (this._isLogicalStarted == false)
                    {
                        this._beforeLogicalStartError = ex;
                    }

                    //releases the async start task, if was
                    ctx.TaskCompletion?.SetException(ex);

                    //raise the exception to the event's subscribers
                    var handler = this._options.AsyncErrorEventHandler;
                    if (handler != null)
                    {
                        var args = new AsyncErrorEventArgs()
                        {
                            Connection = this._owner,
                            State = this._status,
                            Exc = ex
                        };
                        handler(args);
                    }
                }
                finally
                {
                    this._isLogicalStarted = false;
                    this._owner.RequestReplyManager.Stop(disposing: false);
                    this.PingStop(disposing: false);
                    this._tcp?.Dispose();
                    this.SetStatus(ConnState.DISCONNECTED);
                }

                //manages a certain number of attempts, then considers impossible to connect
                completed = ++attempt >= this._options.MaxReconnect;
                this._workerStage = StageServerReconnection;
            }
            while (completed == false);

            this.SetStatus(ConnState.CLOSED);
        }


        /// <summary>
        /// The "inner" worker
        /// </summary>
        /// <remarks>
        /// This method can raise any kind of exception, which will be caught by the <see cref="OuterWorker(object)"/>.
        /// </remarks>
        /// <param name="ctx"></param>
        private void InnerWorker(
            WorkerContext ctx
            )
        {
            this._info = null;

            /**
              * Initial connection to a server
              **/
            try
            {
                this._tcp = this._srvPool.ConnectToAServer(
                    this._workerStage == StageServerInitialConnection,
                    ctx.CancToken
                    );

                if (this._tcp == null) throw new Exception();
            }
            catch (OperationCanceledException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new NATSNoServersException("Unable to connect to a server.");
            }


            /**
             * Waiting for the "INFO" data issued by the server
             **/
            this._workerStage = StageWaitInfo;
            this.ReadThenParseSegment(null, null, ReadTimeout, ctx.CancToken);

            if (this._info == null)
            {
                throw new NATSConnectionException("Protocol exception, INFO not received");
            }


            /**
             * Send "CONNECT" to the server
             **/
            this._workerStage = this._options.Verbose
                ? StageSendConnectVerbose
                : StageSendConnectNormal;

            try
            {
                this._tcp.SocketSendTimeout = 0;
                this._tcp.WriteString(this.ConnectProto());
                this.SendPing();
            }
            catch (Exception ex)
            {
                throw new NATSException("Error sending connect protocol message", ex);
            }

            this.ReadThenParseSegment(
                ex => throw new NATSConnectionException("Connect read error", ex),
                null,
                ReadTimeout,
                ctx.CancToken
                );

            if (this._workerStage != StageLogicallyConnected)
            {
                throw new NATSConnectionException("Connect read protocol error");
            }


            /**
             * Main processing loop
             **/
            this._tcp.SocketReceiveTimeout = ReadTimeout;   //for safety
            this.SetStatus(ConnState.CONNECTED);
            this.PingStart();
            this._owner.RequestReplyManager.Start();

            int readCount = 0;
            while (ctx.CancToken.IsCancellationRequested == false)
            {
                if (this._writePending)
                {
                    //something to send is pending
                    lock (this._writeLocker)
                    {
                        //is PING, maybe?
                        if (this._pingSendPending)
                        {
                            this.SendPing();
                        }

                        //are one or more SUB messages, maybe?
                        if (this._subPending)
                        {
                            for (int i = 0, sublen = this._subQueue.Count; i < sublen; i++)
                            {
                                this._tcp.WriteString(this._subQueue[i]);
                            }
                            this._subQueue.Clear();
                            this._tcp.WriteFlush();
                            this._subPending = false;
                        }

                        this._writePending = false;
                    }
                }
                else if (this._isLogicalStarted)
                {
                    if (readCount == 0)
                    {
                        //no operation was perfomed: no data in, no data out.
                        //the cycle would be too tight, then.
                        //give the OS a breathe of fresh air...
                        Thread.Sleep(1);
                    }
                }
                else
                {
                    //the initial NATS handshake with the server is complete:
                    //if the start was async, then consider its task as terminated
                    this._isLogicalStarted = true;
                    ctx.TaskCompletion?.SetResult(null);
                }

                //listen to incoming data
                readCount = this.ReadThenParseSegment(
                    this.ProcessOpError,
                    this.ProcessOpError,
                    0,
                    ctx.CancToken
                    );
            }
        }


        /// <summary>
        /// Read-and-parse handler for the incoming raw-data from the TCP connection
        /// </summary>
        /// <param name="readex">Handler for an exception during the reading pass</param>
        /// <param name="parseex">Handler for an exception during the parsing pass</param>
        /// <param name="timeout">Amount of millisecs to wait some incoming data.
        /// A value of zero will instruct the reader to return immediately</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns></returns>
        private int ReadThenParseSegment(
            Action<Exception> readex,
            Action<Exception> parseex,
            int timeout,
            CancellationToken token
            )
        {
            int count;

            /**
             * Listen to incoming bytes
             **/
            ArraySegment<byte> segment;
            try
            {
                segment = this._tcp.ReadSegment(timeout, token);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("read: " + ex.Message);
                if (readex != null)
                {
                    readex(ex);
                }
                else
                {
                    throw ex;
                }
            }

            count = segment.Count;
            if (count != 0)
            {
                try
                {
                    this._parser.Parse(segment);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("parse: " + ex.Message);
                    if (parseex != null)
                    {
                        parseex(ex);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            return count;
        }


        /// <summary>
        /// Simple handler for the NATS protocol state-machine
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="op"></param>
        private void NotifyOperationHandler(
            ArraySegment<byte> segment,
            int op
            )
        {
            switch (this._workerStage)
            {
                case StageWaitInfo:
                    if (op == Parser.INFO_ARG)
                    {
                        this._info = this.ProcessInfo(segment, false);

                        // This will check to see if the connection should be
                        // secure. This can be dictated from either end and should
                        // only be called after the INIT protocol has been received.

                        // Check to see if we need to engage TLS
                        // Check for mismatch in setups
                        bool currentIsSecured = this._srvPool.CurrentServer.EndPoint.Secured;
                        if (currentIsSecured && this._info.TlsRequired == false)
                        {
                            throw new NATSSecureConnWantedException();
                        }
                        else if (this._info.TlsRequired && currentIsSecured == false)
                        {
                            throw new NATSSecureConnRequiredException();
                        }

                        // Need to rewrap with bufio
                        if (currentIsSecured)
                        {
                            // makeSecureConn will wrap an existing Conn using TLS
                            this._tcp.MakeTLS();
                        }
                    }
                    break;

                case StageSendConnectVerbose:
                    if (op == Parser.OP_PLUS_OK)
                    {
                        this._workerStage = StageSendConnectNormal;
                    }
                    break;

                case StageSendConnectNormal:
                    if (op == Parser.OP_PONG)
                    {
                        this._workerStage = StageLogicallyConnected;
                    }
                    break;

                case StageLogicallyConnected:
                    if (op == Parser.OP_PING)
                    {
                        this.ProcessPing();
                    }
                    else if (op == Parser.OP_PONG)
                    {
                        this.ProcessPong();
                    }
                    break;

                default:
                    throw new NotSupportedException("worker-stage not supported: " + this._workerStage);
            }
        }


        /// <summary>
        /// Processes the very first INFO message issued by the server
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="notifyOnServerAddition"></param>
        /// <returns></returns>
        private ServerInfo ProcessInfo(
            ArraySegment<byte> segment,
            bool notifyOnServerAddition
            )
        {
            ServerInfo info = null;
            if (segment.Count > 0)
            {
                info = ServerInfo.Create(segment);

                //converts the URLs array received from the server to a new endpoint
                bool currentIsSecured = this._srvPool.CurrentServer.EndPoint.Secured;
                List<ServerEndPoint> newEndPoints = (info.ConnectUrls ?? new string[0])
                    .Select(_ => ServerEndPoint.ParseAnonymous(_, currentIsSecured))
                    .Where(_ => _ != null)
                    .ToList();

                if (newEndPoints.Count != 0)
                {
                    if (this._options.NoRandomize == false)
                    {
                        // If randomization is allowed, shuffle the received array, 
                        // not the entire pool. We want to preserve the pool's
                        // order up to this point (this would otherwise be 
                        // problematic for the (re)connect loop).
                        ServerPool.Shuffle(newEndPoints);
                    }

                    bool serverAdded = this._srvPool.AddAsImplicit(newEndPoints);
                    if (notifyOnServerAddition &&
                        serverAdded
                        )
                    {
                        //notify host
                        this._options.ServerDiscoveredEventHandler?.Invoke(new ServerDiscoveredEventArgs()
                        {
                            Connection = this._owner,
                            State = this._status
                        });
                    }
                }
            }
            return info;
        }


        /// <summary>
        /// Processes a valid message just parsed
        /// </summary>
        /// <param name="message"></param>
        private void NotifyMessageHandler(
            MsgOut message
            )
        {
            //update stats
            this.AddIncomingStats(1, message.PayloadLength);

            //yield the subscription pool to go up
            this._owner.SubPool.ProcessMessage(message);
        }


        /// <summary>
        /// Processes any error messages from the server
        /// </summary>
        /// <param name="segment"></param>
        private void NotifyErrorHandler(
            ArraySegment<byte> segment
            )
        {
            string s = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count)
                .Trim('\'', '"', ' ', '\t', '\r', '\n')
                .ToLower();

            if (s == InternalConstants.STALE_CONNECTION)
            {
                this.ProcessOpError(
                    new NATSStaleConnectionException()
                    );
            }
            else if (s == InternalConstants.AUTH_TIMEOUT)
            {
                // Protect against a timing issue where an authoriztion error
                // is handled before the connection close from the server.
                // This can happen in reconnect scenarios.
                this.ProcessOpError(
                    new NATSConnectionException(InternalConstants.AUTH_TIMEOUT)
                    );
            }
            else
            {
                throw new NATSException($"Error from {nameof(NotifyErrorHandler)}: {s}");
            }
        }


        /// <summary>
        /// Processes an error detected during the parsing process
        /// </summary>
        /// <param name="ex"></param>
        private void ProcessOpError(Exception ex)
        {
            switch (this._status)
            {
                case ConnState.CLOSED:
                case ConnState.CONNECTING:
                case ConnState.RECONNECTING:
                    return;
            }

            throw ex;
        }


        /// <summary>
        /// Sends a publish message (header and payload) straight to the TCP channel
        /// </summary>
        /// <param name="header"></param>
        /// <param name="payload"></param>
        internal void WritePubData(
            ArraySegment<byte> header,
            byte[] payload
            )
        {
            lock (this._writeLocker)
            {
                this._tcp.WriteByteArray(header);
                this._tcp.WriteByteArray(payload);
                this._tcp.WriteByteArray(Connection.CRLF_BYTES);
                this._tcp.WriteFlush();
            }

            //update stats
            this.AddOutgoingStats(1, payload.Length);
        }


        /// <summary>
        /// Enqueues a request of subscription message, which will be delivered to the server
        /// </summary>
        /// <param name="protoMessage"></param>
        internal void EnqueueSubscriptionOperation(
            string protoMessage
            )
        {
            lock (this._writeLocker)
            {
                this._subQueue.Add(protoMessage);
                this._subPending = this._writePending = true;
            }
        }


        /// <summary>
        /// Generate a connect protocol message, issuing user/password if applicable.
        /// </summary>
        /// <returns></returns>
        private string ConnectProto()
        {
            ServerEndPoint endPoint = this._srvPool.CurrentServer.EndPoint;

            //string u = this._srvPool.CurrentServer.Uri.UserInfo;
            //string user = null;
            //string pass = null;
            //string token = null;

            //if (string.IsNullOrEmpty(u))
            //{
            //    user = this._options.User;
            //    pass = this._options.Password;
            //    token = this._options.Token;
            //}
            //else
            //{
            //    int pos = u.IndexOf(':');
            //    if (pos >= 0)
            //    {
            //        if (pos > 0)
            //        {
            //            user = u.Substring(0, pos);
            //        }
            //        if (++pos < u.Length)
            //        {
            //            pass = u.Substring(pos);
            //        }
            //    }
            //    else
            //    {
            //        token = u;
            //    }
            //}

            var info = new ConnectInfo
            {
                Verbose = this._options.Verbose,
                Pedantic = this._options.Pedantic,
                User = endPoint.UserName,
                AuthToken = endPoint.AuthToken,
                SslRequired = endPoint.Secured,
                Name = this._options.Name,
                Lang = Defaults.LangString,
                Version = Defaults.Version,
                Protocol = (int)ClientProtcolVersion.ClientProtoInfo
            };

            if (endPoint.Password != null)
            {
                /**
                 * This section is a "never do it" practice, but seems that there's no way to avoid it
                 **/

                //retrieve the original password back
                int length = endPoint.Password.Length;
                IntPtr pointer = IntPtr.Zero;
                char[] chars = new char[length];

                try
                {
                    pointer = Marshal.SecureStringToBSTR(endPoint.Password);
                    Marshal.Copy(pointer, chars, 0, length);
                    info.Pass = new string(chars);
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                    {
                        Marshal.ZeroFreeBSTR(pointer);
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendFormat(InternalConstants.conProto, info.ToJson());
            return sb.ToString();
        }


        #region Ping-pong

        private static readonly byte[] PING_P_BYTES = Encoding.UTF8.GetBytes(InternalConstants.pingProto);
        private static readonly byte[] PONG_P_BYTES = Encoding.UTF8.GetBytes(InternalConstants.pongProto);

        private static readonly TimeSpan MinPingInterval = TimeSpan.FromSeconds(1);

        private TimeSpan _actualPingInterval;
        private Timer _pingTimer;
        private int _pingCount;
        private volatile bool _pingSendPending;


        /// <summary>
        /// Starts the ping-pong state-machine
        /// </summary>
        private void PingStart()
        {
            this._pingSendPending = false;
            if (this._options.PingInterval > TimeSpan.Zero)
            {
                this._actualPingInterval = Helpers.Max(this._options.PingInterval, MinPingInterval);

                this._pingTimer = new Timer(
                    this.PingTimerCallback,
                    null,
                    this._actualPingInterval,
                    Timeout.InfiniteTimeSpan
                    );
            }
            else
            {
                this._actualPingInterval = TimeSpan.Zero;
            }
        }


        /// <summary>
        /// Stops the ping-pong state-machine
        /// </summary>
        /// <param name="disposing"></param>
        private void PingStop(bool disposing)
        {
            this._pingSendPending = false;
            this._pingTimer?.Dispose();
            this._pingTimer = null;
        }


        /// <summary>
        /// Sends as "PING" message to the server
        /// </summary>
        private void SendPing()
        {
            this._tcp.WriteByteArray(PING_P_BYTES);
            this._tcp.WriteFlush();
            this._pingSendPending = false;
        }


        /// <summary>
        /// ProcessPing will send an immediate pong protocol response to the
        /// server. The server uses this mechanism to detect dead clients.
        /// </summary>
        private void ProcessPing()
        {
            this._tcp.WriteByteArray(PONG_P_BYTES);
        }


        /// <summary>
        /// A "PONG" message has been received from the server
        /// </summary>
        private void ProcessPong()
        {
            this._pingCount = 0;
        }


        /// <summary>
        /// The clock handler as heartbeat of the ping-pong state-machine
        /// </summary>
        /// <param name="state"></param>
        private void PingTimerCallback(object state)
        {
            if (this._pingTimer == null)
            {
                return;
            }

            if (++this._pingCount > this._options.MaxPingsOut)
            {
                this.ProcessOpError(new NATSStaleConnectionException());
            }
            else
            {
                lock (this._writeLocker)
                {
                    this._pingSendPending = this._writePending = true;
                }

                //trigger timer again
                this._pingTimer?.Change(this._actualPingInterval, Timeout.InfiniteTimeSpan);
            }
        }

        #endregion


        #region Statistics

        private long _statsIncomingMessages;
        private long _statsIncomingBytes;
        private long _statsOutgoingMessages;
        private long _statsOutgoingBytes;
        private int _statsReconnections;

        private readonly object _statsLocker = new object();


        /// <summary>
        /// Feeds some incoming info to the stats
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="bytes"></param>
        internal void AddIncomingStats(
            int messages,
            int bytes
            )
        {
            lock (this._statsLocker)
            {
                this._statsIncomingMessages += messages;
                this._statsIncomingBytes += bytes;
            }
        }


        /// <summary>
        /// Feeds some outgoing info to the stats
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="bytes"></param>
        internal void AddOutgoingStats(
            int messages,
            int bytes
            )
        {
            lock (this._statsLocker)
            {
                this._statsOutgoingMessages += messages;
                this._statsOutgoingBytes += bytes;
            }
        }


        /// <summary>
        /// Gets the current stats info
        /// </summary>
        /// <returns></returns>
        internal ConnectionStatsInfo GetStats()
        {
            lock (this._statsLocker)
            {
                return new ConnectionStatsInfo
                {
                    IncomingMessages = this._statsIncomingMessages,
                    IncomingBytes = this._statsIncomingBytes,
                    OutgoingMessages = this._statsOutgoingMessages,
                    OutgoingBytes = this._statsOutgoingBytes,
                    Reconnections = this._statsReconnections
                };
            }
        }


        /// <summary>
        /// Clears the local stats
        /// </summary>
        internal void ResetStats()
        {
            lock (this._statsLocker)
            {
                this._statsIncomingMessages = 0;
                this._statsIncomingBytes = 0;
                this._statsOutgoingMessages = 0;
                this._statsOutgoingBytes = 0;
                this._statsReconnections = 0;
            }
        }

        #endregion


        public void Dispose()
        {
            this.PingStop(disposing: true);
            this.Stop(disposing: true);
            this._tcp?.Dispose();
        }

    }
}
