// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.ComponentModel;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert=NUnit.Framework.Assert;
using NUnit.Framework;

namespace Microsoft.SqlServer.Test.SqlScriptPublishTests
{
    [TestClass]
    public class EnumTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void LocalizedEnumConverter_returns_proper_resource()
        {
            var converter = TypeDescriptor.GetConverter(typeof(ResultType));
            var resource = converter.ConvertToString(ResultType.InProgress);
            Assert.That(resource, Is.EqualTo("In progress"), "ResultType.InProgress");
            converter = TypeDescriptor.GetConverter(typeof(DatabaseObjectType));
            resource = converter.ConvertToString(DatabaseObjectType.UserDefinedFunction);
            Assert.That(resource, Is.EqualTo("User-Defined Functions"), "DatabaseObjectType.UserDefinedFunction");
        }
    }
}
