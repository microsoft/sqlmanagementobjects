// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert=NUnit.Framework.Assert;

namespace RegServerUnitTests
{
    [TestClass]
    public class RegistrationInfoTests
    {
        private const string InputXmlFormat = @"<?xml version='1.0' encoding='utf-8'?>
<RegisteredServers>
	<ServerType id='8c91a03d-f9b4-46c0-a305-b5dcc79ff907' name='Database Engine'>
		<Group name='Replication Monitor Registry Servers' description='Replication Monitor Registry Servers'>
			<Server name='myServer' description=''>
				<ConnectionInformation>
					<ServerType>8c91a03d-f9b4-46c0-a305-b5dcc79ff907</ServerType>
					<ServerName>myServer</ServerName>
					<DisplayName>myDisplay</DisplayName>
					<AuthenticationType>1</AuthenticationType>
					<UserName>sa</UserName>
					<Password>{0}</Password>
					<AdvancedOptions>
						<CONNECTION_TIMEOUT>15</CONNECTION_TIMEOUT>
						<EXEC_TIMEOUT>30</EXEC_TIMEOUT>
					</AdvancedOptions>
				</ConnectionInformation>
			</Server>
		</Group>
	</ServerType>
</RegisteredServers>";

        [ClassInitialize]
        public static void ClassInit(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext context)
        {
            RegistrationProvider.ProviderStoreCredentialVersion = "test";
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerInstanceRegistrationInfo_Import_preserves_encrypted_password_if_no_Windows_credential_exists()
        {
            var serverType = new Guid("8c91a03d-f9b4-46c0-a305-b5dcc79ff907");
            WindowsCredential.RemoveRegSvrCredential("myServer", 1, "sa", serverType, RegistrationProvider.ProviderStoreCredentialVersion);
            var fileName = Path.GetTempFileName();
            using (var file = File.CreateText(fileName))
            {
                file.Write(InputXmlFormat, DataProtection.ProtectData("myPassword"));
                file.Close();
            }
            try
            {
                var registrationInfo = RegistrationProvider.Import(fileName) as ServerTypeRegistrationInfo;
                var group = registrationInfo.Children[0];
                Assert.That(group, Is.InstanceOf(typeof(GroupRegistrationInfo)),
                    "Expected Group as first child");
                var server = ((GroupRegistrationInfo)group).Children[0] as ServerInstanceRegistrationInfo;
                Assert.That(server.ServerType, Is.EqualTo(serverType), "Server instance ServerType");
                Assert.That(server.ConnectionInfo.Password, Is.EqualTo("myPassword"), "Server instance password should be from the file");
                WindowsCredential.SetSqlRegSvrCredential("myServer", 1, "sa", serverType, "overridePassword".StringToSecureString(), RegistrationProvider.ProviderStoreCredentialVersion);
                registrationInfo = RegistrationProvider.Import(fileName) as ServerTypeRegistrationInfo;
                group = registrationInfo.Children[0];
                server = ((GroupRegistrationInfo)group).Children[0] as ServerInstanceRegistrationInfo;
                Assert.That(server.ConnectionInfo.Password, Is.EqualTo("overridePassword"), "Server instance password should be from the Windows credential manager");
            }
            finally
            {
                WindowsCredential.RemoveRegSvrCredential("myServer", 1, "sa", serverType, RegistrationProvider.ProviderStoreCredentialVersion);
                File.Delete(fileName);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerInstanceRegistrationInfo_Export_creates_Windows_credential_and_drops_encrypted_password()
        {
            var serverType = new Guid("8c91a03d-f9b4-46c0-a305-b5dcc79ff907");
            WindowsCredential.RemoveRegSvrCredential("myServer", 1, "sa", serverType, RegistrationProvider.ProviderStoreCredentialVersion);
            var fileName = Path.GetTempFileName();
            using (var file = File.CreateText(fileName))
            {
                file.Write(InputXmlFormat, DataProtection.ProtectData("myPassword"));
                file.Close();
            }
            try
            {
                var registrationInfo = RegistrationProvider.Import(fileName) as ServerTypeRegistrationInfo;
                var server =
                    ((GroupRegistrationInfo)registrationInfo.Children[0]).Children[0] as ServerInstanceRegistrationInfo;
                var newPassword = Guid.NewGuid().ToString();
                server.ConnectionInfo.Password = newPassword;
                RegistrationProvider.Export(registrationInfo, fileName, true);
                var newContents = File.ReadAllText(fileName);
                Assert.That(newContents.Contains("Password"), Is.False, "Serialized file should not have the password: {0}", newContents);
                var savedPassword = WindowsCredential.GetSqlRegSvrCredential("myServer", 1, "sa", serverType, RegistrationProvider.ProviderStoreCredentialVersion);
                Assert.That(savedPassword, Is.Not.Null, "password should exist in credential manager");
                Assert.That(savedPassword.SecureStringToString(), Is.EqualTo(newPassword),
                    "credential manager password");
            }
            finally
            {
                WindowsCredential.RemoveRegSvrCredential("myServer", 1, "sa", serverType, RegistrationProvider.ProviderStoreCredentialVersion);
                File.Delete(fileName);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void RegistrationProvider_GetProviderFilePath()
        {
            // Note: due to the fact that static property RegistrationProvider.ProviderStore
            // can be assigned at most once, the order the the scenarios in the test matters.
            // Also: any other test outside of this one that would try to set 
            // RegistrationProvider.ProviderStore would interfere with this test (in ways
            // that would dependt on the execution order).

            // Initially, we validate the behavior using default values for ProviderStore
            // and ProviderStoreVersion.
            var expectedProviderFilePath =
                Path.Combine(
                    Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Microsoft SQL Server", "90", "Tools", "Shell", "RegSrvr.xml");
            Assert.That(
                RegistrationProvider.GetProviderFilePath(),
                Is.EqualTo(expectedProviderFilePath),
                "Unexpected ProviderFilePath - Default ProviderStore/ProviderStoreVersion");

            // Now we update the ProviderStoreVersion to some arbitrary value (typically,
            // this is something like "150", "160", ... i.e. a compatLevel).
            RegistrationProvider.ProviderStoreVersion = "SomeValueNotNull";
            expectedProviderFilePath =
                Path.Combine(
                    Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Microsoft SQL Server", "SomeValueNotNull", "Tools", "Shell", "RegSrvr.xml");
            Assert.That(
                RegistrationProvider.GetProviderFilePath(),
                Is.EqualTo(expectedProviderFilePath),
                "Unexpected ProviderFilePath - Default ProviderStore, Custom ProviderStoreVersion");

            // Now we update the ProviderStore to be ReplicationMonitor and
            // we reset ProviderStoreVersion to its default (null) value.
            RegistrationProvider.ProviderStore = RegistrationProvider.RegistrationProviderStore.ReplicationMonitor;
            RegistrationProvider.ProviderStoreVersion = null;
            expectedProviderFilePath =
                Path.Combine(
                    Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Microsoft SQL Server", "90", "Tools", "Shell", "RegReplSrvr.xml");
            Assert.That(
                RegistrationProvider.GetProviderFilePath(),
                Is.EqualTo(expectedProviderFilePath),
                "Unexpected ProviderFilePath - ReplicationMonitor ProviderStore, Default ProviderStoreVersion");

            // Now we update the ProviderStoreVersion to some arbitrary value, just like before.
            RegistrationProvider.ProviderStoreVersion = "SomeValueNotNull";
            expectedProviderFilePath =
                Path.Combine(
                    Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Microsoft SQL Server", "SomeValueNotNull", "Tools", "Shell", "RegReplSrvr.xml");
            Assert.That(
                RegistrationProvider.GetProviderFilePath(),
                Is.EqualTo(expectedProviderFilePath),
                "Unexpected ProviderFilePath - ReplicationMonitor ProviderStore, Custom ProviderStoreVersion");

            // Bonus test: confirm that we cannot assign ProviderStore more than once!
            Assert.Throws<InvalidOperationException>(
                () => { RegistrationProvider.ProviderStore = RegistrationProvider.RegistrationProviderStore.SQLServerManagementStudio; } ,
                "It should be illegal to assign static property ProviderStore more than once!"
                );
        }
    }

    internal static class StringExtensionMethods
    {
        /// <summary>
        /// Converts a secure string to a string
        /// </summary>
        /// <param name="secureString">Secure string</param>
        /// <returns>Converted secure string to string object</returns>
        public static string SecureStringToString(this SecureString secureString)
        {
            return new string(StringExtensionMethods.SecureStringToCharArray(secureString));
        }

        /// <summary>
        /// Converts string to a secure string
        /// </summary>
        /// <param name="unsecureString">Unsecured string</param>
        /// <returns>Converted string to secure string</returns>
        public static SecureString StringToSecureString(this string unsecureString)
        {
            return CharArrayToSecureString(unsecureString.ToCharArray());
        }

        /// <summary>
        /// Converts secure string to char array
        /// </summary>
        /// <param name="secureString">Secure string</param>
        /// <returns>secure string converted to array of characters</returns>
        private static char[] SecureStringToCharArray(SecureString secureString)
        {
            if (secureString == null)
            {
                return null;
            }

            var charArray = new char[secureString.Length];
            var ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);

            try
            {
                Marshal.Copy(ptr, charArray, 0, secureString.Length);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }

            return charArray;
        }

        /// <summary>
        /// Converts char array to secure string
        /// </summary>
        /// <param name="charArray">the array of chars</param>
        /// <returns>Array of characters to secure string</returns>
        private static SecureString CharArrayToSecureString(char[] charArray)
        {
            if (charArray == null)
            {
                return null;
            }

            var secureString = new SecureString();
            foreach (var c in charArray)
            {
                secureString.AppendChar(c);
            }

            secureString.MakeReadOnly();

            return secureString;
        }
    }
}
