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
    public sealed  class NumberedStoredProcedureCollection : 



        NumberedObjectCollectionBase

	{

		internal NumberedStoredProcedureCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public StoredProcedure Parent
		{
			get
			{
				return this.ParentInstance as StoredProcedure;
			}
		}

		

















       

		public NumberedStoredProcedure this[Int32 index]
		{
			get
			{ 
			    return GetObjectByIndex(index) as NumberedStoredProcedure;
			}
		}

		protected override Type GetCollectionElementType()
		{
			return typeof(NumberedStoredProcedure);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new NumberedStoredProcedure(this, key, state);
		}


		public NumberedStoredProcedure GetProcedureByNumber(short number)
		{
			return GetObjectByKey(new NumberedObjectKey(number)) as NumberedStoredProcedure;
		}

        public void CopyTo(NumberedStoredProcedure[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

	}
}
