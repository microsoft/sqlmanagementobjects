// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class ServerAuditSpecification : AuditSpecification
    {
        internal ServerAuditSpecification(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state) { }

        public static string UrnSuffix
        {
            get
            {
                return "ServerAuditSpecification";
            }
        }

        internal static string ParentType
        {
            get
            {
                return "SERVER";
            }
        }
    }
}
