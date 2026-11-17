//     Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Test the functionality of the PermissionDecoder class in SqlEnum
    /// </summary>
    [TestClass]
    public class PermissionDecoderTests : UnitTestBase
    {
        #region PermissionCodeToPermissionName Tests

        /// <summary>
        /// Tests that PermissionCodeToPermissionName correctly throws an exception for invalid codes
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void PermissionCodeToPermissionName_Throws_Exception_For_Invalid_Permission_Code()
        {
            PermissionCodeToPermissionName_Throws_Exception_For_Invalid_Permission_Code_Helper<DatabasePermissionSetValue>();
            PermissionCodeToPermissionName_Throws_Exception_For_Invalid_Permission_Code_Helper<ServerPermissionSetValue>();
            PermissionCodeToPermissionName_Throws_Exception_For_Invalid_Permission_Code_Helper<ObjectPermissionSetValue>();
        }

        /// <summary>
        /// Tests that PermissionCodeToPermissionName returns the correct name for every value in the Permission enums
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void PermissionCodeToPermissionName_Returns_Correct_Values_For_Valid_Permission_Code()
        {
            PermissionCodeToPermissionName_Returns_Correct_Values_For_Valid_Permission_Code_Helper<DatabasePermissionSetValue>();
            PermissionCodeToPermissionName_Returns_Correct_Values_For_Valid_Permission_Code_Helper<ServerPermissionSetValue>();
            PermissionCodeToPermissionName_Returns_Correct_Values_For_Valid_Permission_Code_Helper<ObjectPermissionSetValue>();
        }

        #endregion PermissionCodeToPermissionName Tests

        #region PermissionCodeToPermissionName Test Helpers

        /// <summary>
        /// Validates that passing an invalid Permission Code to PermissionCodeToPermissionName results in an InvalidOperationException being thrown
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void PermissionCodeToPermissionName_Throws_Exception_For_Invalid_Permission_Code_Helper<T>() where T : struct, IConvertible
        {
            Assert.Catch<InvalidOperationException>(() => PermissionDecode.PermissionCodeToPermissionName<T>(-1), "Didn't get expected InvalidOperationException from invalid Permission Code -1 ({0})", typeof(T));

            //Enum values are always expected to be sequential and so the highest value should be Length - 1
            int numElements = Enum.GetNames(typeof(T)).Length;
            Assert.Catch<InvalidOperationException>(() => PermissionDecode.PermissionCodeToPermissionName<T>(numElements), "Didn't get expected InvalidOperationException from invalid Permission Code {0} ({1})", numElements, typeof(T));
        }


        /// <summary>
        /// Validates that all values in the specified enum will have the correct Permission Name returned from PermissionCodeToPermissionName
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void PermissionCodeToPermissionName_Returns_Correct_Values_For_Valid_Permission_Code_Helper<T>() where T : struct, IConvertible
        {
            foreach (T val in Enum.GetValues(typeof(T)))
            {
                string actualNameValue = PermissionDecode.PermissionCodeToPermissionName<T>(Convert.ToInt32(val));
                Assert.That(actualNameValue, Is.EqualTo(val.PermissionName()), "Permission name wrong for {0}", val);
            }
        }

        #endregion PermissionCodeToPermissionName Test Helpers

        #region PermissionCodeToPermissionType Tests

        /// <summary>
        /// Tests that PermissionCodeToPermissionName correctly throws an exception for invalid codes
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void PermissionCodeToPermissionType_Throws_Exception_For_Invalid_Permission_Code()
        {
            PermissionCodeToPermissionType_Throws_Exception_For_Invalid_Permission_Code_Base<DatabasePermissionSetValue>();
            PermissionCodeToPermissionType_Throws_Exception_For_Invalid_Permission_Code_Base<ServerPermissionSetValue>();
            PermissionCodeToPermissionType_Throws_Exception_For_Invalid_Permission_Code_Base<ObjectPermissionSetValue>();
        }

        /// <summary>
        /// Tests that PermissionCodeToPermissionType returns the correct name for every value in the Permission enums
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void PermissionCodeToPermissionType_Returns_Correct_Values_For_Valid_Permission_Code()
        {
            PermissionCodeToPermissionType_Returns_Correct_Values_For_Valid_Permission_Code_Helper<DatabasePermissionSetValue>();
            PermissionCodeToPermissionType_Returns_Correct_Values_For_Valid_Permission_Code_Helper<ServerPermissionSetValue>();
            PermissionCodeToPermissionType_Returns_Correct_Values_For_Valid_Permission_Code_Helper<ObjectPermissionSetValue>();
        }

        #endregion PermissionCodeToPermissionType Tests

        #region PermissionCodeToPermissionType Test Helpers

        /// <summary>
        /// Validates that passing an invalid Permission Code to PermissionCodeToPermissionName results in an InvalidOperationException being thrown
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void PermissionCodeToPermissionType_Throws_Exception_For_Invalid_Permission_Code_Base<T>() where T : struct, IConvertible
        {
            Assert.Throws<InvalidOperationException>(() => PermissionDecode.PermissionCodeToPermissionType<T>(-1), "Didn't get expected InvalidOperationException from invalid Permission Code -1 ({0})", typeof(T));

            //Enum values are always expected to be sequential and so the highest value should be Length - 1
            int numElements = Enum.GetNames(typeof(T)).Length;
            Assert.Throws<InvalidOperationException>(() => PermissionDecode.PermissionCodeToPermissionType<T>(numElements), "Didn't get expected InvalidOperationException from invalid Permission Code {0} ({1})", numElements, typeof(T));
        }

        /// <summary>
        /// Validates that all values in the specified enum will have the correct Permission Name returned from PermissionCodeToPermissionName
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void PermissionCodeToPermissionType_Returns_Correct_Values_For_Valid_Permission_Code_Helper<T>() where T : struct, IConvertible
        {
            foreach (T val in Enum.GetValues(typeof(T)))
            {
                string actualNameValue = PermissionDecode.PermissionCodeToPermissionType<T>(Convert.ToInt32(val));
                Assert.That(actualNameValue, Is.EqualTo(val.PermissionType()), "Permission type wrong for {0}", val);
            }
        }

        #endregion PermissionCodeToPermissionType Test Helpers

        #region ToPermissionSetValueEnum Tests

        /// <summary>
        /// Tests that ToPermissionSetValueEnum will return the correct enum value for all types in the Permission enums
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ToPermissionSetValueEnum_Returns_Correct_Values()
        {
            ToPermissionSetValueEnum_Returns_Correct_Values_Helper<DatabasePermissionSetValue>();
            ToPermissionSetValueEnum_Returns_Correct_Values_Helper<ServerPermissionSetValue>();
            ToPermissionSetValueEnum_Returns_Correct_Values_Helper<ObjectPermissionSetValue>();
        }

        /// <summary>
        /// Tests that ToPermissionSetValueEnum will throw an exception for invalid permission type names
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ToPermissionSetValueEnum_Throws_Exception_On_Invalid_Value()
        {
            Assert.Throws<ArgumentException>(() => PermissionDecode.ToPermissionSetValueEnum<DatabasePermissionSetValue>(Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => PermissionDecode.ToPermissionSetValueEnum<ServerPermissionSetValue>(Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => PermissionDecode.ToPermissionSetValueEnum<ObjectPermissionSetValue>(Guid.NewGuid().ToString()));
        }

        #endregion ToPermissionSetValueEnum Tests

        #region ToPermissionSetValueEnum Test Helpers

        /// <summary>
        /// Validates that ToPermissionSetValueEnum will return the correct enum value for all type names in the specified enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void ToPermissionSetValueEnum_Returns_Correct_Values_Helper<T>() where T : struct, IConvertible
        {
            foreach (T val in Enum.GetValues(typeof (T)))
            {
                T actualValue = PermissionDecode.ToPermissionSetValueEnum<T>(val.PermissionType());
                Assert.That(actualValue, Is.EqualTo(val), "Wrong value for permission type {0}", val.PermissionType());
            }
        }

        #endregion ToPermissionSetValueEnum Test Helpers

        #region PermissionType and PermissionName Attribute Tests

        /// <summary>
        /// Tests that all values in the Permission Enums have the PermissionType attribute set
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void PermissionEnum_Values_All_Have_PermissionType_Attribute()
        {
            Enum_Values_Have_Attribute<DatabasePermissionSetValue, PermissionTypeAttribute>();
            Enum_Values_Have_Attribute<ServerPermissionSetValue, PermissionTypeAttribute>();
            Enum_Values_Have_Attribute<ObjectPermissionSetValue, PermissionTypeAttribute>();
        }

        /// <summary>
        /// Tests that all values in the Permission Enums have the PermissionName attribute set
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void PermissionEnum_Values_All_Have_PermissionName_Attribute()
        {
            Enum_Values_Have_Attribute<DatabasePermissionSetValue, PermissionNameAttribute>();
            Enum_Values_Have_Attribute<ServerPermissionSetValue, PermissionNameAttribute>();
            Enum_Values_Have_Attribute<ObjectPermissionSetValue, PermissionNameAttribute>();
        }

        #endregion PermissionType and PermissionName Attribute Tests

        #region PermissionType and PermissionName Attribute Tests

        /// <summary>
        /// Validates that all enum values for the specified enum type have an attribute with the specified type
        /// </summary>
        /// <typeparam name="E">The Enum type</typeparam>
        /// <typeparam name="A">The attribute type</typeparam>
        private void Enum_Values_Have_Attribute<E, A>() 
            where E : struct, IConvertible
            where A : StringValueAttribute
        {
            foreach (E val in Enum.GetValues(typeof(E)))
            {
                var attr = val.GetType().GetMember(val.ToString())[0].GetCustomAttributes(typeof(A), false).FirstOrDefault() as A;
                Assert.IsTrue(attr != null && !string.IsNullOrEmpty(attr.Value), "Enum Value {0} does not have a valid {1}", val, typeof(A));
            }
        }

        #endregion PermissionType and PermissionName Attribute Tests
    }
}
