using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client.NetCoreDemo
{
    /// <summary>
    /// This class demonstrates the <see cref="ISubscription.Unsubscribe"/> feature
    /// </summary>
    /// <remarks>
    /// Once both the publisher and the subscriber are started,
    /// the publisher issues a message and that is received
    /// by the subscriber. At that point, the publisher blocks waiting
    /// for a sempahore, which will be released by the subscriber only 
    /// after it is unsubscribed.
    /// The next messages issued by the publisher won't be received.
    /// The short pause before releasing the semaphore yields enough
    /// time to communicate the unsubscription to the NATS broker.
    /// </remarks>
    class DemoUnsubscribe
    {

        private const string TestPayload = "Hello world!";


        public void Run()
        {
            this._sem = new SemaphoreSlim(0);

            //starts the subscriber
            Task.Factory.StartNew(this.SubscriberSync);

            //starts the publisher
            this.Publisher();
        }


        private SemaphoreSlim _sem;


        private void Publisher()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //sends a message...
                Console.WriteLine("Publishing: " + TestPayload);
                conn.Publish(
                    new MsgIn("The.Target").SetPayload(TestPayload),
                    CancellationToken.None
                    );

                //...then waits the semaphore...
                this._sem.Wait();
                this._sem.Dispose();

                //...then sends another one
                conn.Publish(
                    new MsgIn("The.Target").SetPayload(TestPayload),
                    CancellationToken.None
                    );
            }
        }


        private void SubscriberSync()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                IPassiveSubscription sub = conn.SubscribePassive("The.>");

                //waits for a message
                MsgOut m = sub.NextMessage(CancellationToken.None);
                Console.WriteLine("Sync received: " + m.GetPayloadAsString());
                Debug.Assert(m.GetPayloadAsString() == TestPayload);

                //unsubscribes the subject
                sub.Unsubscribe();
                Task.Delay(1000).Wait();

                //releases the semaphore
                this._sem.Release();
            }
        }

    }
}
