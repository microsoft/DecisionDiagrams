// <copyright file="VarBool.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace DecisionDiagrams
{
    /// <summary>
    /// Boolean variable type.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    public class VarBool<T> : Variable<T>
        where T : IDDNode, IEquatable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VarBool{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="indices">The variable indices.</param>
        internal VarBool(DDManager<T> manager, int[] indices)
            : base(manager, indices, VariableType.BOOL, (i) => i)
        {
        }

        /// <summary>
        /// Identity function for a variable.
        /// </summary>
        /// <returns>The identify function.</returns>
        public DD Id()
        {
            return this.Manager.Id(this);
        }
    }
}
