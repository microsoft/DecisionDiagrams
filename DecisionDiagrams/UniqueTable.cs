// <copyright file="UniqueTable.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;

    /// <summary>
    /// Implementation of a specialized dictionary class
    /// for storing DD nodes.
    ///
    /// Based on the implementation in dictionary.cs,
    /// but deviates in several ways:
    /// 1. Uses a power of 2 mask for indexing since
    ///    the BDD library can carefully control the hash code
    /// 2. Allows looking up and modifying a value in a single
    ///    operation, which is not possible otherwise.
    /// 3. Is a minimalist implementation that only supports the
    ///    couple operations needed by the DD implementation
    /// 4. Optimizes lookup using DD invariants such as the
    ///    node age ordering invariant.
    /// </summary>
    /// <typeparam name="T">The key type.</typeparam>
    internal class UniqueTable<T>
        where T : IDDNode
    {
        /// <summary>
        /// The manager object.
        /// </summary>
        private DDManager<T> manager;

        /// <summary>
        /// Indices representing the collision chain.
        /// </summary>
        private int[] buckets;

        /// <summary>
        /// The array of entries.
        /// </summary>
        private Entry[] entries;

        /// <summary>
        /// Power of 2 mask for finding the bucket.
        /// </summary>
        private int mask;

        /// <summary>
        /// Size of the underlying array.
        /// </summary>
        private int count;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniqueTable{T}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        public UniqueTable(DDManager<T> manager)
            : this(manager, 524288)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniqueTable{T}"/> class.
        /// The capacity must be a power of two and align with the mask.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="capacity">The initial capacity.</param>
        private UniqueTable(DDManager<T> manager, int capacity)
        {
            this.manager = manager;
            this.Initialize(capacity);
        }

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        /// <returns>The number of elements.</returns>
        public int Count
        {
            get { return this.count; }
        }

        /// <summary>
        /// Either get the existing value or create a
        /// new one in a single operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value, possibly freshly created.</returns>
        public DDIndex GetOrAdd(T key)
        {
            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            int targetBucket = hashCode & this.mask;
            var loPos = key.Low.GetPosition();
            var hiPos = key.High.GetPosition();

            int i = this.buckets[targetBucket];
            while (i >= 0)
            {
                // because nodes are ordered and added to the unique table by age,
                // we can terminate the search early in some cases.
                var entry = this.entries[i];
                var idx = entry.Value;
                var pos = idx.GetPosition();
                if (loPos >= pos && hiPos >= pos)
                {
                    break;
                }

                if (this.manager.MemoryPool[pos].Equals(key))
                {
                    return idx;
                }

                i = entry.Next;
            }

            int index;
            if (this.count == this.entries.Length)
            {
                this.Resize();
                targetBucket = hashCode & this.mask;
            }

            index = this.count;
            this.count++;
            var value = this.manager.FreshNode(key);

            if (value.IsConstant())
            {
                throw new Exception("BAD");
            }

            this.entries[index] = new Entry { Next = this.buckets[targetBucket], Value = value };
            this.buckets[targetBucket] = index;
            return value;
        }

        /// <summary>
        /// Either get the existing value or create a
        /// new one in a single operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void AddUnchecked(T key, DDIndex value)
        {
            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            int targetBucket = hashCode & this.mask;
            int index = this.count;
            this.count++;
            this.entries[index] = new Entry { Next = this.buckets[targetBucket], Value = value };
            this.buckets[targetBucket] = index;
        }

        /// <summary>
        /// Rebuild the unique table.
        /// </summary>
        /// <param name="newSize">The new number of nodes in use after a GC.</param>
        /// <param name="forwardingAddresses">The index forwarding addresses.</param>
        /// <returns>A new table with valid indicies.</returns>
        public UniqueTable<T> Rebuild(int newSize, int[] forwardingAddresses)
        {
            var values = new DDIndex[newSize];
            var table = new UniqueTable<T>(this.manager, this.Count);
            for (int bucket = 0; bucket < this.buckets.Length; bucket++)
            {
                int i = this.buckets[bucket];
                while (i >= 0)
                {
                    var entry = this.entries[i];
                    var ddindex = entry.Value;
                    var newPosition = forwardingAddresses[ddindex.GetPosition()];
                    if (newPosition != 0)
                    {
                        values[newPosition] = new DDIndex(newPosition, ddindex.IsComplemented());
                    }

                    i = entry.Next;
                }
            }

            for (int i = 1; i < values.Length; i++)
            {
                table.AddUnchecked(this.manager.MemoryPool[i], values[i]);
            }

            return table;
        }

        /// <summary>
        /// Initialize the dictionary with some capacity.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        private void Initialize(int capacity)
        {
            int size = Bitops.NextPowerOfTwo(capacity);
            this.mask = Bitops.BitmaskForPowerOfTwo(size);
            this.buckets = new int[size];
            for (int i = 0; i < this.buckets.Length; i++)
            {
                this.buckets[i] = -1;
            }

            this.entries = new Entry[size];
        }

        /// <summary>
        /// Resize the dictionary by doubling the capacity.
        /// </summary>
        private void Resize()
        {
            this.mask = (this.mask << 1) | 1;
            this.Resize(this.count * 2);
        }

        /// <summary>
        /// Resize each element by rehashing.
        /// </summary>
        /// <param name="newSize">The new size.</param>
        private void Resize(int newSize)
        {
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++)
            {
                newBuckets[i] = -1;
            }

            Entry[] newEntries = new Entry[newSize];
            Array.Copy(this.entries, 0, newEntries, 0, this.count);
            for (int i = 0; i < this.count; i++)
            {
                var position = newEntries[i].Value.GetPosition();
                var hashCode = this.manager.MemoryPool[position].GetHashCode();
                if (hashCode >= 0)
                {
                    int bucket = hashCode & this.mask;
                    newEntries[i].Next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }

            this.buckets = newBuckets;
            this.entries = newEntries;
        }

        /// <summary>
        /// Dictionary entry.
        /// </summary>
        internal struct Entry
        {
            /// <summary>
            /// The next pointer.
            /// </summary>
            public int Next;

            /// <summary>
            /// The value.
            /// </summary>
            public DDIndex Value;
        }
    }
}