// Copyright (c) Microsoft.
// Licensed under the MIT license.

#define 	INCLUDE_PERF_COUNT
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Server;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Defines an object that exposes application-wide SMO events
    /// </summary>
    public interface ISmoApplicationEvents
    {
        /// <summary>
        /// Event that is called when an object is successfully created.
        /// </summary>
        event SmoApplication.ObjectCreatedEventHandler ObjectCreated;

        /// <summary>
        /// Event that is called when an object is successfully dropped.
        /// </summary>
        event SmoApplication.ObjectDroppedEventHandler ObjectDropped;

        /// <summary>
        /// Event that is called when an object is successfully renamed.
        /// </summary>
        event SmoApplication.ObjectRenamedEventHandler ObjectRenamed;

        /// <summary>
        /// Event that is called when an object is successfully altered.
        /// </summary>
        event SmoApplication.ObjectAlteredEventHandler ObjectAltered;

        /// <summary>
        /// Event raised to indicate something changed about a SMO object.
        /// </summary>
        event SmoApplication.AnyObjectEventHandler AnyObjectEvent;

        event SmoApplication.DatabaseEventHandler DatabaseEvent;
    }

    /// <summary>
    /// This class holds global application data for SMO
    /// </summary>
    internal class SmoApplicationEventsSingleton : ISmoApplicationEvents
    {
        #region Internal events
        private SmoApplication.ObjectCreatedEventHandler objectCreated;
        /// <summary>
        /// Event that is called when an object is successfully created.
        /// </summary>
        public event SmoApplication.ObjectCreatedEventHandler ObjectCreated
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                objectCreated += value;
            }
            remove
            {
                objectCreated -= value;
            }
        }

        internal void CallObjectCreated(object sender, ObjectCreatedEventArgs e)
        {
            if(objectCreated != null)
            {
                objectCreated(sender, e);
            }

            if (anyObjectEvent != null)
            {
                anyObjectEvent(sender, e);
            }
        }

        internal bool IsNullObjectCreated()
        {
            return objectCreated == null;
        }

       
        private SmoApplication.ObjectDroppedEventHandler objectDropped;
        /// <summary>
        /// Event that is called when an object is successfully dropped.
        /// </summary>
        public event SmoApplication.ObjectDroppedEventHandler ObjectDropped
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                objectDropped += value;
            }
            remove
            {
                objectDropped -= value;
            }
        }

        internal void CallObjectDropped(object sender, ObjectDroppedEventArgs e)
        {
            if(objectDropped != null)
            {
                objectDropped(sender, e);
            }

            if (anyObjectEvent != null)
            {
                anyObjectEvent(sender, e);
            }
        }

        internal bool IsNullObjectDropped()
        {
            return objectDropped == null;
        }

        private SmoApplication.ObjectRenamedEventHandler objectRenamed;
        /// <summary>
        /// Event that is called when an object is successfully renamed.
        /// </summary>
        public event SmoApplication.ObjectRenamedEventHandler ObjectRenamed
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                objectRenamed += value;
            }
            remove
            {
                objectRenamed -= value;
            }
        }

        internal void CallObjectRenamed(object sender, ObjectRenamedEventArgs e)
        {
            if(objectRenamed != null)
            {
                objectRenamed(sender, e);
            }

            if (anyObjectEvent != null)
            {
                anyObjectEvent(sender, e);
            }
        }

        internal bool IsNullObjectRenamed()
        {
            return objectRenamed == null;
        }

        
        private SmoApplication.ObjectAlteredEventHandler objectAltered;
        /// <summary>
        /// Event that is called when an object is successfully altered.
        /// </summary>
        public event SmoApplication.ObjectAlteredEventHandler ObjectAltered
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                objectAltered += value;
            }
            remove
            {
                objectAltered -= value;
            }
        }

        internal void CallObjectAltered(object sender, ObjectAlteredEventArgs e)
        {
            if(objectAltered != null)
            {
                objectAltered(sender, e);
            }

            if (anyObjectEvent != null)
            {
                anyObjectEvent(sender, e);
            }
        }

        internal bool IsNullObjectAltered()
        {
            return objectAltered == null;
        }

        
        private SmoApplication.AnyObjectEventHandler anyObjectEvent;
        /// <summary>
        /// Event raised to indicate something changed about a SMO object.
        /// </summary>
        public event SmoApplication.AnyObjectEventHandler AnyObjectEvent
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                anyObjectEvent += value;
            }
            remove
            {
                anyObjectEvent -= value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        private SmoApplication.DatabaseEventHandler databaseEvent;
        public event SmoApplication.DatabaseEventHandler DatabaseEvent
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                databaseEvent += value;
            }
            remove
            {
                databaseEvent -= value;
            }
        }

        internal void CallDatabaseEvent(object sender, DatabaseEventArgs e)
        {
            if(databaseEvent != null)
            {
                databaseEvent(sender, e);
            }

            if (anyObjectEvent != null)
            {
                anyObjectEvent(sender, e);
            }
        }

        internal bool IsNullDatabaseEvent()
        {
            return databaseEvent == null;
        }


        #endregion
    }
    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors")]
    public class SmoApplication 
    {
        // tracing constants
        internal static readonly uint trL1 = Diagnostics.SQLToolsCommonTraceLvl.L1;
        internal static readonly uint trL2 = Diagnostics.SQLToolsCommonTraceLvl.L2;
        internal static readonly uint trErr = Diagnostics.SQLToolsCommonTraceLvl.Error;
        internal static readonly uint trWarn = Diagnostics.SQLToolsCommonTraceLvl.Warning;
        internal static readonly uint trAlways = Diagnostics.SQLToolsCommonTraceLvl.Always;
        internal static readonly SmoApplicationEventsSingleton eventsSingleton = new SmoApplicationEventsSingleton();

        /// <summary>
        /// Returns the object on which to subscribe for application-wide SMO events
        /// </summary>
        public static ISmoApplicationEvents EventsSingleton
        {
            get { return eventsSingleton; }
        }

        internal static readonly string ModuleName = "Smo";
        
        internal static string Namespace 
        { 
            get
            {
                return typeof(SmoApplication).Namespace;
            }
        }
        
        // old-style conversion between UInt32 and Int32 
        // we need this function because the regular conversion throws on overflow
        internal static Int32 ConvertUInt32ToInt32(UInt32 value)
        {
            UInt32[] arr = new UInt32[] { value};
            Int32[] result = new Int32[1];
            Buffer.BlockCopy( arr, 0, result, 0, 4 );
            return result[0];
        }

        internal static UInt32 ConvertInt32ToUInt32(Int32 value)
        {
            Int32[] arr = new Int32[] { value};
            UInt32[] result = new UInt32[1];
            Buffer.BlockCopy( arr, 0, result, 0, 4 );
            return result[0];
        }

        /// <summary>
        /// Returns the default culture used by SMO components for formatting
        /// </summary>
        public static CultureInfo DefaultCulture 
        {
            get
            {
                return CultureInfo.InvariantCulture;
            }
        }

        //return the list of a available Sql Servers
        static public DataTable EnumAvailableSqlServers()
        {
            try
            {
                return Enumerator.GetData(null, "AvailableSqlServer");
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.EnumAvailableSqlServers, null, e);
            }
        }

        //if localOnly == true only local servers are listed
        static public DataTable EnumAvailableSqlServers(bool localOnly)
        {
            try
            {
                if( true == localOnly )
                {
                    return Enumerator.GetData(null, "AvailableSqlServer[@IsLocal = true()]");
                }
                else
                {
                    return Enumerator.GetData(null, "AvailableSqlServer");
                }
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.EnumAvailableSqlServers, null, e);
            }
        }

        //returns a datatable containing 0 rows or one row describing the server Name
        static public DataTable EnumAvailableSqlServers(string name)
        {
            try
            {
                if( null == name )
                {
                    throw new ArgumentNullException("name");
                }

                return Enumerator.GetData(null, "AvailableSqlServer[@Name = '" + Urn.EscapeString(name) + "']");
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);
                
                throw new FailedOperationException(ExceptionTemplates.EnumAvailableSqlServers, null, e);
            }
        }
        #region Internal events
        /// <summary>
        /// Called when an object is successfully created
        /// </summary>
        public delegate void ObjectCreatedEventHandler(object sender, ObjectCreatedEventArgs e);

        /// <summary>
        /// called when an object is successfully dropped
        /// </summary>
        public delegate void ObjectDroppedEventHandler(object sender, ObjectDroppedEventArgs e);

        /// <summary>
        /// called when an object is successfully renamed
        /// </summary>
        public delegate void ObjectRenamedEventHandler(object sender, ObjectRenamedEventArgs e);

        /// <summary>
        /// called when an object is successfully altered
        /// </summary>
        public delegate void ObjectAlteredEventHandler(object sender, ObjectAlteredEventArgs e);

        /// <summary>
        /// called for any of the above events. This allows handling all event types using one handler
        /// </summary>
        public delegate void AnyObjectEventHandler(object sender, SmoEventArgs e);

        /// <summary>
        /// called for database events
        /// </summary>
        public delegate void DatabaseEventHandler(object sender, SmoEventArgs e);

        /// <summary>
        /// Event that is called when an object is successfully created.
        /// </summary>
        #endregion
    }

    #region Arguments for event delegates
    /// <summary>
    /// Base argument class for Smo native events
    /// </summary>
    public class SmoEventArgs : System.EventArgs
    {
        private Urn urn;

        public Urn Urn
        {
            get { return urn; }
        }
        public SmoEventArgs(Urn urn)
        {
            this.urn = urn;
        }
    }

    /// <summary>
    /// argument for ObjectCreatedEventHandler
    /// </summary>
    public class ObjectCreatedEventArgs : SmoEventArgs
    {
        private object innerObject;

        public object SmoObject
        {
            get { return innerObject; }
        }

        public ObjectCreatedEventArgs(Urn urn, object innerObject) : base (urn)
        {
            this.innerObject = innerObject;
        }
    }

    /// <summary>
    /// argument for ObjectDroppedEventHandler
    /// </summary>
    public class ObjectDroppedEventArgs : SmoEventArgs
    {
        public ObjectDroppedEventArgs(Urn urn) : base (urn)
        {
        }
    }

    /// <summary>
    /// argument for ObjectRenamedEventHandler
    /// </summary>
    public class ObjectRenamedEventArgs : SmoEventArgs
    {
        string oldUrn;
        string newName;
        string oldName; // these two are not needed any more but are left for backwards compat with RTM
        object innerObject;

        // Deprecated, do not use
        public object SmoObject
        {
            get { return innerObject; }
        }

        // Deprecated, do not use
        public string OldName
        {
            get { return oldName; }
        }

        public string NewName
        {
            get { return newName; }
        }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string OldUrn
        {
            get { return oldUrn; }
        }

        public ObjectRenamedEventArgs(Urn urn, object innerObject,
            string oldName, string newName) : base (urn)
        {
            this.innerObject = innerObject;
            this.oldName = oldName;
            this.newName = newName;
        }

        // New constructor. Parameters innerObject, oldName and newName are left for backwards
        // compatibility with old receivers of these events
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public ObjectRenamedEventArgs(Urn newUrn, object innerObject,
            string oldName, string newName, string oldUrn) : base (newUrn)
        {
            this.innerObject = innerObject;
            this.oldName = oldName;
            this.newName = newName;
            this.oldUrn = oldUrn;
        }

    }

    /// <summary>
    /// argument for ObjectAlteredEventHandler
    /// </summary>
    public class ObjectAlteredEventArgs : SmoEventArgs
    {
        object innerObject;

        public object SmoObject
        {
            get { return innerObject; }
        }

        public ObjectAlteredEventArgs(Urn urn, object innerObject) : 
            base (urn)
        {
            this.innerObject = innerObject;
        }
    }


    // specifies the operation that causes a database state to change
    public enum DatabaseEventType
    {
        /// <summary>
        /// Server.AttachDatabase
        /// </summary>
        Attach,
        /// <summary>
        /// Server.DetachDatabase
        /// </summary>
        Detach,
        /// <summary>
        /// Restore.SqlRestore[Async]() (RestoreActionType.Database)
        /// </summary>
        Restore,
        /// <summary>
        /// Database.SetOffine()
        /// </summary>
        Offline,
        /// <summary>
        /// Database.SetOnline()
        /// </summary>
        Online,
        /// <summary>
        /// Backup.SqlBackup[Async]() (BackupActionType.Database)
        /// </summary>
        Backup
    }

    /// <summary>
    /// Provides information about a change to a Database SMO object
    /// </summary>
    public class DatabaseEventArgs : SmoEventArgs
    {
        public DatabaseEventArgs(Urn urn, object obj, string name, DatabaseEventType databaseEventType) : 
            base (urn)
        {
            this.innerObject = obj;
            this.databaseName = name;
            this.databaseEventType = databaseEventType;
        }

        object innerObject;
        string databaseName;
        DatabaseEventType databaseEventType;

        public object SmoObject
        {
            get { return innerObject; }
        }

        public DatabaseEventType DatabaseEventType
        {
            get { return databaseEventType; }
        }

        public string Name
        {
            get { return databaseName; }
        }

    }

    #endregion
    

#if INCLUDE_PERF_COUNT
    internal class PerformanceCountersSingleton
    {
        internal bool doCount = false;
        internal TimeSpan enumQueriesDuration = new TimeSpan(0);
        internal Int32 enumQueriesCount = 0;
        internal Hashtable urnSkeletonsPerf = new Hashtable();
        internal TimeSpan sqlExecutionDuration = new TimeSpan(0);
        internal TimeSpan dependencyDiscoveryDuration = new TimeSpan(0);
        internal Int32 objectInfoRequestCount = 0;
        internal Int32 initializeCallsCount = 0;
        internal Int32 urnCallsCount = 0;
        internal Int32 urnSkelCallsCount = 0;
        internal TimeSpan dscoverDependenciesDuration = new TimeSpan(0);
        internal TimeSpan walkDependenciesDuration = new TimeSpan(0);
    }
    public sealed class PerformanceCounters
    {
        
        public static bool DoCount { get{ return performanceCountersSingleton.doCount;} set { performanceCountersSingleton.doCount = value;}}
        static readonly PerformanceCountersSingleton performanceCountersSingleton =
            new PerformanceCountersSingleton();
        
        public static TimeSpan EnumQueriesDuration
        {
            get { return performanceCountersSingleton.enumQueriesDuration;}
            set{ performanceCountersSingleton.enumQueriesDuration = value; }
        }

        
        public static Int32 EnumQueriesCount
        {
            get { return performanceCountersSingleton.enumQueriesCount; }
            set { performanceCountersSingleton.enumQueriesCount = value; }
        }

        
        public static Hashtable UrnSkeletonsPerf
        {
            get { return performanceCountersSingleton.urnSkeletonsPerf; }
        }

        
        public static TimeSpan SqlExecutionDuration 
        {
            get { return performanceCountersSingleton.sqlExecutionDuration; }
            set { performanceCountersSingleton.sqlExecutionDuration = value; }
        }

        
        public static TimeSpan DependencyDiscoveryDuration
        {
            get { return performanceCountersSingleton.dependencyDiscoveryDuration;}
            set { performanceCountersSingleton.dependencyDiscoveryDuration = value; }
        }

        public static Int32 ObjectInfoRequestCount
        {
            get { return performanceCountersSingleton.objectInfoRequestCount; }
            set { performanceCountersSingleton.objectInfoRequestCount = value; }
        }

        
        public static Int32 InitializeCallsCount
        {
            get { return performanceCountersSingleton.initializeCallsCount; }
            set { performanceCountersSingleton.initializeCallsCount = value; }
        }

        
        public static Int32 UrnCallsCount
        {
            get { return performanceCountersSingleton.urnCallsCount; }
            set { performanceCountersSingleton.urnCallsCount = value; }
        }
        
        
        public static Int32 UrnSkelCallsCount
        {
            get { return performanceCountersSingleton.urnSkelCallsCount; }
            set { performanceCountersSingleton.urnSkelCallsCount = value; }
        }

        
        public static TimeSpan DiscoverDependenciesDuration
        {
            get { return performanceCountersSingleton.dscoverDependenciesDuration;}
            set { performanceCountersSingleton.dscoverDependenciesDuration = value; }
        }

        
        public static TimeSpan WalkDependenciesDuration 
        {
            get { return performanceCountersSingleton.walkDependenciesDuration ;}
            set { performanceCountersSingleton.walkDependenciesDuration  = value; }
        }

        public static void Dump(bool toLogFile)
        {
            foreach( string s in GetDumpStrings(true) )
            {
                if( toLogFile )
                {
                    Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, "{0}", s);
                }
                else
                {
                    Console.WriteLine(s);
                }
            }
        }

        public static void Dump( string fileName) { Dump(fileName, true);}

        public static void Dump( string fileName, bool header )
        {
            Stream fs;
            if(File.Exists(fileName))
            {
                fs = File.Open(fileName, FileMode.Truncate);
            }
            else
            {
                fs = File.Create(fileName);
            }
            
            StreamWriter sw = new StreamWriter(fs);

            foreach( string s in GetDumpStrings(header) )
            {
                sw.WriteLine( s );
            }
            
            sw.Close();
        }

        private static StringCollection GetDumpStrings(bool header)
        {
            StringCollection output = new StringCollection();

            if( header)
            {
                output.Add(string.Format(SmoApplication.DefaultCulture, "EnumQueriesDuration={0}", EnumQueriesDuration));
                output.Add(string.Format(SmoApplication.DefaultCulture, "DependencyDiscoveryDuration={0}", DependencyDiscoveryDuration));
                output.Add(string.Format(SmoApplication.DefaultCulture, "EnumQueriesCount={0}", EnumQueriesCount));
                output.Add(string.Format(SmoApplication.DefaultCulture, "ObjectInfoRequestCount={0}", ObjectInfoRequestCount));
                output.Add( string.Format(SmoApplication.DefaultCulture, "SqlExecutionDuration={0}", SqlExecutionDuration));
                output.Add(string.Format(SmoApplication.DefaultCulture, "InitializeCallsCount={0}", InitializeCallsCount));
                output.Add(string.Format(SmoApplication.DefaultCulture, "UrnCallsCount={0}", UrnCallsCount));
                output.Add(string.Format(SmoApplication.DefaultCulture, "UrnSkelCallsCount={0}", UrnSkelCallsCount));
                output.Add(string.Format(SmoApplication.DefaultCulture, "DiscoverDependenciesDuration={0}", DiscoverDependenciesDuration));
                output.Add(string.Format(SmoApplication.DefaultCulture, "WalkDependenciesDuration={0}", WalkDependenciesDuration));
                
                output.Add("\nStatistic of enumerator requests");
            }
            
            output.Add("TotalDuration\tRequestCount\tUrn+Fields");
            foreach( DictionaryEntry de in UrnSkeletonsPerf )
            {
                // duration in milliseconds
                output.Add(string.Format(SmoApplication.DefaultCulture, "{0}\t{1}\t{2}",
                                        ((FrequencyPair)de.Value).Duration.Ticks /10000,
                                        ((FrequencyPair)de.Value).Count, 
                                        de.Key.ToString()));
            }

            return output;
        }

        public static void Reset()
        {
            performanceCountersSingleton.enumQueriesDuration = new TimeSpan(0);
            performanceCountersSingleton.enumQueriesCount = 0;
            performanceCountersSingleton.urnSkeletonsPerf = new Hashtable();
            performanceCountersSingleton.sqlExecutionDuration = new TimeSpan(0);
            performanceCountersSingleton.objectInfoRequestCount = 0;
            performanceCountersSingleton.initializeCallsCount = 0;
            performanceCountersSingleton.urnCallsCount = 0;
            performanceCountersSingleton.urnSkelCallsCount = 0;
        }
    }

    public sealed class FrequencyPair
    {
        Int32 count = 0;
        public Int32 Count 
        { 
            get { return count; } 
            set { count=value;}
        }
        
        TimeSpan duration = new TimeSpan(0);
        public TimeSpan Duration 
        {
            get { return duration; }
            set { duration = value; }
        }
    }
#endif	// INCLUDE_PERF_COUNT

}
