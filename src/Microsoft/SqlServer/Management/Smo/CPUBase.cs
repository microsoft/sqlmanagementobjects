// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// CPU class
    /// </summary>
    public sealed class Cpu 
    {
        int id;
        int numaNodeID;
        int groupID;
        bool affinityMask;
        CpuCollection parent;
        
        internal Cpu(int id, int numaNodeID, int groupID, bool affinityMask , CpuCollection parent)
        {
            this.id = id;
            this.numaNodeID = numaNodeID;
            this.groupID = groupID;
            this.affinityMask = affinityMask;
            this.parent = parent;
        }

        /// <summary>
        /// Gets CPU ID
        /// </summary>
       public Int32 ID
        {
            get
            {
                return id;
            }
        }

        /// <summary>
        /// Gets NumaNode ID to which CPU belongs to
        /// </summary>
        public Int32 NumaNodeID
        {
            get
            {
                return numaNodeID;
            }
        }

        /// <summary>
        /// Gets GroupId to which CPU belongs to
        /// </summary>
        public Int32 GroupID
        {
            get
            {
                return groupID;
            }
        }

        /// <summary>
        /// Gets or sets AffinityMask of CPU
        /// </summary>
        public bool AffinityMask
        {
            get
            {
                return affinityMask;
            }
            set
            {
                affinityMask = value;
                this.parent.setByUser = true;
            }
        }

        internal void InitAffinityMask(bool value)
        {
            this.affinityMask = value;
        }    
    }
}
