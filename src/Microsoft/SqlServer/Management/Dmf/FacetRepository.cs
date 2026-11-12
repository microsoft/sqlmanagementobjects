// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo;

//using System.ComponentModel;

namespace Microsoft.SqlServer.Management.Facets
{
    internal enum FacetType
    {
        Logical = 0,
        Physical,
        Adapted
    }

    /// <summary>
    /// Internal structure to represent Facet attributes in a single place
    /// </summary>
    internal sealed class FacetAttributes
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "FacetAttributes");
        AutomatedPolicyEvaluationMode evaluationMode;
        internal AutomatedPolicyEvaluationMode EvaluationMode
        {
            get { return evaluationMode; }
            set
            {
                traceContext.TraceVerbose("Setting EvaluationMode to: {0}", value);
                evaluationMode = value;
            }
        }

        FacetType facetType;
        internal FacetType FacetType
        {
            get { return facetType; }
            set
            {
                traceContext.TraceVerbose("Setting FacetType to: {0}", value);
                facetType = value;
            }
        }

        bool isReadOnly;
        internal bool IsReadOnly
        {
            get { return isReadOnly; }
            set
            {
                traceContext.TraceVerbose("Setting IsReadOnly to: {0}", value);
                isReadOnly = value;
            }
        }

        Type rootType;
        internal Type RootType
        {
            get { return rootType; }
            set
            {
                traceContext.TraceVerbose("Setting RootType to: {0}", value);
                rootType = value;
            }
        }


        internal FacetAttributes()
        {
            this.evaluationMode = AutomatedPolicyEvaluationMode.None;
            this.facetType = FacetType.Logical;
            this.isReadOnly = false;
            this.rootType = null;
        }
    }

    /// <summary>
    /// Facet evaluation context - facet interface + implementing object
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FacetEvaluationContext
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "FacetEvaluationContext");
        Type iface;
        object target;
        FacetType facetType;
        object physicalTarget;

        /// <summary>
        /// 
        /// </summary>
        public Type Interface { get { return iface; } }

        /// <summary>
        /// 
        /// </summary>
        public object Target { get { return target; } }

        /// <summary>
        /// 
        /// </summary>
        internal FacetType FacetType { get { return facetType; } }

        /// <summary>
        /// 
        /// </summary>
        internal object PhysicalTarget { get { return physicalTarget; } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iface"></param>
        /// <param name="target"></param>
        /// <param name="facetType"></param>
        internal FacetEvaluationContext(Type iface, object target, FacetType facetType)
        {
            traceContext.TraceMethodEnter("FacetEvaluationContext");
            // Tracing Input Parameters
            traceContext.TraceParameters(iface, target, facetType);
            this.iface = iface;
            this.target = target;
            this.facetType = facetType;
            this.physicalTarget = target;  //Default the physicalTarget to the target if one isn't specified
            traceContext.TraceMethodExit("FacetEvaluationContext");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iface"></param>
        /// <param name="target"></param>
        /// <param name="facetType"></param>
        /// <param name="physicalTarget"></param>
        internal FacetEvaluationContext(Type iface, object target, FacetType facetType, object physicalTarget)
        {
            traceContext.TraceMethodEnter("FacetEvaluationContext");
            // Tracing Input Parameters
            traceContext.TraceParameters(iface, target, facetType, physicalTarget);
            this.iface = iface;
            this.target = target;
            this.facetType = facetType;
            this.physicalTarget = physicalTarget;
            traceContext.TraceMethodExit("FacetEvaluationContext");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="facetName"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static FacetEvaluationContext GetFacetEvaluationContext(string facetName, object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            Type facetType = null;
            FacetEvaluationContext context = null;

            if (facetName != null)
            {
                facetType = FacetRepository.GetFacetType(facetName);
            }
            if (facetType != null)
            {
                context = FacetRepository.GetAdapterObject(target, facetType);
            }
            else if ((null == facetName || target.GetType().Name == facetName) && target is ISfcPropertyProvider)
            {
                // Special case: Setup conditions have Facet = NULL

                context = new FacetEvaluationContext(target.GetType(), target, FacetType.Physical);
            }
            else
            {
                throw traceContext.TraceThrow(new Microsoft.SqlServer.Management.Dmf.MissingObjectException(ExceptionTemplatesSR.ManagementFacet, facetName));
            }

            return context;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="facetType"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static FacetEvaluationContext GetFacetEvaluationContext(Type facetType, object target)
        {
            FacetEvaluationContext context = null;
            if (FacetRepository.IsRegisteredFacet(facetType))
            {
                context = FacetRepository.GetAdapterObject(target, facetType);
            }
            else if (target.GetType() == facetType && target is ISfcPropertyProvider)
            {
                context = new FacetEvaluationContext(target.GetType(), target, FacetType.Physical);
            }
            else
            {
                throw traceContext.TraceThrow(new Microsoft.SqlServer.Management.Dmf.MissingObjectException(ExceptionTemplatesSR.ManagementFacet, facetType.Name));
            }

            return context;
        }

        /// <summary>
        /// Alter
        /// </summary>
        public void Alter()
        {
            if (this.Target is IAlterable)
            {
                ((IAlterable)(this.Target)).Alter();
            }
        }

        /// <summary>
        /// Refresh
        /// </summary>
        public void Refresh()
        {
            if (this.Target is IRefreshable)
            {
                ((IRefreshable)(this.Target)).Refresh();
            }
        }

        /// <summary>
        /// Gets named property
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
#if APTCA_ENABLED
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
#endif
        public object GetPropertyValue(string name)
        {
            traceContext.TraceMethodEnter("GetPropertyValue");
            traceContext.TraceParameters(name);

            object res = null;

            if (this.FacetType == FacetType.Physical)
            {
                if (this.Target is ISfcPropertyProvider)
                {
                    bool PropertyFound = ((ISfcPropertyProvider)(this.Target)).GetPropertySet().TryGetPropertyValue(name, out res);

                    if (!PropertyFound)
                    {
                        // $ISSUE: Should be removed once we get those from SFC - VSTS: #103483 
                        //
                        // Special cases - Name and Schema
                        //
                        if (name == "Name" || name == "Schema")
                        {
                            string pname = "get_" + name;

                            MethodInfo mi = this.Target.GetType().GetMethod(pname);
                            if (null != mi)
                            {
                                res = mi.Invoke(this.Target, null);
                                return res;
                            }
                        }

                        throw traceContext.TraceThrow( new MissingPropertyException(name));
                    }
                }
            }
            else
            {
                if (null != this.Interface)
                {
                    string pname = "get_" + name;

                    InterfaceMapping ip = this.Target.GetType().GetInterfaceMap(this.Interface);

                    bool propertyNotFound = true;
                    int idx;
                    int len = ip.InterfaceMethods.Length;
                    for (idx = 0; idx < len; idx++)
                    {
                        if (ip.InterfaceMethods[idx].Name == pname)
                        {
                            MethodInfo mi = ip.TargetMethods[idx];
                            try
                            {
                                res = mi.Invoke(this.Target, null);
                            }
                            catch (TargetInvocationException e) //Exception raised due to un-available property on the target
                            {
                                traceContext.TraceCatch(e);
                                if (e.InnerException is RestartPendingException)
                                {
                                    // if the exception is due to restart pending we 
                                    // want to let it pass through like that
                                    throw traceContext.TraceThrow(e.InnerException);
                                }

                                throw traceContext.TraceThrow(new NonRetrievablePropertyException(name, e.InnerException));
                            }

                            propertyNotFound = false;
                            break;
                        }
                    }

                    if (propertyNotFound)
                    {
                        throw traceContext.TraceThrow(new MissingPropertyException(name));
                    }
                }
            }

            traceContext.TraceParameterOut("return", res);
            traceContext.TraceMethodExit("GetPropertyValue");
            return res;
        }

        /// <summary>
        /// Sets named property
        /// </summary>
        /// <param name="name">property name</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetPropertyValue(string name, object value)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("SetPropertyValue", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(name, value);
                if (!FacetRepository.IsPropertyConfigurable(this.Interface, name))
                {
                    throw methodTraceContext.TraceThrow(new NonConfigurableReadOnlyPropertyException(name));
                }

                if (this.FacetType == FacetType.Physical)
                {
                    if (this.Target is ISfcPropertyProvider)
                    {
                        ISfcProperty p;
                        bool PropertyFound = ((ISfcPropertyProvider)(this.Target)).GetPropertySet().TryGetProperty(name, out p);

                        traceContext.DebugAssert(PropertyFound, "Cannot find verified property");
                        traceContext.DebugAssert(p.Writable, "Property supposed to be writable");
                        p.Value = value;
                    }
                }
                else
                {
                    if (null != this.Interface)
                    {
                        string pname = "set_" + name;

                        InterfaceMapping ip = this.Target.GetType().GetInterfaceMap(this.Interface);

                        bool propertyNotFound = true;
                        int idx;
                        int len = ip.InterfaceMethods.Length;
                        for (idx = 0; idx < len; idx++)
                        {
                            if (ip.InterfaceMethods[idx].Name == pname)
                            {
                                MethodInfo mi = ip.TargetMethods[idx];
                                try
                                {
                                    // First try 'direct' setting

                                    object[] values = new object[1] { value };
                                    mi.Invoke(this.Target, values);
                                }
                                catch (System.ArgumentException)
                                {
                                    methodTraceContext.TraceError("Caught a general Exception of type System.ArgumentException");
                                    // That means the value stored in expression Constant 
                                    // cannot be implicitly converted to actual property type
                                    // (We can get the wrong type when we parse strings into Constants)

                                    object cvalue = Convert.ChangeType(value, mi.GetParameters()[0].ParameterType, System.Globalization.CultureInfo.InvariantCulture);
                                    object[] values = new object[1] { cvalue };
                                    mi.Invoke(this.Target, values);
                                }
                                propertyNotFound = false;
                                break;
                            }
                        }

                        if (propertyNotFound)
                        {
                            traceContext.DebugAssert(false, "Cannot find verified property");
                        }
                    }
                }
            }
        }

    }

    /// <summary>
    /// Helper class that exposes the class factory method
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FacetRepository
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "FacetRepository");
        static readonly Hashtable registeredFacets = new Hashtable();

        // Table matching management facets with Object Types and their Adapters
        // key: (StateInterface, ObjectType)
        // value: Adapter
        static readonly Hashtable FacetNameAndObjectTypeToAdapterMap = new Hashtable();

        // Assemblies scanned for StateInterfaces and Adapters
        static readonly List<string> registeredAssemblies = new List<string>();

        /// <summary>
        /// static constructor to initialize default lookup tables
        /// </summary>
        static FacetRepository()
        {
            BuildDmfLookupTables(Assembly.GetExecutingAssembly());
            BuildDmfLookupTables(typeof(SqlSmoObject).Assembly);
            if (!Microsoft.SqlServer.Server.SqlContext.IsAvailable)
            {
                // note here we use the check-and-try-to-load method 
                // LoadAssemblyHandleErrors instead of use type from Fabirc assembly.
                // The reason is setup has a dependency on DMF during setup time, and 
                // if we let this DMF assembly create a dependency on Utility assembly, 
                // then setup will have dependency on Utility dll, which means Utility
                // dll has to split into a GAC version and an identical but applocal 
                // version just like any other setup dependency DMF/SFC assemblies.
                // We want to avoid that, thus using this dynamically loading mechanism.

                string[] nameparts = Assembly.GetExecutingAssembly ().FullName.Split (new char[] { ',' }, 2);


                // The SAC facets
                string aname = "Microsoft.SqlServer.Dmf.Adapters, " + nameparts[1];
                LoadAssemblyHandleErrors (aname);

                // The Utility facets
                aname = "Microsoft.SqlServer.Management.Utility, " + nameparts[1];
                LoadAssemblyHandleErrors(aname);
            }
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        private static void LoadAssemblyHandleErrors(string assemblyName)
        {
            try
            {
                BuildDmfLookupTables(Assembly.Load(assemblyName));
            }
            catch (System.IO.FileNotFoundException)
            {
                traceContext.TraceError("Caught a general Exception of type System.IO.FileNotFoundException");
                // The type isn't there, move on
            }
            catch (System.IO.FileLoadException)
            {
                traceContext.TraceError("Caught a general Exception of type System.IO.FileLoadException");
                // The assembly isn't there, move on
            }
            catch (System.BadImageFormatException)
            {
                traceContext.TraceError("Caught a general Exception of type System.BadImageFormatException");
                // This isn't good, but DMF won't fail because of it
            }
            catch (System.Reflection.ReflectionTypeLoadException)
            {
                traceContext.TraceError("Caught a general Exception of type System.Reflection.ReflectionTypeLoadException");
                // referenced assemblies are not there or more severe failure
            }
        }




        /// <summary>
        /// Builds lookup tables, used by DMF to associate Management Facets with Objects
        /// and Adapters
        /// </summary>
        /// <param name="assembly">source assembly</param>
        /// <exception cref="AdapterAlreadyExistsException"></exception>
        /// <exception cref="AdapterWrongNumberOfArgumentsException"></exception>
        /// <exception cref="AssemblyAlreadyRegisteredException"></exception>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static void BuildDmfLookupTables(Assembly assembly)
        {
            if (registeredAssemblies.Contains(assembly.FullName))
            {
                throw traceContext.TraceThrow(new AssemblyAlreadyRegisteredException(assembly.FullName));
            }

            registeredAssemblies.Add(assembly.FullName);
            BuildInterfaceLookup(assembly);
            BuildAdapterLookup(assembly);
        }

        /// <summary>
        /// Scans assembly for management facets
        /// </summary>
        /// <param name="assembly">source assembly</param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        static void BuildInterfaceLookup(Assembly assembly)
        {
            FacetType ftype = FacetType.Logical;

            foreach (Type si in assembly.GetTypes())
            {
                if (IsManagementFacet(si))
                {
                    ftype = FacetType.Logical;
                }
                else if (IsPhysicalFacet(si))
                {
                    ftype = FacetType.Physical;
                }
                else
                {
                    continue;
                }

                // Get friendly name and description
                //
                FacetAttributes fas = new FacetAttributes();

                fas.FacetType = ftype;

                fas.IsReadOnly = IsReadOnlyFacet(si);

                // Get EvaluationMode for the facet
                //
                Attribute ema = Attribute.GetCustomAttribute(si, typeof(EvaluationModeAttribute));
                if (null != ema)
                {
                    fas.EvaluationMode = ((EvaluationModeAttribute)ema).AutomatedPolicyEvaluationMode;
                }

                // Get rootfacet for the facet
                //
                Attribute rta = Attribute.GetCustomAttribute(si, typeof(RootFacetAttribute));
                if (null != rta)
                {
                    fas.RootType = ((RootFacetAttribute)rta).RootType;
                }

                registeredFacets.Add(si, fas);
            }
        }

        /// <summary> 
        /// Scans Assembly for Adapters.
        /// Adapter inherits a management facet.
        /// Adapter is bound to {Interface; ObjectType} key pairs. 
        /// The key pairs have to be unique across the entire table.
        /// An object can only have one adapter exposing a management facet.
        /// Adapter constructors must have only one argument.
        /// </summary>
        /// <param name="assembly">source assembly</param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        static void BuildAdapterLookup(Assembly assembly)
        {
            foreach (Type at in assembly.GetTypes())
            {
                if (!at.IsAbstract && !at.IsInterface)
                {
                    foreach (Type iface in at.GetInterfaces())
                    {
                        if (registeredFacets.ContainsKey(iface))
                        {
                            if (IsAdapter(at))
                            {
                                // Identify base types the adapter applies to
                                // (has a constructor, accepting the type)
                                //
                                ConstructorInfo[] constructors = at.GetConstructors();
                                foreach (ConstructorInfo ci in constructors)
                                {
                                    ParameterInfo[] args = ci.GetParameters();
                                    Type objType = args[0].ParameterType;

                                    if (args.Length == 1)
                                    {
                                        InterfaceObjectPair iop = new InterfaceObjectPair(iface, objType);
                                        if (!FacetNameAndObjectTypeToAdapterMap.ContainsKey(iop))
                                        {
                                            FacetNameAndObjectTypeToAdapterMap.Add(iop, at);

                                            FacetAttributes fas = (FacetAttributes)registeredFacets[iface];
                                            fas.FacetType = FacetType.Adapted;
                                            registeredFacets[iface] = fas;
                                        }
                                        else
                                        {
                                            throw traceContext.TraceThrow(new AdapterAlreadyExistsException(iop.Interface.Name, iop.Object.Name));
                                        }
                                    }
                                    else
                                    {
                                        throw traceContext.TraceThrow(new AdapterWrongNumberOfArgumentsException(at.Name));
                                    }
                                }
                            }
                            else
                            {
                                // If type implements a facet interface, but is not an adapter
                                // it must be an implementation of a logical facet

                                InterfaceObjectPair iop = new InterfaceObjectPair(iface, at);
                                if (!FacetNameAndObjectTypeToAdapterMap.ContainsKey(iop))
                                {
                                    FacetNameAndObjectTypeToAdapterMap.Add(iop, null);
                                }
                            }
                        }
                    }
                }
            }

            // Register associations for physical facets
            //
            foreach (Type facet in registeredFacets.Keys)
            {
                FacetAttributes fa = (FacetAttributes)registeredFacets[facet];

                if (fa.FacetType == FacetType.Physical)
                {
                    InterfaceObjectPair iop = new InterfaceObjectPair(facet, facet);

                    if (!FacetNameAndObjectTypeToAdapterMap.ContainsKey(iop))
                    {
                        FacetNameAndObjectTypeToAdapterMap.Add(iop, null);
                    }
                }
            }
        }

        /// <summary>
        ///  Properties for given ManagementFacet
        /// </summary>
        /// <param name="managementFacet">ManagementFacet</param>
        /// <returns>facet properties or null if it's not a facet</returns>
        public static PropertyInfo[] GetFacetProperties(Type managementFacet)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetFacetProperties", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(managementFacet);
                if (!registeredFacets.Contains(managementFacet))
                {
                    methodTraceContext.TraceParameterOut("returnVal", null);
                    return null;
                }

                FacetAttributes fa = (FacetAttributes)registeredFacets[managementFacet];

                if (fa.FacetType == FacetType.Physical)
                {
                    return Utils.GetPhysicalFacetProperties(managementFacet);
                }
                else
                {
                    PropertyInfo[] properties = managementFacet.GetProperties();
                    return properties;
                }
            }
        }

        private static PropertyInfo GetPropertyInfo(Type managementFacet, string propertyName)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetPropertyInfo"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(managementFacet, propertyName);
                if (managementFacet == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("managementFacet"));
                }
                if (propertyName == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("propertyName"));
                }

                PropertyInfo targetProperty = managementFacet.GetProperty(propertyName);
                if (targetProperty == null)
                {
                    throw methodTraceContext.TraceThrow(new MissingPropertyException(propertyName));
                }

                methodTraceContext.TraceParameterOut("returnVal", targetProperty);
                return targetProperty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="managementFacet"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static bool IsPropertyConfigurable(Type managementFacet, string propertyName)
        {
            if (managementFacet == null)
            {
                throw new ArgumentNullException("managementFacet");
            }
            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            if (registeredFacets.Contains(managementFacet))
            {
                FacetAttributes fa = (FacetAttributes)registeredFacets[managementFacet];
                if (fa.IsReadOnly)
                {
                    return false;
                }
            }

            PropertyInfo facetProperty = GetPropertyInfo(managementFacet, propertyName);
            if (!facetProperty.CanWrite)
            {
                return false;
            }

            SfcPropertyAttribute sfcPropertyAttribute = (SfcPropertyAttribute)Attribute.GetCustomAttribute(facetProperty, typeof(SfcPropertyAttribute), true);
            if ((sfcPropertyAttribute != null) && (sfcPropertyAttribute.ReadOnlyAfterCreation == true))
            {
                return false;
            }

            DmfIgnorePropertyAttribute dmfPropertyAttribute = (DmfIgnorePropertyAttribute)Attribute.GetCustomAttribute(facetProperty, typeof(DmfIgnorePropertyAttribute), true);
            if (dmfPropertyAttribute != null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Object types supported by given ManagementFacet
        /// </summary>
        /// <param name="managementFacet"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static List<Type> GetFacetSupportedTypes(Type managementFacet)
        {
            List<Type> rlist = new List<Type>();

            foreach (InterfaceObjectPair iop in FacetNameAndObjectTypeToAdapterMap.Keys)
            {
                if (iop.Interface == managementFacet)
                {
                    rlist.Add(iop.Object);
                }
            }

            rlist.Sort(delegate(Type t1, Type t2)
                        {
                            return t1.Name.CompareTo(t2.Name);
                        }
                      );
            return rlist;
        }

        /// <summary>
        /// Root facets currently registered
        /// </summary>
        /// <param name="rootType"></param>
        /// <returns></returns>
        public static List<Type> GetRootFacets(Type rootType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetRootFacets", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(rootType);
                List<Type> rlist = new List<Type>();

                if (null != rootType)
                {
                    foreach (Type facet in registeredFacets.Keys)
                    {
                        FacetAttributes fa = (FacetAttributes)registeredFacets[facet];

                        if (fa.RootType == rootType)
                        {
                            rlist.Add(facet);
                        }
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", rlist);
                return rlist;
            }
        }

        /// <summary>
        /// Signals whether given facet is a root facet for given Root
        /// </summary>
        /// <param name="rootType"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static bool IsRootFacet(Type rootType, Type facet)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("IsRootFacet", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(rootType, facet);
                if (null == rootType)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("rootType"));
                }

                return (rootType == ((FacetAttributes)registeredFacets[facet]).RootType);
            }
        }

        /// <summary>
        /// Returns root type supported by given facet
        /// </summary>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static Type GetFacetSupportedRootType(Type facet)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetFacetSupportedRootType", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(facet);
                Type rootType = null;

                foreach (Type type in FacetRepository.GetFacetSupportedTypes(facet))
                {
                    rootType = Microsoft.SqlServer.Management.Sdk.Sfc.Metadata.SfcMetadataDiscovery.GetRootFromType(type);

                    // We only support one domain for root check
                    break;
                }

                methodTraceContext.TraceParameterOut("returnVal", rootType);
                return rootType;
            }
        }

        /// <summary>
        /// Returns type implementing the facet
        /// </summary>
        /// <param name="target">ObjectType of object</param>
        /// <param name="facet">Interface</param>
        /// <returns>Type implementing the facet</returns>
        /// <exception cref="NullFacetException">facet is not registered</exception>
        /// <exception cref="MissingTypeFacetAssociationException">target is not associated with facet</exception>
        internal static Type GetFacetImplementingType(Type target, Type facet)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetFacetImplementingType"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target, facet);
                if (!IsRegisteredFacet(facet))
                {
                    throw methodTraceContext.TraceThrow(new NullFacetException(facet.Name));
                }

                switch (((FacetAttributes)registeredFacets[facet]).FacetType)
                {
                    case FacetType.Physical:
                        methodTraceContext.TraceParameterOut("returnVal", facet);
                        return facet;
                    case FacetType.Logical:
                        methodTraceContext.TraceParameterOut("returnVal", target);
                        return target;
                    case FacetType.Adapted:
                        InterfaceObjectPair iop = new InterfaceObjectPair(facet, target);
                        Type adapterType = (Type)FacetNameAndObjectTypeToAdapterMap[iop];
                        traceContext.DebugAssert(null != adapterType, "No adapter registered for adapted facet");
                        methodTraceContext.TraceParameterOut("returnVal", adapterType);
                        return adapterType;
                    default:
                        traceContext.DebugAssert(true, "Unknown Facet type");
                        methodTraceContext.TraceParameterOut("returnVal", null);
                        return null;
                }
            }
        }

        /// <summary>
        /// Returns context (facet interface and implementing object)
        /// Instantiates an Adapter for adapted facets
        /// </summary>
        /// <param name="target"></param>
        /// <param name="facet"></param>
        /// <returns>Adapter or NULL if there is no adapter for given arguments</returns>
        /// <exception cref="NullFacetException">facet is not registered</exception>
        /// <exception cref="MissingTypeFacetAssociationException">target is not associated with facet</exception>
#if APTCA_ENABLED
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
#endif
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static FacetEvaluationContext GetAdapterObject(object target, Type facet)
        {
            object obj = null;
            FacetEvaluationContext context = null;

            if (!IsRegisteredFacet(facet))
            {
                throw traceContext.TraceThrow(new NullFacetException(facet.Name));
            }

            VerifyAssociation(target.GetType(), facet);

            FacetAttributes fa = (FacetAttributes)registeredFacets[facet];

            if (fa.FacetType == FacetType.Adapted)
            {
                // These facets are supposed to have adapters

                Type adapterType = GetFacetImplementingType(target.GetType(), facet);
                if (null == adapterType)
                {
                    throw traceContext.TraceThrow(new MissingTypeFacetAssociationException(target.GetType().Name, facet.Name));
                }

                obj = System.Activator.CreateInstance(adapterType, target);

                context = new FacetEvaluationContext(facet, obj, fa.FacetType, target);
            }
            else
            {
                context = new FacetEvaluationContext(facet, target, fa.FacetType, target);
            }

            return context;
        }

        /// <summary>
        /// Returns a list of management facets exposed by given Object Type
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static List<Type> GetFacetsForType(Type target)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetFacetsForType", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target);
                List<Type> rlist = new List<Type>();

                foreach (InterfaceObjectPair iop in FacetNameAndObjectTypeToAdapterMap.Keys)
                {
                    if (iop.Object.Equals(target))
                    {
                        rlist.Add(iop.Interface);
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", rlist);
                return rlist;
            }
        }

        internal static int RegisteredFacetsCount
        {
            get
            {
                return registeredFacets.Count;
            }
        }

        /// <summary>
        /// List of all registered facets
        /// </summary>
        /// <returns></returns>
        public static IEnumerable RegisteredFacets
        {
            get
            {
                return registeredFacets.Keys as IEnumerable;
            }
        }

        /// <summary>
        /// Gets type of named facet
        /// </summary>
        /// <param name="facetShortName"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static Type GetFacetType(string facetShortName)
        {
            foreach (Type facet in registeredFacets.Keys)
            {
                if (facet.Name == facetShortName)
                {
                    return facet;
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="facet"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static AutomatedPolicyEvaluationMode GetFacetEvaluationMode(Type facet)
        {
            if (registeredFacets.ContainsKey(facet))
            {
                return ((FacetAttributes)registeredFacets[facet]).EvaluationMode;
            }
            else
            {
                traceContext.TraceParameterOut("returnVal", AutomatedPolicyEvaluationMode.None);
                return AutomatedPolicyEvaluationMode.None;
            }
        }

        /// <summary>
        /// Checks if provided facet is Registered by DMF
        /// </summary>
        /// <param name="facet"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static bool IsRegisteredFacet(Type facet)
        {
            return registeredFacets.ContainsKey(facet);
        }

        /// <summary>
        /// Checks if provided type is a facet (an interface, inheriting IDmfFacet)
        /// </summary>
        /// <param name="managementFacet"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        static bool IsManagementFacet(Type managementFacet)
        {
            return ((managementFacet.IsPublic || (managementFacet.IsNested && managementFacet.IsNestedPublic))
                && managementFacet.IsInterface && (null != managementFacet.GetInterface("IDmfFacet")));
        }

        /// <summary>
        /// Internal for test purposes. Do not use!
        /// Checks if provided type is a physical facet (a type marked as PhysicalFacet implementing ISfcPropertySet)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static bool IsPhysicalFacet(Type type)
        {
            return (type.IsPublic || (type.IsNested && type.IsNestedPublic))
                && type.IsClass && !type.IsAbstract && (null != type.GetInterface("ISfcPropertyProvider"))
                && null != Attribute.GetCustomAttribute(type, typeof(PhysicalFacetAttribute));
        }


        /// <summary>
        /// Checks if provided type is a read only physical facet (a type marked as PhysicalFacet(PhysicalFacetOptions.ReadOnly) implementing ISfcPropertySet)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static bool IsReadOnlyFacet(Type type)
        {
            bool isReadonly = false;
            PhysicalFacetAttribute physicalInfo = (PhysicalFacetAttribute)Attribute.GetCustomAttribute(type, typeof(PhysicalFacetAttribute));
            if (physicalInfo != null)
            {
                isReadonly = physicalInfo.IsReadOnly;
            }

            return isReadonly;
        }

        /// <summary>
        /// Checks if provided type is an adapter (a class, implementing IDmfAdapter)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        static bool IsAdapter(Type type)
        {
            return (!type.IsInterface && (null != type.GetInterface("IDmfAdapter")));
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        private static void VerifyAssociation(Type target, Type facet)
        {
            InterfaceObjectPair iop = new InterfaceObjectPair(facet, target);

            if (!FacetNameAndObjectTypeToAdapterMap.ContainsKey(iop))
            {
                throw traceContext.TraceThrow(new MissingTypeFacetAssociationException(target.Name, facet.Name));
            }
        }

        internal sealed class InterfaceObjectPair
        {
            Type iface;
            Type obj;

            public Type Interface { get { return iface; } }
            public Type Object { get { return obj; } }

            public InterfaceObjectPair(Type iface, Type obj)
            {
                this.iface = iface;
                this.obj = obj;
            }

            public override string ToString()
            {
                return String.Format("{0} ; {1}", this.Interface.ToString(), this.Object.ToString());
            }

            // Overriding Equals and GetHashCode required to work as a HashTable key
            public override bool Equals(object obj)
            {
                if (obj is InterfaceObjectPair)
                {
                    InterfaceObjectPair iop = (InterfaceObjectPair)obj;
                    return this.Interface.Equals(iop.Interface) && this.Object.Equals(iop.Object);
                }

                return false;
            }

            // Overriding Equals and GetHashCode required to work as a HashTable key
            public override int GetHashCode()
            {
                return (this.Interface.GetHashCode() & this.Object.GetHashCode());
            }
        }
    }
}
