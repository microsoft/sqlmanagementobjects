// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo.Broker
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class ServiceContractMappingCollection : SimpleObjectCollectionBase
	{


















		internal ServiceContractMappingCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public BrokerService Parent
		{
			get
			{
				return this.ParentInstance as BrokerService;
			}
		}

		
		public ServiceContractMapping this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ServiceContractMapping;
			}
		}


		// returns wrapper class
		public ServiceContractMapping this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ServiceContractMapping;
                    
                



















			}
		}


		public void CopyTo(ServiceContractMapping[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		







































		protected override Type GetCollectionElementType()
		{
			return typeof(ServiceContractMapping);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ServiceContractMapping(this, key, state);
		}




		public void Remove(ServiceContractMapping serviceContractMapping)
		{
			if( null == serviceContractMapping )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("serviceContractMapping"));
			
			RemoveObj(serviceContractMapping, new SimpleObjectKey(serviceContractMapping.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(ServiceContractMapping serviceContractMapping) 
		{
			AddImpl(serviceContractMapping);
		}


		internal SqlSmoObject GetObjectByName(string name)
		{
			return GetObjectByKey(new SimpleObjectKey(name));
		}


		internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
		{ 
			string name = urn.GetAttribute("Name");



            if( null == name || name.Length == 0)

				throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            return new SimpleObjectKey(name);        
        }


















	}
}
