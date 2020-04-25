// <copyright file="Domain.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    /// <summary>
    /// A domain of values represented using DDs.
    /// </summary>
    public class BitVector<T>
        where T : IDDNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="bits">The bits of the integer.</param>
        internal BitVector(DDManager<T> manager, DD[] bits)
        {
            this.Bits = new DDIndex[bits.Length];
            for (int i = 0; i < bits.Length; i++)
            {
                this.Bits[i] = bits[i].Index;
            }

            this.Manager = manager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="size">The domain size in bits.</param>
        internal BitVector(DDManager<T> manager, int size)
        {
            this.Bits = new DDIndex[size];
            this.Manager = manager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="manager">The manager.</param>
        public BitVector(Variable<T> variable, DDManager<T> manager)
        {
            this.Bits = new DDIndex[variable.Indices.Length];
            for (int i = 0; i < variable.Indices.Length; i++)
            {
                this.Bits[i] = variable.GetVariableForIthBit(i).IdIdx();
            }

            this.Manager = manager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="x">The value.</param>
        /// <param name="manager">The manager.</param>
        public BitVector(byte x, DDManager<T> manager)
        {
            this.InitializeConstant(x, 8);
            this.Manager = manager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="x">The value.</param>
        /// <param name="manager">The manager.</param>
        public BitVector(ushort x, DDManager<T> manager)
        {
            this.InitializeConstant(x, 16);
            this.Manager = manager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="x">The value.</param>
        /// <param name="manager">The manager.</param>
        public BitVector(short x, DDManager<T> manager)
        {
            this.InitializeConstant(x, 16);
            this.Manager = manager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="x">The value.</param>
        /// <param name="manager">The manager.</param>
        public BitVector(uint x, DDManager<T> manager)
        {
            this.InitializeConstant(x, 32);
            this.Manager = manager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="x">The value.</param>
        /// <param name="manager">The manager.</param>
        public BitVector(int x, DDManager<T> manager)
        {
            this.InitializeConstant(x, 32);
            this.Manager = manager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="x">The value.</param>
        /// <param name="manager">The manager.</param>
        public BitVector(ulong x, DDManager<T> manager)
        {
            this.InitializeConstant((long)x, 64);
            this.Manager = manager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector{T}"/> class.
        /// </summary>
        /// <param name="x">The value.</param>
        /// <param name="manager">The manager.</param>
        public BitVector(long x, DDManager<T> manager)
        {
            this.InitializeConstant(x, 64);
            this.Manager = manager;
        }

        /// <summary>
        /// Gets the manager object.
        /// </summary>
        public DDManager<T> Manager { get; }

        /// <summary>
        /// One DD per bit, with the MSB appearing in the 0th index.
        /// </summary>
        internal DDIndex[] Bits { get; private set; }

        /// <summary>
        /// Gets the number of bits.
        /// </summary>
        public int Size { get => this.Bits.Length; }

        /// <summary>
        /// Get the underlying bits as decision diagrams.
        /// </summary>
        /// <returns>The bits.</returns>
        public DD[] GetBits()
        {
            var result = new DD[this.Bits.Length];
            for (int i = 0; i < this.Bits.Length; i++)
            {
                result[i] = this[i];
            }

            return result;
        }

        /// <summary>
        /// Get or sets the ith bit.
        /// </summary>
        /// <param name="i">The bit index.</param>
        /// <returns>The ith bit as a decision diagram.</returns>
        public DD this[int i]
        {
            get
            {
                return this.Manager.FromIndex(this.Bits[i]);
            }
            set
            {
                this.Bits[i] = value.Index;
            }
        }

        /// <summary>
        /// Initialize the domain to a constant.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="size">The size in number of bits.</param>
        private void InitializeConstant(long value, int size)
        {
            this.Bits = new DDIndex[size];
            for (int i = size - 1; i >= 0; i--)
            {
                this.Bits[i] = (value & 1) == 1 ? DDIndex.True : DDIndex.False;
                value = value >> 1;
            }
        }
    }
}
