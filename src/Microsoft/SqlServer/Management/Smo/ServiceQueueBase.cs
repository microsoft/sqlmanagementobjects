// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Broker
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.Broker.BrokerLocalizableResources",true)]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    public partial class ServiceQueue : ScriptSchemaObjectBase, IExtendedProperties, Cmn.ICreatable,
        Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
    {
        internal ServiceQueue(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "ServiceQueue";
            }
        }

        /// <summary>
        /// Schema of ServiceQueue
        /// </summary>
        [SfcKey(0)]
        [SfcReference(typeof(Schema), typeof(SchemaCustomResolver), "Resolve")]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        [CLSCompliant(false)]
        public override string Schema
        {
            get
            {
                return base.Schema;
            }
            set
            {
                base.Schema = value;
            }
        }

        /// <summary>
        /// Name of ServiceQueue
        /// </summary>
        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            // retrieve the DDL 
            GetDDL(queries, sp, true );
        }

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

        private void GetDDL(StringCollection queries, ScriptingPreferences sp, bool bCreate)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            /*
                CREATE QUEUE [db.][schema.]queue-name
                        [ WITH 
                        [ STATUS = { ON | OFF }]
                        [ RETENTION = { ON | OFF } ]  
                        [ ACTIVATION (
                            [ STATUS = { ON | OFF } , ]
                            PROCEDURE_NAME = ProcName ,
                            MAX_QUEUE_READERS = maxreaders, 
                            EXECUTE AS { SELF | �<username>�} ) 
                            [ POISON_MESSAGE_HANDLING ([ STATUS = {ON | OFF} ] )
                            ]
                        ]
                        [ ON { file_group | [DEFAULT] } ]

                ALTER QUEUE [db.][schema.]queue-name 
                        WITH 
                        [ STATUS = { ON | OFF }]
                        [ RETENTION = { ON | OFF } ]  
                        [ ACTIVATION (
                            {
                            [ STATUS = { ON | OFF } [, ] ]
                            [ PROCEDURE_NAME = ProcName [ , ] ]
                            [ MAX_QUEUE_READERS = maxreaders [ , ] ] 
                            [ EXECUTE AS { SELF | �<username>� } ] 
                            |
                            DROP 
                            } 
                        ) 
                        [ POISON_MESSAGE_HANDLING ([ STATUS = {ON | OFF} ] )
                ]
            */

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            // retrieve full scripting name
            string sFullScriptingName = FormatFullNameForScripting( sp );

            if (bCreate && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SERVICE_QUEUE, "NOT", FormatFullNameForScripting( sp, false ));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,  "{0} QUEUE {1} ",  bCreate?"CREATE" : "ALTER", sFullScriptingName);

            bool bWithAdded = false;

            object p = GetPropValueOptional("IsEnqueueEnabled");
            if( null != p )
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "WITH STATUS = {0} ", (bool)p ? "ON" : "OFF");
                bWithAdded  = true;
            }
            
            p = GetPropValueOptional("IsRetentionEnabled");
            if( null != p )
            {
                if (!bWithAdded)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "WITH ");
                    bWithAdded = true;
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", ");
                }

                sb.AppendFormat(SmoApplication.DefaultCulture,  "RETENTION = {0} ", (bool)p ? "ON" : "OFF");
            }

            
            bool   bProcedureNameSet = false;
            StringBuilder fullProcName = null;
            
            string procedureName = (string)this.GetPropValueOptional("ProcedureName");

            if( null != procedureName && procedureName.Length > 0 )
            {
                bProcedureNameSet = true;
                fullProcName = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                string s = (string)this.GetPropValueOptional("ProcedureDatabase");
                if( null != s && s.Length > 0 )
                {
                    fullProcName.Append(MakeSqlBraket(s));
                    fullProcName.Append(Globals.Dot);
                }

                s = (string)this.GetPropValueOptional("ProcedureSchema");
                if( null != s && s.Length > 0 )
                {
                    fullProcName.Append(MakeSqlBraket(s));
                    fullProcName.Append(Globals.Dot);
                }
                fullProcName.Append(MakeSqlBraket(procedureName));

                procedureName = fullProcName.ToString();

            }
            bool bInScriptMode = sp.ForDirectExecution == false;

            p = GetPropValueOptional("IsActivationEnabled");
            if( (!bInScriptMode && null != p) ||
                (bInScriptMode  && bProcedureNameSet && procedureName != String.Empty) )
            {

                if (!bWithAdded)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "WITH ");
                    bWithAdded = true;
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", ");
                }

                sb.AppendFormat(SmoApplication.DefaultCulture,  "ACTIVATION ( " );

                
                if( !bCreate && bProcedureNameSet && procedureName == String.Empty)
                {
                    //Need to drop the activation (only allowed on alter
                    sb.AppendFormat(SmoApplication.DefaultCulture,  " DROP " );
                }
                else
                {

                    p = GetPropValue("IsActivationEnabled");
                    bool bIsActivationEnabled = (bool)p;
                    sb.AppendFormat(SmoApplication.DefaultCulture, " STATUS = {0} ", bIsActivationEnabled?"ON" : "OFF");
    
                    if( null != procedureName && procedureName.Length > 0 )
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, ", PROCEDURE_NAME = {0} ", procedureName);
                    }
    
                    p = GetPropValueOptional("MaxReaders");
                    if( null != p )
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, ", MAX_QUEUE_READERS = {0} ", ((System.Int16)p).ToString(SmoApplication.DefaultCulture));

                    }
                    
                    p = GetPropValueOptional("ActivationExecutionContext");
                    if (null != p)
                    {
                        string s = "SELF";

                        ActivationExecutionContext ac = (ActivationExecutionContext)p;
    
                        switch (ac)
                        {
                            case ActivationExecutionContext.Owner: s = "OWNER"; break;
    
                            case ActivationExecutionContext.ExecuteAsUser:
                                {
                                    s = (string)GetPropValueOptional("ExecutionContextPrincipal");
                                    if (null != s && s.Length > 0)
                                    {
                                        s = MakeSqlString(s);
                                    }
                                    else
                                    {
                                        throw new WrongPropertyValueException(ExceptionTemplates.ExecutionContextPrincipalIsNotSpecified);
                                    }
    
                                    break;
                                }
    
                            case ActivationExecutionContext.Self: /*s = "SELF"; //defailt value*/ break;
                        }
                        sb.AppendFormat(SmoApplication.DefaultCulture, ", EXECUTE AS {0} ", s);
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, ", EXECUTE AS SELF ");
                    }
                }

                sb.AppendFormat(SmoApplication.DefaultCulture,  " )" );
            }

            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version105 &&
               (this.ServerVersion.Major > 10 || (this.ServerVersion.Major >= 10 && this.ServerVersion.Minor >= 50)))
            {
                object isPoisonMessageHandlingEnabled = GetPropValueOptional("IsPoisonMessageHandlingEnabled");
                if (null != isPoisonMessageHandlingEnabled)
                {
                    if (!bWithAdded)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "WITH ");
                        bWithAdded = true;
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, ", ");
                    }

                    sb.AppendFormat(SmoApplication.DefaultCulture, "POISON_MESSAGE_HANDLING (STATUS = {0}) ", (bool)isPoisonMessageHandlingEnabled ? "ON" : "OFF");
                }
            }

            if (bCreate) 
            {
                string fileGroup = (string)GetPropValueOptional("FileGroup");
                if( null != fileGroup && fileGroup.Length > 0 )
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " ON [{0}] ", SqlBraket(fileGroup));
                }
            }

            // add the ddl to create the object
            queries.Add( sb.ToString() );
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, 
                    Scripts.INCLUDE_EXISTS_SERVICE_QUEUE, 
                    "", 
                    FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP QUEUE {0}", FormatFullNameForScripting(sp));
            
            queries.Add(sb.ToString());
        }

        public void Reorganize()
        {
            ThrowIfBelowVersion130();
            CheckObjectState(true);
            ReorganizeImpl(null);
        }

        public void Reorganize(bool lobCompaction)
        {
            ThrowIfBelowVersion130();
            CheckObjectState(true);
            ReorganizeImpl(lobCompaction);
        }

        private void ReorganizeImpl(bool? lobCompaction)
        {
            try
            {
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));

                StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER QUEUE {0} REORGANIZE", this.FullQualifiedName);

                if (lobCompaction.HasValue)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, " WITH (LOB_COMPACTION = {0})", lobCompaction.Value ? "ON" : "OFF");
                }

                queries.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.Reorganize, this, e);
            }
        }

        public void Rebuild()
        {
            ThrowIfBelowVersion130();
            CheckObjectState(true);
            RebuildImpl(null);
        }

        public void Rebuild(int maxDop)
        {
            ThrowIfBelowVersion130();
            CheckObjectState(true);
            RebuildImpl(maxDop);
        }

        private void RebuildImpl(int? maxDop)
        {
            try
            {
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));

                StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER QUEUE {0} REBUILD", this.FullQualifiedName);

                if (maxDop.HasValue)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, " WITH (MAXDOP = {0})", maxDop.Value);
                }

                queries.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.Rebuild, this, e);
            }
        }

        public void MoveTo(string fileGroup)
        {
            ThrowIfBelowVersion130();
            CheckObjectState(true);
            MoveToImpl(fileGroup, null);
        }

        public void MoveTo(string fileGroup, int maxDop)
        {
            ThrowIfBelowVersion130();
            CheckObjectState(true);
            MoveToImpl(fileGroup, maxDop);
        }

        private void MoveToImpl(string fileGroup, int? maxDop)
        {
            try
            {
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));

                StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER QUEUE {0} MOVE TO [{1}]", this.FullQualifiedName, SqlBraket(fileGroup));

                if (maxDop.HasValue)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, " WITH (MAXDOP = {0})", maxDop.Value);
                }

                queries.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.Rebuild, this, e);
            }
        }


        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            if( IsObjectDirty() )
            {
                GetDDL( queries, sp, false );
            }
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public double RowCountAsDouble
        {
            get 
            {
                return Convert.ToDouble(Properties["RowCount"].Value, SmoApplication.DefaultCulture);
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get 
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if( null == m_ExtendedProperties )
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

    }

    
}


