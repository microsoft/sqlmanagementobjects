// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// ExternalResourcePoolAffinityInfo 
    /// </summary>
    public sealed class ExternalResourcePoolAffinityInfo : AffinityInfoBase
    {
        internal ExternalResourcePool externalResourcePool;
        public ExternalResourcePoolAffinityInfo(ExternalResourcePool parent)
        {
            this.externalResourcePool = parent;
            this.PopulateDataTable();
        }

        /// <summary>
        /// Returns parent of the object
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject)]
        public ExternalResourcePool Parent
        {
            get
            {
                return this.externalResourcePool;
            }
        }

        internal override SqlSmoObject SmoParent
        {
            get { return this.Parent as SqlSmoObject; }
        }

        public static string UrnSuffix
        {
            get
            {
                return "ExternalResourcePoolAffinityInfo";
            }
        }

        private CpuCollection cpuCol;
        public CpuCollection Cpus
        {
            get
            {
                if (null == table)
                {
                    PopulateDataTable();
                }
                if (null == cpuCol)
                {
                    cpuCol = new CpuCollection(this);
                }
                return cpuCol;
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            this.cpuCol = null;
        }

        internal override void PopulateDataTable()
        {
            int poolId;
            bool isCreating;

            // Check if the external resource pool is in the process of being created.
            // If so, use the 'default' external resource pool as a reference to construct the memory layout and
            // populate the affinity information, since there will be no record on the server for the new external pool yet.
            //
            // The reason to use the 'default' external pool as the reference is that it is the best suited for a newly
            // created pool, because it always exists.
            // However the default external pool's affinity can be changed by the user. So the new pool's affinity won't
            // neccessarily be the same as the default pool's.
            // For this reason we manually set the affinity of all external pools to initially be 'auto', which is the
            // default for a newly created pool.
            // If the pool isn't being created, we change the affinity later to 'manual' in SetCPUAndNumaValues() when
            // first processing an affinitized record
            if (this.externalResourcePool.State == SqlSmoState.Creating)
            {
                poolId = 2; // the 'default' external resource pool id
                isCreating = true;
            }
            else
            {
                poolId = this.externalResourcePool.ID;
                isCreating = false;
            }

            // Format the request
            Request req = new Request("Server/ResourceGovernor/ExternalResourcePool/ExternalResourcePoolAffinityInfo[@PoolID=" + poolId + "]");

            // Execute the request
            this.table = externalResourcePool.Parent.ExecutionManager.GetEnumeratorData(req);
            if (this.table.Rows.Count == 0)
            {
                throw new SmoException(ExceptionTemplates.ResourceGovernorPoolMissing);
            }

            this.AffinityType = AffinityType.Auto;

            // Create the CPU and numa objects and add them to the collection
            this.SetCPUAndNumaValues(isCreating);
        }

        // Create and add new CPU and Numanode objects to their collections
        private void SetCPUAndNumaValues(bool isCreating)
        {
            Int64 bitMap;
            char[] cpuBitMap;
            char[] cpuAffinityMap;
            int currentCpuCount = 0;
            int numaNodeCount = 0;

            foreach (DataRow data in this.table.Rows)
            {
                Cpu cpu;

                bitMap = (long)data["CpuIds"];
                cpuBitMap = Convert.ToString(bitMap, 2).PadLeft(64, '0').ToCharArray();
                Array.Reverse(cpuBitMap);

                bitMap = (long)data["CpuAffinityMask"];
                cpuAffinityMap = Convert.ToString(bitMap, 2).PadLeft(64, '0').ToCharArray();
                Array.Reverse(cpuAffinityMap);

                int numaNodeId = (int)data["NumaNodeId"];
                int groupId = (int)data["GroupID"];

                // If this CPU if affinitized, set the affinity type accordingly
                //
                // DEVNOTE:
                //  We skip setting the affinity type to manual if isCreating=true because new pools are always
                // created with AffinityType=Auto.
                //  (Because by design we to use the default external pool as a reference, and its affinity might
                //  not be Auto, we need to ignore it)
                if ((int)data["AffinityType"] == 1 && !isCreating)
                {
                    this.AffinityType = AffinityType.Manual;
                }

                // Create new numaNode and add it to the collection
                NumaNode nNode = new NumaNode(numaNodeId, groupId, this);
                this.NumaNodes.numaNodeCol.Add(numaNodeCount++, nNode);

                int cpuCount = 0;

                // create CPU object and Add it to numaNode and Cpu Collection
                for (int i = 0; i < cpuBitMap.Length; i++)
                {
                    if (cpuBitMap[i] == '1') // If cpuBitMap is 1 this means there is a cpu at that position
                    {
                        int cpuId = i + (groupId * 64); //Calculate the ID of CPU
                        if (this.AffinityType == AffinityType.Auto)
                        {
                            cpu = new Cpu(cpuId, numaNodeId, groupId, false, this.Cpus);
                        }
                        else
                        {
                            cpu = new Cpu(cpuId, numaNodeId, groupId, cpuAffinityMap[i].Equals('1'), this.Cpus);
                        }

                        this.Cpus.cpuCol.Add(currentCpuCount++, cpu);   // Add CPU to CPUCollection
                        nNode.Cpus.cpuCol.Add(cpuCount++, cpu);         // Add Cpu from 0 position in each numanode , this will remove the ambiguity in for and foreach
                    }
                }
            }
        }

        internal override StringCollection DoAlter(ScriptingPreferences sp)
        {
            // External resource pool affinity is in SQL15 and above
            if (sp.TargetServerVersion < SqlServerVersion.Version130)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(SmoApplication.DefaultCulture,
                "ALTER EXTERNAL RESOURCE POOL {0} WITH (",
                Parent.FormatFullNameForScripting(sp));

            StringCollection sc = DoAlterInternal(sp);
            foreach (string str in sc)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, str);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, ")");

            StringCollection query = new StringCollection();
            query.Add(sb.ToString());
            return query;
        }

        internal StringCollection DoAlterInternal(ScriptingPreferences sp)
        {
            // External resource pool affinity is in SQL15 and above
            if (sp.TargetServerVersion < SqlServerVersion.Version130)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("AFFINITY ");
            if (this.AffinityType == AffinityType.Auto)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "CPU = AUTO");
                if (!this.Cpus.setByUser)
                {
                    StringCollection query = new StringCollection();
                    query.Add(sb.ToString());
                    return query;
                }
                else
                {
                    // AUTO cannot be set if any numa node or CPU were affitinized by user
                    throw new WrongPropertyValueException(ExceptionTemplates.AffinityTypeCannotBeSet);
                }
            }

            // Try adding numanode script, with the parenthesis variant
            StringCollection sc = this.NumaNodes.AddNumaInDdl(sb);
            if (sc == null)
            {
                // if numanode query retruned null it means that no numa nodes were manually affinitized
                // by the user. Try adding CPU script.
                sc = this.Cpus.AddCpuInDdl(sb);
            }

            return sc;
        }
    }
}

