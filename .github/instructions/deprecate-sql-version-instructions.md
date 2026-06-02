# Deprecating Support for Old SQL Server Versions

This document provides comprehensive, step-by-step instructions for removing support for older SQL Server versions from the SMO (SQL Management Objects) codebase. Use this guide when SQL Server versions reach end of extended support.

## Overview

The version deprecation process involves removing dead code paths, simplifying version checks, and updating XML metadata across the SMO codebase. The goal is to make **minimal, surgical changes** that remove obsolete code without breaking public APIs or introducing logic errors.

**Critical Principle**: When removing support for version X and earlier, the new minimum supported version becomes X+1. Any code that checks `if (version <= X)` or `if (version == X)` for versions X and below becomes dead code and should be **removed entirely**, not modified to check for version X+1.

**Example Context**: This guide uses SQL Server 2005 (major version 9) as the example for deprecation, making SQL Server 2008 (major version 10) the new minimum. Replace version numbers appropriately for your deprecation target.

## Prerequisites

- Understand the [SQL Server versioning scheme](https://learn.microsoft.com/en-us/troubleshoot/sql/general/determine-version-edition-update-level): 
  - SQL Server 2005 = Major version 9
  - SQL Server 2008 = Major version 10
  - SQL Server 2008 R2 = Major version 10.50
  - SQL Server 2012 = Major version 11
  - etc.
- Review the requirements document specifying which versions to deprecate
- Have a build environment set up (see main README.md)
- Run tests frequently to catch introduced bugs

## Key Principles

### 1. Preserve Public API Compatibility

**DO**:
- Keep all public constants, enumerations, and type definitions
- Preserve version-related enum values (e.g., `SqlServerVersion.Version90`)
- Maintain compatibility level enums (e.g., `CompatibilityLevel.Version90`)
- Keep scripting target version options

**DON'T**:
- Remove or rename public enum values
- Change method signatures
- Remove public classes or interfaces
- Modify public constants

### 2. Understand Version Check Logic

Before modifying any version check, understand what it does:

**Common patterns**:
- `ServerVersion.Major == X` - Code executes ONLY for version X
- `ServerVersion.Major >= X` - Code executes for version X and later
- `ServerVersion.Major < X` - Code executes for versions before X
- `ServerVersion.Major > X` - Code executes for versions after X
- `ServerVersion.Major <= X` - Code executes for version X and earlier

**When minimum supported version becomes 10 (SQL Server 2008)**:
- `Major == 9` → **Remove block** (dead code, never executes)
- `Major < 10` → **Remove block** (always false, never executes)
- `Major <= 9` → **Remove block** (always false, never executes)
- `Major >= 10` → **Always true** - simplify by removing check or entire if statement
- `Major > 9` → **Always true** - simplify by removing check
- `Major >= 9` → **Always true** - simplify by removing check

### 3. Common Mistakes to Avoid

❌ **WRONG**: Changing version numbers in comparisons
```csharp
// Before
if (ServerVersion.Major == 9) { /* SQL 2005 code */ }
else { /* SQL 2008+ code */ }

// WRONG - Changes 9 to 10
if (ServerVersion.Major == 10) { /* This is now dead code! */ }
else { /* SQL 2012+ code */ }
```

✅ **CORRECT**: Remove the dead code block entirely
```csharp
// CORRECT - Remove the entire if/else and keep SQL 2008+ code
/* SQL 2008+ code directly here */
```

❌ **WRONG**: Keeping always-true conditions
```csharp
// Before
if (ServerVersion.Major >= 9) { /* Code for SQL 2005+ */ }

// WRONG - Changes to >= 10 (always true)
if (ServerVersion.Major >= 10) { /* This condition is meaningless now */ }
```

✅ **CORRECT**: Remove the condition entirely
```csharp
// CORRECT - Remove the if statement, keep the code
/* Code executes unconditionally for all supported versions */
```

## Step-by-Step Instructions

### Phase 1: XML Metadata Updates

**Files**: `src\Microsoft\SqlServer\Management\SqlEnum\xml\*.xml`

#### 1.1 Update MinMajor Attributes

For each XML file, update the minimum version:

```xml
<!-- Before -->
<EnumObject type="Database" min_major="7">

<!-- After - Set to new minimum (10 for SQL 2008) -->
<EnumObject type="Database" min_major="10">
```

**Process**:
1. Search for `min_major` attributes
2. Change any value less than the new minimum to the new minimum
3. Common values to update: `min_major="7"`, `min_major="8"`, `min_major="9"`

#### 1.2 Remove Obsolete Version Snippets

Remove entire `<version>` blocks where `max_major` is less than the new minimum:

```xml
<!-- REMOVE - Only applies to SQL 2005 and earlier -->
<version min_major="7" max_major="9">
  <property_link table="#tmp_help_db" alias="d1" field="db_name" />
  <!-- ... -->
</version>
```

**Process**:
1. Search for `max_major` attributes
2. If `max_major` value is less than new minimum (e.g., `max_major="9"` when minimum is 10), remove entire block
3. Look for combinations like `min_major="7" max_major="9"` or just `max_major="8"`

#### 1.3 Remove Version-Gated Properties for Old Versions

Remove `<property>` elements that only exist for deprecated versions:

```xml
<!-- REMOVE - Property only in SQL 2005 -->
<property name="OldProperty" type="property" access="Read">
  <version min_major="8" max_major="9">
    <link_multiple no="1" expression="{0}">
      <!-- ... -->
    </link_multiple>
  </version>
</property>
```

**Key files to check**:
- `Database.xml`
- `Table.xml`
- `StoredProcedure.xml`
- `View.xml`
- `Index.xml`
- All files in the `xml` directory

### Phase 2: C# Code Analysis and Cleanup

**Focus Projects** (as specified in requirements):
- `Microsoft.SqlServer.ConnectionInfo.csproj`
- `Microsoft.SqlServer.Management.Sdk.Sfc.csproj`
- `Microsoft.SqlServer.Smo.csproj`
- `Microsoft.SqlServer.SqlEnum.csproj`

#### 2.1 Identify Version Check Patterns

**Search for common patterns**:
```powershell
# PowerShell - Search for version checks
Get-ChildItem -Path "src\" -Include "*.cs" -Recurse | Select-String "ServerVersion\.Major" | Select-Object Path, LineNumber, Line

# Search for specific version numbers
Get-ChildItem -Path "src\" -Include "*.cs" -Recurse | Select-String "\b(Version90|Version80|Version70)\b" | Select-Object Path, LineNumber, Line
```

**Patterns to find**:
- `ServerVersion.Major`
- `ConnectionContext.ServerVersion.Major`
- `server.Version.Major`
- `sp.TargetServerVersion`
- `DatabaseEngineType` (may combine with version checks)
- Build number comparisons (e.g., `9.0.4230` for SQL 2005 SP3 CU5)

#### 2.2 Handle Switch Statements

**Pattern**: Switch on `ServerVersion.Major`

```csharp
// Before
switch (ServerVersion.Major)
{
    case 7:
    case 8:
    case 9:
        // SQL 2000/2005 code
        scriptSql80Syntax = true;
        break;
    case 10:
    default:
        // SQL 2008+ code
        scriptSql90Syntax = true;
        break;
}

// After - Remove cases for versions < 10
switch (ServerVersion.Major)
{
    case 10:
    default:
        // SQL 2008+ code
        scriptSql90Syntax = true;
        break;
}

// Or if only one case remains, remove switch entirely
scriptSql90Syntax = true;
```

**Process**:
1. Remove `case` labels for versions below minimum
2. If only one case remains, consider removing the switch entirely
3. Ensure `default:` case handles current and future versions appropriately

#### 2.3 Handle If/Else Chains

**Pattern 1**: Exact version match (dead code removal)

```csharp
// Before
if (ServerVersion.Major == 9)
{
    // SQL 2005 specific code
    AppendDdl(builder, "OLD_SYNTAX");
}
else
{
    // SQL 2008+ code
    AppendDdl(builder, "NEW_SYNTAX");
}

// After - Remove entire if/else, keep the else block content
AppendDdl(builder, "NEW_SYNTAX");
```

**Pattern 2**: Less-than check (always false removal)

```csharp
// Before
if (ServerVersion.Major < 10)
{
    // SQL 2005 and earlier code
    UseOldMethod();
}
else
{
    // SQL 2008+ code
    UseNewMethod();
}

// After - Remove if/else, keep else block content
UseNewMethod();
```

**Pattern 3**: Greater-than-or-equal check (always true simplification)

```csharp
// Before
if (ServerVersion.Major >= 9 && SomeOtherCondition)
{
    // Code for SQL 2005+
    DoSomething();
}

// After - Remove version check, keep other conditions
if (SomeOtherCondition)
{
    DoSomething();
}
```

**Pattern 4**: Complex nested conditions

```csharp
// Before
if (ServerVersion.Major >= 9)
{
    if (TargetServerVersion >= SqlServerVersion.Version90)
    {
        // Code that scripts for SQL 2005+ targets
        ScriptNewSyntax();
    }
}
else
{
    // Old SQL 2000 scripting
    ScriptOldSyntax();
}

// After - Remove always-true server version check, keep target check, remove dead else
if (TargetServerVersion >= SqlServerVersion.Version90)
{
    ScriptNewSyntax();
}
```

**Important**: When a condition has multiple parts, analyze each part:
- If ALL parts are always true → Remove entire condition
- If SOME parts are always true → Remove only those parts, keep others
- Keep conditions that check `TargetServerVersion` (scripting target, not server version)

#### 2.4 Handle Build Number Comparisons

**Pattern**: Build number comparisons for specific versions

```csharp
// Before
int sql2005Sp3Cu5 = 4230; // SQL 2005 SP3 CU5
if (ServerVersion.Major == 9 && ServerVersion.BuildNumber >= sql2005Sp3Cu5)
{
    // Feature added in SQL 2005 SP3 CU5
    UseFeature();
}

// After - Remove dead code and unused variable
// If feature is standard in SQL 2008+, remove condition entirely
UseFeature();

// OR if feature still needs checking for SQL 2008 builds
int sql2008RequiredBuild = 1600; // Example
if (ServerVersion.Major == 10 && ServerVersion.BuildNumber >= sql2008RequiredBuild)
{
    UseFeature();
}
```

**Process**:
1. Identify if the build check is for a deprecated version
2. Remove the entire condition if feature is standard in new minimum version
3. If feature still requires build checking for supported versions, update accordingly

#### 2.5 Handle GetSystemUserName Pattern

**Common pattern**: Conditional prefix based on version

```csharp
// Before
string prefix;
if (ServerVersion.Major >= 9)
{
    prefix = "sys";
}
else
{
    prefix = "dbo";
}
return prefix + "." + userName;

// After - Remove condition, use only the modern value
string prefix = "sys";
return prefix + "." + userName;

// Or even simpler
return "sys." + userName;
```

#### 2.6 Handle Try-Catch-Finally with Version Checks

**Pattern**: Version checks inside exception handling

```csharp
// Before
try
{
    if (ServerVersion.Major < 10)
    {
        ExecuteOldMethod();
    }
    else
    {
        ExecuteNewMethod();
    }
}
finally
{
    Cleanup();
}

// After - Simplify
try
{
    ExecuteNewMethod();
}
finally
{
    Cleanup();
}
```

### Phase 3: Comments and Documentation

#### 3.1 Update Version-Related Comments

```csharp
// Before
// For SQL 2005 and later, use new syntax
// WRONG: Leave as-is (now misleading)

// After
// For SQL 2008 and later, use new syntax
// OR
// For all supported versions, use new syntax
```

#### 3.2 Remove Obsolete Comments

```csharp
// Before
// TODO: Remove SQL 2000 compatibility code after 2005 RTM

// After - Remove obsolete TODO
```

### Phase 4: Testing and Validation

#### 4.1 Build Validation

```powershell
# Set environment variable to avoid SDK version issues
$env:DOTNET_ROLL_FORWARD = "Major"

# Build
dotnet build dirs.sln
```

#### 4.2 Test Execution

Run tests to ensure no regressions:
```powershell
# Run all SMO tests
dotnet test --no-build --logger "console;verbosity=normal"

# Or use init.cmd shortcuts
rtests         # .NET Framework tests
netcoretests   # .NET Core tests
```

#### 4.3 Check for Unintended Changes

```powershell
# Review what files changed
git status --porcelain

# Review diff statistics
git diff --numstat

# Ensure no excessive deletions or unexpected files
# Deleted lines should typically be < 2x inserted lines per file
```

### Phase 5: Update Documentation

#### 5.1 Update CHANGELOG.md

Add an entry describing the changes:

```markdown
## [Unreleased]

### Removed
- **Breaking Behavior Change**: Removed internal code paths for SQL Server 2005 and earlier versions. 
  - Minimum supported server version is now SQL Server 2008 (version 10.0).
  - Public APIs remain unchanged; all version-related enumerations and constants are preserved for backward compatibility.
  - Applications targeting SQL Server 2005 or earlier will need to use an older version of SMO.
  - Internal version checks for SQL Server 2005 and earlier have been removed, simplifying code paths.
```

#### 5.2 Update README if Needed

If the main README mentions supported versions, update it:

```markdown
## Supported SQL Server Versions

SMO supports the following SQL Server versions:
- SQL Server 2008 and later (version 10.0+)
- Azure SQL Database
- Azure SQL Managed Instance
```

## Verification Checklist

Before completing the deprecation work, verify:

- [ ] All XML files updated (min_major, removed max_major blocks)
- [ ] No `case X:` statements for versions < minimum in switch blocks
- [ ] No `if (Major == X)` for versions < minimum
- [ ] No `if (Major < minimum)` conditions remain
- [ ] All `if (Major >= minimum)` simplified or removed
- [ ] Build number comparisons for old versions removed
- [ ] No always-true conditions left in code
- [ ] No unreachable else blocks remain
- [ ] Version-related comments updated or removed
- [ ] CHANGELOG.md updated with summary
- [ ] All changes compile without errors
- [ ] Tests pass (no new failures introduced)
- [ ] Public APIs unchanged (no breaking changes)
- [ ] Git diff reviewed for unintended changes

## Common Files to Review

Based on previous deprecation work, these files often contain version checks:

**SMO Core Objects**:
- `DatabaseBase.cs` - Many version checks for database-level features
- `TableBase.cs` - Table scripting and features
- `IndexBase.cs`, `IndexScripter.cs` - Index types and options
- `StoredProcedureBase.cs` - Stored procedure scripting
- `ViewBase.cs` - View options and scripting
- `UserBase.cs`, `UserDefinedFunctionBase.cs` - User and UDF features
- `ServerBase.cs` - Server-level version checks
- `DatabaseRoleBase.cs` - Role features

**Scripting Infrastructure**:
- `ScriptingOptions.cs` - Target version handling
- `ScriptingPreferences.cs` - Default versions
- `SqlSmoObject.cs` - Base object version utilities

**Utilities**:
- `SmoUtility.cs` - Helper methods with version logic
- `PermissionWorker.cs` - Permission scripting
- `ParamBase.cs` - Parameter handling

**Connection and Metadata**:
- `SqlConnectionInfo.cs` - Connection version detection
- `Enumerator.cs` - Metadata enumeration
- `propertiesMetadata.cs` - Property version support

## Troubleshooting

### Issue: Build Errors After Removing Code

**Symptom**: Compiler errors about missing variables or unreachable code

**Solution**:
- Check if removed code defined variables used later
- Ensure you removed entire dead branches, not just conditions
- Look for warning about unreachable code (sign of incomplete cleanup)

### Issue: Tests Failing After Changes

**Symptom**: Tests that passed before now fail

**Solution**:
- Review git diff for the specific file
- Check if you removed code that wasn't actually version-specific
- Verify conditions were truly always-true/always-false
- Look for target version checks (TargetServerVersion) that should remain

### Issue: Too Many Lines Deleted

**Symptom**: A file shows hundreds of deletions with few additions

**Solution**:
- Review the diff carefully - may indicate over-deletion
- Check if you removed code that applies to all versions
- Verify you didn't remove entire methods by accident
- Rule of thumb: Deleted lines should be < 2x inserted lines per file

### Issue: Always-True Condition Left in Code

**Symptom**: Code reviewer flags `if (Major >= 10)` as always true

**Solution**:
```csharp
// Before (after incorrect fix)
if (ServerVersion.Major >= 10 && TargetServerVersion >= SqlServerVersion.Version90)
{
    ScriptNewSyntax();
}

// After (correct fix)
if (TargetServerVersion >= SqlServerVersion.Version90)
{
    ScriptNewSyntax();
}
```

### Issue: Dead Code Created by Incorrect Fix

**Symptom**: Code reviewer flags `if (Major == 10)` combined with SQL 2005 build check

**Solution**:
```csharp
// Before (incorrect fix)
int sql2005Build = 4230;
if (ServerVersion.Major == 10 && ServerVersion.BuildNumber >= sql2005Build)
{
    // This is nonsense - checking SQL 2008 version against SQL 2005 build
    UseFeature();
}

// After (correct fix)
// Remove entire block if feature is standard in SQL 2008+
UseFeature();
```

## Best Practices

1. **Make changes iteratively**: Don't try to fix everything at once
2. **Build and test frequently**: Catch errors early
3. **Review diffs before committing**: Use `git diff` to verify changes are minimal and correct
4. **Understand before modifying**: Read surrounding code to understand intent
5. **Preserve intent, not literal code**: If code handled both old and new versions, keep only new version handling
6. **Use think tool**: When uncertain about a complex version check, use the think tool to analyze it
7. **Test on real servers**: If possible, test against minimum supported version (SQL 2008)

## Contact / Escalation

If you encounter complex version logic that's unclear:
- Review git history for context (`git log -p -- <file>`)
- Check for related bug work items or PRs
- Consult with team members familiar with that code area
- Add `// TODO: Verify this change` comments for review

---

## Quick Reference: Version Check Decision Tree

```
Is this a version check for version X (deprecated version or lower)?
│
├─ YES: Is it checking ServerVersion (actual server)?
│   │
│   ├─ YES: Is it a comparison?
│   │   │
│   │   ├─ == X or <= X or < (X+1)  →  REMOVE block (dead code)
│   │   ├─ >= (X+1) or > X          →  REMOVE condition (always true)
│   │   └─ >= X                     →  REMOVE condition (always true)
│   │
│   └─ NO: Is it checking TargetServerVersion (scripting target)?
│       │
│       └─ KEEP IT (controls scripting output, not runtime logic)
│
└─ NO: Not a version check for deprecated version
    └─ LEAVE UNCHANGED
```

---

**Remember**: The goal is minimal, surgical changes that remove obsolete code paths while preserving all public APIs and maintaining the semantic meaning of the code.
