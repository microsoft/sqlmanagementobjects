// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// This interface supports upgrading older version's serialized
    /// content to conform to the newer version
    /// </summary>
    public interface ISfcSerializableUpgrade
    {
        UpgradeSession StartSerializationUpgrade();
    }

    /// <summary>
    /// This struct is used to transfer serialized information of any
    /// sfc instance to its corresponding domain
    /// </summary>
    public struct SfcInstanceSerializedData
    {
        SfcSerializedTypes serializedType;
        String name;
        String type;
        Object value;

        public SfcInstanceSerializedData(SfcSerializedTypes serializedType, String name, String type, Object value)
        {
            this.serializedType = serializedType;
            this.name = name;
            this.type = type;
            this.value = value;
        }

        public SfcSerializedTypes SerializedType
        {
            get { return serializedType; }
        }

        public String Name
        {
            get { return name; }
        }

        public String Type
        {
            get { return type; }
        }

        public Object Value
        {
            get { return value; }
        }
    }

    /// <summary>
    /// This enum represents all possible SML nodes in
    /// serialized content of an sfc instance
    /// </summary>
    public enum SfcSerializedTypes
    {
        None,
        Property,
        Parent,
        Collection,
        Reference
    }

    /// <summary>
    /// This class supplies the information of each instance to the domain
    /// and seeks upgraded content, which conforms to the current version
    /// of an object model
    /// </summary>
    public class UpgradeSession
    {
        /// <summary>
        /// This function transfers the serialized information to the domain and 
        /// seeks the upgraded object instance
        /// 
        /// The output needs to be a list of pairs, of sml uri and corresponding instance.
        /// An upgrade can result in multiple instances and hence we expect a list.
        /// If the instance is deleted, a null can be returned.
        /// </summary>
        /// <param name="sfcInstanceData"></param>
        /// <param name="fileVersion"></param>
        /// <param name="smlUri"></param>
        /// <param name="sfcCache"></param>
        /// <returns></returns>
        public virtual List<KeyValuePair<String, object>> UpgradeInstance(
            List<SfcInstanceSerializedData> sfcInstanceData,
            int fileVersion,
            String smlUri,
            Dictionary<String, object> sfcCache)
        {
            return null;
        }


        /// <summary>
        /// This is a helper function for the domain to get instance from serialized instance data
        /// </summary>
        /// <param name="newInstanceType"></param>
        /// <param name="sfcInstanceData"></param>
        /// <returns></returns>
        public object UpgradeInstance(Type newInstanceType, List<SfcInstanceSerializedData> sfcInstanceData)
        {
            SfcSerializer upgradeSerializer = new SfcSerializer();
            //internally, we need instanceUri (2nd param) to support IAlienObject. For external clients, for now, instanceUri
            //is not needed, so instead of changing public interface, just passing dummy argument.
            return upgradeSerializer.CreateInstanceFromSerializedData(newInstanceType, string.Empty, sfcInstanceData);
        }

        /// <summary>
        /// The post process is an additional help to the domains to be able to adjust 
        /// any hierarchy, uri changes or any such overall changes. This function is 
        /// called once deserialization of each instance is done.
        /// </summary>
        /// <param name="sfcCache"></param>
        /// <param name="fileVersion"></param>
        public virtual void PostProcessUpgrade(Dictionary<String, object> sfcCache, int fileVersion)
        {
            return ;
        }

        /// <summary>
        /// SFC calls this function on the domain for each type (with version) before
        /// it would like to deserialize.
        /// 
        /// A false return value indicates a no-upgrade and hence SFC can deserialize the 
        /// content itself. If a true is returned, the upgrade sequence needs to be run.
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="fileVersion"></param>
        /// <returns></returns>
        public virtual bool IsUpgradeRequiredOnType(String instanceType, int fileVersion)
        {
            return false;
        }

        public UpgradeSession()
        {
        }
    }

}
