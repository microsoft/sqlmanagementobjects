// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml.Serialization;

    ///<summary>
    /// Interface that provides access to XPathExpression and String value of Urn.
    /// Implemented by KeyChain and UrnImpl
    ///</summary>
    interface IUrn
    {
        ///<summary>
        /// Get XPathExpression for this object
        ///</summary>
        XPathExpression XPathExpression
        {
            get;
        }

        ///<summary>
        /// Get textual value for this object
        ///</summary>
        String Value
        {
            get;
            set;
        }

        ///<summary>
        /// Get domain instance name
        ///</summary>
        String DomainInstanceName
        {
            get;
        }
    }

    ///<summary>
    /// Expresion used to identify one or more objects
    ///</summary>
    [Serializable]
    public class Urn
    {
        IUrn impl;
        int hashCode;

        /// <summary>
        /// default constructor
        /// </summary>
        public Urn()
        {
            impl = new UrnImpl();
        }
        
        /// <summary>
        /// initialize with string value
        /// </summary>
        /// <param name="value"></param>
        public Urn(String value)
        {
            impl = new UrnImpl(value);
        }

        /// <summary>
        /// initialize with string value
        /// </summary>
        internal Urn(IUrn keychain)
        {
            impl = keychain;
        }

        /// <summary>
        /// syntactical tree representation
        /// </summary>
        /// <value></value>
        public XPathExpression XPathExpression
        {
            get
            {
                return impl.XPathExpression;
            }
        }
        ///<summary>
        /// the urn expresion as string
        ///</summary>
        [XmlAttribute]
        public String Value
        {
            get
            {
                return impl.Value;
            }
            set
            {
                impl.Value = value;
                this.hashCode = 0;
            }
        }

        ///<summary>
        /// Get domain instance name
        ///</summary>
        public String DomainInstanceName
        {
            get
            {
                return impl.DomainInstanceName;
            }
        }

        // This is only needed today for Registered Servers
        // Once registered servers are converted to Urn, this operator will go away
        internal IUrn GetIUrn()
        {
            return impl;
        }

        /// <summary>
        /// Urn equality operator
        /// </summary>
        /// <param name="u1"></param>
        /// <param name="u2"></param>
        /// <returns></returns>
        public static bool operator ==(Urn u1, Urn u2) 
        {
            return Compare(u1, u2, null, null);
        }
        
        /// <summary>
        /// Urn unequal operator
        /// </summary>
        /// <param name="urn1"></param>
        /// <param name="urn2"></param>
        /// <returns></returns>
        public static bool operator !=(Urn urn1, Urn urn2) 
        {
            return !Compare(urn1, urn2, null, null);
        }

        /// <summary>
        /// Equals() and GetHashCode() are overriden by all instance classes 
        /// because we need them to implement equality operators (== and !=)
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            if( null == o )
            {
                return false;
            }
            return Compare(this, (Urn)o, null, null);
        }

        ///<summary>
        /// the type of the object identified by the urn
        ///</summary>
        public String Type
        {
            get
            {
                if( this.XPathExpression.Length > 0 )
                {
                    return this.XPathExpression[this.XPathExpression.Length - 1].Name;
                }

                return string.Empty;
            }
        }

        ///<summary>
        /// the Urn without the last level
        ///</summary>
        public Urn Parent
        {
            get
            {
                bool bInString = false;
                int npos = -1;
                for(int i = Value.Length -1; i >= 0; i--)
                {
                    if( '/' == Value[i] && !bInString )
                    {
                        npos = i;
                        break;
                    }
                    if( '\'' == Value[i] )
                    {
                        if( i > 0 && '\'' == Value[i - 1] )
                        {
                            i--;
                        }
                        else
                        {
                            bInString = !bInString;
                        }
                    }

                }
                if( -1 == npos )
                {
                    return null;
                }

                return Value.Substring(0, npos);
            }
        }

        ///<summary>
        /// cast to String
        ///</summary>
        public static implicit operator String(Urn urn) 
        {
            if( null == urn )
            {
                return null;
            }

            return urn.Value;
        }
        ///<summary>
        /// cast from String
        ///</summary>
        public static implicit operator Urn(String str)
        {
            return new Urn(str);
        }
        ///<summary>
        /// cast to String
        ///</summary>
        public override String ToString()
        {
            return this.Value;
        }

        /// <summary>
        /// Returns hash code
        /// </summary>
        public override int GetHashCode()
        {
            //Since empty urn can't be compiled, this could happen when we're trying to create a new item which doesn't have an urn yet.
            if (string.IsNullOrEmpty(this.Value))
            {
                if (this.Value == string.Empty)
                {
                    return string.Empty.GetHashCode();
                }
                else
                {
                    return 0;
                }
            }
            if (this.hashCode == 0)
            {
                this.hashCode = this.XPathExpression.GetHashCode();
            }

            return this.hashCode;
        }

        ///<summary>
        /// true if the xpath points to only one object
        ///</summary>
        public bool Fixed(Object ci)
        {
            Enumerator en = new Enumerator();
            XPathExpression x = this.XPathExpression;
            
            for(String strurn = this.Value; strurn != null; strurn = ((Urn)strurn).Parent)
            {
                RequestObjectInfo roi = new RequestObjectInfo(strurn, RequestObjectInfo.Flags.UrnProperties);
                ObjectInfo oi = en.Process(ci, roi);

                if( null == oi.UrnProperties || oi.UrnProperties.Length < 1 )
                {
                    return false;
                }

                foreach (ObjectProperty op in oi.UrnProperties)
                {
                    if ( null == roi.Urn.GetAttribute(op.Name) )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// get the property value from filter from the specified level
        /// </summary>
        /// <param name="attributeName">property name</param>
        /// <param name="type">level name</param>
        /// <returns>attribute name, null if no attribute with that name</returns>
        public string GetAttribute(String attributeName, String type)
        {
            XPathExpression xpath = this.XPathExpression;
            return this.XPathExpression.GetAttribute(attributeName, type);
        }

        /// <summary>
        /// get the property value from filter from the last level
        /// </summary>
        /// <param name="attributeName">property name</param>
        /// <returns>attribute name, null if no attribute with that name</returns>
        public string GetAttribute(String attributeName)
        {
            return GetAttribute(attributeName, this.Type);
        }

        /// <summary>
        /// get the @Name attribute from the filter of the specified level
        /// </summary>
        /// <param name="type">level name</param>
        /// <returns></returns>
        public String GetNameForType(String type)
        {
            return GetAttribute("Name", type);
        }

        /// <summary>
        /// escape a string to make it suitable for use inside the XPATH expression
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [ComVisible(false)]
        public static String EscapeString(String value)
        {
            StringBuilder sb = new StringBuilder();
            foreach(char c in value)
            {
                sb.Append(c);
                if( '\'' == c )
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// remove the escaping previously added to a string for insertion in the XPATH
        /// </summary>
        /// <param name="escapedValue"></param>
        /// <returns></returns>
        [ComVisible(false)]
        public static String UnEscapeString(String escapedValue)
        {
            StringBuilder sb = new StringBuilder();
            bool delete = false;
            foreach(char c in escapedValue)
            {
                if ( '\'' == c )
                {
                    //  Check for adjacent (second) quote, skip if so
                    if ( delete )
                    {
                        delete = false;
                        continue;
                    }

                    // A leading (first) quote found, include it then setup to skip adjacent (second) one if found
                    delete = true;
                }
                else
                {
                    // Anything but a quote found
                    delete = false;
                }
                sb.Append(c);
            }

            return sb.ToString();
        }

        // Urn.Compare is called a lot
        // These default values are used in Urn.Compare when corresponding arguments are not provided
        private static readonly CultureInfo DefaultComparisonCulture = new CultureInfo("");
        private static readonly CompareOptions[] DefaultComparisonOptions = new CompareOptions[] { CompareOptions.None };

        /// <summary>
        /// comparares to Urns
        /// </summary>
        /// <param name="u1"></param>
        /// <param name="u2"></param>
        /// <param name="compInfoList"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public static bool Compare(Urn u1, Urn u2, CompareOptions[] compInfoList, CultureInfo cultureInfo)
        {
            Object o1 = (Object)u1;
            Object o2 = (Object)u2;
            if( null == o1 && null == o2 )
            {
                return true;
            }

            if ( null == o1 || null == o2 )
            {
                return false;
            }

            if ( null == cultureInfo )
            {
                cultureInfo = DefaultComparisonCulture;
            }

            if( null == compInfoList )
            {
                compInfoList = DefaultComparisonOptions;
            }

            if (compInfoList == DefaultComparisonOptions)
            {
                // In the case when no special comparison options are used
                // we may rely on hash code and assume that when hash codes are different
                // the two Urns are different as well. Hash codes are cached so this should
                // speed up comparison significantly for cases like dependency ordering
                if (u1.GetHashCode() != u2.GetHashCode())
                {
                    return false;
                }
            }

            return XPathExpression.Compare(u1.XPathExpression, u2.XPathExpression, compInfoList, cultureInfo);
        }

        /// <summary>
        /// Verifies passed string is a valid Urn
        /// </summary>
        /// <returns></returns>
        public bool IsValidUrn ()
        {
            try
            {
                XPathExpression xpe = this.XPathExpression;
                return true;
            }
            catch (XPathException)
            {
                return false;
            }
            catch (InvalidQueryExpressionEnumeratorException)
            {
                return false;
            }
        }

        /// <summary>
        /// Verifies passed string is a valid UrnSkeleton
        /// </summary>
        /// <returns></returns>
        public bool IsValidUrnSkeleton ()
        {
            try
            {
                XPathExpression xpe = this.XPathExpression;
                for (int i =0; i<xpe.Length; i++)
                {
                    XPathExpressionBlock xpb = xpe[i];
                
                    if (null != xpb.Filter)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (XPathException)
            {
                return false;
            }
            catch (InvalidQueryExpressionEnumeratorException)
            {
                return false;
            }
        }
    }

    class UrnImpl : IUrn
    {
        String          m_urn;
        XPathExpression m_xpath;

        /// <summary>
        /// default constructor
        /// </summary>
        public UrnImpl()
        {
            m_xpath = null;
            m_urn = string.Empty;
        }

        /// <summary>
        /// initialize with string value
        /// </summary>
        /// <param name="value"></param>
        public UrnImpl(String value)
        {
            m_xpath = null;
            m_urn = value;
        }

        /// <summary>
        /// syntactical tree representation
        /// </summary>
        /// <value></value>
        public XPathExpression XPathExpression
        {
            get
            {
                if( null == m_xpath )
                {
                    m_xpath = new XPathExpression();
                    m_xpath.Compile(Value);
                }
                return m_xpath;
            }
        }
        ///<summary>
        /// the urn expresion as string
        ///</summary>
        [XmlAttribute]
        public String Value
        {
            get
            {
                return m_urn;
            }
            set
            {
                m_xpath = null;
                m_urn = value;
            }
        }

        public String DomainInstanceName
        {
            get
            {
                // Cannot be reached currently, so don't need to implement it yet
                throw new NotImplementedException();
            }
        }

    }
}
