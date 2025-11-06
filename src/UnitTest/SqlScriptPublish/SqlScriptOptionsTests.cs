// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Resources;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using static Microsoft.SqlServer.Management.SqlScriptPublish.SqlScriptOptions;
using Assert = NUnit.Framework.Assert;

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
                {TargetDatabaseEngineType = ScriptDatabaseEngineType.SingleInstance};
            var typeConverter = TypeDescriptor.GetConverter(scriptOptions);
            var properties = typeConverter.GetProperties(scriptOptions);
            var engineEditionProperty = properties[nameof(scriptOptions.TargetDatabaseEngineEdition)];
            var context = new TestDescriptorContext()
                {Instance = scriptOptions, PropertyDescriptor = engineEditionProperty};
            var editionAllowedValues = engineEditionProperty.Converter.GetStandardValues(context)
                .Cast<ScriptDatabaseEngineEdition>().OrderBy(t => t).ToArray();
            Assert.That(editionAllowedValues,
                Is.EquivalentTo(new[]
                {
                    ScriptDatabaseEngineEdition.SqlServerPersonalEdition,
                    ScriptDatabaseEngineEdition.SqlServerStandardEdition,
                    ScriptDatabaseEngineEdition.SqlServerEnterpriseEdition,
                    ScriptDatabaseEngineEdition.SqlServerExpressEdition,
                    ScriptDatabaseEngineEdition.SqlServerStretchEdition,
                    ScriptDatabaseEngineEdition.SqlServerManagedInstanceEdition,
                    ScriptDatabaseEngineEdition.SqlDatabaseEdgeEdition,
                    ScriptDatabaseEngineEdition.SqlAzureArcManagedInstanceEdition
                }), "Allowed edition values for SingleInstance type");
            scriptOptions.TargetDatabaseEngineType = ScriptDatabaseEngineType.SqlAzure;
            editionAllowedValues = engineEditionProperty.Converter.GetStandardValues(context)
                .Cast<ScriptDatabaseEngineEdition>().OrderBy(t => t).ToArray();
            Assert.That(editionAllowedValues,
                Is.EquivalentTo(new[]
                {
                    ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition,
                    ScriptDatabaseEngineEdition.SqlServerOnDemandEdition
                }), "Allowed edition values for SqlAzure type");
            // Adding assert to check if source Sql DW.
            scriptOptions.TargetDatabaseEngineEdition = ScriptDatabaseEngineEdition.SqlDatawarehouseEdition;
            scriptOptions.SourceEngineEdition = Management.Common.DatabaseEngineEdition.SqlDataWarehouse;
            editionAllowedValues = engineEditionProperty.Converter.GetStandardValues(context)
               .Cast<ScriptDatabaseEngineEdition>().OrderBy(t => t).ToArray();
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
                VerifyDisplayNameKeys(typeof(ScriptDatabaseEngineEdition), resourceManager);
                VerifyDisplayNameKeys(typeof(ScriptDatabaseEngineType), resourceManager);
                VerifyDisplayNameKeys(typeof(BooleanTypeOptions), resourceManager);
                VerifyDisplayNameKeys(typeof(ScriptCompatibilityOptions), resourceManager);
                VerifyDisplayNameKeys(typeof(ScriptStatisticsOptions), resourceManager);
                VerifyDisplayNameKeys(typeof(TypeOfDataToScriptOptions), resourceManager);
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Compat_verify_enum_attribute_versions()
        {
            // Version 17
            var attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script170Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(17));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(0));

            // Version 16
            attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script160Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(16));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(0));

            // Version 15
            attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script150Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(15));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(0));

            // Version 14
            attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script140Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(14));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(0));

            // Version 13
            attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script130Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(13));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(0));

            // Version 12
            attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script120Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(12));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(0));

            // Version 11
            attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script110Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(11));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(0));

            // Version 10.50
            attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script105Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(10));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(50));

            // Version 10
            attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script100Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(10));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(0));

            // Version 9
            attr = CompatibilityLevelSupportedVersionAttribute.GetAttributeForOption(ScriptCompatibilityOptions.Script90Compat);
            Assert.That(attr.MinimumMajorVersion, Is.EqualTo(9));
            Assert.That(attr.MinimumMinorVersion, Is.EqualTo(0));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Compat_GetOptionForVersion_returns_correct_options()
        {
            // Version 17
            var option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(17);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script170Compat));

            // Version 16
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(16);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script160Compat));

            // Version 15
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(15);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script150Compat));

            // Version 14
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(14);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script140Compat));

            // Version 13
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(13);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script130Compat));

            // Version 12
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(12);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script120Compat));

            // Version 11
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(11);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script110Compat));

            // Version 10.50
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(10, 50);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script105Compat));

            // Version 10
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(10);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script100Compat));

            // Version 9
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(9);
            Assert.That(option.Value, Is.EqualTo(ScriptCompatibilityOptions.Script90Compat));

            // Version 8 - Should return nothing since it's unsupported
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(8);
            Assert.That(option, Is.Null);

            // Version 18 - Non-existent version
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(18);
            Assert.That(option, Is.Null);

            // Edge case for minor version-handling
            option = CompatibilityLevelSupportedVersionAttribute.GetOptionForVersion(1, 50);
            Assert.That(option, Is.Null);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Compat_FilterUnsupportedOptions_removes_unsupported_options()
        {
            var optionsArray = Enum.GetValues(typeof(ScriptCompatibilityOptions));
            var optionsList = optionsArray.Cast<ScriptCompatibilityOptions>().ToList();

            var initialCount = optionsList.Count;

            // Version 17
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 17, 0);
            Assert.That(optionsList.Count, Is.EqualTo(initialCount));

            // Version 16
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 16, 0);
            Assert.That(optionsList.Count, Is.EqualTo(--initialCount));
            Assert.That(optionsList, Does.Not.Contain(ScriptCompatibilityOptions.Script170Compat));

            // Version 15
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 15, 0);
            Assert.That(optionsList.Count, Is.EqualTo(--initialCount));
            Assert.That(optionsList, Does.Not.Contain(ScriptCompatibilityOptions.Script160Compat));

            // Version 14
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 14, 0);
            Assert.That(optionsList.Count, Is.EqualTo(--initialCount));
            Assert.That(optionsList, Does.Not.Contain(ScriptCompatibilityOptions.Script150Compat));

            // Version 13
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 13, 0);
            Assert.That(optionsList.Count, Is.EqualTo(--initialCount));
            Assert.That(optionsList, Does.Not.Contain(ScriptCompatibilityOptions.Script140Compat));

            // Version 12
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 12, 0);
            Assert.That(optionsList.Count, Is.EqualTo(--initialCount));
            Assert.That(optionsList, Does.Not.Contain(ScriptCompatibilityOptions.Script130Compat));

            // Version 11
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 11, 0);
            Assert.That(optionsList.Count, Is.EqualTo(--initialCount));
            Assert.That(optionsList, Does.Not.Contain(ScriptCompatibilityOptions.Script120Compat));

            // Version 10.50
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 10, 50);
            Assert.That(optionsList.Count, Is.EqualTo(--initialCount));
            Assert.That(optionsList, Does.Not.Contain(ScriptCompatibilityOptions.Script110Compat));

            // Version 10
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 10, 0);
            Assert.That(optionsList.Count, Is.EqualTo(--initialCount));
            Assert.That(optionsList, Does.Not.Contain(ScriptCompatibilityOptions.Script105Compat));

            // Version 9
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 9, 0);
            Assert.That(optionsList.Count, Is.EqualTo(1));
            Assert.That(optionsList, Does.Not.Contain(ScriptCompatibilityOptions.Script100Compat));
            Assert.That(optionsList[0], Is.EqualTo(ScriptCompatibilityOptions.Script90Compat));

            // Version below lowest supported value
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(optionsList, 1, 0);
            Assert.That(optionsList, Is.Empty);

            // Empty collection
            optionsList = CompatibilityLevelSupportedVersionAttribute.FilterUnsupportedOptions(new List<ScriptCompatibilityOptions>(), 17, 0);
            Assert.That(optionsList, Is.Empty);
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

        /// <summary>
        /// Verifies that LoadShellScriptingOptions sets the expected ScriptCompatibilityOptions to
        /// ensure that the mapping between SqlServerVersion and ScriptCompatibilityOptions is correct.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void LoadShellScriptingOptions_ReturnsExpectedTargetServerVersion()
        {
            var compatVersions = Enum.GetValues(typeof(ScriptCompatibilityOptions));
            for (int i = 0; i < (int)SqlServerVersion.VersionLatest - 1; i++)
            {
                // This version doesn't really matter - we just care about the TargetServerVersion property
                var options = new SqlScriptOptions(new Version(17, 0));
                var publishingOptions = new TestPublishingOptions
                {
                    // SqlServerVersion starts at index 1 and also skips version 80, so we adjust to account for that
                    TargetServerVersion = (SqlServerVersion)(i + 2)
                };
                options.LoadShellScriptingOptions(publishingOptions, smoObject: null);
                // Version 105 corresponds to compat level 100
                var expectedVersion = publishingOptions.TargetServerVersion == SqlServerVersion.Version105 ? ScriptCompatibilityOptions.Script100Compat : compatVersions.GetValue(i);
                Assert.That(options.ScriptCompatibilityOption, Is.EqualTo(expectedVersion));
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
