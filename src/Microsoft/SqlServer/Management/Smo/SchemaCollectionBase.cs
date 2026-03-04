// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Contains common features of all schema collection classes
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TParent"></typeparam>

    public abstract class SchemaCollectionBase<TObject, TParent> : SortedListCollectionBase<TObject, TParent>, ISchemaObjectCollection, ISchemaObjectCollectionInternal
        where TObject : ScriptNameObjectBase
        where TParent : SqlSmoObject
    {

        internal SchemaCollectionBase(TParent parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the parent object
        /// </summary>
        public TParent Parent => ParentInstance as TParent;

        /// <summary>
        /// Returns the object of the given name in the collection, using the default schema.
        /// Returns null if the object is not in the collection
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public TObject this[string name]
        {
            get
            {
                return name == null
                    ? throw new ArgumentNullException(nameof(name))
                    : GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema()));
            }
        }

        /// <summary>
        /// Returns the object in the collection having the given name and schema.
        /// Returns null if the object is not in the collection
        /// </summary>
        /// <param name="name"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public TObject this[string name, string schema]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                else if (schema == null)
                {
                    throw new ArgumentNullException(nameof(schema));
                }

                return GetObjectByKey(new SchemaObjectKey(name, schema));
            }
        }

        /// <summary>
        /// Adds the given object to the collection
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="FailedOperationException"></exception>
        public void Add(TObject obj)
        {
            if (null == obj)
                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException(nameof(obj)));

            AddImpl(obj);
        }

        protected override void InitInnerCollection() => InternalStorage = new SmoSortedList<TObject>(new SchemaObjectComparer(StringComparer));

        void ISchemaObjectCollectionInternal.RemoveObject(string name, string schema) => InternalStorage.Remove(new SchemaObjectKey(name, schema));

        /// <summary>
        /// Returns whether an object of the given name exists in the collection. Assumes the object is a member of the default schema.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name) => Contains(new SchemaObjectKey(name, GetDefaultSchema()));

        /// <summary>
        /// Returns whether an object of the given name and schema exists in the collection
        /// </summary>
        /// <param name="name"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public bool Contains(string name, string schema) => Contains(new SchemaObjectKey(name, schema));

        /// <summary>
        /// Returns the default schema of the database associated with this collection
        /// </summary>
        /// <returns></returns>
        public virtual string GetDefaultSchema()
        {
            SqlSmoObject db;
            if (ParentInstance is Database)
            {
                //level 1
                db = ParentInstance;
            }
            else if (ParentInstance is Broker.ServiceBroker broker)
            {
                //Service Broker is special - it is not in the collection.
                db = broker.Parent;
            }
            else if (ParentInstance.ParentColl.ParentInstance is Database)
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
            return db.State == SqlSmoState.Creating || db.IsDesignMode ? "dbo" : db.Properties["DefaultSchema"].Value as string;
        }

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            string name = urn.GetAttribute("Name");
            if (null == name || (name.Length == 0 && !CanHaveEmptyName(urn)))
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            }

            var schema = urn.GetAttribute("Schema");
            if (string.IsNullOrEmpty(schema))
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
            if (y.Schema != null)
            {
                if (null == x.Schema)
                {
                    return 1;
                }
                int i = stringComparer.Compare(x.Schema, y.Schema);
                if (0 != i)
                {
                    return i;
                }
            }
            return stringComparer.Compare(x.Name, y.Name);
        }
    }

    internal class SchemaObjectKey : SimpleObjectKey
    {

        public SchemaObjectKey(string name, string schema) : base(name)
        {
            Schema = schema;
        }

        internal static StringCollection schemaFields;
        static SchemaObjectKey()
        {
            schemaFields = new StringCollection
            {
                nameof(Schema),
                nameof(Name)
            };
        }

        public string Schema
        {
            get;
            set;
        }

        public override string UrnFilter => !string.IsNullOrEmpty(Schema)
                    ? $"@Name='{Urn.EscapeString(Name)}' and @Schema='{Urn.EscapeString(Schema)}'"
                    : $"@Name='{Urn.EscapeString(Name)}'";

        public override string ToString()
        {
            if (null != Schema)
            {
                return $"[{SqlSmoObject.SqlBraket(Schema)}].[{SqlSmoObject.SqlBraket(Name)}]";
            }
            return Name;
        }

        public override string GetExceptionName()
        {
            if (null != Schema)
            {
                return $"{Schema}.{Name}";
            }
            return Name;
        }


        public override StringCollection GetFieldNames() => schemaFields;

        public override ObjectKeyBase Clone() => new SchemaObjectKey(Name, Schema);

        internal override void Validate(Type objectType)
        {
            if (string.IsNullOrEmpty(Name))
            {
                throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(objectType.ToString())).SetHelpContext("UnsupportedObjectNameExceptionText");
            }

            if (typeof(Table) == objectType)
            {
                Table.CheckTableName(Name);
            }
        }

        public override bool IsNull => null == Name || null == Schema;

        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new SchemaObjectComparer(stringComparer);
    }



    internal interface ISchemaObjectCollectionInternal
    {
        /// <summary>
        /// Removes the object with the given name and schema from the collection
        /// </summary>
        /// <param name="name"></param>
        /// <param name="schema"></param>
        void RemoveObject(string name, string schema);
    }
}

