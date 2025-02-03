// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    internal class Synonym : SchemaOwnedObject<Smo.Synonym>, ISynonym
    {
        private string baseObjectName;

        public Synonym(Smo.Synonym smoMetadataObject, Schema parent)
            : base(smoMetadataObject, parent)
        {
        }

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException("visitor");
            }

            return visitor.Visit(this);
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return false; }
        }

        #region ISynonym Members

        public string BaseObjectName
        {
            get 
            {
                if (this.baseObjectName == null)
                {
                    List<string> nameParts = new List<string>(4);

                    // Smo returns the baseObjectName as 4 separate (unescaped) identifiers:
                    // Server, Database, Schema, Object
                    // so we need to escape them and concatenate them together to create a dot separated qualified identifier.
                    // It is also valid for some identifiers to be not set, even if they are in the middle.
                    // Note: The target object is a schema owned object.

                    Debug.Assert(!string.IsNullOrEmpty(this.m_smoMetadataObject.BaseObject), "SmoMetadataProvider", "!string.IsNullOrEmpty(this.m_smoMetadataObject.BaseObject)");
                    string baseServer, baseDatabase, baseSchema, baseObject;

                    Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "BaseServer", out baseServer);
                    Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "BaseDatabase", out baseDatabase);
                    Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "BaseSchema", out baseSchema);
                    Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "BaseObject", out baseObject);

                    AddEscapedNamePart(nameParts, baseServer);
                    AddEscapedNamePart(nameParts, baseDatabase);
                    AddEscapedNamePart(nameParts, baseSchema);
                    AddEscapedNamePart(nameParts, baseObject);

                    this.baseObjectName = string.Join(".", nameParts.ToArray());
                }

                Debug.Assert(this.baseObjectName != null, "SmoMetadataProvider", "this.baseObjectName != null");
                return this.baseObjectName;
            }
        }

        public SynonymBaseType BaseType
        {
            get { return GetSynonymBaseType(this.m_smoMetadataObject.BaseType); }
        }

        #endregion

        private static SynonymBaseType GetSynonymBaseType(Smo.SynonymBaseType smoSynonymBaseType)
        {
            switch (smoSynonymBaseType)
            {
                case Smo.SynonymBaseType.None:
                    return SynonymBaseType.None;
                case Smo.SynonymBaseType.Table:
                    return SynonymBaseType.Table;
                case Smo.SynonymBaseType.View:
                    return SynonymBaseType.View;
                case Smo.SynonymBaseType.SqlStoredProcedure:
                    return SynonymBaseType.SqlStoredProcedure;
                case Smo.SynonymBaseType.SqlScalarFunction:
                    return SynonymBaseType.SqlScalarFunction;
                case Smo.SynonymBaseType.SqlTableValuedFunction:
                    return SynonymBaseType.SqlTableValuedFunction;
                case Smo.SynonymBaseType.SqlInlineTableValuedFunction:
                    return SynonymBaseType.SqlInlineTableValuedFunction;
                case Smo.SynonymBaseType.ExtendedStoredProcedure:
                    return SynonymBaseType.ExtendedStoredProcedure;
                case Smo.SynonymBaseType.ReplicationFilterProcedure:
                    return SynonymBaseType.ReplicationFilterProcedure;
                case Smo.SynonymBaseType.ClrStoredProcedure:
                    return SynonymBaseType.ClrStoredProcedure;
                case Smo.SynonymBaseType.ClrScalarFunction:
                    return SynonymBaseType.ClrScalarFunction;
                case Smo.SynonymBaseType.ClrTableValuedFunction:
                    return SynonymBaseType.ClrTableValuedFunction;
                case Smo.SynonymBaseType.ClrAggregateFunction:
                    return SynonymBaseType.ClrAggregateFunction;
                default:
                    Debug.Fail("SmoMetadataProvider", "Unexpected smo synonym base type: " + smoSynonymBaseType);
                    return SynonymBaseType.None;
            }
        }

        private static void AddEscapedNamePart(List<string> nameParts, string value)
        {
             Debug.Assert(nameParts != null, "SmoMetadataProvider", "nameParts!= null");

            if (!string.IsNullOrEmpty(value))
            {
                nameParts.Add(Utils.EscapeSqlIdentifier(value));
            }
            else if (nameParts.Count > 0)
            {
                // Empty/not set identifier which is in the middle needs to be added as empty string.
                // Trailing empty/not set identifiers can be skipped.
                nameParts.Add(string.Empty);
            }
        }
    }
}
