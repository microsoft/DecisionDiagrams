﻿// <copyright file="DiagramTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagramTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using DecisionDiagrams;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for decision diagrams.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    [ExcludeFromCodeCoverage]
    public class DiagramTests<T>
        where T : IDDNode
    {
        /// <summary>
        /// How many random inputs to generate per test.
        /// </summary>
        private static int numRandomTests = 2000;

        /// <summary>
        /// Gets or sets the decision diagram factory.
        /// </summary>
        internal IDDNodeFactory<T> Factory { get; set; }

        /// <summary>
        /// Gets or sets the manager.
        /// </summary>
        internal DDManager<T> Manager { get; set; }

        /// <summary>
        /// Gets or sets the zero value.
        /// </summary>
        internal DD Zero { get; set; }

        /// <summary>
        /// Gets or sets the one value.
        /// </summary>
        internal DD One { get; set; }

        /// <summary>
        /// Gets or sets var a.
        /// </summary>
        internal DD VarA { get; set; }

        /// <summary>
        /// Gets or sets var b.
        /// </summary>
        internal DD VarB { get; set; }

        /// <summary>
        /// Gets or sets var c.
        /// </summary>
        internal DD VarC { get; set; }

        /// <summary>
        /// Gets or sets var d.
        /// </summary>
        internal DD VarD { get; set; }

        /// <summary>
        /// Gets or sets the 8 bit var.
        /// </summary>
        internal DD Var8 { get; set; }

        /// <summary>
        /// Gets or sets the 16 bit var.
        /// </summary>
        internal DD Var16 { get; set; }

        /// <summary>
        /// Gets or sets the 32 bit var.
        /// </summary>
        internal DD Var32 { get; set; }

        /// <summary>
        /// Gets or sets the 64 bit var.
        /// </summary>
        internal DD Var64 { get; set; }

        /// <summary>
        /// Gets or sets the 128 bit var.
        /// </summary>
        internal DD Var128 { get; set; }

        /// <summary>
        /// Gets or sets va.
        /// </summary>
        internal VarBool<T> Va { get; set; }

        /// <summary>
        /// Gets or sets vb.
        /// </summary>
        internal VarBool<T> Vb { get; set; }

        /// <summary>
        /// Gets or sets vc.
        /// </summary>
        internal VarBool<T> Vc { get; set; }

        /// <summary>
        /// Gets or sets vd.
        /// </summary>
        internal VarBool<T> Vd { get; set; }

        /// <summary>
        /// Gets or sets v8.
        /// </summary>
        internal VarInt8<T> V8 { get; set; }

        /// <summary>
        /// Gets or sets v16.
        /// </summary>
        internal VarInt16<T> V16 { get; set; }

        /// <summary>
        /// Gets or sets v32.
        /// </summary>
        internal VarInt32<T> V32 { get; set; }

        /// <summary>
        /// Gets or sets v64.
        /// </summary>
        internal VarInt64<T> V64 { get; set; }

        /// <summary>
        /// Gets or sets v128.
        /// </summary>
        internal VarInt<T> V128 { get; set; }

        /// <summary>
        /// Gets or sets v9.
        /// </summary>
        internal VarInt<T> V9 { get; set; }

        /// <summary>
        /// Gets or sets interleaved v8.
        /// </summary>
        internal VarInt8<T>[] InterleavedInt8 { get; set; }

        /// <summary>
        /// Gets or sets interleaved v16.
        /// </summary>
        internal VarInt16<T>[] InterleavedInt16 { get; set; }

        /// <summary>
        /// Gets or sets interleaved v8.
        /// </summary>
        internal VarInt32<T>[] InterleavedInt32 { get; set; }

        /// <summary>
        /// Gets or sets interleaved v8.
        /// </summary>
        internal VarInt64<T>[] InterleavedInt64 { get; set; }

        /// <summary>
        /// Gets or sets interleaved vint.
        /// </summary>
        internal VarInt<T>[] InterleavedInt { get; set; }

        /// <summary>
        /// Gets or sets a collection or random variables.
        /// </summary>
        internal DD[] RandomLiterals { get; set; }

        /// <summary>
        /// Gets or setst he random number generator.
        /// </summary>
        internal Random Rnd { get; set; }

        /// <summary>
        /// Initialize the base class.
        /// </summary>
        public void BaseInitialize()
        {
            this.Zero = this.Manager.False();
            this.One = this.Manager.True();
            this.Va = this.Manager.CreateBool();
            this.Vb = this.Manager.CreateBool();
            this.Vc = this.Manager.CreateBool();
            this.Vd = this.Manager.CreateBool();
            this.V8 = this.Manager.CreateInt8(i => i);
            this.V16 = this.Manager.CreateInt16(i => i);
            this.V32 = this.Manager.CreateInt32(i => i);
            this.V64 = this.Manager.CreateInt64(i => i);
            this.V128 = this.Manager.CreateInt(128, i => i);
            this.V9 = this.Manager.CreateInt(9, i => 8 - i);
            this.InterleavedInt8 = this.Manager.CreateInterleavedInt8(3);
            this.InterleavedInt16 = this.Manager.CreateInterleavedInt16(3);
            this.InterleavedInt32 = this.Manager.CreateInterleavedInt32(3);
            this.InterleavedInt64 = this.Manager.CreateInterleavedInt64(3);
            this.InterleavedInt = this.Manager.CreateInterleavedInt(3, 10);

            this.VarA = this.Manager.Id(this.Va);
            this.VarB = this.Manager.Id(this.Vb);
            this.VarC = this.Manager.Id(this.Vc);
            this.VarD = this.Manager.Id(this.Vd);
            this.Var8 = this.V8.Eq(4);
            this.Var16 = this.V16.Eq(9);
            this.Var32 = this.V32.Eq(11);
            this.Var64 = this.V64.Eq(18);

            var values = new byte[16];
            values[15] = 3;
            this.Var128 = this.V128.Eq(values);

            this.RandomLiterals = new DD[8];
            for (int i = 0; i < 4; i++)
            {
                var v = this.Manager.CreateBool();
                this.RandomLiterals[i] = v.Id();
                this.RandomLiterals[i + 4] = this.Manager.Not(v.Id());
            }

            this.Rnd = new Random(7);
        }

        /// <summary>
        /// Test checking for constants.
        /// </summary>
        [TestMethod]
        public void TestConstants()
        {
            Assert.IsTrue(this.Zero.IsFalse());
            Assert.IsTrue(this.One.IsTrue());
            Assert.IsTrue(this.Zero.IsConstant());
            Assert.IsTrue(this.One.IsConstant());
        }

        /// <summary>
        /// Test for basic logical identities.
        /// </summary>
        [TestMethod]
        public void TestIdentities()
        {
            Assert.AreEqual(this.Zero, this.Manager.And(this.Zero, this.VarA));
            Assert.AreEqual(this.Zero, this.Manager.And(this.VarA, this.Zero));
            Assert.AreEqual(this.VarA, this.Manager.Or(this.Zero, this.VarA));
            Assert.AreEqual(this.VarA, this.Manager.Or(this.VarA, this.Zero));
            Assert.AreEqual(this.VarA, this.Manager.And(this.One, this.VarA));
            Assert.AreEqual(this.VarA, this.Manager.And(this.VarA, this.One));
            Assert.AreEqual(this.One, this.Manager.Or(this.One, this.VarA));
            Assert.AreEqual(this.One, this.Manager.Or(this.VarA, this.One));
        }

        /// <summary>
        /// Check idempotence of And and Or.
        /// </summary>
        [TestMethod]
        public void TestIdempotence()
        {
            this.RandomTest((a, b) => Assert.AreEqual(a, this.Manager.Or(a, a)));
        }

        /// <summary>
        /// Test commutativity of and.
        /// </summary>
        [TestMethod]
        public void TestCommutativityAnd()
        {
            this.RandomTest((a, b) => Assert.AreEqual(this.Manager.And(a, b), this.Manager.And(b, a)));
        }

        /// <summary>
        /// Test commutativity of or.
        /// </summary>
        [TestMethod]
        public void TestCommutativityOr()
        {
            this.RandomTest((a, b) => Assert.AreEqual(this.Manager.Or(a, b), this.Manager.Or(b, a)));
        }

        /// <summary>
        /// Test distributivity of and + or.
        /// </summary>
        [TestMethod]
        public void TestDistributivity1()
        {
            this.RandomTest((a, b, c) =>
            {
                var x = this.Manager.And(a, this.Manager.Or(b, c));
                var y = this.Manager.Or(this.Manager.And(a, b), this.Manager.And(a, c));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test double negation does nothing.
        /// </summary>
        [TestMethod]
        public void TestNegationIdempotence()
        {
            this.RandomTest((a) =>
            {
                Assert.AreEqual(a, this.Manager.Not(this.Manager.Not(a)));
            });
        }

        /// <summary>
        /// Test negation for constants.
        /// </summary>
        [TestMethod]
        public void TestNegationConstant()
        {
            Assert.AreEqual(this.Zero, this.Manager.Not(this.One));
            Assert.AreEqual(this.One, this.Manager.Not(this.Zero));
        }

        /// <summary>
        /// Test DeMorgan's equivalence.
        /// </summary>
        [TestMethod]
        public void TestDeMorgan()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Not(this.Manager.And(a, b));
                var y = this.Manager.Or(this.Manager.Not(a), this.Manager.Not(b));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test DeMorgan's equivalence.
        /// </summary>
        [TestMethod]
        public void TestIff()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Iff(a, b);
                var y = this.Manager.Iff(b, a);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test DeMorgan's equivalence.
        /// </summary>
        [TestMethod]
        public void TestImplies()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Implies(a, b);
                var y = this.Manager.Implies(this.Manager.Not(b), this.Manager.Not(a));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv1()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Ite(a, b, this.Manager.False());
                var y = this.Manager.And(a, b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite works with first variable index and constant.
        /// </summary>
        [TestMethod]
        public void TestIteBasic1()
        {
            var manager = new DDManager<T>(this.Factory);
            var a = manager.CreateBool().Id();
            var result = manager.Ite(a, a, manager.False());
            Assert.AreEqual(a, result);
        }

        /// <summary>
        /// Test ite works with first variable index and constant.
        /// </summary>
        [TestMethod]
        public void TestIteBasic2()
        {
            var manager = new DDManager<T>(this.Factory);
            var a = manager.CreateBool().Id();
            var result = manager.Ite(a, manager.False(), manager.Not(a));
            Assert.AreEqual(manager.Not(a), result);
        }

        /// <summary>
        /// Test ite works with first variable index and constant.
        /// </summary>
        [TestMethod]
        public void TestIteBasic3()
        {
            var manager = new DDManager<T>(this.Factory);
            var a = manager.CreateBool().Id();
            var b = manager.CreateBool().Id();
            var result = manager.Ite(a, b, manager.False());
            Assert.AreEqual(manager.And(a, b), result);
        }

        /// <summary>
        /// Test ite works with first variable index and constant.
        /// </summary>
        [TestMethod]
        public void TestIteBasic4()
        {
            var manager = new DDManager<T>(this.Factory);
            var a = manager.CreateBool().Id();
            var b = manager.CreateBool().Id();
            var result = manager.Ite(a, manager.False(), b);
            Assert.AreEqual(manager.And(manager.Not(a), b), result);
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteExpanded()
        {
            this.RandomTest((a, b, c) =>
            {
                var x = this.Manager.Ite(a, b, c);
                var y = this.Manager.And(
                    this.Manager.Implies(a, b),
                    this.Manager.Implies(this.Manager.Not(a), c));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv2()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Ite(a, this.Manager.Not(b), this.Manager.False());
                var y = this.Manager.And(a, this.Manager.Not(b));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv3()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Ite(a, this.Manager.False(), b);
                var y = this.Manager.And(this.Manager.Not(a), b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv4()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Ite(a, this.Manager.Not(b), b);
                var y = this.Manager.Or(
                    this.Manager.And(a, this.Manager.Not(b)),
                    this.Manager.And(this.Manager.Not(a), b));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv5()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Ite(a, this.Manager.True(), b);
                var y = this.Manager.Or(a, b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv6()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Ite(a, this.Manager.False(), this.Manager.True());
                var y = this.Manager.Not(a);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv7()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Ite(a, this.Manager.True(), this.Manager.False());
                Assert.AreEqual(x, a);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv8()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Ite(a, b, this.Manager.True());
                var y = this.Manager.Implies(a, b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv9()
        {
            this.RandomTest((a, b) =>
            {
                var x = this.Manager.Ite(a, b, this.Manager.True());
                var y = this.Manager.Implies(a, b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test existential quantification.
        /// </summary>
        [TestMethod]
        public void TestExistentialQuantification()
        {
            this.RandomTest((a, b) =>
            {
                var without = this.Manager.And(a, b);
                var all = this.Manager.And(without, this.Manager.And(this.VarA, this.VarB));
                var variableSet = this.Manager.CreateVariableSet(new Variable<T>[] { this.Va, this.Vb });
                var project = this.Manager.Exists(all, variableSet);
                Assert.AreEqual(without, project);
            });
        }

        /// <summary>
        /// Test universal quantification.
        /// </summary>
        [TestMethod]
        public void TestForallQuantification()
        {
            var bc = this.Manager.Or(this.VarB, this.VarC);
            var x = this.Manager.Ite(this.VarA, bc, this.Var16);
            var variableSet = this.Manager.CreateVariableSet(new Variable<T>[] { this.Va });
            x = this.Manager.Forall(x, variableSet);
            var y = this.Manager.And(bc, this.Var16);
            Assert.AreEqual(x, y);
        }

        /// <summary>
        /// Test variable equality.
        /// </summary>
        [TestMethod]
        public void TestVariableEquals()
        {
            Assert.AreEqual(this.Vb, this.Vb);
            Assert.AreEqual(this.V16, this.V16);
            Assert.AreEqual(this.V32, this.V32);
        }

        /// <summary>
        /// Test the hash code.
        /// </summary>
        [TestMethod]
        public void TestHashEquals()
        {
            var x = this.Manager.And(this.VarA, this.VarB);
            var y = this.Manager.And(this.VarB, this.VarA);
            var z = this.Manager.And(this.VarA, this.VarC);
            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
            Assert.AreNotEqual(x.GetHashCode(), z.GetHashCode());
            Assert.IsTrue(x.Equals(y));
            Assert.IsFalse(x.Equals(z));
            Assert.IsFalse(x.Equals(0));
        }

        /// <summary>
        /// Test the hash code is preserved by garbage collection.
        /// </summary>
        [TestMethod]
        public void TestGarbageCollectionPreservesHashcode()
        {
            CreateGarbage();

            var x = this.Manager.And(this.VarA, this.VarB);

            GC.Collect();
            this.Manager.GarbageCollect();

            var y = this.Manager.And(this.VarA, this.VarB);

            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
            Assert.IsTrue(x.Equals(y));
        }

        /// <summary>
        /// Helper function to create garbage in a new scope.
        /// </summary>
        private void CreateGarbage()
        {
            for (int i = 0; i < 100; i++)
            {
                this.Manager.Or(this.RandomDD(), this.RandomDD());
            }
        }

        /// <summary>
        /// Test that garbage collection updates Ids.
        /// </summary>
        [TestMethod]
        public void TestGarbageCollection()
        {
            var foo1 = this.Manager.And(this.Manager.Or(this.VarA, this.VarB), this.VarD);
            var bar = this.Manager.Or(this.VarA, this.VarD);
            bar = null;
            GC.Collect();
            this.Manager.GarbageCollect();
            var foo2 = this.Manager.And(this.VarD, this.Manager.Or(this.VarA, this.VarB));
            Assert.AreEqual(foo1, foo2);
        }

        /// <summary>
        /// Test variable equality constraint.
        /// </summary>
        [TestMethod]
        public void TestVariableEquality()
        {
            var x = this.InterleavedInt32[0];
            var y = this.InterleavedInt32[1];
            var inv = this.Manager.And(x.Eq(y), x.LessOrEqual(10));
            Assignment<T> assignment = this.Manager.Sat(inv);
            int xvalue = assignment.Get(x);
            int yvalue = assignment.Get(y);
            Assert.AreEqual(xvalue, yvalue);
            Assert.IsTrue(xvalue <= 10);
        }

        /// <summary>
        /// Test variable equality invalid.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestVariableEqualityInvalid()
        {
            this.V32.Eq(this.V16);
        }

        /// <summary>
        /// Test satisfiability returns the right values.
        /// </summary>
        [TestMethod]
        public void TestSatisfiabilityCorrect()
        {
            var manager = new DDManager<T>(this.Factory, 16);

            var variables = new VarBool<T>[6];
            for (int i = 0; i < 6; i++)
            {
                variables[i] = manager.CreateBool();
            }

            for (int i = 0; i < numRandomTests; i++)
            {
                var dd = manager.True();
                var expected = new bool[6];
                for (int j = 0; j < 6; j++)
                {
                    if (this.Rnd.Next(0, 2) == 0)
                    {
                        expected[j] = true;
                        dd = manager.And(dd, variables[j].Id());
                    }
                    else
                    {
                        expected[j] = false;
                        dd = manager.And(dd, manager.Not(variables[j].Id()));
                    }
                }

                var assignment = manager.Sat(dd);

                for (int j = 0; j < 6; j++)
                {
                    Assert.AreEqual(expected[j], assignment.Get(variables[j]), $"{i}");
                }
            }
        }

        /// <summary>
        /// Test satisfiability works with integer values.
        /// </summary>
        [TestMethod]
        public void TestSatisfiabilityWithIntegers()
        {
            var all = this.Manager.And(this.Var8, this.Manager.And(this.Var16, this.Manager.And(this.Var32, this.Manager.And(this.Var64, this.Var128))));
            Assignment<T> assignment = this.Manager.Sat(all);
            byte r8 = assignment.Get(this.V8);
            short r16 = assignment.Get(this.V16);
            int r32 = assignment.Get(this.V32);
            long r64 = assignment.Get(this.V64);
            byte[] r128 = assignment.Get(this.V128);
            Assert.AreEqual(r8, 4);
            Assert.AreEqual(r16, 9);
            Assert.AreEqual(r32, 11);
            Assert.AreEqual(r64, 18);
            Assert.AreEqual(r128[15], 3);
        }

        /// <summary>
        /// Test satisfiability for non-power-of-two bits.
        /// </summary>
        [TestMethod]
        public void TestSatisfiabilityOddBits()
        {
            var x = this.V9.Eq(new byte[2] { 1, 128 });
            Assignment<T> assignment = this.Manager.Sat(x);
            byte[] r9 = assignment.Get(this.V9);
            Assert.AreEqual(r9[0], 1);
            Assert.AreEqual(r9[1], 128);
        }

        /// <summary>
        /// Test satisfiability for a subset of variables.
        /// </summary>
        [TestMethod]
        public void TestSatisfiabilityForSubsetOfVariables()
        {
            var manager = new DDManager<T>(this.Factory);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateInt8();
            var f = manager.And(a.Id(), manager.And(b.Id(), c.Eq(1)));
            Assignment<T> assignment = manager.Sat(f, new List<Variable<T>> { a, c });

            Assert.AreNotEqual(null, assignment);
            Assert.AreEqual(true, assignment.Get(a));
            Assert.AreEqual(1, assignment.Get(c));
        }

        /// <summary>
        /// Test satisfiability for integer variables.
        /// </summary>
        [TestMethod]
        public void TestSatisfiabilityForIntegers()
        {
            var manager = new DDManager<T>(this.Factory);
            var x = manager.CreateInt32();
            var y = manager.CreateInt32();

            var rnd = new Random(0);
            for (int i = 0; i < numRandomTests; i++)
            {
                var vx = rnd.Next();
                var vy = rnd.Next();
                var f = manager.And(x.Eq(vx), y.Eq(vy));
                var assignment = manager.Sat(f);

                Assert.AreEqual(vx, assignment.Get(x));
                Assert.AreEqual(vy, assignment.Get(y));
            }
        }

        /// <summary>
        /// Test satisfiability for integer variables.
        /// </summary>
        [TestMethod]
        public void TestSatisfiabilityForIntegersRange()
        {
            var manager = new DDManager<T>(this.Factory);
            var x = manager.CreateInt32();

            var rnd = new Random(0);
            for (int i = 0; i < numRandomTests; i++)
            {
                var vxl = rnd.Next(0, 9);
                var vxh = rnd.Next(0, 9);

                if (vxl > vxh)
                {
                    var temp = vxl;
                    vxl = vxh;
                    vxh = temp;
                }

                var f = manager.And(x.LessOrEqual(vxh), x.GreaterOrEqual(vxl));
                var assignment = manager.Sat(f);

                Assert.AreEqual(vxl, assignment.Get(x), $"{i}");
            }
        }

        /// <summary>
        /// Test satisfiability for a subset of variables.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSatisfiabilityForSubsetOfVariablesException()
        {
            var manager = new DDManager<T>(this.Factory);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var f = manager.And(a.Id(), b.Id());
            Assignment<T> assignment = manager.Sat(f, new List<Variable<T>> { a });
            assignment.Get(b);
        }

        /// <summary>
        /// Test satisfiability of false.
        /// </summary>
        [TestMethod]
        public void TestSatisfiabilityFalse()
        {
            Assignment<T> assignment = this.Manager.Sat(this.Zero);
            Assert.AreEqual(null, assignment);
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException1()
        {
            Assignment<T> assignment = this.Manager.Sat(this.One);
            assignment.Get(this.Manager.CreateBool());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException8()
        {
            Assignment<T> assignment = this.Manager.Sat(this.One);
            assignment.Get(this.Manager.CreateInt8());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException16()
        {
            Assignment<T> assignment = this.Manager.Sat(this.One);
            assignment.Get(this.Manager.CreateInt16());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException32()
        {
            Assignment<T> assignment = this.Manager.Sat(this.One);
            assignment.Get(this.Manager.CreateInt32());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException64()
        {
            Assignment<T> assignment = this.Manager.Sat(this.One);
            assignment.Get(this.Manager.CreateInt64());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException128()
        {
            Assignment<T> assignment = this.Manager.Sat(this.One);
            assignment.Get(this.Manager.CreateInt(128));
        }

        /// <summary>
        /// Test invalid ordering, out of bounds.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestInvalidOrdering()
        {
            this.Manager.CreateInt8(i => i + 1);
        }

        /// <summary>
        /// Test invalid ordering, duplicate target.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestDuplicateInOrdering()
        {
            this.Manager.CreateInt8(i => i % 4);
        }

        /// <summary>
        /// Test inequalities with random tests.
        /// </summary>
        [TestMethod]
        public void TestInequalities()
        {
            for (int i = 0; i < numRandomTests; i++)
            {
                int lower = this.Rnd.Next(1, 15);
                int upper = this.Rnd.Next(16, 30);

                byte lower8 = (byte)this.Rnd.Next(0, 3);
                byte upper8 = (byte)this.Rnd.Next(4, 7);

                byte[] lower128 = new byte[16];
                byte[] upper128 = new byte[16];
                lower128[15] = lower8;
                upper128[15] = upper8;

                var bounds8 = this.Manager.And(this.V8.GreaterOrEqual(lower8), this.V8.LessOrEqual(upper8));
                var bounds16 = this.Manager.And(this.V16.GreaterOrEqual((short)lower), this.V16.LessOrEqual((short)upper));
                var bounds32 = this.Manager.And(this.V32.GreaterOrEqual(lower), this.V32.LessOrEqual(upper));
                var bounds64 = this.Manager.And(this.V64.GreaterOrEqual((long)lower), this.V64.LessOrEqual((long)upper));
                var bounds128 = this.Manager.And(this.V128.GreaterOrEqual(lower128), this.V128.LessOrEqual(upper128));

                var assignment8 = this.Manager.Sat(bounds8);
                var assignment16 = this.Manager.Sat(bounds16);
                var assignment32 = this.Manager.Sat(bounds32);
                var assignment64 = this.Manager.Sat(bounds64);
                var assignment128 = this.Manager.Sat(bounds128);

                var r8 = assignment8.Get(this.V8);
                var r16 = assignment16.Get(this.V16);
                var r32 = assignment32.Get(this.V32);
                var r64 = assignment64.Get(this.V64);
                var r128 = assignment128.Get(this.V128);

                Assert.IsTrue(r8 >= lower8);
                Assert.IsTrue(r8 <= upper8);
                Assert.IsTrue(r16 >= lower);
                Assert.IsTrue(r16 <= upper);
                Assert.IsTrue(r32 >= lower);
                Assert.IsTrue(r32 <= upper);
                Assert.IsTrue(r64 >= lower);
                Assert.IsTrue(r64 <= upper);
                Assert.IsTrue(r128[15] >= lower128[15]);
                Assert.IsTrue(r128[15] <= upper128[15]);

                for (int j = 0; j <= 14; j++)
                {
                    Assert.AreEqual(r128[j], 0);
                }
            }
        }

        /// <summary>
        /// Test 128-bit inequalities with random tests.
        /// </summary>
        [TestMethod]
        public void TestInteger128()
        {
            for (int i = 0; i < numRandomTests; i++)
            {
                byte[] bytes = new byte[16];
                this.Rnd.NextBytes(bytes);

                var f = this.V128.Eq(bytes);
                var assignment = this.Manager.Sat(f);
                var r128 = assignment.Get(this.V128);

                for (int j = 0; j <= 15; j++)
                {
                    Assert.AreEqual(bytes[j], r128[j], $"failed on: {j}");
                }
            }
        }

        /// <summary>
        /// Test DeMorgan's equivalence with random tests.
        /// </summary>
        [TestMethod]
        public void TestInterleavedVariables()
        {
            for (int i = 0; i < numRandomTests; i++)
            {
                byte b1 = (byte)this.Rnd.Next(0, 20);
                byte b2 = (byte)this.Rnd.Next(0, 20);
                byte b3 = (byte)this.Rnd.Next(0, 20);

                short s1 = (short)this.Rnd.Next(0, 20);
                short s2 = (short)this.Rnd.Next(0, 20);
                short s3 = (short)this.Rnd.Next(0, 20);

                int i1 = (short)this.Rnd.Next(0, 20);
                int i2 = (short)this.Rnd.Next(0, 20);
                int i3 = (short)this.Rnd.Next(0, 20);

                long l1 = (long)this.Rnd.Next(0, 20);
                long l2 = (long)this.Rnd.Next(0, 20);
                long l3 = (long)this.Rnd.Next(0, 20);

                var x = this.Manager.True();

                x = this.Manager.And(x, this.InterleavedInt8[0].Eq(b1));
                x = this.Manager.And(x, this.InterleavedInt8[1].Eq(b2));
                x = this.Manager.And(x, this.InterleavedInt8[2].Eq(b3));

                x = this.Manager.And(x, this.InterleavedInt16[0].Eq(s1));
                x = this.Manager.And(x, this.InterleavedInt16[1].Eq(s2));
                x = this.Manager.And(x, this.InterleavedInt16[2].Eq(s3));

                x = this.Manager.And(x, this.InterleavedInt32[0].Eq(i1));
                x = this.Manager.And(x, this.InterleavedInt32[1].Eq(i2));
                x = this.Manager.And(x, this.InterleavedInt32[2].Eq(i3));

                x = this.Manager.And(x, this.InterleavedInt64[0].Eq(l1));
                x = this.Manager.And(x, this.InterleavedInt64[1].Eq(l2));
                x = this.Manager.And(x, this.InterleavedInt64[2].Eq(l3));

                x = this.Manager.And(x, this.InterleavedInt[0].Eq(new byte[2] { b1, 0 }));
                x = this.Manager.And(x, this.InterleavedInt[1].Eq(new byte[2] { b2, 0 }));
                x = this.Manager.And(x, this.InterleavedInt[2].Eq(new byte[2] { b3, 0 }));

                var assignment = this.Manager.Sat(x);

                Assert.IsTrue(assignment.Get(this.InterleavedInt8[0]) == b1);
                Assert.IsTrue(assignment.Get(this.InterleavedInt8[1]) == b2);
                Assert.IsTrue(assignment.Get(this.InterleavedInt8[2]) == b3);

                Assert.IsTrue(assignment.Get(this.InterleavedInt16[0]) == s1);
                Assert.IsTrue(assignment.Get(this.InterleavedInt16[1]) == s2);
                Assert.IsTrue(assignment.Get(this.InterleavedInt16[2]) == s3);

                Assert.IsTrue(assignment.Get(this.InterleavedInt32[0]) == i1);
                Assert.IsTrue(assignment.Get(this.InterleavedInt32[1]) == i2);
                Assert.IsTrue(assignment.Get(this.InterleavedInt32[2]) == i3);

                Assert.IsTrue(assignment.Get(this.InterleavedInt64[0]) == l1);
                Assert.IsTrue(assignment.Get(this.InterleavedInt64[1]) == l2);
                Assert.IsTrue(assignment.Get(this.InterleavedInt64[2]) == l3);

                Assert.IsTrue(assignment.Get(this.InterleavedInt[0])[0] == b1);
                Assert.IsTrue(assignment.Get(this.InterleavedInt[1])[0] == b2);
                Assert.IsTrue(assignment.Get(this.InterleavedInt[2])[0] == b3);
            }
        }

        /// <summary>
        /// Test DeMorgan's equivalence with random tests.
        /// </summary>
        [TestMethod]
        public void TestOrEqualities()
        {
            for (int i = 0; i < numRandomTests; i++)
            {
                byte val1 = (byte)this.Rnd.Next(0, 255);
                byte val2 = (byte)this.Rnd.Next(0, 255);
                byte val3 = (byte)this.Rnd.Next(0, 255);
                byte val4 = (byte)this.Rnd.Next(0, 255);

                var dd = this.V8.Eq(val1);
                dd = this.Manager.Or(dd, this.V8.Eq(val2));
                dd = this.Manager.Or(dd, this.V8.Eq(val3));
                dd = this.Manager.Or(dd, this.V8.Eq(val4));

                var assignment = this.Manager.Sat(dd);
                var v = assignment.Get(this.V8);

                Assert.IsTrue(v == val1 || v == val2 || v == val3 || v == val4);

                dd = this.V8.Eq(val4);
                dd = this.Manager.Or(dd, this.V8.Eq(val3));
                dd = this.Manager.Or(dd, this.V8.Eq(val2));
                dd = this.Manager.Or(dd, this.V8.Eq(val1));

                assignment = this.Manager.Sat(dd);
                v = assignment.Get(this.V8);

                Assert.IsTrue(v == val1 || v == val2 || v == val3 || v == val4);
            }
        }

        /// <summary>
        /// Test bitvector addition.
        /// </summary>
        [TestMethod]
        public void TestBitvectorAddition()
        {
            var bv8 = this.Manager.CreateBitvector(this.V8);
            var bv16 = this.Manager.CreateBitvector(this.V16);
            var bv32 = this.Manager.CreateBitvector(this.V32);
            var bv64 = this.Manager.CreateBitvector(this.V64);

            for (int i = 0; i < numRandomTests; i++)
            {
                var b1 = (byte)this.Rnd.Next(0, 20);
                var b2 = (byte)this.Rnd.Next(0, 20);
                var s1 = (short)this.Rnd.Next(0, 20);
                var s2 = (short)this.Rnd.Next(0, 20);
                var us1 = (ushort)this.Rnd.Next(0, 20);
                var us2 = (ushort)this.Rnd.Next(0, 20);
                var i1 = (int)this.Rnd.Next(0, 20);
                var i2 = (int)this.Rnd.Next(0, 20);
                var ui1 = (uint)this.Rnd.Next(0, 20);
                var ui2 = (uint)this.Rnd.Next(0, 20);
                var l1 = (long)this.Rnd.Next(0, 20);
                var l2 = (long)this.Rnd.Next(0, 20);
                var ul1 = (ulong)this.Rnd.Next(0, 20);
                var ul2 = (ulong)this.Rnd.Next(0, 20);

                var bv1 = this.Manager.CreateBitvector(b1);
                var bv2 = this.Manager.CreateBitvector(b2);
                var sum = this.Manager.Add(bv1, bv2);
                var eq = this.Manager.Eq(bv8, sum);
                Assert.AreEqual(b1 + b2, this.Manager.Sat(eq).Get(this.V8));

                bv1 = this.Manager.CreateBitvector(s1);
                bv2 = this.Manager.CreateBitvector(s2);
                sum = this.Manager.Add(bv1, bv2);
                eq = this.Manager.Eq(bv16, sum);
                Assert.AreEqual(s1 + s2, this.Manager.Sat(eq).Get(this.V16));

                bv1 = this.Manager.CreateBitvector(us1);
                bv2 = this.Manager.CreateBitvector(us2);
                sum = this.Manager.Add(bv1, bv2);
                eq = this.Manager.Eq(bv16, sum);
                Assert.AreEqual(us1 + us2, this.Manager.Sat(eq).Get(this.V16));

                bv1 = this.Manager.CreateBitvector(i1);
                bv2 = this.Manager.CreateBitvector(i2);
                sum = this.Manager.Add(bv1, bv2);
                eq = this.Manager.Eq(bv32, sum);
                Assert.AreEqual(i1 + i2, this.Manager.Sat(eq).Get(this.V32));

                bv1 = this.Manager.CreateBitvector(ui1);
                bv2 = this.Manager.CreateBitvector(ui2);
                sum = this.Manager.Add(bv1, bv2);
                eq = this.Manager.Eq(bv32, sum);
                Assert.AreEqual(ui1 + ui2, (uint)this.Manager.Sat(eq).Get(this.V32));

                bv1 = this.Manager.CreateBitvector(l1);
                bv2 = this.Manager.CreateBitvector(l2);
                sum = this.Manager.Add(bv1, bv2);
                eq = this.Manager.Eq(bv64, sum);
                Assert.AreEqual(l1 + l2, this.Manager.Sat(eq).Get(this.V64));

                bv1 = this.Manager.CreateBitvector(ul1);
                bv2 = this.Manager.CreateBitvector(ul2);
                sum = this.Manager.Add(bv1, bv2);
                eq = this.Manager.Eq(bv64, sum);
                Assert.AreEqual(ul1 + ul2, (ulong)this.Manager.Sat(eq).Get(this.V64));
            }
        }

        /// <summary>
        /// Test bitvector subtraction.
        /// </summary>
        [TestMethod]
        public void TestBitvectorSubtraction()
        {
            var bv8 = this.Manager.CreateBitvector(this.V8);
            var bv16 = this.Manager.CreateBitvector(this.V16);
            var bv32 = this.Manager.CreateBitvector(this.V32);
            var bv64 = this.Manager.CreateBitvector(this.V64);

            for (int i = 0; i < numRandomTests; i++)
            {
                var b1 = (byte)this.Rnd.Next(0, 20);
                var b2 = (byte)this.Rnd.Next(21, 40);
                var s1 = (short)this.Rnd.Next(0, 20);
                var s2 = (short)this.Rnd.Next(21, 40);
                var i1 = (int)this.Rnd.Next(0, 20);
                var i2 = (int)this.Rnd.Next(21, 40);
                var l1 = (long)this.Rnd.Next(0, 20);
                var l2 = (long)this.Rnd.Next(21, 40);

                var bv1 = this.Manager.CreateBitvector(b1);
                var bv2 = this.Manager.CreateBitvector(b2);
                var sum = this.Manager.Subtract(bv2, bv1);
                var eq = this.Manager.Eq(bv8, sum);
                Assert.AreEqual(b2 - b1, this.Manager.Sat(eq).Get(this.V8));

                bv1 = this.Manager.CreateBitvector(s1);
                bv2 = this.Manager.CreateBitvector(s2);
                sum = this.Manager.Subtract(bv2, bv1);
                eq = this.Manager.Eq(bv16, sum);
                Assert.AreEqual(s2 - s1, this.Manager.Sat(eq).Get(this.V16));

                bv1 = this.Manager.CreateBitvector(i1);
                bv2 = this.Manager.CreateBitvector(i2);
                sum = this.Manager.Subtract(bv2, bv1);
                eq = this.Manager.Eq(bv32, sum);
                Assert.AreEqual(i2 - i1, this.Manager.Sat(eq).Get(this.V32));

                bv1 = this.Manager.CreateBitvector(l1);
                bv2 = this.Manager.CreateBitvector(l2);
                sum = this.Manager.Subtract(bv2, bv1);
                eq = this.Manager.Eq(bv64, sum);
                Assert.AreEqual(l2 - l1, this.Manager.Sat(eq).Get(this.V64));
            }
        }

        /// <summary>
        /// Test bitvector operations work with signed values.
        /// </summary>
        [TestMethod]
        public void TestBitvectorSigned()
        {
            var x = this.Manager.CreateBitvector(this.V32);
            var y = this.Manager.CreateBitvector(-10);
            var z = this.Manager.CreateBitvector(-20);

            var a = this.Manager.LessOrEqualSigned(x, y);
            var b = this.Manager.And(a, this.Manager.GreaterOrEqualSigned(x, z));

            Assert.AreEqual(int.MinValue, this.Manager.Sat(a).Get(this.V32));
            Assert.AreEqual(-20, this.Manager.Sat(b).Get(this.V32));
        }

        /// <summary>
        /// Test bitvector less than or equal.
        /// </summary>
        [TestMethod]
        public void TestBitvectorInequalities()
        {
            for (int i = 0; i < numRandomTests; i++)
            {
                var b1 = (byte)this.Rnd.Next(0, 20);
                var b2 = (byte)this.Rnd.Next(21, 40);
                var s1 = (short)this.Rnd.Next(0, 20);
                var s2 = (short)this.Rnd.Next(21, 40);
                var i1 = (int)this.Rnd.Next(0, 20);
                var i2 = (int)this.Rnd.Next(21, 40);
                var l1 = (long)this.Rnd.Next(0, 20);
                var l2 = (long)this.Rnd.Next(21, 40);

                var bv1 = this.Manager.CreateBitvector(b1);
                var bv2 = this.Manager.CreateBitvector(b2);
                Assert.AreEqual(this.Manager.True(), this.Manager.LessOrEqual(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.LessOrEqualSigned(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.Less(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.GreaterOrEqual(bv2, bv1));
                Assert.AreEqual(this.Manager.True(), this.Manager.GreaterOrEqualSigned(bv2, bv1));
                Assert.AreEqual(this.Manager.True(), this.Manager.Greater(bv2, bv1));

                bv1 = this.Manager.CreateBitvector(s1);
                bv2 = this.Manager.CreateBitvector(s2);
                Assert.AreEqual(this.Manager.True(), this.Manager.LessOrEqual(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.LessOrEqualSigned(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.Less(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.GreaterOrEqual(bv2, bv1));
                Assert.AreEqual(this.Manager.True(), this.Manager.GreaterOrEqualSigned(bv2, bv1));
                Assert.AreEqual(this.Manager.True(), this.Manager.Greater(bv2, bv1));

                bv1 = this.Manager.CreateBitvector(i1);
                bv2 = this.Manager.CreateBitvector(i2);
                Assert.AreEqual(this.Manager.True(), this.Manager.LessOrEqual(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.LessOrEqualSigned(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.Less(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.GreaterOrEqual(bv2, bv1));
                Assert.AreEqual(this.Manager.True(), this.Manager.GreaterOrEqualSigned(bv2, bv1));
                Assert.AreEqual(this.Manager.True(), this.Manager.Greater(bv2, bv1));

                bv1 = this.Manager.CreateBitvector(l1);
                bv2 = this.Manager.CreateBitvector(l2);
                Assert.AreEqual(this.Manager.True(), this.Manager.LessOrEqual(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.LessOrEqualSigned(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.Less(bv1, bv2));
                Assert.AreEqual(this.Manager.True(), this.Manager.GreaterOrEqual(bv2, bv1));
                Assert.AreEqual(this.Manager.True(), this.Manager.GreaterOrEqualSigned(bv2, bv1));
                Assert.AreEqual(this.Manager.True(), this.Manager.Greater(bv2, bv1));
            }
        }

        /// <summary>
        /// Test variable to domain.
        /// </summary>
        [TestMethod]
        public void TestVariableToDomain()
        {
            var domain = this.V8.CreateDomain();
            var bits = domain.GetBits();
            Assert.AreEqual(8, bits.Length);
        }

        /// <summary>
        /// Test least significant bit first.
        /// </summary>
        [TestMethod]
        public void TestLeastSignificantBitFirst()
        {
            var v = this.Manager.CreateInt8(Variable<T>.BitOrder.LSB_FIRST);
            var dd = v.Eq(4);
            var assignment = this.Manager.Sat(dd);
            Assert.AreEqual((byte)4, assignment.Get(v));
        }

        /// <summary>
        /// Test node count is correct.
        /// </summary>
        [TestMethod]
        public virtual void TestNodeCountCorrect()
        {
            var dd = this.Manager.Or(this.VarA, this.VarB);
            Assert.AreEqual(4, this.Manager.NodeCount(dd));
        }

        /// <summary>
        /// Test invalid to use different length bitvectors.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestMismatchedBitvectorSizes()
        {
            var bv1 = this.Manager.CreateBitvector((byte)3);
            var bv2 = this.Manager.CreateBitvector((short)3);
            this.Manager.Greater(bv1, bv2);
        }

        /// <summary>
        /// Test invalid to use different length bitvectors.
        /// </summary>
        [TestMethod]
        public void TestBitvectorFromArray()
        {
            var bv1 = this.Manager.CreateBitvector(new DD[] { this.VarA, this.VarB, this.One });
            bv1[2] = this.VarC;
            var bv2 = this.Manager.CreateBitvector(new DD[] { this.One, this.Zero, this.One });
            var dd = this.Manager.Eq(bv1, bv2);
            var assignment = this.Manager.Sat(dd);
            Assert.IsTrue(assignment.Get(this.Va));
            Assert.IsFalse(assignment.Get(this.Vb));
            Assert.IsTrue(assignment.Get(this.Vc));
        }

        /// <summary>
        /// Test invalid to use different length bitvectors.
        /// </summary>
        [TestMethod]
        public void TestManagerNodeCount()
        {
            var count = this.Manager.NodeCount();
            Assert.IsTrue(count > 0);
        }

        /// <summary>
        /// Test bitvector shift right.
        /// </summary>
        [TestMethod]
        public void TestBitvectorShiftRight()
        {
            var bv8 = this.Manager.CreateBitvector(this.V8);

            for (int i = 0; i < numRandomTests; i++)
            {
                var b = (byte)this.Rnd.Next(0, 255);
                var shiftAmount = (byte)this.Rnd.Next(0, 7);
                var bv = this.Manager.CreateBitvector(b);
                var shifted = this.Manager.ShiftRight(bv, shiftAmount);
                var eq = this.Manager.Eq(bv8, shifted);
                Assert.AreEqual(b >> shiftAmount, (int)this.Manager.Sat(eq).Get(this.V8));
            }
        }

        /// <summary>
        /// Test exception for invalid parameter to bitvector shift right.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestBitvectorShiftRightException1()
        {
            var bv = this.Manager.CreateBitvector((byte)0);
            var _ = this.Manager.ShiftRight(bv, -1);
        }

        /// <summary>
        /// Test exception for invalid parameter to bitvector shift right.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestBitvectorShiftRightException2()
        {
            var bv = this.Manager.CreateBitvector((byte)0);
            var _ = this.Manager.ShiftRight(bv, 9);
        }

        /// <summary>
        /// Test bitvector shift left.
        /// </summary>
        [TestMethod]
        public void TestBitvectorShiftLeft()
        {
            var bv8 = this.Manager.CreateBitvector(this.V8);

            for (int i = 0; i < numRandomTests; i++)
            {
                var b = (byte)this.Rnd.Next(0, 255);
                var shiftAmount = (byte)this.Rnd.Next(0, 7);
                var bv = this.Manager.CreateBitvector(b);
                var shifted = this.Manager.ShiftLeft(bv, shiftAmount);
                var eq = this.Manager.Eq(bv8, shifted);
                Assert.AreEqual((byte)(b << shiftAmount), (int)this.Manager.Sat(eq).Get(this.V8));
            }
        }

        /// <summary>
        /// Test exception for invalid parameter to bitvector shift right.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestBitvectorShiftLeftException1()
        {
            var bv = this.Manager.CreateBitvector((byte)0);
            _ = this.Manager.ShiftLeft(bv, -1);
        }

        /// <summary>
        /// Test exception for invalid parameter to bitvector shift right.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestBitvectorShiftLeftException2()
        {
            var bv = this.Manager.CreateBitvector((byte)0);
            _ = this.Manager.ShiftLeft(bv, 9);
        }

        /// <summary>
        /// Test bitvector ite.
        /// </summary>
        [TestMethod]
        public void TestBitvectorIte()
        {
            var bv32 = this.Manager.CreateBitvector(this.V32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value = this.Rnd.Next(0, 255);
                var bv1 = this.Manager.CreateBitvector(value);
                var bv2 = this.Manager.CreateBitvector(5);
                var ite = this.Manager.Ite(this.Manager.True(), bv1, bv2);
                var eq = this.Manager.Eq(bv32, ite);
                Assert.AreEqual(value, this.Manager.Sat(eq).Get(this.V32));
            }
        }

        /// <summary>
        /// Test bitvector or.
        /// </summary>
        [TestMethod]
        public void TestBitvectorOr()
        {
            var bv32 = this.Manager.CreateBitvector(this.V32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value1 = this.Rnd.Next(0, 255);
                var value2 = this.Rnd.Next(0, 255);
                var bv1 = this.Manager.CreateBitvector(value1);
                var bv2 = this.Manager.CreateBitvector(value2);
                var or = this.Manager.Or(bv1, bv2);
                var eq = this.Manager.Eq(bv32, or);
                var val = this.Manager.Sat(eq).Get(this.V32);
                Assert.AreEqual(value1 | value2, val);
            }
        }

        /// <summary>
        /// Test bitvector and.
        /// </summary>
        [TestMethod]
        public void TestBitvectorAnd()
        {
            var bv32 = this.Manager.CreateBitvector(this.V32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value1 = this.Rnd.Next(0, 255);
                var value2 = this.Rnd.Next(0, 255);
                var bv1 = this.Manager.CreateBitvector(value1);
                var bv2 = this.Manager.CreateBitvector(value2);
                var and = this.Manager.And(bv1, bv2);
                var eq = this.Manager.Eq(bv32, and);
                var val = this.Manager.Sat(eq).Get(this.V32);
                Assert.AreEqual(value1 & value2, val);
            }
        }

        /// <summary>
        /// Test bitvector xor.
        /// </summary>
        [TestMethod]
        public void TestBitvectorXor()
        {
            var bv32 = this.Manager.CreateBitvector(this.V32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value1 = this.Rnd.Next(0, 255);
                var value2 = this.Rnd.Next(0, 255);
                var bv1 = this.Manager.CreateBitvector(value1);
                var bv2 = this.Manager.CreateBitvector(value2);
                var xor = this.Manager.Xor(bv1, bv2);
                var eq = this.Manager.Eq(bv32, xor);
                var val = this.Manager.Sat(eq).Get(this.V32);
                Assert.AreEqual(value1 ^ value2, val);
            }
        }

        /// <summary>
        /// Test bitvector negation.
        /// </summary>
        [TestMethod]
        public void TestBitvectorNot()
        {
            var bv32 = this.Manager.CreateBitvector(this.V32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value = this.Rnd.Next(0, 255);
                var bv = this.Manager.CreateBitvector(value);
                var not = this.Manager.Not(bv);
                var eq = this.Manager.Eq(bv32, not);
                var val = this.Manager.Sat(eq).Get(this.V32);
                Assert.AreEqual(~value, val);
            }
        }

        /// <summary>
        /// Test that resizing works ok.
        /// </summary>
        [TestMethod]
        public void TestTableResize()
        {
            var factory = new BDDNodeFactory();
            var manager = new DDManager<BDDNode>(factory, 8, 8, true);
            var v = manager.CreateBool();
            var w = manager.CreateBool();
            var x = manager.CreateBool();
            var y = manager.CreateBool();
            var z = manager.CreateBool();
            Assert.IsTrue(x.IsBool());
            Assert.IsTrue(y.IsBool());
            Assert.IsTrue(z.IsBool());

            // create enough nodes to force a resize and make sure no crash.
            var a = manager.Or(x.Id(), manager.And(y.Id(), z.Id()));
            var b = manager.Or(z.Id(), a);
            var c = manager.And(b, manager.Not(x.Id()));
            var d = manager.Or(v.Id(), x.Id());
            var e = manager.Or(w.Id(), v.Id());
            var f = manager.And(d, e);
        }

        /// <summary>
        /// Test that the node count is correct.
        /// </summary>
        [TestMethod]
        public void TestNodeCount()
        {
            var factory = new BDDNodeFactory();
            var manager = new DDManager<BDDNode>(factory, 8, 8, true);
            var v = manager.CreateBool();
            var w = manager.CreateBool();
            Assert.AreEqual(1, manager.NodeCount(manager.True()));
            Assert.AreEqual(1, manager.NodeCount(manager.False()));
            Assert.AreEqual(4, manager.NodeCount(manager.And(v.Id(), w.Id())));
            Assert.AreEqual(4, manager.NodeCount(manager.Or(v.Id(), w.Id())));
        }

        /// <summary>
        /// Test that resizing works ok.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestManagerParameters()
        {
            var factory = new BDDNodeFactory();
            new DDManager<BDDNode>(factory, 8, -1);
        }

        /// <summary>
        /// Test giving DD to the wrong manager.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestWrongManagerOperation()
        {
            var factory = new BDDNodeFactory();
            var manager1 = new DDManager<BDDNode>(factory);
            var manager2 = new DDManager<BDDNode>(factory);
            var x = manager1.CreateBool();
            var y = manager2.CreateBool();
            manager1.And(x.Id(), y.Id());
        }

        /// <summary>
        /// Test manager rounds to power of two, no crash.
        /// </summary>
        [TestMethod]
        public void TestManagerEnsuresPowerOfTwo()
        {
            var factory = new BDDNodeFactory();
            var _ = new DDManager<BDDNode>(factory, 3, 13, true, 9999);
        }

        /// <summary>
        /// Test that the variables types are correct.
        /// </summary>
        [TestMethod]
        public void TestVariableTypes()
        {
            Assert.IsTrue(this.Va.IsBool());
            Assert.IsTrue(this.V8.IsU8());
            Assert.IsTrue(this.V16.IsU16());
            Assert.IsTrue(this.V32.IsU32());
            Assert.IsTrue(this.V64.IsU64());
            Assert.IsTrue(this.V128.IsUint());
        }

        /// <summary>
        /// Test that the node children are correct.
        /// </summary>
        [TestMethod]
        public void TestAccessChildren()
        {
            Assert.IsTrue(this.Manager.Low(this.VarA).Equals(this.Zero));
            Assert.IsTrue(this.Manager.High(this.VarA).Equals(this.One));
        }

        /// <summary>
        /// Test that the number of bits is correct.
        /// </summary>
        [TestMethod]
        public void TestNumBits()
        {
            var factory = new BDDNodeFactory();
            var manager = new DDManager<BDDNode>(factory);

            Assert.AreEqual(1, manager.CreateBool().NumBits);
            Assert.AreEqual(8, manager.CreateInt8().NumBits);
            Assert.AreEqual(16, manager.CreateInt16().NumBits);
            Assert.AreEqual(32, manager.CreateInt32().NumBits);
            Assert.AreEqual(64, manager.CreateInt64().NumBits);
            Assert.AreEqual(7, manager.CreateInt(7).NumBits);
            Assert.AreEqual(19, manager.CreateInt(19).NumBits);
        }

        /// <summary>
        /// Test that the number of variables is correct.
        /// </summary>
        [TestMethod]
        public void TestNumVariables()
        {
            var manager = new DDManager<T>(this.Factory);
            manager.CreateBool();
            manager.CreateBool();

            Assert.AreEqual(2, manager.NumVariables);

            manager.CreateInt8();
            manager.CreateInt16();

            Assert.AreEqual(26, manager.NumVariables);
        }

        /// <summary>
        /// Test that the node children are correct.
        /// </summary>
        [TestMethod]
        public void TestVariable()
        {
            Assert.AreEqual(this.Manager.Variable(this.VarA), 1);
            Assert.AreEqual(this.Manager.Variable(this.VarB), 2);
            Assert.AreEqual(this.Manager.Variable(this.Manager.Or(this.VarA, this.VarB)), 1);
        }

        /// <summary>
        /// Test that getting a bit works.
        /// </summary>
        [TestMethod]
        public void TestGetVariableBit()
        {
            var x = this.V32.GetVariableForIthBit(31);
            var y = this.V32.Eq(1);
            var z = this.Manager.And(x.Id(), y);
            Assert.AreEqual(y, z);
        }

        /// <summary>
        /// Test that getting a bit is in range.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestGetVariableBitException1()
        {
            this.V32.GetVariableForIthBit(32);
        }

        /// <summary>
        /// Test that getting a bit is in range.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestGetVariableBitException2()
        {
            this.V32.GetVariableForIthBit(-1);
        }

        /// <summary>
        /// Test that quantification works when adding new variables.
        /// </summary>
        [TestMethod]
        public void TestExistsNewVariable()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var x = manager.CreateBool();
            var variableSet = manager.CreateVariableSet(new Variable<T>[] { x });
            var y = manager.CreateBool();
            var z = manager.And(x.Id(), y.Id());
            var y2 = manager.Exists(z, variableSet);
            Assert.AreEqual(y.Id(), y2);
        }

        /// <summary>
        /// Test that quantification works for large variable indices.
        /// </summary>
        [TestMethod]
        public void TestExistsAnyIndex()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);

            for (int i = 0; i < 10000; i++)
            {
                var x = manager.CreateBool();
                var variableSet = manager.CreateVariableSet(new Variable<T>[] { x });
                var e = manager.Exists(x.Id(), variableSet);
                Assert.AreEqual(manager.True(), e, $"{i}");
            }
        }

        /// <summary>
        /// Test that quantification works when adding new variables.
        /// </summary>
        [TestMethod]
        public void TestExistsMultipleVariables()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateInt32();
            var b = manager.CreateBool();
            var variableSet = manager.CreateVariableSet(new Variable<T>[] { b });
            var x = manager.And(a.Eq(9), b.Id());
            var y = manager.Exists(x, variableSet);
            Assert.AreEqual(a.Eq(9), y);
        }

        /// <summary>
        /// Test that quantification works when adding new variables.
        /// </summary>
        [TestMethod]
        public void TestExistsEarlyCutoff()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var d = manager.CreateBool();
            var f1 = manager.And(c.Id(), d.Id());
            var f2 = manager.And(a.Id(), manager.And(b.Id(), f1));
            var variableSet = manager.CreateVariableSet(new Variable<T>[] { a, b });
            var f3 = manager.Exists(f2, variableSet);
            Assert.AreEqual(f1, f3);
        }

        /// <summary>
        /// Test that quantification works when adding new variables.
        /// </summary>
        [TestMethod]
        public void TestExistsAlternatingVariables()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var d = manager.CreateBool();
            var f1 = manager.And(a.Id(), manager.And(b.Id(), manager.And(c.Id(), d.Id())));
            var f2 = manager.And(a.Id(), c.Id());
            var variableSet = manager.CreateVariableSet(new Variable<T>[] { b, d });
            var f3 = manager.Exists(f1, variableSet);
            Assert.AreEqual(f2, f3);
        }

        /// <summary>
        /// Test that quantification works when adding new variables.
        /// </summary>
        [TestMethod]
        public void TestExistsWithDontCareVariables()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var d = manager.CreateBool();
            var f1 = manager.And(a.Id(), manager.And(b.Id(), d.Id()));
            var f2 = manager.And(a.Id(), d.Id());
            var variableSet = manager.CreateVariableSet(new Variable<T>[] { b, c });
            var f3 = manager.Exists(f1, variableSet);
            Assert.AreEqual(f2, f3);
        }

        /// <summary>
        /// Test replace with random.
        /// </summary>
        [TestMethod]
        public void TestsQuanfiersRandom()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);

            var variables = new VarBool<T>[]
            {
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
            };

            // create the dd of all variables
            var x = manager.True();
            foreach (var v in variables)
            {
                x = manager.And(x, v.Id());
            }

            for (int i = 0; i < numRandomTests; i++)
            {
                // randomly select some subset of 5 to quantify away
                var vars = new Variable<T>[5];
                for (int j = 0; j < 5; j++)
                {
                    vars[j] = variables[this.Rnd.Next(0, 10)];
                }

                var variableSet = manager.CreateVariableSet(vars);

                // create the expected result
                var y = manager.True();
                foreach (var v in variables)
                {
                    if (!vars.Contains(v))
                    {
                        y = manager.And(y, v.Id());
                    }
                }

                var z = manager.Exists(x, variableSet);

                Assert.AreEqual(y, z);
            }
        }

        /// <summary>
        /// Test that quantification works when adding new variables.
        /// </summary>
        [TestMethod]
        public void TestVariableSetHasCorrectVariables()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            _ = manager.CreateBool();
            _ = manager.CreateBool();
            var variableSet = manager.CreateVariableSet(new Variable<T>[] { a, b });
            Assert.AreEqual(2, variableSet.Variables.Length);
            Assert.IsTrue(variableSet.Variables.Contains(a));
            Assert.IsTrue(variableSet.Variables.Contains(b));
        }

        /// <summary>
        /// Test that variable set lookup works after allocating new variables.
        /// </summary>
        [TestMethod]
        public void TestVariableSetAfterNewVariables1()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var variableSet = manager.CreateVariableSet(new Variable<T>[] { a, b });
            var c = manager.CreateBool();
            var d = manager.CreateBool();
            var x = manager.And(c.Id(), d.Id());
            var y = manager.And(a.Id(), manager.And(b.Id(), x));
            var z = manager.Exists(y, variableSet);
            Assert.AreEqual(x, z);
        }

        /// <summary>
        /// Test that variable set lookup works after allocating new variables.
        /// </summary>
        [TestMethod]
        public void TestVariableSetAfterNewVariables2()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var variableSet = manager.CreateVariableSet(new Variable<T>[] { });
            var a = manager.CreateBool().Id();
            var x = manager.Exists(a, variableSet);
            Assert.AreEqual(a, x);
        }

        /// <summary>
        /// Test that variable set equality works.
        /// </summary>
        [TestMethod]
        public void TestVariableSetEquality()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateInt8();
            var variableSet1 = manager.CreateVariableSet(new Variable<T>[] { a, b, c });
            var variableSet2 = manager.CreateVariableSet(new Variable<T>[] { c, b, a });
            Assert.AreEqual(variableSet1.AsIndex, variableSet2.AsIndex);
            Assert.AreEqual(variableSet1.ManagerId, variableSet2.ManagerId);
        }

        /// <summary>
        /// Test that variable set Inequality works.
        /// </summary>
        [TestMethod]
        public void TestVariableSetInEquality()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateInt8();
            var variableSet1 = manager.CreateVariableSet(new Variable<T>[] { a, b, c });
            var variableSet2 = manager.CreateVariableSet(new Variable<T>[] { a, b });
            var variableSet3 = manager.CreateVariableSet(new Variable<T>[] { });
            var variableSet4 = manager.CreateVariableSet(new Variable<T>[] { a });
            var variableSet5 = manager.CreateVariableSet(new Variable<T>[] { b });

            Assert.AreNotEqual(variableSet1.AsIndex, variableSet2.AsIndex);
            Assert.AreNotEqual(variableSet2.AsIndex, variableSet3.AsIndex);
            Assert.AreNotEqual(variableSet3.AsIndex, variableSet4.AsIndex);
            Assert.AreNotEqual(variableSet4.AsIndex, variableSet5.AsIndex);
        }

        /// <summary>
        /// Test that we can access the variable mapping.
        /// </summary>
        [TestMethod]
        public void TestVariableMapHasCorrectValues()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateInt16();
            var b = manager.CreateInt16();
            var map = new Dictionary<Variable<T>, Variable<T>>();
            map.Add(a, b);
            var m = manager.CreateVariableMap(map);
            Assert.AreEqual(1, m.Mapping.Count);
            Assert.AreEqual(b, m.Mapping[a]);
        }

        /// <summary>
        /// Test that variable map creation throws an error for mismatched types.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestVariableMapException()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateInt8();
            var b = manager.CreateInt16();
            var map = new Dictionary<Variable<T>, Variable<T>>();
            map.Add(a, b);
            _ = manager.CreateVariableMap(map);
        }

        /// <summary>
        /// Test replacing with a an empty map.
        /// </summary>
        [TestMethod]
        public void TestReplaceWithEmpty()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var f1 = manager.And(a.Id(), b.Id());
            var map = new Dictionary<Variable<T>, Variable<T>>();
            var variableMap = manager.CreateVariableMap(map);
            var f2 = manager.Replace(f1, variableMap);
            Assert.AreEqual(f1, f2);
        }

        /// <summary>
        /// Test replacing for a constant.
        /// </summary>
        [TestMethod]
        public void TestReplaceWithConstant()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var variableMap = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>>());
            var x = manager.Replace(manager.True(), variableMap);
            var y = manager.Replace(manager.False(), variableMap);
            Assert.AreEqual(manager.True(), x);
            Assert.AreEqual(manager.False(), y);
        }

        /// <summary>
        /// Test replacing with a map made before new variables are added.
        /// </summary>
        [TestMethod]
        public void TestReplaceWithEmptyAfterNewVariables()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var variableMap = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>>());
            var a = manager.CreateBool().Id();
            var x = manager.Replace(a, variableMap);
            Assert.AreEqual(a, x);
        }

        /// <summary>
        /// Test replacing a variable in the middle of a chain.
        /// </summary>
        [TestMethod]
        public void TestReplaceSequence()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var d = manager.CreateBool();
            var e = manager.CreateBool();
            var f1 = manager.And(a.Id(), manager.And(b.Id(), manager.And(c.Id(), d.Id())));
            var f2 = manager.And(a.Id(), manager.And(b.Id(), manager.And(e.Id(), d.Id())));
            var map = new Dictionary<Variable<T>, Variable<T>> { { c, e } };
            var variableMap = manager.CreateVariableMap(map);
            var f3 = manager.Replace(f1, variableMap);
            Assert.AreEqual(f2, f3);
        }

        /// <summary>
        /// Test replacing a variable in the middle of a chain.
        /// </summary>
        [TestMethod]
        public void TestReplaceSkippingLevel()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var d = manager.CreateBool();
            var e = manager.CreateBool();
            var f = manager.And(a.Id(), manager.And(b.Id(), d.Id()));
            var map = new Dictionary<Variable<T>, Variable<T>> { { c, e } };
            var variableMap = manager.CreateVariableMap(map);
            Assert.AreEqual(f, manager.Replace(f, variableMap));
        }

        /// <summary>
        /// Test replacing a variable in the middle of a chain.
        /// </summary>
        [TestMethod]
        public void TestReplaceMissingLevel()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var d = manager.CreateBool();
            var e = manager.CreateBool();
            var f = manager.CreateBool();
            var g = manager.CreateBool();
            var h1 = manager.And(a.Id(), manager.And(b.Id(), manager.And(d.Id(), e.Id())));
            var h2 = manager.And(a.Id(), manager.And(b.Id(), manager.And(d.Id(), g.Id())));
            var map = new Dictionary<Variable<T>, Variable<T>> { { c, f }, { e, g } };
            var variableMap = manager.CreateVariableMap(map);
            Assert.AreEqual(h2, manager.Replace(h1, variableMap));
        }

        /// <summary>
        /// Test replacing with a variable later in the ordering.
        /// </summary>
        [TestMethod]
        public void TestReplaceWithLater()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var f1 = manager.And(a.Id(), b.Id());
            var f2 = manager.And(c.Id(), b.Id());
            var map = new Dictionary<Variable<T>, Variable<T>>();
            map[a] = c;
            var variableMap = manager.CreateVariableMap(map);
            var f3 = manager.Replace(f1, variableMap);
            Assert.AreEqual(f2, f3);
        }

        /// <summary>
        /// Test replacing with a variable earlier in the ordering.
        /// </summary>
        [TestMethod]
        public void TestReplaceWithEarlier()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var f1 = manager.And(c.Id(), b.Id());
            var f2 = manager.And(a.Id(), b.Id());
            var map = new Dictionary<Variable<T>, Variable<T>>();
            map[c] = a;
            var variableMap = manager.CreateVariableMap(map);
            var f3 = manager.Replace(f1, variableMap);
            Assert.AreEqual(f2, f3);
        }

        /// <summary>
        /// Test replacing using negation.
        /// </summary>
        [TestMethod]
        public void TestReplaceWithNegation()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var f1 = manager.Not(manager.And(c.Id(), b.Id()));
            var f2 = manager.Not(manager.And(a.Id(), b.Id()));
            var map = new Dictionary<Variable<T>, Variable<T>>();
            map[c] = a;
            var variableMap = manager.CreateVariableMap(map);
            var f3 = manager.Replace(f1, variableMap);
            Assert.AreEqual(f2, f3);
        }

        /// <summary>
        /// Test replacing using xor.
        /// </summary>
        [TestMethod]
        public void TestReplaceWithXor()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var fab = manager.Or(manager.And(a.Id(), b.Id()), manager.And(manager.Not(a.Id()), manager.Not(b.Id())));
            var fac = manager.Or(manager.And(a.Id(), c.Id()), manager.And(manager.Not(a.Id()), manager.Not(c.Id())));
            var fbc = manager.Or(manager.And(b.Id(), c.Id()), manager.And(manager.Not(b.Id()), manager.Not(c.Id())));

            var m1 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { a, b } });
            var m2 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { b, a } });
            var m3 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { a, c } });
            var m4 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { c, a } });
            var m5 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { b, c } });
            var m6 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { c, b } });

            Assert.AreEqual(fbc, manager.Replace(fab, m3));
            Assert.AreEqual(fac, manager.Replace(fab, m5));
            Assert.AreEqual(fbc, manager.Replace(fac, m1));
            Assert.AreEqual(fab, manager.Replace(fac, m6));
            Assert.AreEqual(fac, manager.Replace(fbc, m2));
            Assert.AreEqual(fab, manager.Replace(fbc, m4));
        }

        /// <summary>
        /// Test replacing multiple variables.
        /// </summary>
        [TestMethod]
        public void TestReplaceAllVariables()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var d = manager.CreateBool();
            var e = manager.CreateBool();
            var f = manager.CreateBool();
            var f1 = manager.Or(a.Id(), manager.And(b.Id(), manager.Not(c.Id())));
            var f2 = manager.Or(f.Id(), manager.And(e.Id(), manager.Not(d.Id())));

            var m = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>>
            {
                { a, f },
                { b, e },
                { c, d },
            });

            Assert.AreEqual(f2, manager.Replace(f1, m));
        }

        /// <summary>
        /// Test replacing multiple variables.
        /// </summary>
        [TestMethod]
        public void TestReplaceWithMultiple()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var d = manager.CreateBool();
            var f1 = manager.Or(a.Id(), b.Id());
            var f2 = manager.Or(c.Id(), d.Id());
            var map = new Dictionary<Variable<T>, Variable<T>>();
            map[a] = c;
            map[b] = d;
            var variableMap = manager.CreateVariableMap(map);
            var f3 = manager.Replace(f1, variableMap);
            Assert.AreEqual(f2, f3);
        }

        /// <summary>
        /// Test replacing multiple variables.
        /// </summary>
        [TestMethod]
        public void TestReplaceWithMultiple2()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateInt16();
            var c = manager.CreateBool();
            var d = manager.CreateInt16();
            var f1 = manager.And(a.Id(), b.Eq(9));
            var f2 = manager.And(c.Id(), d.Eq(9));
            var map = new Dictionary<Variable<T>, Variable<T>>();
            map[a] = c;
            map[b] = d;
            var variableMap = manager.CreateVariableMap(map);
            var f3 = manager.Replace(f1, variableMap);
            Assert.AreEqual(f2, f3);
        }

        /// <summary>
        /// Test replacing multiple variables.
        /// </summary>
        [TestMethod]
        public void TestReplacingWithSameVariable()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateBool();
            var b = manager.CreateInt16();
            var f1 = manager.And(a.Id(), b.Eq(9));
            var map = new Dictionary<Variable<T>, Variable<T>>();
            map[b] = b;
            var variableMap = manager.CreateVariableMap(map);
            var f2 = manager.Replace(f1, variableMap);
            Assert.AreEqual(f1, f2);
        }

        /// <summary>
        /// Test replace with random.
        /// </summary>
        [TestMethod]
        public void TestReplacingVariablesCommutes()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);
            var a = manager.CreateInt8();
            var b = manager.CreateInt8();
            var c = manager.CreateInt8();

            for (int i = 0; i < numRandomTests; i++)
            {
                var value1 = (byte)this.Rnd.Next(0, 255);
                var value2 = (byte)this.Rnd.Next(0, 255);
                var value3 = (byte)this.Rnd.Next(0, 255);
                var value4 = (byte)this.Rnd.Next(0, 255);

                var x = manager.Not(manager.Or(a.Eq(value1), a.Eq(value2)));
                var y = manager.Not(manager.Or(c.Eq(value3), c.Eq(value4)));

                var f1 = manager.Not(manager.And(x, y));

                var map1 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { a, b } });
                var map2 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { b, a } });

                var f2 = manager.Replace(manager.Replace(f1, map1), map2);

                var map3 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { c, b } });
                var map4 = manager.CreateVariableMap(new Dictionary<Variable<T>, Variable<T>> { { b, c } });

                var f3 = manager.Replace(manager.Replace(f1, map3), map4);

                Assert.AreEqual(f1, f2);
                Assert.AreEqual(f1, f3);
            }
        }

        /// <summary>
        /// Test replace with random.
        /// </summary>
        [TestMethod]
        public void TestReplacingVariablesRandom()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, true);

            var variables = new VarBool<T>[]
            {
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
                manager.CreateBool(),
            };

            for (int i = 0; i < numRandomTests; i++)
            {
                var map = new Dictionary<Variable<T>, Variable<T>>();
                var keyIndices = new HashSet<int>();

                // select variables to replace
                for (int j = 0; j < 4; j++)
                {
                    var idx = this.Rnd.Next(0, 10);
                    keyIndices.Add(idx);
                }

                // replace each variable with a new variable
                var valueIndices = new HashSet<int>();
                foreach (var index in keyIndices)
                {
                    int newIdx;
                    do
                    {
                        newIdx = this.Rnd.Next(0, 10);
                    } while (keyIndices.Contains(newIdx) || valueIndices.Contains(newIdx));

                    valueIndices.Add(newIdx);
                    map[variables[index]] = variables[newIdx];
                }

                var original = manager.True();
                var replaced = manager.True();

                foreach (var key in map.Keys)
                {
                    original = manager.And(original, ((VarBool<T>)key).Id());
                }

                foreach (var value in map.Values)
                {
                    replaced = manager.And(replaced, ((VarBool<T>)value).Id());
                }

                var mapping = manager.CreateVariableMap(map);
                var x = manager.Replace(original, mapping);

                Assert.AreEqual(replaced, x, $"{i}");
            }
        }

        /// <summary>
        /// Test replacing multiple variables.
        /// </summary>
        [TestMethod]
        public void TestStaticCache()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, false);
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var x = manager.And(a.Id(), b.Id());
            var y = manager.And(x, c.Id());
            var m = manager.And(b.Id(), c.Id());
            var n = manager.And(a.Id(), m);
            Assert.AreEqual(n, y);
        }

        /// <summary>
        /// Test too many variables.
        /// </summary>
        [TestMethod]
        public void TestTooManyVariablesOk()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, false);

            if (this.Factory is CBDDNodeFactory)
            {
                for (int i = 0; i < 32767; i++)
                {
                    manager.CreateBool();
                }
            }
        }

        /// <summary>
        /// Test too many variables.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestTooManyVariablesException()
        {
            var manager = new DDManager<T>(this.Factory, 8, 8, false);

            if (this.Factory is CBDDNodeFactory)
            {
                for (int i = 0; i < 32767; i++)
                {
                    var b = manager.CreateBool();
                    Assert.AreEqual(i + 1, manager.Variable(b.Id()));
                }
            }
            else
            {
                throw new InvalidOperationException();
            }

            manager.CreateBool();
        }

        /// <summary>
        /// Select a random literal.
        /// </summary>
        /// <returns>Returns a random literal.</returns>
        private DD RandomLiteral()
        {
            var i = this.Rnd.Next(0, 7);
            return this.RandomLiterals[i];
        }

        /// <summary>
        /// Run a number of random tests.
        /// </summary>
        /// <param name="action">The action to run.</param>
        private void RandomTest(Action<DD> action)
        {
            for (int i = 0; i <= numRandomTests; i++)
            {
                action.Invoke(this.RandomDD());
            }
        }

        /// <summary>
        /// Run a number of random tests.
        /// </summary>
        /// <param name="action">The action to run.</param>
        private void RandomTest(Action<DD, DD> action)
        {
            for (int i = 0; i <= numRandomTests; i++)
            {
                action.Invoke(this.RandomDD(), this.RandomDD());
            }
        }

        /// <summary>
        /// Run a number of random tests.
        /// </summary>
        /// <param name="action">The action to run.</param>
        private void RandomTest(Action<DD, DD, DD> action)
        {
            for (int i = 0; i <= numRandomTests; i++)
            {
                action.Invoke(this.RandomDD(), this.RandomDD(), this.RandomDD());
            }
        }

        /// <summary>
        /// Select a random literal.
        /// </summary>
        /// <returns>Returns a random literal.</returns>
        private DD RandomDD()
        {
            return this.RandomDD(5);
        }

        /// <summary>
        /// Select a random literal.
        /// </summary>
        /// <param name="maxDepth">The maximum depth.</param>
        /// <returns>Returns a random literal.</returns>
        private DD RandomDD(int maxDepth)
        {
            if (maxDepth == 0)
            {
                return this.RandomLiteral();
            }

            var d = maxDepth - 1;

            switch (this.Rnd.Next(0, 11))
            {
                case 0:
                case 1:
                    return this.Manager.Or(this.RandomDD(d), this.RandomDD(d));
                case 2:
                case 3:
                    return this.Manager.And(this.RandomDD(d), this.RandomDD(d));
                case 4:
                case 5:
                    return this.Manager.Not(this.RandomDD(d));
                case 6:
                case 7:
                    return this.Manager.Implies(this.RandomDD(d), this.RandomDD(d));
                case 8:
                    return this.Manager.True();
                default:
                    return this.RandomLiteral();
            }
        }
    }
}