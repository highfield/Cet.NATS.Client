using Cet.NATS.Client;
using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client.DemoPerfCompare.cet
{
    class DemoPubSubAsync
    {

        private const string Subject = CommonUtils.SubjectAscii;
        private static string Description = CommonUtils.SampleText_1000;
        private const int LoopCount = 1000;
        private const int ConsumingAgentCount = 1;


        public void Run()
        {
            Task consTask;
            {
                var ctx = new MyWorkerContext(Subject, Description, LoopCount);
                consTask = Task.Run(() => this.ConsumingWorkerAsync(ctx));
            }

            Task prodTask;
            {
                var ctx = new MyWorkerContext(Subject, Description, LoopCount);
                prodTask = Task.Run(() => this.ProducingWorkerAsync(ctx));
            }

            prodTask.Wait();
            consTask.Wait();
        }


        private async Task ProducingWorkerAsync(MyWorkerContext ctx)
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = await cf.CreateConnectionAsync(opts, CancellationToken.None))
            {
                Workers.RunPublisher(conn, ctx);
            }
        }


        private async Task ConsumingWorkerAsync(MyWorkerContext ctx)
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = await cf.CreateConnectionAsync(opts, CancellationToken.None))
            {
                await Workers.RunParallelPassiveConsumerAsync(conn, ctx, ConsumingAgentCount);
                //Workers.RunParallelReactiveConsumer(conn, ctx, ConsumingAgentCount);
            }
        }

    }
}
