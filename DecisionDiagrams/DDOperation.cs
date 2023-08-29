// <copyright file="Bitops.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    /// <summary>
    /// A decision diagram operation.
    /// </summary>
    internal enum DDOperation
    {
        /// <summary>
        /// Logical conjunction operation.
        /// </summary>
        And,

        /// <summary>
        /// Logical if-and-only-if operation.
        /// </summary>
        Iff,

        /// <summary>
        /// Logical exists operation.
        /// </summary>
        Exists,

        /// <summary>
        /// Replace operation.
        /// </summary>
        Replace,
    }
}
