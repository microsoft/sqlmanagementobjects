// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// used as a parameter for the property access function
    /// </summary>
    internal enum PropertyAccessPurpose
    {
        Read,
        Write,
        Unknown
    }


    /// <summary>
    ///  Contains the static attributes of a property that do not change with every instance of a class
    /// </summary>
    [DebuggerDisplay("{Name} : {PropertyType}")]
    internal struct StaticMetadata
    {
        string m_name;
        Type m_propertyType;
        bool m_expensive;
        bool m_readonly;

        internal string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        internal bool Expensive
        {
            get { return m_expensive; }
            set { m_expensive = value; }
        }

        internal Type PropertyType
        {
            get { return m_propertyType; }
            set { m_propertyType = value; }
        }

        internal bool ReadOnly
        {
            get { return m_readonly; }
            set { m_readonly = value; }
        }


        internal bool IsEnumeration
        {
            get { return m_propertyType.GetIsEnum(); }
        }

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="expensive"></param>
        /// <param name="readOnly"></param>
        /// <param name="propertyType"></param>
        internal StaticMetadata(string name, bool expensive, bool readOnly, Type propertyType)
        {
            m_name = name;
            m_expensive = expensive;
            m_readonly = readOnly;
            m_propertyType = propertyType;
        }

        /// <summary>
        /// Basic constructor with defaults
        /// </summary>
        /// <param name="name"></param>
        internal StaticMetadata(string name) : this(name, expensive:false, readOnly:false, propertyType:typeof(Object))
        {
        }

        internal static readonly StaticMetadata Empty = new StaticMetadata(null, false, false, null);

        private bool CompareNameOnly(StaticMetadata p)
        {
            return (this.m_name == p.m_name);
        }

        public Predicate<StaticMetadata> Match
        {
            get
            {
                return CompareNameOnly;
            }
        }
    }



    /// <summary>
    /// Stores StaticMetadata and provides accessor functions
    /// </summary>
    internal abstract class PropertyMetadataProvider
    {
        /// <summary>
        /// Translates a property name to the index in the corresponding metadata array
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public abstract int PropertyNameToIDLookup(string propertyName);

        /// <summary>
        /// Gets the static metadata corresponding to the given index
        /// </summary>
        /// <param name="id">The index to re</param>
        /// <returns></returns>
        public abstract StaticMetadata GetStaticMetadata(int id);

        /// <summary>
        /// The number of properties this provider contains
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Gets the corresponding static metadata array for a given database Engine Type
        /// </summary>
        /// <param name="databaseEngineType"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <returns></returns>
        internal static StaticMetadata[] GetStaticMetadataArray(DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            return new StaticMetadata[] { };
        }

        /// <summary>
        /// Enum used to identify the index in the version array a particular version of
        /// the Standalone engine is at
        /// </summary>
        protected enum StandaloneVersionIndex
        {
            // VBUMP
            v70 = 0,
            v80 = 1,
            v90 = 2,
            v100 = 3,
            v105 = 4,
            v110 = 5,
            v120 = 6,
            v130 = 7,
            v140 = 8,
            v150 = 9,
            v160 = 10,
            v170 = 11,
            // VBUMP
        }

        /// <summary>
        /// Enum used to identify the index in the version array a particular version of
        /// the Azure engine is at
        /// </summary>
        protected enum CloudVersionIndex
        {
            v100 = 0,
            v110 = 1,
            v120 = 2
            // VBUMP - Note: Cloud versions have different versioning than on-prem
        }

        internal static int[] defaultSingletonArray = new int[] { 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>
        /// This method returns the version count array for a given database engine type
        /// </summary>
        /// <param name="databaseEngineType"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <returns></returns>
        internal static int[] GetVersionArray(DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            return defaultSingletonArray;
        }


        /// <summary>
        /// Translate from a property name to an index. Throws an exception if the name is unknown.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="pap"></param>
        /// <returns></returns>
        internal virtual int PropertyNameToIDLookupWithException(string propertyName, PropertyAccessPurpose pap)
        {
            int index = PropertyNameToIDLookup(propertyName);

            if (0 > index || index >= this.Count)
            {
                throw new UnknownPropertyException(propertyName);
            }
            return index;
        }

        /// <summary>
        /// Translate from a property name to an index. Throws an exception if the name is unknown.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        internal int PropertyNameToIDLookupWithException(string propertyName)
        {
            return PropertyNameToIDLookupWithException(propertyName, PropertyAccessPurpose.Unknown);
        }

        internal virtual bool TryPropertyNameToIDLookup(string propertyName, out int index)
        {
            index = PropertyNameToIDLookup(propertyName);
            if (index < 0)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// This method is used to check if the given property is valid in the given version and given database engine type
        ///  This is mainly used to verify if a property is valid in the target server scripting options.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <param name="version"></param>
        /// <param name="databaseEngineType"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <returns></returns>
        internal static bool CheckPropertyValid(Type type,string propertyName, ServerVersion version, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            MethodInfo mi = type.GetMethod("GetStaticMetadataArray",
                 BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            StaticMetadata[] properties = { };
            if (mi != null)
            {
                properties = mi.Invoke(null, new object[] { databaseEngineType, databaseEngineEdition }) as StaticMetadata[];
            }
            int index = Array.FindIndex<StaticMetadata>(properties, (new StaticMetadata(propertyName)).Match);

            //GetVersionArray returns the array of version counts for a given database engine type of an object from the generated file.
            mi = type.GetMethod("GetVersionArray",
                 BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            int[] versionArray = { };
            if (mi != null)
            {
                versionArray = mi.Invoke(null, new object[] { databaseEngineType, databaseEngineEdition}) as int[];
            }
            int versionIndex = GetCurrentVersionIndex(version, databaseEngineType, databaseEngineEdition);
            if (0 > index || versionArray.Length < versionIndex || index >= versionArray[versionIndex])
            {
                return false;
            }
            return true;
        }

        internal static int GetCurrentVersionIndex(ServerVersion sv, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            int versionIndex = 0;
            if (databaseEngineType == DatabaseEngineType.Standalone)
            {
                switch (sv.Major)
                {case 10:
                        {
                            if (sv.Minor == 0)
                            {
                                versionIndex = (int)StandaloneVersionIndex.v100;
                            }
                            else if (sv.Minor == 50)
                            {
                                versionIndex = (int)StandaloneVersionIndex.v105;
                            }
                            break;
                        }
                    case 11:
                        {
                            versionIndex = (int)StandaloneVersionIndex.v110;
                            break;
                        }
                    case 12:
                        {
                            versionIndex = (int)StandaloneVersionIndex.v120;
                            break;
                        }
                    case 13:
                        {
                            versionIndex = (int) StandaloneVersionIndex.v130;
                            break;
                        }
                    case 14:
                        {
                            versionIndex = (int) StandaloneVersionIndex.v140;
                            break;
                        }
                    case 15:
                        {
                            versionIndex = (int)StandaloneVersionIndex.v150;
                            break;
                        }
                    case 16:
                        {
                            versionIndex = (int)StandaloneVersionIndex.v160;
                            break;
                        }
                    case 17:
                        {
                            versionIndex = (int)StandaloneVersionIndex.v170;
                            break;
                        }
                    // VBUMP
                    //Forward Compatibility: An older version SSMS/Smo connecting to a future version sql server database engine.
                    //That is why if the server version is unknown, we need to set it according to the latest database engine available,
                    //so that all Latest-Version-Supported-Features in the Tools work seamlessly for the unknown future version database engines too.
                    default:
                        versionIndex = (int)StandaloneVersionIndex.v170;
                        break;
                }
            }
            else if(databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                if (databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
                {
                    //Data Warehouse only has one "version" so we don't need to actually do a lookup
                    versionIndex = 0;
                }
                else
                {
                    //Not data warehouse so default to normal cloud properties
                    switch (sv.Major)
                    {
                        case 10:
                        {
                            versionIndex = (int)CloudVersionIndex.v100;
                            break;
                        }
                        case 11:
                        {
                            versionIndex = (int)CloudVersionIndex.v110;
                            break;
                        }
                        case 12:
                        {
                            versionIndex = (int)CloudVersionIndex.v120;
                            break;
                        }
                        // VBUMP
                        default:
                        {
                            //Default to the latest known version for unknown versions since it's
                            //likely an older SSMS connecting to a new Azure version and thus we
                            //want all current features enabled
                            versionIndex = (int)CloudVersionIndex.v120;
                            break;
                        }
                    }
                }
            }
            else
            { //Unknown DatabaseEngineType, just return the default value in this case since it's not a critical issue
                Debug.Assert(false, string.Format(CultureInfo.InvariantCulture, "Unknown DatabaseEngineType {0} when getting current version index", databaseEngineType.ToString()));
            }

            return versionIndex;
        }

    }

    /// <summary>
    /// Extends PropertyMetadataProvider with version support
    /// We consider the organization of properties as follows:
    ///  from indexes 0...n1 supported on 7/8/9
    ///  from indexes n1...n2 supported on 8/9
    ///  from indexes n2...n3 supported on 9
    /// This implies that there are no properties that are supported on a down-level and not on a up-level server.
    /// </summary>
    internal abstract class SqlPropertyMetadataProvider : PropertyMetadataProvider
    {

        /// <summary>
        /// The index of the current version
        /// </summary>
        protected int currentVersionIndex = 0;
        protected DatabaseEngineType databaseEngineType = DatabaseEngineType.Standalone;

        protected DatabaseEngineType DatabaseEngineType
        {
            get
            {
                return databaseEngineType;
            }
        }

        protected DatabaseEngineEdition databaseEngineEdition = DatabaseEngineEdition.Unknown;

        protected DatabaseEngineEdition DatabaseEngineEdition
        {
            get
            {
                return databaseEngineEdition;
            }
        }

        /// <summary>
        /// Provider initialized with a specific version, engine type and engine edition
        /// </summary>
        /// <param name="sv"></param>
        /// <param name="databaseEngineType"></param>
        /// <param name="databaseEngineEdition"></param>
        public SqlPropertyMetadataProvider(ServerVersion sv, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            this.databaseEngineType = databaseEngineType;
            this.currentVersionIndex = GetCurrentVersionIndex(sv, databaseEngineType, databaseEngineEdition);
            this.databaseEngineEdition = databaseEngineEdition;
        }




        /// <summary>
        /// Translate from a property name to an index. Throws an exception if the name is unknown.
        /// Takes into consideration version information, throwing an exception if the property isn't
        /// defined for this version.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="pap"></param>
        /// <returns></returns>
        internal override int PropertyNameToIDLookupWithException(string propertyName, PropertyAccessPurpose pap)
        {
            int index = PropertyNameToIDLookup(propertyName);

            if (0 > index || index >= this.VersionCount[this.VersionCount.Length - 1])
            {
                throw new UnknownPropertyException(propertyName);
            }

            if (index >= this.VersionCount[currentVersionIndex])
            {
                throw new UnknownPropertyException(propertyName, GetExceptionText(propertyName, pap));
            }

            return index;
        }


        internal override bool TryPropertyNameToIDLookup(string propertyName, out int index)
        {
            index = PropertyNameToIDLookup(propertyName);

            if (0 > index || index >= this.VersionCount[this.VersionCount.Length - 1])
            {
                return false;
            }

            if (index >= this.VersionCount[currentVersionIndex])
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// The exception text should be:
        ///     Cannot read property ?{0}.IsSparse?. This property is not available on {1}.
        ///     Cannot write property ?{0}.IsSparse?. This property is not available on {1}.
        ///     {0} = class (as there are 2 classes that have IsSparse)
        ///     {1} = ?SQL Server 7.0? or ?SQL Server 2000? or ?SQL Server 2005?
        ///     Of course, if a property is Unknown (through indexer/getter) then it still be the same old 'Unknown' error message.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="pap"></param>
        /// <returns></returns>
        protected string GetExceptionText(string propertyName, PropertyAccessPurpose pap)
        {
            string text = "";

            switch (pap)
            {
                case PropertyAccessPurpose.Read:
                    text = ExceptionTemplates.CannotReadProperty(propertyName);
                    break;
                case PropertyAccessPurpose.Write:
                    text = ExceptionTemplates.CannotWriteProperty(propertyName);
                    break;
                default:
                    text = ExceptionTemplates.CannotAccessProperty(propertyName);
                    break;
            }

            //Append on the server version to the error text
            string versionName = GetServerNameFromVersionIndex(currentVersionIndex);
            if(string.IsNullOrEmpty(versionName) == false)
            {
                text = string.Format(CultureInfo.CurrentUICulture, "{0} {1} {2}.",
                        text,
                        ExceptionTemplates.PropertyAvailable,
                        versionName);
            }

            return text;
        }

        /// <summary>
        /// Get the supported versions as ordinals
        /// </summary>
        protected abstract int[] VersionCount { get; }

        /// <summary>
        /// Gets the name of the server associated with a particular version index. This is based off of the
        /// current DatabaseEngineType.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The server version name, or an empty string if an invalid index is specified</returns>
        private string GetServerNameFromVersionIndex(int index)
        {
            string versionName = string.Empty;

            if (this.databaseEngineType == DatabaseEngineType.Standalone)
            {
                switch (index)
                {
                    case (int)StandaloneVersionIndex.v70:
                    {
                        versionName = LocalizableResources.ServerSphinx;
                        break;
                    }
                    case (int)StandaloneVersionIndex.v80:
                    {
                        versionName = LocalizableResources.ServerShiloh;
                        break;
                    }
                    case (int)StandaloneVersionIndex.v90:
                    {
                        versionName = LocalizableResources.ServerYukon;
                        break;
                    }
                    case (int)StandaloneVersionIndex.v100:
                    {
                        versionName = LocalizableResources.ServerKatmai;
                        break;
                    }
                    case (int)StandaloneVersionIndex.v105:
                    {
                        versionName = LocalizableResources.ServerKilimanjaro;
                        break;
                    }
                    case (int)StandaloneVersionIndex.v110:
                    {
                        versionName = LocalizableResources.ServerDenali;
                        break;
                    }
                    case (int)StandaloneVersionIndex.v120:
                    {
                        versionName = LocalizableResources.ServerSQL14;
                        break;
                    }
                    case (int)StandaloneVersionIndex.v130:
                    {
                        versionName = LocalizableResources.ServerSQL15;
                        break;
                    }
                    case (int)StandaloneVersionIndex.v140:
                    {
                        versionName = LocalizableResources.ServerSQL2017;
                        break;
                    }
                    case (int)StandaloneVersionIndex.v150:
                        {
                            versionName = LocalizableResources.ServerSQLv150;
                            break;
                        }
                    case (int)StandaloneVersionIndex.v160:
                        {
                            versionName = LocalizableResources.ServerSQLv160;
                            break;
                        }
                    case (int)StandaloneVersionIndex.v170:
                        {
                            versionName = LocalizableResources.ServerSQLv170;
                            break;
                        }
                    default:
                    // VBUMP
                    { //Index is unknown, leave as default value but log the error since it shouldn't happen
                        Debug.Assert(false, string.Format(CultureInfo.InvariantCulture, "Unknown server version index {0}", index));
                        break;
                    }
                }
            }
            else if (this.databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                if (this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
                {
                    versionName = LocalizableResources.EngineDatawarehouse;
                }
                else
                {
                    //For all non-Data warehouse editions default to plain cloud name
                    versionName = LocalizableResources.EngineCloud;
                }
            }
            else
            { //Just leave as default version but log the error since this shouldn't happen normally
                Debug.Assert(false, string.Format(CultureInfo.InvariantCulture, "Unknown DatabaseEngineType {0}", this.databaseEngineType));
            }

            return versionName;
        }


        /// <summary>
        /// Returns the supported versions for a property as a SqlServerVersions flag enum
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private SqlServerVersions GetSupportedVersions(int index)
        {
            //check out of range
            Debug.Assert(index >= 0 && index < this.VersionCount[this.VersionCount.Length - 1]
                                        , "SuportedVersions: index out of range");
            if (index < 0 || index >= this.VersionCount[this.VersionCount.Length - 1])
            {
                return SqlServerVersions.Unknown;
            }

            SqlServerVersions ssv = SqlServerVersions.Version150;
            if (index >= this.VersionCount[8])
            {
                return ssv;
            }
            // VBUMP

            ssv |= SqlServerVersions.Version140;
            if (index >= this.VersionCount[7])
            {
                return ssv;
            }

            ssv |= SqlServerVersions.Version130;
            if (index >= this.VersionCount[6])
            {
                return ssv;
            }

            ssv |= SqlServerVersions.Version120;
            if (index >= this.VersionCount[5])
            {
                return ssv;
            }

            ssv |= SqlServerVersions.Version110;
            if (index >= this.VersionCount[4])
            {
                return ssv;
            }

            ssv |= SqlServerVersions.Version105;
            if (index >= this.VersionCount[3])
            {
                return ssv;
            }

            ssv |= SqlServerVersions.Version100;
            if (index >= this.VersionCount[2])
            {
                return ssv;
            }

            ssv |= SqlServerVersions.Version90;
            if (index >= this.VersionCount[1])
            {
                return ssv;
            }

            ssv |= SqlServerVersions.Version80;
            if (index >= this.VersionCount[0])
            {
                return ssv;
            }

            ssv |= SqlServerVersions.Version70;

            return ssv;
        }

        /// <summary>
        /// Get a SqlPropertyInfo for the property with the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal SqlPropertyInfo GetPropertyInfo(int index)
        {
            return new SqlPropertyInfo(this.GetStaticMetadata(index), this.GetSupportedVersions(index));
        }

        /// <summary>
        /// Get the count of properties supported for the given versions
        /// </summary>
        /// <param name="m_versions"></param>
        /// <returns></returns>
        private int GetCountForVersions(SqlServerVersions m_versions)
        {
            // VBUMP
            if ((m_versions | SqlServerVersions.Version150) == SqlServerVersions.Version150)
            {
                return this.VersionCount[9];
            }
            if ((m_versions | SqlServerVersions.Version140) == SqlServerVersions.Version140)
            {
                return this.VersionCount[8];
            }
            if ((m_versions | SqlServerVersions.Version130) == SqlServerVersions.Version130)
            {
                return this.VersionCount[7];
            }
            if ((m_versions | SqlServerVersions.Version120) == SqlServerVersions.Version120)
            {
                return this.VersionCount[6];
            }
            if ((m_versions | SqlServerVersions.Version110) == SqlServerVersions.Version110)
            {
                return this.VersionCount[5];
            }
            if ((m_versions | SqlServerVersions.Version105) == SqlServerVersions.Version105)
            {
                return this.VersionCount[4];
            }
            if ((m_versions | SqlServerVersions.Version100) == SqlServerVersions.Version100)
            {
                return this.VersionCount[3];
            }
            if ((m_versions | SqlServerVersions.Version90) == SqlServerVersions.Version90)
            {
                return this.VersionCount[2];
            }
            if ((m_versions | SqlServerVersions.Version80) == SqlServerVersions.Version80)
            {
                return this.VersionCount[1];
            }
            if ((m_versions | SqlServerVersions.Version70) == SqlServerVersions.Version70)
            {
                return this.VersionCount[0];
            }
            return 0;
        }

        /// <summary>
        /// Get SqlPropertyInfo for every property supported on the given versions
        /// </summary>
        /// <param name="versions"></param>
        /// <returns></returns>
        internal SqlPropertyInfo[] EnumPropertyInfo(SqlServerVersions versions)
        {
            int count = GetCountForVersions(versions);
            SqlPropertyInfo[] list = new SqlPropertyInfo[count];

            for (int i = 0; i < count; i++)
            {
                list[i] = GetPropertyInfo(i);
            }
            return list;
        }
    }

    /// <summary>
    /// Extends PropertyMetadataProvider to support a dynamic list of properties
    /// </summary>
    internal class DynamicPropertyMetadataProvider : PropertyMetadataProvider
    {
        SortedList<string, StaticMetadata> m_listData = new SortedList<string, StaticMetadata>(System.StringComparer.Ordinal);

        /// <summary>
        /// Get property index from a property name name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public override int PropertyNameToIDLookup(string propertyName)
        {
            return m_listData.IndexOfKey(propertyName);
        }

        /// <summary>
        /// Get the number of properties in this provider
        /// </summary>
        public override int Count
        {
            get { return m_listData.Count; }
        }

        /// <summary>
        /// Get StaticMetadata fro the property with the given index
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override StaticMetadata GetStaticMetadata(int id)
        {
            return m_listData.Values[id];
        }

        /// <summary>
        /// Add a new property with the given attributes
        /// </summary>
        /// <param name="name"></param>
        /// <param name="readOnly"></param>
        /// <param name="type"></param>
        public void AddMetadata(string name, bool readOnly, Type type)
        {
            m_listData.Add(name, new StaticMetadata(name, false, readOnly, type));
        }
    }


    /// <summary>
    ///  Class encapsulating metadata and version info about a sql property.
    /// </summary>
    public class SqlPropertyInfo
    {
        StaticMetadata m_sm;
        SqlServerVersions m_versions;

        internal SqlPropertyInfo(StaticMetadata sm, SqlServerVersions versions)
        {
            m_sm = sm;
            m_versions = versions;
        }

        public string Name
        {
            get { return m_sm.Name; }
        }

        public bool IsWriteable
        {
            get { return m_sm.ReadOnly; }
        }

        public bool IsExpensive
        {
            get { return m_sm.Expensive; }
        }

        public System.Type PropertyType
        {
            get { return m_sm.PropertyType; }
        }

        /// <summary>
        /// The versions this property is supported on
        /// </summary>
        public SqlServerVersions Versions
        {
            get { return m_versions; }
        }
    }

    internal static class MetadataProviderLookup
    {
        private static readonly Dictionary<Type, Type> providerLookup = new Dictionary<Type, Type>();
        static MetadataProviderLookup()
        {
            var smoAssembly = Assembly.GetExecutingAssembly();
            UpdateProviderLookup(smoAssembly);
        }

        private static void UpdateProviderLookup(Assembly assembly)
        {
            // There's no lock because it's ok for threads to race here. The static constructor covers the vast majority of types anyway.
            foreach (var providerType in assembly.GetTypes().Where(t => typeof(SqlPropertyMetadataProvider).IsAssignableFrom(t)))
            {
                if (typeof(SqlSmoObject).IsAssignableFrom(providerType.DeclaringType))
                {
                    providerLookup[providerType.DeclaringType] = providerType;
                }
            }
        }

        public static Type GetPropertyMetadataProviderType<T>() where T: SqlSmoObject
        {
            return GetPropertyMetadataProviderType(typeof(T));
        }

        public static Type GetPropertyMetadataProviderType(Type t)
        {
            if (!providerLookup.ContainsKey(t))
            {
                UpdateProviderLookup(t.Assembly);
            }
            if (providerLookup.ContainsKey(t))
            {
                return providerLookup[t];
            }
            throw new ArgumentException(ExceptionTemplates.InvalidTypeForMetadataProvider(t.Name));
        }

    }
}
