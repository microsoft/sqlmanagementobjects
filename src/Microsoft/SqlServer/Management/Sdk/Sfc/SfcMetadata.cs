// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using Microsoft.SqlServer.Server;

namespace Microsoft.SqlServer.Management.Sdk.Sfc.Metadata
{
    /// <summary>
    /// Specifies the cardinality of the type in relationship to its parent
    /// </summary>
    public enum SfcCardinality
    {
        None = 0,
        /// <summary>
        /// Always one reference (default)
        /// </summary>
        One,
        /// <summary>
        /// The reference may be null
        /// </summary>
        ZeroToOne,
        /// <summary>
        /// Zero to any (typical for SFC/SMO collections)
        /// </summary>
        ZeroToAny,
        /// <summary>
        /// At least one to any (rare, but it happens, such as (Table.)Columns.Column)
        /// </summary>
        OneToAny
    }

    public enum SfcRelationship
    {
        /// <summary>
        /// No relationship
        /// </summary>
        None,
        /// <summary>
        /// Standalone objects (such as SMO Server, Database, Table, and regular (value type and other) properties)
        /// </summary>
        Object,
        /// <summary>
        /// A container that contains independent objects (such as SMO Server.Databases)
        /// </summary>
        ObjectContainer,
        /// <summary>
        /// A child object (such as SMO Server.Information, or Columns.Column) 
        /// </summary>
        ChildObject,
        /// <summary>
        /// A container that contains children (such as SMO Table.Columns)
        /// </summary>
        ChildContainer,
        /// <summary>
        /// A back reference to the parent
        /// </summary>
        ParentObject,
        /// <summary>
        /// Ignore this object, not a part of SFC type system
        /// </summary>
        Ignore,
    }

    [Flags]
    internal enum SfcElementFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0,
        /// <summary>
        /// ElementType is supported in Standalone database engine type.
        /// </summary>
        Standalone = 0x0010,
        /// <summary>
        /// ElementType is supported in SqlAzure database engine type.
        /// </summary>
        SqlAzureDatabase = 0x0020,
    }

    [Flags]
    public enum SfcPropertyFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0,
        /// <summary>
        /// Property is required to have a value to create or alter the object.
        /// </summary>
        Required = 0x0010,
        /// <summary>
        /// Property is expensive to retrieve.
        /// Non-expensive properties marked in the Enumerator are usually retrieved on initial access to an object.
        /// The model and Enumerator are independently specifies but should agree.
        /// </summary>
        Expensive = 0x0020,
        /// <summary>
        /// Property is computed. This implies that the property is not Required.
        /// </summary>
        Computed = 0x0040,
        /// <summary>
        /// Property contains encrypted data.
        /// </summary>
        Encrypted = 0x0080,
        /// <summary>
        /// Proprety cannot be set after the instance has been persisted.
        /// </summary>
        ReadOnlyAfterCreation = 0x0100,
        /// <summary>
        /// Property contains data (such as CreateDate). It is never set, directly or indirectly.
        /// This implies that the property is not Required.
        /// </summary>
        Data = 0x0200,
        /// <summary>
        /// Property is applicable to the SQL Server standalone database engine model.
        /// </summary>
        Standalone = 0x0400,
        /// <summary>
        /// Property is applicable to the Microsoft Azure SQL Database engine model.
        /// </summary>
        SqlAzureDatabase = 0x0800, 

        /// <summary>
        /// Property is relevant for Design purposes. 
        /// A property with this flag will be processed by the Differentiator service.
        /// </summary>
        Design = 0x1000,
        /// <summary>
        /// Property is relevant for Deploy purposes. 
        /// A property with this flag will be processed by the Differentiator service.
        /// </summary>
        Deploy = 0x2000,
    }

    [Flags]
    public enum SfcObjectFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0,
        /// <summary>
        /// The NaturalOrder Flag indicates whether the ordering of the children in a 
        /// parent-child relationship is important (true) or not (false). 
        /// 
        /// The flag affects the behaviour of Differentiator. If it is true, then when 
        /// the order of a child in one graph differs from that of another graph, the 
        /// child object will appear as a “remove” entry from its original position, and 
        /// as an “add” to the new position.
        /// </summary>
        NaturalOrder = 0x0010,
        /// <summary>
        /// Property is relevant for Design purpose. 
        ///
        /// The flag affects the behaviour of Differentiator. Property with this flag
        /// will processed by the Differentiator.
        /// </summary>
        Design = 0x0020,
        /// <summary>
        /// Property is relevant for Deploy purpose. 
        ///
        /// The flag affects the behaviour of Differentiator. Property with this flag
        /// will processed by the Differentiator.
        /// </summary>
        Deploy = 0x0040,
    }

    /// <summary>
    /// Interface for resolving from a source to a single target.
    /// </summary>
    public interface ISfcReferenceResolver
    {
        object Resolve(object instance, object[] args);
    }

    /// <summary>
    /// Generic interface for resolving from a source to a single target.
    /// </summary>
    /// <typeparam name="S">The type of the source object to resolve from.</typeparam>
    /// <typeparam name="T">The type of the target object.</typeparam>
    public interface ISfcReferenceResolver<T, S> : ISfcReferenceResolver
    {
        T Resolve(S instance, object[] args);
    }

    /// <summary>
    /// Interface for resolving from a source to an enumerable target.
    /// </summary>
    public interface ISfcReferenceCollectionResolver
    {
        IEnumerable ResolveCollection(object instance, object[] args);
    }

    /// <summary>
    /// Generic interface for resolving from a source to an enumerable target.
    /// </summary>
    /// <typeparam name="S">The type of the source object to resolve from.</typeparam>
    /// <typeparam name="T">The element type of the target enumeration.</typeparam>
    public interface ISfcReferenceCollectionResolver<T, S> : ISfcReferenceCollectionResolver
    {
        IEnumerable<T> ResolveCollection(S instance, object[] args);
    }


    public class SfcMetadataDiscovery
    {
        private static Dictionary<Type, List<SfcMetadataRelation>> typesRelationsCache = new Dictionary<Type, List<SfcMetadataRelation>>();
        private static Dictionary<Type, List<SfcMetadataRelation>> typesKeysCache = new Dictionary<Type, List<SfcMetadataRelation>>();
        private static Dictionary<RuntimeTypeHandle, List<SfcMetadataRelation>> typesPropertiesCache = new Dictionary<RuntimeTypeHandle, List<SfcMetadataRelation>>();
        private static Dictionary<Type, List<Type>> typesReferencesCache = new Dictionary<Type, List<Type>>();
        private static Dictionary<TypeHandlePropertyNameKey, PropertyInfo> typePropertyInfosCache = new Dictionary<TypeHandlePropertyNameKey, PropertyInfo>();
        private static Dictionary<SfcMetadataRelation, List<string>> relationViewNamesCache = new Dictionary<SfcMetadataRelation, List<string>>();
 
        private static bool internalGraphBuilt = false;

        private Type m_type;                        // Reference to the actual type
        private AttributeCollection m_typeAttributes = null;

        /// <summary>
        /// This is a key used in the TypePopertyInfo cache and contains the type handle and the property Name
        /// </summary>
        internal struct TypeHandlePropertyNameKey
        {
            readonly RuntimeTypeHandle typeHandle;
            readonly string propertyName;

            internal TypeHandlePropertyNameKey(string propertyName, RuntimeTypeHandle typeHandle)
            {
                TraceHelper.Assert(propertyName != null, "PropertyName can't be null in TypeHandlePropertyNameKey");
                TraceHelper.Assert(typeHandle != null, "TypeHandle can't be null in TypeHandlePropertyNameKey");

                this.propertyName = propertyName;
                this.typeHandle = typeHandle;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is TypeHandlePropertyNameKey))
                {
                    return false;
                }

                return Equals((TypeHandlePropertyNameKey) obj);
            }

            public bool Equals(TypeHandlePropertyNameKey obj)
            {
                return (obj.typeHandle.Equals(typeHandle) && (obj.propertyName.Equals(propertyName, StringComparison.Ordinal)));
            }

            public override int GetHashCode()
            {
                return typeHandle.GetHashCode() ^ propertyName.GetHashCode();
            }

            public override string ToString()
            {
                return propertyName + "," + typeHandle.ToString();
            }
        }

        /// <summary>
        /// Specifies the type of this node.
        /// </summary>
        public Type Type
        {
            get
            {
                return m_type;
            }
        }

        /// <summary>
        /// Returns the element type name, if it is different from the type name.
        /// This element type name is used in a URN.
        /// </summary>
        public string ElementTypeName
        {
            get
            {
                SfcElementTypeAttribute[] attrs = (SfcElementTypeAttribute[])m_type.GetCustomAttributes(typeof(SfcElementTypeAttribute), false);
                if (attrs.Length == 0)
                {
                    return m_type.Name;
                }

                return attrs[0].ElementTypeName;
            }
        }

        /// <summary>
        /// Indicates whether the type should be browseable -- used for UI such as Powershell
        /// </summary>
        public bool IsBrowsable
        {
            get
            {
                // First see if there are attributes overriding the calculated behavior
                SfcBrowsableAttribute[] attrs = (SfcBrowsableAttribute[])m_type.GetCustomAttributes(typeof(SfcBrowsableAttribute), false);

                if (attrs.Length > 0)
                {
                    return attrs[0].IsBrowsable;
                }

                bool browsable = true; // browseable by default

                foreach (Type t in this.ReferredBy)
                {
                    // This is somewhat of a problem. Browseable is now inferred through the relationship
                    // the object has with its parent. If we have multiple parents, then the object itself
                    // must be annotated.
                    // Chances are that if one relationship is not browsable, the other should not.
                    // It will be very uncommon that it is different for different parent objects.
                    // Exceptions to this must be annotated with attributes.
                    SfcMetadataDiscovery disc = new SfcMetadataDiscovery(t);
                    foreach (SfcMetadataRelation rel in disc.Relations)
                    {
                        if (rel.Relationship == SfcRelationship.ParentObject ||
                            rel.Relationship == SfcRelationship.Ignore ||
                            rel.Relationship == SfcRelationship.None)
                        {
                            continue;
                        }
                        if (rel.Type == this.Type)
                        {
                            if (!rel.IsBrowsable)
                            {
                                browsable = false;
                                break;
                            }
                        }
                    }
                }

                return browsable;
            }
        }

        public AttributeCollection TypeAttributes
        {
            get
            {
                if (m_typeAttributes == null)
                {
                    object[] attibutes = m_type.GetCustomAttributes(true);
                    List<Attribute> list = new List<Attribute>();
                    foreach (object o in attibutes)
                    {
                        if (o is SfcSkuAttribute)
                        {
                            list.Add(o as SfcSkuAttribute);
                        }
                        else if (o is SfcVersionAttribute)
                        {
                            list.Add(o as SfcVersionAttribute);
                        }
                        else if (o is SfcElementTypeAttribute)
                        {
                            list.Add(o as SfcElementTypeAttribute);
                        }
                    }
                    if (list.Count == 0)
                    {
                        m_typeAttributes = new AttributeCollection();
                    }
                    else
                    {
                        Attribute[] temp = new Attribute[list.Count];
                        list.CopyTo(temp);
                        m_typeAttributes = new AttributeCollection(temp);
                    }
                }
                return m_typeAttributes;
            }
        }

        /// <summary>
        /// Create an instance of the SfcMetadataDiscovery class
        /// </summary>
        /// <param name="type">An SFC type</param>
        public SfcMetadataDiscovery(Type type)
        {
            //Type can't be null
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            m_type = type;
        }

        /// <summary>
        /// Returns the list of properties this object contains
        /// </summary>
        public virtual List<SfcMetadataRelation> Relations
        {
            get
            {
                List<SfcMetadataRelation> copyList = SfcMetadataDiscovery.GetTypeRelations(this.Type);
                SfcMetadataRelation[] dataCopy = new SfcMetadataRelation[copyList.Count];
                copyList.CopyTo(dataCopy);
                return new List<SfcMetadataRelation>(dataCopy);
            }
        }

        /// <summary>
        /// Returns a readonly list of properties this object contains.
        /// This is much faster than the public Relations properties since it avoids copying each element to a new list.
        /// </summary>
        internal ReadOnlyCollection<SfcMetadataRelation> ReadOnlyCollectionRelations
        {
            get
            {
                return SfcMetadataDiscovery.GetTypeRelations(this.Type).AsReadOnly();
            }
        }

        internal List<SfcMetadataRelation> InternalStorageSupported
        {
            get
            {
                List<SfcMetadataRelation> temp = new List<SfcMetadataRelation>();
                foreach (SfcMetadataRelation relation in this.Relations)
                {
                    if (relation.IsSfcProperty)
                    {
                        temp.Add(relation);
                    }
                }
                return temp;
            }
        }

        internal int InternalStorageSupportedCount
        {
            get
            {
                int count = 0;
                foreach (SfcMetadataRelation relation in this.Relations)
                {
                    if (relation.IsSfcProperty)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public virtual List<Type> ReferredBy
        {
            get
            {
                lock (SfcMetadataDiscovery.typesReferencesCache)
                {
                    List<Type> copyList = new List<Type>();
                    //If the cache is created, search for a match on it.
                    if (SfcMetadataDiscovery.typesReferencesCache.ContainsKey(this.Type))
                    {
                        //Dont give a reference to the internal cache
                        List<Type> result = SfcMetadataDiscovery.typesReferencesCache[this.Type];
                        Type[] dataCopy = new Type[result.Count];
                        result.CopyTo(dataCopy);
                        return new List<Type>(dataCopy);
                    }
                    InternalBuildRelationsGraph(this.Type);
                    SfcMetadataDiscovery.internalGraphBuilt = true;
                    if (SfcMetadataDiscovery.typesReferencesCache.ContainsKey(this.Type))
                    {
                        List<Type> result = SfcMetadataDiscovery.typesReferencesCache[this.Type];
                        Type[] dataCopy = new Type[result.Count];
                        result.CopyTo(dataCopy);
                        return new List<Type>(dataCopy);
                    }
                    else
                    {
                        return new List<Type>();
                    }
                }
            }
        }

        internal static List<SfcMetadataRelation> GetTypeRelations(Type sfcType)
        {
            List<SfcMetadataRelation> typeRelations = null;
            //First try to get the collection from the cache to preserve doing reflection again.
            if (!SfcMetadataDiscovery.typesRelationsCache.TryGetValue(sfcType, out typeRelations))
            {
                lock (SfcMetadataDiscovery.typesRelationsCache)
                {
                    if (!SfcMetadataDiscovery.typesRelationsCache.TryGetValue(sfcType, out typeRelations))
                    {
                        //Now load everything using reflection and add it into cache
                        return InternalLoadTypeRelationsInformationThroughReflection(sfcType);
                    }
                }
            }

            TraceHelper.Assert(typeRelations != null, "TypeRelations list can't be null");
            return typeRelations;
        }

        internal static List<SfcMetadataRelation> GetTypeKeys(Type sfcType)
        {
            List<SfcMetadataRelation> typeKeys = null;
            //First try to get the collection from the cache to preserve doing reflection again.
            if (!SfcMetadataDiscovery.typesKeysCache.TryGetValue(sfcType, out typeKeys))
            {
                lock (SfcMetadataDiscovery.typesKeysCache)
                {
                    //First try to get the collection from the cache to preserve doing reflection again.
                    if (!SfcMetadataDiscovery.typesKeysCache.TryGetValue(sfcType, out typeKeys))
                    {
                        //Now load everything using reflection and add it into cache
                        return InternalLoadTypeKeysInformationThroughReflection(sfcType);
                    }
                }
            }

            TraceHelper.Assert(typeKeys != null, "TypeKeys list can't be null");
            return typeKeys;
        }

        private static void InternalBuildRelationsGraph(Type type)
        {
            if (internalGraphBuilt)
            {
                return;
            }
            Type parentType = GetParentType(type);
            SfcMetadataDiscovery parent = new SfcMetadataDiscovery(parentType);
            //Parent type always have an empty list of references
            SfcMetadataDiscovery.typesReferencesCache[parentType] = new List<Type>();
            InternalBuildRelationsGraphRecursive(parent);
        }

        private static void InternalBuildRelationsGraphRecursive(SfcMetadataDiscovery metadataType)
        {
            List<SfcMetadataRelation> relations = metadataType.Relations;
            foreach (SfcMetadataRelation relation in relations)
            {
                //Check if already exists, to prevent duplicates and infinite loops.
                if (typesReferencesCache.ContainsKey(relation.Type))
                {
                    List<Type> checkAlreadyExist = typesReferencesCache[relation.Type];
                    if (checkAlreadyExist.Contains(metadataType.Type))
                    {
                        continue;
                    }
                }
                if (relation.Relationship == SfcRelationship.None || relation.Relationship == SfcRelationship.ParentObject)
                {
                    continue;
                }

                List<Type> references = null;
                if (typesReferencesCache.ContainsKey(relation.Type))
                {
                    references = typesReferencesCache[relation.Type];
                }
                else
                {
                    references = new List<Type>();
                }
                references.Add(metadataType.Type);
                typesReferencesCache[relation.Type] = references;
                InternalBuildRelationsGraphRecursive(relation);
            }
        }

        /// <summary>
        /// Temporary function, will be replaced by a call to domain registration
        /// </summary>
        private static Type GetParentType(Type childType)
        {
            return SfcRegistration.GetRegisteredDomainForType(childType).RootType;
        }

        /// <summary>
        /// The list of valid parent Types which an instance of this child type may have as its parent.
        /// Usually there is a single Type that can be the parent, but sometimes there are more.
        /// </summary>
        /// <returns>null if there is no parent for the type, otherwise the list of possible parent Types</returns>
        /// <param name="childType"></param>
        public static List<Type> GetParentsFromType(Type childType)
        {
#if false
            // This is how it should work when ReferredBy is fixed.
            SfcMetadataDiscovery md = new SfcMetadataDiscovery(childType);
            return md.ReferredBy;
#else
            List<Type> list = new List<Type>();

			//we have to add BindingFlags here to avoid the ambiguous match between the two definitions of the parent class (one in sfcInstance
            //and one overriden in the type definition)
            PropertyInfo parentInfo = childType.GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) ?? childType.GetProperty("Parent");


            if (parentInfo == null)
            {
                // No parent here, must be a root type
                return null;
            }

            object[] customAttributes = parentInfo.GetCustomAttributes(typeof(SfcParentAttribute), true);
            if (customAttributes != null && customAttributes.Length > 0)
            {
                // Get the list of possible parents from the SfcParentAttribute(s) present
                foreach (object obj in customAttributes)
                {
                    if (obj is SfcParentAttribute)
                    {
                        SfcParentAttribute parentAttr = obj as SfcParentAttribute;
                        string parentName = parentAttr.Parent;

                        // Qualify the type name with the child Type's namespace if it doesn't already have one
                        if (parentName.IndexOf(".") == -1)
                        {
                            parentName = childType.Namespace + "." + parentName;
                        }

                        // Use the case-insensitive lookup since we don't know if the type name
                        // really has the proper case or not, and we shouldn't care.
                        Type parentType = SfcRegistration.GetObjectTypeFromFullName(parentName, true);
                        list.Add(parentType);
                    }
                }
            }
            else
            {
                // Get the single possible parent from the return type of the child type's Parent property
                Type parentType = parentInfo.PropertyType;
                list.Add(parentType);
            }

            return list;
#endif
        }

        /// <summary>
        /// Get the list of possible Urn skeleton strings that lead to this type.
        /// </summary>
        /// <param name="inputType"></param>
        /// <returns></returns>
        public static List<String> GetUrnSkeletonsFromType(Type inputType)
        {
            List<String> urns = new List<String>();
            GetUrnSkeletonsFromTypeRec(inputType, "", urns);
            return urns;
        }

        private static string GetUrnSuffixForType(Type type)
        {
            // Property UrnSuffic takes precedence, to support SMO behavior
            string urnSuffix;
            PropertyInfo urnProp = type.GetProperty("UrnSuffix", BindingFlags.Default | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Public);
            if (null == urnProp)
            {
                //we have to check if there is an SfcElementType defined or just use the type name as the Urn suffix
                object[] attributes = type.GetCustomAttributes(typeof(SfcElementTypeAttribute), false);
                if (attributes != null && attributes.Length > 0)
                {
                    TraceHelper.Assert(attributes.Length == 1, "SfcElementType attribute should exist only once");
                    SfcElementTypeAttribute elementType = (SfcElementTypeAttribute)attributes[0];
                    urnSuffix = elementType.ElementTypeName;
                }
                else
                {
                    urnSuffix = type.Name;
                }

            }
            else
            {
                urnSuffix = urnProp.GetValue(null, new object[] { }) as string;
            }

            return urnSuffix;
        }

        private static void GetUrnSkeletonsFromTypeRec(Type t, String urnFragment, List<String> urns)
        {
            List<Type> parentTypes = GetParentsFromType(t);

            // When we are at the root, add the fully-formed urn to our list of possible skeletons
            if (parentTypes == null || (t.GetInterface(typeof(ISfcDomain).FullName) != null))
            {
                // Omit the initial "/"
                string urn = GetUrnSuffixForType(t) + urnFragment;
                urns.Add(urn);
            }
            else
            {
                foreach (Type parentType in parentTypes)
                {
                    GetUrnSkeletonsFromTypeRec(parentType, "/" + GetUrnSuffixForType(t) + urnFragment, urns);
                }
            }
        }

        public static Type GetRootFromType(Type inputType)
        {
            Type parentType = inputType;

            List<Type> seenTypes = new List<Type>();

            while (true)
            {
                seenTypes.Add(parentType);

                List<Type> parentTypes = GetParentsFromType(parentType);
                if (parentTypes == null)
                {
                    return parentType;
                }

                // This assumes that if we return multiple possible parents, they all eventually lead
                // to the same root type, so we just pick the first one.
                // Otherwise this function would have to return a list of possible root Types.
                parentType = parentTypes[0];

                // Have we already seen this type? If so, avoid infinite loop, and return the type the immediately preceds the type causing the loop
                // Microsoft.AnalysisServices.Server is an example of such a type.
                int index = seenTypes.IndexOf(parentType);
                if( index != -1 )
                {
                    return seenTypes[index == 0 ? 0 : index -1];
                }
            }
        }

        private static List<SfcMetadataRelation> InternalLoadTypeRelationsInformationThroughReflection(Type sfcType)
        {
            List<SfcMetadataRelation> list = new List<SfcMetadataRelation>();
            Dictionary<int, SfcMetadataRelation> keyDict = new Dictionary<int,SfcMetadataRelation>();

            foreach (PropertyInfo property in sfcType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                AddToPropertyInfoCache(sfcType.TypeHandle, property.Name, property);

                object[] customAttributes = property.GetCustomAttributes(true);
                if (customAttributes == null || customAttributes.Length == 0)
                {
                    continue;
                }
                Type containerType = null;
                Type propertyType = property.PropertyType;
                SfcCardinality cardinality = SfcCardinality.None;
                SfcRelationship relationship = SfcRelationship.None;
                SfcPropertyFlags flags = SfcPropertyFlags.None;
                bool addToRelations = false;
                bool versionExist = false;
                bool skuExist = false;
                bool keysExist = false;
                int keyPosition = -1;
                bool isReference = false;
                bool isReferenceCollection = false;
                object defaultValue = null;
                List<Attribute> attributes = new List<Attribute>();

                foreach (object obj in customAttributes)
                {
                    if (obj is Attribute)
                    {
                        Type t = obj.GetType();
                        if (t == typeof(SfcObjectAttribute))
                        {
                            if (relationship != SfcRelationship.None || flags != SfcPropertyFlags.None || isReference || isReferenceCollection)
                            {
                                ConflictReporting(customAttributes, sfcType.Name, property.Name);
                            }
                            SfcObjectAttribute o = obj as SfcObjectAttribute;
                            relationship = o.Relationship;
                            if (relationship == SfcRelationship.ChildContainer || relationship == SfcRelationship.ObjectContainer)
                            {
                                propertyType = o.ContainsType;
                                containerType = property.PropertyType;
                            }
                            cardinality = o.Cardinality;
                            attributes.Add(o);
                            addToRelations = true;
                        }
                        else if (t == typeof(SfcIgnoreAttribute))
                        {
                            addToRelations = false;
                            break;
                        }
                        else if (t == typeof(SfcKeyAttribute))
                        {
                            if (skuExist || versionExist)
                            {
                                ConflictReporting(customAttributes, sfcType.Name, property.Name);
                            }
                            keysExist = true;
                            keyPosition = ((SfcKeyAttribute)obj).Position;
                            attributes.Add((SfcKeyAttribute)obj);
                            addToRelations = true;
                        }
                        else if (t == typeof(SfcPropertyAttribute))
                        {
                            if (relationship != SfcRelationship.None)
                            {
                                ConflictReporting(customAttributes, sfcType.Name, property.Name);
                            }
                            SfcPropertyAttribute p = obj as SfcPropertyAttribute;
                            flags = p.Flags;
                            defaultValue = ConvertDefaultValue(p.DefaultValue, propertyType);
                            attributes.Add(p);
                            addToRelations = true;
                        }
                        else if (t == typeof(SfcReferenceAttribute))
                        {
                            if (relationship != SfcRelationship.None)
                            {
                                ConflictReporting(customAttributes, sfcType.Name, property.Name);
                            }
                            SfcReferenceAttribute r = obj as SfcReferenceAttribute;
                            attributes.Add(r);
                            isReference = true;
                            addToRelations = true;
                        }
                        else if (t == typeof(SfcReferenceSelectorAttribute))
                        {
                            if (relationship != SfcRelationship.None)
                            {
                                ConflictReporting(customAttributes, sfcType.Name, property.Name);
                            }
                            SfcReferenceSelectorAttribute r = obj as SfcReferenceSelectorAttribute;
                            attributes.Add(r);
                            isReference = true;
                            addToRelations = true;
                        }
                        else if (t == typeof(SfcReferenceCollectionAttribute))
                        {
                            if (relationship != SfcRelationship.None)
                            {
                                ConflictReporting(customAttributes, sfcType.Name, property.Name);
                            }
                            SfcReferenceCollectionAttribute r = obj as SfcReferenceCollectionAttribute;
                            attributes.Add(r);
                            isReferenceCollection = true;
                            addToRelations = true;
                        }
                        else if (t == typeof(SfcSkuAttribute))
                        {
                            if (keysExist)
                            {
                                ConflictReporting(customAttributes, sfcType.Name, property.Name);
                            }
                            attributes.Add(obj as SfcSkuAttribute);
                            skuExist = true;
                            addToRelations = true;
                        }
                        else if (t == typeof(SfcVersionAttribute))
                        {
                            if (keysExist)
                            {
                                ConflictReporting(customAttributes, sfcType.Name, property.Name);
                            }
                            attributes.Add(obj as SfcVersionAttribute);
                            versionExist = true;
                            addToRelations = true;
                        }
                        else if (t == typeof(SfcSerializationAdapterAttribute))
                        {
                            attributes.Add(obj as SfcSerializationAdapterAttribute);
                            addToRelations = true;
                        }
                        //Any other user attribute
                        else
                        {
                            attributes.Add(obj as Attribute);
                        }
                    }
                }
                if (addToRelations && attributes.Count != 0)
                {
                    SfcMetadataRelation relation = new SfcMetadataRelation(property.Name, propertyType,
                        cardinality, relationship, containerType, flags, defaultValue, new AttributeCollection(attributes.ToArray()));
                    list.Add(relation);
                }
                if (keysExist)
                {
                    SfcMetadataRelation relation = new SfcMetadataRelation(property.Name, propertyType,
                        SfcCardinality.None, SfcRelationship.None, null, SfcPropertyFlags.None, new AttributeCollection(attributes.ToArray()));
                    keyDict.Add(keyPosition, relation);
                }
            }
            typesRelationsCache[sfcType] = list;

            // Read the keys in position order and add to the cache
            // by assigning based on position (the new keyList is guaranteed to be big enough since it is based on the scan we just did)
            List<SfcMetadataRelation> keyList = new List<SfcMetadataRelation>(keyDict.Count);
            for (int i=0; i<keyDict.Count; i++)
            {
                keyList.Add(keyDict[i]);
            }
            typesKeysCache[sfcType] = keyList;
           
            return list;
        }


        private static void ConflictReporting(object[] attributes, string typeName, string propertyName)
        {
            bool isProperty = false;
            bool isObject = false;
            bool isKey = false;
            bool isSku = false;
            bool isVersion = false;
            bool isReference = false;
            bool isReferenceSelector = false;
            bool isReferenceCollection = false;

            foreach (object attribute in attributes)
            {
                if (attribute is Attribute)
                {
                    if (attribute.GetType() == typeof(SfcPropertyAttribute))
                    {
                        isProperty = true;
                    }
                    else if (attribute.GetType() == typeof(SfcObjectAttribute))
                    {
                        isObject = true;
                    }
                    else if (attribute.GetType() == typeof(SfcReferenceAttribute))
                    {
                        isReference = true;
                    }
                    else if (attribute.GetType() == typeof(SfcReferenceSelectorAttribute))
                    {
                        isReferenceSelector = true;
                    }
                    else if (attribute.GetType() == typeof(SfcReferenceCollectionAttribute))
                    {
                        isReferenceCollection = true;
                    }
                    else if (attribute.GetType() == typeof(SfcKeyAttribute))
                    {
                        isKey = true;
                    }
                    else if (attribute.GetType() == typeof(SfcVersionAttribute))
                    {
                        isVersion = true;
                    }
                    else if (attribute.GetType() == typeof(SfcSkuAttribute))
                    {
                        isSku = true;
                    }
                }
            }

            string exceptionMessage = "";

            if (isProperty && isObject)
            {
                exceptionMessage = SfcStrings.AttributeConflict("SfcPropertyAttribute",
                    "SfcObjectAttribute", typeName, propertyName);
            }
            else if (isObject && isReference)
            {
                exceptionMessage = SfcStrings.AttributeConflict("SfcObjectAttribute",
                    "SfcReferenceAttribute", typeName, propertyName);
            }
            else if (isObject && isReferenceSelector)
            {
                exceptionMessage = SfcStrings.AttributeConflict("SfcObjectAttribute",
                    "SfcReferenceSelectorAttribute", typeName, propertyName);
            }
            else if (isObject && isReferenceCollection)
            {
                exceptionMessage = SfcStrings.AttributeConflict("SfcObjectAttribute",
                    "SfcReferenceCollectionAttribute", typeName, propertyName);
            }
            else if (isKey && isSku)
            {
                exceptionMessage = SfcStrings.AttributeConflict("SfcKeyAttribute",
                    "SfcSkuAttribute", typeName, propertyName);
            }
            else if (isKey && isVersion)
            {
                exceptionMessage = SfcStrings.AttributeConflict("SfcKeyAttribute",
                    "SfcVersionAttribute", typeName, propertyName);
            }
            throw new SfcMetadataException(exceptionMessage);
        }

        // This const keeps the maximum amount of types we allow in the dictionary. The number comes from 1GB allowed in the dictionary / 50 properties
        // on average per type and 50 KB on average per propertyInfo (excluding the size of the struct)
        const int maximumDictionaryCount = 1000000 / 50;

        /// <summary>
        /// We limit the number of types to avoid too much memory pressure
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="propertyName"></param>
        /// <param name="property"></param>
        private static void AddToPropertyInfoCache(RuntimeTypeHandle handle, string propertyName, PropertyInfo property)
        {
            TypeHandlePropertyNameKey key = new TypeHandlePropertyNameKey(propertyName, handle);

            if (typePropertyInfosCache.Count >= maximumDictionaryCount)
            {
                return;        
            }

            PropertyInfo existingProp = null;

            //we have to store the declared only properties in case of a collision
            if (!typePropertyInfosCache.TryGetValue(key, out existingProp) || !existingProp.DeclaringType.TypeHandle.Equals(handle))
            {
                typePropertyInfosCache[key] = property;
            }

        }

        internal static bool TryGetCachedPropertyInfo(RuntimeTypeHandle typeHandle, string propertyName, out PropertyInfo pInfo)
        {
            TypeHandlePropertyNameKey key = new TypeHandlePropertyNameKey(propertyName, typeHandle);

            return typePropertyInfosCache.TryGetValue(key, out pInfo);
        }

        private static List<SfcMetadataRelation> InternalLoadTypeKeysInformationThroughReflection(Type sfcType)
        {
            Dictionary<int, SfcMetadataRelation> keyDict = new Dictionary<int,SfcMetadataRelation>();

            foreach (PropertyInfo property in sfcType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                object[] customAttributes = property.GetCustomAttributes(true);
                if (customAttributes == null || customAttributes.Length == 0)
                {
                    continue;
                }
                bool hasKey = false;
                int keyPosition = -1;
                List<Attribute> attributes = new List<Attribute>();

                foreach (object obj in customAttributes)
                {
                    if (obj is Attribute)
                    {
                        if (obj is SfcKeyAttribute)
                        {
                            hasKey = true;
                            keyPosition = ((SfcKeyAttribute)obj).Position;
                        }
                        attributes.Add(obj as Attribute);
                    }
                }

                if (hasKey)
                {
                    AttributeCollection collection = new AttributeCollection(attributes.ToArray());
                    SfcMetadataRelation relation = new SfcMetadataRelation(property.Name, property.PropertyType,
                        SfcCardinality.None, SfcRelationship.None, null, SfcPropertyFlags.None, collection);
                    keyDict.Add(keyPosition, relation);
                }
            }

            // Read the keys in position order and add to the cache
            // by assigning based on position (the new keyList is guaranteed to be big enough since it is based on the scan we just did)
            List<SfcMetadataRelation> keyList = new List<SfcMetadataRelation>(keyDict.Count);
            for (int i=0; i<keyDict.Count; i++)
            {
                keyList.Add(keyDict[i]);
            }

            typesKeysCache[sfcType] = keyList;

            return keyList;
        }

        private static object ConvertDefaultValue(string defaultValueAsString, Type propertyType)
        {
            object defaultValue = null;

            // We need to guard the following code with a check to SqlContext to prevent 
            // from executiing TypeDescriptor while running inside SQLCLR. That means
            // the default values are not available when executing inside SQLCLR context.
            // That is fine, because default values are used only for Design Mode. 
#if !NETSTANDARD2_0
            if (!SqlContext.IsAvailable && !string.IsNullOrEmpty(defaultValueAsString))
#else
            if (!string.IsNullOrEmpty(defaultValueAsString))

#endif
            {
                TypeConverter converter = TypeDescriptor.GetConverter(propertyType);
                if (null != converter && converter.CanConvertFrom(typeof(string)))
                {
                    defaultValue = converter.ConvertFrom(defaultValueAsString);
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// If this is an object with one or more identifying keys, the list of properties holding the keys will be returned.
        /// </summary>
        public virtual List<SfcMetadataRelation> Keys
        {
            get
            {
                List<SfcMetadataRelation> copyList = SfcMetadataDiscovery.GetTypeKeys(this.Type);
                SfcMetadataRelation[] dataCopy = new SfcMetadataRelation[copyList.Count];
                copyList.CopyTo(dataCopy);
                return new List<SfcMetadataRelation>(dataCopy);
            }
        }

        internal ReadOnlyCollection<SfcMetadataRelation> ReadOnlyKeys
        {
            get
            {
                return SfcMetadataDiscovery.GetTypeKeys(this.Type).AsReadOnly();
            }
        }

        public virtual List<SfcMetadataRelation> Objects
        {
            get
            {
                List<SfcMetadataRelation> list = new List<SfcMetadataRelation>();
                foreach (SfcMetadataRelation r in this.Relations)
                {
                    foreach (Attribute a in r.RelationshipAttributes)
                    {
                        if (a is SfcRelationshipAttribute)
                        {
                            list.Add(r.MemberwiseClone() as SfcMetadataRelation);
                            break;
                        }
                    }
                }
                return list;
            }
        }

        public virtual List<SfcMetadataRelation> Properties
        {
            get
            {
                List<SfcMetadataRelation> list = new List<SfcMetadataRelation>();
                foreach (SfcMetadataRelation r in this.Relations)
                {
                    foreach (Attribute a in r.RelationshipAttributes)
                    {
                        if (a is SfcPropertyAttribute)
                        {
                            list.Add(r.MemberwiseClone() as SfcMetadataRelation);
                            break;
                        }
                    }
                }
                return list;


            }
        }

        internal ReadOnlyCollection<SfcMetadataRelation> ReadOnlyCollectionProperties
        {
            get
            {
                List<SfcMetadataRelation> list = null;

                if (!typesPropertiesCache.TryGetValue(this.Type.TypeHandle, out list))
                {
                    lock (typesPropertiesCache)
                    {
                        if (!typesPropertiesCache.TryGetValue(this.Type.TypeHandle, out list))
                        {
                            list = new List<SfcMetadataRelation>();
                            foreach (SfcMetadataRelation r in this.ReadOnlyCollectionRelations)
                            {
                                foreach (Attribute a in r.RelationshipAttributes)
                                {
                                    if (a is SfcPropertyAttribute)
                                    {
                                        list.Add(r);
                                        break;
                                    }
                                }
                            }

                            typesPropertiesCache[this.Type.TypeHandle] = list;
                        }
                    }
                }

                TraceHelper.Assert(list != null, "ReadOnlyProperties return list can't be null");
                return list.AsReadOnly();
            }
        }

        /// <summary>
        /// This function is used to clean the static caches. It is used by serialization to dispose of unused objects.
        /// </summary>
        internal static void CleanupCaches()
        {
            lock (typesRelationsCache)
            {
                typesRelationsCache.Clear();
            }

            lock (relationViewNamesCache)
            {
                relationViewNamesCache.Clear();
            }

            lock (typesKeysCache)
            {
                typesKeysCache.Clear();
            }

            lock (typesPropertiesCache)
            {
                typesPropertiesCache.Clear();
            }

            lock (typesReferencesCache)
            {
                typesReferencesCache.Clear();
            }

            lock (typePropertyInfosCache)
            {
                typePropertyInfosCache.Clear();
            }

            lock (SfcUtility.typeCache)
            {
                SfcUtility.typeCache.Clear();
            }
        }

        public virtual SfcMetadataRelation FindProperty(string propertyName)
        {
            foreach (SfcMetadataRelation r in this.Relations)
            {
                if (r.IsSfcProperty && r.PropertyName == propertyName)
                {
                    return r.MemberwiseClone() as SfcMetadataRelation;
                }
            }

            return null;
        }
    }


    public class SfcMetadataRelation : SfcMetadataDiscovery
    {
        private string m_propertyName;              // Name of property
        private SfcCardinality m_cardinality;       // Cardinality in relationship with parent
        private SfcRelationship m_relationship;     // Relationship with parent
        private SfcPropertyFlags m_propertyFlags;   // Property flags
        private Type m_containerType;               // Container type, for container objects
        private object m_defaultValue;              // Property default value
        private AttributeCollection m_attributes;

        public SfcMetadataRelation(string propertyName, Type type, SfcCardinality cardinality, SfcRelationship relationship, Type containerType,
            SfcPropertyFlags flags, object defaultValue, AttributeCollection attributes)
            : base(type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            m_propertyName = propertyName;
            m_cardinality = cardinality;
            m_relationship = relationship;
            m_containerType = containerType;
            m_propertyFlags = flags;
            m_defaultValue = defaultValue;
            m_attributes = attributes;
        }

        public SfcMetadataRelation(string propertyName, Type type, SfcCardinality cardinality, SfcRelationship relationship, Type containerType,
            SfcPropertyFlags flags, AttributeCollection attributes)
            : this(propertyName, type, cardinality, relationship, containerType, flags, null, attributes)
        {
        }

        public SfcMetadataRelation(string propertyName, Type type, SfcCardinality cardinality, SfcRelationship relationship, Type containerType,
            AttributeCollection attributes)
            : base(type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            m_propertyName = propertyName;
            m_cardinality = cardinality;
            m_relationship = relationship;
            m_containerType = containerType;
            m_attributes = attributes;
        }

        public SfcMetadataRelation(string propertyName, Type type, SfcCardinality cardinality, SfcRelationship relationship, Type containerType)
            : base(type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            m_propertyName = propertyName;
            m_cardinality = cardinality;
            m_relationship = relationship;
            m_containerType = containerType;
        }


        public SfcMetadataRelation(string propertyName, Type type, SfcCardinality cardinality, SfcRelationship relationship, AttributeCollection attributes)
            : base(type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            m_propertyName = propertyName;
            m_cardinality = cardinality;
            m_relationship = relationship;
            m_attributes = attributes;
        }

        public SfcMetadataRelation(string propertyName, Type type, SfcCardinality cardinality, AttributeCollection attributes)
            : base(type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            m_propertyName = propertyName;
            m_cardinality = cardinality;
            m_relationship = SfcRelationship.Object; // This is the most common
            m_attributes = attributes;
        }

        public SfcMetadataRelation(string propertyName, Type type, SfcPropertyFlags flags, AttributeCollection attributes)
            : base(type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            m_propertyName = propertyName;
            m_cardinality = SfcCardinality.One;
            m_relationship = SfcRelationship.None;
            m_propertyFlags = flags;
            m_attributes = attributes;
        }

        public SfcMetadataRelation(string propertyName, Type type, SfcPropertyFlags flags)
            : base(type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            m_propertyName = propertyName;
            m_cardinality = SfcCardinality.One;
            m_relationship = SfcRelationship.None;
            m_propertyFlags = flags;
        }

        public SfcMetadataRelation(string propertyName, Type type)
            : base(type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            m_propertyName = propertyName;
            m_cardinality = SfcCardinality.One; // This is the most common
            m_relationship = SfcRelationship.Object;
        }

        /// <summary>
        /// Name of the property.
        /// </summary>
        public string PropertyName
        {
            get
            {
                return m_propertyName;
            }
        }

        /// <summary>
        /// Specifies the cardinality of this type in relationship with its parent.
        /// </summary>
        public SfcCardinality Cardinality
        {
            get
            {
                return m_cardinality;
            }
        }

        /// <summary>
        /// Relationship with the parent.
        /// </summary>
        public SfcRelationship Relationship
        {
            get
            {
                return m_relationship;
            }
        }

        /// <summary>
        /// If this is a property type, this will return the flags that have been defined for it.
        /// </summary>
        public SfcPropertyFlags PropertyFlags
        {
            get
            {
                return m_propertyFlags;
            }
        }

        /// <summary>
        /// If htis is a property type, this will return a default value for the property
        /// </summary>
        public object PropertyDefaultValue
        {
            get
            {
                return m_defaultValue;
            }
        }

        /// <summary>
        /// Attributes for this node.
        /// </summary>
        public AttributeCollection RelationshipAttributes
        {
            get
            {
                return this.m_attributes;
            }
        }

        /// <summary>
        /// If this is a container, this is the type it contains
        /// </summary>
        public Type ContainerType
        {
            get
            {
                return m_containerType;
            }
        }

        /// <summary>
        /// Helper method to resolve references
        /// If the metadata relation is a reference relationship, it will use the reference attribute's resolver to return the target instances.
        /// If the metadata relation is a simple property value, it will return the value itself.
        /// </summary>
        /// <returns>The target value.</returns>
        public object Resolve(object instance)
        {
            foreach (Attribute attribute in this.RelationshipAttributes)
            {
                if (attribute is SfcReferenceAttribute)
                {
                    SfcReferenceAttribute sfcReferenceAttribute = attribute as SfcReferenceAttribute;
                    return sfcReferenceAttribute.Resolve(instance);
                }
            }

            PropertyInfo property = instance.GetType().GetProperty(this.PropertyName);
            TraceHelper.Assert(property != null);
            // Just return the property value directly for non-reference properties
            return property.GetValue(instance, null);
        }

        /// <summary>
        /// Helper method to resolve a single-target reference.
        /// It returns a strongly-typed instance.
        /// </summary>
        /// <typeparam name="S">The type of the source instance.</typeparam>
        /// <typeparam name="T">The type of the target instance.</typeparam>
        /// <param name="instance">The source instance to resolve for.</param>
        /// <returns>The target instance.</returns>
        public T Resolve<T, S>(S instance)
        {
            foreach (Attribute attribute in this.RelationshipAttributes)
            {
                if (attribute is SfcReferenceAttribute)
                {
                    SfcReferenceAttribute sfcReferenceAttribute = attribute as SfcReferenceAttribute;
                    return sfcReferenceAttribute.Resolve<T, S>(instance);
                }
            }

            PropertyInfo property = instance.GetType().GetProperty(this.PropertyName);
            TraceHelper.Assert(property != null);
            // Just return the property value directly for non-reference properties
            return (T)property.GetValue(instance, null);
        }

        /// <summary>
        /// Helper method to resolve reference collections.
        /// If the metadata relation is a reference collection relationship, it will enumerate the target collection instances.
        /// If the metadata relation is a simple property value, it will enumerate the value itself.
        /// </summary>
        /// <param name="instance">The source instance to resolve for.</param>
        /// <returns>The target instance enumeration.</returns>
        public IEnumerable ResolveCollection(object instance)
        {
            foreach (Attribute attribute in this.RelationshipAttributes)
            {
                if (attribute is SfcReferenceCollectionAttribute)
                {
                    SfcReferenceCollectionAttribute sfcReferenceCollectionAttribute = (SfcReferenceCollectionAttribute)attribute;
                    return sfcReferenceCollectionAttribute.ResolveCollection(instance);
                }
            }

            // Yield normal property values if we didn't find any reference collection attributes
            PropertyInfo property = instance.GetType().GetProperty(this.PropertyName);
            TraceHelper.Assert(property != null);
            // Just yield the property values directly for non-reference collection properties
            return (IEnumerable)property.GetValue(instance, null);
        }

        /// <summary>
        /// Helper method to resolve reference collections.
        /// It yields strongly-typed instances.
        /// Any enumerated values which are not compatible as type <typeparamref name="T"/> are skipped over.
        /// </summary>
        /// <typeparam name="S">The type of the source instance.</typeparam>
        /// <typeparam name="T">The type of the target enumeration.</typeparam>
        /// <param name="instance">The source instance to resolve for.</param>
        /// <returns>The target values enumeration.</returns>
        public IEnumerable<T> ResolveCollection<T, S>(S instance)
        {
            foreach (Attribute attribute in this.RelationshipAttributes)
            {
                if (attribute is SfcReferenceCollectionAttribute)
                {
                    SfcReferenceCollectionAttribute sfcReferenceCollectionAttribute = (SfcReferenceCollectionAttribute)attribute;
                    return sfcReferenceCollectionAttribute.ResolveCollection<T, S>(instance);
                }
            }

            // Yield normal property values if we didn't find any reference collection attributes
            PropertyInfo property = instance.GetType().GetProperty(this.PropertyName);
            TraceHelper.Assert(property != null);
            // Just yield the property values directly for non-reference collection properties
            return (IEnumerable<T>)property.GetValue(instance, null);
        }

        internal bool IsSfcProperty
        {
            get
            {
                foreach (Attribute attribute in this.RelationshipAttributes)
                {
                    if (attribute.GetType() == typeof(SfcPropertyAttribute))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the type should be browseable -- used for UI such as Powershell
        /// </summary>
        public new bool IsBrowsable
        {
            //If the SfcBrowsable attr is not set, current object is browsable if it is a collection, 
            //or if it is a singleton with at least one browsable child object.
            get
            {
                SfcBrowsableAttribute[] attrs = (SfcBrowsableAttribute[])this.Type.GetCustomAttributes(typeof(SfcBrowsableAttribute), false);
                if (attrs.Length > 0)
                {
                    return attrs[0].IsBrowsable;
                }
                else if(this.Cardinality == SfcCardinality.None ||
                        this.Cardinality == SfcCardinality.One ||
                        this.Cardinality == SfcCardinality.ZeroToOne)
                {
                    foreach(SfcMetadataRelation rel in this.Relations)
                    {
                        if(rel.Relationship != SfcRelationship.None &&
                           rel.Relationship != SfcRelationship.Ignore &&
                           rel.Relationship != SfcRelationship.ParentObject &&
                           rel.IsBrowsable)
                        {
                            return true;
                        }
                    }
                    
                    return false;
                }
                
                return true;
            }
        }

        /// <summary>
        /// Indicates whether the type supports design mode
        /// </summary>
        public bool SupportsDesignMode
        {
            get
            {
                return this.Type.GetInterface("ISfcSupportsDesignMode") != null;
            }
        }
    }


    /// <summary>
    /// Interface that returns metadata for the type instance on which it is implemented
    /// </summary>
    public interface ISfcMetadata
    {
        SfcMetadataDiscovery Metadata
        {
            get;
        }
    }

    /// <summary>
    /// Interface that returns the root instance of the metadata provider. As each domain
    /// may implement its own provider, this is the method that will return the domain's implementation
    /// of the metadata provider.
    /// </summary>
    public interface ISfcMetadataProvider
    {
        SfcMetadataDiscovery GetMetadataProvider();
    }
}

