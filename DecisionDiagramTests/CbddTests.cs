// <copyright file="CbddTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagramTests
{
    using System.Diagnostics.CodeAnalysis;
    using DecisionDiagrams;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for the CBDD implementation.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class CbddTests : DiagramTests<CBDDNode>
    {
        /// <summary>
        /// Initialize the test class.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.Factory = new CBDDNodeFactory();
            this.Manager = new DDManager<CBDDNode>(this.Factory, 16, gcMinCutoff: 4);
            this.QuantifiersSupported = true;
            this.ReplaceSupported = false;
            this.BaseInitialize();
        }

        /// <summary>
        /// Test node count is correct.
        /// </summary>
        [TestMethod]
        public override void NodeCountCorrect()
        {
            var dd = this.Manager.Or(this.VarA, this.VarB);
            Assert.AreEqual(3, this.Manager.NodeCount(dd));
        }

        /// <summary>
        /// Test conversion to a string.
        /// </summary>
        [TestMethod]
        public void TestDisplay()
        {
            var dd = this.Manager.Or(this.VarA, this.VarB);
            Assert.AreEqual(this.Manager.Display(dd), "(0:2 ? true : false)");
        }
    }
}
