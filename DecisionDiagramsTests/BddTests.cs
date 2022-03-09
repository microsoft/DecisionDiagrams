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
            this.BaseInitialize();
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

            var dd = manager.Not(manager.And(va.Id(), vb.Id()));
            Assert.AreEqual(manager.Display(dd), "(1 ? (2 ? false : true) : true)");
        }
    }
}
