// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System.Collections.Specialized;
    using System.Runtime.InteropServices;

    /// <summary>
    ///encapsulates a request parent select
    ///basically the select that would give the results for the parent level ( without prefix/postfix)</summary>
    [ComVisible(false)]
	public class RequestParentSelect
	{
		StringCollection m_Fields;

		/// <summary>
		///initalize with xml reader</summary>
		public RequestParentSelect(XmlRequestParentSelect xrrps)
		{
			m_Fields = new StringCollection();
			XmlRequestParentSelectField field = xrrps.Field;
			do
			{
				m_Fields.Add(field.Name);
			}
			while( field.Next() );
		}

		/// <summary>
		///the fields that must be selected</summary>
		public StringCollection Fields
		{
			get 
			{ 
				return m_Fields; 
			}
		}
	}
}
