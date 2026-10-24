// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    static class Config
    {
        /// <summary>
        /// Defines initialization field list for different SMO object types. Each
        /// configuration includes an optimized and safe field list.
        /// Theoretically we could vary these fields per-engined type should the set of properties
        /// not exist everywhere, but that set of properties is currently empty.
        /// </summary>
        internal sealed class SmoInitFields
        {
            private static readonly Dictionary<Type, SmoInitFields> smoInitFields;
            private static readonly SmoInitFields database;

            public readonly Type Type;
            public readonly string[] Optimized;
            public readonly string[] Safe;

            private SmoInitFields(Type type, string[] optimized, string[] safe)
            {
                this.Type = type;
                this.Optimized = optimized;
                this.Safe = safe;
            }

            static SmoInitFields()
            {
                smoInitFields = new Dictionary<Type, SmoInitFields>();

                // SMO Database default init fields.
                database = new SmoInitFields(
                    typeof(Smo.Database),
                    new string[] { "Name", "ID", "IsSystemObject", "Collation", "IsAccessible" },
                    // We need IsAccessible early because it's used frequently 
                    new string[] { "Name", "ID", "IsAccessible" });
                AddSmoInitFields(database);

                // SMO AsymmetricKey default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.AsymmetricKey),
                    new string[] { "Name", "ID" },
                    new string[] { "Name", "ID" }));

                // SMO Column default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.Column),
                    new string[] { "Name", "ID", "Nullable", "InPrimaryKey", "Identity", "DataType", "Computed" },
                    new string[] { "Name", "ID", "Nullable", "InPrimaryKey", "Identity", "Computed" }));

                // SMO Certificate default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.Certificate),
                    new string[] { "Name", "ID" },
                    new string[] { "Name", "ID" }));

                // SMO Credential default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.Credential),
                    new string[] { "Name", "ID" },
                    new string[] { "Name", "ID" }));

                // SMO Check Constraint default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.Check),
                    new string[] { "Name" },
                    new string[] { "Name" }));

                // SMO DatabaseDdlTrigger default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.DatabaseDdlTrigger),
                    new string[] { "Name", "ID", "IsSystemObject", "ImplementationType", "IsEncrypted" },
                    new string[] { "Name", "ID" }));

                // SMO ExtendedStoredProcedure default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.ExtendedStoredProcedure),
                   new string[] { "Name", "ID", "Schema", "IsSystemObject" },
                   new string[] { "Name", "ID" }));

                // SMO ForeignKey default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.ForeignKey),
                    new string[] { "Name" },
                    new string[] { "Name" }));

                // SMO Index default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.Index),
                    new string[] { "Name", "IndexKeyType", "IsSpatialIndex", "IsXmlIndex" },
                    new string[] { "Name" }));

                // SMO Login default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.Login),
                    new string[] { "Name", "ID", "AsymmetricKey", "Certificate", "Credential", "LoginType" },
                    new string[] { "Name", "ID" }));

                // SMO ServerDdlTrigger default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.ServerDdlTrigger),
                    new string[] { "Name", "ID", "IsSystemObject", "ImplementationType", "IsEncrypted" },
                    new string[] { "Name", "ID" }));

                // SMO StoredProcedure default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.StoredProcedure),
                    new string[] { "Name", "ID", "Schema", "IsSystemObject", "ImplementationType", "IsEncrypted" },
                    new string[] { "Name", "ID", "Schema" }));

                // SMO Synonym default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.Synonym),
                    new string[] { "Name", "ID", "Schema", "BaseDatabase", "BaseObject", "BaseSchema", "BaseServer", "BaseType" },
                    new string[] { "Name", "ID", "Schema" }));

                // SMO StoredProcedureParameter default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.StoredProcedureParameter),
                    new string[] { "Name", "ID", "DefaultValue", "IsOutputParameter", "IsReadOnly", "DataType" },
                    new string[] { "Name", "ID" }));

                // SMO Trigger default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.Trigger),
                    new string[] { "Name", "ID", "ImplementationType", "IsSystemObject", "IsEncrypted", "Insert", "Delete", "Update" },
                    new string[] { "Name", "ID" }));

                // SMO User default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.User),
                    new string[] { "Name", "ID", "AsymmetricKey", "Certificate", "UserType" },
                    new string[] { "Name", "ID" }));

                // SMO UserDefinedAggregate default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.UserDefinedAggregate),
                    new string[] { "Name", "ID", "Schema", "DataType" },
                    new string[] { "Name", "ID", "Schema" }));

                // SMO UserDefinedFunction default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.UserDefinedFunction),
                    new string[] { "Name", "ID", "Schema", "FunctionType", "ImplementationType", "IsSystemObject", "IsEncrypted", "IsSchemaBound", "DataType" },
                    new string[] { "Name", "ID", "Schema" }));

                // SMO UserDefinedFunctionParameter default init fields.
                AddSmoInitFields(new SmoInitFields(
                    typeof(Smo.UserDefinedFunctionParameter),
                    new string[] { "Name", "ID", "DataType" },
                    new string[] { "Name", "ID" }));
            }

            public static SmoInitFields Database
            {
                get { return database; }
            }

            public static IEnumerable<SmoInitFields> GetAllInitFields()
            {
                return smoInitFields.Values;
            }

            public static SmoInitFields GetInitFields(Type type)
            {
                TraceHelper.TraceContext.Assert(type != null, "SmoMetadataProvider Assert", "type != null");

                SmoInitFields initFields;
                smoInitFields.TryGetValue(type, out initFields);

                return initFields;
            }

            private static void AddSmoInitFields(SmoInitFields initFields)
            {
                TraceHelper.TraceContext.Assert(initFields != null, "SmoMetadataProvider Assert", "initFields != null");
                TraceHelper.TraceContext.Assert(!smoInitFields.ContainsKey(initFields.Type), "SmoMetadataProvider Assert", "!smoInitFields.ContainsKey(initFields.Type)");

                smoInitFields.Add(initFields.Type, initFields);                
            }
        }
    }
}
