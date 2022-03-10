// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Reflection;
    using System.Xml;
    using System.Collections.Specialized;
    using Microsoft.SqlServer.Management.Common;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif



    class ObjectLoadInfo
    {
        public string Name;
        public string Assembly;
        public string InitData;
        public string ImplementClass;
        public uint UniqueKey;
        public Assembly AssemblyReference; // This is used when an extension registers manually
        public bool typeAllowsRecursion = false;

        public SortedList Children;

        public ObjectLoadInfo()
        {
            Children = new SortedList();
        }
    }

    internal class ObjectLoadInfoManager
    {
        static SortedList m_Hierarchy;
        static uint m_UniqueKey = 0;
        static Object lock_obj = new Object();

        static public ObjectLoadInfo GetObjectLoadInfo(Urn urn, object ci)
        {
            LoadHierarchy();
            StringCollection types = GetCanonicUrn(urn, ci);

            return GetObjectLoadInfo(types);
        }

        static public ObjectLoadInfo GetFirstObjectLoadInfo(Urn urn, object ci)
        {
            LoadHierarchy();
            StringCollection types = GetCanonicUrn(urn, ci);

            StringCollection strcol = new StringCollection();
            strcol.Add(types[0]);

            return GetObjectLoadInfo(strcol);
        }

        static public ArrayList GetAllObjectsLoadInfo(Urn urn, object ci)
        {
            LoadHierarchy();
            StringCollection types = GetCanonicUrn(urn, ci);

            return GetAllObjectsLoadInfo(types);
        }

        static private StringCollection GetCanonicUrn(Urn urn, object ci)
        {
            StringCollection types = new StringCollection();
            for(Urn u = urn; null != u; u  = u.Parent)
            {
                types.Insert(0, u.Type);
            }

            // handle the enumerator providers, if any, for the domain
            // types[0] = ProviderUrnPrefixFactory.GetProviderUrnPrefix(types[0], ci) + types[0];

            return types;
        }

        static private ObjectLoadInfo GetHierarchyRoot(string s)
        {
            ObjectLoadInfo oli = (ObjectLoadInfo)m_Hierarchy[s];
            if( null == oli )
            {
                throw new InvalidQueryExpressionEnumeratorException(SfcStrings.UrnCouldNotBeResolvedAtLevel(s));
            }
            return oli;
        }

        static private ObjectLoadInfo GetNextLevel(ObjectLoadInfo curent_oli, string s)
        {
            ObjectLoadInfo oli = (ObjectLoadInfo)(curent_oli.Children[s]);
            if( null == oli )
            {
                if (curent_oli.typeAllowsRecursion && (s == curent_oli.Name))
                {
                    return curent_oli;
                }
                throw new InvalidQueryExpressionEnumeratorException(SfcStrings.UrnCouldNotBeResolvedAtLevel(s));
            }
            return oli;
        }

        static private ObjectLoadInfo GetObjectLoadInfo(StringCollection types)
        {
            ObjectLoadInfo curent_oli = null;
            foreach(string s in types)
            {
                if( null == curent_oli )
                {
                    curent_oli = GetHierarchyRoot(s);
                }
                else
                {
                    curent_oli = GetNextLevel(curent_oli, s);
                }
            }
            return curent_oli;
        }

        static private ArrayList GetAllObjectsLoadInfo(StringCollection types)
        {
            ArrayList list = new ArrayList();
            ObjectLoadInfo curent_oli = null;
            foreach(string s in types)
            {
                if( null == curent_oli )
                {
                    curent_oli = GetHierarchyRoot(s);
                }
                else
                {
                    curent_oli = GetNextLevel(curent_oli, s);
                }
                list.Add(curent_oli);
            }
            return list;
        }

        static private void LoadHierarchy()
        {
            SmoManagementUtil.EnterMonitor(lock_obj);
            Stream fs = null;
            try
            {
                if( null != m_Hierarchy )
                {
                    return;
                }

                m_Hierarchy = new SortedList(System.StringComparer.Ordinal);

#if SMOCODEGEN
                string realFile = Path.Combine(Path.Combine(CodeGenSettings.Directory, "core\\src\\xml"), "Config.xml");
                if (!File.Exists(realFile))
                {
                    realFile = Path.Combine(Path.Combine(CodeGenSettings.Directory, "xml"), "Config.xml");
                }
                Console.WriteLine("CodeGenSettings realFile:{0}", realFile);
                using (fs = new FileStream(realFile, FileMode.Open,FileAccess.Read, FileShare.Read))
                {
#else

                using (fs = SmoManagementUtil.LoadResourceFromAssembly(SmoManagementUtil.GetExecutingAssembly(), "Config.xml"))
                {
                    if (fs == null)
                    {
                        Console.WriteLine("fs is null!");
                        Console.ReadLine();
                    }
#endif
#if NETSTANDARD2_0
                    XmlTextReader reader = new XmlTextReader(fs, new XmlReaderSettings {DtdProcessing = DtdProcessing.Prohibit});
#else
                    XmlTextReader reader = new XmlTextReader(fs){DtdProcessing = DtdProcessing.Prohibit};
#endif
                    reader.MoveToContent();
                    XmlUtility.SelectNextElement(reader);

                    LoadChildren(reader, reader.Depth, m_Hierarchy);
                }
            }
            finally
            {               
                SmoManagementUtil.ExitMonitor(lock_obj);
            }
        }

        static private bool LoadChildren(XmlTextReader reader, int nLevelDepth, SortedList list)
        {
            int nCurrentDepth = nLevelDepth;
            ObjectLoadInfo oli = null;
            for(;;)
            {
                nCurrentDepth = reader.Depth;

                if( nCurrentDepth < nLevelDepth )
                {
                    return true;
                }
                else if( nCurrentDepth > nLevelDepth )
                {
                    if( !LoadChildren(reader, nCurrentDepth, oli.Children) )
                    {
                        return false;
                    }
                }
                else //nCurrentDepth == nLevelDepth
                {
                    oli = Add(reader, list);

                    if( !XmlUtility.SelectNextElement(reader) )
                    {
                        return false;
                    }
                }
            }
        }

        static private ObjectLoadInfo Add(XmlTextReader reader, SortedList list)
        {
            ObjectLoadInfo oli = new ObjectLoadInfo();
            oli.Name = reader["type"];
            oli.Assembly = reader["assembly"];
            oli.InitData = reader["cfg"];
            oli.ImplementClass = reader["implement"];
            oli.UniqueKey = m_UniqueKey;
            m_UniqueKey += ObjectCache.SameObjectNumber;
            string allowsRecursion = reader["allow_recursion"];
            if (allowsRecursion != null)
            {
                oli.typeAllowsRecursion = Convert.ToBoolean(allowsRecursion);
            }

            list.Add(reader["type"], oli);

            return oli;
        }

        /// <summary>
        /// Adds an extension to the provided object urn. If the urn is null, then the object is a new extension root.
        /// </summary>
        /// <param name="urn">The Urn to extend. Pass in null for extension root.</param>
        /// <param name="name">Name of the type.</param>
        /// <param name="assembly">Assembly that contains the type specified with implementsType.</param>
        /// <param name="implementsType">The type that implements the enumerator level.</param>
        static public void AddExtension(Urn urn, string name, Assembly assembly, string implementsType)
        {
            ObjectLoadInfo oli = new ObjectLoadInfo();
            oli.Name = name;
            oli.Assembly = null; // We provide a strong reference to a loaded assembly with AssemblyReference
            oli.InitData = null; // Not needed right now.
            oli.ImplementClass = implementsType;
            oli.UniqueKey = m_UniqueKey;
            m_UniqueKey += ObjectCache.SameObjectNumber;
            oli.AssemblyReference = assembly;

            if (urn == null)
            {
                // First load the hierachy from Config.xml resource.
                LoadHierarchy();
                // Add the extension.
                m_Hierarchy.Add(name, oli);
            }
            else
            {
                ArrayList l = GetAllObjectsLoadInfo(urn, null);
                if (l.Count > 0)
                {
                    ((ObjectLoadInfo)l[l.Count - 1]).Children.Add(name, oli);
                }
            }
        }
    }


    class ProviderUrnPrefixFactory
    {
        /// <summary>
        ///    returns the urn prefix for the corresponding provider
        /// </summary>
        /// <param name="rootLevel">the root level of the urns, corresponds to the domain root</param>
        /// <param name="ci">the connection info</param>
        /// <returns> the provider specific urn prefix</returns>
        internal static string GetProviderUrnPrefix(string rootLevel, object ci)
        {
            if (HasProviders(rootLevel))
            {
                if (IsSqlConnection(ci))
                {
                    return SQL_PROVIDER_URN_PREFIX;
                }
#if TESTHOOKS
                if (IsTestXMLConnection(ci))
                {
                    return TEST_PROVIDER_URN_PREFIX;
                }
#endif
                //@TODO: handle other possible providers
                throw new InternalEnumeratorException(SfcStrings.InvalidConnectionType);
            }

            //default
            return string.Empty;
        }


        private const string SQL_PROVIDER_URN_PREFIX = "Sql";

#if TESTHOOKS
        private const string TEST_PROVIDER_URN_PREFIX = "Test";


        /// <summary>
        ///   returns true iff the ci corressponds to a connection to TestPRovider
        /// </summary>
        /// <param name="ci">the connection info</param>
        /// <returns></returns>
        private static bool IsTestXMLConnection(object ci)
        {
            if (ci.GetType().FullName.Equals("Microsoft.SqlServer.Test.XeventTestProvider.TestXMLConnection"))
            {                                 
                return true;
            }
            return false;
        }
#endif

        /// <summary>
        ///   returns true iff the ci corressponds to a connection to sql
        /// </summary>
        /// <param name="ci">the connection info</param>
        /// <returns></returns>
        private static bool IsSqlConnection(object ci)
        {
            if (ci is ServerConnection || ci is SqlConnectionInfoWithConnection || ci is SqlConnectionInfo || ci is SqlConnection || ci is SqlDirectConnection)
            {
                return true;
            }
            return false;
        }


        /// <summary>
        ///   returns true iff there are distinct enumerator providers for the domain
        /// </summary>
        /// <param name="rootLevel">the root level of the urns, corresponds to the domain root</param>
        /// <returns></returns>
        private static bool HasProviders(string rootLevel)
        {
            if (rootLevel == "XEStore")
            {
                return true;
            }

            return false;
        }


    }


}
            
