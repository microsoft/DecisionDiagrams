// <copyright file="Assignment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A satisfying assignment for decision diagram variables.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public class Assignment<T>
        where T : IDDNode
    {
        /// <summary>
        /// Gets the boolean assignments.
        /// </summary>
        internal Dictionary<VarBool<T>, bool> BoolAssignment { get; } = new Dictionary<VarBool<T>, bool>();

        /// <summary>
        /// Gets the int8 assignments.
        /// </summary>
        internal Dictionary<VarInt8<T>, byte> Int8Assignment { get; } = new Dictionary<VarInt8<T>, byte>();

        /// <summary>
        /// Gets the int16 assignments.
        /// </summary>
        internal Dictionary<VarInt16<T>, short> Int16Assignment { get; } = new Dictionary<VarInt16<T>, short>();

        /// <summary>
        /// Gets the int32 assignments.
        /// </summary>
        internal Dictionary<VarInt32<T>, int> Int32Assignment { get; } = new Dictionary<VarInt32<T>, int>();

        /// <summary>
        /// Gets the int64 assignments.
        /// </summary>
        internal Dictionary<VarInt64<T>, long> Int64Assignment { get; } = new Dictionary<VarInt64<T>, long>();

        /// <summary>
        /// Gets the int assignments.
        /// </summary>
        internal Dictionary<VarInt<T>, byte[]> IntAssignment { get; } = new Dictionary<VarInt<T>, byte[]>();

        /// <summary>
        /// Get the result for a boolean variable.
        /// </summary>
        /// <param name="var">The variable.</param>
        /// <returns>A boolean value.</returns>
        public bool Get(VarBool<T> var)
        {
            if (this.BoolAssignment == null || !this.BoolAssignment.ContainsKey(var))
            {
                throw new ArgumentException("Invalid boolean variable");
            }

            return this.BoolAssignment[var];
        }

        /// <summary>
        /// Get the result for an int8 variable.
        /// </summary>
        /// <param name="var">The variable.</param>
        /// <returns>A byte value.</returns>
        public byte Get(VarInt8<T> var)
        {
            if (!this.Int8Assignment.ContainsKey(var))
            {
                throw new ArgumentException("Invalid int8 variable");
            }

            return this.Int8Assignment[var];
        }

        /// <summary>
        /// Get the result for an int16 variable.
        /// </summary>
        /// <param name="var">The variable.</param>
        /// <returns>A short value.</returns>
        public short Get(VarInt16<T> var)
        {
            if (!this.Int16Assignment.ContainsKey(var))
            {
                throw new ArgumentException("Invalid int16 variable");
            }

            return this.Int16Assignment[var];
        }

        /// <summary>
        /// Get the result for an int32 variable.
        /// </summary>
        /// <param name="var">The variable.</param>
        /// <returns>An integer value.</returns>
        public int Get(VarInt32<T> var)
        {
            if (!this.Int32Assignment.ContainsKey(var))
            {
                throw new ArgumentException("Invalid int32 variable");
            }

            return this.Int32Assignment[var];
        }

        /// <summary>
        /// Get the result for an int64 variable.
        /// </summary>
        /// <param name="var">The variable.</param>
        /// <returns>An integer value.</returns>
        public long Get(VarInt64<T> var)
        {
            if (!this.Int64Assignment.ContainsKey(var))
            {
                throw new ArgumentException("Invalid int64 variable");
            }

            return this.Int64Assignment[var];
        }

        /// <summary>
        /// Get the result for an int variable. The
        /// resulting byte[] will hold the answer in
        /// big endian order (most significant bit first).
        /// </summary>
        /// <param name="var">The variable.</param>
        /// <returns>An integer value.</returns>
        public byte[] Get(VarInt<T> var)
        {
            if (!this.IntAssignment.ContainsKey(var))
            {
                throw new ArgumentException("Invalid int variable");
            }

            return this.IntAssignment[var];
        }
    }
}
