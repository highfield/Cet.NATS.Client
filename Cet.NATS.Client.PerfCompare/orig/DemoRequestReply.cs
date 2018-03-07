using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Original = NATS.Client;

namespace Cet.NATS.Client.DemoPerfCompare.orig
{
    class DemoRequestReply : IReqRepImpl
    {

        public void Run()
        {
            var args = new ReqRepArgs()
            {
                SlaveClientCount = 1,
                SlaveSubsPerClient = 5,
                MasterClientPerSlave = 10,
                LoopCount = 1000,
                BaseSubjectPath = CommonUtils.SubjectAscii,
                SampleText = CommonUtils.SampleText_1000,
            };

            var rrctx = new ReqRepContext(this);
            rrctx.Run(args);
        }


        void IReqRepImpl.SetupConnection(ClientContextBase ctx)
        {
            Original.Options opts = ConnectionUtils.GetDefaultOptions();
            var cf = new Original.ConnectionFactory();
            ctx.Connection = cf.CreateConnection(opts);
        }


        void IReqRepImpl.SetupSubscriptions(SlaveClientContext ctx)
        {
            var conn = (Original.IConnection)ctx.Connection;
            ctx.Subscription = conn.SubscribeSync(ctx.Subject + ".>");
        }


        void IReqRepImpl.MasterEndPointWorker(object state)
        {
            var ep = (IMasterEndPoint)state;
            var conn = (Original.IConnection)ep.MasterClient.Connection;
            int n = ep.MasterClient.LoopCount;
            while (--n >= 0)
            {
                if (n == 0)
                {
                    ep.Release();
                }
                var segment = ep.GetRequest();
                byte[] ba = new byte[segment.Count];
                Buffer.BlockCopy(segment.Array, segment.Offset, ba, 0, segment.Count);
                Original.Msg m = conn.Request(ep.TargetSubject, ba);

                ep.ValidateResponse(m.Data);
            }
        }


        void IReqRepImpl.SlaveEndPointWorker(SlaveClientContext ctx)
        {
            var conn = (Original.IConnection)ctx.Connection;
            var sub = (Original.ISyncSubscription)ctx.Subscription;
            while (ctx.IsComplete == false)
            {
                Original.Msg m = sub.NextMessage();
                ISlaveEndPoint ep = ctx.EndPoints[m.Subject];
                var segment = ep.GetResponse(m.Data);
                ctx.AddBytes(m.Data.Length + segment.Count);

                byte[] ba = new byte[segment.Count];
                Buffer.BlockCopy(segment.Array, segment.Offset, ba, 0, segment.Count);
                conn.Publish(m.Reply, ba);
            }
        }

    }
}
