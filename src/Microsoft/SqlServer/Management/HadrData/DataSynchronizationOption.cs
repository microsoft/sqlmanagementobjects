// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// The Database SychronizationOption for an AG 
    /// </summary>
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.HadrData.Resource")]
    public enum DataSynchronizationOption
    {
        [DisplayNameKey("FullDataSync")]
        Full = 0,
        [DisplayNameKey("JoinOnlyDataSync")]
        JoinOnly,
        [DisplayNameKey("ManualDataSync")]
        Manual,
        [DisplayNameKey("AutomaticSeedingDataSync")]
        AutomaticSeeding,
    };
}
