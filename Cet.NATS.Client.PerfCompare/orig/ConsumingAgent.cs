using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

using Original = NATS.Client;

namespace Cet.NATS.Client.DemoPerfCompare.orig
{
    class ConsumingAgent 
        : IDisposable
    {
        private ConsumingAgent() { }

        public static ConsumingAgent CreatePassive(
            Original.IConnection conn,
            MyWorkerContext ctx,
            CountdownEvent cde
            )
        {
            var instance = new ConsumingAgent();
            instance._subPassive = conn.SubscribeSync(ctx.Subject + ".>");
            instance._ctx = ctx;
            instance._cde = cde;
            return instance;
        }

        public static ConsumingAgent CreateReactive(
            Original.IConnection conn,
            MyWorkerContext ctx,
            CountdownEvent cde
            )
        {
            var instance = new ConsumingAgent();
            instance._subReactive = conn.SubscribeAsync(ctx.Subject + ".>", instance.ReactiveHandler);
            instance._ctx = ctx;
            instance._cde = cde;
            return instance;
        }


        private Original.ISyncSubscription _subPassive;
        private Original.IAsyncSubscription _subReactive;
        private int _n;
        private MyWorkerContext _ctx;
        private CountdownEvent _cde;


        public void PassiveWorker(object state)
        {
            while (this._n < this._ctx.LoopCount)
            {
                Original.Msg m = this._subPassive.NextMessage();
                bool match = this._ctx.DeserializeAndCheckSimpleJson(m.Data, this._n);
                Debug.Assert(match);
                ++_n;
            }
            this._cde.Signal();
        }


        private void ReactiveHandler(object sender, Original.MsgHandlerEventArgs e)
        {
            bool match = this._ctx.DeserializeAndCheckSimpleJson(e.Message.Data, this._n);
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
