// Stub: returns empty strings for all resources to remove dependency on embedded .resx resources.

#nullable enable

namespace Microsoft.SqlServer.Management.Common
{
    internal static partial class StringConnectionInfo
    {
        public static global::System.Globalization.CultureInfo? Culture { get; set; }

        internal static string? GetResourceString(string resourceKey, string? defaultValue = null) =>
            defaultValue ?? string.Empty;

        private static string GetResourceString(string resourceKey, string[]? formatterNames) =>
            string.Empty;

        /// <summary>Connection properties cannot be changed after a connection has been established.</summary>
        public static string @ConnectionCannotBeChanged => GetResourceString("ConnectionCannotBeChanged")!;
        /// <summary>Cannot perform this operation as there is no open transaction.</summary>
        public static string @NotInTransaction => GetResourceString("NotInTransaction")!;
        /// <summary>Failed to connect to server {server}.</summary>
        public static string @ConnectionFailure => GetResourceString("ConnectionFailure")!;
        /// <summary>Failed to connect to server {server}.</summary>
        internal static string FormatConnectionFailure(object? server)
           => string.Format(Culture, GetResourceString("ConnectionFailure", new[] { "server" }), server);

        /// <summary>Cannot apply value '{value}' to property {property}: {reason}.</summary>
        public static string @InvalidPropertyValue => GetResourceString("InvalidPropertyValue")!;
        /// <summary>Cannot apply value '{value}' to property {property}: {reason}.</summary>
        internal static string FormatInvalidPropertyValue(object? value, object? property, object? reason)
           => string.Format(Culture, GetResourceString("InvalidPropertyValue", new[] { "value", "property", "reason" }), value, property, reason);

        /// <summary>Value cannot be null.</summary>
        public static string @InvalidPropertyValueReasonString => GetResourceString("InvalidPropertyValueReasonString")!;
        /// <summary>Property {property} is only supported on Windows operating systems.</summary>
        public static string @InvalidIntegratedSecureValue => GetResourceString("InvalidIntegratedSecureValue")!;
        /// <summary>Property {property} is only supported on Windows operating systems.</summary>
        internal static string FormatInvalidIntegratedSecureValue(object? property)
           => string.Format(Culture, GetResourceString("InvalidIntegratedSecureValue", new[] { "property" }), property);

        /// <summary>Value cannot be smaller than {value}.</summary>
        public static string @InvalidPropertyValueReasonInt => GetResourceString("InvalidPropertyValueReasonInt")!;
        /// <summary>Value cannot be smaller than {value}.</summary>
        internal static string FormatInvalidPropertyValueReasonInt(object? value)
           => string.Format(Culture, GetResourceString("InvalidPropertyValueReasonInt", new[] { "value" }), value);

        /// <summary>An exception occurred while executing a Transact-SQL statement or batch.</summary>
        public static string @ExecutionFailure => GetResourceString("ExecutionFailure")!;
        /// <summary>Property {property} was not set.</summary>
        public static string @PropertyNotSetException => GetResourceString("PropertyNotSetException")!;
        /// <summary>Property {property} was not set.</summary>
        internal static string FormatPropertyNotSetException(object? property)
           => string.Format(Culture, GetResourceString("PropertyNotSetException", new[] { "property" }), property);

        /// <summary>Cannot set {property} property because LoginSecure property is set to true.</summary>
        public static string @CannotSetWhenLoginSecure => GetResourceString("CannotSetWhenLoginSecure")!;
        /// <summary>Cannot set {property} property because LoginSecure property is set to true.</summary>
        internal static string FormatCannotSetWhenLoginSecure(object? property)
           => string.Format(Culture, GetResourceString("CannotSetWhenLoginSecure", new[] { "property" }), property);

        /// <summary>Property {property} cannot be changed or read after a connection string has been set.</summary>
        public static string @PropertyNotAvailable => GetResourceString("PropertyNotAvailable")!;
        /// <summary>Property {property} cannot be changed or read after a connection string has been set.</summary>
        internal static string FormatPropertyNotAvailable(object? property)
           => string.Format(Culture, GetResourceString("PropertyNotAvailable", new[] { "property" }), property);

        /// <summary>This SQL Server version ({serverVersion}) is not supported.</summary>
        public static string @ConnectToInvalidVersion => GetResourceString("ConnectToInvalidVersion")!;
        /// <summary>This SQL Server version ({serverVersion}) is not supported.</summary>
        internal static string FormatConnectToInvalidVersion(object? serverVersion)
           => string.Format(Culture, GetResourceString("ConnectToInvalidVersion", new[] { "serverVersion" }), serverVersion);

        /// <summary>{className} class default constructor cannot be used.</summary>
        public static string @ClassDefaulConstructorCannotBeUsed => GetResourceString("ClassDefaulConstructorCannotBeUsed")!;
        /// <summary>{className} class default constructor cannot be used.</summary>
        internal static string FormatClassDefaulConstructorCannotBeUsed(object? className)
           => string.Format(Culture, GetResourceString("ClassDefaulConstructorCannotBeUsed", new[] { "className" }), className);

        /// <summary>Method {methodName} is not supported.</summary>
        public static string @MethodNotSupported => GetResourceString("MethodNotSupported")!;
        /// <summary>Method {methodName} is not supported.</summary>
        internal static string FormatMethodNotSupported(object? methodName)
           => string.Format(Culture, GetResourceString("MethodNotSupported", new[] { "methodName" }), methodName);

        /// <summary>Parse error occurred while looking for GO statement. Line {line}.</summary>
        public static string @ParseError => GetResourceString("ParseError")!;
        /// <summary>Parse error occurred while looking for GO statement. Line {line}.</summary>
        internal static string FormatParseError(object? line)
           => string.Format(Culture, GetResourceString("ParseError", new[] { "line" }), line);

        /// <summary>Password could not be changed.</summary>
        public static string @PasswordCouldNotBeChanged => GetResourceString("PasswordCouldNotBeChanged")!;
        /// <summary>Invalid LockTimeout value: {lockTimeout}.</summary>
        public static string @InvalidLockTimeout => GetResourceString("InvalidLockTimeout")!;
        /// <summary>Invalid LockTimeout value: {lockTimeout}.</summary>
        internal static string FormatInvalidLockTimeout(object? lockTimeout)
           => string.Format(Culture, GetResourceString("InvalidLockTimeout", new[] { "lockTimeout" }), lockTimeout);

        /// <summary>Cache capacity must be greater than {capacity}.</summary>
        public static string @InvalidArgumentCacheCapacity => GetResourceString("InvalidArgumentCacheCapacity")!;
        /// <summary>Cache capacity must be greater than {capacity}.</summary>
        internal static string FormatInvalidArgumentCacheCapacity(object? capacity)
           => string.Format(Culture, GetResourceString("InvalidArgumentCacheCapacity", new[] { "capacity" }), capacity);

        /// <summary>The cache item returns a null key.</summary>
        public static string @InvalidArgumentCacheNullKey => GetResourceString("InvalidArgumentCacheNullKey")!;
        /// <summary>The cache already contains an item with the same key {key}.</summary>
        public static string @InvalidArgumentCacheDuplicateKey => GetResourceString("InvalidArgumentCacheDuplicateKey")!;
        /// <summary>The cache already contains an item with the same key {key}.</summary>
        internal static string FormatInvalidArgumentCacheDuplicateKey(object? key)
           => string.Format(Culture, GetResourceString("InvalidArgumentCacheDuplicateKey", new[] { "key" }), key);

        /// <summary>Microsoft Azure SQL Database Edition</summary>
        public static string @SqlAzureDatabaseEdition => GetResourceString("SqlAzureDatabaseEdition")!;
        /// <summary>Microsoft Azure Synapse Analytics dedicated SQL pools Edition</summary>
        public static string @SqlDataWarehouseEdition => GetResourceString("SqlDataWarehouseEdition")!;
        /// <summary>Microsoft SQL Server Enterprise Edition</summary>
        public static string @EnterpriseEdition => GetResourceString("EnterpriseEdition")!;
        /// <summary>Microsoft SQL Server Express Edition</summary>
        public static string @ExpressEdition => GetResourceString("ExpressEdition")!;
        /// <summary>Microsoft SQL Server Personal Edition</summary>
        public static string @PersonalEdition => GetResourceString("PersonalEdition")!;
        /// <summary>Microsoft SQL Server Standard Edition</summary>
        public static string @StandardEdition => GetResourceString("StandardEdition")!;
        /// <summary>Microsoft SQL Server Stretch Database Edition</summary>
        public static string @StretchEdition => GetResourceString("StretchEdition")!;
        /// <summary>Microsoft Azure SQL Database Managed Instance Edition</summary>
        public static string @SqlManagedInstanceEdition => GetResourceString("SqlManagedInstanceEdition")!;
        /// <summary>Microsoft Azure Synapse Analytics serverless SQL pool on-demand Edition</summary>
        public static string @SqlOnDemandEdition => GetResourceString("SqlOnDemandEdition")!;
        /// <summary>Microsoft Azure SQL Edge Edition</summary>
        public static string @SqlDatabaseEdgeEdition => GetResourceString("SqlDatabaseEdgeEdition")!;
        /// <summary>Microsoft Azure Arc SQL Managed Instance Edition</summary>
        public static string @SqlAzureArcManagedInstanceEdition => GetResourceString("SqlAzureArcManagedInstanceEdition")!;
        /// <summary>Microsoft Azure SQL Database</summary>
        public static string @SqlAzureDatabase => GetResourceString("SqlAzureDatabase")!;
        /// <summary>Standalone SQL Server</summary>
        public static string @Standalone => GetResourceString("Standalone")!;
        /// <summary>Failed to close trace controller.</summary>
        public static string @CannotCloseTraceController => GetResourceString("CannotCloseTraceController")!;
        /// <summary>Failed to retrieve schema table.</summary>
        public static string @CannotRetrieveSchemaTable => GetResourceString("CannotRetrieveSchemaTable")!;
        /// <summary>Failed to read next event.</summary>
        public static string @CannotReadNextEvent => GetResourceString("CannotReadNextEvent")!;
        /// <summary>Failed to retrieve column name.</summary>
        public static string @CannotGetColumnName => GetResourceString("CannotGetColumnName")!;
        /// <summary>Failed to retrieve column type.</summary>
        public static string @CannotGetColumnType => GetResourceString("CannotGetColumnType")!;
        /// <summary>Failed to retrieve column value.</summary>
        public static string @CannotGetColumnValue => GetResourceString("CannotGetColumnValue")!;
        /// <summary>Failed to set column value.</summary>
        public static string @CannotSetColumnValue => GetResourceString("CannotSetColumnValue")!;
        /// <summary>Failed to write event.</summary>
        public static string @CannotWriteEvent => GetResourceString("CannotWriteEvent")!;
        /// <summary>Failed to close writer.</summary>
        public static string @CannotCloseWriter => GetResourceString("CannotCloseWriter")!;
        /// <summary>Failed to initialize object as reader.</summary>
        public static string @CannotInitializeAsReader => GetResourceString("CannotInitializeAsReader")!;
        /// <summary>Failed to initialize object as writer.</summary>
        public static string @CannotInitializeAsWriter => GetResourceString("CannotInitializeAsWriter")!;
        /// <summary>Failed to initialize object as replay output writer.</summary>
        public static string @CannotInitializeAsReplayOutputWriter => GetResourceString("CannotInitializeAsReplayOutputWriter")!;
        /// <summary>Failed to pause trace.</summary>
        public static string @CannotPause => GetResourceString("CannotPause")!;
        /// <summary>Failed to stop trace.</summary>
        public static string @CannotStop => GetResourceString("CannotStop")!;
        /// <summary>Failed to restart trace.</summary>
        public static string @CannotRestart => GetResourceString("CannotRestart")!;
        /// <summary>Failed to start replay.</summary>
        public static string @CannotStartReplay => GetResourceString("CannotStartReplay")!;
        /// <summary>Failed to pause replay.</summary>
        public static string @CannotPauseReplay => GetResourceString("CannotPauseReplay")!;
        /// <summary>Failed to stop replay.</summary>
        public static string @CannotStopReplay => GetResourceString("CannotStopReplay")!;
        /// <summary>Failed to create an instance of type {typeName}.</summary>
        public static string @CannotLoadType => GetResourceString("CannotLoadType")!;
        /// <summary>Failed to create an instance of type {typeName}.</summary>
        internal static string FormatCannotLoadType(object? typeName)
           => string.Format(Culture, GetResourceString("CannotLoadType", new[] { "typeName" }), typeName);

        /// <summary>Failed to get SQL Tools directory path from InstAPI.</summary>
        public static string @FailedToGetSQLToolsDirPathFromInstAPI => GetResourceString("FailedToGetSQLToolsDirPathFromInstAPI")!;
        /// <summary>InstAPI is required for this type, but it is not installed on this computer.</summary>
        public static string @InstAPIIsNotInstalled => GetResourceString("InstAPIIsNotInstalled")!;
        /// <summary>Could not instantiate InstAPI.</summary>
        public static string @CouldNotInstantiateInstAPI => GetResourceString("CouldNotInstantiateInstAPI")!;
        /// <summary>Could not load MethodInfo from InstAPI.</summary>
        public static string @CouldNotLoadMethodInfoFromInstAPI => GetResourceString("CouldNotLoadMethodInfoFromInstAPI")!;
        /// <summary>Failed to translate subclass value.</summary>
        public static string @CannotTranslateSubclass => GetResourceString("CannotTranslateSubclass")!;
        /// <summary>Failed to find column ordinal.</summary>
        public static string @CannotGetOrdinal => GetResourceString("CannotGetOrdinal")!;
        /// <summary>Initializing Trace or Replay Objects in a non-default AppDomain is not fully supported. Verify that you invoke an instance of Trace or Replay Object from the same domain and that the caller object is not marshaled-by-reference.</summary>
        public static string @CannotLoadInAppDomain => GetResourceString("CannotLoadInAppDomain")!;
        /// <summary>Failed to load assembly {assemblyName}. Error message: {originalerror}</summary>
        public static string @AssemblyLoadFailed => GetResourceString("AssemblyLoadFailed")!;
        /// <summary>Failed to load assembly {assemblyName}. Error message: {originalerror}</summary>
        internal static string FormatAssemblyLoadFailed(object? assemblyName, object? originalerror)
           => string.Format(Culture, GetResourceString("AssemblyLoadFailed", new[] { "assemblyName", "originalerror" }), assemblyName, originalerror);

        /// <summary>This functionality is disabled in the SQLCLR. It is recommended that you execute from your client application.</summary>
        public static string @SmoSQLCLRUnAvailable => GetResourceString("SmoSQLCLRUnAvailable")!;
        /// <summary>ServerVersion cannot be set while connected.</summary>
        public static string @CannotBeSetWhileConnected => GetResourceString("CannotBeSetWhileConnected")!;
        /// <summary>Operation cannot be performed when the connection is forced to be disconnected.</summary>
        public static string @CannotPerformOperationWhileDisconnected => GetResourceString("CannotPerformOperationWhileDisconnected")!;
        /// <summary>TrueName cannot be set, unless the connection is forced to be disconnected.</summary>
        public static string @CannotSetTrueName => GetResourceString("CannotSetTrueName")!;
        /// <summary>TrueName must be set if the connection is forced to be disconnected.</summary>
        public static string @TrueNameMustBeSet => GetResourceString("TrueNameMustBeSet")!;
        /// <summary>The password for user '{userName}' could not be saved to the Windows credential manager.</summary>
        public static string @UnableToSavePasswordFormat => GetResourceString("UnableToSavePasswordFormat")!;
        /// <summary>The password for user '{userName}' could not be saved to the Windows credential manager.</summary>
        internal static string FormatUnableToSavePasswordFormat(object? userName)
           => string.Format(Culture, GetResourceString("UnableToSavePasswordFormat", new[] { "userName" }), userName);


    }
}
