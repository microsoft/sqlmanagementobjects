// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    // this is the class that contains common features of all schema collection classes

    public class SchemaCollectionBase: SortedListCollectionBase
	{
		internal SchemaCollectionBase(SqlSmoObject parent) : base(parent)
		{
		}

		protected override void InitInnerCollection()
		{
			InternalStorage = new SmoSortedList(new SchemaObjectComparer(this.StringComparer));
		}
		
		internal void RemoveObject(string name, string schema)
		{
			InternalStorage.Remove(new SchemaObjectKey(name, schema));
		}

		public bool Contains(String name) 
		{
			return this.Contains(new SchemaObjectKey(name, GetDefaultSchema()));
		}

		public bool Contains(String name, String schema) 
		{
			return this.Contains(new SchemaObjectKey(name, schema));
		}

		internal virtual String GetDefaultSchema()
		{
			SqlSmoObject db = null;

			if( ParentInstance is Database )
			{	
				//level 1
				db = ParentInstance;
			}
			else if (ParentInstance is Broker.ServiceBroker)
			{
				//Service Broker is special - it is not in the collection.
				db = ((Broker.ServiceBroker)ParentInstance).Parent;
			}
			else if( ParentInstance.ParentColl.ParentInstance is Database )
            {
                db = ParentInstance.ParentColl.ParentInstance;
            }
            else
            {
                db = ParentInstance.ParentColl.ParentInstance.ParentColl.ParentInstance;
            }

            // if the object is just creating or pending, then we can assume that the schema is going to be 
            // dbo, since the user is the one that creates the database. This does not work in some 
            // edge cases, but they are not a big user problem.
            if ( db.State == SqlSmoState.Creating || db.IsDesignMode)
            {
                return "dbo";
            }
            else
            {
                return db.Properties["DefaultSchema"].Value as string;
            }
        }

		internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
		{ 
			string name = urn.GetAttribute("Name");
			if( null == name || (name.Length == 0 && !CanHaveEmptyName(urn)))
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            }

            string schema = urn.GetAttribute("Schema");
			if( null == schema || schema.Length == 0 )
            {
                schema = GetDefaultSchema();
            }

            return new SchemaObjectKey(name, schema);
		}
		
	}
	
	internal class SchemaObjectComparer : ObjectComparerBase
	{
		internal SchemaObjectComparer(IComparer stringComparer) : base(stringComparer)
		{
		}

		public override int Compare(object obj1, object obj2)
		{
			SchemaObjectKey x = obj1 as SchemaObjectKey;
			SchemaObjectKey y = obj2 as SchemaObjectKey;

			//if search schema is null search only by name
			if( y.Schema != null )
			{
				if( null == x.Schema )
				{
					return 1;
				}
				int i = stringComparer.Compare(x.Schema, y.Schema);
				if( 0 != i )
				{
					return i;
				}
			}
			return stringComparer.Compare(x.Name, y.Name);
		}
	}

	internal class SchemaObjectKey : SimpleObjectKey
	{
		String schema;

		public SchemaObjectKey(String name, String schema) : base(name)
		{
			this.schema = schema;
		}

		internal static StringCollection schemaFields;
		static SchemaObjectKey()
		{
			schemaFields = new StringCollection();
			schemaFields.Add("Schema");
			schemaFields.Add("Name");
		}

		public string Schema
		{
			get { return schema; }
			set { schema = value; }
		}

		public override string UrnFilter
		{
			get 
			{ 
				if( null != schema && schema.Length > 0)
                {
                    return string.Format(SmoApplication.DefaultCulture, "@Name='{0}' and @Schema='{1}'", 
									Urn.EscapeString(name), Urn.EscapeString(schema));
                }
                else
                {
                    return string.Format(SmoApplication.DefaultCulture, "@Name='{0}'", Urn.EscapeString(name));
                }
            }
		}
			
		public override string ToString()
		{
			if( null != schema )
			{
				return string.Format(SmoApplication.DefaultCulture, "[{0}].[{1}]", 
												SqlSmoObject.SqlBraket(schema), 
												SqlSmoObject.SqlBraket(name));
			}
			return name;
		}

		public override string GetExceptionName()
		{
			if (null != schema)
			{
				return string.Format(SmoApplication.DefaultCulture, "{0}.{1}", schema, name);
			}
			return name;
		}


		public override StringCollection GetFieldNames()
		{
			return schemaFields;
		}

		public override ObjectKeyBase Clone()
		{
			return new SchemaObjectKey(this.Name, this.Schema);
		}
			
		internal override void Validate(Type objectType)
		{
			if( null == this.Name || this.Name.Length == 0 )
			{
				throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(objectType.ToString())).SetHelpContext("UnsupportedObjectNameExceptionText");
			}
			
			if( "Microsoft.SqlServer.Management.Smo.Table" == objectType.ToString() )
			{
				Table.CheckTableName(this.Name);
			}
		}

		public override bool IsNull
		{
			get { return (null == name || null == schema); }
		}

		public override ObjectComparerBase GetComparer(IComparer stringComparer)
		{
			return new SchemaObjectComparer(stringComparer);
		}




	}

	
}

