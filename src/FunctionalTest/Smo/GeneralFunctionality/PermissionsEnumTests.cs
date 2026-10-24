// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    [UnsupportedFeature(SqlFeature.Fabric)]
    public class PermissionsEnumTests : SqlTestBase
    {
        [TestMethod]
        public void PermEnum_DatabasePermissionSetValue_enum_is_complete()
        {
            ExecuteTest(() =>
            {
                CompareEnumToServerPermissions(typeof(DatabasePermissionSetValue), @"select type, permission_name from sys.fn_builtin_permissions('DATABASE')");
            });
        }

        [TestMethod]
        public void PermEnum_ServerPermissionSetValue_enum_is_complete()
        {
            ExecuteTest(() =>
            {
                CompareEnumToServerPermissions(typeof(ServerPermissionSetValue), @"select type, permission_name from sys.fn_builtin_permissions('SERVER')");
            });
        }

        [TestMethod]
        public void PermEnum_ObjectPermissionSetValue_enum_is_complete()
        {
            ExecuteTest(() =>
            {
                CompareEnumToServerPermissions(typeof(ObjectPermissionSetValue), @"select type, permission_name from sys.fn_builtin_permissions(DEFAULT) where class_desc <> 'SERVER' and class_desc <> 'DATABASE'");
            });
        }

        /// <summary>
        /// Simple test that verifies that the permission options are defined in the permission enum and permission set classes.
        /// This is primarily used for code coverage purposes - the "*_enum_is_complete" tests above are more comprehensive.
        /// </summary>
        /// <param name="enumType">The permission set value type</param>
        /// <param name="permissionType">The permission object type</param>
        /// <param name="permissionSetType">The permission set object type</param>
        /// <param name="valuesToSkip">What values to skip</param>
        [TestMethod]
        [DisconnectedTest]
        [DataRow(typeof(DatabasePermissionSetValue), typeof(DatabasePermission), typeof(DatabasePermissionSet), new string[] { })]
        [DataRow(typeof(ServerPermissionSetValue), typeof(ServerPermission), typeof(ServerPermissionSet), new string[] { nameof(ServerPermissionSetValue.AlterAnyExternalDataSource), nameof(ServerPermissionSetValue.AlterAnyExternalFileFormat) })]
        [DataRow(typeof(ObjectPermissionSetValue), typeof(ObjectPermission), typeof(ObjectPermissionSet), new string[] { })]
        public void PermEnum_PermissionOptions_AreDefined(Type enumType, Type permissionType, Type permissionSetType, IEnumerable<string> valuesToSkip)
        {
            foreach (var permissionEnumName in Enum.GetNames(enumType))
            {
                if (valuesToSkip.Contains(permissionEnumName))
                {
                    // If values are removed then they will sometimes still be kept in the enum but have their
                    // corresponding permission and permission set properties removed.
                    continue;
                }
                var permissionObj = permissionType.GetProperty(permissionEnumName, BindingFlags.Static | BindingFlags.Public);
                Assert.That(permissionObj, Is.Not.Null, $"{permissionEnumName} is missing from {permissionType.Name}.");
                Assert.That(permissionObj.GetValue(null), Is.Not.Null, $"{permissionEnumName} is null in {permissionType.Name}.");
                var setObj = Activator.CreateInstance(permissionSetType);
                var setObjProperty = permissionSetType.GetProperty(permissionEnumName);
                Assert.That(setObjProperty, Is.Not.Null, $"{permissionEnumName} is null in {permissionSetType.Name}.");
                Assert.That(setObjProperty.GetValue(setObj), Is.Not.Null, $"{permissionEnumName} is null in {permissionSetType.Name}.");
                Assert.DoesNotThrow(() => setObjProperty.SetValue(setObj, null), $"Setting {permissionEnumName} in {permissionSetType.Name} should not throw");
            }
        }

        private void CompareEnumToServerPermissions(Type enumType, string permissionQuery)
        {
            var permissionTypes = GetAttributeValues<PermissionTypeAttribute>(enumType);
            var permissionNames = GetAttributeValues<PermissionNameAttribute>(enumType);
            var permissionList = ServerContext.ConnectionContext.ExecuteWithResults(permissionQuery).Tables[0].Rows.Cast<DataRow>();
            var maxValue = Enum.GetValues(enumType).Cast<int>().Max();
            var textInfo = new CultureInfo("en-US", false).TextInfo;
            var typeName = enumType.Name.Substring(0, enumType.Name.IndexOf("PermissionSetValue"));

            // codegen stringbuilders for missing permissions
            var missingEnumValues = new StringBuilder();
            var missingPermissionDefinitions = new StringBuilder($"{Environment.NewLine}{Environment.NewLine}Add the following to permissionOptions.cs in the {typeName}Permission class, in alphabetical order:{Environment.NewLine}{Environment.NewLine}");
            var missingPermissionSetDefinitions = new StringBuilder($"{Environment.NewLine}{Environment.NewLine}Add the following to permissionOptions.cs in the {typeName}PermissionSet class, in alphabetical order:{Environment.NewLine}{Environment.NewLine}");

            foreach (var row in permissionList)
            {
                var name = row["permission_name"].ToString().Trim();
                var type = row["type"].ToString().Trim();
                if (!permissionNames.Contains(name) || !permissionTypes.Contains(type))
                {
                    var enumName = textInfo.ToTitleCase(name.ToLower()).Replace(" ", "");

                    missingEnumValues.Append(
                        $"{Environment.NewLine}[PermissionType(\"{type}\")]{Environment.NewLine}" +
                        $"[PermissionName(\"{name}\")]{Environment.NewLine}" +
                        $"{enumName} = {++maxValue},{Environment.NewLine}");
                    missingPermissionDefinitions.Append(
                        $"    public static {typeName}Permission {enumName}{Environment.NewLine}" +
                        $"    {{{Environment.NewLine}"+
                        $"        get {{ return new {typeName}Permission({enumType.Name}.{enumName}); }}{Environment.NewLine}" +
                        $"    }}");
                    missingPermissionSetDefinitions.Append(
                        $"    public bool {enumName}{Environment.NewLine}" +
                        $"    {{{Environment.NewLine}"+
                        $"        get {{ return this.Storage[(int){enumType.Name}.{enumName}]; }}{Environment.NewLine}"+
                        $"        set {{ this.Storage[(int){enumType.Name}.{enumName}] = value; }}{Environment.NewLine}" +
                        $"    }}");
                }
            }

            Assert.That(missingEnumValues.ToString(), Is.Empty, $"{enumType.Name} is incomplete. Add the missing values in permenum.cs and update permissionOptions.cs accordingly: {missingPermissionDefinitions}{missingPermissionSetDefinitions}");
        }
        /// <summary>
        /// Returns the set of string attribute values associated with the given enumeration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumType"></param>
        /// <returns></returns>
        static internal IEnumerable<string> GetAttributeValues<T>(Type enumType) where T: StringValueAttribute {
            return new HashSet<string>(Enum.GetNames(enumType).Select(n => enumType.GetMember(n).Single().GetCustomAttributes(typeof(T), false).Cast<T>().Single().Value));
        }
    }
}
