// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class PartitionScheme : ScriptNameObjectBase, Cmn.IDroppable, Cmn.IDropIfExists,
        Cmn.IAlterable, Cmn.ICreatable, IScriptable/*, IExtendedProperties*/
    {
        internal PartitionScheme(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "PartitionScheme";
            }
        }

        /// <summary>
        /// Name of PartitionScheme
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
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
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(statement, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_PARTITION_SCHEME, "NOT", FormatFullNameForScripting(sp, false));
                statement.AppendFormat(SmoApplication.DefaultCulture, sp.NewLine);
            }
            statement.AppendFormat(SmoApplication.DefaultCulture, "CREATE PARTITION SCHEME {0}", FormatFullNameForScripting(sp));

            if (FileGroups.Count == 0)
            {
                throw new SmoException(ExceptionTemplates.ObjectWithNoChildren(this.GetType().Name, "FileGroup"));
            }

            string pfnName = (string)GetPropValue("PartitionFunction");

            statement.AppendFormat(SmoApplication.DefaultCulture, " AS PARTITION [{0}] TO (", SqlBraket(pfnName));

            int fgCount = 0;

            foreach (string fgname in FileGroups)
            {
                if (0 < fgCount++)
                {
                    statement.Append(Globals.commaspace);
                }
                // if we don't script file groups we are using the PRIMARY filegroup
                statement.Append(MakeSqlBraket(!sp.Storage.FileGroup ? "PRIMARY" : fgname));
            }

            // Script the next used FileGroup if defined
            string nextFileGroup = this.GetPropValueIfSupportedWithThrowOnTarget("NextUsedFileGroup", String.Empty, sp);
            if (!string.IsNullOrEmpty(nextFileGroup))
            {
                statement.Append(Globals.commaspace);
                statement.Append(MakeSqlBraket(!sp.Storage.FileGroup ? "PRIMARY" : nextFileGroup));
            }

            statement.Append(")");
            queries.Add(statement.ToString());
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

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_PARTITION_SCHEME, "", FormatFullNameForScripting(sp, false));
                sb.AppendFormat(SmoApplication.DefaultCulture, sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP PARTITION SCHEME {0}", FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            Property propNextUsed = Properties.Get("NextUsedFileGroup");
            if (propNextUsed.Value != null && (propNextUsed.Dirty || sp.ScriptForCreateDrop))
            {
                ThrowIfPropertyNotSupported("NextUsedFileGroup", sp);
                string nextUsed = (string)propNextUsed.Value;
                StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER PARTITION SCHEME {0} NEXT USED", this.FullQualifiedName);
                if (nextUsed.Length > 0)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, " [{0}]", SqlBraket(nextUsed));
                }

                alterQuery.Add(statement.ToString());
            }
        }

        /// <summary>
        /// Resets the NEXT USED status of the filegroup that has been marked as NEXT USED.
        /// If no partition scheme has been marked as such, the method will silently ignore this.
        /// May only be used on an existing Partition Scheme.
        /// </summary>
        public void ResetNextUsed()
        {
            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER PARTITION FUNCTION {0} NEXT USED", this.FullQualifiedName));
                this.ExecutionManager.ExecuteNonQuery(queries);
                if (!this.ExecutionManager.Recording)
                {
                    Properties.Get("NextUsedFileGroup").SetValue(string.Empty);
                }
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ResetNextUsed, this, e);
            }
        }

        #region IExtendedProperties

        /// <summary>
        /// Collection of extended properties for this object.
        /// </summary>
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
        #endregion

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] {
                new PropagateInfo(ExtendedProperties, true, ExtendedProperty.UrnSuffix ),
            };
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != m_ExtendedProperties)
            {
                m_ExtendedProperties.MarkAllDropped();
            }
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        private StringCollection fileGroups = null;
        /// <summary>
        /// Collection of File Groups to which data for this scheme's destination is being mapped.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public StringCollection FileGroups
        {
            get
            {
                try
                {
                    CheckObjectState();
                    if (null == fileGroups)
                    {
                        if (State == SqlSmoState.Creating)
                        {
                            fileGroups = new StringCollection();
                        }
                        else
                        {
                            Request req = new Request(this.Urn + "/FileGroup");
                            req.Fields = new string[] { "Name", "ID" };
                            req.OrderByList = new OrderBy[] { new OrderBy("ID", OrderBy.Direction.Asc) };
                            DataTable tblRes = this.ExecutionManager.GetEnumeratorData(req);

                            fileGroups = new StringCollection();
                            foreach (DataRow dr in tblRes.Rows)
                            {
                                fileGroups.Add((string)dr["Name"]);
                            }
                        }
                    }

                    return fileGroups;
                }
                catch (Exception e)
                {
                    SqlSmoObject.FilterException(e);

                    throw new FailedOperationException(ExceptionTemplates.GetFileGroups, this, e);
                }
            }
        }

        ///<summary>
        /// refreshes the object's properties by reading them from the server
        ///</summary>
        public override void Refresh()
        {
            base.Refresh();
            fileGroups = null;
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
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            return new string[]
            {
                "NextUsedFileGroup",
                "PartitionFunction",
            };
        }
    }
}




