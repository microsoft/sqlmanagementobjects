// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;

namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of UserPermission objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    internal sealed  class UserPermissionCollection : SimpleObjectCollectionBase
	{

		internal UserPermissionCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}

		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}

		public UserPermission this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as UserPermission;
			}
		}

		// returns wrapper class
		public UserPermission this[string name]
		{
			get
			{
				return  GetObjectByName(name) as UserPermission;
			}
		}

		public void CopyTo(UserPermission[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public UserPermission ItemById(int id)
		{
			return (UserPermission)GetItemById(id);
		}

		protected override Type GetCollectionElementType()
		{
			return typeof(UserPermission);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new UserPermission(this, key, state);
		}

		internal SqlSmoObject GetObjectByName(string name)
		{
			return GetObjectByKey(new SimpleObjectKey(name));
		}
	}
}
