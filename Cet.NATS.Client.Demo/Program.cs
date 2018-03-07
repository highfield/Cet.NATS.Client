using System;

namespace Cet.NATS.Client.NetCoreDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            //new DemoPubSubPassive().Run();
            //new DemoPubSubReactive().Run();
            //new DemoReqRepSync().Run();
            //new DemoUnsubscribe().Run();
            //new DemoAutoUnsubscribe().Run();
            new DemoAuthorize().Run();

            Console.Write("Press any key...");
            Console.ReadKey();
        }
    }
}
