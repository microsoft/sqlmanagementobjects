// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

class ModelGen
{
    static bool _hasNameGetter;
    static bool _hasNameSetter;
    static bool _hasSchema;
    static CodeWriter _modelFile;
    static string _className;
    static SortedList _listProperties;
    static XmlNode _startNode;

    static List<string> _typesToSkip;

    // Can we emit the type refered by this Xml node?
    static bool CanEmitType( XmlNode node )
    {
        bool hasName = true;
        XmlAttribute hasCtorAttribute = node.Attributes["has_constructors"];
        if( hasCtorAttribute != null )
        {
            hasName = Boolean.Parse(hasCtorAttribute.Value) && node.Attributes["parent_type"] != null;
        }

        return hasName;
    }

    static void InitSkippableTypes( XmlNode node )
    {
        _typesToSkip = new List<string>();
        foreach( XmlNode namespaceNode in node.ChildNodes )
        {
            foreach( XmlNode classNode in namespaceNode.ChildNodes )
            {
                if( !CanEmitType(classNode) )
                {
                    _typesToSkip.Add( classNode.Attributes["class_name"].Value );
                }
            }
        }
    }

    static Hashtable _collectionNames;

    static void InitCollectionNames( XmlNode node )
    {
        _collectionNames = new Hashtable();
        foreach( XmlNode namespaceNode in node.ChildNodes )
        {
            foreach( XmlNode classNode in namespaceNode.ChildNodes )
            {
                string className = classNode.Attributes["class_name"].Value;
                if( _typesToSkip.Contains(className ) )
                {
                    continue; // don't even bother with this type
                }

                XmlAttribute collectionNameAttribute = classNode.Attributes["collection_name"];
                if( collectionNameAttribute != null )
                {
                    _collectionNames.Add( className, collectionNameAttribute.Value );
                }
            }
        }
    }

    // Main entry point in the class
    public static void DoIt( string modelFilename, SortedList listProperties, XmlNode startNode )
    {
        try{

            if( File.Exists(modelFilename) )
            {
                File.Delete(modelFilename);
            }

            XmlNode rootNode = startNode.ParentNode.ParentNode;

            if( _typesToSkip == null )
            {
                InitSkippableTypes(rootNode);
            }

            if( _collectionNames == null )
            {
                InitCollectionNames(rootNode);
            }

            _className = startNode.Attributes["class_name"].Value;

            if( _typesToSkip.Contains(_className) )
            {
                return;
            }

            _hasSchema = false;
            XmlAttribute hasSchemaAttribute = startNode.Attributes["has_schema"];
            if( hasSchemaAttribute != null )
            {
                _hasSchema = Boolean.Parse(hasSchemaAttribute.Value);
            }

            _hasNameGetter = true;
            _hasNameSetter = _className != "Server"; // special cases...

            _modelFile = new CodeWriter(modelFilename);
            _listProperties = listProperties;
            _startNode = startNode;

            WriteHeader();
            WriteInterfaceDefinition();
            WriteFactory();
            WriteClassDefinition();
            WriteTail();
        }
        finally
        {
            _modelFile.Dispose();
        }
    }

    // Write header of the file: namespaces etc.
    private static void WriteHeader()
    {
        // Not all of these namespaces are needed for all classes but it's easier to just have them -- as long as they don't clash
        _modelFile.WriteCodeLines(
            "//**** This file has been automaticaly generated. Do not attempt to modify manually! ****",
            "using System;",
            "using System.Collections;",
            "using System.Net;",
            "using Microsoft.SqlServer.Management.Smo;",
            "using Microsoft.SqlServer.Management.Common;",
            "using Microsoft.SqlServer.Management.Smo.Agent;",
            "using Microsoft.SqlServer.Management.Smo.Broker;",
            "using Microsoft.SqlServer.Management.Smo.Wmi;",
            "using Microsoft.SqlServer.Management.Smo.Mail;",
            "using Microsoft.SqlServer.Management.Smo.RegisteredServers;",
            "",
            "namespace  Microsoft.SqlServer.Management.Data.Designer" // This name might change
        );

        _modelFile.IncrementIndent();
    }

    // Write definition of the interface and collection-interface
    // Not every class has a collection -- think singletons
    private static void WriteInterfaceDefinition()
    {
        string interfaceName = GetModelInterfaceName(_className);

        _modelFile.WriteCodeLine("//");
        _modelFile.WriteCodeLine("// Public interfaces {0} and {0}Collection", interfaceName);
        _modelFile.WriteCodeLine("//");

        _modelFile.WriteCodeLine("public interface {0}", interfaceName );
        _modelFile.IncrementIndent();

        // property Name is special
        if( _hasNameGetter )
        {
            if( _hasNameSetter )
            {
                _modelFile.WriteCodeLine("System.String Name {{ get; set; }}");
            }
            else
            {
                _modelFile.WriteCodeLine("System.String Name {{ get; }}");
            }
        }

        if( _hasSchema )
        {
            _modelFile.WriteCodeLine("System.String Schema {{ get; set; }}");
        }

        foreach( ObjectPropertyEx op in _listProperties.Values )
        {
            if( op.isIntrinsic )
            {
                string setter;
                if( op.ReadOnly )
                {
                    setter = "";
                }
                else
                {
                    setter = "set; ";
                }

                _modelFile.WriteCodeLine("{0} {1} {{ get; {2}}}", op.Type, op.Name, setter );
            }
        }

        foreach(XmlNode n in _startNode.SelectNodes("collection"))
        {
            string collectionElementType = n.Attributes["element_type"].Value;

            if( _typesToSkip.Contains(collectionElementType) )
            {
                continue; // this type is not to be emitted
            }

            string collectionName = (string)_collectionNames[collectionElementType];

            _modelFile.WriteCodeLine("I{0}ModelCollection {1} {{ get; }}", collectionElementType, collectionName );
        }

        _modelFile.DecrementIndent(); // interface def

        if( _collectionNames.ContainsKey( _className ) )
        {
            _modelFile.WriteCodeLine("public interface {0}Collection : ICollection", interfaceName);
            _modelFile.IncrementIndent();
            _modelFile.WriteCodeLine("{0} this[int index]{{ get;}}", interfaceName);
            _modelFile.WriteCodeLine("{0} this[string name]{{ get;}}", interfaceName);
            _modelFile.WriteCodeLine("void Add({0} new{1});", interfaceName, _className);
            _modelFile.DecrementIndent();
        }
    }

    // Factory for instantiating implementations of interfaces. There is no factory for collection classes
    private static void WriteFactory()
    {
        _modelFile.WriteCodeLine("// Factory for newing Impl classes");
        _modelFile.WriteCodeLine("public partial class ModelFactory");
        _modelFile.IncrementIndent();
        string interfaceName = GetModelInterfaceName(_className);
        string paramName = "smo" + _className;

        _modelFile.WriteCodeLine("// This method takes an existing SMO object");
        _modelFile.WriteCodeLine("public static {0} Create{1}({1} {2})", interfaceName, _className, paramName );
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("return new {0}Impl({1});",_className, paramName);
        _modelFile.DecrementIndent();

        _modelFile.WriteCodeLine("// This method creates a new nameless SMO object");
        _modelFile.WriteCodeLine("public static {0} Create{1}()", interfaceName, _className );
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("return new {0}Impl( new {0}() );",_className);
        _modelFile.DecrementIndent();

        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine();
    }

    // Write class definition: all it has is properties for SMO intrinsic properties and collections, plus a ctor
    private static void WriteClassDefinition()
    {
        _modelFile.WriteCodeLine();
        _modelFile.WriteCodeLine("//");
        _modelFile.WriteCodeLine("// Private Implementation of {0} and {0}Collection", GetModelInterfaceName(_className));
        _modelFile.WriteCodeLine("//");

        string implRefName = "m_" + _className.ToLower();
        _modelFile.WriteCodeLine("class {0}Impl : I{0}Model", _className );
        _modelFile.IncrementIndent(); // start class def

        _modelFile.WriteCodeLine("{0} {1};", _className, implRefName );

        _modelFile.WriteCodeLine("internal {0} GetSmoObject()",_className);
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("return {0};",implRefName);
        _modelFile.DecrementIndent();

        string paramName = "smo" + _className;
        _modelFile.WriteCodeLine("// The constructor takes an existing SMO object");
        _modelFile.WriteCodeLine("public {0}Impl( {0} {1} )", _className, paramName );
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("{0} = {1};", implRefName, paramName);
        _modelFile.DecrementIndent();

        // Yeah, name is special...
        if( _hasNameGetter )
        {
            string nameSetter = "";
            if( _hasNameSetter )
            {
                nameSetter = String.Format(" set {{ {0}.Name = value; }} ", implRefName );
            }
            _modelFile.WriteCodeLine("public System.String Name {{ get {{ return {0}.Name; }}{1}}}", implRefName, nameSetter );
        }

        if( _hasSchema )
        {
            _modelFile.WriteCodeLine("public System.String Schema {{ get {{ return {0}.Schema; }} set {{ {0}.Schema = value; }} }}", implRefName );
        }

        foreach(ObjectPropertyEx op in _listProperties.Values)
        {
            if( op.isIntrinsic )
            {
                string setter;
                if( op.ReadOnly )
                {
                    setter = "";
                }
                else
                {
                    setter = string.Format("set{{ {0}.Properties[{1}].Value=value; }} ", implRefName, op.index);
                }
                _modelFile.WriteCodeLine("public {0} {1} {{ get{{ return ({0}){2}.Properties[{4}].Value; }} {3}}}",op.Type, op.Name, implRefName, setter, op.index);
            }
        }

        WriteCollectionProperties();

        _modelFile.DecrementIndent(); // end of class def

        if( _collectionNames.ContainsKey( _className ) )
        {
            WriteCollectionDefinition();
        }
    }

    // Write collection getters -- called from WriteClassDefinition
    private static void WriteCollectionProperties()
    {
        foreach(XmlNode n in _startNode .SelectNodes("collection"))
        {
            string collectionElementType = (string)n.Attributes["element_type"].Value;

            if( _typesToSkip.Contains(collectionElementType) )
            {
                continue; // this type is not to be emitted
            }

            string collectionName = (string)_collectionNames[collectionElementType];

            string implRefName = "m_" + _className.ToLower();

            _modelFile.WriteCodeLine("{0}Collection {1}.{2}", GetModelInterfaceName(collectionElementType), GetModelInterfaceName(_className),collectionName );
            _modelFile.IncrementIndent();
            _modelFile.WriteCodeLine("get");
            _modelFile.IncrementIndent();
            _modelFile.WriteCodeLine("return new {0}ModelCollection({1}.{2});", collectionElementType, implRefName, collectionName );
            _modelFile.DecrementIndent();
            _modelFile.DecrementIndent();
        }
    }

    // Write definitions of collections with all the methods required to implement ICollection
    private static void WriteCollectionDefinition()
    {
        _modelFile.WriteCodeLine("class {0}ModelCollection : I{0}ModelCollection", _className );
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("{0}Collection _parent;", _className);
        _modelFile.WriteCodeLine("internal {0}ModelCollection({0}Collection parent)", _className);
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("_parent = parent;");
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public I{0}Model this[int index]",_className);
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("get{{ return new {0}Impl(_parent[index]); }}",_className);
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public I{0}Model this[string name]",_className);
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("get{{ return new {0}Impl(_parent[name]); }}",_className);
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public void CopyTo(Array a, int index)");
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("for( int i=0; i<Count; i++ )");
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("a.SetValue( _parent[i],i+index );");
        _modelFile.DecrementIndent();
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public int Count");
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("get{{return _parent.Count;}}");
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public object SyncRoot");
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("get{{return this;}}");
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public bool IsSynchronized");
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("get{{return false;}}");
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public IEnumerator GetEnumerator()");
        _modelFile.IncrementIndent();
        string enumeratorName = _className + "ModelCollectionEnumerator";
        _modelFile.WriteCodeLine("return new {0}(_parent);",enumeratorName);
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public void Add(I{0}Model new{0})", _className );
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("_parent.Add((({0}Impl)new{0}).GetSmoObject());", _className );
        _modelFile.DecrementIndent();

        WriteEnumeratorDefinition(enumeratorName);

        _modelFile.DecrementIndent();
    }

    // Enumerator is part of the collection definition
    private static void WriteEnumeratorDefinition(string enumeratorName)
    {
        _modelFile.WriteCodeLine("class {0} : IEnumerator", enumeratorName);
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("{0}Collection _collection;",_className);
        _modelFile.WriteCodeLine("int m_currentPos;");
        _modelFile.WriteCodeLine("public {0}({1}Collection collection)",enumeratorName,_className);
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("_collection = collection;");
        _modelFile.WriteCodeLine("m_currentPos = -1;");
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public object Current");
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("get");
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("return new {0}Impl(_collection[m_currentPos]);",_className);
        _modelFile.DecrementIndent();
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public bool MoveNext()");
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("return ++m_currentPos < _collection.Count;");
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("public void Reset()");
        _modelFile.IncrementIndent();
        _modelFile.WriteCodeLine("m_currentPos = -1;");
        _modelFile.DecrementIndent();
        _modelFile.DecrementIndent();
    }

    // End of the file
    private static void WriteTail()
    {
        _modelFile.DecrementIndent();
        _modelFile.WriteCodeLine("// EOF");
    }

    static string GetModelInterfaceName( string className )
    {
        return string.Format("I{0}Model",className);
    }
}

