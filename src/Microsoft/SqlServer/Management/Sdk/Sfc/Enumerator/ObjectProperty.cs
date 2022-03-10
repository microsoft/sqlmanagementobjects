// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Runtime.InteropServices;
    using System.Xml.Serialization;

    /// <summary>
    /// enumerates possible usages for a property
    /// </summary>
    [Flags]
	public enum ObjectPropertyUsages
	{
		/// <summary>
		/// can't be used much
		/// </summary>
		None = 0x00,
		/// <summary>
		/// can be used in filter
		/// </summary>
		Filter = 0x01,
		/// <summary>
		/// can be requested
		/// </summary>
		Request = 0x02,
		/// <summary>
		/// cen be ordered by
		/// </summary>
		OrderBy = 0x04,
		/// <summary>
		/// reserved for usage by the enumerator extensions
		/// for example for parent-children internal link -> = Request
		/// </summary>
		Reserved1 = 0x08,
		/// <summary>
		/// can be used everywhere
		/// </summary>
		All = Filter | Request | OrderBy
	}

    /// <summary>
    /// Enumarates possible modes of a property
    /// </summary>
    [Flags]
    public enum PropertyMode
    {
        /// <summary>
        /// No mode
        /// </summary>
        None = 0x00,
        /// <summary>
        /// Design time property
        /// </summary>
        Design = 0x01,
        /// <summary>
        /// Deploy time property
        /// </summary>
        Deploy = 0x02,
        /// <summary>
        /// Design and deploy time property
        /// </summary>
        All = Design | Deploy
    }
	///<summary>
	/// ObjectProperty description
	///</summary>
	[ComVisible(false)]
	[Serializable]
	public class ObjectProperty
	{
		String m_name;
		String m_type;
		bool m_expensive;
		bool m_readOnly;
		bool m_extendedType;
        bool m_readOnlyAfterCreation;
        short m_keyIndex;
        PropertyMode m_propMode;
	ObjectPropertyUsages m_usage;
        string m_defaultValue;

        // Object reference
        string m_referenceTemplate;
        string m_referenceType;
        string m_referenceKeys;
        string m_referenceTemplateParameters;

		///<summary>
		/// name of the property
		///</summary>
		[XmlAttribute]
		public String Name
		{
			get
			{ return m_name; }
			set
			{ m_name = value; }
		}

		///<summary>
		/// type of the property
		///</summary>
		[XmlAttribute]
		public String Type
		{
			get
			{ return m_type; }
			set
			{ m_type = value; }
		}


        ///<summary>
        /// is the property read only after creation
        ///</summary>
        [XmlAttribute]
        public bool ReadOnlyAfterCreation
        {
            get
            { return m_readOnlyAfterCreation; }
            set
            { m_readOnlyAfterCreation = value; }
        }

        ///<summary>
        /// is the property key idex
        ///</summary>
        [XmlAttribute]
        public short KeyIndex
        {
            get
            { return m_keyIndex; }
            set
            { m_keyIndex = value; }
        }
        ///<summary>
        /// the mode of the property
        ///</summary>
        [XmlAttribute]
        public PropertyMode PropertyMode
        {
            get
            { return this.m_propMode; }
            set
            { this.m_propMode = value; }
        }


		///<summary>
		/// is the property expensive
		///</summary>
		[XmlAttribute]
		public bool Expensive
		{
			get
			{ return m_expensive; }
			set
			{ m_expensive = value; }
		}

		///<summary>
		/// access: Read/Write
		///</summary>
		[XmlAttribute]
		public bool ReadOnly
		{
			get
			{ return m_readOnly; }
			set
			{ m_readOnly = value; }
		}

		/// <summary>
		/// true if it is a type than cannot be stored in a DataTable
		/// </summary>
		/// <value></value>
		[XmlAttribute]
		public bool ExtendedType
		{
			get
			{ return m_extendedType; }
			set
			{ m_extendedType = value; }
		}

		/// <summary>
		/// accepted usages for the property
		/// </summary>
		/// <value></value>
		[XmlAttribute]
		public ObjectPropertyUsages Usage
		{
			get
			{ return m_usage; }
			set
			{ m_usage = value; }
		}

        [XmlAttribute]
        public string DefaultValue 
        {
            get { return m_defaultValue; }
            set { m_defaultValue = value; }
        }

		/// <summary>
		/// URN template for object reference
		/// </summary>
        [XmlAttribute]
        public string ReferenceTemplate 
        {
            get { return m_referenceTemplate; }
            set { m_referenceTemplate = value; }
        }

		/// <summary>
		/// Type pointed to by object reference
		/// </summary>
        [XmlAttribute]
        public string ReferenceType
        {
            get { return m_referenceType; }
            set { m_referenceType = value; }
        }

		/// <summary>
		/// Information about the keys that make up the reference
		/// </summary>
        [XmlAttribute]
        public string ReferenceKeys
        {
            get { return m_referenceKeys; }
            set { m_referenceKeys = value; }
        }
  
		/// <summary>
		/// Information about the keys that make up the reference
		/// </summary>
        [XmlAttribute]
        public string ReferenceTemplateParameters
        {
            get { return m_referenceTemplateParameters; }
            set { m_referenceTemplateParameters = value; }
        }
	}
}
			
