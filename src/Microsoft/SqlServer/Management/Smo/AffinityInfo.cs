// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;



#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587
namespace Microsoft.SqlServer.Management.Smo
{
    //
    // For information on concepts like CPU, Processor, Group, NUMA, etc... see, for example:
    // - https://docs.microsoft.com/windows/win32/procthread/processor-groups
    // - https://docs.microsoft.com/sql/database-engine/configure-windows/soft-numa-sql-server
    //
    /// <summary>
    /// AffinityInfo
    /// </summary>
    public sealed class AffinityInfo : AffinityInfoBase
    {
        /// <summary>
        /// Number of CPUs per K-Group.
        /// </summary>
        private const byte CPUSPERKGROUP = 64;

        internal Server server;
        internal AffinityInfo(Server parentsrv)
        {
            server = parentsrv;
            PopulateDataTable();
        }

        /// <summary>
        /// This ctor will avoid querying the server to fetch the Affinity Info Table.
        /// Typically used as a test hook to validate this class without hitting an
        /// actual server.
        /// </summary>
        /// <param name="affinityInfoTable"></param>
        //  Here's an example of such a table:
        //  - 1 K-Group (obviously: there are less than 64 processors)
        //  - 4 logical processors (aka CPUs)
        //  - CPU 0 and 1 online; CPU 2 and 3 offline
        //  - all logical processor belonging to the same (hardware) NUMANODE (ID=0)
        //  - you cannot really tell if there are soft-NUMA nodes from this table
        //  AffinityType    NodeStateDesc    ID    GroupID    CpuIds    CpuAffinityMask
        //  1               OFFLINE          0     0          12        0   
        //  1               ONLINE           0     0          3         3
        //  1               ONLINE DAC       64    0          0         1
        internal AffinityInfo(DataTable affinityInfoTable)
        {
            table = affinityInfoTable;
            PopulateDataTable();
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "AffinityInfo";
            }
        }

        /// <summary>
        /// Returns Parent of the object
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                //CheckObjectState();
                return server;
            }
        }

        /// <summary>
        /// Getting CPUcollection instance
        /// </summary>
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
            cpuCol = null;
        }

        internal override SqlSmoObject SmoParent
        {
            get { return Parent as SqlSmoObject; }
        }

        //Will internally populate the DataTable 
        internal override void PopulateDataTable()
        {
            // Server should be null only if we are running unit tests, i.e.
            // the test hook ctor AffinityInfo(DataTable) was invoked earlier on.
            if (server != null)
            {
                // format the request
                Request req = new Request("Server/AffinityInfo");

                // execute the request
                Enumerator e = new Enumerator();
                table = server.ExecutionManager.GetEnumeratorData(req);
            }

            // Set Affinity Type in CPUCollection.
            // Note: all the rows in the AffinityInfo table will have the same value by 
            //       construction of the table, so it is ok to check the first row.
            //       The value is really the scalar:
            //           select affinity_type from sys.dm_os_sys_info
            AffinityType =
                (int)table.Rows[0]["AffinityType"] == 1
                ? AffinityType = AffinityType.Manual
                : AffinityType = AffinityType.Auto;

            // This Function will create CPU and Numa Objects and add them to the collection
            SetCPUAndNumaValues();
        }

        //This will create DDL script for alter
        internal override StringCollection DoAlter(ScriptingPreferences sp)
        {
            /*
            // ALTER SERVER CONFIGURATION SET PROCESS AFFINITY 
            // CPU  = { AUTO | <range_spec> } 
            // | NUMANODE  = <range_spec>
            // [;]

            // <range_spec> ::= { <integer> | <integer> TO <integer> } [,...n]
             */

            StringCollection query = new StringCollection();
            // G64 is only supported after KJ (10.5)
            if (sp.TargetServerVersion < SqlServerVersion.Version105)
            {
                return query;
            }

            // handle possible Affinity info options changes if user ever touched the object
            if (null == this.AffinityInfoTable)
            {
                return null;
            }


            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER SERVER CONFIGURATION SET PROCESS AFFINITY ");
            if (this.AffinityType == AffinityType.Auto)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "CPU = AUTO");
                if (!this.Cpus.setByUser) //If user set's affinity to any of the cpu or NumaNode
                {
                    query.Add(sb.ToString());
                    return query;
                }
                else
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.AffinityTypeCannotBeSet); // AUTO cannot go with Anything (NUMA or CPU )Affitinized by user.
                }

            }
            query.Add(sb.ToString());

            StringCollection sc = this.NumaNodes.AddNumaInDdl(sb); //Try Adding numanode query
            if (sc == null)
            {
                sc = this.Cpus.AddCpuInDdl(sb); // if numanode query retruned null try adding CPU query.

            }
            return sc;
        }

        //Will internally create and Add new CPU and Numa objects to their collections
        internal void SetCPUAndNumaValues()
        {
            char[] cpuBitMap;
            char[] cpuAffinityMap;

            Dictionary<int, List<Cpu>> numaNodeIDsToCPUs = new Dictionary<int, List<Cpu>>();
            Dictionary<int, List<NumaNode>> groupIDsToNumaNodes = new Dictionary<int, List<NumaNode>>();

            // DEVNOTE: the OrderBy GroupID is used to make sure reflects the physical constraint that the OS allocated
            //          CPUs (aka logical processors) have properly incrementing (not necessarily consecutive) IDs.
            foreach (var data in (from DataRow dr in table.Rows
                                 // Ignore the DAC line
                                  let isOnlineDAC = ((string)dr["NodeStateDesc"]).Equals("ONLINE DAC", StringComparison.OrdinalIgnoreCase)
                                  where !isOnlineDAC
                                  select new
                                  {
                                      GroupID = (int)dr["GroupID"],
                                      NodeId = (int)dr["ID"],
                                      CpuIds = (long)dr["CpuIds"],
                                      CpuAffinityMask = (long)dr["CpuAffinityMask"]
                                  }).OrderBy(dr => dr.GroupID))
            {
                cpuBitMap = Convert.ToString(data.CpuIds, 2).PadLeft(CPUSPERKGROUP, '0').ToCharArray();
                Array.Reverse(cpuBitMap);

                cpuAffinityMap = Convert.ToString(data.CpuAffinityMask, 2).PadLeft(CPUSPERKGROUP, '0').ToCharArray();
                Array.Reverse(cpuAffinityMap);

                if (!groupIDsToNumaNodes.ContainsKey(data.GroupID))
                {
                    groupIDsToNumaNodes.Add(data.GroupID, new List<NumaNode>());
                }

                if (!numaNodeIDsToCPUs.ContainsKey(data.NodeId))
                {
                    numaNodeIDsToCPUs.Add(data.NodeId, new List<Cpu>());
                }

                if (groupIDsToNumaNodes[data.GroupID].SingleOrDefault(x => x.ID == data.NodeId) == null)
                {
                    groupIDsToNumaNodes[data.GroupID].Add(new NumaNode(data.NodeId, data.GroupID, this));
                }

                // Create CPU objects and collect them: later on, they'll be added to the NumaNode and Cpus collection
                for (int i = 0; i < cpuBitMap.Length; i++)
                {
                    if (cpuBitMap[i] == '1') //If cpuBitMap is 1 this means there is a cpu at that position
                    {
                        var cpu =
                            new Cpu(
                                i + CPUSPERKGROUP * data.GroupID,
                                data.NodeId,
                                data.GroupID,
                                AffinityType == AffinityType.Auto ? false : cpuAffinityMap[i].Equals('1'),
                                Cpus);

                        System.Diagnostics.Debug.Assert(
                            !numaNodeIDsToCPUs[data.NodeId].Any(x => x.GroupID == data.GroupID && x.ID == cpu.ID && x.NumaNodeID == data.NodeId),
                            $"You cannot add the same CPU {cpu.ID} to node {data.NodeId} in group {data.GroupID} more than once!");

                        numaNodeIDsToCPUs[data.NodeId].Add(cpu);
                    }
                }
            }

            // Add CPUs to CPUCollection
            // This is the collection of all the CPUs, regardless of the (hardware) NUMA node they
            // belong to.
            foreach (var cpu in numaNodeIDsToCPUs.SelectMany(x => x.Value).OrderBy(c => c.ID))
            {
                // Note that 'cpuCol' is (currently) a SortedList: by using 'Cpus.cpuCol.Count' as
                // the key for the SortedList, we are guaranteed an increasing and contiguous ordinal
                // key (which is what the collection expects); the CPU elements are sorted by virtue
                // of the fact that we sorted the collection we are iterating over (though, it is not
                // guaranteed for the CPU IDs (cpu.ID) to be contiguous).
                Cpus.cpuCol.Add(Cpus.cpuCol.Count, cpu);
            }

            // Add CPU to each (hardware) NUMA node.
            //
            // The CPUs that were identified above are now partitioned and grouped by the (hardware)
            // NUMA node they belong to.
            //
            // Notes:
            // - 'nNode.Cpus.cpuCol' is a (currently) a SortedList: by using 'nNode.Cpus.cpuCol.Count' 
            //   as the key for the the SortedList, we are guaranteed an increasing and contigous ordinal
            //   key (which is what the collection expects); the CPU elements are sorted by virtue
            //   of the fact that we sorted the collection we are iterating over (though, it is not
            //   guaranteed for the CPU IDs (cpu.ID) to be contiguous).
            // - 'NumaNodes.numaNodeCol' is a (currently) a SortedList: by using 'NumaNodes.numaNodeCol.Count' 
            //   as the key for the the SortedList, we are guaranteed an increasing and contigous ordinal
            //   key (which is what the collection expects); the NUMA node elements are sorted by virtue
            //   of the fact that we sorted the collection we are iterating over. Contrary to CPUs, it just
            //   happens that the NUMA node PU IDs (nNode.ID) are contiguous and identical to the key of the
            //   SortedList.
            // - The group ID does not really partecipate in any ordinal logic of nodes (you can't have 
            //   NUMA nodes with the same ID in different groups)
            foreach (var gnn in groupIDsToNumaNodes.OrderBy(g => g.Key))
            {
                var groupId = gnn.Key;
                foreach (var nNode in gnn.Value.OrderBy(nn => nn.ID))
                {
                    foreach (var cpu in numaNodeIDsToCPUs[nNode.ID].OrderBy(cpu => cpu.ID))
                    {
                        nNode.Cpus.cpuCol.Add(nNode.Cpus.cpuCol.Count, cpu);
                    }

                    NumaNodes.numaNodeCol.Add(NumaNodes.numaNodeCol.Count, nNode);
                }
            }
        }
    }
}

