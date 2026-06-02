// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587

namespace Microsoft.SqlServer.Management.Smo
{
    internal abstract class DataClassificationScriptGenerator
    {
        /// <summary>
        /// Data classification script generator class for versions [v7,v14]
        /// </summary>
        internal sealed class V7 : DataClassificationScriptGenerator
        {

            internal V7(Column column, ScriptingPreferences sp) : base(column, sp) 
            {
                dcPropertyTuples = new Tuple<string, Property>[] {
                        new Tuple<string, Property>("sys_sensitivity_label_name", column.Properties.Get(nameof(Column.SensitivityLabelName))),
                        new Tuple<string, Property>("sys_sensitivity_label_id", column.Properties.Get(nameof(Column.SensitivityLabelId))),
                        new Tuple<string, Property>("sys_information_type_name", column.Properties.Get(nameof(Column.SensitivityInformationTypeName))),
                        new Tuple<string, Property>("sys_information_type_id", column.Properties.Get(nameof(Column.SensitivityInformationTypeId)))
                    };
            }

            internal V7(SensitivityClassification sensitivityClassification, ScriptingPreferences sp) : base(sensitivityClassification, sp) 
            {
                dcPropertyTuples = new Tuple<string, Property>[] {
                        new Tuple<string, Property>("sys_sensitivity_label_name", sensitivityClassification.Properties.Get(nameof(SensitivityClassification.SensitivityLabelName))),
                        new Tuple<string, Property>("sys_sensitivity_label_id", sensitivityClassification.Properties.Get(nameof(SensitivityClassification.SensitivityLabelId))),
                        new Tuple<string, Property>("sys_information_type_name", sensitivityClassification.Properties.Get(nameof(SensitivityClassification.SensitivityInformationTypeName))),
                        new Tuple<string, Property>("sys_information_type_id", sensitivityClassification.Properties.Get(nameof(SensitivityClassification.SensitivityInformationTypeId)))
                    };
            }

            internal override string Add()
            {
                StringBuilder sb = new StringBuilder();

                foreach (Tuple<string, Property> tuple in dcPropertyTuples)
                {
                    string value = (string)tuple.Item2.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        sb.Append($"EXEC sp_addextendedproperty @name = N'{SqlSmoObject.SqlString(tuple.Item1)}', @value = N'{SqlSmoObject.SqlString(value)}', @level0type = N'schema', @level0name = N'{SqlSmoObject.SqlString(schemaName)}', @level1type = N'table', @level1name = N'{SqlSmoObject.SqlString(tableName)}', @level2type = N'column', @level2name = N'{SqlSmoObject.SqlString(columnName)}'");
                        sb.Append(ScriptingPreferences.NewLine);
                    }
                }

                return sb.ToString();
            }

            internal override string Update()
            {
                StringBuilder sb = new StringBuilder();

                foreach (Tuple<string, Property> tuple in dcPropertyTuples)
                {
                    if (tuple.Item2.Dirty)
                    {
                        string value = (string)tuple.Item2.Value;

                        if (string.IsNullOrEmpty(value))
                        {
                            var scriptTemplate = @"if exists(
SELECT s.name AS schema_name
FROM sys.columns c
	LEFT JOIN sys.tables t ON t.object_id = c.object_id
	LEFT JOIN sys.schemas s ON s.schema_id = t.schema_id
	LEFT JOIN sys.extended_properties EP ON c.object_id = EP.major_id AND c.column_id = EP.minor_id and EP.name = N'{0}'
	WHERE s.name = N'{1}' AND t.name = N'{2}' AND c.name = N'{3}' AND EP.value IS NOT NULL
)
  EXEC sp_dropextendedproperty @name = N'{0}', @level0type = N'schema', @level0name = N'{1}', @level1type = N'table', @level1name = N'{2}', @level2type = N'column', @level2name = N'{3}'";
                            var scriptPopulated = string.Format(scriptTemplate.FixNewLines(), SqlSmoObject.SqlString(tuple.Item1), SqlSmoObject.SqlString(schemaName), SqlSmoObject.SqlString(tableName), SqlSmoObject.SqlString(columnName));
                            sb.Append(scriptPopulated);
                        }
                        else
                        {
                            var scriptTemplate = @"if exists(
SELECT s.name AS schema_name
FROM sys.columns c
	LEFT JOIN sys.tables t ON t.object_id = c.object_id
	LEFT JOIN sys.schemas s ON s.schema_id = t.schema_id
	LEFT JOIN sys.extended_properties EP ON c.object_id = EP.major_id AND c.column_id = EP.minor_id and EP.name = N'{0}'
	WHERE s.name = N'{2}' AND t.name = N'{3}' AND c.name = N'{4}' AND EP.value IS NOT NULL
)
  EXEC sp_dropextendedproperty @name = N'{0}', @level0type = N'schema', @level0name = N'{2}', @level1type = N'table', @level1name = N'{3}', @level2type = N'column', @level2name = N'{4}'
EXEC sp_addextendedproperty @name = N'{0}', @value = N'{1}', @level0type = N'schema', @level0name = N'{2}', @level1type = N'table', @level1name = N'{3}', @level2type = N'column', @level2name = N'{4}'";
                            var scriptPopulated = string.Format(scriptTemplate.FixNewLines(), SqlSmoObject.SqlString(tuple.Item1), SqlSmoObject.SqlString(value), SqlSmoObject.SqlString(schemaName), SqlSmoObject.SqlString(tableName), SqlSmoObject.SqlString(columnName));
                            sb.Append(scriptPopulated);
                        }

                        sb.Append(ScriptingPreferences.NewLine);
                    }
                }

                return sb.ToString();
            }

            internal override string Drop()
            {
                if (!scriptOnlyMode)
                {
                    return Update();
                }
                else
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (Tuple<string, Property> tuple in dcPropertyTuples)
                    {
                        string value = (string)tuple.Item2.Value;

                        if (!string.IsNullOrEmpty(value))
                        {
                            var scriptTemplate = @"if exists(
SELECT s.name AS schema_name
FROM sys.columns c
LEFT JOIN sys.tables t ON t.object_id = c.object_id
LEFT JOIN sys.schemas s ON s.schema_id = t.schema_id
LEFT JOIN sys.extended_properties EP ON c.object_id = EP.major_id AND c.column_id = EP.minor_id and EP.name = N'{0}'
WHERE s.name = N'{1}' AND t.name = N'{2}' AND c.name = N'{3}' AND EP.value IS NOT NULL
)
EXEC sp_dropextendedproperty @name = N'{0}', @level0type = N'schema', @level0name = N'{1}', @level1type = N'table', @level1name = N'{2}', @level2type = N'column', @level2name = N'{3}'";
                            var scriptPopulated = string.Format(scriptTemplate.FixNewLines(), SqlSmoObject.SqlString(tuple.Item1), SqlSmoObject.SqlString(schemaName), SqlSmoObject.SqlString(tableName), SqlSmoObject.SqlString(columnName));
                            sb.Append(scriptPopulated);
                            sb.Append(ScriptingPreferences.NewLine);
                        }
                    }

                    return sb.ToString();
                }
            }

        }

        /// <summary>
        /// Data classification script generator class for versions [v15,...)
        /// </summary>
        internal sealed class V15 : DataClassificationScriptGenerator
        {
            internal V15(Column column, ScriptingPreferences sp) : base(column, sp) 
            {
                dcPropertyTuples = new Tuple<string, Property>[] {
                    new Tuple<string, Property>("label", column.Properties.Get(nameof(Column.SensitivityLabelName))),
                    new Tuple<string, Property>("label_id", column.Properties.Get(nameof(Column.SensitivityLabelId))),
                    new Tuple<string, Property>("information_type", column.Properties.Get(nameof(Column.SensitivityInformationTypeName))),
                    new Tuple<string, Property>("information_type_id", column.Properties.Get(nameof(Column.SensitivityInformationTypeId))),
                    new Tuple<string, Property>("rank", column.IsSupportedProperty(nameof(Column.SensitivityRank)) ? column.Properties.Get(nameof(Column.SensitivityRank)) : null)
                };
            }

            internal V15(SensitivityClassification sensitivityClassification, ScriptingPreferences sp) : base(sensitivityClassification, sp) 
            {
                dcPropertyTuples = new Tuple<string, Property>[] {
                    new Tuple<string, Property>("label", sensitivityClassification.Properties.Get(nameof(SensitivityClassification.SensitivityLabelName))),
                    new Tuple<string, Property>("label_id", sensitivityClassification.Properties.Get(nameof(SensitivityClassification.SensitivityLabelId))),
                    new Tuple<string, Property>("information_type", sensitivityClassification.Properties.Get(nameof(SensitivityClassification.SensitivityInformationTypeName))),
                    new Tuple<string, Property>("information_type_id", sensitivityClassification.Properties.Get(nameof(SensitivityClassification.SensitivityInformationTypeId))),
                    new Tuple<string, Property>("rank", sensitivityClassification.IsSupportedProperty(nameof(SensitivityClassification.SensitivityRank)) ? sensitivityClassification.Properties.Get(nameof(SensitivityClassification.SensitivityRank)) : null)
                };
            }

            internal override string Add()
            {
                string columnFullName = $"[{SqlSmoObject.SqlBraket(schemaName)}].[{SqlSmoObject.SqlBraket(tableName)}].[{SqlSmoObject.SqlBraket(columnName)}]";
                ScriptStringBuilder ssb = new ScriptStringBuilder($"ADD SENSITIVITY CLASSIFICATION TO {columnFullName} WITH");

                foreach (Tuple<string, Property> tuple in dcPropertyTuples)
                {
                    Property property = tuple.Item2;

                    if (property != null)
                    {
                        string value = property.Value as string;
                        SensitivityRank? rank = property.Value as SensitivityRank?;

                        if (!string.IsNullOrEmpty(value))
                        {
                            ssb.SetParameter(tuple.Item1, Util.EscapeString(value, '\''));
                        }
                        else if (rank.HasValue && rank.Value != SensitivityRank.Undefined)
                        {
                            ssb.SetParameter(tuple.Item1, rank.Value.ToString(), ParameterValueFormat.NotString);
                        }
                    }
                }

                return ssb.ToString();
            }

            internal override string Update()
            {
                return Add();
            }

            internal override string Drop()
            {
                string columnFullName = $"[{SqlSmoObject.SqlBraket(schemaName)}].[{SqlSmoObject.SqlBraket(tableName)}].[{SqlSmoObject.SqlBraket(columnName)}]";
                return $"DROP SENSITIVITY CLASSIFICATION FROM {columnFullName}";
            }
        }

        internal static DataClassificationScriptGenerator Create(Column column, ScriptingPreferences sp)
        {
            // Check target server version:
            //  - If it exists, return the corresponding script generator.
            //  - If it doesn't exist, return the corresponding script generator based on source server version.
            bool isV150 = sp == null ? VersionUtils.IsSql15Azure12OrLater(column.DatabaseEngineType, column.ServerVersion) :
                VersionUtils.IsTargetVersionSql15Azure12OrLater(sp.TargetDatabaseEngineType, sp.TargetServerVersion);

            return isV150 ? new V15(column, sp) : new V7(column, sp) as DataClassificationScriptGenerator;
        }

        internal static DataClassificationScriptGenerator Create(SensitivityClassification sensitivityClassification, ScriptingPreferences sp)
        {
            // Check target server version:
            //  - If it exists, return the corresponding script generator.
            //  - If it doesn't exist, return the corresponding script generator based on source server version.
            bool isV150 = sp == null ? VersionUtils.IsSql15Azure12OrLater(sensitivityClassification.DatabaseEngineType, sensitivityClassification.ServerVersion) :
                VersionUtils.IsTargetVersionSql15Azure12OrLater(sp.TargetDatabaseEngineType, sp.TargetServerVersion);

            return isV150 ? new V15(sensitivityClassification, sp) : new V7(sensitivityClassification, sp) as DataClassificationScriptGenerator;
        }

        internal abstract string Add();
        internal abstract string Update();
        internal abstract string Drop();

        protected ScriptingPreferences ScriptingPreferences;
        protected Tuple<string, Property>[] dcPropertyTuples;
        protected string columnName;
        protected string tableName;
        protected string schemaName;
        protected bool scriptOnlyMode;

        private DataClassificationScriptGenerator(Column column, ScriptingPreferences sp)
        {
            ScriptingPreferences = sp;
            columnName = column.Name;
            scriptOnlyMode = false;
            
            Table table = column.ParentColl.ParentInstance as Table;
            tableName = table.Name;
            schemaName = table.Schema;
        }

        private DataClassificationScriptGenerator(SensitivityClassification sensitivityClassification, ScriptingPreferences sp)
        {
            ScriptingPreferences = sp;
            columnName = (string)sensitivityClassification.Properties.GetValueWithNullReplacement(nameof(SensitivityClassification.ReferencedColumn), false, false);
            tableName = (string)sensitivityClassification.Properties.GetValueWithNullReplacement(nameof(SensitivityClassification.ReferencedTable), false, false);
            schemaName = (string)sensitivityClassification.Properties.GetValueWithNullReplacement(nameof(SensitivityClassification.ReferencedTableSchema), false, false);
            scriptOnlyMode = true;
        }
    }
}