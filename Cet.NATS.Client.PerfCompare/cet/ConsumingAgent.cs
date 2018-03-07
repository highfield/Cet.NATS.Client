using Cet.NATS.Client;
using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client.DemoPerfCompare.cet
{
    class ConsumingAgent
        : IDisposable
    {
        private ConsumingAgent() { }

        public static ConsumingAgent CreatePassive(
            IConnection conn,
            MyWorkerContext ctx,
            CountdownEvent cde
            )
        {
            var instance = new ConsumingAgent();
            instance._subPassive = conn.SubscribePassive(ctx.Subject + ".>");
            instance._ctx = ctx;
            instance._cde = cde;
            return instance;
        }

        public static ConsumingAgent CreateReactive(
            IConnection conn,
            MyWorkerContext ctx,
            CountdownEvent cde
            )
        {
            var instance = new ConsumingAgent();
            instance._subReactive = conn.SubscribeReactive(ctx.Subject + ".>", instance.ReactiveListener);
            instance._ctx = ctx;
            instance._cde = cde;
            return instance;
        }


        private IPassiveSubscription _subPassive;
        private IReactiveSubscription _subReactive;
        private int _n;
        private MyWorkerContext _ctx;
        private CountdownEvent _cde;


        public void PassiveWorker(object state)
        {
            while (this._n < this._ctx.LoopCount)
            {
                MsgOut m = this._subPassive.NextMessage(CancellationToken.None);
                bool match = this._ctx.DeserializeAndCheckSimpleJson(m.GetPayloadAsByteArray(), this._n);
                Debug.Assert(match);
                ++_n;
            }
            this._cde.Signal();
        }


        public async Task PassiveWorkerAsync()
        {
            while (this._n < this._ctx.LoopCount)
            {
                MsgOut m = await this._subPassive.NextMessageAsync(CancellationToken.None);
                bool match = this._ctx.DeserializeAndCheckSimpleJson(m.GetPayloadAsByteArray(), this._n);
                Debug.Assert(match);
                ++_n;
            }
        }


        private void ReactiveListener(MsgOut message, CancellationToken token)
        {
            bool match = this._ctx.DeserializeAndCheckSimpleJson(message.GetPayloadAsByteArray(), this._n);
            Debug.Assert(match);
            ++_n;
            if (this._n >= this._ctx.LoopCount)
            {
                this._cde.Signal();
            }
        }


        public void Dispose()
        {
            this._subPassive?.Dispose();
            this._subReactive?.Dispose();
        }

    }
}
