// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

    internal class PostProcessText : PostProcess
    {
        protected object m_text;
        bool m_btextSet;
        DataTable m_dtText;

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PostProcessText()
        {
            CleanRowData();
            m_dtText = null;
        }

        protected DataTable GetTextForAllRows()
        {
            Request reqtext = new Request();
            reqtext.Urn = this.Request.Urn.Value + "/Text";

            reqtext.ResultType = ResultType.DataTable;
            reqtext.RequestFieldsTypes = RequestFieldsTypes.Request;
            reqtext.Fields = new String[] { "ObjectIdentifier", "ID", "Text" };

            reqtext.OrderByList = new OrderBy [] 
            {
                new OrderBy("ObjectIdentifier", OrderBy.Direction.Asc),
                new OrderBy("ID", OrderBy.Direction.Asc)
            };

            return new Enumerator().Process(this.ConnectionInfo, reqtext);
        }

        protected String GetTextForObject(string sObjectIdentifier)
        {
            int i = BinarySearchSetOnFirst(m_dtText.Rows, sObjectIdentifier, "ObjectIdentifier");

            if( 0 > i )
            {
                return string.Empty; // this can't be
            }

            String s = String.Empty;
            do
            {
                s += (String)m_dtText.Rows[i++]["Text"];
            }
            while( i < m_dtText.Rows.Count && "1" != m_dtText.Rows[i]["ID"].ToString() );
            return s;
        }

        protected bool IsTextSet
        {
            get { return m_btextSet; }
        }

        protected void SetText(object data, DataProvider dp)
        {
            m_btextSet = true;
            if( IsNull(data) )
            {
                m_text = String.Empty;
                return;
            }

            if( ExecuteSql.GetServerVersion(this.ConnectionInfo).Major < 9 )
            {
                if( null == m_dtText )
                {
                    m_dtText = GetTextForAllRows();
                }
                m_text = GetTextForObject((string)data);
            }
            else
            {
                m_text = GetTextFor90(data, dp);
            }

            if( null == m_text )
            {
                m_text = string.Empty;
            }
        }

        protected virtual string GetTextFor90(object data, DataProvider dp)
        {
            return (string)data;
        }

        protected override bool SupportDataReader
        {
            get 
            { 
                if( ExecuteSql.GetServerVersion(this.ConnectionInfo).Major < 9 )
                {
                    return false;
                }
                return true;
            }
        }

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if( !this.IsTextSet )
            {
                SetText(data, dp);
            }
            return m_text;
        }

        public override void CleanRowData()
        {
            m_btextSet = false;
            m_text = null;
        }
    }



    internal class PostProcessBodyText : PostProcessText
    {
        int m_idx;
        int m_idxEnd;

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PostProcessBodyText()
        {
            CleanRowData();
        }

        public override void CleanRowData()
        {
            base.CleanRowData();
            m_idx = -2;
            m_idxEnd = -2;
        }

        int IdxStart
        {
            get
            {
                if( -2 == m_idx )
                {
                    m_idx = DdlTextParser.ParseDdlHeader((string)m_text);
                }
                return m_idx;
            }
        }

        int IdxEnd
        {
            get
            {
                if( "View" == this.ObjectName && -2 == m_idxEnd )
                {
                    m_idxEnd = DdlTextParser.ParseCheckOption((string)m_text);
                }
                return m_idxEnd;
            }
        }

        bool HasParantesis
        {
            get
            {
                if( -2 == m_idx )
                {
                    DdlTextParser.ParseDdlHeader((string)m_text);
                }
                return DdlTextParser.ddlTextParserSingleton.hasParanthesis;
            }
        }

        string TableVariableName
        {
            get
            {
                if( -2 == m_idx )
                {
                    DdlTextParser.ParseDdlHeader((string)m_text);
                }
                return DdlTextParser.ddlTextParserSingleton.returnTableVariableName;
            }
        }

        protected override string GetTextFor90(object data, DataProvider dp)
        {
            return GetTriggeredString(dp, 0);
        }

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if( !this.IsTextSet )
            {
                Object objData = GetTriggeredObject(dp, 0);
                SetText(objData, dp);
            }
            if( IsNull(m_text) )
            {
                return m_text;
            }
            switch(name)
            {
                case "Text":
                    return m_text;
                case "TextBody":
                    return ((string)m_text).Remove(0, this.IdxStart);
                case "BodyStartIndex":
                    return this.IdxStart;
                case "HasColumnSpecification":
                    return this.HasParantesis;
                case "TableVariableName":
                    if( null != this.TableVariableName )
                    {
                        return this.TableVariableName;
                    }
                    return string.Empty;
            }
            return data;
        }

        protected override bool SupportDataReader
        {
            get 
            { 
                return false;
            }
        }
    }

}
