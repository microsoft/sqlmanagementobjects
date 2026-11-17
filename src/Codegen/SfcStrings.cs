// Stub: returns empty strings for all resources to remove dependency on embedded .resx resources.

#nullable enable

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    internal static partial class SfcStrings
    {
        public static global::System.Globalization.CultureInfo? Culture { get; set; }

        internal static string? GetResourceString(string resourceKey, string? defaultValue = null) =>
            defaultValue ?? string.Empty;

        private static string GetResourceString(string resourceKey, string[]? formatterNames) =>
            string.Empty;

        /// <summary>Property '{propertyName}' is read-only.</summary>
        public static string @PropertyReadOnly => GetResourceString("PropertyReadOnly")!;
        /// <summary>Property '{propertyName}' is read-only.</summary>
        internal static string FormatPropertyReadOnly(object? propertyName)
           => string.Format(Culture, GetResourceString("PropertyReadOnly", new[] { "propertyName" }), propertyName);

        /// <summary>Property '{propertyName}' is not set.</summary>
        public static string @PropertyNotSet => GetResourceString("PropertyNotSet")!;
        /// <summary>Property '{propertyName}' is not set.</summary>
        internal static string FormatPropertyNotSet(object? propertyName)
           => string.Format(Culture, GetResourceString("PropertyNotSet", new[] { "propertyName" }), propertyName);

        /// <summary>This operation is only valid when the object is in the Pending state.</summary>
        public static string @OperationValidOnlyInPendingState => GetResourceString("OperationValidOnlyInPendingState")!;
        /// <summary>The initialize operation on object {objName} failed.</summary>
        public static string @SfcObjectInitFailed => GetResourceString("SfcObjectInitFailed")!;
        /// <summary>The initialize operation on object {objName} failed.</summary>
        internal static string FormatSfcObjectInitFailed(object? objName)
           => string.Format(Culture, GetResourceString("SfcObjectInitFailed", new[] { "objName" }), objName);

        /// <summary>The connection context mode cannot be changed from {fromMode} to {toMode}.</summary>
        public static string @SfcInvalidConnectionContextModeChange => GetResourceString("SfcInvalidConnectionContextModeChange")!;
        /// <summary>The connection context mode cannot be changed from {fromMode} to {toMode}.</summary>
        internal static string FormatSfcInvalidConnectionContextModeChange(object? fromMode, object? toMode)
           => string.Format(Culture, GetResourceString("SfcInvalidConnectionContextModeChange", new[] { "fromMode", "toMode" }), fromMode, toMode);

        /// <summary>The {keyName} key is not a valid key. This can be because member variables are not yet set.</summary>
        public static string @InvalidKey => GetResourceString("InvalidKey")!;
        /// <summary>The {keyName} key is not a valid key. This can be because member variables are not yet set.</summary>
        internal static string FormatInvalidKey(object? keyName)
           => string.Format(Culture, GetResourceString("InvalidKey", new[] { "keyName" }), keyName);

        /// <summary>The parent or key of the KeyChain is not the same as the previously set parent or key.</summary>
        public static string @InvalidKeyChain => GetResourceString("InvalidKeyChain")!;
        /// <summary>The rename failed.</summary>
        public static string @InvalidRename => GetResourceString("InvalidRename")!;
        /// <summary>The move failed.</summary>
        public static string @InvalidMove => GetResourceString("InvalidMove")!;
        /// <summary>The {key} key already exists in the collection.</summary>
        public static string @KeyExists => GetResourceString("KeyExists")!;
        /// <summary>The {key} key already exists in the collection.</summary>
        internal static string FormatKeyExists(object? key)
           => string.Format(Culture, GetResourceString("KeyExists", new[] { "key" }), key);

        /// <summary>The {key} key is not found in the collection.</summary>
        public static string @KeyNotFound => GetResourceString("KeyNotFound")!;
        /// <summary>The {key} key is not found in the collection.</summary>
        internal static string FormatKeyNotFound(object? key)
           => string.Format(Culture, GetResourceString("KeyNotFound", new[] { "key" }), key);

        /// <summary>The key to this object is already set. Setting the keychain is not allowed.</summary>
        public static string @KeyAlreadySet => GetResourceString("KeyAlreadySet")!;
        /// <summary>The key chain to this object already exists and the parents do not match.</summary>
        public static string @KeyChainAlreadySet => GetResourceString("KeyChainAlreadySet")!;
        /// <summary>The '{argumentName}' argument is either null or invalid.</summary>
        public static string @SfcInvalidArgument => GetResourceString("SfcInvalidArgument")!;
        /// <summary>The '{argumentName}' argument is either null or invalid.</summary>
        internal static string FormatSfcInvalidArgument(object? argumentName)
           => string.Format(Culture, GetResourceString("SfcInvalidArgument", new[] { "argumentName" }), argumentName);

        /// <summary>The '{argumentName}' input stream is closed, at end-of-file or in an error state.</summary>
        public static string @SfcInvalidReaderStream => GetResourceString("SfcInvalidReaderStream")!;
        /// <summary>The '{argumentName}' input stream is closed, at end-of-file or in an error state.</summary>
        internal static string FormatSfcInvalidReaderStream(object? argumentName)
           => string.Format(Culture, GetResourceString("SfcInvalidReaderStream", new[] { "argumentName" }), argumentName);

        /// <summary>The '{argumentName}' output stream is closed or in an error state.</summary>
        public static string @SfcInvalidWriterStream => GetResourceString("SfcInvalidWriterStream")!;
        /// <summary>The '{argumentName}' output stream is closed or in an error state.</summary>
        internal static string FormatSfcInvalidWriterStream(object? argumentName)
           => string.Format(Culture, GetResourceString("SfcInvalidWriterStream", new[] { "argumentName" }), argumentName);

        /// <summary>Serialization output is invalid.</summary>
        public static string @SfcInvalidSerialization => GetResourceString("SfcInvalidSerialization")!;
        /// <summary>Deserialization input in corrupt.</summary>
        public static string @SfcInvalidDeserialization => GetResourceString("SfcInvalidDeserialization")!;
        /// <summary>Deserialization input is invalid. The parent entry of '{instanceUri}' is missing. The parent entry '{parentUri}' is expected to occurs before its children.</summary>
        public static string @SfcInvalidDeserializationMissingParent => GetResourceString("SfcInvalidDeserializationMissingParent")!;
        /// <summary>Deserialization input is invalid. The parent entry of '{instanceUri}' is missing. The parent entry '{parentUri}' is expected to occurs before its children.</summary>
        internal static string FormatSfcInvalidDeserializationMissingParent(object? instanceUri, object? parentUri)
           => string.Format(Culture, GetResourceString("SfcInvalidDeserializationMissingParent", new[] { "instanceUri", "parentUri" }), instanceUri, parentUri);

        /// <summary>Serialization operation on {instanceName} has failed.</summary>
        public static string @SfcInvalidSerializationInstance => GetResourceString("SfcInvalidSerializationInstance")!;
        /// <summary>Serialization operation on {instanceName} has failed.</summary>
        internal static string FormatSfcInvalidSerializationInstance(object? instanceName)
           => string.Format(Culture, GetResourceString("SfcInvalidSerializationInstance", new[] { "instanceName" }), instanceName);

        /// <summary>Deserialization operation on {instanceName} has failed.</summary>
        public static string @SfcInvalidDeserializationInstance => GetResourceString("SfcInvalidDeserializationInstance")!;
        /// <summary>Deserialization operation on {instanceName} has failed.</summary>
        internal static string FormatSfcInvalidDeserializationInstance(object? instanceName)
           => string.Format(Culture, GetResourceString("SfcInvalidDeserializationInstance", new[] { "instanceName" }), instanceName);

        /// <summary>The instance passed in to serialize is a null instance.</summary>
        public static string @SfcNullArgumentToSerialize => GetResourceString("SfcNullArgumentToSerialize")!;
        /// <summary>The instance passed in to resolve is a null instance.</summary>
        public static string @SfcNullArgumentToResolve => GetResourceString("SfcNullArgumentToResolve")!;
        /// <summary>The instance passed in to resolve collection is a null instance.</summary>
        public static string @SfcNullArgumentToResolveCollection => GetResourceString("SfcNullArgumentToResolveCollection")!;
        /// <summary>The resolver type passed in to the '{attribute}' attribute is null.</summary>
        public static string @SfcNullArgumentToSfcReferenceAttribute => GetResourceString("SfcNullArgumentToSfcReferenceAttribute")!;
        /// <summary>The resolver type passed in to the '{attribute}' attribute is null.</summary>
        internal static string FormatSfcNullArgumentToSfcReferenceAttribute(object? attribute)
           => string.Format(Culture, GetResourceString("SfcNullArgumentToSfcReferenceAttribute", new[] { "attribute" }), attribute);

        /// <summary>The resolver type '{resolverType}' does not implement interface '{resolverInterface}' needed to perform resolving.</summary>
        public static string @SfcNullInvalidSfcReferenceResolver => GetResourceString("SfcNullInvalidSfcReferenceResolver")!;
        /// <summary>The resolver type '{resolverType}' does not implement interface '{resolverInterface}' needed to perform resolving.</summary>
        internal static string FormatSfcNullInvalidSfcReferenceResolver(object? resolverType, object? resolverInterface)
           => string.Format(Culture, GetResourceString("SfcNullInvalidSfcReferenceResolver", new[] { "resolverType", "resolverInterface" }), resolverType, resolverInterface);

        /// <summary>The view name passed in to the relation view attribute is null.</summary>
        public static string @SfcNullArgumentToViewAttribute => GetResourceString("SfcNullArgumentToViewAttribute")!;
        /// <summary>The instance passed in to the proxy as the referenced instance is null.</summary>
        public static string @SfcNullArgumentToProxyInstance => GetResourceString("SfcNullArgumentToProxyInstance")!;
        /// <summary>The writer passed in to serialize is a null instance.</summary>
        public static string @SfcNullWriterToSerialize => GetResourceString("SfcNullWriterToSerialize")!;
        /// <summary>The reader passed in to serialize is a null instance.</summary>
        public static string @SfcNullReaderToSerialize => GetResourceString("SfcNullReaderToSerialize")!;
        /// <summary>Type {typeName} is not supported for serialization.</summary>
        public static string @SfcNonSerializableType => GetResourceString("SfcNonSerializableType")!;
        /// <summary>Type {typeName} is not supported for serialization.</summary>
        internal static string FormatSfcNonSerializableType(object? typeName)
           => string.Format(Culture, GetResourceString("SfcNonSerializableType", new[] { "typeName" }), typeName);

        /// <summary>Write method invoked without setting instances to serialize.</summary>
        public static string @SfcInvalidWriteWithoutDiscovery => GetResourceString("SfcInvalidWriteWithoutDiscovery")!;
        /// <summary>Serializer cannot handle the property {property}. The property is not in the property bag of the instance type.</summary>
        public static string @SfcNonSerializableProperty => GetResourceString("SfcNonSerializableProperty")!;
        /// <summary>Serializer cannot handle the property {property}. The property is not in the property bag of the instance type.</summary>
        internal static string FormatSfcNonSerializableProperty(object? property)
           => string.Format(Culture, GetResourceString("SfcNonSerializableProperty", new[] { "property" }), property);

        /// <summary>The Xml contains an unregistered Sfc Domain '{sfcDomainName}'</summary>
        public static string @UnregisteredXmlSfcDomain => GetResourceString("UnregisteredXmlSfcDomain")!;
        /// <summary>The Xml contains an unregistered Sfc Domain '{sfcDomainName}'</summary>
        internal static string FormatUnregisteredXmlSfcDomain(object? sfcDomainName)
           => string.Format(Culture, GetResourceString("UnregisteredXmlSfcDomain", new[] { "sfcDomainName" }), sfcDomainName);

        /// <summary>The Xml contains an unregistered Sfc Domain '{sfcDomain}' with Type '{sfcType}'</summary>
        public static string @UnregisteredSfcXmlType => GetResourceString("UnregisteredSfcXmlType")!;
        /// <summary>The Xml contains an unregistered Sfc Domain '{sfcDomain}' with Type '{sfcType}'</summary>
        internal static string FormatUnregisteredSfcXmlType(object? sfcDomain, object? sfcType)
           => string.Format(Culture, GetResourceString("UnregisteredSfcXmlType", new[] { "sfcDomain", "sfcType" }), sfcDomain, sfcType);

        /// <summary>The Xml contains property '{propertyName}' within Sfc Type '{sfcTypeName}' that is attributed as non-serializable.</summary>
        public static string @CannotDeserializeNonSerializableProperty => GetResourceString("CannotDeserializeNonSerializableProperty")!;
        /// <summary>The Xml contains property '{propertyName}' within Sfc Type '{sfcTypeName}' that is attributed as non-serializable.</summary>
        internal static string FormatCannotDeserializeNonSerializableProperty(object? propertyName, object? sfcTypeName)
           => string.Format(Culture, GetResourceString("CannotDeserializeNonSerializableProperty", new[] { "propertyName", "sfcTypeName" }), propertyName, sfcTypeName);

        /// <summary>The serialized content's version is higher than the domain's current version. Deserialization cannot proceed.</summary>
        public static string @SfcUnsupportedVersion => GetResourceString("SfcUnsupportedVersion")!;
        /// <summary>The domain does not support Serialization upgrade. Deserialization cannot proceed with older version content.</summary>
        public static string @SfcUnsupportedDomainUpgrade => GetResourceString("SfcUnsupportedDomainUpgrade")!;
        /// <summary>The Xml passed in is either empty or does not contain any Xml elements that could be deserialized.</summary>
        public static string @EmptySfcXml => GetResourceString("EmptySfcXml")!;
        /// <summary>The Xml expects a parent object in Sfc Domain '{sfcExpectedParentDomain}' with Type '{sfcExpectedParentType}', but the parent object Type is '{sfcGivenParentType}'.</summary>
        public static string @InvalidSfcXmlParentType => GetResourceString("InvalidSfcXmlParentType")!;
        /// <summary>The Xml expects a parent object in Sfc Domain '{sfcExpectedParentDomain}' with Type '{sfcExpectedParentType}', but the parent object Type is '{sfcGivenParentType}'.</summary>
        internal static string FormatInvalidSfcXmlParentType(object? sfcExpectedParentDomain, object? sfcExpectedParentType, object? sfcGivenParentType)
           => string.Format(Culture, GetResourceString("InvalidSfcXmlParentType", new[] { "sfcExpectedParentDomain", "sfcExpectedParentType", "sfcGivenParentType" }), sfcExpectedParentDomain, sfcExpectedParentType, sfcGivenParentType);

        /// <summary>'{query}' is an invalid query expression for a SMO ObjectQuery. The query is expected to start with 'Server'.</summary>
        public static string @InvalidSMOQuery => GetResourceString("InvalidSMOQuery")!;
        /// <summary>'{query}' is an invalid query expression for a SMO ObjectQuery. The query is expected to start with 'Server'.</summary>
        internal static string FormatInvalidSMOQuery(object? query)
           => string.Format(Culture, GetResourceString("InvalidSMOQuery", new[] { "query" }), query);

        /// <summary>Cannot set a parent that is a root but does not have a connection.</summary>
        public static string @ParentHasNoConnecton => GetResourceString("ParentHasNoConnecton")!;
        /// <summary>A query connection is unavailable or not supported for the requested query execution environment. This is usually due to a request for Multiple Active Queries on a single user mode server or other condition preventing a valid connection.</summary>
        public static string @SfcQueryConnectionUnavailable => GetResourceString("SfcQueryConnectionUnavailable")!;
        /// <summary>CreateIdentityKey returned null. The domain implementation is incorrect.</summary>
        public static string @BadCreateIdentityKey => GetResourceString("BadCreateIdentityKey")!;
        /// <summary>Unable to perform this operation on an object in state '{current_state}'. The object must be in state '{required_state}'.</summary>
        public static string @InvalidState => GetResourceString("InvalidState")!;
        /// <summary>Unable to perform this operation on an object in state '{current_state}'. The object must be in state '{required_state}'.</summary>
        internal static string FormatInvalidState(object? current_state, object? required_state)
           => string.Format(Culture, GetResourceString("InvalidState", new[] { "current_state", "required_state" }), current_state, required_state);

        /// <summary>Operation '{opname}' on object '{objname}' failed during execution.</summary>
        public static string @CRUDOperationFailed => GetResourceString("CRUDOperationFailed")!;
        /// <summary>Operation '{opname}' on object '{objname}' failed during execution.</summary>
        internal static string FormatCRUDOperationFailed(object? opname, object? objname)
           => string.Format(Culture, GetResourceString("CRUDOperationFailed", new[] { "opname", "objname" }), opname, objname);

        /// <summary>Domain error: object '{objname}' is in the dependency graph but CRUD operation cannot be scripted. Consider changing IsCrudActionHandledByParent in TypeMetadata for class '{className}'.</summary>
        public static string @ObjectNotScriptabe => GetResourceString("ObjectNotScriptabe")!;
        /// <summary>Domain error: object '{objname}' is in the dependency graph but CRUD operation cannot be scripted. Consider changing IsCrudActionHandledByParent in TypeMetadata for class '{className}'.</summary>
        internal static string FormatObjectNotScriptabe(object? objname, object? className)
           => string.Format(Culture, GetResourceString("ObjectNotScriptabe", new[] { "objname", "className" }), objname, className);

        /// <summary>The action '{action}' is unsupported for type '{className}'.</summary>
        public static string @UnsupportedAction => GetResourceString("UnsupportedAction")!;
        /// <summary>The action '{action}' is unsupported for type '{className}'.</summary>
        internal static string FormatUnsupportedAction(object? action, object? className)
           => string.Format(Culture, GetResourceString("UnsupportedAction", new[] { "action", "className" }), action, className);

        /// <summary>Must set the Parent to perform this operation.</summary>
        public static string @MissingParent => GetResourceString("MissingParent")!;
        /// <summary>Create</summary>
        public static string @opCreate => GetResourceString("opCreate")!;
        /// <summary>Rename</summary>
        public static string @opRename => GetResourceString("opRename")!;
        /// <summary>Move</summary>
        public static string @opMove => GetResourceString("opMove")!;
        /// <summary>Alter</summary>
        public static string @opAlter => GetResourceString("opAlter")!;
        /// <summary>Drop</summary>
        public static string @opDrop => GetResourceString("opDrop")!;
        /// <summary>Cannot move object '{obj}', no destination parent object given.</summary>
        public static string @CannotMoveNoDestination => GetResourceString("CannotMoveNoDestination")!;
        /// <summary>Cannot move object '{obj}', no destination parent object given.</summary>
        internal static string FormatCannotMoveNoDestination(object? obj)
           => string.Format(Culture, GetResourceString("CannotMoveNoDestination", new[] { "obj" }), obj);

        /// <summary>Cannot move object '{obj}', the destination parent object '{destObj}' may not be our descendant.</summary>
        public static string @CannotMoveDestinationIsDescendant => GetResourceString("CannotMoveDestinationIsDescendant")!;
        /// <summary>Cannot move object '{obj}', the destination parent object '{destObj}' may not be our descendant.</summary>
        internal static string FormatCannotMoveDestinationIsDescendant(object? obj, object? destObj)
           => string.Format(Culture, GetResourceString("CannotMoveDestinationIsDescendant", new[] { "obj", "destObj" }), obj, destObj);

        /// <summary>Cannot move object '{obj}', the destination parent object '{destObj}' has a duplicate child.</summary>
        public static string @CannotMoveDestinationHasDuplicate => GetResourceString("CannotMoveDestinationHasDuplicate")!;
        /// <summary>Cannot move object '{obj}', the destination parent object '{destObj}' has a duplicate child.</summary>
        internal static string FormatCannotMoveDestinationHasDuplicate(object? obj, object? destObj)
           => string.Format(Culture, GetResourceString("CannotMoveDestinationHasDuplicate", new[] { "obj", "destObj" }), obj, destObj);

        /// <summary>Cannot rename object '{obj}', key has no properties.</summary>
        public static string @CannotRenameNoProperties => GetResourceString("CannotRenameNoProperties")!;
        /// <summary>Cannot rename object '{obj}', key has no properties.</summary>
        internal static string FormatCannotRenameNoProperties(object? obj)
           => string.Format(Culture, GetResourceString("CannotRenameNoProperties", new[] { "obj" }), obj);

        /// <summary>Cannot rename object '{obj}', must have property '{missingProperty}'.</summary>
        public static string @CannotRenameMissingProperty => GetResourceString("CannotRenameMissingProperty")!;
        /// <summary>Cannot rename object '{obj}', must have property '{missingProperty}'.</summary>
        internal static string FormatCannotRenameMissingProperty(object? obj, object? missingProperty)
           => string.Format(Culture, GetResourceString("CannotRenameMissingProperty", new[] { "obj", "missingProperty" }), obj, missingProperty);

        /// <summary>Cannot rename object '{obj}', no key given.</summary>
        public static string @CannotRenameNoKey => GetResourceString("CannotRenameNoKey")!;
        /// <summary>Cannot rename object '{obj}', no key given.</summary>
        internal static string FormatCannotRenameNoKey(object? obj)
           => string.Format(Culture, GetResourceString("CannotRenameNoKey", new[] { "obj" }), obj);

        /// <summary>Cannot rename object '{obj}', the key '{key}' already exists.</summary>
        public static string @CannotRenameDestinationHasDuplicate => GetResourceString("CannotRenameDestinationHasDuplicate")!;
        /// <summary>Cannot rename object '{obj}', the key '{key}' already exists.</summary>
        internal static string FormatCannotRenameDestinationHasDuplicate(object? obj, object? key)
           => string.Format(Culture, GetResourceString("CannotRenameDestinationHasDuplicate", new[] { "obj", "key" }), obj, key);

        /// <summary>Don't have rights to execute requested operation.</summary>
        public static string @PermissionDenied => GetResourceString("PermissionDenied")!;
        /// <summary>Provided Collection is not compatible with SfcListAdapter. It should implement IList, IListSource or IEnumerable&lt;{type}&gt;.</summary>
        public static string @IncompatibleWithSfcListAdapterCollection => GetResourceString("IncompatibleWithSfcListAdapterCollection")!;
        /// <summary>Provided Collection is not compatible with SfcListAdapter. It should implement IList, IListSource or IEnumerable&lt;{type}&gt;.</summary>
        internal static string FormatIncompatibleWithSfcListAdapterCollection(object? type)
           => string.Format(Culture, GetResourceString("IncompatibleWithSfcListAdapterCollection", new[] { "type" }), type);

        /// <summary>'{query}': invalid query expression root when connected to '{rootName}'</summary>
        public static string @BadQueryForConnection => GetResourceString("BadQueryForConnection")!;
        /// <summary>'{query}': invalid query expression root when connected to '{rootName}'</summary>
        internal static string FormatBadQueryForConnection(object? query, object? rootName)
           => string.Format(Culture, GetResourceString("BadQueryForConnection", new[] { "query", "rootName" }), query, rootName);

        /// <summary>Object '{obj}' already exists</summary>
        public static string @CannotCreateDestinationHasDuplicate => GetResourceString("CannotCreateDestinationHasDuplicate")!;
        /// <summary>Object '{obj}' already exists</summary>
        internal static string FormatCannotCreateDestinationHasDuplicate(object? obj)
           => string.Format(Culture, GetResourceString("CannotCreateDestinationHasDuplicate", new[] { "obj" }), obj);

        /// <summary>Unable to Load Microsoft SQL Server Compact. Install Microsoft SQL Server Compact MSIs from the folder - Servers\Setup on the SQL Server installation media. For more details, please refer to KB Article 952218.</summary>
        public static string @MissingSqlCeTools => GetResourceString("MissingSqlCeTools")!;
        /// <summary>The {firstAttribute} and {secondAttribute} attributes on {typeName}.{propertyName} are conflicting.</summary>
        public static string @AttributeConflict => GetResourceString("AttributeConflict")!;
        /// <summary>The {firstAttribute} and {secondAttribute} attributes on {typeName}.{propertyName} are conflicting.</summary>
        internal static string FormatAttributeConflict(object? firstAttribute, object? secondAttribute, object? typeName, object? propertyName)
           => string.Format(Culture, GetResourceString("AttributeConflict", new[] { "firstAttribute", "secondAttribute", "typeName", "propertyName" }), firstAttribute, secondAttribute, typeName, propertyName);

        /// <summary>Domain '{name}' was not found on the list of registered domain.</summary>
        public static string @DomainNotFound => GetResourceString("DomainNotFound")!;
        /// <summary>Domain '{name}' was not found on the list of registered domain.</summary>
        internal static string FormatDomainNotFound(object? name)
           => string.Format(Culture, GetResourceString("DomainNotFound", new[] { "name" }), name);

        /// <summary>Property {name} cannot be used for {usage}</summary>
        public static string @PropertyUsageError => GetResourceString("PropertyUsageError")!;
        /// <summary>Property {name} cannot be used for {usage}</summary>
        internal static string FormatPropertyUsageError(object? name, object? usage)
           => string.Format(Culture, GetResourceString("PropertyUsageError", new[] { "name", "usage" }), name, usage);

        /// <summary>request</summary>
        public static string @UsageRequest => GetResourceString("UsageRequest")!;
        /// <summary>filter</summary>
        public static string @UsageFilter => GetResourceString("UsageFilter")!;
        /// <summary>order by</summary>
        public static string @UsageOrderBy => GetResourceString("UsageOrderBy")!;
        /// <summary>Property {name} cannot have an alias. It is not requested.</summary>
        public static string @PropertyCannotHaveAlias => GetResourceString("PropertyCannotHaveAlias")!;
        /// <summary>Property {name} cannot have an alias. It is not requested.</summary>
        internal static string FormatPropertyCannotHaveAlias(object? name)
           => string.Format(Culture, GetResourceString("PropertyCannotHaveAlias", new[] { "name" }), name);

        /// <summary>Cannot find alias for property {name}. The Prefix property is null.</summary>
        public static string @InvalidPrefixAlias => GetResourceString("InvalidPrefixAlias")!;
        /// <summary>Cannot find alias for property {name}. The Prefix property is null.</summary>
        internal static string FormatInvalidPrefixAlias(object? name)
           => string.Format(Culture, GetResourceString("InvalidPrefixAlias", new[] { "name" }), name);

        /// <summary>Cannot find alias for property {name}. Alias was not specified.</summary>
        public static string @AliasNotSpecified => GetResourceString("AliasNotSpecified")!;
        /// <summary>Cannot find alias for property {name}. Alias was not specified.</summary>
        internal static string FormatAliasNotSpecified(object? name)
           => string.Format(Culture, GetResourceString("AliasNotSpecified", new[] { "name" }), name);

        /// <summary>Invalid alias kind.</summary>
        public static string @InvalidAlias => GetResourceString("InvalidAlias")!;
        /// <summary>result type not supported</summary>
        public static string @ResultNotSupported => GetResourceString("ResultNotSupported")!;
        /// <summary>unknown property {property}</summary>
        public static string @UnknownProperty => GetResourceString("UnknownProperty")!;
        /// <summary>unknown property {property}</summary>
        internal static string FormatUnknownProperty(object? property)
           => string.Format(Culture, GetResourceString("UnknownProperty", new[] { "property" }), property);

        /// <summary>unknown type {type}</summary>
        public static string @UnknownType => GetResourceString("UnknownType")!;
        /// <summary>unknown type {type}</summary>
        internal static string FormatUnknownType(object? type)
           => string.Format(Culture, GetResourceString("UnknownType", new[] { "type" }), type);

        /// <summary>unclosed string</summary>
        public static string @XPathUnclosedString => GetResourceString("XPathUnclosedString")!;
        /// <summary>syntax error</summary>
        public static string @XPathSyntaxError => GetResourceString("XPathSyntaxError")!;
        /// <summary>failed to load assembly {assembly}.</summary>
        public static string @FailedToLoadAssembly => GetResourceString("FailedToLoadAssembly")!;
        /// <summary>failed to load assembly {assembly}.</summary>
        internal static string FormatFailedToLoadAssembly(object? assembly)
           => string.Format(Culture, GetResourceString("FailedToLoadAssembly", new[] { "assembly" }), assembly);

        /// <summary>could not instantiate object {objType}.</summary>
        public static string @CouldNotInstantiateObj => GetResourceString("CouldNotInstantiateObj")!;
        /// <summary>could not instantiate object {objType}.</summary>
        internal static string FormatCouldNotInstantiateObj(object? objType)
           => string.Format(Culture, GetResourceString("CouldNotInstantiateObj", new[] { "objType" }), objType);

        /// <summary>unknown node type</summary>
        public static string @UnknowNodeType => GetResourceString("UnknowNodeType")!;
        /// <summary>unknown operator</summary>
        public static string @UnknownOperator => GetResourceString("UnknownOperator")!;
        /// <summary>unknown function</summary>
        public static string @UnknownFunction => GetResourceString("UnknownFunction")!;
        /// <summary>variables not supported</summary>
        public static string @VariablesNotSupported => GetResourceString("VariablesNotSupported")!;
        /// <summary>unknown element type</summary>
        public static string @UnknownElemType => GetResourceString("UnknownElemType")!;
        /// <summary>child expressions are not supported.</summary>
        public static string @ChildrenNotSupported => GetResourceString("ChildrenNotSupported")!;
        /// <summary>unsupported expression</summary>
        public static string @UnsupportedExpresion => GetResourceString("UnsupportedExpresion")!;
        /// <summary>{objType} is not derived from {objName}.</summary>
        public static string @NotDerivedFrom => GetResourceString("NotDerivedFrom")!;
        /// <summary>{objType} is not derived from {objName}.</summary>
        internal static string FormatNotDerivedFrom(object? objType, object? objName)
           => string.Format(Culture, GetResourceString("NotDerivedFrom", new[] { "objType", "objName" }), objType, objName);

        /// <summary>{objType} doesn't implement ISupportInitData, but it has a configuration file.</summary>
        public static string @ISupportInitDataNotImplement => GetResourceString("ISupportInitDataNotImplement")!;
        /// <summary>{objType} doesn't implement ISupportInitData, but it has a configuration file.</summary>
        internal static string FormatISupportInitDataNotImplement(object? objType)
           => string.Format(Culture, GetResourceString("ISupportInitDataNotImplement", new[] { "objType" }), objType);

        /// <summary>urn could not be resolved at level {level}.</summary>
        public static string @UrnCouldNotBeResolvedAtLevel => GetResourceString("UrnCouldNotBeResolvedAtLevel")!;
        /// <summary>urn could not be resolved at level {level}.</summary>
        internal static string FormatUrnCouldNotBeResolvedAtLevel(object? level)
           => string.Format(Culture, GetResourceString("UrnCouldNotBeResolvedAtLevel", new[] { "level" }), level);

        /// <summary>invalid node.</summary>
        public static string @InvalidNode => GetResourceString("InvalidNode")!;
        /// <summary>The query must have at least one property to return.</summary>
        public static string @NoPropertiesRequested => GetResourceString("NoPropertiesRequested")!;
        /// <summary>Failed to retrieve data for this request.</summary>
        public static string @FailedRequest => GetResourceString("FailedRequest")!;
        /// <summary>Incorrect version tag. You must specify either a min_major, cloud_min_major or datawarehouse_enabled attribute. 
        /// 
        /// {elementContent}</summary>
        public static string @IncorrectVersionTag => GetResourceString("IncorrectVersionTag")!;
        /// <summary>Incorrect version tag. You must specify either a min_major, cloud_min_major or datawarehouse_enabled attribute. 
        /// 
        /// {elementContent}</summary>
        internal static string FormatIncorrectVersionTag(object? elementContent)
           => string.Format(Culture, GetResourceString("IncorrectVersionTag", new[] { "elementContent" }), elementContent);

        /// <summary>Value '{value}' on attribute '{attributeName}' was unable to be parsed correctly.</summary>
        public static string @InvalidAttributeValue => GetResourceString("InvalidAttributeValue")!;
        /// <summary>Value '{value}' on attribute '{attributeName}' was unable to be parsed correctly.</summary>
        internal static string FormatInvalidAttributeValue(object? value, object? attributeName)
           => string.Format(Culture, GetResourceString("InvalidAttributeValue", new[] { "value", "attributeName" }) ?? "", value, attributeName);

        /// <summary>Version was not specified on loading configuration file.</summary>
        public static string @NullVersionOnLoadingCfgFile => GetResourceString("NullVersionOnLoadingCfgFile")!;
        /// <summary>Enumeration object tag was not found.</summary>
        public static string @EnumObjectTagNotFound => GetResourceString("EnumObjectTagNotFound")!;
        /// <summary>Connection type is not valid.</summary>
        public static string @InvalidConnectionType => GetResourceString("InvalidConnectionType")!;
        /// <summary>Only Path or only FullName must be specified.</summary>
        public static string @OnlyPathOrFullName => GetResourceString("OnlyPathOrFullName")!;
        /// <summary>Database name must be specified.</summary>
        public static string @DatabaseNameMustBeSpecified => GetResourceString("DatabaseNameMustBeSpecified")!;
        /// <summary>File {fileName} was not found.
        /// The file might be missing in sources or misspelled in config.xml (note: file name in config.xml is case sensitive)</summary>
        public static string @FailedToLoadResFile => GetResourceString("FailedToLoadResFile")!;
        /// <summary>File {fileName} was not found.
        /// The file might be missing in sources or misspelled in config.xml (note: file name in config.xml is case sensitive)</summary>
        internal static string FormatFailedToLoadResFile(object? fileName)
           => string.Format(Culture, GetResourceString("FailedToLoadResFile", new[] { "fileName" }), fileName);

        /// <summary>{objType} is not supported in dependency discovery. Only objects of the following types are supported: {suppList}.</summary>
        public static string @UnsupportedTypeDepDiscovery => GetResourceString("UnsupportedTypeDepDiscovery")!;
        /// <summary>{objType} is not supported in dependency discovery. Only objects of the following types are supported: {suppList}.</summary>
        internal static string FormatUnsupportedTypeDepDiscovery(object? objType, object? suppList)
           => string.Format(Culture, GetResourceString("UnsupportedTypeDepDiscovery", new[] { "objType", "suppList" }), objType, suppList);

        /// <summary>Cannot provide DataReader because of the properties: {propList}</summary>
        public static string @QueryNotSupportedPostProcess => GetResourceString("QueryNotSupportedPostProcess")!;
        /// <summary>Cannot provide DataReader because of the properties: {propList}</summary>
        internal static string FormatQueryNotSupportedPostProcess(object? propList)
           => string.Format(Culture, GetResourceString("QueryNotSupportedPostProcess", new[] { "propList" }), propList);

        /// <summary>failed to create Urn for object code: {objCode}</summary>
        public static string @FailedToCreateUrn => GetResourceString("FailedToCreateUrn")!;
        /// <summary>failed to create Urn for object code: {objCode}</summary>
        internal static string FormatFailedToCreateUrn(object? objCode)
           => string.Format(Culture, GetResourceString("FailedToCreateUrn", new[] { "objCode" }), objCode);

        /// <summary>{prop} must be specified on {obj}.</summary>
        public static string @PropMustBeSpecified => GetResourceString("PropMustBeSpecified")!;
        /// <summary>{prop} must be specified on {obj}.</summary>
        internal static string FormatPropMustBeSpecified(object? prop, object? obj)
           => string.Format(Culture, GetResourceString("PropMustBeSpecified", new[] { "prop", "obj" }), prop, obj);

        /// <summary>Urn is not valid for dependency discovery: {urn}</summary>
        public static string @InvalidUrnForDepends => GetResourceString("InvalidUrnForDepends")!;
        /// <summary>Urn is not valid for dependency discovery: {urn}</summary>
        internal static string FormatInvalidUrnForDepends(object? urn)
           => string.Format(Culture, GetResourceString("InvalidUrnForDepends", new[] { "urn" }), urn);

        /// <summary>too many database levels.</summary>
        public static string @TooManyDbLevels => GetResourceString("TooManyDbLevels")!;
        /// <summary>Configuration file is not valid.</summary>
        public static string @InvalidConfigurationFile => GetResourceString("InvalidConfigurationFile")!;
        /// <summary>invalid configuration file: missing section {section}.</summary>
        public static string @MissingSection => GetResourceString("MissingSection")!;
        /// <summary>invalid configuration file: missing section {section}.</summary>
        internal static string FormatMissingSection(object? section)
           => string.Format(Culture, GetResourceString("MissingSection", new[] { "section" }), section);

        /// <summary>object is not under database.</summary>
        public static string @NotDbObject => GetResourceString("NotDbObject")!;
        /// <summary>objects are not in a single database.</summary>
        public static string @NotSingleDb => GetResourceString("NotSingleDb")!;
        /// <summary>&lt;post_process&gt; tag missing class_name.</summary>
        public static string @NoClassNamePostProcess => GetResourceString("NoClassNamePostProcess")!;
        /// <summary>Operation not supported on version {version}.</summary>
        public static string @InvalidVersion => GetResourceString("InvalidVersion")!;
        /// <summary>Operation not supported on version {version}.</summary>
        internal static string FormatInvalidVersion(object? version)
           => string.Format(Culture, GetResourceString("InvalidVersion", new[] { "version" }), version);

        /// <summary>Operation not supported on {productName}.</summary>
        public static string @InvalidSqlServer => GetResourceString("InvalidSqlServer")!;
        /// <summary>Operation not supported on {productName}.</summary>
        internal static string FormatInvalidSqlServer(object? productName)
           => string.Format(Culture, GetResourceString("InvalidSqlServer", new[] { "productName" }), productName);

        /// <summary>The database name must be specified in the urn: {urn}</summary>
        public static string @DatabaseNameMustBeSpecifiedinTheUrn => GetResourceString("DatabaseNameMustBeSpecifiedinTheUrn")!;
        /// <summary>The database name must be specified in the urn: {urn}</summary>
        internal static string FormatDatabaseNameMustBeSpecifiedinTheUrn(object? urn)
           => string.Format(Culture, GetResourceString("DatabaseNameMustBeSpecifiedinTheUrn", new[] { "urn" }), urn);

        /// <summary>Failed to retrieve dependency information ({rowInformation}).</summary>
        public static string @CouldNotGetInfoFromDependencyRow => GetResourceString("CouldNotGetInfoFromDependencyRow")!;
        /// <summary>Failed to retrieve dependency information ({rowInformation}).</summary>
        internal static string FormatCouldNotGetInfoFromDependencyRow(object? rowInformation)
           => string.Format(Culture, GetResourceString("CouldNotGetInfoFromDependencyRow", new[] { "rowInformation" }), rowInformation);

        /// <summary>SQL Server 2005</summary>
        public static string @SqlServer90Name => GetResourceString("SqlServer90Name")!;
        /// <summary>SQL Server 2000</summary>
        public static string @SqlServer80Name => GetResourceString("SqlServer80Name")!;
        /// <summary>The property '{propertyName}' is not valid from the type '{typeName}' context</summary>
        public static string @InvalidTypeForProperty => GetResourceString("InvalidTypeForProperty")!;
        /// <summary>The property '{propertyName}' is not valid from the type '{typeName}' context</summary>
        internal static string FormatInvalidTypeForProperty(object? propertyName, object? typeName)
           => string.Format(Culture, GetResourceString("InvalidTypeForProperty", new[] { "propertyName", "typeName" }), propertyName, typeName);

        /// <summary>Invalid URN.</summary>
        public static string @InvalidUrn => GetResourceString("InvalidUrn")!;
        /// <summary>URN starts with an unknown root element '{root}'.</summary>
        public static string @UnknownDomain => GetResourceString("UnknownDomain")!;
        /// <summary>URN starts with an unknown root element '{root}'.</summary>
        internal static string FormatUnknownDomain(object? root)
           => string.Format(Culture, GetResourceString("UnknownDomain", new[] { "root" }), root);

        /// <summary>No provider is available for URN '{urn}'.</summary>
        public static string @NoProvider => GetResourceString("NoProvider")!;
        /// <summary>No provider is available for URN '{urn}'.</summary>
        internal static string FormatNoProvider(object? urn)
           => string.Format(Culture, GetResourceString("NoProvider", new[] { "urn" }), urn);

        /// <summary>Unknown level '{level}' in URN '{urn}'.</summary>
        public static string @LevelNotFound => GetResourceString("LevelNotFound")!;
        /// <summary>Unknown level '{level}' in URN '{urn}'.</summary>
        internal static string FormatLevelNotFound(object? level, object? urn)
           => string.Format(Culture, GetResourceString("LevelNotFound", new[] { "level", "urn" }), level, urn);

        /// <summary>Empty key '{key}' in URN '{urn}'.</summary>
        public static string @InvalidKeyValue => GetResourceString("InvalidKeyValue")!;
        /// <summary>Empty key '{key}' in URN '{urn}'.</summary>
        internal static string FormatInvalidKeyValue(object? key, object? urn)
           => string.Format(Culture, GetResourceString("InvalidKeyValue", new[] { "key", "urn" }), key, urn);

        /// <summary>URN '{urn}' has one or more missing keys at level '{level}'.</summary>
        public static string @MissingKeys => GetResourceString("MissingKeys")!;
        /// <summary>URN '{urn}' has one or more missing keys at level '{level}'.</summary>
        internal static string FormatMissingKeys(object? urn, object? level)
           => string.Format(Culture, GetResourceString("MissingKeys", new[] { "urn", "level" }), urn, level);

        /// <summary>Server name is missing from URN '{urn}'.</summary>
        public static string @ServerNameMissing => GetResourceString("ServerNameMissing")!;
        /// <summary>Server name is missing from URN '{urn}'.</summary>
        internal static string FormatServerNameMissing(object? urn)
           => string.Format(Culture, GetResourceString("ServerNameMissing", new[] { "urn" }), urn);

        /// <summary>Domain root for type '{fullTypeName}' is unknown. Cannot retrieve logical version.</summary>
        public static string @DomainRootUnknown => GetResourceString("DomainRootUnknown")!;
        /// <summary>Domain root for type '{fullTypeName}' is unknown. Cannot retrieve logical version.</summary>
        internal static string FormatDomainRootUnknown(object? fullTypeName)
           => string.Format(Culture, GetResourceString("DomainRootUnknown", new[] { "fullTypeName" }), fullTypeName);

        /// <summary>Property not supported for current server version.</summary>
        public static string @PropertyNotsupported => GetResourceString("PropertyNotsupported")!;
        /// <summary>This object is not supported on Azure Synapse Analytics databases.</summary>
        public static string @ObjectNotSupportedOnSqlDw => GetResourceString("ObjectNotSupportedOnSqlDw")!;

    }
}
