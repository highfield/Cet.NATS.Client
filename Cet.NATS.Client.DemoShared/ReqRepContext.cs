using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Cet.NATS.Client.DemoShared
{
    public class ReqRepArgs
    {
        public int SlaveClientCount;
        public int SlaveSubsPerClient;
        public int MasterClientPerSlave;
        public int LoopCount;
        public string BaseSubjectPath;
        public string SampleText;
    }


    public class ReqRepContext
    {
        public ReqRepContext(
            IReqRepImpl rrimpl
            )
        {
            this._rrimpl = rrimpl;
        }


        private readonly IReqRepImpl _rrimpl;
        private readonly List<ReqRepBridge> _bridges = new List<ReqRepBridge>();

        private readonly List<MasterClientContext> _masterClients = new List<MasterClientContext>();
        private readonly List<SlaveClientContext> _slaveClients = new List<SlaveClientContext>();


        public void Run(
            ReqRepArgs args
            )
        {
            /**
             * Basic concepts:
             * - the master plays the role of the requesting subject
             * - the slave plays the role of the replying subject
             * - each "client" refers to an independent connection against the broker
             * 
             * How this demo works:
             * - the user defines M master clients and S slave clients, thus a total of M+S connections
             * - each slave client is logically targeted by one or more masters, thus M should be a multiple of S
             * - each slave targets a different subject meant as a "base path"
             * - the user defines also how many subjects each slave should subscribe (as T)
             * - each subscription being indexed matches a subject path composed as follows:
             *      (base-path).sub(index).>
             * - at the end, there are S*T unique subscriptions reachable through the broker
             * - to match them all, each master client will run S*T/M so-called "endpoints" as threads
             * - each endpoint will match its paired subscription target
             * - each endpoint will run "loopcount" different requests sequentially validating the response
             **/

            for (int ixs = 0; ixs < args.SlaveClientCount; ixs++)
            {
                var sl = new SlaveClientContext(args.BaseSubjectPath + ixs, args.SlaveSubsPerClient);
                this._slaveClients.Add(sl);

                for (int ixm = 0; ixm < args.MasterClientPerSlave; ixm++)
                {
                    var ms = new MasterClientContext();
                    this._masterClients.Add(ms);

                    for (int ixt = 0; ixt < args.SlaveSubsPerClient; ixt++)
                    {
                        string targetPath = sl.GetSubjectPath(ixt, ixm);
                        var bridge = new ReqRepBridge(
                            ms, 
                            sl,
                            targetPath,
                            args.SampleText
                            );

                        this._bridges.Add(bridge);
                        sl.EndPoints.Add(targetPath, bridge);
                        ms.EndPoints.Add(bridge);
                    }

                    ms.LoopCount = args.LoopCount;
                    ms.BlockingEvent = new CountdownEvent(ms.EndPoints.Count);
                }
            }

            var tlist = new List<Thread>();

            //slave clients
            for (int i = 0; i < this._slaveClients.Count; i++)
            {
                var t = new Thread(this.SlaveClientWorker);
                t.Start(this._slaveClients[i]);
                tlist.Add(t);
            }

            //master clients
            for (int i = 0; i < this._masterClients.Count; i++)
            {
                var t = new Thread(this.MasterClientWorker);
                t.Start(this._masterClients[i]);
                tlist.Add(t);
            }

            foreach (var t in tlist)
            {
                t.Join();
            }
        }


        private void MasterClientWorker(object state)
        {
            var ctx = (MasterClientContext)state;
            Console.WriteLine($"Master starting {ctx.EndPoints.Count} endpoints.");

            this._rrimpl.SetupConnection(ctx);
            using (IDisposable conn = ctx.Connection)
            {
                for (int i = 0; i < ctx.EndPoints.Count; i++)
                {
                    ThreadPool.QueueUserWorkItem(this._rrimpl.MasterEndPointWorker, ctx.EndPoints[i]);
                }
                ctx.BlockingEvent.Wait();
            }

            Console.WriteLine($"Master ended.");
        }


        private void SlaveClientWorker(object state)
        {
            var ctx = (SlaveClientContext)state;
            Console.WriteLine($"Slave starting {ctx.EndPoints.Count} endpoints.");

            long ms;
            this._rrimpl.SetupConnection(ctx);
            using (IDisposable conn = ctx.Connection)
            {
                this._rrimpl.SetupSubscriptions(ctx);

                var sw = Stopwatch.StartNew();
                this._rrimpl.SlaveEndPointWorker(ctx);
                sw.Stop();
                ms = sw.ElapsedMilliseconds;
            }

            Console.WriteLine($"Slave ended: {ms}ms; {ctx.Stats(ms):F1}MB/s");
        }

    }


    public interface IReqRepImpl
    {
        void SetupConnection(ClientContextBase ctx);
        void SetupSubscriptions(SlaveClientContext ctx);
        void MasterEndPointWorker(object state);
        void SlaveEndPointWorker(SlaveClientContext ctx);
    }


    public interface IMasterEndPoint
    {
        MasterClientContext MasterClient { get; }
        string TargetSubject { get; }
        ArraySegment<byte> GetRequest();
        void ValidateResponse(ArraySegment<byte> response);
        void Release();
    }

    public interface ISlaveEndPoint
    {
        SlaveClientContext SlaveClient { get; }
        ArraySegment<byte> GetResponse(ArraySegment<byte> request);
    }

    public class ClientContextBase
    {
        protected ClientContextBase() { }

        public IDisposable Connection;

        private long _totalBytes;
        public void AddBytes(int count)
        {
            Interlocked.Add(ref this._totalBytes, count);
        }

        public double Stats(long ms)
        {
            var count = Interlocked.Read(ref this._totalBytes);
            return count / 1000.0 / ms;
        }
    }

    public class MasterClientContext : ClientContextBase
    {
        public List<IMasterEndPoint> EndPoints { get; } = new List<IMasterEndPoint>();
        public CountdownEvent BlockingEvent;
        public int LoopCount;
    }

    public class SlaveClientContext : ClientContextBase
    {
        public SlaveClientContext(
            string subject,
            int slaveSubsPerClient
            )
        {
            this.Subject = subject;
            this.SlaveSubsPerClient = slaveSubsPerClient;
        }

        private int _completedCount;

        public string Subject { get; }
        public int SlaveSubsPerClient { get; }
        public IDisposable Subscription;
        public bool IsComplete => this._completedCount == this.EndPoints.Count;

        public Dictionary<string, ISlaveEndPoint> EndPoints { get; } = new Dictionary<string, ISlaveEndPoint>();

        public void Release() => this._completedCount++;

        public string GetSubjectPath(int subIndex, int masterIndex=-1)
        {
            return (masterIndex >= 0)
                ? $"{this.Subject}.sub{subIndex}.ms{masterIndex}"
                : $"{this.Subject}.sub{subIndex}";
        }
    }
}
