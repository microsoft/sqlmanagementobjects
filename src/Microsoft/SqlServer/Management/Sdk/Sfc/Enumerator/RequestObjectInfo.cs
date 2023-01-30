// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Runtime.InteropServices;
    using System.Xml.Serialization;

    ///<summary>
    /// The Request encapsulates connection info and the actual request
    ///</summary>
    [ComVisible(false)]
	[Serializable]
	public class RequestObjectInfo
	{
		///<summary>
		/// flags indicate what must be retrieved
		///</summary>
		[Flags]
		public enum Flags { 
			///<summary>
			/// retrive none :-) must have something for the implicit 0
			///</summary>
			None = 0,
			///<summary>
			/// retrive properies
			///</summary>
			Properties = 0x1, 
			///<summary>
			/// retrive children
			///</summary>
			Children = 0x2, 
			///<summary>
			/// retrive parents
			///</summary>
			Parents = 0x4,
			/// <summary>
			/// what <see>ResultType</see>s are supported
			/// </summary>
			ResultTypes = 0x8,
			/// <summary>
			/// what properties make up the Urn property
			/// </summary>
			UrnProperties = 0x10,
			///<summary>
			/// retrive all
			///</summary>
			All = Properties | Children | Parents | ResultTypes
			};

		Urn m_urn;
		Flags m_flags;

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

		///<summary>
		/// flags which indicate what to return
		///</summary>
		[XmlAttribute]
		public Flags InfoType
		{
			get
			{ return m_flags; }
			set
			{ m_flags = value; }
		}

		/// <summary>
		/// default constructor
		/// </summary>
		public RequestObjectInfo()
		{
		}

		/// <summary>
		/// initialize with level info and flags
		/// </summary>
		/// <param name="urn">the level for which info is requested</param>
		/// <param name="infoType">what info is requested</param>
		public RequestObjectInfo(Urn urn, Flags infoType)
		{
			Urn = urn;
			InfoType = infoType;
		}
	}
}
			
