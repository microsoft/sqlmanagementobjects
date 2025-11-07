// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class FullTextStopList : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
    {
        //Local collection for design mode
        Dictionary<string, List<string>> stopListCollection = new Dictionary<string, List<string>>();

        //name of the existing source stoplist
        internal string srcFullTextStopListName = String.Empty;

        //name of the existing database of which the source stoplist is a part of
        internal string srcDbName = String.Empty;

        //bool to indicate whether the source stoplist is system default stoplist or not
        internal bool srcSystemDefault = false;

        internal FullTextStopList(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
        {

        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "FullTextStopList";
            }
        }

        /// <summary>
        /// Name of FullTextStopList
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

        /// <summary>
        /// Creates a new stoplist object without any source stoplist
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        /// <summary>
        /// Creates a new stoplist from an existing source stoplist in the current database
        /// </summary>
        public void CreateFromExistingStopList(string stoplistName)
        {
            this.srcFullTextStopListName = stoplistName;
            base.CreateImpl();
        }

        /// <summary>
        /// Creates a new stoplist from an existing source stoplist in the different database
        /// </summary>
        public void CreateFromExistingStopList(string dbName, string stoplistName)
        {
            this.srcDbName = dbName;
            this.srcFullTextStopListName = stoplistName;
            base.CreateImpl();
        }

        /// <summary>
        /// Creates a new stoplist from the system default stoplist
        /// </summary>
        public void CreateFromSystemStopList()
        {
            this.srcSystemDefault = true;
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            Property property;

            if (sp.TargetServerVersion >= SqlServerVersion.Version100 && ServerVersion.Major >= 10)
            {
                if (sp.IncludeScripts.Header)
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, this.Name, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_STOPLIST, "NOT", SqlString(this.Name));
                    sb.Append(sp.NewLine);
                    sb.Append(Scripts.BEGIN);
                    sb.Append(sp.NewLine);
                }

                // CREATE FULLTEXT STOPLIST <stoplist_name>
                sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE FULLTEXT STOPLIST {0}", MakeSqlBraket(this.Name));
                sb.Append(sp.NewLine);

                // FROM <source_stoplist_name>
                if ((this.srcFullTextStopListName != String.Empty || this.srcSystemDefault)
                    && this.State == SqlSmoState.Creating)
                {
                    sb.Append("FROM ");

                    if (this.srcSystemDefault)
                    {
                        sb.Append("SYSTEM STOPLIST");
                    }
                    else
                    {
                        if (this.srcDbName != String.Empty)
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, "{0}.", MakeSqlBraket(this.srcDbName));
                        }
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(this.srcFullTextStopListName));
                    }

                    sb.Append(sp.NewLine);
                }

                // AUTHORIZATION <owner_name>
                if (sp.IncludeScripts.Owner)
                {
                    property = this.Properties.Get("Owner");
                    if ((null != property.Value) && (property.Value.ToString().Length > 0))
                    {
                        sb.AppendFormat("AUTHORIZATION {0}", MakeSqlBraket(property.Value.ToString()));
                    }
                }

                sb.Append(";");
                sb.Append(sp.NewLine);

                // scripting the stopwords also when the script is generated by the Script function

                if (this.State == SqlSmoState.Existing)
                {
                    if (this.IsDesignMode)
                    {
                        foreach (KeyValuePair<string, List<string>> kvp in stopListCollection)
                        {
                            foreach (string str in kvp.Value)
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER FULLTEXT STOPLIST {0} ADD '{1}' LANGUAGE '{2}';",
                                            MakeSqlBraket(this.Name), str, kvp.Key);
                                sb.Append(sp.NewLine);
                            }
                        }

                    }
                    else
                    {
                        DataTable dt = EnumStopWords();
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (!dr.IsNull("language"))
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER FULLTEXT STOPLIST {0} ADD '{1}' LANGUAGE '{2}';", MakeSqlBraket(this.Name), SqlString(dr["stopword"].ToString()), SqlString(dr["language"].ToString()));
                            }
                            sb.Append(sp.NewLine);
                        }
                    }
                }

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.Append(Scripts.END);
                    sb.Append(sp.NewLine);
                }

                createQuery.Add(sb.ToString());
            }
            else
            {
                throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
            }
        }

        /// <summary>
        /// Drops the stoplist
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

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.TargetServerVersion >= SqlServerVersion.Version100 && ServerVersion.Major >= 10)
            {
                if (sp.IncludeScripts.Header)
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, this.Name, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_STOPLIST, String.Empty, SqlString(this.Name));
                    sb.Append(sp.NewLine);
                    sb.Append(Scripts.BEGIN);
                    sb.Append(sp.NewLine);
                }

                // DROP FULLTEXT STOPLIST <stoplist_name>
                sb.AppendFormat(SmoApplication.DefaultCulture, "DROP FULLTEXT STOPLIST {0};", MakeSqlBraket(this.Name));
                sb.Append(sp.NewLine);

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.Append(Scripts.END);
                    sb.Append(sp.NewLine);
                }

                dropQuery.Add(sb.ToString());
            }
            else
            {
                throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
            }
        }

        /// <summary>
        /// Scripts object with default scripting options
        /// </summary>
        public StringCollection Script()
        {
            return base.ScriptImpl();
        }

        /// <summary>
        /// Scripts object with specific scripting options
        /// </summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return base.ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Returns the datatable for the enumerated stopwords in the existing stoplist
        /// </summary>
        public DataTable EnumStopWords()
        {
            StringCollection query = new StringCollection();

            query.Add(String.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket((this.Parent).Name)));
            query.Add("select stopword, language from sys.fulltext_stopwords where stoplist_id=" + ((this.ID).ToString()));

            return this.ExecutionManager.ExecuteWithResults(query).Tables[0];
        }

        /// <summary>
        /// Returns true if the specified stopword is present in the stoplist with the specified full text language otherwise false
        /// </summary>
        public bool HasStopWord(string stopword, string language)
        {
            if (this.IsDesignMode)
            {
                if (!stopListCollection.ContainsKey(language))
                {
                    return false;
                }
                List<string> langList = stopListCollection[language];
                if (langList.Contains(stopword))
                {
                    return true;
                }
                return false;

            }

            DataTable dt = EnumStopWords();
            var stringComparer = NetCoreHelpers.InvariantCulture.GetStringComparer(ignoreCase: true);
            foreach (DataRow dr in dt.Rows)
            {
                if (stringComparer.Compare((string)dr["stopword"], stopword) == 0 &&
                    stringComparer.Compare((string)dr["language"], language) == 0)
                {
                    // if the stopword is present in the specified full text language
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds the specified stopword with the specified full-text language to the stoplist
        /// </summary>
        public void AddStopWord(string stopword, string language)
        {
            if (this.IsDesignMode)
            {
                if (!stopListCollection.ContainsKey(language))
                {
                    stopListCollection.Add(language, new List<string>());
                }
                List<string> langList = stopListCollection[language];
                if (!langList.Contains(stopword))
                {
                    langList.Add(stopword);
                }
                return;
            }
            StringCollection queries = new StringCollection();

            AddDatabaseContext(queries, new ScriptingPreferences(this));
            queries.Add(String.Format(SmoApplication.DefaultCulture, "ALTER FULLTEXT STOPLIST {0} ADD '{1}' LANGUAGE '{2}';", MakeSqlBraket(this.Name), SqlString(stopword), SqlString(language)));
            this.ExecutionManager.ExecuteNonQuery(queries);


        }

        /// <summary>
        /// Removes the specified stopword with the specified full-text language from the stoplist
        /// </summary>
        public void RemoveStopWord(string stopword, string language)
        {
            if (this.IsDesignMode)
            {
                if (!stopListCollection.ContainsKey(language))
                {
                    return;
                }
                List<string> langList = stopListCollection[language];
                if (langList.Contains(stopword))
                {
                    langList.Remove(stopword);
                }
                return;
            }
            StringCollection queries = new StringCollection();

            AddDatabaseContext(queries, new ScriptingPreferences(this));
            queries.Add(String.Format(SmoApplication.DefaultCulture, "ALTER FULLTEXT STOPLIST {0} DROP '{1}' LANGUAGE '{2}';", MakeSqlBraket(this.Name), SqlString(stopword), SqlString(language)));
            this.ExecutionManager.ExecuteNonQuery(queries);
        }

        /// <summary>
        // Removes all the stopwords from the stoplist
        /// </summary>
        public void RemoveAllStopWords()
        {
            if (this.IsDesignMode)
            {
                stopListCollection.Clear();
                return;
            }
            StringCollection queries = new StringCollection();

            AddDatabaseContext(queries, new ScriptingPreferences(this));
            queries.Add(String.Format(SmoApplication.DefaultCulture, "ALTER FULLTEXT STOPLIST {0} DROP ALL;", MakeSqlBraket(this.Name)));
            this.ExecutionManager.ExecuteNonQuery(queries);
        }

        /// <summary>
        // Removes all the stopwords with the specified full-text language from the stoplist
        /// </summary>
        public void RemoveAllStopWords(string language)
        {
            if (this.IsDesignMode)
            {
                if (stopListCollection.ContainsKey(language))
                {
                    stopListCollection.Remove(language);
                }
                return;
            }
            StringCollection queries = new StringCollection();

            AddDatabaseContext(queries, new ScriptingPreferences(this));
            queries.Add(String.Format(SmoApplication.DefaultCulture, "ALTER FULLTEXT STOPLIST {0} DROP ALL LANGUAGE '{1}';", MakeSqlBraket(this.Name), SqlString(language)));
            this.ExecutionManager.ExecuteNonQuery(queries);
        }
    }
}
