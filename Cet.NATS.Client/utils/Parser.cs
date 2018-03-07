
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
    /// Incoming data protocol parser
    /// </summary>
    /// <remarks>
    /// Due to performance reasons, the access to this instance must be guaranteed 
    /// to a single thread at once
    /// </remarks>
    internal sealed class Parser
    {
        // For performance declare these as consts - they'll be
        // baked into the IL code (thus faster).  An enum would
        // be nice, but we want speed in this critical section of
        // message handling.
        internal const int OP_START = 0;
        internal const int OP_PLUS = 1;
        internal const int OP_PLUS_O = 2;
        internal const int OP_PLUS_OK = 3;
        internal const int OP_MINUS = 4;
        internal const int OP_MINUS_E = 5;
        internal const int OP_MINUS_ER = 6;
        internal const int OP_MINUS_ERR = 7;
        internal const int OP_MINUS_ERR_SPC = 8;
        internal const int MINUS_ERR_ARG = 9;
        internal const int OP_C = 10;
        internal const int OP_CO = 11;
        internal const int OP_CON = 12;
        internal const int OP_CONN = 13;
        internal const int OP_CONNE = 14;
        internal const int OP_CONNEC = 15;
        internal const int OP_CONNECT = 16;
        internal const int CONNECT_ARG = 17;
        internal const int OP_M = 18;
        internal const int OP_MS = 19;
        internal const int OP_MSG = 20;
        internal const int OP_MSG_SPC = 21;
        internal const int MSG_ARG = 22;
        internal const int MSG_PAYLOAD = 23;
        internal const int MSG_END = 24;
        internal const int OP_P = 25;
        internal const int OP_PI = 26;
        internal const int OP_PIN = 27;
        internal const int OP_PING = 28;
        internal const int OP_PO = 29;
        internal const int OP_PON = 30;
        internal const int OP_PONG = 31;
        internal const int OP_I = 32;
        internal const int OP_IN = 33;
        internal const int OP_INF = 34;
        internal const int OP_INFO = 35;
        internal const int OP_INFO_SPC = 36;
        internal const int INFO_ARG = 37;

        //local buffer to accumulate the incoming data
        private byte[] _xbuffer = new byte[Defaults.defaultBufSize];
        private int _xbuflen;

        //outgoing message
        private MsgOut _message;

        private int _state = 0;
        internal int State => this._state;

        //callback to notify a special operation
        internal Action<ArraySegment<byte>, int> NotifyOperation;

        //callback to notify a ready-to-deliver message
        internal Action<MsgOut> NotifyMessage;

        //callback to notify some error
        internal Action<ArraySegment<byte>> NotifyError;


        /// <summary>
        /// Entry-point to feed the parser with data incoming from the NATS server
        /// </summary>
        /// <param name="segment"></param>
        internal void Parse(
            ArraySegment<byte> segment
            )
        {
            byte[] buffer = segment.Array;
            int bstart = 0;
            int bcount = 0;
#if DEBUG
            //Print(buffer, 0, segment.Count);
#endif
            for (int i = 0, len = segment.Count; i < len; i++)
            {
                char b = (char)buffer[i];

                switch (_state)
                {
                    case OP_START:
                        switch (b)
                        {
                            case 'M':
                            case 'm':
                                _state = OP_M;
                                break;
                            case 'C':
                            case 'c':
                                _state = OP_C;
                                break;
                            case 'P':
                            case 'p':
                                _state = OP_P;
                                break;
                            case '+':
                                _state = OP_PLUS;
                                break;
                            case '-':
                                _state = OP_MINUS;
                                break;
                            case 'i':
                            case 'I':
                                _state = OP_I;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_M:
                        switch (b)
                        {
                            case 'S':
                            case 's':
                                _state = OP_MS;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_MS:
                        switch (b)
                        {
                            case 'G':
                            case 'g':
                                _state = OP_MSG;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_MSG:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                _state = OP_MSG_SPC;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_MSG_SPC:
                        switch (b)
                        {
                            case ' ':
                                break;
                            case '\t':
                                break;
                            default:
                                this._xbuflen = 0;
                                bcount = 0;
                                bstart = i;
                                _state = MSG_ARG;
                                i--;
                                break;
                        }
                        break;
                    case MSG_ARG:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                if (bcount != 0)
                                {
                                    AddSegment();
                                    bcount = 0;
                                }
                                this.ParseHeader();
                                this._xbuflen = 0;
                                _state = MSG_PAYLOAD;
                                break;
                            default:
                                bcount++;
                                break;
                        }
                        break;
                    case MSG_PAYLOAD:
                        int msgSize = this._message.PayloadLength;
                        if (msgSize == 0)
                        {
                            this.NotifyMessage(this._message);
                            this._message = null;
                            _state = MSG_END;
                        }
                        else
                        {
                            bcount = len - i;
                            if ((this._xbuflen + bcount) > msgSize)
                            {
                                bcount = msgSize - this._xbuflen;
                            }
                            if (bcount != 0)
                            {
                                bstart = i;
                                AddSegment();

                                i += bcount - 1;
                                bcount = 0;
                            }
                            if (this._xbuflen >= msgSize)
                            {
                                this._message.SetPayload(this._xbuffer);
                                this.NotifyMessage(this._message);
                                this._message = null;

                                this._xbuflen = 0;
                                _state = MSG_END;
                            }
                        }
                        break;
                    case MSG_END:
                        switch (b)
                        {
                            case '\n':
                                _state = OP_START;
                                break;
                            default:
                                continue;
                        }
                        break;
                    case OP_PLUS:
                        switch (b)
                        {
                            case 'O':
                            case 'o':
                                _state = OP_PLUS_O;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_PLUS_O:
                        switch (b)
                        {
                            case 'K':
                            case 'k':
                                _state = OP_PLUS_OK;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_PLUS_OK:
                        switch (b)
                        {
                            case '\n':
                                this.NotifyOperation(
                                    default(ArraySegment<byte>),
                                    this._state
                                    );

                                _state = OP_START;
                                break;
                        }
                        break;
                    case OP_MINUS:
                        switch (b)
                        {
                            case 'E':
                            case 'e':
                                _state = OP_MINUS_E;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_MINUS_E:
                        switch (b)
                        {
                            case 'R':
                            case 'r':
                                _state = OP_MINUS_ER;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_MINUS_ER:
                        switch (b)
                        {
                            case 'R':
                            case 'r':
                                _state = OP_MINUS_ERR;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_MINUS_ERR:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                _state = OP_MINUS_ERR_SPC;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_MINUS_ERR_SPC:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                _state = OP_MINUS_ERR_SPC;
                                break;
                            default:
                                this._xbuflen = 0;
                                bcount = 0;
                                bstart = i;
                                _state = MINUS_ERR_ARG;
                                i--;
                                break;
                        }
                        break;
                    case MINUS_ERR_ARG:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                if (bcount != 0)
                                {
                                    AddSegment();
                                    bcount = 0;
                                }
                                this.NotifyError(
                                    new ArraySegment<byte>(this._xbuffer, 0, this._xbuflen)
                                    );

                                this._xbuflen = 0;
                                _state = OP_START;
                                break;
                            default:
                                bcount++;
                                break;
                        }
                        break;
                    case OP_P:
                        switch (b)
                        {
                            case 'I':
                            case 'i':
                                _state = OP_PI;
                                break;
                            case 'O':
                            case 'o':
                                _state = OP_PO;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_PO:
                        switch (b)
                        {
                            case 'N':
                            case 'n':
                                _state = OP_PON;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_PON:
                        switch (b)
                        {
                            case 'G':
                            case 'g':
                                _state = OP_PONG;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_PONG:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                this.NotifyOperation(
                                    default(ArraySegment<byte>),
                                    this._state
                                    );

                                _state = OP_START;
                                break;
                        }
                        break;
                    case OP_PI:
                        switch (b)
                        {
                            case 'N':
                            case 'n':
                                _state = OP_PIN;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_PIN:
                        switch (b)
                        {
                            case 'G':
                            case 'g':
                                _state = OP_PING;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_PING:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                this.NotifyOperation(
                                    default(ArraySegment<byte>),
                                    this._state
                                    );

                                _state = OP_START;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_I:
                        switch (b)
                        {
                            case 'N':
                            case 'n':
                                _state = OP_IN;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_IN:
                        switch (b)
                        {
                            case 'F':
                            case 'f':
                                _state = OP_INF;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_INF:
                        switch (b)
                        {
                            case 'O':
                            case 'o':
                                _state = OP_INFO;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_INFO:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                _state = OP_INFO_SPC;
                                break;
                            default:
                                this.ParseError(buffer, i, len);
                                break;
                        }
                        break;
                    case OP_INFO_SPC:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                break;
                            default:
                                this._xbuflen = 0;
                                bcount = 0;
                                bstart = i;
                                this._state = INFO_ARG;
                                i--;
                                break;
                        }
                        break;
                    case INFO_ARG:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                if (bcount != 0)
                                {
                                    AddSegment();
                                    bcount = 0;
                                }
                                this.NotifyOperation(
                                    new ArraySegment<byte>(this._xbuffer, 0, this._xbuflen),
                                    this._state
                                    );

                                this._xbuflen = 0;
                                this._state = OP_START;
                                break;
                            default:
                                bcount++;
                                break;
                        }
                        break;
                    default:
                        throw new NATSException("Unable to parse.");
                } // switch(state)
            }  // for

            if (bcount != 0)
            {
                AddSegment();
            }

            //inner data accumulator
            //appends a block of fresh bytes to the local cache
            void AddSegment()
            {
                int newlen = this._xbuflen + bcount;
                if (this._xbuffer.Length < newlen)
                {
                    Array.Resize(
                        ref this._xbuffer,
                        this._xbuffer.Length + Defaults.defaultBufSize
                        );
                }

                VectorizedCopyExtension.VectorizedCopy(
                    buffer,
                    bstart,
                    this._xbuffer,
                    this._xbuflen,
                    bcount
                    );

                this._xbuflen = newlen;
            }
        }


        /// <summary>
        /// Parser for a error message issued by the NATS server
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        private void ParseError(byte[] buffer, int position, int length)
        {
            const int N = 16;

            //segment before
            var before_txt = string.Empty;
            var before_hex = string.Empty;
            if (position > 0)
            {
                int offset = Math.Max(0, position - N);
                before_txt = Encoding.UTF8.GetString(buffer, offset, position - offset);
                before_hex = BitConverter.ToString(buffer, offset, position - offset);
            }

            //segment after
            var after_txt = string.Empty;
            var after_hex = string.Empty;
            {
                int count = Math.Max(length - position, N);
                after_txt = Encoding.UTF8.GetString(buffer, position, count);
                after_hex = BitConverter.ToString(buffer, position, count);
            }

            //TODO refine with various exception kinds to better give the context
            throw new NATSException(
                $"Parse Error: state={_state}; before={before_txt}; after={after_txt}; before={before_hex}; after={after_hex}"
                );
        }


        #region Message header parser

        private const int CharBufferBlockSize = 1024;
        private char[] _cbuffer = new char[CharBufferBlockSize];

        private const int MaxMessageArgs = 4;
        private int[] _sepPos = new int[MaxMessageArgs - 1];


        /// <summary>
        /// Parser for a message header
        /// </summary>
        /// <remarks>
        /// see: https://nats.io/documentation/internals/nats-protocol/
        /// </remarks>
        private void ParseHeader()
        {
            //initial byte-array to UTF-8 char-array conversion
            int charCount = Encoding.UTF8.GetChars(this._xbuffer, 0, this._xbuflen, this._cbuffer, 0);

            //the idea is to avoid splitting a string into other strings,
            //so simply mark the useful points of a character array
            int numseps = 0;
            for (int i = 0; i < charCount; i++)
            {
                if (this._cbuffer[i] == ' ')
                {
                    this._sepPos[numseps++] = i;
                }
            }

            string replyTo = null;
            if (numseps == 3)
            {
                //read the 'reply' argument
                replyTo = new string(
                    this._cbuffer,
                    this._sepPos[1] + 1,
                    this._sepPos[2] - this._sepPos[1] - 1
                    );
            }

            this._message = new MsgOut(
                subject: new string(this._cbuffer, 0, this._sepPos[0]),
                replyTo: replyTo,
                subId: Converters.StringToInt64(this._cbuffer, this._sepPos[0] + 1, this._sepPos[1] - this._sepPos[0] - 1)
                )
            {
                PayloadLength = (int)Converters.StringToInt64(this._cbuffer, this._sepPos[numseps - 1] + 1, this._xbuflen - this._sepPos[numseps - 1] - 1),
            };
        }

        #endregion

#if DEBUG
        //just a dubug utility
        public static void Print(byte[] buffer, int position, int count)
        {
            string s = Encoding.UTF8.GetString(buffer, position, count);
            Console.WriteLine(s);
        }
#endif

    }
}