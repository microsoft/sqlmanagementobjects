// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;


namespace Microsoft.SqlServer.Management.Smo
{
    internal class SqlObjectUnion : SqlObject
    {
        ArrayList m_listObjects;

        SqlObjectUnion()
        {
            m_listObjects = new ArrayList();
        }

        public override ResultType[] ResultTypes
        {
            get	{return new ResultType[2] { ResultType.DataTable, ResultType.DataSet };	}
        }

        public override Request RetrieveParentRequest()
        {
            SqlRequest req = (SqlRequest)base.RetrieveParentRequest();
            foreach(SqlObject o in m_listObjects)
            {
                o.Request = this.Request;
                o.SetUrn(this.Urn);
                req.MergeLinkFileds((SqlRequest)o.RetrieveParentRequest());
            }
            return req;
        }

        void ProcessStatementBuilder(SqlEnumResult ser, SqlObject o, StringBuilder sql)
        {
            StatementBuilder sb = ser.StatementBuilder;
            ser.StatementBuilder = sb.MakeCopy();
            o.PrepareGetData(ser);
            ser.StatementBuilder = sb;

            o.StatementBuilder.AddStoredProperties();
            sql.Append(o.StatementBuilder.InternalSelect());
        }

        //get the data from the object
        public override EnumResult GetData(EnumResult erParent)
        {
            StringBuilder sql = new StringBuilder();
            SqlEnumResult ser = (SqlEnumResult)erParent;

            ProcessStatementBuilder(ser, this, sql);

            foreach(SqlObject o in m_listObjects)
            {
                sql.Append("\nUNION\n");

                ProcessStatementBuilder(ser, o, sql);
            }
            this.StatementBuilder.SetInternalSelect(sql);
            ser.StatementBuilder = this.StatementBuilder;
            //transform the StamentBuilder in whatever is asked in Request
            return BuildResult(erParent);
        }

        public override void PostProcess(EnumResult erChildren)
        {
            base.PostProcess(erChildren);
            foreach(SqlObject o in m_listObjects)
            {
                o.PostProcess(erChildren);
            }
        }

        public override void Initialize(Object ci, XPathExpressionBlock block)
        {
            base.Initialize(ci, block);

            foreach(SqlObject o in m_listObjects)
            {
                o.Initialize(ci, block);
            }
        }

        internal protected override void LoadAndStore(XmlReadDoc xrd, Assembly assembly, StringCollection requestedFields, StringCollection roAfterCreation)
        {
            xrd.ReadUnion();
            base.LoadAndStore(xrd, assembly, requestedFields, roAfterCreation);

            while( xrd.ReadUnion() )
            {
                SqlObject o = new SqlObject();

                o.LoadAndStore(xrd, assembly, requestedFields, roAfterCreation);
                m_listObjects.Add(o);
            }
        }
    }
}
