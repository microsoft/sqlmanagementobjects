# Intro

SMO tests are designed to executed by the vstest runner. We use NUnit for its fluent asserts and constraints which are extremely expressive.

## Test philosophy

1. The name of the test should specify what is being tested and what the expected behavior is. Long names are great! Having the object type as the initial part of the method name helps group related tests visually and enables finding them in Test Explorer with a filter.
2. The test log should be sufficient to identify the reason for a test failure. We shouldn't have to go digging in the source code or run the test locally in the debugger to understand what failed.
3. All bug fixes must be accompanied by a test which would have failed before the fix and now passes with it.
4. SMO PR verification builds measure differential code coverage. We have a goal of covering 80% of all new code with tests.

## Most common test types

All object types should be included in the database setup scripts under src\functionaltest\framework\scripts. Name the test object `SmoBaselineVerification_<objecttype>`. Additionally, create a baseline xml file for it under src\functionaltest\smo\scriptingtests\smobaselineverification\baselines, for each version of SQL it is relevant for.
Having the object there enables coverage in these tests automatically:

- src\functionaltest\smo\scriptingtests\smobaselineverification\verifysmobaselinesbase.cs
- src\functionaltest\smo\ScriptingTests\GenerateScriptsModelTests\VerifyScriptPublishModel.cs
- src\FunctionalTest\Smo\Transfer\TransferScriptingTests.cs

Please run all those tests against the functionaltestsettings.runsettings file to recreate and verify baselines affected by your objects. The tests have log output with instructions on updating the baselines as they change.

### Baseline Tests

Baselines tests verify the logic for reading the properties of database objects by:
- creating the objects as specified in setup scripts
- constructing a SMO object from the database object
- comparing the values with those specified in baseline xml

NOTE:
- Using values which are not defaults, and where the T-Sql value is not the value literal is most effective at catching bugs.

### Scripting Tests
Scripting tests are located under src\FunctionalTest\Smo\ScriptingTests. These can be be used validate arbitary logic, e.g. which can't be validated through baseline tests.

NOTE:
- Invoke `<object>.Refresh()` when mutating objects, e.g. alter an object and verify the altered value, to ensure the updated value is being used.

## External resources and secrets

### Azure Key Vault

- Store secrets in https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/88d5392f-a34f-4769-b405-f597fc533613/resourceGroups/SqlToolsSecretStore_RG/providers/Microsoft.KeyVault/vaults/SqlToolsSecretStore/overview
- The AzureKeyVaultHelper class is hard-coded to use that location
- The script de-tokenizer converts references of the form `$(SecretStore:secretName/Secret)` to a lookup in that AKV for a secret with the name secretName

### Azure storage

- Put public, anonymously accessible blobs in https://sqltools.blob.core.windows.net/public
- Put SAS keys or storage access keys for other blobs in the AKV

## Test Categorization

Use "Legacy" category to mark tests that cover old/deprecated functionality and shouldn't be run during Pull Request validation. 

## Implementation notes

### SqlTestBase

Test classes should inherit from SqlTestBase. If for some reason your test precludes such inheritance, replicate its workaround for NUnit Assert's incompatibility with the vstest runner.

#### ExecuteWithDBDrop

This method is the most common way to wrap test code in a delegate that gets invoked once for each supported target server version. It creates a new database using an optional name prefix and optional edition, invokes the test delegate, then deletes the database.

##### Database caching

For tests that aren't compatible with Azure SQL DW, you can use ExecuteFromDbPool to recycle an existing database associated with the current server. This Database object is guaranteed to have been created on the same thread as the test execution.

### Use NUnit constraint-based asserts

NUnit asserts print nice error messages when they fail, like the contents of a collection or subsets of mismatched strings showing where the comparison failed.
