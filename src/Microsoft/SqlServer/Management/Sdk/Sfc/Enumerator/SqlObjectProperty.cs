// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Runtime.InteropServices;

    ///	<summary>
    ///class ecapsulating information about a property </summary>
    [ComVisible(false)]
	public class SqlObjectProperty : ObjectProperty
	{
		String m_value;
		String m_dbType;
		String m_size;
		String m_Alias;
		String m_SessionValue;
		bool m_bCast;
		LinkMultiple m_LinkMultiple;

		///	<summary>
		///intialize from xml reader</summary>
		public SqlObjectProperty(XmlReadProperty xrp)
		{
			Name = xrp.Name;
			Type = xrp.ClrType;
			Expensive = xrp.Expensive;
			ReadOnly = xrp.ReadOnly;
			ExtendedType = xrp.ExtendedType;
			m_dbType = xrp.DbType;
			m_size = xrp.Size;
			Usage = xrp.Usage;
			m_bCast = xrp.Cast;
			m_value = xrp.Value;

			XmlReadMultipleLink xrmpl = xrp.MultipleLink;
			if( null != xrmpl )
			{
				m_LinkMultiple = new LinkMultiple();
				m_LinkMultiple.Init(xrmpl);
			}
		}

		///	<summary>
		///get the link fields 
		///the links necessary to get the value for this property</summary>
		public ArrayList LinkFields
		{
			get 
			{ 
				if( null == m_LinkMultiple )
                {
                    return null;
                }

                return m_LinkMultiple.LinkFields; 
			}
		}

		///	<summary>
		///get/set the tsql value for this property</summary>
		public String Value
		{
			get 
			{
				return m_value;
			}
			set
			{
				m_value = value;
			}
		}

		///	<summary>
		///get the tsql value rendering the link multiple</summary>
		public string GetValue(SqlObjectBase o)
		{
			if (null != m_value)
			{
				return m_value;
			}
			if( null != m_LinkMultiple )
			{
				return m_LinkMultiple.GetSqlExpression(o);
			}
			return null; 
		}

		///	<summary>
		///get the tsql type with the size specified</summary>
		internal string GetTypeWithSize()
		{
			if( null == this.Size )
			{
				return this.DBType;
			}
			return String.Format(CultureInfo.InvariantCulture, "{0}({1})", this.DBType, this.Size);
		}

		///	<summary>
		///get the tsql value with cast if needed</summary>
		internal string GetValueWithCast(SqlObjectBase o)
		{
			if( true == m_bCast )
			{
				return String.Format(CultureInfo.InvariantCulture, "CAST({0} AS {1})", this.GetValue(o), GetTypeWithSize());
			}
			else
			{
				return GetValue(o);
			}
		}

		///	<summary>
		///init a temporary value with the tsql representation of the type</summary>
		void InitSessionValue(SqlObjectBase o)
		{
			this.SessionValue = GetValueWithCast(o);
		}

		///	<summary>
		///get set the temporary value</summary>
		public string SessionValue
		{
			get { return m_SessionValue; }
			set { m_SessionValue = value; }
		}

		///	<summary>
		///get the tsql type without size</summary>
		public String DBType
		{
			get { return m_dbType; }
		}

		///	<summary>
		///get the size</summary>
		public String Size
		{
			get { return m_size; }
		}

		///	<summary>
		///get the alias name for this property</summary>
		public String Alias
		{
			get
			{
				return m_Alias;
			}
			set 
			{ 
				m_Alias = value; 
			}
		}

		///	<summary>
		///ad this property to the StatementBuilder
		///isTriggered=true means that it was not requested by the user but is 
		///necessary for a property requested by the user</summary>
		public void Add(SqlObjectBase o, bool isTriggered)
		{
			InitSessionValue(o);

			o.StatementBuilder.StoreParentProperty(this, isTriggered);
		}
	}
}
