// <copyright file="DiagramTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagram.Tests
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
        where T : IDDNode, IEquatable<T>
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
        /// Gets or setst he random number generator.
        /// </summary>
        internal Random Rnd { get; set; }

        /// <summary>
        /// Initialize the base class.
        /// </summary>
        public void BaseInitialize()
        {
            this.Rnd = new Random(7);
        }

        /// <summary>
        /// Test checking for constants.
        /// </summary>
        [TestMethod]
        public void TestConstants()
        {
            var manager = this.GetManager();
            Assert.IsTrue(manager.False().IsFalse());
            Assert.IsTrue(manager.True().IsTrue());
            Assert.IsTrue(manager.False().IsConstant());
            Assert.IsTrue(manager.True().IsConstant());
        }

        /// <summary>
        /// Test for basic logical identities.
        /// </summary>
        [TestMethod]
        public void TestIdentities()
        {
            var manager = this.GetManager();
            var a = manager.CreateBool();

            Assert.AreEqual(manager.False(), manager.And(manager.False(), a.Id()));
            Assert.AreEqual(manager.False(), manager.And(a.Id(), manager.False()));
            Assert.AreEqual(a.Id(), manager.Or(manager.False(), a.Id()));
            Assert.AreEqual(a.Id(), manager.Or(a.Id(), manager.False()));
            Assert.AreEqual(a.Id(), manager.And(manager.True(), a.Id()));
            Assert.AreEqual(a.Id(), manager.And(a.Id(), manager.True()));
            Assert.AreEqual(manager.True(), manager.Or(manager.True(), a.Id()));
            Assert.AreEqual(manager.True(), manager.Or(a.Id(), manager.True()));
        }

        /// <summary>
        /// Check idempotence of And and Or.
        /// </summary>
        [TestMethod]
        public void TestIdempotence()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) => Assert.AreEqual(a, manager.Or(a, a)));
        }

        /// <summary>
        /// Test commutativity of and.
        /// </summary>
        [TestMethod]
        public void TestCommutativityAnd()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) => Assert.AreEqual(manager.And(a, b), manager.And(b, a)));
        }

        /// <summary>
        /// Test commutativity of or.
        /// </summary>
        [TestMethod]
        public void TestCommutativityOr()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) => Assert.AreEqual(manager.Or(a, b), manager.Or(b, a)));
        }

        /// <summary>
        /// Test distributivity of and + or.
        /// </summary>
        [TestMethod]
        public void TestDistributivity1()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b, c) =>
            {
                var x = manager.And(a, manager.Or(b, c));
                var y = manager.Or(manager.And(a, b), manager.And(a, c));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test double negation does nothing.
        /// </summary>
        [TestMethod]
        public void TestNegationIdempotence()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a) =>
            {
                Assert.AreEqual(a, manager.Not(manager.Not(a)));
            });
        }

        /// <summary>
        /// Test negation for constants.
        /// </summary>
        [TestMethod]
        public void TestNegationConstant()
        {
            var manager = this.GetManager();

            Assert.AreEqual(manager.False(), manager.Not(manager.True()));
            Assert.AreEqual(manager.True(), manager.Not(manager.False()));
        }

        /// <summary>
        /// Test DeMorgan's equivalence.
        /// </summary>
        [TestMethod]
        public void TestDeMorgan()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Not(manager.And(a, b));
                var y = manager.Or(manager.Not(a), manager.Not(b));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test DeMorgan's equivalence.
        /// </summary>
        [TestMethod]
        public void TestIff()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Iff(a, b);
                var y = manager.Iff(b, a);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test DeMorgan's equivalence.
        /// </summary>
        [TestMethod]
        public void TestImplies()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Implies(a, b);
                var y = manager.Implies(manager.Not(b), manager.Not(a));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv1()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Ite(a, b, manager.False());
                var y = manager.And(a, b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite works with first variable index and constant.
        /// </summary>
        [TestMethod]
        public void TestIteBasic1()
        {
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b, c) =>
            {
                var x = manager.Ite(a, b, c);
                var y = manager.And(
                    manager.Implies(a, b),
                    manager.Implies(manager.Not(a), c));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv2()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Ite(a, manager.Not(b), manager.False());
                var y = manager.And(a, manager.Not(b));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv3()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Ite(a, manager.False(), b);
                var y = manager.And(manager.Not(a), b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv4()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Ite(a, manager.Not(b), b);
                var y = manager.Or(
                    manager.And(a, manager.Not(b)),
                    manager.And(manager.Not(a), b));
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv5()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Ite(a, manager.True(), b);
                var y = manager.Or(a, b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv6()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Ite(a, manager.False(), manager.True());
                var y = manager.Not(a);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv7()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Ite(a, manager.True(), manager.False());
                Assert.AreEqual(x, a);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv8()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Ite(a, b, manager.True());
                var y = manager.Implies(a, b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test ite equivalence.
        /// </summary>
        [TestMethod]
        public void TestIteEquiv9()
        {
            var manager = this.GetManager();
            this.RandomTest(manager, (a, b) =>
            {
                var x = manager.Ite(a, b, manager.True());
                var y = manager.Implies(a, b);
                Assert.AreEqual(x, y);
            });
        }

        /// <summary>
        /// Test existential quantification.
        /// </summary>
        [TestMethod]
        public void TestExistentialQuantification()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();

            this.RandomTest(manager, (a, b) =>
            {
                var without = manager.And(a, b);
                var all = manager.And(without, manager.And(va.Id(), vb.Id()));
                var variableSet = manager.CreateVariableSet(new Variable<T>[] { va, vb });
                var project = manager.Exists(all, variableSet);
                Assert.AreEqual(without, project);
            });
        }

        /// <summary>
        /// Test universal quantification.
        /// </summary>
        [TestMethod]
        public void TestForallQuantification()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();
            var vc = manager.CreateBool();
            var v16 = manager.CreateInt16();

            var bc = manager.Or(vb.Id(), vc.Id());
            var x = manager.Ite(va.Id(), bc, v16.Eq(9));
            var variableSet = manager.CreateVariableSet(new Variable<T>[] { va });
            x = manager.Forall(x, variableSet);
            var y = manager.And(bc, v16.Eq(9));
            Assert.AreEqual(x, y);
        }

        /// <summary>
        /// Test variable equality.
        /// </summary>
        [TestMethod]
        public void TestVariableEquals()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var v16 = manager.CreateInt16();
            var v32 = manager.CreateInt32();

            Assert.AreEqual(va.Id(), va.Id());
            Assert.AreEqual(v16.Eq(9), v16.Eq(9));
            Assert.AreEqual(v32.Eq(13), v32.Eq(13));
        }

        /// <summary>
        /// Test the hash code.
        /// </summary>
        [TestMethod]
        public void TestHashEquals()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();
            var vc = manager.CreateBool();

            var x = manager.And(va.Id(), vb.Id());
            var y = manager.And(vb.Id(), va.Id());
            var z = manager.And(va.Id(), vc.Id());
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
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();

            CreateGarbage(manager);

            var x = manager.And(va.Id(), vb.Id());

            GC.Collect();
            manager.GarbageCollect();

            var y = manager.And(va.Id(), vb.Id());

            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
            Assert.IsTrue(x.Equals(y));
        }

        /// <summary>
        /// Helper function to create garbage in a new scope.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        private void CreateGarbage(DDManager<T> manager)
        {
            var literals = this.GetLiterals(manager, 5);
            for (int i = 0; i < 100; i++)
            {
                var left = this.RandomDD(manager, literals);
                var right = this.RandomDD(manager, literals);
                manager.Or(left, right);
            }
        }

        /// <summary>
        /// Test that garbage collection updates Ids.
        /// </summary>
        [TestMethod]
        public void TestGarbageCollection()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();
            var vc = manager.CreateBool();

            var foo1 = manager.And(manager.Or(va.Id(), vb.Id()), vc.Id());
            var bar = manager.Or(va.Id(), vc.Id());
            bar = null;
            GC.Collect();
            manager.GarbageCollect();
            var foo2 = manager.And(vc.Id(), manager.Or(va.Id(), vb.Id()));
            Assert.AreEqual(foo1, foo2);
        }

        /// <summary>
        /// Test variable equality constraint.
        /// </summary>
        [TestMethod]
        public void TestVariableEquality()
        {
            var manager = this.GetManager();
            var xs = manager.CreateInterleavedInt32(2);
            var x = xs[0];
            var y = xs[1];

            var inv = manager.And(x.Eq(y), x.LessOrEqual(10));
            Assignment<T> assignment = manager.Sat(inv);
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
            var manager = this.GetManager();
            var x = manager.CreateInt32();
            var y = manager.CreateInt16();

            x.Eq(y);
        }

        /// <summary>
        /// Test satisfiability returns the right values.
        /// </summary>
        [TestMethod]
        public void TestSatisfiabilityCorrect()
        {
            var manager = this.GetManager();

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
            var manager = this.GetManager();
            var v8 = manager.CreateInt8();
            var v16 = manager.CreateInt16();
            var v32 = manager.CreateInt32();
            var v64 = manager.CreateInt64();
            var v128 = manager.CreateInt(128);

            var values = new byte[16];
            values[15] = 3;

            var all = manager.And(v8.Eq(4), manager.And(v16.Eq(9), manager.And(v32.Eq(11), manager.And(v64.Eq(18), v128.Eq(values)))));
            Assignment<T> assignment = manager.Sat(all);
            byte r8 = assignment.Get(v8);
            short r16 = assignment.Get(v16);
            int r32 = assignment.Get(v32);
            long r64 = assignment.Get(v64);
            byte[] r128 = assignment.Get(v128);
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
            var manager = this.GetManager();
            var v9 = manager.CreateInt(9, i => 8 - i);

            var x = v9.Eq(new byte[2] { 1, 128 });
            Assignment<T> assignment = manager.Sat(x);
            byte[] r9 = assignment.Get(v9);
            Assert.AreEqual(r9[0], 1);
            Assert.AreEqual(r9[1], 128);
        }

        /// <summary>
        /// Test satisfiability for a subset of variables.
        /// </summary>
        [TestMethod]
        public void TestSatisfiabilityForSubsetOfVariables()
        {
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
            Assignment<T> assignment = manager.Sat(manager.False());
            Assert.AreEqual(null, assignment);
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException1()
        {
            var manager = this.GetManager();
            Assignment<T> assignment = manager.Sat(manager.True());
            assignment.Get(manager.CreateBool());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException8()
        {
            var manager = this.GetManager();
            Assignment<T> assignment = manager.Sat(manager.True());
            assignment.Get(manager.CreateInt8());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException16()
        {
            var manager = this.GetManager();
            Assignment<T> assignment = manager.Sat(manager.True());
            assignment.Get(manager.CreateInt16());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException32()
        {
            var manager = this.GetManager();
            Assignment<T> assignment = manager.Sat(manager.True());
            assignment.Get(manager.CreateInt32());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException64()
        {
            var manager = this.GetManager();
            Assignment<T> assignment = manager.Sat(manager.True());
            assignment.Get(manager.CreateInt64());
        }

        /// <summary>
        /// Test satisfiability for invalid or missing key.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestSatisfiabilityException128()
        {
            var manager = this.GetManager();
            Assignment<T> assignment = manager.Sat(manager.True());
            assignment.Get(manager.CreateInt(128));
        }

        /// <summary>
        /// Test invalid ordering, out of bounds.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestInvalidOrdering()
        {
            var manager = this.GetManager();
            manager.CreateInt8(i => i + 1);
        }

        /// <summary>
        /// Test invalid ordering, duplicate target.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestDuplicateInOrdering()
        {
            var manager = this.GetManager();
            manager.CreateInt8(i => i % 4);
        }

        /// <summary>
        /// Test inequalities with random tests.
        /// </summary>
        [TestMethod]
        public void TestInequalities()
        {
            var manager = this.GetManager();
            var v8 = manager.CreateInt8();
            var v16 = manager.CreateInt16();
            var v32 = manager.CreateInt32();
            var v64 = manager.CreateInt64();
            var v128 = manager.CreateInt(128);

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

                var bounds8 = manager.And(v8.GreaterOrEqual(lower8), v8.LessOrEqual(upper8));
                var bounds16 = manager.And(v16.GreaterOrEqual((short)lower), v16.LessOrEqual((short)upper));
                var bounds32 = manager.And(v32.GreaterOrEqual(lower), v32.LessOrEqual(upper));
                var bounds64 = manager.And(v64.GreaterOrEqual((long)lower), v64.LessOrEqual((long)upper));
                var bounds128 = manager.And(v128.GreaterOrEqual(lower128), v128.LessOrEqual(upper128));

                var assignment8 = manager.Sat(bounds8);
                var assignment16 = manager.Sat(bounds16);
                var assignment32 = manager.Sat(bounds32);
                var assignment64 = manager.Sat(bounds64);
                var assignment128 = manager.Sat(bounds128);

                var r8 = assignment8.Get(v8);
                var r16 = assignment16.Get(v16);
                var r32 = assignment32.Get(v32);
                var r64 = assignment64.Get(v64);
                var r128 = assignment128.Get(v128);

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
            var manager = this.GetManager();
            var v128 = manager.CreateInt(128);

            for (int i = 0; i < numRandomTests; i++)
            {
                byte[] bytes = new byte[16];
                this.Rnd.NextBytes(bytes);

                var f = v128.Eq(bytes);
                var assignment = manager.Sat(f);
                var r128 = assignment.Get(v128);

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
            var manager = this.GetManager();
            var v8s = manager.CreateInterleavedInt8(3);
            var v16s = manager.CreateInterleavedInt16(3);
            var v32s = manager.CreateInterleavedInt32(3);
            var v64s = manager.CreateInterleavedInt64(3);
            var v10s = manager.CreateInterleavedInt(3, 10);

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

                var x = manager.True();

                x = manager.And(x, v8s[0].Eq(b1));
                x = manager.And(x, v8s[1].Eq(b2));
                x = manager.And(x, v8s[2].Eq(b3));

                x = manager.And(x, v16s[0].Eq(s1));
                x = manager.And(x, v16s[1].Eq(s2));
                x = manager.And(x, v16s[2].Eq(s3));

                x = manager.And(x, v32s[0].Eq(i1));
                x = manager.And(x, v32s[1].Eq(i2));
                x = manager.And(x, v32s[2].Eq(i3));

                x = manager.And(x, v64s[0].Eq(l1));
                x = manager.And(x, v64s[1].Eq(l2));
                x = manager.And(x, v64s[2].Eq(l3));

                x = manager.And(x, v10s[0].Eq(new byte[2] { b1, 0 }));
                x = manager.And(x, v10s[1].Eq(new byte[2] { b2, 0 }));
                x = manager.And(x, v10s[2].Eq(new byte[2] { b3, 0 }));

                var assignment = manager.Sat(x);

                Assert.IsTrue(assignment.Get(v8s[0]) == b1);
                Assert.IsTrue(assignment.Get(v8s[1]) == b2);
                Assert.IsTrue(assignment.Get(v8s[2]) == b3);

                Assert.IsTrue(assignment.Get(v16s[0]) == s1);
                Assert.IsTrue(assignment.Get(v16s[1]) == s2);
                Assert.IsTrue(assignment.Get(v16s[2]) == s3);

                Assert.IsTrue(assignment.Get(v32s[0]) == i1);
                Assert.IsTrue(assignment.Get(v32s[1]) == i2);
                Assert.IsTrue(assignment.Get(v32s[2]) == i3);

                Assert.IsTrue(assignment.Get(v64s[0]) == l1);
                Assert.IsTrue(assignment.Get(v64s[1]) == l2);
                Assert.IsTrue(assignment.Get(v64s[2]) == l3);

                Assert.IsTrue(assignment.Get(v10s[0])[0] == b1);
                Assert.IsTrue(assignment.Get(v10s[1])[0] == b2);
                Assert.IsTrue(assignment.Get(v10s[2])[0] == b3);
            }
        }

        /// <summary>
        /// Test DeMorgan's equivalence with random tests.
        /// </summary>
        [TestMethod]
        public void TestOrEqualities()
        {
            var manager = this.GetManager();
            var v8 = manager.CreateInt8();

            for (int i = 0; i < numRandomTests; i++)
            {
                byte val1 = (byte)this.Rnd.Next(0, 255);
                byte val2 = (byte)this.Rnd.Next(0, 255);
                byte val3 = (byte)this.Rnd.Next(0, 255);
                byte val4 = (byte)this.Rnd.Next(0, 255);

                var dd = v8.Eq(val1);
                dd = manager.Or(dd, v8.Eq(val2));
                dd = manager.Or(dd, v8.Eq(val3));
                dd = manager.Or(dd, v8.Eq(val4));

                var assignment = manager.Sat(dd);
                var v = assignment.Get(v8);

                Assert.IsTrue(v == val1 || v == val2 || v == val3 || v == val4);

                dd = v8.Eq(val4);
                dd = manager.Or(dd, v8.Eq(val3));
                dd = manager.Or(dd, v8.Eq(val2));
                dd = manager.Or(dd, v8.Eq(val1));

                assignment = manager.Sat(dd);
                v = assignment.Get(v8);

                Assert.IsTrue(v == val1 || v == val2 || v == val3 || v == val4);
            }
        }

        /// <summary>
        /// Test bitvector addition.
        /// </summary>
        [TestMethod]
        public void TestBitvectorAddition()
        {
            var manager = this.GetManager();
            var v8 = manager.CreateInt8();
            var v16 = manager.CreateInt16();
            var v32 = manager.CreateInt32();
            var v64 = manager.CreateInt64();

            var bv8 = manager.CreateBitvector(v8);
            var bv16 = manager.CreateBitvector(v16);
            var bv32 = manager.CreateBitvector(v32);
            var bv64 = manager.CreateBitvector(v64);

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

                var bv1 = manager.CreateBitvector(b1);
                var bv2 = manager.CreateBitvector(b2);
                var sum = manager.Add(bv1, bv2);
                var eq = manager.Eq(bv8, sum);
                Assert.AreEqual(b1 + b2, manager.Sat(eq).Get(v8));

                bv1 = manager.CreateBitvector(s1);
                bv2 = manager.CreateBitvector(s2);
                sum = manager.Add(bv1, bv2);
                eq = manager.Eq(bv16, sum);
                Assert.AreEqual(s1 + s2, manager.Sat(eq).Get(v16));

                bv1 = manager.CreateBitvector(us1);
                bv2 = manager.CreateBitvector(us2);
                sum = manager.Add(bv1, bv2);
                eq = manager.Eq(bv16, sum);
                Assert.AreEqual(us1 + us2, manager.Sat(eq).Get(v16));

                bv1 = manager.CreateBitvector(i1);
                bv2 = manager.CreateBitvector(i2);
                sum = manager.Add(bv1, bv2);
                eq = manager.Eq(bv32, sum);
                Assert.AreEqual(i1 + i2, manager.Sat(eq).Get(v32));

                bv1 = manager.CreateBitvector(ui1);
                bv2 = manager.CreateBitvector(ui2);
                sum = manager.Add(bv1, bv2);
                eq = manager.Eq(bv32, sum);
                Assert.AreEqual(ui1 + ui2, (uint)manager.Sat(eq).Get(v32));

                bv1 = manager.CreateBitvector(l1);
                bv2 = manager.CreateBitvector(l2);
                sum = manager.Add(bv1, bv2);
                eq = manager.Eq(bv64, sum);
                Assert.AreEqual(l1 + l2, manager.Sat(eq).Get(v64));

                bv1 = manager.CreateBitvector(ul1);
                bv2 = manager.CreateBitvector(ul2);
                sum = manager.Add(bv1, bv2);
                eq = manager.Eq(bv64, sum);
                Assert.AreEqual(ul1 + ul2, (ulong)manager.Sat(eq).Get(v64));
            }
        }

        /// <summary>
        /// Test bitvector subtraction.
        /// </summary>
        [TestMethod]
        public void TestBitvectorSubtraction()
        {
            var manager = this.GetManager();
            var v8 = manager.CreateInt8();
            var v16 = manager.CreateInt16();
            var v32 = manager.CreateInt32();
            var v64 = manager.CreateInt64();

            var bv8 = manager.CreateBitvector(v8);
            var bv16 = manager.CreateBitvector(v16);
            var bv32 = manager.CreateBitvector(v32);
            var bv64 = manager.CreateBitvector(v64);

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

                var bv1 = manager.CreateBitvector(b1);
                var bv2 = manager.CreateBitvector(b2);
                var sum = manager.Subtract(bv2, bv1);
                var eq = manager.Eq(bv8, sum);
                Assert.AreEqual(b2 - b1, manager.Sat(eq).Get(v8));

                bv1 = manager.CreateBitvector(s1);
                bv2 = manager.CreateBitvector(s2);
                sum = manager.Subtract(bv2, bv1);
                eq = manager.Eq(bv16, sum);
                Assert.AreEqual(s2 - s1, manager.Sat(eq).Get(v16));

                bv1 = manager.CreateBitvector(i1);
                bv2 = manager.CreateBitvector(i2);
                sum = manager.Subtract(bv2, bv1);
                eq = manager.Eq(bv32, sum);
                Assert.AreEqual(i2 - i1, manager.Sat(eq).Get(v32));

                bv1 = manager.CreateBitvector(l1);
                bv2 = manager.CreateBitvector(l2);
                sum = manager.Subtract(bv2, bv1);
                eq = manager.Eq(bv64, sum);
                Assert.AreEqual(l2 - l1, manager.Sat(eq).Get(v64));
            }
        }

        /// <summary>
        /// Test bitvector operations work with signed values.
        /// </summary>
        [TestMethod]
        public void TestBitvectorSigned()
        {
            var manager = this.GetManager();
            var v32 = manager.CreateInt32();

            var x = manager.CreateBitvector(v32);
            var y = manager.CreateBitvector(-10);
            var z = manager.CreateBitvector(-20);

            var a = manager.LessOrEqualSigned(x, y);
            var b = manager.And(a, manager.GreaterOrEqualSigned(x, z));

            Assert.AreEqual(int.MinValue, manager.Sat(a).Get(v32));
            Assert.AreEqual(-20, manager.Sat(b).Get(v32));
        }

        /// <summary>
        /// Test bitvector less than or equal.
        /// </summary>
        [TestMethod]
        public void TestBitvectorInequalities()
        {
            var manager = this.GetManager();

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

                var bv1 = manager.CreateBitvector(b1);
                var bv2 = manager.CreateBitvector(b2);
                Assert.AreEqual(manager.True(), manager.LessOrEqual(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.LessOrEqualSigned(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.Less(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.GreaterOrEqual(bv2, bv1));
                Assert.AreEqual(manager.True(), manager.GreaterOrEqualSigned(bv2, bv1));
                Assert.AreEqual(manager.True(), manager.Greater(bv2, bv1));

                bv1 = manager.CreateBitvector(s1);
                bv2 = manager.CreateBitvector(s2);
                Assert.AreEqual(manager.True(), manager.LessOrEqual(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.LessOrEqualSigned(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.Less(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.GreaterOrEqual(bv2, bv1));
                Assert.AreEqual(manager.True(), manager.GreaterOrEqualSigned(bv2, bv1));
                Assert.AreEqual(manager.True(), manager.Greater(bv2, bv1));

                bv1 = manager.CreateBitvector(i1);
                bv2 = manager.CreateBitvector(i2);
                Assert.AreEqual(manager.True(), manager.LessOrEqual(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.LessOrEqualSigned(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.Less(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.GreaterOrEqual(bv2, bv1));
                Assert.AreEqual(manager.True(), manager.GreaterOrEqualSigned(bv2, bv1));
                Assert.AreEqual(manager.True(), manager.Greater(bv2, bv1));

                bv1 = manager.CreateBitvector(l1);
                bv2 = manager.CreateBitvector(l2);
                Assert.AreEqual(manager.True(), manager.LessOrEqual(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.LessOrEqualSigned(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.Less(bv1, bv2));
                Assert.AreEqual(manager.True(), manager.GreaterOrEqual(bv2, bv1));
                Assert.AreEqual(manager.True(), manager.GreaterOrEqualSigned(bv2, bv1));
                Assert.AreEqual(manager.True(), manager.Greater(bv2, bv1));
            }
        }

        /// <summary>
        /// Test variable to domain.
        /// </summary>
        [TestMethod]
        public void TestVariableToDomain()
        {
            var manager = this.GetManager();
            var v8 = manager.CreateInt8();

            var domain = v8.ToBitvector();
            var bits = domain.GetBits();
            Assert.AreEqual(8, bits.Length);
        }

        /// <summary>
        /// Test least significant bit first.
        /// </summary>
        [TestMethod]
        public void TestLeastSignificantBitFirst()
        {
            var manager = this.GetManager();

            var v = manager.CreateInt8(BitOrder.LSB_FIRST);
            var dd = v.Eq(4);
            var assignment = manager.Sat(dd);
            Assert.AreEqual((byte)4, assignment.Get(v));
        }

        /// <summary>
        /// Test node count is correct.
        /// </summary>
        [TestMethod]
        public virtual void TestNodeCountCorrect()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();

            var dd = manager.Or(va.Id(), vb.Id());
            Assert.AreEqual(4, manager.NodeCount(dd));
        }

        /// <summary>
        /// Test invalid to use different length bitvectors.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestMismatchedBitvectorSizes()
        {
            var manager = this.GetManager();
            var bv1 = manager.CreateBitvector((byte)3);
            var bv2 = manager.CreateBitvector((short)3);
            manager.Greater(bv1, bv2);
        }

        /// <summary>
        /// Test invalid to use different length bitvectors.
        /// </summary>
        [TestMethod]
        public void TestBitvectorFromArray()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();
            var vc = manager.CreateBool();

            var bv1 = manager.CreateBitvector(new DD[] { va.Id(), vb.Id(), manager.True() });
            bv1[2] = vc.Id();
            var bv2 = manager.CreateBitvector(new DD[] { manager.True(), manager.False(), manager.True() });
            var dd = manager.Eq(bv1, bv2);
            var assignment = manager.Sat(dd);
            Assert.IsTrue(assignment.Get(va));
            Assert.IsFalse(assignment.Get(vb));
            Assert.IsTrue(assignment.Get(vc));
        }

        /// <summary>
        /// Test invalid to use different length bitvectors.
        /// </summary>
        [TestMethod]
        public void TestManagerNodeCount()
        {
            var manager = this.GetManager();
            var count = manager.NodeCount();
            Assert.IsTrue(count > 0);
        }

        /// <summary>
        /// Test bitvector shift right.
        /// </summary>
        [TestMethod]
        public void TestBitvectorShiftRight()
        {
            var manager = this.GetManager();
            var v8 = manager.CreateInt8();

            var bv8 = manager.CreateBitvector(v8);

            for (int i = 0; i < numRandomTests; i++)
            {
                var b = (byte)this.Rnd.Next(0, 255);
                var shiftAmount = (byte)this.Rnd.Next(0, 7);
                var bv = manager.CreateBitvector(b);
                var shifted = manager.ShiftRight(bv, shiftAmount);
                var eq = manager.Eq(bv8, shifted);
                Assert.AreEqual(b >> shiftAmount, (int)manager.Sat(eq).Get(v8));
            }
        }

        /// <summary>
        /// Test exception for invalid parameter to bitvector shift right.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestBitvectorShiftRightException1()
        {
            var manager = this.GetManager();
            var bv = manager.CreateBitvector((byte)0);
            var _ = manager.ShiftRight(bv, -1);
        }

        /// <summary>
        /// Test exception for invalid parameter to bitvector shift right.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestBitvectorShiftRightException2()
        {
            var manager = this.GetManager();
            var bv = manager.CreateBitvector((byte)0);
            var _ = manager.ShiftRight(bv, 9);
        }

        /// <summary>
        /// Test bitvector shift left.
        /// </summary>
        [TestMethod]
        public void TestBitvectorShiftLeft()
        {
            var manager = this.GetManager();
            var v8 = manager.CreateInt8();

            var bv8 = manager.CreateBitvector(v8);

            for (int i = 0; i < numRandomTests; i++)
            {
                var b = (byte)this.Rnd.Next(0, 255);
                var shiftAmount = (byte)this.Rnd.Next(0, 7);
                var bv = manager.CreateBitvector(b);
                var shifted = manager.ShiftLeft(bv, shiftAmount);
                var eq = manager.Eq(bv8, shifted);
                Assert.AreEqual((byte)(b << shiftAmount), (int)manager.Sat(eq).Get(v8));
            }
        }

        /// <summary>
        /// Test exception for invalid parameter to bitvector shift right.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestBitvectorShiftLeftException1()
        {
            var manager = this.GetManager();
            var bv = manager.CreateBitvector((byte)0);
            _ = manager.ShiftLeft(bv, -1);
        }

        /// <summary>
        /// Test exception for invalid parameter to bitvector shift right.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestBitvectorShiftLeftException2()
        {
            var manager = this.GetManager();
            var bv = manager.CreateBitvector((byte)0);
            _ = manager.ShiftLeft(bv, 9);
        }

        /// <summary>
        /// Test bitvector ite.
        /// </summary>
        [TestMethod]
        public void TestBitvectorIte()
        {
            var manager = this.GetManager();
            var v32 = manager.CreateInt32();

            var bv32 = manager.CreateBitvector(v32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value = this.Rnd.Next(0, 255);
                var bv1 = manager.CreateBitvector(value);
                var bv2 = manager.CreateBitvector(5);
                var ite = manager.Ite(manager.True(), bv1, bv2);
                var eq = manager.Eq(bv32, ite);
                Assert.AreEqual(value, manager.Sat(eq).Get(v32));
            }
        }

        /// <summary>
        /// Test bitvector or.
        /// </summary>
        [TestMethod]
        public void TestBitvectorOr()
        {
            var manager = this.GetManager();
            var v32 = manager.CreateInt32();

            var bv32 = manager.CreateBitvector(v32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value1 = this.Rnd.Next(0, 255);
                var value2 = this.Rnd.Next(0, 255);
                var bv1 = manager.CreateBitvector(value1);
                var bv2 = manager.CreateBitvector(value2);
                var or = manager.Or(bv1, bv2);
                var eq = manager.Eq(bv32, or);
                var val = manager.Sat(eq).Get(v32);
                Assert.AreEqual(value1 | value2, val);
            }
        }

        /// <summary>
        /// Test bitvector and.
        /// </summary>
        [TestMethod]
        public void TestBitvectorAnd()
        {
            var manager = this.GetManager();
            var v32 = manager.CreateInt32();

            var bv32 = manager.CreateBitvector(v32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value1 = this.Rnd.Next(0, 255);
                var value2 = this.Rnd.Next(0, 255);
                var bv1 = manager.CreateBitvector(value1);
                var bv2 = manager.CreateBitvector(value2);
                var and = manager.And(bv1, bv2);
                var eq = manager.Eq(bv32, and);
                var val = manager.Sat(eq).Get(v32);
                Assert.AreEqual(value1 & value2, val);
            }
        }

        /// <summary>
        /// Test bitvector xor.
        /// </summary>
        [TestMethod]
        public void TestBitvectorXor()
        {
            var manager = this.GetManager();
            var v32 = manager.CreateInt32();

            var bv32 = manager.CreateBitvector(v32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value1 = this.Rnd.Next(0, 255);
                var value2 = this.Rnd.Next(0, 255);
                var bv1 = manager.CreateBitvector(value1);
                var bv2 = manager.CreateBitvector(value2);
                var xor = manager.Xor(bv1, bv2);
                var eq = manager.Eq(bv32, xor);
                var val = manager.Sat(eq).Get(v32);
                Assert.AreEqual(value1 ^ value2, val);
            }
        }

        /// <summary>
        /// Test bitvector negation.
        /// </summary>
        [TestMethod]
        public void TestBitvectorNot()
        {
            var manager = this.GetManager();
            var v32 = manager.CreateInt32();

            var bv32 = manager.CreateBitvector(v32);

            for (int i = 0; i < numRandomTests; i++)
            {
                var value = this.Rnd.Next(0, 255);
                var bv = manager.CreateBitvector(value);
                var not = manager.Not(bv);
                var eq = manager.Eq(bv32, not);
                var val = manager.Sat(eq).Get(v32);
                Assert.AreEqual(~value, val);
            }
        }

        /// <summary>
        /// Test that resizing works ok.
        /// </summary>
        [TestMethod]
        public void TestTableResize()
        {
            var manager = this.GetManager();
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
            var manager = this.GetManager();
            var v = manager.CreateBool();
            var w = manager.CreateBool();
            Assert.AreEqual(1, manager.NodeCount(manager.True()));
            Assert.AreEqual(1, manager.NodeCount(manager.False()));
            Assert.AreEqual(4, manager.NodeCount(manager.And(v.Id(), w.Id())));
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
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var v8 = manager.CreateInt8();
            var v16 = manager.CreateInt16();
            var v32 = manager.CreateInt32();
            var v64 = manager.CreateInt64();
            var v128 = manager.CreateInt(128);

            Assert.IsTrue(va.IsBool());
            Assert.IsTrue(v8.IsU8());
            Assert.IsTrue(v16.IsU16());
            Assert.IsTrue(v32.IsU32());
            Assert.IsTrue(v64.IsU64());
            Assert.IsTrue(v128.IsUint());
        }

        /// <summary>
        /// Test that the node children are correct.
        /// </summary>
        [TestMethod]
        public void TestAccessChildren()
        {
            var manager = this.GetManager();
            var va = manager.CreateBool();

            Assert.IsTrue(manager.Low(va.Id()).Equals(manager.False()));
            Assert.IsTrue(manager.High(va.Id()).Equals(manager.True()));
        }

        /// <summary>
        /// Test that the number of bits is correct.
        /// </summary>
        [TestMethod]
        public void TestNumBits()
        {
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
            var va = manager.CreateBool();
            var vb = manager.CreateBool();

            Assert.AreEqual(manager.Variable(va.Id()), 1);
            Assert.AreEqual(manager.Variable(vb.Id()), 2);
            Assert.AreEqual(manager.Variable(manager.Or(va.Id(), vb.Id())), 1);
        }

        /// <summary>
        /// Test that getting a bit works.
        /// </summary>
        [TestMethod]
        public void TestGetVariableBit()
        {
            var manager = this.GetManager();
            var v32 = manager.CreateInt32();

            var x = v32.GetVariableForIthBit(31);
            var y = v32.Eq(1);
            var z = manager.And(x.Id(), y);
            Assert.AreEqual(y, z);
        }

        /// <summary>
        /// Test that getting a bit is in range.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestGetVariableBitException1()
        {
            var manager = this.GetManager();
            var v32 = manager.CreateInt32();
            v32.GetVariableForIthBit(32);
        }

        /// <summary>
        /// Test that getting a bit is in range.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected ArgumentException.")]
        public void TestGetVariableBitException2()
        {
            var manager = this.GetManager();
            var v32 = manager.CreateInt32();
            v32.GetVariableForIthBit(-1);
        }

        /// <summary>
        /// Test that quantification works when adding new variables.
        /// </summary>
        [TestMethod]
        public void TestExistsNewVariable()
        {
            var manager = this.GetManager();
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
            var manager = this.GetManager();

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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();

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
                GC.Collect();
                manager.GarbageCollect();

                // randomly select some subset of 5 to quantify away
                var vars = new HashSet<Variable<T>>();
                for (int j = 0; j < 5; j++)
                {
                    var variable = variables[this.Rnd.Next(0, 10)];
                    vars.Add(variable);
                }

                var variableSet = manager.CreateVariableSet(vars.ToArray());

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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateInt8();
            var variableSet1 = manager.CreateVariableSet(new Variable<T>[] { a, b, c });
            var variableSet2 = manager.CreateVariableSet(new Variable<T>[] { c, b, a });
            Assert.AreEqual(variableSet1.Id, variableSet2.Id);
            Assert.AreEqual(variableSet1.ManagerId, variableSet2.ManagerId);
        }

        /// <summary>
        /// Test that variable set Inequality works.
        /// </summary>
        [TestMethod]
        public void TestVariableSetInEquality()
        {
            var manager = this.GetManager();
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateInt8();
            var variableSet1 = manager.CreateVariableSet(new Variable<T>[] { a, b, c });
            var variableSet2 = manager.CreateVariableSet(new Variable<T>[] { a, b });
            var variableSet3 = manager.CreateVariableSet(new Variable<T>[] { });
            var variableSet4 = manager.CreateVariableSet(new Variable<T>[] { a });
            var variableSet5 = manager.CreateVariableSet(new Variable<T>[] { b });

            Assert.AreNotEqual(variableSet1.Id, variableSet2.Id);
            Assert.AreNotEqual(variableSet2.Id, variableSet3.Id);
            Assert.AreNotEqual(variableSet3.Id, variableSet4.Id);
            Assert.AreNotEqual(variableSet4.Id, variableSet5.Id);
        }

        /// <summary>
        /// Test that we can access the variable mapping.
        /// </summary>
        [TestMethod]
        public void TestVariableMapHasCorrectValues()
        {
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();
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
            var manager = this.GetManager();

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
        /// Test that the sat count operation works.
        /// </summary>
        [TestMethod]
        public void TestSatCountTrivial()
        {
            var manager = this.GetManager();
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            Assert.AreEqual(0, manager.SatCount(manager.False()));
            Assert.AreEqual(8, manager.SatCount(manager.True()));
        }

        /// <summary>
        /// Test that the sat count operation works.
        /// </summary>
        [TestMethod]
        public void TestSatCountAnd()
        {
            var manager = this.GetManager();
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var f = manager.And(a.Id(), manager.And(b.Id(), c.Id()));
            var count = manager.SatCount(f);
            Assert.AreEqual(1, count);
        }

        /// <summary>
        /// Test that the sat count operation works.
        /// </summary>
        [TestMethod]
        public void TestSatCountOr()
        {
            var manager = this.GetManager();
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var f = manager.Or(a.Id(), manager.Or(b.Id(), c.Id()));
            var count = manager.SatCount(f);
            Assert.AreEqual(7, count);
        }

        /// <summary>
        /// Test that the sat count operation works.
        /// </summary>
        [TestMethod]
        public void TestSatCountSame()
        {
            var manager = this.GetManager();
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            Assert.AreEqual(4, manager.SatCount(a.Id()));
            Assert.AreEqual(4, manager.SatCount(b.Id()));
            Assert.AreEqual(4, manager.SatCount(c.Id()));
            Assert.AreEqual(4, manager.SatCount(manager.Not(a.Id())));
            Assert.AreEqual(4, manager.SatCount(manager.Not(b.Id())));
            Assert.AreEqual(4, manager.SatCount(manager.Not(c.Id())));
        }

        /// <summary>
        /// Test that the sat count operation works.
        /// </summary>
        [TestMethod]
        public void TestSatCountCases()
        {
            var manager = this.GetManager();
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            var c = manager.CreateBool();
            var f = manager.Or(manager.And(a.Id(), b.Id()), manager.And(b.Id(), c.Id()));
            Assert.AreEqual(3, manager.SatCount(f));
        }

        /// <summary>
        /// Test that the sat count operation works.
        /// </summary>
        [TestMethod]
        public void TestSatCountInteger()
        {
            var manager = this.GetManager();
            var a = manager.CreateInt32();
            var b = manager.LessOrEqual(a.ToBitvector(), manager.CreateBitvector(255));
            Assert.AreEqual(256, manager.SatCount(b));
        }

        /// <summary>
        /// Test that garbage collection doesn't invalidate nodes.
        /// </summary>
        [TestMethod]
        public void TestVariableSetsAfterGarbageCollection()
        {
            var manager = this.GetManager();
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            this.CreateGarbage(manager);
            var set1 = manager.CreateVariableSet(a, b);
            this.CreateGarbage(manager);
            this.CreateGarbage(manager);
            GC.Collect();
            manager.GarbageCollect();
            var set2 = manager.CreateVariableSet(a, b);

            Assert.AreEqual(set1.Id, set2.Id);
        }

        /// <summary>
        /// Test that garbage collection doesn't fail with bitvectors.
        /// </summary>
        [TestMethod]
        public void TestBitvectorsAfterGarbageCollection()
        {
            var manager = this.GetManager();
            var vars = manager.CreateInterleavedInt32(2);
            var a = vars[0].ToBitvector();
            var b = vars[1].ToBitvector();
            this.CreateGarbage(manager);
            var x = manager.Add(a, b);
            this.CreateGarbage(manager);
            this.CreateGarbage(manager);
            GC.Collect();
            manager.GarbageCollect();
            var y = manager.Add(a, b);

            Assert.AreEqual(manager.True(), manager.Eq(x, y));
        }

        /// <summary>
        /// Test replacing multiple variables.
        /// </summary>
        [TestMethod]
        public void TestStaticCache()
        {
            var manager = new DDManager<T>(this.Factory, dynamicCache: false);
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
            var manager = this.GetManager();

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
            var manager = this.GetManager();

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
        /// Test creating a variable set with duplicate variables throws an exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSatWithDuplicateVariables()
        {
            var manager = this.GetManager();
            var a = manager.CreateBool();
            var b = manager.CreateBool();
            manager.CreateVariableSet(new Variable<T>[] { a, b, a });
        }

        /// <summary>
        /// Run a number of random tests.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="action">The action to run.</param>
        private void RandomTest(DDManager<T> manager, Action<DD> action)
        {
            var literals = GetLiterals(manager, 5);
            for (int i = 0; i <= numRandomTests; i++)
            {
                action.Invoke(this.RandomDD(manager, literals));
            }
        }

        /// <summary>
        /// Run a number of random tests.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="action">The action to run.</param>
        private void RandomTest(DDManager<T> manager, Action<DD, DD> action)
        {
            var literals = GetLiterals(manager, 5);
            for (int i = 0; i <= numRandomTests; i++)
            {
                action.Invoke(this.RandomDD(manager, literals), this.RandomDD(manager, literals));
            }
        }

        /// <summary>
        /// Run a number of random tests.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="action">The action to run.</param>
        private void RandomTest(DDManager<T> manager, Action<DD, DD, DD> action)
        {
            var literals = GetLiterals(manager, 5);
            for (int i = 0; i <= numRandomTests; i++)
            {
                action.Invoke(this.RandomDD(manager, literals), this.RandomDD(manager, literals), this.RandomDD(manager, literals));
            }
        }

        /// <summary>
        /// Select a random literal.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="randomLiterals">Random literal values.</param>
        /// <returns>Returns a random literal.</returns>
        private DD RandomDD(DDManager<T> manager, DD[] randomLiterals)
        {
            return this.RandomDD(manager, randomLiterals, 7);
        }

        /// <summary>
        /// Select a random literal.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="randomLiterals">The random literals.</param>
        /// <param name="maxDepth">The maximum depth.</param>
        /// <returns>Returns a random decision diagram.</returns>
        private DD RandomDD(DDManager<T> manager, DD[] randomLiterals, int maxDepth)
        {
            if (maxDepth == 0)
            {
                var i = this.Rnd.Next(0, randomLiterals.Length);
                return randomLiterals[i];
            }

            var d = maxDepth - 1;

            switch (this.Rnd.Next(0, 11))
            {
                case 0:
                case 1:
                    return manager.Or(this.RandomDD(manager, randomLiterals, d), this.RandomDD(manager, randomLiterals, d));
                case 2:
                case 3:
                    return manager.And(this.RandomDD(manager, randomLiterals, d), this.RandomDD(manager, randomLiterals, d));
                case 4:
                case 5:
                    return manager.Not(this.RandomDD(manager, randomLiterals, d));
                case 6:
                case 7:
                    return manager.Implies(this.RandomDD(manager, randomLiterals, d), this.RandomDD(manager, randomLiterals, d));
                case 8:
                    return manager.True();
                default:
                    var i = this.Rnd.Next(0, randomLiterals.Length);
                    return randomLiterals[i];
            }
        }

        /// <summary>
        /// Get some number of literals for a given manager object.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="numLiterals">The number of literals.</param>
        /// <returns></returns>
        private DD[] GetLiterals(DDManager<T> manager, int numLiterals)
        {
            var literals = new DD[2 * numLiterals];
            for (int i = 0; i < numLiterals; i++)
            {
                var v = manager.CreateBool();
                literals[i] = v.Id();
                literals[i + numLiterals] = manager.Not(v.Id());
            }

            return literals;
        }

        /// <summary>
        /// Get a new manager object.
        /// </summary>
        /// <returns>A new manager object.</returns>
        internal DDManager<T> GetManager()
        {
            return new DDManager<T>(this.Factory, numNodes: 16, gcMinCutoff: 16, printDebug: false);
        }
    }
}
