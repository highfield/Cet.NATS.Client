using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Cet.NATS.Client.DemoShared
{
    public class MyWorkerContext
    {
        public MyWorkerContext(
            string subject, 
            string description,
            int loopCount
            )
        {
            this.Subject = subject;
            this.Description = description;
            this.LoopCount = loopCount;

            _jobjSimple = new JObject()
            {
                ["descr"] = description,
                ["dt"] = DateTime.Now,
                ["num"] = 0,
                ["flag"] = false
            };
        }


        public string Subject { get; }
        public string Description { get; }
        public int LoopCount { get; }


        #region Simple Json

        private JObject _jobjSimple;


        public int SerializeSimpleJson(
            byte[] buffer,
            int num
            )
        {
            this._jobjSimple["num"] = num;
            string s = _jobjSimple.ToString(Newtonsoft.Json.Formatting.None);
            return Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
        }


        public bool DeserializeAndCheckSimpleJson(
            byte[] buffer,
            int expectedNum
            )
        {
            var jobj = JObject.Parse(Encoding.UTF8.GetString(buffer));
            var descr = (string)jobj["descr"];
            var num = (int)jobj["num"];
            return descr == this.Description && num == expectedNum;
        }


        #endregion


        #region Stats

        public int TotalBytes;


        public string FormatStats(int agentCount)
        {
            double totalBytePerAgent = (double)this.TotalBytes / agentCount;
            return $"{this.Subject} ({agentCount}): {this._ws.ElapsedMilliseconds}ms; {totalBytePerAgent / 1000.0 / this._ws.ElapsedMilliseconds:F1}MB/s";
        }

        #endregion


        #region Stopwatch

        private Stopwatch _ws;


        public void CronoStart()
        {
            this.TotalBytes = 0;
            this._ws = Stopwatch.StartNew();
        }


        public long CronoStop()
        {
            this._ws.Stop();
            return this._ws.ElapsedMilliseconds;
        }

        #endregion

    }
}
