// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using DataTypeSpec = Microsoft.SqlServer.Management.SqlParser.Metadata.DataTypeSpec;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests for SMO metadata provider data type lookups.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class SmoMetadataProviderTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow(1, "float16", "float16")]
        [DataRow(1536, "float16", "float16")]
        [DataRow(1998, "float32", "float32")]
        [DataRow(1536, "", null)]
        [DataRow(1536, null, null)]
        public void Find_Vector_PreservesDimensionsAndBaseType(int dimensions, string baseType, string expectedIdentifier)
        {
            var dataType = SmoSystemDataTypeLookup.Instance.Find(DataType.Vector(dimensions, baseType));

            Assert.That(dataType, Is.Not.Null, "Vector lookup should return a system data type.");
            Assert.Multiple(() =>
            {
                Assert.That(dataType.TypeSpec, Is.SameAs(DataTypeSpec.Vector),
                    "Vector lookup should preserve the vector type specification.");
                Assert.That(dataType.Length, Is.EqualTo(dimensions),
                    "Vector metadata length should match the requested dimensions.");
                Assert.That(dataType.Identifier1, Is.EqualTo(expectedIdentifier),
                    "Vector metadata should preserve an explicit base type and omit an unspecified base type.");
            });
        }

        [TestMethod]
        public void Find_NonVectorTypes_PreservesExistingArguments()
        {
            var lookup = SmoSystemDataTypeLookup.Instance;
            var characterType = lookup.Find(DataType.NVarChar(128));
            var decimalType = lookup.Find(DataType.Decimal(scale: 2, precision: 10));
            var timeType = lookup.Find(DataType.Time(3));
            var integerType = lookup.Find(DataType.Int);

            Assert.Multiple(() =>
            {
                Assert.That(characterType.Length, Is.EqualTo(128), "Character length should remain unchanged.");
                Assert.That(decimalType.NumericPrecision, Is.EqualTo(10), "Decimal precision should remain unchanged.");
                Assert.That(decimalType.NumericScale, Is.EqualTo(2), "Decimal scale should remain unchanged.");
                Assert.That(timeType.NumericScale, Is.EqualTo(3), "Time scale should remain unchanged.");
                Assert.That(integerType.TypeSpec, Is.SameAs(DataTypeSpec.Int),
                    "Parameterless integer lookup should remain unchanged.");
                Assert.That(characterType.Identifier1, Is.Null, "Non-vector lookup should not add an identifier.");
            });
        }
    }
}
