
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
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client
{
    /// <summary>
    /// <see cref="IPassiveSubscription"/> provides messages for a subject through
    /// an external host awaiting (sync or async) for them.
    /// </summary>
    public interface IPassiveSubscription 
        : ISubscription, IDisposable
    {
        /// <summary>
        /// Returns the next <see cref="MsgOut"/> available to a subscriber, 
        /// or blocks up to a given timeout until the next one is available.
        /// </summary>
        /// <param name="timeout">The amount of time, in milliseconds, to wait for
        /// the next message.</param>
        /// <returns>The next <see cref="MsgOut"/> available to a subscriber.</returns>
        MsgOut NextMessage(TimeSpan timeout);


        /// <summary>
        /// Returns the next <see cref="MsgOut"/> available to a subscriber
        /// </summary>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>The next <see cref="MsgOut"/> available to a subscriber.</returns>
        MsgOut NextMessage(CancellationToken token);


        /// <summary>
        /// Returns the next <see cref="MsgOut"/> available to a subscriber,
        /// or block up to a given timeout until the next one is available.
        /// </summary>
        /// <param name="timeout">The amount of time, in milliseconds, to wait for
        /// the next message.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>The next <see cref="MsgOut"/> available to a subscriber.</returns>
        MsgOut NextMessage(TimeSpan timeout, CancellationToken token);


        /// <summary>
        /// Returns a task to await the next <see cref="MsgOut"/> available to a subscriber
        /// or block up to a given timeout until the next one is available.
        /// </summary>
        /// <param name="timeout">The amount of time, in milliseconds, to wait for
        /// the next message.</param>
        /// <returns>A task to await the next <see cref="MsgOut"/> available to a subscriber.</returns>
        Task<MsgOut> NextMessageAsync(TimeSpan timeout);


        /// <summary>
        /// Returns a task to await the next <see cref="MsgOut"/> available to a subscriber
        /// </summary>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A task to await the next <see cref="MsgOut"/> available to a subscriber.</returns>
        Task<MsgOut> NextMessageAsync(CancellationToken token);


        /// <summary>
        /// Returns a task to await the next <see cref="MsgOut"/> available to a subscriber
        /// or block up to a given timeout until the next one is available.
        /// </summary>
        /// <param name="timeout">The amount of time, in milliseconds, to wait for
        /// the next message.</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>A task to await the next <see cref="MsgOut"/> available to a subscriber.</returns>
        Task<MsgOut> NextMessageAsync(TimeSpan timeout, CancellationToken token);
    }
}
