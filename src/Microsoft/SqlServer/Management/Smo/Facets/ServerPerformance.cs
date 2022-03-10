// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Dmf = Microsoft.SqlServer.Management.Dmf;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{

    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [CLSCompliantAttribute(false)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.FacetSR")]
    [Sfc.DisplayNameKey("ServerPerformanceName")]
    [Sfc.DisplayDescriptionKey("ServerPerformanceDesc")]
    public interface IServerPerformanceFacet : Sfc.IDmfFacet
    {
#region Properties
        [Sfc.DisplayNameKey("AffinityMaskName")]
        [Sfc.DisplayDescriptionKey("AffinityMaskDesc")]
        int AffinityMask
        {
            get;
        }

        [Sfc.DisplayNameKey("Affinity64MaskName")]
        [Sfc.DisplayDescriptionKey("Affinity64MaskDesc")]
        int Affinity64Mask
        {
            get;
        }

        [Sfc.DisplayNameKey("AffinityMaskIOName")]
        [Sfc.DisplayDescriptionKey("AffinityMaskIODesc")]
        [Dmf.PostConfigurationAction(Dmf.PostConfigurationAction.RestartService)]
        int AffinityIOMask
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("Affinity64IOMaskName")]
        [Sfc.DisplayDescriptionKey("Affinity64IOMaskDesc")]
        int Affinity64IOMask
        {
            get;
        }

        [Sfc.DisplayNameKey("BlockedProcessThresholdName")]
        [Sfc.DisplayDescriptionKey("ServerPerformanceDesc")]
        int BlockedProcessThreshold
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("DynamicLocksName")]
        [Sfc.DisplayDescriptionKey("DynamicLocksDesc")]
        [Dmf.PostConfigurationAction(Dmf.PostConfigurationAction.RestartService)]
        int DynamicLocks
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("LightweightPoolingEnabledName")]
        [Sfc.DisplayDescriptionKey("LightweightPoolingEnabledDesc")]
        [Dmf.PostConfigurationAction(Dmf.PostConfigurationAction.RestartService)]
        bool LightweightPoolingEnabled
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("ServerPerformanceName")]
        [Sfc.DisplayDescriptionKey("ServerPerformanceDesc")]
        int MaxDegreeOfParallelism
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("CostThresholdforParallelismName")]
        [Sfc.DisplayDescriptionKey("CostThresholdforParallelismDesc")]
        int CostThresholdForParallelism
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("MaxWorkerThreadsName")]
        [Sfc.DisplayDescriptionKey("MaxWorkerThreadsDesc")]
        [Dmf.PostConfigurationAction(Dmf.PostConfigurationAction.RestartService)]
        int MaxWorkerThreads
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("NetworkPacketSizeName")]
        [Sfc.DisplayDescriptionKey("NetworkPacketSizeDesc")]
        int NetworkPacketSize
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("OpenObjectsName")]
        [Sfc.DisplayDescriptionKey("OpenObjectsDesc")]
        [Dmf.PostConfigurationAction(Dmf.PostConfigurationAction.RestartService)]
        int OpenObjects
        {
            get;
            set;
        }
#endregion
    }

    /// <summary>
    /// This facet is entirely a subset of configuration options, so it is just an interface on the ServerConfigurationAdapter
    /// </summary>
    public partial class ServerConfigurationAdapter : IServerPerformanceFacet
    {

    }
}
