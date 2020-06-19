// <copyright file="CBDDNodeFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Implementation of a CBDD node factory that creates CBDD nodes that
    /// are always reduced to a canonical form.
    /// </summary>
    public class CBDDNodeFactory : IDDNodeFactory<CBDDNode>
    {
        /// <summary>
        /// Gets or sets the manager object. We call
        /// back into the manager recursively
        /// The manager takes care of caching and
        /// ensuring canonicity.
        /// </summary>
        public DDManager<CBDDNode> Manager { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the factory supports ite.
        /// </summary>
        public bool SupportsIte { get; } = false;

        /// <summary>
        /// The logical conjunction of two BDDs as the
        /// standard BDD "apply" operation.
        /// </summary>
        /// <param name="xid">The left operand index.</param>
        /// <param name="x">The left operand node.</param>
        /// <param name="yid">The right operand index.</param>
        /// <param name="y">The right operand node.</param>
        /// <returns>A new node representing the "And".</returns>
        public DDIndex And(DDIndex xid, CBDDNode x, DDIndex yid, CBDDNode y)
        {
            if (x.Variable < y.Variable)
            {
                if (x.NextVariable <= y.Variable)
                {
                    var xlow = this.Manager.And(x.Low, yid);
                    var xhigh = this.Manager.And(x.High, yid);
                    return this.Manager.Allocate(new CBDDNode(x.Variable, x.NextVariable, xlow, xhigh));
                }
                else
                {
                    var child = this.Manager.Allocate(new CBDDNode(y.Variable, x.NextVariable, x.Low, x.High));
                    var xlow = this.Manager.And(child, yid);
                    var xhigh = this.Manager.And(x.High, yid);
                    return this.Manager.Allocate(new CBDDNode(x.Variable, y.Variable, xlow, xhigh));
                }
            }
            else if (y.Variable < x.Variable)
            {
                if (y.NextVariable <= x.Variable)
                {
                    var ylow = this.Manager.And(y.Low, xid);
                    var yhigh = this.Manager.And(y.High, xid);
                    return this.Manager.Allocate(new CBDDNode(y.Variable, y.NextVariable, ylow, yhigh));
                }
                else
                {
                    var child = this.Manager.Allocate(new CBDDNode(x.Variable, y.NextVariable, y.Low, y.High));
                    var ylow = this.Manager.And(child, xid);
                    var yhigh = this.Manager.And(y.High, xid);
                    return this.Manager.Allocate(new CBDDNode(y.Variable, x.Variable, ylow, yhigh));
                }
            }
            else
            {
                if (x.NextVariable == y.NextVariable)
                {
                    var lo = this.Manager.And(x.Low, y.Low);
                    var hi = this.Manager.And(x.High, y.High);
                    return this.Manager.Allocate(new CBDDNode(x.Variable, x.NextVariable, lo, hi));
                }
                else if (x.NextVariable < y.NextVariable)
                {
                    var ychild = this.Manager.Allocate(new CBDDNode(x.NextVariable, y.NextVariable, y.Low, y.High));
                    var lo = this.Manager.And(x.Low, ychild);
                    var hi = this.Manager.And(x.High, y.High);
                    return this.Manager.Allocate(new CBDDNode(x.Variable, x.NextVariable, lo, hi));
                }
                else
                {
                    var xchild = this.Manager.Allocate(new CBDDNode(y.NextVariable, x.NextVariable, x.Low, x.High));
                    var lo = this.Manager.And(y.Low, xchild);
                    var hi = this.Manager.And(y.High, x.High);
                    return this.Manager.Allocate(new CBDDNode(y.Variable, y.NextVariable, lo, hi));
                }
            }
        }

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
        [ExcludeFromCodeCoverage]
        public DDIndex Ite(DDIndex fid, CBDDNode f, DDIndex gid, CBDDNode g, DDIndex hid, CBDDNode h)
        {
            throw new System.NotSupportedException();
        }

        /// <summary>
        /// Implement the logical "exists" operation,
        /// recursively calling the manager if necessary.
        /// </summary>
        /// <param name="xid">The left index.</param>
        /// <param name="x">The left node.</param>
        /// <param name="variables">The variable set.</param>
        /// <returns>The resulting function.</returns>
        public DDIndex Exists(DDIndex xid, CBDDNode x, VariableSet<CBDDNode> variables)
        {
            throw new System.NotSupportedException();
        }

        /// <summary>
        /// Implement a replacement operation that substitutes
        /// variables for other variables.
        /// </summary>
        /// <param name="xid">The left index.</param>
        /// <param name="x">The left node.</param>
        /// <param name="variableMap">The variable set.</param>
        /// <returns>A new formula with the susbtitution.</returns>
        public DDIndex Replace(DDIndex xid, CBDDNode x, VariableMap<CBDDNode> variableMap)
        {
            throw new System.NotSupportedException();
        }

        /// <summary>
        /// Create a new node with children flipped.
        /// </summary>
        /// <param name="node">The old node.</param>
        /// <returns>A copy of the node with the children flipped.</returns>
        public CBDDNode Flip(CBDDNode node)
        {
            return new CBDDNode(node.Variable, node.NextVariable, node.Low.Flip(), node.High.Flip());
        }

        /// <summary>
        /// The identity node for a variable.
        /// </summary>
        /// <param name="variable">The variable index.</param>
        /// <returns>The identity node.</returns>
        public CBDDNode Id(int variable)
        {
            return new CBDDNode(variable, variable + 1, DDIndex.False, DDIndex.True);
        }

        /// <summary>
        /// Reduction rules for a BDD.
        /// </summary>
        /// <param name="node">The node to reduce.</param>
        /// <param name="result">The modified node.</param>
        /// <returns>If there was a reduction.</returns>
        public virtual bool Reduce(CBDDNode node, out DDIndex result)
        {
            result = DDIndex.False;
            if (node.Low.Equals(node.High))
            {
                result = node.Low;
                return true;
            }

            if (node.Low.IsConstant())
            {
                return false;
            }

            var lo = this.Manager.LookupNodeByIndex(node.Low);
            if (lo.Variable == node.NextVariable && lo.High.Equals(node.High))
            {
                var reduced = new CBDDNode(node.Variable, lo.NextVariable, lo.Low, node.High);
                result = this.Manager.Allocate(reduced);
                return true;
            }

            return false;
        }

        /// <summary>
        /// How to display a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="negated">Parity of negation.</param>
        /// <returns>The string representation.</returns>
        public string Display(CBDDNode node, bool negated)
        {
            return string.Format(
                "({0}:{1} ? {2} : {3})",
                node.Variable,
                node.Length,
                this.Manager.Display(node.High, negated),
                this.Manager.Display(node.Low, negated));
        }

        /// <summary>
        /// Update an assignment to variables given an edge.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="hi">Which edge.</param>
        /// <param name="assignment">current assignment.</param>
        public void Sat(CBDDNode node, bool hi, Dictionary<int, bool> assignment)
        {
            if (!hi)
            {
                for (int i = node.Variable; i < node.NextVariable; i++)
                {
                    assignment.Add(i, hi);
                }
            }
            else
            {
                assignment.Add(node.Variable, hi);
            }
        }
    }
}
