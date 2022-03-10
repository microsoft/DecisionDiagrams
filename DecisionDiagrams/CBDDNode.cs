// <copyright file="CBDDNode.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// An implementation of Compressed Binary Decision Diagrams (CBDDs).
    /// CBDDs are a variant of decision diagrams that compress long chains
    /// of redudant variable assignments to save space.
    /// </summary>
    public struct CBDDNode : IDDNode, IEquatable<CBDDNode>
    {
        /// <summary>
        /// Node meta data.
        /// </summary>
        private NodeData32Packed data;

        /// <summary>
        /// Initializes a new instance of the <see cref="CBDDNode"/> struct.
        /// </summary>
        /// <param name="variable">The variable index.</param>
        /// <param name="nextVariable">The next index.</param>
        /// <param name="lo">The low (false) child.</param>
        /// <param name="hi">The high (true) child.</param>
        public CBDDNode(int variable, int nextVariable, DDIndex lo, DDIndex hi)
        {
            this.data = new NodeData32Packed(variable, false, nextVariable);
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
        /// Gets the node next index.
        /// </summary>
        public int NextVariable
        {
            get { return this.data.Metadata; }
        }

        /// <summary>
        /// Gets the number of variables.
        /// </summary>
        public int Length
        {
            get
            {
                return this.NextVariable - this.Variable;
            }
        }

        /// <summary>
        /// Equality between bdd nodes.
        /// </summary>
        /// <param name="other">The other node.</param>
        /// <returns>Whether the objects are equal.</returns>
        public bool Equals(CBDDNode other)
        {
            return this.Variable == other.Variable &&
                   this.Low.Equals(other.Low) &&
                   this.High.Equals(other.High) &&
                   this.NextVariable.Equals(other.NextVariable);
        }

        /// <summary>
        /// Equality between BDDNodes.
        /// </summary>
        /// <param name="obj">The other node.</param>
        /// <returns>Whether the objects are equal.</returns>
        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
        {
            return this.Equals((CBDDNode)obj);
        }

        /// <summary>
        /// Hash code for BDDNode. Custom hashcode found to
        /// work well in practice.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return (7 * this.Variable) + this.Low.GetHashCode() + this.High.GetHashCode() + this.NextVariable;
        }
    }
}
