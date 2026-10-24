// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CollectionGenerator
{
    /// <summary>
    /// This class builds a switch statement that maps {[Parent type],[Urn Suffix]} tuples to 
    /// a func&lt;SqlSmoObject,AbstractCollectionBase&gt; that returns the appropriate collection
    /// instance without using reflection
    /// </summary>
    [Generator]
    public class SqlSmoObjectGenerator : ISourceGenerator, ISyntaxContextReceiver
    {
        public Dictionary<string, Dictionary<string, string>> CollectionsMap = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, Dictionary<string, string>> SingletonsMap = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string,string> TypeAliases = new Dictionary<string, string>();
        private string GetSingletonFactoryMethod(string className, SqlSmoObjectGenerator generator)
        {
            var lastDot = className.LastIndexOf('.');
            var cname = className.Substring(lastDot + 1);
            var ns  = className.Substring(0, lastDot);
            var builder = new StringBuilder(
$@"// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
namespace {ns}
{{
    public partial class {cname}
    {{
        protected override SqlSmoObject GetSingletonInstance(string childTypeName)
        {{
            switch (childTypeName)
            {{
");
            foreach (var typename in generator.SingletonsMap[className].Keys)
            {
                if (generator.TypeAliases.ContainsKey(typename))
                {
                    _ = builder.AppendLine($"                case \"{generator.TypeAliases[typename]}\":");
                }
                _ = builder.AppendLine($"                case \"{typename}\":");
                _ = builder.AppendLine($"                    return {generator.SingletonsMap[className][typename]};");
            }
            _ = builder.Append(
$@"
                default:
                    return base.GetSingletonInstance(childTypeName);
            }}
        }}
    }}
}}
");
            return builder.ToString();
        }

        private string GetCollectionFactoryMethod(string className, SqlSmoObjectGenerator generator)
        {
            var lastDot = className.LastIndexOf('.');
            var cname = className.Substring(lastDot + 1);
            var ns = className.Substring(0, lastDot);
            var builder = new StringBuilder(
$@"// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
namespace {ns}
{{
    public partial class {cname}
    {{
        protected override AbstractCollectionBase GetCollectionInstance(string childTypeName)
        {{
            switch (childTypeName)
            {{
");
            foreach (var typename in generator.CollectionsMap[className].Keys)
            {
                if (generator.TypeAliases.ContainsKey(typename))
                {
                    _ = builder.AppendLine($"                case \"{generator.TypeAliases[typename]}\":");
                }
                _ = builder.AppendLine($"                case \"{typename}\":");
                _ = builder.AppendLine($"                    return {generator.CollectionsMap[className][typename]};");
            }
            _ = builder.Append(
$@"
                default:
                    return base.GetCollectionInstance(childTypeName);
            }}
        }}
    }}
}}
");
            return builder.ToString();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            _ = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.targetframework", out var targetFramework);
            if (!(context.SyntaxContextReceiver is SqlSmoObjectGenerator generator))
            {
                return;
            }
            Trace.TraceInformation("*** Printing collection references");
            foreach (var className in generator.CollectionsMap.Keys)
            {
                Trace.TraceInformation($"Class name: {className}");
                foreach (var collectionType in generator.CollectionsMap[className].Keys)
                {
                    Trace.TraceInformation($"\tChild type: {collectionType}\tProperty name: {generator.CollectionsMap[className][collectionType]}");
                }
                var collectionsMethod = GetCollectionFactoryMethod(className, generator);
                try
                {
                    context.AddSource($"{className}.{targetFramework ?? "default"}.collections.g.cs", collectionsMethod);
                }
                catch { }

            }
            Trace.TraceInformation("+++ Printing singleton references");
            foreach (var className in generator.SingletonsMap.Keys)
            {
                Console.Write($"Class name: {className}\r\n\t");
                foreach (var propertyType in generator.SingletonsMap[className].Keys)
                {
                    Trace.TraceInformation($"Child type: {propertyType}\tProperty name: {generator.SingletonsMap[className][propertyType]}");
                }
                var singletonMethod = GetSingletonFactoryMethod(className, generator);
                try
                {
                    context.AddSource($"{className}.{targetFramework ?? "default"}.singles.g.cs", singletonMethod);
                }
                catch { }

            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SqlSmoObjectGenerator());
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (!(context.Node is AttributeSyntax attributeSyntax))
            {
                return;
            }
            if (attributeSyntax.Name.ToString() == "SfcElementType")
            {
                OnVisitSfcElement(attributeSyntax);
                return;
            }
            if (!attributeSyntax.Name.ToString().Equals("SfcObject"))
            {
                return;
            }
            if (!(attributeSyntax.Parent.Parent is PropertyDeclarationSyntax propertyDeclaration))
            {
                Trace.TraceInformation($"Skipping SfcObject attribute at {context.Node.GetLocation()} because it's not part of a property declaration");
                return;
            }
            if (!(propertyDeclaration.Parent is ClassDeclarationSyntax classDeclaration))
            {
                Trace.TraceInformation($"Skipping SfcObject attribute at {context.Node.GetLocation()} because it's not part of a class");
                return;
            }
            if (!(classDeclaration.Parent is NamespaceDeclarationSyntax namespaceDeclaration))
            {
                Trace.TraceInformation($"Skipping SfcObject attribute at {context.Node.GetLocation()} because it's part of a nest class");
                return;
            }
            
            if (attributeSyntax.ArgumentList == null || attributeSyntax.ArgumentList.Arguments.Count < 2) 
            {
                Trace.TraceInformation($"Skipping SfcObject attribute at {context.Node.GetLocation()} because it is the wrong constructor");
                return;
            }
            
            var className = $"{namespaceDeclaration.Name}.{classDeclaration.Identifier}";
            var arguments = attributeSyntax.ArgumentList.Arguments.ToString();
            var propertyName = propertyDeclaration.Identifier.ToString();
            if (arguments.Contains("SfcContainerRelationship.ChildContainer") || arguments.Contains("SfcContainerRelationship.ObjectContainer"))
            {
                var collectionType =  attributeSyntax.ArgumentList.Arguments[2].ToString().TrimEnd(')', ' ');
                collectionType = collectionType.Substring(collectionType.IndexOf('(') + 1);

                if (!CollectionsMap.ContainsKey(className))
                {
                    CollectionsMap[className] = new Dictionary<string, string>();
                }
                CollectionsMap[className][collectionType] = propertyName;
            } 
            else if (arguments.Contains("SfcObjectRelationship.ChildObject")|| arguments.Contains("SfcObjectRelationship.Object"))
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(propertyDeclaration.Type).Type;
                var isSmoObject = typeInfo.Name.EndsWith("SqlSmoObject");
                while (!isSmoObject && typeInfo.BaseType != null)
                {
                    typeInfo = typeInfo.BaseType;
                    isSmoObject = typeInfo.Name.EndsWith("SqlSmoObject");
                }
                if (!isSmoObject)
                {
                    Trace.TraceInformation($"Skipping SfcObject attribute at {context.Node.GetLocation()} because the property isn't a SqlSmoObject");
                    return;
                }
                if (!SingletonsMap.ContainsKey(className))
                {
                    SingletonsMap[className] = new Dictionary<string, string>();
                }
                SingletonsMap[className][propertyDeclaration.Type.ToString()] = propertyName;
            } 
            else
            {
                Trace.TraceInformation($"Skipping SfcObject attribute at {context.Node.GetLocation()} because it's not a ChildContainer or ChildObject. {arguments}");
            }
        }

        private void OnVisitSfcElement(AttributeSyntax attributeSyntax)
        {
            var classDeclaration = (ClassDeclarationSyntax)attributeSyntax.Parent.Parent;
            var className = classDeclaration.Identifier.ToString();
            var arguments = attributeSyntax.ArgumentList.Arguments.ToString().Trim('"', ' ');
            if (TypeAliases.ContainsKey(className))
            {
                throw new InvalidOperationException("Multiple identical class names use SfcElement");
            }
            TypeAliases[className] = arguments;
        }
    }
}
