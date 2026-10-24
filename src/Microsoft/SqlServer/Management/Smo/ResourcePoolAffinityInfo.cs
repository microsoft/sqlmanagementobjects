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
    ///ResourcePoolAffinityInfo 
    /// </summary>
    public sealed class ResourcePoolAffinityInfo : AffinityInfoBase
    {
        internal ResourcePool resourcePool;
        internal ResourcePoolAffinityInfo(ResourcePool parent)           
        {
            this.resourcePool = parent;
            this.PopulateDataTable();
        }

        /// <summary>
        /// Returns Parent of the object
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject)]
        public ResourcePool Parent
        {
            get
            {
                //CheckObjectState();
                return this.resourcePool;
            }
        }

        internal override SqlSmoObject SmoParent
        {
            get { return this.Parent as SqlSmoObject; }
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "ResourcePoolAffinityInfo";
            }
        }

        /// <summary>
        /// Getting CPUcollection instance
        /// </summary>
        private SchedulerCollection schedulerCollection;
        public SchedulerCollection Schedulers
        {
            get
            {
                if (null == table)
                {
                    PopulateDataTable();
                }
                if (null == schedulerCollection)
                {
                    schedulerCollection = new SchedulerCollection(this);
                }
                return schedulerCollection;
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            this.schedulerTable = null;
            this.schedulerCollection = null;
        }

        //Will internally populate the DataTable 
        internal override void PopulateDataTable()
        {
            int poolId;

            // Check if the resource pool is in the creation state, if so, use the 'internal' resource pool as a reference to populate
            // affinity information since there will be no record on the server for the pool yet.
            //
            // The reason to use the 'internal' pool as the reference is that it is the best suited for a newly created pool:
            //     1- It always exists.
            //     2- It always has 'auto' affinity, which the default for a newly created pool

            if (this.resourcePool.State == SqlSmoState.Creating)
            {
                poolId = 1; // the 'internal' resource pool id.
            }
            else
            {
                poolId = this.resourcePool.ID;
            }

            // format the request
            Request req = new Request("Server/ResourceGovernor/ResourcePool/ResourcePoolAffinityInfo[@PoolID=" + poolId + "]");

            // execute the request
            this.table = resourcePool.Parent.ExecutionManager.GetEnumeratorData(req);

            //check if you have any rows returned
            if (this.table.Rows.Count == 0)
            {
                throw new SmoException(ExceptionTemplates.ResourceGovernorPoolMissing);
            }

            //Set Affinity Type.
            if (1 == (int)this.table.Rows[0]["AffinityType"])
            {
                this.AffinityType = AffinityType.Manual;
            }
            else
            {
                this.AffinityType = AffinityType.Auto;
            }

            //this Function will create CPU and Numa Objects and add them to the collection
            this.SetNumaValues();

            //Get the scheudlers information and populate the scheduler tables
            req = new Request("Server/ResourceGovernor/ResourcePool/ResourcePoolScheduler[@PoolId=" + poolId + "]");

            // execute the request
            this.schedulerTable = resourcePool.Parent.ExecutionManager.GetEnumeratorData(req);
            this.SetSchedulerValues();
        }

        internal void SetSchedulerValues()
        {
            long prevSchedulerMask = 0;
            char[] schedulerBitMap = Convert.ToString(prevSchedulerMask, 2).PadLeft(64, '0').ToCharArray(); //Convert it to char array
            Array.Reverse(schedulerBitMap);

            //iterate over all scheduler, for each one set the affinity of the pool according to the bitmask
            foreach (DataRow data in this.schedulerTable.Rows)
            {
                bool schedulerAffinity;

                int numaNodeId = (int)data["NumaNodeId"];
                int cpuId = (int)data["CpuId"];
                int schedulerId = (int)data["SchedulerId"];
                long schedulerMask = (long)data["SchedulerMask"];

                //compute a new bitmap if the mask has changed
                if (schedulerMask != prevSchedulerMask)
                {
                    prevSchedulerMask = schedulerMask;
                    schedulerBitMap = Convert.ToString(prevSchedulerMask, 2).PadLeft(64, '0').ToCharArray(); //Convert it to char array
                    Array.Reverse(schedulerBitMap);
                }
                schedulerAffinity = this.AffinityType == Smo.AffinityType.Auto ? false : schedulerBitMap[schedulerId % 64] == '1';

                Scheduler scheduler = new Scheduler(schedulerId, this.NumaNodes.GetByID(numaNodeId).Cpus.GetByID(cpuId), schedulerAffinity, this.Schedulers);
                this.Schedulers.schedulerList.Add(schedulerId, scheduler);

                //set the corresponding cpu affinity
                scheduler.Cpu.InitAffinityMask(schedulerAffinity);
            }
        }

        //Will internally create and Add new CPU and Numa objects to their collections
        internal void SetNumaValues()
        {
            Int64 bitMap;

            char[] cpuBitMap;
            char[] cpuAffinityMap;
            int numaNodeCount = 0;

            foreach (DataRow data in this.table.Rows)
            {
                Cpu cpu;

                bitMap = (long)data["CpuIds"]; // get the cpuBitMap
                cpuBitMap = Convert.ToString(bitMap, 2).PadLeft(64, '0').ToCharArray(); //Convert it to char array
                Array.Reverse(cpuBitMap);
                bitMap = (long)data["CpuAffinityMask"]; //get AffinityInfo bit Map
                cpuAffinityMap = Convert.ToString(bitMap, 2).PadLeft(64, '0').ToCharArray(); // convert it to CPU Affinity char array
                Array.Reverse(cpuAffinityMap);

                int nodeId = (int)data["ID"];  // get NodeId
                int groupId = (int)data["GroupID"]; // get GroupId

                //Create new numaNode and add it to the collection
                NumaNode nNode = new NumaNode(nodeId, groupId, this);
                this.NumaNodes.numaNodeCol.Add(numaNodeCount++, nNode);

                int cpuCount = 0;
                //create CPU object and Add it to numaNode and Cpu Collection
                for (int i = 0; i < cpuBitMap.Length; i++)
                {
                    if (cpuBitMap[i] == '1') //If cpuBitMap is 1 this means there is a cpu at that position
                    {
                        int cpuId = i + (groupId * 64); //Calculate the ID of CPU

                        //consider the cpu unaffinitized by default until we set the affinity later when we get the corresponding scheduler
                        cpu = new Cpu(cpuId, nodeId, groupId, false, nNode.Cpus); 

                        nNode.Cpus.cpuCol.Add(cpuCount++, cpu); //Adding Cpu from 0 position in each numanode , this will remove the ambiguity in for and foreach
                    }
                }
            }
        }

        //This will create DDL script for alter
        internal override StringCollection DoAlter(ScriptingPreferences sp)
        {
            //resource pool scheduler affinity is in Denali and above
            if (sp.TargetServerVersion < SqlServerVersion.Version110)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(SmoApplication.DefaultCulture,
                "ALTER RESOURCE POOL {0} WITH (",
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
            //resource pool scheduler affinity is in Denali and above
            if (sp.TargetServerVersion < SqlServerVersion.Version110)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("AFFINITY ");
            if (this.AffinityType == AffinityType.Auto)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "SCHEDULER = AUTO");
                if (!this.Schedulers.setByUser && !this.NumaNodes.IsManuallySet()) //If user sets affinity to any of the schedulers or NumaNodes
                {
                    StringCollection query = new StringCollection();
                    query.Add(sb.ToString());
                    return query;
                }
                else
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.AffinityTypeCannotBeSet); // AUTO cannot go with Anything (NUMA or scheduler) Affitinized by user.
                }
            }
           
            StringCollection sc = this.NumaNodes.AddNumaInDdl(sb); //Try Adding numanode query, with the parenthesis variant
            if (sc == null)
            {
                sc = this.Schedulers.AddSchedulerInDdl(sb); // if numanode query retruned null try adding scheduler script.
            }

            return sc;
        }

        private DataTable schedulerTable;
    }
}

