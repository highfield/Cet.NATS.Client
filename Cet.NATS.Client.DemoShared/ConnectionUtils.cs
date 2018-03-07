using Cet.NATS.Client;
using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client.DemoShared
{
    public static class ConnectionUtils
    {

        //private static Uri[] servers = new Uri[]
        //{
        //    new Uri("nats://127.0.0.1:4222"),
        //    //new Uri("nats://localhost:4222")
        //};


        public static ClientOptions GetDefaultOptions()
        {
            var opts = new ClientOptions();
            opts.PingInterval = TimeSpan.FromSeconds(30);
            opts.Timeout = TimeSpan.FromSeconds(5);
            return opts;
        }

    }
}
