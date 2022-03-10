// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Represents a permission that is being granted/revoked/denied to a user/login/role
    /// </summary>
    internal sealed partial class UserPermission
    {
        internal override SqlPropertyMetadataProvider GetPropertyMetadataProvider()
        {
            return new PropertyMetadataProvider(this.ServerVersion, this.DatabaseEngineType, this.DatabaseEngineEdition);
        }

        private class PropertyMetadataProvider : SqlPropertyMetadataProvider
        {

            internal PropertyMetadataProvider(Common.ServerVersion version, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
                : base(version, databaseEngineType, databaseEngineEdition)
            {
            }
            public override int PropertyNameToIDLookup(string propertyName)
            {
                if (this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    //This handles the Datawarehouse as well since it has the same properties
                    switch (propertyName)
                    {
                        case "Code": return 0;
                        case "Grantee": return 1;
                        case "GranteeType": return 2;
                        case "Grantor": return 3;
                        case "GrantorType": return 4;
                        case "ObjectClass": return 5;
                        case "PermissionState": return 6;
                    }
                    return -1;
                }
                else
                {
                    switch (propertyName)
                    {
                        case "Code": return 0;
                        case "Grantee": return 1;
                        case "GranteeType": return 2;
                        case "Grantor": return 3;
                        case "GrantorType": return 4;
                        case "ObjectClass": return 5;
                        case "PermissionState": return 6;
                    }
                    return -1;
                }
            }

            // VBUMP
            /// <summary>
            /// This is the number of properties available for each version of the Standalone SQL engine
            /// </summary>
            static int[] versionCount = new int[] { 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7 }; //7.0, 8.0, 9.0, 10.0, 10.5, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0

            /// <summary>
            /// This is the number of properties available for each version of the Cloud SQL engine
            /// </summary>
            static int[] cloudVersionCount = new int[] { 7, 7, 7 }; //10.0, 11.0, 12.0

            public override int Count
            {
                get
                {
                    if (this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                    {
                        //This handles the Datawarehouse as well since it has the same properties
                        int index = (currentVersionIndex < cloudVersionCount.Length) ? currentVersionIndex : cloudVersionCount.Length - 1;
                        return cloudVersionCount[index];
                    }
                    else
                    {
                        int index = (currentVersionIndex < versionCount.Length) ? currentVersionIndex : versionCount.Length - 1;
                        return versionCount[index];
                    }
                }
            }


            public override StaticMetadata GetStaticMetadata(int id)
            {
                if (this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    return cloudStaticMetadata[id];
                }
                else
                {
                    return staticMetadata[id];
                }
            }

            new internal static int[] GetVersionArray(DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
            {
                if (databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    //This handles the Datawarehouse as well since it has the same properties
                    return cloudVersionCount;
                }
                else
                {
                    return versionCount;
                }
            }

            new internal static StaticMetadata[] GetStaticMetadataArray(DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
            {
                if (databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    //This handles the Datawarehouse as well since it has the same properties
                    return cloudStaticMetadata;
                }
                else
                {
                    return staticMetadata;
                }
            }
            protected override int[] VersionCount
            {
                get
                {
                    if (this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                    {
                        //This handles the Datawarehouse as well since it has the same properties
                        return cloudVersionCount;
                    }
                    else
                    {
                        return versionCount;
                    }
                }
            }

            internal static StaticMetadata[] cloudStaticMetadata =
            {
                    new StaticMetadata("Code", false, false,typeof(Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue)),
                    new StaticMetadata("Grantee", false, false,typeof(System.String)),
                    new StaticMetadata("GranteeType", false, false, typeof(Microsoft.SqlServer.Management.Smo.PrincipalType)),
                    new StaticMetadata("Grantor", false, false, typeof(System.String)),
                    new StaticMetadata("GrantorType", false, false, typeof(Microsoft.SqlServer.Management.Smo.PrincipalType)),
                    new StaticMetadata("ObjectClass", false, false, typeof(Microsoft.SqlServer.Management.Smo.ObjectClass)),
                    new StaticMetadata("PermissionState", false, false, typeof(Microsoft.SqlServer.Management.Smo.PermissionState)),
            };

            internal static StaticMetadata[] staticMetadata =
            {
                    new StaticMetadata("Code", false, false,typeof(Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue)),
                    new StaticMetadata("Grantee", false, false,typeof(System.String)),
                    new StaticMetadata("GranteeType", false, false, typeof(Microsoft.SqlServer.Management.Smo.PrincipalType)),
                    new StaticMetadata("Grantor", false, false, typeof(System.String)),
                    new StaticMetadata("GrantorType", false, false, typeof(Microsoft.SqlServer.Management.Smo.PrincipalType)),
                    new StaticMetadata("ObjectClass", false, false, typeof(Microsoft.SqlServer.Management.Smo.ObjectClass)),
                    new StaticMetadata("PermissionState", false, false, typeof(Microsoft.SqlServer.Management.Smo.PermissionState)),
            };
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue Code
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue)this.Properties.GetValueWithNullReplacement("Code");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("Code", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public System.String Grantee
        {
            get
            {
                return (System.String)this.Properties.GetValueWithNullReplacement("Grantee");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("Grantee", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Microsoft.SqlServer.Management.Smo.PrincipalType GranteeType
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.PrincipalType)this.Properties.GetValueWithNullReplacement("GranteeType");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("GranteeType", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public System.String Grantor
        {
            get
            {
                return (System.String)this.Properties.GetValueWithNullReplacement("Grantor");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("Grantor", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Microsoft.SqlServer.Management.Smo.PrincipalType GrantorType
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.PrincipalType)this.Properties.GetValueWithNullReplacement("GrantorType");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("GrantorType", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public System.Int32 IntCode
        {
            get
            {
                return (System.Int32)this.Properties.GetValueWithNullReplacement("IntCode");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("IntCode", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Microsoft.SqlServer.Management.Smo.ObjectClass ObjectClass
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.ObjectClass)this.Properties.GetValueWithNullReplacement("ObjectClass");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("ObjectClass", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Microsoft.SqlServer.Management.Smo.PermissionState PermissionState
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.PermissionState)this.Properties.GetValueWithNullReplacement("PermissionState");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("PermissionState", value);
            }
        }
    }
}
