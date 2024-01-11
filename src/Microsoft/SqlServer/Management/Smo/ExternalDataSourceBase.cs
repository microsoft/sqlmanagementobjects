// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents a SQL server external data source object.
    ///</summary>
    public partial class ExternalDataSource : NamedSmoObject, Common.IAlterable, Common.ICreatable, Common.IDroppable, IScriptable
    {
        // Microsoft Azure blob storage external data source location
        private const string ExternalDataSourceLocationWasb = "wasb";
        // Azure storage vault external data source location
        private const string ExternalDataSourceLocationAsv = "asv";
        
        internal ExternalDataSource(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Parameterized constructor - populates properties from parameter values.
        /// Overloaded constructor to set all external data source required properties.
        /// </summary>
        /// <param name="parent">The name of the parent database.</param>
        /// <param name="name">The name of the external data source.</param>
        /// <param name="dataSourceType">The external data source type: HADOOP.</param>
        /// <param name="location">The external data source location.</param>
        public ExternalDataSource(Database parent, string name, ExternalDataSourceType dataSourceType, string location)
            : base()
        {
            this.Parent = parent;
            base.Name = name;
            this.DataSourceType = dataSourceType;
            this.Location = location;
        }

        #region IAlterable Members
        /// <summary>
        /// Alter an external data source object.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }
        #endregion

        #region ICreatable Members
        /// <summary>
        /// Create an external data source object.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }
        #endregion

        #region IDroppable Members
        /// <summary>
        /// Drop an external data source object.
        /// </summary>
        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }
        #endregion

        #region IScriptable Members
        /// <summary>
        /// Generate a script for the external data source.
        /// </summary>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Generate a script for the external data source using the
        /// specified scripting options.
        /// </summary>
        /// <param name="scriptingOptions">The scripting options.</param>
        /// <returns>A string collection representing the T-SQL script.</returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }
        #endregion

        #region InternalProperties
        /// <summary>
        /// Returns the name of the object type in the urn expression.
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "ExternalDataSource";
            }
        }
        #endregion

        #region InternalOverrides
        /// <summary>
        /// Constructs a T-SQL string to drop an external data source object.
        /// </summary>
        /// <param name="dropQuery">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string.</param>
        /// <param name="sp">The scripting preferences.</param>
        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            /* DROP EXTERNAL DATA SOURCE external_data_source_name
             */    
        
            string fullyFormattedName = FormatFullNameForScripting(sp);

            // check SQL Server version supports external data source
            this.ThrowIfNotSupported(typeof(ExternalDataSource), sp);

            // check the external data source object state
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // build the T-SQL script to drop the specified external data source, if it exists
            // add a comment header to the T-SQL script
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    ExternalDataSource.UrnSuffix, fullyFormattedName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // check if the specified external data source object exists before attempting to drop it
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(string.Format(SmoApplication.DefaultCulture,
                   Scripts.INCLUDE_EXISTS_EXTERNAL_DATA_SOURCE, String.Empty, FormatFullNameForScripting(sp, false)));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            sb.Append("DROP EXTERNAL DATA SOURCE " + fullyFormattedName);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            dropQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Constructs the T-SQL string to create an external data source object.
        /// </summary>
        /// <param name="createQuery">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string.</param>
        /// <param name="sp">The scripting preferences.</param>
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            /* 
                Polybase Java (aka PolyBase v1) Usage
                CREATE EXTERNAL DATA SOURCE external_data_source_name WITH
                (
                    TYPE = { Hadoop },
                    LOCATION = 'protocol:ip_address:port'
                    ,[RESOURCE_MANAGER_LOCATION = 'ip_address:port']
                    ,[CREDENTIAL = 'credential']
                )[;]

                Polybase external generics (PolyBaseCore) Usage
                CREATE EXTERNAL DATA SOURCE external_data_source_name WITH
                (
                    LOCATION                       = '<prefix>://<path>[:<port>]'
                    [,   CONNECTION_OPTIONS        = '<name_value_pairs>']
                    [,   CREDENTIAL                = <credential_name> ]
                    [,   PUSHDOWN                  = ON | OFF]
                )[;]

                GQ Usage
                CREATE EXTERNAL DATA SOURCE external_data_source_name WITH
                (
                    TYPE = { RDBMS, SHARD_MAP_MANAGER },
                    LOCATION = 'external_location',
                    CREDENTIAL = 'credential',
                    DATABASE_NAME = 'database_name'
                    ,[SHARD_MAP_NAME = 'shard_map_name']
                )[;]
             */
            
            const string DataSourceTypePropertyName = nameof(DataSourceType);
            const string LocationPropertyName = nameof(Location);
            const string ResourceManagerLocationPropertyName = nameof(ResourceManagerLocation);
            const string CredentialPropertyName = nameof(Credential);
            const string DatabaseNamePropertyName = nameof(DatabaseName);
            const string ShardMapNamePropertyName = nameof(ShardMapName);

            // check SQL Server version supports external data source
            this.ThrowIfNotSupported(typeof(ExternalDataSource), sp);

            ExternalDataSourceType externalDataSourceType = ExternalDataSourceType.Hadoop;
            string location = string.Empty;

            // validate properties required for all data source types exist
            ValidatePropertySet(DataSourceTypePropertyName, sp);
            ValidatePropertySet(LocationPropertyName, sp);

            if (IsSupportedProperty(DataSourceTypePropertyName, sp))
            {
                externalDataSourceType = (ExternalDataSourceType)this.GetPropValue(DataSourceTypePropertyName);
                if (!Enum.IsDefined(typeof(ExternalDataSourceType), externalDataSourceType))
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration(externalDataSourceType.ToString()));
                }
            }

            if (IsSupportedProperty(LocationPropertyName, sp))
            {
                location = (string)this.GetPropValue(LocationPropertyName);
            }
            // Supported engine type/edition and source type matrix
            // Gleaned from https://docs.microsoft.com/en-us/sql/t-sql/statements/create-external-data-source-transact-sql
            //
            // SQL2016 : Hadoop
            // SQL2017+ : Hadoop, BlobStorage
            // Azure Sql DB: BlobStorage, Rdbms, ShardMapManager
            // Azure Sql DW: Hadoop
            // Managed Instance: ???

            // validate additional required properties exist depending on the type of data source
            // being created and the right database engine type if targetted            
            switch (externalDataSourceType)
            {
                case ExternalDataSourceType.Hadoop:
                    if (sp.TargetDatabaseEngineEdition == Common.DatabaseEngineEdition.SqlDatabase)
                    {
                        ThrowIfNotSqlDw(sp.TargetDatabaseEngineEdition, ExceptionTemplates.UnsupportedEngineEditionException);
                    }
                    ValidatePropertySet(DataSourceTypePropertyName, sp);
                    ValidatePropertySet(LocationPropertyName, sp);
                    break;
                case ExternalDataSourceType.Rdbms:
                    ThrowIfNotCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);
                    if (sp.TargetDatabaseEngineEdition == Common.DatabaseEngineEdition.SqlDataWarehouse)
                    {
                        throw new UnsupportedEngineEditionException(ExceptionTemplates.UnsupportedEngineEditionException);
                    }
                    ValidatePropertySet(CredentialPropertyName, sp);
                    ValidatePropertySet(DatabaseNamePropertyName, sp);
                    break;
                case ExternalDataSourceType.ShardMapManager:
                    ThrowIfNotCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);
                    if (sp.TargetDatabaseEngineEdition == Common.DatabaseEngineEdition.SqlDataWarehouse)
                    {
                        throw new UnsupportedEngineEditionException(ExceptionTemplates.UnsupportedEngineEditionException);
                    }
                    ValidatePropertySet(CredentialPropertyName, sp);
                    ValidatePropertySet(DatabaseNamePropertyName, sp);
                    ValidatePropertySet(ShardMapNamePropertyName, sp);
                    break;
                case ExternalDataSourceType.ExternalGenerics:
                    ValidatePropertySet(LocationPropertyName, sp);
                    break;
                case ExternalDataSourceType.BlobStorage:
                    if ((sp.TargetDatabaseEngineType == Common.DatabaseEngineType.Standalone && sp.TargetServerVersion < SqlServerVersion.Version140)
                        || (sp.TargetDatabaseEngineType == Common.DatabaseEngineType.SqlAzureDatabase && sp.TargetDatabaseEngineEdition == Common.DatabaseEngineEdition.SqlDataWarehouse))
                    {
                        throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersionException);
                    }
                    ValidatePropertySet(LocationPropertyName, sp);
                    break;
            }

            // validate no unwarranted property is specified for a given data source type
            switch (externalDataSourceType)
            {
                case ExternalDataSourceType.Hadoop:
                    ValidatePropertyUnset(DatabaseNamePropertyName, externalDataSourceType, sp);
                    ValidatePropertyUnset(ShardMapNamePropertyName, externalDataSourceType, sp);
                    break;
                case ExternalDataSourceType.Rdbms:
                    ValidatePropertyUnset(ResourceManagerLocationPropertyName, externalDataSourceType, sp);
                    ValidatePropertyUnset(ShardMapNamePropertyName, externalDataSourceType, sp);
                    break;
                case ExternalDataSourceType.ShardMapManager:
                    ValidatePropertyUnset(ResourceManagerLocationPropertyName, externalDataSourceType, sp);
                    break;
            }
            
            string fullyFormattedName = FormatFullNameForScripting(sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // build the T-SQL script to create an external data source
            // add a comment header to the T-SQL script
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    ExternalDataSource.UrnSuffix, fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // check if the specified external data source object does not already exist before attempting to create one
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_EXTERNAL_DATA_SOURCE, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            TypeConverter typeConverter = SmoManagementUtil.GetTypeConverter(typeof(ExternalDataSourceType));

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE EXTERNAL DATA SOURCE {0} WITH ", fullyFormattedName);
            sb.Append(Globals.LParen);

            if(externalDataSourceType != ExternalDataSourceType.ExternalGenerics)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "TYPE = {0}", typeConverter.ConvertToInvariantString(externalDataSourceType));
                sb.Append(", ");
            }
            sb.AppendFormat(SmoApplication.DefaultCulture, "LOCATION = {0}", Util.MakeSqlString(location));

            // check for the optional properties - ResourceManagerLocation and Credentials
            // check if the ResourceManagerLocation property is supported and set, then add it to the script
            if (IsSupportedProperty(ResourceManagerLocationPropertyName, sp))
            {
                if (!this.GetPropertyOptional(ResourceManagerLocationPropertyName).IsNull)
                {
                    string resourceManagerLocation = Convert.ToString(this.GetPropValueOptional(ResourceManagerLocationPropertyName), SmoApplication.DefaultCulture);
                    if (!string.IsNullOrEmpty(resourceManagerLocation))
                    {
                        ValidateResourceManagerLocation(resourceManagerLocation, location);
                        
                        sb.Append(", ");
                        sb.AppendFormat(SmoApplication.DefaultCulture, "RESOURCE_MANAGER_LOCATION = {0}", Util.MakeSqlString(resourceManagerLocation));
                    }
                }
            }

            // check if the Credential property is supported and set, then add it to the script
            if (IsSupportedProperty(CredentialPropertyName, sp))
            {
                if (!this.GetPropertyOptional(CredentialPropertyName).IsNull)
                {
                    string credential = Convert.ToString(this.GetPropValueOptional(CredentialPropertyName), SmoApplication.DefaultCulture);
                    if (!string.IsNullOrEmpty(credential))
                    {
                        sb.Append(", ");
                        sb.AppendFormat(SmoApplication.DefaultCulture, "CREDENTIAL = {0}", MakeSqlBraket(credential));
                    }
                }
            }

            if (IsSupportedProperty(DatabaseNamePropertyName, sp))
            {
                Property databaseNameProperty = this.GetPropertyOptional(DatabaseNamePropertyName);
                if (!databaseNameProperty.IsNull)
                {
                    string databaseName = Convert.ToString(databaseNameProperty.Value, SmoApplication.DefaultCulture);
                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        sb.Append(", ");
                        sb.AppendFormat(SmoApplication.DefaultCulture, "DATABASE_NAME = {0}", Util.MakeSqlString(databaseName));
                    }
                }
            }

            if (IsSupportedProperty(ShardMapNamePropertyName, sp))
            {
                Property shardMapNameProperty = this.GetPropertyOptional(ShardMapNamePropertyName);
                if (!shardMapNameProperty.IsNull)
                {
                    string shardMapName = Convert.ToString(shardMapNameProperty.Value, SmoApplication.DefaultCulture);
                    if (!string.IsNullOrEmpty(shardMapName))
                    {
                        sb.Append(", ");
                        sb.AppendFormat(SmoApplication.DefaultCulture, "SHARD_MAP_NAME = {0}", Util.MakeSqlString(shardMapName));
                    }
                }
            }

            sb.Append(Globals.RParen);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            createQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Constructs the T-SQL string to alter an external data source object.
        /// </summary>
        /// <param name="alterQuery">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string.</param>
        /// <param name="sp">The scripting preferences.</param>
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            /* ALTER EXTERNAL DATA SOURCE external_data_source_name SET
             * [LOCATION = 'protocol:ip_address:port']
             * ,[RESOURCE_MANAGER_LOCATION = 'ip_address:port']
             * ,[CREDENTIAL = 'credential']
             */

            const string DataSourceTypePropertyName = "DataSourceType";
            const string LocationPropertyName = "Location";
            const string ResourceManagerLocationPropertyName = "ResourceManagerLocation";
            const string CredentialPropertyName = "Credential";
            const string DatabaseNamePropertyName = "DatabaseName";
            const string ShardMapNamePropertyName = "ShardMapName";

            string location = string.Empty;
            string resourceManagerLocation = string.Empty;
            string credential = string.Empty;
            bool addComma = false;

            // check SQL Server version supports external data source
            this.ThrowIfNotSupported(typeof(ExternalDataSource), sp);

            // check if the object has been already created; if not, return.
            if (this.State == SqlSmoState.Creating)
            {
                return;
            }

            // check if the data source type property is supported and is specified for alter
            if (IsSupportedProperty(DataSourceTypePropertyName, sp))
            {
                Property dataSourceTypeProperty = this.GetPropertyOptional(DataSourceTypePropertyName);
                if (!dataSourceTypeProperty.IsNull)
                {
                    if (dataSourceTypeProperty.Dirty)
                    {
                        throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.UnsupportedPropertyForAlter, DataSourceTypePropertyName));
                    }

                    ExternalDataSourceType externalDataSourceType = (ExternalDataSourceType)dataSourceTypeProperty.Value;
                    if (externalDataSourceType == ExternalDataSourceType.ShardMapManager ||
                        externalDataSourceType == ExternalDataSourceType.Rdbms)
                    {
                        throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.AlterNotSupportedForRelationalTypes, externalDataSourceType));
                    }
                }
            }

            // Check is the database name property is supported and is specified for alter. Only relational data sources (sharded and rdbms) can have the database 
            // name property and currently 'alter' is not supported for such data sources.
            if (IsSupportedProperty(DatabaseNamePropertyName, sp))
            {
                Property databaseNameProperty = this.GetPropertyOptional(DatabaseNamePropertyName);
                if (!databaseNameProperty.IsNull && databaseNameProperty.Dirty)
                {
                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.UnsupportedPropertyForAlter, DatabaseNamePropertyName));
                }
            }

            // The shard map name property too can only be had by relational sources and altering the property isn't allowed.
            if (IsSupportedProperty(ShardMapNamePropertyName, sp))
            {
                Property shardMapNameProperty = this.GetPropertyOptional(ShardMapNamePropertyName);
                if (!shardMapNameProperty.IsNull && shardMapNameProperty.Dirty)
                {
                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.UnsupportedPropertyForAlter, ShardMapNamePropertyName));
                }
            }

            string fullyFormattedName = FormatFullNameForScripting(sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // build the T-SQL script to create an external data source
            // add a comment header to the T-SQL script
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    ExternalDataSource.UrnSuffix, fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "ALTER EXTERNAL DATA SOURCE {0} SET ", fullyFormattedName);

            // check if the Location property is supported and set, then add it to the script
            if (IsSupportedProperty(LocationPropertyName, sp))
            {
                Property locationProperty = this.GetPropertyOptional(LocationPropertyName);
                if (!locationProperty.IsNull)
                {
                    location = Convert.ToString(this.GetPropValueOptional(LocationPropertyName), SmoApplication.DefaultCulture);
                    if (!string.IsNullOrEmpty(location))
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "LOCATION = {0}", Util.MakeSqlString(location));
                        addComma = true;
                    }
                }
            }
            
            // check if the ResourceManagerLocation property is supported and set, then add it to the script
            if (IsSupportedProperty(ResourceManagerLocationPropertyName, sp))
            {
                if (!this.GetPropertyOptional(ResourceManagerLocationPropertyName).IsNull)
                {
                    resourceManagerLocation = Convert.ToString(this.GetPropValueOptional(ResourceManagerLocationPropertyName), SmoApplication.DefaultCulture);
                    if (!string.IsNullOrEmpty(resourceManagerLocation))
                    {
                        ValidateResourceManagerLocation(resourceManagerLocation, location);
                        
                        if (addComma)
                        {
                            sb.Append(", ");
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture, "RESOURCE_MANAGER_LOCATION = {0}", Util.MakeSqlString(resourceManagerLocation));
                        addComma = true;
                    }
                }
            }

            // check if the Credential property is supported and set, then add it to the script
            if (IsSupportedProperty(CredentialPropertyName, sp))
            {
                if (!this.GetPropertyOptional(CredentialPropertyName).IsNull)
                {
                    credential = Convert.ToString(this.GetPropValueOptional(CredentialPropertyName), SmoApplication.DefaultCulture);
                    if (!string.IsNullOrEmpty(credential))
                    {
                        if (addComma)
                        {
                            sb.Append(", ");
                        }
                        sb.AppendFormat(SmoApplication.DefaultCulture, "CREDENTIAL = {0}", MakeSqlBraket(credential));
                        addComma = true;
                    }
                }
            }

            if (string.IsNullOrEmpty(location) && string.IsNullOrEmpty(resourceManagerLocation) && string.IsNullOrEmpty(credential))
            {
                // do not throw for this case, as the alter might be invoked by a parent that is modifying multiple child objects
                // simply no-op for this case
                return;
            }

            alterQuery.Add(sb.ToString());
        }
        #endregion

        #region PrivateMethods
        /// <summary>
        /// Validates the specified property is not null and has a
        /// non-null value.
        /// Throws exception, 
        ///     if the property is null, or 
        ///     its value is null or an emptry string.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="sp">The scripting preferences.</param>
        private void ValidatePropertySet(string propertyName, ScriptingPreferences sp)
        {
            if (IsSupportedProperty(propertyName, sp))
            {
                if (this.GetPropertyOptional(propertyName).IsNull)
                {
                    throw new ArgumentNullException(propertyName);
                }
            
                string propertyValue = this.GetPropValue(propertyName).ToString();
                if (string.IsNullOrEmpty(propertyValue))
                {
                    throw new PropertyNotSetException(propertyName);
                }
            }
        }

        /// <summary>
        /// Validates the specified property is not set for a given data source type.
        /// Throws exception, 
        ///     if the property is not null, and it's value is not null or an empty string.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="dataSourceType">The type of external data source.</param>
        /// <param name="sp">The scripting preferences.</param>
        private void ValidatePropertyUnset(string propertyName, ExternalDataSourceType dataSourceType, ScriptingPreferences sp)
        {
            if (IsSupportedProperty(propertyName, sp))
            {
                if (!this.GetPropertyOptional(propertyName).IsNull &&
                    !string.IsNullOrEmpty(this.GetPropValue(propertyName).ToString()))
                {
                    throw new WrongPropertyValueException(
                        string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.UnsupportedParamForDataSourceType, propertyName, dataSourceType));
                }
            }
        }

        /// <summary>
        /// Validates that the ResourceManageLocation property is not specified for 
        /// the external data stored in WASB, secure WASB, ASV, or secure ASV.
        /// If the resource manager location is specified and the external data location is WASB(S) or ASV(S),
        /// throws an exception, as the resource manager location is only supported for external data stored in Hadoop.
        /// </summary>
        /// <param name="externalDataSourceResourceManagerLocaiton">The external data source resource manager location.</param>
        /// <param name="externalDataSourceLocation">The external data source location.</param>
        private void ValidateResourceManagerLocation(string externalDataSourceResourceManagerLocaiton, string externalDataSourceLocation)
        {
            if (!string.IsNullOrEmpty(externalDataSourceResourceManagerLocaiton) &&
                externalDataSourceLocation.StartsWith(ExternalDataSourceLocationWasb, System.StringComparison.OrdinalIgnoreCase) ||
                externalDataSourceLocation.StartsWith(ExternalDataSourceLocationAsv, System.StringComparison.OrdinalIgnoreCase))
            {
                throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.UnsupportedResourceManagerLocationProperty, "ResourceManagerLocation"));
            }
        }
        #endregion

        internal static string[] GetScriptFields(Type parentType,
            Common.ServerVersion version,
            Common.DatabaseEngineType databaseEngineType,
            Common.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            var fields = new string[]
            {
                "Credential",
                "DatabaseName",
                "DataSourceType",
                "ID",
                "Location",
                "Name",
                "ResourceManagerLocation",
                "ShardMapName"
            };

            var list = GetSupportedScriptFields(typeof(DatabaseScopedConfiguration.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}
