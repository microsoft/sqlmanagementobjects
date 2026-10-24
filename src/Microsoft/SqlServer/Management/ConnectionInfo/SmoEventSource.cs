// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// EventSource for SQL Server Management Objects (SMO) tracing.
    /// Provides structured logging for database operations, scripting, and execution management.
    /// </summary>
    [EventSource(Name = "Microsoft-SqlServer-Management-Smo")]
    #pragma warning disable CA1822 // EventSource methods must be instance methods
    internal sealed class SmoEventSource : EventSource
    {
        /// <summary>
        /// Singleton instance of the SmoEventSource.
        /// </summary>
        public static readonly SmoEventSource Log = new SmoEventSource();

        /// <summary>
        /// Keywords for categorizing SMO events by functional area.
        /// Ordered by increasing likelihood of being needed for debugging (most common = lowest bit value).
        /// </summary>
        public static class Keywords
        {
            /// <summary>General informational events not specific to a functional area.</summary>
            public const EventKeywords General = (EventKeywords)0x0001;

            /// <summary>Events related to database operations and connections.</summary>
            public const EventKeywords Database = (EventKeywords)0x0002;

            /// <summary>Events related to execution management and SQL command processing.</summary>
            public const EventKeywords Execution = (EventKeywords)0x0004;

            /// <summary>Events related to connection lifecycle and management.</summary>
            public const EventKeywords Connection = (EventKeywords)0x0008;

            /// <summary>Events related to SFC (SQL Foundation Classes) operations.</summary>
            public const EventKeywords Sfc = (EventKeywords)0x0010;

            /// <summary>Events related to SQL script generation and execution.</summary>
            public const EventKeywords Scripting = (EventKeywords)0x0020;

            /// <summary>Events related to data enumeration and type handling.</summary>
            public const EventKeywords DataEnumeration = (EventKeywords)0x0040;

            /// <summary>Events related to database object transfer operations.</summary>
            public const EventKeywords Transfer = (EventKeywords)0x0080;

            /// <summary>Events related to performance and timing measurements.</summary>
            public const EventKeywords Performance = (EventKeywords)0x0100;

            /// <summary>Events related to server event handling and subscriptions.</summary>
            public const EventKeywords ServerEvents = (EventKeywords)0x0200;

            /// <summary>Events related to differencing operations.</summary>
            public const EventKeywords Differencing = (EventKeywords)0x0400;

            /// <summary>Events related to serialization operations.</summary>
            public const EventKeywords Serialization = (EventKeywords)0x0800;
        }

        /// <summary>
        /// Event IDs used throughout the SMO EventSource.
        /// These IDs must remain stable for backwards compatibility.
        /// </summary>
        public static class EventIds
        {
            #region Database Events (1-9)
            /// <summary>Database connection or operation failure.</summary>
            public const int DatabaseConnectionFailure = 1;
            
            /// <summary>SQL exception during database operations.</summary>
            public const int DatabaseSqlException = 2;
            
            /// <summary>General database operation message.</summary>
            public const int DatabaseOperation = 3;
            
            /// <summary>HADR query failure.</summary>
            public const int HadrQueryFailure = 4;
            #endregion

            #region Execution Management Events (10-19)
            /// <summary>URN data retrieval operation.</summary>
            public const int GetDataForUrn = 10;
            
            /// <summary>Dependency resolution operation.</summary>
            public const int GetDependencies = 11;
            
            /// <summary>General execution manager message.</summary>
            public const int ExecutionMessage = 12;
            
            /// <summary>Connection retry exception caught and not thrown.</summary>
            public const int ConnectionRetryException = 13;
            
            /// <summary>Connection configuration warning.</summary>
            public const int ConnectionConfigurationWarning = 14;
            #endregion

            #region Connection Events (20-29)
            /// <summary>Connection attempt started.</summary>
            public const int ConnectionAttemptStarted = 20;
            
            /// <summary>Connection established successfully.</summary>
            public const int ConnectionEstablished = 21;
            
            /// <summary>Connection closed or disconnected.</summary>
            public const int ConnectionClosed = 22;
            
            /// <summary>Connection failure with retry information.</summary>
            public const int ConnectionFailure = 23;
            #endregion

            #region Performance Events (30-39)
            /// <summary>Long-running query detected.</summary>
            public const int LongRunningQuery = 30;
            
            /// <summary>Query execution completed with timing.</summary>
            public const int QueryExecutionCompleted = 31;
            
            /// <summary>Connection timeout occurred.</summary>
            public const int ConnectionTimeout = 32;
            #endregion

            #region Scripting Events (40-49)
            /// <summary>Script generation invocation.</summary>
            public const int ScriptWorkerInvoked = 40;
            
            /// <summary>Scripting progress update.</summary>
            public const int ScriptingProgress = 41;
            
            /// <summary>URN discovery during script generation.</summary>
            public const int UrnsDiscovered = 42;
            
            /// <summary>Object creation script generation.</summary>
            public const int ScriptCreateObjects = 43;
            
            /// <summary>Script creation completion for individual objects.</summary>
            public const int ScriptCreateComplete = 44;
            #endregion

            #region Data Enumeration Events (50-59)
            /// <summary>Error in data type scripting.</summary>
            public const int DataTypeScriptingError = 50;
            #endregion

            #region Server Events (60-69)
            /// <summary>Removal of all server event subscriptions.</summary>
            public const int RemoveAllSubscriptions = 60;

            /// <summary>Server event subscription updated.</summary>
            public const int ServerEventSubscriptionUpdated = 61;
            
            /// <summary>Server event subscription added.</summary>
            public const int ServerEventSubscriptionAdded = 62;
            
            /// <summary>Server event subscription removed.</summary>
            public const int ServerEventSubscriptionRemoved = 63;
            
            /// <summary>Server event subscription started.</summary>
            public const int ServerEventSubscriptionStarted = 64;
            
            /// <summary>Server event subscription query created.</summary>
            public const int ServerEventSubscriptionQueryCreated = 65;
            
            /// <summary>Server event received.</summary>
            public const int ServerEventReceived = 66;
            
            /// <summary>Server event raised to handler.</summary>
            public const int ServerEventRaised = 67;
            #endregion

            #region General Events (70-79)
            /// <summary>General informational message.</summary>
            public const int GeneralMessage = 70;
            #endregion

            #region SFC Events (80-89)
            /// <summary>SFC component trace message.</summary>
            public const int SfcTrace = 80;
            
            /// <summary>SFC exception logging.</summary>
            public const int SfcExceptionCaught = 81;
            
            /// <summary>SFC connection helper exception.</summary>
            public const int SfcConnectionException = 82;
            #endregion

            #region Differencing Events (90-99)
            /// <summary>Differencing operation trace.</summary>
            public const int DifferencingTrace = 90;
            
            /// <summary>Differencing exception logging.</summary>
            public const int DifferencingException = 91;
            #endregion

            #region Transfer Events (100-109)
            /// <summary>Transfer dependency discovery started.</summary>
            public const int TransferDiscoveringDependencies = 100;
            
            /// <summary>Transfer scripting all discovered objects.</summary>
            public const int TransferScriptingObjects = 101;
            
            /// <summary>Transfer visiting object for dependency analysis.</summary>
            public const int TransferVisitingObject = 102;
            
            /// <summary>Transfer marking node for cycle breaking.</summary>
            public const int TransferMarkingNodeForBreaking = 103;
            #endregion

            #region Initialization Events (110-119)
            /// <summary>Missing property warning during initialization.</summary>
            public const int PropertyMissing = 110;
            #endregion
        }

        /// <summary>
        /// Initializes a new instance of the SmoEventSource class.
        /// </summary>
        private SmoEventSource() : base() { }

        #region Database Events

        /// <summary>
        /// Logs database connection or operation failures.
        /// </summary>
        /// <param name="state">Current state of the database object.</param>
        /// <param name="propertyBagState">State of the property bag.</param>
        /// <param name="errorMessage">Error message details.</param>
        [Event(EventIds.DatabaseConnectionFailure, Level = EventLevel.Warning, Keywords = Keywords.Database,
               Message = "Failed to connect for edition fetch, defaulting to Unknown edition. State: {0} PropertyBagState: {1} Error: {2}")]
        public void DatabaseConnectionFailure(string state, string propertyBagState, string errorMessage)
        {
            WriteEvent(EventIds.DatabaseConnectionFailure, state, propertyBagState, errorMessage);
        }

        /// <summary>
        /// Logs SQL exceptions during database operations.
        /// </summary>
        /// <param name="sqlExceptionNumber">SQL exception number.</param>
        /// <param name="state">Current state of the database object.</param>
        [Event(EventIds.DatabaseSqlException, Level = EventLevel.Warning, Keywords = Keywords.Database,
               Message = "Failed to connect for edition fetch. State: {1}, SqlException number: {0}")]
        public void DatabaseSqlException(int sqlExceptionNumber, string state)
        {
            WriteEvent(EventIds.DatabaseSqlException, sqlExceptionNumber, state);
        }

        /// <summary>
        /// Logs general database operation messages.
        /// </summary>
        /// <param name="message">Database operation message.</param>
        [Event(EventIds.DatabaseOperation, Level = EventLevel.Informational, Keywords = Keywords.Database,
               Message = "Database operation: {0}")]
        public void DatabaseOperation(string message)
        {
            WriteEvent(EventIds.DatabaseOperation, message);
        }

        /// <summary>
        /// Logs HADR (High Availability Disaster Recovery) query failures.
        /// </summary>
        /// <param name="errorMessage">Primary error message.</param>
        /// <param name="innerErrorMessage">Inner exception message if available.</param>
        [Event(EventIds.HadrQueryFailure, Level = EventLevel.Warning, Keywords = Keywords.Database,
               Message = "Unable to query sys.fn_hadr_is_primary_replica. {0} {1}")]
        public void HadrQueryFailure(string errorMessage, string innerErrorMessage)
        {
            WriteEvent(EventIds.HadrQueryFailure, errorMessage, innerErrorMessage);
        }

        #endregion

        #region Execution Management Events

        /// <summary>
        /// Logs URN (Uniform Resource Name) data retrieval operations.
        /// </summary>
        /// <param name="urn">The URN being processed.</param>
        [Event(EventIds.GetDataForUrn, Level = EventLevel.Verbose, Keywords = Keywords.Execution,
               Message = "Get data for urn: {0}")]
        public void GetDataForUrn(string urn)
        {
            WriteEvent(EventIds.GetDataForUrn, urn);
        }

        /// <summary>
        /// Logs dependency resolution operations.
        /// </summary>
        [Event(EventIds.GetDependencies, Level = EventLevel.Informational, Keywords = Keywords.Execution,
               Message = "Get dependencies")]
        public void GetDependencies()
        {
            WriteEvent(EventIds.GetDependencies);
        }

        /// <summary>
        /// Logs general execution manager messages.
        /// </summary>
        /// <param name="message">Execution manager message.</param>
        [Event(EventIds.ExecutionMessage, Level = EventLevel.Verbose, Keywords = Keywords.Execution,
               Message = "Execution: {0}")]
        public void ExecutionMessage(string message)
        {
            WriteEvent(EventIds.ExecutionMessage, message);
        }

        /// <summary>
        /// Logs connection retry exceptions that are caught and not thrown.
        /// </summary>
        /// <param name="exceptionMessage">The exception message from the caught exception.</param>
        [Event(EventIds.ConnectionRetryException, Level = EventLevel.Error, Keywords = Keywords.Execution,
               Message = "Exception caught and not thrown while connection retry: {0}")]
        public void ConnectionRetryException(string exceptionMessage)
        {
            WriteEvent(EventIds.ConnectionRetryException, exceptionMessage);
        }

        /// <summary>
        /// Logs connection configuration warnings.
        /// </summary>
        /// <param name="warningMessage">The configuration warning message.</param>
        [Event(EventIds.ConnectionConfigurationWarning, Level = EventLevel.Warning, Keywords = Keywords.Execution,
               Message = "Connection configuration warning: {0}")]
        public void ConnectionConfigurationWarning(string warningMessage)
        {
            WriteEvent(EventIds.ConnectionConfigurationWarning, warningMessage);
        }

        #endregion

        #region Connection Events

        /// <summary>
        /// Logs connection attempt started.
        /// </summary>
        /// <param name="serverName">Name of the server being connected to.</param>
        /// <param name="databaseName">Name of the database being connected to.</param>
        [Event(EventIds.ConnectionAttemptStarted, Level = EventLevel.Informational, Keywords = Keywords.Connection,
               Message = "Connection attempt started to server: {0}, database: {1}")]
        public void ConnectionAttemptStarted(string serverName, string databaseName)
        {
            WriteEvent(EventIds.ConnectionAttemptStarted, serverName ?? "", databaseName ?? "");
        }

        /// <summary>
        /// Logs successful connection establishment.
        /// </summary>
        /// <param name="durationMs">Time taken to establish connection in milliseconds.</param>
        /// <param name="serverName">Name of the server connected to.</param>
        /// <param name="databaseName">Name of the database connected to.</param>
        [Event(EventIds.ConnectionEstablished, Level = EventLevel.Informational, Keywords = Keywords.Connection,
               Message = "Connection established to server: {1}, database: {2}, duration: {0}ms")]
        public void ConnectionEstablished(long durationMs, string serverName, string databaseName)
        {
            WriteEvent(EventIds.ConnectionEstablished, durationMs, serverName ?? "", databaseName ?? "");
        }

        /// <summary>
        /// Logs connection closure or disconnection.
        /// </summary>
        /// <param name="serverName">Name of the server being disconnected from.</param>
        /// <param name="reason">Reason for disconnection.</param>
        [Event(EventIds.ConnectionClosed, Level = EventLevel.Informational, Keywords = Keywords.Connection,
               Message = "Connection closed to server: {0}, reason: {1}")]
        public void ConnectionClosed(string serverName, string reason)
        {
            WriteEvent(EventIds.ConnectionClosed, serverName ?? "", reason ?? "");
        }

        /// <summary>
        /// Logs connection failure with retry information.
        /// </summary>
        /// <param name="willRetry">Whether a retry will be attempted.</param>
        /// <param name="serverName">Name of the server that failed to connect.</param>
        /// <param name="errorMessage">Error message from the connection failure.</param>
        [Event(EventIds.ConnectionFailure, Level = EventLevel.Warning, Keywords = Keywords.Connection,
               Message = "Connection failed to server: {1}, error: {2}, will retry: {0}")]
        public void ConnectionFailure(bool willRetry, string serverName, string errorMessage)
        {
            WriteEvent(EventIds.ConnectionFailure, willRetry, serverName ?? "", errorMessage ?? "");
        }

        #endregion

        #region Performance Events

        /// <summary>
        /// Logs detection of long-running queries.
        /// </summary>
        /// <param name="durationMs">Duration of the query in milliseconds.</param>
        /// <param name="threshold">Threshold that was exceeded.</param>
        /// <param name="queryText">Text of the long-running query (truncated if necessary).</param>
        [Event(EventIds.LongRunningQuery, Level = EventLevel.Warning, Keywords = Keywords.Performance,
               Message = "Long-running query detected: duration {0}ms exceeded threshold {1}ms, query: {2}")]
        public void LongRunningQuery(long durationMs, long threshold, string queryText)
        {
            WriteEvent(EventIds.LongRunningQuery, durationMs, threshold, queryText ?? "");
        }

        /// <summary>
        /// Logs query execution completion with timing information.
        /// </summary>
        /// <param name="durationMs">Duration of the query execution in milliseconds.</param>
        /// <param name="rowsAffected">Number of rows affected by the query.</param>
        /// <param name="queryText">Text of the executed query (truncated if necessary).</param>
        [Event(EventIds.QueryExecutionCompleted, Level = EventLevel.Verbose, Keywords = Keywords.Performance,
               Message = "Query execution completed: duration {0}ms, rows affected: {1}, query: {2}")]
        public void QueryExecutionCompleted(long durationMs, int rowsAffected, string queryText)
        {
            WriteEvent(EventIds.QueryExecutionCompleted, durationMs, rowsAffected, queryText ?? "");
        }

        /// <summary>
        /// Logs connection timeout occurrences.
        /// </summary>
        /// <param name="timeoutSeconds">Timeout value in seconds.</param>
        /// <param name="operation">Operation that timed out.</param>
        [Event(EventIds.ConnectionTimeout, Level = EventLevel.Error, Keywords = Keywords.Performance,
               Message = "Connection timeout occurred during: {1}, timeout: {0}s")]
        public void ConnectionTimeout(int timeoutSeconds, string operation)
        {
            WriteEvent(EventIds.ConnectionTimeout, timeoutSeconds, operation ?? "");
        }

        #endregion

        #region Scripting Events

        /// <summary>
        /// Logs script generation operations.
        /// </summary>
        /// <param name="urnCount">Number of URNs being scripted.</param>
        /// <param name="urnList">List of URNs being processed.</param>
        [Event(EventIds.ScriptWorkerInvoked, Level = EventLevel.Informational, Keywords = Keywords.Scripting,
               Message = "ScriptWorker invoked for {0} Urns: {1}")]
        public void ScriptWorkerInvoked(int urnCount, string urnList)
        {
            WriteEvent(EventIds.ScriptWorkerInvoked, urnCount, urnList);
        }

        /// <summary>
        /// Logs scripting progress updates.
        /// </summary>
        /// <param name="progressStage">Current progress stage.</param>
        [Event(EventIds.ScriptingProgress, Level = EventLevel.Informational, Keywords = Keywords.Scripting,
               Message = "OnScriptingProgress {0}")]
        public void ScriptingProgress(string progressStage)
        {
            WriteEvent(EventIds.ScriptingProgress, progressStage);
        }

        /// <summary>
        /// Logs discovered URNs during script generation.
        /// </summary>
        /// <param name="discoveredCount">Number of URNs discovered.</param>
        /// <param name="urnList">List of discovered URNs.</param>
        [Event(EventIds.UrnsDiscovered, Level = EventLevel.Informational, Keywords = Keywords.Scripting,
               Message = "Discovered {0} Urns: {1}")]
        public void UrnsDiscovered(int discoveredCount, string urnList)
        {
            WriteEvent(EventIds.UrnsDiscovered, discoveredCount, urnList);
        }

        /// <summary>
        /// Logs object creation script generation.
        /// </summary>
        /// <param name="urnCount">Number of URNs for object creation.</param>
        /// <param name="urnList">List of URNs being created.</param>
        [Event(EventIds.ScriptCreateObjects, Level = EventLevel.Informational, Keywords = Keywords.Scripting,
               Message = "ScriptCreateObjects for {0} Urns: {1}")]
        public void ScriptCreateObjects(int urnCount, string urnList)
        {
            WriteEvent(EventIds.ScriptCreateObjects, urnCount, urnList);
        }

        /// <summary>
        /// Logs completion of script creation for individual objects.
        /// </summary>
        /// <param name="urn">URN of the completed object.</param>
        [Event(EventIds.ScriptCreateComplete, Level = EventLevel.Verbose, Keywords = Keywords.Scripting,
               Message = "ScriptCreate complete for {0}")]
        public void ScriptCreateComplete(string urn)
        {
            WriteEvent(EventIds.ScriptCreateComplete, urn);
        }

        #endregion

        #region Server Events

        /// <summary>
        /// Logs removal of all server event subscriptions.
        /// </summary>
        [Event(EventIds.RemoveAllSubscriptions, Level = EventLevel.Informational, Keywords = Keywords.ServerEvents,
               Message = "Removing all subscriptions")]
        public void RemoveAllSubscriptions()
        {
            WriteEvent(EventIds.RemoveAllSubscriptions);
        }

        /// <summary>
        /// Logs when a server event subscription handler is updated.
        /// </summary>
        /// <param name="eventClass">The event class being updated.</param>
        /// <param name="targetClassName">The target class name.</param>
        [Event(EventIds.ServerEventSubscriptionUpdated, Level = EventLevel.Informational, Keywords = Keywords.ServerEvents,
               Message = "Updating event handler for event {0} on class {1}")]
        public void ServerEventSubscriptionUpdated(string eventClass, string targetClassName)
        {
            WriteEvent(EventIds.ServerEventSubscriptionUpdated, eventClass, targetClassName);
        }

        /// <summary>
        /// Logs when a server event subscription is added.
        /// </summary>
        /// <param name="eventClass">The event class being subscribed to.</param>
        /// <param name="targetClassName">The target class name.</param>
        [Event(EventIds.ServerEventSubscriptionAdded, Level = EventLevel.Informational, Keywords = Keywords.ServerEvents,
               Message = "Adding subscription for event {0} on class {1}")]
        public void ServerEventSubscriptionAdded(string eventClass, string targetClassName)
        {
            WriteEvent(EventIds.ServerEventSubscriptionAdded, eventClass, targetClassName);
        }

        /// <summary>
        /// Logs when a server event subscription is removed.
        /// </summary>
        /// <param name="eventClass">The event class being unsubscribed from.</param>
        /// <param name="targetClassName">The target class name.</param>
        [Event(EventIds.ServerEventSubscriptionRemoved, Level = EventLevel.Informational, Keywords = Keywords.ServerEvents,
               Message = "Removing subscription for event {0} on class {1}")]
        public void ServerEventSubscriptionRemoved(string eventClass, string targetClassName)
        {
            WriteEvent(EventIds.ServerEventSubscriptionRemoved, eventClass, targetClassName);
        }

        /// <summary>
        /// Logs when a server event subscription is started.
        /// </summary>
        /// <param name="scopePath">The WMI scope path.</param>
        /// <param name="queryString">The WQL query string.</param>
        [Event(EventIds.ServerEventSubscriptionStarted, Level = EventLevel.Informational, Keywords = Keywords.ServerEvents,
               Message = "Starting event subscription on: {0}, query: {1}")]
        public void ServerEventSubscriptionStarted(string scopePath, string queryString)
        {
            WriteEvent(EventIds.ServerEventSubscriptionStarted, scopePath, queryString);
        }

        /// <summary>
        /// Logs when a server event subscription query is created.
        /// </summary>
        /// <param name="queryString">The WQL query string.</param>
        [Event(EventIds.ServerEventSubscriptionQueryCreated, Level = EventLevel.Informational, Keywords = Keywords.ServerEvents,
               Message = "Subscription query {0}")]
        public void ServerEventSubscriptionQueryCreated(string queryString)
        {
            WriteEvent(EventIds.ServerEventSubscriptionQueryCreated, queryString);
        }

        /// <summary>
        /// Logs when a server event is received from WMI.
        /// </summary>
        /// <param name="eventClassName">The event class name.</param>
        /// <param name="targetClassName">The target class name.</param>
        [Event(EventIds.ServerEventReceived, Level = EventLevel.Informational, Keywords = Keywords.ServerEvents,
               Message = "Received event {0} on class {1}")]
        public void ServerEventReceived(string eventClassName, string targetClassName)
        {
            WriteEvent(EventIds.ServerEventReceived, eventClassName, targetClassName);
        }

        /// <summary>
        /// Logs when a server event is raised to a handler.
        /// </summary>
        /// <param name="eventClassName">The event class name.</param>
        /// <param name="subscriptionKey">The subscription key.</param>
        [Event(EventIds.ServerEventRaised, Level = EventLevel.Informational, Keywords = Keywords.ServerEvents,
               Message = "Raising event {0} on subscription: {1}")]
        public void ServerEventRaised(string eventClassName, string subscriptionKey)
        {
            WriteEvent(EventIds.ServerEventRaised, eventClassName, subscriptionKey);
        }

        #endregion

        #region Data Enumeration Events

        /// <summary>
        /// Logs errors in data type scripting.
        /// </summary>
        /// <param name="dataType">Data type that caused the error.</param>
        [Event(EventIds.DataTypeScriptingError, Level = EventLevel.Error, Keywords = Keywords.DataEnumeration,
               Message = "ERROR: Attempting to script data for type {0}")]
        public void DataTypeScriptingError(string dataType)
        {
            WriteEvent(EventIds.DataTypeScriptingError, dataType);
        }

        #endregion

        #region General Events

        /// <summary>
        /// Logs general informational messages.
        /// </summary>
        /// <param name="message">General message.</param>
        [Event(EventIds.GeneralMessage, Level = EventLevel.Informational, Keywords = Keywords.General,
               Message = "{0}")]
        public void GeneralMessage(string message)
        {
            WriteEvent(EventIds.GeneralMessage, message);
        }

        #endregion

        #region SFC Events

        /// <summary>
        /// Logs SFC component trace messages.
        /// </summary>
        /// <param name="componentName">Name of the SFC component.</param>
        /// <param name="message">Trace message.</param>
        [Event(EventIds.SfcTrace, Level = EventLevel.Verbose, Keywords = Keywords.Sfc,
               Message = "[{0}] {1}")]
        public void SfcTrace(string componentName, string message)
        {
            WriteEvent(EventIds.SfcTrace, componentName ?? "", message ?? "");
        }

        /// <summary>
        /// Logs SFC exceptions that are caught.
        /// </summary>
        /// <param name="exceptionMessage">The exception message.</param>
        [Event(EventIds.SfcExceptionCaught, Level = EventLevel.Error, Keywords = Keywords.Sfc,
               Message = "SFC exception caught: {0}")]
        public void SfcExceptionCaught(string exceptionMessage)
        {
            WriteEvent(EventIds.SfcExceptionCaught, exceptionMessage ?? "");
        }

        /// <summary>
        /// Logs SFC exceptions that are caught with full exception details.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        [NonEvent]
        public void SfcExceptionCaught(Exception exception)
        {
            // Use ToString() to get full exception details including stack trace and inner exceptions
            SfcExceptionCaught(exception?.ToString() ?? "Unknown exception");
        }

        /// <summary>
        /// Logs SFC connection helper exceptions.
        /// </summary>
        /// <param name="exceptionMessage">The exception message.</param>
        [Event(EventIds.SfcConnectionException, Level = EventLevel.Error, Keywords = Keywords.Sfc,
               Message = "SFC connection exception: {0}")]
        public void SfcConnectionException(string exceptionMessage)
        {
            WriteEvent(EventIds.SfcConnectionException, exceptionMessage ?? "");
        }

        /// <summary>
        /// Logs SFC connection helper exceptions with full exception details.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        [NonEvent]
        public void SfcConnectionException(Exception exception)
        {
            // Use ToString() to get full exception details including stack trace and inner exceptions
            SfcConnectionException(exception?.ToString() ?? "Unknown exception");
        }

        #endregion

        #region Differencing Events

        /// <summary>
        /// Logs differencing operation trace messages.
        /// </summary>
        /// <param name="message">Trace message.</param>
        [Event(EventIds.DifferencingTrace, Level = EventLevel.Verbose, Keywords = Keywords.Differencing,
               Message = "{0}")]
        public void DifferencingTrace(string message)
        {
            WriteEvent(EventIds.DifferencingTrace, message ?? "");
        }

        /// <summary>
        /// Logs differencing exceptions that are caught.
        /// </summary>
        /// <param name="exceptionMessage">The exception message.</param>
        [Event(EventIds.DifferencingException, Level = EventLevel.Error, Keywords = Keywords.Differencing,
               Message = "Differencing exception: {0}")]
        public void DifferencingException(string exceptionMessage)
        {
            WriteEvent(EventIds.DifferencingException, exceptionMessage ?? "");
        }

        /// <summary>
        /// Logs differencing exceptions that are caught with full exception details.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        [NonEvent]
        public void DifferencingException(Exception exception)
        {
            // Use ToString() to get full exception details including stack trace and inner exceptions
            DifferencingException(exception?.ToString() ?? "Unknown exception");
        }

        #endregion

        #region Transfer Events

        /// <summary>
        /// Logs transfer dependency discovery phase.
        /// </summary>
        [Event(EventIds.TransferDiscoveringDependencies, Level = EventLevel.Informational, Keywords = Keywords.Transfer,
               Message = "Transfer: Discovering dependencies")]
        public void TransferDiscoveringDependencies()
        {
            WriteEvent(EventIds.TransferDiscoveringDependencies);
        }

        /// <summary>
        /// Logs transfer scripting phase.
        /// </summary>
        [Event(EventIds.TransferScriptingObjects, Level = EventLevel.Informational, Keywords = Keywords.Transfer,
               Message = "Transfer: Script all discovered objects")]
        public void TransferScriptingObjects()
        {
            WriteEvent(EventIds.TransferScriptingObjects);
        }

        /// <summary>
        /// Logs transfer visiting an object for dependency analysis.
        /// </summary>
        /// <param name="nonSystemOnly">True if only non-system objects are added</param>
        /// <param name="objectInfo">Information about the object being visited.</param>
        [Event(EventIds.TransferVisitingObject, Level = EventLevel.Verbose, Keywords = Keywords.Transfer,
               Message = "Transfer: Visiting object {0}")]
        public void TransferVisitingObject(bool nonSystemOnly, string objectInfo)
        {
            WriteEvent(EventIds.TransferVisitingObject, nonSystemOnly, objectInfo ?? "");
        }

        /// <summary>
        /// Logs transfer marking node for cycle breaking.
        /// </summary>
        /// <param name="nodeInfo">Information about the node being marked.</param>
        [Event(EventIds.TransferMarkingNodeForBreaking, Level = EventLevel.Verbose, Keywords = Keywords.Transfer,
               Message = "Transfer: Marking node for breaking - {0}")]
        public void TransferMarkingNodeForBreaking(string nodeInfo)
        {
            WriteEvent(EventIds.TransferMarkingNodeForBreaking, nodeInfo ?? "");
        }

        #endregion

        #region Initialization Events

        /// <summary>
        /// Logs missing property warnings during object initialization.
        /// </summary>
        /// <param name="isExpensive">Whether the property is an expensive property.</param>
        /// <param name="isDesignMode">Whether the object is in design mode.</param>
        /// <param name="propertyName">Name of the missing property.</param>
        /// <param name="typeName">Type name of the object.</param>
        /// <param name="propertyBagState">Current property bag state.</param>
        [Event(EventIds.PropertyMissing, Level = EventLevel.Warning, Keywords = Keywords.General,
               Message = "Missing property '{2}' for type {3}: IsExpensive={0}, DesignMode={1}, PropertyBagState={4}")]
        public void PropertyMissing(bool isExpensive, bool isDesignMode, string propertyName, string typeName, string propertyBagState)
        {
            WriteEvent(EventIds.PropertyMissing, isExpensive, isDesignMode, propertyName ?? "", typeName ?? "", propertyBagState ?? "");
        }

        #endregion
    }
}