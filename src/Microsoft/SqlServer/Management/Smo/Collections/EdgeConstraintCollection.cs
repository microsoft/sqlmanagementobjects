// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class EdgeConstraintCollection : SimpleObjectCollectionBase
	{


















		internal EdgeConstraintCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}

		
		public EdgeConstraint this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as EdgeConstraint;
			}
		}


		// returns wrapper class
		public EdgeConstraint this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as EdgeConstraint;
                    
                



















			}
		}


		public void CopyTo(EdgeConstraint[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public EdgeConstraint ItemById(int id)
		{
			return (EdgeConstraint)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(EdgeConstraint);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new EdgeConstraint(this, key, state);
		}




		public void Remove(EdgeConstraint edgeconstraint)
		{
			if( null == edgeconstraint )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("edgeconstraint"));
			
			RemoveObj(edgeconstraint, new SimpleObjectKey(edgeconstraint.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(EdgeConstraint edgeconstraint) 
		{
			AddImpl(edgeconstraint);
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
