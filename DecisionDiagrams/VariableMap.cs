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
        where T : IDDNode, IEquatable<T>
    {
        private static int nextId = 0;

        /// <summary>
        /// Gets the smallest index in the map.
        /// </summary>
        internal int MinIndex { get; } = -1;

        /// <summary>
        /// Gets the largest index in the map.
        /// </summary>
        internal int MaxIndex { get; } = -1;

        /// <summary>
        /// Gets the id as an index for caching.
        /// </summary>
        internal DDIndex IdIndex { get; }

        /// <summary>
        /// Gets the variables in the set.
        /// </summary>
        internal Dictionary<int, int> VariableMapping { get; }

        /// <summary>
        /// Gets the manager object.
        /// </summary>
        public int ManagerId { get; }

        /// <summary>
        /// Gets the variable pairings.
        /// </summary>
        public Dictionary<Variable<T>, Variable<T>> Mapping { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableMap{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="mapping">The mapping to add.</param>
        internal VariableMap(DDManager<T> manager, Dictionary<Variable<T>, Variable<T>> mapping)
        {
            this.ManagerId = manager.Uid;
            this.IdIndex = new DDIndex(Interlocked.Increment(ref nextId), false);
            this.Mapping = mapping;
            this.VariableMapping = new Dictionary<int, int>();

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
                    this.MinIndex = this.MinIndex < 0 ? keyIndex : Math.Min(this.MinIndex, keyIndex);
                    this.MaxIndex = Math.Max(this.MaxIndex, keyIndex);
                    this.VariableMapping[keyIndex] = valueIndex;
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
            if (variable < this.MinIndex)
            {
                return variable;
            }

            if (this.VariableMapping.TryGetValue(variable, out int newVariable))
            {
                return newVariable;
            }

            return variable;
        }
    }
}
