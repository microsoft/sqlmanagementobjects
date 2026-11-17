// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class DatabaseAuditSpecification : AuditSpecification
    {
        internal DatabaseAuditSpecification(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state) { }

        public static string UrnSuffix
        {
            get
            {
                return "DatabaseAuditSpecification";
            }
        }

        internal static string ParentType
        {
            get
            {
                return "DATABASE";
            }
        }

        /// <summary>
        /// Name of Audit Specification
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }
        
    }
}
