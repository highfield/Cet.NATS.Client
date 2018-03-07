
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

using IllyriadGames.ByteArrayExtensions;
using System;
using System.Text;

namespace Cet.NATS.Client
{
    /// <summary>
    /// Represents an input message produced by the host app,
    /// and sent to the NATS server.
    /// This typically holds data to deliver to another endpoint.
    /// </summary>
    public sealed class MsgIn
    {
        /// <summary>
        /// Creates the simplest form of a <see cref="MsgIn"/>.
        /// </summary>
        /// <param name="subject">The target subject to deliver the message to.</param>
        public MsgIn(
            string subject
            )
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentNullException(nameof(subject));
            }

            this.Subject = subject;
            this.ReplyTo = string.Empty;
        }


        /// <summary>
        /// Creates a <see cref="MsgIn"/> with a "reply-to" indication.
        /// </summary>
        /// <param name="subject">The target subject to deliver the message to.</param>
        /// <param name="replyTo">The target subject to deliver the response message to.</param>
        public MsgIn(
            string subject, 
            string replyTo
            )
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentNullException(nameof(subject));
            }
            if (string.IsNullOrWhiteSpace(replyTo))
            {
                throw new ArgumentNullException(nameof(replyTo));
            }

            this.Subject = subject;
            this.ReplyTo = replyTo;
        }


        /// <summary>
        /// The target subject to deliver the message to.
        /// </summary>
        public readonly string Subject;


        /// <summary>
        /// The target subject to deliver the response message to.
        /// </summary>
        public readonly string ReplyTo;

        internal InFlightRequest InboxRequest;
        internal byte[] PayloadInternal = Helpers.EmptyByteArray;


        /// <summary>
        /// Sets the payload as a simple byte-array
        /// </summary>
        /// <param name="buffer">The byte array to be used as payload</param>
        /// <returns>The <see cref="MsgIn"/> instance itself, useful for fluent patterns.</returns>
        public MsgIn SetPayload(byte[] buffer)
        {
            return this.SetPayload(buffer, 0, buffer.Length);
        }


        /// <summary>
        /// Sets the payload as a segment of a byte-array
        /// </summary>
        /// <param name="buffer">The byte array where to read the data from.</param>
        /// <param name="offset">The index where to start to read from.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The <see cref="MsgIn"/> instance itself, useful for fluent patterns.</returns>
        public MsgIn SetPayload(byte[] buffer, int offset, int count)
        {
            this.PayloadInternal = new byte[count];
            VectorizedCopyExtension.VectorizedCopy(
                buffer,
                offset,
                this.PayloadInternal,
                0,
                count
                );

            return this;
        }


        /// <summary>
        /// Sets the payload as a segment of a byte-array
        /// </summary>
        /// <param name="segment">An <see cref="ArraySegment{byte}"/> indicating the data to read.</param>
        /// <returns>The <see cref="MsgIn"/> instance itself, useful for fluent patterns.</returns>
        public MsgIn SetPayload(ArraySegment<byte> segment)
        {
            return this.SetPayload(segment.Array, segment.Offset, segment.Count);
        }


        /// <summary>
        /// Sets the payload as a simple string
        /// </summary>
        /// <param name="text">The string to be used as payload</param>
        /// <returns>The <see cref="MsgIn"/> instance itself, useful for fluent patterns.</returns>
        public MsgIn SetPayload(string text)
        {
            return this.SetPayload(text, 0, text.Length);
        }


        /// <summary>
        /// Sets the payload as a segment of a string.
        /// </summary>
        /// <param name="text">The string where to read the data from.</param>
        /// <param name="offset">The index where to start to read from.</param>
        /// <param name="count">The number of chars to read.</param>
        /// <returns>The <see cref="MsgIn"/> instance itself, useful for fluent patterns.</returns>
        public MsgIn SetPayload(string text, int offset, int count)
        {
            char[] array = text.ToCharArray(offset, count);
            this.PayloadInternal = Encoding.UTF8.GetBytes(array);
            return this;
        }

    }
}
