// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using SmoServer = Microsoft.SqlServer.Management.Smo.Server;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests to verify that all SMO and related objects with LocalizableTypeConverter have
    /// properly named localization strings for their properties.
    /// This helps catch typos in .strings files like:
    /// - "Datababase_" instead of "Database_"
    /// - "SServer_" instead of "Server_"
    /// - Missing "Name" suffix (e.g., "Table_DataRetentionFilterColumnName" instead of "Table_DataRetentionFilterColumnNameName")
    /// </summary>
    [TestClass]
    public class LocalizablePropertyResourcesTests
    {
        // Cache resource managers to avoid repeated lookups
        private static readonly Dictionary<string, ResourceManager> ResourceManagerCache = new Dictionary<string, ResourceManager>();

        /// <summary>
        /// Gets or sets the test context which provides information about the current test run.
        /// </summary>
        public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }

        /// <summary>
        /// List of assemblies to scan for localization validation.
        /// These are assemblies that the test project has references to and use LocalizedPropertyResources.
        /// </summary>
        private static readonly (Assembly Assembly, string Name)[] AssembliesToScan = new[]
        {
            (typeof(SmoServer).Assembly, "Smo"),
            (typeof(PolicyStore).Assembly, "Dmf"),
            (typeof(SfcInstance).Assembly, "Sdk.Sfc"),
        };

        /// <summary>
        /// Verifies that all types with LocalizedPropertyResources attribute have valid
        /// Name and Description strings for each property that uses the default key pattern.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void AllAssemblies_WithLocalizableTypeConverter_HaveValidPropertyNameAndDescriptionStrings()
        {
            var allErrors = new List<string>();
            var totalCheckedProperties = 0;
            var testedClassesAndProperties = new Dictionary<string, List<string>>();
            var totalTypesScanned = 0;

            foreach (var (assembly, assemblyName) in AssembliesToScan)
            {
                var (errors, checkedProperties, testedClasses, typesScanned) = ValidateAssemblyLocalization(assembly);
                
                // Prefix errors with assembly name for clarity
                allErrors.AddRange(errors.Select(e => $"[{assemblyName}] {e}"));
                totalCheckedProperties += checkedProperties;
                totalTypesScanned += typesScanned;

                foreach (var kvp in testedClasses)
                {
                    testedClassesAndProperties[kvp.Key] = kvp.Value;
                }
            }

            // Generate and attach the tested classes and properties file
            AttachTestedClassesReport(testedClassesAndProperties, totalCheckedProperties, totalTypesScanned, allErrors);

            // Fail the test if there are missing localization strings
            if (allErrors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Found {allErrors.Count} missing localization strings (checked {totalCheckedProperties} properties across {totalTypesScanned} types in {AssembliesToScan.Length} assemblies):");
                sb.AppendLine();
                foreach (var error in allErrors.Take(50))
                {
                    sb.AppendLine($"  - {error}");
                }
                if (allErrors.Count > 50)
                {
                    sb.AppendLine($"  ... and {allErrors.Count - 50} more errors");
                }

                Assert.Fail(sb.ToString());
            }

            // Ensure we actually checked something
            Assert.That(totalCheckedProperties, Is.GreaterThan(0), 
                "Expected to check at least some properties, but none were found. This may indicate a test setup issue.");
        }

        /// <summary>
        /// Validates localization for a single assembly.
        /// </summary>
        private (List<string> errors, int checkedProperties, Dictionary<string, List<string>> testedClasses, int typesScanned) ValidateAssemblyLocalization(Assembly assembly)
        {
            var errors = new List<string>();
            var checkedProperties = 0;
            var testedClassesAndProperties = new Dictionary<string, List<string>>();

            // Find all types that have LocalizedPropertyResources attribute
            var typesWithLocalization = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<LocalizedPropertyResourcesAttribute>() != null)
                .Where(t => !t.IsAbstract || t.IsInterface)
                .Where(t => !t.IsEnum) // Enums are handled separately
                .OrderBy(t => t.FullName)
                .ToList();

            foreach (var type in typesWithLocalization)
            {
                var resourceAttr = type.GetCustomAttribute<LocalizedPropertyResourcesAttribute>();
                if (resourceAttr == null)
                {
                    continue;
                }

                var resourceManager = GetResourceManager(resourceAttr.ResourcesName, assembly);
                if (resourceManager == null)
                {
                    errors.Add($"Could not load resource manager for {resourceAttr.ResourcesName} (used by {type.Name})");
                    continue;
                }

                // Get properties that would be localized
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var testedPropertiesForType = new List<string>();

                foreach (var property in properties)
                {
                    // Skip common infrastructure properties that don't need localization
                    if (IsInfrastructureProperty(property.Name))
                    {
                        continue;
                    }

                    // Check for explicit display name/description key attributes
                    var explicitNameKeyAttr = property.GetCustomAttribute<DisplayNameKeyAttribute>();
                    var explicitDescKeyAttr = property.GetCustomAttribute<DisplayDescriptionKeyAttribute>();

                    // If type doesn't use default keys and has no explicit keys, skip this property
                    if (!resourceAttr.UseDefaultKeys && explicitNameKeyAttr == null && explicitDescKeyAttr == null)
                    {
                        continue;
                    }

                    checkedProperties++;
                    testedPropertiesForType.Add(property.Name);

                    // Default key pattern: {TypeName}_{PropertyName}Name and {TypeName}_{PropertyName}Desc
                    var typeName = type.Name;

                    // Validate display name key
                    if (explicitNameKeyAttr != null)
                    {
                        var nameValue = resourceManager.GetString(explicitNameKeyAttr.Key);
                        if (string.IsNullOrEmpty(nameValue))
                        {
                            errors.Add($"{type.Name}.{property.Name}: Missing display name string '{explicitNameKeyAttr.Key}' (explicit key) in {resourceAttr.ResourcesName}");
                        }
                    }
                    else if (resourceAttr.UseDefaultKeys)
                    {
                        var nameKey = $"{typeName}_{property.Name}Name";
                        var nameValue = resourceManager.GetString(nameKey);
                        if (string.IsNullOrEmpty(nameValue))
                        {
                            errors.Add($"{type.Name}.{property.Name}: Missing display name string '{nameKey}' in {resourceAttr.ResourcesName}");
                        }
                    }

                    // Validate description key
                    if (explicitDescKeyAttr != null)
                    {
                        var descValue = resourceManager.GetString(explicitDescKeyAttr.Key);
                        if (string.IsNullOrEmpty(descValue))
                        {
                            errors.Add($"{type.Name}.{property.Name}: Missing description string '{explicitDescKeyAttr.Key}' (explicit key) in {resourceAttr.ResourcesName}");
                        }
                    }
                    else if (resourceAttr.UseDefaultKeys)
                    {
                        var descKey = $"{typeName}_{property.Name}Desc";
                        var descValue = resourceManager.GetString(descKey);
                        if (string.IsNullOrEmpty(descValue))
                        {
                            errors.Add($"{type.Name}.{property.Name}: Missing description string '{descKey}' in {resourceAttr.ResourcesName}");
                        }
                    }
                }

                if (testedPropertiesForType.Count > 0)
                {
                    testedClassesAndProperties[type.FullName] = testedPropertiesForType;
                }
            }

            return (errors, checkedProperties, testedClassesAndProperties, typesWithLocalization.Count);
        }

        /// <summary>
        /// Verifies that all types with LocalizedPropertyResources attribute have valid
        /// Name and Description strings for each property that uses the default key pattern.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void SmoTypes_WithLocalizableTypeConverter_HaveValidPropertyNameAndDescriptionStrings()
        {
            var smoAssembly = typeof(SmoServer).Assembly;
            var (errors, checkedProperties, testedClassesAndProperties, typesScanned) = ValidateAssemblyLocalization(smoAssembly);


            // Generate and attach the tested classes and properties file
            AttachTestedClassesReport(testedClassesAndProperties, checkedProperties, typesScanned, errors);

            // Fail the test if there are missing localization strings
            if (errors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Found {errors.Count} missing localization strings (checked {checkedProperties} properties across {typesScanned} types):");
                sb.AppendLine();
                foreach (var error in errors.Take(50))
                {
                    sb.AppendLine($"  - {error}");
                }
                if (errors.Count > 50)
                {
                    sb.AppendLine($"  ... and {errors.Count - 50} more errors");
                }

                Assert.Fail(sb.ToString());
            }

            Assert.That(checkedProperties, Is.GreaterThan(0), 
                "Expected to check at least some properties, but none were found. This may indicate a test setup issue.");
        }


        /// <summary>
        /// Generates a text file report of all tested classes and properties and attaches it to the test results.
        /// </summary>
        private void AttachTestedClassesReport(Dictionary<string, List<string>> testedClassesAndProperties, int totalProperties, int totalTypes, List<string> missingStrings = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=============================================================================");
            sb.AppendLine("SMO Localizable Property Resources Test - Tested Classes and Properties");
            sb.AppendLine("=============================================================================");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.UtcNow:o}");
            sb.AppendLine($"Total Types Scanned: {totalTypes}");
            sb.AppendLine($"Total Properties Tested: {totalProperties}");
            sb.AppendLine($"Types with Tested Properties: {testedClassesAndProperties.Count}");
            if (missingStrings != null && missingStrings.Count > 0)
            {
                sb.AppendLine($"Missing Localization Strings: {missingStrings.Count}");
            }
            sb.AppendLine();
            sb.AppendLine("=============================================================================");
            sb.AppendLine();

            // Report missing strings first if any
            if (missingStrings != null && missingStrings.Count > 0)
            {
                sb.AppendLine("MISSING LOCALIZATION STRINGS:");
                sb.AppendLine("-----------------------------");
                foreach (var missing in missingStrings.OrderBy(s => s))
                {
                    sb.AppendLine($"  - {missing}");
                }
                sb.AppendLine();
                sb.AppendLine("=============================================================================");
                sb.AppendLine();
            }

            sb.AppendLine("TESTED CLASSES AND PROPERTIES:");
            sb.AppendLine("------------------------------");
            sb.AppendLine();

            foreach (var kvp in testedClassesAndProperties.OrderBy(k => k.Key))
            {
                var typeName = kvp.Key;
                var properties = kvp.Value;

                sb.AppendLine($"Class: {typeName}");
                sb.AppendLine($"  Properties ({properties.Count}):");
                foreach (var prop in properties.OrderBy(p => p))
                {
                    sb.AppendLine($"    - {prop}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("=============================================================================");
            sb.AppendLine("End of Report");
            sb.AppendLine("=============================================================================");

            // Write to a temporary file and attach to test results
            var tempPath = Path.Combine(Path.GetTempPath(), $"LocalizablePropertyResourcesTest_TestedClasses_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(tempPath, sb.ToString());

            TestContext?.AddResultFile(tempPath);
        }

        /// <summary>
        /// Verifies that localization string keys follow the expected naming pattern.
        /// This catches typos like "Datababase_" or "SServer_".
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void LocalizableResources_StringKeys_FollowNamingConvention()
        {
            var smoAssembly = typeof(SmoServer).Assembly;
            var errors = new List<string>();

            // Get the main LocalizableResources
            var resourceManager = GetResourceManager("Microsoft.SqlServer.Management.Smo.LocalizableResources", smoAssembly);
            Assert.That(resourceManager, Is.Not.Null, "Could not load LocalizableResources");

            // Get all types that could have localized properties
            var knownTypeNames = smoAssembly.GetTypes()
                .Where(t => !t.IsAbstract || t.IsInterface)
                .Select(t => t.Name)
                .Concat(new[] { "NamedSmoObject", "ScriptSchemaObjectBase" }) // Base types
                .Distinct()
                .ToHashSet(System.StringComparer.OrdinalIgnoreCase);

            // Use reflection to get the resource set
            var resourceSet = resourceManager.GetResourceSet(
                System.Globalization.CultureInfo.InvariantCulture, 
                createIfNotExists: true, 
                tryParents: false);

            if (resourceSet != null)
            {
                foreach (System.Collections.DictionaryEntry entry in resourceSet)
                {
                    var key = entry.Key?.ToString();
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    // Check if key follows pattern {TypeName}_{PropertyName}{Suffix}
                    var underscoreIndex = key.IndexOf('_');
                    if (underscoreIndex > 0)
                    {
                        var typePart = key.Substring(0, underscoreIndex);

                        // Check if the type part looks like a typo of a known type
                        if (!knownTypeNames.Contains(typePart))
                        {
                            // Check for common typo patterns
                            var possibleCorrection = FindSimilarTypeName(typePart, knownTypeNames);
                            if (possibleCorrection != null)
                            {
                                errors.Add($"Possible typo in string key '{key}': '{typePart}' might be '{possibleCorrection}'");
                            }
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Found {errors.Count} possible typos in localization string keys:");
                sb.AppendLine();
                foreach (var error in errors)
                {
                    sb.AppendLine($"  - {error}");
                }

                Assert.Fail(sb.ToString());
            }
        }

        /// <summary>
        /// Verifies that enums with LocalizedPropertyResources attribute have valid DisplayNameKey strings
        /// for each enum value. Scans all assemblies.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void AllEnums_WithDisplayNameKey_HaveValidResourceStrings()
        {
            var allErrors = new List<string>();
            var totalCheckedEnumValues = 0;
            var totalEnumsScanned = 0;

            foreach (var (assembly, assemblyName) in AssembliesToScan)
            {
                var (errors, checkedEnumValues, enumsScanned) = ValidateEnumLocalization(assembly);
                allErrors.AddRange(errors.Select(e => $"[{assemblyName}] {e}"));
                totalCheckedEnumValues += checkedEnumValues;
                totalEnumsScanned += enumsScanned;
            }

            if (allErrors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Found {allErrors.Count} missing enum display name strings (checked {totalCheckedEnumValues} values across {totalEnumsScanned} enums in {AssembliesToScan.Length} assemblies):");
                sb.AppendLine();
                foreach (var error in allErrors)
                {
                    sb.AppendLine($"  - {error}");
                }

                Assert.Fail(sb.ToString());
            }

            TestContext?.WriteLine($"Checked {totalCheckedEnumValues} enum values across {totalEnumsScanned} enums in {AssembliesToScan.Length} assemblies. All display name strings found.");
        }

        /// <summary>
        /// Verifies that enums with LocalizedPropertyResources attribute have valid DisplayNameKey strings
        /// for each enum value. SMO-only version for backwards compatibility.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void SmoEnums_WithDisplayNameKey_HaveValidResourceStrings()
        {
            var smoAssembly = typeof(SmoServer).Assembly;
            var (errors, checkedEnumValues, enumsScanned) = ValidateEnumLocalization(smoAssembly);

            if (errors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Found {errors.Count} missing enum display name strings (checked {checkedEnumValues} values across {enumsScanned} enums):");
                sb.AppendLine();
                foreach (var error in errors)
                {
                    sb.AppendLine($"  - {error}");
                }

                Assert.Fail(sb.ToString());
            }

            TestContext?.WriteLine($"Checked {checkedEnumValues} enum values across {enumsScanned} enums. All display name strings found.");
        }

        /// <summary>
        /// Validates enum localization for a single assembly.
        /// </summary>
        private (List<string> errors, int checkedEnumValues, int enumsScanned) ValidateEnumLocalization(Assembly assembly)
        {
            var errors = new List<string>();
            var checkedEnumValues = 0;

            // Find all enum types that have LocalizedPropertyResources attribute
            var enumsWithLocalization = assembly.GetTypes()
                .Where(t => t.IsEnum)
                .Where(t => t.GetCustomAttribute<LocalizedPropertyResourcesAttribute>() != null)
                .OrderBy(t => t.FullName)
                .ToList();

            foreach (var enumType in enumsWithLocalization)
            {
                var resourceAttr = enumType.GetCustomAttribute<LocalizedPropertyResourcesAttribute>();
                if (resourceAttr == null)
                {
                    continue;
                }

                var resourceManager = GetResourceManager(resourceAttr.ResourcesName, assembly);
                if (resourceManager == null)
                {
                    errors.Add($"Could not load resource manager for {resourceAttr.ResourcesName} (used by {enumType.Name})");
                    continue;
                }

                // Check each enum value for DisplayNameKey attribute
                foreach (var fieldName in Enum.GetNames(enumType))
                {
                    var fieldInfo = enumType.GetField(fieldName);
                    var displayNameAttr = fieldInfo?.GetCustomAttribute<DisplayNameKeyAttribute>();

                    if (displayNameAttr != null)
                    {
                        checkedEnumValues++;
                        var resourceValue = resourceManager.GetString(displayNameAttr.Key);
                        if (string.IsNullOrEmpty(resourceValue))
                        {
                            errors.Add($"{enumType.Name}.{fieldName}: Missing display name string '{displayNameAttr.Key}' in {resourceAttr.ResourcesName}");
                        }
                    }
                }
            }

            return (errors, checkedEnumValues, enumsWithLocalization.Count);
        }

        private static ResourceManager GetResourceManager(string resourcesName, Assembly assembly)
        {
            if (ResourceManagerCache.TryGetValue(resourcesName, out var cached))
            {
                return cached;
            }

            try
            {
                var rm = new ResourceManager(resourcesName, assembly);
                // Force load to validate
                _ = rm.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, true, false);
                ResourceManagerCache[resourcesName] = rm;
                return rm;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Common infrastructure properties that don't need localization strings.
        /// These are typically inherited from base classes or are internal infrastructure.
        /// </summary>
        private static readonly HashSet<string> InfrastructurePropertyNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Parent",
            "Events",
            "ExtendedProperties",
            "State",
            "Urn",
            "Properties",
            "UserData",
            "Metadata",
            "AbstractIdentityKey",
            "IdentityKey",
            "ExecutionManager",
            "ObjectInSpace",
            "IsDesignMode"
        };

        private static bool IsInfrastructureProperty(string propertyName)
            => InfrastructurePropertyNames.Contains(propertyName);

        private static string FindSimilarTypeName(string typePart, HashSet<string> knownTypeNames)
        {
            // Check for common typo patterns
            foreach (var knownName in knownTypeNames)
            {
                // Check for doubled letters (e.g., "Datababase" vs "Database")
                if (Math.Abs(typePart.Length - knownName.Length) <= 2)
                {
                    var distance = ComputeLevenshteinDistance(typePart, knownName);
                    if (distance > 0 && distance <= 2)
                    {
                        return knownName;
                    }
                }

                // Check for prefix typos (e.g., "SServer" vs "Server")
                if (typePart.Length > knownName.Length && 
                    typePart.EndsWith(knownName, StringComparison.OrdinalIgnoreCase))
                {
                    return knownName;
                }
            }

            return null;
        }

        private static int ComputeLevenshteinDistance(string s1, string s2)
        {
            // See https://en.wikipedia.org/wiki/Levenshtein_distance
            var n = s1.Length;
            var m = s2.Length;
            var d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (var i = 0; i <= n; i++) d[i, 0] = i;
            for (var j = 0; j <= m; j++) d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}
