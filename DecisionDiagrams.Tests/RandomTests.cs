// <copyright file="DiagramTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagram.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using DecisionDiagrams;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests based on building random formulas to evaluate.
    /// Includes all the generated tests that caused bugs before.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    [ExcludeFromCodeCoverage]
    public class RandomTests<T>
        where T : IDDNode, IEquatable<T>
    {
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
        /// Test checking random formulas.
        /// </summary>
        [TestMethod]
        public void TestFormulasRandomly()
        {
            var numVars = 4;
            for (int i = 0; i < 8000; i++)
            {
                var f = Formula.CreateRandom(this.Rnd, numVars, 8);
                if (!IsSound(f, numVars))
                {
                    f = Minimize(f, numVars);
                    Console.WriteLine(f);
                    Console.WriteLine(Debug(f, numVars));
                    Console.WriteLine(f.ToTest());
                    Assert.Fail();
                }
            }
        }

        /// <summary>
        /// Run a formula to check for differences with the evaluation.
        /// </summary>
        /// <param name="f">The formula f.</param>
        /// <param name="numVars">The number of variables.</param>
        /// <returns>True if the formula evaluated ok.</returns>
        private bool RunFormula(Formula f, int numVars)
        {
            var manager = this.GetManager(16);
            var variables = new VarBool<T>[numVars];
            for (int j = 0; j < numVars; j++)
            {
                variables[j] = manager.CreateBool();
            }

            var bdd = f.Evaluate(manager, variables, f);
            var assignment = manager.Sat(bdd);

            var negated = false;
            if (assignment == null)
            {
                assignment = manager.Sat(manager.Not(bdd));
                negated = true;
            }

            var evaluation = ImmutableDictionary<int, bool>.Empty;
            for (int j = 0; j < numVars; j++)
            {
                evaluation = evaluation.Add(j, assignment.Get(variables[j]));
            }

            var result = f.Evaluate(f, evaluation);

            return negated != result;
        }

        /// <summary>
        /// Checks if a formula is evaluated soundly.
        /// </summary>
        /// <param name="f">The formula.</param>
        /// <param name="numVars">The number of variables.</param>
        /// <returns></returns>
        private bool IsSound(Formula f, int numVars)
        {
            try
            {
                var b = RunFormula(f, numVars);
                return b;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a formula is evaluated soundly.
        /// </summary>
        /// <param name="f">The formula.</param>
        /// <param name="numVars">The number of variables.</param>
        /// <returns></returns>
        private string Debug(Formula f, int numVars)
        {
            try
            {
                var b = RunFormula(f, numVars);
                return "Incorrect answer.";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        /// <summary>
        /// Minimize a formula so long as it is not sound.
        /// </summary>
        /// <param name="f">The formula.</param>
        /// <param name="numVars">The number of variables.</param>
        /// <returns></returns>
        private Formula Minimize(Formula f, int numVars)
        {
            while (true)
            {
                if (f.Children == null)
                {
                    return f;
                }

                var stop = true;
                foreach (var child in f.Children)
                {
                    Console.WriteLine($"checking child");
                    if (!IsSound(child, numVars))
                    {
                        Console.WriteLine($"got minimal child");
                        f = child;
                        stop = false;
                        break;
                    }
                }

                if (stop)
                {
                    return f;
                }
            }
        }

        /// <summary>
        /// Get a new manager object.
        /// </summary>
        /// <param name="initialSize">The initial size.</param>
        /// <returns>A new manager object.</returns>
        private DDManager<T> GetManager(uint initialSize)
        {
            return new DDManager<T>(numNodes: initialSize, gcMinCutoff: (int)initialSize, printDebug: false);
        }
    }
}
