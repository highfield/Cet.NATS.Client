using Cet.NATS.Client.DemoShared;
using System;
using System.Threading;
using Original = NATS.Client;

namespace Cet.NATS.Client.DemoPerfCompare.orig
{
    class PublishingAgent
        : IDisposable
    {
        private PublishingAgent() { }

        public static PublishingAgent Create(
            Original.IConnection conn,
            MyWorkerContext ctx,
            CountdownEvent cde
            )
        {
            var instance = new PublishingAgent();
            instance._conn = conn;
            instance._ctx = ctx;
            instance._cde = cde;
            instance._buffer = new byte[1000 + (int)(ctx.Description.Length * 1.1)];
            return instance;
        }


        private Original.IConnection _conn;
        private MyWorkerContext _ctx;
        private CountdownEvent _cde;
        private byte[] _buffer;


        public void Worker(object state)
        {
            for (int i = 0; i < this._ctx.LoopCount; i++)
            {
                int buflen = this._ctx.SerializeSimpleJson(this._buffer, i);
                byte[] ba = new byte[buflen];
                Buffer.BlockCopy(this._buffer, 0, ba, 0, buflen);
                this._conn.Publish(
                    this._ctx.Subject + ".t" + i, 
                    ba
                    );

                Interlocked.Add(ref this._ctx.TotalBytes, buflen);
                if ((i % 1000) == 0)
                {
                    Thread.Sleep(10);
                    //Task.Delay(10).Wait();
                }
            }
            this._cde?.Signal();
        }


        public void Dispose()
        {
            //do nothing
        }

    }
}
