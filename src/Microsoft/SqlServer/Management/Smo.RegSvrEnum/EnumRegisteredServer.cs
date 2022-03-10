//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
using System;
using System.Data;
using System.Collections;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{

    /// <summary>
    /// Connection structure that points to the root of the registered servers store
    /// </summary>
    public sealed class RegSvrConnectionInfo
    {
        public static readonly Guid SqlServerTypeGuid = new Guid("8c91a03d-f9b4-46c0-a305-b5dcc79ff907");

        public RegSvrConnectionInfo()
        {
            m_RootServerType = RegistrationProvider.RootNode;
        }

        public ParentRegistrationInfo RootServerType
        {
            get { return m_RootServerType; }
        }


        public ParentRegistrationInfo GetRegistrationInfoFromPath(Urn path)
        {
            //Find the SqlServer Type node. This is the starting point for the smo tree
            ServerTypeRegistrationInfo sqlRegType = null;

            foreach(RegistrationInfo ri in m_RootServerType.Children)
            {
                ServerTypeRegistrationInfo sType = ri as ServerTypeRegistrationInfo;
                if(sType != null)
                {
                    if (sType.ServerType.CompareTo(SqlServerTypeGuid) == 0)
                    {
                        sqlRegType = sType;
                        break;
                    }
                }
            }

            if(sqlRegType == null)
            {
                throw new InternalEnumeratorException(SRError.SqlServerTypeNotFound(SqlServerTypeGuid.ToString()));
            }

            if (path == null || ((string)path).Length <= 0)
            {
                return sqlRegType;
            }

            ParentRegistrationInfo priInner = sqlRegType;
            //Now get resolve the path
            //Walk the path to find the required container
            for (int blockIdx = 0; blockIdx < path.XPathExpression.Length; blockIdx++)
            {
                XPathExpressionBlock eb = path.XPathExpression[blockIdx];
                string groupName = eb.GetAttributeFromFilter("Name");

                priInner = priInner.Children[groupName] as ParentRegistrationInfo;

                if(priInner == null)
                {
                    throw new InternalEnumeratorException(SRError.PathFilterInvalid(groupName));
                }
            }

            return priInner;
        }

        public RegistrationInfo AddServerTypeNode(ServerTypeRegistrationInfo node)
        {
            return RegistrationProvider.AddServerTypeNode(node);
        }

        public void AddNode(RegistrationInfo reg, ParentRegistrationInfo parent)
        {
            RegistrationProvider.AddNode(reg, parent, RegistrationAddBehavior.CreateCopy);
        }


        public void Save(RegistrationInfo node)
        {
            RegistrationProvider.MarkNodeModified(node);
        }


        public void RemoveNode(RegistrationInfo reg, ParentRegistrationInfo parent)
        {
            RegistrationProvider.RemoveNode(reg, parent);
        }

        private ParentRegistrationInfo m_RootServerType;
    }

    
    internal abstract class EnumRegServerBase : EnumObject
    {
        /// <summary>
        /// return what types of results does this object support
        /// </summary>
        public override Request RetrieveParentRequest()
        {
            return null;
        }

        /// <summary>
        /// Override this method to generate a Request for the parent object
        /// The response from the parent object will give us the subset of parent objects for which 
        /// the current level must generate it's result
        /// </summary>
        /// <returns></returns>
        public override void PostProcess(EnumResult erChildren)
        {
        }

        /// <summary>
        /// The ResultTypes that this object supports
        /// </summary>
        /// <returns></returns>
        public override ResultType[] ResultTypes
        {
            get { return new ResultType [] { ResultType.DataTable }; }
        }


        protected ArrayList BuildRegistrationInfoList(ServerTypeRegistrationInfo svrTypeReg, String sPath, String sName, Type type)
        {
            ArrayList list = new ArrayList();

            ParentRegistrationInfo priLevel = GetRegistrationInfoFromPath(svrTypeReg, Urn.UnEscapeString(sPath));
            //return an empty list if the path doesn't exist
            if(priLevel != null)
            {
                foreach(RegistrationInfo ri in priLevel.Children)
                {
                    if( ri.GetType() == type )
                    {
                        if( sName == null || sName.Length <= 0 || ri.FriendlyName == sName)
                        {
                            list.Add(ri);
                        }
                    }
                }
            }

            return list;
        }


        protected ParentRegistrationInfo GetRegistrationInfoFromPath(ServerTypeRegistrationInfo svrTypeReg, Urn path)
        {
            if (path == null || ((string)path).Length <= 0)
            {
                return svrTypeReg;
            }

            ParentRegistrationInfo priInner = svrTypeReg;
            //Now get resolve the path
            //Walk the path to find the required container
            for( int blockIdx = 0; blockIdx < path.XPathExpression.Length; blockIdx++)
            {
                XPathExpressionBlock eb = path.XPathExpression[blockIdx];
                string groupName = eb.GetAttributeFromFilter("Name");

                priInner = priInner.Children[groupName] as ParentRegistrationInfo;

                if (priInner == null)
                {
                    throw new InternalEnumeratorException(SRError.PathFilterInvalid(groupName));
                }
            }

            return priInner;
        }


        protected ParentRegistrationInfo RootServerTypeRegistration
        {
            get
            {
                RegSvrConnectionInfo regConInfo = this.ConnectionInfo as RegSvrConnectionInfo;
                if(regConInfo == null)
                {
                    throw new InternalEnumeratorException(SRError.ConnectionInfoTypeIncorrect);
                }

                return regConInfo.RootServerType;
            }
        }


        protected void GetFilterOptions(out string sPath, out string sName)
        {
            //get filter info
            sPath = this.GetFixedStringProperty("Path", true);
            sName = this.GetFixedStringProperty("Name", true);
        }


        protected DataTable GetSchemaTable()
        {
            DataTable dt = new DataTable();
            dt.Locale = CultureInfo.InvariantCulture;

            foreach(string propertyName in this.Request.Fields)
            {
                ObjectProperty op = this.GetProperty(propertyName, ObjectPropertyUsages.Request);
                dt.Columns.Add(new DataColumn(op.Name, Type.GetType(op.Type)));
            }
            return dt;
        }
    }


    /// <summary>
    /// Class to handle the ServerGroup enum element
    /// </summary>
    internal class EnumServerType : EnumRegServerBase
    {
        internal EnumServerType()
        {
            ObjectProperty op = new ObjectProperty();
            op.Name = "Name";
            op.Type = "System.String";
            op.ReadOnly = false;
            op.Usage = ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "Description";
            op.Type = "System.String";
            op.ReadOnly = false;
            op.Usage = ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "ServerType";
            op.Type = "System.Guid";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Filter | ObjectPropertyUsages.Request;
            this.AddProperty(op);
        }

        /// <summary>
        /// This is called after the data has been retrieved by the final object so the chain is preparing to be freed
        /// Because the objects are persisted between calls free any call specific data
        /// </summary>
        /// <param name="erParent"></param>
        public override EnumResult GetData(EnumResult erParent)
        {
            //get filter info
            string serverType = this.GetFixedStringProperty("ServerType", true);

            //The child is asking for info. Pass back the root node of the server type in the @ServerType filter
            if(this.Request == null)
            {
                //If a child is calling this the filter must be supplied
                if(null == serverType)
                {
                    throw new InternalEnumeratorException(SRError.ServerTypeNotSpecified);
                }

                Guid svrTypeGuid;
                try
                {
                    //throw if serverType can't be converted to a guid
                    svrTypeGuid = new Guid(serverType);
                }
                catch (FormatException e)
                {
                    throw new InternalEnumeratorException(SRError.ServerTypeMustBeGuid, e);
                }

                ParentRegistrationInfo root = this.RootServerTypeRegistration;
                ServerTypeRegistrationInfo svrType = null;

                //First get the server type
                foreach(RegistrationInfo regInfo in root.Children)
                {
                    svrType = regInfo as ServerTypeRegistrationInfo;
                    if(svrType != null)
                    {
                        if(svrType.ServerType.CompareTo(svrTypeGuid) == 0) 
                            break;
                    }
                }

                if(svrType == null)
                {
                    //Handle the case where the server type is SqlServer and create it if it doesn't exist
                    //RegSvrConnectionInfo.SqlServerTypeGuid == '8c91a03d-f9b4-46c0-a305-b5dcc79ff907'
                    if (RegSvrConnectionInfo.SqlServerTypeGuid.CompareTo(svrTypeGuid) == 0)
                    {
                        //Get the provider
                        RegSvrConnectionInfo regConInfo = this.ConnectionInfo as RegSvrConnectionInfo;
                        if (regConInfo == null)
                        {
                            throw new InternalEnumeratorException(SRError.ConnectionInfoTypeIncorrect);
                        }

                        //Create the node 
                        ServerTypeRegistrationInfo sqlType = new ServerTypeRegistrationInfo();

                        sqlType.ServerType = svrTypeGuid;
                        svrType = regConInfo.AddServerTypeNode(sqlType) as ServerTypeRegistrationInfo;
                    }
                    else
                    {
                        throw new InternalEnumeratorException(SRError.ServerTypeFilterInvalid);
                    }
                }

                return new EnumResult(svrType, ResultType.Reserved1);
            }
            //return all server types if this is the last URN element
            else
            {
                DataTable dt = GetSchemaTable();

                ParentRegistrationInfo root = this.RootServerTypeRegistration;
                //First get the server type
                foreach(RegistrationInfo regInfo in root.Children)
                {
                    ServerTypeRegistrationInfo svrTypeReg = regInfo as ServerTypeRegistrationInfo;

                    bool svrTypeSame;
                    try
                    {
                        //throw if serverType can't be converted to a guid
                        svrTypeSame = (serverType == null || svrTypeReg.ServerType.CompareTo(new Guid(serverType)) == 0);
                    }
                    catch (FormatException e)
                    {
                        throw new InternalEnumeratorException(SRError.ServerTypeMustBeGuid, e);
                    }

                    if (svrTypeReg != null && svrTypeSame)
                    {
                        AddRow(svrTypeReg, dt);
                    }
                }

                return new EnumResult(dt, ResultType.DataTable);
            }
        }
        

        void AddRow(ServerTypeRegistrationInfo svrTypeReg, DataTable dt)
        {
            DataRow dr = dt.NewRow();

            foreach(DataColumn dc in dt.Columns)
            {
                switch(dc.Caption)
                {
                    case "ServerType":  dr["ServerType"] = svrTypeReg.ServerType; 
                        break;
                    case "Name":        dr["Name"] = svrTypeReg.FriendlyName; 
                        break;
                    case "Description": dr["Description"] = svrTypeReg.Description; 
                        break;
                }
            }

            dt.Rows.Add(dr);
        }
    }



    /// <summary>
    /// Class to handle the ServerGroup enum element
    /// </summary>
    internal class EnumServerGroup : EnumRegServerBase
    {
        internal EnumServerGroup()
        {
            ObjectProperty op = new ObjectProperty();
            op.Name = "Name";
            op.Type = "System.String";
            op.ReadOnly = false;
            op.Usage = ObjectPropertyUsages.Filter | ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "Path";
            op.Type = "System.String";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Filter | ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "Description";
            op.Type = "System.String";
            op.ReadOnly = false;
            op.Usage = ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "ServerType";
            op.Type = "System.Guid";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Request;
            this.AddProperty(op);
        }

        /// <summary>
        /// This is called after the data has been retrieved by the final object so the chain is preparing to be freed
        /// Because the objects are persisted between calls free any call specific data
        /// </summary>
        /// <param name="erParent"></param>
        public override EnumResult GetData(EnumResult erParent)
        {
            //get filter info
            String sPath, sName;
            GetFilterOptions(out sPath, out sName);

            ServerTypeRegistrationInfo svrTypeReg = erParent.Data as ServerTypeRegistrationInfo;
            if(erParent.Type != ResultType.Reserved1 || svrTypeReg == null)
            {
                throw new InternalEnumeratorException(SRError.InvalidParentResultType);
            }

            //init result storage
            ArrayList list = BuildRegistrationInfoList(svrTypeReg, sPath, sName, typeof(GroupRegistrationInfo));

            //return result to user
            if( null == sPath )
                sPath = string.Empty; 

            DataTable dt = GetSchemaTable();
            foreach(GroupRegistrationInfo gri in list)
            {
                AddRow(sPath, gri, dt);
            }
            
            return new EnumResult(dt, ResultType.DataTable);
        }
        
        void AddRow(string sPath, GroupRegistrationInfo gri, DataTable dt)
        {
            DataRow dr = dt.NewRow();

            foreach(DataColumn dc in dt.Columns)
            {
                switch(dc.Caption)
                {
                    case "ServerType":  dr["ServerType"] = gri.ServerType; 
                        break;
                    case "Path":        dr["Path"] = sPath; 
                        break;
                    case "Name":        dr["Name"] = gri.FriendlyName; 
                        break;
                    case "Description": dr["Description"] = gri.Description; 
                        break;
                }
            }

            dt.Rows.Add(dr);
        }
    }


    /// <summary>
    /// Class to handle the RegisteredServer enum element
    /// </summary>
    internal class EnumRegisteredServer : EnumRegServerBase
    {
        internal EnumRegisteredServer()
        {
            ObjectProperty op = new ObjectProperty();
            op.Name = "Name";
            op.Type = "System.String";
            op.ReadOnly = false;
            op.Usage = ObjectPropertyUsages.Filter | ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "Description";
            op.Type = "System.String";
            op.ReadOnly = false;
            op.Usage = ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "Login";
            op.Type = "System.String";
            op.ReadOnly = false;
            op.Usage = ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "EncryptedPassword";
            op.Type = "System.String";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "AuthenticationType";
            op.Type = "System.Int32";
            op.ReadOnly = false;
            op.Usage = ObjectPropertyUsages.Request;
            this.AddProperty(op);

            op = new ObjectProperty();
            op.Name = "ServerInstance";
            op.Type = "System.String";
            op.ReadOnly = false;
            op.Usage = ObjectPropertyUsages.Request;
            this.AddProperty(op);
        }

        /// <summary>
        /// This is called after the data has been retrieved by the final object so the chain is preparing to be freed
        /// Because the objects are persisted between calls free any call specific data
        /// </summary>
        /// <param name="erParent"></param>
        public override EnumResult GetData(EnumResult erParent)
        {
            //get filter info
            String sPath, sName;
            GetFilterOptions(out sPath, out sName);

            ServerTypeRegistrationInfo svrTypeReg = erParent.Data as ServerTypeRegistrationInfo;
            if(erParent.Type != ResultType.Reserved1 || svrTypeReg == null)
            {
                throw new InternalEnumeratorException(SRError.InvalidParentResultType);
            }

            //init result storage
            ArrayList list = BuildRegistrationInfoList(svrTypeReg, sPath, sName, typeof(ServerInstanceRegistrationInfo));

            //return result to user
            if( null == sPath )
                sPath = string.Empty; 

            DataTable dt = GetSchemaTable();
            foreach(ServerInstanceRegistrationInfo gri in list)
            {
                AddRow(sPath, gri, dt);
            }
            
            return new EnumResult(dt, ResultType.DataTable);
        }

        
        void AddRow(string sPath, ServerInstanceRegistrationInfo regInst, DataTable dt)
        {
            DataRow dr = dt.NewRow();
            UIConnectionInfo conInfo = regInst.ConnectionInfo;

            foreach(DataColumn dc in dt.Columns)
            {
                switch(dc.Caption)
                {
                    case "Name":
                        dr["Name"] = regInst.FriendlyName;
                        break;
                    case "Description":
                        dr["Description"] = regInst.Description;
                        break;
                    case "Login":
                        dr["Login"] = conInfo.UserName;
                        break;
                    case "EncryptedPassword":
                        dr["EncryptedPassword"] = conInfo.InMemoryPassword;
                        break;
                    case "AuthenticationType":
                        dr["AuthenticationType"] = conInfo.AuthenticationType; 
                        break;
                    case "ServerInstance":
                        dr["ServerInstance"] = conInfo.ServerName; 
                        break;
                }
            }

            dt.Rows.Add(dr);
        }
    }

}
