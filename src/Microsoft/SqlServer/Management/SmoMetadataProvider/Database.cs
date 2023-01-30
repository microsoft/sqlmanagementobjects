// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class Database : ServerOwnedObject<Smo.Database>, IDatabase
    {
        private const string SysSchemaName = "sys";

        private readonly ApplicationRoleCollectionHelper m_applicationRoles;
        private readonly AsymmetricKeyCollectionHelper m_asymmetricKeys;
        private readonly CertificateCollectionHelper m_certificates;
        private readonly DatabaseRoleCollectionHelper m_databaseRoles;
        private readonly SchemaCollectionHelper m_schemaBasedSchemas;
        private readonly RoleBasedSchemaCollectionHelper m_roleBasedSchemas;
        private readonly UserBasedSchemaCollectionHelper m_userBasedSchemas;
        private readonly DatabaseDdlTriggerCollectionHelper m_triggers;
        private readonly UserCollectionHelper m_users;

        private SchemaCollection m_schemaCollection;
        private CollationInfo m_collationInfo;
        private string m_defaultSchemaName;
        private bool m_defaultSchemaNameRetrieved;
        private ISchema m_sysSchema;
        private bool m_sysSchemaRetrieved;
        private DatabaseCompatibilityLevel? m_compatibilityLevel;

        public Database(Smo.Database smoMetadataObject, Server parent)
            : base(smoMetadataObject, parent)
        {
            // create and set collection helpers
            this.m_applicationRoles = new ApplicationRoleCollectionHelper(this);
            this.m_asymmetricKeys = new AsymmetricKeyCollectionHelper(this);
            this.m_certificates = new CertificateCollectionHelper(this);
            this.m_databaseRoles = new DatabaseRoleCollectionHelper(this);
            this.m_schemaBasedSchemas = new SchemaCollectionHelper(this);
            this.m_roleBasedSchemas = new RoleBasedSchemaCollectionHelper(this);
            this.m_userBasedSchemas = new UserBasedSchemaCollectionHelper(this);
            this.m_triggers = new DatabaseDdlTriggerCollectionHelper(this);
            this.m_users = new UserCollectionHelper(this);   
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return this.m_smoMetadataObject.IsSystemObject; }
        }

        public override T Accept<T>(IServerOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        public ISchema SysSchema
        {
            get 
            {
                if (!m_sysSchemaRetrieved)
                {
                    // cache the sys schema object
                    this.m_sysSchema = this.Schemas[SysSchemaName];
                    this.m_sysSchemaRetrieved = true;
                }

                return this.m_sysSchema; 
            }
        }

        #region IDatabase Members
        public IMetadataCollection<IApplicationRole> ApplicationRoles
        {
            get { return this.m_applicationRoles.MetadataCollection; }
        }

        public IMetadataCollection<IAsymmetricKey> AsymmetricKeys
        {
            get { return this.m_asymmetricKeys.MetadataCollection; }
        }

        public IMetadataCollection<ICertificate> Certificates
        {
            get { return this.m_certificates.MetadataCollection; }
        }

        public CollationInfo CollationInfo
        {
            get 
            {
                if (this.m_collationInfo == null)
                {
                    string databaseCollation;
                    Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "Collation", out databaseCollation);

                    // retrieve database collation and create collation info object
                    this.m_collationInfo = databaseCollation != null ? Utils.GetCollationInfo(databaseCollation) : CollationInfo.Default;
                }

                Debug.Assert(this.m_collationInfo != null, "SmoMetadataProvider Assert", "this.m_collationInfo != null");
                return this.m_collationInfo; 
            }
        }

        public DatabaseCompatibilityLevel CompatibilityLevel
        {
            get 
            {
                if (!this.m_compatibilityLevel.HasValue)
                {
                    Smo.CompatibilityLevel? compatibilityLevel;
                    if (Utils.TryGetPropertyValue<Smo.CompatibilityLevel>(this.m_smoMetadataObject, "CompatibilityLevel", out compatibilityLevel) && compatibilityLevel.HasValue)
                    {
                        switch (compatibilityLevel.Value)
                        {
                            case Smo.CompatibilityLevel.Version80:
                                this.m_compatibilityLevel = DatabaseCompatibilityLevel.Version80;
                                break;
                            case Smo.CompatibilityLevel.Version90:
                                this.m_compatibilityLevel = DatabaseCompatibilityLevel.Version90;
                                break;
                            case Smo.CompatibilityLevel.Version100:
                                this.m_compatibilityLevel = DatabaseCompatibilityLevel.Version100;
                                break;
                            case Smo.CompatibilityLevel.Version110:
                                this.m_compatibilityLevel = DatabaseCompatibilityLevel.Version110;
                                break;
                            default:
                                // IMPORTANT : This is specific to DAC.
                                //             We do not support compatability level lower than 80.
                                // TODO : We might want to throw an exception (and let DAC catch it)
                                //        instead of silently ignoring lower versions.
                                this.m_compatibilityLevel = DatabaseCompatibilityLevel.Current;
                                break;
                        }
                    }
                    else
                    {
                        this.m_compatibilityLevel = DatabaseCompatibilityLevel.Current;
                    }
                }

                Debug.Assert(this.m_compatibilityLevel.HasValue, "SmoMetadataProvider Assert", "this.m_compatibilityLevel.HasValue");
                return this.m_compatibilityLevel.Value; 
            }
        }

        public string DefaultSchemaName
        {
            get 
            {
                if (!this.m_defaultSchemaNameRetrieved)
                {
                    // VSTS:106375 - Windows Authentication with Domain group and binding
                    // User might not have a default schema. In such a case SMO will throw
                    // a 'PropertyCannotBeRetrievedException' exception when we attempt to
                    // retrieve default schema name. We need to handle the exception and set
                    // the default schema to the empty one.

                    try
                    {
                        this.m_defaultSchemaName = this.m_smoMetadataObject.DefaultSchema;
                    }
                    catch (ConnectionException)
                    {
                        this.m_defaultSchemaName = null;
                    }
                    catch (Smo.SmoException)
                    {
                        this.m_defaultSchemaName = null;
                    }

                    this.m_defaultSchemaNameRetrieved = true;
                }

                Debug.Assert(this.m_defaultSchemaNameRetrieved == true, "SmoMetadataProvider Assert", "this.m_defaultSchemaNameRetrieved == true");
                return this.m_defaultSchemaName; 
            }
        }

        public IUser Owner
        {
            get
            {
                string userName;
                Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "UserName", out userName);
                return String.IsNullOrEmpty(userName) ? null : this.Users[userName];
            }
        }

        public IMetadataCollection<IDatabaseRole> Roles
        {
            get { return this.m_databaseRoles.MetadataCollection; }
        }

        public IMetadataCollection<ISchema> Schemas
        {
            get
            {
                if (this.m_schemaCollection == null)
                {
                    this.m_schemaCollection = new SchemaCollection(this.CollationInfo);
                    if (Utils.IsShilohDatabase(this.m_smoMetadataObject))
                    {
                        this.m_schemaCollection.AddRange(
                            this.m_userBasedSchemas.MetadataCollection.Union(
                                this.m_roleBasedSchemas.MetadataCollection));
                    }
                    else
                    {
                        this.m_schemaCollection.AddRange(
                            this.m_schemaBasedSchemas.MetadataCollection);
                    }
                }

                return this.m_schemaCollection;
            }
        }

        public IMetadataCollection<IDatabaseDdlTrigger> Triggers
        {
            get { return this.m_triggers.MetadataCollection; }
        }

        public IMetadataCollection<IUser> Users
        {
            get { return this.m_users.MetadataCollection; }
        }

        #endregion


        #region CollectionHelper Class
        abstract private class CollectionHelper<T, S> : UnorderedCollectionHelperBase<T, S>
            where T : class, IDatabaseOwnedObject
            where S : Smo.NamedSmoObject
        {
            protected readonly Database m_database;

            public CollectionHelper(Database database)
            {
                Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");

                this.m_database = database;
            }

            protected override Server Server
            {
                get { return this.m_database.Server; }
            }

            protected override CollationInfo GetCollationInfo()
            {
                return this.m_database.CollationInfo;
            }
        }

        /// <summary>
        /// ApplicationRoles
        /// </summary>
        private class ApplicationRoleCollectionHelper : CollectionHelper<IApplicationRole, Smo.ApplicationRole>
        {
            public ApplicationRoleCollectionHelper(Database database)
                : base(database)
            {
            }

            protected override IMetadataList<Smo.ApplicationRole> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.ApplicationRole>(
                    this.m_database.Server,
                    this.m_database.m_smoMetadataObject.ApplicationRoles);
            }

            protected override IMutableMetadataCollection<IApplicationRole> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new ApplicationRoleCollection(initialCapacity, collationInfo);
            }

            protected override IApplicationRole CreateMetadataObject(Smo.ApplicationRole smoObject)
            {
                return new ApplicationRole(smoObject, this.m_database);
            }
        }

        /// <summary>
        /// AsymmetricKeys
        /// </summary>
        private class AsymmetricKeyCollectionHelper : CollectionHelper<IAsymmetricKey, Smo.AsymmetricKey>
        {
            public AsymmetricKeyCollectionHelper(Database database)
                : base(database)
            {
            }

            protected override IMetadataList<Smo.AsymmetricKey> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.AsymmetricKey>(
                    this.m_database.Server,
                    this.m_database.m_smoMetadataObject.AsymmetricKeys);
            }

            protected override IMutableMetadataCollection<IAsymmetricKey> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new AsymmetricKeyCollection(initialCapacity, collationInfo);
            }

            protected override IAsymmetricKey CreateMetadataObject(Smo.AsymmetricKey smoObject)
            {
                return new AsymmetricKey(smoObject, this.m_database);
            }
        }

        /// <summary>
        /// Certificates
        /// </summary>
        private class CertificateCollectionHelper : CollectionHelper<ICertificate, Smo.Certificate>
        {
            public CertificateCollectionHelper(Database database)
                : base(database)
            {
            }

            protected override IMetadataList<Smo.Certificate> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.Certificate>(
                    this.m_database.Server,
                    this.m_database.m_smoMetadataObject.Certificates);
            }

            protected override IMutableMetadataCollection<ICertificate> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new CertificateCollection(initialCapacity, collationInfo);
            }

            protected override ICertificate CreateMetadataObject(Smo.Certificate smoObject)
            {
                return new Certificate(smoObject, this.m_database);
            }
        }

        /// <summary>
        /// DatabaseRoles
        /// </summary>
        private class DatabaseRoleCollectionHelper : CollectionHelper<IDatabaseRole, Smo.DatabaseRole>
        {
            public DatabaseRoleCollectionHelper(Database database)
                : base(database)
            {
            }

            protected override IMetadataList<Smo.DatabaseRole> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.DatabaseRole>(
                    this.m_database.Server,
                    this.m_database.m_smoMetadataObject.Roles);
            }

            protected override IMutableMetadataCollection<IDatabaseRole> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new DatabaseRoleCollection(initialCapacity, collationInfo);
            }

            protected override IDatabaseRole CreateMetadataObject(Smo.DatabaseRole smoObject)
            {
                return new DatabaseRole(smoObject, this.m_database);
            }
        }

        /// <summary>
        /// Schemas
        /// </summary>
        private class SchemaCollectionHelper : CollectionHelper<ISchema, Smo.Schema>
        {
            public SchemaCollectionHelper(Database database)
                : base(database)
            {
            }

            protected override IMetadataList<Smo.Schema> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.Schema>(
                    this.m_database.Server,
                    this.m_database.m_smoMetadataObject.Schemas);
            }

            protected override IMutableMetadataCollection<ISchema> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new SchemaCollection(initialCapacity, collationInfo);
            }

            protected override ISchema CreateMetadataObject(Smo.Schema smoObject)
            {
                return new Schema(smoObject, this.m_database);
            }
        }

        /// <summary>
        /// Schemas for Shiloh, based on Users
        /// </summary>
        private class UserBasedSchemaCollectionHelper : CollectionHelper<ISchema, Smo.User>
        {
            public UserBasedSchemaCollectionHelper(Database database)
                : base(database)
            {
            }

            protected override IMetadataList<Smo.User> RetrieveSmoMetadataList()
            {
                IMetadataList<Smo.User> users = new SmoCollectionMetadataList<Smo.User>(
                    this.m_database.Server,
                    this.m_database.m_smoMetadataObject.Users);

                return new EnumerableMetadataList<Smo.User>(
                    users.Where(Utils.IsUserConvertableToSchema));
            }

            protected override IMutableMetadataCollection<ISchema> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new SchemaCollection(initialCapacity, collationInfo);
            }

            protected override ISchema CreateMetadataObject(Smo.User smoObject)
            {
                return new Schema(
                    new Smo.Schema(smoObject.Parent, smoObject.Name), 
                    this.m_database);
            }
        }

        /// <summary>
        /// Schemas for Shiloh, based on Users
        /// </summary>
        private class RoleBasedSchemaCollectionHelper : CollectionHelper<ISchema, Smo.DatabaseRole>
        {
            public RoleBasedSchemaCollectionHelper(Database database)
                : base(database)
            {
            }

            protected override IMetadataList<Smo.DatabaseRole> RetrieveSmoMetadataList()
            {
                IMetadataList<Smo.DatabaseRole> roles = new SmoCollectionMetadataList<Smo.DatabaseRole>(
                    this.m_database.Server,
                    this.m_database.m_smoMetadataObject.Roles);

                return new EnumerableMetadataList<Smo.DatabaseRole>(
                    roles.Where(Utils.IsRoleConvertableToSchema));
            }

            protected override IMutableMetadataCollection<ISchema> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new SchemaCollection(initialCapacity, collationInfo);
            }

            protected override ISchema CreateMetadataObject(Smo.DatabaseRole smoObject)
            {
                return new Schema(
                    new Smo.Schema(smoObject.Parent, smoObject.Name),
                    this.m_database);
            }
        }

        /// <summary>
        /// Database DDL Triggers
        /// </summary>
        private class DatabaseDdlTriggerCollectionHelper : CollectionHelper<IDatabaseDdlTrigger, Smo.DatabaseDdlTrigger>
        {
            public DatabaseDdlTriggerCollectionHelper(Database database)
                : base(database)
            {
            }

            protected override IMetadataList<Smo.DatabaseDdlTrigger> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.DatabaseDdlTrigger>(
                    this.m_database.Server,
                    this.m_database.m_smoMetadataObject.Triggers);
            }

            protected override IMutableMetadataCollection<IDatabaseDdlTrigger> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new DatabaseDdlTriggerCollection(initialCapacity, collationInfo);
            }

            protected override IDatabaseDdlTrigger CreateMetadataObject(Smo.DatabaseDdlTrigger smoObject)
            {
                return new DatabaseDdlTrigger(smoObject, this.m_database);
            }
        }

        /// <summary>
        /// Users
        /// </summary>
        private class UserCollectionHelper : CollectionHelper<IUser, Smo.User>
        {
            public UserCollectionHelper(Database database)
                : base(database)
            {
            }

            protected override IMetadataList<Smo.User> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.User>(
                    this.m_database.Server,
                    this.m_database.m_smoMetadataObject.Users);
            }

            protected override IMutableMetadataCollection<IUser> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new UserCollection(initialCapacity, collationInfo);
            }

            protected override IUser CreateMetadataObject(Smo.User smoObject)
            {
                return User.CreateUser(smoObject, this.m_database);
            }
        }
        #endregion


        #region Schema Helper Methods
        // -----------------------------------------
        // Metadata Tables
        // -----------------------------------------
        private TableMetadataList m_metadataTables;
        internal TableMetadataList MetadataTables
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataTables == null)
                {
                    this.m_metadataTables = new TableMetadataList(
                        this.m_smoMetadataObject.Tables, this.Schemas.Count, this);
                }
                return this.m_metadataTables;
            }
        }

        // -----------------------------------------
        // Metadata Views
        // -----------------------------------------
        private ViewMetadataList m_metadataViews;
        internal ViewMetadataList MetadataViews
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataViews == null)
                {
                    this.m_metadataViews = new ViewMetadataList(
                        this.m_smoMetadataObject.Views, this.Schemas.Count, this);
                }
                return this.m_metadataViews;
            }
        }

        // -----------------------------------------
        // Metadata User-defined Functions
        // -----------------------------------------
        private UserDefinedFunctionMetadataList m_metadataUdfs;
        internal UserDefinedFunctionMetadataList MetadataUdfs
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataUdfs == null)
                {
                    this.m_metadataUdfs = new UserDefinedFunctionMetadataList(
                        this.m_smoMetadataObject.UserDefinedFunctions, this.Schemas.Count, this);
                }
                return this.m_metadataUdfs;
            }
        }

        // -----------------------------------------
        // Metadata User-defined Aggregates
        // -----------------------------------------
        private UserDefinedAggregateMetadataList m_metadataUserDefinedAggregates;
        internal UserDefinedAggregateMetadataList MetadataUserDefinedAggregates
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataUserDefinedAggregates == null)
                {
                    this.m_metadataUserDefinedAggregates = new UserDefinedAggregateMetadataList(
                        this.m_smoMetadataObject.UserDefinedAggregates, this.Schemas.Count, this);
                }
                return this.m_metadataUserDefinedAggregates;
            }
        }

        // -----------------------------------------
        // Metadata Stored Procedures
        // -----------------------------------------
        private StoredProcedureMetadataList m_metadataStoredProcedures;
        internal StoredProcedureMetadataList MetadataStoredProcedures
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataStoredProcedures == null)
                {
                    this.m_metadataStoredProcedures = new StoredProcedureMetadataList(
                        this.m_smoMetadataObject.StoredProcedures, this.Schemas.Count, this);
                }
                return this.m_metadataStoredProcedures;
            }
        }

        // -----------------------------------------
        // Metadata Synonyms
        // -----------------------------------------
        private SynonymMetadataList m_metadataSynonyms;
        internal SynonymMetadataList MetadataSynonyms
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataSynonyms == null)
                {
                    this.m_metadataSynonyms = new SynonymMetadataList(
                        this.m_smoMetadataObject.Synonyms, this.Schemas.Count, this);
                }
                return this.m_metadataSynonyms;
            }
        }

        // -----------------------------------------
        // Metadata Extended Stored Procedures
        // -----------------------------------------
        private ExtendedStoredProcedureMetadataList m_metadataExtendedStoredProcedures;
        internal ExtendedStoredProcedureMetadataList MetadataExtendedStoredProcedures
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataExtendedStoredProcedures == null)
                {
                    this.m_metadataExtendedStoredProcedures = new ExtendedStoredProcedureMetadataList(
                        this.m_smoMetadataObject.ExtendedStoredProcedures, this.Schemas.Count, this);
                }
                return this.m_metadataExtendedStoredProcedures;
            }
        }

        // -----------------------------------------
        // Metadata User-defined Data Types
        // -----------------------------------------
        private UserDefinedDataTypeMetadataList m_metadataUserDefinedDataTypes;
        internal UserDefinedDataTypeMetadataList MetadataUserDefinedDataTypes
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataUserDefinedDataTypes == null)
                {
                    this.m_metadataUserDefinedDataTypes = new UserDefinedDataTypeMetadataList(
                        this.m_smoMetadataObject.UserDefinedDataTypes, this.Schemas.Count, this);
                }
                return this.m_metadataUserDefinedDataTypes;
            }
        }

        // -----------------------------------------
        // Metadata User-defined Table Types
        // -----------------------------------------
        private UserDefinedTableTypeMetadataList m_metadataUserDefinedTableTypes;
        internal UserDefinedTableTypeMetadataList MetadataUserDefinedTableTypes
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataUserDefinedTableTypes == null)
                {
                    this.m_metadataUserDefinedTableTypes = new UserDefinedTableTypeMetadataList(
                        this.m_smoMetadataObject.UserDefinedTableTypes, this.Schemas.Count, this);
                }
                return this.m_metadataUserDefinedTableTypes;
            }
        }

        // -----------------------------------------
        // Metadata User-defined (CLR) Types
        // -----------------------------------------
        private UserDefinedTypeMetadataList m_metadataUserDefinedClrTypes;
        internal UserDefinedTypeMetadataList MetadataUserDefinedClrTypes
        {
            get
            {
                // if we haven't created the list yet then we do so
                if (this.m_metadataUserDefinedClrTypes == null)
                {
                    this.m_metadataUserDefinedClrTypes = new UserDefinedTypeMetadataList(
                        this.m_smoMetadataObject.UserDefinedTypes, this.Schemas.Count, this);
                }
                return this.m_metadataUserDefinedClrTypes;
            }
        }

        /// <summary>
        /// This class represents a range of elements in an array.
        /// </summary>
        /// <typeparam name="T">Type of elements in the array.</typeparam>
        private class ArrayRange<T> : IMetadataList<T>
            where T : Smo.ScriptSchemaObjectBase
        {
            private readonly T[] m_data;
            private readonly int m_startIndex;
            private readonly int m_count;

            public ArrayRange(T[] data, int startIndex, int count)
            {
                Debug.Assert(data != null);
                Debug.Assert(startIndex >= 0);
                Debug.Assert((count >= 0) && (count <= data.Length));

                this.m_data = data;
                this.m_startIndex = startIndex;
                this.m_count = count;
            }

            public int Count
            {
                get { return this.m_count; }
            }

            public T this[int index]
            {
                get
                {
                    Debug.Assert((index >= 0) && (index < this.m_count), "SmoMetadataProvider Assert",
                        "index is out of range!");

                    return this.m_data[this.m_startIndex + index];
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < this.m_count; i++)
                    yield return this.m_data[this.m_startIndex + i];
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// This class represents an empty range of elements.
        /// </summary>
        /// <typeparam name="T">Type of elements in the array.</typeparam>
        private class EmptyRange<T> : IMetadataList<T>
            where T : Smo.ScriptSchemaObjectBase
        {
            public int Count
            {
                get { return 0; }
            }

            public T this[int index]
            {
                get
                {
                    Debug.Fail("SmoMetadataProvider", "index is out of range!");
                    return null;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                yield break;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                yield break;
            }
        }

        /// <summary>
        /// This helper class retrieves a collection of SMO objects from the
        /// database SMO object and provides retrieval of those object for
        /// each schema in the database.
        /// </summary>
        /// <remarks>
        /// This class relies on the assumption that objects in the collection
        /// are order by schema name. If this is not true the per-schema grouping
        /// will be incorrect.
        /// This class copies all objects in the collection of objects specified to
        /// a single-dimension array. It uses an internal lookup table that associate
        /// a range of elements in the array to a schema name.
        /// </remarks>
        /// <typeparam name="T">Type of the SMO object to be retrieved.</typeparam>
        /// <typeparam name="U">Type of the SMO collection that contains the objects.</typeparam>
        abstract internal class MetadataList<T, U>
            where T : Smo.ScriptSchemaObjectBase
            where U : Smo.SmoCollectionBase
        {
            private static readonly IMetadataList<T> EmptyRange = new EmptyRange<T>();

            private readonly T[] m_data;
            private readonly SortedList<string, ArrayRange<T>> m_rangeLookup;

            /// <summary>
            /// Constructs a new MetadataList object for the given collection
            /// of SMO objects.
            /// </summary>
            /// <param name="smoCollection">Collection of SMO objects to be grouped by
            /// their schema names.</param>
            /// <param name="schemaCount">Number of schemas in the database.</param>
            /// <param name="database">Database object the metadata belongs to.</param>
            protected MetadataList(U smoCollection, int schemaCount, Database database)
            {
                Debug.Assert(smoCollection != null, "MetadataProvider Assert", "smoCollection != null");
                Debug.Assert(schemaCount > 0, "MetadataProvider Assert", "schemaCount > 0");
                Debug.Assert(database != null, "MetadataProvider Assert", "database != null");

                IMetadataList<T> metadataList = new SmoCollectionMetadataList<T>(database.Server, smoCollection);

                int itemCount = metadataList.Count;
                this.m_data = new T[itemCount];
                this.m_rangeLookup = new SortedList<string, ArrayRange<T>>(schemaCount,
                    database.CollationInfo.Comparer);

                if (itemCount != 0)
                {
                    int index = 0;

                    foreach (T item in metadataList)
                    {
                        Debug.Assert(item != null, "SmoMetadataProvider", "item != null");

                        this.m_data[index] = item;
                        index++;
                    }
                }

                Comparison<T> comparison = new Comparison<T>((x,y) => database.CollationInfo.Comparer.Compare(x.Schema, y.Schema));
                Array.Sort(this.m_data, comparison);

                for (int startIndex = 0; startIndex < itemCount; )
                {
                    string currentSchema = this.m_data[startIndex].Schema;
                    Debug.Assert(currentSchema != null, "SmoMetadataProvider", "currentSchema != null");

                    // Find the range for items with the same schema.
                    int endIndex = startIndex + 1;

                    // NOTE: We do a simple string comparison here rather than using the
                    // identifier comparer becuase schema names should not change for all
                    // objects that belong to this schema.
                    while (endIndex < itemCount && this.m_data[endIndex].Schema == currentSchema)
                    {
                        endIndex++;
                    }

                    this.AddSchemaRange(currentSchema, startIndex, endIndex - startIndex);
                    startIndex = endIndex;
                }
            }

            private void AddSchemaRange(string schemaName, int startIndex, int count)
            {
                Debug.Assert(!string.IsNullOrEmpty(schemaName));
                Debug.Assert(count >= 0);

                // IMPORTANT-DO NOT REMOVE: This assert guards the assumption 
                // that objects are sorted by schema name.
                Debug.Assert(!this.m_rangeLookup.ContainsKey(schemaName));

                // add new array range to lookup table
                this.m_rangeLookup.Add(schemaName,
                    new ArrayRange<T>(this.m_data, startIndex, count));
            }

            /// <summary>
            /// Returns an iterator for SMO objects that belong to the specified
            /// schema.
            /// </summary>
            /// <param name="schemaName">Name of the schema to get items for.</param>
            /// <returns>An iterator of all objects the belong to the specified schema.</returns>
            public IMetadataList<T> this[string schemaName]
            {
                get
                {
                    Debug.Assert(!string.IsNullOrEmpty(schemaName));

                    ArrayRange<T> arrayRange;
                    if (this.m_rangeLookup.TryGetValue(schemaName, out arrayRange))
                        return arrayRange;
                    else
                        return EmptyRange;
                }
            }

            public IMetadataList<T> this[string schemaName, Predicate<T> filter]
            {
                get
                {
                    Debug.Assert(filter != null, "SmoMetadataProvider != null", "filter != null");

                    IMetadataList<T> fullList = this[schemaName];
                    
                    T[] tmpList = new T[fullList.Count];

                    int count = 0;
                    foreach (T item in fullList)
                    {
                        if (filter(item)) tmpList[count++] = item;
                    }

                    return count == 0 ? EmptyRange : new ArrayRange<T>(tmpList, 0, count);
                }
            }
        }

        /// <summary>
        /// This class represents a list of SMO Table objects.
        /// </summary>
        internal class TableMetadataList : MetadataList<Smo.Table, Smo.TableCollection>
        {
            public TableMetadataList(Smo.TableCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }

        /// <summary>
        /// This class represents a list of SMO View objects.
        /// </summary>
        internal class ViewMetadataList : MetadataList<Smo.View, Smo.ViewCollection>
        {
            public ViewMetadataList(Smo.ViewCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }

        /// <summary>
        /// This class represents a list of SMO user-defined function objects.
        /// </summary>
        /// <remarks>
        /// This list includes both scalar-valued and table-valued functions.
        /// </remarks>
        internal class UserDefinedFunctionMetadataList : MetadataList<Smo.UserDefinedFunction, Smo.UserDefinedFunctionCollection>
        {
            public UserDefinedFunctionMetadataList(Smo.UserDefinedFunctionCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }

        /// <summary>
        /// This class represents a list of SMO user-defined aggregate objects.
        /// </summary>
        internal class UserDefinedAggregateMetadataList : MetadataList<Smo.UserDefinedAggregate, Smo.UserDefinedAggregateCollection>
        {
            public UserDefinedAggregateMetadataList(Smo.UserDefinedAggregateCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }

        /// <summary>
        /// This class represents a list of SMO stored procedure objects.
        /// </summary>
        internal class StoredProcedureMetadataList : MetadataList<Smo.StoredProcedure, Smo.StoredProcedureCollection>
        {
            public StoredProcedureMetadataList(Smo.StoredProcedureCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }

        /// <summary>
        /// This class represents a list of SMO synonym objects.
        /// </summary>
        internal class SynonymMetadataList : MetadataList<Smo.Synonym, Smo.SynonymCollection>
        {
            public SynonymMetadataList(Smo.SynonymCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }

        /// <summary>
        /// This class represents a list of SMO extended stored procedure objects.
        /// </summary>
        internal class ExtendedStoredProcedureMetadataList : MetadataList<Smo.ExtendedStoredProcedure, Smo.ExtendedStoredProcedureCollection>
        {
            public ExtendedStoredProcedureMetadataList(Smo.ExtendedStoredProcedureCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }

        /// <summary>
        /// This class represents a list of SMO user-defined data type objects.
        /// </summary>
        internal class UserDefinedDataTypeMetadataList : MetadataList<Smo.UserDefinedDataType, Smo.UserDefinedDataTypeCollection>
        {
            public UserDefinedDataTypeMetadataList(Smo.UserDefinedDataTypeCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }

        /// <summary>
        /// This class represents a list of SMO user-defined table type objects.
        /// </summary>
        internal class UserDefinedTableTypeMetadataList : MetadataList<Smo.UserDefinedTableType, Smo.UserDefinedTableTypeCollection>
        {
            public UserDefinedTableTypeMetadataList(Smo.UserDefinedTableTypeCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }

        /// <summary>
        /// This class represents a list of SMO user-defined type (CLR types) objects.
        /// </summary>
        internal class UserDefinedTypeMetadataList : MetadataList<Smo.UserDefinedType, Smo.UserDefinedTypeCollection>
        {
            public UserDefinedTypeMetadataList(Smo.UserDefinedTypeCollection collection, int schemaCount, Database database)
                : base(collection, schemaCount, database)
            {
            }
        }
        #endregion
    }
}
