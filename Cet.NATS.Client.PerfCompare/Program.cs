using System;

namespace Cet.NATS.Client.DemoPerfCompare
{
    class Program
    {
        static void Main(string[] args)
        {
            new orig.DemoPubSub().Run();
            //new cet.DemoPubSub().Run();
            //new cet.DemoPubSubAsync().Run();
            //new orig.DemoRequestReply().Run();
            //new cet.DemoRequestReply().Run();

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
