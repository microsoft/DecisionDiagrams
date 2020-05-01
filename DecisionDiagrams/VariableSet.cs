// <copyright file="VariableSet.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
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
            foreach (var v in variables)
            {
                for (int i = v.Indices.Length - 1; i >= 0; i--)
                {
                    this.AsIndex = manager.And(this.AsIndex, manager.IdIdx(v.Indices[i]));
                    this.variables.Set(v.Indices[i], true);
                }
            }
        }

        /// <summary>
        /// Gets the DD representing the variables for efficient
        /// caching purposes.
        /// </summary>
        internal DDIndex AsIndex { get; private set; }

        /// <summary>
        /// Does the variable set contain the variable.
        /// </summary>
        /// <param name="variable">The variable index.</param>
        /// <returns>Whether the set contains that variable.</returns>
        internal bool Contains(int variable)
        {
            return variable < this.variables.Length && this.variables.Get(variable);
        }
    }
}
