// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Scheduler class
    /// </summary>
    public sealed class Scheduler
    {
        int id;
        Cpu cpu;
        bool affinityMask;
        SchedulerCollection parent;
        
        internal Scheduler(int id, Cpu cpu, bool affinityMask , SchedulerCollection parent)
        {
            this.id = id;
            this.cpu = cpu;
            this.affinityMask = affinityMask;
            this.parent = parent;
        }

        /// <summary>
        /// Gets Scheduler ID
        /// </summary>
       public Int32 Id
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// Gets CPU object which the scheduler is assigned to 
        /// </summary>
        public Cpu Cpu
        {
            get
            {
                return this.cpu;
            }
        }

        /// <summary>
        /// Gets or sets AffinityMask of scheduler
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
    
    }
}
