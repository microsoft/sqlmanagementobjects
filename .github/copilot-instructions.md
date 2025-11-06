<!--
NOTE: This file provides instructions to the GitHub Copilot coding agent for working effectively in this repository.
Add ONLY non-sensitive operational guidance. Do not place secrets here.
-->

# Copilot Repository Instructions (SMO)

## 1. Overview
This repository implements SQL Management Objects (SMO). SMO provides a rich object model to create, alter, drop, enumerate, and script SQL Server / Azure SQL database objects.

Primary goals when contributing:
1. Maintain backward compatibility of public APIs.
2. Preserve scripting determinism (idempotent + stable ordering).
3. Ensure test coverage for every bug fix or behavioral change.

Use the `#solution` context keyword in Copilot Chat to supply full solution-wide context for better answers.


## 2. Tech Stack & Build
Language: C# (.NET).
Build system: MSBuild / `dotnet build` via solution `dirs.sln` (aggregator) or project-level builds.
Delay-signed assemblies are required for testing inside SSMS.
Generated / intermediate content lives under `obj/` and must never be edited directly.
When invoking "dotnet build" or "dotnet test" set DOTNET_ROLL_FORWARD=Major to avoid SDK version mismatches.
If localization projects fail to build, you can use `/p:EnableLocalization=false` to skip them.

### Build Shortcuts (via init.cmd)
After running `init.cmd`, these doskey aliases are available:
- `bsmont` - Build SMO without tests (faster when you don't need test assemblies)
- `msb` - MSBuild with localization disabled
- `rtests` - Run tests from bin\debug\net472

### Key Directories
- `src/` – Primary source.
- `SmoBuild/` & `Build/` – Build infrastructure scripts/config.
- `docs/` – Public documentation artifacts.

### Do Not Modify
- `obj/` generated files.

## 3. Code Generation & Adding Properties
When adding a property to an existing SMO object:
1. Update `src/codegen/cfg.xml` (define metadata).
2. Update the object's XML under `src/Microsoft/SqlServer/Management/SqlEnum/xml`.
3. Regenerate if a codegen step exists (follow existing patterns; prefer invoking existing build targets instead of ad-hoc scripts).
4. Add tests validating:
	 - Reflection/name exposure (use `nameof(PropertyName)` not string literals).
	 - Scripting behavior (included/excluded when default vs. non-default).
	 - Serialization (if applicable).

## 4. Adding a new datatype
When adding a new datatype:
1. Ensure that its properties (such as length, precision or scale) are included in SPParams.xml, UDF.xml and UDFParams.xml.
2. Update code marked with a `// SMO_NEW_DATATYPE` comment to handle the new datatype.

## 4A. Adding a new specialized index type
When adding support for a new specialized index type (e.g., Vector Index, Spatial Index, XML Index):
1. **Define enums in `enumstructs.cs`**: Add the enum type to `IndexType` and any supporting enums for configuration (e.g., `VectorIndexMetric`). Use `[TsqlSyntaxString]` attributes to map enum values to T-SQL syntax strings.
2. **Update `index.xml`**: Add version-gated `property_link` elements joining to the appropriate system catalog views (e.g., `sys.vector_indexes`). Define properties with appropriate version gates (min_major, min_build). Return empty strings for unsupported versions to avoid breaking queries.
3. **Update `cfg.xml`**: Add property definitions with `generate="true"` and `is_intrinsic="true"` (if needed for scripting). Properties marked intrinsic are batched during enumeration and included in `GetScriptFields()`.
4. **Implement specialized scripter in `IndexScripter.cs`**: Create a new class derived from `IndexScripter` (e.g., `VectorIndexScripter`). Override `ScriptCreateHeaderDdl()` for the CREATE statement, `ScriptIndexOptions()` for WITH clause parameters, and `Validate()` for validation logic. Add case to `GetIndexScripter()` switch statement.
5. **Add validation methods**: Implement `Check[IndexType]Properties()` to validate that properties specific to this index type aren't set on other index types. Call from `CheckRegularIndexProperties()`.
6. **Use version helpers**: Use `ThrowIfBelowVersionXXX()` or `ThrowIfNotCloudAndBelowSpecifiedVersion()` for version validation. Add new version helpers to `SqlSmoObject.cs` if needed.
7. **Handle complex properties**: For properties containing JSON or complex structures, create a dedicated class (e.g., `VectorIndexBuildProperties`) to deserialize and provide strongly-typed access. Reference `System.Text.Json` if using JsonObject/JsonNode.
8. **Version-gate property_link carefully**: When SQL versions (e.g., MI CTP vs GA) have schema differences, use min_build/max_build to avoid querying non-existent catalog views. Return empty defaults for unsupported versions.
   - Example: Vector indexes require build 950+ for SQL Server 2025. Use `<version min_major='17' max_major='17' min_minor='0' max_minor='0' min_build="950" cloud_min_major='12'>` to gate the property_link.
   - For versions below the threshold, define fallback properties that return empty strings (e.g., `N''`) to avoid query failures.
9. **Update test baselines**: Add integration tests using dedicated baseline databases (see `DbSetup_XXX_All.sql`). Update all version-specific baseline XML files to include new properties with appropriate exception types (UnsupportedVersionException, UnknownPropertyException) for older versions.
10. **Run CodeGen**: Regenerate partial classes by building the solution, which runs CodeGen.exe over cfg.xml.

## 5. Testing Conventions
Framework: NUnit (assert style) with VSTest discovery attributes (`[TestClass]`, `[TestMethod]`, etc.). Use NUnit constraints inside methods for clarity.

Rules:
- Every bug fix must ship with at least one test that fails prior to the fix and passes after.
- Tests using internal infrastructure or tenant resources must live in the `SmoInternal` project.
- Long, descriptive test method names: Start with the SMO object type being tested, then scenario, then expected outcome.
- Always include assertion messages clarifying intent & expected result.
- Logs should make failures diagnosable without rerunning with a debugger.

Pattern example (pseudo):
```
// ObjectType_Scenario_ExpectedResult
[TestMethod]
public void Table_CreateWithFileGroup_IncludesFileGroupInScript() { /* ... */ }
```

### Test Versioning and Targeting
- Use `[SupportedServerVersionRange]` to specify version ranges (MinMajor, MaxMajor, Edition, HostPlatform).

### Test Environment Notes
- Only delay-signed binaries can be loaded in SSMS for integration-style validation.
- Running tests against the SQL Managed Instance does not work over AzVpn except from an Azure VM.
- Test resources live in the TME tenant. Authenticate first:
	- `az login --tenant 70a036f6-8e4d-4615-bad6-149c02e7720d`
	- Or sign in via Visual Studio to that tenant.

## 6. Coding Guidelines
- Prefer `nameof(PropertyOrType)` over hard-coded strings.
- Avoid adding links to internal (non-public) resources in comments.
- Keep public API surface stable; if change is unavoidable, document rationale in the PR description + changelog.
- Follow existing nullability / exception patterns in similar classes before introducing new patterns.
- Use expressive NUnit constraint assertions (`Assert.That(x, Is.Not.Null, "...context...")`).

## 7. Common Tasks for Copilot
| Task | Guidance |
|------|----------|
| Add new SMO property | Follow Section 3; add tests per Section 5. |
| Add new specialized index type | Follow Section 4A step-by-step; reference Vector Index implementation as example. |
| Fix bug in scripting | Reproduce with a failing test; fix; ensure deterministic script ordering. |
| Add integration test hitting internal MI | Place in `SmoInternal`; guard with environment/tenant checks. |
| Refactor constant string names | Replace with `nameof`; ensure no breaking rename side-effects. |
| Improve test diagnostics | Add assertion messages & context logging (but no secrets). |
| Add version-specific helper | Add `ThrowIfBelowVersionXXX()` to SqlSmoObject.cs following existing pattern. |
| Handle JSON properties | Create strongly-typed wrapper class (see `VectorIndexBuildProperties.cs`); reference System.Text.Json. |

## 8. Do / Avoid
Do:
- Add a failing test before a fix.
- Keep changes minimal & localized.
- Update documentation if behavior shifts.
- Use strongly-typed wrapper classes for JSON properties from catalog views (e.g., `VectorIndexBuildProperties` wrapping JSON from `sys.vector_indexes.build_parameters`).
- Add XML comments to all public classes and properties for IntelliSense support.

Avoid:
- Editing `obj/` or generated artifacts manually.
- Introducing hard-coded server names, credentials, or internal URLs.
- Placing secret values or tokens in source or tests.
- Publishing internal-only folder content to public mirrors.
- Exposing raw JSON strings in public SMO APIs; wrap them in typed classes.

## 9. Security & Compliance
- Never commit credentials, connection strings with auth info, or access tokens.
- Sanitize test output so it omits sensitive hostnames or tenant-specific secrets.
- Use environment variables or secure configuration providers for runtime secrets (not stored in repo).


## 11. Performance Considerations
When modifying enumeration or scripting logic:
- Avoid excessive round-trips to SQL Server.
- Batch queries or reuse existing metadata caches where feasible.
- Maintain lazy evaluation patterns already present.
- Mark properties as `is_intrinsic="true"` in cfg.xml if they are commonly needed for scripting. Intrinsic properties are prefetched in batch queries (via `GetScriptFields()` and stored in `XSchemaProps`), while non-intrinsic properties are fetched on-demand (stored in `XRuntimeProps`).
- When adding properties to index.xml or other XML metadata files, use `property_link` with `left_join` to batch-fetch related data from system views in a single query per collection.


## 12. Review Checklist (Quick)
Before concluding a change ask Copilot (or yourself):
1. Is there a test proving correctness (and guarding regressions)?
2. Are any public APIs altered? If yes, documented?
3. Did I avoid editing generated or mirrored exclusion folders?
4. Are all new property names using `nameof` where needed?
5. Are logs/assert messages clear & actionable?

## 13. Troubleshooting
- Build errors in generated code: Ensure codegen inputs (`cfg.xml` & XML metadata) are valid; re-run full solution build.
- Tests failing only in CI: Check for dependency on local environment (tenant login missing). Add explicit skip or setup logic.
- Script differences: Confirm ordering rules and compare with existing similar object's script builder.
- **Version compatibility issues**: When adding features for new SQL versions, always include version guards using `min_major`, `min_build`, and `max_build` attributes in XML metadata. For Managed Instance (MI), which may lag behind on-premises releases, use build-level version gates to prevent querying catalog views that don't exist yet. Return empty/default values for unsupported versions rather than failing queries.
- **Missing property errors**: If you see PropertyMissingException during scripting, the property likely needs to be added to `GetScriptFields()` in the base class and marked as `is_intrinsic="true"` in cfg.xml for batch fetching.
- **Test baseline updates**: When modifying object properties, run baseline verification tests and apply regenerated baselines. Baseline XML files under `Baselines/SqlvXXX/` directories must be updated to include new properties with appropriate exception markers (UnsupportedVersionException, UnknownPropertyException) for versions that don't support them.

## 14. Contact / Escalation
Use normal repository contribution channels (PR reviewers / CODEOWNERS-defined maintainers). Do not embed private distribution lists here.

## 15. Supplemental READMEs
Many subdirectories include a focused `README.md` describing domain specifics (e.g., code generation nuances, build/task scripts, test harness caveats). When performing a change in a given folder:
1. Look for the nearest `README.md` walking upward from the target file.
2. Prefer those localized instructions over generic assumptions (they may specify required build props, environment variables, or regeneration steps).
3. If instructions appear outdated, note discrepancies in your PR description and (ideally) update the README in the same change—small, surgical edits only.
4. Do not duplicate content: link (relative path) to the authoritative README section instead of copying.

Agent Hint: Before adding or altering code in an unfamiliar area, read the local README to pick up naming, nullability, threading, and performance patterns to mirror.

---
Concise Canonical Rules (TL;DR):
1. Always add a failing test for a bug fix.
2. Use `nameof` not magic strings.
3. Never edit `obj/` or include internal links.
4. Properties: update `cfg.xml` + corresponding XML metadata.

