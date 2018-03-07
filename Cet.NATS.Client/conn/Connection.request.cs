
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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client
{
    partial class Connection
    {

        /// <summary>
        /// Sends a request payload and returns the response <see cref="MsgOut"/>, or throws
        /// <see cref="NATSTimeoutException"/> if the <paramref name="timeout"/> expires.
        /// </summary>
        /// <remarks>
        /// <para>NATS supports two flavors of request-reply messaging: point-to-point or one-to-many. Point-to-point
        /// involves the fastest or first to respond. In a one-to-many exchange, you set a limit on the number of 
        /// responses the requestor may receive and instead must use a subscription (<see cref="ISubscription.AutoUnsubscribe(int)"/>).
        /// In a request-response exchange, publish request operation publishes a message with a reply subject expecting
        /// a response on that reply subject.</para>
        /// <para><see cref="Request(MsgIn, CancellationToken)"/> will create an unique inbox for this request, sharing a single
        /// subscription for all replies to this <see cref="Connection"/> instance. However, if 
        /// <see cref="ClientOptions.UseOldRequestStyle"/> is set, each request will have its own underlying subscription. 
        /// The old behavior is not recommended as it may cause unnecessary overhead on connected NATS servers.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A <see cref="MsgOut"/> with the response from the NATS server.</returns>
        public MsgOut Request(MsgIn message, CancellationToken token)
        {
            return this.RequestCore(message, Timeout.InfiniteTimeSpan, token);
        }


        /// <summary>
        /// Sends a request payload and returns the response <see cref="MsgOut"/>, or throws
        /// <see cref="NATSTimeoutException"/> if the <paramref name="timeout"/> expires.
        /// </summary>
        /// <remarks>
        /// <para>NATS supports two flavors of request-reply messaging: point-to-point or one-to-many. Point-to-point
        /// involves the fastest or first to respond. In a one-to-many exchange, you set a limit on the number of 
        /// responses the requestor may receive and instead must use a subscription (<see cref="ISubscription.AutoUnsubscribe(int)"/>).
        /// In a request-response exchange, publish request operation publishes a message with a reply subject expecting
        /// a response on that reply subject.</para>
        /// <para><see cref="Request(MsgIn, TimeSpan, CancellationToken)"/> will create an unique inbox for this request, sharing a single
        /// subscription for all replies to this <see cref="Connection"/> instance. However, if 
        /// <see cref="ClientOptions.UseOldRequestStyle"/> is set, each request will have its own underlying subscription. 
        /// The old behavior is not recommended as it may cause unnecessary overhead on connected NATS servers.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="timeout">Represents the overall time to wait for the connection going stable, if not, and for the message loopback.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A <see cref="MsgOut"/> with the response from the NATS server.</returns>
        public MsgOut Request(MsgIn message, TimeSpan timeout, CancellationToken token)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
            }

            return this.RequestCore(message, timeout, token);
        }


        /// <summary>
        /// Common entry-point for all the "request" public synchronous methods
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private MsgOut RequestCore(
            MsgIn message,
            TimeSpan timeout,
            CancellationToken token
            )
        {

            InFlightRequest request = null;
            try
            {
                request = this._rrmgr.SetupRequest(message);
                this.PublishCore(message, timeout, token);
            }
            catch (Exception ex)
            {
                if (request != null)
                {
                    this._rrmgr.DropRequest(request);
                    request.Dispose();
                }
                throw ex;
            }

            return request.GetReply(timeout, token);
        }


        /// <summary>
        /// Sends a request payload and returns the response <see cref="MsgOut"/>, or throws
        /// <see cref="NATSTimeoutException"/> if the <paramref name="timeout"/> expires.
        /// </summary>
        /// <remarks>
        /// <para>NATS supports two flavors of request-reply messaging: point-to-point or one-to-many. Point-to-point
        /// involves the fastest or first to respond. In a one-to-many exchange, you set a limit on the number of 
        /// responses the requestor may receive and instead must use a subscription (<see cref="ISubscription.AutoUnsubscribe(int)"/>).
        /// In a request-response exchange, publish request operation publishes a message with a reply subject expecting
        /// a response on that reply subject.</para>
        /// <para><see cref="RequestAsync(MsgIn, CancellationToken)"/> will create an unique inbox for this request, sharing a single
        /// subscription for all replies to this <see cref="Connection"/> instance. However, if 
        /// <see cref="ClientOptions.UseOldRequestStyle"/> is set, each request will have its own underlying subscription. 
        /// The old behavior is not recommended as it may cause unnecessary overhead on connected NATS servers.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the <see cref="Task{TResult}.Result"/>
        /// parameter contains a <see cref="MsgOut"/> with the response from the NATS server.</returns>
        public Task<MsgOut> RequestAsync(MsgIn message, CancellationToken token)
        {
            return this.RequestAsync(message, Timeout.InfiniteTimeSpan, token);
        }


        /// <summary>
        /// Sends a request payload and returns the response <see cref="MsgOut"/>, or throws
        /// <see cref="NATSTimeoutException"/> if the <paramref name="timeout"/> expires.
        /// </summary>
        /// <remarks>
        /// <para>NATS supports two flavors of request-reply messaging: point-to-point or one-to-many. Point-to-point
        /// involves the fastest or first to respond. In a one-to-many exchange, you set a limit on the number of 
        /// responses the requestor may receive and instead must use a subscription (<see cref="ISubscription.AutoUnsubscribe(int)"/>).
        /// In a request-response exchange, publish request operation publishes a message with a reply subject expecting
        /// a response on that reply subject.</para>
        /// <para><see cref="RequestAsync(MsgIn, TimeSpan, CancellationToken)"/> will create an unique inbox for this request, sharing a single
        /// subscription for all replies to this <see cref="Connection"/> instance. However, if 
        /// <see cref="ClientOptions.UseOldRequestStyle"/> is set, each request will have its own underlying subscription. 
        /// The old behavior is not recommended as it may cause unnecessary overhead on connected NATS servers.</para>
        /// </remarks>
        /// <param name="message">A <see cref="MsgIn"/> instance containing the subject, optional reply, and data to publish
        /// to the NATS server.</param>
        /// <param name="timeout">Represents the overall time to wait for the connection going stable, if not, and for the message loopback.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the <see cref="Task{TResult}.Result"/>
        /// parameter contains a <see cref="MsgOut"/> with the response from the NATS server.</returns>
        public Task<MsgOut> RequestAsync(MsgIn message, TimeSpan timeout, CancellationToken token)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
            }

            return this.RequestCoreAsync(message, timeout, token);
        }


        /// <summary>
        /// Common entry-point for all the "request" public asynchronous methods
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<MsgOut> RequestCoreAsync(
            MsgIn message,
            TimeSpan timeout,
            CancellationToken token
            )
        {
            InFlightRequest request = null;
            try
            {
                request = this._rrmgr.SetupRequest(message);
                this.PublishCore(message, timeout, token);
            }
            catch (Exception ex)
            {
                if (request != null)
                {
                    this._rrmgr.DropRequest(request);
                    request.Dispose();
                }
                throw ex;
            }

            return await request.GetReplyAsync(timeout, token);
        }

    }
}
