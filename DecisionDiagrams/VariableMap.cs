// <copyright file="VariableMap.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Represents a mapping from variable to variable.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public class VariableMap<T>
        where T : IDDNode
    {
        private static int nextId = 0;

        /// <summary>
        /// The manager object.
        /// </summary>
        private DDManager<T> manager;

        /// <summary>
        /// Gets the largest index in the set.
        /// </summary>
        internal int MaxIndex { get; private set; }

        /// <summary>
        /// Gets the DD representing the variables for efficient
        /// caching purposes.
        /// </summary>
        internal int Id { get; private set; }

        /// <summary>
        /// Gets the variables in the set.
        /// </summary>
        internal int[] variableArray { get; }

        /// <summary>
        /// Gets the variable pairings.
        /// </summary>
        public Dictionary<Variable<T>, Variable<T>> Mapping { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableMap{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="mapping">The mapping to add.</param>
        /// <param name="numVariables">The number of variables.</param>
        internal VariableMap(DDManager<T> manager, Dictionary<Variable<T>, Variable<T>> mapping, int numVariables)
        {
            this.manager = manager;
            this.MaxIndex = 0;
            this.Id = Interlocked.Increment(ref nextId);
            this.variableArray = new int[numVariables];
            this.Mapping = mapping;

            for (int i = 0; i < numVariables; i++)
            {
                this.variableArray[i] = -1;
            }

            foreach (var keyValuePair in mapping)
            {
                var variable1 = keyValuePair.Key;
                var variable2 = keyValuePair.Value;

                if (variable1.Type != variable2.Type)
                {
                    throw new ArgumentException($"Adding mismatched variable types to map: {variable1.Type} and {variable2.Type}");
                }

                for (int i = variable1.Indices.Length - 1; i >= 0; i--)
                {
                    var keyIndex = variable1.Indices[i];
                    var valueIndex = variable2.Indices[i];
                    this.MaxIndex = Math.Max(this.MaxIndex, keyIndex);
                    this.variableArray[keyIndex] = valueIndex;
                }
            }
        }

        /// <summary>
        /// Gets the index a variable index maps to.
        /// </summary>
        /// <param name="variable">The variable index.</param>
        /// <returns>The index mapped to. Negative if none.</returns>
        internal int Get(int variable)
        {
            return this.variableArray[variable];
        }
    }
}
