// <copyright file="NodeData16.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams
{
    /// <summary>
    /// Common node metadata type for packing data together.
    /// </summary>
    internal struct NodeData16
    {
        /// <summary>
        /// The data as a packed 32-bit integer.
        /// </summary>
        private ushort data;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeData16"/> struct.
        /// </summary>
        /// <param name="variable">The variable id.</param>
        /// <param name="mark">The GC mark.</param>
        public NodeData16(int variable, bool mark)
        {
            this.data = unchecked((ushort)variable);
            this.Mark = mark;
        }

        /// <summary>
        /// Gets the variable id.
        /// </summary>
        public int Variable
        {
            get
            {
                return unchecked((int)(this.data & 0x7FFF));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the GC mark bit is set.
        /// </summary>
        public bool Mark
        {
            get
            {
                return (this.data >> 15) == 1;
            }

            set
            {
                if (value)
                {
                    this.data |= 0x8000;
                }
                else
                {
                    this.data &= 0x7FF;
                }
            }
        }
    }
}
