// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Runtime.InteropServices;

    /// <summary>
    ///component of link multiple
    ///specifies the link fileld name, type and 
    ///value that is dynamically resoolved</summary>
    [ComVisible(false)]
	public class LinkField
	{
		String m_field;
		String m_value;
		LinkFieldType m_type;

		/// <summary>
		///type of link field , see LinkFieldType enum</summary>
		public LinkFieldType Type
		{
			get { return m_type; }
			set { m_type = value; }
		}

		/// <summary>
		///link field name</summary>
		public String Field
		{
			get { return m_field; }
			set { m_field = value; }
		}

		/// <summary>
		///place holder for the value calculated for this link field</summary>
		public String Value
		{
			get { return m_value; }
			set { m_value = value; }
		}
	}


	/// <summary>
	///encapsulates a concept used in many constructs in the 
	///xml configuration file: creating an expression based on one or more fields</summary>
	[ComVisible(false)]
	public class LinkMultiple
	{
		String m_no;
		String m_expression;
		ArrayList m_listLink;

		/// <summary>
		///init with xml reader</summary>
		internal void Init(XmlReadMultipleLink xrpl)
		{
			m_listLink = new ArrayList();
			m_no = xrpl.No;
			m_expression = xrpl.Expression;
			XmlReadLinkFields xrlf = xrpl.LinkFields;
			do
			{
				LinkField lf = new LinkField();
				lf.Type = xrlf.Type;
				lf.Field = xrlf.Field;
				lf.Value = xrlf.DefaultValue;
				m_listLink.Add(lf);
			}
			while( xrlf.Next() );
		}

		/// <summary>
		///true if it has link fields
		/// ( if false then we have a constant expression )</summary>
		bool HasLinkFields
		{
			get
			{
				return null != m_listLink;
			}
		}

		/// <summary>
		///set the list of link fields</summary>
		public void SetLinkFields(ArrayList list)
		{
			m_listLink = list;
		}

		/// <summary>
		///get the list of link fields</summary>
		public ArrayList LinkFields
		{
			get 
			{ 
				return m_listLink; 
			}
		}

		/// <summary>
		///get the number of link fields</summary>
		public String No
		{
			get { return m_no; }
			set { m_no = value; }
		}

		/// <summary>
		///calculate the tsql expression based on the format and on the link fields values</summary>
		public String GetSqlExpression(SqlObjectBase obj)
		{
			if( !this.HasLinkFields )
			{
				return m_expression;
			}

			foreach(LinkField f in m_listLink)
			{
				if( LinkFieldType.Computed == f.Type )
				{
					f.Value = obj.ResolveComputedField(f.Field);
				}
				else if( LinkFieldType.Filter == f.Type )
				{
					string s = obj.GetFixedFilterValue(f.Field);
					if( null != s )
					{
						f.Value = s;
					}
					else if( null == f.Value )
					{
						f.Value = String.Empty;
					}
				}
			}

            int count = Int32.Parse(m_no,CultureInfo.InvariantCulture); // using m_listLink.Count is not reliable as it might be different from m_no
            object[] parameters = new object[count];
            for( int i=0; i<count; i++)
            {
                parameters[i] = ((LinkField)m_listLink[i]).Value;
            }

            string result = String.Format(CultureInfo.InvariantCulture, m_expression, parameters);
            return result;
		}

		/// <summary>
		///set the tsql expression to be expr</summary>
		internal void SetSqlExpression(string expr)
		{
			m_expression = expr;
		}
	}
}
