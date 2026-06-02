// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Runtime.InteropServices;
    using System.Xml;
    using System.Xml.Serialization;

    ///<summary>
    /// The Request encapsulates connection info and the actual request
    ///</summary>
    [ComVisible(false)]
    [Serializable]

    public class ObjectInfo
    {
        String[] m_children;
        ObjectProperty[] m_properties;
        ObjectProperty[] m_urnproperties;
        ResultType[] m_resultTypes;
        
        ///<summary>
        /// string
        ///</summary>
        [XmlElement]
        public String[] Children
        {
            get
            { return m_children; }
            set
            { m_children = value; }
        }

        ///<summary>
        /// XPath expression
        ///</summary>
        [XmlElement]
        public ObjectProperty[] Properties
        {
            get
            { return m_properties; }
            set
            { m_properties = value; }
        }

        /// <summary>
        /// list of supprted ResultTypes, the first is the default for the level
        /// </summary>
        /// <value></value>
        [XmlElement]
        public ResultType[] ResultTypes
        {
            get
            { return m_resultTypes; }
            set
            { m_resultTypes = value; }
        }

        /// <summary>
        /// the list of prperties that make up the Urn for the level
        /// </summary>
        /// <value></value>
        public ObjectProperty[] UrnProperties
        {
            get
            { return m_urnproperties; }
            set
            { m_urnproperties = value; }
        }
    }
}
