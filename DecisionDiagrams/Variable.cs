// <copyright file="Variable.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Class to hold a collection of DD variables and
    /// provide convenience operations.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public abstract class Variable<T>
        where T : IDDNode
    {
        /// <summary>
        /// Unique id for variables.
        /// </summary>
        private static int id = 0;

        /// <summary>
        /// Unique id for this variable.
        /// </summary>
        private int uid;

        /// <summary>
        /// Variable order mapping from the range [0, hi] to the
        /// range [0, hi]. Allows for optimizing the representation.
        /// </summary>
        private Func<int, int> order;

        /// <summary>
        /// An array storing the inverse order function.
        /// </summary>
        private int[] reverseOrder;

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="indices">The variable indices.</param>
        /// <param name="type">The variable type.</param>
        /// <param name="order">The variable order.</param>
        internal Variable(DDManager<T> manager, int[] indices, VariableType type, Func<int, int> order)
        {
            this.uid = Interlocked.Increment(ref id);
            this.Manager = manager;
            this.Indices = indices;
            this.ReverseIndices = new Dictionary<int, int>();
            this.Type = type;
            this.order = order;
            this.reverseOrder = new int[indices.Length];

            var mapped = new HashSet<int>();
            for (int i = 0; i < indices.Length; i++)
            {
                this.ReverseIndices[this.Indices[i]] = i;
                var j = this.order(i);
                if (j < 0 || j >= indices.Length)
                {
                    throw new ArgumentException($"Invalid variable order provided.");
                }

                if (mapped.Contains(j))
                {
                    throw new ArgumentException($"Variable order not unique. Value {j} repeated.");
                }

                mapped.Add(j);
                this.reverseOrder[j] = i;
            }
        }

        /// <summary>
        /// The type of the variable, which determines the bit width.
        /// </summary>
        internal enum VariableType
        {
            /// <summary>
            /// Boolean type.
            /// </summary>
            BOOL,

            /// <summary>
            /// Unsigned 8-bit integer type.
            /// </summary>
            INT8,

            /// <summary>
            /// Unsigned 16-bit integer type.
            /// </summary>
            INT16,

            /// <summary>
            /// Unsigned 32-bit integer type.
            /// </summary>
            INT32,

            /// <summary>
            /// Unsigned 64-bit integer type.
            /// </summary>
            INT64,

            /// <summary>
            /// Arbitrary sized integer.
            /// </summary>
            INT,
        }

        /// <summary>
        /// Gets the number of bits contained in the variable.
        /// </summary>
        public int NumBits { get => this.Indices.Length; }

        /// <summary>
        /// Gets the manager.
        /// </summary>
        internal DDManager<T> Manager { get; }

        /// <summary>
        /// Gets the mapping from bit position to variable index.
        /// </summary>
        internal int[] Indices { get; }

        /// <summary>
        /// Gets the mapping from variable index to bit position.
        /// </summary>
        internal Dictionary<int, int> ReverseIndices { get; }

        /// <summary>
        /// Gets the variable type.
        /// </summary>
        internal VariableType Type { get; }

        /// <summary>
        /// Is this a boolean variable.
        /// </summary>
        /// <returns>If the type is BOOL.</returns>
        public bool IsBool()
        {
            return this.Type == VariableType.BOOL;
        }

        /// <summary>
        /// Is this a u8 variable.
        /// </summary>
        /// <returns>If the type is INT8.</returns>
        public bool IsU8()
        {
            return this.Type == VariableType.INT8;
        }

        /// <summary>
        /// Is this a u16 variable.
        /// </summary>
        /// <returns>If the type is INT16.</returns>
        public bool IsU16()
        {
            return this.Type == VariableType.INT16;
        }

        /// <summary>
        /// Is this a u32 variable.
        /// </summary>
        /// <returns>If the type is INT32.</returns>
        public bool IsU32()
        {
            return this.Type == VariableType.INT32;
        }

        /// <summary>
        /// Is this a u64 variable.
        /// </summary>
        /// <returns>If the type is INT64.</returns>
        public bool IsU64()
        {
            return this.Type == VariableType.INT64;
        }

        /// <summary>
        /// Is this a uint variable.
        /// </summary>
        /// <returns>If the type is INT.</returns>
        public bool IsUint()
        {
            return this.Type == VariableType.INT;
        }

        /// <summary>
        /// Equality for Variables.
        /// </summary>
        /// <param name="obj">Another variable.</param>
        /// <returns>Whether the objects are equal.</returns>
        public override bool Equals(object obj)
        {
            return this.uid == ((Variable<T>)obj).uid;
        }

        /// <summary>
        /// Hash code for a variable.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return this.uid;
        }

        /// <summary>
        /// Create a bitvector from a variable.
        /// </summary>
        /// <returns></returns>
        public BitVector<T> ToBitvector()
        {
            return new BitVector<T>(this, this.Manager);
        }

        /// <summary>
        /// DD representing an arbitary sized value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="length">The number of bits to encode.</param>
        /// <returns>The value as a function.</returns>
        public DD Eq(byte[] value, int length = -1)
        {
            var len = this.Indices.Length;
            length = length < 0 ? len : length;

            var x = DDIndex.True;
            for (int v = this.Indices.Length - 1; v >= 0; v--)
            {
                var variable = this.Indices[v];
                var i = this.GetBitPositionForVariableIndex(variable);
                if (i >= length)
                {
                    continue;
                }

                var id = this.Manager.IdIdx(variable);
                var whichByte = i / 8;
                var whichIndex = i % 8;
                var set = GetBit(value[whichByte], whichIndex, 8);
                var y = set ? id : this.Manager.Not(id);
                x = this.Manager.And(x, y);
            }

            return this.Manager.FromIndex(x);
        }

        /// <summary>
        /// Equality of two variables.
        /// </summary>
        /// <param name="other">The other variable.</param>
        /// <returns>DD representing whether the variables are equal.</returns>
        public DD Eq(Variable<T> other)
        {
            if (this.Type != other.Type || this.Indices.Length != other.Indices.Length)
            {
                throw new ArgumentException("DecisionDiagram Eq called on different bit length variables.");
            }

            var x = DDIndex.True;
            for (int i = this.Indices.Length - 1; i >= 0; i--)
            {
                var v1 = this.GetVariableIndexForBitPosition(i);
                var v2 = other.GetVariableIndexForBitPosition(i);
                var id1 = this.Manager.IdIdx(v1);
                var id2 = this.Manager.IdIdx(v2);
                var bothPos = this.Manager.And(id1, id2);
                var bothNeg = this.Manager.And(this.Manager.Not(id1), this.Manager.Not(id2));
                x = this.Manager.And(x, this.Manager.Or(bothPos, bothNeg));
            }

            return this.Manager.FromIndex(x);
        }

        /// <summary>
        /// Return a function capturing a variable being less
        /// than or equal to a particular value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The function representing the inequality.</returns>
        public DD LessOrEqual(byte[] value)
        {
            var len = this.Indices.Length;
            var eq = new DDIndex[len];
            var less = new DDIndex[len];
            for (int v = len - 1; v >= 0; v--)
            {
                var variable = this.Indices[v];
                var i = this.GetBitPositionForVariableIndex(variable);
                var whichByte = i / 8;
                var whichIndex = i % 8;
                var set = GetBit(value[whichByte], whichIndex, 8);
                if (set)
                {
                    var node = this.Manager.IdIdx(variable);
                    eq[i] = node;
                    less[i] = this.Manager.Not(node);
                }
                else
                {
                    var node = this.Manager.IdIdx(variable);
                    eq[i] = this.Manager.Not(node);
                    less[i] = DDIndex.False;
                }
            }

            var acc = DDIndex.True;
            for (int i = len - 1; i >= 0; i--)
            {
                acc = this.Manager.Or(less[i], this.Manager.And(acc, eq[i]));
            }

            return this.Manager.FromIndex(acc);
        }

        /// <summary>
        /// Return a function capturing a variable being greater
        /// than or equal to a particular value. The value is
        /// represented as a byte array and should be have MSBs first.
        /// For example, the 32-bit value 300 would be represented as
        /// the byte array [0, 0, 1, 44].
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The function representing the inequality.</returns>
        public DD GreaterOrEqual(byte[] value)
        {
            var len = this.Indices.Length;
            var eq = new DDIndex[len];
            var greater = new DDIndex[len];
            for (int v = len - 1; v >= 0; v--)
            {
                var variable = this.Indices[v];
                var i = this.GetBitPositionForVariableIndex(variable);
                var whichByte = i / 8;
                var whichIndex = i % 8;
                var set = GetBit(value[whichByte], whichIndex, 8);
                if (set)
                {
                    var node = this.Manager.IdIdx(variable);
                    eq[i] = node;
                    greater[i] = DDIndex.False;
                }
                else
                {
                    var node = this.Manager.IdIdx(variable);
                    eq[i] = this.Manager.Not(node);
                    greater[i] = node;
                }
            }

            var acc = DDIndex.True;
            for (int i = len - 1; i >= 0; i--)
            {
                acc = this.Manager.Or(greater[i], this.Manager.And(acc, eq[i]));
            }

            return this.Manager.FromIndex(acc);
        }

        /// <summary>
        /// Gets a boolean variable representing the ith bit of another variable.
        /// </summary>
        /// <param name="i">The bit position.</param>
        /// <returns>A new boolean variable for the bit.</returns>
        public VarBool<T> GetVariableForIthBit(int i)
        {
            if (i < 0 || i >= this.Indices.Length)
            {
                throw new ArgumentException("Invalid bit position: " + i);
            }

            int index = this.GetVariableIndexForBitPosition(i);
            return new VarBool<T>(this.Manager, new int[1] { index });
        }

        /// <summary>
        /// Create a DD for an integer variable.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="bitwidth">Number of bits the variable uses.</param>
        /// <param name="length">Number of bits, starting from MSB, to encode.</param>
        /// <returns>Function representing the value provided.</returns>
        internal DD Eq(long value, int bitwidth, int length)
        {
            return this.Eq(ToBytes(value, bitwidth), length);
        }

        /// <summary>
        /// Less than or equal to function for a variable.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="bitwidth">Number of bits the variable uses.</param>
        /// <returns>Function representing the inequality.</returns>
        internal DD LessOrEqual(long value, int bitwidth)
        {
            return this.LessOrEqual(ToBytes(value, bitwidth));
        }

        /// <summary>
        /// Greater than or equal to function for a variable.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="bitwidth">Number of bits the variable uses.</param>
        /// <returns>Function representing the inequality.</returns>
        internal DD GreaterOrEqual(long value, int bitwidth)
        {
            return this.GreaterOrEqual(ToBytes(value, bitwidth));
        }

        /// <summary>
        /// Gets the variable index for a given MSB bit position.
        /// </summary>
        /// <param name="i">Bit position in MSB format.</param>
        /// <returns>The variable index.</returns>
        internal int GetVariableIndexForBitPosition(int i)
        {
            return this.Indices[this.order(i)];
        }

        /// <summary>
        /// Gets the bit position for a variable.
        /// </summary>
        /// <param name="v">The variable index.</param>
        /// <returns>The bit position for the variable.</returns>
        internal int GetBitPositionForVariableIndex(int v)
        {
            return this.reverseOrder[this.ReverseIndices[v]];
        }

        /// <summary>
        /// Convert an integer with a particular bitwidth
        /// to a byte[] representation.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="bitwidth">The integer bitwidth.</param>
        /// <returns>The representation as a byte[].</returns>
        private static byte[] ToBytes(long value, int bitwidth)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Resize(ref bytes, bitwidth / 8);
            Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// Get the nth MSB of an integer.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <param name="n">The nth bit.</param>
        /// <param name="bitwidth">The bitwidth.</param>
        /// <returns>Whether the bit is set.</returns>
        private static bool GetBit(long value, int n, int bitwidth)
        {
            return ((value >> (bitwidth - 1 - n)) & 1) == 1;
        }
    }
}
