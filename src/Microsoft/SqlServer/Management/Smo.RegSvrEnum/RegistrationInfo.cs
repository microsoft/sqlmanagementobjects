
//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
using System;
using System.Xml;
#if STRACE
using Microsoft.SqlServer.Management.Diagnostics;
#endif
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    //TODO: Prompt should be renamed ?
    /// <summary>
    /// 
    /// </summary>
    public enum RegistrationAddBehavior
    {
        /// <summary>
        /// 
        /// </summary>
        CreateCopy, 
        /// <summary>
        /// 
        /// </summary>
        Overwrite , 
        /// <summary>
        /// 
        /// </summary>
        Prompt
    }

    
    /// <summary>
    /// base class for various types of registration information for registered servers
    /// infrastructure
    /// </summary>
    public abstract class RegistrationInfo : IComparable 
    {
        internal RegistrationInfo() 
            : this(string.Empty, string.Empty, Guid.Empty)
        {
        }

        internal RegistrationInfo(string name, string desc, Guid st)
        {
            this.FriendlyName = name;
            this.Description = desc;
            this.ServerType = st;
            this.parent = null;
        }
        
        
#region Public Properties and methods
        
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="RegisteredServerException"></exception>
        public string FriendlyName
        {
            get
            {
                return this.fName;
            }
            set
            {
                if(value == this.fName)
                {
                    return; // do nothing if we are not really changing the name
                }
                // check for name collisions
                if(this.Parent != null)
                {
                    if(this.Parent.HasChildWithName(value))
                    {
                        throw new RegisteredServerException(SRError.ErrCannotHaveSiblingsWithSameName);
                    }
                }
                this.fName = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Description
        {
            get
            {
                return this.desc;
            }
            set
            {
                this.desc = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Guid ServerType
        {
            get
            {
                if(this.stype != Guid.Empty)
                {
                    return this.stype;
                }
                else
                {
                    //will work for server groups
                    foreach(RegistrationInfo reg in this.Ancestors)
                    {
                        if(reg.stype != Guid.Empty)
                        {
                            return reg.stype;
                        }
                    }
                    return Guid.Empty;
                }
            }
            set
            {
                this.stype = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ParentRegistrationInfo Parent
        {
            get
            {
                return this.parent;
            }
            set
            {
                this.parent = value;
            }
        }

        

        // REMOVE:
        //public abstract System.Windows.Forms.TreeNode GenerateTreeNode();

#endregion

#region internal properties and methods

        /// <summary>
        /// returns collection of our ancestors (Parent, Parent.Parent etc)
        /// </summary>
        internal RegistrationInfoCollection Ancestors
        {
            get
            {
                RegistrationInfoCollection list = new RegistrationInfoCollection();
                RegistrationInfo c = this;
                                
                while(c.Parent != null)
                {
                    list.Add(c.Parent);
                    c = c.Parent;
                }
                return list;
            }
        }
        
        internal static RegistrationInfo LoadNodeFromXml(System.Xml.XmlReader reader)
        {
            
            // get this node's descendents
            while (reader.NodeType != XmlNodeType.Element && reader.Read());
            string s = reader.ReadOuterXml();
            if (s == string.Empty) // there is no more left to read
            {
                return null;
            }
            XmlTextReader input = new XmlTextReader( new StringReader(s) )
            {
                DtdProcessing = DtdProcessing.Prohibit,
                WhitespaceHandling = WhitespaceHandling.None
            };
            while (input.Read())
            {
                if (input.NodeType == XmlNodeType.Element)
                {
                    RegistrationInfo r = null;
                    switch (input.LocalName)
                    {
                        case "ServerType":
                            r = RegistrationInfo.LoadServerTypeFromXml(input);                          
                            break;
                        case "Server":
                            r = RegistrationInfo.LoadServerFromXml(input);
                            break;
                        case "Group":
                            r = RegistrationInfo.LoadGroupFromXml(input);
                            break;
                        default:
#if STRACE
                            STrace.Trace(Tracing.ConnDialog, Tracing.Warning, 
                                "Unknown Element in registration file: {0}", input.LocalName);
#endif
                            break;
                    }
                    return r;
                }
            }
            return null;
        }

        internal abstract void SaveNodeToXml(System.Xml.XmlWriter writer, bool saveName);

#endregion

#region Serialization/Deserialization Methods

        private static ServerTypeRegistrationInfo LoadServerTypeFromXml(XmlReader reader)
        {
            
            string s = reader.ReadOuterXml();
            XmlTextReader input = new XmlTextReader( new StringReader(s) )
            {
                DtdProcessing = DtdProcessing.Prohibit                
            };
            input.Read();

            ServerTypeRegistrationInfo r = new ServerTypeRegistrationInfo();

            // check to see if we have any children
            bool isEmpty = input.IsEmptyElement;

            // get the attributes
            input.MoveToFirstAttribute();
            r.ServerType = new Guid(input.Value);
            input.MoveToNextAttribute();
            r.FriendlyName = input.Value;

            if (!isEmpty)
            {
                // keep reading in children until we are at the end of this group definition
                RegistrationInfo child = null;
                while((child = RegistrationInfo.LoadNodeFromXml(input)) != null)
                {
                    r.AddChild(child);
                }
            }

            // clean up by advancing to the current ending node
            while(reader.Read() && reader.NodeType != XmlNodeType.EndElement);
            return r;
        }

        private static GroupRegistrationInfo LoadGroupFromXml(System.Xml.XmlReader reader)
        {
            
            string s = reader.ReadOuterXml();
            XmlTextReader input = new XmlTextReader( new StringReader(s) )
            {
                DtdProcessing = DtdProcessing.Prohibit,
                WhitespaceHandling = WhitespaceHandling.None
            };
            input.Read();
            
            GroupRegistrationInfo r = new GroupRegistrationInfo();
                        
            // check to see if we have any children
            bool isEmpty = input.IsEmptyElement;

            // get the attributes
            input.MoveToFirstAttribute();
            r.FriendlyName = input.Value;
            input.MoveToNextAttribute();
            r.Description = input.Value;

            if(!isEmpty)
            {
                // keep reading in children until we are at the end of this group definition
                RegistrationInfo child = null;
                RegistrationInfo existingNode = null;
                while((child = RegistrationInfo.LoadNodeFromXml(input)) != null)
                {
                    existingNode = r.Children[child.FriendlyName];
                    //BUGBUG - if group names can be the same as server names, we need to adjust this code
                    if (existingNode != null)
                    {
                        //generate unique name
                        child.FriendlyName = r.GenerateUniqueName(child.FriendlyName);
                    }
                    r.AddChild(child);
                }               
            }

            return r;
        }

        private static ServerInstanceRegistrationInfo LoadServerFromXml(XmlReader reader)
        {

            string s = reader.ReadOuterXml();
            XmlTextReader input = new XmlTextReader( new StringReader(s) )
            {
                DtdProcessing = DtdProcessing.Prohibit,
                WhitespaceHandling = WhitespaceHandling.None
            };
            input.Read();
            
            ServerInstanceRegistrationInfo r = new ServerInstanceRegistrationInfo();

            // get the attributes
            input.MoveToFirstAttribute();
            r.FriendlyName = input.Value;
            input.MoveToNextAttribute();
            r.Description = input.Value;

            while(input.LocalName != "ConnectionInformation" && 
                  input.Read() );

            // deserialize the UIConnectionInfo portion
            r.ConnectionInfo = UIConnectionInfo.LoadFromStream(input);
            r.ServerType = r.ConnectionInfo.ServerType;
            var windowsPassword = WindowsCredential.GetSqlRegSvrCredential(r.ConnectionInfo.ServerName,
                r.ConnectionInfo.AuthenticationType, r.ConnectionInfo.UserName, r.ServerType, RegistrationProvider.ProviderStoreCredentialVersion);
            // credentials saved in credential manager take precedence over the DPAPI-encoded password in the XML file
            if (windowsPassword != null)
            {
                r.ConnectionInfo.InMemoryPassword = windowsPassword;
            }
            return r;
        }


#endregion
        
#region Private Data
        private string fName;
        private string desc;
        private Guid stype;
        private ParentRegistrationInfo parent;
#endregion

#region IComparable Members


        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        public int CompareTo(object obj)
        {            
            if(obj is RegistrationInfo)
            {
                string name = (obj as RegistrationInfo).FriendlyName;
                return String.Compare(this.fName, name, StringComparison.CurrentCultureIgnoreCase);                
            }
            throw new ArgumentException("Object is not of type RegistrationInfo");            
        }

#endregion
}   
}