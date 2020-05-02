// <copyright file="VariableSet.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Collections;

    /// <summary>
    /// Represents a set of variables.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public class VariableSet<T>
        where T : IDDNode
    {
        /// <summary>
        /// The manager object.
        /// </summary>
        private DDManager<T> manager;

        /// <summary>
        /// The set of variables.
        /// </summary>
        private BitArray variables;

        /// <summary>
        /// Gets the largest index in the set.
        /// </summary>
        internal int MaxIndex { get; } = 0;

        /// <summary>
        /// Gets the DD representing the variables for efficient
        /// caching purposes.
        /// </summary>
        internal DDIndex AsIndex { get; private set; }

        /// <summary>
        /// Gets the variables in the set.
        /// </summary>
        public Variable<T>[] Variables { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableSet{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="variables">The variables.</param>
        /// <param name="numVariables">The number of variables in the manager.</param>
        internal VariableSet(DDManager<T> manager, Variable<T>[] variables, int numVariables)
        {
            this.manager = manager;
            this.variables = new BitArray(numVariables);
            this.AsIndex = DDIndex.True;
            this.Variables = variables;
            foreach (var v in variables)
            {
                for (int i = v.Indices.Length - 1; i >= 0; i--)
                {
                    var variableIndex = v.Indices[i];
                    this.MaxIndex = Math.Max(this.MaxIndex, variableIndex);
                    this.AsIndex = manager.And(this.AsIndex, manager.IdIdx(variableIndex));
                    this.variables.Set(variableIndex, true);
                }
            }
        }

        /// <summary>
        /// Does the variable set contain the variable.
        /// </summary>
        /// <param name="variable">The variable index.</param>
        /// <returns>Whether the set contains that variable.</returns>
        internal bool Contains(int variable)
        {
            return this.variables.Get(variable);
        }
    }
}
