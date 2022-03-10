// <copyright file="Queens.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagramsBench
{
    using System;
    using DecisionDiagrams;

    /// <summary>
    /// Benchmark for the n queens problem.
    /// </summary>
    public class Queens<T> where T : IDDNode
    {
        private DDManager<T> manager;

        private int boardSize;

        private DD[,] boardConstraints;

        private VarBool<T>[,] variables;

        private DD problemEncoding;

        /// <summary>
        /// Creates a new instance of the <see cref="Queens{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="boardSize">The size of the board.</param>
        public Queens(DDManager<T> manager, int boardSize)
        {
            this.manager = manager;
            this.problemEncoding = manager.True();
            this.boardSize = boardSize;
            this.boardConstraints = new DD[boardSize, boardSize];
            this.variables = new VarBool<T>[boardSize, boardSize];

            for (int i = 0; i < this.boardSize; i++)
            {
                for (int j = 0; j < this.boardSize; j++)
                {
                    this.variables[i, j] = manager.CreateBool();
                    this.boardConstraints[i, j] = this.variables[i, j].Id();
                }
            }
        }

        /// <summary>
        /// Run the benchmark.
        /// </summary>
        public void Run()
        {
            PlaceQueenInEachRow();

            for (int i = 0; i < this.boardSize; i++)
            {
                for (int j = 0; j < this.boardSize; j++)
                {
                    // System.Console.WriteLine(GC.GetTotalMemory(true) / 1000 / 1000);
                    Console.WriteLine($"Adding position {i}, {j}");
                    Build(i, j);
                }
            }
        }

        /// <summary>
        /// Adds diaganol constraints for position i, j.
        /// </summary>
        /// <param name="i">The row.</param>
        /// <param name="j">The column.</param>
        private void Build(int i, int j)
        {
            DD a = manager.True();
            DD b = manager.True();
            DD c = manager.True();
            DD d = manager.True();

            // no other queens in same column.
            for (int l = 0; l < this.boardSize; l++)
            {
                if (l != j)
                {
                    a = manager.And(a, manager.Implies(this.boardConstraints[i, j], manager.Not(this.boardConstraints[i, l])));
                }
            }

            // no other queens in same row.
            for (int k = 0; k < this.boardSize; k++)
            {
                if (k != i)
                {
                    b = manager.And(b, manager.Implies(this.boardConstraints[i, j], manager.Not(this.boardConstraints[k, j])));
                }
            }

            // no other queens in same up right diagonal
            for (int k = 0; k < this.boardSize; k++)
            {
                int ll = k - i + j;
                if (ll >= 0 && ll < this.boardSize)
                {
                    if (k != i)
                    {
                        c = manager.And(c, manager.Implies(this.boardConstraints[i, j], manager.Not(this.boardConstraints[k, ll])));
                    }
                }
            }

            // no other queens in same down right diagonal
            for (int k = 0; k < this.boardSize; k++)
            {
                int ll = i + j - k;
                if (ll >= 0 && ll < this.boardSize)
                {
                    if (k != i)
                    {
                        d = manager.And(d, manager.Implies(this.boardConstraints[i, j], manager.Not(this.boardConstraints[k, ll])));
                    }
                }
            }

            this.problemEncoding = manager.And(this.problemEncoding, manager.And(a, manager.And(b, manager.And(c, d))));
        }

        /// <summary>
        /// Place a queen in each row.
        /// </summary>
        private void PlaceQueenInEachRow()
        {
            for (int i = 0; i < this.boardSize; i++)
            {
                DD e = manager.False();
                for (int j = 0; j < this.boardSize; j++)
                {
                    e = manager.Or(e, this.boardConstraints[i, j]);
                }

                this.problemEncoding = manager.And(this.problemEncoding, e);
            }
        }
    }
}
