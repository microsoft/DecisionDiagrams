// <copyright file="CbddTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagram.Tests
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
            this.BaseInitialize();
        }

        /// <summary>
        /// Test node count is correct.
        /// </summary>
        [TestMethod]
        public override void TestNodeCountCorrect()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();
            var dd = manager.Or(va.Id(), vb.Id());
            Assert.AreEqual(3, manager.NodeCount(dd));
        }

        /// <summary>
        /// Test conversion to a string.
        /// </summary>
        [TestMethod]
        public void TestDisplay()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();
            var dd = manager.Or(va.Id(), vb.Id());
            Assert.AreEqual(manager.Display(dd), "(1:2 ? true : false)");
        }
    }
}
