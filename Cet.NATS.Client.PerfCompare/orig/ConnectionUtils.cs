using Cet.NATS.Client.DemoShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

using Original = NATS.Client;

namespace Cet.NATS.Client.DemoPerfCompare.orig
{
    static class ConnectionUtils
    {

        private static string[] servers = new string[]
        {
            "nats://127.0.0.1:4222",
            //"nats://localhost:4222"
        };


        public static Original.Options GetDefaultOptions()
        {
            Original.Options opts = Original.ConnectionFactory.GetDefaultOptions();
            opts.PingInterval = 30000;
            opts.Timeout = 5000;
            opts.Servers = servers;
            return opts;
        }

    }
}
