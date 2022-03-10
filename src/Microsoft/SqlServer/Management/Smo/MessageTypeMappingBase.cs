// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Broker
{
    public partial class MessageTypeMapping : NamedSmoObject, Cmn.IMarkForDrop
    {
        internal MessageTypeMapping(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {

        }

        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "MessageTypeMapping";
            }
        }

        /// <summary>
        /// Name of message type mapping
        /// </summary>
        [SfcKey(0)]
        [SfcReference(typeof(MessageType), "Server[@Name = '{0}']/Database[@Name = '{1}']/ServiceBroker/MessageType[@Name='{2}']", "Parent.Parent.Parent.Parent.ConnectionContext.TrueName", "Parent.Parent.Parent.Name", "Name")]
        [CLSCompliant(false)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server.
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
            Cmn.ServerVersion version,
            Cmn.DatabaseEngineType databaseEngineType,
            Cmn.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            string[] fields =
            {
                nameof(MessageTypeMapping.MessageSource)
            };
            var list = GetSupportedScriptFields(typeof(MessageTypeMapping.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}

