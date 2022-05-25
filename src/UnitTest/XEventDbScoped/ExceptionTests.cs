// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class ExceptionTests : UnitTestBase
    {
        // Core SMO Assemblies that may have public Exception classes
        static IEnumerable<System.Reflection.Assembly> assemblies = new[] {
            typeof(SmoException).Assembly, 
            typeof(DmfException).Assembly, 
            typeof(InternalEnumeratorException).Assembly, 
            typeof(Management.RegisteredServers.RegisteredServerException).Assembly,
            typeof(Management.XEvent.XEventException).Assembly};

        [TestMethod]
        [TestCategory("Unit")]
        public void SmoExceptions_have_empty_or_valid_HelpLink() {
            var message = "Message";
            var innerException = new Exception("Inner");
            Assert.Multiple(() =>
            {
                foreach (var assembly in assemblies)
                {
                    var exceptionTypes = assembly.GetTypes().Where(t => typeof(SqlServerManagementException).IsAssignableFrom(t) && !t.ContainsGenericParameters);
                    foreach (var exceptionType in exceptionTypes)
                    {
                        var args = new List<object>();
                        var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                        Trace.TraceInformation($"Found exception type {exceptionType.AssemblyQualifiedName}");
                        var defaultConstructor = exceptionType.GetConstructor(bindingFlags, null, new Type[] { }, null);
                        Assert.That(defaultConstructor, Is.Not.Null, $"{exceptionType.AssemblyQualifiedName} has no default constructor");
                        if (defaultConstructor != null)
                        {
                            var exception = (SqlServerManagementException)defaultConstructor.Invoke(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, args.ToArray(), null);
                            ValidateSmoException(exception, null);
                        }
                        var messageConstructor = exceptionType.GetConstructor(bindingFlags, null, new Type[] {typeof(string) }, null);
                        args.Add(message);
                        Assert.That(messageConstructor, Is.Not.Null, $"{exceptionType.AssemblyQualifiedName} has no constructor that accepts a message");
                        if (messageConstructor != null)
                        {
                            var exception = (SqlServerManagementException)messageConstructor.Invoke(args.ToArray());
                            ValidateSmoException(exception, null);
                        }
                        var innerConstructor = exceptionType.GetConstructor(bindingFlags, null, new System.Type[] { typeof(string), typeof(System.Exception) }, null);
                        args.Add(innerException);
                        Assert.That(innerConstructor, Is.Not.Null, $"{exceptionType.AssemblyQualifiedName} has no constructor that accepts innerException");
                        if (innerConstructor != null)
                        {
                            var exception = (SqlServerManagementException)innerConstructor.Invoke(args.ToArray());
                            ValidateSmoException(exception, innerException);
                        }
                    }
                }
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SmoExceptions_derive_from_SqlServerManagementException()
        {
            Assert.Multiple(() =>
            {
                foreach (var assembly in assemblies)
                {
                    var exceptionTypes = assembly.GetTypes().Where(t => t.IsPublic && typeof(Exception).IsAssignableFrom(t) && !typeof(SqlServerManagementException).IsAssignableFrom(t));
                    Assert.That(exceptionTypes.Select(t => t.AssemblyQualifiedName), Is.Empty, "All public SMO exceptions should inherit from SqlServerManagementException");
                }
            });
        }

        private void ValidateSmoException(SqlServerManagementException exception, Exception innerException)
        {
            if (innerException != null)
            {
                Assert.That(exception.InnerException?.Message, Is.EqualTo(innerException.Message), $"{exception.GetType().AssemblyQualifiedName}.InnerException");
            }
            if (!string.IsNullOrEmpty(exception.HelpLink))
            {
                if (!Uri.TryCreate(exception.HelpLink, UriKind.Absolute, out var helpLink))
                {
                    Assert.Fail($"{exception.GetType().AssemblyQualifiedName}.HelpLink is not a valid URL: {exception.HelpLink}");
                }
                else
                {
                    Assert.That(helpLink.Host, Is.EqualTo("go.microsoft.com"), $"{exception.GetType().AssemblyQualifiedName} HelpLink host");
                    Assert.That(helpLink.Scheme, Is.EqualTo("https"), $"{exception.GetType().AssemblyQualifiedName} HelpLink scheme");
                    Assert.That(helpLink.AbsolutePath, Is.EqualTo("/fwlink"), $"{exception.GetType().AssemblyQualifiedName} HelpLink path");
                    Assert.That(helpLink.Query, Contains.Substring("LinkId=20476"), $"{exception.GetType().AssemblyQualifiedName} HelpLink LinkId");
                    Assert.That(helpLink.Query, Contains.Substring("ProdName=Microsoft+SQL+Server"), $"{exception.GetType().AssemblyQualifiedName} HelpLink ProdName");
                }
            }
            else
            {
                Trace.TraceWarning($"{exception.GetType().AssemblyQualifiedName}.HelpLink is empty");
            }
        }
    }
}
