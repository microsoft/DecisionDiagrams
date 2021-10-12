// <copyright file="ZddTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagramTests
{
    using System.Diagnostics.CodeAnalysis;
    using DecisionDiagrams;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for the ZDD implementation.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class ZddTests : DiagramTests<BDDNode>
    {
        /// <summary>
        /// Initialize the test class.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.Factory = new BDDNodeFactory();
            this.Manager = new DDManager<BDDNode>(this.Factory, 16, gcMinCutoff: 4);
            this.BaseInitialize();
        }

        /// <summary>
        /// Test constant identities.
        /// </summary>
        [TestMethod]
        public void TestBasicReductions()
        {
            var factory = new ZDDNodeFactory();
            var manager = new DDManager<BDDNode>(factory, 16);
            var x = manager.CreateInt16();
            var constraint = x.Eq(54);
            var assignment = manager.Sat(constraint);
            Assert.AreEqual(assignment.Get(x), 54);
        }

        /// <summary>
        /// Test satisfiabililty with hidden nodes.
        /// </summary>
        [TestMethod]
        public void TestSatWithHiddenEdges()
        {
            var factory = new ZDDNodeFactory();
            var manager = new DDManager<BDDNode>(factory, 16);

            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var d = manager.CreateBool();

            var c1 = a.Id();
            var c2 = manager.Not(b.Id());
            var c3 = c.Id();
            var c4 = manager.Not(d.Id());

            var f = manager.And(c1, manager.And(c2, manager.And(c3, c4)));
            var assignment = manager.Sat(f);

            Assert.IsTrue(assignment.Get(a));
            Assert.IsFalse(assignment.Get(b));
            Assert.IsTrue(assignment.Get(c));
            Assert.IsFalse(assignment.Get(d));
        }
    }
}
