// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Runtime.InteropServices;
    using System.Xml;

    /// <summary>
    /// Collection of functions for parsing an xml using SAX
    /// </summary>
    [ComVisible(false)]
	public class XmlUtility
	{
		/// <summary>
		/// default constructor - hidden
		/// </summary>
		private XmlUtility()
		{
		}

		/// <summary>
		/// select children elements by name
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="strName"></param>
		/// <returns></returns>
		public static bool SelectChildrenByName(XmlTextReader reader, String strName)
		{
			if( !GoDownOneLevel(reader) )
            {
                return false;
            }
            //search in sibblings
            return SelectElementByName(reader, strName);
		}

		/// <summary>
		/// select children elements by attribute
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="strAttribute"></param>
		/// <param name="strValue"></param>
		/// <returns></returns>
		public static bool SelectChildrenByAttribute(XmlTextReader reader, String strAttribute, String strValue)
		{
			if( !GoDownOneLevel(reader) )
            {
                return false;
            }

            int nDepth = reader.Depth;
			do
			{
				if( reader.NodeType == XmlNodeType.Element && strValue == reader[strAttribute] )
                {
                    return true;
                }

                reader.Skip();
			}
			while( !reader.EOF && nDepth == reader.Depth );
			return false;
		}

		/// <summary>
		/// select the next sibbling of the current element
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static bool SelectNextSibling(XmlTextReader reader)
		{
			int nDepth = reader.Depth;
			for(reader.Skip(); nDepth == reader.Depth && !reader.EOF; reader.Skip())
            {
                if ( reader.NodeType == XmlNodeType.Element )
                {
                    return true;
                }
            }

            return nDepth == reader.Depth;
		}

		/// <summary>
		/// select the next element on the same level
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static bool SelectNextElementOnLevel(XmlTextReader reader)
		{
			int nDepth = reader.Depth;
			for(reader.Skip(); nDepth >= reader.Depth && !reader.EOF; reader.Skip())
			{
				if( reader.NodeType == XmlNodeType.Element && nDepth == reader.Depth)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// select the first eleemtn with the given name
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="strName"></param>
		/// <returns></returns>
		public static bool SelectElementByName(XmlTextReader reader, String strName)
		{
			int nDepth = reader.Depth;
			do
			{
				if( reader.NodeType == XmlNodeType.Element && strName == reader.Name )
                {
                    return true;
                }

                reader.Skip();
			}
			while( !reader.EOF && nDepth == reader.Depth );
			return false;
		}

		/// <summary>
		/// get the first element on the current level
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static bool GetFirstElementOnLevel(XmlTextReader reader)
		{
			int nDepth = reader.Depth;
			do
			{
				if( reader.NodeType == XmlNodeType.Element )
                {
                    return true;
                }
            }
			while( reader.Read() && nDepth == reader.Depth );
			return false;
		}

		/// <summary>
		/// read until we go down one level
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static bool GoDownOneLevel(XmlTextReader reader)
		{
			int nDepth = reader.Depth + 1;
			//go to the children level
			for(; nDepth != reader.Depth || reader.NodeType != XmlNodeType.Element;)
			{
				if( !(reader.Read()) || nDepth < reader.Depth || nDepth > reader.Depth + 1 )
                {
                    return false;
                }

                if ( reader.NodeType == XmlNodeType.Element && nDepth - 1 ==  reader.Depth )
                {
                    return false;
                }
            }
			return true;
		}

		/// <summary>
		/// read until we go up one level
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static bool GoUpOneLevel(XmlTextReader reader)
		{
			int nDepth = reader.Depth - 1;
			for(; nDepth != reader.Depth || reader.NodeType != XmlNodeType.Element;)
			{
				if( !(reader.Read()) || nDepth > reader.Depth || nDepth < reader.Depth - 1 )
                {
                    return false;
                }
            }
			return true;			
		}

		/// <summary>
		/// select the next element
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static bool SelectNextElement(XmlTextReader reader)
		{
			for(;reader.Read();)
            {
                if ( reader.NodeType == XmlNodeType.Element )
                {
                    return true;
                }
            }

            return false;
		}

		/// <summary>
		/// select the next element by name
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="strName"></param>
		/// <returns></returns>
		public static bool SelectNextElement(XmlTextReader reader, String strName)
		{
			return SelectNextElement(reader) && IsElement(reader, strName);
		}

		/// <summary>
		/// true if the current entity is an element with the given name
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="strName"></param>
		/// <returns></returns>
		public static bool IsElement(XmlTextReader reader, String strName)
		{
			return !reader.EOF && reader.NodeType == XmlNodeType.Element && reader.Name == strName;
		}
	}
}
