using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Threading;
using Original = NATS.Client;

namespace Cet.NATS.Client.DemoPerfCompare.orig
{
    static class Workers
    {

        public static void RunPublisher(
            Original.IConnection conn,
            MyWorkerContext ctx
            )
        {
            Console.WriteLine("Pub start " + ctx.Subject);
            ctx.CronoStart();

            var agent = PublishingAgent.Create(conn, ctx, null);
            agent.Worker(null);

            ctx.CronoStop();
            Console.WriteLine($"Pub end {ctx.FormatStats(1)}");
        }


        public static void RunParallelPassiveConsumer(
            Original.IConnection conn,
            MyWorkerContext ctx,
            int agentCount
            )
        {
            var agentList = new List<ConsumingAgent>();
            try
            {
                Console.WriteLine($"Sub ({agentCount}) start {ctx.Subject}");
                ctx.CronoStart();

                var cde = new CountdownEvent(agentCount);
                for (int i = 0; i < agentCount; i++)
                {
                    var agent = ConsumingAgent.CreatePassive(conn, ctx, cde);
                    agentList.Add(agent);
                    ThreadPool.QueueUserWorkItem(agent.PassiveWorker);
                }
                cde.Wait();

                ctx.CronoStop();
                Console.WriteLine($"Sub ({agentCount}) end {ctx.Subject}");
            }
            finally
            {
                foreach (var agent in agentList)
                {
                    agent.Dispose();
                }
            }
        }


        public static void RunParallelReactiveConsumer(
            Original.IConnection conn,
            MyWorkerContext ctx,
            int agentCount
            )
        {
            var agentList = new List<ConsumingAgent>();
            try
            {
                Console.WriteLine($"Sub ({agentCount}) start {ctx.Subject}");
                ctx.CronoStart();

                var cde = new CountdownEvent(agentCount);
                for (int i = 0; i < agentCount; i++)
                {
                    var agent = ConsumingAgent.CreatePassive(conn, ctx, cde);
                    agentList.Add(agent);
                    ThreadPool.QueueUserWorkItem(agent.PassiveWorker);
                }
                cde.Wait();

                ctx.CronoStop();
                Console.WriteLine($"Sub ({agentCount}) end {ctx.Subject}");
            }
            finally
            {
                foreach (var agent in agentList)
                {
                    agent.Dispose();
                }
            }
        }

    }
}
