// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Runtime.InteropServices;

    ///	<summary>
    ///encapsulates the link between this level and the parent level</summary>
    [ComVisible(false)]
	internal class ParentLink
	{
		LinkMultiple m_LinkMultiple;

		///	<summary>
		///intialize with xml reader</summary>
		public ParentLink(XmlReadParentLink xrpl)
		{
			m_LinkMultiple = new LinkMultiple();
			XmlReadSimpleParentLink xrspl = xrpl.SimpleParentLink;
			if( null != xrspl )
			{
				Init(xrspl);
				return;
			}
			XmlReadMultipleLink xrmpl = xrpl.MultipleLink;
			if( null != xrmpl )
            {
                m_LinkMultiple.Init(xrmpl);
            }
        }

		///	<summary>
		///read a simple parent link</summary>
		internal void Init(XmlReadSimpleParentLink xrspl)
		{
			m_LinkMultiple.SetLinkFields(new ArrayList());
			String expr = string.Empty;

			int i = 0;
			do
			{
				LinkField lf = new LinkField();
				lf.Type = LinkFieldType.Local;
				lf.Field = xrspl.Local;
				m_LinkMultiple.LinkFields.Add(lf);
				lf = new LinkField();
				lf.Type = LinkFieldType.Parent;
				lf.Field = xrspl.Parent;
				m_LinkMultiple.LinkFields.Add(lf);
				if( i > 0 )
                {
                    expr += " AND ";
                }

                expr += String.Format(CultureInfo.InvariantCulture, "{{{0}}}={{{1}}}", i++, i++);
			}
			while( xrspl.Next() );
			m_LinkMultiple.SetSqlExpression(expr);
			m_LinkMultiple.No = i.ToString(CultureInfo.InvariantCulture);
		}

		///	<summary>
		///return the list of link fields</summary>
		public ArrayList LinkFields
		{
			get 
			{ 
				return m_LinkMultiple.LinkFields; 
			}
		}

		///	<summary>
		///return the link multiple</summary>
		public LinkMultiple LinkMultiple
		{
			get
			{
				return m_LinkMultiple;
			}
		}
	}
}
