// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

    internal class PostProcessText : PostProcess
    {
        protected object m_text;
        bool m_btextSet;

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PostProcessText()
        {
            CleanRowData();
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

            m_text = GetTextFor90(data, dp);

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
