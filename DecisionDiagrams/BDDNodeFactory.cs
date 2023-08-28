// <copyright file="BDDNodeFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;

    /// <summary>
    /// Implementation of a factory for BDDNode objects.
    /// Calls back into DDManager recursively.
    /// </summary>
    public class BDDNodeFactory : IDDNodeFactory<BDDNode>
    {
        /// <summary>
        /// Gets or sets the manager object. We call
        /// back into the manager recursively
        /// The manager takes care of caching and
        /// ensuring canonicity.
        /// </summary>
        public DDManager<BDDNode> Manager { get; set; }

        /// <summary>
        /// Gets the maximum number of variables allowed by the manager.
        /// </summary>
        public long MaxVariables { get; } = (long)(1U << 31) - 1;

        /// <summary>
        /// The apply of two bdds.
        /// </summary>
        /// <param name="xid">The left operand index.</param>
        /// <param name="x">The left operand node.</param>
        /// <param name="yid">The right operand index.</param>
        /// <param name="y">The right operand node.</param>
        /// <param name="operation">The apply operation.</param>
        /// <returns>A new node representing the "Apply".</returns>
        public DDIndex Apply(DDIndex xid, BDDNode x, DDIndex yid, BDDNode y, DDOperation operation)
        {
            if (x.Variable < y.Variable)
            {
                var xlow = this.Manager.Apply(x.Low, yid, operation);
                var xhigh = this.Manager.Apply(x.High, yid, operation);
                return this.Manager.Allocate(new BDDNode(x.Variable, xlow, xhigh));
            }
            else if (y.Variable < x.Variable)
            {
                var ylow = this.Manager.Apply(y.Low, xid, operation);
                var yhigh = this.Manager.Apply(y.High, xid, operation);
                return this.Manager.Allocate(new BDDNode(y.Variable, ylow, yhigh));
            }
            else
            {
                var low = this.Manager.Apply(x.Low, y.Low, operation);
                var high = this.Manager.Apply(x.High, y.High, operation);
                return this.Manager.Allocate(new BDDNode(x.Variable, low, high));
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
        public DDIndex Ite(DDIndex fid, BDDNode f, DDIndex gid, BDDNode g, DDIndex hid, BDDNode h)
        {
            var flevel = Level(fid, f);
            var glevel = Level(gid, g);
            var hlevel = Level(hid, h);

            if (flevel == glevel)
            {
                if (flevel == hlevel)
                {
                    var x = this.Manager.Ite(f.Low, g.Low, h.Low);
                    var y = this.Manager.Ite(f.High, g.High, h.High);
                    return this.Manager.Allocate(new BDDNode(f.Variable, x, y));
                }
                else if (flevel < hlevel)
                {
                    var x = this.Manager.Ite(f.Low, g.Low, hid);
                    var y = this.Manager.Ite(f.High, g.High, hid);
                    return this.Manager.Allocate(new BDDNode(f.Variable, x, y));
                }
                else
                {
                    var x = this.Manager.Ite(fid, gid, h.Low);
                    var y = this.Manager.Ite(fid, gid, h.High);
                    return this.Manager.Allocate(new BDDNode(h.Variable, x, y));
                }
            }
            else if (flevel < glevel)
            {
                if (flevel == hlevel)
                {
                    var x = this.Manager.Ite(f.Low, gid, h.Low);
                    var y = this.Manager.Ite(f.High, gid, h.High);
                    return this.Manager.Allocate(new BDDNode(f.Variable, x, y));
                }
                else if (flevel < hlevel)
                {
                    var x = this.Manager.Ite(f.Low, gid, hid);
                    var y = this.Manager.Ite(f.High, gid, hid);
                    return this.Manager.Allocate(new BDDNode(f.Variable, x, y));
                }
                else
                {
                    var x = this.Manager.Ite(fid, gid, h.Low);
                    var y = this.Manager.Ite(fid, gid, h.High);
                    return this.Manager.Allocate(new BDDNode(h.Variable, x, y));
                }
            }
            else
            {
                if (glevel == hlevel)
                {
                    var x = this.Manager.Ite(fid, g.Low, h.Low);
                    var y = this.Manager.Ite(fid, g.High, h.High);
                    return this.Manager.Allocate(new BDDNode(g.Variable, x, y));
                }
                else if (glevel < hlevel)
                {
                    var x = this.Manager.Ite(fid, g.Low, hid);
                    var y = this.Manager.Ite(fid, g.High, hid);
                    return this.Manager.Allocate(new BDDNode(g.Variable, x, y));
                }
                else
                {
                    var x = this.Manager.Ite(fid, gid, h.Low);
                    var y = this.Manager.Ite(fid, gid, h.High);
                    return this.Manager.Allocate(new BDDNode(h.Variable, x, y));
                }
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
        public DDIndex Exists(DDIndex xid, BDDNode x, VariableSet<BDDNode> variables)
        {
            if (x.Variable > variables.MaxIndex)
            {
                return xid;
            }

            var lo = this.Manager.Exists(x.Low, variables);
            var hi = this.Manager.Exists(x.High, variables);
            if (variables.Contains(x.Variable))
            {
                return this.Manager.Or(lo, hi);
            }

            return this.Manager.Allocate(new BDDNode(x.Variable, lo, hi));
        }

        /// <summary>
        /// Implement a replacement operation that substitutes
        /// variables for other variables.
        /// </summary>
        /// <param name="xid">The left index.</param>
        /// <param name="x">The left node.</param>
        /// <param name="variableMap">The variable set.</param>
        /// <returns>A new formula with the susbtitution.</returns>
        public DDIndex Replace(DDIndex xid, BDDNode x, VariableMap<BDDNode> variableMap)
        {
            if (x.Variable > variableMap.MaxIndex)
            {
                return xid;
            }

            var lo = this.Manager.Replace(x.Low, variableMap);
            var hi = this.Manager.Replace(x.High, variableMap);

            var level = variableMap.Get(x.Variable);
            return RepairOrder(level, lo, hi);
        }

        /// <summary>
        /// Returns a new formula that repairs the order after a substitution.
        /// </summary>
        /// <param name="level">Variable level of the new node.</param>
        /// <param name="lo">The node's lo branch.</param>
        /// <param name="hi">The node's hi branch.</param>
        /// <returns></returns>
        private DDIndex RepairOrder(int level, DDIndex lo, DDIndex hi)
        {
            var loNode = this.Manager.MemoryPool[lo.GetPosition()];
            var hiNode = this.Manager.MemoryPool[hi.GetPosition()];

            loNode = lo.IsComplemented() ? Flip(loNode) : loNode;
            hiNode = hi.IsComplemented() ? Flip(hiNode) : hiNode;

            var loLevel = Level(lo, loNode);
            var hiLevel = Level(hi, hiNode);

            if (level < loLevel && level < hiLevel)
            {
                return this.Manager.Allocate(new BDDNode(level, lo, hi));
            }
            else if (loLevel < hiLevel)
            {
                var l = RepairOrder(level, loNode.Low, hi);
                var h = RepairOrder(level, loNode.High, hi);
                return this.Manager.Allocate(new BDDNode(loNode.Variable, l, h));
            }
            else if (loLevel > hiLevel)
            {
                var l = RepairOrder(level, lo, hiNode.Low);
                var h = RepairOrder(level, lo, hiNode.High);
                return this.Manager.Allocate(new BDDNode(hiNode.Variable, l, h));
            }
            else
            {
                var l = RepairOrder(level, loNode.Low, hiNode.Low);
                var h = RepairOrder(level, loNode.High, hiNode.High);
                return this.Manager.Allocate(new BDDNode(loNode.Variable, l, h));
            }
        }

        /// <summary>
        /// Create a new node with children flipped.
        /// </summary>
        /// <param name="node">The old node.</param>
        /// <returns>A copy of the node with the children flipped.</returns>
        public BDDNode Flip(BDDNode node)
        {
            return new BDDNode(node.Variable, node.Low.Flip(), node.High.Flip());
        }

        /// <summary>
        /// The identity node for a variable.
        /// </summary>
        /// <param name="variable">The variable index.</param>
        /// <returns>The identity node.</returns>
        public BDDNode Id(int variable)
        {
            return new BDDNode(variable, DDIndex.False, DDIndex.True);
        }

        /// <summary>
        /// Reduction rules for a BDD.
        /// </summary>
        /// <param name="node">The node to reduce.</param>
        /// <param name="result">The modified node.</param>
        /// <returns>If there was a reduction.</returns>
        public bool Reduce(BDDNode node, out DDIndex result)
        {
            result = DDIndex.False;
            if (node.Low.Equals(node.High))
            {
                result = node.Low;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The sat count for a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The number of satisfying assignments.</returns>
        public double SatCount(BDDNode node)
        {
            var loNode = this.Manager.MemoryPool[node.Low.GetPosition()];
            var hiNode = this.Manager.MemoryPool[node.High.GetPosition()];
            var loLevel = Level(node.Low, loNode);
            var hiLevel = Level(node.High, hiNode);
            var scaleLo = Math.Pow(2.0, loLevel - node.Variable - 1);
            var scaleHi = Math.Pow(2.0, hiLevel - node.Variable - 1);
            var countLo = this.Manager.SatCount(node.Low);
            var countHi = this.Manager.SatCount(node.High);
            return scaleLo * countLo + scaleHi * countHi;
        }

        /// <summary>
        /// How to display a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="negated">Parity of negation.</param>
        /// <returns>The string representation.</returns>
        public string Display(BDDNode node, bool negated)
        {
            return string.Format(
                "({0} ? {1} : {2})",
                node.Variable,
                this.Manager.Display(node.High, negated),
                this.Manager.Display(node.Low, negated));
        }

        /// <summary>
        /// Update an assignment to variables given an edge.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="hi">Which edge.</param>
        /// <param name="assignment">current assignment.</param>
        public void Sat(BDDNode node, bool hi, Dictionary<int, bool> assignment)
        {
            assignment.Add(node.Variable, hi);
        }

        /// <summary>
        /// Gets the "level" for the node, where the maximum
        /// value is used for constants.
        /// </summary>
        /// <param name="idx">The node index.</param>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public int Level(DDIndex idx, BDDNode node)
        {
            return idx.IsConstant() ? this.Manager.NumVariables + 1 : node.Variable;
        }
    }
}
