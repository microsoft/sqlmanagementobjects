// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

    ///	<summary>
    ///	SqlObject adds to SqlObjectBase logic to initialize from an xml file </summary>
    [ComVisible(false)]
    internal class SqlObject : SqlObjectBase, ISupportInitDatabaseEngineData
    {
        ///	<summary>
        ///	initialize the connection info and xpath information </summary>
        public override void Initialize(Object ci, XPathExpressionBlock block)
        {
            base.Initialize(ci, block);
        }

        ///	<summary>
        ///	load the specified file for the specified version </summary>
        public void LoadInitData(String file, ServerVersion ver,DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            LoadInitDataFromAssembly(this.ResourceAssembly, file, ver,databaseEngineType, databaseEngineEdition);
        }

        /// <summary>
        /// Creates a SqlObject based on config from the given stream. Note that "include" tags will only work for resources defined in the current resource assembly.
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="ver"></param>
        /// <param name="databaseEngineType"></param>
        /// <param name="databaseEngineEdition"></param>
        public void LoadInitData(Stream xml, ServerVersion ver, DatabaseEngineType databaseEngineType,
            DatabaseEngineEdition databaseEngineEdition)
        {
            LoadInitDataFromAssemblyInternal(this.ResourceAssembly, null, ver: ver, alias: null, requestedFields: null,
                store: true, roAfterCreation: null, databaseEngineType: databaseEngineType,
                databaseEngineEdition: databaseEngineEdition, configXml: xml);
        }
        ///	<summary>
        ///	load the specified file for the specified version from the specified assembly </summary>
        public void LoadInitDataFromAssembly(Assembly assemblyObject, String file, ServerVersion ver,DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            LoadInitDataFromAssemblyInternal(assemblyObject, file, ver, alias: null, requestedFields: null, store: true,
                roAfterCreation: null, databaseEngineType: databaseEngineType,
                databaseEngineEdition: databaseEngineEdition);
        }

        void LoadInitDataFromAssemblyInternal(Assembly assemblyObject, String file, ServerVersion ver, String alias, StringCollection requestedFields, bool store, StringCollection roAfterCreation, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition, Stream configXml = null)
        {            
            XmlReadDoc xrd = new XmlReadDoc(ver, alias, databaseEngineType, databaseEngineEdition);
            if (configXml == null)
            {
#if DEBUGTRACE
                Enumerator.TraceInfo("Load xml:\n{0} from assembly {1}\n", file, assemblyObject.FullName);
#endif
                xrd.LoadFile(assemblyObject, file);
            }
            else
            {
                xrd.LoadXml(configXml);
            }
            if (store)
            {
                LoadAndStore(xrd, assemblyObject, requestedFields, roAfterCreation);
            }
            else
            {
                Load(xrd, assemblyObject, requestedFields, roAfterCreation);
            }
            xrd.Close();
        }

        ///	<summary>
        ///	load from the specfied xml doc in the assemblyObject assembly the fields
        ///	specified in requestedFields; also after loading initialize static links between 
        ///	the properties. </summary>
        internal protected virtual void LoadAndStore(XmlReadDoc xrd, Assembly assemblyObject, StringCollection requestedFields, StringCollection roAfterCreation)
        {
            Load(xrd, assemblyObject, requestedFields, roAfterCreation);
            StoreInitialState();
        }

        ///	<summary>
        ///	load from the specfied xml doc in the assemblyObject assembly the fields
        ///	specified in requestedFields</summary>
        internal virtual void Load(XmlReadDoc xrd, Assembly assembly, StringCollection requestedFields, StringCollection roAfterCreation)
        {
            XmlReadSettings xrs = xrd.Settings;
            XmlReadInclude xri;
            Hashtable computedProperties = new Hashtable();
            if (null != xrs)
            {
                this.Distinct = xrs.Distinct;

                SqlPropertyLink.Add(this.PropertyLinkList, xrs);

                XmlReadParentLink xrparl = xrs.ParentLink;
                if (null != xrparl)
                {
                    this.ParentLink = new ParentLink(xrparl);
                    xrparl.Close();
                }

                XmlReadConditionedStatementFailCondition xrcsfc = (XmlReadConditionedStatementFailCondition)xrs.FailCondition;
                if (null != xrcsfc)
                {
                    SqlConditionedStatementFailCondition.AddAll(this.ConditionedSqlList, xrcsfc);
                    xrcsfc.Close();
                }

                XmlRequestParentSelect xrps = xrs.RequestParentSelect;
                if (null != xrps)
                {
                    this.RequestParentSelect = new RequestParentSelect(xrps);
                    xrps.Close();
                }

                xri = xrs.Include;
                if (null != xri)
                {
                    IncludeFile(xri, assembly, requestedFields, roAfterCreation);
                }

                XmlReadPropertyLink xrpl = xrs.PropertyLink;
                if (null != xrpl)
                {
                    SqlPropertyLink.AddAll(this.PropertyLinkList, xrpl);
                    xrpl.Close();
                }
                XmlReadConditionedStatementPrefix xrcsp = (XmlReadConditionedStatementPrefix)xrs.Prefix;
                if (null != xrcsp)
                {
                    SqlConditionedStatementPrefix.AddAll(this.ConditionedSqlList, xrcsp);
                    xrcsp.Close();
                }
                XmlReadConditionedStatementPostfix xrcspost = (XmlReadConditionedStatementPostfix)xrs.Postfix;
                if (null != xrcspost)
                {
                    SqlConditionedStatementPostfix.AddAll(this.ConditionedSqlList, xrcspost);
                    xrcspost.Close();
                }
                XmlReadConditionedStatementPostProcess xrpp = (XmlReadConditionedStatementPostProcess)xrs.PostProcess;
                if (null != xrpp)
                {
                    SqlPostProcess.AddAll(this.PostProcessList, xrpp, this.ResourceAssembly);
                    xrpp.Close();

                    // the fields that require post processing are computed
                    // on the client, store this list so later we can set the 
                    // usage flag correctly
                    foreach (ConditionedSql cs in this.PostProcessList)
                    {
                        foreach (string postProcessedProp in cs.Fields)
                        {
                            computedProperties.Add(postProcessedProp, postProcessedProp);
                        }
                    }
                }
                XmlReadOrderByRedirect xrobr = xrs.OrderByRedirect;
                if (null != xrobr)
                {
                    do
                    {
                        this.OrderByRedirect[xrobr.Field] = xrobr.RedirectFields;
                    }
                    while (xrobr.Next());

                    xrobr.Close();
                }
                XmlReadSpecialQuery xrsq = xrs.SpecialQuery;
                if (null != xrsq)
                {
                    this.AddSpecialQuery(xrsq.Database, xrsq.Query);
                    this.AddQueryHint(xrsq.Hint);
                    xrsq.Close();
                }
            }

            XmlReadProperties xrp = xrd.Properties;
            XmlReadProperty xrpy = xrp.Property;
            do
            {
                xrpy = xrp.Property;
                xri = xrp.Include;
                if (null != xrpy)
                {
                    if (null == requestedFields || requestedFields.Contains(xrpy.Name) || xrpy.Hidden)
                    {
                        SqlPropertyLink.Add(this.PropertyLinkList, xrpy);

                        SqlObjectProperty sop = new SqlObjectProperty(xrpy);
                        if (null != roAfterCreation && roAfterCreation.Contains(sop.Name))
                        {
                            sop.ReadOnlyAfterCreation = true;
                        }

                        // if this property is computed we clear the Filter 
                        // and OrderBy usage flags because enumerator does  not
                        // do client-side expression evaluation and record filtering
                        if (computedProperties.ContainsKey(sop.Name))
                        {
                            sop.Usage &= ~ObjectPropertyUsages.Filter;
                            sop.Usage &= ~ObjectPropertyUsages.OrderBy;
                        }
                        AddProperty(sop);
                        xrpy.Close();
                    }
                    else
                    {
                        xrpy.Skip();
                    }

                }
                else if (null != xri)
                {
                    IncludeFile(xri, assembly, requestedFields, roAfterCreation);
                }
            }
            while (xrpy != null || xri != null);
        }

        void IncludeFile(XmlReadInclude xri, Assembly assembly, StringCollection requestedFields, StringCollection roAfterCreation)
        {
            StringCollection c = xri.RequestedFields;
            if (null != requestedFields)
            {
                foreach (String s in requestedFields)
                {
                    c.Add(s);
                }
            }
            if (0 == c.Count)
            {
                c = null;
            }

            StringCollection ro = xri.ROAfterCreation;
            if (null != roAfterCreation)
            {
                foreach (String s in roAfterCreation)
                {
                    ro.Add(s);
                }
            }
            if (0 == ro.Count)
            {
                ro = null;
            }

            LoadInitDataFromAssemblyInternal(assembly, xri.File, xri.Version, xri.TableAlias, c, false, ro,xri.DatabaseEngineType, xri.DatabaseEngineEdition);
            xri.Close();
        }
    }
}
