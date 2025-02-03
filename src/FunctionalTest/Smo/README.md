# Intro

SMO tests are designed to executed by the vstest runner. We use NUnit for its fluent asserts and constraints which are extremely expressive.
See [ToolsConnectionInfo.xml](./ToolsConnectionInfo.xml) to learn how to point the tests at a SQL Server instance.

## Test philosophy

1. The name of the test should specify what is being tested and what the expected behavior is. Long names are great! Having the object type as the initial part of the method name helps group related tests visually and enables finding them in Test Explorer with a filter.
2. The test log should be sufficient to identify the reason for a test failure. We shouldn't have to go digging in the source code or run the test locally in the debugger to understand what failed.
3. All bug fixes must be accompanied by a test which would have failed before the fix and now passes with it.
4. SMO PR verification builds measure differential code coverage. We have a goal of covering 80% of all new code with tests.


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
