
/********************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright 2018+ Cet Electronics.
 * 
 * Based on the original work by Apcera Inc.
 * https://github.com/nats-io/csharp-nats
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*********************************************************************************/

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Cet.NATS.Client
{
    /// <summary>
    /// POCO-class to hold the connection parameters to
    /// send the connected NATS server
    /// </summary>
    [DataContract]
    internal class ConnectInfo
    {

        [DataMember(Name = "verbose")]
        public bool Verbose { get; set; }

        [DataMember(Name = "pedantic")]
        public bool Pedantic { get; set; }

        [DataMember(Name = "user")]
        public string User { get; set; }

        [DataMember(Name = "pass")]
        public string Pass { get; set; }

        [DataMember(Name = "ssl_required")]
        public bool SslRequired { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "auth_token")]
        public string AuthToken { get; set; }

        [DataMember(Name = "lang")]
        public string Lang { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "protocol")]
        public int Protocol { get; set; }


        /// <summary>
        /// Instance to JSON to string serialization.
        /// </summary>
        /// <returns></returns>
        internal string ToJson()
        {
            var serializer = new DataContractJsonSerializer(typeof(ConnectInfo));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

    }
}
