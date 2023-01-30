// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;


namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    internal enum OnCollision
    {
        Fail = 0,
        Overwrite = 1,
        Discard = 2,
    }

    /// <summary>
    /// Holder for any instance in the cache 
    /// </summary>
    internal class ObjectContainer
    {
        private object sfcInstance;
        private Dictionary<String, Dictionary<String, ObjectContainer>> collections;
        private Dictionary<String, ObjectContainer> children;
        private String uri; 

        public ObjectContainer(object sfcInstance, String uri)
        {
            this.collections = new Dictionary<string, Dictionary<string, ObjectContainer>>();
            this.children = new Dictionary<string, ObjectContainer>();
            this.sfcInstance = sfcInstance;
            this.Uri = uri;
        }

        public Dictionary<String, Dictionary<String, ObjectContainer>> Collections
        {
            get { return collections;}
        }

        public Dictionary<String, ObjectContainer> Children
        {
            get { return children; }
        }

        public String Uri
        {
            get {return uri;}
            set { uri = value;}
        }

        public object SfcInstance
        {
            get { return sfcInstance; }
        }
    }

    internal class SfcCache
    {
        Dictionary<String, object> instances;
        Dictionary<String, bool> deserializedStore;
        Dictionary<String, bool> typeUrnFragmentTable;

        internal Dictionary<String, object> Instances
        {
            get
            {
                return instances;
            }
        }

        public SfcCache()
        {
            instances = new Dictionary<string, object>();
            deserializedStore = new Dictionary<string, bool>();
            typeUrnFragmentTable = new Dictionary<String, bool>();
        }

        public void Add(String uri, object obj, OnCollision onCollision)
        {
            instances.Add(uri, obj);
            deserializedStore.Add(uri, false);
            DiscoverKeysOfTypes(obj);
        }

        //This method remembers the urn fragment of given sfc type (that has sfc keys?)
        //In deserialization ,we use URN to infer the tree structure. There, we need to
        //distinguish between urn fragment that is just type name versus node name.
        private void DiscoverKeysOfTypes(object instance)
        {
            Type instanceType = instance.GetType();

            if (SfcMetadataDiscovery.GetTypeKeys(instanceType).Count > 0)
            {
                //To be precise, there is a problem with what we're doing below. We want to remember 
                //the urn fragment of types we have seen during deserialization, so we store it here.
                //This helps when we later reconstruct the tree using urns of individual nodes.
                //However, many types have same urn fragments (e.g: DatabaseRole and ServerRole has
                //fragment 'Role'). This means we'll think we've seen DatabaseRole type when in fact
                //we've seen ServerRole type. However, semantics is OK so far, since all we care is
                //we correctly determine that 'Role' corresponds to some type (see ParseUri method).
                string urnFragment = new SfcMetadataDiscovery(instanceType).ElementTypeName;
                if (!typeUrnFragmentTable.ContainsKey(urnFragment))
                {
                    typeUrnFragmentTable.Add(urnFragment, true);
                }
            }
        }

        private String GetContainerType(Type containedType, String relationshipName)
        {
            String relationIndexer = null;
            SfcMetadataDiscovery metaData = new SfcMetadataDiscovery(containedType);

            foreach (SfcMetadataRelation relation in metaData.ReadOnlyCollectionRelations)
            {
                if ((relation.Relationship == SfcRelationship.ChildContainer) ||
                    (relation.Relationship == SfcRelationship.ObjectContainer))
                {
                    //the relationshipname is essentially the urn fragment of some type. Since SMO types have
                    //urn fragment which differs from type name, we should use ElementTypeName instead of relation.Type.Name
                    if (relationshipName == relation.ElementTypeName)
                    {
                        relationIndexer = relation.PropertyName;
                    }
                }
            }

            return relationIndexer;
        }

        /// <summary>
        /// Gets the property name of singleton property marked as a child of an object (using metadata), given the parent's
        /// and the singleton's type.
        /// </summary>
        /// <param name="containingType"></param>
        /// <param name="singletonType"></param>
        /// <returns></returns>
        private static String GetSingletonPropertyNameFromType(Type containingType, Type singletonType)
        {
            string propertyName = string.Empty;
            SfcMetadataDiscovery metaData = new SfcMetadataDiscovery(containingType);

            foreach (SfcMetadataRelation relation in metaData.ReadOnlyCollectionRelations)
            {
                if ((relation.Relationship == SfcRelationship.ChildObject ||
                     relation.Relationship == SfcRelationship.Object) &&
                    relation.Type == singletonType)
                {
                    propertyName = relation.PropertyName;
                }
            }

            return propertyName;
        }

        internal static List<String> GetSmlFragments(String smlUri, bool smlUnEscape)
        {
            // the fragments in the input smlUri is always escaped with '_', and joined by separator '/'.
            // This method get the fragments back. It splits it back to fragments on "non-escaped" 
            // separators.

            List<String> fragments = new List<String>();

            int i = 0;
            int start = 0;
            for (; i < smlUri.Length; i++)
            {
                if (smlUri[i] == '/') // real separator
                {
                    // add everything before separator as a segment
                    fragments.Add(GetSmlSegment(smlUri, start, i, smlUnEscape));
                    start = i+1;
                    continue;
                } 
                else if (smlUri[i] == SfcSecureString.SmlEscaper) // escape ch
                {
                    ++i; // skip a single escape token. might or might not be separator, doesn't matter
                    if (i >= smlUri.Length)
                    {
                        throw new ArgumentException("The string not properly escaped");
                    }
                }
            }
            fragments.Add(GetSmlSegment(smlUri, start, i, smlUnEscape));

            return fragments;
        }

        private static String GetSmlSegment(String smlUri, int startPos, int sepPos, bool smlUnEscape)
        {
            // if the fragment is empty, we add an empty string.
            // it means splitting "/" will return 2 empty fragments.
            if (sepPos == startPos)
            {
                return "";
            }
            else
            {
                String segment = smlUri.Substring(startPos, sepPos - startPos);
                if (smlUnEscape)
                {
                    segment = SfcSecureString.SmlUnEscape(segment);
                }
                return segment;
            }
        }

        private void ParseUri(String subUri, out List<String> fragments, out List<bool> typeBits)
        {
            bool isType = true;
            fragments = new List<string>();
            typeBits = new List<bool>();

            fragments = SfcCache.GetSmlFragments(subUri, false);

            //Note: The following code assumes that:
            //1. For collection, the fragment after a type fragment always corresponds to a name fragment('typename/instance name').
            //2. For singleton, URN fragment is always simply 'typename'.
            //3. Singleton properties occur only as leaves!
            foreach (String fragment in fragments)
            {
                //If this fragment is a type, check if it has keys
                if (isType)
                {
                    typeBits.Add(true);

                    if (typeUrnFragmentTable.ContainsKey(fragment))
                    {
                        isType = false;
                    }
                    //else isType remains true
                }
                else
                {
                    typeBits.Add(false);
                    isType = true;
                }
            }

            return;
        }

        private bool IsParent(String possibleParent, String possibleChild)
        {
            Regex theReg = new Regex(@"[^" + SfcSecureString.SmlEscaper + @"]/"); //To Split all fragments ending with '/' except '_/'

            List<String> fragmentsOfParent = new List<string>();
            List<String> fragmentsOfChild = new List<string>();

            fragmentsOfParent = SfcCache.GetSmlFragments(possibleParent, true);
            fragmentsOfChild = SfcCache.GetSmlFragments(possibleChild, true);
            if (fragmentsOfChild.Count < fragmentsOfParent.Count)
            {
                return false;
            }

            for (int i = 0; i < fragmentsOfParent.Count; i++)
            {
                if (fragmentsOfParent[i] != fragmentsOfChild[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void AddToContainer(ObjectContainer oldContainer, ObjectContainer newContainer)
        {
            object instance = oldContainer.SfcInstance;
            String uri = oldContainer.Uri;

            String rootUri = newContainer.Uri;
            String subUri = uri.Substring(newContainer.Uri.Length + 1); //exclude root portion of the string and additional /

            //check if the newContainer's root is a singleton - if so, add to children list
            //if the root is a collection element, add to collections list

            List<String> fragments = SfcCache.GetSmlFragments(subUri, true);
            
            
            if (fragments.Count == 2)
            {
                Dictionary<String, ObjectContainer> collectionDictionary = new Dictionary<string,ObjectContainer>();
                collectionDictionary.Add(fragments[0], new ObjectContainer(oldContainer.SfcInstance, oldContainer.Uri));

                //Get type holding container
                String relationIndexer = GetContainerType(newContainer.SfcInstance.GetType(), fragments[0]);
                newContainer.Collections.Add(relationIndexer, collectionDictionary);                                            
            }
            else
            {
                //cannot assume name of singleton property is the same as the URN fragment. So attempt to query metadata.
                //if not found, fallback to fragment
                string childKey = GetSingletonPropertyNameFromType(newContainer.SfcInstance.GetType(), oldContainer.SfcInstance.GetType());
                if (string.IsNullOrEmpty(childKey))
                {
                    childKey = fragments[0];
                }
                newContainer.Children.Add(childKey, new ObjectContainer(oldContainer.SfcInstance, oldContainer.Uri));
            }

            //Set parent. For non SFC domains, we set parent link on the fly (instead of at the end in this method)
            //so parent link setting occurs only for SFC objects
            if (instance is SfcInstance)
            {
                PropertyInfo pi = instance.GetType().GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                pi.SetValue(oldContainer.SfcInstance, newContainer.SfcInstance, null);
            }

        }

        private void AddToContainer(String uri, ObjectContainer rootContainer, List<ObjectContainer> virtualRootContainers)
        {
            if ((deserializedStore.ContainsKey(uri)) && (deserializedStore[uri] == true))
            {
                return; //already deserialized
            }

            object instance = instances[uri];
            String rootUri = rootContainer.Uri;
            String subUri = uri.Substring(rootContainer.Uri.Length + 1); //exclude root portion of the string and additional /

            //call parseUri
            List<String> fragments;
            List<bool> typeBits;
            ParseUri(subUri, out fragments, out typeBits);

            ObjectContainer currentContainer = rootContainer;

            for(int count = 0; count < fragments.Count; count++)
            {
                String relationshipName = fragments[count];
                String instanceName = "";

                bool singletonType = false;
                if (count < fragments.Count - 1)
                {
                    singletonType = typeBits[count + 1]; // Is the next fragment a type?
                }
                else
                {
                    //boundary case for last fragment being a singleton element
                    singletonType = typeBits[count]; 
                }

                if (!singletonType)
                {
                    instanceName = fragments[++count];

                    String relationIndexer = GetContainerType(currentContainer.SfcInstance.GetType(), relationshipName);
                    if (relationIndexer != null)
                    {
                        if (!currentContainer.Collections.ContainsKey(relationIndexer))
                        {
                            Dictionary<String, ObjectContainer> instanceContainer = new Dictionary<String, ObjectContainer>();
                            currentContainer.Collections.Add(relationIndexer, instanceContainer);
                        }

                        //the collection could already exist with this instance as its member
                        if (!currentContainer.Collections[relationIndexer].ContainsKey(instanceName))
                        {
                            currentContainer.Collections[relationIndexer].Add(instanceName, new ObjectContainer(instance, uri));
                        }

                        //Step into the next container level
                        currentContainer = currentContainer.Collections[relationIndexer][instanceName];

                    }
                    else //If there is no container property for holding this type
                    {
                        throw new SfcNonSerializableTypeException(SfcStrings.SfcNonSerializableType(
                            currentContainer.SfcInstance.GetType().Name));
                    }
                }
                else
                {

                    //cannot assume name of singleton property is the same as the URN fragment. So attempt to query metadata.
                    //if not found, fallback to fragment
                    string childKey = GetSingletonPropertyNameFromType(currentContainer.SfcInstance.GetType(),
                                                                                  instance.GetType());
                    if (string.IsNullOrEmpty(childKey))
                    {
                        childKey = relationshipName;
                    }
                    //We use the relationshipName as the key as for singletons, it is unique per type
                    if (!currentContainer.Children.ContainsKey(relationshipName))
                    {
                        ObjectContainer instanceContainer = new ObjectContainer(instance, uri);
                        currentContainer.Children.Add(childKey, instanceContainer);
                    }

                    currentContainer = currentContainer.Children[childKey];
                }

                //Set the parent if it's last fragment
                if (count == fragments.Count - 1) //last fragment of the URI
                {
                    StringBuilder parentUri = new StringBuilder();
                    parentUri.Append(rootUri);

                    //Determine the parent's boundary based on the last fragment.
                    //If the last fragment is a type, parent is one level higher
                    //else, parent is two levels higher                    
                    List<String> escapedFragments = SfcCache.GetSmlFragments(subUri, false);
                    int parentFragment = escapedFragments.Count - 1;

                    parentFragment = (typeBits[parentFragment] == true) ?
                        (parentFragment - 1) : (parentFragment - 2);
                    for(int i = 0; i <= parentFragment; i++)
                    {
                        parentUri.Append("/");
                        parentUri.Append(escapedFragments[i]);
                    }

                    //If parent is not deserialized, we should get parent first and then deserialize this instance
                    if (deserializedStore[parentUri.ToString()] == false)
                    {
                        deserializedStore[parentUri.ToString()] = true;
                        CheckAndAddNonRootInstances(parentUri.ToString(), rootContainer, virtualRootContainers);
                    }

                    //For non sfc instance objects (SMO now), parent link has already been set. 
                    if (instance is SfcInstance)
                    {
                        PropertyInfo pi = instance.GetType().GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                        pi.SetValue(instance, instances[parentUri.ToString()], null);
                    }
                }
            }
        }

        private void CreateObjectModel(ObjectContainer rootContainer)
        {
            ObjectContainer parentContainer = rootContainer;
            object parentInstance = parentContainer.SfcInstance;

            Queue<ObjectContainer> containerQueue = new Queue<ObjectContainer>();
            containerQueue.Enqueue(parentContainer);

            while (containerQueue.Count > 0)
            {
                parentContainer = containerQueue.Dequeue();
                parentInstance = parentContainer.SfcInstance;

                //Add collections
                foreach (String key in parentContainer.Collections.Keys)
                {
                    PropertyInfo containingField = parentInstance.GetType().GetProperty(key);
                    Object propertyObject = containingField.GetValue(parentInstance, null);

                    Dictionary<String, ObjectContainer> collectionObjects = parentContainer.Collections[key];
                    MethodInfo addMethodInfo = null;
                    foreach (ObjectContainer containedObject in collectionObjects.Values)
                    {
                        //get the add method that takes one argument, which is collection member, in case there are multiple add methods,
                        //each taking different parameter sets
                        if (addMethodInfo == null)
                        {
                            addMethodInfo = propertyObject.GetType().GetMethod("Add", new Type[]{containedObject.SfcInstance.GetType()});
                        }
                        Object[] parameters = { containedObject.SfcInstance };
                        addMethodInfo.Invoke(propertyObject, parameters);
                        containerQueue.Enqueue(containedObject);
                    }
                    
                }

                //Set the singletons
                foreach (String key in parentContainer.Children.Keys)
                {
                    PropertyInfo singletonTypeInfo = parentInstance.GetType().GetProperty(key);
                    //there are computed and readonly singleton properties in SMO domain. So we check for writability first.
                    if (singletonTypeInfo.CanWrite)
                    {
                        singletonTypeInfo.SetValue(parentInstance, parentContainer.Children[key].SfcInstance, null);
                    }

                    containerQueue.Enqueue(parentContainer.Children[key]);
                }
            }
        }

        internal void CheckAndAddNonRootInstances(String uri, ObjectContainer rootContainer, List<ObjectContainer> virtualRootContainers)
        {
            if (!uri.StartsWith(rootContainer.Uri, StringComparison.Ordinal))
            {
                bool hierarchyExists = false;

                ObjectContainer [] originalVirtualRootContainers = new ObjectContainer[virtualRootContainers.Count];
                virtualRootContainers.CopyTo(originalVirtualRootContainers);

                foreach (ObjectContainer existingRootContainer in originalVirtualRootContainers)
                {
                    //Search if any existing virtual root is  a parent of this instance
                    //If so, this instance should be added into its hierarchy - 
                    //else if any existing virtual root is a child of this instance, it should be
                    //replaced by the new instance as the root and become a child.
                    if (IsParent(existingRootContainer.Uri, uri))
                    {
                        AddToContainer(uri, existingRootContainer, virtualRootContainers);
                        hierarchyExists = true;
                    }
                    else if (IsParent(uri, existingRootContainer.Uri))
                    {
                        ObjectContainer newRootContainer = new ObjectContainer(instances[uri], uri);
                        AddToContainer(existingRootContainer, newRootContainer);

                        virtualRootContainers.Remove(existingRootContainer);
                        virtualRootContainers.Add(newRootContainer);
                        hierarchyExists = true;
                    }
                }

                if (!hierarchyExists)
                {
                    virtualRootContainers.Add(new ObjectContainer(instances[uri], uri));
                }
            }
            else
            {
                AddToContainer(uri, rootContainer, virtualRootContainers);
            }

            deserializedStore[uri] = true;
        }

        internal void CreateHierarchy(object root, String rootUri, List<object> unParentedReferences)
        {
            //Start adding up now into a tree
            //Follow a trie datastructure and keep adding children  at each level
            ObjectContainer rootContainer = new ObjectContainer(root, rootUri);
            deserializedStore[rootContainer.Uri] = true;
            List<ObjectContainer> virtualRootContainers = new List<ObjectContainer>();

            foreach (String uri in instances.Keys)
            {
                if (!uri.Equals(rootUri))
                {
                    CheckAndAddNonRootInstances(uri, rootContainer, virtualRootContainers);
                }
            }

            //Add/Set the links to create the object model            
            CreateObjectModel(rootContainer);

            //Create the islands of references
            foreach (ObjectContainer referenceRootContainer in virtualRootContainers)
            {
                unParentedReferences.Add(referenceRootContainer.SfcInstance);
                CreateObjectModel(referenceRootContainer);
            }

            return;
        }
    }
}
