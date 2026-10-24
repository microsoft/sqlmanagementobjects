// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Runtime.InteropServices;
    using System.Xml.Serialization;

    /// <summary>
    /// in what storage will the result be retrived for the Request
    /// </summary>
    [ComVisible(false)]
	public enum ResultType 
	{ 
		/// <summary>
		/// each level has prefered storage, the prefered storage for the last 
		/// level will be used
		/// </summary>
		Default, 
		/// <summary>
		/// return as DataSet
		/// </summary>
		DataSet, 
		/// <summary>
		/// return as dataTable
		/// </summary>
		DataTable, 
		/// <summary>
		/// return as IDataReader
		/// </summary>
		IDataReader, 
		/// <summary>
		/// return as XmlDocument
		/// </summary>
		XmlDocument, 
		/// <summary>
		/// reserved for enumerator extensions implementations
		/// to be used for comunication between levels
		/// </summary>
		Reserved1, 
		/// <summary>
		/// reserved for enumerator extensions implementations
		/// to be used for comunication between levels
		/// </summary>
		Reserved2 
	};

	/// <summary>
	/// describes an order by clause
	/// </summary>
	[ComVisible(false)]
	[Serializable]
	public class OrderBy
	{
		///<summary>
		/// direction to order by
		///</summary>
		public enum Direction 
		{ 
			///<summary>
			/// ascending
			///</summary>
			Asc, 
			///<summary>
			/// descending
			///</summary>
			Desc
		};

		String m_field;
		Direction m_direction;
		
		///<summary>
		/// field to order by
		///</summary>
		[XmlAttribute]
		public String Field
		{
			get
			{ return m_field; }
			set
			{ m_field = value; }
		}

		///<summary>
		/// direction to order by
		///</summary>
		[XmlAttribute]
		public Direction Dir
		{
			get
			{ return m_direction; }
			set
			{ m_direction = value; }
		}

		/// <summary>
		/// default constructor
		/// </summary>
		public OrderBy()
		{
		}

		/// <summary>
		/// init an order by clause
		/// </summary>
		/// <param name="field">field to order by</param>
		/// <param name="dir">direction to order by</param>
		public OrderBy(String field, Direction dir)
		{
			Field = field;
			Dir = dir;
		}
	}

	/// <summary>
	/// this component of the <see>Request</see> specifies
	/// which properties has to be returned for a level
	/// </summary>
	[ComVisible(false)]
    [Serializable]
	public class PropertiesRequest
	{
		String[] m_fields;
		RequestFieldsTypes m_fieldsType;
		PropertyAlias m_PropertyAlias;
		OrderBy[] m_orderBy;

		/// <summary>
		/// default constructor
		/// </summary>
		public PropertiesRequest()
		{
			m_fieldsType = RequestFieldsTypes.Request;
			m_PropertyAlias = new PropertyAlias();
		}

		/// <summary>
		/// initalize with the list of fields 
		/// </summary>
		/// <param name="fields">list of fields</param>
		public PropertiesRequest(String[] fields)
		{
			m_fieldsType = RequestFieldsTypes.Request;
			m_PropertyAlias = new PropertyAlias();

			Fields = fields;
		}

		/// <summary>
		/// initialize list of properties and ordering information
		/// </summary>
		/// <param name="fields">list of properties</param>
		/// <param name="orderBy">ordering info</param>
		public PropertiesRequest(String[] fields, OrderBy[] orderBy)
		{
			m_fieldsType = RequestFieldsTypes.Request;
			m_PropertyAlias = new PropertyAlias();

			Fields = fields;
			OrderByList = orderBy;
		}

		///<summary>
		/// properties to be brought back
		///</summary>
		[XmlArrayItem(ElementName = "field", Type = typeof(String)), XmlArray]		
		public String[] Fields
		{
			get
			{ return m_fields; }
			set
			{ m_fields = value; }
		}

		/// <summary>
		/// <see>RequestFieldsTypes</see> describes what the list of fields means
		/// </summary>
		/// <value></value>
		[XmlAttribute]
		public RequestFieldsTypes RequestFieldsTypes
		{
			get
			{ return m_fieldsType; }
			set
			{ m_fieldsType = value; }
		}

		/// <summary>
		/// list of order by clauses
		/// </summary>
		/// <value></value>
		public OrderBy[] OrderByList
		{
			get
			{ return m_orderBy; }
			set
			{ m_orderBy = value; }
		}

		/// <summary>
		/// describes how the property names will be aliased
		/// </summary>
		/// <value></value>
		public PropertyAlias PropertyAlias
		{
			get
			{ return m_PropertyAlias; }
			set
			{ m_PropertyAlias = value; }
		}
	}

	/// <summary>
	/// describes how the property names will be aliased
	/// </summary>
	[ComVisible(false)]
    [Serializable]
	public class PropertyAlias
	{
		/// <summary>
		/// describes the alias method
		/// </summary>
        public enum AliasKind 
		{ 
			/// <summary>
			/// an alias will be specified for each property
			/// </summary>
			Each, 
			/// <summary>
			/// the specified prefix will be added in front of the property names
			/// </summary>
			Prefix, 
			/// <summary>
			/// the level name will be added in front of the property name
			/// </summary>
			NodeName 
		};

		AliasKind m_Kind;
		String m_Prefix;
		String[] m_Aliases;

		/// <summary>
		/// default constructor
		/// </summary>
		public PropertyAlias()
		{
			m_Kind = AliasKind.NodeName;
		}

		/// <summary>
		/// initialize to use a specified prefix
		/// </summary>
		/// <param name="prefix">the prefix to be added in fron of names</param>
		public PropertyAlias(string prefix)
		{
			m_Kind = AliasKind.Prefix;
			m_Prefix = prefix;
		}

		/// <summary>
		/// initialize to use a specifing string for each property
		/// the maching is done using the order
		/// </summary>
		/// <param name="aliases">list of alias names</param>
		public PropertyAlias(string [] aliases)
		{
			m_Kind = AliasKind.Each;
			m_Aliases = aliases;
		}

		/// <summary>
		/// the kind of alias method to be used
		/// </summary>
		/// <value></value>
		public AliasKind Kind
		{
			get
			{ return m_Kind; }
			set
			{ m_Kind = value; }
		}

		/// <summary>
		/// the prefix to be used
		/// </summary>
		/// <value></value>
		public String Prefix
		{
			get
			{ return m_Prefix; }
			set
			{ m_Prefix = value; }
		}

		/// <summary>
		/// the alias list to be used
		/// </summary>
		/// <value></value>
		public String[] Aliases
		{
			get
			{ return m_Aliases; }
			set
			{ m_Aliases= value; }
		}
	}

	/// <summary>
	/// specified how the list of fields specified in <see>Request</see>
	/// is to be interpreted
	/// </summary>
	[Flags]
	public enum RequestFieldsTypes 
	{ 
		/// <summary>
		/// when set, brings properties in the list else rejects them
		/// </summary>
		Request = 1,
		/// <summary>
		/// when set also brings expensive properties, else it does not
		/// </summary>
		IncludeExpensiveInResult = 2,
		/// <summary>
		/// all_properties - (input_list + expensive_props )
		/// </summary>
		Reject = 0
	};

	///<summary>
	/// The Request encapsulates the request options
	///</summary>
	[ComVisible(false)]
	[Serializable]
	public class Request : PropertiesRequest
	{
		Urn m_urn;
		ResultType m_resultType;
		PropertiesRequest[] m_ParentPropertiesRequests;

		///<summary>
		/// XPath expression
		///</summary>
		[XmlElement]
		public Urn Urn
		{
			get
			{ return m_urn; }
			set
			{ m_urn = value; }
		}

		/// <summary>
		/// the list of properties requested for the upper levels
		/// </summary>
		/// <value></value>
		public PropertiesRequest[] ParentPropertiesRequests
		{
			get
			{ return m_ParentPropertiesRequests; }
			set
			{ m_ParentPropertiesRequests = value; }
		}

		/// <summary>
		/// the requested <see>ResultType</see>
		/// </summary>
		/// <value></value>
		[XmlAttribute]
		public ResultType ResultType
		{
			get
			{ return m_resultType; }
			set
			{ m_resultType = value; }
		}

		/// <summary>
		/// default constructor
		/// </summary>
		public Request()
		{
			m_resultType = ResultType.Default;
			this.PropertyAlias = null;
		}

		/// <summary>
		/// initalize with xpath
		/// </summary>
		/// <param name="urn">the xpath to be queried for</param>
		public Request(Urn urn)
		{
			Urn = urn;
			m_resultType = ResultType.Default;
			this.PropertyAlias = null;
		}

		/// <summary>
		/// initialize with xpath and requeste fields list
		/// </summary>
		/// <param name="urn">the xpath to be queried for</param>
		/// <param name="fields">requeste fields list</param>
		public Request(Urn urn, String[] fields) : base(fields, null)
		{
			Urn = urn;
			m_resultType = ResultType.Default;
			this.PropertyAlias = null;
		}

		/// <summary>
		/// initialize with xpath and requeste fields list
		/// </summary>
		/// <param name="urn">the xpath to be queried for</param>
		/// <param name="fields">requeste fields list</param>
		/// <param name="orderBy">order by clauses</param>
		public Request(Urn urn, String[] fields, OrderBy[] orderBy) : base(fields, orderBy)
		{
			Urn = urn;
			m_resultType = ResultType.Default;
			this.PropertyAlias = null;
		}

		/// <summary>
		/// make a shalow clobe of the <see>Request</see>
		/// </summary>
		/// <returns></returns>
		internal Request ShallowClone()
		{
			Request req = new Request();
			req.Urn = this.Urn;
			req.Fields = this.Fields;
			req.OrderByList = this.OrderByList;
			req.ResultType = this.ResultType;
			req.PropertyAlias = this.PropertyAlias;
			req.ParentPropertiesRequests = this.ParentPropertiesRequests;
			req.RequestFieldsTypes = this.RequestFieldsTypes;
			req.PropertyAlias = this.PropertyAlias;
			return req;
		}
	}
}
			
