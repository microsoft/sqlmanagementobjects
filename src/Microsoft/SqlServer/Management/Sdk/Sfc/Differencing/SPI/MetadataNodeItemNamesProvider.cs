// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Sdk.Differencing.SPI
{
    /// <summary>
    /// An implementation of NodeItemNamesAdapterProvider that is based
    /// on SfcMetadataDiscovery.
    /// </summary>
    public class MetadataNodeItemNamesProvider : NodeItemNamesAdapterProvider
    {
        // code extracted and modified from Serializer.cs
        public override bool IsGraphSupported(ISfcSimpleNode source)
        {
            //$FUTURE: 3/20/09-tyip Do we have a reliable way to tell SfcMetadata is defined for some specific graph?
            if (source.ObjectReference is IAlienObject)
            {
                return true;
            }
            else if (source.ObjectReference is SfcInstance)
            {
                return true;
            }
            return false;
        }

        public override bool IsContainerInNatrualOrder(ISfcSimpleNode node, string name)
        {
            SfcMetadataDiscovery metaData = new SfcMetadataDiscovery(node.ObjectReference.GetType());
            foreach (SfcMetadataRelation relation in metaData.Relations)
            {
                if (!name.Equals(relation.PropertyName))
                {
                    continue;
                }

                foreach (Attribute attribute in relation.RelationshipAttributes)
                {
                    SfcObjectAttribute sfcAttribute = attribute as SfcObjectAttribute;
                    if (sfcAttribute != null && sfcAttribute.NaturalOrder)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override IEnumerable<string> GetRelatedContainerNames(ISfcSimpleNode source)
        {
            List<string> result = new List<string>();

            SfcMetadataDiscovery metaData = new SfcMetadataDiscovery(source.ObjectReference.GetType());
            // code extracted from Serializer.cs
            foreach (SfcMetadataRelation relation in metaData.Relations)
            {
                switch (relation.Relationship)
                {
                    case SfcRelationship.ChildContainer: // container for children that is weak entity
                    case SfcRelationship.ObjectContainer: // container for children that is not weak entity
                        foreach (Attribute attribute in relation.RelationshipAttributes)
                        {
                            SfcObjectAttribute sfcAttribute = attribute as SfcObjectAttribute;
                            if (sfcAttribute != null && (sfcAttribute.Deploy || sfcAttribute.Design))
                            {
                                result.Add(relation.PropertyName);
                                break;
                            }
                        }
                        break;
                }
            }
            return result;
        }

        public override IEnumerable<string> GetRelatedObjectNames(ISfcSimpleNode source)
        {
            List<string> result = new List<string>();

            SfcMetadataDiscovery metaData = new SfcMetadataDiscovery(source.ObjectReference.GetType());
            // code extracted from Serializer.cs
            foreach (SfcMetadataRelation relation in metaData.Relations)
            {
                switch (relation.Relationship)
                {
                    case SfcRelationship.ChildObject: // container for children that is weak entity
                    case SfcRelationship.Object: // container for children that is not weak entity
                        foreach (Attribute attribute in relation.RelationshipAttributes)
                        {
                            SfcObjectAttribute sfcAttribute = attribute as SfcObjectAttribute;
                            if (sfcAttribute != null && (sfcAttribute.Deploy || sfcAttribute.Design))
                            {
                                result.Add(relation.PropertyName);
                                break;
                            }
                        }
                        break;
                }
            }
            return result;
        }

        public override IEnumerable<string> GetPropertyNames(ISfcSimpleNode source)
        {
            List<string> result = new List<string>();

            SfcMetadataDiscovery metaData = new SfcMetadataDiscovery(source.ObjectReference.GetType());
            // code extracted from Serializer.cs
            List<SfcMetadataRelation> metaProperties = metaData.Properties;
            foreach (SfcMetadataRelation relation in metaProperties)
            {
                foreach (Attribute attribute in relation.RelationshipAttributes)
                {
                    SfcPropertyAttribute sfcAttribute = attribute as SfcPropertyAttribute;
                    if (sfcAttribute != null && (sfcAttribute.Deploy || sfcAttribute.Design))
                    {
                        result.Add(relation.PropertyName);
                    }
                }
            }
            return result;
        }

    }
}
