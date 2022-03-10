// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.ComponentModel;
using System.Linq;
using System.Resources;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using ScriptDatabaseEngineEdition = Microsoft.SqlServer.Management.SqlScriptPublish.SqlScriptOptions.ScriptDatabaseEngineEdition;

namespace Microsoft.SqlServer.Test.SqlScriptPublishTests
{
    [TestClass]

    public class SqlScriptOptionsTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void ConfigureVisibleEnumFields_hides_inappropriate_values()
        {
            var scriptOptions = new SqlScriptOptions(Version.Parse("14.0.0.0"))
                {TargetDatabaseEngineType = SqlScriptOptions.ScriptDatabaseEngineType.SingleInstance};
            var typeConverter = TypeDescriptor.GetConverter(scriptOptions);
            var properties = typeConverter.GetProperties(scriptOptions);
            var engineEditionProperty = properties[nameof(scriptOptions.TargetDatabaseEngineEdition)];
            var context = new TestDescriptorContext()
                {Instance = scriptOptions, PropertyDescriptor = engineEditionProperty};
            var editionAllowedValues = engineEditionProperty.Converter.GetStandardValues(context)
                .Cast<SqlScriptOptions.ScriptDatabaseEngineEdition>().OrderBy(t => t).ToArray();
            Assert.That(editionAllowedValues,
                Is.EquivalentTo(new[]
                {
                    ScriptDatabaseEngineEdition.SqlServerPersonalEdition,
                    ScriptDatabaseEngineEdition.SqlServerStandardEdition,
                    ScriptDatabaseEngineEdition.SqlServerEnterpriseEdition,
                    ScriptDatabaseEngineEdition.SqlServerExpressEdition,
                    ScriptDatabaseEngineEdition.SqlServerStretchEdition,
                    ScriptDatabaseEngineEdition.SqlServerManagedInstanceEdition,
                    ScriptDatabaseEngineEdition.SqlDatabaseEdgeEdition
                }), "Allowed edition values for SingleInstance type");
            scriptOptions.TargetDatabaseEngineType = SqlScriptOptions.ScriptDatabaseEngineType.SqlAzure;
            editionAllowedValues = engineEditionProperty.Converter.GetStandardValues(context)
                .Cast<SqlScriptOptions.ScriptDatabaseEngineEdition>().OrderBy(t => t).ToArray();
            Assert.That(editionAllowedValues,
                Is.EquivalentTo(new[]
                {
                    ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition,
                    ScriptDatabaseEngineEdition.SqlServerOnDemandEdition
                }), "Allowed edition values for SqlAzure type");
            // Adding assert to check if source Sql DW.
            scriptOptions.TargetDatabaseEngineEdition = SqlScriptOptions.ScriptDatabaseEngineEdition.SqlDatawarehouseEdition;
            scriptOptions.SourceEngineEdition = Management.Common.DatabaseEngineEdition.SqlDataWarehouse;
            editionAllowedValues = engineEditionProperty.Converter.GetStandardValues(context)
               .Cast<SqlScriptOptions.ScriptDatabaseEngineEdition>().OrderBy(t => t).ToArray();
            Assert.That(editionAllowedValues,
                Is.EquivalentTo(new[]
                {
                    ScriptDatabaseEngineEdition.SqlDatawarehouseEdition
                }), "Allowed edition values for Sql Data warehouse Edition ");

        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SqlScriptOptions_Enums_have_valid_DisplayNameKey_attributes()
        {
            var resourceTypeName =
                ((LocalizedPropertyResourcesAttribute) Attribute.GetCustomAttribute(typeof(SqlScriptOptions),
                    typeof(LocalizedPropertyResourcesAttribute))).ResourcesName;
            var resourceManager = new ResourceManager(resourceTypeName, typeof(SqlScriptOptions).Assembly);
            Assert.Multiple(() =>
            {
                VerifyDisplayNameKeys(typeof(SqlScriptOptions.ScriptDatabaseEngineEdition), resourceManager);
                VerifyDisplayNameKeys(typeof(SqlScriptOptions.ScriptDatabaseEngineType), resourceManager);
                VerifyDisplayNameKeys(typeof(SqlScriptOptions.BooleanTypeOptions), resourceManager);
                VerifyDisplayNameKeys(typeof(SqlScriptOptions.ScriptCompatibilityOptions), resourceManager);
                VerifyDisplayNameKeys(typeof(SqlScriptOptions.ScriptStatisticsOptions), resourceManager);
                VerifyDisplayNameKeys(typeof(SqlScriptOptions.TypeOfDataToScriptOptions), resourceManager);
            });
        }

        private static void VerifyDisplayNameKeys(Type type, ResourceManager resourceManager)
        {
            foreach (string fieldName in Enum.GetNames(type))
            {
                
                var fi = type.GetField(fieldName);

                var attributeAsObject = fi.GetCustomAttributes(typeof(DisplayNameKeyAttribute), true)
                    .Cast<DisplayNameKeyAttribute>().Single();

                var resource = resourceManager.GetString(attributeAsObject.Key);
                Assert.That(resource, Is.Not.Null.And.Not.Empty,
                    $"Resource '{attributeAsObject.Key}' missing for enum value {type.Name}.{fieldName}");
            }
        }

        internal class TestDescriptorContext : ITypeDescriptorContext
        {
            public object GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }

            public bool OnComponentChanging()
            {
                throw new NotImplementedException();
            }

            public void OnComponentChanged()
            {
                throw new NotImplementedException();
            }

            public IContainer Container { get; set; }
            public object Instance { get; set; }
            public PropertyDescriptor PropertyDescriptor { get; set; }
        }

    }
}
