// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    class DomainRegistrationEncapsulation
    {
        private static SfcDomainInfoCollection domains = null;
        private static SfcDomainInfo[] registeredDomains = new SfcDomainInfo[] {
            new SfcDomainInfo("DMF","Microsoft.SqlServer.Management.Dmf.PolicyStore","Microsoft.SqlServer.Dmf", "SQLSERVER:\\SQLPolicy"),
            new SfcDomainInfo("DC","Microsoft.SqlServer.Management.Collector.CollectorConfigStore","Microsoft.SqlServer.Management.Collector", "SQLSERVER:\\DataCollection"),
            new SfcDomainInfo("XEvent","Microsoft.SqlServer.Management.XEvent.XEStore","Microsoft.SqlServer.Management.XEvent", "SQLSERVER:\\XEvent"),
            new SfcDomainInfo("DatabaseXEvent","Microsoft.SqlServer.Management.XEventDbScoped.DatabaseXEStore","Microsoft.SqlServer.Management.XEventDbScoped", "SQLSERVER:\\DatabaseXEvent"),
            new SfcDomainInfo("SMO","Microsoft.SqlServer.Management.Smo.Server","Microsoft.SqlServer.Smo", "SQLSERVER:\\SQL"),
            new SfcDomainInfo("RegisteredServers","Microsoft.SqlServer.Management.RegisteredServers.RegisteredServersStore","Microsoft.SqlServer.Management.RegisteredServers", "SQLSERVER:\\SQLRegistration"),
            new SfcDomainInfo("Utility","Microsoft.SqlServer.Management.Utility.Utility","Microsoft.SqlServer.Management.Utility", "SQLSERVER:\\Utility"),
    	    new SfcDomainInfo("SSIS","Microsoft.SqlServer.Management.IntegrationServices.IntegrationServices","Microsoft.SqlServer.Management.IntegrationServices", "SQLSERVER:\\SSIS"),
            new SfcDomainInfo("DependencyServices","Microsoft.SqlServer.Management.DependencyServices.DependencyServicesStore","Microsoft.SqlServer.Management.DependencyServices", "SQLSERVER:\\DependencyServices"),
            new SfcDomainInfo("DAC","Microsoft.SqlServer.Management.Dac.DacDomain","Microsoft.SqlServer.Management.Dac", "SQLSERVER:\\DAC"),
        };

        private static SfcDomainInfo[] testDomains = new SfcDomainInfo[] {
            new SfcDomainInfo("ACME","Microsoft.SqlServer.Test.ManagementSDKTests.AcmePrototype.ACMEServer","Microsoft.SqlServer.Test.ManagementSDKTests.AcmePrototype",null,false),
            new SfcDomainInfo("FlashSFC","Microsoft.SqlServer.Test.ManagementSDKTests.FlashSfc.FlashSfcServer","Microsoft.SqlServer.Test.ManagementSDKTests.FlashSfc",null,false),
        };
        internal static SfcDomainInfoCollection Domains
        {
            get
            {
                if (domains == null)
                {
                    domains = new SfcDomainInfoCollection(registeredDomains);
                }
                return domains;
            }
        }

        internal static void AddTestDomainsToDomainsList()
        {
            List<SfcDomainInfo> temp = new List<SfcDomainInfo>();
            temp.AddRange(registeredDomains);
            temp.AddRange(testDomains);
            domains = new SfcDomainInfoCollection(temp);
        }
    }
    public sealed class SfcRegistration
    {
        private SfcRegistration() { } //Static Holders should not have constructors

        // This is the guy should be Vlads implementation of the registration service directly
        // We shouldn't need to call the servicing model, because SFC doesn't support it.
        // We may need a wrapper that both VS and SFC call in to ensure there is only a single per app domain.
        public static object CreateObject(string fullTypeName)
        {
            Type instanceType = GetObjectTypeFromFullName(fullTypeName);

            // TODO: Figure out if you want GetType to throw the exception or do you want to handle it.
            // At the very least you need to catch and enclose it in our exception type.

            // TODO: this will be rewritten to use object factory and not reflection
            return Activator.CreateInstance(instanceType, true);
        }

        /// <summary>
        /// Get a fully qualified .Net type from fully qualified type name
        /// If assembly is not registered, exception is thrown.
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        public static Type GetObjectTypeFromFullName(string fullTypeName)
        {
            // The default lookup is to be case-sensitive
            return GetObjectTypeFromFullName(fullTypeName, false);
        }

        /// <summary>
        /// Get a fully qualified .Net type from fully qualified type name
        /// If assembly is not registered, exception is thrown.
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <param name="ignoreCase">true to ignore the case of the type name; otherwise, false.</param>
        /// <returns></returns>
        public static Type GetObjectTypeFromFullName(string fullTypeName, bool ignoreCase)
        {
            Assembly sfcExtension = LocateSfcExtension(GetRegisteredDomainForType(fullTypeName), true);
            return sfcExtension.GetType(fullTypeName, true, ignoreCase);
        }

        /// <summary>
        /// Get a fully qualified .Net type from fully qualified type name
        /// If assembly is not registered, null is returned.
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        public static Type TryGetObjectTypeFromFullName(string fullTypeName)
        {
            Type returnType = Type.GetType(fullTypeName);
            if (returnType != null)
            {
                return returnType;
            }

            Assembly sfcExtension = LocateSfcExtension(GetRegisteredDomainForType(fullTypeName,false), false);
            if (sfcExtension == null)
            {
                return null;
            }

            return sfcExtension.GetType(fullTypeName, true, false);
        }

        /// <summary>
        /// Loads a specific assembly based on domain's name
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns></returns>
        private static Assembly LocateSfcExtension(string domainName)
        {
            return LocateSfcExtension(domainName, true);
        }


        /// <summary>
        /// Loads a specific assembly based on domain's name
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="throwOnUnregisteredDomain"></param>
        /// <returns></returns>
        private static Assembly LocateSfcExtension(string domainName,bool throwOnUnregisteredDomain)
        {
            Assembly sfcExtension = null;
#if !SETUP_SUPPORT
            foreach (SfcDomainInfo domain in Domains)
            {
		if (string.Compare(domain.Name.ToUpperInvariant(), domainName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (!domain.IsAssemblyInGAC)//ACME
                    {
                        sfcExtension = SmoManagementUtil.LoadAssemblyFromFile(new FileInfo(domain.AssemblyStrongName).FullName);
                    }
                    else
                    {
                        sfcExtension = SmoManagementUtil.LoadAssembly(domain.AssemblyStrongName);
                    }
                }
            }
#else
            AssemblyName assemblyName = new AssemblyName(GetFullAssemblyName("Microsoft.SqlServer.Configuration.Dmf"));
	    sfcExtension = Assembly.Load(assemblyName);
#endif	    
            if (sfcExtension == null && throwOnUnregisteredDomain)
            {
                throw new SfcUnregisteredXmlDomainException(SfcStrings.UnregisteredXmlSfcDomain(domainName));
            }

            return sfcExtension;
        }


        /// <summary>
        /// Returns the domain name for a specific type
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        public static string GetRegisteredDomainForType(string fullTypeName)
        {
            return GetRegisteredDomainForType(fullTypeName, true);
        }

        /// <summary>
        /// Returns the domain name for a specific type
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <param name="throwOnUnregisteredDomain"></param>
        /// <returns></returns>
        public static string GetRegisteredDomainForType(string fullTypeName,bool throwOnUnregisteredDomain)
        {
            foreach (SfcDomainInfo domain in Domains)
            {
                if (fullTypeName.Contains(domain.DomainNamespace))
                {
                    return domain.Name.ToUpperInvariant();
                }
            }
            if(throwOnUnregisteredDomain)
            {
                throw new SfcUnregisteredXmlTypeException(SfcStrings.UnregisteredSfcXmlType(string.Empty, fullTypeName), null);
            }

            return null;
        }


        /// <summary>
        /// Returns the domain Information object for a given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SfcDomainInfo GetRegisteredDomainForType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return Domains[GetRegisteredDomainForType(type.FullName)];
        }

        /// <summary>
        /// Construct the full assembly name out of just an assembly/dll name [microsoft.sqlserver.dmf.dll for example]. 
        /// Since we don't support external plugins in this release,
        /// all parts of its strong name except for the assembly name should be exactly the same as ours,
        /// so we use this logic for creating the full name.
        /// NOTE: assembly name can be both with or without .DLL or .EXE extensions
        /// (adapted from OE StaticHelpers.cs)
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        internal static string GetFullAssemblyName(string assemblyName)
        {
            // We'll use the name to construct full assembly name. If what we got ends with
            //.DLL or some other suffix, we should get rid of it, as file extension is not part of assembly name.
            if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                assemblyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = System.IO.Path.GetFileNameWithoutExtension(assemblyName);
            }

            // Construct full target assembly name. Since we don't support external plugins in v1,
            // all parts of its strong name except for the assembly name should be exactly the same as ours.
            System.Reflection.Assembly ourAssembly = SmoManagementUtil.GetExecutingAssembly();
            String fullName = ourAssembly.FullName;

            // Real SFC needs to use SqlClrProvider to load assemblies; Setup version doesn't care
            fullName = fullName.Replace(SmoManagementUtil.GetAssemblyName(ourAssembly), assemblyName);

            return fullName;
        }

        /// <summary>
        /// A static function to return the current registered domains, currenly it will return a hard coded list for the only known
        /// domains: SMO, DC, Acme and DMF
        /// </summary>
        public static SfcDomainInfoCollection Domains
        {
            get
            {
                return DomainRegistrationEncapsulation.Domains;
            }
        }

        internal static void AddTestDomainsToDomainsList()
        {
            DomainRegistrationEncapsulation.AddTestDomainsToDomainsList();
        }
    }

    /// <summary>
    /// Class to hold domains' information
    /// </summary>
    public sealed class SfcDomainInfo
    {
        private string name = null;
        private Type rootType = null;
        private string rootTypeFullName = null;
        private string namespaceQualifier = null;
        private string assemblyStrongName = null;
        private string domainNamespace = null;
        private string psDriveName = null;
        private bool   assemblyInGAC = true;

        #region Constructors
        internal SfcDomainInfo(string namespaceQualifier, string rootTypeFullName, string assemblyName, string psDriveName)
            : this(namespaceQualifier, rootTypeFullName, assemblyName, psDriveName, true)
        {
        }

        internal SfcDomainInfo(string namespaceQualifier, string rootTypeFullName, string assemblyName, string psDriveName,
            bool assemblyInGAC)
        {
            this.rootTypeFullName = rootTypeFullName;
            if (assemblyInGAC)
            {
                this.assemblyStrongName = SfcRegistration.GetFullAssemblyName(assemblyName);
            }
            else
            {
                this.assemblyStrongName = assemblyName;
                if (!this.assemblyStrongName.ToUpperInvariant().EndsWith(".DLL",StringComparison.Ordinal))
                {
                    this.assemblyStrongName += ".dll";
                }
            }
            this.name = rootTypeFullName.Substring(rootTypeFullName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            this.namespaceQualifier = namespaceQualifier;
            this.domainNamespace = rootTypeFullName.Substring(0, rootTypeFullName.LastIndexOf(".", StringComparison.Ordinal));
            this.psDriveName = psDriveName;
            this.assemblyInGAC = assemblyInGAC;
        }
        #endregion

        #region Public Properties
        public string Name
        {
            get
            {
                return name;
            }
            internal set
            {
                name = value;
            }
        }

        public bool IsAssemblyInGAC
        {
            get
            {
                return this.assemblyInGAC;
            }
        }

        public string DomainNamespace
        {
            get
            {
                return domainNamespace;
            }
        }

        public Type RootType
        {
            get
            {
                if (rootType == null)
                {
                    LateLoadRootType();
                }
                return rootType;
            }
            internal set
            {
                rootType = value;
            }
        }

        public string RootTypeFullName
        {
            get
            {
                return rootTypeFullName;
            }
            internal set
            {
                rootTypeFullName = value;
            }
        }

        public string AssemblyStrongName
        {
            get
            {
                return assemblyStrongName;
            }
            internal set
            {
                assemblyStrongName = value;
            }
        }

        public string NamespaceQualifier
        {
            get
            {
                return namespaceQualifier;
            }
            internal set
            {
                this.namespaceQualifier = value;
            }
        }

        public string PSDriveName
        {
            get
            {
                return this.psDriveName;
            }
            internal set
            {
                this.psDriveName = value;
            }
        }
        #endregion

        #region Private & Internal Properties and Functions
        /// <summary>
        /// Load root type from an assembly
        /// </summary>
        private void LateLoadRootType()
        {
            Assembly assembly = null;
            if (!this.IsAssemblyInGAC)
            {
                assembly = SmoManagementUtil.LoadAssemblyFromFile(new FileInfo(assemblyStrongName).FullName);
            }
            else
            {
                assembly = SmoManagementUtil.LoadAssembly(assemblyStrongName);
            }
            this.rootType = assembly.GetType(rootTypeFullName);
        }


        public int GetLogicalVersion(object instance)
        {
            if (instance is SfcInstance)
            {
                SfcInstance sfcInstance = instance as SfcInstance;

                ISfcDomain domain = sfcInstance.KeyChain.Domain;
                return domain.GetLogicalVersion();
            }
            else if (instance is IAlienObject)
            {
                return ((IAlienObject)instance).GetDomainRoot().GetLogicalVersion();
            }
            else
            {
                //$TODO - string has been localized but needs review
                throw new SfcSerializationException(SfcStrings.DomainRootUnknown(instance.GetType().FullName));
            }
        }

        #endregion
    }

    /// <summary>
    /// Readonly Collection to hold the current domains information
    /// Currently it holds information only about SMO, DC, DMF and ACME
    /// TODO: Add extensions to support loading more domains information from XML documents.
    /// </summary>
    public class SfcDomainInfoCollection : ReadOnlyCollection<SfcDomainInfo>
    {
        public SfcDomainInfoCollection(System.Collections.Generic.IList<SfcDomainInfo> list)
            :base(list)
        { }
        /// <summary>
        /// Finds if a specific domain exists in the collection or not using domain's name.
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns></returns>
        public bool Contains(string domainName)
        {
            foreach (SfcDomainInfo info in this.Items)
            {
                if (string.Compare(info.Name, domainName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get domain from the current collection using domain's name
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns></returns>
        public SfcDomainInfo this[string domainName]
        {
            get
            {
                foreach (SfcDomainInfo info in this.Items)
                {
                    if (string.Compare(info.Name, domainName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return info;
                    }
                }
                throw new SfcMetadataException(SfcStrings.DomainNotFound(domainName));
            }
        }

        /// <summary>
        /// Get domain from the current collection using Namespace Qualifier name
        /// </summary>
        /// <param name="namespaceQualifier"></param>
        /// <returns></returns>
        public SfcDomainInfo GetDomainForNamespaceQualifier(String namespaceQualifier)
        {
            foreach (SfcDomainInfo info in this.Items)
            {
                if (string.Compare(info.NamespaceQualifier, namespaceQualifier, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return info;
                }
            }
            throw new SfcMetadataException(SfcStrings.DomainNotFound(namespaceQualifier));
        }
    }
}
