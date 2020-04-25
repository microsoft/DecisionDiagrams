// <copyright file="VarInt16.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;

    /// <summary>
    /// 16-bit integer variable type.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public class VarInt16<T> : Variable<T>
        where T : IDDNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VarInt16{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="indices">The variable indices.</param>
        /// <param name="bitOrder">The variable order.</param>
        internal VarInt16(DDManager<T> manager, int[] indices, Func<int, int> bitOrder)
            : base(manager, indices, VariableType.INT16, bitOrder)
        {
        }

        /// <summary>
        /// DD representing a u16 value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="length">The number of bits to encode.</param>
        /// <returns>The value as a function.</returns>
        public DD Eq(short value, int length = 16)
        {
            return this.Eq(value, 16, length);
        }

        /// <summary>
        /// Less than or equal to constraint.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The inequality.</returns>
        public DD LessOrEqual(short value)
        {
            return this.LessOrEqual(value, 16);
        }

        /// <summary>
        /// Greater than or equal to constraint.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The inequality.</returns>
        public DD GreaterOrEqual(short value)
        {
            return this.GreaterOrEqual(value, 16);
        }
    }
}
