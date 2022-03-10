// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.ConnectionInfoUnitTests
{
    /// <summary>
    /// Tests that validate the functionality of the <see cref="CommonLocalizableEnumConverter"/>
    /// </summary>
    [TestClass]
    public class LocalizableTypeConverterTests
    {
        #region Private Members

        private static readonly Type DatabaseEngineEditionType = typeof (DatabaseEngineEdition);
        private static readonly Type DatabaseEngineTypeType = typeof(DatabaseEngineType);

        #endregion Private Members

        #region Tests

        /// <summary>
        /// Validates that the DatabaseEngineEdition enum has a TypeConverter defined that can
        /// convert each enum value to its corresponding localized string and back.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TypeConverter_DatabaseEngineEdition_ConvertsEnumToStringAndBack()
        {
            ValidateTypeConverterForEnumConvertsToStringAndBackCorrectly(DatabaseEngineEditionType);
        }

        /// <summary>
        /// Validates that the DatabaseEngineType enum has a TypeConverter defined that can
        /// convert each enum value to its corresponding localized string and back.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TypeConverter_DatabaseEngineType_ConvertsEnumToStringAndBack()
        {
            ValidateTypeConverterForEnumConvertsToStringAndBackCorrectly(DatabaseEngineTypeType);
        }

        #endregion Tests

        #region Private Helpers

        /// <summary>
        /// Validates that the TypeConverter defined for an enum correctly converts the enum
        /// values to their localized string representations and back.
        /// </summary>
        /// <param name="type"></param>
        private static void ValidateTypeConverterForEnumConvertsToStringAndBackCorrectly(Type type)
        {
            ResourceManager resourceManager = GetResourceManager(type);
            foreach (var enumValue in Enum.GetValues(type))
            {
                //First get the type converter that will be used to convert the enum into its
                //localized string value
                TypeConverter typeConverter = TypeDescriptor.GetConverter(type);
                Assert.That(typeConverter, Is.Not.Null, "Type {0} did not have a TypeConverter defined", type.Name);

                FieldInfo fi = type.GetField(Enum.GetName(type, enumValue));

                string typeConverterString =
                    typeConverter.ConvertToString(enumValue);
                //Get the resource string we expect - note this might be the enum field name if no DisplayNameKeyAttribute is defined
                string resourceString = GetResourceStringFromDisplayNameKey(fi, resourceManager);
                Assert.That(typeConverterString, Is.EqualTo(resourceString));

                //Convert back to the enum value using the localized string value retrieved above
                var typeConverterEnumValue =
                    typeConverter.ConvertFromString(typeConverterString);
                Assert.That(typeConverterEnumValue, Is.EqualTo(enumValue));
            }
        }

        /// <summary>
        /// Gets the Resource Manager for the specified type, with a base name set to the value
        /// specified by the <see cref="CommonLocalizedPropertyResourcesAttribute"/> if that is defined.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static ResourceManager GetResourceManager(Type type)
        {
            var resourcesAttribute =
                type.GetCustomAttributes(true).OfType<CommonLocalizedPropertyResourcesAttribute>().FirstOrDefault();
            ResourceManager manager = resourcesAttribute != null
                ? new ResourceManager(resourcesAttribute.ResourcesName, type.Assembly)
                : new ResourceManager(type.Namespace, type.Assembly);

            return manager;
        }

        /// <summary>
        /// Gets the localized string for the specified field info using the <see cref="CommonDisplayNameKeyAttribute"/>
        /// if one exists. If not defaults to returning the field name.
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <param name="resourceManager"></param>
        /// <returns></returns>
        private static string GetResourceStringFromDisplayNameKey(FieldInfo fieldInfo, ResourceManager resourceManager)
        {
            CommonDisplayNameKeyAttribute displayNameKeyAttribute =
                fieldInfo.GetCustomAttributes(typeof (CommonDisplayNameKeyAttribute), true).Cast<CommonDisplayNameKeyAttribute>().FirstOrDefault();
            //We return the field name if the field doesn't have a DisplayNameKeyAttribute
            return displayNameKeyAttribute == null
                ? fieldInfo.Name
                : resourceManager.GetString(displayNameKeyAttribute.Key);
        }

        #endregion Private Helpers
    }
}
