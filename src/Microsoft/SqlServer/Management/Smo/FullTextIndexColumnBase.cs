// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class FullTextIndexColumn : ScriptNameObjectBase,Cmn.IAlterable, Cmn.ICreatable, Cmn.IDroppable, IScriptable
    {
        internal FullTextIndexColumn(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "FullTextIndexColumn";
            }
        }

        /// <summary>
        /// Name of FullTextIndexColumn
        /// </summary>
        [SfcKey(0)]
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


        public void Alter()
        {
            base.AlterImpl();
        }

        public void Alter(bool noPopulation)
        {
            try
            {
                this.noPopulation = noPopulation;
                base.AlterImpl();
            }
            finally
            {
                noPopulation = false;
            }
        }

        internal bool noPopulation = false;

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ScriptAlterFullTextIndexColumn(alterQuery, sp);
        }

        internal void ScriptAlterFullTextIndexColumn(StringCollection queries, ScriptingPreferences sp)
        {
            //  ALTER COLUMN syntax is only available in Denali or later
            if (sp.TargetServerVersion >= SqlServerVersion.Version110)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                FullTextIndex ftIndex = (FullTextIndex)ParentColl.ParentInstance;
                TableViewBase table = (TableViewBase)ftIndex.Parent;

                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER FULLTEXT INDEX ON {0} ALTER COLUMN ", table.FormatFullNameForScripting(sp));

                sb.AppendFormat(SmoApplication.DefaultCulture, "{0} ", FormatFullNameForScripting(sp));

                //TODO: Come back here and verify the logic for DROP STATISTICAL SEMANTICS when we get to a
                //      point where we can see what that looks like...

                //  If we're calling ALTER, then we must need to do whatever the property indicates is the
                //      current state...
                Property propSemantic = Properties.Get("StatisticalSemantics");
                int semantics = 0;
                if (propSemantic.Value != null)
                {
                    semantics = (int)propSemantic.Value;
                }

                if (semantics > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " ADD STATISTICAL_SEMANTICS");
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " DROP STATISTICAL_SEMANTICS");
                }
            }
        }


        public void Create()
        {
            base.CreateImpl();
        }

        public void Create(bool noPopulation)
        {
            try
            {
                this.noPopulation = noPopulation;
                base.CreateImpl();
            }
            finally
            {
                noPopulation = false;
            }
        }


        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ScriptCreateFullTextIndexColumn(queries, sp);
        }

        internal void ScriptCreateFullTextIndexColumn(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            FullTextIndex ftIndex = (FullTextIndex)ParentColl.ParentInstance;
            TableViewBase table = (TableViewBase)ftIndex.Parent;

            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                // ALTER FULLTEXT INDEX ON <tablename>
                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER FULLTEXT INDEX ON {0} ADD (", table.FormatFullNameForScripting(sp));

                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", FormatFullNameForScripting(sp));

                // TYPE COLUMN <typecolname>
                Property propType = Properties.Get("TypeColumnName");
                if (propType.Value != null)
                {
                    string typeColumn = (string)propType.Value;
                    if (typeColumn.Length > 0)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " TYPE COLUMN [{0}]", SqlBraket(typeColumn));
                    }
                }

                // LANGUAGE <language_string> is only supported on Shiloh or bigger
                if (this.ServerVersion.Major >= 8) //scripting for server bigger than 7
                {
                    Property propLan = Properties.Get("Language");
                    if (propLan.Value != null)
                    {
                        string language = (string)propLan.Value;
                        if (language.Length > 0)
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, " LANGUAGE [{0}]", SqlBraket(language));
                        }
                    }
                }

                // STATISTICAL SEMANTICS is only supported on Denali or higher
                if (this.ServerVersion.Major >= 11) //scripting for server bigger than 11
                {
                    Property propSemantic = Properties.Get("StatisticalSemantics");
                    if (propSemantic.Value != null)
                    {
                        int semantics = (int)propSemantic.Value;
                        if (semantics > 0)  // Statistical_Semantics is either ON (1) or absent (0)
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, " STATISTICAL_SEMANTICS");
                        }
                    }
                }

                sb.Append(")");

                if (this.noPopulation || ftIndex.noPopulation)
                {
                    sb.Append("WITH NO POPULATION");
                }

                queries.Add(sb.ToString());
            }

            // Target version < 9
            else
            {
                string language = String.Empty;
                // LANGUAGE <language_string> is only supported on Shiloh or bigger
                if (this.ServerVersion.Major >= 8 && //current server bigger than 7 and
                    sp.TargetServerVersion >= SqlServerVersion.Version80) //scripting for server bigger than 7
                {
                    Property propLan = Properties.Get("Language");
                    if (propLan.Value != null)
                    {
                        language = (string)propLan.Value;
                        if (0 < language.Length)
                        {
                            sb.Append("declare @lcid int ");
                            sb.AppendFormat(SmoApplication.DefaultCulture, "select @lcid=lcid from master.dbo.syslanguages where alias=N'{0}' ", SqlString(language));
                        }
                    }
                }


                // Add column
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_fulltext_column @tabname=N'{0}', @colname={1}, @action=N'add'",
                    SqlString(table.FormatFullNameForScripting(sp)),
                    FormatFullNameForScripting(sp, false));

                Property propType = Properties.Get("TypeColumnName");
                if (propType.Value != null)
                {
                    string typeColumn = (string)propType.Value;
                    if (typeColumn.Length > 0)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, ", @type_colname=N'{0}'",
                        SqlString(typeColumn));
                    }
                }

                if (this.ServerVersion.Major >= 8 && sp.TargetServerVersion >= SqlServerVersion.Version80
                    && language.Length > 0)
                {
                    sb.Append(", @language=@lcid");
                }

                queries.Add(sb.ToString());
            }
        }

        public void Drop()
        {
            base.DropImpl();
        }

        public void Drop(bool noPopulation)
        {
            try
            {
                this.noPopulation = noPopulation;
                base.DropImpl();
            }
            finally
            {
                noPopulation = false;
            }
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            FullTextIndex ftIndex = (FullTextIndex)ParentColl.ParentInstance;
            TableViewBase table = (TableViewBase)ftIndex.Parent;

            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER FULLTEXT INDEX ON {0} DROP ({1}) ",
                                table.FormatFullNameForScripting(sp),
                                FormatFullNameForScripting(sp));
                if (this.noPopulation || ftIndex.noPopulation)
                {
                    sb.Append("WITH NO POPULATION");
                }

                dropQuery.Add(sb.ToString());
            }

            // Target version < 9
            else
            {
                // Drop column
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_fulltext_column @tabname=N'{0}', @colname={1}, @action=N'drop'",
                    table.FormatFullNameForScripting(sp),
                    FormatFullNameForScripting(sp, false));

                dropQuery.Add(sb.ToString());
            }
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting options
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server.
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Microsoft.SqlServer.Management.Common.DatabaseEngineType databaseEngineType,
                                    Microsoft.SqlServer.Management.Common.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            string[] fields = {
                        "TypeColumnName",
                        "Language",
                        "StatisticalSemantics"};
            List<string> list = GetSupportedScriptFields(typeof(FullTextIndexColumn.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}


