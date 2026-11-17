// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests language literal encoding in user-defined message scripts.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class UserDefinedMessageSqlLiteralTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("us_english", "us_english", "all")]
        [DataRow("O'Brien", "O''Brien", "O''Brien")]
        [DataRow("\u540d'Language]", "\u540d''Language]", "\u540d''Language]")]
        public void UserDefinedMessage_ScriptAlterAndDrop_EscapeLanguage(
            string language, string expectedLanguage, string expectedDropLanguage)
        {
            var server = ServerTests.GetDesignModeServer(17);
            var message = new UserDefinedMessage(server, 50001, language, 16, "Test message", false);
            var preferences = new ScriptingPreferences(message);
            var alterScripts = new StringCollection();
            var dropScripts = new StringCollection();

            message.ScriptAlter(alterScripts, preferences);
            message.ScriptDrop(dropScripts, preferences);

            Assert.That(alterScripts, Is.EqualTo(new[]
                {
                    $"EXEC master.dbo.sp_addmessage  @msgnum=50001, @lang=N'{expectedLanguage}', " +
                    $"@severity=16, @msgtext=N'Test message', {System.Environment.NewLine}\t\t@with_log=false, @replace='REPLACE'"
                }), "ALTER must escape the language independently of the message text.");
            Assert.That(dropScripts, Is.EqualTo(new[]
                {
                    $"EXEC master.dbo.sp_dropmessage @msgnum=50001, @lang=N'{expectedDropLanguage}'"
                }), "DROP must escape the language and preserve us_english-to-all handling.");
        }
    }
}
