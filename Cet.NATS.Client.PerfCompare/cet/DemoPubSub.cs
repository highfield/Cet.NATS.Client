using Cet.NATS.Client.DemoShared;
using System.Threading;

namespace Cet.NATS.Client.DemoPerfCompare.cet
{
    class DemoPubSub
    {

        private const string Subject = CommonUtils.SubjectAscii;
        private static string Description = CommonUtils.SampleText_1000;
        private const int LoopCount = 100000;
        private const int ConsumingAgentCount = 5;


        public void Run()
        {
            var consThread = new Thread(this.ConsumingWorker);
            {
                var ctx = new MyWorkerContext(Subject, Description, LoopCount);
                consThread.Start(ctx);
            }

            var prodThread = new Thread(this.ProducingWorker);
            {
                var ctx = new MyWorkerContext(Subject, Description, LoopCount);
                prodThread.Start(ctx);
            }

            prodThread.Join();
            consThread.Join();
        }


        private void ProducingWorker(object state)
        {
            var ctx = (MyWorkerContext)state;

            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                Thread.Sleep(1000);
                Workers.RunPublisher(conn, ctx);
            }
        }


        private void ConsumingWorker(object state)
        {
            var ctx = (MyWorkerContext)state;

            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                Workers.RunParallelPassiveConsumer(conn, ctx, ConsumingAgentCount);
                //Workers.RunParallelReactiveConsumer(conn, ctx, ConsumingAgentCount);
            }
        }

    }
}
