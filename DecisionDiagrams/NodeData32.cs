// <copyright file="NodeData32.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    /// <summary>
    /// Common node metadata for packing data together.
    /// </summary>
    internal struct NodeData32
    {
        /// <summary>
        /// The data as a packed 32-bit integer.
        /// </summary>
        private uint data;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeData32"/> struct.
        /// </summary>
        /// <param name="variable">The variable id.</param>
        /// <param name="mark">The GC mark.</param>
        /// <param name="metadata">16 bits of metadata.</param>
        public NodeData32(int variable, bool mark, int metadata = 0)
        {
            this.data = unchecked((uint)variable);
            this.Metadata = metadata;
            this.Mark = mark;
        }

        /// <summary>
        /// Gets the variable id.
        /// </summary>
        public int Variable
        {
            get
            {
                return unchecked((int)this.data & 0x00007FFF);
            }
        }

        /// <summary>
        /// Gets or sets the node metadata.
        /// </summary>
        public int Metadata
        {
            get
            {
                return unchecked((int)this.data & 0x7FFF8000) >> 15;
            }

            set
            {
                this.data |= unchecked((uint)(value << 15));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the GC mark bit is set.
        /// </summary>
        public bool Mark
        {
            get
            {
                return (this.data >> 31) == 1;
            }

            set
            {
                if (value)
                {
                    this.data |= 0x80000000;
                }
                else
                {
                    this.data &= 0x7FFFFFFF;
                }
            }
        }
    }
}
