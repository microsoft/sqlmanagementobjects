// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// The ordering used to sort objects when scripting.
    /// </summary>
    internal enum ObjectOrder
    {
        uninitialized = -1,
        unresolvedentity = 0,
        server,
        settings,
        oledbprovidersettings,
        useroptions,
        filestreamsettings,
        fulltextservice,
        cryptographicprovider = 11,
        credential,
        database,
        databasescopedcredential,
        login,
        masterassembly,
        mastercertificate,
        masterasymmetrickey,
        certificatekeylogin,
        roleserver,
        serverpermission,
        serverassociation,
        serverownership,
        linkedserver,
        audit = 31,
        userdefinedmessage,
        httpendpoint,
        endpoint,
        databaseencryptionkey = 41,
        masterkey,
        applicationrole,
        user,        
        userassembly,
        usercertificate,
        userasymmetrickey,
        certificatekeyuser,
        roledatabase,
        databasepermission,
        databaseassociation,
        databaseownership,
        sqlassembly = 61,
        externalLanguage,
        externalLibrary,
        asymmetrickey,
        certificate,
        symmetrickeys,
        schema,
        defaultdatabase,
        fulltextcatalog,
        fulltextstoplist,
        searchpropertylist,
        searchproperty,
        partitionfunction,
        partitionscheme,
        rule,
        xmlschemacollection,
        userdefineddatatype,
        userdefinedtype,
        sequence,
        userdefinedtabletype,
        userdefinedaggregate,
        storedprocedure,
        servicebroker,
        messagetype,
        servicecontract,
        servicequeue,
        brokerservice,
        serviceroute,
        remoteservicebinding,
        brokerpriority,
        synonym,
        scalarudf =101,
        regulartable,
        userdefinedfunction,
        externaldatasource,
        externalfileformat,
        externalStream,
        externalStreamingJob,
        columnmasterkey,
        columnencryptionkey,
        columnencryptionkeyvalue,
        table,
        view,
        tableviewudf,
        creatingudf,
        creatingtable,
        creatingview,
        securitypolicy,
        securitypredicate,
        clusteredindex = 120,
        data,
        nonclusteredindex,
        columnstoreindex,
        clusteredcolumnstoreindex,
        primaryxmlindex,
        secondaryxmlindex,
        selectivexmlindex,
        secondaryselectivexmlindex,
        index,
        fulltextindex,
        defaultcolumn,
        foreignkey,
        check,        
        creatingsproc,
        nonschemaboundsproc,
        trigger,
        statistic,
        planguide = 140,
        databaseauditspecification,
        ddltriggerdatabase,
        ddltriggerdatabaseenable,
        ddltriggerdatabasedisable,
        extendedproperty,
        resourcepool = 160,
        externalresourcepool,
        workloadgroup,
        workloadmanagementworkloadclassifier,
        workloadmanagementworkloadgroup,
        resourcegovernor,
        mail =170,
        mailprofile,
        mailaccount,
        mailserver,
        configurationvalue = 180,
        job,
        step,
        @operator,
        operatorcategory,
        jobcategory,
        alertcategory,
        schedule,
        targetservergroup,
        alert,
        backupdevice,
        proxyaccount,
        jobserver,
        alertsystem,
        serverauditspecification =250,
        ddltriggerserver,
        ddltriggerserverenable,
        ddltriggerserverdisable,
        availabilitygroup = 260,
        availabilityreplica,
        availabilitydatabase,
        availabilitygrouplistener,
        availabilitygrouplisteneripaddress,
        querystoreoptions,
        databasescopedconfiguration,
        resumableindex,
        edgeconstraint,
        spatialindex,
        databasereadonly,
        @default =999
    }

    /// <summary>
    /// Class to uniquely identify a object's type through urn
    /// </summary>
    internal class UrnTypeKey : IComparable
    {
        string uniqueUrnType;

        ObjectOrder createOrder = ObjectOrder.uninitialized;

        internal ObjectOrder CreateOrder 
        {
            get
            {
                if(createOrder.Equals(ObjectOrder.uninitialized))
                {
                    createOrder = this.SetCreateOrder();
                }
                return createOrder;
            }
        }

        /// <summary>
        /// Sets the enum value for the UrnType of this key - this value is used for ordering the objects
        /// when scripting.
        /// </summary>
        /// <returns></returns>
        /// <remarks>If a URN does not have a specific order mapping then it will be given the default
        /// value which will mean the ordering is not deterministic. Because of this the normal case for
        /// objects sorted by this key should be to have a specific ordering to avoid inconsistencies in
        /// the generated scripts.</remarks>
        private ObjectOrder SetCreateOrder()
        {
            ObjectOrder objectOrder;
            if (!Enum.TryParse(this.uniqueUrnType, true, out objectOrder))
            {
                objectOrder = ObjectOrder.@default;
            }
            return objectOrder;
        }

        /// <summary>
        /// Initialize by getting urn type from a urn
        /// </summary>
        /// <param name="urn"></param>
        public UrnTypeKey(Urn urn)
        {
            //check for null and throw error

            uniqueUrnType = GetUniqueUrnType(urn);
        }

        /// <summary>
        /// Get unique type
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        private string GetUniqueUrnType(Urn urn)
        {
            try
            {
                string urnType = urn.Type;
                string parentType = (urn.Parent != null) ? urn.Parent.Type : string.Empty;
                return this.GetUniqueSmoType(urnType, parentType); 
            }
            catch (Exception ex)
            {
                throw new SmoException("Invalid Urn",ex);
            }
        }

        /// <summary>
        /// Initialize using  a urn type key
        /// </summary>
        /// <param name="urnTypeKey"></param>
        public UrnTypeKey(string urnTypeKey)
        {
            //check for null and throw error
            uniqueUrnType = urnTypeKey.ToLowerInvariant();
        }

        /// <summary>
        /// Initialize using urn type and urn's parent type
        /// </summary>
        /// <param name="urnType"></param>
        /// <param name="parentUrnType"></param>
        public UrnTypeKey(string urnType,string parentUrnType)
        {
            uniqueUrnType = this.GetUniqueSmoType(urnType, parentUrnType); 
        }

        /// <summary>
        /// Resolve issues with same urn type
        /// </summary>
        /// <param name="urnType"></param>
        /// <param name="parentUrnType"></param>
        /// <returns></returns>
        private string GetUniqueSmoType(string urnType, string parentUrnType)
        {
            switch (urnType.ToLowerInvariant())
            {
                case "default":
                case "ddltrigger":
                case "role": return string.Concat(urnType.ToLowerInvariant(), parentUrnType.ToLowerInvariant());
                default:
                    return urnType.ToLowerInvariant();                   
            }
        }

        public override int GetHashCode()
        {
            return uniqueUrnType.GetHashCode();
        }

        public override string ToString()
        {
            return uniqueUrnType;
        }

        public override bool Equals(Object obj)
        {
            if (obj is UrnTypeKey key)
            {
                return this.uniqueUrnType.Equals(key.uniqueUrnType, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return false;
            }
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is UrnTypeKey urnTypeKey)
            {
                if (this.CreateOrder == urnTypeKey.CreateOrder)
                {
                    // If they have the same CreateOrder (same type) then sort them by the type name instead. This only matters for
                    // default types which didn't have a mapping and is primarily so we keep a consistent order when ordering the URNs
                    // even for those that didn't have a mapping created.
                    return String.Compare(this.uniqueUrnType, urnTypeKey.uniqueUrnType, StringComparison.InvariantCultureIgnoreCase);
                }
                return this.CreateOrder.CompareTo(urnTypeKey.CreateOrder);
            }
            else
            {
                throw new ArgumentException("Object is not a UrnTypeKey.");

            }
        }

        #endregion
    }

    /// <summary>
    /// This class implements Urn type based filtering
    /// </summary>
    internal class SmoUrnFilter : ISmoFilter
    {
        /// <summary>
        /// Server for the urns provided to be filtered
        /// </summary>
        public Server Server { get; set; }

        /// <summary>
        /// Collection of all the filtered urn types
        /// </summary>
        internal HashSet<UrnTypeKey> filteredTypes;

        /// <summary>
        /// Filter the list of input urns which are in filtered urn types
        /// </summary>
        /// <param name="urns">input urns</param>
        /// <returns></returns>
        public IEnumerable<Urn> Filter(IEnumerable<Urn> urns)
        {
            if (filteredTypes.Count == 0)
            {
                return urns;
            }

            List<Urn> filteredUrns = new List<Urn>();

            foreach (Urn item in urns)
            {
                if (!filteredTypes.Contains(new UrnTypeKey(item)))
                {
                    filteredUrns.Add(item);
                }

            }
            return filteredUrns;
        }

        /// <summary>
        /// Initialize SmoUrnFilter
        /// </summary>
        /// <param name="srv">Server for the urns to be filtered</param>
        public SmoUrnFilter(Server srv)
        {
            Server = srv;
            filteredTypes = new HashSet<UrnTypeKey>();
        }

        /// <summary>
        /// Add a urn type to filtering list
        /// </summary>
        /// <param name="urnType">urn type </param>
        /// <param name="parentType">urn type's parent</param>
        public void AddFilteredType(string urnType, string parentType)
        {
            UrnTypeKey urnTypeKey = new UrnTypeKey(urnType,parentType);

            if (!filteredTypes.Contains(urnTypeKey))
            {           
                filteredTypes.Add(urnTypeKey);
            }
        }

        /// <summary>
        /// Remove a urn type from filtering list
        /// </summary>
        /// <param name="urnType">urn type</param>
        /// <param name="parentType">urn type's parent</param>
        public void RemoveFilteredType(string urnType, string parentType)
        {
            UrnTypeKey urnTypeKey = new UrnTypeKey(urnType, parentType);

            if (filteredTypes.Contains(urnTypeKey))
            {
                filteredTypes.Remove(urnTypeKey);
            }
        }

    }

    /// <summary>
    /// Class to bucketize indexes based on their types and clustering
    /// </summary>
    internal class IndexPropagateInfo
    {
        IndexCollection Indexes;

        Index clusteredPrimaryKey;
        Index nonclusteredPrimaryKey;
        List<Index> clusteredUniqueKeys;
        List<Index> nonclusteredUniqueKeys;
        List<Index> clusteredIndexes;
        List<Index> nonclusteredIndexes;
        List<Index> xmlIndexes;
        List<Index> spatialIndexes;

        /// <summary>
        /// Initializes with given index collection
        /// </summary>
        /// <param name="indexCollection"></param>
        public IndexPropagateInfo(IndexCollection indexCollection)
        {
            this.Indexes = indexCollection;
        }        

        private void SetupIndexPropagation()
        {
            this.ResetIndexPropagation();
            if (this.Indexes != null)
            {
                foreach (Index index in this.Indexes)
                {

                    bool? indexIsClustered = index.GetPropValueOptional<bool>("IsClustered");
                    if (indexIsClustered.HasValue && indexIsClustered.Value)
                    {
                        this.CheckKeyAndAdd(index, ref this.clusteredPrimaryKey, this.clusteredUniqueKeys, this.clusteredIndexes);
                    }
                    else if (index.HasXmlColumn(true))
                    {
                        this.xmlIndexes.Add(index);
                    }
                    else if (index.HasSpatialColumn(true))
                    {
                        this.spatialIndexes.Add(index);
                    }
                    else
                    {
                        this.CheckKeyAndAdd(index, ref this.nonclusteredPrimaryKey, this.nonclusteredUniqueKeys, this.nonclusteredIndexes);
                    }
                }
            }
        }

        private void CheckKeyAndAdd(Index index,ref Index primaryKey, List<Index> uniqueKeys, List<Index> indexes)
        {
            Nullable<IndexKeyType> indexKeyType = index.GetPropValueOptional<IndexKeyType>("IndexKeyType");

            if (!indexKeyType.HasValue)
            {
                indexKeyType = IndexKeyType.None;
            }

            switch (indexKeyType)
            {
                case IndexKeyType.DriPrimaryKey:
                    primaryKey = index;
                    break;
                case IndexKeyType.DriUniqueKey:
                    uniqueKeys.Add(index);
                    break;
                case IndexKeyType.None:
                    indexes.Add(index);
                    break;
                default:
                    break;
            }
        }

        private void ResetIndexPropagation()
        {
            this.clusteredPrimaryKey = null;
            this.nonclusteredPrimaryKey = null;
            this.clusteredUniqueKeys = new List<Index>();
            this.nonclusteredUniqueKeys = new List<Index>();
            this.clusteredIndexes = new List<Index>();
            this.nonclusteredIndexes = new List<Index>();
            this.xmlIndexes = new List<Index>();
            this.spatialIndexes = new List<Index>();
        }

        /// <summary>
        /// Add various indexes to  Propagateinfo 
        /// </summary>
        /// <param name="propInfo"></param>
        public void PropagateInfo(ArrayList propInfo)
        {
            this.SetupIndexPropagation();

            propInfo.Add(new SqlSmoObject.PropagateInfo(this.clusteredPrimaryKey, true, "ClusteredPrimaryKey"));
            propInfo.Add(new SqlSmoObject.PropagateInfo(this.nonclusteredPrimaryKey, true, "NonclusteredPrimaryKey"));
            propInfo.Add(new SqlSmoObject.PropagateInfo(this.clusteredUniqueKeys, true, "ClusteredUniqueKey"));
            propInfo.Add(new SqlSmoObject.PropagateInfo(this.nonclusteredUniqueKeys, true, "NonclusteredUniqueKey"));


            propInfo.Add(new SqlSmoObject.PropagateInfo(this.clusteredIndexes, true, "ClusteredIndex"));
            propInfo.Add(new SqlSmoObject.PropagateInfo(this.nonclusteredIndexes, true, "NonclusteredIndex"));
            propInfo.Add(new SqlSmoObject.PropagateInfo(this.xmlIndexes, true, "XmlIndex"));
            propInfo.Add(new SqlSmoObject.PropagateInfo(this.spatialIndexes, true, "SpatialIndex"));
        }
    }
}
