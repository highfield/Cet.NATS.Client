using Cet.NATS.Client.DemoShared;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client.NetCoreDemo
{
    /// <summary>
    /// This class demonstrates one of the simplest forms of publish-subscribe
    /// </summary>
    /// <remarks>
    /// Once both the publisher and the subscribers are started,
    /// the publisher issues a message and that should be correctly received
    /// by both the subscribers.
    /// The two subscribers join the same subject, and differs for the
    /// kind of pattern: synchronous and asynchronous.
    /// In this demo, the approach to consume the incoming messages is "passive",
    /// that is, the worker "waits" sticky the next massage.
    /// That's much like "pulling" the message out the NATS client.
    /// </remarks>
    class DemoPubSubReactive
    {

        private const string TestPayload = "Hello world!";


        public void Run()
        {
            //starts two kinds of subscribers
            Task.Factory.StartNew(this.Subscriber1);
            Task.Factory.StartNew(this.Subscriber2);

            //starts the publisher
            this.Publisher();
        }


        private void Publisher()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //sends a single message having a string as payload
                Console.WriteLine("Publishing: " + TestPayload);
                conn.Publish(
                    new MsgIn("The.Target").SetPayload(TestPayload),
                    CancellationToken.None
                    );
            }
        }


        /// <summary>
        /// The sync-handler subscriber
        /// </summary>
        private void Subscriber1()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //subscribe to the subject
                IReactiveSubscription sub = conn.SubscribeReactive("The.>", Sub1Handler);

                //waits here until the semaphore will be released
                _sem1.Wait();
            }
        }


        private SemaphoreSlim _sem1 = new SemaphoreSlim(0);


        private void Sub1Handler(MsgOut m, CancellationToken token)
        {
            Console.WriteLine("Sub1 received: " + m.GetPayloadAsString());
            Debug.Assert(m.GetPayloadAsString() == TestPayload);

            //releases the semaphore
            _sem1.Release();
        }


        /// <summary>
        /// The async-handler subscriber
        /// </summary>
        /// <returns></returns>
        private void Subscriber2()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //subscribe to the subject
                IReactiveSubscription sub = conn.SubscribeAsyncReactive("The.>", Sub2HandlerAsync);

                //waits here until the semaphore will be released
                _sem2.Wait();
            }
        }


        private SemaphoreSlim _sem2 = new SemaphoreSlim(0);


        private async Task Sub2HandlerAsync(MsgOut m, CancellationToken token)
        {
            Console.WriteLine("Sub2 (async) received: " + m.GetPayloadAsString());
            Debug.Assert(m.GetPayloadAsString() == TestPayload);

            //releases the semaphore
            _sem2.Release();
            await Task.CompletedTask;
        }

    }
}
