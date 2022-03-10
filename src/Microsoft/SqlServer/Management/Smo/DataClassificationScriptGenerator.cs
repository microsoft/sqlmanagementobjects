// Copyright (c) Microsoft.
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
            internal V7(Column column, ScriptingPreferences sp) : base(column, sp) { }

            internal override string Add()
            {
                StringBuilder sb = new StringBuilder();
                Table table = Column.ParentColl.ParentInstance as Table;
                Tuple<string, string>[] tuples = new Tuple<string, string>[] {
                        new Tuple<string, string>("sys_sensitivity_label_name", (string)Column.Properties.Get("SensitivityLabelName").Value),
                        new Tuple<string, string>("sys_sensitivity_label_id", (string)Column.Properties.Get("SensitivityLabelId").Value),
                        new Tuple<string, string>("sys_information_type_name", (string)Column.Properties.Get("SensitivityInformationTypeName").Value),
                        new Tuple<string, string>("sys_information_type_id", (string)Column.Properties.Get("SensitivityInformationTypeId").Value)
                    };

                foreach (Tuple<string, string> tuple in tuples)
                {
                    if (!string.IsNullOrEmpty(tuple.Item2))
                    {
                        sb.Append($"exec sp_addextendedproperty @level0type = N'schema', @level0name = N'{SqlSmoObject.SqlString(table.Schema)}', @level1type = N'table', @level1name = N'{SqlSmoObject.SqlString(table.Name)}', @level2type = N'column', @level2name = N'{SqlSmoObject.SqlString(Column.Name)}', @name = N'{SqlSmoObject.SqlString(tuple.Item1)}', @value = N'{SqlSmoObject.SqlString(tuple.Item2)}'");
                        sb.Append(ScriptingPreferences.NewLine);
                    }
                }

                return sb.ToString();
            }

            internal override string Update()
            {
                StringBuilder sb = new StringBuilder();
                Table table = Column.ParentColl.ParentInstance as Table;
                Tuple<string, Property>[] tuples = new Tuple<string, Property>[] {
                        new Tuple<string, Property>("sys_sensitivity_label_name", Column.Properties.Get("SensitivityLabelName")),
                        new Tuple<string, Property>("sys_sensitivity_label_id", Column.Properties.Get("SensitivityLabelId")),
                        new Tuple<string, Property>("sys_information_type_name", Column.Properties.Get("SensitivityInformationTypeName")),
                        new Tuple<string, Property>("sys_information_type_id", Column.Properties.Get("SensitivityInformationTypeId"))
                    };

                foreach (Tuple<string, Property> tuple in tuples)
                {
                    if (tuple.Item2.Dirty)
                    {
                        string value = (string)tuple.Item2.Value;

                        if (string.IsNullOrEmpty(value))
                        {
                            sb.Append($@"if exists(
SELECT s.name AS schema_name
FROM sys.columns c
	LEFT JOIN sys.tables t ON t.object_id = c.object_id
	LEFT JOIN sys.schemas s ON s.schema_id = t.schema_id
	LEFT JOIN sys.extended_properties EP ON c.object_id = EP.major_id AND c.column_id = EP.minor_id and EP.name = N'{SqlSmoObject.SqlString(tuple.Item1)}'
	WHERE s.name = N'{SqlSmoObject.SqlString(table.Schema)}' AND t.name = N'{SqlSmoObject.SqlString(table.Name)}' AND c.name = N'{SqlSmoObject.SqlString(Column.Name)}' AND EP.value IS NOT NULL
)
  exec sp_dropextendedproperty @name = N'{SqlSmoObject.SqlString(tuple.Item1)}', @level0type = N'schema', @level0name = N'{SqlSmoObject.SqlString(table.Schema)}', @level1type = N'table', @level1name = N'{SqlSmoObject.SqlString(table.Name)}', @level2type = N'column', @level2name = N'{SqlSmoObject.SqlString(Column.Name)}'".FixNewLines());
                        }
                        else
                        {
                            sb.Append($@"if exists(
SELECT s.name AS schema_name
FROM sys.columns c
	LEFT JOIN sys.tables t ON t.object_id = c.object_id
	LEFT JOIN sys.schemas s ON s.schema_id = t.schema_id
	LEFT JOIN sys.extended_properties EP ON c.object_id = EP.major_id AND c.column_id = EP.minor_id and EP.name = N'{SqlSmoObject.SqlString(tuple.Item1)}'
	WHERE s.name = N'{SqlSmoObject.SqlString(table.Schema)}' AND t.name = N'{SqlSmoObject.SqlString(table.Name)}' AND c.name = N'{SqlSmoObject.SqlString(Column.Name)}' AND EP.value IS NOT NULL
)
  exec sp_dropextendedproperty @name = N'{SqlSmoObject.SqlString(tuple.Item1)}', @level0type = N'schema', @level0name = N'{SqlSmoObject.SqlString(table.Schema)}', @level1type = N'table', @level1name = N'{SqlSmoObject.SqlString(table.Name)}', @level2type = N'column', @level2name = N'{SqlSmoObject.SqlString(Column.Name)}'
exec sp_addextendedproperty @level0type = N'schema', @level0name = N'{SqlSmoObject.SqlString(table.Schema)}', @level1type = N'table', @level1name = N'{SqlSmoObject.SqlString(table.Name)}', @level2type = N'column', @level2name = N'{SqlSmoObject.SqlString(Column.Name)}', @name = N'{SqlSmoObject.SqlString(tuple.Item1)}', @value = N'{SqlSmoObject.SqlString(value)}'".FixNewLines());
                        }

                        sb.Append(ScriptingPreferences.NewLine);
                    }
                }

                return sb.ToString();
            }

            internal override string Drop()
            {
                return Update();
            }
        }

        /// <summary>
        /// Data classification script generator class for versions [v15,...)
        /// </summary>
        internal sealed class V15 : DataClassificationScriptGenerator
        {
            internal V15(Column column, ScriptingPreferences sp) : base(column, sp) { }

            internal override string Add()
            {
                ScriptStringBuilder ssb = new ScriptStringBuilder($"ADD SENSITIVITY CLASSIFICATION TO {Column.ParentColl.ParentInstance.FullQualifiedName}.{Column.FullQualifiedName} WITH");

                Tuple<string, Property>[] tuples = new Tuple<string, Property>[] {
                        new Tuple<string, Property>("label", Column.Properties.Get("SensitivityLabelName")),
                        new Tuple<string, Property>("label_id", Column.Properties.Get("SensitivityLabelId")),
                        new Tuple<string, Property>("information_type", Column.Properties.Get("SensitivityInformationTypeName")),
                        new Tuple<string, Property>("information_type_id", Column.Properties.Get("SensitivityInformationTypeId")),
                        new Tuple<string, Property>("rank", Column.IsSupportedProperty("SensitivityRank") ? Column.Properties.Get("SensitivityRank") : null)
                    };

                foreach (Tuple<string, Property> tuple in tuples)
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
                return $"DROP SENSITIVITY CLASSIFICATION FROM {Column.ParentColl.ParentInstance.FullQualifiedName}.{Column.FullQualifiedName}";
            }
        }

        internal static DataClassificationScriptGenerator Create(Column column, ScriptingPreferences sp)
        {
            // Check target server version:
            //  - If it exists, return the corresponding script generator.
            //  - If it doesn't exist, return the corresponding script generator based on source server version.
            bool isV150 = sp == null ? VersionUtils.IsSql15Azure12OrLater(column.DatabaseEngineType, column.ServerVersion) :
                VersionUtils.IsTargetVersionSql15Azure12OrLater(sp.TargetDatabaseEngineType, sp.TargetServerVersionInternal);

            return isV150 ? new V15(column, sp) : new V7(column, sp) as DataClassificationScriptGenerator;
        }

        internal abstract string Add();
        internal abstract string Update();
        internal abstract string Drop();

        protected Column Column;
        protected ScriptingPreferences ScriptingPreferences;

        private DataClassificationScriptGenerator(Column column, ScriptingPreferences sp)
        {
            this.Column = column;
            this.ScriptingPreferences = sp;
        }
    }
}