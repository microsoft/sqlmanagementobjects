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
    public sealed  class IndexedXmlPathCollection : ParameterCollectionBase 
	{
		internal IndexedXmlPathCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Index Parent
		{
			get
			{
				return this.ParentInstance as Index;
			}
		}


		public IndexedXmlPath this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as IndexedXmlPath;
			}
		}

		public void CopyTo(IndexedXmlPath[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public IndexedXmlPath this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as IndexedXmlPath;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(IndexedXmlPath);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new IndexedXmlPath(this, key, state);
		}


		public void Add(IndexedXmlPath indexedXmlPath)
		{
			AddImpl(indexedXmlPath);
		}

		public void Add(IndexedXmlPath indexedXmlPath, string insertAtColumnName)
		{
			AddImpl(indexedXmlPath, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(IndexedXmlPath indexedXmlPath, int insertAtPosition)
		{
			AddImpl(indexedXmlPath, insertAtPosition);
		}


		public void Remove(IndexedXmlPath indexedXmlPath)
		{
			if( null == indexedXmlPath )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("indexedXmlPath"));
			
			RemoveObj(indexedXmlPath, indexedXmlPath.key);
		}


























		public IndexedXmlPath ItemById(int id)
		{
			return (IndexedXmlPath)GetItemById(id);
		}

		
	}
}
