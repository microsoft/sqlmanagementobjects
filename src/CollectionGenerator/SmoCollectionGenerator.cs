// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CollectionGenerator
{
    [Generator]
    public class SmoCollectionGenerator : ISourceGenerator, ISyntaxContextReceiver
    {
        private static readonly DiagnosticDescriptor InvalidCollectionTypeArgument = new DiagnosticDescriptor(id: "SMOCOLL001",
                            title: "Invalid collection type argument",
                            messageFormat: "Unable to process collection {0}. {1} or {2} is an invalid type argument.",
                            category: "SmoCollection",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true);
#if DEBUGCOLLECTIONGENERATOR
        static SmoCollectionGenerator()
        {
            Debugger.Launch();
        }
#endif

        public class CollectionInfo
        {
            public string BaseClass { get; set; }
            public string Name { get; }
            public string TParent { get;  }
            public string TObject { get; }
            public string Namespace { get; }
            public string Visibility { get; }
            public CollectionInfo(string baseClass, string name, string tObject, string tParent, string @namespace, string visibility)
            {
                BaseClass = baseClass;
                Name = name;
                TParent = tParent;
                TObject = tObject;
                Namespace = @namespace;
                Visibility = visibility;
            }
            public override string ToString()
            {
                return $"{Namespace}.{Name} : {BaseClass}<{TObject}, {TParent}>";
            }
        }

        private class CollectionFactoryInfo
        {
            public List<CollectionInfo> Collections { get; } = new List<CollectionInfo>();
            public Func<CollectionInfo, string> CollectionClassGenerator { get; }
            public CollectionFactoryInfo(Func<CollectionInfo, string> collectionClassGenerator)
            {
                CollectionClassGenerator = collectionClassGenerator;
            }
        }

        private readonly Dictionary<string, CollectionFactoryInfo> allCollections = new Dictionary<string, CollectionFactoryInfo>()
        {
            // Currently all collections use the same generator template but we'll keep the capability to vary them in the future
            {"SimpleObjectCollectionBase", new CollectionFactoryInfo(CreateSimpleObjectCollection) },
            {"ParameterCollectionBase", new CollectionFactoryInfo(CreateSimpleObjectCollection)},
            {"SchemaCollectionBase",new CollectionFactoryInfo(CreateSimpleObjectCollection) },
            {"RemovableCollectionBase", new CollectionFactoryInfo(CreateSimpleObjectCollection) },
        };
        public List<CollectionInfo> SimpleCollections => allCollections["SimpleObjectCollectionBase"].Collections;
        //public readonly List<CollectionInfo> SchemaCollections = new List<CollectionInfo>();
        public List<CollectionInfo> ParameterCollections => allCollections["ParameterCollectionBase"].Collections;

        private static string CreateSimpleObjectCollection(CollectionInfo collection) => $@"
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
namespace {collection.Namespace}
{{
    ///<summary>
    ///Collection of {collection.TObject} objects associated with an instance of {collection.TParent}
    ///</summary>
    {collection.Visibility} partial class {collection.Name}
    {{
        internal {collection.Name}(SqlSmoObject parentInstance) : base(({collection.TParent})parentInstance)
        {{
        }}

        protected override string UrnSuffix => {collection.TObject}.UrnSuffix;

        internal override {collection.TObject} GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state) => new {collection.TObject}(this, key, state);
    }}
}}
";

        public void Execute(GeneratorExecutionContext context)
        {
            _ = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.targetframework", out var targetFramework);
            if (!(context.SyntaxContextReceiver is SmoCollectionGenerator generator))
            {
                return;
            }
            foreach (var collectionFactory in generator.allCollections)
            {
                foreach (var collectionInfo in collectionFactory.Value.Collections)
                {
                    var source = collectionFactory.Value.CollectionClassGenerator(collectionInfo);
                    if (collectionInfo.TObject.Contains("global") || collectionInfo.TParent.Contains("global"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidCollectionTypeArgument, Location.None, collectionInfo.Name, collectionInfo.TObject, collectionInfo.TParent));
                    }
                    Trace.TraceInformation(source);
                    try
                    {
                        context.AddSource($"{collectionInfo.Name}.{targetFramework ?? "default"}.generated.cs", source);
                    }
                    catch
                    { }
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SmoCollectionGenerator());
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (!(context.Node is ClassDeclarationSyntax cds))
            {
                return;
            }
            if (!cds.Identifier.ValueText.EndsWith("Collection"))
            {
                return;
            }
            Trace.TraceInformation($"Found collection candidate {cds.Identifier.Value}");

            if (!(context.SemanticModel.GetDeclaredSymbol(cds) is INamedTypeSymbol classSymbol))
            {
                return;
            }
            if (!allCollections.ContainsKey(classSymbol.BaseType?.Name))
            {
                return;
            }

            Trace.TraceInformation($"{cds.Identifier.Value} implements {classSymbol.BaseType.Name}");
            if (classSymbol.BaseType.TypeArguments.Length != 2)
            {
                return;
            }
            var tObject = classSymbol.BaseType.TypeArguments[0];
            var tParent = classSymbol.BaseType.TypeArguments[1];
            var collectionInfo = new CollectionInfo(classSymbol.BaseType.Name,
                cds.Identifier.ValueText,
                tObject.NameForDeclaration(),
                tParent.NameForDeclaration(),
                classSymbol.ContainingNamespace.ToDisplayString(),
                classSymbol.DeclaredAccessibility == Accessibility.Internal ? "internal" : "public");
            allCollections[collectionInfo.BaseClass].Collections.Add(collectionInfo);
        }
    }

    internal static class SyntaxExtensions
    {
        public static string NameForDeclaration(this ITypeSymbol typeSymbol)
        {
            var ns = typeSymbol.ContainingNamespace.ToDisplayString();
            return ns == "Microsoft.SqlServer.Management.Smo" ? typeSymbol.Name : $"{ns}.{typeSymbol.Name}";
        }
    }

}