//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ServerInstanceRegistrationInfo : RegistrationInfo
    {
#region Constructor
        /// <summary>
        /// 
        /// </summary>
        public ServerInstanceRegistrationInfo()
            : base()
        {
            this.ConnectionInfo = new UIConnectionInfo();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ci"></param>
        public ServerInstanceRegistrationInfo(UIConnectionInfo ci)
            : base()
        {
            this.ConnectionInfo = ci;
        }
#endregion

#region Public Properties
        /// <summary>
        /// 
        /// </summary>
        public UIConnectionInfo ConnectionInfo
        {
            get
            {
                return this.ci;
            }
            set
            {
                this.ci = value;
            }
        }
#endregion

        internal override void SaveNodeToXml(System.Xml.XmlWriter writer, bool saveName)
        {
            
            writer.WriteStartElement("Server", null);
            writer.WriteAttributeString("name", null, this.FriendlyName);
            writer.WriteAttributeString("description", null, this.Description);
            this.ConnectionInfo.SaveToStream(writer, saveName);
            if (ConnectionInfo.InMemoryPassword != null)
            {
                WindowsCredential.SetSqlRegSvrCredential(ConnectionInfo.ServerName, ConnectionInfo.AuthenticationType,
                    ConnectionInfo.UserName, ServerType, ConnectionInfo.InMemoryPassword, RegistrationProvider.ProviderStoreCredentialVersion);
            }
            writer.WriteEndElement();
        }
        
#region Private Members
        private UIConnectionInfo ci;
#endregion
    }
}
