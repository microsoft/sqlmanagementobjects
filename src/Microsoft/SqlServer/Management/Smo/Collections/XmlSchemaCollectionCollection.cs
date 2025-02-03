// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587

























namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed collection of MAPPED_TYPE objects
    /// Supports indexing objects by their Name and Schema properties
    ///</summary>
    public sealed class XmlSchemaCollectionCollection : SchemaCollectionBase
	{

		internal XmlSchemaCollectionCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}


		public XmlSchemaCollection this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as XmlSchemaCollection;
			}
		}

		public XmlSchemaCollection this[string name]
		{
			get
			{
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }

		        return GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema())) as XmlSchemaCollection;
			}
		}

		public XmlSchemaCollection this[string name, string schema]
		{
			get
			{
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }
                else if (schema == null)
                {
                    throw new ArgumentNullException("schema cannot be null");
                }

                return GetObjectByKey(new SchemaObjectKey(name, schema)) as XmlSchemaCollection;
			}
		}

		public void CopyTo(XmlSchemaCollection[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		public XmlSchemaCollection ItemById(int id)
		{
			return (XmlSchemaCollection)GetItemById(id);
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(XmlSchemaCollection);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new XmlSchemaCollection(this, key, state);
		}

		


































			public void Add(XmlSchemaCollection xmlSchemaCollection) 
			{
				if( null == xmlSchemaCollection )
					throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("xmlSchemaCollection"));
			
				AddImpl(xmlSchemaCollection);
			}

		internal SqlSmoObject GetObjectByName(string name)
		{
            if (name == null)
            {
                throw new ArgumentNullException("schema cannot be null");
            }

			return GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema()));
		}

	}
}
