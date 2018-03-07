
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cet.NATS.Client
{
    internal static class Helpers
    {

        /// <summary>
        /// Simple empty byte array
        /// </summary>
        internal static readonly byte[] EmptyByteArray = new byte[0];


        /// <summary>
        /// Halts the execution for the specified period of time.
        /// The cancellation token allows to break the pause any time.
        /// </summary>
        /// <param name="delay">The amount of time to wait</param>
        /// <param name="token">Propagates notification that operations should be canceled.</param>
        /// <returns>Indicates whether the pause was canceled.</returns>
        internal static bool CancelableDelay(
            TimeSpan delay, 
            CancellationToken token
            )
        {
            if (delay > TimeSpan.Zero)
            {
                using (var sem = new SemaphoreSlim(0))
                {
                    try
                    {
                        sem.Wait(delay, token);
                    }
                    catch (OperationCanceledException)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// Selects the max between teo <see cref="TimeSpan"/> values.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static TimeSpan Max(TimeSpan a, TimeSpan b)
        {
            return a > b ? a : b;
        }

    }
}
