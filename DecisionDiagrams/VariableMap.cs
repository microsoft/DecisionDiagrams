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
        /// Gets the smallest index in the map.
        /// </summary>
        internal int MinIndex { get; } = -1;

        /// <summary>
        /// Gets the largest index in the map.
        /// </summary>
        internal int MaxIndex { get; } = -1;

        /// <summary>
        /// A unique id for the variable map.
        /// </summary>
        internal int Id { get; private set; }

        /// <summary>
        /// Gets the id as an index for caching.
        /// </summary>
        internal DDIndex IdIndex { get; }

        /// <summary>
        /// Gets the variables in the set.
        /// </summary>
        internal int[] VariableArray { get; }

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
            this.Id = Interlocked.Increment(ref nextId);
            this.IdIndex = new DDIndex(this.Id, false);
            this.Mapping = mapping;

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
                    this.MinIndex = this.MinIndex < 0 ? keyIndex : Math.Min(this.MinIndex, keyIndex);
                    this.MaxIndex = Math.Max(this.MaxIndex, keyIndex);
                }
            }

            this.VariableArray = new int[this.MaxIndex - this.MinIndex + 1];

            for (int i = 0; i < this.VariableArray.Length; i++)
            {
                this.VariableArray[i] = i + this.MinIndex;
            }

            foreach (var keyValuePair in mapping)
            {
                var variable1 = keyValuePair.Key;
                var variable2 = keyValuePair.Value;

                for (int i = variable1.Indices.Length - 1; i >= 0; i--)
                {
                    var keyIndex = variable1.Indices[i];
                    var valueIndex = variable2.Indices[i];
                    this.VariableArray[keyIndex - this.MinIndex] = valueIndex;
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

            var index = variable - this.MinIndex;
            return this.VariableArray[index];
        }
    }
}
