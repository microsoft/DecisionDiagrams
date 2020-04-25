// <copyright file="VarInt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;

    /// <summary>
    /// Variable sized integer variable type.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public class VarInt<T> : Variable<T>
        where T : IDDNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VarInt{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="indices">The variable indices.</param>
        /// <param name="bitOrder">The variable order.</param>
        internal VarInt(DDManager<T> manager, int[] indices, Func<int, int> bitOrder)
            : base(manager, indices, VariableType.INT, bitOrder)
        {
        }

        /// <summary>
        /// 32-bit integer value.
        /// </summary>
        /// <param name="value">The 32-bit value.</param>
        /// <param name="length">The number of bits to encode.</param>
        /// <returns>Function capturing the value.</returns>
        public new DD Eq(byte[] value, int length = -1)
        {
            return base.Eq(value, length);
        }
    }
}
