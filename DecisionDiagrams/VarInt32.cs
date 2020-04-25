// <copyright file="VarInt32.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;

    /// <summary>
    /// 32-bit integer variable type.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public class VarInt32<T> : Variable<T>
        where T : IDDNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VarInt32{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="indices">The variable indices.</param>
        /// <param name="bitOrder">The variable order.</param>
        internal VarInt32(DDManager<T> manager, int[] indices, Func<int, int> bitOrder)
            : base(manager, indices, VariableType.INT32, bitOrder)
        {
        }

        /// <summary>
        /// DD representing a u32 value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="length">The number of bits to encode.</param>
        /// <returns>The value as a function.</returns>
        public DD Eq(int value, int length = 32)
        {
            return this.Eq(value, 32, length);
        }

        /// <summary>
        /// Less than or equal to constraint.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The inequality.</returns>
        public DD LessOrEqual(int value)
        {
            return this.LessOrEqual(value, 32);
        }

        /// <summary>
        /// Less than or equal to constraint.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The inequality.</returns>
        public DD GreaterOrEqual(int value)
        {
            return this.GreaterOrEqual(value, 32);
        }
    }
}
