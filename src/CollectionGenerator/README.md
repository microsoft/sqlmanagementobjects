# Source Generators for SMO

We have two source generators in this project.

## SmoCollectionGenerator

To simplify adding new object types and their corresponding collections to SMO, we have strongly typed base collection classes and a code generator that emits the body of the collection classes.
To add a new collection, a code author can simply declare the new collection class and specify which of the base classes it uses.

The source generator looks at the `UrnSuffix` and other properties of the generic type arguments to emit the body of the new collection class at build time. There are 4 base classes to choose from:

- `SimpleObjectCollectionBase` is the base of the other collections and should be used if your object type doesn't match the criteria for one of the other base classes.
- `RemovableCollectionBase` derives from `SimpleObjectCollectionBase` and can be used when your collection allows removal of elements when the parent of the collection is in `Creating` state. For example, an app can add or remove elements to `Database.FileGroups` before calling `Database.Create`, but afterward it can only call `Database.FileGroups.Add`. 
- `SchemaCollectionBase` contains objects that are part of a database schema.
- `ParameterCollectionBase` contains elements whose enumeration order is based on their `ID` instead of on their `Name`

### Examples

Input:

```C#
public sealed partial class FileGroupCollection : RemovableCollectionBase<FileGroup, Database>
{
}
```

Output:

```C#
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    ///Collection of FileGroup objects associated with an instance of Database
    ///</summary>
    public partial class FileGroupCollection
    {
        internal FileGroupCollection(SqlSmoObject parentInstance) : base((Database)parentInstance)
        {
        }

        protected override string UrnSuffix => FileGroup.UrnSuffix;

        internal override FileGroup GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state) => new FileGroup(this, key, state);
    }
}
```



## SqlSmoObjectGenerator

This source generator uses `SfcObject` attributes in the declaration of a `SqlSmoObject`-derived class to build switch statements that enable the scripting engine to map URN components to child property names without using reflection. Attributes using `SfcContainerRelationship` arguments identify child collections, while attributes using `SfcObjectRelationship` arguments identify child singletons.

### Examples:

Input:

```C#

[SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Index), SfcObjectFlags.Design | SfcObjectFlags.Deploy)]
public override IndexCollection Indexes
{
    get { return base.Indexes; }
}

[SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
public DatabaseOptions DatabaseOptions
{
    get
    {
        CheckObjectStateImpl(false);
        if (null == m_DatabaseOptions)
        {
            m_DatabaseOptions = new DatabaseOptions(this, new ObjectKeyBase(), this.State/*SqlSmoState.Existing*/);
        }
        return m_DatabaseOptions;
    }
}
```

Output:

```C#
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class Database
    {
        protected override SqlSmoObject GetSingletonInstance(string childTypeName)
        {
            switch (childTypeName)
            {
                case "Option":
                case "DatabaseOptions":
                    return DatabaseOptions;
                case "QueryStoreOptions":
                    return QueryStoreOptions;
                case "DatabaseEncryptionKey":
                    return DatabaseEncryptionKey;
                case "MasterKey":
                    return MasterKey;
                case "ServiceBroker":
                    return ServiceBroker;

                default:
                    return base.GetSingletonInstance(childTypeName);
            }
        }
    }
}
```