// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlParser;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

using Microsoft.SqlServer.Management.SqlParser.Parser;
using ConstraintCollection = Microsoft.SqlServer.Management.SqlParser.MetadataProvider.ConstraintCollection;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    static class Utils
    {
        private readonly static ParseOptions ParseOptionsQuotedIdentifierSet = new ParseOptions(true);
        private readonly static ParseOptions ParseOptionsQuotedIdentifierNotSet = new ParseOptions(false);

        //
        // These two methods retrieve and return the value of a SMO property that is 
        // specified by name. This allows for accessing properties that are not set 
        // without hitting 'PropertyNotSetException'. Rather, these two methods will
        // return null for reference types and Nullable struct for value types.
        //

        public static T GetPropertyObject<T>(Smo.SqlSmoObject smoObject, string propertyName)
            where T : class
        {
            return RetrievePropertyValue<T>(smoObject, propertyName);
        }

        public static T GetPropertyValue<T>(Smo.SqlSmoObject smoObject, string propertyName, T defaultValue)
            where T : struct
        {
            T? value = GetPropertyValue<T>(smoObject, propertyName);
            return value.HasValue ? value.Value : defaultValue;
        }

        public static T? GetPropertyValue<T>(Smo.SqlSmoObject smoObject, string propertyName)
            where T : struct
        {
            return RetrievePropertyValue<T?>(smoObject, propertyName);
        }

        private static T RetrievePropertyValue<T>(Smo.SqlSmoObject smoObject, string propertyName)
        {
            Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "SmoMetadataProvider Assert", "Property name cannot be null or empty!");

            Smo.Property property;
            if (IsDesignMode(smoObject))
            {
                // IMPORTANT : Although the boolean parameter below asks SMO to load the property value
                //             it is completely ignored and no value is loaded.
                // This is exactly the behavior we want in design mode.
                // We do not care for values of properties that are not set.
                property = smoObject.Properties.GetPropertyObject(propertyName, false);
            }
            else
            {
                // In online mode however (IntelliSense for example), we need the value of the property as well.
                // Therefore we call the overload that retrieves the property from property bag
                //           and also reads the value of that property.
                // An exception will be thrown by SMO if the value is null.
                property = smoObject.Properties.GetPropertyObject(propertyName);
            }

            Debug.Assert(property != null, "SmoMetadataProvider Assert", "Property '" + propertyName + "' could not be found!");
            Debug.Assert(IsDesignMode(smoObject) || property.Retrieved, "SmoMetadataProvider Assert", 
                "Property value must be retrieved!");

            object value = property.Value;
            Debug.Assert((value == null) || (value is T), "SmoMetadataProvider Assert",
                "Property value should be of type '" + typeof(T).Name + "'!");

            return (T)value;
        }

        public static bool IsDesignMode(Smo.SqlSmoObject smoObject)
        {
            Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");

            ISfcSupportsDesignMode designModeObject = smoObject as ISfcSupportsDesignMode;
            return (designModeObject != null) && designModeObject.IsDesignMode;
        }

        // 
        // The below two methods are used for getting a property when the user is unsure if a property 
        // is available on the smo object. This is generally true for a trying to fetch a high version property
        // on a lower level server version object.
        //

        public static bool TryGetPropertyObject<T>(Smo.SqlSmoObject smoObject, string propertyName, out T value)
            where T : class
        {
            Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "SmoMetadataProvider Assert", "Property name cannot be null or empty!");

            bool result = true;

            try
            {
                value = Utils.GetPropertyObject<T>(smoObject, propertyName);
            }
            catch (Smo.UnknownPropertyException)
            {
                value = null;
                result = false;
            }
            catch (Smo.PropertyCannotBeRetrievedException)
            {
                value = null;
                result = false;
            }
            catch (Microsoft.SqlServer.Management.Common.PropertyNotAvailableException)
            {
                // We never hit this exception here in any of our Unit tests.
                // Not sure if that should be true in general.
                // Therefore this Debug.Fail may not be correct in future.
                Debug.Fail("SmoMetadataProvider Assert", "Unexpected exception!");
                value = null;
                result = false;
            }

            Debug.Assert(result || value == null, "SmoMetadataProvider Assert", "value must be null when exception is caught.");
            return result;
        }

        public static bool TryGetPropertyValue<T>(Smo.SqlSmoObject smoObject, string propertyName, out T? value)
            where T : struct
        {
            Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "SmoMetadataProvider Assert", "Property name cannot be null or empty!");

            bool result = true;

            try
            {
                value = Utils.GetPropertyValue<T>(smoObject, propertyName);
            }
            catch (Smo.UnknownPropertyException)
            {
                value = default(T?);
                result = false;
            }
            catch (Smo.PropertyCannotBeRetrievedException)
            {
                value = default(T?);
                result = false;
            }
            catch (Microsoft.SqlServer.Management.Common.PropertyNotAvailableException)
            {
                // We never hit this exception here in any of our Unit tests.
                // Not sure if that should be true in general.
                // Therefore this Debug.Fail may not be correct in future.
                Debug.Fail("SmoMetadataProvider Assert", "Unexpected exception!");
                value = default(T?);
                result = false;
            }

            Debug.Assert(result || value == null, "SmoMetadataProvider Assert", "value must be null when exception is caught.");
            return result;
        }

        public static string RetriveStoredProcedureBody(string sql, bool isQuotedIdentifierOn)
        {
            return RetrieveModuleBody(sql, isQuotedIdentifierOn, false);
        }

        public static string RetriveFunctionBody(string sql, bool isQuotedIdentifierOn)
        {
            return RetrieveModuleBody(sql, isQuotedIdentifierOn, false);
        }

        public static string RetriveTriggerBody(string sql, bool isQuotedIdentifierOn)
        {
            return RetrieveModuleBody(sql, isQuotedIdentifierOn, true);
        }

        private static string RetrieveModuleBody(string sql, bool isQuotedIdentifierOn, bool isTrigger)
        {
            Debug.Assert(sql != null, "SmoMetadataProvider Assert", "sql != null");

            ParseOptions parseOptions = new ParseOptions();
            parseOptions.IsQuotedIdentifierSet = isQuotedIdentifierOn;
            parseOptions.TransactSqlVersion = SqlParser.Common.TransactSqlVersion.Current;
            
            IDictionary<string, object> moduleProperties = isTrigger ?
                ParseUtils.RetrieveTriggerDefinition(sql, parseOptions) :
                ParseUtils.RetrieveModuleDefinition(sql, parseOptions);

            Debug.Assert(moduleProperties.ContainsKey(PropertyKeys.BodyDefinition), "MetadataAdaptor Assert", "Key not found");

            object body = moduleProperties[PropertyKeys.BodyDefinition];

            Debug.Assert(body == null || body is string, "MetadataAdaptor Assert", "Unexpected data type");

            return (string)body;
        }

        public static bool IsSpatialIndex(Smo.Index smoIndex)
        {
            Debug.Assert(smoIndex != null, "SmoMetadataProvider Assert", "smoIndex != null");

            bool? isSpatialIndex;

            Utils.TryGetPropertyValue<bool>(smoIndex, "IsSpatialIndex", out isSpatialIndex);
            return isSpatialIndex.GetValueOrDefault();
        }

        public static bool IsXmlIndex(Smo.Index smoIndex)
        {
            Debug.Assert(smoIndex != null, "SmoMetadataProvider Assert", "smoIndex != null");

            bool? isXmlIndex;

            Utils.TryGetPropertyValue<bool>(smoIndex, "IsXmlIndex", out isXmlIndex);
            return isXmlIndex.GetValueOrDefault();
        }

        /// <summary>
        /// Retrieves data type object for a given metadata type.
        /// </summary>
        /// <param name="database">Database object to lookup UDT's.</param>
        /// <param name="metadataType">Metadata type to get the data type for.</param>
        /// <returns>Data type object if found; otherwise null.</returns>
        /// <remarks>Null value schould never be returned. Caller schould ask for existing object</remarks>
        public static IDataType GetDataType(IDatabase database, Smo.DataType metadataType)
        {
            Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
            Debug.Assert(metadataType != null, "SmoMetadataProvider Assert", "metadataType != null");

            IDataType dataType = null;
            
            switch (metadataType.SqlDataType)
            {
                case Smo.SqlDataType.UserDefinedDataType:
                    {
                        string schemaName = metadataType.Schema;
                        string typeName = metadataType.Name;

                        Debug.Assert(!string.IsNullOrEmpty(schemaName), "SmoMetadataProvider Assert", "UDDT must have a valid schema name!");
                        Debug.Assert(!string.IsNullOrEmpty(typeName), "SmoMetadataProvider Assert", "UDDT must have a valid type name!");

                        ISchema schema = database.Schemas[schemaName];
                        Debug.Assert(schema != null, "SmoMetadataProvider Assert", 
                            string.Concat("Failed to retireve schema '", schemaName, "' of database '", database.Name, "'!"));

                        dataType = schema.UserDefinedDataTypes[typeName];
                    }
                    break;
                case Smo.SqlDataType.UserDefinedTableType:
                    {
                        string schemaName = metadataType.Schema;
                        string typeName = metadataType.Name;

                        Debug.Assert(!string.IsNullOrEmpty(schemaName), "SmoMetadataProvider Assert", "UDTT must have a valid schema name!");
                        Debug.Assert(!string.IsNullOrEmpty(typeName), "SmoMetadataProvider Assert", "UDTT must have a valid type name!");

                        ISchema schema = database.Schemas[schemaName];
                        Debug.Assert(schema != null, "SmoMetadataProvider Assert",
                            string.Concat("Failed to retireve schema '", schemaName, "' of database '", database.Name, "'!"));

                        dataType = schema.UserDefinedTableTypes[typeName];
                    }
                    break;
                case Smo.SqlDataType.UserDefinedType:
                    {
                        string schemaName = metadataType.Schema;
                        string typeName = metadataType.Name;

                        Debug.Assert(!string.IsNullOrEmpty(schemaName), "SmoMetadataProvider Assert", "UDT must have a valid schema name!");
                        Debug.Assert(!string.IsNullOrEmpty(typeName), "SmoMetadataProvider Assert", "UDT must have a valid type name!");

                        ISchema schema = database.Schemas[schemaName];
                        Debug.Assert(schema != null, "SmoMetadataProvider Assert",
                            string.Concat("Failed to retireve schema '", schemaName, "' of database '", database.Name, "'!"));

                        dataType = schema.UserDefinedClrTypes[typeName];
                    }
                    break;
                default:
                    {
                        Debug.Assert(metadataType.SqlDataType != Smo.SqlDataType.None, "SmoMetadataProvider",
                            "Smo.SqlDataType cannot be 'None'!");
                        Debug.Assert((metadataType.SqlDataType == Smo.SqlDataType.Xml) || string.IsNullOrEmpty(metadataType.Schema),
                            "SmoMetadataProvider Assert", "System data types should not have schema name!");

                        dataType = SmoSystemDataTypeLookup.Instance.Find(metadataType);
                    }
                    break;
            }

            Debug.Assert(dataType != null, "SmoMetadataProvider Assert", "dataType != null");

            return dataType;
        }

        /// <summary>
        /// Retrieves login object for a given name.
        /// </summary>
        /// <param name="server">Server object to lookup logins.</param>
        /// <param name="loginName">Login name to get the Login for.</param>
        /// <returns>Login object if found; otherwise null.</returns>
        /// <remarks>Null value schould never be returned. Caller schould ask for existing object</remarks>
        public static ILogin GetLogin(IServer server, string loginName)
        {
            Debug.Assert(server != null, "SmoMetadataProvider Assert", "server != null");
            Debug.Assert(loginName != null, "SmoMetadataProvider Assert", "loginName != null");

            ILogin login = server.Logins[loginName];

            Debug.Assert(login != null, "SmoMetadataProvider Assert", "login != null");

            return login;
        }

        /// <summary>
        /// Retrieves and returns <see cref="ISchema"/> object for a given <see cref="IDatabase"/>
        /// object and schema name.
        /// </summary>
        /// <param name="database">The database that contains the schema to search for.</param>
        /// <param name="schemaName">Name of the schema to get.</param>
        /// <returns>Schema object if found; otherwise null.</returns>
        /// <remarks>Null value schould never be returned. Caller schould ask for existing schemas.</remarks>
        public static ISchema GetSchema(IDatabase database, string schemaName)
        {
            Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
            Debug.Assert(schemaName != null, "SmoMetadataProvider Assert", "schemaName != null");

            ISchema schema = database.Schemas[schemaName];

            Debug.Assert(schema != null, "SmoMetadataProvider Assert", "schema != null");

            return schema;
        }

        /// <summary>
        /// Retrieves and returns <see cref="IDatabase"/> object for a given <see cref="IServer"/>
        /// object and database name.
        /// </summary>
        /// <param name="server">The server that contains the database to search for.</param>
        /// <param name="databaseName">Name of the database to get.</param>
        /// <returns>Database object if found; otherwise null.</returns>
        /// <remarks>Null value schould never be returned. Caller schould ask for existing databases.</remarks>
        public static IDatabase GetDatabase(IServer server, string databaseName)
        {
            Debug.Assert(server != null, "SmoMetadataProvider Assert", "server != null");
            Debug.Assert(databaseName != null, "SmoMetadataProvider Assert", "databaseName != null");

            IDatabase database = server.Databases[databaseName];

            Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");

            return database;
        }

        public static IDatabasePrincipal GetDatabasePrincipal(IDatabase database, string prinipalName)
        {
            Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
            Debug.Assert(prinipalName != null, "SmoMetadataProvider Assert", "prinipalName != null");

            IDatabasePrincipal dbPrincipal;

            // users
            dbPrincipal = database.Users[prinipalName];
            if (dbPrincipal != null) return dbPrincipal;

            // database roles
            dbPrincipal = database.Roles[prinipalName];
            if (dbPrincipal != null) return dbPrincipal;

            // application roles
            return database.ApplicationRoles[prinipalName];
        }

        public static ICollation GetCollation(string name)
        {
            Debug.Assert(name != null, "SmoMetadataProvider Assert", "name != null");

            ICollation collation = CollationInfo.GetCollationInfo(name);

            Debug.Assert(collation != null, "SmoMetadataProvider Assert", "collation != null");

            return collation;
        }

        public static CollationInfo GetCollationInfo(string name)
        {
            Debug.Assert(name != null, "SmoMetadataProvider Assert", "name != null");

            CollationInfo collationInfo = CollationInfo.GetCollationInfo(name);

            Debug.Assert(collationInfo != null, "SmoMetadataProvider Assert", "collationInfo != null");

            return collationInfo;
        }

        public static bool IsShilohDatabase(Smo.Database database)
        {
            Debug.Assert(database != null, "SmoMetadataProvider Assert!", "database != null");

            Smo.Server server = database.Parent;
            return server != null && server.VersionMajor == 8;
        }

        public static bool IsUserConvertableToSchema(Smo.User user)
        {
            Debug.Assert(user != null, "SmoMetadataProvider Assert!", "user != null");

            //
            // 1. Consider dbo as a special case
            // 2. Since SMO treats guest as a user schema on Shiloh (not on other Servers), we explicitly
            //    remove guest among the user schemas.

            bool isDboUser = user.Name.Equals("dbo", StringComparison.OrdinalIgnoreCase);
            bool isGuestUser = user.Name.Equals("guest", StringComparison.OrdinalIgnoreCase);
            bool isSystemUser = user.IsSystemObject;

            return isDboUser || (!isSystemUser && !isGuestUser);
        }

        public static bool IsRoleConvertableToSchema(Smo.DatabaseRole role)
        {
            Debug.Assert(role != null, "SmoMetadataProvider Assert!", "role != null");

            //
            // 1. Skip Fixed Roles 
            // 2. Skip public even though it's not a fixed role
            return !role.IsFixedRole 
                && !role.Name.Equals("public", StringComparison.OrdinalIgnoreCase);
        }

        abstract class SmoExecutionContextInfo
        {
            protected abstract ExecutionContextType ContextType { get; }
            protected virtual IServer Server { get { return null; } }
            protected virtual IDatabase Database { get { return null; } }
            protected virtual string UserName { get { return null; } }
            protected virtual string LoginName { get { return null; } }

            public IExecutionContext GetExecutionContext()
            {
                IExecutionContext execContext;
                IExecutionContextFactory execContextFactory = SmoMetadataFactory.Instance.ExecutionContext;

                IDatabase database;
                IServer server;
                IUser user;
                ILogin login;
                string userName;
                string loginName;

                switch (this.ContextType)
                {
                    case ExecutionContextType.Caller:
                        execContext = execContextFactory.CreateExecuteAsCaller();
                        break;
                    case ExecutionContextType.Owner:
                        execContext = execContextFactory.CreateExecuteAsOwner();
                        break;
                    case ExecutionContextType.Self:
                        execContext = execContextFactory.CreateExecuteAsSelf();
                        break;
                    case ExecutionContextType.ExecuteAsUser:
                        database = this.Database;
                        Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");

                        userName = this.UserName;
                        Debug.Assert(userName != null, "SmoMetadataProvider Assert", "userName != null");

                        user = database.Users[userName];
                        Debug.Assert(user != null, "SmoMetadataProvider Assert", "user != null");

                        execContext = execContextFactory.CreateExecuteAsUser(user);
                        break;
                    case ExecutionContextType.ExecuteAsLogin:
                        server = this.Server;
                        Debug.Assert(server != null, "SmoMetadataProvider Assert", "server != null");

                        loginName = this.LoginName;
                        Debug.Assert(loginName != null, "SmoMetadataProvider Assert", "loginName != null");

                        login = server.Logins[loginName];
                        Debug.Assert(login != null, "SmoMetadataProvider Assert", "login != null");

                        execContext = execContextFactory.CreateExecuteAsLogin(login);
                        break;
                    default:
                        Debug.Fail("SmoMetadataProvider Assert", "Unrecognized ExecutionContextType enum value '" + this.ContextType + "'!");
                        execContext = null;
                        break;
                }

                Debug.Assert(execContext != null, "SmoMetadataProvider Assert", "execContext != null");

                return execContext;
            }

#region Constructor Methods

            public static SmoExecutionContextInfo Create(IDatabase database, Smo.StoredProcedure storedProcedure)
            {
                return new StoredProcedureContextInfo(database, storedProcedure);
            }

            public static SmoExecutionContextInfo Create(IDatabase database, Smo.UserDefinedFunction userDefinedFunction)
            {
                return new UserDefinedFunctionContextInfo(database, userDefinedFunction);
            }

            public static SmoExecutionContextInfo Create(IDatabase database, Smo.Trigger dmlTrigger)
            {
                return new DmlTriggerContextInfo(database, dmlTrigger);
            }

            public static SmoExecutionContextInfo Create(IDatabase database, Smo.DatabaseDdlTrigger databaseDdlTrigger)
            {
                return new DatabaseDdlTriggerContextInfo(database, databaseDdlTrigger);
            }

            public static SmoExecutionContextInfo Create(IServer server, Smo.ServerDdlTrigger serverDdlTrigger)
            {
                return new ServerDdlTriggerContextInfo(server, serverDdlTrigger);
            }

#endregion

#region GetContextType Methods

            protected static ExecutionContextType GetContextType(Smo.ExecutionContext executionContext)
            {
                switch (executionContext)
                {
                    case Smo.ExecutionContext.Caller:
                        return ExecutionContextType.Caller;
                    case Smo.ExecutionContext.ExecuteAsUser:
                        return ExecutionContextType.ExecuteAsUser;
                    case Smo.ExecutionContext.Owner:
                        return ExecutionContextType.Owner;
                    case Smo.ExecutionContext.Self:
                        return ExecutionContextType.Self;
                    default:
                        Debug.Fail("SmoMetadataProvider Assert", "Unrecognized Smo.ExecutionContext enum value '" + executionContext + "'!");
                        return default(ExecutionContextType);
                }
            }

            protected static ExecutionContextType GetContextType(Smo.DatabaseDdlTriggerExecutionContext executionContext)
            {
                switch (executionContext)
                {
                    case Smo.DatabaseDdlTriggerExecutionContext.Caller:
                        return ExecutionContextType.Caller;
                    case Smo.DatabaseDdlTriggerExecutionContext.ExecuteAsUser:
                        return ExecutionContextType.ExecuteAsUser;
                    case Smo.DatabaseDdlTriggerExecutionContext.Self:
                        return ExecutionContextType.Self;
                    default:
                        Debug.Fail("SmoMetadataProvider Assert", "Unrecognized Smo.DatabaseDdlTriggerExecutionContext enum value '" + executionContext + "'!");
                        return default(ExecutionContextType);
                }
            }

            protected static ExecutionContextType GetContextType(Smo.ServerDdlTriggerExecutionContext executionContext)
            {
                switch (executionContext)
                {
                    case Smo.ServerDdlTriggerExecutionContext.Caller:
                        return ExecutionContextType.Caller;
                    case Smo.ServerDdlTriggerExecutionContext.ExecuteAsLogin:
                        return ExecutionContextType.ExecuteAsLogin;
                    case Smo.ServerDdlTriggerExecutionContext.Self:
                        return ExecutionContextType.Self;
                    default:
                        Debug.Fail("SmoMetadataProvider Assert", "Unrecognized Smo.ServerDdlTriggerExecutionContext enum value '" + executionContext + "'!");
                        return default(ExecutionContextType);
                }
            }
#endregion

#region Private Classes

            private class StoredProcedureContextInfo : SmoExecutionContextInfo
            {
                private readonly IDatabase database;
                private readonly Smo.StoredProcedure storedProcedure;

                public StoredProcedureContextInfo(IDatabase database, Smo.StoredProcedure storedProcedure)
                {
                    Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
                    Debug.Assert(storedProcedure != null, "SmoMetadataProvider Assert", "storedProcedure != null");

                    this.database = database;
                    this.storedProcedure = storedProcedure;
                }

                protected override ExecutionContextType ContextType
                {
                    get 
                    {
                        Smo.ExecutionContext? executionContext;

                        Utils.TryGetPropertyValue<Smo.ExecutionContext>(this.storedProcedure, "ExecutionContext", out executionContext);

                        //return caller context if the property is not available (due to low level server version)
                        return (executionContext == null || executionContext == default(Smo.ExecutionContext)) ?
                            ExecutionContextType.Caller :
                            GetContextType(executionContext.Value);
                    }
                }

                protected override IDatabase Database
                {
                    get { return this.database;}
                }

                protected override string UserName
                {
                    get { return this.storedProcedure.ExecutionContextPrincipal; }
                }
            }

            private class UserDefinedFunctionContextInfo : SmoExecutionContextInfo
            {
                private readonly IDatabase database;
                private readonly Smo.UserDefinedFunction userDefinedFunction;

                public UserDefinedFunctionContextInfo(IDatabase database, Smo.UserDefinedFunction userDefinedFunction)
                {
                    Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
                    Debug.Assert(userDefinedFunction != null, "SmoMetadataProvider Assert", "userDefinedFunction != null");

                    this.database = database;
                    this.userDefinedFunction = userDefinedFunction;
                }

                protected override ExecutionContextType ContextType
                {
                    get
                    {
                        Smo.ExecutionContext? executionContext;

                        Utils.TryGetPropertyValue<Smo.ExecutionContext>(this.userDefinedFunction, "ExecutionContext", out executionContext);

                        //return caller context if the property is not available (due to low level server version)
                        return (executionContext == null || executionContext == default(Smo.ExecutionContext)) ?
                            ExecutionContextType.Caller :
                            GetContextType(executionContext.Value);
                    }
                }

                protected override IDatabase Database
                {
                    get { return this.database; }
                }

                protected override string UserName
                {
                    get { return this.userDefinedFunction.ExecutionContextPrincipal; }
                }
            }

            private class DmlTriggerContextInfo : SmoExecutionContextInfo
            {
                private readonly IDatabase database;
                private readonly Smo.Trigger dmlTrigger;

                public DmlTriggerContextInfo(IDatabase database, Smo.Trigger dmlTrigger)
                {
                    Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
                    Debug.Assert(dmlTrigger != null, "SmoMetadataProvider Assert", "dmlTrigger != null");

                    this.database = database;
                    this.dmlTrigger = dmlTrigger;
                }

                protected override ExecutionContextType ContextType
                {
                    get
                    {
                        Smo.ExecutionContext? executionContext;

                        Utils.TryGetPropertyValue<Smo.ExecutionContext>(this.dmlTrigger, "ExecutionContext", out executionContext);

                        //return caller context if the property is not available (due to low level server version)
                        return (executionContext == null || executionContext == default(Smo.ExecutionContext)) ?
                            ExecutionContextType.Caller :
                            GetContextType(executionContext.Value);
                    }
                }

                protected override IDatabase Database
                {
                    get { return this.database; }
                }

                protected override string UserName
                {
                    get { return this.dmlTrigger.ExecutionContextPrincipal; }
                }
            }

            private class DatabaseDdlTriggerContextInfo : SmoExecutionContextInfo
            {
                private readonly IDatabase database;
                private readonly Smo.DatabaseDdlTrigger databaseDdlTrigger;

                public DatabaseDdlTriggerContextInfo(IDatabase database, Smo.DatabaseDdlTrigger databaseDdlTrigger)
                {
                    Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
                    Debug.Assert(databaseDdlTrigger != null, "SmoMetadataProvider Assert", "databaseDdlTrigger != null");

                    this.database = database;
                    this.databaseDdlTrigger = databaseDdlTrigger;
                }

                protected override ExecutionContextType ContextType
                {
                    get { return GetContextType(this.databaseDdlTrigger.ExecutionContext); }
                }

                protected override IDatabase Database
                {
                    get { return this.database; }
                }

                protected override string UserName
                {
                    get { return this.databaseDdlTrigger.ExecutionContextUser; }
                }
            }

            private class ServerDdlTriggerContextInfo : SmoExecutionContextInfo
            {
                private readonly IServer server;
                private readonly Smo.ServerDdlTrigger serverDdlTrigger;

                public ServerDdlTriggerContextInfo(IServer server, Smo.ServerDdlTrigger serverDdlTrigger)
                {
                    Debug.Assert(server != null, "SmoMetadataProvider Assert", "server != null");
                    Debug.Assert(serverDdlTrigger != null, "SmoMetadataProvider Assert", "serverDdlTrigger != null");

                    this.server = server;
                    this.serverDdlTrigger = serverDdlTrigger;
                }

                protected override ExecutionContextType ContextType
                {
                    get { return GetContextType(this.serverDdlTrigger.ExecutionContext); }
                }

                protected override IServer Server
                {
                    get { return this.server; }
                }

                protected override string LoginName
                {
                    get { return this.serverDdlTrigger.ExecutionContextLogin; }
                }
            }

#endregion
        }

        public static IExecutionContext GetExecutionContext(IDatabase database, Smo.StoredProcedure smoStoredProc)
        {
            SmoExecutionContextInfo contextInfo = SmoExecutionContextInfo.Create(database, smoStoredProc);
            return contextInfo.GetExecutionContext();
        }

        public static IExecutionContext GetExecutionContext(IDatabase database, Smo.UserDefinedFunction smoFunction)
        {
            SmoExecutionContextInfo contextInfo = SmoExecutionContextInfo.Create(database, smoFunction);
            return contextInfo.GetExecutionContext();
        }

        public static IExecutionContext GetExecutionContext(IDatabase database, Smo.Trigger smoDmlTrigger)
        {
            SmoExecutionContextInfo contextInfo = SmoExecutionContextInfo.Create(database, smoDmlTrigger);
            return contextInfo.GetExecutionContext();
        }

        public static IExecutionContext GetExecutionContext(IDatabase database, Smo.DatabaseDdlTrigger smoDatabaseDdlTrigger)
        {
            SmoExecutionContextInfo contextInfo = SmoExecutionContextInfo.Create(database, smoDatabaseDdlTrigger);
            return contextInfo.GetExecutionContext();
        }

        public static IExecutionContext GetExecutionContext(IServer server, Smo.ServerDdlTrigger smoServerDdlTrigger)
        {
            SmoExecutionContextInfo contextInfo = SmoExecutionContextInfo.Create(server, smoServerDdlTrigger);
            return contextInfo.GetExecutionContext();
        }

        public static string EscapeSqlIdentifier(string value)
        {
            Debug.Assert(!string.IsNullOrEmpty(value), "SmoMetadataProvider", "!string.IsNullOrEmpty(value)");

            return "[" + value.Replace("]", "]]") + "]";
        }

         /// <summary>
        /// Module specific utility methods.
        /// </summary>
        public static class Module
        {
            /// <summary>
            /// Retrieved the module definition text.
            /// </summary>
            /// <param name="module">Module smo object.</param>
            /// <returns>Definition text.</returns>
            public static string GetDefinitionTest(Smo.NamedSmoObject module)
            {
                Debug.Assert(module != null, "SmoMetadataProvider Assert", "module != null");

                string definitionText;
                Utils.TryGetPropertyObject<string>(module, "Text", out definitionText);

                if (definitionText == null)
                {
                    string textHeader;
                    string textBody;
                    Utils.TryGetPropertyObject<string>(module, "TextHeader", out textHeader);
                    Utils.TryGetPropertyObject<string>(module, "TextBody", out textBody);

                    if (textHeader != null && textBody != null)
                    {
                        definitionText = textHeader + " " + textBody;
                    }
                }

                return definitionText;
            }
        }

        /// <summary>
        /// UserDefinedFunction specific utility methods.
        /// </summary>
        public static class UserDefinedFunction
        {
            /// <summary>
            /// Creates a collection of BindInfo Parameter object from a given
            /// metadata parameter collection.
            /// </summary>
            /// <param name="metadataCollection">Collection of metadata parameter objects.</param>
            /// <param name="database">Database object that the table belongs to. 
            /// This is needed for UDT retrieval.</param>
            /// <param name="moduleInfo">Module information retrieved by the ParseUtils.</param>
            /// <returns>
            /// A collection of BindInfo parameter objects for that corresponds to the
            /// given metadata parameter collection.
            /// </returns>
            public static ParameterCollection CreateParameterCollection(
                Database database, Smo.ParameterCollectionBase metadataCollection, IDictionary<string, object> moduleInfo)
            {
                Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
                Debug.Assert(metadataCollection != null, "SmoMetadataProvider Assert", "metadataCollection != null");

                Server server = database.Parent;
                IParameterFactory parameterFactory = SmoMetadataFactory.Instance.Parameter;

                // refresh metadata
                database.Parent.TryRefreshSmoCollection(metadataCollection, Config.SmoInitFields.GetInitFields(typeof(Smo.UserDefinedFunctionParameter)));

                // create parameter collection
                ParameterCollection parameterCollection = new
                    ParameterCollection(metadataCollection.Count, database.CollationInfo);

                IList<IDictionary<string, object>> parametersInfo = moduleInfo != null ?
                        (IList<IDictionary<string, object>>)moduleInfo[PropertyKeys.Parameters] : null;

                // create and add parameters
                foreach (Smo.ParameterBase smoParameter in metadataCollection)
                {
                    // SMO would throw an exception if the parameter's data type is a 
                    // user-defined that the connected user doesn't have access to.
                    // We need to catch the exception in this case and use the
                    // unknown data type.

                    IParameter parameter = null;

                    IDataType dataType;
                    try
                    {
                        dataType = Utils.GetDataType(database, smoParameter.DataType);

                        Debug.Assert(dataType != null, "SmoMetadataProvider Assert",
                            "A user-defined function parameter must have a valid data type!");
                        Debug.Assert(dataType.IsScalar || dataType.IsTable, "SmoMetadataProvider Assert",
                            "A user-defined function parameter can either be scalar or table!");
                    }
                    catch (Smo.SmoException)
                    {
                        // We failed to retrieve data type, assume scalar, create scalar parameter 
                        // using unknown scalar data type.
                        dataType = SmoMetadataFactory.Instance.DataType.UnknownScalar;
                    }

                    IScalarDataType scalarDataType = dataType as IScalarDataType;

                    if (scalarDataType != null)
                    {
                        IDictionary<string, object> parameterInfo = GetParameterInfo(parametersInfo, smoParameter.Name);
                        string defaultValue = parameterInfo != null ? (string)parameterInfo[PropertyKeys.DefaultValue] : null;

                        if (string.IsNullOrEmpty(defaultValue))
                        {
                            // No default value
                            parameter = parameterFactory.CreateScalarParameter(
                                smoParameter.Name, scalarDataType);
                        }
                        else
                        {
                            // Default value
                            parameter = parameterFactory.CreateScalarParameter(
                                smoParameter.Name, scalarDataType, false, defaultValue);
                        }
                    }
                    else
                    {
                        ITableDataType tableDataType = dataType as ITableDataType;
                        Debug.Assert(tableDataType != null, "SmoMetadataProvider Assert", "tableDataType != null");

                        // create table parameter
                        parameter = parameterFactory.CreateTableParameter(smoParameter.Name, tableDataType);
                    }
                    

                    // add created parameter to collection
                    Debug.Assert(parameter != null, "SmoMetadataProvider Assert", "parameter != null");
                    parameterCollection.Add(parameter);
                }

                return parameterCollection;
            }

            private static IDictionary<string, object> GetParameterInfo(IList<IDictionary<string, object>> parametersInfo, string parameterName)
            {
                if (parametersInfo != null)
                {
                    foreach (IDictionary<string, object> parameterInfo in parametersInfo)
                    {
                        Debug.Assert(parameterInfo.ContainsKey(PropertyKeys.Name), "SmoMetadataProvider Assert", "parameterInfo.ContainsKey(PropertyKeys.Name)");
                        Debug.Assert(parameterInfo[PropertyKeys.Name] is string, "SmoMetadataProvider Assert", "parameterInfo[PropertyKeys.Name] is string");

                        if ((string)parameterInfo[PropertyKeys.Name] == parameterName)
                        {
                            return parameterInfo;
                        }
                    }
                }

                return null;
            }
        }


        /// <summary>
        /// Stored procedure specific utility methods.
        /// </summary>
        public static class StoredProcedure
        {
            public static IMetadataOrderedCollection<IParameter> CreateParameterCollection(
                Database database, Smo.StoredProcedureParameterCollection metadataCollection)
            {
                Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
                Debug.Assert(metadataCollection != null, "SmoMetadataProvider Assert", "metadataCollection != null");

                Server server = database.Parent;
                IParameterFactory parameterFactory = SmoMetadataFactory.Instance.Parameter;

                // refresh metadata
                database.Parent.TryRefreshSmoCollection(metadataCollection, Config.SmoInitFields.GetInitFields(typeof(Smo.StoredProcedureParameter)));

                // create parameter collection
                ParameterCollection parameterCollection = new
                    ParameterCollection(metadataCollection.Count, database.CollationInfo);

                // create and add parameters
                foreach (Smo.StoredProcedureParameter smoParameter in metadataCollection)
                {
                    IParameter parameter = null;

                    bool? isCursorParameter;
                    Utils.TryGetPropertyValue<bool>(smoParameter, "IsCursorParameter", out isCursorParameter);

                    if (isCursorParameter.GetValueOrDefault())
                    {
                        parameter = parameterFactory.CreateCursorParameter(smoParameter.Name);
                    }
                    else
                    {
                        // SMO would throw an exception if the parameter's data type is a 
                        // user-defined that the connected user doesn't have access to.
                        // We need to catch the exception in this case and use the
                        // unknown data type.

                        try
                        {
                            IDataType dataType = Utils.GetDataType(database, smoParameter.DataType);

                            Debug.Assert(dataType != null, "SmoMetadataProvider Assert",
                                "A stored proc parameter must have a valid data type!");
                            Debug.Assert(dataType.IsScalar || dataType.IsTable, "SmoMetadataProvider Assert",
                                "A stored proc parameter can either be scalar or table!");

                            IScalarDataType scalarDataType = dataType as IScalarDataType;

                            if (scalarDataType != null)
                            {
                                // Parameters of a scalar function can have default value.
                                // But those of aggregate and table valued functions cannot.
                                // We treat parameter collections for all functions same way since we accept ParameterBase.
                                // To preserve the default value in case of some parameters, we access the property through the property bag.
                                string defaultValue;
                                Utils.TryGetPropertyObject<string>(smoParameter, "DefaultValue", out defaultValue);

                                // Missing default value is indicated by null or empty string.
                                // If a default value is an empty string, smoParameter.DefaultValue = "''".
                                parameter = parameterFactory.CreateScalarParameter(
                                    smoParameter.Name,
                                    scalarDataType,
                                    smoParameter.IsOutputParameter,
                                    string.IsNullOrEmpty(defaultValue) ? null : defaultValue);
                            }
                            else
                            {
                                ITableDataType tableDataType = dataType as ITableDataType;
                                Debug.Assert(tableDataType != null, "SmoMetadataProvider Assert", "tableDataType != null");

                                // create table parameter
                                parameter = parameterFactory.CreateTableParameter(smoParameter.Name, tableDataType);
                            }
                        }
                        catch (Smo.SmoException)
                        {
                            // We failed to retrieve data type, assume scalar, create scalar parameter 
                            // using unknown scalar data type.
                            IScalarDataType dataType = SmoMetadataFactory.Instance.DataType.UnknownScalar;
                            parameter = parameterFactory.CreateScalarParameter(smoParameter.Name, dataType,
                                    smoParameter.IsOutputParameter, smoParameter.DefaultValue);
                        }
                    }

                    // add created parameter to collection
                    Debug.Assert(parameter != null, "SmoMetadataProvider Assert", "parameter != null");
                    parameterCollection.Add(parameter);
                }

                return parameterCollection;
            }

            public static IMetadataOrderedCollection<IParameter> CreateParameterCollection(
                Database database, Smo.StoredProcedure metadataStoredProc)
            {
                Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
                Debug.Assert(metadataStoredProc != null, "SmoMetadataProvider Assert", "metadataStoredProc != null");

                // return value
                IMetadataOrderedCollection<IParameter> parameterCollection = null;

                try
                {
                    // We can only parse SP header for system SPs that are not encrypted.
                    // The reason for that is we cannot reliabily retrieve parameter 
                    // data type if it happened to be user defined (UDDT, UDTT and (UDT).
                    // As for encrypted SPs, we cannot retrieve header text for them.
                    if (metadataStoredProc.IsSystemObject && !metadataStoredProc.IsEncrypted)
                    {
                        // retrieve SP text header
                        string storedProcText = metadataStoredProc.TextHeader;

                        // set parse options
                        ParseOptions parseOptions = metadataStoredProc.QuotedIdentifierStatus ?
                            ParseOptionsQuotedIdentifierSet : ParseOptionsQuotedIdentifierNotSet;

                        parameterCollection = MetadataProviderUtils.GetStoredProcParameters(storedProcText,
                                                                                            SmoMetadataFactory.Instance,
                                                                                            SmoSystemDataTypeLookup.Instance,
                                                                                            database.CollationInfo,
                                                                                            parseOptions);
                    }
                }
                catch (Smo.PropertyCannotBeRetrievedException)
                {
                    // set parameterCollection to null to indicate failure
                    parameterCollection = null;
                }
                catch (SqlParserException)
                {
                    // set parameterCollection to null to indicate failure
                    parameterCollection = null;
                }

                // If we got here with null collection then we failed to retrieve 
                // parameters on our own. We ask SMO for the parameters.
                if (parameterCollection == null)
                {
                    parameterCollection = StoredProcedure.CreateParameterCollection(database, metadataStoredProc.Parameters);
                }

                return parameterCollection;
            }
        }
        public static class DdlTrigger
        {           
            // annawaw-ISSUE-HACK 7/11/2011 Smo Trigger Collection is useless for metadata provider purposes - 
            // it does not have any generic way of enumerating trigger events.
            // Given that, we are better just running a query to retrieve them manually.

            public static TriggerEventTypeSet GetDatabaseTriggerEvents(Smo.DatabaseDdlTrigger trigger)
            {
                Debug.Assert(trigger != null, "SmoMetadataProvider Assert", "trigger != null");

                TriggerEventTypeSet eventSet = new TriggerEventTypeSet();

                string sql = String.Format("select type_desc from sys.trigger_events where object_id={0}", trigger.ID);
                DataSet dataSet = trigger.Parent.ExecuteWithResults(sql);
                
                return GetTriggerEvents(dataSet);
            }

            public static TriggerEventTypeSet GetServerTriggerEvents(Smo.ServerDdlTrigger trigger)
            {
                Debug.Assert(trigger != null, "SmoMetadataProvider Assert", "trigger != null");

                Smo.Database masterDatabase = trigger.Parent.Databases["master"];
                Debug.Assert(masterDatabase != null, "SmoMetadataProvider Assert", "masterDatabase != null");

                TriggerEventTypeSet eventSet = new TriggerEventTypeSet();

                string sql = String.Format("select type_desc from sys.server_trigger_events where object_id={0}", trigger.ID);
                DataSet dataSet = masterDatabase.ExecuteWithResults(sql);

                return GetTriggerEvents(dataSet);
            }

            private static TriggerEventTypeSet GetTriggerEvents(DataSet dataSet)
            {
                Debug.Assert(dataSet != null, "SmoMetadataProvider Assert", "dataSet != null");
                Debug.Assert(dataSet.Tables.Count == 1, "SmoMetadataProvider Assert", "dataSet.Tables.Count == 1");

                DataTable dataTable = dataSet.Tables[0];
                Debug.Assert(dataTable.Columns.Count == 1, "SmoMetadataProvider Assert", "dataTable.Columns.Count == 1");

                TriggerEventTypeSet eventSet = new TriggerEventTypeSet();

                foreach (DataRow dataRow in dataTable.Rows)
                {
                    Debug.Assert(!dataRow.IsNull(0), "SmoMetadataProvider Assert", "!dataRow.IsNull(0)");
                    Debug.Assert(dataRow[0] is string, "SmoMetadataProvider Assert", "dataRow[0] is string");
                    string eventType = (string)dataRow[0];

                    Debug.Assert(!string.IsNullOrEmpty(eventType), "SmoMetadataProvider Assert", "!string.IsNullOrEmpty(eventType)");
                    eventSet.Add(eventType);
                }

                return eventSet;
            }
        }

#region Collection Helper classes
        public abstract class OrderedCollectionHelper<T, S> : DatabaseObjectBase.OrderedCollectionHelperBase<T, S>
            where T : class, IMetadataObject
            where S : Smo.NamedSmoObject
        {
            protected readonly Database m_database;

            public OrderedCollectionHelper(Database database)
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

        public abstract class UnorderedCollectionHelper<T, S> : DatabaseObjectBase.UnorderedCollectionHelperBase<T, S>
            where T : class, IMetadataObject
            where S : Smo.NamedSmoObject
        {
            protected readonly Database m_database;

            public UnorderedCollectionHelper(Database database)
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
        /// Index
        /// </summary>
        public class IndexCollectionHelper : UnorderedCollectionHelper<IIndex, Smo.Index>
        {
            private readonly Smo.IndexCollection smoCollection;
            private readonly IDatabaseTable dbTable;

            public IndexCollectionHelper(Database database, IDatabaseTable dbTable, Smo.IndexCollection smoCollection)
                : base(database)
            {
                this.dbTable = dbTable;
                this.smoCollection = smoCollection;
            }

            protected override DatabaseObjectBase.IMetadataList<Smo.Index> RetrieveSmoMetadataList()
            {
                return new DatabaseObjectBase.SmoCollectionMetadataList<Smo.Index>(
                    this.m_database.Server,
                    this.smoCollection);
            }

            protected override IMutableMetadataCollection<IIndex> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new IndexCollection(initialCapacity, collationInfo);
            }

            protected override IIndex CreateMetadataObject(Smo.Index smoObject)
            {
                Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");

                if (Utils.IsSpatialIndex(smoObject))
                    return new SpatialIndex(this.dbTable, smoObject);

                if (Utils.IsXmlIndex(smoObject))
                    return new XmlIndex(this.dbTable, smoObject);

                return new RelationalIndex(this.m_database, this.dbTable, smoObject);
            }
        }

        /// <summary>
        /// Column
        /// </summary>
        public class ColumnCollectionHelper : OrderedCollectionHelper<IColumn, Smo.Column>
        {
            private readonly Smo.ColumnCollection smoCollection;
            private readonly ISchemaOwnedObject parent;

            public ColumnCollectionHelper(Database database, ISchemaOwnedObject parent, Smo.ColumnCollection smoCollection)
                : base(database)
            {
                Debug.Assert(parent != null, "SmoMetadataProvider Assert", "parent != null");
                Debug.Assert(parent is IDatabaseTable, "SmoMetadataProvider Assert", "parent is IDatabaseTable");

                this.parent = parent;
                this.smoCollection = smoCollection;
            }

            protected override DatabaseObjectBase.IMetadataList<Smo.Column> RetrieveSmoMetadataList()
            {
                return new DatabaseObjectBase.SmoCollectionMetadataList<Smo.Column>(
                    this.m_database.Server,
                    this.smoCollection);
            }

            protected override CollationInfo GetCollationInfo()
            {
                Debug.Assert(this.parent is IDatabaseTable, "SmoMetadataProvider Assert", "this.parent is IDatabaseTable");

                IDatabaseTable dbTable = (IDatabaseTable)this.parent;
                return dbTable.CollationInfo;
            }

            protected override IColumn CreateMetadataObject(Smo.Column smoObject)
            {
                Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");

                return Column.Create(this.parent, smoObject);
            }
        }

        /// <summary>
        /// Index
        /// </summary>
        public class ConstraintCollectionHelper : DatabaseObjectBase.CollectionHelperBase<IConstraint, IMetadataCollection<IConstraint>>
        {
            private readonly Database database;
            private readonly Smo.CheckCollection checks;
            private readonly Smo.ForeignKeyCollection foreignKeys;
            private readonly IDatabaseTable dbTable;

            public ConstraintCollectionHelper(Database database, IDatabaseTable dbTable, Smo.Table table)
            {
                this.database = database;
                this.dbTable = dbTable;
                this.checks = table.Checks;
                this.foreignKeys = table.ForeignKeys;
            }

            public ConstraintCollectionHelper(Database database, IDatabaseTable dbTable, Smo.View view)
            {
                this.database = database;
                this.dbTable = dbTable;
                this.checks = null;
                this.foreignKeys = null;
            }

            public ConstraintCollectionHelper(Database database, IDatabaseTable dbTable, Smo.UserDefinedFunction tableValuedFunction)
            {
                this.database = database;
                this.dbTable = dbTable;
                this.checks = tableValuedFunction.Checks;
                this.foreignKeys = null;
            }

            public ConstraintCollectionHelper(Database database, IDatabaseTable dbTable, Smo.UserDefinedTableType tableType)
            {
                this.database = database;
                this.dbTable = dbTable;
                this.checks = tableType.Checks;
                this.foreignKeys = null;
            }

            protected override Server Server
            {
                get { return this.database.Server; }
            }

            protected override IMetadataCollection<IConstraint> CreateMetadataCollection()
            {
                Server server = database.Parent;

                ConstraintCollection constraints = new ConstraintCollection(database.CollationInfo);

                // add checks
                if (this.checks != null)
                {
                    DatabaseObjectBase.IMetadataList<Smo.Check> checkList = new DatabaseObjectBase.SmoCollectionMetadataList<Smo.Check>(
                        server, this.checks);

                    foreach (Smo.Check smoCheck in checkList)
                        constraints.Add(new CheckConstraint(dbTable, smoCheck));
                }

                // add foreign keys
                if (this.foreignKeys != null)
                {
                    DatabaseObjectBase.IMetadataList<Smo.ForeignKey> foreignKeyList = new DatabaseObjectBase.SmoCollectionMetadataList<Smo.ForeignKey>(
                       server, this.foreignKeys);

                    ITable table = dbTable as ITable;
                    Debug.Assert(table != null, "SmoMetadataProvider Assert", "table != null");

                    foreach (Smo.ForeignKey smoForeignKey in foreignKeyList)
                        constraints.Add(new ForeignKeyConstraint(this.database, table, smoForeignKey));
                }

                foreach (IIndex index in dbTable.Indexes)
                {
                    IRelationalIndex relIndex = index as IRelationalIndex;
                    if (relIndex != null)
                    {
                        IConstraint indexKey = relIndex.IndexKey;
                        if (indexKey != null)
                        {
                            constraints.Add(indexKey);
                        }
                    }
                }

                return constraints;
            }

            protected override IMetadataCollection<IConstraint> GetEmptyCollection()
            {
                return Collection<IConstraint>.Empty;
            }
        }

        /// <summary>
        /// ForeignKeyColumn
        /// </summary>
        public class ForeignKeyColumnCollectionHelper : OrderedCollectionHelper<IForeignKeyColumn, Smo.ForeignKeyColumn>
        {
            private readonly Smo.ForeignKeyColumnCollection smoCollection;
            private readonly ITable table;
            private readonly ITable refTable;

            public ForeignKeyColumnCollectionHelper(Database database, ITable table, ITable refTable, Smo.ForeignKeyColumnCollection smoCollection)
                : base(database)
            {
                this.table = table;
                this.refTable = refTable;
                this.smoCollection = smoCollection;
            }

            protected override DatabaseObjectBase.IMetadataList<Smo.ForeignKeyColumn> RetrieveSmoMetadataList()
            {
                return new DatabaseObjectBase.SmoCollectionMetadataList<Smo.ForeignKeyColumn>(
                    this.m_database.Server,
                    this.smoCollection);
            }

            protected override IForeignKeyColumn CreateMetadataObject(Smo.ForeignKeyColumn smoObject)
            {
                Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");

                IConstraintFactory factory = SmoMetadataFactory.Instance.Constraint;

                IMetadataCollection<IColumn> tableColumns = table.Columns;
                IMetadataCollection<IColumn> refTableColumns = refTable.Columns;

                IColumn referencingColumn = tableColumns[smoObject.Name];
                IColumn referencedColumn = refTableColumns[smoObject.ReferencedColumn];

                Debug.Assert(referencingColumn != null, "SmoMetadataProvider Assert", "referencingColumn != null");
                Debug.Assert(referencedColumn != null, "SmoMetadataProvider Assert", "referencedColumn != null");

                return factory.CreateForeignKeyColumn(referencingColumn, referencedColumn);
            }
        }

        /// <summary>
        /// IndexedColumn
        /// </summary>
        public class IndexedColumnCollectionHelper : OrderedCollectionHelper<IIndexedColumn, Smo.IndexedColumn>
        {
            private readonly Smo.IndexedColumnCollection smoCollection;
            private readonly IDatabaseTable dbTable;

            public IndexedColumnCollectionHelper(Database database, IDatabaseTable dbTable, Smo.IndexedColumnCollection smoCollection)
                : base(database)
            {
                this.dbTable = dbTable;
                this.smoCollection = smoCollection;
            }

            protected override DatabaseObjectBase.IMetadataList<Smo.IndexedColumn> RetrieveSmoMetadataList()
            {
                return new DatabaseObjectBase.SmoCollectionMetadataList<Smo.IndexedColumn>(
                    this.m_database.Server,
                    this.smoCollection);
            }

            protected override IIndexedColumn CreateMetadataObject(Smo.IndexedColumn smoObject)
            {
                Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");
                Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");

                IIndexFactory factory = SmoMetadataFactory.Instance.Index;

                IColumn referencedColumn = dbTable.Columns[smoObject.Name];
                Debug.Assert(referencedColumn != null, "SmoMetadataProvider Assert", "referencedColumn != null");

                IMutableIndexedColumn indexedColumn = factory.CreateIndexedColumn(referencedColumn);

                indexedColumn.SortOrder = smoObject.Descending ? SortOrder.Descending : SortOrder.Ascending;

                bool? included;
                Utils.TryGetPropertyValue<bool>(smoObject, "IsIncluded", out included);

                indexedColumn.IsIncluded = included.GetValueOrDefault();

                return indexedColumn;
            }
        }

        /// <summary>
        /// Statistics
        /// </summary>
        public class StatisticsCollectionHelper : OrderedCollectionHelper<IStatistics, Smo.Statistic>
        {
            private readonly Smo.StatisticCollection smoCollection;
            private readonly IDatabaseTable dbTable;

            public StatisticsCollectionHelper(Database database, IDatabaseTable dbTable, Smo.StatisticCollection smoCollection)
                : base(database)
            {
                this.dbTable = dbTable;
                this.smoCollection = smoCollection;
            }

            protected override DatabaseObjectBase.IMetadataList<Smo.Statistic> RetrieveSmoMetadataList()
            {
                return new DatabaseObjectBase.SmoCollectionMetadataList<Smo.Statistic>(
                    this.m_database.Server,
                    this.smoCollection);
            }

            protected override IStatistics CreateMetadataObject(Smo.Statistic smoObject)
            {
                Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");

                return new Statistics(this.m_database, dbTable, smoObject);
            }
        }

        /// <summary>
        /// StatisticsColumn
        /// </summary>
        public class StatisticsColumnCollectionHelper :  DatabaseObjectBase.CollectionHelperBase<IColumn, IMetadataOrderedCollection<IColumn>>
        {
            private readonly Database database;
            private readonly Smo.StatisticColumnCollection smoCollection;
            private readonly IDatabaseTable dbTable;

            public StatisticsColumnCollectionHelper(Database database, IDatabaseTable dbTable, Smo.StatisticColumnCollection smoCollection)
            {
                this.database = database;
                this.dbTable = dbTable;
                this.smoCollection = smoCollection;
            }

            protected override Server Server
            {
                get { return this.database.Server; }
            }

            protected override IMetadataOrderedCollection<IColumn> CreateMetadataCollection()
            {
                CollationInfo collationInfo = this.dbTable.CollationInfo;

                DatabaseObjectBase.IMetadataList<Smo.StatisticColumn> columnList = new DatabaseObjectBase.SmoCollectionMetadataList<Smo.StatisticColumn>(
                       this.Server, this.smoCollection);

                IColumn[] statisticsColumns = new IColumn[columnList.Count];

                Collection<IColumn>.CreateOrderedCollection(collationInfo);
                int i = 0;
                foreach (Smo.StatisticColumn smoColumn in columnList)
                {
                    statisticsColumns[i++] = dbTable.Columns[smoColumn.Name];
                }
                return Collection<IColumn>.CreateOrderedCollection(collationInfo, statisticsColumns);
            }

            protected override IMetadataOrderedCollection<IColumn> GetEmptyCollection()
            {
                return Collection<IColumn>.EmptyOrdered;
            }
        }

        /// <summary>
        /// DmlTrigger
        /// </summary>
        public class DmlTriggerCollectionHelper : OrderedCollectionHelper<IDmlTrigger, Smo.Trigger>
        {
            private readonly Smo.TriggerCollection smoCollection;
            private readonly ITableViewBase dbTable;

            public DmlTriggerCollectionHelper(Database database, ITableViewBase dbTable, Smo.TriggerCollection smoCollection)
                : base(database)
            {
                this.dbTable = dbTable;
                this.smoCollection = smoCollection;
            }

            protected override DatabaseObjectBase.IMetadataList<Smo.Trigger> RetrieveSmoMetadataList()
            {
                return new DatabaseObjectBase.SmoCollectionMetadataList<Smo.Trigger>(
                    this.m_database.Server,
                    this.smoCollection);
            }

            protected override IDmlTrigger CreateMetadataObject(Smo.Trigger smoObject)
            {
                Debug.Assert(smoObject != null, "SmoMetadataProvider Assert", "smoObject != null");

                return new DmlTrigger(dbTable, smoObject);
            }
        }

#endregion
    }
}

