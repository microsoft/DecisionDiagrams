// <copyright file="IDDNode.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    /// <summary>
    /// A generic node definition for use with the decision
    /// diagram manager class to provide various logical
    /// operations.
    /// </summary>
    public interface IDDNode
    {
        /// <summary>
        /// Gets or sets a value indicating whether the node is marked as garbage.
        /// </summary>
        bool Mark { get; set; }

        /// <summary>
        /// Gets a variable index from a node.
        /// </summary>
        int Variable { get; }

        /// <summary>
        /// Gets or sets the left child from a node.
        /// </summary>
        DDIndex Low { get; set; }

        /// <summary>
        /// Gets or sets the right child from a node.
        /// </summary>
        DDIndex High { get; set; }
    }
}
