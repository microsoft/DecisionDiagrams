// <copyright file="HandleTable.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Simple fast implementation of a specialized dictionary
    /// class for storing DD nodes.
    ///
    /// Based on the implementation in dictionary.cs,
    /// but deviates in several ways:
    /// 1. Uses a power of 2 mask for indexing since
    ///    the BDD library can carefully control the hash code
    /// 2. Allows looking up and modifying a value in a single
    ///    operation, which is not possible otherwise.
    /// 3. Is a minimalist implementation that only supports the
    ///    couple operations needed by the DD implementation.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    internal class HandleTable<T>
        where T : IDDNode
    {
        /// <summary>
        /// The manager.
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
        /// Initializes a new instance of the <see cref="HandleTable{T}"/> class.
        /// </summary>
        /// <param name="manager">The DD manager.</param>
        public HandleTable(DDManager<T> manager)
            : this(manager, 65536)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleTable{T}"/> class.
        /// The capacity must be a power of two and align with the mask.
        /// </summary>
        /// <param name="manager">The DD manager.</param>
        /// <param name="capacity">The initial capacity.</param>
        private HandleTable(DDManager<T> manager,  int capacity)
        {
            this.manager = manager;
            this.Initialize(capacity);
        }

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        /// <returns>The number of elements.</returns>
        public int Count { get; private set; }

        /// <summary>
        /// Either get the existing value or create a
        /// new one in a single operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value, possibly freshly created.</returns>
        public DD GetOrAdd(DDIndex key)
        {
            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            int targetBucket = hashCode & this.mask;
            for (int i = this.buckets[targetBucket]; i >= 0; i = this.entries[i].Next)
            {
                if (this.entries[i].Key.Equals(key))
                {
                    var wref = this.entries[i].Value;
                    if (!wref.TryGetTarget(out DD target))
                    {
                        target = new DD(this.manager.Uid, key);
                        wref.SetTarget(target);
                    }

                    return target;
                }
            }

            int index;
            if (this.Count == this.entries.Length)
            {
                this.Resize();
                targetBucket = hashCode & this.mask;
            }

            index = this.Count;
            this.Count++;
            var dd = new DD(this.manager.Uid, key);
            var value = new WeakReference<DD>(dd);
            this.entries[index] = new Entry { Next = this.buckets[targetBucket], Key = key, Value = value };
            this.buckets[targetBucket] = index;
            return dd;
        }

        /// <summary>
        /// Mark all live nodes.
        /// </summary>
        public void MarkAllLive()
        {
            for (int bucket = 0; bucket < this.buckets.Length; bucket++)
            {
                int i = this.buckets[bucket];
                while (i >= 0)
                {
                    var position = this.entries[i].Key.GetPosition();
                    var externalHandle = this.entries[i].Value;
                    if (position != 0 && externalHandle.TryGetTarget(out _))
                    {
                        this.manager.MemoryPool[position].Mark = true;
                    }

                    i = this.entries[i].Next;
                }
            }
        }

        /// <summary>
        /// Remove all elements matching a predicate.
        /// </summary>
        /// <param name="forwardingAddresses">The forwarding addresses.</param>
        /// <returns>A new handle table after garbage collection.</returns>
        public HandleTable<T> Rebuild(int[] forwardingAddresses)
        {
            var table = new HandleTable<T>(this.manager, this.Count);
            for (int bucket = 0; bucket < this.buckets.Length; bucket++)
            {
                int i = this.buckets[bucket];
                while (i >= 0)
                {
                    var entry = this.entries[i];
                    var index = entry.Key;
                    var wref = entry.Value;
                    var position = index.GetPosition();
                    var newPosition = forwardingAddresses[position];
                    if (newPosition != 0 || position == 0)
                    {
                        var newIndex = new DDIndex(newPosition, index.IsComplemented());
                        if (wref.TryGetTarget(out DD target))
                        {
                            target.Index = newIndex;
                            table.AddUnique(newIndex, wref);
                        }
                    }

                    i = entry.Next;
                }
            }

            return table;
        }

        /// <summary>
        /// Either get the existing value or create a
        /// new one in a single operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        private void AddUnique(DDIndex key, WeakReference<DD> value)
        {
            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            int targetBucket = hashCode & this.mask;
            int index = this.Count;
            this.Count++;
            this.entries[index] = new Entry { Next = this.buckets[targetBucket], Key = key, Value = value };
            this.buckets[targetBucket] = index;
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
            this.Resize(this.Count * 2);
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
            Array.Copy(this.entries, 0, newEntries, 0, this.Count);
            for (int i = 0; i < this.Count; i++)
            {
                var hashCode = newEntries[i].Key.GetHashCode();
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
            /// The key.
            /// </summary>
            public DDIndex Key;

            /// <summary>
            /// The value.
            /// </summary>
            public WeakReference<DD> Value;
        }
    }
}