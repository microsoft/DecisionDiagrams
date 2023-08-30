// <copyright file="CBDDNodeFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Implementation of a CBDD node factory that creates CBDD nodes that
    /// are always reduced to a canonical form.
    /// </summary>
    internal struct CBDDNodeFactory : IDDNodeFactory<CBDDNode>
    {
        /// <summary>
        /// Gets or sets the manager object. We call back into the manager recursively.
        /// The manager takes care of caching and ensuring canonicity.
        /// </summary>
        public DDManager<CBDDNode> Manager { get; set; }

        /// <summary>
        /// Gets the maximum number of variables allowed by the manager.
        /// </summary>
        public long MaxVariables { get; set; }

        /// <summary>
        /// The apply of two CBDDs.
        /// </summary>
        /// <param name="xid">The left operand index.</param>
        /// <param name="x">The left operand node.</param>
        /// <param name="yid">The right operand index.</param>
        /// <param name="y">The right operand node.</param>
        /// <param name="operation">The apply operation.</param>
        /// <returns>A new node representing the "And".</returns>
        public DDIndex Apply(DDIndex xid, CBDDNode x, DDIndex yid, CBDDNode y, DDOperation operation)
        {
            if (x.Variable < y.Variable)
            {
                if (x.NextVariable <= y.Variable)
                {
                    var xlow = this.Manager.Apply(x.Low, yid, operation);
                    var xhigh = this.Manager.Apply(x.High, yid, operation);
                    return this.Manager.Allocate(new CBDDNode(x.Variable, x.NextVariable, xlow, xhigh));
                }
                else
                {
                    var child = this.Manager.Allocate(new CBDDNode(y.Variable, x.NextVariable, x.Low, x.High));
                    var xlow = this.Manager.Apply(child, yid, operation);
                    var xhigh = this.Manager.Apply(x.High, yid, operation);
                    return this.Manager.Allocate(new CBDDNode(x.Variable, y.Variable, xlow, xhigh));
                }
            }
            else if (y.Variable < x.Variable)
            {
                if (y.NextVariable <= x.Variable)
                {
                    var ylow = this.Manager.Apply(y.Low, xid, operation);
                    var yhigh = this.Manager.Apply(y.High, xid, operation);
                    return this.Manager.Allocate(new CBDDNode(y.Variable, y.NextVariable, ylow, yhigh));
                }
                else
                {
                    var child = this.Manager.Allocate(new CBDDNode(x.Variable, y.NextVariable, y.Low, y.High));
                    var ylow = this.Manager.Apply(child, xid, operation);
                    var yhigh = this.Manager.Apply(y.High, xid, operation);
                    return this.Manager.Allocate(new CBDDNode(y.Variable, x.Variable, ylow, yhigh));
                }
            }
            else
            {
                if (x.NextVariable == y.NextVariable)
                {
                    var lo = this.Manager.Apply(x.Low, y.Low, operation);
                    var hi = this.Manager.Apply(x.High, y.High, operation);
                    return this.Manager.Allocate(new CBDDNode(x.Variable, x.NextVariable, lo, hi));
                }
                else if (x.NextVariable < y.NextVariable)
                {
                    var ychild = this.Manager.Allocate(new CBDDNode(x.NextVariable, y.NextVariable, y.Low, y.High));
                    var lo = this.Manager.Apply(x.Low, ychild, operation);
                    var hi = this.Manager.Apply(x.High, y.High, operation);
                    return this.Manager.Allocate(new CBDDNode(x.Variable, x.NextVariable, lo, hi));
                }
                else
                {
                    var xchild = this.Manager.Allocate(new CBDDNode(y.NextVariable, x.NextVariable, x.Low, x.High));
                    var lo = this.Manager.Apply(y.Low, xchild, operation);
                    var hi = this.Manager.Apply(y.High, x.High, operation);
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
        public DDIndex Ite(DDIndex fid, CBDDNode f, DDIndex gid, CBDDNode g, DDIndex hid, CBDDNode h)
        {
            var flevel = Level(fid, f);
            var glevel = Level(gid, g);
            var hlevel = Level(hid, h);

            var t = Math.Min(Math.Min(flevel, glevel), hlevel);
            var bf = f.Variable == t ? f.NextVariable : flevel;
            var bg = g.Variable == t ? g.NextVariable : glevel;
            var bh = h.Variable == t ? h.NextVariable : hlevel;
            var b = Math.Min(Math.Min(bf, bg), bh);

            GetCofactors(b, fid, f, out var flo, out var fhi);
            GetCofactors(b, gid, g, out var glo, out var ghi);
            GetCofactors(b, hid, h, out var hlo, out var hhi);

            var newLo = this.Manager.Ite(flo, glo, hlo);
            var newHi = this.Manager.Ite(fhi, ghi, hhi);
            return Manager.Allocate(new CBDDNode(t, b, newLo, newHi));
        }

        /// <summary>
        /// Gets the cofactors for the ITE operation.
        /// </summary>
        /// <param name="b">The bottom common index.</param>
        /// <param name="xid">The DD index.</param>
        /// <param name="x">The DD node.</param>
        /// <param name="lo">The out parameter for the lo cofactor.</param>
        /// <param name="hi">The out parameter for the hi cofactor.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetCofactors(int b, DDIndex xid, CBDDNode x, out DDIndex lo, out DDIndex hi)
        {
            if (x.Variable >= b)
            {
                lo = xid;
                hi = xid;
            }
            else if (x.NextVariable == b)
            {
                lo = x.Low;
                hi = x.High;
            }
            else
            {
                lo = Manager.Allocate(new CBDDNode(b, x.NextVariable, x.Low, x.High));
                hi = x.High;
            }
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
            if (x.Variable > variables.MaxIndex)
            {
                return xid;
            }

            var lo = this.Manager.Exists(ExpandLowChild(x), variables);
            var hi = this.Manager.Exists(x.High, variables);
            if (variables.Contains(x.Variable))
            {
                return this.Manager.Or(lo, hi);
            }

            return this.Manager.Allocate(new CBDDNode(x.Variable, x.Variable + 1, lo, hi));
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
            if (x.Variable > variableMap.MaxIndex)
            {
                return xid;
            }

            var lo = this.Manager.Replace(ExpandLowChild(x), variableMap);
            var hi = this.Manager.Replace(x.High, variableMap);
            var level = variableMap.Get(x.Variable);
            return this.Manager.Ite(this.Manager.Allocate(this.Id(level)), hi, lo);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Reduce(CBDDNode node, out DDIndex result)
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

            var lo = this.Manager.MemoryPool[node.Low.GetPosition()];
            if (lo.Variable == node.NextVariable && lo.High.Equals(node.High))
            {
                var reduced = new CBDDNode(node.Variable, lo.NextVariable, lo.Low, node.High);
                result = this.Manager.Allocate(reduced);
                return true;
            }

            return false;
        }

        /// <summary>
        /// The sat count for a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The number of satisfying assignments.</returns>
        public double SatCount(CBDDNode node)
        {
            var low = this.ExpandLowChild(node);
            var loNode = this.Manager.MemoryPool[low.GetPosition()];
            var hiNode = this.Manager.MemoryPool[node.High.GetPosition()];
            var loLevel = Level(low, loNode);
            var hiLevel = Level(node.High, hiNode);
            var scaleLo = Math.Pow(2.0, loLevel - node.Variable - 1);
            var scaleHi = Math.Pow(2.0, hiLevel - node.Variable - 1);
            var countLo = this.Manager.SatCount(low);
            var countHi = this.Manager.SatCount(node.High);
            return scaleLo * countLo + scaleHi * countHi;
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
                    assignment.Add(i, false);
                }
            }
            else
            {
                assignment.Add(node.NextVariable - 1, true);
            }
        }

        /// <summary>
        /// Expand the low child to the next variable if in a chain.
        /// </summary>
        /// <param name="x">The cbdd node.</param>
        /// <returns>The index of the new low child.</returns>
        private DDIndex ExpandLowChild(CBDDNode x)
        {
            if (x.NextVariable == x.Variable + 1)
            {
                return x.Low;
            }

            return this.Manager.Allocate(new CBDDNode(x.Variable + 1, x.NextVariable, x.Low, x.High));
        }

        /// <summary>
        /// Gets the "level" for the node, where the maximum
        /// value is used for constants.
        /// </summary>
        /// <param name="idx">The node index.</param>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public int Level(DDIndex idx, CBDDNode node)
        {
            return idx.IsConstant() ? this.Manager.NumVariables + 1 : node.Variable;
        }
    }
}
