// <copyright file="IDDNodeFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A factory for custom nodes types. In order to support
    /// logical operations from the decision diagram manager,
    /// the factory must implement this interface.
    /// </summary>
    /// <typeparam name="T">The custom node type.</typeparam>
    internal interface IDDNodeFactory<T>
        where T : IDDNode, IEquatable<T>
    {
        /// <summary>
        /// Gets or sets the decision diagram manager.
        /// </summary>
        DDManager<T> Manager { get; set; }

        /// <summary>
        /// Gets the maximum number of variables allowed by the manager.
        /// </summary>
        long MaxVariables { get; set; }

        /// <summary>
        /// Create the node representing the identity function.
        /// </summary>
        /// <param name="variable">The variable index.</param>
        /// <returns>The identity function.</returns>
        T Id(int variable);

        /// <summary>
        /// Create a new node with children flipped.
        /// </summary>
        /// <param name="node">The old node.</param>
        /// <returns>A new node with children flipped.</returns>
        T Flip(T node);

        /// <summary>
        /// Apply any reduction rules for a node.
        /// </summary>
        /// <param name="node">The node to reduce.</param>
        /// <param name="result">The reduced node.</param>
        /// <returns>If a reduction ocurred.</returns>
        bool Reduce(T node, out DDIndex result);

        /// <summary>
        /// Implement the logical "apply" operation,
        /// recursively calling the manager if necessary.
        /// </summary>
        /// <param name="xid">The left index.</param>
        /// <param name="x">The left node.</param>
        /// <param name="yid">The right index.</param>
        /// <param name="y">The right node.</param>
        /// <param name="operation">The operation.</param>
        /// <returns>The apply of the two nodes.</returns>
        DDIndex Apply(DDIndex xid, T x, DDIndex yid, T y, DDOperation operation);

        /// <summary>
        /// Implement the logical "ite" operation,
        /// recursively calling the manager if necessary.
        /// </summary>
        /// <param name="fid">The f index.</param>
        /// <param name="f">The f node.</param>
        /// <param name="gid">The g index.</param>
        /// <param name="g">The g node.</param>
        /// <param name="hid">The h index.</param>
        /// <param name="h">The h node.</param>
        /// <returns>The ite of the three nodes.</returns>
        DDIndex Ite(DDIndex fid, T f, DDIndex gid, T g, DDIndex hid, T h);

        /// <summary>
        /// Implement the logical "exists" operation,
        /// recursively calling the manager if necessary.
        /// </summary>
        /// <param name="xid">The left index.</param>
        /// <param name="x">The left node.</param>
        /// <param name="variables">The variable set.</param>
        /// <returns>The and of the two nodes.</returns>
        DDIndex Exists(DDIndex xid, T x, VariableSet<T> variables);

        /// <summary>
        /// Implement a replacement operation that substitutes
        /// variables for other variables.
        /// </summary>
        /// <param name="xid">The left index.</param>
        /// <param name="x">The left node.</param>
        /// <param name="variableMap">The variable set.</param>
        /// <returns>A new formula with the susbtitution.</returns>
        DDIndex Replace(DDIndex xid, T x, VariableMap<T> variableMap);

        /// <summary>
        /// The sat count for a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The number of satisfying assignments.</returns>
        double SatCount(T node);

        /// <summary>
        /// How to display a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="negated">Parity of negation.</param>
        /// <returns>The string representation.</returns>
        string Display(T node, bool negated);

        /// <summary>
        /// Update an assignment to variables given an edge.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="hi">Which edge.</param>
        /// <param name="assignment">current assignment.</param>
        void Sat(T node, bool hi, Dictionary<int, bool> assignment);

        /// <summary>
        /// Gets the "level" for the node, where the maximum
        /// value is used for constants.
        /// </summary>
        /// <param name="idx">The node index.</param>
        /// <param name="node">The node.</param>
        /// <returns>The level.</returns>
        int Level(DDIndex idx, T node);
    }
}
