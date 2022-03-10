// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Microsoft.SqlServer.Management.Common;

    ///	<summary>
    ///	SqlObject adds to SqlObjectBase logic to initialize from an xml file </summary>
    [ComVisible(false)]
	abstract public class SqlObject : SqlObjectBase, ISupportInitDatabaseEngineData
	{
		///	<summary>
		///	initialize the connection info and xpath information </summary>
		public override void Initialize(Object ci, XPathExpressionBlock block)
		{
			base.Initialize(ci, block);
		}

		///	<summary>
		///	load the specified file for the specified version </summary>
		public void LoadInitData(String file, ServerVersion ver, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
		{
			LoadInitDataFromAssembly(this.ResourceAssembly, file, ver, databaseEngineType, databaseEngineEdition);
		}

		///	<summary>
		///	load the specified file for the specified version from the specified assembly </summary>
		public void LoadInitDataFromAssembly(Assembly assemblyObject, String file, ServerVersion ver, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
		{
			LoadInitDataFromAssemblyInternal(assemblyObject, file, ver, null, null, true, databaseEngineType, databaseEngineEdition);
		}

		private void LoadInitDataFromAssemblyInternal(Assembly assemblyObject, String file, ServerVersion ver, String alias, StringCollection requestedFields, bool store, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
		{
#if DEBUGTRACE
			Enumerator.TraceInfo("Load xml:\n{0} from assembly {1}\n", file, assemblyObject.FullName);
#endif
			XmlReadDoc xrd = new XmlReadDoc(ver, alias, databaseEngineType, databaseEngineEdition);
			xrd.LoadFile(assemblyObject, file);
			if( store )
			{
				LoadAndStore(xrd, assemblyObject, requestedFields);
			}
			else
			{
				Load(xrd, assemblyObject, requestedFields);
			}
			xrd.Close();
		}

		///	<summary>
		///	load from the specfied xml doc in the assemblyObject assembly the fields
		///	specified in requestedFields; also after loading initialize static links between 
		///	the properties. </summary>
		internal protected virtual void LoadAndStore(XmlReadDoc xrd, Assembly assemblyObject, StringCollection requestedFields)
		{
			Load(xrd, assemblyObject, requestedFields);
			StoreInitialState();
		}

		///	<summary>
		///	load from the specfied xml doc in the assemblyObject assembly the fields
		///	specified in requestedFields</summary>
		internal virtual void Load(XmlReadDoc xrd, Assembly assembly, StringCollection requestedFields)
		{
			XmlReadSettings xrs = xrd.Settings;
			XmlReadInclude xri;
			if( null != xrs )
			{
				this.Distinct = xrs.Distinct;

				SqlPropertyLink.Add(this.PropertyLinkList, xrs);

				XmlReadParentLink xrparl = xrs.ParentLink;
				if( null != xrparl )
				{
					this.ParentLink = new ParentLink(xrparl);
					xrparl.Close();
				}

				XmlReadConditionedStatementFailCondition xrcsfc = (XmlReadConditionedStatementFailCondition)xrs.FailCondition;
				if( null != xrcsfc )
				{
					SqlConditionedStatementFailCondition.AddAll(this.ConditionedSqlList, xrcsfc);
					xrcsfc.Close();
				}
				
				XmlRequestParentSelect xrps = xrs.RequestParentSelect;
				if( null != xrps )
				{
					this.RequestParentSelect = new RequestParentSelect(xrps);
					xrps.Close();
				}

				xri = xrs.Include;
				if( null != xri )
				{
					IncludeFile(xri, assembly, requestedFields);
				}

				XmlReadPropertyLink xrpl = xrs.PropertyLink;
				if( null != xrpl )
				{
					SqlPropertyLink.AddAll(this.PropertyLinkList, xrpl);
					xrpl.Close();
				}
				XmlReadConditionedStatementPrefix xrcsp = (XmlReadConditionedStatementPrefix)xrs.Prefix;
				if( null != xrcsp )
				{
					SqlConditionedStatementPrefix.AddAll(this.ConditionedSqlList, xrcsp);
					xrcsp.Close();
				}
				XmlReadConditionedStatementPostfix xrcspost = (XmlReadConditionedStatementPostfix)xrs.Postfix;
				if( null != xrcspost )
				{
					SqlConditionedStatementPostfix.AddAll(this.ConditionedSqlList, xrcspost);
					xrcspost.Close();
				}
				XmlReadConditionedStatementPostProcess xrpp = (XmlReadConditionedStatementPostProcess)xrs.PostProcess;
				if( null != xrpp )
				{
					SqlPostProcess.AddAll(this.PostProcessList, xrpp, this.ResourceAssembly);
					xrpp.Close();
				}
				XmlReadOrderByRedirect xrobr = xrs.OrderByRedirect;
				if( null != xrobr )
				{
					do
					{
						this.OrderByRedirect[xrobr.Field] = xrobr.RedirectFields;
					}
					while( xrobr.Next() );

					xrobr.Close();
				}
				XmlReadSpecialQuery xrsq = xrs.SpecialQuery;
				if( null != xrsq )
				{
					this.AddSpecialQuery(xrsq.Database, xrsq.Query);
					xrsq.Close();
				}
			}

			XmlReadProperties xrp = xrd.Properties;
			XmlReadProperty xrpy = xrp.Property;
			do
			{
				xrpy = xrp.Property;
				xri = xrp.Include;
				if( null != xrpy )
				{
					if( null == requestedFields || requestedFields.Contains(xrpy.Name) || xrpy.Hidden )
					{
						SqlPropertyLink.Add(this.PropertyLinkList, xrpy);
						SqlObjectProperty sop = new SqlObjectProperty(xrpy);
						AddProperty(sop);
						xrpy.Close();
					}
					else
					{
						xrpy.Skip();
					}
					
				}
				else if( null != xri )
				{
					IncludeFile(xri, assembly, requestedFields);
				}
			}
			while( xrpy != null || xri != null );
		}

		private void IncludeFile(XmlReadInclude xri, Assembly assembly, StringCollection requestedFields)
		{
			StringCollection c = xri.RequestedFields;
			if( null != requestedFields )
			{
				foreach(String s in requestedFields)
				{
					c.Add(s);
				}
			}
			if( 0 == c.Count )
			{
				c = null;
			}

			LoadInitDataFromAssemblyInternal(assembly, xri.File, xri.Version, xri.TableAlias, c, false, xri.DatabaseEngineType, xri.DatabaseEngineEdition);
			xri.Close();
		}

		///	<summary>
		///	abstract function that returns the assembly in which this object has the configuration file</summary>
		///	this function is abstract because only domain enumerator can implement it correctly
		public abstract Assembly ResourceAssembly
		{
			get;
		}

	}
}
