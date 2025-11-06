// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;


namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Base SmoObjectTestBase class for SMO Scripting Tests
    /// </summary>
    public abstract class SmoObjectTestBase : SmoTestBase
    {
        /// <summary>
        /// Generates a Smo object name in form of:
        /// {objName}_{testIdentifier}
        /// </summary>
        /// <returns></returns>
        internal string GenerateSmoObjectName(string objName, int maxLength = int.MaxValue)
        {
            var tempName = objName + "_" + (this.TestContext.TestName ?? "");
            return tempName.Length > maxLength ? tempName.Substring(tempName.Length - maxLength) : tempName;
        }

        /// <summary>
        /// Generates an unique Smo object name in form of:
        /// {smoObjName}{uniqueIdentifier}
        /// </summary>
        /// <returns></returns>
        internal string GenerateUniqueSmoObjectName(string objName, int maxLength = int.MaxValue)
        {
            var guidStr = Guid.NewGuid().ToString();

            return
                (maxLength >= guidStr.Length)
                ? GenerateSmoObjectName(objName, maxLength - guidStr.Length) + guidStr
                : guidStr.Substring(guidStr.Length - maxLength);
        }

        /// <summary>
        /// Create Smo object.
        /// <param name="obj">Smo object.</param>
        /// </summary>
        protected virtual void CreateSmoObject(_SMO.SqlSmoObject obj)
        {
            ((ICreatable)obj).Create();
            var endpoint = obj as _SMO.Endpoint;
            if (endpoint != null && endpoint.ProtocolType == _SMO.ProtocolType.Tcp)
            {
                //Creating a new TCP Endpoint makes it so connecting via TCP fails for public. We have to specifically enable it again
                // See https://social.msdn.microsoft.com/Forums/sqlserver/en-US/29abd791-1ef7-4191-9740-ea9100b7705d/error-18456-severity-14-state-12?forum=sqlsecurity
                obj.ExecutionManager.ExecuteNonQuery(@"GRANT CONNECT ON ENDPOINT::[TSQL Default TCP] to [public]");
            }
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected abstract void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify);

        /// <summary>
        /// Verify whether SMO object exists in the database
        /// </summary>
        /// <param name="database">Smo database.</param>
        /// <param name="fullQualifiedName">Smo object full qualified name.</param>
        protected void VerifyObjectExists(_SMO.Database database, String fullQualifiedName)
        {
            String sqlCommand = "SELECT OBJECT_DEFINITION(OBJECT_ID('" + SmoObjectHelpers.SqlEscapeSingleQuote(fullQualifiedName) + "'))";
            var result = database.ExecutionManager.ConnectionContext.ExecuteScalar(sqlCommand);
            Assert.NotNull(result, string.Format("The object {0} does not exist in the database.", fullQualifiedName));
        }

        /// <summary>
        /// Verify ScriptCreateOrAlter() obtained the expected script
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="expectedScriptText"></param>
        protected void VerifyScriptCreateOrAlter(_SMO.SqlSmoObject obj, string expectedScriptText)
        {
            var script = new StringCollection();
            var prefs = new _SMO.ScriptingPreferences();
            // These preferences emulate the defaults in SSMS 
            prefs.IncludeScripts.Header = true;
            prefs.IncludeScripts.DatabaseContext = true;
            prefs.IncludeScripts.SchemaQualify = true;
            prefs.TargetDatabaseEngineEdition = DatabaseEngineEdition.Standard;
            prefs.TargetDatabaseEngineType = DatabaseEngineType.Standalone;
            prefs.TargetServerVersion = _SMO.SqlServerVersion.Version130;
            prefs.ForDirectExecution = false;
            prefs.OldOptions.DdlBodyOnly = false;
            prefs.OldOptions.DdlHeaderOnly = false;
            prefs.OldOptions.EnforceScriptingPreferences = true;
            prefs.OldOptions.PrimaryObject = true;
            prefs.DataType.UserDefinedDataTypesToBaseType = false;
            prefs.DataType.XmlNamespaces = true;
            obj.Touch();
            obj.ScriptCreateOrAlter(script, prefs);
            // the constraints don't like carriage returns/line feeds. SMO seems to use both \r\n and \n 
            var actualScript = script.ToSingleString().Trim().Replace("\r\n", " ").Replace("\n", " ");
            // the header has the current timestamp, so just skip most of it
            Assert.That(actualScript, 
                        Does.EndWith(expectedScriptText),
                        string.Format("Wrong CREATE OR ALTER script for {0}, actual scripts: {1} is not ending with expected scripts: {2}.",
                                      obj.GetType().Name,
                                      actualScript,
                                      expectedScriptText));
        }

        /// <summary>
        /// Verify DropIfExists method for object.
        /// Test steps:
        /// 1. Verify object drop before object is created.
        /// 2. Verify scripting for drop with existence check.
        /// 3. Verify object drop.
        /// 4. Verify object drop after object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// <param name="script">Expected part of script statement.</param>
        /// </summary>
        protected void VerifySmoObjectDropIfExists(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify,
            string script = "IF  EXISTS")
        {
            // 1. Try to drop object before it is created.
            //
            ((IDropIfExists)obj).DropIfExists();

            CreateSmoObject(obj);

            // 2. Verify the script contains expected statement.
            //
            if (obj is _SMO.IScriptable)
            {
                string scriptDropIfExists;

                var so = new _SMO.ScriptingOptions
                {
                    IncludeIfNotExists = true,
                    ScriptDrops = true
                };

                scriptDropIfExists = ScriptSmoObject((_SMO.IScriptable)obj, so);
                Assert.That(scriptDropIfExists, Contains.Substring(script), "Expected drop with existence check is not in script output");

            }

            // 3. Drop object with DropIfExists and check if it is dropped.
            //
            ((IDropIfExists)obj).DropIfExists();
            VerifyIsSmoObjectDropped(obj, objVerify);

            // 4. Try to drop already dropped object.
            //
            ((IDropIfExists)obj).DropIfExists();
        }

        /// <summary>
        /// Verify CreateOrAlter method for object, when it doesn't exist. This method calls CreateOrAlter() by itself, which shouldn't be done by the caller.
        /// Test steps:
        /// 1. Verify scripting is ending with expected script text using ScriptCreateOrAlter() before the object was created.
        /// 2. Create the object using CreateOrAlter().
        /// 3. Verify the object was created correctly in the database.
        /// <param name="database">Smo database.</param>
        /// <param name="obj">Smo object.</param>
        /// <param name="expectedScriptText">Expected part of script statement.</param>
        /// </summary>
        protected void VerifySmoObjectCreateOrAlterForCreate(_SMO.Database database, _SMO.SqlSmoObject obj, string expectedScriptText)
        {
            // 1. Verify ScriptCreateOrAlter() generated expected script text before the object was created
            VerifyScriptCreateOrAlter(obj, expectedScriptText);

            // 2. Create the object.
            //
            ((ICreateOrAlterable)obj).CreateOrAlter();

            // 3. Verify with existence check.
            //
            VerifyObjectExists(database, ((_SMO.ScriptNameObjectBase)obj).FullQualifiedName);
        }

        /// <summary>
        /// Verify CreateOrAlter method for object when existing, this calls CreateOrAlter() by itself - the caller shouldn't do that.
        /// Test steps:
        /// 1. Alter the object using CreateOrAlter().
        /// 2. Verify the object was altered correctly in the database.
        /// 3. Verify scripting for alter is ending with expected script text using ScriptCreateOrAlter().
        /// <param name="database">Smo database.</param>
        /// <param name="obj">Smo object.</param>
        /// <param name="expectedScriptText">Expected part of script statement.</param>
        /// </summary>
        protected void VerifySmoObjectCreateOrAlterForAlter(_SMO.Database database, _SMO.SqlSmoObject obj, string expectedScriptText)
        {
            // 1. Alter the object
            //
            ((ICreateOrAlterable)obj).CreateOrAlter();

            // 2. Verify with existence check.
            //
            VerifyObjectExists(database, ((_SMO.ScriptNameObjectBase)obj).FullQualifiedName);

            // 3. Refresh() the object, then verify ScriptCreateOrAlter() generated expected script text.
            //
            obj.Refresh();
            VerifyScriptCreateOrAlter(obj, expectedScriptText);
        }

        /// <summary>
        /// This method validates that the collection provided has the correct number of elements. If a collection
        /// does not contain the expected number of elements, the elements are enumerated to append their names to
        /// an exception string.
        /// </summary>
        /// <param name="collection">The collection to verify.</param>
        /// <param name="expectedCount">The expected count.</param>
        /// <param name="additionalComments">Additional test related comments.</param>
        protected void VerifySmoCollectionCount(_SMO.SmoCollectionBase collection, int expectedCount, params string[] additionalComments)
        {
            if (collection == null)
            {
                throw new ArgumentException("Null collection");
            }

            if (expectedCount < 0)
            {
                throw new ArgumentException("expected count must be non-negative.");
            }

            if (collection.Count != expectedCount)
            {
                StringBuilder errorMessage = new StringBuilder();

                errorMessage.Append(additionalComments);

                errorMessage.AppendLine("Unexpected number of objects in collection.");

                foreach (_SMO.SqlSmoObject obj in collection)
                {
                    errorMessage.AppendFormat("Object: {0}\r\n", obj.InternalName);
                }

                throw new ArgumentException(errorMessage.ToString());
            }
        }
    }
}
