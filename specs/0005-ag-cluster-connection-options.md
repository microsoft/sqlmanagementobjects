# Spec 0005: Availability Group ClusterConnectionOptions for TDS 8.0

**Status:** Implemented  
**Created:** 2025-11-13  
**Implemented:** Commit 847f3027f633c4ba10423c666cf2ead419a9a946  
**Authors:** Dawei Wang  

## 1. Summary

Add support for the `CLUSTER_CONNECTION_OPTIONS` property on Availability Groups. This property specifies ODBC connection string options used by Windows Server Failover Clustering (WSFC) to connect to SQL Server, enabling TDS 8.0 secure connections between the cluster and SQL Server instances.

## 2. Motivation

SQL Server 2025 introduces TDS 8.0, which provides enhanced security features including:

- **Strict encryption:** TLS 1.3 support with mandatory certificate validation
- **Certificate-based authentication:** Modern authentication patterns for cluster connectivity

For WSFC-based Availability Groups, the cluster nodes need to communicate with SQL Server using ODBC. The `ClusterConnectionOptions` property allows administrators to configure these connections with TDS 8.0 settings like `Encrypt=Strict` and `HostNameInCertificate`.

## 3. Target SQL Server Versions

| Version | Support |
|---------|---------|
| SQL Server 2025 (v17.x) | ✅ Fully supported |
| SQL Server 2022 and earlier | ❌ Not applicable |

**Cluster Type Requirement:** WSFC only (not applicable to Linux clusters or NONE cluster type)

**Catalog View:** `sys.availability_groups`  
**Relevant Column:** `cluster_connection_options` (nvarchar)

## 4. DDL Syntax

### 4.1 CREATE AVAILABILITY GROUP

```sql
CREATE AVAILABILITY GROUP [ag_name]
WITH (
    AUTOMATED_BACKUP_PREFERENCE = SECONDARY,
    CLUSTER_TYPE = WSFC,
    CLUSTER_CONNECTION_OPTIONS = N'Encrypt=Strict;HostNameInCertificate=server.domain.com;'
)
FOR DATABASE [db1], [db2]
REPLICA ON ...
GO
```

### 4.2 ALTER AVAILABILITY GROUP

```sql
ALTER AVAILABILITY GROUP [ag_name]
SET (CLUSTER_CONNECTION_OPTIONS = N'Encrypt=Strict;HostNameInCertificate=server.domain.com;')
GO
```

### 4.3 Common Connection Options

| Option | Description | Example |
|--------|-------------|---------|
| `Encrypt` | Encryption mode | `Strict`, `Mandatory`, `Optional` |
| `HostNameInCertificate` | Expected hostname in server certificate | `server.domain.com` |
| `TrustServerCertificate` | Whether to trust self-signed certificates (not recommended for TDS 8.0) | `Yes`, `No` |

## 5. SMO Implementation

### 5.1 Properties

**AvailabilityGroup Class:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ClusterConnectionOptions` | `string` | `""` (empty) | ODBC connection string options for WSFC connectivity |

**AvailabilityGroupData Class (HadrData):**

| Property | Type | Description |
|----------|------|-------------|
| `ClusterConnectionOptions` | `string` | Data transfer object property for wizard/task flows |

### 5.2 Helper Method

The `AvailabilityGroup` class includes a helper method for appending key/value pairs:

```csharp
public void SetClusterConnectionOptions(string key, string value)
```

This appends options in the format `key=value;` to the existing string.

### 5.3 Scripting Behavior

| Value | Script Action |
|-------|---------------|
| Empty/null | Property omitted from script |
| Non-empty | `CLUSTER_CONNECTION_OPTIONS = N'<value>'` included |

### 5.4 Code Locations

- **SMO Object:** [AvailabilityGroup.cs](../src/Microsoft/SqlServer/Management/Smo/AvailabilityGroup.cs)
- **Data Object:** [AvailabilityGroupData.cs](../src/Microsoft/SqlServer/Management/HadrData/AvailabilityGroupData.cs)
- **Task:** [CreateAvailabilityGroupTask.cs](../src/Microsoft/SqlServer/Management/HadrModel/CreateAvailabilityGroupTask.cs)
- **XML Metadata:** [AvailabilityGroup.xml](../src/Microsoft/SqlServer/Management/SqlEnum/xml/AvailabilityGroup.xml)

### 5.5 XML Metadata

```xml
<version min_major='17'>
  <property name="ClusterConnectionOptions" type="nvarchar">
    ISNULL(AG.cluster_connection_options, N'')
  </property>
</version>
```

### 5.6 cfg.xml Entry

```xml
<property name="ClusterConnectionOptions" generate="true"/>
```

## 6. Version Guarding

The implementation uses `IsSupportedProperty` to check for SQL Server 2025+:

```csharp
if (availabilityGroup.IsSupportedProperty(nameof(availabilityGroup.ClusterConnectionOptions)))
{
    availabilityGroup.ClusterConnectionOptions = this.availabilityGroupData.ClusterConnectionOptions;
}
```

This ensures the property is only set when connected to a supported SQL Server version.

## 7. Testing

### 7.1 Functional Tests

Test scenarios:
1. **Default (not set):** Verify `ClusterConnectionOptions` is empty when not specified
2. **Empty string:** Verify setting to empty string and ALTER works correctly
3. **Non-empty value:** Verify appended key/value pairs with trailing semicolon

**Test Location:** [HadrTests.cs](../src/FunctionalTest/SmoInternal/HighAvailability/HadrTests.cs) (SmoInternal - requires multi-server WSFC environment)

### 7.2 Test Restrictions

- Windows only (`[UnsupportedHostPlatform(SqlHostPlatforms.Linux)]`)
- SQL Server 2025+ (`MinMajor = 17`)
- WSFC cluster type only

## 8. Platform Considerations

| Platform | Support |
|----------|---------|
| Windows + WSFC | ✅ Full support |
| Linux | ❌ Not applicable (no WSFC) |
| Azure SQL MI | ❌ Not applicable (managed clustering) |

## 9. Documentation References

- [CREATE AVAILABILITY GROUP (Transact-SQL)](https://learn.microsoft.com/sql/t-sql/statements/create-availability-group-transact-sql)
- [ALTER AVAILABILITY GROUP (Transact-SQL)](https://learn.microsoft.com/sql/t-sql/statements/alter-availability-group-transact-sql)
- [sys.availability_groups](https://learn.microsoft.com/sql/relational-databases/system-catalog-views/sys-availability-groups-transact-sql)
- [TDS 8.0 and TLS 1.3 support](https://learn.microsoft.com/sql/relational-databases/security/networking/tds-8)
- [Always On Availability Groups Overview](https://learn.microsoft.com/sql/database-engine/availability-groups/windows/overview-of-always-on-availability-groups-sql-server)
