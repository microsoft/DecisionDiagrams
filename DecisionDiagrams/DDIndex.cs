// <copyright file="DDIndex.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;

    /// <summary>
    /// Decision diagram index that serves as a pointer into
    /// the DDManager memory pool. The advantage of using
    /// indices rather than pointers for decision diagrams is
    /// the lower memory use. We use a 32-bit index but also
    /// use 2 bits of this index for metadata per node. The scheme is:
    ///
    /// 1 bit to mark an index as invalid for future use or debugging.
    /// 1 bit to mark an index as negated (complemented)
    /// 30 bits for the actual index.
    ///
    /// This allows for up to 1 billion nodes to be allocated for
    /// a given manager. By using the first bit for marking a node
    /// as invalid we can take advantage of 2's complement representation
    /// by simply checking if the index is less than 0.
    ///
    /// Negating a formula is done by simply flipping the complement
    /// bit in the index.
    /// </summary>
    public struct DDIndex : IEquatable<DDIndex>
    {
        /// <summary>
        /// The formula for false.
        /// </summary>
        public static readonly DDIndex False = new DDIndex(0, false);

        /// <summary>
        /// The formula for true.
        /// </summary>
        public static readonly DDIndex True = new DDIndex(0, true);

        /// <summary>
        /// Initializes a new instance of the <see cref="DDIndex"/> struct.
        /// </summary>
        /// <param name="index">A valid manager index.</param>
        /// <param name="negate">Is the formula negated.</param>
        public DDIndex(int index, bool negate)
        {
            this.Index = (index << 1) | (negate ? 1 : 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DDIndex"/> struct.
        /// </summary>
        /// <param name="index">Index value.</param>
        private DDIndex(int index)
        {
            this.Index = index;
        }

        /// <summary>
        /// Gets the 32-bit index value.
        /// </summary>
        internal int Index { get; private set; }

        /// <summary>
        /// Is this a negated formula?.
        /// </summary>
        /// <returns>Whether this DD is negated.</returns>
        public bool IsComplemented()
        {
            return (this.Index & 1) == 1;
        }

        /// <summary>
        /// Create a new formula that flips the negation bit.
        /// </summary>
        /// <returns>A new index that is complemented.</returns>
        public DDIndex Flip()
        {
            return new DDIndex(this.Index ^ 1);
        }

        /// <summary>
        /// Does this index represent a constant formula (true or false).
        /// </summary>
        /// <returns>Whether the index represents a constant node.</returns>
        public bool IsConstant()
        {
            return this.GetPosition() == 0;
        }

        /// <summary>
        /// Does this index represent the true formula.
        /// </summary>
        /// <returns>Whether the index represents true.</returns>
        public bool IsOne()
        {
            return this.Index == True.Index;
        }

        /// <summary>
        /// Does this index represent the false formula.
        /// </summary>
        /// <returns>Whether the index represents false.</returns>
        public bool IsZero()
        {
            return this.Index == False.Index;
        }

        /// <summary>
        /// Extract the manager index from this DD.
        /// </summary>
        /// <returns>The index.</returns>
        public int GetPosition()
        {
            return this.Index >> 1;
        }

        /// <summary>
        /// Equality between indices.
        /// </summary>
        /// <param name="other">The other index.</param>
        /// <returns>Whether the objects are equal.</returns>
        public bool Equals(DDIndex other)
        {
            return this.Index == other.Index;
        }

        /// <summary>
        /// Hash code for a DD index. It is important that
        /// this returns a positive value, and distinguishes
        /// between negated and not negated DDs for the manager.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return this.GetPosition() + (this.Index & 1);
        }
    }
}
