// <copyright file="Bitops.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    /// <summary>
    /// Collection of helper functions for bit twiddling operations.
    /// </summary>
    public class Bitops
    {
        /// <summary>
        /// A table used for an efficient implementation of BitScanForward.
        /// </summary>
        private static readonly int[] BitScanForwardTable =
        {
            0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
            31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9,
        };

        /// <summary>
        /// Find the index of the first bit set to true.
        /// </summary>
        /// <param name="b">The input integer.</param>
        /// <returns>The first position.</returns>
        public static int BitScanForward(uint b)
        {
            uint idx = unchecked((uint)(b & -b) * 0x077CB531U) >> 27;
            return BitScanForwardTable[idx];
        }

        /// <summary>
        /// Get the next highest power of two.
        /// </summary>
        /// <param name="v">A number.</param>
        /// <returns>The next highest number that is a power of two.</returns>
        public static int NextPowerOfTwo(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        /// <summary>
        /// Get the bit mask for a power of two number by filling in
        /// the least-significant bits with 1s.
        /// </summary>
        /// <param name="v">A power of two number.</param>
        /// <returns>The bit mask.</returns>
        public static int BitmaskForPowerOfTwo(int v)
        {
            var firstIndex = BitScanForward((uint)v);
            if (firstIndex == 0)
            {
                return 0;
            }

            return (int)(0xFFFFFFFF >> (32 - firstIndex));
        }
    }
}
