# Object definition files

These files are read during both Code Generation time (for creating the class definition files) and during runtime (for generating the queries used to populate/modify the objects)

## IMPORTANT! The nodes defined in the settings node MUST be defined in a specific order. Putting them out of order will cause issues as the reader ($\Sql\ssms\smo\SMO\Enumerator\sql\src\SqlObject.cs) will throw an exception when it tries to read the improperly-ordered node.  The order is this

    1. distinct
    2. parent_link
    3. fail_condition
    4. request_parent_select
    5. include
    6. property_link
    7. prefix
    8. postfix
    9. post_process
    10. orderby_redirect
    11. special_query

The ordering of property_link/prefix/postfix seems to be gated on whether you have both min_major and max_major defined for a given property_link.
If you have both defined, you can put the prefix/postfix version tag right below the corresponding property_link version section. If you only define min_major,
the matching prefix/postfix section has to come after all other sections that contain a property_link.

### BEST PRACTICE IS TO SPLIT prefix AND postfix TAGS INTO SEPARATE MATCHING version SECTIONS SO YOU CAN READILY SORT ALL prefix ENTRIES BEFORE ALL postfix ENTRIES

## Elements

Reference [xmlread.cs](../XmlRead.CS)

### EnumObject

The root element. Has a number of elements defining how the class is generated
type -The name of the class type this file is representing
min_major -The minimum major version (On-Prem) this will be used in
cloud_min_Major - The minimum major version (Cloud) this will be used in
datawarehouse_enabled - Whether this object is enabled for Datawarehouse, in addition to the other versions specified by the version tags (including non-DW Azure databases)

### property_Link

Defines a join to a table. See [SqlPropertyLink.cs](../SqlPropertyLink.cs) for how this is implemented

    table- Object is added as a FROM clause
    join - Object is joined via a JOIN clause
    left_join - Object is joined via a LEFT JOIN clause
    fields- The fields which trigger this property link to be applied (skipped if none of the properties listed are requested)
    
    The join type is determined by the attributes of the node, in this order (the first one it finds is the one it'll use, others are ignored):
    - table
    - join 
    - left_join

### parent_link

Defines a property whose value is pulled from the parent object

parent -The name of the property on the parent object
local -The name of the property on the child object (will call parentObj.ParentProperty)

### version

Used to selectively include/exclude other elements based on the version specified. If the connection does not meet the requirements specified by the various attributes

    min_major-The minimum major version (On-Prem) the contents of this element will be included in
    max_major-The maximum major version (On-Prem) the contents of this element will be included in
    min_minor-The minimum minor version (On-Prem) the contents of this element will be included in
    max_minor-The maximum minor version (On-Prem) the contents of this element will be included in
    min_build-The minimum build version (On-Prem) the contents of this element will be included in
    max_build-The maximum build version (On-Prem) the contents of this element will be included in
    cloud_Min_major- The minimum major version (Cloud) the contents of this element will be included in
    cloud_Max_major- The maximum major version (Cloud) the contents of this element will be included in
    cloud_Min_minor- The minimum minor version (Cloud) the contents of this element will be included in
    cloud_Max_minor- The maximum minor version (Cloud) the contents of this element will be included in
    cloud_Min_build - The minimum build version (Cloud) the contents of this element will be included in
    cloud_Max_build - The maximum build version (Cloud) the contents of this element will be included in
    datawarehouse_enabled - Whether this object is enabled for Datawarehouse, in addition to the other versions specified by the version tags (including non-DW Azure databases)

### property

Provides the definition of a property. This is used by codegen and during runtime to populate the Property value (usually from the value returned from the SQL query for the object)

    report_type- Specifies the CLR type for this property. The value here will be prefixed with the SMO namespace, so it NEEDS to be used for any SMO types so that the correct SMO namespace is used (since engine builds its own from the same source)
    
    report_type2 -Specifies the CLR type for this property. The value will be used as is, so should be used for any non-SMO types
    type- Specifies the T-SQL type (varchar for example) which is then mapped to the appropriate CLR type for the property. See $\Sql\mpu\shared\SMO\Enumerator\sql\src\Util.cs!DbTypeToClrType for the mapping
    smo_class_name - For post_process elements only. Specifies the name of the class that the post_process element is updating. Will be prefixed with the SMO namespace so NEEDS to be used for any SMO types so that the correct SMO namespace is used (since engine builds its own from the same source)
    
    class_name  - For post_process elements only. Specifies the name of the class that the post_process element is updating. The value is used as is so should be used for any non-SMO types
    cast - This will make the generated t-SQL cast the property to the type specified with the type attribute (so type='bit' cast='true' will result in the T-SQL looking like CAST(myPropValue AS bit). Use this if the original type is not what you want.
    mode - The modes this property is available for. Maps to the PropertyMode enum, which is defined in $\sql\mpu\shared\managementsdk\sfc\enumerator\core\src\ObjectProperty.cs. This will add the SfcPropertyFlags.Design or SfcPropertyFlags.Deploy attributes to the property. Doesn't appear to actually do anything except for mark the property as being usable for either the Design or Deploy modes of SFC SDK. 
        Properties that make sense in the design (offline) mode should have mode="design" set. Properties available in the online mode (should be most SMO properties) should have mode="deploy". If both are true then mode="ALL" should be set. 
    expensive - Marks the property as being expensive to fetch - which means that when it is accessed only that property will be fetched (default behavior is to fetch all properties whenever a non-initialized property value is requested). 
        
        This also marks properties that will NOT be pre-populated when calling Script(), so if the property is needed for scripting and is marked as expensive then the Script methods need to ensure that they use the appropriate Get methods that will fetch it if it hasn't been fetched already. 
                
                ○ NOTE : If this attribute is set then calls to Properties.Get (or any of the other methods which don't query for a property) will return NULL until the property is requested via one of the methods that do. See Ways to Retrieve Property in SMO for details on these methods. 
    hidden - Whether the property is hidden from the user. If this attribute exists (it doesn't matter what the value is) then the property will not have a 
    usage - What this property can be used for in SFC requests. Valid values are :
                • Request (maps to ObjectPropertyUsages.Request)
                • Filter (maps to ObjectPropertyUsages.Filter
                • Order (maps to ObjectPropertyUsages.OrderBy)
        
        If both this and notusage DO NOT exist then the property defaults to :
        
                • Hidden = FALSE : ObjectPropertyUsages.All, which is an OR of all three values
                • Hidden = TRUE : None of the above values (only Reserved1 is set)
        
        Only one attribute of usage or notusage is allowed - if both are specified then usage will be used and notusage ignored.
        
        NOTE : Any properties which are marked for PostProcessing will automatically have the Filter and OrderBy usages removed from the list of valid usages. This is because the values are calculated client-side so can't be used to generate the request query sent to the server. SqlObject.cs!Load (look for the computedProperties HashMap)
    notusage - What this property CAN'T be used for in SFC requests. Valid values are :
        
                • request (maps to ObjectPropertyUsages.Request)
                • filter (maps to ObjectPropertyUsages.Filter
                • order (maps to ObjectPropertyUsages.OrderBy)
        
        If both this and notusage DO NOT exist then the property defaults to :
        
                • hidden = FALSE : ObjectPropertyUsages.All, which is an OR of all three values
                • hidden = TRUE : None of the above values (only Reserved1 is set)
        
        Only one attribute of usage or notusage is allowed - if both are specified then usage will be used and notusage ignored.
        
        NOTE : Any properties which are marked for PostProcessing will automatically have the Filter and OrderBy usages removed from the list of valid usages. This is because the values are calculated client-side so can't be used to generate the request query sent to the server. SqlObject.cs!Load (look for the computedProperties HashMap)

### post_process

Post Processing is for doing additional calculations on the returned data. This allows client-side manipulation of the data before the property values are set.

- class_name: The name of the class that handles the post-processing. Should extend [PostProcess](../PostProcess.cs)
- fields: The list of fields which will cause the post-processing to happen
- triggered_fields: The list of fields that are needed to compute the value for the field requested by the user
