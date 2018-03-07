using Cet.NATS.Client;
using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Cet.NATS.Client.DemoPerfCompare.cet
{
    class DemoRequestReply : IReqRepImpl
    {

        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);


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
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            ctx.Connection = cf.CreateConnection(opts);
        }


        void IReqRepImpl.SetupSubscriptions(SlaveClientContext ctx)
        {
            var conn = (IConnection)ctx.Connection;
            ctx.Subscription = conn.SubscribePassive(ctx.Subject + ".>");
        }


        void IReqRepImpl.MasterEndPointWorker(object state)
        {
            var ep = (IMasterEndPoint)state;
            var conn = (IConnection)ep.MasterClient.Connection;
            int n = ep.MasterClient.LoopCount;
            while (--n >= 0)
            {
                if (n == 0)
                {
                    ep.Release();
                }
                var segment = ep.GetRequest();
                MsgOut m = conn.Request(
                    new MsgIn(ep.TargetSubject).SetPayload(segment),
                    RequestTimeout,
                    CancellationToken.None
                    );

                ep.ValidateResponse(m.GetPayloadAsByteArray());
            }
        }


        void IReqRepImpl.SlaveEndPointWorker(SlaveClientContext ctx)
        {
            var conn = (IConnection)ctx.Connection;
            var sub = (IPassiveSubscription)ctx.Subscription;
            while (ctx.IsComplete == false)
            {
                MsgOut m = sub.NextMessage(CancellationToken.None);
                ISlaveEndPoint ep = ctx.EndPoints[m.Subject];
                var segment = ep.GetResponse(m.GetPayloadAsByteArray());
                ctx.AddBytes(m.PayloadLength + segment.Count);
                conn.Publish(
                    new MsgIn(m.ReplyTo).SetPayload(segment),
                    CancellationToken.None
                    );
            }
        }

    }
}
