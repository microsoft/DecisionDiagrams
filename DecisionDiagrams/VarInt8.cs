// <copyright file="VarInt8.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;

    /// <summary>
    /// 8-bit integer variable type.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public class VarInt8<T> : Variable<T>
        where T : IDDNode, IEquatable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VarInt8{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="indices">The variable indices.</param>
        /// <param name="bitOrder">The variable order.</param>
        internal VarInt8(DDManager<T> manager, int[] indices, Func<int, int> bitOrder)
            : base(manager, indices, VariableType.INT8, bitOrder)
        {
        }

        /// <summary>
        /// DD representing a u8 value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="length">The number of bits to encode.</param>
        /// <returns>The value as a function.</returns>
        public DD Eq(byte value, int length = 8)
        {
            return this.Eq(value, 8, length);
        }

        /// <summary>
        /// Less than or equal to constraint.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The inequality.</returns>
        public DD LessOrEqual(byte value)
        {
            return this.LessOrEqual(value, 8);
        }

        /// <summary>
        /// Greater than or equal to constraint.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The inequality.</returns>
        public DD GreaterOrEqual(byte value)
        {
            return this.GreaterOrEqual(value, 8);
        }
    }
}
