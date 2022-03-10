// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// encapsulates a dependency request
    /// </summary>
    [ComVisible(false)]
	public class DependencyRequest
	{
		Urn[] m_listUrn;
		bool m_bParentDeps;

		/// <summary>
		/// default constructor
		/// </summary>
		public DependencyRequest()
		{
			m_bParentDeps = true;
		}

		/// <summary>
		/// list of XPATHs which gives the list of object for 
		/// which we need to discover dependencies
		/// </summary>
		/// <value></value>
		public Urn[] Urns
		{
			get { return m_listUrn; }
			set { m_listUrn = value; }
		}

		/// <summary>
		/// true if we need to discover parent dependencies as opposed to children dependencies
		/// </summary>
		/// <value></value>
		public bool ParentDependencies
		{
			get { return m_bParentDeps; }
			set { m_bParentDeps = value; }
		}
	}
}
			
