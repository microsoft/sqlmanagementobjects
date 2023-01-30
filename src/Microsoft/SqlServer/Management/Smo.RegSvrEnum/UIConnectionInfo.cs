//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// The state of the password for this connection
    /// </summary>
    #region Password State Enum
    public
    enum SavePasswordState
    {
        /// <summary>
        /// The password has been loaded from persistance storage
        /// </summary>
        PasswordLoaded,
        /// <summary>
        /// The password has been marked to be persisted
        /// </summary>
        PasswordChecked,
        /// <summary>
        /// The password has not been marked to be persisted
        /// </summary>
        PasswordUnchecked
    }
    #endregion

    /// <summary>
    /// Object for storing and persisting server connection information
    /// </summary>
    public class UIConnectionInfo : IComparable<UIConnectionInfo>
    {       
        #region Constants
        private const string XmlStart = "ConnectionInformation";
        private const string XmlServerType = "ServerType";
        private const string XmlServerName = "ServerName";
        private const string XmlDisplayName = "DisplayName";
        private const string XmlUserName = "UserName";
        private const string XmlPassword = "Password";
        private const string XmlAuthenticationType = "AuthenticationType";
        private const string XmlAdvancedOptions = "AdvancedOptions";
        private const string XmlItemTypeAttribute = "type";
        #endregion
        
        #region Private Data
        private NameValueCollection advancedOptions;
        private Guid serverType = Guid.Empty;
        private int authType = int.MinValue;
        private string serverName = null;
        private string displayName = null;
        private string userName = null;
        private string password = null;
        [NonSerialized]
        private SecureString inMemoryPassword;
        private string appName = null;
        private bool savepwd;
        private ServerVersion serverVer = null;
        private string otherParams = null;

        private long id;
        private static long nextId;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the object to fetch or renew an access token for SqlConnection objects generated from this connection info instance.
        /// </summary>
        public IRenewableToken RenewableToken { get; set; }
    
        /// <summary>
        /// Used by the connection dialog to determine if the password should be persisted
        /// </summary>        
        public bool PersistPassword
        {
            get
            {
                return this.savepwd;
            }
            set
            {
                this.savepwd = value;
            }
        }

        /// <summary>
        /// The server type that this object is configured for
        /// </summary>
        public Guid ServerType
        {
            get
            {
                return this.serverType;
            }
            set
            {
                this.serverType = value;
            }
        }

        /// <summary>
        /// The Application from thich this UIConnectionInfo instance will
        /// be used to connect from
        /// </summary>
        public string ApplicationName
        {
            get
            {
                if(this.appName != null)
                {
                    return this.appName;
                }
                return string.Empty;
            }
            set
            {
                this.appName = value;
            }
        }
        
        /// <summary>
        /// The server name to connect to 
        /// </summary>
        public string ServerName
        {
            get
            {
                return this.serverName;
            }
            set
            {
                this.serverName = value;
            }
        }

        /// <summary>
        /// The display name for the server
        /// </summary>
        public string DisplayName
        {
            get
            {
                return (this.displayName != null) ? this.displayName : this.ServerNameNoDot;
            }
            set
            {
                this.displayName = value;
            }
        }

        
        /// <summary>
        /// returns either server name or "(local)" if the server name is "." or empty
        /// </summary>
        public string ServerNameNoDot
        {
            get
            {
                string strRealName = ServerName;
                if (strRealName != null)
                {
                    strRealName = strRealName.Trim();
                }

                //see if we need to adjust it
                if (strRealName.Length == 0 || strRealName == "." )
                {
                    return "(local)";
                } 
                else if (strRealName.StartsWith(".\\",StringComparison.Ordinal))
                {
                    return String.Format(CultureInfo.InvariantCulture, "{0}{1}", "(local)", strRealName.Substring(1));
                } 
                else
                {
                    return strRealName;
                }
            }
        }

        /// <summary>
        /// username to connect with
        /// </summary>
        public virtual string UserName
        {
            get
            {
                return this.userName ?? string.Empty;
            }
            set
            {
                this.userName = value;
            }
        }
        
        /// <summary>
        /// Password to connect with. This class holds password in encrypted format. The getter
        /// will return decrypted version of the password. The setter expects to get clear-text
        /// password and immediately encrypts it
        /// </summary>
        public string Password
        {
            get
            {
                if (this.inMemoryPassword != null)
                {
                    return this.inMemoryPassword.SecureStringToString();
                }
                return this.password;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    this.password = string.Empty;
                    this.inMemoryPassword = string.Empty.StringToSecureString();
                }
                else
                {
                    this.inMemoryPassword = value.StringToSecureString();
                    this.password = null;
                }
            }
        }

        /// <summary>
        /// The SecureString representing the current password
        /// </summary>
        public SecureString InMemoryPassword
        {
            get { return inMemoryPassword; }
            set
            {
                inMemoryPassword = value;
                password = null;
            }
        }

        /// <summary>
        /// the password in encrypted form
        /// </summary>
        [Obsolete]
        public string EncryptedPassword
        {
            get
            {
                return this.password;
            }
            set
            {
                this.password = value;
            }
        }

        /// <summary>
        /// Authentication type to use.  This is interpreted by the corresponding
        /// IServerType object
        /// </summary>
        public int AuthenticationType
        {
            get
            {
                return this.authType;
            }
            set
            {
                this.authType = value;
            }
        }

        /// <summary>
        /// Collection for storing user-defined connection parameters
        /// </summary>
        public NameValueCollection AdvancedOptions
        {
            get
            {
                return this.advancedOptions;
            }
        }

        /// <summary>
        /// The version of the server for this connection
        /// </summary>
        public ServerVersion ServerVersion
        {
            get
            {
                return this.serverVer;
            } 
            
            set
            {
                this.serverVer = value;
            }
        }
        
        /// <summary>
        /// The serial number of the connection info
        /// </summary>
        public long Id
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// The additional parameters if any of connection info
        /// </summary>
        public string OtherParams
        {
            get
            {
                return this.otherParams;
            }
            set
            {
                this.otherParams = value;
            }
        }


        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public UIConnectionInfo()
        {
            this.advancedOptions = new NameValueCollection();
            this.id = UIConnectionInfo.NewId();
        }
        
        
        /// <summary>
        /// Copy constructor - create connection like lhs.  
        /// </summary>
        /// <param name="lhs">The UIConnection info to copy</param>
        /// <param name="generateNewId">
        /// Set to true to generate a new id for the connection, or false
        /// if this info will refer to the same connection as lhs 
        /// </param>
        public UIConnectionInfo(UIConnectionInfo lhs, bool generateNewId)
            : this(lhs)
        {
            if (generateNewId)
            {
                this.id = UIConnectionInfo.NewId();
            }
        }

        /// <summary>
        /// Copy constructor - create connection to the same server as lhs
        /// </summary>
        /// <param name="lhs">The UIConnection info to copy</param>
        public UIConnectionInfo(UIConnectionInfo lhs)
        {
            this.serverType         = lhs.serverType;
            this.AuthenticationType = lhs.AuthenticationType;  
            this.appName            = lhs.appName;
            this.id                 = lhs.id;
            this.serverName         = lhs.serverName;
            this.displayName        = lhs.displayName;
            this.userName           = lhs.userName;
            this.inMemoryPassword   = lhs.inMemoryPassword;
            this.password           = lhs.password;
            this.savepwd            = lhs.savepwd;
            this.otherParams        = lhs.otherParams;
            this.RenewableToken     = lhs.RenewableToken;

            if(lhs.advancedOptions != null)
            {
                // will attempt to do a deep copy, objects that don't implement ICloneable
                // will only have shallow copies made.
                this.advancedOptions = new NameValueCollection(lhs.advancedOptions.Count);
                foreach(string key in lhs.advancedOptions.Keys)
                {
                    this.advancedOptions[key] = lhs.AdvancedOptions[key];
                }
            }
            else
            {
                this.advancedOptions = null;
            }
        }
        
        static UIConnectionInfo()
        {
            UIConnectionInfo.nextId = 0L;
        }
        #endregion

        #region Load/Save Methods

        /// <summary>
        /// Saves a UIConnectionInfo object to an XML stream
        /// </summary>
        /// <param name="writer">The XML Stream to save the UIConnectionInfo object to.  It is the caller's
        /// responsibility to open and close the stream</param>
        /// <param name="saveName">True if the user name (with possibly the encrypted 
        /// password) needs to be serialized to the XML stream; false, otherwise.</param>
        public void SaveToStream(System.Xml.XmlWriter writer, bool saveName)
        {   
            
            // write all of the predefined entries
            writer.WriteStartElement(UIConnectionInfo.XmlStart);
            writer.WriteElementString(UIConnectionInfo.XmlServerType, null, this.ServerType.ToString());
            writer.WriteElementString(UIConnectionInfo.XmlServerName, null, this.ServerName.ToString());
            writer.WriteElementString(UIConnectionInfo.XmlDisplayName, null, this.DisplayName);
            writer.WriteElementString(UIConnectionInfo.XmlAuthenticationType, null, this.AuthenticationType.ToString(CultureInfo.InvariantCulture));
            string userNameToSave = string.Empty;
            if(saveName)
            {
                if( this.UserName.Length > 0 && this.UserName != System.Security.Principal.WindowsIdentity.GetCurrent().Name)
                {
                    // save the user name
                    userNameToSave = this.UserName;
                    // we do not save passwords to any stream now. They are only saved to the local Windows credential manager
                }
            }
            writer.WriteElementString(UIConnectionInfo.XmlUserName, null, userNameToSave);
            
            writer.WriteStartElement("AdvancedOptions", null);

            foreach(string key in this.AdvancedOptions.Keys)
            {
                writer.WriteElementString(key, null, this.AdvancedOptions[key]);
            }

            writer.WriteEndElement();
            writer.WriteEndElement();           
        }

        /// <summary>
        /// Reads from an open XML Stream
        /// </summary>
        /// <param name="reader">an open XmlReader</param>
        /// <returns>A fully created UIConnectionInfo object</returns>
        public static UIConnectionInfo LoadFromStream(System.Xml.XmlReader reader)
        {
            UIConnectionInfo ci = new UIConnectionInfo();
            
            string s = reader.ReadOuterXml();

            XmlTextReader input = new XmlTextReader( new StringReader(s) )
            {
                DtdProcessing = DtdProcessing.Prohibit
            };
            // read the servertype
            while(input.Read())
            {
                if(input.NodeType == XmlNodeType.Element && input.LocalName == UIConnectionInfo.XmlServerType)
                {
                    ci.ServerType = new Guid(input.ReadString());
                    break;
                }
            }

            // read the servername
            while (input.Read())
            {
                if (input.NodeType == XmlNodeType.Element && input.LocalName == UIConnectionInfo.XmlServerName)
                {
                    ci.ServerName = input.ReadString();
                    break;
                }
            }

            // read the display name
            while(input.Read())
            {
                if(input.NodeType == XmlNodeType.Element && input.LocalName == UIConnectionInfo.XmlDisplayName)
                {
                    ci.DisplayName = input.ReadString();
                    break;
                }
            }
            
            // read the AuthenticationType
            while(input.Read())
            {
                if(input.NodeType == XmlNodeType.Element && input.LocalName == UIConnectionInfo.XmlAuthenticationType)
                {
                    ci.AuthenticationType = Convert.ToInt32(input.ReadString(), CultureInfo.InvariantCulture);
                    break;
                }
            }

            // read the username
            while(input.Read())
            {
                if(input.NodeType == XmlNodeType.Element && input.LocalName == UIConnectionInfo.XmlUserName)
                {
                    ci.UserName = input.ReadString();
                    break;
                }
            
            }

            // read the password (which is optional as of 15.0) and/or the AdvancedOptions
            var atPasswordOrAvancedOptionsElement = false;

            while (input.Read())     // Read all elements until we find either the Password or the AdvancedOptions
            {
                if (input.NodeType == XmlNodeType.Element &&
                    (input.LocalName == UIConnectionInfo.XmlPassword || input.LocalName == UIConnectionInfo.XmlAdvancedOptions))
                {
                    atPasswordOrAvancedOptionsElement = true;
                    break;
                }
            }

            // If the reader is on a Password or an AdvancedOption element, then read it...
            // The assumption is that the Password (if there) comes before the AdvancedOption element.
            if (atPasswordOrAvancedOptionsElement)
            {
                if (input.LocalName == UIConnectionInfo.XmlPassword)
                {

                    var encryptedPassword = input.ReadString();
                    // we can read the password but we don't store it anymore
                    // when the entry is edited, we save the password to credential manager instead
                    ci.Password = DataProtection.UnprotectData(encryptedPassword);

                    // advance to the additional properties element
                    while (input.Read() &&
                          !(input.NodeType == XmlNodeType.Element && input.LocalName == UIConnectionInfo.XmlAdvancedOptions)) ;

                }

                // If we are not empty then read the Advanced Options
                if (!input.IsEmptyElement)
                {
                    while (input.Read())
                    {
                        if (input.NodeType == XmlNodeType.Element)
                        {
                            ci.AdvancedOptions.Set(input.LocalName, input.ReadString());
                        }
                    }
                }
            }

            return ci;
        }
        
        #endregion

        #region Copy method
        /// <summary>
        /// Create a new connection info like this connection info.
        /// </summary>
        /// <remarks> Note that this generates a new id for the clone,
        /// so the clone does not exactly match its progenitor, which 
        /// is useful if we are going to change some parameter in the 
        /// new connection info.
        /// </remarks>
        public UIConnectionInfo Copy()
        {
            return new UIConnectionInfo(this, true);
        }
        #endregion

        #region private methods
        
        /// <summary>
        /// Get the next serial number for a new UIConnectionInfo
        /// </summary>
        private static long NewId()
        {
            return System.Threading.Interlocked.Add(ref UIConnectionInfo.nextId, 1L);
        }
        
        
        #endregion

        #region IComparable<UIConnectionInfo> Members

        /// <summary>
        /// Comparison used for sorting connection info
        /// </summary>
        /// <param name="other">The connection info to compare to</param>
        /// <returns>negative if this comes before other, 0 if they are the same, or positive if this comes after other </returns>
        public int CompareTo(UIConnectionInfo other)
        {
            // compare based on:
            // 1) server type
            // 2) registered server display name
            // 3) server name
            // 4) login name
            // 5) Auth token data
            // 6) ID
            
            int result = String.CompareOrdinal(
                this.ServerType.ToString(),
                other.ServerType.ToString());

            if (result == 0)
            {
                result = String.CompareOrdinal(this.DisplayName, other.DisplayName);
            }

            if (result == 0)
            {
                result = String.CompareOrdinal(this.ServerName, other.ServerName);
            }

            if (result == 0)
            {
                result = String.CompareOrdinal(this.UserName, other.UserName);
            }

            if (result == 0 && this.RenewableToken != null && other.RenewableToken != null)
            {
                result = String.CompareOrdinal(this.RenewableToken.Resource, other.RenewableToken.Resource);
                if (result == 0)
                {
                    result = String.CompareOrdinal(this.RenewableToken.UserId, other.RenewableToken.UserId);
                }
                if (result == 0)
                {
                    result = String.CompareOrdinal(this.RenewableToken.Tenant, other.RenewableToken.Tenant);
                }
            }
            // if server and login name are the same, compare ids
            if (result == 0)
            {
                if (this.Id < other.Id)
                {
                    result = -1;
                }
                else if (other.Id < this.Id)
                {
                    result = 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Whether this connection info refers to the same connection as the other
        /// </summary>
        /// <param name="obj">The other connection info</param>
        /// <returns>True if they connect to the same server as the same login, otherwise false</returns>
        public override bool Equals(object obj)
        {
            bool result = false;

            UIConnectionInfo other = obj as UIConnectionInfo;
            if (other != null)
            {
                result = (this.Id == other.Id);
            }

            return result;
        }

        /// <summary>
        /// The hash code for finding the connection info in hash tables
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (int) (this.Id % (long) Int32.MaxValue);
        }

        /// <summary>
        /// Whether the two connection info objects are equal
        /// </summary>
        /// <param name="infoA">The first connection info</param>
        /// <param name="infoB">The second connection info</param>
        /// <returns>True if the objects are equal; otherwise, false</returns>
        public static bool operator ==(UIConnectionInfo infoA, UIConnectionInfo infoB)
        {
            bool result = false;
            bool aIsNull = (((object) infoA) == null);
            bool bIsNull = (((object) infoB) == null);

            if (aIsNull && bIsNull)
            {
                result = true;
            }
            else if (!aIsNull && !bIsNull)
            {
                result = infoA.Equals(infoB);
            }

            return result;
        }

        /// <summary>
        /// Whether the two connection info objects are equal
        /// </summary>
        /// <param name="infoA">The first connection info</param>
        /// <param name="infoB">The second connection info</param>
        /// <returns>True if the objects are equal; otherwise, false</returns>
        public static bool operator ==(UIConnectionInfo infoA, object infoB)
        {
            bool result = false;
            bool aIsNull = (((object) infoA) == null);
            bool bIsNull = (infoB == null);

            if (aIsNull && bIsNull)
            {
                result = true;
            }
            else if (!aIsNull && !bIsNull)
            {
                result = infoA.Equals(infoB);
            }

            return result;
        }

        /// <summary>
        /// Whether the two connection info objects are equal
        /// </summary>
        /// <param name="infoA">The first connection info</param>
        /// <param name="infoB">The second connection info</param>
        /// <returns>True if the objects are equal; otherwise, false</returns>
        public static bool operator ==(object infoA, UIConnectionInfo infoB)
        {
            bool result = false;
            bool aIsNull = (infoA == null);
            bool bIsNull = (((object) infoB) == null);

            if (aIsNull && bIsNull)
            {
                result = true;
            }
            else if (!aIsNull && !bIsNull)
            {
                result = infoB.Equals(infoA);
            }

            return result;
        }

        /// <summary>
        /// Whether the two connection info objects are not equal
        /// </summary>
        /// <param name="infoA">The first connection info</param>
        /// <param name="infoB">The second connection info</param>
        /// <returns>True if the objects are not equal; otherwise, false</returns>
        public static bool operator !=(UIConnectionInfo infoA, UIConnectionInfo infoB)
        {
            return !(infoA == infoB);
        }

        /// <summary>
        /// Whether the two connection info objects are not equal
        /// </summary>
        /// <param name="infoA">The first connection info</param>
        /// <param name="infoB">The second connection info</param>
        /// <returns>True if the objects are not equal; otherwise, false</returns>
        public static bool operator !=(UIConnectionInfo infoA, object infoB)
        {
            return !(infoA == infoB);
        }

        /// <summary>
        /// Whether the two connection info objects are not equal
        /// </summary>
        /// <param name="infoA">The first connection info</param>
        /// <param name="infoB">The second connection info</param>
        /// <returns>True if the objects are not equal; otherwise, false</returns>
        public static bool operator !=(object infoA, UIConnectionInfo infoB)
        {
            return !(infoA == infoB);
        }

        #endregion

    }

    internal static class StringExtensionMethods
    {
        /// <summary>
        /// Converts a secure string to a string
        /// </summary>
        /// <param name="secureString"></param>
        /// <returns>Converted secure string to string object</returns>
        public static string SecureStringToString(this SecureString secureString)
        {
            return new string(secureString.SecureStringToCharArray());
        }

        /// <summary>
        /// Converts string to a secure string
        /// </summary>
        /// <param name="unsecureString"></param>
        /// <returns>Converted string to secure string</returns>
        public static SecureString StringToSecureString(this string unsecureString)
        {
            return unsecureString.ToCharArray().CharArrayToSecureString();
        }

        /// <summary>
        /// Converts secure string to char array
        /// </summary>
        /// <param name="secureString"></param>
        /// <returns>secure string converted to array of characters</returns>
        private static char[] SecureStringToCharArray(this SecureString secureString)
        {

            var charArray = new char[secureString.Length];
            IntPtr ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);

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
        /// <param name="charArray"></param>
        /// <returns>Array of characters to secure string</returns>
        private static SecureString CharArrayToSecureString(this IEnumerable<char> charArray)
        {
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
