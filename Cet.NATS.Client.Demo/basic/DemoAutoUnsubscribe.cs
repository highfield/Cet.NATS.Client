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
    /// This class demonstrates the <see cref="ISubscription.AutoUnsubscribe(int)"/> feature
    /// </summary>
    /// <remarks>
    /// Once both the publisher and the subscriber are started,
    /// the publisher will issue a number of messages delayed each one.
    /// The subscriber should accept the exact number of messages,
    /// then self-unsubscribe and quit the thread
    /// Actually, the publisher will send a message more,
    /// but that shouldn't received by the subscriber.
    /// The short pause between messages yields enough
    /// time to communicate the unsubscription to the NATS broker.
    /// </remarks>
    class DemoAutoUnsubscribe
    {

        private const string TestPayload = "Hello world!";
        private const int MessageCount = 5;


        public void Run()
        {
            //starts the subscriber
            Task.Factory.StartNew(this.SubscriberSync);

            //starts the publisher
            this.Publisher();
        }


        private void Publisher()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //sends MessageCount+1 messages to the subscriber
                for (int i = 0; i <= MessageCount; i++)
                {
                    Console.WriteLine("Publishing: " + TestPayload);
                    conn.Publish(
                        new MsgIn("The.Target").SetPayload(TestPayload),
                        CancellationToken.None
                        );

                    //a short delay before the next message
                    Task.Delay(1000).Wait();
                }
            }
        }


        private void SubscriberSync()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                IPassiveSubscription sub = conn.SubscribePassive("The.>");

                //instructs the subscriber to self-destroy after
                //exactly MessageCount messages
                sub.AutoUnsubscribe(MessageCount);

                int count = MessageCount;
                while (sub.IsValid)
                {
                    try
                    {
                        //waits for the next message
                        MsgOut m = sub.NextMessage(CancellationToken.None);
                        Console.WriteLine("Sync received: " + m.GetPayloadAsString());
                        Debug.Assert(m.GetPayloadAsString() == TestPayload);
                    }
                    catch (NATSBadSubscriptionException)
                    {
                        //that should be raised by the subscriber,
                        //because the current thread is stick to NextMessage method
                        //but the subscription is actually being disposed
                        break;
                    }
                    catch
                    {
                        //any other exception should be thrown elsewhere
                        throw;
                    }
                    count--;
                }

                Console.WriteLine("Unsubscribed");
                Debug.Assert(count == 0);
            }
        }

    }
}
