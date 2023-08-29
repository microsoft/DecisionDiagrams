// <copyright file="DDManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    /// <summary>
    /// Manager class for decision diagrams. Does all the heavy
    /// lifting for allocating new nodes and applying basic operations
    /// that implementations can call back to. This class is parametric
    /// over the specific implementation of the decision diagram, which
    /// makes it easy implement BDD variants (e.g., BDD, CBDD).
    ///
    /// Garbage collection is performed by maintaining unique external
    /// handle objects for every external node. The GC is a simple
    /// mark, sweep, and shift based collector that will traverse external
    /// handles and mark nodes as being live. It compacts nodes while
    /// preserving the node age invariant -- nodes allocated first will
    /// appear first in the memory pool. This enables a number of
    /// optimizations when looking up nodes in the unique table.
    ///
    /// Decision diagram nodes are kept in a unique table and are hash
    /// consed to ensure uniqueness. Two boolean functions are equal
    /// if and only if their pointers (index represented by DDIndex)
    /// are equal. The unique table [uniqueTable] maintains all nodes
    /// and is periodically resized when out of memory. For performance
    /// reasons, we ensure that this table is always a power of two size.
    /// This makes allocating new space a bit inflexible (harder to use
    /// all memory) but in return makes all operations faster.
    ///
    /// The manager implements logic operations: (and,or,not,implies,etc.)
    /// and uses a implementation technique called "complement edges"
    /// that mark an edge as being negated. Negation involves simply
    /// flipping the bit on the edge, and reduces memory consumption.
    ///
    /// Internally, the manager only supports a single operation: and
    /// but then leverages free negation to support other operations
    /// efficiently. This does make some operations such as ite and iff
    /// more costly, but can improve cache behavior since there is now
    /// only a single operation cache. Because "and" is commutative, the
    /// cache can further order the arguments to avoid redundant entries.
    /// </summary>
    /// <typeparam name="T">The type of the decision diagram node.</typeparam>
    [SuppressMessage("CSharp.DocumentationRules", "SA1503", Justification = "Readability of 'And' cases")]
    public sealed class DDManager<T>
        where T : IDDNode, IEquatable<T>
    {
        /// <summary>
        /// Unique manager ID that makes it possible to
        /// check if the user adds a node for the wrong manager.
        /// </summary>
        private static int nextManagerId = 0;

        /// <summary>
        /// Trigger point for a garbage collection.
        /// </summary>
        private static double gcLoadTrigger = 0.90;

        /// <summary>
        /// The client factory implementation that knows how to
        /// build and operate on nodes of type DDNode.
        /// </summary>
        private readonly IDDNodeFactory<T> factory;

        /// <summary>
        /// The current index into the memoryPool that holds nodes.
        /// Is incremented when a new unique node is created.
        /// </summary>
        private int index = 1;

        /// <summary>
        /// The size of the memory pool that holds unique nodes.
        /// </summary>
        private uint poolSize;

        /// <summary>
        /// mask for the hashes into the cache.
        /// </summary>
        private int cacheMask;

        /// <summary>
        /// Cache ratio to maintain as a fraction of the
        /// unique table size. A cache ratio of 2 means
        /// that the cache is twice as small.
        /// </summary>
        private int cacheRatio;

        /// <summary>
        /// Whether to dynamically expand the cache.
        /// </summary>
        private bool dynamicCache;

        /// <summary>
        /// Minimum node table size to trigger collection.
        /// </summary>
        private int gcMinCutoff;

        /// <summary>
        /// The number of variables this manager knows about.
        /// </summary>
        private int numVariables = 0;

        /// <summary>
        /// The names of each of the variables.
        /// </summary>
        private List<Variable<T>> variables;

        /// <summary>
        /// The memory pool that holds all the unique nodes.
        /// The index from a DD points into this array.
        /// </summary>
        private T[] memoryPool;

        /// <summary>
        /// The unique table that maps DDNode to DD in order to
        /// ensure that there is always complete structural sharing.
        /// </summary>
        private UniqueTable<T> uniqueTable;

        /// <summary>
        /// Dictionary from handles (externally visible nodes)
        /// to internal indices.
        /// </summary>
        private Dictionary<DDIndex, WeakReference<DD>> handleTable;

        /// <summary>
        /// The operation cache for the 1 argument operations.
        /// </summary>
        private OperationResult[] operationCache;

        /// <summary>
        /// The operation cache for the 2 argument operations.
        /// </summary>
        private OperationResult2[] operation2Cache;

        /// <summary>
        /// The operation cache for the "ite" operation.
        /// </summary>
        private OperationResult3[] iteCache;

        /// <summary>
        /// Fraction of the nodes that, if remain after a collection,
        /// will trigger a unique table resize.
        /// </summary>
        private double gcLoadIncrease;

        /// <summary>
        /// The number of nodes allocated before a GC is triggered currently.
        /// This is the memory pool size * gcLoadIncrease.
        /// </summary>
        private int currentGcNodeCount;

        /// <summary>
        /// The initial cache size.
        /// </summary>
        private int initialCacheSize;

        /// <summary>
        /// Whether to print debugging information including when a
        /// resize occurs and when garbage collection takes place.
        /// </summary>
        private bool printDebug;

        /// <summary>
        /// The true decision diagram.
        /// </summary>
        private DD trueDD;

        /// <summary>
        /// The false decision diagram.
        /// </summary>
        private DD falseDD;

        /// <summary>
        /// Gets the unique id for this manager. Allows allow for safety
        /// checks so that a node can't be used with the wrong manager.
        /// </summary>
        public ushort Uid { get; }

        /// <summary>
        /// Gets the number of allocated variables for this manager.
        /// </summary>
        public int NumVariables { get => this.numVariables; }

        /// <summary>
        /// Gets the underlying memory pool.
        /// </summary>
        internal T[] MemoryPool { get => this.memoryPool; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DDManager{DDNode}"/> class.
        /// </summary>
        /// <param name="numNodes">Initial number of nodes to allocate.</param>
        /// <param name="cacheRatio">Size of the cache relative to the number of nodes.</param>
        /// <param name="dynamicCache">Whether to dynamically expand the cache.</param>
        /// <param name="gcMinCutoff">The minimum node table size required to invoke collection.</param>
        /// <param name="printDebug">Whether to print debugging information such as GC collections.</param>
        public DDManager(
            uint numNodes = 1 << 19,
            int cacheRatio = 16,
            bool dynamicCache = true,
            int gcMinCutoff = 1 << 20,
            bool printDebug = false)
        {
            // check for invalid settings.
            if (cacheRatio < 0)
            {
                throw new ArgumentException("Cache ratio must be positive");
            }

            // create the node factory object based on the type.
            IDDNodeFactory<T> nodeFactory;
            if (typeof(T) == typeof(CBDDNode))
            {
                nodeFactory = (IDDNodeFactory<T>)(object)new CBDDNodeFactory();
                nodeFactory.Manager = this;
                nodeFactory.MaxVariables = (long)(1U << 15) - 1;
            }
            else
            {
                Debug.Assert(typeof(T) == typeof(BDDNode));
                nodeFactory = (IDDNodeFactory<T>)(object)new BDDNodeFactory();
                nodeFactory.Manager = this;
                nodeFactory.MaxVariables = (long)(1U << 31) - 1;
            }

            // set the initial values.
            var nodes = (uint)this.EnsurePowerOfTwo((int)Math.Max(numNodes, 16));
            var ratio = this.EnsurePowerOfTwo(cacheRatio);
            this.Uid = (ushort)Interlocked.Increment(ref nextManagerId);
            this.poolSize = nodes;
            this.cacheRatio = ratio;
            this.dynamicCache = dynamicCache;
            this.initialCacheSize = DynamicCacheSize();
            this.gcMinCutoff = gcMinCutoff;
            this.printDebug = printDebug;
            this.factory = nodeFactory;
            this.variables = new List<Variable<T>>();
            this.memoryPool = new T[this.poolSize];
            this.uniqueTable = new UniqueTable<T>(this);
            this.UpdateGcLoadIncrease();
            this.currentGcNodeCount = (int)(gcLoadTrigger * this.memoryPool.Length);
            this.ResetCaches();
            this.handleTable = new Dictionary<DDIndex, WeakReference<DD>>();
            this.trueDD = this.FromIndex(DDIndex.True);
            this.falseDD = this.FromIndex(DDIndex.False);
        }

        /// <summary>
        /// Allocate a fresh node in the memory pool
        /// and return its index.
        /// </summary>
        /// <param name="node">The node to allocate.</param>
        /// <returns>Its new index in the memory pool.</returns>
        internal DDIndex FreshNode(T node)
        {
            if (this.index == this.memoryPool.Length)
            {
                this.Resize();
            }

            var value = new DDIndex(this.index, false);
            this.memoryPool[this.index++] = node;
            return value;
        }

        /// <summary>
        /// Allocate a new DD for a DDNode.
        /// </summary>
        /// <param name="node">The DDNode.</param>
        /// <returns>An index for the allocated node.</returns>
        internal DDIndex Allocate(T node)
        {
            bool flipResult = false;
            if (node.Low.IsComplemented())
            {
                node = this.factory.Flip(node);
                flipResult = true;
            }

            if (this.factory.Reduce(node, out DDIndex result))
            {
                return flipResult ? result.Flip() : result;
            }

            DDIndex ret = this.uniqueTable.GetOrAdd(node);
            return flipResult ? ret.Flip() : ret;
        }

        /// <summary>
        /// Convert from an index to a DD. To ensure GC works correctly,
        /// we need to make sure there is a unique DD per DDIndex. We
        /// use the handleTable for this and use a WeakReference to ensure
        /// that GC collection occurs.
        /// </summary>
        /// <param name="index">The node index.</param>
        /// <returns>An external handle.</returns>
        internal DD FromIndex(DDIndex index)
        {
            if (this.handleTable.TryGetValue(index, out var wref))
            {
                if (!wref.TryGetTarget(out var target))
                {
                    target = new DD(this.Uid, index);
                    wref.SetTarget(target);
                }

                return target;
            }

            var result = new DD(this.Uid, index);
            this.handleTable[index] = new WeakReference<DD>(result);
            return result;
        }

        /// <summary>
        /// Creates a new variable set.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <returns>The variable set.</returns>
        public VariableSet<T> CreateVariableSet(params Variable<T>[] variables)
        {
            return new VariableSet<T>(this, variables);
        }

        /// <summary>
        /// Creates a new variable map.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <returns>The variable map.</returns>
        public VariableMap<T> CreateVariableMap(Dictionary<Variable<T>, Variable<T>> variables)
        {
            return new VariableMap<T>(this, variables);
        }

        /// <summary>
        /// Create the identity function for a variable.
        /// </summary>
        /// <param name="var">The boolean variable.</param>
        /// <returns>The identity function.</returns>
        public DD Id(VarBool<T> var)
        {
            this.Check(var.Manager.Uid);
            this.CheckForCollection();
            return this.FromIndex(this.IdIdx(var));
        }

        /// <summary>
        /// Convert the decision diagram to a string.
        /// </summary>
        /// <param name="value">The function.</param>
        /// <returns>The string representation.</returns>
        public string Display(DD value)
        {
            this.Check(value.ManagerId);
            return this.Display(value.Index);
        }

        /// <summary>
        /// The "false" function.
        /// </summary>
        /// <returns>The false function.</returns>
        public DD False()
        {
            return this.falseDD;
        }

        /// <summary>
        /// The "true" function.
        /// </summary>
        /// <returns>The true function.</returns>
        public DD True()
        {
            return this.trueDD;
        }

        /// <summary>
        /// Negation for DDs.
        /// </summary>
        /// <param name="x">The function.</param>
        /// <returns>The negated function.</returns>
        public DD Not(DD x)
        {
            this.Check(x.ManagerId);
            return this.FromIndex(this.Not(x.Index));
        }

        /// <summary>
        /// Conjunction for DDs.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>Their conjunction.</returns>
        public DD And(DD x, DD y)
        {
            this.Check(x.ManagerId);
            this.Check(y.ManagerId);
            this.CheckForCollection();
            return this.FromIndex(this.Apply(x.Index, y.Index, DDOperation.And));
        }

        /// <summary>
        /// Disjunction for DDs.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>Their conjunction.</returns>
        public DD Or(DD x, DD y)
        {
            this.Check(x.ManagerId);
            this.Check(y.ManagerId);
            this.CheckForCollection();
            return this.FromIndex(this.Or(x.Index, y.Index));
        }

        /// <summary>
        /// Exclusive or for DDs.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>Their conjunction.</returns>
        public DD Xor(DD x, DD y)
        {
            this.Check(x.ManagerId);
            this.Check(y.ManagerId);
            this.CheckForCollection();
            return this.FromIndex(this.Xor(x.Index, y.Index));
        }

        /// <summary>
        /// Existential quantification for DDs.
        /// </summary>
        /// <param name="x">The operand.</param>
        /// <param name="variables">The variable set to quantify.</param>
        /// <returns>The existential quantification.</returns>
        public DD Exists(DD x, VariableSet<T> variables)
        {
            this.Check(x.ManagerId);
            this.Check(variables.ManagerId);
            this.CheckForCollection();
            return this.FromIndex(this.Exists(x.Index, variables));
        }

        /// <summary>
        /// Variable substitution for DDs.
        /// </summary>
        /// <param name="x">The operand.</param>
        /// <param name="variableMap">The variable map to perform the substitution.</param>
        /// <returns>The replaced formula.</returns>
        public DD Replace(DD x, VariableMap<T> variableMap)
        {
            this.Check(x.ManagerId);
            this.Check(variableMap.ManagerId);
            this.CheckForCollection();
            return this.FromIndex(this.Replace(x.Index, variableMap));
        }

        /// <summary>
        /// Universal quantification for DDs.
        /// </summary>
        /// <param name="x">The operand.</param>
        /// <param name="variables">The variable set to quantify.</param>
        /// <returns>The existential quantification.</returns>
        public DD Forall(DD x, VariableSet<T> variables)
        {
            return this.FromIndex(this.Not(this.Exists(this.Not(x.Index), variables)));
        }

        /// <summary>
        /// Implication for DDs.
        /// </summary>
        /// <param name="x">The guard.</param>
        /// <param name="y">The implicant.</param>
        /// <returns>The resulting function.</returns>
        public DD Implies(DD x, DD y)
        {
            this.Check(x.ManagerId);
            this.Check(y.ManagerId);
            this.CheckForCollection();
            return this.FromIndex(this.Or(this.Not(x.Index), y.Index));
        }

        /// <summary>
        /// If-then-else for DDs.
        /// </summary>
        /// <param name="g">The guard.</param>
        /// <param name="t">The true branch.</param>
        /// <param name="f">The false branch.</param>
        /// <returns>The resulting function.</returns>
        public DD Ite(DD g, DD t, DD f)
        {
            this.Check(g.ManagerId);
            this.Check(t.ManagerId);
            this.Check(f.ManagerId);
            this.CheckForCollection();
            return this.FromIndex(this.Ite(g.Index, t.Index, f.Index));
        }

        /// <summary>
        /// If and only if for DDs.
        /// </summary>
        /// <param name="x">The first function.</param>
        /// <param name="y">The second function.</param>
        /// <returns>The resulting function.</returns>
        public DD Iff(DD x, DD y)
        {
            this.Check(x.ManagerId);
            this.Check(y.ManagerId);
            this.CheckForCollection();
            return this.FromIndex(this.Apply(x.Index, y.Index, DDOperation.Iff));
        }

        /// <summary>
        /// Satisfiability count for DDs.
        /// </summary>
        /// <param name="x">The operand.</param>
        /// <returns>The number of satisfying assignments.</returns>
        public double SatCount(DD x)
        {
            this.Check(x.ManagerId);
            var level = this.factory.Level(x.Index, this.MemoryPool[x.Index.GetPosition()]);
            return Math.Pow(2.0, level - 1) * this.SatCount(x.Index);
        }

        /// <summary>
        /// Gets the left child of a DD.
        /// </summary>
        /// <param name="x">The DD.</param>
        /// <returns>The left child.</returns>
        public DD Low(DD x)
        {
            var node = this.MemoryPool[x.Index.GetPosition()];
            return this.FromIndex(node.Low);
        }

        /// <summary>
        /// Gets the right child of a DD.
        /// </summary>
        /// <param name="x">The DD.</param>
        /// <returns>The left child.</returns>
        public DD High(DD x)
        {
            var node = this.MemoryPool[x.Index.GetPosition()];
            return this.FromIndex(node.High);
        }

        /// <summary>
        /// Gets the variable for a DD.
        /// </summary>
        /// <param name="x">The DD.</param>
        /// <returns>The left child.</returns>
        public int Variable(DD x)
        {
            var node = this.MemoryPool[x.Index.GetPosition()];
            return node.Variable;
        }

        /// <summary>
        /// Create a new boolean variable.
        /// </summary>
        /// <returns>A boolean variable.</returns>
        public VarBool<T> CreateBool()
        {
            var indices = this.CreateSequentialVariables(this.numVariables, 1);
            var v = new VarBool<T>(this, indices[0]);
            this.variables.Add(v);
            this.numVariables++;
            return v;
        }

        /// <summary>
        /// Create a new u8 variable.
        /// </summary>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned 8-bit variable.</returns>
        public VarInt8<T> CreateInt8(BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInt8(FunctionOfOrder(8, order));
        }

        /// <summary>
        /// Create a new u8 variable.
        /// </summary>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned 8-bit variable.</returns>
        public VarInt8<T> CreateInt8(Func<int, int> order)
        {
            var indices = this.CreateSequentialVariables(this.numVariables, 8);
            var v = new VarInt8<T>(this, indices[0], order);
            this.variables.Add(v);
            this.numVariables += 8;
            return v;
        }

        /// <summary>
        /// Create interleaved 8-bit variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned 8-bit variables.</returns>
        public VarInt8<T>[] CreateInterleavedInt8(int numVariables, BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInterleavedInt8(numVariables, FunctionOfOrder(8, order));
        }

        /// <summary>
        /// Create interleaved 8-bit variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned 8-bit variables.</returns>
        public VarInt8<T>[] CreateInterleavedInt8(int numVariables, Func<int, int> order)
        {
            var result = new VarInt8<T>[numVariables];
            var indices = this.CreateSequentialVariables(this.numVariables, 8, numVariables);
            for (int i = 0; i < numVariables; i++)
            {
                var v = new VarInt8<T>(this, indices[i], order);
                result[i] = v;
                this.variables.Add(v);
                this.numVariables += 8;
            }

            return result;
        }

        /// <summary>
        /// Create a new u16 variable.
        /// </summary>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned 16-bit variable.</returns>
        public VarInt16<T> CreateInt16(BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInt16(FunctionOfOrder(16, order));
        }

        /// <summary>
        /// Create a new u16 variable.
        /// </summary>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned 16-bit variable.</returns>
        public VarInt16<T> CreateInt16(Func<int, int> order)
        {
            var indices = this.CreateSequentialVariables(this.numVariables, 16);
            var v = new VarInt16<T>(this, indices[0], order);
            this.variables.Add(v);
            this.numVariables += 16;
            return v;
        }

        /// <summary>
        /// Create interleaved 16-bit variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned 16-bit variables.</returns>
        public VarInt16<T>[] CreateInterleavedInt16(int numVariables, BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInterleavedInt16(numVariables, FunctionOfOrder(16, order));
        }

        /// <summary>
        /// Create interleaved 16-bit variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned 16-bit variables.</returns>
        public VarInt16<T>[] CreateInterleavedInt16(int numVariables, Func<int, int> order)
        {
            var result = new VarInt16<T>[numVariables];
            var indices = this.CreateSequentialVariables(this.numVariables, 16, numVariables);
            for (int i = 0; i < numVariables; i++)
            {
                var v = new VarInt16<T>(this, indices[i], order);
                result[i] = v;
                this.variables.Add(v);
                this.numVariables += 16;
            }

            return result;
        }

        /// <summary>
        /// Create a new u32 variable.
        /// </summary>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned 32-bit variable.</returns>
        public VarInt32<T> CreateInt32(BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInt32(FunctionOfOrder(32, order));
        }

        /// <summary>
        /// Create a new u32 variable.
        /// </summary>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned 32-bit variable.</returns>
        public VarInt32<T> CreateInt32(Func<int, int> order)
        {
            var indices = this.CreateSequentialVariables(this.numVariables, 32);
            var v = new VarInt32<T>(this, indices[0], order);
            this.variables.Add(v);
            this.numVariables += 32;
            return v;
        }

        /// <summary>
        /// Create interleaved 32-bit variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned 32-bit variables.</returns>
        public VarInt32<T>[] CreateInterleavedInt32(int numVariables, BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInterleavedInt32(numVariables, FunctionOfOrder(32, order));
        }

        /// <summary>
        /// Create interleaved 32-bit variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned 32-bit variables.</returns>
        public VarInt32<T>[] CreateInterleavedInt32(int numVariables, Func<int, int> order)
        {
            var result = new VarInt32<T>[numVariables];
            var indices = this.CreateSequentialVariables(this.numVariables, 32, numVariables);
            for (int i = 0; i < numVariables; i++)
            {
                var v = new VarInt32<T>(this, indices[i], order);
                result[i] = v;
                this.variables.Add(v);
                this.numVariables += 32;
            }

            return result;
        }

        /// <summary>
        /// Create a new u64 variable.
        /// </summary>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned 64-bit variable.</returns>
        public VarInt64<T> CreateInt64(BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInt64(FunctionOfOrder(64, order));
        }

        /// <summary>
        /// Create a new u64 variable.
        /// </summary>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned 64-bit variable.</returns>
        public VarInt64<T> CreateInt64(Func<int, int> order)
        {
            var indices = this.CreateSequentialVariables(this.numVariables, 64);
            var v = new VarInt64<T>(this, indices[0], order);
            this.variables.Add(v);
            this.numVariables += 64;
            return v;
        }

        /// <summary>
        /// Create interleaved 64-bit variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned 64-bit variables.</returns>
        public VarInt64<T>[] CreateInterleavedInt64(int numVariables, BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInterleavedInt64(numVariables, FunctionOfOrder(64, order));
        }

        /// <summary>
        /// Create interleaved 64-bit variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned 64-bit variables.</returns>
        public VarInt64<T>[] CreateInterleavedInt64(int numVariables, Func<int, int> order)
        {
            var result = new VarInt64<T>[numVariables];
            var indices = this.CreateSequentialVariables(this.numVariables, 64, numVariables);
            for (int i = 0; i < numVariables; i++)
            {
                var v = new VarInt64<T>(this, indices[i], order);
                result[i] = v;
                this.variables.Add(v);
                this.numVariables += 64;
            }

            return result;
        }

        /// <summary>
        /// Create a new integer of any bitwidth.
        /// </summary>
        /// <param name="size">The number of bits.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned integer variable.</returns>
        public VarInt<T> CreateInt(int size, BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInt(size, FunctionOfOrder(size, order));
        }

        /// <summary>
        /// Create a new integer of any bitwidth.
        /// </summary>
        /// <param name="size">The number of bits.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>An unsigned integer variable.</returns>
        public VarInt<T> CreateInt(int size, Func<int, int> order)
        {
            var indices = this.CreateSequentialVariables(this.numVariables, size);
            var v = new VarInt<T>(this, indices[0], order);
            this.variables.Add(v);
            this.numVariables += size;
            return v;
        }

        /// <summary>
        /// Create interleaved integer variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="size">The number of bits per variable.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned integer variables.</returns>
        public VarInt<T>[] CreateInterleavedInt(int numVariables, int size, BitOrder order = BitOrder.MSB_FIRST)
        {
            return this.CreateInterleavedInt(numVariables, size, FunctionOfOrder(size, order));
        }

        /// <summary>
        /// Create interleaved integer variables.
        /// </summary>
        /// <param name="numVariables">The number of variables.</param>
        /// <param name="size">The number of bits per variable.</param>
        /// <param name="order">The variable order.</param>
        /// <returns>Unsigned integer variables.</returns>
        public VarInt<T>[] CreateInterleavedInt(int numVariables, int size, Func<int, int> order)
        {
            var result = new VarInt<T>[numVariables];
            var indices = this.CreateSequentialVariables(this.numVariables, size, numVariables);
            for (int i = 0; i < numVariables; i++)
            {
                var v = new VarInt<T>(this, indices[i], order);
                result[i] = v;
                this.variables.Add(v);
                this.numVariables += size;
            }

            return result;
        }

        /// <summary>
        /// Create a new bitvector from a byte constant.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>bitvector.</returns>
        public BitVector<T> CreateBitvector(byte value)
        {
            return new BitVector<T>(value, this);
        }

        /// <summary>
        /// Create a new bitvector from a short constant.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>bitvector.</returns>
        public BitVector<T> CreateBitvector(short value)
        {
            return new BitVector<T>(value, this);
        }

        /// <summary>
        /// Create a new bitvector from a ushort constant.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>bitvector.</returns>
        public BitVector<T> CreateBitvector(ushort value)
        {
            return new BitVector<T>(value, this);
        }

        /// <summary>
        /// Create a new bitvector from an int constant.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>bitvector.</returns>
        public BitVector<T> CreateBitvector(int value)
        {
            return new BitVector<T>(value, this);
        }

        /// <summary>
        /// Create a new bitvector from a uint constant.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>bitvector.</returns>
        public BitVector<T> CreateBitvector(uint value)
        {
            return new BitVector<T>(value, this);
        }

        /// <summary>
        /// Create a new bitvector from a long constant.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>bitvector.</returns>
        public BitVector<T> CreateBitvector(long value)
        {
            return new BitVector<T>(value, this);
        }

        /// <summary>
        /// Create a new bitvector from a ulong constant.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>bitvector.</returns>
        public BitVector<T> CreateBitvector(ulong value)
        {
            return new BitVector<T>(value, this);
        }

        /// <summary>
        /// Create a new bitvector from a variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>bitvector.</returns>
        public BitVector<T> CreateBitvector(Variable<T> variable)
        {
            return new BitVector<T>(variable, this);
        }

        /// <summary>
        /// Create a new bitvector from bit variables.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <returns>bitvector.</returns>
        public BitVector<T> CreateBitvector(DD[] bits)
        {
            return new BitVector<T>(this, bits);
        }

        /// <summary>
        /// Compute the conjunction of two bitvector.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Bitvector.</returns>
        public BitVector<T> And(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            this.CheckBitvectorSizes(bitvector1, bitvector2);
            var result = new BitVector<T>(this, bitvector1.Size);
            for (int i = 0; i < bitvector1.Size; i++)
            {
                result.Bits[i] = this.And(bitvector1.Bits[i], bitvector2.Bits[i]);
            }

            return result;
        }

        /// <summary>
        /// Compute the disjunction of two bitvectors.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Bitvector.</returns>
        public BitVector<T> Or(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            this.CheckBitvectorSizes(bitvector1, bitvector2);
            var result = new BitVector<T>(this, bitvector1.Size);
            for (int i = 0; i < bitvector1.Size; i++)
            {
                result.Bits[i] = this.Or(bitvector1.Bits[i], bitvector2.Bits[i]);
            }

            return result;
        }

        /// <summary>
        /// Compute the negation of a bitvector.
        /// </summary>
        /// <param name="bitvector">The bitvector.</param>
        /// <returns>Bitvector.</returns>
        public BitVector<T> Not(BitVector<T> bitvector)
        {
            var result = new BitVector<T>(this, bitvector.Size);
            for (int i = 0; i < bitvector.Size; i++)
            {
                result.Bits[i] = this.Not(bitvector.Bits[i]);
            }

            return result;
        }

        /// <summary>
        /// Compute the exclusive or of two bitvectors.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Bitvector.</returns>
        public BitVector<T> Xor(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            this.CheckBitvectorSizes(bitvector1, bitvector2);
            var result = new BitVector<T>(this, bitvector1.Size);
            for (int i = 0; i < bitvector1.Size; i++)
            {
                var b1 = bitvector1.Bits[i];
                var b2 = bitvector2.Bits[i];
                result.Bits[i] = this.Xor(b1, b2);
            }

            return result;
        }

        /// <summary>
        /// If the else for bitvectors.
        /// </summary>
        /// <param name="guard">The guard condition.</param>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Bitvector.</returns>
        public BitVector<T> Ite(DD guard, BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            this.CheckBitvectorSizes(bitvector1, bitvector2);
            var result = new BitVector<T>(this, bitvector1.Size);
            for (int i = 0; i < bitvector1.Size; i++)
            {
                var b1 = bitvector1.Bits[i];
                var b2 = bitvector2.Bits[i];
                result.Bits[i] = this.Ite(guard, b1, b2);
            }

            return result;
        }

        /// <summary>
        /// Equality for bitvectors.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Whether the bitvector are equal.</returns>
        public DD Eq(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            this.CheckBitvectorSizes(bitvector1, bitvector2);

            var result = this.True();
            for (int i = 0; i < bitvector1.Size; i++)
            {
                var b1 = bitvector1.Bits[i];
                var b2 = bitvector2.Bits[i];
                var bitsEqual = this.Iff(b1, b2);
                result = this.And(result, bitsEqual);
            }

            return result;
        }

        /// <summary>
        /// Less than or equal for two bitvectors.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Whether the first bitvector is less or equal to the second.</returns>
        public DD LessOrEqual(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            this.CheckBitvectorSizes(bitvector1, bitvector2);

            var result = this.True();
            for (int i = bitvector1.Size - 1; i >= 0; i--)
            {
                var b1 = bitvector1.Bits[i];
                var b2 = bitvector2.Bits[i];
                var eq = this.Iff(b1, b2);
                var lt = this.And(this.Not(b1), b2);
                result = this.Or(lt, this.And(result, eq));
            }

            return result;
        }

        /// <summary>
        /// Less than or equal for two signed bitvectors.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Whether the first bitvector is less or equal to the second.</returns>
        public DD LessOrEqualSigned(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            this.CheckBitvectorSizes(bitvector1, bitvector2);

            var result = this.True();
            for (int i = bitvector1.Size - 1; i >= 0; i--)
            {
                var b1 = bitvector1.Bits[i];
                var b2 = bitvector2.Bits[i];
                var eq = this.Iff(b1, b2);
                var lt = i == 0 ? this.And(b1, this.Not(b2)) : this.And(this.Not(b1), b2);
                result = this.Or(lt, this.And(result, eq));
            }

            return result;
        }

        /// <summary>
        /// Greater than or equal for two bitvector.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Whether the first bitvector is greater or equal to the second.</returns>
        public DD GreaterOrEqual(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            this.CheckBitvectorSizes(bitvector1, bitvector2);

            var result = this.True();
            for (int i = bitvector1.Size - 1; i >= 0; i--)
            {
                var b1 = bitvector1.Bits[i];
                var b2 = bitvector2.Bits[i];
                var eq = this.Iff(b1, b2);
                var gt = this.And(b1, this.Not(b2));
                result = this.Or(gt, this.And(result, eq));
            }

            return result;
        }

        /// <summary>
        /// Greater than or equal for two signed bitvector.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Whether the first bitvector is greater or equal to the second.</returns>
        public DD GreaterOrEqualSigned(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            this.CheckBitvectorSizes(bitvector1, bitvector2);

            var result = this.True();
            for (int i = bitvector1.Size - 1; i >= 0; i--)
            {
                var b1 = bitvector1.Bits[i];
                var b2 = bitvector2.Bits[i];
                var eq = this.Iff(b1, b2);
                var gt = (i == 0) ? this.And(this.Not(b1), b2) : this.And(b1, this.Not(b2));
                result = this.Or(gt, this.And(result, eq));
            }

            return result;
        }

        /// <summary>
        /// Bitwise and for two bitvectors.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>The sum of the results.</returns>
        public BitVector<T> Add(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            var result = new BitVector<T>(this, bitvector1.Size);
            var c = this.False();

            for (int n = result.Size - 1; n >= 0; n--)
            {
                result.Bits[n] = this.Xor(bitvector1.Bits[n], bitvector2.Bits[n]);
                result.Bits[n] = this.Xor(result.Bits[n], c);

                var tmp1 = this.And(this.Or(bitvector1.Bits[n], bitvector2.Bits[n]), c);
                var tmp2 = this.Or(this.And(bitvector1.Bits[n], bitvector2.Bits[n]), tmp1);
                c = tmp2;
            }

            return result;
        }

        /// <summary>
        /// Less for two bitvectors.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Whether the first bitvector is less than the second.</returns>
        public DD Less(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            return this.And(this.LessOrEqual(bitvector1, bitvector2), this.Not(this.Eq(bitvector1, bitvector2)));
        }

        /// <summary>
        /// Greater than for two bitvectors.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>Whether the first bitvector is greater than the second.</returns>
        public DD Greater(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            return this.And(this.GreaterOrEqual(bitvector1, bitvector2), this.Not(this.Eq(bitvector1, bitvector2)));
        }

        /// <summary>
        /// Subtraction of two bitvectors.
        /// </summary>
        /// <param name="bitvector1">The first bitvector.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        /// <returns>The subtraction of the arguments as a bitvector.</returns>
        public BitVector<T> Subtract(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            var result = new BitVector<T>(this, bitvector1.Size);
            var c = this.False();

            for (int n = result.Size - 1; n >= 0; n--)
            {
                result.Bits[n] = this.Xor(bitvector1.Bits[n], bitvector2.Bits[n]);
                result.Bits[n] = this.Xor(result.Bits[n], c);

                var tmp1 = this.Or(bitvector2.Bits[n], c);
                var tmp2 = this.And(this.Not(bitvector1.Bits[n]), tmp1);
                tmp1 = this.And(bitvector1.Bits[n], bitvector2.Bits[n]);
                tmp1 = this.And(tmp1, c);
                tmp1 = this.Or(tmp1, tmp2);
                c = tmp1;
            }

            return result;
        }

        /// <summary>
        /// Shift a bitvector right.
        /// </summary>
        /// <param name="bitvector">The bitvector.</param>
        /// <param name="num">The number of bits to shift.</param>
        /// <returns>The shifted bitvector.</returns>
        public BitVector<T> ShiftRight(BitVector<T> bitvector, int num)
        {
            if (num < 0 || num > bitvector.Size)
            {
                throw new ArgumentException($"Invalid bitvector shift amount {num}");
            }

            var result = new BitVector<T>(this, bitvector.Size);
            for (int n = result.Size - 1; n >= num; n--)
            {
                result.Bits[n] = bitvector.Bits[n - num];
            }

            for (int n = num - 1; n >= 0; n--)
            {
                result.Bits[n] = this.False();
            }

            return result;
        }

        /// <summary>
        /// Shift a bitvector left.
        /// </summary>
        /// <param name="bitvector">The bitvector.</param>
        /// <param name="num">The number of bits to shift.</param>
        /// <returns>The shifted bitvector.</returns>
        public BitVector<T> ShiftLeft(BitVector<T> bitvector, int num)
        {
            if (num < 0 || num > bitvector.Size)
            {
                throw new ArgumentException($"Invalid bitvector shift amount {num}");
            }

            var result = new BitVector<T>(this, bitvector.Size);
            for (int n = 0; n <= (result.Size - num - 1); n++)
            {
                result.Bits[n] = bitvector.Bits[n + num];
            }

            for (int n = result.Size - num; n < result.Size; n++)
            {
                result.Bits[n] = this.False();
            }

            return result;
        }

        /// <summary>
        /// Check the equality of bitvector sizes.
        /// </summary>
        /// <param name="bitvector1">The first domain.</param>
        /// <param name="bitvector2">The second bitvector.</param>
        private void CheckBitvectorSizes(BitVector<T> bitvector1, BitVector<T> bitvector2)
        {
            if (bitvector1.Size != bitvector2.Size)
            {
                throw new ArgumentException($"Mismatched bitvector sizes: {bitvector1.Size}, {bitvector2.Size}");
            }
        }

        /// <summary>
        /// Get the number of allocated nodes.
        /// </summary>
        /// <returns>The node count.</returns>
        public int NodeCount()
        {
            return this.index;
        }

        /// <summary>
        /// Get the node count for a decision diagram.
        /// </summary>
        /// <param name="value">The decision diagram.</param>
        /// <returns>The number of nodes in the diagram.</returns>
        public int NodeCount(DD value)
        {
            return this.NodeCount(value.Index, new HashSet<DDIndex>());
        }

        /// <summary>
        /// Find a satisfying assignment for a function.
        /// Returns null if the function is "false".
        /// </summary>
        /// <param name="value">The function.</param>
        /// <param name="variables">The variables to find assignments for. By default gets all variables.</param>
        /// <returns>The satisfying assignment.</returns>
        public Assignment<T> Sat(DD value, List<Variable<T>> variables = null)
        {
            if (value.IsFalse())
            {
                return null;
            }

            if (variables == null)
            {
                variables = this.variables;
            }

            Assignment<T> assignment = new Assignment<T>();
            var maps = this.SatInt(value, variables);
            foreach (Variable<T> var in variables)
            {
                switch (var.Type)
                {
                    case Variable<T>.VariableType.BOOL:
                        assignment.BoolAssignment.Add((VarBool<T>)var, maps.Item1[var] == 1 ? true : false);
                        break;

                    case Variable<T>.VariableType.INT8:
                        assignment.Int8Assignment.Add((VarInt8<T>)var, (byte)maps.Item1[var]);
                        break;

                    case Variable<T>.VariableType.INT16:
                        assignment.Int16Assignment.Add((VarInt16<T>)var, (short)maps.Item1[var]);
                        break;

                    case Variable<T>.VariableType.INT32:
                        assignment.Int32Assignment.Add((VarInt32<T>)var, (int)maps.Item1[var]);
                        break;

                    case Variable<T>.VariableType.INT64:
                        assignment.Int64Assignment.Add((VarInt64<T>)var, maps.Item1[var]);
                        break;

                    case Variable<T>.VariableType.INT:
                        VarInt<T> v = (VarInt<T>)var;
                        assignment.IntAssignment.Add(v, maps.Item2[v]);
                        break;
                }
            }

            return assignment;
        }

        /// <summary>
        /// Compute the negation of a function.
        /// </summary>
        /// <param name="x">The input function.</param>
        /// <returns>The function's negation.</returns>
        [ExcludeFromCodeCoverage] // all implementations support complement for now
        internal DDIndex Not(DDIndex x)
        {
            return x.Flip();
        }

        /// <summary>
        /// Compute the apply of two functions.
        /// </summary>
        /// <param name="x">The first function.</param>
        /// <param name="y">The second function.</param>
        /// <param name="operation">The operation.</param>
        /// <returns>The logical "and".</returns>
        internal DDIndex Apply(DDIndex x, DDIndex y, DDOperation operation)
        {
            if (this.operation2Cache == null)
            {
                this.operation2Cache = CreateCache2();
            }

            if (operation == DDOperation.And)
            {
                if (x.IsOne())
                {
                    return y;
                }

                if (y.IsOne())
                {
                    return x;
                }

                if (x.Equals(y))
                {
                    return x;
                }

                if (x.IsZero() || y.IsZero())
                {
                    return DDIndex.False;
                }
            }
            else
            {
                Debug.Assert(operation == DDOperation.Iff);
                if (x.IsZero())
                {
                    return y.Flip();
                }

                if (x.IsOne())
                {
                    return y;
                }

                if (y.IsZero())
                {
                    return x.Flip();
                }

                if (y.IsOne())
                {
                    return x;
                }

                if (x.Equals(y))
                {
                    return DDIndex.True;
                }
            }

            var xidx = x.GetPosition();
            var yidx = y.GetPosition();

            if (xidx == yidx)
            {
                return DDIndex.False;
            }

            // operations are commutative, so we can use a canonical order
            var arg = xidx < yidx ? new OperationArg2(x, y, operation) : new OperationArg2(y, x, operation);
            var hash = arg.GetHashCode() & 0x7FFFFFFF;

            // Look for result in the cache
            int index = hash & this.cacheMask;
            OperationResult2 result = this.operation2Cache[index];
            if (result.Arg.Equals(arg))
            {
                return result.Result;
            }

            T lo = this.memoryPool[xidx];
            T hi = this.memoryPool[yidx];

            // restore the canonical negation form
            lo = x.IsComplemented() ? this.factory.Flip(lo) : lo;
            hi = y.IsComplemented() ? this.factory.Flip(hi) : hi;

            var res = this.factory.Apply(x, lo, y, hi, operation);

            // insert the result into the cache
            OperationResult2 oresult = new OperationResult2 { Arg = arg, Result = res };
            this.operation2Cache[index] = oresult;

            return res;
        }

        /// <summary>
        /// Compute the exists of a function.
        /// </summary>
        /// <param name="x">The input function.</param>
        /// <param name="variables">The variables to quantify.</param>
        /// <returns>The logical "existential" quantification.</returns>
        internal DDIndex Exists(DDIndex x, VariableSet<T> variables)
        {
            if (this.operation2Cache == null)
            {
                this.operation2Cache = CreateCache2();
            }

            if (x.IsConstant())
            {
                return x;
            }

            var xidx = x.GetPosition();
            var arg = new OperationArg2(x, variables.Id.Index, DDOperation.Exists);
            var hash = arg.GetHashCode() & 0x7FFFFFFF;

            // Look for result in the cache
            int index = hash & this.cacheMask;
            OperationResult2 result = this.operation2Cache[index];
            if (result.Arg.Equals(arg))
            {
                return result.Result;
            }

            T node = this.memoryPool[xidx];

            // complement if needed
            node = x.IsComplemented() ? this.factory.Flip(node) : node;

            var res = this.factory.Exists(x, node, variables);

            // insert the result into the cache
            // cache may have been resized during allocation
            OperationResult2 oresult = new OperationResult2 { Arg = arg, Result = res };
            this.operation2Cache[index] = oresult;

            return res;
        }

        /// <summary>
        /// Replace the variables according to a mapping.
        /// </summary>
        /// <param name="x">The input function.</param>
        /// <param name="variableMap">The variables to quantify.</param>
        /// <returns>The substituted decision diagram.</returns>
        internal DDIndex Replace(DDIndex x, VariableMap<T> variableMap)
        {
            if (this.operation2Cache == null)
            {
                this.operation2Cache = CreateCache2();
            }

            if (x.IsConstant())
            {
                return x;
            }

            var xidx = x.GetPosition();
            var arg = new OperationArg2(x, variableMap.IdIndex, DDOperation.Replace);
            var hash = arg.GetHashCode() & 0x7FFFFFFF;

            // Look for result in the cache
            int index = hash & this.cacheMask;
            OperationResult2 result = this.operation2Cache[index];
            if (result.Arg.Equals(arg))
            {
                return result.Result;
            }

            T node = this.memoryPool[xidx];

            // complement if needed
            node = x.IsComplemented() ? this.factory.Flip(node) : node;

            var res = this.factory.Replace(x, node, variableMap);

            // insert the result into the cache
            // cache may have been resized during allocation
            OperationResult2 oresult = new OperationResult2 { Arg = arg, Result = res };
            this.operation2Cache[index] = oresult;

            return res;
        }

        /// <summary>
        /// Count the number of satisfying assignments.
        /// </summary>
        /// <param name="x">The input function.</param>
        /// <returns>The substituted decision diagram.</returns>
        internal double SatCount(DDIndex x)
        {
            if (this.operationCache == null)
            {
                this.operationCache = CreateCache();
            }

            if (x.IsOne())
            {
                return 1;
            }

            if (x.IsZero())
            {
                return 0;
            }

            var xidx = x.GetPosition();
            var hash = xidx & 0x7FFFFFFF;
            int index = hash & this.cacheMask;
            OperationResult result = this.operationCache[index];
            if (result.Arg.Equals(x))
            {
                return result.Result;
            }

            T node = this.memoryPool[xidx];
            node = x.IsComplemented() ? this.factory.Flip(node) : node;
            var res = this.factory.SatCount(node);
            OperationResult oresult = new OperationResult { Arg = x, Result = res };
            this.operationCache[index] = oresult;

            return res;
        }

        /// <summary>
        /// Compute the disjunction of two functions.
        /// </summary>
        /// <param name="x">The first function.</param>
        /// <param name="y">The second function.</param>
        /// <returns>The logical "or".</returns>
        internal DDIndex Or(DDIndex x, DDIndex y)
        {
            return this.Not(this.Apply(this.Not(x), this.Not(y), DDOperation.And));
        }

        /// <summary>
        /// Compute the conjunction of two functions.
        /// </summary>
        /// <param name="x">The first function.</param>
        /// <param name="y">The second function.</param>
        /// <returns>The logical "and".</returns>
        internal DDIndex And(DDIndex x, DDIndex y)
        {
            return this.Apply(x, y, DDOperation.And);
        }

        /// <summary>
        /// Compute the xor of two functions.
        /// </summary>
        /// <param name="x">The first function.</param>
        /// <param name="y">The second function.</param>
        /// <returns>The logical "xor".</returns>
        internal DDIndex Xor(DDIndex x, DDIndex y)
        {
            return this.Not(this.Apply(x, y, DDOperation.Iff));
        }

        /// <summary>
        /// Compute the ite of two functions.
        /// </summary>
        /// <param name="f">The guard function.</param>
        /// <param name="g">The then function.</param>
        /// <param name="h">The else function.</param>
        /// <returns>The logical "ite".</returns>
        internal DDIndex Ite(DDIndex f, DDIndex g, DDIndex h)
        {
            if (this.iteCache == null)
            {
                this.iteCache = CreateCache3();
            }

            if (f.IsOne())
            {
                return g;
            }

            if (f.IsZero())
            {
                return h;
            }

            if (g.Equals(h))
            {
                return g;
            }

            if (g.IsOne() && h.IsZero())
            {
                return f;
            }

            if (g.IsZero() && h.IsOne())
            {
                return f.Flip();
            }

            var fidx = f.GetPosition();
            var gidx = g.GetPosition();
            var hidx = h.GetPosition();

            var arg = new OperationArg3(f, g, h);
            var hash = arg.GetHashCode() & 0x7FFFFFFF;

            // Look for result in the cache
            int index = hash & this.cacheMask;
            OperationResult3 result = this.iteCache[index];
            if (result.Arg.Equals(arg))
            {
                return result.Result;
            }

            T fnode = this.memoryPool[fidx];
            T gnode = this.memoryPool[gidx];
            T hnode = this.memoryPool[hidx];

            // complement if needed
            fnode = f.IsComplemented() ? this.factory.Flip(fnode) : fnode;
            gnode = g.IsComplemented() ? this.factory.Flip(gnode) : gnode;
            hnode = h.IsComplemented() ? this.factory.Flip(hnode) : hnode;

            var res = this.factory.Ite(f, fnode, g, gnode, h, hnode);

            // insert the result into the cache
            OperationResult3 oresult = new OperationResult3 { Arg = arg, Result = res };
            this.iteCache[index] = oresult;

            return res;
        }

        /// <summary>
        /// Create the identity function for a variable.
        /// </summary>
        /// <param name="var">Variable index.</param>
        /// <returns>The index for the identity function.</returns>
        internal DDIndex IdIdx(VarBool<T> var)
        {
            return this.Allocate(this.factory.Id(var.Indices[0]));
        }

        /// <summary>
        /// Create the identity function for a variable.
        /// </summary>
        /// <param name="index">Variable index.</param>
        /// <returns>The index for the identity function.</returns>
        internal DDIndex IdIdx(int index)
        {
            return this.Allocate(this.factory.Id(index));
        }

        /// <summary>
        /// Convert the decision diagram to a string.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The string representation.</returns>
        internal string Display(DDIndex index)
        {
            return this.Display(index, false);
        }

        /// <summary>
        /// Convert the decision diagram to a string.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="negated">Parity of nested negations.</param>
        /// <returns>The string representation.</returns>
        internal string Display(DDIndex index, bool negated)
        {
            if (index.IsOne())
            {
                return negated ? "false" : "true";
            }

            if (index.IsZero())
            {
                return negated ? "true" : "false";
            }

            negated ^= index.IsComplemented();
            var node = this.memoryPool[index.GetPosition()];
            return this.factory.Display(node, negated);
        }

        /// <summary>
        /// Find a satisfying assignment for a function.
        /// "Don't care" variable will be abset from the result.
        /// </summary>
        /// <param name="value">The function.</param>
        /// <returns>Assignment of variables to values.</returns>
        internal Dictionary<int, bool> Sat(DDIndex value)
        {
            var result = new Dictionary<int, bool>();
            this.Sat(value, true, result);
            return result;
        }

        /// <summary>
        /// Convert a BitOrder to a function on indices.
        /// </summary>
        /// <param name="len">The bitwidth.</param>
        /// <param name="order">The bitorder.</param>
        /// <returns>A function on indices.</returns>
        private static Func<int, int> FunctionOfOrder(int len, BitOrder order)
        {
            if (order == BitOrder.MSB_FIRST)
            {
                return i => i;
            }

            return i => (len - 1 - i);
        }

        /// <summary>
        /// Check if we need to perform a garbage collection.
        /// If so, perform the collection.
        /// </summary>
        private void CheckForCollection()
        {
            if (this.memoryPool.Length >= this.gcMinCutoff && this.index >= this.currentGcNodeCount)
            {
                this.GarbageCollect();
            }
        }

        /// <summary>
        /// Perform a garbage collection of all live nodes.
        /// </summary>
        public void GarbageCollect()
        {
            // This is a sliding collector that works in 4 stages:
            //
            // 1. We look through all live external nodes and
            //    mark these nodes as reachable.
            // 2. We walk through all nodes in the memoryPool
            //    in order of descending node age, and mark children
            //    of nodes as being reachable.
            // 3. We simultaneously clear all non-reachable nodes
            //    and compact the remaining nodes by sliding them left
            //    into unused spots.
            // 4. We rebuild the unique table and the handle table.
            //    The cache also must be invalidated.
            PrintDebug($"[DD] Garbage collection: {this.index} / {this.memoryPool.Length}");
            PrintDebug($"[DD] Garbage collection: unique table size before {this.uniqueTable.Count}");
            PrintDebug($"[DD] Garbage collection: handle table size before {this.handleTable.Count}");

            // find all live external handles, mark those nodes
            foreach (var kv in this.handleTable)
            {
                var position = kv.Key.GetPosition();
                if (position != 0 && kv.Value.TryGetTarget(out var _))
                {
                    this.memoryPool[position].Mark = true;
                }
            }

            // recursively mark all nodes that are reachable
            var numMarked = 0;
            for (int i = this.index - 1; i >= 1; i--)
            {
                if (this.memoryPool[i].Mark)
                {
                    numMarked++;
                    var posl = this.memoryPool[i].Low.GetPosition();
                    var posh = this.memoryPool[i].High.GetPosition();
                    this.memoryPool[posl].Mark = true;
                    this.memoryPool[posh].Mark = true;
                }
            }

            PrintDebug($"[DD] Garbage collection: total live nodes {numMarked}");

            // compact all nodes by shifting left into unused spots.
            // preserves the order of node age, which is important.
            int[] forwardingAddresses = new int[this.memoryPool.Length];

            var nextFree = 1;
            for (int i = 1; i < this.index; i++)
            {
                if (this.memoryPool[i].Mark)
                {
                    var n = nextFree++;
                    var lo = this.memoryPool[i].Low;
                    var hi = this.memoryPool[i].High;
                    var posl = lo.GetPosition();
                    var posh = hi.GetPosition();
                    this.memoryPool[i].Low = new DDIndex(forwardingAddresses[posl], lo.IsComplemented());
                    this.memoryPool[i].High = new DDIndex(forwardingAddresses[posh], hi.IsComplemented());
                    this.memoryPool[i].Mark = false;
                    this.memoryPool[n] = this.memoryPool[i];
                    forwardingAddresses[i] = n;
                }
            }

            PrintDebug($"[DD] Garbage collection: shifted {nextFree - 1} nodes");

            // rebuild the unique and handle tables now that indices are invalidated
            this.uniqueTable = this.uniqueTable.Rebuild(nextFree, forwardingAddresses);

            // rebuild the handle table
            var table = new Dictionary<DDIndex, WeakReference<DD>>(this.handleTable.Count);
            foreach (var kv in this.handleTable)
            {
                var index = kv.Key;
                var wref = kv.Value;
                var position = index.GetPosition();
                var newPosition = forwardingAddresses[position];
                if (newPosition != 0)
                {
                    var newIndex = new DDIndex(newPosition, index.IsComplemented());
                    if (wref.TryGetTarget(out DD target))
                    {
                        target.Index = newIndex;
                        table.Add(newIndex, wref);
                    }
                }
            }

            this.handleTable = table;

            PrintDebug($"[DD] Garbage collection: unique table size now {this.uniqueTable.Count}");
            PrintDebug($"[DD] Garbage collection: handle table size now {this.handleTable.Count}");

            // set the new free index
            this.index = nextFree;

            var fractionRetained = this.index / (double)this.memoryPool.Length;

            PrintDebug($"[DD] Garbage collection finished: {100 * fractionRetained}% nodes remaining.");

            // decide if we need to resize the node table based on how many nodes we freed.
            if (fractionRetained > this.gcLoadIncrease)
            {
                this.Resize();
                this.UpdateGcLoadIncrease();
            }
            else
            {
                this.ResetCaches();
            }
        }

        /// <summary>
        /// Update the garbage collection load factor based on the number of allocated nodes.
        ///
        /// We need to decide whether or not to resize the memory pool
        /// based on some heuristics after a collection. if we are too eager to resize,
        /// then memory will continue to grow, but if we are too conservative, then
        /// we waste too much time repeatedly garbage collecting to fully use memory.
        ///
        /// BuDDy does a resize if more than 80% of nodes remain after collection by,
        /// default. To strike a tradeoff, between performance and memory, we start
        /// at 20% and increase the load factor we will tolerate after a collection
        /// up to 80% as the number of nodes grows.
        /// </summary>
        [ExcludeFromCodeCoverage]  // requires too large of a manager
        private void UpdateGcLoadIncrease()
        {
            var currentNodeCount = this.memoryPool.Length;

            // about 10MB so we can be quick to resize
            if (currentNodeCount <= (1 << 19))
            {
                this.gcLoadIncrease = 0.2;
            }
            // about 20MB
            else if (currentNodeCount <= (1 << 20))
            {
                this.gcLoadIncrease = 0.35;
            }
            // about 40MB
            else if (currentNodeCount <= (1 << 21))
            {
                this.gcLoadIncrease = 0.5;
            }
            // about 80MB
            else if (currentNodeCount <= (1 << 22))
            {
                this.gcLoadIncrease = 0.65;
            }
            // for large problems, be conservative to resize.
            else
            {
                this.gcLoadIncrease = 0.8;
            }
        }

        /// <summary>
        /// Reset the caches if necessary.
        /// </summary>
        private void ResetCaches()
        {
            var computedSize = CurrentCacheSize();
            this.cacheMask = Bitops.BitmaskForPowerOfTwo(computedSize);

            if (this.operation2Cache != null)
            {
                this.operation2Cache = new OperationResult2[computedSize];
            }

            if (this.iteCache != null)
            {
                this.iteCache = new OperationResult3[computedSize];
            }
        }

        /// <summary>
        /// Gets the dynamic cache size.
        /// </summary>
        /// <returns>The cache size.</returns>
        private int DynamicCacheSize()
        {
            return (int)(this.poolSize / this.cacheRatio);
        }

        /// <summary>
        /// Gets the current cache size.
        /// </summary>
        /// <returns>The cache size.</returns>
        private int CurrentCacheSize()
        {
            return dynamicCache ? DynamicCacheSize() : this.initialCacheSize;
        }

        /// <summary>
        /// Creates a cache with the correct size.
        /// </summary>
        private OperationResult[] CreateCache()
        {
            return new OperationResult[CurrentCacheSize()];
        }

        /// <summary>
        /// Creates a cache with the correct size.
        /// </summary>
        private OperationResult2[] CreateCache2()
        {
            return new OperationResult2[CurrentCacheSize()];
        }

        /// <summary>
        /// Creates a cache with the correct size.
        /// </summary>
        private OperationResult3[] CreateCache3()
        {
            return new OperationResult3[CurrentCacheSize()];
        }

        /// <summary>
        /// Resize the memory pool, increasing it by a factor of 2.
        /// </summary>
        private void Resize()
        {
            PrintDebug($"[DD] Resizing node table to: {2 * this.poolSize}");
            this.poolSize = 2 * this.poolSize;
            Array.Resize(ref this.memoryPool, unchecked((int)this.poolSize));
            this.currentGcNodeCount = (int)(gcLoadTrigger * this.memoryPool.Length);
            this.ResetCaches();
        }

        /// <summary>
        /// Print a message if debug printing enabled.
        /// </summary>
        /// <param name="message"></param>
        [ExcludeFromCodeCoverage]
        private void PrintDebug(string message)
        {
            if (printDebug)
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Get the node count for a decision diagram.
        /// </summary>
        /// <param name="value">The decision diagram.</param>
        /// <param name="seen">Nodes seen so far.</param>
        /// <returns>The number of nodes in the diagram.</returns>
        private int NodeCount(DDIndex value, ISet<DDIndex> seen)
        {
            if (seen.Contains(value))
            {
                return 0;
            }

            seen.Add(value);

            if (value.IsConstant())
            {
                return 1;
            }

            var node = this.MemoryPool[value.GetPosition()];
            return 1 + this.NodeCount(node.Low, seen) + this.NodeCount(node.High, seen);
        }

        /// <summary>
        /// Allocate a sequential set of variable indices.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="variableCount">The number of variables.</param>
        /// <param name="interleaved">How many interleaved copies to create.</param>
        /// <returns>A collection of allocated variables where the individual bits are interleaved.</returns>
        private int[][] CreateSequentialVariables(int startIndex, int variableCount, int interleaved = 1)
        {
            if (startIndex + variableCount > this.factory.MaxVariables)
            {
                throw new InvalidOperationException($"Exceeded the maximum variable count of {this.factory.MaxVariables}");
            }

            startIndex++;
            var totalBits = interleaved * variableCount;
            int[][] result = new int[interleaved][];

            for (int i = 0; i < interleaved; i++)
            {
                int[] variableIndices = new int[variableCount];
                int j = 0;
                for (int k = startIndex + i; k < startIndex + totalBits; k += interleaved)
                {
                    variableIndices[j++] = k;
                }

                result[i] = variableIndices;
            }

            return result;
        }

        /// <summary>
        /// Find a satisfying assignment for a function and
        /// create results with the appropriate type.
        /// </summary>
        /// <param name="value">The function.</param>
        /// <param name="variables">The variables to find an assignment for.</param>
        /// <returns>The satisfying assignment.</returns>
        private ValueTuple<Dictionary<Variable<T>, long>, Dictionary<VarInt<T>, byte[]>> SatInt(DD value, List<Variable<T>> variables)
        {
            this.Check(value.ManagerId);

            // get the per-bit assignment
            var bitValues = this.Sat(value.Index);

            // convert into values based on user-allocated variables
            var ret = new Dictionary<Variable<T>, long>();
            var retInt = new Dictionary<VarInt<T>, byte[]>();

            foreach (Variable<T> var in variables)
            {
                Check(var.Manager.Uid);

                var len = var.Indices.Length;

                switch (var.Type)
                {
                    case Variable<T>.VariableType.BOOL:
                        bool b = bitValues.ContainsKey(var.Indices[0]) && bitValues[var.Indices[0]];
                        ret.Add(var, b ? 1 : 0);
                        continue;

                    case Variable<T>.VariableType.INT8:
                    case Variable<T>.VariableType.INT16:
                    case Variable<T>.VariableType.INT32:
                    case Variable<T>.VariableType.INT64:
                        long x = 0;
                        for (int i = 0; i < len; i++)
                        {
                            var key = var.Indices[i];
                            if (bitValues.ContainsKey(key) && bitValues[key])
                            {
                                var bitPos = var.GetBitPositionForVariableIndex(key);
                                var shift = len - 1 - bitPos;
                                x |= (1L << shift);
                            }
                        }

                        ret.Add(var, x);
                        break;

                    case Variable<T>.VariableType.INT:
                        var alloc = len % 8 == 0 ? (len / 8) : ((len / 8) + 1);
                        var bytes = new byte[alloc];
                        for (int i = 0; i < len; i++)
                        {
                            var key = var.Indices[i];
                            if (bitValues.ContainsKey(key) && bitValues[key])
                            {
                                var bitPos = var.GetBitPositionForVariableIndex(key);
                                var whichByte = bitPos / 8;
                                var whichIndex = bitPos % 8;
                                var shift = 7 - whichIndex;
                                bytes[whichByte] |= (byte)(1 << shift);
                            }
                        }

                        retInt.Add((VarInt<T>)var, bytes);
                        break;
                }
            }

            return (ret, retInt);
        }

        /// <summary>
        /// Find a satisfying assignment for the function.
        /// </summary>
        /// <param name="value">The function.</param>
        /// <param name="lookingFor">What terminal we are looking for.</param>
        /// <param name="result">Mapping from variable index to value.</param>
        private void Sat(DDIndex value, bool lookingFor, Dictionary<int, bool> result)
        {
            if (value.IsConstant())
            {
                return;
            }

            var node = this.MemoryPool[value.GetPosition()];
            lookingFor = value.IsComplemented() ? !lookingFor : lookingFor;
            var goLeft = (lookingFor && !node.Low.IsZero()) || (!lookingFor && !node.Low.IsOne());
            if (goLeft)
            {
                this.factory.Sat(node, false, result);
                this.Sat(node.Low, lookingFor, result);
                return;
            }

            this.factory.Sat(node, true, result);
            this.Sat(node.High, lookingFor, result);
        }

        /// <summary>
        /// Basic sanity check for nodes provided by the user.
        /// </summary>
        /// <param name="managerId">The manager id.</param>
        private void Check(int managerId)
        {
            if (managerId != this.Uid)
            {
                throw new ArgumentException("Mixed manager id: " + managerId + " with actual id " + this.Uid);
            }
        }

        /// <summary>
        /// Ensure an argument is a power of two.
        /// If not, return the next largest power of two.
        /// </summary>
        /// <param name="arg">The value.</param>
        /// <returns>The argument, possibly to the next power of two value.</returns>
        private int EnsurePowerOfTwo(int arg)
        {
            return Bitops.NextPowerOfTwo(arg);
        }

        /// <summary>
        /// A data structure for a cache result for a single argument.
        /// </summary>
        private struct OperationResult
        {
            /// <summary>
            /// Gets or sets the key argument in the cache.
            /// </summary>
            public DDIndex Arg { get; set; }

            /// <summary>
            /// Gets or sets the result from the operation.
            /// </summary>
            public double Result { get; set; }
        }

        /// <summary>
        /// A data structure capturing a pair of arguments
        /// to an "and" operation.
        /// </summary>
        private struct OperationArg2 : IEquatable<OperationArg2>
        {
            private DDIndex param1;

            private DDIndex param2;

            private DDOperation operation;

            /// <summary>
            /// Initializes a new instance of the <see cref="OperationArg2"/> struct.
            /// </summary>
            /// <param name="param1">The first operand.</param>
            /// <param name="param2">The second operand.</param>
            /// <param name="operation">The apply operation.</param>
            public OperationArg2(DDIndex param1, DDIndex param2, DDOperation operation)
            {
                this.param1 = param1;
                this.param2 = param2;
                this.operation = operation;
            }

            /// <summary>
            /// Equality between a pair of arguments.
            /// </summary>
            /// <param name="other">The other argument.</param>
            /// <returns>Whether the objects are equal.</returns>
            public bool Equals(OperationArg2 other)
            {
                return this.param1.Equals(other.param1) && this.param2.Equals(other.param2) && this.operation == other.operation;
            }

            /// <summary>
            /// Compute the hashcode as a simple sum for performance.
            /// </summary>
            /// <returns>The hash code.</returns>
            public override int GetHashCode()
            {
                var a = this.param1.GetHashCode();
                var b = this.param2.GetHashCode();
                var c = (int)this.operation;
                return a + b + c;
            }
        }

        /// <summary>
        /// A data structure representing the result in the
        /// cache. It sotres the key argument so that we can
        /// compare in the case of a hash collision.
        /// </summary>
        private struct OperationResult2
        {
            /// <summary>
            /// Gets or sets the key argument in the cache.
            /// </summary>
            public OperationArg2 Arg { get; set; }

            /// <summary>
            /// Gets or sets the DD result from the "and" operation.
            /// </summary>
            public DDIndex Result { get; set; }
        }

        /// <summary>
        /// A data structure capturing a pair of arguments
        /// to an "and" operation.
        /// </summary>
        private struct OperationArg3 : IEquatable<OperationArg3>
        {
            private DDIndex param1;

            private DDIndex param2;

            private DDIndex param3;

            /// <summary>
            /// Initializes a new instance of the <see cref="OperationArg3"/> struct.
            /// </summary>
            /// <param name="param1">The first operand.</param>
            /// <param name="param2">The second operand.</param>
            /// <param name="param3">The third operand.</param>
            public OperationArg3(DDIndex param1, DDIndex param2, DDIndex param3)
            {
                this.param1 = param1;
                this.param2 = param2;
                this.param3 = param3;
            }

            /// <summary>
            /// Equality between a pair of arguments.
            /// </summary>
            /// <param name="other">The other argument.</param>
            /// <returns>Whether the objects are equal.</returns>
            public bool Equals(OperationArg3 other)
            {
                return this.param1.Equals(other.param1) &&
                       this.param2.Equals(other.param2) &&
                       this.param3.Equals(other.param3);
            }

            /// <summary>
            /// Compute the hashcode as a simple sum for performance.
            /// </summary>
            /// <returns>The hash code.</returns>
            public override int GetHashCode()
            {
                var a = this.param1.GetHashCode();
                var b = this.param2.GetHashCode();
                var c = this.param3.GetHashCode();
                return a + b + c;
            }
        }

        /// <summary>
        /// A data structure representing the result in the
        /// cache. It sotres the key argument so that we can
        /// compare in the case of a hash collision.
        /// </summary>
        private struct OperationResult3
        {
            /// <summary>
            /// Gets or sets the key argument in the cache.
            /// </summary>
            public OperationArg3 Arg { get; set; }

            /// <summary>
            /// Gets or sets the DD result from the "and" operation.
            /// </summary>
            public DDIndex Result { get; set; }
        }
    }
}