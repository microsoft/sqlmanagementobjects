// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices;

    ///	<summary>
    ///SqlRequest extends the enuemrator Request 
    ///with sql extension specific fields</summary>
    [ComVisible(false)]
	public class SqlRequest : Request
	{
		ArrayList m_listLinkField;
		StringCollection m_PrefixPostfixFields;
		bool m_bResolveDatabases;

		///	<summary>
		///default constructor</summary>
		public SqlRequest()
		{
			m_bResolveDatabases = true;
		}

		///	<summary>
		///initialize from a Request</summary>
		public SqlRequest(Request reqUser)
		{
			Urn = reqUser.Urn;
			Fields = reqUser.Fields;
			OrderByList = reqUser.OrderByList;
			ResultType = reqUser.ResultType;
			RequestFieldsTypes = reqUser.RequestFieldsTypes;
		}

		///	<summary>
		///set a list of link fields ( will we need to be have thei value resolved )</summary>
		public void SetLinkFields(ArrayList list)
		{
			m_listLinkField = list;
		}

		///	<summary>
		///if false the database level does not special processing</summary>
		public bool ResolveDatabases
		{
			set { m_bResolveDatabases = value; }
			get { return m_bResolveDatabases; }
		}

		///	<summary>
		///get the list of link fields</summary>
		public ArrayList LinkFields
		{
			get { return m_listLinkField; }
		}

		///	<summary>
		///doesn't look to be used</summary>
		internal StringCollection PrefixPostfixFields
		{
			get 
			{ 
				return m_PrefixPostfixFields; 
			}
			
			set
			{
				m_PrefixPostfixFields = value;
			}
		}

		///	<summary>
		///add the link fields fom another SqlRequest</summary>
		internal void MergeLinkFileds(SqlRequest req)
		{
			if( null == m_listLinkField )
			{
				m_listLinkField = req.LinkFields;
				return;
			}

			foreach(Object o in req.LinkFields )
			{
				m_listLinkField.Add(o);
			}
		}
	}
}
			
