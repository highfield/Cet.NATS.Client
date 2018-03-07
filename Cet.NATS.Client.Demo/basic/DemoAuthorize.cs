using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Cet.NATS.Client.NetCoreDemo
{
    class DemoAuthorize
    {

        public void Run()
        {
            //starts the authorized publisher first,
            //then the anonymous one
            this.PublisherAuthorized();
            this.PublisherAnonymous();
        }


        private void PublisherAuthorized()
        {
            var endPoint = ServerEndPoint.WithCredentials(
                Defaults.Address,
                Defaults.Port,
                userName: "foo",
                password: "bar",
                secured: false
                );

            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            opts.AddServer(endPoint);

            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //sends a single message having a string as payload
                Console.WriteLine("Authorized is publishing...");
                conn.Publish(
                    new MsgIn("The.Target").SetPayload("the message"),
                    CancellationToken.None
                    );
            }
        }


        private void PublisherAnonymous()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //sends a single message having a string as payload
                Console.WriteLine("Anonymous is publishing...");
                conn.Publish(
                    new MsgIn("The.Target").SetPayload("another message"),
                    CancellationToken.None
                    );
            }
        }

    }
}
