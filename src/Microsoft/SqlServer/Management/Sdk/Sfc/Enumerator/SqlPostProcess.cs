// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Runtime.InteropServices;

    ///	<summary>
    ///represents the need to execute post process for a particular field</summary>
    [ComVisible(false)]
	internal class SqlPostProcess : ConditionedSql
	{
		String m_className;
		Assembly m_assembly;
		StringCollection m_triggeredFields;
		SortedList m_HitFields;

		///	<summary>
		///initialize with xml reader, and asembly in which the class that knows to do the post process resides</summary>
		public SqlPostProcess(XmlReadConditionedStatementPostProcess xrpp, Assembly asembly)
		{
			SetFields(xrpp.Fields);

			m_triggeredFields = xrpp.TriggeredFields;

			if( null == xrpp.ClassName )
			{
				throw new InternalEnumeratorException(SfcStrings.NoClassNamePostProcess);
			}
			m_className = xrpp.ClassName;
			m_assembly = asembly;
		}

#if false // CC_REMOVE_POSTPROCESS
		///	<summary>
		///create a instance of a class that knows how to do the post process</summary>
		PostProcess GetPostProcessInstance()
		{
			PostProcess instPostProcess = Util.CreateObjectInstance(m_assembly, m_className) as PostProcess;
			if( null == instPostProcess )
			{
				throw new InternalEnumeratorException(SfcStrings.NotDerivedFrom(m_className, "PostProcess"));
			}
			return instPostProcess;
		}
#endif
		///	<summary>
		///list of triggered fields for this post process
		/// ( fields that are needed to compute the value for the field requested by the user )</summary>
		public StringCollection TriggeredFields
		{
			get
			{
				return m_triggeredFields;
			}
		}

		///	<summary>
		///add hit </summary>
		public override void AddHit(string field, SqlObjectBase obj, StatementBuilder sb)
		{
			if( null == m_HitFields )
			{
                m_HitFields = new SortedList(System.StringComparer.Ordinal);
			}
			m_HitFields[field] = null;
		}

		///	<summary>
		///always true</summary>
		protected override bool AcceptsMultipleHits
		{
			get { return true; }
		}

		///	<summary>
		///register and prepare the post process for run</summary>
		internal void Register(SqlObjectBase obj)
		{
			if( null != m_HitFields )
			{
#if false // CC_REMOVE_POSTPROCESS
				PostProcess instPostProcess = GetPostProcessInstance();
				instPostProcess.ObjectName = obj.ObjectName;
				instPostProcess.ConnectionInfo = obj.ConnectionInfo;
				instPostProcess.Request = obj.Request;

				instPostProcess.InitNameBasedLookup(obj, this.TriggeredFields);
				instPostProcess.HitFields = m_HitFields;

				foreach(string f in m_HitFields.Keys)
				{
					obj.StatementBuilder.AddPostProcess(obj.GetAliasPropertyName(f), instPostProcess);
				}
				m_HitFields = null;
#endif
			}
		}

		///	<summary>
		///read alll post_process tags from the configuration xml</summary>
		public static void AddAll(ConditionedSqlList list, XmlReadConditionedStatementPostProcess xrcs, Assembly asembly)
		{
			if( null != xrcs )
			{
				do
				{
					list.Add(new SqlPostProcess(xrcs, asembly));
				}
				while( xrcs.Next() );
			}
		}
	}
}
