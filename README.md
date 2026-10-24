# SQL Management Objects

SQL Management Objects (SMO) provides objects and APIs to discover, modify, and script SQL Server entities.

## Documentation

See the [SMO documentation](https://learn.microsoft.com/sql/relational-databases/server-management-objects-smo/overview-smo).

## Packages

- [Microsoft.SqlServer.SqlManagementObjects](https://www.nuget.org/packages/Microsoft.SqlServer.SqlManagementObjects/) is the primary development SDK for SMO.
- Microsoft.SqlServer.SqlManagementObjects.Loc provides localized resources.
- Microsoft.SqlServer.Management.SmoMetadataProvider provides completion support for T-SQL language services.

See [CHANGELOG.md](CHANGELOG.md) for release changes.

## Build

Install the .NET SDK specified in `global.json`, then run:

```powershell
$env:DOTNET_ROLL_FORWARD = "Major"
dotnet build /p:EnableLocalization=false
```

Functional tests require your own SQL Server instances. Configure test connections using your own `ToolsConnectionInfo.xml`; the public workflow configuration under `.github/workflows` provides an example using an environment variable for the password. Do not commit credentials.

## Contributing

Please report bugs and feature requests through the [GitHub issues](https://github.com/microsoft/sqlmanagementobjects/issues), including the SMO version and a reproduction where possible.

See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and [SECURITY.md](SECURITY.md).

## License

This project is licensed under the [MIT license](LICENSE).
