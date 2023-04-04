# Overview

`GetSmoObject` is a small SMO-based application to generate the script for an object in an Azure SQL Database. It demonstrates the following:
- Establishing a connection using Azure Active Directory interactive authentication
- Getting an instance of a `SqlSmoObject` using `Server.GetSmoObject`
- Using `Scripter` to generate the `CREATE` script for the object
- Capturing `SqlClient` events such as connections and queries

## Usage

```
GetSmoObject serverName databaseName "Server/Database[@Name='databaseName']/Table[@Name='tableName' and Schema='schemaName']"
```

## Examples of URNs

From WideWorldImporters

```
Server/Database[@Name='WideWorldImporters']/Table[@Name='SystemParameters' and @Schema='Application']
Server/Database[@Name='WideWorldImporters']/StoredProcedure[@Name='AddRoleMemberIfNonexistent' and @Schema='Application']

```


## Potential enhancements

This sample has lots of room for growth! Some possibilities:

- Enable other authentication types so it can be used with on premises servers
- Enable use of other URN roots than `Server`, such as `XEStore` and `DatabaseXEStore`
- Add parameters to redirect the script or the SqlClient traces to files
