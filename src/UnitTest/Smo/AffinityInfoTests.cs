// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.Data;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests for the AffinityInfo object
    /// </summary>
    [TestClass]
    public class AffinityInfoTests : UnitTestBase
    {
        [TestCategory("Unit")]
        [TestMethod]
        public void AffinityInfo_SetCPUAndNumaValues()
        {
            var dt = new DataTable();

            dt.Columns.Add("AffinityType", typeof(int));
            dt.Columns.Add("NodeStateDesc", typeof(string));
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("GroupID", typeof(int));
            dt.Columns.Add("CpuIds", typeof(long));
            dt.Columns.Add("CpuAffinityMask", typeof(long));

            Assert.Multiple(
                () =>
                {
                    dt.Rows.Clear();
                    dt.Rows.Add(1, "OFFLINE", 0, 0, 12, 0);
                    dt.Rows.Add(1, "ONLINE", 0, 0, 3, 3);
                    dt.Rows.Add(1, "ONLINE DAC", 64, 0, 0, 1);
                    RunAffinityInfoTest(dt, "T1");

                    dt.Rows.Clear();
                    dt.Rows.Add(1, "ONLINE DAC", 64, 0, 0, 1);
                    dt.Rows.Add(1, "ONLINE", 0, 0, 3, 3);
                    dt.Rows.Add(1, "OFFLINE", 0, 0, 12, 0);
                    RunAffinityInfoTest(dt, "T2");

                    dt.Rows.Clear();
                    dt.Rows.Add(1, "ONLINE DAC", 64, 0, 0, 1);
                    dt.Rows.Add(1, "ONLINE", 0, 0, 1, 1);
                    dt.Rows.Add(1, "ONLINE", 0, 0, 2, 2);
                    dt.Rows.Add(1, "OFFLINE", 0, 0, 4, 0);
                    dt.Rows.Add(1, "OFFLINE", 0, 0, 8, 0);
                    RunAffinityInfoTest(dt, "T3");

                    dt.Rows.Clear();
                    dt.Rows.Add(1, "ONLINE DAC", 64, 0, 0, 1);
                    dt.Rows.Add(1, "OFFLINE", 0, 0, 4, 0);
                    dt.Rows.Add(1, "ONLINE", 0, 0, 2, 2);
                    dt.Rows.Add(1, "OFFLINE", 0, 0, 8, 0);
                    dt.Rows.Add(1, "ONLINE", 0, 0, 1, 1);
                   
                    RunAffinityInfoTest(dt, "T4");
                });
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void AffinityInfo_SetCPUAndNumaValues_MultiGroup()
        {
            var dt = new DataTable();

            dt.Columns.Add("AffinityType", typeof(int));
            dt.Columns.Add("NodeStateDesc", typeof(string));
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("GroupID", typeof(int));
            dt.Columns.Add("CpuIds", typeof(long));
            dt.Columns.Add("CpuAffinityMask", typeof(long));

            Assert.Multiple(
                () =>
                {
                    dt.Rows.Clear();
                    for (int kg = 0; kg < 2; kg++)
                    {
                        // AffinityType	NodeStateDesc	ID	GroupID	CpuIds	CpuAffinityMask
                        dt.Rows.Add(1, "ONLINE", kg, kg, 0x3f << 0, 0x03);
                        dt.Rows.Add(1, "OFFLINE", kg, kg, (0x3f << 6) + (0x3f << 12), 0x00);
                    }
                    RunAffinityInfoTestMultGroup(dt, "MG1");
                });
        }

        private static void RunAffinityInfoTest(DataTable dt, string scenarioName)
        {
            
            System.Diagnostics.Trace.TraceInformation($"Scenario: {scenarioName}");
            AffinityInfo af = new AffinityInfo(dt);

            Assert.That(af.AffinityType, Is.EqualTo(AffinityType.Manual), "Unexpected AffinityType");

            var cpus = af.Cpus.Cast<Cpu>().ToArray();

            Assert.That(
                cpus.Select(cpu => (cpu.ID, cpu.AffinityMask, cpu.GroupID, cpu.NumaNodeID)),
                Is.EquivalentTo(
                    new[]
                    {
                        (0, true,  0, 0),
                        (1, true,  0, 0),
                        (2, false, 0, 0),
                        (3, false, 0, 0),
                    }), "Mismatching AffinityInfo (ID, AffinityMask, GroupID, NumaNodeID)");

            var numaNode = af.NumaNodes.Cast<NumaNode>().ToArray();
            Assert.That(numaNode, Has.Length.EqualTo(1), "There shoul ne only 1 NumaNode");

            Assert.That((numaNode[0].ID, numaNode[0].GroupID, numaNode[0].AffinityMask), Is.EqualTo((0, 0, NumaNodeAffinity.Partial)), "Mismatching NumaNode info (ID, GroupID, AffinityMask)");

            Assert.That(
                numaNode[0].Cpus.Cast<Cpu>().Select( (cpu, i) => (i, cpu.ID)),
                Is.EquivalentTo(
                    cpus.Select((cpu, i) => (i , cpu.ID))
                    ), "Mismatching CPU IDs on NumaNode (OrdinalIndex, ID)"
            );
        }

        private static void RunAffinityInfoTestMultGroup(DataTable dt, string scenarioName)
        {

            System.Diagnostics.Trace.TraceInformation($"Scenario: {scenarioName}");
            AffinityInfo af = new AffinityInfo(dt);

            Assert.That(af.AffinityType, Is.EqualTo(AffinityType.Manual), "Unexpected AffinityType");

            var cpus = af.Cpus.Cast<Cpu>().ToArray();
            Assert.That(cpus, Has.Length.EqualTo(36), "There should be 36 CPUs");
            for (int kg = 0; kg < 2; kg++)
            {
                for (int i = 0; i < 2; i++)
                {
                    var cpuordinal = i + kg * 18;
                    //af.Cpus.GetByID(cpuid).
                    Assert.That(cpus[cpuordinal].AffinityMask, Is.True, $"AffinityMask for CPU {cpuordinal}");
                    Assert.That(cpus[cpuordinal].ID, Is.EqualTo(64 * kg + i), $"AffinityMask for CPU {cpuordinal}");
                    Assert.That(cpus[cpuordinal].GroupID, Is.EqualTo(kg), $"GroupID for CPU {cpuordinal}");
                }
                for (int i = 2; i < 18; i++)
                {
                    var cpuordinal = i + kg * 18;
                    Assert.That(cpus[cpuordinal].AffinityMask, Is.False, $"AffinityMask for CPU {cpuordinal}");
                    Assert.That(cpus[cpuordinal].ID, Is.EqualTo(64 * kg + i), $"AffinityMask for CPU {cpuordinal}");
                    Assert.That(cpus[cpuordinal].GroupID, Is.EqualTo(kg), $"GroupID for CPU {cpuordinal}");
                    Assert.That(cpus[cpuordinal].NumaNodeID, Is.EqualTo(kg), $"NumaNodeID for CPU {cpuordinal}");
                }
            }

            var numaNode = af.NumaNodes.Cast<NumaNode>().ToArray();
            Assert.That(numaNode, Has.Length.EqualTo(2), "Unexpected number of (hardware) NUMA nodes");

            for (int kg = 0; kg < 2; kg++)
            {
                Assert.That(numaNode[kg].AffinityMask, Is.EqualTo(NumaNodeAffinity.Partial), $"AffinityMask for NUMA node {kg}");

                Assert.That(numaNode[kg].GroupID, Is.EqualTo(kg), $"NumaNode GroupID for NUMA node {kg}");
                Assert.That(numaNode[kg].ID, Is.EqualTo(kg), $"NumaNode ID for NUMA node {kg}");

                foreach (Cpu cpu in numaNode[kg].Cpus)
                {
                    var cpuViaGetByID = numaNode[kg].Cpus.GetByID(cpu.ID).ID;
                    Assert.That(cpu.ID, Is.EqualTo(cpuViaGetByID), $"CPU ID {cpu.ID}");
                }
            }
        }
    }
}
