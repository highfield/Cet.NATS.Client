
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
using System.Globalization;
using System.Text;
using System.Threading;

namespace Cet.NATS.Client
{
    partial class Connection
    {

        // Prepare protocol messages for efficiency
        internal static readonly byte[] CRLF_BYTES = Encoding.UTF8.GetBytes(InternalConstants._CRLF_);

        //local string builder, pre-instantiated for performance reasons
        private readonly StringBuilder _pubBuilder = new StringBuilder();

        //local buffer to hold the message header, converted from the string builder
        private const int PublishProtoBufferBlockSize = 1 << 10;    //must be a power of two
        private const int PublishProtoBufferBlockMask = PublishProtoBufferBlockSize - 1;
        private byte[] _pubProtoBuf = new byte[PublishProtoBufferBlockSize];
        private readonly object _pubLocker = new object();


        /// <summary>
        /// Publishes a <see cref="MsgIn"/> instance, which includes the subject, an optional reply, and an
        /// optional data field.
        /// </summary>
        /// <remarks>
        /// <para>NATS implements a publish-subscribe message distribution model. NATS publish subscribe is a
        /// one-to-many communication. A publisher sends a message on a subject. Any active subscriber listening
        /// on that subject receives the message. Subscribers can register interest in wildcard subjects.</para>
        /// <para>In the basic NATS platfrom, if a subscriber is not listening on the subject (no subject match),
        /// or is not acive when the message is sent, the message is not received. NATS is a fire-and-forget
        /// messaging system. If you need higher levels of service, you can either use NATS Streaming, or build the
        /// additional reliability into your client(s) yourself.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        public void Publish(MsgIn message, CancellationToken token)
        {
            this.PublishCore(message, Timeout.InfiniteTimeSpan, token);
        }


        /// <summary>
        /// Publishes a <see cref="MsgIn"/> instance, which includes the subject, an optional reply, and an
        /// optional data field.
        /// </summary>
        /// <remarks>
        /// <para>NATS implements a publish-subscribe message distribution model. NATS publish subscribe is a
        /// one-to-many communication. A publisher sends a message on a subject. Any active subscriber listening
        /// on that subject receives the message. Subscribers can register interest in wildcard subjects.</para>
        /// <para>In the basic NATS platfrom, if a subscriber is not listening on the subject (no subject match),
        /// or is not acive when the message is sent, the message is not received. NATS is a fire-and-forget
        /// messaging system. If you need higher levels of service, you can either use NATS Streaming, or build the
        /// additional reliability into your client(s) yourself.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="timeout">Represents the time to wait for the connection going stable.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        public void Publish(MsgIn message, TimeSpan timeout, CancellationToken token)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
            }

            this.PublishCore(message, timeout, token);
        }


        /// <summary>
        /// Common entry-point for all the "publish" public methods
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timeout"></param>
        /// <param name="token"></param>
        private void PublishCore(
            MsgIn message, 
            TimeSpan timeout, 
            CancellationToken token
            )
        {
            if (this._connmgr.IsLogicalStarted == false)
            {
                this._connmgr.WaitLogicalStart(timeout, token);
            }

            // Proactively reject payloads over the threshold set by server.
            int payloadLength = message.PayloadInternal.Length;
            if (payloadLength > this._connmgr.MaxPayload)
            {
                throw new NATSMaxPayloadException();
            }

            //ensures the following section mutex-accessed
            lock (this._pubLocker)
            {
                //build the header as described here:
                //https://nats.io/documentation/internals/nats-protocol/
                this._pubBuilder.Length = 0;
                this._pubBuilder.Append(InternalConstants._PUB_P_);
                this._pubBuilder.Append(message.Subject);
                this._pubBuilder.Append(' ');

                if (message.InboxRequest != null)
                {
                    this._pubBuilder.Append(message.InboxRequest.ReplyToSubject);
                    this._pubBuilder.Append(' ');
                }
                else if (message.ReplyTo.Length != 0)
                {
                    this._pubBuilder.Append(message.ReplyTo);
                    this._pubBuilder.Append(' ');
                }

                this._pubBuilder.Append(payloadLength.ToString(CultureInfo.InvariantCulture));
                this._pubBuilder.Append(InternalConstants._CRLF_);
                int buflen = Encoding.UTF8.GetBytes(this._pubBuilder.ToString(), 0, this._pubBuilder.Length, this._pubProtoBuf, 0);

                //outstreams the pub data (header and payload)
                this._connmgr.WritePubData(
                    new ArraySegment<byte>(this._pubProtoBuf, 0, buflen),
                    message.PayloadInternal
                    );
            }
        }

    }
}
