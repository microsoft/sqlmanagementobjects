// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SfcUnitTests
{
    /// <summary>
    /// We can't readily unit test the implementation because SFC has a hard-coded list of domains and
    /// associated types. It's non-trivial amount of work to implement an SFC domain even for a unit test
    /// so we will defer that.
    /// </summary>
    [TestClass]
    public class SfcSerializerTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void SfcSerializer_Serialize_handles_invalid_input()
        {
            Assert.Multiple(() =>
            {
                var serializer = new SfcSerializer();
                Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null), "null");
                Assert.Throws<SfcUnregisteredXmlTypeException>(() => serializer.Serialize(""), "a string");
            });
        }

    }
}