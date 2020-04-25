// <copyright file="DD.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System.Collections.Generic;

    /// <summary>
    /// Decision diagram representing a boolean function.
    /// </summary>
    public sealed class DD
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DD"/> class.
        /// </summary>
        /// <param name="managerId">Id of the manager that created this node.</param>
        /// <param name="index">Index of the underlying IDDNode.</param>
        internal DD(ushort managerId, DDIndex index)
        {
            this.ManagerId = managerId;
            this.Index = index;
        }

        /// <summary>
        /// Gets the id of the manager that allocated this DD.
        /// </summary>
        internal ushort ManagerId { get; }

        /// <summary>
        /// Gets or sets the internal manager index for where the node resides.
        /// </summary>
        internal DDIndex Index { get; set; }

        /// <summary>
        /// Does this index represent a constant formula (true or false).
        /// </summary>
        /// <returns>Whether this index is for a constant.</returns>
        public bool IsConstant()
        {
            return this.Index.IsConstant();
        }

        /// <summary>
        /// Does this index represent the true formula.
        /// </summary>
        /// <returns>Whether this index is for true.</returns>
        public bool IsTrue()
        {
            return this.Index.IsOne();
        }

        /// <summary>
        /// Does this index represent the false formula.
        /// </summary>
        /// <returns>Whether this index is for false.</returns>
        public bool IsFalse()
        {
            return this.Index.IsZero();
        }

        /// <summary>
        /// Check equality between two functions.
        /// </summary>
        /// <param name="obj">The other function.</param>
        /// <returns>Whether the objects are equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is DD dD && this.ManagerId == dD.ManagerId && this.Index.Equals(dD.Index);
        }

        /// <summary>
        /// Hash code for DD function.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            var hashCode = 7;
            hashCode = (hashCode * 31) + this.ManagerId.GetHashCode();
            hashCode = (hashCode * 31) + EqualityComparer<DDIndex>.Default.GetHashCode(this.Index);
            return hashCode;
        }
    }
}
