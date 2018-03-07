
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

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Cet.NATS.Client
{
    /// <summary>
    /// POCO-class to hold the info received from
    /// the connected NATS server
    /// </summary>
    [DataContract]
    internal class ServerInfo
    {
        /// <summary>
        /// Creates and populates the instance upon the received byte-array segment
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        internal static ServerInfo Create(ArraySegment<byte> segment)
        {
            using (var stream = new MemoryStream(segment.Array, segment.Offset, segment.Count))
            {
                var serializer = new DataContractJsonSerializer(typeof(ServerInfo));
                stream.Position = 0;
                return (ServerInfo)serializer.ReadObject(stream);
            }
        }


        [DataMember(Name = "server_id")]
        public string ServerId { get; set; }

        [DataMember(Name ="host")]
        public string Host { get; set; }

        [DataMember(Name = "port")]
        public int Port { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "auth_required")]
        public bool AuthRequired { get; set; }

        [DataMember(Name = "tls_required")]
        public bool TlsRequired { get; set; }

        [DataMember(Name = "max_payload")]
        public long MaxPayload { get; set; }

        [DataMember(Name = "connect_urls")]
        public string[] ConnectUrls { get; set; }

    }
}
