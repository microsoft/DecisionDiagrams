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
            this.QuantifiersSupported = true;
            this.ReplaceSupported = true;
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
    }
}
