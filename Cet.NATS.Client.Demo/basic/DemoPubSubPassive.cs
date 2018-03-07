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
    class DemoPubSubPassive
    {

        private const string TestPayload = "Hello world!";


        public void Run()
        {
            //starts two kinds of subscribers
            Task.Factory.StartNew(this.SubscriberSync);
            Task.Factory.StartNew(this.SubscriberAsync);

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
        /// The sync-way subscriber
        /// </summary>
        private void SubscriberSync()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //subscribe to the subject
                IPassiveSubscription sub = conn.SubscribePassive("The.>");

                //waits the message
                MsgOut m = sub.NextMessage(CancellationToken.None);

                //verify the expectation
                Console.WriteLine("Sync received: " + m.GetPayloadAsString());
                Debug.Assert(m.GetPayloadAsString() == TestPayload);
            }
        }


        /// <summary>
        /// The async-way subscriber
        /// </summary>
        /// <returns></returns>
        private async Task SubscriberAsync()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //subscribe to the subject
                IPassiveSubscription sub = conn.SubscribePassive("The.>");

                //waits the message
                MsgOut m = await sub.NextMessageAsync(CancellationToken.None);

                //verify the expectation
                Console.WriteLine("Async received: " + m.GetPayloadAsString());
                Debug.Assert(m.GetPayloadAsString() == TestPayload);
            }
        }

    }
}
