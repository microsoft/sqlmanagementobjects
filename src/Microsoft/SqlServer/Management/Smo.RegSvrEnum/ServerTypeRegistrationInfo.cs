//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// 
    /// </summary>
    public class ServerTypeRegistrationInfo : ParentRegistrationInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ServerTypeRegistrationInfo()			
        {
        }

        internal override void SaveNodeToXml(System.Xml.XmlWriter writer, bool saveName)
        {
            
            writer.WriteStartElement("ServerType", null);
            writer.WriteAttributeString("id", null, this.ServerType.ToString());
            writer.WriteAttributeString("name", null, this.FriendlyName);

            // write out all of the children
            foreach(RegistrationInfo reg in this.Children)
            {
                reg.SaveNodeToXml(writer, saveName);
            }
            writer.WriteEndElement();
        }
    }
}
