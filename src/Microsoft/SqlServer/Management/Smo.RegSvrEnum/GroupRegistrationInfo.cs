
namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// 
    /// </summary>
    public class GroupRegistrationInfo : ParentRegistrationInfo
    {
        public GroupRegistrationInfo()
            : base()
        {
        }



        internal override void SaveNodeToXml(System.Xml.XmlWriter writer, bool saveName)
        {            
            writer.WriteStartElement("Group", null);
            writer.WriteAttributeString("name", null, this.FriendlyName);
            writer.WriteAttributeString("description", null, this.Description);

            // write out all of the children
            foreach(RegistrationInfo reg in this.Children)
            {
                reg.SaveNodeToXml(writer, saveName);
            }
            writer.WriteEndElement();
        }
    }
}
