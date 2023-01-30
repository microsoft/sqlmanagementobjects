// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587

























namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed class SoapPayloadMethodCollection : SoapMethodCollectionBase
	{

		internal SoapPayloadMethodCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public SoapPayload Parent
		{
			get
			{
				return this.ParentInstance as SoapPayload;
			}
		}

		
		public SoapPayloadMethod this[string name]
		{
			get
			{ 
				return GetObjectByKey(new SoapMethodKey(name, SoapMethodCollectionBase.GetDefaultNamespace())) as SoapPayloadMethod;
			}
		}

		// returns wrapper class
		public SoapPayloadMethod this[string name, string methodNamespace]
		{
			get
			{
				return  GetObjectByKey(new SoapMethodKey(name, methodNamespace)) as SoapPayloadMethod;
			}
		}

		public SoapPayloadMethod this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as SoapPayloadMethod;
			}
		}

		protected override Type GetCollectionElementType()
		{
			return typeof(SoapPayloadMethod);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new SoapPayloadMethod(this, key, state);
		}

		public void CopyTo(SoapPayloadMethod[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		public void Remove(SoapPayloadMethod soapMethod)
		{
			if( null == soapMethod )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("soapMethod"));
			
			RemoveObj(soapMethod, soapMethod.key);
		}

		public void Add(SoapPayloadMethod soapMethod) 
		{
			AddImpl(soapMethod);
		}



	}
}

