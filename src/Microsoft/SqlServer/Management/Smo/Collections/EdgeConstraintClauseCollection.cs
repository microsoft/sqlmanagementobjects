// Copyright (c) Microsoft.
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
    public sealed  class EdgeConstraintClauseCollection : SimpleObjectCollectionBase
	{


















		internal EdgeConstraintClauseCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}

		
		public EdgeConstraintClause this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as EdgeConstraintClause;
			}
		}


		// returns wrapper class
		public EdgeConstraintClause this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as EdgeConstraintClause;
                    
                



















			}
		}


		public void CopyTo(EdgeConstraintClause[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public EdgeConstraintClause ItemById(int id)
		{
			return (EdgeConstraintClause)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(EdgeConstraintClause);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new EdgeConstraintClause(this, key, state);
		}




		public void Remove(EdgeConstraintClause edgeconstraintclause)
		{
			if( null == edgeconstraintclause )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("edgeconstraintclause"));
			
			RemoveObj(edgeconstraintclause, new SimpleObjectKey(edgeconstraintclause.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(EdgeConstraintClause edgeconstraintclause) 
		{
			AddImpl(edgeconstraintclause);
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
