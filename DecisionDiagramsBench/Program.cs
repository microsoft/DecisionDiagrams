// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagramsBench
{
    using System;
    using DecisionDiagrams;

    /// <summary>
    /// Main program for the benchmarks.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        static void Main(string[] args)
        {
            var manager = new DDManager<CBDDNode>(new CBDDNodeFactory());
            var q = new Queens<CBDDNode>(manager, 12);

            var timer = System.Diagnostics.Stopwatch.StartNew();
            q.Run();
            Console.WriteLine(timer.ElapsedMilliseconds);
        }
    }
}
