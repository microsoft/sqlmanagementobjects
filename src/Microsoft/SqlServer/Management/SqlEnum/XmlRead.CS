// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.IO;
    using System.Reflection;
    using System.Collections.Specialized;
    using Microsoft.SqlServer.Management.Common;
    using System.Runtime.InteropServices;
#if !NETSTANDARD2_0 && !NETCOREAPP
    using Microsoft.SqlServer.Smo.UnSafeInternals;
#endif
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo.SqlEnum;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    ///base class for parsing a xml configuration file</summary>
    [ComVisible(false)]
    internal class XmlRead
    {
#if !NETSTANDARD2_0
        private XmlTextReader m_reader;
#else
        private Microsoft.SqlServer.Management.Sdk.Sfc.XmlTextReader m_reader;
#endif

        private String m_alias;
        private bool m_closed;
        private ServerVersion m_version;
        private DatabaseEngineEdition m_DatabaseEngineEdition;
        private DatabaseEngineType m_databaseEngineType;

#region String Constants
        protected const string ATTR_MIN_MAJOR = "min_major";
        protected const string ATTR_MAX_MAJOR = "max_major";
        protected const string ATTR_MIN_MINOR = "min_minor";
        protected const string ATTR_MAX_MINOR = "max_minor";
        protected const string ATTR_MIN_BUILD = "min_build";
        protected const string ATTR_MAX_BUILD = "max_build";
        protected const string ATTR_CLOUD_MIN_MAJOR = "cloud_min_major";
        protected const string ATTR_CLOUD_MAX_MAJOR = "cloud_max_major";
        protected const string ATTR_CLOUD_MIN_MINOR = "cloud_min_minor";
        protected const string ATTR_CLOUD_MAX_MINOR = "cloud_max_minor";
        protected const string ATTR_CLOUD_MIN_BUILD = "cloud_min_build";
        protected const string ATTR_CLOUD_MAX_BUILD = "cloud_max_build";
        protected const string ATTR_DATAWAREHOUSE_ENABLED = "datawarehouse_enabled";
        #endregion //String Constants

        /// <summary>
        ///get the XmlTextReader</summary>
#if !NETSTANDARD2_0
        protected XmlTextReader Reader
#else
        protected Microsoft.SqlServer.Management.Sdk.Sfc.XmlTextReader Reader
#endif
        {
            get { return m_reader; }
            set { m_reader = value; }
        }

        /// <summary>
        ///true if the current element has been fully read</summary>
        protected bool Closed
        {
            get { return m_closed; }
            set { m_closed = value; }
        }
        /// <summary>
        ///get database engine type</summary>
        public DatabaseEngineType DatabaseEngineType
        {
            get { return m_databaseEngineType; }
            set { m_databaseEngineType = value; }
        }

        /// <summary>
        ///get server version</summary>
        public ServerVersion Version
        {
            get { return m_version; }
            set { m_version = value; }
        }

        /// <summary>
        /// The DatabaseEngineEdition for the server connection
        /// </summary>
        public DatabaseEngineEdition DatabaseEngineEdition
        {
            get { return m_DatabaseEngineEdition; }
            set { m_DatabaseEngineEdition = value; }
        }

        /// <summary>
        ///an alias that is to be used when parsing the file</summary>
        public String Alias
        {
            get { return m_alias; }
            set { m_alias = value; }
        }

        /// <summary>
        ///intialize from another XmlReader</summary>
        public XmlRead(XmlRead xmlReader)
        {
            this.Reader = xmlReader.Reader;
            this.Version = xmlReader.Version;
            this.DatabaseEngineType = xmlReader.DatabaseEngineType;
            this.Alias = xmlReader.Alias;
            this.DatabaseEngineEdition = xmlReader.DatabaseEngineEdition;
            this.Closed = false;
        }

        public XmlRead(ServerVersion version, String serverAlias, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            this.Reader = null;
            this.Version = version;
            this.Alias = serverAlias;
            this.Closed = false;
            this.DatabaseEngineType = databaseEngineType;
            this.DatabaseEngineEdition = databaseEngineEdition;
        }

        /// <summary>
        ///default constructor</summary>
        public XmlRead()
        {
            this.Closed = false;
        }

        /// <summary>
        ///finish parsing this element; step until the next element</summary>
        public virtual void Close()
        {
            if( !this.Closed )
            {
                XmlUtility.SelectNextElement(this.Reader);
            }
        }

        /// <summary>
        ///skip this element</summary>
        public virtual void Skip()
        {
            if( !this.Closed )
            {
                XmlUtility.SelectNextElementOnLevel(this.Reader);
            }
        }

        /// <summary>
        /// Check if the current Element has the specified name, skipping any
        /// version elements along the way for which the version requirements
        /// aren't met
        /// </summary>
        protected bool IsElementWithCheckVersion(String elemName)
        {
            //NOTE - This method is mostly duplicated in smo/ManagementSDK/SFC/Enumerator/core/src/XmlRead.cs
            //       Until we can look into merging them into a single method make sure any changes are made
            //       to both

            if (null == this.Version)
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.NullVersionOnLoadingCfgFile);
            }

            //Loop over all version elements, skipping any which don't meet their
            //requirements, until we reach the end (return false) or get to a
            //non-version element so we can check its name
            while( XmlUtility.IsElement(this.Reader, "version") )
            {

                String sMin = this.Reader[ATTR_MIN_MAJOR];
                String cloudMin = this.Reader[ATTR_CLOUD_MIN_MAJOR];
                String datawarehouseEnabled = this.Reader[ATTR_DATAWAREHOUSE_ENABLED];

                // A minimum major version or datawarehouse_enabled is required
                if (String.IsNullOrEmpty(sMin) &&
                    String.IsNullOrEmpty(cloudMin) &&
                    String.IsNullOrEmpty(datawarehouseEnabled))
                {
                    // $Issue - Exception "IncorectVersionTag" that was raised here does not provide enough
                    //          context for users to debug failures
                    throw new InternalEnumeratorException(StringSqlEnumerator.IncorrectVersionTag(this.Reader.ReadOuterXml()));
                }

                //Whether we should skip this version node (because of version
                //requirements not being met)
                bool shouldSkip = false;

                //Datawarehouse doesn't use server version, it's either enabled or not
                if (this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
                {
                    bool isDatawarehouseEnabled = false;

                    if (datawarehouseEnabled != null &&
                        !Boolean.TryParse(datawarehouseEnabled, out isDatawarehouseEnabled))
                    {
                        throw new InvalidConfigurationFileEnumeratorException(
                            string.Format(StringSqlEnumerator.InvalidAttributeValue, datawarehouseEnabled,
                                ATTR_DATAWAREHOUSE_ENABLED));
                    }

                    // If this isn't marked as datawarehouse enabled then
                    // we should just skip this version node
                    shouldSkip = !isDatawarehouseEnabled;
                }
                else
                {
                    String sMax = string.Empty;
                    String mMin = string.Empty;
                    String mMax = string.Empty;
                    String bMin = string.Empty;
                    String bMax = string.Empty;

                    //All other connection types use the normal versioning logic
                    if (this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                    {
                        sMin = cloudMin;
                        sMax = this.Reader[ATTR_CLOUD_MAX_MAJOR];
                        mMin = this.Reader[ATTR_CLOUD_MIN_MINOR];
                        mMax = this.Reader[ATTR_CLOUD_MAX_MINOR];
                        bMin = this.Reader[ATTR_CLOUD_MIN_BUILD];
                        bMax = this.Reader[ATTR_CLOUD_MAX_BUILD];
                    }
                    else
                    {
                        sMax = this.Reader[ATTR_MAX_MAJOR];
                        mMin = this.Reader[ATTR_MIN_MINOR];
                        mMax = this.Reader[ATTR_MAX_MINOR];
                        bMin = this.Reader[ATTR_MIN_BUILD];
                        bMax = this.Reader[ATTR_MAX_BUILD];
                    }

                    // get the minimum supported version number
                    int minMajor = String.IsNullOrEmpty(sMin) ? Int32.MaxValue : Int32.Parse(sMin);
                    int minMinor = String.IsNullOrEmpty(mMin) ? 0 : Int32.Parse(mMin);
                    int minBuild = String.IsNullOrEmpty(bMin) ? 0 : Int32.Parse(bMin);
                    Version minimumVersion = new Version(minMajor, minMinor, minBuild);

                    // Get the maximum supported version number.  Note that if the maximum
                    // major version number is omitted, then the maximum build number is infinite.
                    //
                    // If the maximum minor, or maximum builds are omited, infinite minor or build value will be used
                    // This means that a tag specifying something like <version min_major=7 max_major=8> will include all
                    // versions bigger or equal with 7.0.0 AND smaller or equal with 8.MAX.MAX
                    int maxMajor = String.IsNullOrEmpty(sMax) ? Int32.MaxValue : Int32.Parse(sMax);
                    int maxMinor = String.IsNullOrEmpty(mMax) ? Int32.MaxValue : Int32.Parse(mMax);
                    int maxBuild = String.IsNullOrEmpty(bMax) ? Int32.MaxValue : Int32.Parse(bMax);

                    Version maximumVersion = new Version(maxMajor, maxMinor, maxBuild);

                    Version currentVersion = new Version(this.Version.Major, this.Version.Minor,
                        this.Version.BuildNumber);


                    // Version requirements are not met so we should skip this node
                    if ((currentVersion < minimumVersion) || (maximumVersion < currentVersion))
                    {
                        shouldSkip = true;
                    }
                }



                if (shouldSkip)
                {
                    //One of the checks failed so skip to the next
                    //sibling if it exists
                    if (!XmlUtility.SelectNextSibling(this.Reader))
                    {
                        XmlUtility.SelectNextElement(this.Reader);
                        return false;
                    }
                }
                else
                {
                    //Passed all checks so select the next element
                    //(should be a child of the version node)
                    XmlUtility.SelectNextElement(this.Reader);
                }
            }

            //Done with version nodes so we should be at the next element now
            //and can check the name
            return XmlUtility.IsElement(this.Reader, elemName);
        }

        /// <summary>
        ///apply the alias on the string str and return the result</summary>
        protected String GetAliasString(String str)
        {
            if( null == this.Alias || null == str )
            {
                return str;
            }
            return String.Format(CultureInfo.InvariantCulture, str, this.Alias);
        }

        /// <summary>
        ///read a 'fields' entry ( list of fields separated by '#' )</summary>
        static protected StringCollection GetFields(string fields)
        {
            var fieldName = new StringBuilder();
            var col = new StringCollection();

            if( null != fields )
            {
                foreach(var c in fields)
                {
                    if( '#' == c )
                    {
                        if( 0 < fieldName.Length )
                        {
                            col.Add(fieldName.ToString());
                        }
                        fieldName = new StringBuilder();
                        continue;
                    }
                    fieldName.Append(c);
                }
            }
            return col;
        }

        /// <summary>
        ///reads the text of the current element if available
        ///it assumes the current element is empty or has inside it either a text node or
        ///an element node. probably a link_multiple.</summary>
        protected string GetTextOfElement()
        {
            if( this.Reader.IsEmptyElement )
            {
                return null;
            }
            //search inside this element
            while( this.Reader.Read() && XmlNodeType.EndElement != this.Reader.NodeType )
            {
                //it's not text so get out
                if( XmlNodeType.Element == this.Reader.NodeType )
                {
                    break;
                }

                //we found text
                if( XmlNodeType.Text == this.Reader.NodeType )
                {
                    //so get its value
                    return this.Reader.Value;
                }
            }
            return null;
        }
    }

    /// <summary>
    ///read an element that is repated more then once
    /// ( e.g. a list of property tags )</summary>
    [ComVisible(false)]
    internal class XmlReadRepeated : XmlRead
    {
        /// <summary>
        ///initialize with the reader</summary>
        public XmlReadRepeated(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///default constructor</summary>
        public XmlReadRepeated()
        {
        }

        /// <summary>
        ///step to the next element
        ///return false if there are no more siblings</summary>
        virtual public bool Next()
        {
            this.Closed = true;
            return XmlUtility.SelectNextSibling(this.Reader);
        }

        /// <summary>
        ///step to the next element with the given name</summary>
        public bool Next(String elemName)
        {
            this.Closed = true;
            if( !XmlUtility.SelectNextElement(this.Reader) )
            {
                return false;
            }

            return IsElementWithCheckVersion(elemName);
        }
    }

    /// <summary>
    ///read an xml configuration file</summary>
    [ComVisible(false)]
    internal class XmlReadDoc : XmlRead
    {
        Stream m_fs;

        /// <summary>
        ///initialize server version and alias</summary>
        public XmlReadDoc(ServerVersion version, String serverAlias, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
            : base(version, serverAlias, databaseEngineType, databaseEngineEdition)
        {
        }

        /// <summary>
        ///load the file attached as a resource to the assembly</summary>
        public void LoadFile(Assembly a, String strFile)
        {
#if SMOCODEGEN
    // This temporary solution is needed to accomodate filenames in form "xml='Database.xml' last='true'"
    // Currently there is only one
            Regex rx = new Regex(@"xml='(?<filename>\w+.xml)'.*last='\w+'", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Match match = rx.Match(strFile);
            if( match.Success )
            {
                strFile = match.Groups["filename"].Value;
            }

            // Another temporary solution to accomodate NS files
            rx = new Regex(@"NS\.(?<filename>\w+.xml)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            match = rx.Match(strFile);
            if( match.Success )
            {
                strFile = match.Groups["filename"].Value;
            }

            string realPath = CodeGenSettings.Directory;

            // Additional paths to look into. Pray that they have no duplicates!
            string[] subPaths = {
                @"..\..\..\SqlEnum\xml",
                @"..\..\..\SqlEnum\xml\endpoint",
                @"..\..\..\SqlEnum\xml\extendedproperty",
                @"..\..\..\SqlEnum\xml\include",
                @"..\..\..\SqlEnum\xml\perm",
                @"..\..\..\Smo.Broker\xml",
                @"..\..\..\SMO\Enumerator\sql\src\xml",
                @"..\..\..\SMO\Enumerator\sql\src\xml\endpoint",
                @"..\..\..\SMO\Enumerator\sql\src\xml\extendedproperty",
                @"..\..\..\SMO\Enumerator\sql\src\xml\include",
                @"..\..\..\SMO\Enumerator\sql\src\xml\perm",
                @"..\..\..\SMO\Enumerator\ServiceBroker\src\xml" };

            string realFile = "";
            foreach( string subPath in subPaths )
            {
                string tmpName = Path.Combine(realPath, subPath, strFile);
                if( File.Exists( tmpName ) )
                {
                    realFile = tmpName;
                    break;
                }
            }

            if( realFile.Length == 0 )
            {
                // Despite all the temporary solutions, we were unable to find the right XML file. Die painful death.
                throw new InvalidConfigurationFileEnumeratorException(StringSqlEnumerator.FailedToLoadResFileFromPaths(strFile, string.Join(System.Environment.NewLine, subPaths.Select(p => "'" + Path.GetFullPath(Path.Combine(realPath, p)) + "'"))));
            }

            m_fs = new FileStream(realFile, FileMode.Open,FileAccess.Read, FileShare.Read);
#elif !NETSTANDARD2_0 && !NETCOREAPP
            m_fs = ManagementUtil.LoadResourceFromAssembly(a, strFile);
#else
            m_fs = a.GetManifestResourceStream(a.GetName().Name + "." + strFile) ??
                   a.GetManifestResourceStream(strFile);
#endif
            if (null == m_fs)
            {
                throw new InvalidConfigurationFileEnumeratorException(StringSqlEnumerator.FailedToLoadResFileFromAssembly(strFile, a.CodeBase));
            }
            LoadInternal();
        }

        /// <summary>
        /// Initializes ReadXml from the given Stream. Ownership of the Stream is taken over by the XmlReadDoc
        /// </summary>
        /// <param name="xmlStream"></param>
        public void LoadXml(Stream xmlStream)
        {
            m_fs = xmlStream;
            LoadInternal();
        }

        private void LoadInternal()
        {

#if NETSTANDARD2_0
            this.Reader = new Microsoft.SqlServer.Management.Sdk.Sfc.XmlTextReader(m_fs, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
#else
            this.Reader = new XmlTextReader(m_fs) { DtdProcessing = DtdProcessing.Prohibit };
#endif
            if ( XmlUtility.SelectNextElement(this.Reader) )
            {
                if( !XmlUtility.IsElement(this.Reader, "EnumObject") )
                {
                    throw new InvalidConfigurationFileEnumeratorException(StringSqlEnumerator.EnumObjectTagNotFound);
                }

                String cloudMin = this.Reader[ATTR_CLOUD_MIN_MAJOR];
                String sMin = this.Reader[ATTR_MIN_MAJOR];
                String datawarehouseEnabled = this.Reader[ATTR_DATAWAREHOUSE_ENABLED];

                //We require at least one of these three attributes
                if (String.IsNullOrEmpty(sMin) &&
                    String.IsNullOrEmpty(cloudMin) &&
                    String.IsNullOrEmpty(datawarehouseEnabled))
                {
                    throw new InternalEnumeratorException(StringSqlEnumerator.IncorrectVersionTag(this.Reader.ReadOuterXml()));
                }

                if (this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
                {
                    bool isDatawarehouseEnabled = false;

                    if (datawarehouseEnabled != null &&
                        !Boolean.TryParse(datawarehouseEnabled, out isDatawarehouseEnabled))
                    {
                        throw new InvalidConfigurationFileEnumeratorException(
                            string.Format(StringSqlEnumerator.InvalidAttributeValue, datawarehouseEnabled,
                                ATTR_DATAWAREHOUSE_ENABLED));
                    }

                    if (!isDatawarehouseEnabled)
                    {
                        //If we're loading for a Datawarehouse DB and this object isn't specifically enabled for that
                        //it's considered unsupported so throw
                        throw new InvalidVersionEnumeratorException(StringSqlEnumerator.ObjectNotSupportedOnSqlDw);
                    }
                }
                else
                {
                    //Not datawarehouse, do normal version checks

                    String sMax = string.Empty;
                    if (this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                    {
                        sMin = cloudMin;
                        sMax = this.Reader[ATTR_CLOUD_MAX_MAJOR];



                    }
                    else
                    {
                        sMax = this.Reader[ATTR_MAX_MAJOR];
                    }

                    if (String.IsNullOrEmpty(sMax))
                    {
                        sMax = Int32.MaxValue.ToString();
                    }
                    if (String.IsNullOrEmpty(sMin))
                    {
                        sMin = Int32.MaxValue.ToString();
                    }

                    if (Int32.Parse(sMin, CultureInfo.InvariantCulture) > this.Version.Major ||
                        Int32.Parse(sMax, CultureInfo.InvariantCulture) < this.Version.Major)
                    {
                        switch (this.Version.Major)
                        {
                            case 9:
                                throw new InvalidVersionEnumeratorException(
                                    StringSqlEnumerator.InvalidSqlServer(StringSqlEnumerator.SqlServer90Name));
                            case 8:
                                throw new InvalidVersionEnumeratorException(
                                    StringSqlEnumerator.InvalidSqlServer(StringSqlEnumerator.SqlServer80Name));
                            //version 7.0 or earlier is not supported, therefore verion number is used.
                            default:
                                if (this.DatabaseEngineType == DatabaseEngineType.Standalone)
                                {
                                    throw new InvalidVersionEnumeratorException(
                                        StringSqlEnumerator.InvalidVersion(this.Version.ToString()));
                                }
                                else
                                {
                                    throw new InvalidVersionEnumeratorException(
                                        StringSqlEnumerator.InvalidVersion(this.Version.ToString() + " " +
                                                                           this.DatabaseEngineType.ToString()));
                                }
                        }
                    }
                }

                //We passed all the version checks without throwing so advance the reader and return
                if( XmlUtility.SelectNextElement(this.Reader) )
                {
                    return;
                }
            }
            throw new InvalidConfigurationFileEnumeratorException(StringSqlEnumerator.InvalidConfigurationFile);
        }

        /// <summary>
        ///close the reader</summary>
        public override void Close()
        {
            this.Reader = null;
            m_fs.Dispose();
        }

        /// <summary>
        ///try to read an UNION tag, return true if succesfull</summary>
        public bool ReadUnion()
        {
            if( IsElementWithCheckVersion("union") &&
                XmlUtility.SelectNextElement(this.Reader) )
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///read a SETTINGS tag</summary>
        public XmlReadSettings Settings
        {
            get
            {
                if( !IsElementWithCheckVersion("settings") )
                {
                    throw new InvalidConfigurationFileEnumeratorException(StringSqlEnumerator.MissingSection("settings"));
                }
                return new XmlReadSettings(this);
            }
        }

        /// <summary>
        ///read a PROPERTIES tag</summary>
        public XmlReadProperties Properties
        {
            get
            {
                if( !IsElementWithCheckVersion("properties") && !XmlUtility.IsElement(this.Reader, "properties") )
                {
                    throw new InvalidConfigurationFileEnumeratorException(StringSqlEnumerator.MissingSection("properties"));
                }

                XmlUtility.SelectNextElement(this.Reader);
                return new XmlReadProperties(this);
            }
        }
    }

    /// <summary>
    ///class for reading a list of properties</summary>
    [ComVisible(false)]
    internal class XmlReadProperties : XmlRead
    {
        /// <summary>
        ///intialize with reader</summary>
        public XmlReadProperties(XmlRead xmlReader) : base(xmlReader)
        {

        }

        /// <summary>
        ///get a PROPERTY</summary>
        public XmlReadProperty Property
        {
            get
            {
                if( IsElementWithCheckVersion("property") )
                {
                    return new XmlReadProperty(this);
                }

                return null;
            }
        }

        /// <summary>
        ///get an INCLUDE</summary>
        public XmlReadInclude Include
        {
            get
            {
                if( IsElementWithCheckVersion("include") )
                {
                    return new XmlReadInclude(this);
                }

                return null;
            }
        }
    }

    /// <summary>
    ///class for reading an INCLUDE</summary>
    [ComVisible(false)]
    internal class XmlReadInclude : XmlRead
    {
        /// <summary>
        ///intialize with reader</summary>
        public XmlReadInclude(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - file to be included</summary>
        public String File
        {
            get{ return this.Reader["file"]; }
        }

        /// <summary>
        ///get attribute - alias to be used</summary>
        public String TableAlias
        {
            get{ return GetAliasString(this.Reader["alias"]); }
        }

        /// <summary>
        ///get attribute - fields to be loaded</summary>
        public StringCollection RequestedFields
        {
            get{ return GetFields(this.Reader["for"]); }
        }

        /// <summary>
        ///get attribute - fields to be loaded</summary>
        public StringCollection ROAfterCreation
        {
            get { return GetFields(this.Reader["ro_after_creation"]); }
        }
    }

    /// <summary>
    ///class for reading a property</summary>
    [ComVisible(false)]
    internal class XmlReadProperty : XmlRead
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadProperty(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - property name</summary>
        public String Name
        {
            get{ return this.Reader["name"]; }
        }

        /// <summary>
        ///get attribute - property is readonly</summary>
        public bool ReadOnly
        {
            get
            {
                String val = this.Reader["read_only"];
                if( null != val )
                {
                    return Boolean.Parse(val);
                }

                val = this.Reader["access"];
                if( null != val )
                {
                    if( 0 == String.Compare("read", val, StringComparison.OrdinalIgnoreCase) )
                    {
                        return true;
                    }
                    return false;
                }
                String strName = Name;
                if( "Urn" == strName || "ID" == strName || "Name" == strName || "CreateDate" == strName || "Schema" == strName )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        ///get the clr type for this property</summary>
        public String ClrType
        {
            get
            {
                String strType = this.Reader["report_type"];
                if( null != strType )
                {
                    return "Microsoft.SqlServer.Management.Smo." + strType;
                }

                strType = this.Reader["report_type2"];
                if( null != strType )
                {
                    return strType;
                }

                return Util.DbTypeToClrType(this.Reader["type"]);
            }
        }

        /// <summary>
        ///get attribute - if it is extended</summary>
        public bool ExtendedType
        {
            get { return null != this.Reader["report_type"]; }
        }

        /// <summary>
        ///get attribute - database type</summary>
        public String DbType
        {
            get
            {
                return this.Reader["type"];
            }
        }

        /// <summary>
        ///get attribute - if it is expensive</summary>
        public bool Expensive
        {
            get
            {
                String s = this.Reader["expensive"];
                if( null == s )
                {
                    return false;
                }

                return Boolean.Parse(s);
            }
        }

        /// <summary>
        ///get attribute - if it is read only after creation</summary>
        public bool ReadOnlyAfterCreation
        {
            get
            {
                String s = this.Reader["read_only_after_creation"];
                if (null == s)
                {
                    return false;
                }

                return Boolean.Parse(s);
            }
        }

        /// <summary>
        ///get attribute - if it is key</summary>
        public short KeyIndex
        {
            get
            {
                String s = this.Reader["key_index"];
                if (null == s)
                {
                    return -1;
                }

                return short.Parse(s);
            }
        }

        ///<summary>
        ///get attribute - mode of the property
        ///</summary>
        public PropertyMode PropertyMode
        {
            get
            {
                String s = this.Reader["mode"];
                PropertyMode mode = PropertyMode.None;
                if (null != s)
                {
                    s = s.ToUpperInvariant();

                    if (-1 != s.LastIndexOf("DESIGN", StringComparison.Ordinal))
                    {
                        mode |= PropertyMode.Design;
                    }
                    if (-1 != s.LastIndexOf("DEPLOY", StringComparison.Ordinal))
                    {
                        mode |= PropertyMode.Deploy;
                    }
                }
                return mode;
            }
        }

        /// <summary>
        ///get attribute - if it requires cast</summary>
        public bool Cast
        {
            get
            {
                String s = this.Reader["cast"];
                if( null == s )
                {
                    return false;
                }
                return Boolean.Parse(s);
            }
        }

        /// <summary>
        ///get attribute - if it should be hidden from the user</summary>
        public bool Hidden
        {
            get
            {
                return null != this.Reader["hidden"];
            }
        }

        /// <summary>
        ///get attribute - whre can this property be used ( request, filter, order by)</summary>
        public ObjectPropertyUsages Usage
        {
            get
            {
                String s = this.Reader["usage"];
                ObjectPropertyUsages usage = ObjectPropertyUsages.Reserved1;
                if( null != s )
                {
                    s = s.ToLowerInvariant();

                    usage = ObjectPropertyUsages.Reserved1;
                    if( -1 != s.LastIndexOf("request",StringComparison.Ordinal) )
                    {
                        usage |= ObjectPropertyUsages.Request;
                    }
                    if (-1 != s.LastIndexOf("filter", StringComparison.Ordinal))
                    {
                        usage |= ObjectPropertyUsages.Filter;
                    }
                    if (-1 != s.LastIndexOf("order", StringComparison.Ordinal))
                    {
                        usage |= ObjectPropertyUsages.OrderBy;
                    }
                    return usage;
                }

                s = this.Reader["notusage"];
                if( null != s )
                {
                    s = s.ToLowerInvariant();

                    usage = ObjectPropertyUsages.All | ObjectPropertyUsages.Reserved1;
                    if (-1 != s.LastIndexOf("request", StringComparison.Ordinal))
                    {
                        usage &= ~ObjectPropertyUsages.Request;
                    }
                    if (-1 != s.LastIndexOf("filter", StringComparison.Ordinal))
                    {
                        usage &= ~ObjectPropertyUsages.Filter;
                    }
                    if (-1 != s.LastIndexOf("order", StringComparison.Ordinal))
                    {
                        usage &= ~ObjectPropertyUsages.OrderBy;
                    }
                    return usage;
                }
                if( this.Hidden )
                {
                    return usage;
                }
                return ObjectPropertyUsages.All | ObjectPropertyUsages.Reserved1;
            }
        }

        /// <summary>
        ///get attribute - tsql representation</summary>
        public String Value
        {
            get
            {
                return GetAliasString(GetTextOfElement());
            }
        }

        /// <summary>
        ///required table</summary>
        public String Table
        {
            get { return this.Reader["table"]; }
        }

        /// <summary>
        ///get attribute</summary>
        public String Link
        {
            get { return this.Reader["link"]; }
        }

        /// <summary>
        ///get attribute - size if it is a string for example</summary>
        public String Size
        {
            get { return this.Reader["size"]; }
        }

        /// <summary>
        ///has lik to other properties</summary>
        public bool HasPropertyLink
        {
            get { return null != Table || null != Link; }
        }

        /// <summary>
        ///read a multiple link</summary>
        public XmlReadMultipleLink MultipleLink
        {
            get
            {
                if( XmlUtility.IsElement(this.Reader, "link_multiple") )
                {
                    return new XmlReadMultipleLink(this);
                }

                return null;
            }
        }
    }

    /// <summary>
    ///class to read the SETTING tag</summary>
    [ComVisible(false)]
    internal class XmlReadSettings : XmlRead
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadSettings(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - main table ( not used anymore )</summary>
        public String MainTable
        {
            get { return this.Reader["main_table"]; }
        }

        /// <summary>
        ///get attribute - filter ( not used anymore )</summary>
        public String AdditionalFilter
        {
            get { return this.Reader["aditional_filter"];   }
        }

        /// <summary>
        ///get attribute - the select is distinct</summary>
        public bool Distinct
        {
            get
            {
                String s = this.Reader["distinct"];
                if( null == s )
                {
                    return false;
                }

                return Boolean.Parse(s);
            }
        }

        /// <summary>
        ///main table or filter has been set</summary>
        public bool HasPropertyLink
        {
            get { return null != MainTable || null != AdditionalFilter; }
        }

        /// <summary>
        ///read the parent link</summary>
        public XmlReadParentLink ParentLink
        {
            get
            {
                if( !XmlUtility.SelectNextElement(this.Reader) )
                {
                    return null;
                }

                if ( IsElementWithCheckVersion("parent_link") )
                {
                    return new XmlReadParentLink(this);
                }
                return null;
            }
        }

        /// <summary>
        ///read the fail condition</summary>
        public XmlReadConditionedStatementFailCondition FailCondition
        {
            get
            {
                if( IsElementWithCheckVersion("fail_condition") )
                {
                    return new XmlReadConditionedStatementFailCondition(this);
                }

                return null;
            }
        }

        /// <summary>
        ///read the request_parent_select</summary>
        public XmlRequestParentSelect RequestParentSelect
        {
            get
            {
                if( IsElementWithCheckVersion("request_parent_select") )
                {
                    return new XmlRequestParentSelect(this);
                }
                return null;
            }
        }

        /// <summary>
        ///read the include</summary>
        public XmlReadInclude Include
        {
            get
            {
                if( IsElementWithCheckVersion("include") )
                {
                    return new XmlReadInclude(this);
                }

                return null;
            }
        }

        /// <summary>
        ///read the property link</summary>
        public XmlReadPropertyLink PropertyLink
        {
            get
            {
                if( IsElementWithCheckVersion("property_link") )
                {
                    return new XmlReadPropertyLink(this);
                }
                return null;
            }
        }

        /// <summary>
        ///read prefix</summary>
        public XmlReadConditionedStatementPrefix Prefix
        {
            get
            {
                if( IsElementWithCheckVersion("prefix") )
                {
                    return new XmlReadConditionedStatementPrefix(this);
                }

                return null;
            }
        }

        /// <summary>
        ///read postfix</summary>
        public XmlReadConditionedStatementPostfix Postfix
        {
            get
            {
                if( IsElementWithCheckVersion("postfix") )
                {
                    return new XmlReadConditionedStatementPostfix(this);
                }

                return null;
            }
        }

        /// <summary>
        ///read post_process</summary>
        public XmlReadConditionedStatementPostProcess PostProcess
        {
            get
            {
                if( IsElementWithCheckVersion("post_process") )
                {
                    return new XmlReadConditionedStatementPostProcess(this);
                }

                return null;
            }
        }

        /// <summary>
        ///read order_by_redirect</summary>
        public XmlReadOrderByRedirect OrderByRedirect
        {
            get
            {
                if( IsElementWithCheckVersion("orderby_redirect") )
                {
                    return new XmlReadOrderByRedirect(this);
                }

                return null;
            }
        }

        /// <summary>
        ///read special query</summary>
        public XmlReadSpecialQuery SpecialQuery
        {
            get
            {
                if( IsElementWithCheckVersion("special_query") )
                {
                    return new XmlReadSpecialQuery(this);
                }

                return null;
            }
        }
    }

    /// <summary>
    ///class to read a parent link</summary>
    [ComVisible(false)]
    internal class XmlReadParentLink : XmlRead
    {
        /// <summary>
        ///initalize with reader</summary>
        public XmlReadParentLink(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///read a simple parent link</summary>
        public XmlReadSimpleParentLink SimpleParentLink
        {
            get
            {
                if( !XmlUtility.SelectNextElement(this.Reader) )
                {
                    return null;
                }

                if ( XmlUtility.IsElement(this.Reader, "link") )
                {
                    return new XmlReadSimpleParentLink(this);
                }

                return null;
            }
        }

        /// <summary>
        ///read a multiple_link</summary>
        public XmlReadMultipleLink MultipleLink
        {
            get
            {
                if( XmlUtility.IsElement(this.Reader, "link_multiple") )
                {
                    return new XmlReadMultipleLink(this);
                }

                return null;
            }
        }
    }

    /// <summary>
    ///class to read a request_parent_select</summary>
    [ComVisible(false)]
    internal class XmlRequestParentSelect : XmlRead
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlRequestParentSelect(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - field</summary>
        public XmlRequestParentSelectField Field
        {
            get
            {
                if( !XmlUtility.SelectNextElement(this.Reader) )
                {
                    return null;
                }

                if ( XmlUtility.IsElement(this.Reader, "field") )
                {
                    return new XmlRequestParentSelectField(this);
                }

                return null;
            }
        }
    }

    /// <summary>
    ///class to read a simple parent linkd</summary>
    [ComVisible(false)]
    internal class XmlReadSimpleParentLink : XmlReadRepeated
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadSimpleParentLink(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get the local property</summary>
        public String Local
        {
            get { return this.Reader["local"]; }
        }

        /// <summary>
        ///get the parent property</summary>
        public String Parent
        {
            get { return this.Reader["parent"]; }
        }

        /// <summary>
        ///go to the next local-parent pair ( link tag )</summary>
        public override bool Next()
        {
            bool b = base.Next();
            if( b && XmlUtility.IsElement(this.Reader, "link") )
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///claa to read a parent select field</summary>
    [ComVisible(false)]
    internal class XmlRequestParentSelectField : XmlReadRepeated
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlRequestParentSelectField(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - name</summary>
        public String Name
        {
            get { return this.Reader["name"]; }
        }

        /// <summary>
        ///go to the next field</summary>
        public override bool Next()
        {
            bool b = base.Next();
            if( b && XmlUtility.IsElement(this.Reader, "field") )
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///class to read a multiple link</summary>
    [ComVisible(false)]
    internal class XmlReadMultipleLink : XmlRead
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadMultipleLink(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - number of links</summary>
        public String No
        {
            get { return this.Reader["no"]; }
        }

        /// <summary>
        ///get attribute - link expression</summary>
        public String Expression
        {
            get { return GetAliasString(this.Reader["expression"]); }
        }

        /// <summary>
        ///read the link_fileds</summary>
        public XmlReadLinkFields LinkFields
        {
            get
            {
                if( !XmlUtility.SelectNextElement(this.Reader) )
                {
                    return null;
                }

                if ( XmlUtility.IsElement(this.Reader, "link_field") )
                {
                    return new XmlReadLinkFields(this);
                }

                return null;
            }
        }
    }

    /// <summary>
    ///type of the field in link</summary>
    public enum LinkFieldType
    {
        /// <summary>
        ///parent property</summary>
        Parent,
        /// <summary>
        ///local property</summary>
        Local,
        /// <summary>
        ///computed property</summary>
        Computed,
        /// <summary>
        ///property is deduced from filter</summary>
        Filter }

    /// <summary>
    ///class to read link_fields</summary>
    [ComVisible(false)]
    internal class XmlReadLinkFields : XmlReadRepeated
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadLinkFields(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - link filed type</summary>
        public LinkFieldType Type
        {
            get
            {
                switch(this.Reader["type"])
                {
                    case "parent":
                        return LinkFieldType.Parent;
                    case "local":
                        return LinkFieldType.Local;
                    case "computed":
                        return LinkFieldType.Computed;
                    case "filter":
                        return LinkFieldType.Filter;
                    default:
                        return LinkFieldType.Computed;
                }
            }
        }

        /// <summary>
        ///get attribute - field name</summary>
        public String Field
        {
            get { return this.Reader["field"]; }
        }

        /// <summary>
        ///get attribute - default value if it cannot be deduced from the filter</summary>
        public String DefaultValue
        {
            get { return this.Reader["default_value"]; }
        }
    }

    /// <summary>
    ///class to read a property link</summary>
    [ComVisible(false)]
    internal class XmlReadPropertyLink : XmlReadRepeated
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadPropertyLink(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - list of triggering fields</summary>
        public StringCollection Fields
        {
            get { return GetFields(this.Reader["fields"]);  }
        }

        /// <summary>
        ///get attribute - table name</summary>
        public string Table
        {
            get { return GetAliasString(this.Reader["table"]);  }
        }

        /// <summary>
        ///get attribute - table alias in the query</summary>
        public string TableAlias
        {
            get { return GetAliasString(this.Reader["alias"]);  }
        }

        /// <summary>
        ///get attribute - join table name</summary>
        public string InnerJoin
        {
            get { return GetAliasString(this.Reader["join"]);   }
        }

        /// <summary>
        ///get attribute - expression is not for the filter by it is the actuall table name
        ///( used to create the table name with multiple link )</summary>
        public bool ExpressionIsForTableName
        {
            get { return null != this.Reader["expression_is_for_table_name"];   }
        }

        /// <summary>
        ///get attribute - read the left join table</summary>
        public string LeftJoin
        {
            get { return GetAliasString(this.Reader["left_join"]);  }
        }

        /// <summary>
        ///get attribute - read the filter</summary>
        public string Filter
        {
            get
            {
                return GetAliasString(GetTextOfElement());
            }
        }

        /// <summary>
        ///get attribute - get the next property link</summary>
        override public bool Next()
        {
            return Next("property_link");
        }

        /// <summary>
        ///get a multiple_link</summary>
        public XmlReadMultipleLink MultipleLink
        {
            get
            {
                if( XmlUtility.IsElement(this.Reader, "link_multiple") )
                {
                    return new XmlReadMultipleLink(this);
                }

                return null;
            }
        }
    }

    /// <summary>
    ///class to read a conditioned statement</summary>
    [ComVisible(false)]
    internal class XmlReadConditionedStatement : XmlReadRepeated
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadConditionedStatement(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - tsql</summary>
        public String Sql
        {
            get
            {
                return GetTextOfElement();
            }
        }

        /// <summary>
        ///get attribute - list of triggering fields</summary>
        public StringCollection Fields
        {
            get { return GetFields(this.Reader["fields"]);  }
        }

        /// <summary>
        ///read multiple_link</summary>
        public XmlReadMultipleLink MultipleLink
        {
            get
            {
                if( XmlUtility.IsElement(this.Reader, "link_multiple") )
                {
                    return new XmlReadMultipleLink(this);
                }

                return null;
            }
        }
    }

    /// <summary>
    ///class to read a prefix tag</summary>
    [ComVisible(false)]
    internal class XmlReadConditionedStatementPrefix : XmlReadConditionedStatement
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadConditionedStatementPrefix(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///go to the next prefix</summary>
        override public bool Next()
        {
            return Next("prefix");
        }
    }

    /// <summary>
    ///class to read a fail_condition</summary>
    [ComVisible(false)]
    internal class XmlReadConditionedStatementFailCondition : XmlReadConditionedStatement
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadConditionedStatementFailCondition(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///go to the next fail_condition</summary>
        override public bool Next()
        {
            return Next("fail_condition");
        }
    }

    /// <summary>
    ///class to read postfix</summary>
    [ComVisible(false)]
    internal class XmlReadConditionedStatementPostfix : XmlReadConditionedStatement
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadConditionedStatementPostfix(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get next postfix</summary>
        override public bool Next()
        {
            return Next("postfix");
        }
    }

    /// <summary>
    ///class to read a post_process</summary>
    [ComVisible(false)]
    internal class XmlReadConditionedStatementPostProcess : XmlReadRepeated
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadConditionedStatementPostProcess(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - post process clr class name</summary>
        public string ClassName
        {
            get { return this.Reader["class_name"]; }
        }

        /// <summary>
        ///get attribute - list of triggering fields</summary>
        public StringCollection Fields
        {
            get { return GetFields(this.Reader["fields"]);  }
        }

        /// <summary>
        ///get attribute - list of triggered fields</summary>
        public StringCollection TriggeredFields
        {
            get { return GetFields(this.Reader["triggered_fields"]);    }
        }

        /// <summary>
        ///get the next post process</summary>
        override public bool Next()
        {
            return Next("post_process");
        }
    }

    /// <summary>
    ///read an redirect_orderby</summary>
    [ComVisible(false)]
    internal class XmlReadOrderByRedirect : XmlReadRepeated
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadOrderByRedirect(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - field for which to redirect</summary>
        public string Field
        {
            get { return this.Reader["field"];  }
        }

        /// <summary>
        ///get attribute - list of redirect fields</summary>
        public StringCollection RedirectFields
        {
            get { return GetFields(this.Reader["redirect_fields"]); }
        }

        /// <summary>
        ///get next redirect_orderby</summary>
        override public bool Next()
        {
            return Next("redirect_orderby");
        }
    }

    /// <summary>
    ///class to read a special query</summary>
    [ComVisible(false)]
    internal class XmlReadSpecialQuery : XmlReadConditionedStatement
    {
        /// <summary>
        ///initialize with reader</summary>
        public XmlReadSpecialQuery(XmlRead xmlReader) : base(xmlReader)
        {
        }

        /// <summary>
        ///get attribute - database name</summary>
        public string Database
        {
            get { return this.Reader["database"];   }
        }


        /// <summary>
        /// Used to get attribute 'query hint'
        /// for the query being generated.
        /// For example:
        /// SELECT (...)  OPTION (FORCE ORDER)
        /// Here, 'FORCE ORDER' is the query hint text
        /// used within 'special_tag' in table.xml
        /// </summary>
        public string Hint
        {
            get { return this.Reader["hint"]; }
        }

        public override bool Next()
        {
            return Next("special_query");
        }
    }
}
