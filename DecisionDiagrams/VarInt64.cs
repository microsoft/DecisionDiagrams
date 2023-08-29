// <copyright file="VarInt64.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;

    /// <summary>
    /// 32-bit integer variable type.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public class VarInt64<T> : Variable<T>
        where T : IDDNode, IEquatable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VarInt64{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="indices">The variable indices.</param>
        /// <param name="bitOrder">The variable order.</param>
        internal VarInt64(DDManager<T> manager, int[] indices, Func<int, int> bitOrder)
            : base(manager, indices, VariableType.INT64, bitOrder)
        {
        }

        /// <summary>
        /// DD representing a u64 value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="length">The number of bits to encode.</param>
        /// <returns>The value as a function.</returns>
        public DD Eq(long value, int length = 64)
        {
            return this.Eq(value, 64, length);
        }

        /// <summary>
        /// Less than or equal to constraint.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The inequality.</returns>
        public DD LessOrEqual(long value)
        {
            return this.LessOrEqual(value, 64);
        }

        /// <summary>
        /// Less than or equal to constraint.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The inequality.</returns>
        public DD GreaterOrEqual(long value)
        {
            return this.GreaterOrEqual(value, 64);
        }
    }
}
