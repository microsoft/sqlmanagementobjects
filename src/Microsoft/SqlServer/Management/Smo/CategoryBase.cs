// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public class CategoryBase : AgentObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, IScriptable
    {
        internal CategoryBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        protected internal CategoryBase() : base() { }

        public void Create()
        {
            base.CreateImpl();
        }		

        internal virtual string GetCategoryClassName()
        {
            return string.Empty;
        }

        internal virtual int GetCategoryClass()
        {
            return 0;
        }
        
        internal virtual string GetCategoryTypeName()
        {
            return GetCatTypeName(CategoryType.None);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string urnSuffix = this.GetType().InvokeMember("UrnSuffix", UrnSuffixBindingFlags,
                null, null, new object[] { }, SmoApplication.DefaultCulture) as string;

            ScriptIncludeHeaders(createQuery, sp, urnSuffix);


            if (sp.IncludeScripts.ExistenceCheck)
            {
                createQuery.AppendFormat(Scripts.INCLUDE_EXISTS_AGENT_CATEGORY, "NOT", SqlString(this.Name), GetCategoryClass());
                createQuery.Append(Globals.newline);
                createQuery.Append("BEGIN");
                createQuery.Append(Globals.newline);
            }
            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                                    "EXEC {0}msdb.dbo.sp_add_category @class=N'{1}', @type=N'{2}', @name=N'{3}'",
                                    Job.GetReturnCode(sp),
                                    GetCategoryClassName(),
                                    GetCategoryTypeName(),
                                    SqlString(this.Name));
            if (sp.Agent.InScriptJob)
            {
                createQuery.Append(Globals.newline);
                Job.AddCheckErrorCode(createQuery);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                createQuery.Append(Globals.newline);
                createQuery.Append("END");
                createQuery.Append(Globals.newline);
            }
            queries.Add(createQuery.ToString());
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

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string urnSuffix = this.GetType().InvokeMember("UrnSuffix",
                UrnSuffixBindingFlags,
                null, null, new object[] { }, SmoApplication.DefaultCulture) as string;

            ScriptIncludeHeaders(sb, sp, urnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(Scripts.INCLUDE_EXISTS_AGENT_CATEGORY, "", SqlString(this.Name), GetCategoryClass());
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC msdb.dbo.sp_delete_category @class=N'{0}', @name=N'{1}'",
                            GetCategoryClassName(), SqlString(this.Name));

            queries.Add(sb.ToString());
        }

        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        internal override void ScriptRename(StringCollection queries, ScriptingPreferences sp, string newName)
        {
            queries.Add( string.Format(SmoApplication.DefaultCulture,  
                            "EXEC msdb.dbo.sp_update_category @class=N'{0}', @name=N'{1}', @new_name=N'{2}'",
                            GetCategoryClassName(),SqlString(this.Name), SqlString(newName) )
                        );
        }

        protected string GetCatTypeName(CategoryType ct)
        {
            switch( ct )
            {
                case CategoryType.LocalJob : 
                    return "LOCAL";
                case CategoryType.MultiServerJob :
                    return "MULTI-SERVER";
                case CategoryType.None : 
                    return "NONE";
            }

            throw new InternalSmoErrorException( ExceptionTemplates.UnknownCategoryType(ct.ToString()));
        }

        /// <summary>
        /// Generate object creation script using default scripting options
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting options
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }		
    }
}


