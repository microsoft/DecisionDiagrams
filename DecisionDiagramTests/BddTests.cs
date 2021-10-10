// <copyright file="BddTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagramTests
{
    using System.Diagnostics.CodeAnalysis;
    using DecisionDiagrams;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for binary decision diagrams.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class BddTests : DiagramTests<BDDNode>
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
        /// Test conversion to a string.
        /// </summary>
        [TestMethod]
        public void TestDisplay()
        {
            var dd = this.Manager.Not(this.Manager.And(this.VarA, this.VarB));
            Assert.AreEqual(this.Manager.Display(dd), "(0 ? (1 ? false : true) : true)");
        }
    }
}
