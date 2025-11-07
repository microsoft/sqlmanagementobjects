// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using CollectionGenerator;
using System.Linq;
using System.Diagnostics;

namespace CollectionGeneratorTest
{
    [TestClass]
    public class SimpleObjectCollectionGeneratorTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void SimpleObjectCollectionGeneratorTest() 
        {
            var compilation = CreateCompilation(@"
namespace Microsoft.SqlServer.Management.Smo {
    public abstract class AbstractCollectionBase
    {
    }
    public enum SqlSmoState
    {
    }
    public class ObjectKeyBase
    {
    }

    public class SqlSmoObject
    {
        internal SqlSmoObject(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
        {
        }
    }

    public class Child : SqlSmoObject
    {
        internal Child(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) : base(parentColl, key, state)
        {
        }

        public static string UrnSuffix => ""Child"";
    }
    
    // These base class definitions are gross simplifications of the real base collection classes.

    public abstract class SimpleObjectCollectionBase<TObject, TParent> : AbstractCollectionBase
    where TObject: SqlSmoObject
    where TParent: SqlSmoObject
    {
        internal SimpleObjectCollectionBase(TParent parent)
        {
        }
 
        protected abstract string UrnSuffix { get; }

        internal abstract TObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state);
    }

    public abstract class ParameterCollectionBase<TObject, TParent> : AbstractCollectionBase
    where TObject: SqlSmoObject
    where TParent: SqlSmoObject
    {
        internal ParameterCollectionBase(TParent parent)
        {
        }
 
        protected abstract string UrnSuffix { get; }

        internal abstract TObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state);
    }

    public sealed partial class MySmoObjectCollection : SimpleObjectCollectionBase<Child,OtherNamespace.Parent>
    {
        
    }

    public sealed partial class MyParameterObjectCollection : ParameterCollectionBase<Child, OtherNamespace.Parent>
    {
    }
}
namespace OtherNamespace
{
    using Microsoft.SqlServer.Management.Smo;
    public class Parent : Microsoft.SqlServer.Management.Smo.SqlSmoObject
    {
        internal Parent(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) : base(parentColl, key, state)
        {
        }
    }
}
");
            var generator = new SmoCollectionGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            Assert.That(diagnostics, Is.Empty, nameof(diagnostics));
            Assert.That(outputCompilation.SyntaxTrees.Count, Is.EqualTo(3), "Output compilation should have a syntax tree per supplemented class, plus the original syntax");
            Assert.That(outputCompilation.GetDiagnostics(), Is.Empty, "Output compilation should have no errors");
            Trace.TraceInformation("Checking compilation of SimpleObjectCollectionBase");
            var outputType = outputCompilation.GetTypeByMetadataName("Microsoft.SqlServer.Management.Smo.MySmoObjectCollection");
            Assert.That(outputType, Is.Not.Null, "Compilation doesn't include the MySmoObjectCollection type");
            Assert.That(outputType.IsSealed, Is.True, nameof(outputType.IsSealed));
            Assert.That(outputType.GetMembers().Select(m => m.Name), Has.Member("UrnSuffix"), "UrnSuffix property not added");
            Trace.TraceInformation("Checking compilation of ParameterCollectionBase");
            outputType = outputCompilation.GetTypeByMetadataName("Microsoft.SqlServer.Management.Smo.MyParameterObjectCollection");
            Assert.That(outputType, Is.Not.Null, "Compilation doesn't include the MyParameterObjectCollection type");
            Assert.That(outputType.IsSealed, Is.True, nameof(outputType.IsSealed));
            Assert.That(outputType.GetMembers().Select(m => m.Name), Has.Member("UrnSuffix"), "UrnSuffix property not added");
        }

        private static Compilation CreateCompilation(string source)
           => CSharpCompilation.Create("compilation",
               new[] { CSharpSyntaxTree.ParseText(source) },
               new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
               new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    }
}
