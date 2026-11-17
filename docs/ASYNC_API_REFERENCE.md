# Quick Reference: ServerConnection Async Methods

## New Public APIs

### ExecuteNonQueryAsync
```csharp
// Execute single command
Task<int> rowCount = await serverConnection.ExecuteNonQueryAsync(
    "CREATE TABLE Test (Id INT)", 
    cancellationToken);

// Execute batch
var commands = new List<string> { "INSERT INTO Test VALUES (1)", "INSERT INTO Test VALUES (2)" };
Task<int> totalRows = await serverConnection.ExecuteNonQueryAsync(commands, cancellationToken);
```

### ExecuteReaderAsync
```csharp
using (SqlDataReader reader = await serverConnection.ExecuteReaderAsync(
    "SELECT * FROM Test", 
    cancellationToken))
{
    while (await reader.ReadAsync(cancellationToken))
    {
        var id = reader.GetInt32(0);
    }
}
```

### ExecuteWithResultsAsync
```csharp
// Single command
DataTable results = await serverConnection.ExecuteWithResultsAsync(
    "SELECT * FROM Test", 
    cancellationToken);

// Multiple commands (results merged)
var queries = new List<string> { "SELECT 1 AS Col1", "SELECT 2 AS Col2" };
DataTable merged = await serverConnection.ExecuteWithResultsAsync(queries, cancellationToken);
```

### ExecuteScalarAsync
```csharp
object result = await serverConnection.ExecuteScalarAsync(
    "SELECT COUNT(*) FROM Test", 
    cancellationToken);
int count = Convert.ToInt32(result);
```

## Internal Implementation

### ConnectionManager.ExecuteTSqlAsync
```csharp
protected async Task<object> ExecuteTSqlAsync(
    ExecuteTSqlAction action,
    object execObject,
    bool catchException,
    CancellationToken cancellationToken)
```

**Actions Supported:**
- `ExecuteNonQuery` → `SqlCommand.ExecuteNonQueryAsync()`
- `ExecuteReader` → `SqlCommand.ExecuteReaderAsync()`
- `ExecuteScalar` → `SqlCommand.ExecuteScalarAsync()`
- `FillDataSet` → `ExecuteReaderAsync()` + `PopulateDataTableAsync()`

### Helper Methods
```csharp
// Async DataTable population from SqlDataReader
private async Task<DataTable> PopulateDataTableAsync(
    SqlCommand command, 
    CancellationToken cancellationToken)

// Async exception handling with retry
private async Task<(bool shouldReturn, object result)> HandleExecuteExceptionAsync(
    SqlException exc,
    ExecuteTSqlAction action,
    object execObject,
    bool catchException,
    CancellationToken cancellationToken)

// Async database validation
private async Task<bool> IsDatabaseValidAsync(
    SqlConnection sqlConnection,
    string dbName,
    CancellationToken cancellationToken)
```

## CapturedSql Mode

When `SqlExecutionModes.CaptureSql` is enabled:
```csharp
serverConnection.SqlExecutionModes = SqlExecutionModes.CaptureSql;

// Commands are captured but not executed
await serverConnection.ExecuteNonQueryAsync("SELECT 1"); // Returns 0
await serverConnection.ExecuteScalarAsync("SELECT 1");   // Returns null
var dt = await serverConnection.ExecuteWithResultsAsync("SELECT 1"); // Empty DataTable
var reader = await serverConnection.ExecuteReaderAsync("SELECT 1");  // Returns null

// Review captured SQL
string capturedSql = serverConnection.CapturedSql.Text;
```

## Cancellation

```csharp
var cts = new CancellationTokenSource();
cts.CancelAfter(5000); // Cancel after 5 seconds

try
{
    await serverConnection.ExecuteNonQueryAsync("WAITFOR DELAY '00:01:00'", cts.Token);
}
catch (OperationCanceledException)
{
    // Query was cancelled
}
catch (ExecutionFailureException)
{
    // SQL Server reported the cancellation
}
```

## Batch Execution Behavior

```csharp
var commands = new List<string>
{
    "INSERT INTO Test VALUES (1)", // Executes
    "WAITFOR DELAY '00:01:00'",   // Gets cancelled
    "INSERT INTO Test VALUES (2)"  // Does NOT execute
};

var cts = new CancellationTokenSource();
cts.CancelAfter(500);

try
{
    await serverConnection.ExecuteNonQueryAsync(commands, cts.Token);
}
catch (OperationCanceledException)
{
    // First INSERT succeeded, second INSERT never ran
    // No automatic rollback
}
```

## Error Handling

```csharp
try
{
    await serverConnection.ExecuteNonQueryAsync("INVALID SQL");
}
catch (ExecutionFailureException ex)
{
    // Contains SqlException details
    SqlException sqlEx = ex.InnerException as SqlException;
    Console.WriteLine($"Error {sqlEx?.Number}: {sqlEx?.Message}");
}
```

## Retry Logic

The async implementation includes automatic retry for:
- **Connection failures** - Reopens connection and retries once
- **Token expiration** - Refreshes access token and retries once
- **Database context loss** - Restores database context and retries

```csharp
// Automatic retry happens internally for transient failures
await serverConnection.ExecuteNonQueryAsync("SELECT 1");
// If connection drops, it will automatically:
// 1. Detect the failure
// 2. Reopen the connection
// 3. Retry the command once
```

## Performance Logging

When enabled, performance metrics are logged:
```csharp
// Logs query execution time
// Warns on long-running queries (>5 seconds)
// Logs timeout events
```

## ConfigureAwait Usage

All internal await calls use `ConfigureAwait(false)`:
```csharp
// Internal SMO code
var result = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

// Consumer code (your application)
// You can use ConfigureAwait as appropriate for your context
await serverConnection.ExecuteNonQueryAsync("SELECT 1"); // or .ConfigureAwait(false)
```

## Testing Examples

### Unit Test Pattern
```csharp
[TestMethod]
public async Task MyAsyncTest()
{
    var conn = new ServerConnection(new SqlConnection(connectionString));
    conn.SqlExecutionModes = SqlExecutionModes.CaptureSql; // No real DB needed
    
    var result = await conn.ExecuteNonQueryAsync("SELECT 1");
    
    Assert.AreEqual(0, result); // Captured mode returns 0
    Assert.IsTrue(conn.CapturedSql.Text.Contains("SELECT 1"));
}
```

### Functional Test Pattern
```csharp
[TestMethod]
public async Task MyFunctionalTest()
{
    await ExecuteFromDbPoolAsync(async (db) =>
    {
        var conn = db.Parent.ConnectionContext;
        
        var result = await conn.ExecuteNonQueryAsync("CREATE TABLE #Temp (Id INT)");
        
        Assert.AreEqual(0, result); // No rows affected by CREATE TABLE
    });
}
```

## Migration from Sync to Async

**Before (Sync):**
```csharp
int rows = serverConnection.ExecuteNonQuery("INSERT INTO Test VALUES (1)");
DataSet results = serverConnection.ExecuteWithResults("SELECT * FROM Test");
object scalar = serverConnection.ExecuteScalar("SELECT COUNT(*) FROM Test");
```

**After (Async):**
```csharp
int rows = await serverConnection.ExecuteNonQueryAsync("INSERT INTO Test VALUES (1)");
DataTable results = await serverConnection.ExecuteWithResultsAsync("SELECT * FROM Test"); // Note: DataTable not DataSet
object scalar = await serverConnection.ExecuteScalarAsync("SELECT COUNT(*) FROM Test");
```

## Common Patterns

### Transaction Support
```csharp
// Async methods respect existing transaction context
serverConnection.BeginTransaction();
try
{
    await serverConnection.ExecuteNonQueryAsync("INSERT INTO Test VALUES (1)");
    await serverConnection.ExecuteNonQueryAsync("INSERT INTO Test VALUES (2)");
    serverConnection.CommitTransaction();
}
catch
{
    serverConnection.RollBackTransaction();
    throw;
}
```

### Statement Splitting
```csharp
// GO batches are automatically split
string script = @"
    CREATE TABLE Test1 (Id INT)
    GO
    CREATE TABLE Test2 (Id INT)
    GO
";

await serverConnection.ExecuteNonQueryAsync(script);
// Executes as two separate commands
```

### Multiple Result Sets
```csharp
// Use ExecuteReaderAsync for multiple result sets
string query = "SELECT 1 AS Col1; SELECT 2 AS Col2";

using (var reader = await serverConnection.ExecuteReaderAsync(query))
{
    // First result set
    while (await reader.ReadAsync())
    {
        Console.WriteLine(reader.GetInt32(0));
    }
    
    // Move to next result set
    await reader.NextResultAsync();
    
    // Second result set
    while (await reader.ReadAsync())
    {
        Console.WriteLine(reader.GetInt32(0));
    }
}
```

## Troubleshooting

### Issue: Deadlock when calling async method
**Solution:** Make sure you're using `await` properly and not blocking with `.Result` or `.Wait()`

### Issue: CancellationToken not cancelling
**Solution:** Ensure the token is passed all the way through: `await method(cancellationToken)`

### Issue: ExecuteWithResultsAsync returns empty DataTable
**Solution:** Check if `SqlExecutionModes.CaptureSql` is enabled

### Issue: Multiple result sets missing
**Solution:** Use `ExecuteReaderAsync()` with `NextResultAsync()` instead of `ExecuteWithResultsAsync()`

## Best Practices

1. ✅ **Always pass CancellationToken** - Enables cancellation support
2. ✅ **Use ConfigureAwait(false)** in library code - Avoids context capture
3. ✅ **Dispose SqlDataReader** - Use `using` statement
4. ✅ **Handle OperationCanceledException** - User-initiated cancellation
5. ✅ **Handle ExecutionFailureException** - SQL execution errors
6. ✅ **Test with CapturedSql mode** - Unit tests without DB
7. ✅ **Wrap batches in transactions** - If atomicity required
8. ❌ **Don't block on async** - Never use `.Result` or `.Wait()`
9. ❌ **Don't assume DataSet** - `ExecuteWithResultsAsync` returns `DataTable`
10. ❌ **Don't modify captured SQL** - Read-only in capture mode
