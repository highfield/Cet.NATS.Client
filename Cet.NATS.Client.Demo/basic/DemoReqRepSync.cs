using Cet.NATS.Client.DemoShared;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client.NetCoreDemo
{
    /// <summary>
    /// This class demonstrates the simplest form of request-reply
    /// </summary>
    /// <remarks>
    /// There is a "master" which initiates a request against a certain subject,
    /// then there is a "slave" which accepts the request, and publish a response
    /// back to another subject. The latter subject will (typically) match 
    /// the originator, that is the master.
    /// Contrary to the pub-sub case, when the issuer of the request (master)
    /// targets an invalid subject (the NATS broker isn't able to dispatch)
    /// it will get an exception.
    /// By other hand, when the slave sends the reply back, that is a
    /// normal <see cref="IConnection.Publish(MsgIn, CancellationToken)"/>
    /// operation which won't give errors for bad addressing.
    /// </remarks>
    class DemoReqRepSync
    {

        public void Run()
        {
            //starts the slave
            Task.Factory.StartNew(this.Slave);

            //starts the master
            this.Master();
        }


        private string _expectedResponsePayload;


        private void Master()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //sends a request and waits for the response
                MsgOut m = conn.Request(
                    new MsgIn("The.Target").SetPayload("Mario"),
                    TimeSpan.FromSeconds(1),
                    CancellationToken.None
                    );

                Console.WriteLine("Master received: " + m.GetPayloadAsString());
                Debug.Assert(m.GetPayloadAsString() == this._expectedResponsePayload);
            }
        }


        private void Slave()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                IPassiveSubscription sub = conn.SubscribePassive("The.>");

                //waits for a request
                MsgOut m = sub.NextMessage(CancellationToken.None);
                Console.WriteLine("Slave received: " + m.GetPayloadAsString());

                //builds the response up, then publish it back as reply
                this._expectedResponsePayload = "Hello " + m.GetPayloadAsString() + "!";
                conn.Publish(
                    new MsgIn(m.ReplyTo).SetPayload(this._expectedResponsePayload),
                    CancellationToken.None
                    );
            }
        }

    }
}
