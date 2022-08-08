// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
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
        private void CompareEnumToServerPermissions(Type enumType, string permissionQuery)
        {
            var permissionTypes = GetAttributeValues<PermissionTypeAttribute>(enumType);
            var permissionNames = GetAttributeValues<PermissionNameAttribute>(enumType);
            var permissionList = ServerContext.ConnectionContext.ExecuteWithResults(permissionQuery).Tables[0].Rows.Cast<DataRow>();
            var maxValue = Enum.GetValues(enumType).Cast<int>().Max();
            var textInfo = new CultureInfo("en-US", false).TextInfo;
            var missingValues = new StringBuilder();
            foreach (var row in permissionList)
            {
                var name = row["permission_name"].ToString().Trim();
                var type = row["type"].ToString().Trim();
                if (!permissionNames.Contains(name) || !permissionTypes.Contains(type))
                {
                    var enumName = new StringBuilder();
                    foreach (var s in name.Split(' '))
                    {
                        enumName.Append(textInfo.ToTitleCase(s.ToLowerInvariant()));
                    }
                    enumName.Append($" = {++maxValue},");
                    missingValues.Append($"{Environment.NewLine}[PermissionType(\"{type}\")]{Environment.NewLine}[PermissionName(\"{name}\")]{Environment.NewLine}{enumName}{Environment.NewLine}");
                }
            }
            Assert.That(missingValues.ToString(), Is.Empty, $"{ enumType.Name} is incomplete. Add the missing values.");
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
