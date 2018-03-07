using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cet.NATS.Client.DemoShared
{
    public class ReqRepBridge
        : IMasterEndPoint, ISlaveEndPoint
    {
        public ReqRepBridge(
            MasterClientContext master,
            SlaveClientContext slave,
            string masterTargetSubject,
            string sampleText
            )
        {
            this._master = master;
            this._slave = slave;
            this._masterTargetSubject = masterTargetSubject;
            this._sampleText = sampleText;

            _jobjSimple = new JObject()
            {
                ["descr"] = sampleText,
                ["dt"] = DateTime.Now,
                ["num"] = 0,
                ["flag"] = false
            };

            int buflen = Math.Max((int)(sampleText.Length * 1.2), 1000);
            this._buffer = new byte[buflen];
        }


        private readonly MasterClientContext _master;
        private readonly SlaveClientContext _slave;
        private readonly string _sampleText;
        private readonly string _masterTargetSubject;

        private JObject _jobjSimple;
        private int _num;
        private readonly byte[] _buffer;


        #region IMasterEndPoint

        MasterClientContext IMasterEndPoint.MasterClient => this._master;

        string IMasterEndPoint.TargetSubject => this._masterTargetSubject;

        ArraySegment<byte> IMasterEndPoint.GetRequest()
        {
            return this.SerializeJson(++this._num);
        }

        void IMasterEndPoint.Release()
        {
            this._slave.Release();
            this._master.BlockingEvent.Signal();
        }

        void IMasterEndPoint.ValidateResponse(ArraySegment<byte> response)
        {
            this.DeserializeJson(response, -this._num);
        }

        #endregion


        #region ISlaveEndPoint

        SlaveClientContext ISlaveEndPoint.SlaveClient => this._slave;

        ArraySegment<byte> ISlaveEndPoint.GetResponse(ArraySegment<byte> request)
        {
            this.DeserializeJson(request, this._num);
            return this.SerializeJson(-this._num);
        }

        #endregion


        private ArraySegment<byte> SerializeJson(int num)
        {
            this._jobjSimple["num"] = num;
            string s = _jobjSimple.ToString(Newtonsoft.Json.Formatting.None);
            int count = Encoding.UTF8.GetBytes(s, 0, s.Length, this._buffer, 0);
            return new ArraySegment<byte>(this._buffer, 0, count);
        }


        public bool DeserializeJson(
            ArraySegment<byte> segment,
            int expectedNum
            )
        {
            string srx = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
            this._jobjSimple = JObject.Parse(srx);
            bool match =
                (string)this._jobjSimple["descr"] == this._sampleText &&
                (int)this._jobjSimple["num"] == expectedNum;

            System.Diagnostics.Debug.Assert(match);
            return match;
        }

    }
}
