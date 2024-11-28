// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public class DefaultRuleBase : ScriptSchemaObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IAlterable, IExtendedProperties, IScriptable, ITextObject
    {
        internal DefaultRuleBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        protected internal DefaultRuleBase() : base() { }

        /// <summary>
        /// Binds the default or rule to a column.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="colname"></param>
        /// <param name="tableschema"></param>
        public void BindToColumn(string tablename, string colname, string tableschema)
        {
            try
            {
                if (null == tablename)
                {
                    throw new ArgumentNullException("tablename");
                }

                if (null == colname)
                {
                    throw new ArgumentNullException("colname");
                }

                if (null == tableschema)
                {
                    throw new ArgumentNullException("tableschema");
                }

                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                ScriptingPreferences sp = new ScriptingPreferences();

                if (this.Schema != null)
                {
                    sp.IncludeScripts.SchemaQualify = true;
                }
                else
                {
                    sp.IncludeScripts.SchemaQualify = false;
                }

                string fullColName = string.Empty;
                if (tableschema.Length == 0)
                {
                    fullColName = string.Format(SmoApplication.DefaultCulture, "N'[{0}].[{1}]'",
                                SqlStringBraket(tablename),
                                SqlStringBraket(colname));
                }
                else
                {
                    fullColName = string.Format(SmoApplication.DefaultCulture, "N'[{0}].[{1}].[{2}]'",
                                SqlStringBraket(tableschema),
                                SqlStringBraket(tablename),
                                SqlStringBraket(colname));
                }

                // we take different action if the object is a rule or a default
                if (this is Rule)
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC dbo.sp_bindrule @rulename=N'{0}', @objname={1}",
                                SqlString(FormatFullNameForScripting(sp)),
                                fullColName));
                }
                else
                {
                    // FormatFullNameForScripting creates a bracketed name which is not escaped for strings
                    queries.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC dbo.sp_bindefault @defname=N'{0}', @objname={1}",
                                SqlString(FormatFullNameForScripting(sp)),
                                fullColName));
                }

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Bind, this, e);
            }
        }

        /// <summary>
        /// Binds the default or rule to a column.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="colname"></param>
        public void BindToColumn(string tablename, string colname)
        {
            BindToColumn(tablename, colname, string.Empty);
        }

        /// <summary>
        /// This method enables a default or rule on the user-defined data type specified.
        /// </summary>
        /// <param name="datatypename"></param>
        /// <param name="bindcolumns"></param>
        public void BindToDataType(string datatypename, bool bindcolumns)
        {
            try
            {
                if (null == datatypename)
                {
                    throw new ArgumentNullException("datatypename");
                }

                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                ScriptingPreferences sp = new ScriptingPreferences();
                sp.IncludeScripts.SchemaQualify = true;

                if (this.Schema != null)
                {
                    sp.IncludeScripts.SchemaQualify = true;
                }
                else
                {
                    sp.IncludeScripts.SchemaQualify = false;
                }

                // we take different action if the object is a rule or a default
                if (this is Rule)
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC dbo.sp_bindrule N'{0}', N'[{1}]' {2}",
                                SqlString(FormatFullNameForScripting(sp)),
                                SqlStringBraket(datatypename),
                                bindcolumns ? ", futureonly" : ""));
                }
                else
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC dbo.sp_bindefault N'{0}', N'[{1}]' {2}",
                                SqlString(FormatFullNameForScripting(sp)),
                                SqlStringBraket(datatypename),
                                bindcolumns ? ", futureonly" : ""));
                }

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Bind, this, e);
            }
        }

        /// <summary>
        /// This method breaks the binding between a default or rule and the column of a table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="colname"></param>
        public void UnbindFromColumn(string tablename, string colname)
        {
            UnbindFromColumn(tablename, colname, String.Empty);
        }

        /// <summary>
        /// This method breaks the binding between a default or rule and the column of a table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="colname"></param>
        /// <param name="tableschema"></param>
        public void UnbindFromColumn(string tablename, string colname, string tableschema)
        {
            try
            {
                if (null == tablename)
                {
                    throw new ArgumentNullException("tablename");
                }

                if (null == colname)
                {
                    throw new ArgumentNullException("colname");
                }

                if (null == tableschema)
                {
                    throw new ArgumentNullException("tableschema");
                }

                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));

                string fullColName = string.Empty;
                if (tableschema.Length == 0)
                {
                    fullColName = string.Format(SmoApplication.DefaultCulture, "N'[{0}].[{1}]'",
                                SqlStringBraket(tablename),
                                SqlStringBraket(colname));
                }
                else
                {
                    fullColName = string.Format(SmoApplication.DefaultCulture, "N'[{0}].[{1}].[{2}]'",
                                SqlStringBraket(tableschema),
                                SqlStringBraket(tablename),
                                SqlStringBraket(colname));
                }

                // we take different action if the object is a rule or a default
                if (this is Rule)
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC dbo.sp_unbindrule @objname={0}", fullColName));
                }
                else
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC dbo.sp_unbindefault @objname={0}", fullColName));
                }

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Unbind, this, e);
            }
        }

        /// <summary>
        /// This method breaks the binding between a default or rule and a user-defined data type.
        /// </summary>
        /// <param name="datatypename"></param>
        /// <param name="bindcolumns"></param>
        public void UnbindFromDataType(string datatypename, bool bindcolumns)
        {
            try
            {
                if (null == datatypename)
                {
                    throw new ArgumentNullException("datatypename");
                }

                CheckObjectState();
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                // we take different action if the object is a rule or a default
                if (this is Rule)
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture, "EXEC sp_unbindrule N'[{0}]' {1}",
                                SqlStringBraket(datatypename), bindcolumns ? ", futureonly" : ""));
                }
                else
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture, "EXEC sp_unbindefault N'[{0}]' {1}",
                                SqlStringBraket(datatypename), bindcolumns ? ", futureonly" : ""));
                }

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Unbind, this, e);
            }
        }

        /// <summary>
        /// Enumerates columns bound to this default or rule.
        /// </summary>
        /// <returns></returns>
        public SqlSmoObject[] EnumBoundColumns()
        {
            try
            {
                CheckObjectState();
                // make the request Urn
                DataTable tbl = this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/Column"));
                // allocate result
                SqlSmoObject[] results = new SqlSmoObject[tbl.Rows.Count];
                Urn thisurn = this.Urn;
                int idx = 0;
                // get all objects
                foreach (DataRow dr in tbl.Rows)
                {
                    string objecturn = string.Format(SmoApplication.DefaultCulture, "{0}/Table[@Name='{1}' and @Schema='{2}']/Column[@Name='{3}']",
                                                thisurn.Parent, Urn.EscapeString((string)dr["TableName"]),
                                                Urn.EscapeString((string)dr["TableSchema"]), Urn.EscapeString((string)dr["Name"]));
                    results[idx++] = GetServerObject().GetSmoObject(objecturn);
                }

                return results;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumBoundColumns, this, e);
            }
        }

        /// <summary>
        /// Enumerates data types bound to this default or rule.
        /// </summary>
        /// <returns></returns>
        public SqlSmoObject[] EnumBoundDataTypes()
        {
            try
            {
                CheckObjectState();
                // make the request Urn
                DataTable tbl = this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/DataType"));
                // allocate result
                SqlSmoObject[] results = new SqlSmoObject[tbl.Rows.Count];
                Urn thisurn = this.Urn;
                int idx = 0;
                // get all objects
                foreach (DataRow dr in tbl.Rows)
                {
                    string objecturn = string.Format(SmoApplication.DefaultCulture, "{0}/UserDefinedDataType[@Name='{1}' and @Schema='{2}']",
                                                thisurn.Parent, Urn.EscapeString((string)dr["Name"]), Urn.EscapeString((string)dr["Schema"]));
                    results[idx++] = GetServerObject().GetSmoObject(objecturn);
                }

                return results;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumBoundDataTypes, this, e);
            }
        }


        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // format full rule name for scripting
            string sFullScriptingName = FormatFullNameForScripting(sp);

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                ScriptIncludeHeaders(sb, sp, (this is Default) ? "Default" : "Rule");

                if (sp.IncludeScripts.ExistenceCheck)
                {

                    sb.AppendFormat(SmoApplication.DefaultCulture,
                        (sp.TargetServerVersion == SqlServerVersion.Version80)
                            ? Scripts.INCLUDE_EXISTS_RULE_DEFAULT80
                            : Scripts.INCLUDE_EXISTS_RULE_DEFAULT90,
                        "NOT", SqlString(sFullScriptingName), this is Default ? "IsDefault" : "IsRule");
                    sb.Append(sp.NewLine);
                }
            }

            StringBuilder stmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            //doesn't support sp.IncludeScripts.ExistenceCheck
            if ((false == this.TextMode) || (true == sp.OldOptions.EnforceScriptingPreferences))
            {
                if (!sp.OldOptions.DdlBodyOnly)
                {
                    stmt.AppendFormat(SmoApplication.DefaultCulture, "CREATE {0} {1} ", (this is Default) ? "DEFAULT" : "RULE", sFullScriptingName);
                    stmt.Append(sp.NewLine);

                    stmt.Append("AS");
                }


                if (!sp.OldOptions.DdlHeaderOnly)
                {
                    if (!sp.OldOptions.DdlBodyOnly)
                    {
                        stmt.Append(sp.NewLine);
                    }
                    stmt.Append(GetTextBody(true));
                }
            }
            else
            {
                stmt.Append(GetTextForScript(sp, expectedObjectTypes: null, forceCheckNameAndManipulateIfRequired: false, scriptHeaderType: ScriptHeaderType.ScriptHeaderForCreate));
            }

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat("EXEC dbo.sp_executesql N'{0}'", SqlString(stmt.ToString()));
            }
            else
            {
                sb.Append(stmt.ToString());
            }


            queries.Add(sb.ToString());
        }


        /// <summary>
        /// Drops the object.
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

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            string sFullScriptingName = FormatFullNameForScripting(sp);

            ScriptIncludeHeaders(sb, sp, (this is Default) ? "Default" : "Rule");

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion < SqlServerVersion.Version130)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                        (sp.TargetServerVersion == SqlServerVersion.Version80)
                            ? Scripts.INCLUDE_EXISTS_RULE_DEFAULT80
                            : Scripts.INCLUDE_EXISTS_RULE_DEFAULT90,
                    "", SqlString(sFullScriptingName), this is Default ? "IsDefault" : "IsRule");
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP {0} {1}{2}",
                                    (this is Default) ? "DEFAULT" : "RULE",
                                    (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty,
                                    sFullScriptingName);
            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            ThrowIfTextIsDirtyForAlter();
        }

        protected override void PostCreate()
        {
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        /// <summary>
        /// Scripts the object.
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Scripts object with specific scripting optiions.
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        #region TextModeImpl

        public string ScriptHeader(bool forAlter)
        {
            CheckObjectState();
            return GetTextHeader(forAlter);
        }

        public string ScriptHeader(ScriptHeaderType scriptHeaderType)
        {
            if (ScriptHeaderType.ScriptHeaderForCreateOrAlter == scriptHeaderType)
            {
                throw new NotSupportedException(
                    ExceptionTemplates.CreateOrAlterNotSupported(this.GetType().Name));
            }
            else
            {
                return GetTextHeader(scriptHeaderType);
            }
        }

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public string TextBody
        {
            get { CheckObjectState(); return GetTextBody(); }
            set { CheckObjectState(); SetTextBody(value); }
        }

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public string TextHeader
    {
        get { CheckObjectState(); return GetTextHeader(false); }
        set { CheckObjectState(); SetTextHeader(value); }
    }

        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public bool TextMode
        {
            get { CheckObjectState(); return GetTextMode(); }
            set { CheckObjectState(); SetTextMode(value, null); }
        }

        #endregion
    }
}


