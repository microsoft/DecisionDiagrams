// <copyright file="ZDDNodeFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    /// <summary>
    /// Implementation of a factory for ZDDs.
    /// Extends BDDs with a new type of reduction rule.
    /// </summary>
    public class ZDDNodeFactory : BDDNodeFactory
    {
        /// <summary>
        /// Reduction rules for a ZBDD.
        /// </summary>
        /// <param name="node">The node to reduce.</param>
        /// <param name="result">The modified node.</param>
        /// <returns>If there was a reduction.</returns>
        public override bool Reduce(BDDNode node, out DDIndex result)
        {
            result = DDIndex.False;
            if (node.High.IsZero())
            {
                result = node.Low;
                return true;
            }

            return false;
        }
    }
}
