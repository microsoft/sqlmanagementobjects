# SMO Development wiki

This readme is the root of our "How to" wiki for implementing features in SMO. The links herein include other md files that may be collocated with their appropriate csproj files.

## Adding new objects

The SMO object model reflects a hierarchical view of SQL Server, including both control plane components (eg agent and service broker) and schema level database components (eg tables and views). The hierarchy is defined using a URN to identify each level, where each part of the URN is an XPATH expression.

`Server/Database[@Name='mydatabase']/Table[@Name='mytable']` is a URN that identifies a Table named mytable in database mydatabase.

The most common changes to SMO involve databases. When new DDL is created, there is likely to be an associated object for that DDL or new properties added to existing objects. SMO has an extensive set of code generation constructs in place to automate creation of and population of these objects.

To add a new object, first identify where it will fall in the hierarchy and devise an appropriate name for its URN component.

### Update codegen

First, define the object and its properties in [cfg.xml](/src/Codegen/README.md). For completely new objects, create a corresponding XML file in `%basedir%\src\Microsoft\SqlServer\Management\SqlEnum\xml` and add the XML as a resource to the SqlEnum csproj. For existing objects just add your property implementation queries to the appropriate [XML file](/src/Microsoft/SqlServer/Management/SqlEnum/xml/README.md).
Note: In order to view the changes in xml files you may have to reload your solution/project.

### New objects

Create a corresponding partial class in `%basedir%\src\Microsoft\SqlServer\Management\Smo\<ObjectType>Base.cs` where ObjectType is the same name as your object's class.

1. Facets.StateChangeEvent:  Include all events that can cause the properties of this object to change as defined in `[msdb].[dbo].[syspolicy_facet_events]`.
2. Your class should most likely extend either ScriptSchemaObject, NamedSmoObject, or SqlSmoObject.
3. Determine which operations you would like users to be able to perform, and implement each respective interface eg.  Cmn.IAlterable, Cmn.ICreateable, Cmn.IDroppable, Cmn.IMarkForDrop, Cmn.Iscriptable
4. For each interface there is at least one method that must be implemented.  For example to implement Cmn.ICreatable Create, and ScriptCreate must be implemented.
   1. Note, the behavior of each method can be affected with scripting preferences, e.g. `ScriptCreate` and `ScriptDrop`, which outputs the DDL for creating an object, can include an existence check if `IncludeScripts.ExistenceCheck` is set (and if the object supports it).
5. You must explicitly define any child collections, as these will not be generated.
6. For all properties in cfg.xml that were set to generate=false you must implement getters and setters.
7. Add a UrnSuffix: `public static string UrnSuffix => "<ObjectType>";`
8. If the object is scriptable on its own, not directly included as part of the script for its parent, reference its UrnSuffix in the scriptableTypes HashSet in ScriptMaker.cs.

### DesignMode

`DesignMode` (or `Design Mode`) is the label for an instance of the object hierarchy whose root `Server` object is using a `ServerConnection` that's in `Offline` mode. Any object added to a child collection in a `DesignMode` hierarchy is automatically set to the `Existing` state.


When the connection is offline, many code paths get short circuited and skip running queries, leaving the objects in whatever state the caller has set them. Correct support for DesignMode in your object will enable it to be used for offline unit tests. In `DesignMode`, certain methods like `Alter` are blocked completely, but unit tests can call the internal `ScriptAlter` method and validate that correct scripts are generated based on the set of properties the unit test set. Such a unit test can detect bugs that affect non-DesignMode operation, such as a failure to properly check if a property has been set on the object before trying to reference its value. 

If an object property is not explicitly set by a caller and that object doesn't have a `default` value set for it in [cfg.xml](/src/Codegen/cfg.xml), attempting to get the property value will result in an exception. Typically, readonly properties _should_ have a default value assigned. Default values for settable properties are optional, and may be limited to the properties that don't directly affect the `ScriptAlter` implementation of the object.

To make an object available in `DesignMode`, add `is_design_mode="true"` attribute to its definition in `cfg.xml`. To mark a property explicitly as `DesignMode`-friendly, use `mode="design"` in its definition in the object XML file. Note that `mode="deploy"` no longer seems to have any effect and can be removed or changed to `mode="all"` if you want to make an existing property available for `DesignMode`.

Adding an existing property to `DesignMode` is safe because it doesn't change its behavior at all in normal connected scenarios.

It's not clear how decisions were made in the past to choose which objects and properties support DesignMode. Going forward, we recommend new code supports DesignMode and includes appropriate offline unit tests.

### Collections

If your new object is part of a collection under a parent object, create a corresponding .cs file for your new collection. This can be done by adding an entry to [collections_codegen.proj](/src/codegen/README.md#collections_codegen.proj) and building that project. Then add the appropriate ```<Compile>``` tag to [Microsoft.SqlServer.Smo.csproj](/src/Microsoft/SqlServer/Management/Smo/Microsoft.SqlServer.Smo.csproj).

### IObjectPermissions

If the object type you are adding corresponds to an entry in sys.securable_classes, be sure your change includes the following:

- `implements="IObjectPermission" gen_body="obj1,1;obj2,1;enobj,2"` in the object declaration in src\Codegen\cfg.xml
- Declarations for ExtPropMajorID, ExtPropMinorID, and ExtPropClass properties. ExtPropClass value corresponds to the class column in sys.securable_classes. See [sys.database_permissions](<https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-database-permissions-transact-sql?view=sql-server-ver15>) for information on permissions. ExtPropMajorID and ExtPropMinorID properties map to major_id and minor_id in that view.
- Add the object type to src\functionaltest\smo\generalfunctionality\objectpermissionstests.cs
- Add the  appropriate prefix for setting permissions on the object in SqlSmoObject.PermissionPrefix

### Taxes and overhead

#### Update the enumerations and references under

- Enumerations.cs/DatabaseObjectBase.cs - If necessary update the DatabaseObjectTypes with your new object type and add it to the EnumObjects method of Database
  - Note this is only for actual objects in the Database. Options and other "virtual" objects shouldn't be added here
- ExceptionTemplatesImpl.strings - Any new exception template messages go here
  - Such as a new "object unsupported in current version" message for your new type.
- LocalizableResources.strings - Add descriptions for each publicly exposed property of the added type
  - This is REQUIRED if the object is a DMF Facet (it will have the `[Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]` attribute on the base class definition). If it is not a DMF facet you won't need it unless you added something that's using the text, but it's still recommended you add it anyways
  - Note that this applies to ALL public properties. So if you add a new object and it inherits from a base class with public properties you need to add descriptions for those as well. A suggestion is to view the object with something like ILSpy to see all the properties and make sure you aren't missing any. 
- script_patterns.cs - Add any common script additions here
  - e.g. Existence check for the new object
- ScriptMaker.cs - If your newly added object implements Cmn.IScriptable, then add it to the HashSet in Scriptable
- Serverbase.cs - If your newly added type uses a field other than the default "Name" as its key you must add the field correctly in CreateInitFieldsColl
  - This only applies for objects in collections
- SmoCollectionBase.cs - If your newly added type has a custom set of necessary fields for population add a case for it in GetFieldNames.
  - Only for objects in collections
- SmoUrnFilter.cs - Add the new type strings into the ObjectOrder enum and SetCreateOrder respectively.
  - Make sure the order in the ObjectOrder enum is correct, it should be before any types which depend on it and should be after any types which it depends on
- SmoUtility.cs - Set the minimum server version for object type supportability in IsSupportedObject().
- SqlSmoObject.cs - If the object type has a non-standard (default is to add an s to the end) pluralization add the pluralization string to GetPluralName().
- Map the Object type to the .xml property definitions in [config.xml](/src/Microsoft/SqlServer/Management/Sdk/Sfc/Enumerator/xml/Config.xml)

## Add Tests

Add unit tests under /src/UnitTest. These tests run during the build so they cannot rely on any external resources.
Functional tests go in /src/FunctionalTest/Smo. A subset of these tests will run as part of pull request validation. Guidance for best practices will be in the test [readme](/src/FunctionalTest/Smo/README.md)

## Important Tips

- Do not directly access the Properties of SMO objects in the scripting methods (especially Create). The property accessors are set to throw an exception if the state is creating (which is valid behavior most of the time) but in scripting we need to be able to handle this so that we can create the object (ScriptCreate is called for Create() so we can't throw an exception). Either nothing should be scripted (leave the defaults up to the DB) or the property should have some default value set that it can use.

    So instead call GetPropertyOptional(propName) and then check if the returned property.Value is null, if it isn't (or if NULL is expected) then add the appropriate scripting logic.

