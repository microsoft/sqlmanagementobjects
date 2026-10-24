// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.SqlServer.Test.SMO.ScriptingTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert=NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    [TestClass]
    public class LanguageTests : SmoTestBase
    {
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        public void Languages_ItemById_uses_LocaleId()
        {
            ExecuteTest(() =>
            {
                var nullLanguage = ServerContext.Languages.ItemById(0);
                var english = ServerContext.Languages.ItemById(0x409);
                Assert.Multiple(() =>
                {
                    Assert.That(nullLanguage, Is.Null, "0 as item id");
                    Assert.That(english, Is.Not.Null, "0x409 as item id");
                    Assert.That(english.LocaleID, Is.EqualTo(0x409), "LocaleID of English");
                });
            });
        }
    }
}
