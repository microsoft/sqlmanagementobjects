// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    #region serializer delegates
    public class FilterPropertyEventArgs : EventArgs
    {
        String propertyName;
        SfcInstance instance;

        public FilterPropertyEventArgs(SfcInstance instance, String propertyName)
        {
            this.instance = instance;
            this.propertyName = propertyName;
        }

        public String PropertyName
        {
            get { return propertyName; }
        }

        public SfcInstance Instance
        {
            get { return instance; }
        }
    }

    public delegate object FilterPropertyHandler(SfcSerializer serializer, FilterPropertyEventArgs propertyArgs);
    #endregion

    public sealed class SfcSerializer
    {
        #region members
        private SfcCache cache;
        private List<object> instanceList;  //instance list is needed for supporting SMO - this should be removed soon
        private SfcDependencyEngine.DependencyListEnumerator listEnumerator;
        private object rootInstance;
        private XmlWriter writer;
        private SfcDomainInfo domainInfo;
        private Dictionary<RuntimeTypeHandle, XmlSerializer> xmlSerializersCache = new Dictionary<RuntimeTypeHandle, XmlSerializer>();

        private List<object> unParentedReferences;
        public List<object> UnParentedReferences
        {
            get
            {
                return unParentedReferences;
            }
        }
        #endregion

        #region filter property Event Handler
        public event FilterPropertyHandler FilterPropertyHandler = null;
        #endregion

        #region constructor
        public SfcSerializer()
        {
            this.cache = new SfcCache();
            this.instanceList = new List<object>();
            this.unParentedReferences = new List<object>();
            listEnumerator = null;
            rootInstance = null;
        }
        #endregion

        #region Serialize
        /// <summary>
        /// This function compiles the list of the instances which are need to be serialize 
        /// the root successfully. 
        /// </summary>
        /// <param name="instance">The root instance to serialize</param>
        public void Serialize(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance", SfcStrings.SfcNullArgumentToSerialize);
            }

            rootInstance = instance;
            this.domainInfo = SfcRegistration.GetRegisteredDomainForType(instance.GetType());

            //TODO: Once dependencyengine is ready, we need to adjust the enum values - for now, using FULL always
            if (instance is SfcInstance)
            {
                SfcInstance sfcInstance = instance as SfcInstance;
                SfcDependencyEngine depEng = new SfcDependencyEngine(SfcDependencyDiscoveryMode.Full, SfcDependencyAction.Serialize);
                depEng.Add(sfcInstance);
                depEng.Discover();
                listEnumerator = depEng.GetListEnumerator();
            }
            else if (instance is IAlienObject)
            {
                IAlienObject alien = instance as IAlienObject;
                instanceList = alien.Discover();
            }
            else
            {
                throw new SfcNonSerializableTypeException(SfcStrings.SfcNonSerializableType(instance.GetType().Name));
            }
        }

        /// <summary>
        /// This function serializes the dependency list instances along with the root
        /// </summary>
        /// <param name="xmlWriter">XmlWriter where the whole serialized document is committed to</param>
        public void Write(XmlWriter xmlWriter)
        {
            if (xmlWriter == null)
            {
                throw new ArgumentNullException("writer", SfcStrings.SfcNullWriterToSerialize);
            }

            if (rootInstance == null)
            {
                throw new SfcSerializationException(SfcStrings.SfcInvalidWriteWithoutDiscovery);
            }

            try
            {
                writer = xmlWriter;
                WriteAllInstances();

            }
            finally
            {
                //cleanup all the static dictionaries of SFCMetadata to dispose all the unused metadata information
                SfcMetadataDiscovery.CleanupCaches();

                writer.Close();
            }
        }

        /// <summary>
        /// Writes the docinfo
        /// </summary>
        /// <param name="docWriter"></param>
        /// <param name="smlUri"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        internal void WriteInstancesDocInfo(XmlWriter docWriter, String smlUri, int version)
        {
            docWriter.WriteStartElement("docinfo");
            
            docWriter.WriteStartElement("aliases");
            docWriter.WriteStartElement("alias");
            docWriter.WriteRaw(smlUri);
            docWriter.WriteEndElement();
            docWriter.WriteEndElement();

            docWriter.WriteStartElement("sfc", "version", null);
            docWriter.WriteAttributeString("DomainVersion", version.ToString());
            docWriter.WriteEndElement();

            docWriter.WriteEndElement();
        }

        /// <summary>
        /// Serializes each instance
        /// </summary>
        /// <param name="instanceWriter"></param>
        /// <param name="instance">the element to be serialized</param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        internal void Write(XmlWriter instanceWriter, object instance, Dictionary<String, String> namespaces)
        {           
            try
            {
                String smluri = SfcUtility.GetSmlUri(SfcUtility.GetUrn(instance), instance.GetType(), true);
                if (smluri != null)
                {
                    instanceWriter.WriteStartElement("document");

                    WriteInstancesDocInfo(instanceWriter, smluri, domainInfo.GetLogicalVersion(rootInstance));

                    instanceWriter.WriteStartElement("data");
                    WriteInternal(instanceWriter, instance, namespaces);
                    instanceWriter.WriteEndElement();

                    instanceWriter.WriteEndElement();
                }
            }
            catch (Exception e)
            {
                throw new SfcSerializationException(SfcStrings.SfcInvalidSerializationInstance(instance.ToString()), e);
            }
        }

        /// <summary>
        /// Writes all the dependent list of instances
        /// </summary>
        private void WriteAllInstances()
        {
            Dictionary<String, String> namespaces = new Dictionary<string, string>();
            
            namespaces.Add(domainInfo.NamespaceQualifier,
                String.Format("http://schemas.microsoft.com/sqlserver/{0}/2007/08", domainInfo.NamespaceQualifier));
            namespaces.Add("sfc", "http://schemas.microsoft.com/sqlserver/sfc/serialization/2007/08");
            namespaces.Add("sml", "http://schemas.serviceml.org/sml/2007/02");
            namespaces.Add("xs", "http://www.w3.org/2001/XMLSchema");

            //BeginSML information 
            writer.WriteStartDocument();

            writer.WriteStartElement("model", "http://schemas.serviceml.org/smlif/2007/02");

            //basic identity info
            writer.WriteStartElement("identity");
            writer.WriteElementString("name", "urn:uuid:96fe1236-abf6-4a57-b54d-e9baab394fd1");
            writer.WriteElementString("baseURI", "http://documentcollection/");
            writer.WriteEndElement();

            writer.WriteStartElement("xs", "bufferSchema", namespaces["xs"]);
            writer.WriteStartElement("definitions", "http://schemas.serviceml.org/smlif/2007/02");
            writer.WriteAttributeString("xmlns", "sfc", null, namespaces["sfc"]);
            writer.WriteStartElement("document");
            WriteInstancesDocInfo(writer, 
                "/system/schema/" + domainInfo.NamespaceQualifier, 
                domainInfo.GetLogicalVersion(rootInstance));
            writer.WriteStartElement("data");

            writer.WriteStartElement("xs", "schema", null);
            writer.WriteAttributeString("targetNamespace", namespaces[domainInfo.NamespaceQualifier]);
            foreach (String xmlnamespace in namespaces.Keys)
            {
                if (xmlnamespace != domainInfo.NamespaceQualifier)
                {
                    writer.WriteAttributeString(
                        "xmlns",
                        xmlnamespace,
                        null,
                        namespaces[xmlnamespace]);
                }
            }
            writer.WriteAttributeString("elementFormDefault", "qualified");

            WriteSchemaToWriter(writer, namespaces);

            writer.WriteStartElement(domainInfo.NamespaceQualifier, 
                "bufferData", 
                namespaces[domainInfo.NamespaceQualifier]);
            writer.WriteStartElement("instances", "http://schemas.serviceml.org/smlif/2007/02");
            writer.WriteAttributeString("xmlns", "sfc", null, namespaces["sfc"]);

            if (rootInstance is SfcInstance)
            {
                Write(writer, rootInstance, namespaces); //Dependency Enumerator does not include root at the top

                while (listEnumerator.MoveNext())
                {
                    if (listEnumerator.Current.Instance != rootInstance)
                    {
                        Write(writer, listEnumerator.Current.Instance, namespaces);
                    }
                }
            }
            else //SMO case
            {
                foreach (object instance in instanceList)
                {
                    Write(writer, instance, namespaces);
                }
            }

            writer.WriteEndElement();   //close schema tag
            writer.WriteEndElement();   //close data tag
            writer.WriteEndElement();   //close document tag
            writer.WriteEndElement();   //close definition tag
            writer.WriteEndElement();   //close bufferSchema tag

            writer.WriteEndElement(); //close instances tag
            writer.WriteEndElement(); //close bufferData tag

            //EndSML information
            writer.WriteEndDocument();
            writer.Close();
        }

        private void WriteSchemaToWriter(XmlWriter writer, Dictionary<String, String> namespaces)
        {
            Dictionary<Type, int> typeCache = new Dictionary<Type, int>();

            if (rootInstance is SfcInstance)
            {
                typeCache.Add(rootInstance.GetType(), 1);
                WriteSchema(writer, rootInstance.GetType(), namespaces);

                while (listEnumerator.MoveNext())
                {
                    if (listEnumerator.Current.Instance != rootInstance)
                    {
                        if (!typeCache.ContainsKey(listEnumerator.Current.Instance.GetType()))
                        {
                            typeCache.Add(listEnumerator.Current.Instance.GetType(), 1); //value is dummy
                            WriteSchema(writer, listEnumerator.Current.Instance.GetType(), namespaces);
                        }
                    }
                }

                listEnumerator.Reset();
            }
            else //SMO case
            {
                foreach (object instance in instanceList)
                {
                    if (!typeCache.ContainsKey(instance.GetType()))
                    {
                        typeCache.Add(instance.GetType(), 1); //value is dummy
                        WriteSchema(writer, instance.GetType(), namespaces);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instanceWriter"></param>
        /// <param name="instance"></param>
        /// <param name="namespaces"></param>
        private void WriteInternal(XmlWriter instanceWriter, object instance, Dictionary<String, String> namespaces)
        {
            instanceWriter.WriteStartElement(this.domainInfo.NamespaceQualifier, instance.GetType().Name, null);
            foreach (String xmlnamespace in namespaces.Keys)
            {
                instanceWriter.WriteAttributeString(
                    "xmlns",
                    xmlnamespace,
                    null,
                    namespaces[xmlnamespace]);
            }

            SfcMetadataDiscovery metaData = new SfcMetadataDiscovery(instance.GetType());
            string parentUri = null;
            foreach (SfcMetadataRelation relation in metaData.ReadOnlyCollectionRelations)
            {
                switch (relation.Relationship)
                {
                    case SfcRelationship.ParentObject:
                        instanceWriter.WriteStartElement(domainInfo.NamespaceQualifier, "Parent", null);
                        instanceWriter.WriteStartElement("sfc", "Reference", null);
                        instanceWriter.WriteAttributeString("sml", "ref", null, "true");
                        instanceWriter.WriteStartElement("sml", "Uri", null);
                        instanceWriter.WriteRaw(parentUri ?? 
                                        (parentUri = SfcUtility.GetSmlUri(
                                                                          SfcUtility.GetUrn(SfcUtility.GetParent(instance))
                                                                        , SfcUtility.GetParent(instance).GetType()
                                                                        , true)
                                                                        ) 
                                                );
                        instanceWriter.WriteEndElement();
                        instanceWriter.WriteEndElement();
                        instanceWriter.WriteEndElement();
                        break;
                    case SfcRelationship.ChildContainer: //child container
                    case SfcRelationship.ObjectContainer: //reference/object container - not intrinsic in parent
                        bool serializeContainer = true;
                        foreach (Attribute sfcAttribute in relation.RelationshipAttributes)
                        {
                            if (sfcAttribute is SfcNonSerializableAttribute)
                            {
                                serializeContainer = false;
                                break;
                            }
                        }

                        // Serialize the container relationship only if it does not have the SfcNonSerializableAttribute on it.
                        if (serializeContainer == true)
                        {
                            try
                            {
                                PropertyInfo pi = null;
                                if (!SfcMetadataDiscovery.TryGetCachedPropertyInfo(instance.GetType().TypeHandle,
                                    relation.PropertyName, out pi))
                                {
                                    pi = instance.GetType().GetProperty(relation.PropertyName,
                                        BindingFlags.Public | BindingFlags.Instance);
                                }

                                object collection = pi.GetValue(instance, null);

                                if (collection == null)
                                {
                                    break;
                                }


                                IEnumerator collectionEnum = ((IEnumerable) collection).GetEnumerator();


                                if (!collectionEnum.MoveNext())
                                {
                                    break;
                                }


                                instanceWriter.WriteStartElement(domainInfo.NamespaceQualifier, relation.PropertyName,
                                    null);
                                instanceWriter.WriteStartElement("sfc", "Collection", null);

                                do
                                {
                                    object childObject = collectionEnum.Current;

                                    //TODO : VSTS#120524 - this will help remove the checks through .NET reflection
                                    try
                                    {
                                        PropertyInfo systemInfo = null;
                                        ;
                                        if (!SfcMetadataDiscovery.TryGetCachedPropertyInfo(
                                            childObject.GetType().TypeHandle, "IsSystemObject", out systemInfo))
                                        {
                                            systemInfo = childObject.GetType().GetProperty("IsSystemObject");
                                        }

                                        if (systemInfo != null)
                                        {
                                            bool systemProperty = (bool) systemInfo.GetValue(childObject, null);

                                            if (systemProperty == true)
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                    catch (TargetInvocationException)
                                    {
                                        continue;
                                    }

                                    instanceWriter.WriteStartElement("sfc", "Reference", null);
                                    instanceWriter.WriteAttributeString("sml", "ref", null, "true");
                                    instanceWriter.WriteStartElement("sml", "Uri", null);
                                    instanceWriter.WriteRaw(SfcUtility.GetSmlUri(SfcUtility.GetUrn(childObject),
                                        childObject.GetType(), true));
                                    instanceWriter.WriteEndElement();
                                    instanceWriter.WriteEndElement();
                                } while (collectionEnum.MoveNext());
                            }
                            catch (TargetInvocationException)
                            {
                                continue;
                            }
                            // Thrown if a property refers to an object type not supported 
                            // on the current platform. Example - Logfile for Azure SQL DB
                            catch (EnumeratorException)
                            {
                                continue;
                            }

                            instanceWriter.WriteEndElement();
                            instanceWriter.WriteEndElement();
                        }
                        break;
                    default:
                        break;
                }
            }

            foreach (SfcMetadataRelation referenceProperty in metaData.ReadOnlyCollectionProperties)
            {
                foreach (Attribute attribute in referenceProperty.RelationshipAttributes)
                {
                    if (attribute is SfcReferenceAttribute)
                    {
                        SfcReferenceAttribute referenceAttribute = attribute as SfcReferenceAttribute;

                        String referenceUri = null;

                        try
                        {
                            referenceUri = SfcUtility.GetSmlUri(referenceAttribute.GetUrn(instance), referenceAttribute.Type, true);
                        }
                        catch (TargetInvocationException e)
                        {
                            if (e.InnerException.GetType() == typeof(PropertyNotAvailableException))
                            {
                                continue;
                            }

                            throw;
                        }
                        catch (SfcUnsupportedVersionException)
                        {
                            continue;
                        }

                        //Only non-null references are written
                        if (referenceUri != null)
                        {
                            instanceWriter.WriteStartElement(domainInfo.NamespaceQualifier, instance.GetType().Name + referenceProperty.PropertyName, null);
                            instanceWriter.WriteStartElement("sfc", "Reference", null);
                            instanceWriter.WriteAttributeString("sml", "ref", null, "true");

                            instanceWriter.WriteStartElement("sml", "Uri", null);
                            instanceWriter.WriteRaw(referenceUri);
                            instanceWriter.WriteEndElement();

                            instanceWriter.WriteEndElement();
                            instanceWriter.WriteEndElement();
                        }
                    }
                }
            }

            //serializing properties 
            ReadOnlyCollection<SfcMetadataRelation> properties = metaData.ReadOnlyCollectionProperties;
            foreach (SfcMetadataRelation propertyRelation in properties)
            {
                foreach (Attribute attribute in propertyRelation.RelationshipAttributes)
                {
                    if (attribute is SfcPropertyAttribute)
                    {
                        SfcPropertyAttribute property = attribute as SfcPropertyAttribute;
                        if ( ((property.Flags & SfcPropertyFlags.Data) != SfcPropertyFlags.Data) 
                            &&((property.Flags & SfcPropertyFlags.Computed) != SfcPropertyFlags.Computed)
                            )
                        {
                            //this should be cached by XML serializer
                            Type propertyType = null;
                            object propertyVal = null;

                            if (instance is SfcInstance)
                            {
                                SfcInstance sfcInstance = instance as SfcInstance;
                                propertyType = sfcInstance.Properties[propertyRelation.PropertyName].Type;
                                if (FilterPropertyHandler != null)
                                {
                                    FilterPropertyEventArgs propertyArgs = new FilterPropertyEventArgs(sfcInstance, propertyRelation.PropertyName);
                                    propertyVal = FilterPropertyHandler(this, propertyArgs);
                                }
                                else
                                {
                                    propertyVal = ((SfcInstance)instance).Properties[propertyRelation.PropertyName].Value;
                                }
                            }
                            else if (instance is IAlienObject)
                            {
                                IAlienObject alienObject = instance as IAlienObject;

                                try
                                {
                                    propertyType = propertyRelation.Type;
                                    propertyVal = alienObject.GetPropertyValue(propertyRelation.PropertyName, propertyType);
                                }
                                catch (TargetInvocationException)
                                {
                                    continue;
                                }
                            }

                            if (propertyVal != null)
                            {

                                StringBuilder propBuilder = new StringBuilder();
                                XmlWriter propWriter = XmlWriter.Create(propBuilder);

                                //Use custom serialization adapter if it is given. If not use xmlserializer by default.
                                //Get the serialization adapter for this property if it is specified in metadata.
                                IXmlSerializationAdapter serializationAdapter = SfcSerializer.GetSerializationAdapter(propertyRelation);
                                if (serializationAdapter != null)
                                {
                                    serializationAdapter.WriteXml(propWriter, propertyVal);
                                }
                                else
                                {
                                    XmlSerializer serializer = null;

                                    if (!xmlSerializersCache.TryGetValue(propertyType.TypeHandle, out serializer))
                                    {
                                        serializer = new XmlSerializer(propertyType);
                                        xmlSerializersCache.Add(propertyType.TypeHandle, serializer);
                                    }

                                    if (propertyVal.GetType().Equals(typeof(String)))
                                    {
                                        propertyVal = SfcSecureString.XmlEscape(propertyVal.ToString());
                                    }

                                    serializer.Serialize(propWriter, propertyVal);
                                }

                                propWriter.Close();


                                StringReader stringReader = new StringReader(propBuilder.ToString());

                                XmlReader reader = XmlReader.Create(stringReader);

                                reader.MoveToContent();
                                String typeName = reader.LocalName;
                                reader.Read();
                                instanceWriter.WriteStartElement(domainInfo.NamespaceQualifier, propertyRelation.PropertyName, null);
                                instanceWriter.WriteAttributeString("type", typeName);
                                do
                                {
                                    instanceWriter.WriteNode(reader, false);
                                }
                                while (reader.IsStartElement());

                                instanceWriter.WriteEndElement();
                                reader.Close();
                            }
                        }
                    }
                }
            }

            //close the loop
            instanceWriter.WriteEndElement();
        }

        private void WriteSchema(XmlWriter schemaWriter, Type type, Dictionary<String, String> namespaces)
        {
            schemaWriter.WriteStartElement("xs", "element", null);
            schemaWriter.WriteAttributeString("name", type.Name);
            schemaWriter.WriteStartElement("xs", "complexType", null);
            schemaWriter.WriteStartElement("xs", "sequence", null);

            schemaWriter.WriteStartElement("xs", "any", null);
            schemaWriter.WriteAttributeString("namespace", namespaces[domainInfo.NamespaceQualifier]);
            schemaWriter.WriteAttributeString("processContents", "skip");
            schemaWriter.WriteAttributeString("minOccurs", "0");
            schemaWriter.WriteAttributeString("maxOccurs", "unbounded");
            schemaWriter.WriteEndElement();

            schemaWriter.WriteEndElement();
            schemaWriter.WriteEndElement();
            schemaWriter.WriteEndElement();
        }
        #endregion

        #region DeSerialize
        /// <summary>
        /// The public interface for clients to deserialize the file stream
        /// </summary>
        /// <param name="xmlReader">The stream which holds the serialized document</param>
        /// <returns></returns>
        public object Deserialize(XmlReader xmlReader)
        {
            if (xmlReader == null)
            {
                throw new ArgumentNullException("reader", SfcStrings.SfcNullReaderToSerialize);
            }

            // this is the default behavior: all objects deserialized with 
            // Pending state
            return Deserialize(xmlReader, SfcObjectState.Pending);
        }


        /// <summary>
        /// The public interface for clients to deserialize the file stream
        /// </summary>
        /// <param name="xmlReader">The stream which holds the serialized document</param>
        /// <param name="state">All of the deserialized objects will have this state</param>
        /// <returns></returns>
        public object Deserialize(XmlReader xmlReader, SfcObjectState state)
        {
            object rootInstance = null;
            String rootUri = null;
            bool isFirst = false;

            XmlReader docReader;
            StringReader docStream;

            try
            {
                //Check if Upgrade is needed on the serialized document
                xmlReader.ReadToFollowing("definitions");
                xmlReader.ReadToFollowing("alias");
                xmlReader.ReadStartElement();

                String domainPrefix = xmlReader.Value; //system/schema/<Domain name> 
                domainPrefix = domainPrefix.Substring("/system/schema/".Length); //Retrieve the prefix

                String domainName = SfcRegistration.Domains.GetDomainForNamespaceQualifier(domainPrefix).RootTypeFullName;
                ISfcDomainLite domain = SfcRegistration.CreateObject(domainName) as ISfcDomainLite;
                
                int domainVersion = domain.GetLogicalVersion();
                

                xmlReader.ReadToFollowing("sfc:version");
                int fileVersion = int.Parse(xmlReader.GetAttribute("DomainVersion"));
                UpgradeSession session = null;


                if (fileVersion > domainVersion) //cannot support downgrade
                {
                    throw new SfcUnsupportedVersionSerializationException(SfcStrings.SfcUnsupportedVersion);
                }
                else if (fileVersion < domainVersion)
                {
                    if (domain is ISfcSerializableUpgrade)
                    {
                        ISfcSerializableUpgrade upgradeDomain = domain as ISfcSerializableUpgrade;
                        session = upgradeDomain.StartSerializationUpgrade();
                    }
                    else
                    {
                        throw new SfcSerializationException(SfcStrings.SfcUnsupportedDomainUpgrade);
                    }
                }

                //Construct each instance
                xmlReader.ReadToFollowing("instances");

                while (xmlReader.ReadToFollowing("document"))
                {
                    xmlReader.ReadToFollowing("alias");
                    xmlReader.ReadStartElement();
                    String instanceUri = xmlReader.Value;

                    xmlReader.ReadToFollowing("data");

                    docStream = new StringReader(xmlReader.ReadInnerXml());
                    docReader = XmlReader.Create(docStream);

                    docReader.MoveToContent();
                    bool upgradeRequired = false;
                    if (fileVersion < domainVersion)
                    {
                        upgradeRequired = session.IsUpgradeRequiredOnType(docReader.LocalName, fileVersion);
                    }

                    object instance = null;
                    List<KeyValuePair<String, Object>> upgradedInstanceList = new List<KeyValuePair<string, object>>();

                    if (!upgradeRequired)
                    {
                        try
                        {
                            Deserialize(docReader, instanceUri, out instance, state);
                        }
                        catch (SfcSerializationException)
                        {
                            // do not wrap our own exception
                            throw;
                        }
                        catch (Exception e)
                        {
                            //cannot use instance.ToString() for instance information since instance will most
                            //probably be null. So, using instanceUri in the exception message instead.
                            throw new SfcSerializationException(SfcStrings.SfcInvalidDeserializationInstance(instanceUri), e);
                        }
                    }
                    else
                    {
                        List<SfcInstanceSerializedData> serializedData = new List<SfcInstanceSerializedData>();
                        ParseXmlData(docReader, ref serializedData, true);

                        upgradedInstanceList = session.UpgradeInstance(
                            serializedData,
                            fileVersion,
                            instanceUri,
                            cache.Instances);

                        if (upgradedInstanceList != null) //Sometimes, an upgrade can result in deletion
                        {
                            foreach (KeyValuePair<String, Object> upgradedInstance in upgradedInstanceList)
                            {
                                cache.Add(upgradedInstance.Key,
                                    upgradedInstance.Value,
                                    OnCollision.Fail);
                            }
                        }
                    }

                    if (!isFirst)
                    {
                        isFirst = true;
                        if (upgradeRequired)
                        {
                            rootInstance = upgradedInstanceList[0].Value;
                            rootUri = upgradedInstanceList[0].Key;
                        }
                        else
                        {
                            rootInstance = instance;
                            rootUri = instanceUri;
                        }
                    }
                    docReader.Close();
                }
                xmlReader.Close();

                if ((fileVersion < domainVersion)
                    && (session != null))
                {
                    session.PostProcessUpgrade(cache.Instances, fileVersion);
                }
            }
            catch (SfcSerializationException)
            {
                // do not wrap our own exception
                throw;
            }
            catch (Exception e)
            {
                throw new SfcSerializationException(SfcStrings.SfcInvalidDeserialization, e);
            }

            //create the internal hierarchy in the cache
            cache.CreateHierarchy(rootInstance, rootUri, UnParentedReferences);

            return rootInstance;
        }

        /// <summary>
        /// Deserializes each instance and adds to the internal cache
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="instanceUri"></param>
        /// <param name="instance"></param>
        /// <param name="state"></param>
        private void Deserialize(XmlReader reader, String instanceUri, out object instance, SfcObjectState state)
        {
            if (!reader.IsStartElement())
            {
                throw new XmlException(); //need to create serialization exception messages
            }

            String domainQualifierName = SfcRegistration.Domains.
                GetDomainForNamespaceQualifier(reader.Prefix).DomainNamespace + ".";
            Type instanceType = SfcRegistration.GetObjectTypeFromFullName(domainQualifierName + reader.LocalName);

            List<SfcInstanceSerializedData> serializedData = new List<SfcInstanceSerializedData>();
            ParseXmlData(reader, ref serializedData , false);
            instance = CreateInstanceFromSerializedData(instanceType, instanceUri, serializedData);
            if (instance is SfcInstance)
            {
                ((SfcInstance)instance).State = state;
            }
            else if (instance is IAlienObject)
            {
                ((IAlienObject)instance).SetObjectState(state);
            }

            //Add this element to the inner Cache - to bring the instance into the web of Object model
            cache.Add(instanceUri, instance, OnCollision.Fail);
        }

        /// <summary>
        /// This method reads the whole IF document and either fills data into the instance
        /// or creates the serialized data bag needed for the domain to upgrade the instance.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="serializedData"></param>
        /// <param name="isUpgrade"></param>
        private void ParseXmlData(XmlReader reader,
            ref List<SfcInstanceSerializedData> serializedData,
            bool isUpgrade)
        {
            reader.ReadStartElement(); //rip off the instance data tag
            while (reader.IsStartElement())
            {
                string nodeName = reader.LocalName;
                String typeTag = null;
                bool isProperty = false;
                bool isEmptyNode = false;

                if (reader.AttributeCount > 0)
                {
                    typeTag = reader.GetAttribute("type");
                    isProperty = true;

                    if (reader.IsEmptyElement)
                    {
                        isEmptyNode = true;
                    }
                }
                reader.ReadStartElement();

                if (nodeName == "Parent")
                {
                    //Handle Parent specially
                    reader.ReadToFollowing("sml:Uri");
                    object smlUri = reader.ReadElementContentAsObject();
                    reader.ReadEndElement(); //reference
                    reader.ReadEndElement(); //end-parent tag

                    SfcInstanceSerializedData parentRow = new SfcInstanceSerializedData(
                        SfcSerializedTypes.Parent, nodeName, nodeName, smlUri);
                    
                    serializedData.Add(parentRow);
                }
                else if (reader.IsStartElement()) //if top node is not parent and yet a start tag - ref/colln
                {
                    if (reader.LocalName == "Collection")
                    {
                        XmlReader collectionTree = reader.ReadSubtree();
                        List<String> collectionUriList = new List<string>();

                        while (collectionTree.ReadToFollowing("sml:Uri"))
                        {
                            object smlUri = reader.ReadElementContentAsObject();
                            collectionUriList.Add(smlUri.ToString());
                        }

                        SfcInstanceSerializedData collectionRow = new SfcInstanceSerializedData(
                            SfcSerializedTypes.Collection, nodeName, nodeName, collectionUriList);
                        serializedData.Add(collectionRow);

                        if (!reader.IsEmptyElement)
                        {
                            reader.ReadEndElement(); //collection
                        }
                        else
                        {
                            reader.ReadStartElement(); //Read past empty element
                        }
                        reader.ReadEndElement(); //end-node tag

                    }
                    else if (reader.LocalName == "Reference")
                    {
                        reader.ReadToFollowing("sml:Uri");
                        object smlUri = reader.ReadElementContentAsObject();
                        reader.ReadEndElement(); //reference
                        reader.ReadEndElement(); //end-parent tag

                        SfcInstanceSerializedData referenceRow = new SfcInstanceSerializedData(
                            SfcSerializedTypes.Reference, nodeName, nodeName, smlUri);

                        serializedData.Add(referenceRow);
                    }
                    else
                    {
                        isProperty = true;
                    }
                }

                if (isProperty)
                {
                    object value = SfcUtility.GetXmlContent(reader, typeTag, isEmptyNode);
                    SfcInstanceSerializedData propertyRow = new SfcInstanceSerializedData(
                        SfcSerializedTypes.Property, nodeName, typeTag, value);

                    serializedData.Add(propertyRow);

                    if (!isEmptyNode)
                    {
                        reader.ReadEndElement();
                    }
                }
            }

        }

        /// <summary>
        /// Creates object of appropriate type and state based on deserialized data.
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="instanceUri"></param>
        /// <param name="serializedData"></param>
        /// <returns></returns>
        internal object CreateInstanceFromSerializedData(Type instanceType, string instanceUri, List<SfcInstanceSerializedData> serializedData)    
        {
            object instance = SfcRegistration.CreateObject(instanceType.FullName);

            if (instance == null)
            {
                throw new SfcSerializationException(
                    SfcStrings.SfcInvalidSerializationInstance(instanceType.Name));
            }

            //for non-sfc domain root (SMO), we force disconnect. Otherwise, because the default
            //mode is not offline, at least now, root may attempt to connect to backend.
            //
            //$TODO: See VSTS 245031. This needs reconsideration.
            if (instance is IAlienRoot)
            {
                ((IAlienRoot)instance).DesignModeInitialize();
            }

            //Serializer always creates all objects and sets the links (parent/child) at the end.
            //But SMO objects require parent property to be set even before their state can be populated.
            //So, for SMO, we set parent property if it is available
            if(!string.IsNullOrEmpty(instanceUri) && instance is IAlienObject)
            {
                this.SetParent(instance as IAlienObject, instanceUri);
            }

            SfcMetadataDiscovery metadataDiscoverer = new SfcMetadataDiscovery(instanceType);

            foreach (SfcInstanceSerializedData serializedRow in serializedData)
            {
                if (serializedRow.SerializedType == SfcSerializedTypes.Property)
                {
                    //first get an instance of adapter if an adapter was specified for serializing this sfcproperty.

                    SfcMetadataRelation propertyMetadata = null;
                    foreach (SfcMetadataRelation relation in metadataDiscoverer.Properties)
                    {
                        if (relation.PropertyName.Equals(serializedRow.Name, StringComparison.Ordinal))
                        {
                            propertyMetadata = relation;
                            break;
                        }
                    }

                    if (propertyMetadata == null)
                    {
                        //property metadata for this property should exist, since that was used
                        //to serialize this property in the first place.
                        throw new SfcNonSerializablePropertyException(
                                SfcStrings.SfcNonSerializableProperty(serializedRow.Name));
                    }

                    //adapter could be null if it wasn't specified. Rest of the code would detect this and use XmlSerializer
                    //as default.
                    IXmlSerializationAdapter serializationAdapter = SfcSerializer.GetSerializationAdapter(propertyMetadata);
  

                    if (instance is SfcInstance)
                    {
                        SfcInstance sfcInstance = instance as SfcInstance;
                        int propIndex = sfcInstance.Properties.LookupID(serializedRow.Name);
                        
                        if (propIndex < 0)
                        {
                            throw new SfcNonSerializablePropertyException(
                            SfcStrings.SfcNonSerializableProperty(serializedRow.Name));
                        }

                        SfcProperty sfcProperty = sfcInstance.Properties[serializedRow.Name];
                        sfcProperty.Value = SfcSerializer.GetPropertyValueFromXmlString(serializedRow.Value.ToString(), 
                                                                                        sfcProperty.Type,
                                                                                        serializationAdapter);
                        sfcProperty.Retrieved = true;
                    }
                    else if (instance is IAlienObject)
                    {
                        IAlienObject alienObject = instance as IAlienObject;

                        try
                        {
                            Type propType = alienObject.GetPropertyType(serializedRow.Name);
                            object value = SfcSerializer.GetPropertyValueFromXmlString(serializedRow.Value.ToString(),
                                                                                       propType,
                                                                                       serializationAdapter);

                            alienObject.SetPropertyValue(serializedRow.Name, propType, value);
                        }
                        catch (TargetInvocationException tie)
                        {
                            // there are special IsXYZSupported properties that can be get/set in
                            // newer verision. But, it can only get and not set. In thise case,
                            // they throw PropertyNotAvailableException, and we handle it peacefully.
                            if (tie.InnerException.GetType() == typeof(PropertyNotAvailableException))
                            {
                                continue;
                            }

                            throw;
                        }
                        catch (Exception e)
                        {
                            throw new SfcNonSerializablePropertyException(
                            SfcStrings.SfcNonSerializableProperty(serializedRow.Name), e);
                        }
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// Creates .net object from value in xml format.
        /// </summary>
        /// <param name="xmlString"></param>
        /// <param name="propType"></param>
        /// <param name="serializationAdapter"></param>
        /// <returns></returns>
        private static object GetPropertyValueFromXmlString(string xmlString, Type propType, IXmlSerializationAdapter serializationAdapter)
        {
            object value;

            //if adapter is given, use that to deserialize. Otherwise, default to XmlSerializer.
            if (serializationAdapter != null)
            {
                XmlReader xmlReader = XmlReader.Create(new StringReader(xmlString));
                serializationAdapter.ReadXml(xmlReader, out value);
            }
            else
            {
                value = SfcUtility.GetXmlValue(xmlString, propType);
                if (value.GetType().Equals(typeof(String)))
                {
                    value = SfcSecureString.XmlUnEscape(value.ToString());
                }
            }

            return value;
        }

        /// <summary>
        /// Sets parent property on the object if we have already deserialized the parent and it lives in cache.
        /// 
        /// Throw SfcSerializationException if parent is missing.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="instanceUri"></param>
        private void SetParent(IAlienObject instance, string instanceUri)
        {
            //Get parent's uri
            
            //skip the first '/'. Otherwise first fragment will always be '/Server', instead of 'Server'
            List<string> fragments = SfcCache.GetSmlFragments(instanceUri.Substring(1), false);

            //sml uri is always of the form /type/name/type/name.. where the last type/name pair represents leaf node
            //For singleton objects, uri will have type but no name. So, we deduce parent uri by checking last fragment. If
            //it is type, we have singleton, so drop only last fragment, otherwise the last 2.
            //
            //There is caveat with type.  We have .Net type from the object, but URN type name may differ, so we need the mapping.
            //Fortunately metadata discovery provides it.
            SfcMetadataDiscovery metadataDiscovery = new SfcMetadataDiscovery(instance.GetType());
            int fragmentsToDiscard = (fragments[fragments.Count - 1] == metadataDiscovery.ElementTypeName ) ? 1 : 2;
            
            StringBuilder parentUriBuilder = new StringBuilder();
            for(int i = 0; i < (fragments.Count - fragmentsToDiscard); i++)
            {
                parentUriBuilder.Append("/");
                parentUriBuilder.Append(fragments[i]);
            }

            //use parent URI to see if sfccache has seen the parent instance. If yes, we can set the parent property.
            //Cannot use IAlienObject's SetPropertyValue interface since it may internally attempt an operation that
            //requires parent before setting the parent (currently that is the case)
            string parentUri = parentUriBuilder.ToString();
            object parent = null;
            if (!String.IsNullOrEmpty(parentUri))
            {
                if (this.cache.Instances.TryGetValue(parentUri, out parent))
                {
                    PropertyInfo propInfo = instance.GetType().GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public);
                    if (propInfo != null && propInfo.CanWrite)
                    {
                        propInfo.SetValue(instance, parent, null);
                    }
                }
                else
                {
                    throw new SfcSerializationException(SfcStrings.SfcInvalidDeserializationMissingParent(instanceUri, parentUri));
                }
            }
        }

        /// <summary>
        /// Helper method to extract the serialization adapter for a property if it was specified (as an attribute).
        /// </summary>
        /// <param name="propertyRelation">Metadata record for the property to be serialized</param>
        /// <returns>adapter object or null if no adapter type was specified</returns>
        private static IXmlSerializationAdapter GetSerializationAdapter(SfcMetadataRelation propertyRelation)
        {
            TraceHelper.Assert(propertyRelation != null);

            //create the serialization adapter for this property if it is specified
            IXmlSerializationAdapter serializationAdapter = null;

            //check if adapter attribute is specified. we know there is only one. Will get default value if there is none.
            Attribute attribute = propertyRelation.RelationshipAttributes[typeof(SfcSerializationAdapterAttribute)];

            if (attribute != null)
            {
                TraceHelper.Assert(attribute is SfcSerializationAdapterAttribute);

                SfcSerializationAdapterAttribute adapterAttribute = attribute as SfcSerializationAdapterAttribute;
                try
                {
                    object adapter = Activator.CreateInstance(adapterAttribute.SfcSerializationAdapterType);
                    serializationAdapter = adapter as IXmlSerializationAdapter;

                    //the given adapter should implement IXmlSerializationAdapter
                    TraceHelper.Assert(serializationAdapter != null, "Specified serialization adapter of type " + adapterAttribute.SfcSerializationAdapterType.Name
                                                                + " does not implement IXmlSerializationAdapter");
                }
                catch (System.MissingMethodException mme)
                {
                    //adapter should allow construction using default constructor, but here it doesn't.
                    TraceHelper.Assert(false, "Serialization adapter is specified but cannot be constructed using default constructor. Caught exception: " + mme.Message);
                }
            }
            

            return serializationAdapter;
        }

        #endregion
    }

}

