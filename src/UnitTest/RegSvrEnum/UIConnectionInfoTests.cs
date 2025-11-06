// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert=NUnit.Framework.Assert;

namespace RegServerUnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class UIConnectionInfoTests
    {
        private const string ExpectedXmlFormat =
            @"<ConnectionInformation><ServerType>{0}</ServerType><ServerName>myServer</ServerName><DisplayName>myDisplayName</DisplayName><AuthenticationType>1</AuthenticationType><UserName>myUser</UserName><AdvancedOptions /></ConnectionInformation>";
        [TestMethod]
        public void UIConnectionInfo_SaveToStream_omits_password()
        {
            var connectionInfo = new UIConnectionInfo()
            {
                ApplicationName = "myApp",
                AuthenticationType = 1,
                DisplayName = "myDisplayName",
                Password = Guid.NewGuid().ToString(),
                ServerName = "myServer",
                ServerType = Guid.NewGuid(),
                UserName = "myUser"
            };
            var stringBuilder = new StringBuilder();
            var textWriter = XmlWriter.Create(stringBuilder);
            connectionInfo.SaveToStream(textWriter, saveName: true);
            textWriter.Close();
            var xmlText = stringBuilder.ToString();
            Trace.TraceInformation(xmlText);
            Assert.That(xmlText, Contains.Substring(string.Format(ExpectedXmlFormat, connectionInfo.ServerType)),
                "Xml text");
        }

        private const string InputXmlFormat =
            "<ConnectionInformation><ServerType>44a4a605-1e3a-4caa-a697-f16b8f65e78a</ServerType><ServerName>myServer</ServerName><DisplayName>myDisplayName</DisplayName><AuthenticationType>1</AuthenticationType><UserName>myUser</UserName><Password>{0}</Password><AdvancedOptions /></ConnectionInformation>";
        [TestMethod]
        public void UIConnectionInfo_LoadFromStream_preserves_encrypted_password()
        {
            var password = Guid.NewGuid().ToString();
            var inputXml = String.Format(InputXmlFormat, DataProtection.ProtectData(password));
            var xmlReader = XmlReader.Create(new StringReader(inputXml));
            xmlReader.MoveToContent();
            var uiConnectionInfo = UIConnectionInfo.LoadFromStream(xmlReader);
            Assert.That(uiConnectionInfo.Password, Is.EqualTo(password), "Decrypted password");
        }

        // Verify that when there is no password in the XML file, LoadFromStream() deserializes the data
        // correctly and can retrieve the AdvancedOptions. 
        private const string InputXmlNoPassword =
            "<ConnectionInformation><ServerType>44a4a605-1e3a-4caa-a697-f16b8f65e78a</ServerType><ServerName>myServer</ServerName><DisplayName>myDisplayName</DisplayName><AuthenticationType>1</AuthenticationType><UserName>myUser</UserName><AdvancedOptions><ao1>10</ao1></AdvancedOptions></ConnectionInformation>";
        [TestMethod]
        public void UIConnectionInfo_LoadFromStream_CanRead_AdvancedOptions_When_Password_Not_Listed()
        {
            UIConnectionInfo uiConnectionInfo = null;

            using (var xmlReader = XmlReader.Create(new StringReader(InputXmlNoPassword)))
            {
                xmlReader.MoveToContent();
                uiConnectionInfo = UIConnectionInfo.LoadFromStream(xmlReader);
            }

            Assert.That(uiConnectionInfo.AdvancedOptions["ao1"], Is.EqualTo("10"), "AdvancedOption");
        }

        // Verify that I can call SaveToStream() and then LoadFromStream() without losing data.
        [TestMethod]
        public void UIConnectionInfo_SaveToStrem_and_LoadFromStream_Roundtrip()
        {
            var connectionInfo = new UIConnectionInfo()
            {
                ApplicationName = "myApp",
                AuthenticationType = 1,
                DisplayName = "myDisplayName",
                Password = Guid.NewGuid().ToString(),
                ServerName = "myServer",
                ServerType = Guid.NewGuid(),
                UserName = "myUser"
            };

            connectionInfo.AdvancedOptions.Add("advname1", "advvalue1");
            connectionInfo.AdvancedOptions.Add("advname2", "advvalue2");

            var stringBuilder = new StringBuilder();
            var textWriter = XmlWriter.Create(stringBuilder);
            connectionInfo.SaveToStream(textWriter, saveName: true);
            textWriter.Close();
            var xmlText = stringBuilder.ToString();

            Trace.TraceInformation(xmlText);

            UIConnectionInfo connectionInfoFromStream = null;

            using (var xmlReader = XmlReader.Create(new StringReader(xmlText)))
            {
                xmlReader.MoveToContent();
                connectionInfoFromStream = UIConnectionInfo.LoadFromStream(xmlReader);
            }

            Assert.That(connectionInfoFromStream,                               Is.Not.Null,                                    "connectionInfoFromStream should not be null");
            // ApplicationName is not persisted and it is read as ""
            Assert.That(connectionInfoFromStream.ApplicationName,               Is.Empty,                                       "ApplicationName should be empty");

            Assert.That(connectionInfoFromStream.AuthenticationType,            Is.EqualTo(connectionInfo.AuthenticationType),  "AuthenticationType");
            Assert.That(connectionInfoFromStream.DisplayName,                   Is.EqualTo(connectionInfo.DisplayName),         "DisplayName");
            // Password is not persisted, so this is read as 'null'
            Assert.That(connectionInfoFromStream.Password,                      Is.Null,                                        "Password should be null");
            Assert.That(connectionInfoFromStream.ServerName,                    Is.EqualTo(connectionInfo.ServerName),          "ServerName");
            Assert.That(connectionInfoFromStream.ServerType,                    Is.EqualTo(connectionInfo.ServerType),          "ServerType");
            Assert.That(connectionInfoFromStream.UserName,                      Is.EqualTo(connectionInfo.UserName),            "UserName");
            Assert.That(connectionInfoFromStream.AdvancedOptions["advname2"],   Is.EqualTo("advvalue2"),                        "AdvancedOptions was not read or had incorrect value");
        }

        [TestMethod]
        public void UIConnectionInfo_Copy_preserves_password()
        {
            var inputXml = String.Format(InputXmlFormat, DataProtection.ProtectData("somedata"));
            var xmlReader = XmlReader.Create(new StringReader(inputXml));
            xmlReader.MoveToContent();
            var uiConnectionInfo = UIConnectionInfo.LoadFromStream(xmlReader);
            var copy = uiConnectionInfo.Copy();
            Assert.That(copy.Password, Is.EqualTo(uiConnectionInfo.Password), "copy.Password");
            Assert.That(copy.ServerName, Is.EqualTo(uiConnectionInfo.ServerName), "copy.ServerName");
        }

        [DataRow(null, null)]
        [DataRow("", "(local)")]
        [DataRow(".", "(local)")]
        [DataRow("myServer", "myServer")]
        [DataRow(".\\abc", "(local)\\abc")]
        [TestMethod]
        public void UIConnectionInfo_ServerNameNoDotProperty(string input, string expectedOutput)
        {
            var uiConnectionInfo = new UIConnectionInfo() { ServerName = input };
            Assert.That(uiConnectionInfo.ServerNameNoDot, Is.EqualTo(expectedOutput));
        }
    }

    // copied from product code
    /// <summary>
    /// Summary description for DataProtection.
    /// </summary>
    //[StrongNameIdentityPermissionAttribute(
    //	 SecurityAction.LinkDemand, 
    //	 PublicKey="0024000004800000940000000602000000240000525341310004000001000100976BDD12F5AC899FF7F6081A3DA4EF2C88BC3B3571D299F67C24C5EBA386BEAA77F494B10DF5EE7C69ACB27A8F9A7801192F4274C6B2C442F12061BFF6A8F2A2490F7338C7DE6A096780A6D15A4B16E5522CB977BE0B8E4341AD32DA46617D2A0ED5962A904DDC50403A09AAAC75937A8ABB78B4CA119FF3F96DAC20C6E6B7DC")]
    internal sealed class DataProtection
    {
        // statics only here
        private DataProtection()
        {
        }

        // takes a string and returns a base64 encoded string containing
        // the protected data
        public static string ProtectData(string data) 
        {
            return ProtectData(data, CryptoNativeMethods.UIForbidden);
        }

        // takes a base64 encoded containing protected data and
        // returns the data as a string
        public static string UnprotectData(string data) 
        {
            byte[] dataIn = Convert.FromBase64String(data);
            byte[] dataOut = UnprotectData(dataIn);
            string strOut = null;

            if ( null != dataOut )
            {
                if (dataOut.Length == 0)
                {
                    strOut = null;
                } 
                else
                {
                    strOut = Encoding.Unicode.GetString(dataOut);
                }
            }
            dataOut = null;
            return strOut;
        }
        // works on byte arrays
        public static byte[] ProtectData(byte[] data) 
        {
            return ProtectData(data, CryptoNativeMethods.UIForbidden);
        }
        // works on byte arrays
        public static byte[] UnprotectData(byte[] data) 
        {
            return UnprotectData(data, CryptoNativeMethods.UIForbidden);
        }

        private sealed class CryptoNativeMethods 
        {
            // p/invoke decls
            [DllImport("crypt32", CharSet=System.Runtime.InteropServices.CharSet.Auto, SetLastError=true)]
            internal static extern bool CryptProtectData
                (ref CryptoNativeMethods.DATA_BLOB dataIn, 
                string szDataDescr, 
                IntPtr optionalEntropy, 
                IntPtr pvReserved, 
                IntPtr pPromptStruct,
                int dwFlags, 
                ref CryptoNativeMethods.DATA_BLOB pDataOut);

            [DllImport("crypt32", CharSet=System.Runtime.InteropServices.CharSet.Auto, SetLastError=true)]
            internal static extern bool CryptUnprotectData(
                ref CryptoNativeMethods.DATA_BLOB dataIn, 
                StringBuilder ppszDataDescr,
                IntPtr optionalEntropy, 
                IntPtr pvReserved,
                IntPtr pPromptStruct, 
                int dwFlags,
                ref CryptoNativeMethods.DATA_BLOB pDataOut);

            [DllImport("kernel32")]
            internal static extern IntPtr LocalFree(IntPtr hMem);
        
            [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
                public struct DATA_BLOB 
            {
                public int cbData;
                public IntPtr pbData;
            }

            public const int UIForbidden = 0x1;
            public const int LocalMachine = 0x4;
        }

#region private stuff
        // exposes flags
        private static string ProtectData(string data, int flags) 
        {
            byte[] dataIn = Encoding.Unicode.GetBytes(data);
            byte[] dataOut = ProtectData(dataIn, flags);
            string strOut = null;
            if ( null != dataOut )
                strOut = Convert.ToBase64String(dataOut);
            dataIn = null;
            return strOut;
        }

        private static byte[] UnprotectData(byte[] data, int dwFlags) 
        {
            byte[] clearText = null;

            // copy data into unmanaged memory
            CryptoNativeMethods.DATA_BLOB din = new CryptoNativeMethods.DATA_BLOB();		

            StringBuilder name = new StringBuilder();

            din.cbData = data.Length;
            din.pbData = Marshal.AllocHGlobal(din.cbData);
            Marshal.Copy(data, 0, din.pbData, din.cbData);
            CryptoNativeMethods.DATA_BLOB dout = new CryptoNativeMethods.DATA_BLOB();
            try 
            {
                bool ret = CryptoNativeMethods.CryptUnprotectData(
                    ref din, 
                    name, 
                    IntPtr.Zero, 
                    IntPtr.Zero, 
                    IntPtr.Zero, 
                    dwFlags, 
                    ref dout);
                if ( ret ) 
                {
                    clearText = new byte[dout.cbData];
                    Marshal.Copy(dout.pbData, clearText, 0, dout.cbData);
                    byte[] emptyBytes = new Byte[dout.cbData];
                    // zero out the unmanaged buffer
                    Marshal.Copy(emptyBytes, 0, dout.pbData, dout.cbData);
                    CryptoNativeMethods.LocalFree(dout.pbData);
                    dout.pbData = IntPtr.Zero;
                }
                else 
                {
                    throw new Win32Exception("Unprotect failed");
                }
            }
            finally 
            {
                if (din.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(din.pbData);
                if (dout.pbData != IntPtr.Zero)
                {
                    CryptoNativeMethods.LocalFree(dout.pbData);
                    dout.pbData = IntPtr.Zero;
                }
            }
            return clearText;
        }

        private static byte[] ProtectData(byte[] data, int dwFlags) 
        {
            string name = "Default"; // description of data, is required on Win2K
            byte[] cipherText = null;
            // copy data into unmanaged memory
            CryptoNativeMethods.DATA_BLOB din = new CryptoNativeMethods.DATA_BLOB();
            din.cbData = data.Length;
            din.pbData = Marshal.AllocHGlobal(din.cbData);
            Marshal.Copy(data, 0, din.pbData, din.cbData);
            CryptoNativeMethods.DATA_BLOB dout = new CryptoNativeMethods.DATA_BLOB();
            try 
            {
                bool ret = CryptoNativeMethods.CryptProtectData(
                    ref din,       // data in
                    name,          // szDataDescr
                    IntPtr.Zero,   // pOptionalEntropy
                    IntPtr.Zero,   // pvReserved
                    IntPtr.Zero,   // pPromptStruct
                    dwFlags,       // dwFlags
                    ref dout       // pDataOut
                    );
                // zero out the unmanaged buffer buffer
                byte[] emptyBytes = new byte[din.cbData];
                Marshal.Copy(emptyBytes, 0, din.pbData, din.cbData);

                if ( ret ) 
                {
                    cipherText = new byte[dout.cbData];
                    Marshal.Copy(dout.pbData, cipherText, 0, dout.cbData);
                    CryptoNativeMethods.LocalFree(dout.pbData);
                    dout.pbData = IntPtr.Zero;
                }
                else 
                {
                    throw new Win32Exception("ProtectData failed");
                }
            }
            finally 
            {
                if (din.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(din.pbData);
                if (dout.pbData != IntPtr.Zero)
                {
                    CryptoNativeMethods.LocalFree(dout.pbData);
                    dout.pbData = IntPtr.Zero;
                }
            }
            return cipherText;
        }
#endregion
    }

}
