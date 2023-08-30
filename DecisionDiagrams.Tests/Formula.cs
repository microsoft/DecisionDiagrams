// <copyright file="Formula.cs" company="Microsoft">
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

    /// <summary>
    /// Tests for binary decision diagrams.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Formula
    {
        /// <summary>
        /// The formula type.
        /// </summary>
        public AstType Type { get; }

        /// <summary>
        /// Data for the formula.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// The formula children.
        /// </summary>
        public Formula[] Children { get; }

        /// <summary>
        /// Create a new instance of the <see cref="Formula"/> type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="children">The children.</param>
        /// <param name="data">The data.</param>
        public Formula(AstType type, Formula[] children, object data = null)
        {
            Type = type;
            Data = data;
            Children = children;
        }

        /// <summary>
        /// The variable formula.
        /// </summary>
        /// <param name="v">The variable id.</param>
        /// <returns></returns>
        public static Formula Var(int v) => new Formula(AstType.VAR, null, data: v);

        /// <summary>
        /// The `true` formula.
        /// </summary>
        /// <returns></returns>
        public static Formula True() => new Formula(AstType.TRUE, null);

        /// <summary>
        /// The `false` formula.
        /// </summary>
        /// <returns></returns>
        public static Formula False() => new Formula(AstType.FALSE, null);

        /// <summary>
        /// The `not` of a formula.
        /// </summary>
        /// <param name="a">The first formula.</param>
        /// <returns></returns>
        public static Formula Not(Formula a) => new Formula(AstType.NOT, new Formula[] { a });

        /// <summary>
        /// The `and` of two formula.
        /// </summary>
        /// <param name="a">The first formula.</param>
        /// <param name="b">The second formula.</param>
        /// <returns></returns>
        public static Formula And(Formula a, Formula b) => new Formula(AstType.AND, new Formula[] { a, b });

        /// <summary>
        /// The `or` of two formula.
        /// </summary>
        /// <param name="a">The first formula.</param>
        /// <param name="b">The second formula.</param>
        /// <returns></returns>
        public static Formula Or(Formula a, Formula b) => new Formula(AstType.OR, new Formula[] { a, b });

        /// <summary>
        /// The `implies` of two formula.
        /// </summary>
        /// <param name="a">The first formula.</param>
        /// <param name="b">The second formula.</param>
        /// <returns></returns>
        public static Formula Implies(Formula a, Formula b) => new Formula(AstType.IMPLIES, new Formula[] { a, b });

        /// <summary>
        /// The `iff` of two formula.
        /// </summary>
        /// <param name="a">The first formula.</param>
        /// <param name="b">The second formula.</param>
        /// <returns></returns>
        public static Formula Iff(Formula a, Formula b) => new Formula(AstType.IFF, new Formula[] { a, b });

        /// <summary>
        /// The `ite` of two formula.
        /// </summary>
        /// <param name="a">The first formula.</param>
        /// <param name="b">The second formula.</param>
        /// <param name="c">The third formula.</param>
        /// <returns></returns>
        public static Formula Ite(Formula a, Formula b, Formula c) => new Formula(AstType.ITE, new Formula[] { a, b, c });

        /// <summary>
        /// The `replace` on a formula.
        /// </summary>
        /// <param name="a">The first formula.</param>
        /// <param name="from">The variable to substitute.</param>
        /// <param name="to">The variable that will replace.</param>
        /// <returns></returns>
        public static Formula Replace(Formula a, int from, int to) => new Formula(AstType.REPLACE, new Formula[] { a }, data: (from, to));

        /// <summary>
        /// The `exists` of a formula.
        /// </summary>
        /// <param name="a">The first formula.</param>
        /// <param name="v">The variable to quantify over.</param>
        /// <returns></returns>
        public static Formula Exists(Formula a, int v) => new Formula(AstType.EXISTS, new Formula[] { a }, data: v);

        /// <summary>
        /// The `forall` of a formula.
        /// </summary>
        /// <param name="a">The first formula.</param>
        /// <param name="v">The variable to quantify over.</param>
        /// <returns></returns>
        public static Formula Forall(Formula a, int v) => new Formula(AstType.FORALL, new Formula[] { a }, data: v);

        /// <summary>
        /// Create a random formula up to a maximum depth.
        /// </summary>
        /// <param name="random">The random number generator.</param>
        /// <param name="numVars">The number of variables to use.</param>
        /// <param name="maxDepth">The maximum depth of the formula.</param>
        /// <returns></returns>
        public static Formula CreateRandom(Random random, int numVars, int maxDepth)
        {
            if (numVars < 0 || maxDepth < 0)
            {
                throw new ArgumentException("Invalid call to CreateRandom.");
            }

            return CreateRandom(random, numVars, maxDepth, 0);
        }

        /// <summary>
        /// Create a random formula up to a maximum depth.
        /// </summary>
        /// <param name="random">The random number generator.</param>
        /// <param name="numVars">The number of variables to use.</param>
        /// <param name="maxDepth">The maximum depth of the formula.</param>
        /// <param name="currentDepth">The current depth.</param>
        /// <returns></returns>
        private static Formula CreateRandom(Random random, int numVars, int maxDepth, int currentDepth)
        {
            var r = random.Next(15);
            r = currentDepth == maxDepth ? r % 3 : r;

            switch (r)
            {
                case 0:
                    return Formula.True();
                case 1:
                    return Formula.False();
                case 2:
                case 3:
                case 4:
                case 5:
                    return Formula.Var(random.Next(numVars));
                case 6:
                    return Formula.And(
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1),
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1));
                case 7:
                    return Formula.Or(
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1),
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1));
                case 8:
                    return Formula.Not(CreateRandom(random, numVars, maxDepth, currentDepth + 1));
                case 9:
                    return Formula.Iff(
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1),
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1));
                case 10:
                    return Formula.Implies(
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1),
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1));
                case 11:
                    return Formula.Ite(
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1),
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1),
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1));
                case 12:
                    return Formula.Replace(
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1),
                        random.Next(numVars),
                        random.Next(numVars));
                case 13:
                    return Formula.Exists(
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1),
                        random.Next(numVars));
                case 14:
                    return Formula.Forall(
                        CreateRandom(random, numVars, maxDepth, currentDepth + 1),
                        random.Next(numVars));
                default:
                    throw new Exception("impossible");
            }
        }

        /// <summary>
        /// Evaluate a formula using BDDs.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="variables">The variables.</param>
        /// <param name="f">The formula.</param>
        /// <returns></returns>
        public DD Evaluate<T>(DDManager<T> manager, VarBool<T>[] variables, Formula f) where T : IDDNode, IEquatable<T>
        {
            switch (f.Type)
            {
                case AstType.TRUE:
                    return manager.True();
                case AstType.FALSE:
                    return manager.False();
                case AstType.VAR:
                    return variables[(int)f.Data].Id();
                case AstType.AND:
                    return manager.And(
                        Evaluate(manager, variables, f.Children[0]),
                        Evaluate(manager, variables, f.Children[1]));
                case AstType.OR:
                    return manager.Or(
                        Evaluate(manager, variables, f.Children[0]),
                        Evaluate(manager, variables, f.Children[1]));
                case AstType.IFF:
                    return manager.Iff(
                        Evaluate(manager, variables, f.Children[0]),
                        Evaluate(manager, variables, f.Children[1]));
                case AstType.IMPLIES:
                    return manager.Implies(
                        Evaluate(manager, variables, f.Children[0]),
                        Evaluate(manager, variables, f.Children[1]));
                case AstType.ITE:
                    return manager.Ite(
                        Evaluate(manager, variables, f.Children[0]),
                        Evaluate(manager, variables, f.Children[1]),
                        Evaluate(manager, variables, f.Children[2]));
                case AstType.NOT:
                    return manager.Not(Evaluate(manager, variables, f.Children[0]));
                case AstType.REPLACE:
                    var (from, to) = ((int, int))f.Data;
                    var dict = new Dictionary<Variable<T>, Variable<T>> { { variables[from], variables[to] } };
                    var map = manager.CreateVariableMap(dict);
                    return manager.Replace(Evaluate(manager, variables, f.Children[0]), map);
                case AstType.EXISTS:
                    return manager.Exists(
                        Evaluate(manager, variables, f.Children[0]),
                        manager.CreateVariableSet(variables[(int)f.Data]));
                case AstType.FORALL:
                    return manager.Forall(
                        Evaluate(manager, variables, f.Children[0]),
                        manager.CreateVariableSet(variables[(int)f.Data]));
                default:
                    throw new Exception("Unreachable");
            }
        }

        /// <summary>
        /// Evaluate a formula using a concrete assignment.
        /// </summary>
        /// <param name="f">The formula.</param>
        /// <param name="assignment">The assignment.</param>
        /// <returns></returns>
        public bool Evaluate(Formula f, ImmutableDictionary<int, bool> assignment)
        {
            switch (f.Type)
            {
                case AstType.TRUE:
                    return true;
                case AstType.FALSE:
                    return false;
                case AstType.VAR:
                    return assignment[(int)f.Data];
                case AstType.AND:
                    return Evaluate(f.Children[0], assignment) && Evaluate(f.Children[1], assignment);
                case AstType.OR:
                    return Evaluate(f.Children[0], assignment) || Evaluate(f.Children[1], assignment);
                case AstType.IFF:
                    return Evaluate(f.Children[0], assignment) == Evaluate(f.Children[1], assignment);
                case AstType.IMPLIES:
                    return !Evaluate(f.Children[0], assignment) || Evaluate(f.Children[1], assignment);
                case AstType.ITE:
                    return Evaluate(f.Children[0], assignment) ? Evaluate(f.Children[1], assignment) : Evaluate(f.Children[2], assignment);
                case AstType.NOT:
                    return !Evaluate(f.Children[0], assignment);
                case AstType.REPLACE:
                    var (from, to) = ((int, int))f.Data;
                    return Evaluate(f.Children[0], assignment.SetItem(from, assignment[to]));
                case AstType.EXISTS:
                    var v1 = (int)f.Data;
                    return Evaluate(f.Children[0], assignment.SetItem(v1, false)) || Evaluate(f.Children[0], assignment.SetItem(v1, true));
                case AstType.FORALL:
                    var v2 = (int)f.Data;
                    return Evaluate(f.Children[0], assignment.SetItem(v2, false)) && Evaluate(f.Children[0], assignment.SetItem(v2, true));
                default:
                    throw new Exception("Unreachable");
            }
        }

        /// <summary>
        /// Convert the formula to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var arguments = new List<string>();
            if (this.Children != null)
            {
                foreach (var child in this.Children)
                {
                    arguments.Add(child.ToString());
                }
            }

            if (this.Data != null)
            {
                arguments.Add(this.Data.ToString());
            }

            if (arguments.Count == 0)
            {
                return this.Type.ToString();
            }
            else
            {
                var args = string.Join(", ", arguments);
                return $"{this.Type}({args})";
            }
        }

        /// <summary>
        /// Convert the formula to a C# test.
        /// </summary>
        /// <returns>A C# test to reproduce the evaluation of the formula.</returns>
        public string ToTest()
        {
            var lines = new List<string>
            {
                "var manager = this.GetManager(2048);",
                "var a = manager.CreateBool();",
                "var b = manager.CreateBool();",
                "var c = manager.CreateBool();",
            };
            var x = ToTest(lines);
            lines.Add($"manager.Sat({x});");
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Create a fresh variable name.
        /// </summary>
        /// <returns></returns>
        private string FreshVariable()
        {
            return "x" + Guid.NewGuid().ToString().Substring(0, 8);
        }

        /// <summary>
        /// Convert the formula to a C# test.
        /// </summary>
        /// <param name="lines">The lines to accumulate.</param>
        /// <returns>The variable number for this sub formula.</returns>
        public string ToTest(List<string> lines)
        {
            var x = this.FreshVariable();
            switch (this.Type)
            {
                case AstType.TRUE:
                    lines.Add($"var {x} = manager.True();");
                    break;
                case AstType.FALSE:
                    lines.Add($"var {x} = manager.False();");
                    break;
                case AstType.NOT:
                    lines.Add($"var {x} = manager.Not({this.Children[0].ToTest(lines)});");
                    break;
                case AstType.VAR:
                    var variables0 = new string[] { "a", "b", "c" };
                    lines.Add($"var {x} = {variables0[(int)this.Data]}.Id();");
                    break;
                case AstType.AND:
                case AstType.OR:
                case AstType.IMPLIES:
                case AstType.IFF:
                    var op = this.Type.ToString();
                    op = op.Substring(0, 1) + op.Substring(1).ToLower();
                    lines.Add($"var {x} = manager.{op}({this.Children[0].ToTest(lines)}, {this.Children[1].ToTest(lines)});");
                    break;
                case AstType.ITE:
                    lines.Add($"var {x} = manager.Ite({this.Children[0].ToTest(lines)}, {this.Children[1].ToTest(lines)}, {this.Children[2].ToTest(lines)});");
                    break;
                case AstType.FORALL:
                case AstType.EXISTS:
                    var setId = this.FreshVariable();
                    var variables1 = new string[] { "a", "b", "c" };
                    var item = variables1[(int)this.Data];
                    lines.Add($"var {setId} = manager.CreateVariableSet({item});");
                    lines.Add($"var {x} = manager.Exists({this.Children[0].ToTest(lines)}, {setId});");
                    break;
                case AstType.REPLACE:
                    var mapId = this.FreshVariable();
                    var dictId = this.FreshVariable();
                    var (from, to) = ((int, int))this.Data;
                    var variables2 = new string[] { "a", "b", "c" };
                    var fromItem = variables2[from];
                    var toItem = variables2[to];
                    lines.Add($"var {dictId} = new Dictionary<Variable<T>, Variable<T>>();");
                    lines.Add($"{dictId}[{fromItem}] = {toItem};");
                    lines.Add($"var {mapId} = manager.CreateVariableMap({dictId});");
                    lines.Add($"var {x} = manager.Replace({this.Children[0].ToTest(lines)}, {mapId});");
                    break;
                default:
                    throw new Exception("Invalid");
            }

            return x;
        }
    }

    /// <summary>
    /// A formula AST type.
    /// </summary>
    public enum AstType
    {
        /// <summary>
        /// The true formula.
        /// </summary>
        TRUE,

        /// <summary>
        /// The false formula.
        /// </summary>
        FALSE,

        /// <summary>
        /// A variable.
        /// </summary>
        VAR,

        /// <summary>
        /// The `and` of two formulas.
        /// </summary>
        AND,

        /// <summary>
        /// The `or` or two formulas.
        /// </summary>
        OR,

        /// <summary>
        /// The `not` of a formula.
        /// </summary>
        NOT,

        /// <summary>
        /// The `iff` of a formula.
        /// </summary>
        IFF,

        /// <summary>
        /// The `implies` of a formula.
        /// </summary>
        IMPLIES,

        /// <summary>
        /// The `ite` of a formula.
        /// </summary>
        ITE,

        /// <summary>
        /// A replacement of a variable in a formula.
        /// </summary>
        REPLACE,

        /// <summary>
        /// The `exists` formula.
        /// </summary>
        EXISTS,

        /// <summary>
        /// The `forall` formula.
        /// </summary>
        FORALL,
    }
}
