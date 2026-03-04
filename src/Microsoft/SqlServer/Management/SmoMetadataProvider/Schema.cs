// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class Schema : DatabaseOwnedObject<Smo.Schema>, ISchema
    {
        private readonly ExtendedStoredProcedureCollectionHelper m_extendedStoredProcedures;
        private readonly ScalarValuedFunctionCollectionHelper m_scalarValuedFunctions;
        private readonly StoredProcedureCollectionHelper m_storedProcedures;
        private readonly SynonymCollectionHelper m_synonyms;
        private readonly TableCollectionHelper m_tables;
        private readonly TableValuedFunctionCollectionHelper m_tableValuedFunctions;
        private readonly UserDefinedAggregateCollectionHelper m_userDefinedAggregates;
        private readonly UserDefinedClrTypeCollectionHelper m_userDefinedClrTypes;
        private readonly UserDefinedDataTypeCollectionHelper m_userDefinedDataTypes;
        private readonly UserDefinedTableTypeCollectionHelper m_userDefinedTableTypes;
        private readonly ViewCollectionHelper m_views;

        public Schema(Smo.Schema smoMetadataObject, Database parent)
            : base(smoMetadataObject, parent)
        {
            this.m_extendedStoredProcedures = new ExtendedStoredProcedureCollectionHelper(this);
            this.m_scalarValuedFunctions = new ScalarValuedFunctionCollectionHelper(this);
            this.m_storedProcedures = new StoredProcedureCollectionHelper(this);
            this.m_synonyms = new SynonymCollectionHelper(this);
            this.m_tables = new TableCollectionHelper(this);
            this.m_tableValuedFunctions = new TableValuedFunctionCollectionHelper(this);
            this.m_userDefinedAggregates = new UserDefinedAggregateCollectionHelper(this);
            this.m_userDefinedClrTypes = new UserDefinedClrTypeCollectionHelper(this);
            this.m_userDefinedDataTypes = new UserDefinedDataTypeCollectionHelper(this);
            this.m_userDefinedTableTypes = new UserDefinedTableTypeCollectionHelper(this);
            this.m_views = new ViewCollectionHelper(this);
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get
            {
                bool? isSystemObject;
                Utils.TryGetPropertyValue(this.m_smoMetadataObject, "IsSystemObject", out isSystemObject);

                return isSystemObject.GetValueOrDefault();
            }
        }

        public bool IsSysSchema
        {
            get { return this.m_parent.SysSchema == this; }
        }

        public override T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region ISchema Members
        public IDatabasePrincipal Owner
        {
            get 
            { 
                // owner is not always set on SMO so we need make sure it is before trying to translate to a principle
                string ownerName;
                Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "Owner", out ownerName);
                return (ownerName != null) ? Utils.GetDatabasePrincipal(this.Parent, ownerName) : null;
            }
        }

        public IMetadataCollection<ITable> Tables
        {
            get { return this.m_tables.MetadataCollection; }
        }

        public IMetadataCollection<IView> Views
        {
            get { return this.m_views.MetadataCollection; }
        }

        public IMetadataCollection<IUserDefinedAggregate> UserDefinedAggregates
        {
            get { return this.m_userDefinedAggregates.MetadataCollection; }
        }

        public IMetadataCollection<ITableValuedFunction> TableValuedFunctions
        {
            get { return this.m_tableValuedFunctions.MetadataCollection; }
        }

        public IMetadataCollection<IScalarValuedFunction> ScalarValuedFunctions
        {
            get { return this.m_scalarValuedFunctions.MetadataCollection; }
        }

        public IMetadataCollection<IStoredProcedure> StoredProcedures
        {
            get { return this.m_storedProcedures.MetadataCollection; }
        }

        public IMetadataCollection<ISynonym> Synonyms
        {
            get { return this.m_synonyms.MetadataCollection; }
        }

        public IMetadataCollection<IExtendedStoredProcedure> ExtendedStoredProcedures
        {
            get { return this.m_extendedStoredProcedures.MetadataCollection; }
        }

        public IMetadataCollection<IUserDefinedDataType> UserDefinedDataTypes
        {
            get { return this.m_userDefinedDataTypes.MetadataCollection; }
        }

        public IMetadataCollection<IUserDefinedTableType> UserDefinedTableTypes
        {
            get { return this.m_userDefinedTableTypes.MetadataCollection; }
        }

        public IMetadataCollection<IUserDefinedClrType> UserDefinedClrTypes
        {
            get { return this.m_userDefinedClrTypes.MetadataCollection; }
        }
        #endregion

        #region CollectionHelper Class
        abstract private class CollectionHelper<T, S> : UnorderedCollectionHelperBase<T, S>
            where T : class, ISchemaOwnedObject
            where S : Smo.ScriptSchemaObjectBase
        {
            protected readonly Schema m_schema;

            public CollectionHelper(Schema schema)
            {
                Debug.Assert(schema != null, "SmoMetadataProvider Assert", "schema != null");

                this.m_schema = schema;
            }

            protected override Server Server
            {
                get { return this.m_schema.Database.Server; }
            }

            protected override CollationInfo GetCollationInfo()
            {
                return this.m_schema.Database.CollationInfo;
            }
        }

        /// <summary>
        /// ExtendedStoredProcedure
        /// </summary>
        private sealed class ExtendedStoredProcedureCollectionHelper : CollectionHelper<IExtendedStoredProcedure, Smo.ExtendedStoredProcedure>
        {
            public ExtendedStoredProcedureCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.ExtendedStoredProcedure> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataExtendedStoredProcedures[this.m_schema.Name];
            }

            protected override IMutableMetadataCollection<IExtendedStoredProcedure> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new ExtendedStoredProcedureCollection(initialCapacity, collationInfo);
            }

            protected override IExtendedStoredProcedure CreateMetadataObject(Smo.ExtendedStoredProcedure smoObject)
            {
                return new ExtendedStoredProcedure(smoObject, this.m_schema);
            }
        }

        /// <summary>
        /// ScalarValuedFunction
        /// </summary>
        private sealed class ScalarValuedFunctionCollectionHelper : CollectionHelper<IScalarValuedFunction, Smo.UserDefinedFunction>
        {
            public ScalarValuedFunctionCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.UserDefinedFunction> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataUdfs[this.m_schema.Name, IsScalarValuedFunction];
            }

            protected override IMutableMetadataCollection<IScalarValuedFunction> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new ScalarValuedFunctionCollection(initialCapacity, collationInfo);
            }

            protected override IScalarValuedFunction CreateMetadataObject(Smo.UserDefinedFunction smoObject)
            {
                return new ScalarValuedFunction(smoObject, this.m_schema);
            }

            private static bool IsScalarValuedFunction(Smo.UserDefinedFunction function)
            {
                Debug.Assert(function != null, "SmoMetadataProvider Assert", "function != null");

                return function.FunctionType == Smo.UserDefinedFunctionType.Scalar;
            }
        }

        /// <summary>
        /// StoredProcedure
        /// </summary>
        private sealed class StoredProcedureCollectionHelper : CollectionHelper<IStoredProcedure, Smo.StoredProcedure>
        {
            public StoredProcedureCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.StoredProcedure> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataStoredProcedures[this.m_schema.Name];
            }

            protected override IMutableMetadataCollection<IStoredProcedure> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new StoredProcedureCollection(initialCapacity, collationInfo);
            }

            protected override IStoredProcedure CreateMetadataObject(Smo.StoredProcedure smoObject)
            {
                return new StoredProcedure(smoObject, this.m_schema);
            }
        }

        /// <summary>
        /// Synonym
        /// </summary>
        private sealed class SynonymCollectionHelper : CollectionHelper<ISynonym, Smo.Synonym>
        {
            public SynonymCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.Synonym> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataSynonyms[this.m_schema.Name];
            }

            protected override IMutableMetadataCollection<ISynonym> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new SynonymCollection(initialCapacity, collationInfo);
            }

            protected override ISynonym CreateMetadataObject(Smo.Synonym smoObject)
            {
                return new Synonym(smoObject, this.m_schema);
            }
        }

        /// <summary>
        /// Table
        /// </summary>
        private sealed class TableCollectionHelper : CollectionHelper<ITable, Smo.Table>
        {
            public TableCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.Table> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataTables[this.m_schema.Name];
            }

            protected override IMutableMetadataCollection<ITable> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new TableCollection(initialCapacity, collationInfo);
            }

            protected override ITable CreateMetadataObject(Smo.Table smoObject)
            {
                return new Table(smoObject, this.m_schema);
            }
        }

        /// <summary>
        /// TableValuedFunction
        /// </summary>
        private sealed class TableValuedFunctionCollectionHelper : CollectionHelper<ITableValuedFunction, Smo.UserDefinedFunction>
        {
            public TableValuedFunctionCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.UserDefinedFunction> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataUdfs[this.m_schema.Name, IsTableValuedFunction];
            }

            protected override IMutableMetadataCollection<ITableValuedFunction> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new TableValuedFunctionCollection(initialCapacity, collationInfo);
            }

            protected override ITableValuedFunction CreateMetadataObject(Smo.UserDefinedFunction smoObject)
            {
                return new TableValuedFunction(smoObject, this.m_schema);
            }

            private static bool IsTableValuedFunction(Smo.UserDefinedFunction function)
            {
                Debug.Assert(function != null, "SmoMetadataProvider Assert", "function != null");

                return function.FunctionType == Smo.UserDefinedFunctionType.Inline ||
                    function.FunctionType == Smo.UserDefinedFunctionType.Table;
            }
        }

        /// <summary>
        /// UserDefinedAggregate
        /// </summary>
        private sealed class UserDefinedAggregateCollectionHelper : CollectionHelper<IUserDefinedAggregate, Smo.UserDefinedAggregate>
        {
            public UserDefinedAggregateCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.UserDefinedAggregate> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataUserDefinedAggregates[this.m_schema.Name];
            }

            protected override IMutableMetadataCollection<IUserDefinedAggregate> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new UserDefinedAggregateCollection(initialCapacity, collationInfo);
            }

            protected override IUserDefinedAggregate CreateMetadataObject(Smo.UserDefinedAggregate smoObject)
            {
                return new UserDefinedAggregate(smoObject, this.m_schema);
            }
        }

        /// <summary>
        /// UserDefinedClrType
        /// </summary>
        private sealed class UserDefinedClrTypeCollectionHelper : CollectionHelper<IUserDefinedClrType, Smo.UserDefinedType>
        {
            public UserDefinedClrTypeCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.UserDefinedType> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataUserDefinedClrTypes[this.m_schema.Name];
            }

            protected override IMutableMetadataCollection<IUserDefinedClrType> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new UserDefinedClrTypeCollection(initialCapacity, collationInfo);
            }

            protected override IUserDefinedClrType CreateMetadataObject(Smo.UserDefinedType smoObject)
            {
                return new UserDefinedClrType(smoObject, this.m_schema);
            }
        }

        /// <summary>
        /// UserDefinedDataType
        /// </summary>
        private sealed class UserDefinedDataTypeCollectionHelper : CollectionHelper<IUserDefinedDataType, Smo.UserDefinedDataType>
        {
            public UserDefinedDataTypeCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.UserDefinedDataType> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataUserDefinedDataTypes[this.m_schema.Name];
            }

            protected override IMutableMetadataCollection<IUserDefinedDataType> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new UserDefinedDataTypeCollection(initialCapacity, collationInfo);
            }

            protected override IUserDefinedDataType CreateMetadataObject(Smo.UserDefinedDataType smoObject)
            {
                return new UserDefinedDataType(smoObject, this.m_schema);
            }
        }

        /// <summary>
        /// UserDefinedTableType
        /// </summary>
        private sealed class UserDefinedTableTypeCollectionHelper : CollectionHelper<IUserDefinedTableType, Smo.UserDefinedTableType>
        {
            public UserDefinedTableTypeCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.UserDefinedTableType> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataUserDefinedTableTypes[this.m_schema.Name];
            }

            protected override IMutableMetadataCollection<IUserDefinedTableType> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new UserDefinedTableTypeCollection(initialCapacity, collationInfo);
            }

            protected override IUserDefinedTableType CreateMetadataObject(Smo.UserDefinedTableType smoObject)
            {
                return new UserDefinedTableType(smoObject, this.m_schema);
            }
        }

        /// <summary>
        /// View
        /// </summary>
        private sealed class ViewCollectionHelper : CollectionHelper<IView, Smo.View>
        {
            public ViewCollectionHelper(Schema schema)
                : base(schema)
            {
            }

            protected override IMetadataList<Smo.View> RetrieveSmoMetadataList()
            {
                return this.m_schema.m_parent.MetadataViews[this.m_schema.Name];
            }

            protected override IMutableMetadataCollection<IView> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new ViewCollection(initialCapacity, collationInfo);
            }

            protected override IView CreateMetadataObject(Smo.View smoObject)
            {
                return new View(smoObject, this.m_schema);
            }
        }
        #endregion
    }
}
