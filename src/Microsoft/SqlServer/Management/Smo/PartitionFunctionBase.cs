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
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class PartitionFunction : ScriptNameObjectBase, Cmn.IDroppable, Cmn.IDropIfExists,
        Cmn.IAlterable, Cmn.ICreatable, IScriptable, IExtendedProperties
    {
        internal PartitionFunction(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "PartitionFunction";
            }
        }

        /// <summary>
        /// Name of PartitionFunction
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
            string sFullTableName = FormatFullNameForScripting(sp);

            ScriptIncludeHeaders(statement, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_PARTITION_FUNCTION, "NOT", FormatFullNameForScripting(sp, false));
                statement.AppendFormat(SmoApplication.DefaultCulture, sp.NewLine);
            }
            statement.AppendFormat(SmoApplication.DefaultCulture, "CREATE PARTITION FUNCTION {0}", sFullTableName);

            if (PartitionFunctionParameters.Count == 0)
            {
                throw new SmoException(ExceptionTemplates.ObjectWithNoChildren(this.GetType().Name, "PartitionFunctionParameter"));
            }

            statement.Append("(");
            int paramCount = 0;
            foreach (PartitionFunctionParameter pfp in this.PartitionFunctionParameters)
            {
                if (0 < paramCount++)
                {
                    statement.Append(Globals.commaspace);
                }

                if (UserDefinedDataType.TypeAllowsLength(pfp.Name, pfp.StringComparer) &&
                    pfp.Length > 0)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                                           "{0}({1})",
                                           pfp.Name,
                                           pfp.Length);
                }
                else if (UserDefinedDataType.TypeAllowsPrecisionScale(pfp.Name, pfp.StringComparer))
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                                           "{0}({1},{2})",
                                           pfp.Name,
                                           pfp.NumericPrecision,
                                           pfp.NumericScale);
                }
                else if (UserDefinedDataType.TypeAllowsScale(pfp.Name, pfp.StringComparer))
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                                           "{0}({1})",
                                           pfp.Name,
                                           pfp.NumericScale);
                }
                else if (DataType.IsTypeFloatStateCreating(pfp.Name, pfp))
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                                           "{0}({1})",
                                           pfp.Name,
                                           pfp.NumericPrecision);
                }
                else
                {
                    statement.Append(pfp.Name);
                }

                //////////////////////////////////////////////////////////////////////////
                // TODO: don't forget to add COLLATE + collation name check
                //////////////////////////////////////////////////////////////////////////
            }
            statement.Append(") AS ");

            statement.Append("RANGE ");
            // Range
            if (null != Properties.Get("RangeType").Value)
            {
                RangeType rt = (RangeType)Properties["RangeType"].Value;
                statement.AppendFormat(SmoApplication.DefaultCulture, "{0} ", rt == RangeType.Left ? "LEFT" : "RIGHT");
            }

            statement.Append("FOR VALUES (");
            if (null != RangeValues)
            {
                for (int idx = 0; idx < RangeValues.Length; idx++)
                {
                    if (idx > 0)
                    {
                        statement.Append(Globals.commaspace);
                    }

                    statement.Append(FormatSqlVariant(RangeValues[idx]));
                }
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
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_PARTITION_FUNCTION, "", FormatFullNameForScripting(sp, false));
                sb.AppendFormat(SmoApplication.DefaultCulture, sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP PARTITION FUNCTION {0}", FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            // only extended properties to alter
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

        private PartitionFunctionParameterCollection m_PartitionFunctionParameters;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(PartitionFunctionParameter))]
        public PartitionFunctionParameterCollection PartitionFunctionParameters
        {
            get
            {
                CheckObjectState();
                if (null == m_PartitionFunctionParameters)
                {
                    m_PartitionFunctionParameters = new PartitionFunctionParameterCollection(this);
                    m_PartitionFunctionParameters.AcceptDuplicateNames = true;
                }
                return m_PartitionFunctionParameters;
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] {
                new PropagateInfo(ExtendedProperties, true, ExtendedProperty.UrnSuffix ),
            };
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

        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != m_PartitionFunctionParameters)
            {
                m_PartitionFunctionParameters.MarkAllDropped();
            }

            if (null != m_ExtendedProperties)
            {
                m_ExtendedProperties.MarkAllDropped();
            }
        }

        public void MergeRangePartition(object boundaryValue)
        {
            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER PARTITION FUNCTION {0}() MERGE RANGE({1})",
                                           this.FullQualifiedName, FormatSqlVariant(boundaryValue)));
                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.MergeRangePartition, this, e);
            }
        }

        public void SplitRangePartition(object boundaryValue)
        {
            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER PARTITION FUNCTION {0}() SPLIT RANGE({1})",
                                           this.FullQualifiedName, FormatSqlVariant(boundaryValue)));
                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SplitRangePartition, this, e);
            }
        }

        private object[] rangeValues = null;
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        [SfcSerializationAdapter(typeof(SqlVariantSerializationAdapter))]
        public object[] RangeValues
        {
            get
            {
                try
                {
                    CheckObjectState();
                    if (null == rangeValues && State != SqlSmoState.Creating)
                    {
                        Request req = new Request(this.Urn + "/RangeValue");
                        req.Fields = new string[] { "Value", "ID" };
                        req.OrderByList = new OrderBy[] { new OrderBy("ID", OrderBy.Direction.Asc) };
                        DataTable tblRes = this.ExecutionManager.GetEnumeratorData(req);

                        rangeValues = new object[tblRes.Rows.Count];
                        int rvIdx = 0;
                        foreach (DataRow dr in tblRes.Rows)
                        {
                            rangeValues[rvIdx++] = dr["Value"];
                        }
                    }

                    return rangeValues;
                }
                catch (Exception e)
                {
                    SqlSmoObject.FilterException(e);

                    throw new FailedOperationException(ExceptionTemplates.GetRangeValues, this, e);
                }
            }

            set
            {
                CheckObjectState();
                rangeValues = value;
            }
        }

        ///<summary>
        /// refreshes the object's properties by reading them from the server
        ///</summary>
        public override void Refresh()
        {
            base.Refresh();
            rangeValues = null;
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
            return new string[] {
                                    "RangeType"
                                };
        }
    }
}



