// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class ExternalStreamingJob : NamedSmoObject, ICreatable, IDroppable, IScriptable
    {
        // This is need because External Stream is a collection of Database Object.
        //
        internal ExternalStreamingJob(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        private bool HasStatementOption()
        {
            return !String.IsNullOrEmpty(GetPropertyOptional(nameof(Statement)).Value as string);
        }

        private string GetRequiredName()
        {
            // sysname is an nvarchar
            //
            return MakeSqlString(this.Name);
        }

        private bool HasStatusOption()
        {
            return !String.IsNullOrEmpty(GetPropertyOptional(nameof(Status)).Value as string);
        }

        // If the object is scriptable on its own, not directly included as part of the script for its parent,
        // its UrnSuffix is referenced in the scriptableTypes HashSet in ScriptMaker.cs
        //
        public static string UrnSuffix
        {
            get
            {
                return nameof(ExternalStreamingJob);
            }
        }

        //  Since the object is a collection and has a custom
        //  set of necessary fields for population
        //  It is referenced in the GetFieldNames in SmoCollectionBase.cs
        //
        public static StringCollection RequiredFields
        {
            get 
            {
                StringCollection col = new StringCollection();
                col.Add(nameof(Name));
                return col;
            }
        }

        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            if (!sp.ContinueOnScriptingError && sp.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlDatabaseEdge)
            {
                throw new UnsupportedEngineEditionException(ExceptionTemplates.UnsupportedEngineEditionException);
            }
            if (DatabaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                string statement= this.HasStatementOption() ? $"{ MakeSqlString(GetPropertyOptional(nameof(Statement)).Value as string) }" : string.Empty;
                
                if (this.HasStatementOption())
                {
                    sb.Append($"EXEC sys.sp_create_streaming_job @name={GetRequiredName()}, @statement={statement}");
                    query.Add(sb.ToString());
                }
                else
                {
                    if (!sp.ContinueOnScriptingError)
                    {
                        throw new PropertyNotSetException(nameof(Statement));
                    }
                }
            }
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            if (!sp.ContinueOnScriptingError && sp.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlDatabaseEdge)
            {
                throw new UnsupportedEngineEditionException(ExceptionTemplates.UnsupportedEngineEditionException);
            }
            if (DatabaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.Append($"EXEC sys.sp_drop_streaming_job @name={GetRequiredName()}");
                dropQuery.Add(sb.ToString());
            }
            else 
            {
                if (!sp.ContinueOnScriptingError)
                {
                    throw new UnsupportedEngineEditionException(ExceptionTemplates.UnsupportedEngineEditionException);
                }
            }
        }

        public void StartStreamingJob()
        {
            if (DatabaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
            {
                StringCollection startStreamingJobQuery = new StringCollection();
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.Append($"EXEC sys.sp_start_streaming_job @name={GetRequiredName()}");
                startStreamingJobQuery.Add(sb.ToString());
                ExecuteNonQuery(startStreamingJobQuery, true, false);
            }
            else
            {
                throw new UnsupportedEngineEditionException(ExceptionTemplates.UnsupportedEngineEditionException);
            }
        }

        public void StopStreamingJob()
        {
            if (DatabaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
            {
                StringCollection stopStreamingJobQuery = new StringCollection();
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                
                sb.Append($"EXEC sys.sp_stop_streaming_job @name={GetRequiredName()}");
                stopStreamingJobQuery.Add(sb.ToString());
                ExecuteNonQuery(stopStreamingJobQuery, true, false);
            }
            else
            {
                throw new UnsupportedEngineEditionException(ExceptionTemplates.UnsupportedEngineEditionException);
            }
        }

        public void Create()
        {
           this.CreateImpl();
        }

        public void Drop()
        {
            this.DropImpl();
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }
    }
}
