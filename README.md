# SQL Management Objects
SQL Management Objects, or SMO, provides objects and APIs to discover, modify, and script out SQL Server entities. 

## Documentation
See https://docs.microsoft.com/sql/relational-databases/server-management-objects-smo/overview-smo?view=sql-server-ver15

## Usage
SMO binaries are distributed via the Microsoft.SqlServer.SqlManagementObjects nuget package, available from Nuget.org.

## Versioning
The major version for each SMO release corresponds with the highest Sql Server compatibility level that version of SMO supports. For example, 140 means it supports SQL Server 2017 and below. Some features of SMO may require having a matching SQL Server version in order to work effectively, but most features are fully backward compatible. 

## Dependents
SMO is a integral part of the SQL Server ecosystem. A broad set of client tools, engine components, and service components rely on it extensively. The set of SMO dependents includes:
- Azure Data Studio/Sql Tools Service
- Sql Server Management Studio
- Sql Server Integration Services (SSIS)
- Sql Powershell module
- Sql Data Sync service
- Polybase
- Azure Sql Database
- Microsoft Dynamics
- Sql Server SCOM Management Pack

# Contributing

## Types of contributions
- Please open issues related to bugs or other deficiencies in SMO on the [Issues](https://github.com/microsoft/sqlmanagementobjects/issues) feed of this repo. 
    - Include SMO version where the issue was found
    - Include as much of the source code to reproduce the issue as possible
    - Ask for sample code for areas where you find the docs lacking
- If you are a SMO application developer, we welcome contributions to the [wiki](https://github.com/microsoft/sqlmanagementobjects/wiki) or even source code samples to illustrate effective ways to use SMO in applications.


## Stuff our attorney added
This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
