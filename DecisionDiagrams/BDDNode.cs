// <copyright file="BDDNode.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// An implementation of Binary Decision Diagrams (BDDs).
    /// BDDs are a primary variable of decision diagrams that
    /// branch on individual bits. When both left and right
    /// branches point to the same child, then the parent is
    /// simply replaced with the child to ensure canonicity.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BDDNode : IDDNode, IEquatable<BDDNode>
    {
        /// <summary>
        /// Node meta data.
        /// </summary>
        private NodeData16 data;

        /// <summary>
        /// Initializes a new instance of the <see cref="BDDNode"/> struct.
        /// </summary>
        /// <param name="variable">The variable index.</param>
        /// <param name="lo">The low (false) child.</param>
        /// <param name="hi">The high (true) child.</param>
        public BDDNode(int variable, DDIndex lo, DDIndex hi)
        {
            this.data = new NodeData16(variable, false);
            this.Low = lo;
            this.High = hi;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the node is marked as garbage.
        /// </summary>
        public bool Mark
        {
            get { return this.data.Mark; }
            set { this.data.Mark = value; }
        }

        /// <summary>
        /// Gets or sets the low (false) child.
        /// </summary>
        public DDIndex Low { get; set; }

        /// <summary>
        /// Gets or sets the high (true) child.
        /// </summary>
        public DDIndex High { get; set; }

        /// <summary>
        /// Gets the variable id.
        /// </summary>
        public int Variable
        {
            get { return this.data.Variable; }
        }

        /// <summary>
        /// Equality between bdd nodes.
        /// </summary>
        /// <param name="other">The other node.</param>
        /// <returns>Whether the objects are equal.</returns>
        public bool Equals(BDDNode other)
        {
            return this.Variable == other.Variable &&
                   this.Low.Equals(other.Low) &&
                   this.High.Equals(other.High);
        }

        /// <summary>
        /// Equality between BDDNodes.
        /// </summary>
        /// <param name="obj">The other node.</param>
        /// <returns>Whether the objects are equal.</returns>
        public override bool Equals(object obj)
        {
            return this.Equals((BDDNode)obj);
        }

        /// <summary>
        /// Hash code for BDDNode. Custom hashcode found to
        /// work well in practice.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return (7 * this.Variable) + this.Low.GetHashCode() + this.High.GetHashCode();
        }
    }
}
