// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Defines Affinity Type of NumaNode
    /// </summary>
    /* Full refers to all the CPU's belonging to Numa are Affinitnized
       None means none of the CPU's belonging to Numa is affinitnized
       Partial means some of the CPU's are affitnized and some are not. 
     */
    public enum NumaNodeAffinity 
    {
        Full,
        None,
        Partial
    };

    /// <summary>
    /// NumaNode Class
    /// </summary>
    public sealed class NumaNode
    {
        int id;
        int groupID;
        AffinityInfoBase affInfo;
        
        internal NumaNode(int id, int groupID, AffinityInfoBase parent)
        {
            this.id = id;
            this.groupID = groupID;
            this.affInfo = parent;
        }

        CpuCollection cpuCol;
        /// <summary>
        /// Gets CPU's which are associated to this NumaNode
        /// </summary>
        public CpuCollection Cpus
        {
            get
            {
                if (null == cpuCol)
                {
                    cpuCol = new CpuCollection(this.affInfo);
                }
                return cpuCol;
            }
        }

        /// <summary>
        /// Gets ID of NuamNode
        /// </summary>
        public Int32 ID
        {
            get
            {
                return id;
            }
        }

        /// <summary>
        /// Gets GroupId to which this NumaNode belongs
        /// </summary>
        public Int32 GroupID
        {
            get
            {
                return groupID;
            }
        }

        /// <summary>
        /// Gets or sets AffinityMask of NumaNode
        /// </summary>
        public NumaNodeAffinity AffinityMask
        {
            get
            {
                /*Traverse through all the CPU's present in NumaNode and add number of non affitinized CPUs to a variable
                 * Case 1 : If  toReturn remains 0 , this means NumaNode was fully affitinized
                 * Case 2: if toReturn equals CPU's present in Numanode then it was not affintinized
                 * Case 3: else it was partial */
                int toReturn = 0;
                foreach (Cpu c in this.Cpus)
                {
                    if (c.AffinityMask == false)
                    {
                        toReturn = toReturn + 1;
                    }
                }
                if (toReturn == 0)
                {
                    return NumaNodeAffinity.Full;
                }
                if (toReturn == this.Cpus.Count)
                {
                    return NumaNodeAffinity.None;
                }
                return NumaNodeAffinity.Partial;
            }

            set
            {
                if (value != NumaNodeAffinity.Partial) //If Tried to set partial , throw exception
                {
                    if (value == NumaNodeAffinity.Full) //If full then affitinize all the CPU's belonging to a NumaNode
                    {
                        foreach (Cpu c in this.Cpus)
                        {
                            c.AffinityMask = true;
                        }
                    }

                    else
                    {
                        foreach (Cpu c in this.Cpus) //If None then affitinize all the CPU's belonging to a NumaNode
                        {
                            c.AffinityMask = false;
                        }
                    }
                }
                else
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.AffinityValueCannotBeSet);
                }
            }
        }

    }

}
