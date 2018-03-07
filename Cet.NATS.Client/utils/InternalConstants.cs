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

namespace Cet.NATS.Client
{
    /**
     * Internal Constants
     */
    internal static class InternalConstants
    {
        internal const string _CRLF_ = "\r\n";
        internal const string _EMPTY_ = "";
        internal const string _SPC_ = " ";
        internal const string _PUB_P_ = "PUB ";

        internal const string _OK_OP_ = "+OK";
        internal const string _ERR_OP_ = "-ERR";
        internal const string _MSG_OP_ = "MSG";
        internal const string _PING_OP_ = "PING";
        internal const string _PONG_OP_ = "PONG";
        internal const string _INFO_OP_ = "INFO";

        internal const string inboxPrefix = "_INBOX.";

        internal const string conProto = "CONNECT {0}" + InternalConstants._CRLF_;
        internal const string pingProto = "PING" + InternalConstants._CRLF_;
        internal const string pongProto = "PONG" + InternalConstants._CRLF_;
        internal const string pubProto = "PUB {0} {1} {2}" + InternalConstants._CRLF_;
        internal const string subProto = "SUB {0} {1} {2}" + InternalConstants._CRLF_;
        internal const string unsubProto = "UNSUB {0} {1}" + InternalConstants._CRLF_;

        internal const string pongProtoNoCRLF = "PONG";
        internal const string okProtoNoCRLF = "+OK";

        internal const string STALE_CONNECTION = "stale connection";
        internal const string AUTH_TIMEOUT = "authorization timeout";
    }
}
