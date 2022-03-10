// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using System.ComponentModel;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;


namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [CLSCompliantAttribute(false)]

    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.FacetSR")]
    [Sfc.DisplayNameKey("ServerAuditName")]
    [Sfc.DisplayDescriptionKey("ServerAuditDesc")]
    public interface IServerAuditFacet : Sfc.IDmfFacet
    {
        #region Interface Properties
        [Sfc.DisplayNameKey("DefaultTraceEnabledName")]
        [Sfc.DisplayDescriptionKey("DefaultTraceEnabledDesc")]
        bool DefaultTraceEnabled
        {
            get;
            set;
        }


        [Sfc.DisplayNameKey("C2AuditTracingEnabledName")]
        [Sfc.DisplayDescriptionKey("C2AuditTracingEnabledDesc")]
        [Dmf.PostConfigurationAction(Dmf.PostConfigurationAction.RestartService)]
        bool C2AuditTracingEnabled
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("LoginAuditLevelName")]
        [Sfc.DisplayDescriptionKey("LoginAuditLevelDesc")]
        AuditLevel LoginAuditLevel
        {
            get;
            set;
        }
        #endregion
    }

    /// <summary>
    /// Relational Engine Security.  This facet mixes server settings and configuration, so it can be put on the ServerAdapter directly.
    /// </summary>
    public partial class ServerAdapter : IServerAuditFacet
    {

    }
}
