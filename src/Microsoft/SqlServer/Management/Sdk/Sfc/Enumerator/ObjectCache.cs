// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.IO;
    using System.Security;
#if STRACE
    using Microsoft.SqlServer.Management.Diagnostics;
#else
#endif
#if !NETCOREAPP
    using System.Security.Permissions;
    using Microsoft.SqlServer.Smo.UnSafeInternals;
#endif
    using System.Globalization;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.SqlServer.Management.Common;
#if SMOCODEGEN
    using Microsoft.SqlServer.Management.Smo;
#endif

    class CacheElement
    {
        EnumObject element;
        ObjectLoadInfo oli;
        uint usage;
        CacheKey key;

        public CacheElement(ObjectLoadInfo oli, EnumObject element, CacheKey key)
        {
            usage = 0;
            this.oli = oli;
            this.element = element;
            this.key = key;
        }

        public EnumObject EnumObject
        {
            get
            {
                return element;
            }
        }

        public CacheKey CacheKey
        {
            get
            {
                return key;
            }
        }

        public uint Usage
        {
            get
            {
                return usage;
            }
        }

        public void IncrementUsage()
        {
            if( usage < ObjectCache.MaxUsagePoints )
            {
                usage++;
            }
        }

        public void DecrementUsage()
        {
            if( usage > 0 )
            {
                usage--;
            }
        }

        public String[] GetChildren()
        {
            String[] str = new String[oli.Children.Count];

            int i = 0;
            foreach(ObjectLoadInfo olichild in oli.Children.Values)
            {
                str[i++] = olichild.Name;
            }
            return str;
        }
    }

    internal class CacheKey : IComparable
    {
        private readonly uint base_key;
        private readonly uint version;
        private readonly DatabaseEngineType databaseEngineType;
        private readonly DatabaseEngineEdition databaseEngineEdition;

        private uint same_obj_key;

        public CacheKey(uint base_key, uint version, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            this.base_key = base_key;
            this.version = version;
            this.same_obj_key = 0;
            this.databaseEngineType = databaseEngineType;
            this.databaseEngineEdition = databaseEngineEdition;
        }

        public uint SameObjKey
        {
            get
            {
                return same_obj_key;
            }
            set
            {
                same_obj_key = value;
            }
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}.{1}", base_key, version);
        }

        public int CompareTo(object o)
        {
            CacheKey k = (CacheKey)o;

            if( base_key < k.base_key )
            {
                return -1;
            }
            if( base_key > k.base_key )
            {
                return 1;
            }
            if( version < k.version )
            {
                return -1;
            }
            if( version > k.version )
            {
                return 1;
            }
            if( same_obj_key < k.same_obj_key )
            {
                return -1;
            }
            if( same_obj_key > k.same_obj_key )
            {
                return 1;
            }
            if (databaseEngineType < k.databaseEngineType)
            {
                return -1;
            }
            if (databaseEngineType > k.databaseEngineType)
            {
                return 1;
            }
            if (databaseEngineEdition < k.databaseEngineEdition)
            {
                return -1;
            }
            if (databaseEngineEdition > k.databaseEngineEdition)
            {
                return 1;
            }
            return 0;
        }
    }


    internal class ObjectCache
    {
        public const uint MaxUsagePoints = 15;
        public const uint PingsForAging = 1;
        const uint MaxCacheSize = 15;

        /// This value is used as the maximum number of instances of
        /// the same object that will be stored in the cache. This has
        /// impact on the enumerator's support of recursive types, so
        /// if you edit this please make sure the login in the
        /// construction of the XmlReadSettings class in XmlRead.cs is
        /// still valid. It should be no matter what this number
        /// changes to.
        internal const uint SameObjectNumber = 2;

        static uint m_CurrentPings = 0;
        static SortedList m_cache = new SortedList();
        static Object lock_obj = new Object();

        static public CacheElement GetElement(Urn urn, ServerVersion ver, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition, object ci)
        {
            ObjectLoadInfo oli = ObjectLoadInfoManager.GetObjectLoadInfo(urn, ci);

            return GetElement(oli, ver, databaseEngineType, databaseEngineEdition);
        }

        static public void PutElement(CacheElement elem)
        {
            SmoManagementUtil.EnterMonitor(lock_obj);

            try
            {
                if( m_cache.Count >= MaxCacheSize )
                {
                    if( ++m_CurrentPings > PingsForAging )
                    {
                        TryInsert(elem, true);
                        m_CurrentPings = 0;
                    }
                    else
                    {
                        TryInsert(elem, false);
                    }
                }
                else
                {
                    InsertInCache(elem);
                }
            }
            finally
            {
                SmoManagementUtil.ExitMonitor(lock_obj);
            }
        }

        static public EnumObject LoadFirstElementVersionless(Urn urn, object ci)
        {
            ObjectLoadInfo oli = ObjectLoadInfoManager.GetFirstObjectLoadInfo(urn, ci);
            return LoadElement(oli);
        }

        static public ArrayList GetAllElements(Urn urn, ServerVersion ver, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition, object ci)
        {
            ArrayList listOli = ObjectLoadInfoManager.GetAllObjectsLoadInfo(urn, ci);

            ArrayList listElements = new ArrayList();
            foreach(ObjectLoadInfo oli in listOli)
            {

                listElements.Add(GetElement(oli, ver, databaseEngineType, databaseEngineEdition));
            }
            return listElements;
        }

        static public void PutAllElements(ArrayList list)
        {
            foreach(CacheElement elem in list)
            {
                PutElement(elem);
            }
        }

        static uint GetNumberFromVersion(ServerVersion ver)
        {
            if( null != ver )
            {
                return (uint)(ver.Major * 100 + ver.Minor);
            }
            return 0;
        }

        static private CacheElement GetElement(ObjectLoadInfo oli, ServerVersion ver, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            SmoManagementUtil.EnterMonitor(lock_obj);

            CacheElement elem;

            try
            {
                elem = FindInCacheAndRemove(oli, ver, databaseEngineType, databaseEngineEdition);
                if( null == elem )
                {
#if DEBUGTRACE
                    Enumerator.TraceInfo("cache miss: obj={0} objcode={1} ver={2} {3}", oli.Name, oli.UniqueKey, ver, ShowCache());
#endif
                    elem = LoadElement(oli, ver, databaseEngineType, databaseEngineEdition);
                }
#if DEBUGTRACE
                else
                {
                    Enumerator.TraceInfo("cache hit: obj={0} objcode={1} ver={2} hit on key={3} usage={4} {5}", oli.Name, oli.UniqueKey, ver, elem.CacheKey.ToString(), elem.Usage.ToString(), ShowCache());
                }
#endif
                elem.IncrementUsage();
            }
            finally
            {
                SmoManagementUtil.ExitMonitor(lock_obj);
            }
            return elem;
        }

        static private CacheElement FindInCacheAndRemove(ObjectLoadInfo oli, ServerVersion ver, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            CacheElement el = null;
            CacheKey key = new CacheKey(oli.UniqueKey, GetNumberFromVersion(ver), databaseEngineType, databaseEngineEdition);
            for(uint i = 0; i < SameObjectNumber; i++)
            {
                key.SameObjKey = i;
                el = (CacheElement)m_cache[key];
                if( null != el )
                {
                    m_cache.Remove(key);
                    break;
                }
            }
            return el;
        }

        static private EnumObject LoadElement(ObjectLoadInfo oli)
        {
            Object o;

            if (oli.AssemblyReference != null)
            {
                o = ObjectCache.CreateObjectInstance(oli.AssemblyReference, oli.ImplementClass);
            }
            else
            {
                o = ObjectCache.CreateObjectInstance(oli.Assembly, oli.ImplementClass);
            }
            
            EnumObject en = o as EnumObject;
            if( null == en )
            {
                throw new InternalEnumeratorException(SfcStrings.NotDerivedFrom(oli.ImplementClass, "EnumObject"));
            }

            return en;
        }

        static private CacheElement LoadElement(ObjectLoadInfo oli, ServerVersion ver, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            EnumObject en = LoadElement(oli);
            if( null != oli.InitData )
            {
                ISupportInitDatabaseEngineData idt = en as ISupportInitDatabaseEngineData;
                if (idt != null)
                {
                    idt.LoadInitData(oli.InitData, ver, databaseEngineType, databaseEngineEdition);
                }
                else
                {
                    ISupportInitData ic = en as ISupportInitData;
                    if (null == ic)
                    {
                        throw new InternalEnumeratorException(SfcStrings.ISupportInitDataNotImplement(oli.InitData));
                    }
                    ic.LoadInitData(oli.InitData, ver);
                }
            }

            return new CacheElement(oli, en, new CacheKey(oli.UniqueKey, GetNumberFromVersion(ver), databaseEngineType, databaseEngineEdition));
        }

        static private bool InsertInCache(CacheElement elem)
        {
            CacheElement elpres = null;
            for(uint i = 0; i < SameObjectNumber; i++)
            {
                elem.CacheKey.SameObjKey = i;
                elpres = (CacheElement)m_cache[elem.CacheKey];
                if( null == elpres )
                {
                    m_cache[elem.CacheKey] = elem;
                    return true;
                }
            }
            elpres.IncrementUsage();
            return false;
        }

        static private void TryInsert(CacheElement elem, bool bWithAging)
        {
            uint smallestUsage = 0;

            int i = -1;
            int smallestUsagePos = 0;
            foreach(CacheElement el in m_cache.Values)
            {
                i++;
                if( bWithAging )
                {
                    el.DecrementUsage();
                }
                if( smallestUsage >= el.Usage || 0 == i  )
                {
                    smallestUsagePos = i;
                    smallestUsage = el.Usage;
                }
            }

            if( elem.Usage < smallestUsage )
            {
                return;
            }
            if( InsertInCache(elem) )
            {
                m_cache.RemoveAt(smallestUsagePos);
            }
        }

        // Avoid re-loading the same assembly multiple times. This is especially useful for
        // Acme, which throws an exception on attempt to load from GAC and then loads from current directory
        static Dictionary<string,Assembly> assemblyCache = new Dictionary<string,Assembly>();

        // VSTS 109337: Supress: Consider adding a security demand to ObjectCache.CreateObjectInstance(Assembly, String):Object.
        // The following call stack might expose a way to circumvent security protection.
        // $ISSUE 122248: Our parameters always come from a closed metadata definition system in Katmai. If this changes revisit.
        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
#if !NETCOREAPP
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
#endif
        static Assembly LoadAssembly(string fullName)
        {
            lock( assemblyCache )
            {
                Assembly a = null;

                if( assemblyCache.TryGetValue(fullName, out a) )
                {
                    return a;
                }

                // Attempt to load the assembly given its display name
                AssemblyName assemblyName = new AssemblyName(fullName);
                Version ourVersion = SmoManagementUtil.GetExecutingAssembly().GetName().Version;
                byte[] ourKey = SmoManagementUtil.GetExecutingAssembly().GetName().GetPublicKey();

                try
                {
                    // Try using our own SFC/Enumerator assembly full name and just substitute the simple name portion
                    // while retaining the version and public key strength
                    AssemblyName useExecutingName = new AssemblyName(SmoManagementUtil.GetExecutingAssembly().FullName);
                    useExecutingName.Name = assemblyName.Name;

                    try
                    {
                        a = SmoManagementUtil.LoadAssembly(useExecutingName.FullName);
                    }
                    // The assembly we're trying to load was not found in our current version, try to load the most recent version instead
                    // For example, in a typical Katmai tools install with CE 3.5, the strong name of the CE enumerator is:
                    // Microsoft.SqlServerCe.Enumerator, Version=3.5.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91, processorArchitecture=MSIL
                    // This can either throw a FileLoadException or FileNotFoundException
                    catch (System.IO.FileLoadException)
                    {
                        useExecutingName.Version = null;
                        return Assembly.Load(useExecutingName);
                    }
                    catch (FileNotFoundException)
                    {
                        useExecutingName.Version = null;
                        return Assembly.Load(useExecutingName);
                    }
                }
                //transform the fail to load assembly in a custom assembly
                catch (FileNotFoundException e)
                {
                    throw new InternalEnumeratorException(SfcStrings.FailedToLoadAssembly(fullName), e);
                }
                catch (BadImageFormatException e)
                {
                    throw new InternalEnumeratorException(SfcStrings.FailedToLoadAssembly(fullName), e);
                }
                catch (SecurityException e)
                {
                    throw new InternalEnumeratorException(SfcStrings.FailedToLoadAssembly(fullName), e);
                }

                if( null == a )
                {
                    throw new InternalEnumeratorException(SfcStrings.FailedToLoadAssembly(fullName));
                }

                assemblyCache.Add(fullName,a);

                return a;
            }
        }

        // VSTS 109337: Supress: Consider adding a security demand to ObjectCache.CreateObjectInstance(Assembly, String):Object.
        // The following call stack might expose a way to circumvent security protection.
        // $ISSUE 122248: Our parameters always come from a closed metadata definition system in Katmai. If this changes revisit.
        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
#if !NETCOREAPP
        [ReflectionPermission(SecurityAction.Assert, Unrestricted=true)]
#endif
        static Object CreateObjectInstance(Assembly assembly, string objectType)
        {
            Object o = SmoManagementUtil.CreateInstance(assembly, objectType);
            if( null == o )
            {
                throw new InternalEnumeratorException(SfcStrings.CouldNotInstantiateObj(objectType));
            }
            return o;
        }

        internal static Object CreateObjectInstance(string assemblyName, string objectType)
        {
#if SMOCODEGEN
            // Bingo: this little sucker lives with us already, no need to load it from an assembly
            return new SqlObject();
#else
            return CreateObjectInstance(LoadAssembly(assemblyName), objectType);
#endif
        }

#if DEBUG
        ///<summary>
        /// get debug cache summary
        ///</summary>
        static private string ShowCache()
        {
            String cacheInfo = "CacheInfo count: " + m_cache.Count.ToString();
            cacheInfo += " elements: ";
            foreach(CacheElement el in m_cache.Values)
            {
                cacheInfo += String.Format(CultureInfo.InvariantCulture, "key={0} usage={1} " , el.CacheKey.ToString(), el.Usage.ToString());
            }
            
            return cacheInfo;
        }
#endif
    }
}
            
