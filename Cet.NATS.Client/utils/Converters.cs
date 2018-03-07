
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

namespace Cet.NATS.Client
{
    internal static class Converters
    {

        #region Fast Unsigned Integer to UTF8 Conversion

        // Adapted from "C++ String Toolkit Library" using bit tricks
        // to avoid the string copy/reverse.
        /*
         *****************************************************************
         *                     String Toolkit Library                    *
         *                                                               *
         * Author: Arash Partow (2002-2017)                              *
         * URL: http://www.partow.net/programming/strtk/index.html       *
         *                                                               *
         * Copyright notice:                                             *
         * Free use of the String Toolkit Library is permitted under the *
         * guidelines and in accordance with the most current version of *
         * the MIT License.                                              *
         * http://www.opensource.org/licenses/MIT                        *
         *                                                               *
         *****************************************************************
        */

        static readonly byte[] rev_3digit_lut = Encoding.UTF8.GetBytes(
                "000001002003004005006007008009010011012013014015016017018019020021022023024"
            + "025026027028029030031032033034035036037038039040041042043044045046047048049"
            + "050051052053054055056057058059060061062063064065066067068069070071072073074"
            + "075076077078079080081082083084085086087088089090091092093094095096097098099"
            + "100101102103104105106107108109110111112113114115116117118119120121122123124"
            + "125126127128129130131132133134135136137138139140141142143144145146147148149"
            + "150151152153154155156157158159160161162163164165166167168169170171172173174"
            + "175176177178179180181182183184185186187188189190191192193194195196197198199"
            + "200201202203204205206207208209210211212213214215216217218219220221222223224"
            + "225226227228229230231232233234235236237238239240241242243244245246247248249"
            + "250251252253254255256257258259260261262263264265266267268269270271272273274"
            + "275276277278279280281282283284285286287288289290291292293294295296297298299"
            + "300301302303304305306307308309310311312313314315316317318319320321322323324"
            + "325326327328329330331332333334335336337338339340341342343344345346347348349"
            + "350351352353354355356357358359360361362363364365366367368369370371372373374"
            + "375376377378379380381382383384385386387388389390391392393394395396397398399"
            + "400401402403404405406407408409410411412413414415416417418419420421422423424"
            + "425426427428429430431432433434435436437438439440441442443444445446447448449"
            + "450451452453454455456457458459460461462463464465466467468469470471472473474"
            + "475476477478479480481482483484485486487488489490491492493494495496497498499"
            + "500501502503504505506507508509510511512513514515516517518519520521522523524"
            + "525526527528529530531532533534535536537538539540541542543544545546547548549"
            + "550551552553554555556557558559560561562563564565566567568569570571572573574"
            + "575576577578579580581582583584585586587588589590591592593594595596597598599"
            + "600601602603604605606607608609610611612613614615616617618619620621622623624"
            + "625626627628629630631632633634635636637638639640641642643644645646647648649"
            + "650651652653654655656657658659660661662663664665666667668669670671672673674"
            + "675676677678679680681682683684685686687688689690691692693694695696697698699"
            + "700701702703704705706707708709710711712713714715716717718719720721722723724"
            + "725726727728729730731732733734735736737738739740741742743744745746747748749"
            + "750751752753754755756757758759760761762763764765766767768769770771772773774"
            + "775776777778779780781782783784785786787788789790791792793794795796797798799"
            + "800801802803804805806807808809810811812813814815816817818819820821822823824"
            + "825826827828829830831832833834835836837838839840841842843844845846847848849"
            + "850851852853854855856857858859860861862863864865866867868869870871872873874"
            + "875876877878879880881882883884885886887888889890891892893894895896897898899"
            + "900901902903904905906907908909910911912913914915916917918919920921922923924"
            + "925926927928929930931932933934935936937938939940941942943944945946947948949"
            + "950951952953954955956957958959960961962963964965966967968969970971972973974"
            + "975976977978979980981982983984985986987988989990991992993994995996997998999"
            + "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"
        );


        static readonly byte[] rev_2digit_lut = Encoding.UTF8.GetBytes(
                "0001020304050607080910111213141516171819"
            + "2021222324252627282930313233343536373839"
            + "4041424344454647484950515253545556575859"
            + "6061626364656667686970717273747576777879"
            + "8081828384858687888990919293949596979899"
            + "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"
        );


        // Log(n) adapted from:
        // http://graphics.stanford.edu/~seander/bithacks.html#IntegerLog10
        static readonly int[] MultiplyDeBruijnBitPosition = new[]
        {
            0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
            8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
        };


        static readonly int[] PowersOf10 = new[]
        {
            1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000
        };


        // Fast conversion from an unsigned Int32 to a UTF8 string.
        // Assumes: non-negative.
        internal static int WriteInt32ToBuffer(byte[] buffer, int offset, int value)
        {
            const int radix = 10;
            const int radix_sqr = radix * radix;
            const int radix_cube = radix * radix * radix;

            if (value >= 10)
            {
                // calculate Log10 to get the digit count (via Log2 and bit tricks)
                int v = value;
                v |= v >> 1;
                v |= v >> 2;
                v |= v >> 4;
                v |= v >> 8;
                v |= v >> 16;
                int lg2 = MultiplyDeBruijnBitPosition[(uint)(v * 0x07C4ACDD) >> 27];
                int t = (lg2 + 1) * 1233 >> 12;
                int lg10 = t - (value < PowersOf10[t] ? 1 : 0) + 1;

                // once we have the count of digits, start from the end of the output
                // number in the buffer
                int itr = offset + lg10;

                while (value >= radix_sqr)
                {
                    itr -= 3;
                    int temp_v = value / radix_cube;
                    int temp_off = 3 * (value - (temp_v * radix_cube));
                    buffer[itr] = rev_3digit_lut[temp_off];
                    buffer[itr + 1] = rev_3digit_lut[temp_off + 1];
                    buffer[itr + 2] = rev_3digit_lut[temp_off + 2];
                    value = temp_v;
                }

                while (value >= radix)
                {
                    itr -= 2;
                    int temp_v = value / radix_sqr;
                    int temp_off = 2 * (value - (temp_v * radix_sqr));
                    buffer[itr] = rev_2digit_lut[temp_off];
                    buffer[itr + 1] = rev_2digit_lut[temp_off + 1];
                    value = temp_v;
                }

                if (value != 0)
                {
                    buffer[--itr] = (byte)('0' + value);
                }

                return offset + lg10;
            }
            else
            {
                buffer[offset++] = (byte)('0' + value);
            }

            return offset;
        }

        #endregion


        /// <summary>
        /// Fast conversion of a char-array (string) to an Int64 number
        /// </summary>
        /// <param name="value">The source char-array.</param>
        /// <param name="offset">The offset where to start consuming characters from.</param>
        /// <param name="count">The number of characters to consume. 
        /// A count of zero means to consume chars up to the end of the array.</param>
        /// <returns>The resulting number.</returns>
        internal static long StringToInt64(
            char[] value,
            int offset = 0,
            int count = 0
            )
        {
            const char Zero = '0';

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int limit = count > 0
                ? offset + count
                : value.Length;

            if (offset == limit)
            {
                throw new ArgumentException("Cannot convert a zero-length string.");
            }

            long result = value[offset] - Zero;
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            result = (result * 10) + (value[offset] - Zero);
            if (++offset == limit) return result;

            return result;
        }


        /// <summary>
        /// Fast conversion of an Int64 non-negative number to a char-array or a string
        /// </summary>
        /// <param name="value">The source number, which has to be non-negative</param>
        /// <param name="array">An optional char-array reference as target of the conversion</param>
        /// <param name="offset">The offset position of the array</param>
        /// <returns>A string as result of the conversion when the array reference was not specified. Null otherwise.</returns>
        internal static string Int64ToString(
            long value,
            char[] array = null,
            int offset = 0
            )
        {
            const int Zero = '0';
            const int Size = 24;

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            var a = new char[Size];
            int index = Size;
            long rem;
            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            value = Math.DivRem(value, 10, out rem);
            a[--index] = (char)(rem + Zero);
            if (value == 0) goto label_exit;

            label_exit:
            if (array == null)
            {
                return new string(a, index, Size - index);
            }
            else
            {
                Array.Copy(a, index, array, offset, Size - index);
                return null;
            }
        }

    }
}
