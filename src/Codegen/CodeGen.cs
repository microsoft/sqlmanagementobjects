// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 414, 162

class CodeWriter : StreamWriter
{
    internal const int DefaultFileStreamBufferSize = 4096;

    int depth;
    public CodeWriter(string filename)
        : base(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read, DefaultFileStreamBufferSize, FileOptions.SequentialScan))
    {
        depth = 0;
    }

    private bool mirrorToConsole = false; // useful to know what's going on when debugging

    private void WriteImpl(string s, params object[] obj)
    {
        Write(s, obj);
        if (mirrorToConsole)
        {
            Console.Write(s, obj);
        }
    }

    private void WriteImpl(string s)
    {
        Write(s);
        if (mirrorToConsole)
        {
            Console.Write(s);
        }
    }

    public void WriteCodeLine(string s, params object[] obj)
    {
        for (int i = 0; i < depth; i++)
        {
            WriteImpl("\t");
        }
        WriteImpl(s, obj);
        WriteImpl("\r\n");
    }


    public void WriteCodeLine(string s)
    {
        for (int i = 0; i < depth; i++)
        {
            WriteImpl("\t");
        }
        WriteImpl(s);
        WriteImpl("\r\n");
    }


    public void WriteCodeLines(params string[] lines)
    {
        foreach (string line in lines)
        {
            WriteCodeLine(line);
        }
    }

    public void WriteCodeLine()
    {
        WriteImpl("");
    }

    public void IncrementIndent()
    {
        WriteCodeLine("{");
        depth++;
    }

    void DecrementIntendWorker(string s)
    {
        depth--;
        WriteCodeLine("}" + s);
    }

    public void DecrementIndent()
    {
        DecrementIntendWorker("");
    }

    public void DecrementIntendWithSemi()
    {
        DecrementIntendWorker(";");
    }
}

class ObjectPropertyEx : ObjectProperty
{
    public ObjectPropertyEx()
        : base()
    {
    }

    public ObjectPropertyEx(ObjectProperty op)
        : base()
    {
        this.Expensive = op.Expensive;
        this.ExtendedType = op.ExtendedType;
        this.Name = op.Name;
        this.ReadOnly = op.ReadOnly;
        this.Type = op.Type;
        this.Usage = op.Usage;
        this.ReadOnlyAfterCreation = op.ReadOnlyAfterCreation;
        this.KeyIndex = op.KeyIndex;
        this.DefaultValue = op.DefaultValue;
        this.PropertyMode = op.PropertyMode;
        // Reference
        this.ReferenceTemplate = op.ReferenceTemplate;
        this.ReferenceType = op.ReferenceType;
        this.ReferenceKeys = op.ReferenceKeys;
        this.ReferenceTemplateParameters = op.ReferenceTemplateParameters;

        this.isIntrinsic = true;
        this.generate = true;
        this.suppressSfcAttribute = false;
    }

    // Yep, public data members
    public int index;
    public bool isIntrinsic;
    public bool generate;
    public bool dmfIgnore;
    internal bool suppressSfcAttribute;
}

static class CodeGenSettings
{
    static string m_Directory;
    public static string Directory
    {
        get { return m_Directory; }
        set { m_Directory = value; }
    }
}

public class CodeGen
{
    /// <summary>
    /// Flags for keeping track of what on-prem supported versions a property
    /// is applicable for
    /// </summary>
    [Flags]
    private enum SingletonSupportedVersionFlags
    {
        // IF YOU UPDATE THIS LIST, ALSO UPDATE GeneratePropNameToIDLookup and  GenerateMetadataTable functions
        // VBUMP
        NOT_SET = 0,
        v7_0 = 1,
        v8_0 = 2,
        v9_0 = 4,
        v10_0 = 8,
        v10_50 = 16,
        v11_0 = 32,
        v12_0 = 64,
        v13_0 = 128,
        v14_0 = 256,
        v15_0 = 512,
        v16_0 = 1024,
        v17_0 = 2048,
    }

    private static KeyValuePair<ServerVersion, int>[] m_SingletonSupportedVersion =
    {
        new KeyValuePair<ServerVersion, int>(new ServerVersion(7,0), (int)SingletonSupportedVersionFlags.v7_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(8,0), (int)SingletonSupportedVersionFlags.v8_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(9,0), (int)SingletonSupportedVersionFlags.v9_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(10,0), (int)SingletonSupportedVersionFlags.v10_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(10,50), (int)SingletonSupportedVersionFlags.v10_50),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(11,0), (int)SingletonSupportedVersionFlags.v11_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(12,0), (int)SingletonSupportedVersionFlags.v12_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(13,0), (int)SingletonSupportedVersionFlags.v13_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(14,0), (int)SingletonSupportedVersionFlags.v14_0),
        // The build number should probably be 65535 for all the above
        // However, that does not matter for two reasons:
        // - if there is another line after this one, we are safe: any M.m.b with b>0 observed in 
        //   in object xml files (the ones with the object definitions) will be considered
        //   "supported" by matching the next entry.
        // - we rarely seem to rely on min_build/max_build attributes.
        new KeyValuePair<ServerVersion, int>(new ServerVersion(15,0,ushort.MaxValue), (int)SingletonSupportedVersionFlags.v15_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(16,0,ushort.MaxValue), (int)SingletonSupportedVersionFlags.v16_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(17,0,ushort.MaxValue), (int)SingletonSupportedVersionFlags.v17_0),
        //VBUMP
    };

    /// <summary>
    /// Flags for keeping track of what cloud supported versions a
    /// property is applicable for
    /// </summary>
    [Flags]
    private enum CloudSupportedVersionFlags
    {
        NOT_SET = 0,
        v10_0 = 1,
        v11_0 = 2,
        v12_0 = 4,
    }

    private static KeyValuePair<ServerVersion, int>[] m_CloudSupportedVersion =
    {
        new KeyValuePair<ServerVersion, int>(new ServerVersion(10,0), (int)CloudSupportedVersionFlags.v10_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(11,0), (int)CloudSupportedVersionFlags.v11_0),
        new KeyValuePair<ServerVersion, int>(new ServerVersion(12,0), (int)CloudSupportedVersionFlags.v12_0)
    };

    //Note - Datawarehouse is a special case since it doesn't currently actually have
    //multiple versions. I'm adding this though since the logic assumes it does and so
    //we'll just "fake" it since it's likely that at some point in the future we'll need
    //the support for it (especially once the on-prem version is live)

    /// <summary>
    /// Flags for keeping track of what cloud supported versions a property
    /// is applicable for
    /// </summary>
    [Flags]
    private enum DatawarehouseSupportedVersionFlags
    {
        NOT_SET = 0,
        v10_0 = 1,
    }

    private static KeyValuePair<ServerVersion, int>[] m_DatawarehouseSupportedVersion =
    {
        new KeyValuePair<ServerVersion, int>(new ServerVersion(10, 0), (int)DatawarehouseSupportedVersionFlags.v10_0)
    };

    static int m_nRetValue;
    static string m_sDefCtorParent = "AbstractCollectionBase";
    static XmlDocument m_gen;
    static List<String> m_validClassObjects = new List<string>(100);
    //Singleton Properties
    private static SortedList<string, int> listSingletonPropertiesVersion = new SortedList<string, int>();

    private static SortedList<string, ObjectPropertyEx> listSingletonProperties = new SortedList<string, ObjectPropertyEx>();

    //CLoud Properties
    private static SortedList<string, int> listCloudPropertiesVersion = new SortedList<string, int>();
    private static SortedList<string, ObjectPropertyEx> listCloudProperties = new SortedList<string, ObjectPropertyEx>();

    // SQL DW Properties
    static SortedList<string, int> listSqlDwPropertiesVersion = new SortedList<string, int>();
    static SortedList<string, ObjectPropertyEx> listSqlDwProperties = new SortedList<string, ObjectPropertyEx>();

    // Attribute names
    const string attrSupportsXSchema = "support_xschema";

    static void GenerateNewConfigFile(XmlDocument dc, string output_dir)
    {
        if (!String.IsNullOrEmpty(output_dir) && !Directory.Exists(output_dir))
        {
            Directory.CreateDirectory(output_dir);
        }

        string sFilename = Path.Combine(output_dir, "cfg_gen.xml");

        AssemblyName assemblyName = new AssemblyName("Microsoft.SqlServer.Smo");
        Assembly assembly = Assembly.Load(assemblyName);
        Type[] allSmoTypes = assembly.GetTypes();

        foreach (XmlNode namespaceNode in dc.DocumentElement.ChildNodes)
        {
            string node_namespace = namespaceNode.Attributes["name"].Value;

            foreach (XmlNode classNode in namespaceNode.ChildNodes)
            {
                string className = (string)classNode.Attributes["class_name"].Value;

                listSingletonProperties = GeneratePropertiesList(classNode, listSingletonPropertiesVersion, true, DatabaseEngineType.Standalone, DatabaseEngineEdition.Unknown);
                listCloudProperties = GeneratePropertiesList(classNode, listCloudPropertiesVersion, true, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.Unknown);
                listSqlDwProperties = GeneratePropertiesList(classNode, listSqlDwPropertiesVersion, true, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse);

                StringCollection no_gen_nodes = new StringCollection();
                XmlNodeList listNoGen = classNode.SelectNodes("no_gen/property");
                foreach (XmlNode n in listNoGen)
                {
                    no_gen_nodes.Add((string)n.Attributes["name"].Value);
                }

                if (listNoGen.Count > 0)
                {
                    classNode.RemoveChild(classNode.SelectSingleNode("no_gen"));
                }

                string theTypeName = node_namespace + "." + className;
                Type thisType = null;

                foreach (Type t in allSmoTypes)
                {
                    if (t.FullName == theTypeName)
                    {
                        thisType = t;
                        break;
                    }
                }

                string[] intrinsic_props;
                MethodInfo mi = thisType.GetMethod("GetScriptFields", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);

                if (mi != null)
                {
                    intrinsic_props = mi.Invoke(null, new object[] {typeof(void), new ServerVersion(9,0),DatabaseEngineType.Standalone, DatabaseEngineEdition.SqlDataWarehouse, false }) as string[];
                }
                else
                {
                    intrinsic_props = new string[0];
                }

                foreach (ObjectPropertyEx op in listSingletonProperties.Values)
                {
                    XmlNode newChild = dc.CreateElement("property");

                    XmlAttribute attrName = dc.CreateAttribute("name");
                    attrName.Value = op.Name;
                    newChild.Attributes.Append(attrName);

                    XmlAttribute attrGen = dc.CreateAttribute("generate");
                    attrGen.Value = no_gen_nodes.Contains(op.Name) ? "false" : "true";
                    newChild.Attributes.Append(attrGen);

                    bool bIsIntrinsic = false;
                    foreach (string intrinsic_prop in intrinsic_props)
                    {
                        if (op.Name == intrinsic_prop)
                        {
                            bIsIntrinsic = true;
                            break;
                        }
                    }

                    XmlAttribute attrIsIntrinsic = dc.CreateAttribute("is_intrinsic");
                    attrIsIntrinsic.Value = bIsIntrinsic ? "true" : "false";
                    newChild.Attributes.Append(attrIsIntrinsic);

                    if (op.DefaultValue != null)
                    {
                        XmlAttribute attrDefaultValue = dc.CreateAttribute("default");
                        attrDefaultValue.Value = op.DefaultValue;
                        newChild.Attributes.Append(attrDefaultValue);
                    }

                    // Add reference attribute
                    // All reference attributes have to be set for this to work
                    if (op.ReferenceTemplate != null &&
                        op.ReferenceType != null &&
                        op.ReferenceTemplateParameters != null)
                    {
                        XmlAttribute attr = dc.CreateAttribute("reference");
                        attr.Value = op.ReferenceTemplate;
                        newChild.Attributes.Append(attr);

                        attr = dc.CreateAttribute("reference_type");
                        attr.Value = op.ReferenceType;
                        newChild.Attributes.Append(attr);

                        attr = dc.CreateAttribute("reference_template_parameters");
                        attr.Value = op.ReferenceTemplateParameters;
                        newChild.Attributes.Append(attr);

                        if (op.ReferenceKeys != null)
                        {
                            attr = dc.CreateAttribute("reference_keys");
                            attr.Value = op.ReferenceKeys;
                            newChild.Attributes.Append(attr);
                        }
                    }

                    classNode.AppendChild(newChild);
                }
                foreach( string prop_name in no_gen_nodes )
                {

                }
            }
        }

        using (FileStream fileStream = new FileStream(sFilename, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            dc.Save(fileStream);
        }
    }

    private static string thisClassOnly = null; // Make it name of the class to generate that class only, and not any other classes. Useful for debugging

    public static int Main(string[] args)
    {
        m_nRetValue = 0;
        if (args.Length != 3 && args.Length != 4)
        {
            EmitError("usage:\r\nsmocodegen path_to_enumerator configuration_file_name.xml configuration_gen_name.xml [output directory]");
            return 100;
        }

        for (int i = 0; i < args.Length; ++i)
        {
            args[i] = System.Environment.ExpandEnvironmentVariables(args[i]);
        }

        CodeGenSettings.Directory = args[0];

        if (!Directory.Exists(CodeGenSettings.Directory))
        {
            EmitError("path_to_enumerator must exit");
            return 101;
        }

        XmlDocument dc = new XmlDocument();
        dc.Load(args[1]);

        m_gen = new XmlDocument();
        m_gen.Load(args[2]);

        string output_dir = "";
        if (args.Length == 4)
        {
            output_dir = args[3] + "\\";
        }

        bool bGenerateNewFile = false;
        if (bGenerateNewFile)
        {
            GenerateNewConfigFile(dc, output_dir);
        }
        else
        {
            // do real code-gen

            foreach(XmlNode node in dc.DocumentElement.ChildNodes)
            {
                string node_namespace = node.Attributes["name"].Value;

                // Build the list of all class objects in the namespace to validate against
                // when we encounter a possible_parents attribute.
                BuildValidNamespaceClassObjectsList(node);

                foreach (XmlNode node1 in node.ChildNodes)
                {
                    //Don't need to process comment nodes, GenerateClass
                    //will throw if passed one
                    if(node1 is XmlComment)
                    {
                        continue;
                    }
                    GenerateClass(node1, node_namespace, output_dir);
                }
            }
        }

        return m_nRetValue;
    }

    static string GetAttribute(string name, XmlNode node, string default_value)
    {
        if (node.Attributes[name] != null)
        {
            return node.Attributes[name].Value;
        }
        return default_value;
    }

    static void BuildValidNamespaceClassObjectsList(XmlNode namespaceNode)
    {
        // Empty any prior namespace's objects since we currently don't want to look across namespaces.
        // (e.g. a possible parent type cannot be located in another namespace than the child type).
        m_validClassObjects.Clear();
        foreach (XmlNode objClassNode in namespaceNode.SelectNodes("object"))
        {
            string objClassName = GetAttribute("class_name", objClassNode, null);
            m_validClassObjects.Add(objClassName);
        }
    }

    static bool CheckValidNamespaceClassObjectsList(string name)
    {
        // must be exact match, no case-insensitive comparison
        return m_validClassObjects.Contains(name);
    }

    
    static SortedList<string, ObjectPropertyEx> GeneratePropertiesList(XmlNode node, SortedList<string, int> listPropertiesVersion, bool removeStdProps, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
    {
        var listProperties = new SortedList<string, ObjectPropertyEx>();
        bool bSuccess = true;
        string sUrn = GetAttribute("urn", node, null);
        bool removeName = Boolean.Parse(GetAttribute("remove_name", node, "true"));
        KeyValuePair<ServerVersion,int>[] supportedVersions = GetSupportedVersions(databaseEngineType, databaseEngineEdition);
        try
        {
            foreach (KeyValuePair<ServerVersion, int> version in supportedVersions)
            {
                try
                {
                    bSuccess = bSuccess && AddPropertiesForVersion(node, sUrn, version, listProperties, listPropertiesVersion, databaseEngineType, databaseEngineEdition);
                }
                catch (InvalidVersionEnumeratorException)
                {
                }
            }
        }
        catch (Exception x)
        {
            m_nRetValue = 1;
            EmitError(String.Format(CultureInfo.CurrentCulture, "\t{0}", x.Message));
            EmitError(String.Format(CultureInfo.CurrentCulture, "\t{0}", x.StackTrace));
            bSuccess = false;
        }

        if (removeStdProps)
        {
            if (removeName)
            {
                listProperties.Remove("Name");
            }
            listProperties.Remove("Urn");
            listProperties.Remove("Schema");
        }

        // TODO: look for properties that exist in the cfg.xml
        //    but are missing from the enumerator taking possible storage types into consideration
        //    tracking in VSTS: 335223

        return bSuccess ? listProperties : null;
    }

    static XmlNode FindChildWithName(XmlNode node, string name)
    {
        foreach (XmlNode n in node.SelectNodes("property"))
        {
            string propName = (string)n.Attributes["name"].Value;
            if (propName == name)
            {
                return n;
            }
        }
        return null;
    }


    static bool AddPropertiesForVersion(XmlNode node, String urn, KeyValuePair<ServerVersion, int> version, SortedList<string, ObjectPropertyEx> listProperties, SortedList<string, int> listPropertiesVersion, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
    {
        bool bRetValue = true;
        RequestObjectInfo reqinfo = new RequestObjectInfo();
        reqinfo.Urn = urn;
        reqinfo.InfoType = RequestObjectInfo.Flags.Properties;
        ServerInformation serverInformation = new ServerInformation(version.Key, new Version(version.Key.ToString()), databaseEngineType, databaseEngineEdition);
        Enumerator en = new Enumerator();
        ObjectInfo oi;
        oi = en.Process(serverInformation, reqinfo);

        if (oi.Properties.Length == 0)
        {
            // If enumerator doesn't give us properties, we'll get them from config.xml
            // This is the case for non-SQL Enumerator
            foreach (XmlNode n in node.SelectNodes("property"))
            {
                string propName = (string)n.Attributes["name"].Value;
                if (!listProperties.ContainsKey(propName))
                {
                    ObjectPropertyEx opex = new ObjectPropertyEx();
                    opex.index = -1; // not yet determined

                    opex.Name = propName;
                    opex.Type = GetAttribute("type", n, null);
                    opex.Expensive = Boolean.Parse(GetAttribute("expensive", n, "false"));
                    opex.ReadOnly = Boolean.Parse(GetAttribute("readonly", n, "true"));
                    opex.isIntrinsic = Boolean.Parse(GetAttribute("is_intrinsic", n, "true"));
                    opex.generate = Boolean.Parse(GetAttribute("generate", n, "true"));
                    opex.ReadOnlyAfterCreation = Boolean.Parse(GetAttribute("read_only_after_creation", n, "false"));
                    opex.KeyIndex = short.Parse(GetAttribute("key_index", n, "-1"), CultureInfo.InvariantCulture);
                    opex.dmfIgnore = Boolean.Parse(GetAttribute("dmf_ignore", n, "false"));
                    opex.DefaultValue = GetAttribute("default", n, null);
                    opex.suppressSfcAttribute = Boolean.Parse(GetAttribute("suppress_sfc_attribute", n, "false"));

                    string s = GetAttribute("mode", n, null);
                    opex.PropertyMode = PropertyMode.None;
                    if (!string.IsNullOrEmpty(s))
                    {
                        opex.PropertyMode = PropertyMode.None;
                        s = s.ToUpperInvariant();
                        if (-1 != s.LastIndexOf("DESIGN", StringComparison.Ordinal))
                        {
                            opex.PropertyMode = PropertyMode.Design;
                        }
                        if (-1 != s.LastIndexOf("DEPLOY", StringComparison.Ordinal))
                        {
                            opex.PropertyMode = opex.PropertyMode | PropertyMode.Deploy;
                        }
                        if (-1 != s.LastIndexOf("ALL", StringComparison.Ordinal))
                        {
                            opex.PropertyMode = PropertyMode.All;
                        }
                    }

                    // Reference attributes
                    opex.ReferenceTemplate = GetAttribute("reference_template", n, null);
                    opex.ReferenceType = GetAttribute("reference_type", n, null);
                    opex.ReferenceKeys = GetAttribute("reference_keys", n, null);
                    opex.ReferenceTemplateParameters = GetAttribute("reference_template_parameters", n, null);

                    if (propName == "Urn" || propName == "Name" || propName == "Schema")
                    {
                        opex.generate = false;
                    }
                    listProperties.Add(propName, opex);

                    listPropertiesVersion[propName] = 0xFFFF;
                }
            }
            return bRetValue;
        }
        foreach (ObjectProperty op in oi.Properties)
        {
            if (0 != (op.Usage & ObjectPropertyUsages.Request))
            {
                if (listPropertiesVersion.ContainsKey(op.Name) == false)
                {
                    listPropertiesVersion[op.Name] = 0;
                }

                listPropertiesVersion[op.Name] = (listPropertiesVersion[op.Name]) | version.Value;

                if (listProperties.ContainsKey(op.Name))
                {
                    ObjectPropertyEx opPresent = listProperties[op.Name];

                    if (opPresent.Type != op.Type)
                    {
                        m_nRetValue = 2;
                        EmitError("for Urn:{0}, property {1} has different types in different versions", urn, op.Name);
                        bRetValue = false;
                    }

                    // always choose the most restrictive expensive mark
                    // of all versions
                    (listProperties[op.Name]).Expensive |= op.Expensive;
                }
                else
                {
                    ObjectPropertyEx opex = new ObjectPropertyEx(op);
                    opex.index = -1; // not yet determined

                    XmlNode propNode = FindChildWithName(node, op.Name);

                    if (propNode != null)
                    {
                        opex.isIntrinsic = Boolean.Parse(GetAttribute("is_intrinsic", propNode, "true"));
                        opex.generate = Boolean.Parse(GetAttribute("generate", propNode, "true"));
                        opex.dmfIgnore = Boolean.Parse(GetAttribute("dmf_ignore", propNode, "false"));
                        opex.suppressSfcAttribute = Boolean.Parse(GetAttribute("suppress_sfc_attribute", propNode, "false"));
                        opex.DefaultValue = GetAttribute("default", propNode, null);

                        // Reference attributes
                        opex.ReferenceTemplate = GetAttribute("reference_template", propNode, null);
                        opex.ReferenceType = GetAttribute("reference_type", propNode, null);
                        opex.ReferenceKeys = GetAttribute("reference_keys", propNode, null);
                        opex.ReferenceTemplateParameters = GetAttribute("reference_template_parameters", propNode, null);

                    }
                    // Only Urn, Name and Schema are allowed to be missing
                    else if (op.Name == "Urn" || op.Name == "Name" || op.Name == "Schema")
                    {
                        opex.generate = false;
                    }
                    else
                    {
                        m_nRetValue = 5;
                        EmitError("property '{0}' of class '{1}' is defined in the Enumerator but is missing from cfg.xml", op.Name, GetAttribute("class_name", node, null));
                        bRetValue = false;
                    }

                    listProperties.Add(op.Name, opex);
                }
            }
        }

        return bRetValue;
    }

    static KeyValuePair<ServerVersion, int>[] GetSupportedVersions(DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
    {
        if (databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
        {
            if (databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
            {
                return m_DatawarehouseSupportedVersion;
            }
            else
            {
                return m_CloudSupportedVersion;
            }
        }
        else
        {
            return m_SingletonSupportedVersion;
        }
    }

    static void GenerateClass(XmlNode node, string node_namespace, string output_dir)
    {
        string sClassName = GetAttribute("class_name", node, null);
        bool bGenMetadata = Boolean.Parse(GetAttribute("gen_metadata", node, "true"));
        string sUrn = GetAttribute("urn", node, null);

        string modelOutputDir = output_dir + "model\\";

        if (!String.IsNullOrEmpty(output_dir) && !Directory.Exists(output_dir))
        {
            Directory.CreateDirectory(output_dir);
        }

        if (!String.IsNullOrEmpty(modelOutputDir) && !Directory.Exists(modelOutputDir))
        {
            Directory.CreateDirectory(modelOutputDir);
        }

        string sFilename = Path.Combine(output_dir, sClassName + ".cs");
        string modelFilename = Path.Combine(modelOutputDir, sClassName + ".cs");

        if (thisClassOnly == null)
        {
            Console.Write(sFilename + ": ");
        }
        else if (thisClassOnly != sClassName)
        {
            return; // skip this class
        }

        if (File.Exists(sFilename))
        {
            File.Delete(sFilename);
        }
        listSingletonPropertiesVersion.Clear();
        listCloudPropertiesVersion.Clear();
        listSqlDwPropertiesVersion.Clear();

        listSingletonProperties = GeneratePropertiesList(node, listSingletonPropertiesVersion, true, DatabaseEngineType.Standalone, DatabaseEngineEdition.Unknown);
        listCloudProperties = GeneratePropertiesList(node, listCloudPropertiesVersion, true, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.Unknown);
        listSqlDwProperties = GeneratePropertiesList(node, listSqlDwPropertiesVersion, true, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse);

        if (null == listSingletonProperties || null == listCloudProperties || null == listSqlDwProperties)
        {
            if (m_nRetValue == 0) m_nRetValue = 3;
            EmitError("{0} could not be generated", sFilename);
            return;
        }

        CodeWriter f = new CodeWriter(sFilename);

        f.WriteCodeLine("/*\r\n**** This file has been automatically generated. Do not attempt to modify manually! ****\r\n*/");
        f.WriteCodeLine("/*\r\n**** The generated file is compatible with SFC attribute (metadata) requirement ****\r\n*/");
        f.WriteCodeLine("using System;");
        f.WriteCodeLine("using System.Collections;");
        f.WriteCodeLine("using System.Net;");
        f.WriteCodeLine("using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;");
        f.WriteCodeLine("using Microsoft.SqlServer.Management.Sdk.Sfc;");

        f.WriteCodeLine("using Microsoft.SqlServer.Management.Common;");
        f.WriteLine();

        f.WriteCodeLine("namespace {0}", node_namespace);
        f.IncrementIndent();
        f.WriteCodeLine("/// <summary>");
        f.WriteCodeLine("/// Instance class encapsulating : {0}", sUrn);
        f.WriteCodeLine("/// </summary>");
        f.WriteCodeLine("/// <inheritdoc/>");

        GenerateHeader(f, node, node_namespace);

        if (bGenMetadata)
        {
            GenerateMetadataProvider(f,sClassName, node_namespace, "");
            GenerateXSchema(f, node);
        }
        if (!GenerateCodeProps(f))
        {
            m_nRetValue = 4;
            EmitError("{0} could not be generated", sFilename);
            return;
        }

        GenerateGetPropertyDefaultValueMethod(f);


        if( null != GetAttribute("gen_body", node, null) )
        {
            // generate the body fragments that are inside the class definition
            GenerateBody(f, GetAttribute("gen_body", node, null), false);
        }

        XmlNodeList additionalPropBags = node.SelectNodes("additional_property_bag");
        foreach (XmlNode n in additionalPropBags)
        {
            listSingletonPropertiesVersion = new SortedList<string, int>();
            listSingletonProperties = GeneratePropertiesList(n, listSingletonPropertiesVersion, false, DatabaseEngineType.Standalone, DatabaseEngineEdition.Unknown);
            listCloudProperties = GeneratePropertiesList(n, listCloudPropertiesVersion, false, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.Unknown);
            listSqlDwProperties = GeneratePropertiesList(n, listSqlDwPropertiesVersion, false, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse);


            if (null == listCloudProperties || null == listSingletonProperties || null == listSqlDwProperties)
            {
                m_nRetValue = 4;
                EmitError("{0} could not be generated", sFilename);
                return;
            }
            GenerateMetadataProvider(f, sClassName, node_namespace, GetAttribute("suffix", n, ""));
        }

        f.DecrementIndent();

        // generate the body fragments that are not inside the class definition
        if (null != GetAttribute("gen_body", node, null))
        {
            GenerateBody(f, GetAttribute("gen_body", node, null), true);
        }

        f.DecrementIndent();

        f.Dispose();

        Console.WriteLine("done");
    }

    static void GenerateXSchema(CodeWriter f, XmlNode node)
    {
        bool bSupportXSchema = Boolean.Parse(GetAttribute(attrSupportsXSchema, node, "false"));
        if (bSupportXSchema)
        {
            string sClassName = GetAttribute("class_name", node, null);

            f.WriteCodeLine("private sealed class XSchemaProps");
            f.IncrementIndent();
            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                if (op.isIntrinsic)
                {
                    f.WriteCodeLine("private {0} _{1};", op.Type, op.Name);
                    f.WriteCodeLine("internal {0} {1} {{ get{{ return _{1}; }} set{{ _{1}=value; }} }}", op.Type, op.Name);
                    f.WriteLine();
                }
            }

	        foreach (ObjectPropertyEx op in listCloudProperties.Values)
            {
                if (op.isIntrinsic && !listSingletonProperties.ContainsKey(op.Name))
                {
                    f.WriteCodeLine("private {0} _{1};", op.Type, op.Name);
                    f.WriteCodeLine("internal {0} {1} {{ get{{ return _{1}; }} set{{ _{1}=value; }} }}", op.Type, op.Name);
                    f.WriteLine();
                }
            }

            foreach (ObjectPropertyEx op in listSqlDwProperties.Values)
            {
                if (op.isIntrinsic && !listSingletonProperties.ContainsKey(op.Name) && !listCloudProperties.ContainsKey(op.Name))
                {
                    f.WriteCodeLine("private {0} _{1};", op.Type, op.Name);
                    f.WriteCodeLine("internal {0} {1} {{ get{{ return _{1}; }} set{{ _{1}=value; }} }}", op.Type, op.Name);
                    f.WriteLine();
                }
            }

            f.DecrementIndent();

            f.WriteLine();

            f.WriteCodeLine("private sealed class XRuntimeProps");
            f.IncrementIndent();
            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                if (!op.isIntrinsic)
                {
                    f.WriteCodeLine("private {0} _{1};", op.Type, op.Name);
                    f.WriteCodeLine("internal {0} {1} {{ get{{ return _{1}; }} set{{ _{1}=value; }} }}", op.Type, op.Name);
                    f.WriteLine();
                }
            }

            foreach (ObjectPropertyEx op in listCloudProperties.Values)
            {
                if (!op.isIntrinsic && !listSingletonProperties.ContainsKey(op.Name))
                {
                    f.WriteCodeLine("private {0} _{1};", op.Type, op.Name);
                    f.WriteCodeLine("internal {0} {1} {{ get{{ return _{1}; }} set{{ _{1}=value; }} }}", op.Type, op.Name);
                    f.WriteLine();
                }
            }

            foreach (ObjectPropertyEx op in listSqlDwProperties.Values)
            {
                if (!op.isIntrinsic && !listSingletonProperties.ContainsKey(op.Name) && !listCloudProperties.ContainsKey(op.Name))
                {
                    f.WriteCodeLine("private {0} _{1};", op.Type, op.Name);
                    f.WriteCodeLine("internal {0} {1} {{ get{{ return _{1}; }} set{{ _{1}=value; }} }}", op.Type, op.Name);
                    f.WriteLine();
                }
            }

            f.DecrementIndent();

            f.WriteLine();

            f.WriteCodeLine("object IPropertyDataDispatch.GetPropertyValue( int index )");
            f.IncrementIndent();
            f.WriteCodeLine("object value;");
            f.WriteCodeLine("if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)");
            f.IncrementIndent();
            //Engine Edition of SQL Datawarehouse has its own set of properties
            f.WriteCodeLine("if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)");
            f.IncrementIndent();
            f.WriteCodeLine("switch(index)");
            f.IncrementIndent();
            foreach (ObjectPropertyEx op in listSqlDwProperties.Values)
            {
                string schema_or_runtime = op.isIntrinsic ? "XSchema" : "XRuntime";
                f.WriteCodeLine("case {0}:", op.index);
                f.WriteCodeLine("value = this.{0}.{1};", schema_or_runtime, op.Name);
                f.WriteCodeLine("break;");
            }

            f.WriteCodeLine("default:");
            f.WriteCodeLine("throw new IndexOutOfRangeException();");
            f.DecrementIndent();
            f.DecrementIndent();
            //Not Datawarehouse so default to normal cloud properties
            f.WriteCodeLine("else");
            f.IncrementIndent();
            f.WriteCodeLine("switch(index)");
            f.IncrementIndent();
            foreach (ObjectPropertyEx op in listCloudProperties.Values)
            {
                string schema_or_runtime = op.isIntrinsic ? "XSchema" : "XRuntime";
                f.WriteCodeLine("case {0}:", op.index);
                f.WriteCodeLine("value = this.{0}.{1};", schema_or_runtime, op.Name);
                f.WriteCodeLine("break;");
            }

            f.WriteCodeLine("default:");
            f.WriteCodeLine("throw new IndexOutOfRangeException();");
            f.DecrementIndent();
            f.DecrementIndent();
            f.DecrementIndent();

            f.WriteCodeLine("else");
            f.IncrementIndent();
            f.WriteCodeLine("switch(index)");
            f.IncrementIndent();
            foreach(ObjectPropertyEx op in listSingletonProperties.Values)
            {
                string schema_or_runtime = op.isIntrinsic ? "XSchema" : "XRuntime";
                f.WriteCodeLine("case {0}:", op.index);
                f.WriteCodeLine("value = this.{0}.{1};", schema_or_runtime, op.Name);
                f.WriteCodeLine("break;");
            }

            f.WriteCodeLine("default:");
            f.WriteCodeLine("throw new IndexOutOfRangeException();");
            f.DecrementIndent();
            f.DecrementIndent();
            f.WriteCodeLine("return value;");
            f.DecrementIndent();

            f.WriteCodeLine("void IPropertyDataDispatch.SetPropertyValue( int index, object value )");
            f.IncrementIndent();
            f.WriteCodeLine("if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)");
            //SQL Datawarehouse has its own set of properties
            f.IncrementIndent();
            f.WriteCodeLine("if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)");
            f.IncrementIndent();
            f.WriteCodeLine("switch(index)");
            f.IncrementIndent();

            int id = 0;
            foreach (ObjectPropertyEx op in listSqlDwProperties.Values)
            {
                string schema_or_runtime = op.isIntrinsic ? "XSchema" : "XRuntime";
                f.WriteCodeLine("case {0}:", op.index);
                f.WriteCodeLine("this.{0}.{1} = ({2})value;", schema_or_runtime, op.Name, op.Type);
                f.WriteCodeLine("break;");
                id++;
            }
            f.WriteCodeLine("default:");
            f.WriteCodeLine("throw new IndexOutOfRangeException();");
            f.DecrementIndent();
            f.DecrementIndent();

            //Not SQL Datawarehouse, default to normal cloud properties
            f.WriteCodeLine("else");
            f.IncrementIndent();
            f.WriteCodeLine("switch(index)");
            f.IncrementIndent();

            foreach (ObjectPropertyEx op in listCloudProperties.Values)
            {
                string schema_or_runtime = op.isIntrinsic ? "XSchema" : "XRuntime";
                f.WriteCodeLine("case {0}:", op.index);
                f.WriteCodeLine("this.{0}.{1} = ({2})value;", schema_or_runtime, op.Name, op.Type);
                f.WriteCodeLine("break;");
            }

            f.WriteCodeLine("default:");
            f.WriteCodeLine("throw new IndexOutOfRangeException();");
            f.DecrementIndent();
            f.DecrementIndent();
            f.DecrementIndent();

            f.WriteCodeLine("else");
            f.IncrementIndent();
            f.WriteCodeLine("switch(index)");
            f.IncrementIndent();
            foreach(ObjectPropertyEx op in listSingletonProperties.Values)
            {
                string schema_or_runtime = op.isIntrinsic ? "XSchema" : "XRuntime";
                f.WriteCodeLine("case {0}:", op.index);
                f.WriteCodeLine("this.{0}.{1} = ({2})value;", schema_or_runtime, op.Name, op.Type);
                f.WriteCodeLine("break;");
            }

            f.WriteCodeLine("default:");
            f.WriteCodeLine("throw new IndexOutOfRangeException();");
            f.DecrementIndent();
            f.DecrementIndent();
            f.DecrementIndent();

            f.WriteLine();

            f.WriteCodeLine("XSchemaProps _XSchema;");
            f.WriteCodeLine("XSchemaProps XSchema ");
            f.IncrementIndent();
            f.WriteCodeLine("get");
            f.IncrementIndent();
            f.WriteCodeLine("if( _XSchema == null )");
            f.IncrementIndent();
            f.WriteCodeLine("_XSchema = new XSchemaProps();");
            f.DecrementIndent();
            f.WriteCodeLine("return _XSchema; ");
            f.DecrementIndent();
            f.DecrementIndent();

            f.WriteCodeLine("XRuntimeProps _XRuntime;");
            f.WriteCodeLine("XRuntimeProps XRuntime");
            f.IncrementIndent();
            f.WriteCodeLine("get");
            f.IncrementIndent();
            f.WriteCodeLine("if( _XRuntime == null )");
            f.IncrementIndent();
            f.WriteCodeLine("_XRuntime = new XRuntimeProps();");
            f.DecrementIndent();
            f.WriteCodeLine("return _XRuntime;");
            f.DecrementIndent();
            f.DecrementIndent();

            f.WriteLine();

        }
    }

    static bool IsWithVersion(string node_namespace)
    {
        bool bWithVersion = false;
        if ("Microsoft.SqlServer.Management.Smo" == node_namespace ||
            "Microsoft.SqlServer.Management.Smo.Agent" == node_namespace ||
            "Microsoft.SqlServer.Management.Smo.Broker" == node_namespace ||
            "Microsoft.SqlServer.Management.Smo.Mail" == node_namespace ||
            "Microsoft.SqlServer.Management.Nmo" == node_namespace)
        {
            bWithVersion = true;
        }

        return bWithVersion;
    }

    static void GenerateMetadataProvider(CodeWriter f,string sClassName, string node_namespace, string suffix)
    {
        bool bWithVersion = IsWithVersion(node_namespace);
        string strOveride = " override ";
        if (suffix.Length > 0)
        {
            strOveride = " ";
        }
        if (bWithVersion)
        {
            f.WriteCodeLine("internal" + strOveride + "SqlPropertyMetadataProvider GetPropertyMetadataProvider" + suffix + "()");
            f.IncrementIndent();
            f.WriteCodeLine("return new PropertyMetadataProvider" + suffix + "(this.ServerVersion,this.DatabaseEngineType, this.DatabaseEngineEdition);");
            f.DecrementIndent();

            f.WriteCodeLine("internal class PropertyMetadataProvider" + suffix + " : SqlPropertyMetadataProvider");
            f.IncrementIndent();

            f.WriteCodeLine("internal PropertyMetadataProvider" + suffix + "(Common.ServerVersion version,DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition) : base(version,databaseEngineType, databaseEngineEdition)");

            f.IncrementIndent();
            f.DecrementIndent();
        }
        else
        {
            f.WriteCodeLine("internal" + strOveride + "Smo.PropertyMetadataProvider GetPropertyMetadataProvider" + suffix + "()");
            f.IncrementIndent();
            f.WriteCodeLine("return new PropertyMetadataProvider" + suffix + "();");
            f.DecrementIndent();

            f.WriteCodeLine("internal class PropertyMetadataProvider" + suffix + " : Smo.PropertyMetadataProvider");
            f.IncrementIndent();
        }

        GeneratePropNameToIDLookup(f, bWithVersion);
        GenerateMetadataTable(f, bWithVersion);

        f.DecrementIndent();

    }

    static void GeneratePropNameToIDLookup(CodeWriter f, bool bWithVersion)
    {
        f.WriteCodeLine("public override int PropertyNameToIDLookup(string propertyName)");
        f.IncrementIndent();
        // VBUMP
        int v7=0;
        int v8=0;
        int v9=0;
        int v10=0;
        int v10_50 = 0;
        int v11 = 0;
        int v12 = 0;
        int v13 = 0;
        int v14 = 0;
        int v15 = 0;
        int v16 = 0;
        int v17 = 0;
        int cv10 = 0;
        int cv11 = 0;
        int cv12 = 0;
        //There's only going to be one version of datawarehouse so we don't
        //need to keep track of counts for each version
        int datawarehousePropertyCount = 0;

        if (bWithVersion)
        {
            f.WriteCodeLine("if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)");
            f.IncrementIndent();
            f.WriteCodeLine("if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)");
            f.IncrementIndent();

            /**
            *
            *   SQL DW PROPERTIES
            *
            **/
            if (listSqlDwProperties.Count > 0)
            {
                int id = 0;
                bool addedSwitch = false;
                foreach (ObjectPropertyEx op in listSqlDwProperties.Values)
                {
                    if (!addedSwitch)
                    {
                        f.WriteCodeLine("switch(propertyName)");
                        f.IncrementIndent();
                        addedSwitch = true;
                    }
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
                datawarehousePropertyCount = id;
                if (addedSwitch)
                {
                    f.DecrementIndent();
                }
            }
            f.WriteCodeLine("return -1;");
            f.DecrementIndent();
            f.WriteCodeLine("else");
            f.IncrementIndent();

            /**
            *
            *   CLOUD PROPERTIES
            *
            **/
            if (listCloudProperties.Count > 0)
            {

                f.WriteCodeLine("switch(propertyName)");
                f.IncrementIndent();

                int id = 0;
                //Add the cloud v10 properties
                foreach (ObjectPropertyEx op in listCloudProperties.Values)
                {
                    int i = (int)listCloudPropertiesVersion[op.Name];
                    if( (i & (int)CloudSupportedVersionFlags.v10_0) == (int)CloudSupportedVersionFlags.v10_0)
                    {
                        op.index = id;
                        f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                    }
                }
                cv10 = id;

                //Add the cloud v11 properties
                foreach (ObjectPropertyEx op in listCloudProperties.Values)
                {
                    int i = (int)listCloudPropertiesVersion[op.Name];
                    if ((i & (int)CloudSupportedVersionFlags.v10_0) == (int)CloudSupportedVersionFlags.NOT_SET &&
                        (i & (int)CloudSupportedVersionFlags.v11_0) == (int)CloudSupportedVersionFlags.v11_0)
                    {
                        op.index = id;
                        f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                    }
                }
                cv11 = id;

                //Add the cloud v12 properties
                foreach (ObjectPropertyEx op in listCloudProperties.Values)
                {
                    int i = (int)listCloudPropertiesVersion[op.Name];
                    if ((i & (int)CloudSupportedVersionFlags.v10_0) == (int)CloudSupportedVersionFlags.NOT_SET &&
                        (i & (int)CloudSupportedVersionFlags.v11_0) == (int)CloudSupportedVersionFlags.NOT_SET &&
                        (i & (int)CloudSupportedVersionFlags.v12_0) == (int)CloudSupportedVersionFlags.v12_0)
                    {
                        op.index = id;
                        f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                    }
                }
                cv12 = id;

                f.DecrementIndent();
            }
            f.WriteCodeLine("return -1;");
            f.DecrementIndent();
            f.DecrementIndent();
            f.WriteCodeLine("else");
            f.IncrementIndent();

        }

        /**
        *
        *   ON-PREM PROPERTIES
        *   There has to be a better way to do this...
        **/
        if (listSingletonProperties.Count > 0)
        {
            f.WriteCodeLine("switch(propertyName)");
            f.IncrementIndent();

            int id = 0;
            //On-Prem v7.0
            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.v7_0)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v7 = id;
            //On-Prem v8.0
            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.v8_0)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v8 = id;
            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.v9_0)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v9 = id;
            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.v10_0)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v10 = id;
            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.v10_50)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v10_50 = id;
            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.v11_0)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v11 = id;
            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                        && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.v12_0)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v12 = id;

            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int) listSingletonPropertiesVersion[op.Name];
                if ((i & (int) SingletonSupportedVersionFlags.v7_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v8_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v9_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v10_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v10_50) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v11_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v12_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v13_0) == (int) SingletonSupportedVersionFlags.v13_0)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v13 = id;

            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int) listSingletonPropertiesVersion[op.Name];
                if ((i & (int) SingletonSupportedVersionFlags.v7_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v8_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v9_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v10_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v10_50) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v11_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v12_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v13_0) == (int) SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int) SingletonSupportedVersionFlags.v14_0) == (int) SingletonSupportedVersionFlags.v14_0)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v14 = id;

            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v13_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v14_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v15_0) == (int)SingletonSupportedVersionFlags.v15_0)
                {
                    op.index = id;
                    f.WriteCodeLine("case \"{0}\": return {1};", op.Name, id++);
                }
            }
            v15 = id;

            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v13_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v14_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v15_0) == (int)SingletonSupportedVersionFlags.NOT_SET 
                    && (i & (int)SingletonSupportedVersionFlags.v16_0) == (int)SingletonSupportedVersionFlags.v16_0)
                {
                    op.index = id;
                    f.WriteCodeLine($"case \"{op.Name}\": return {id++};");
                }
            }
            v16 = id;

            foreach (ObjectPropertyEx op in listSingletonProperties.Values)
            {
                int i = (int)listSingletonPropertiesVersion[op.Name];
                if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v13_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v14_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v15_0) == (int)SingletonSupportedVersionFlags.NOT_SET 
                    && (i & (int)SingletonSupportedVersionFlags.v16_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v17_0) == (int)SingletonSupportedVersionFlags.v17_0)
                {
                    op.index = id;
                    f.WriteCodeLine($"case \"{op.Name}\": return {id++};");
                }
            }
            v17 = id;
            // VBUMP

            f.DecrementIndent();


        }
        f.WriteCodeLine("return -1;");
        f.DecrementIndent();
        if (bWithVersion)
        {
            f.DecrementIndent();
        }

        if (bWithVersion)
        {
            // VBUMP
            f.WriteCodeLine("static int [] versionCount = new int [] {{ {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11} }};", v7, v8, v9, v10, v10_50, v11, v12, v13, v14, v15, v16, v17);
            f.WriteCodeLine("static int [] cloudVersionCount = new int [] {{ {0}, {1}, {2} }};", cv10, cv11, cv12);
            f.WriteCodeLine("static int sqlDwPropertyCount = {0};", datawarehousePropertyCount);
            f.WriteCodeLine("public override int Count");
            f.IncrementIndent();
            f.WriteCodeLine("get");
            f.IncrementIndent();
            f.WriteCodeLine("if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)");
            f.IncrementIndent();
            //Datawarehouse has its own set of properties
            f.WriteCodeLine("if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)");
            f.IncrementIndent();
            f.WriteCodeLine("return sqlDwPropertyCount;");
            f.DecrementIndent();
            //Not Datawarehouse, default to normal cloud properties
            f.WriteCodeLine("else");
            f.IncrementIndent();
            f.WriteCodeLine("int index = (currentVersionIndex < cloudVersionCount.Length) ? currentVersionIndex : cloudVersionCount.Length - 1;");
            f.WriteCodeLine("return cloudVersionCount[index];");
            f.DecrementIndent();
            f.DecrementIndent();
            f.WriteCodeLine(" else ");
            f.IncrementIndent();
            f.WriteCodeLine("int index = (currentVersionIndex < versionCount.Length) ? currentVersionIndex : versionCount.Length - 1;");
            f.WriteCodeLine("return versionCount[index];");
            f.DecrementIndent();
            f.DecrementIndent();
            f.DecrementIndent();

            f.WriteCodeLine("protected override int[] VersionCount");
            f.IncrementIndent();
            f.WriteCodeLine("get");
            f.IncrementIndent();
            f.WriteCodeLine("if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)");
            f.IncrementIndent();
            //Datawarehouse has a separate set of properties
            f.WriteCodeLine("if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)");
            f.IncrementIndent();
            f.WriteCodeLine(" return new int[] { sqlDwPropertyCount }; ");
            f.DecrementIndent();
            //Not datawarehouse, default to normal cloud properties
            f.WriteCodeLine("else");
            f.IncrementIndent();
            f.WriteCodeLine(" return cloudVersionCount; ");
            f.DecrementIndent();
            f.DecrementIndent();

            f.WriteCodeLine(" else ");
            f.IncrementIndent();
            f.WriteCodeLine(" return versionCount;  ");
            f.DecrementIndent();
            f.DecrementIndent();
            f.DecrementIndent();
            f.WriteCodeLine("new internal static int[] GetVersionArray(DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)");
            f.IncrementIndent();
             f.WriteCodeLine("if(databaseEngineType == DatabaseEngineType.SqlAzureDatabase)");
            f.IncrementIndent();
            //Datawarehouse has its own set of properties
            f.WriteCodeLine("if(databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)");
            f.IncrementIndent();
            f.WriteCodeLine(" return new int[] { sqlDwPropertyCount }; ");
            f.DecrementIndent();
            //Not datawarehouse, default to normal cloud properties
            f.WriteCodeLine("else");
            f.IncrementIndent();
            f.WriteCodeLine(" return cloudVersionCount; ");
            f.DecrementIndent();

            f.DecrementIndent();
            f.WriteCodeLine(" else ");
            f.IncrementIndent();
            f.WriteCodeLine(" return versionCount;  ");
            f.DecrementIndent();
            f.DecrementIndent();
        }
        else
        {
            f.WriteCodeLine("public override int Count");
            f.IncrementIndent();
            // VBUMP
            f.WriteCodeLine("get {{ return {0}; }}", v17);
            f.DecrementIndent();
        }

        f.WriteCodeLine();
    }

    static void GenerateMetadataTable(CodeWriter f, bool bWithVersion)
    {
        f.WriteCodeLine("public override StaticMetadata GetStaticMetadata(int id)");
        f.IncrementIndent();
        if (bWithVersion)
        {
            f.WriteCodeLine("if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)");
            f.IncrementIndent();
            //Datawarehouse has its own set of properties
            f.WriteCodeLine("if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)");
            f.IncrementIndent();
            f.WriteCodeLine(" return sqlDwStaticMetadata[id]; ");
            f.DecrementIndent();

            //Not datawarehouse, default to normal cloud properties
            f.WriteCodeLine("else");
            f.IncrementIndent();
            f.WriteCodeLine(" return cloudStaticMetadata[id]; ");
            f.DecrementIndent();
            f.DecrementIndent();

            f.WriteCodeLine(" else ");
            f.IncrementIndent();
            f.WriteCodeLine("return staticMetadata[id];");
            f.DecrementIndent();
            f.DecrementIndent();
            f.WriteCodeLine("new internal static StaticMetadata[] GetStaticMetadataArray(DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)");
            f.IncrementIndent();
            f.WriteCodeLine("if(databaseEngineType == DatabaseEngineType.SqlAzureDatabase)");
            f.IncrementIndent();
            //Datawarehouse has its own set of properties
            f.WriteCodeLine("if(databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)");
            f.IncrementIndent();
            f.WriteCodeLine(" return sqlDwStaticMetadata; ");
            f.DecrementIndent();

            //Not datawarehouse, default to normal cloud properties
            f.WriteCodeLine("else");
            f.IncrementIndent();
            f.WriteCodeLine(" return cloudStaticMetadata;");
            f.DecrementIndent();
            f.DecrementIndent();

            f.WriteCodeLine(" else ");
            f.IncrementIndent();
            f.WriteCodeLine("return staticMetadata;");
            f.DecrementIndent();
            f.DecrementIndent();

            /**
            *
            *   SQL DW PROPERTIES STATIC METADATA
            *
            **/
            f.WriteCodeLine("internal static StaticMetadata [] sqlDwStaticMetadata = ");
            f.IncrementIndent();
            foreach (ObjectProperty op in listSqlDwProperties.Values)
            {
                int i = (int)listSqlDwPropertiesVersion[op.Name];
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
            f.DecrementIntendWithSemi();

            f.WriteCodeLine("internal static StaticMetadata [] cloudStaticMetadata = ");
            f.IncrementIndent();
            /**
            *
            *   CLOUD PROPERTIES STATIC METADATA
            *
            **/
            //Cloud version 10.0
            foreach (ObjectProperty op in listCloudProperties.Values)
            {
                int i = (int)listCloudPropertiesVersion[op.Name];
                if ((i & (int)CloudSupportedVersionFlags.v10_0) == (int)CloudSupportedVersionFlags.v10_0)
                {
                    f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString().ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString().ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
                }
            }
            //Cloud version 11.0
            foreach (ObjectProperty op in listCloudProperties.Values)
            {
                int i = (int)listCloudPropertiesVersion[op.Name];
                if ((i & (int)CloudSupportedVersionFlags.v10_0) == (int)CloudSupportedVersionFlags.NOT_SET &&
                    (i & (int)CloudSupportedVersionFlags.v11_0) == (int)CloudSupportedVersionFlags.v11_0)
                {
                    f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
                }
            }
            //Cloud version 12.0
            foreach (ObjectProperty op in listCloudProperties.Values)
            {
                int i = (int)listCloudPropertiesVersion[op.Name];
                if ((i & (int)CloudSupportedVersionFlags.v10_0) == (int)CloudSupportedVersionFlags.NOT_SET &&
                    (i & (int)CloudSupportedVersionFlags.v11_0) == (int)CloudSupportedVersionFlags.NOT_SET &&
                    (i & (int)CloudSupportedVersionFlags.v12_0) == (int)CloudSupportedVersionFlags.v12_0)
                {
                    f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
                }
            }
            f.DecrementIntendWithSemi();
        }
        else
        {
            f.WriteCodeLine("return staticMetadata[id];");
            f.DecrementIndent();
        }

        /**
        *
        *   ON-PREM PROPERTIES STATIC METADATA
        *
        **/
        f.WriteCodeLine("internal static StaticMetadata [] staticMetadata = ");
        f.IncrementIndent();
        //On-Prem v7.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.v7_0)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString().ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString().ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }
        //On-Prem v8.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.v8_0)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }
        //On-Prem v9.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.v9_0)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }
        //On-Prem v10.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.v10_0)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }
        //On-Prem v10.5
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.v10_50)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString().ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString().ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }
        //On-Prem v11.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.v11_0)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString().ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString().ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }
        //On-Prem v12.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.v12_0)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString().ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString().ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }

        //On-Prem v13.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v13_0) == (int)SingletonSupportedVersionFlags.v13_0)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString().ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString().ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }

        //On-Prem v14.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v13_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v14_0) == (int)SingletonSupportedVersionFlags.v14_0)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString().ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString().ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }

        //On-Prem v15.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v13_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v14_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v15_0) == (int)SingletonSupportedVersionFlags.v15_0)
            {
                f.WriteCodeLine("new StaticMetadata(\"{0}\", {1}, {2}, {3}),", op.Name, op.Expensive.ToString().ToLower(CultureInfo.CurrentCulture), op.ReadOnly.ToString().ToLower(CultureInfo.CurrentCulture), "typeof(" + op.Type + ")");
            }
        }

        //On-Prem v16.0
        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v13_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v14_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v15_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v16_0) == (int)SingletonSupportedVersionFlags.v16_0)
            {
                f.WriteCodeLine($"new StaticMetadata(\"{op.Name}\", {op.Expensive.ToString().ToLower(CultureInfo.InvariantCulture)}, {op.ReadOnly.ToString().ToLower(CultureInfo.InvariantCulture)}, typeof({op.Type})),");
            }
        }

        foreach (ObjectProperty op in listSingletonProperties.Values)
        {
            int i = (int)listSingletonPropertiesVersion[op.Name];
            if ((i & (int)SingletonSupportedVersionFlags.v7_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v8_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v9_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v10_50) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v11_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v12_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v13_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v14_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v15_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v16_0) == (int)SingletonSupportedVersionFlags.NOT_SET
                    && (i & (int)SingletonSupportedVersionFlags.v17_0) == (int)SingletonSupportedVersionFlags.v17_0)
            {
                f.WriteCodeLine($"new StaticMetadata(\"{op.Name}\", {op.Expensive.ToString().ToLower(CultureInfo.InvariantCulture)}, {op.ReadOnly.ToString().ToLower(CultureInfo.InvariantCulture)}, typeof({op.Type})),");
            }

            // VBUMP
        }

        f.DecrementIntendWithSemi();
    }

    static void GenerateHeader(CodeWriter f, XmlNode node, string node_namespace)
    {
        string sClassName = GetAttribute("class_name", node, null);
        string sParent = GetAttribute("parent_type", node, null);
        string sPossibleParents = GetAttribute("possible_parents", node, null);
        string sUrn = GetAttribute("urn", node, null);
        string sCtorParent = GetAttribute("ctor_parent", node, m_sDefCtorParent);
        string sBaseClassName = GetAttribute("base_class", node, string.Empty);
        bool hasSchema = Boolean.Parse(GetAttribute("has_schema", node, "false"));
        bool hasPublicCtors = Boolean.Parse(GetAttribute("has_constructors", node, "true"));
        string sImplements = GetAttribute("implements", node, "");
        string sSealed = Boolean.Parse(GetAttribute("sealed", node, "true")) ? "sealed" : "";
        bool bHasNew = Boolean.Parse(GetAttribute("has_new", node, "true"));
        bool singleton = Boolean.Parse(GetAttribute("singleton", node, "false"));
        bool init_defaults = Boolean.Parse(GetAttribute("init_defaults", node, "false"));
        bool parent_has_setter = Boolean.Parse(GetAttribute("parent_has_setter", node, "false"));
        bool isDesignMode = Boolean.Parse(GetAttribute("is_design_mode", node, "false"));
        string parentMode = GetAttribute("parent_mode", node, null);

        string sNew = " ";
        if (sCtorParent != m_sDefCtorParent)
        {
            sNew = " new ";
        }
        if (null != GetAttribute("new_on_get_parent", node, null))
        {
            sNew = Boolean.Parse(GetAttribute("new_on_get_parent", node, null)) ? " new " : " ";
        }

        string baseClassOrItf = string.Empty;
        if (sBaseClassName.Length > 0)
        {
            baseClassOrItf = " : " + sBaseClassName;
            if (isDesignMode)
            {
                baseClassOrItf += ", ISfcSupportsDesignMode";
            }
        }
        else if (isDesignMode)
        {
            baseClassOrItf = " : ISfcSupportsDesignMode";
        }

        bool implementsPermissions = false;

        if (sImplements.Length > 0)
        {
            if ((sBaseClassName.Length > 0) || isDesignMode)
                baseClassOrItf += ", " + sImplements;
            else
                baseClassOrItf = " : " + sImplements;

            implementsPermissions = sImplements.Contains("IObjectPermission");
        }

        if (Boolean.Parse(GetAttribute(attrSupportsXSchema, node, "false")))
        {
            if (baseClassOrItf.Length > 0)
                baseClassOrItf += ", IPropertyDataDispatch";
            else
                baseClassOrItf = " : IPropertyDataDispatch";
        }

        GenerateSfcElementAttribute(f, node_namespace);

        f.WriteCodeLine("public {0} {1} class {2} {3}",
            sSealed,
            sBaseClassName.Length > 0 ? "" : "partial",
            sClassName,
            baseClassOrItf);

        f.IncrementIndent();

        if (0 < node_namespace.LastIndexOf(".Wmi", StringComparison.Ordinal))
        {
        }
        else
        {
            if (sBaseClassName.Length > 0)
            {
                f.WriteCodeLine("internal {0}({1} parent, ObjectKeyBase key, SqlSmoState state) :", sClassName, sCtorParent);
                f.WriteCodeLine("base(parent, key, state)");
                f.IncrementIndent();
                f.DecrementIndent();
                f.WriteCodeLine();
            }

            // public constructors
            if (null != sParent && hasPublicCtors)
            {
                f.WriteCodeLine("public {0}() : base(){{ }}", sClassName);

                string parentVar = string.Empty;
                if (sParent == "SqlSmoObject")
                {
                    parentVar = "parent";
                }
                else
                {
                    StringBuilder parentName = new StringBuilder(sParent);
                    parentName[0] = char.ToLower(parentName[0]);
                    parentVar = parentName.ToString();
                }

                f.WriteCodeLine("public {0}({1} {2}, string name) : base()", sClassName, sParent, parentVar);
                f.IncrementIndent();
                f.WriteCodeLine("ValidateName(name);");
                if (hasSchema)
                {
                    f.WriteCodeLine("this.key = new SchemaObjectKey(name, null);");
                }
                else
                {
                    f.WriteCodeLine("this.key = new SimpleObjectKey(name);");
                }
                f.WriteCodeLine("this.Parent = {0};", parentVar);

                if (init_defaults)
                {
                    f.WriteCodeLine("InitializeDefaults();");
                }
                f.DecrementIndent();

                if (hasSchema)
                {
                    f.WriteCodeLine("public {0}({1} {2}, string name, string schema) : base()", sClassName, sParent, parentVar);
                    f.IncrementIndent();
                    f.WriteCodeLine("ValidateName(name);");
                    f.WriteCodeLine("this.key = new SchemaObjectKey(name, schema);");
                    f.WriteCodeLine("this.Parent = {0};", parentVar);
                    f.DecrementIndent();
                }
            }
        }

        if (bHasNew && null != sParent && !singleton)
        {
            StringBuilder parentRelation = new StringBuilder();
            parentRelation.Append("SfcObjectRelationship.ParentObject");
            if (!string.IsNullOrEmpty(parentMode))
            {
                char[] delim = new char[] { ';' };
                string[] parentModes = parentMode.Split(delim);
                parentRelation.Append(",");
                foreach (string mode in parentModes)
                {
                    switch (mode.ToUpperInvariant())
                    {
                        case "DESIGN":
                            parentRelation.Append(" SfcObjectFlags.Design |");
                            break;
                        case "DEPLOY":
                            parentRelation.Append(" SfcObjectFlags.Deploy |");
                            break;
                    }
                }
                parentRelation.Remove(parentRelation.Length - 2, 2);
            }

            f.WriteCodeLine("[SfcObject({0})]", parentRelation.ToString());
            if (sPossibleParents != null)
            {
                // The list of all possible derived parent types of this type
                // If not present, it is assumed there is only one valid parent type and it is the return type
                // of the Parent property of this class.
                char[] delim = new char[] { ';' };
                string[] parentNames = sPossibleParents.Split(delim);
                foreach (string parentName in parentNames)
                {
                    // Validate that each type name refers to aclass object in the same namespace.
                    if (!CheckValidNamespaceClassObjectsList(parentName))
                    {
                        m_nRetValue = 7;
                        EmitError("for Class:{0}, the possible_parent {1} is not a valid class in the same namespace",
                        sClassName, parentName);
                    }
                    f.WriteCodeLine("[SfcParent(\"{0}\")]", parentName);
                }
            }
            f.WriteCodeLine("public{1}{0} Parent", sParent, sNew);
            f.IncrementIndent();
            f.WriteCodeLine("get");
            f.IncrementIndent();
            f.WriteCodeLine("CheckObjectState();");
            if (m_sDefCtorParent == sCtorParent)
            {
                f.WriteCodeLine("return base.ParentColl.ParentInstance as {0};", sParent);
            }
            else
            {
                f.WriteCodeLine("return base.Parent as {0};", sParent);
            }
            f.DecrementIndent();
            if (hasPublicCtors || init_defaults || parent_has_setter)
            {
                f.WriteCodeLine("set{SetParentImpl(value);}");
            }
            f.DecrementIndent();
        }

        // add support for permissions
        if (implementsPermissions)
        {
            /////////// <summary>
            /////////// This object supports permissions.
            /////////// </summary>
            ////////internal override UserPermissionCollection Permissions
            ////////{
            ////////    get
            ////////    {
            ////////        // call the base class
            ////////        return GetUserPermissions();
            ////////    }
            ////////}
            f.WriteCodeLine("/// <summary>");
            f.WriteCodeLine("/// This object supports permissions.");
            f.WriteCodeLine("/// </summary>");
            f.WriteCodeLine("internal override UserPermissionCollection Permissions");
            f.IncrementIndent();
            f.WriteCodeLine("get");
            f.IncrementIndent();
            f.WriteCodeLine("// call the base class ");
            f.WriteCodeLine("return GetUserPermissions();");
            f.DecrementIndent();
            f.DecrementIndent();
        }
        //add implementation of ISfcSupportsDesignMode
        if (isDesignMode)
        {
            f.WriteCodeLine("/// <summary>");
            f.WriteCodeLine("/// This object extend ISfcSupportsDesignMode.");
            f.WriteCodeLine("/// </summary>");
            f.WriteCodeLine("bool ISfcSupportsDesignMode.IsDesignMode");
            f.IncrementIndent();
            f.WriteCodeLine("get");
            f.IncrementIndent();
            f.WriteCodeLine("// call the base class ");
            f.WriteCodeLine("return IsDesignMode;");
            f.DecrementIndent();
            f.DecrementIndent();
        }
    }

    static void GenerateSfcElementAttribute(CodeWriter f, string node_namespace)
    {
        StringBuilder sb = new StringBuilder();

        if (0 == string.Compare(node_namespace, "Microsoft.SqlServer.Management.Smo", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("[SfcElement( SfcElementFlags.Standalone ");
            if (listCloudProperties.Count > 0 || listSqlDwProperties.Count > 0)
            {
                sb.Append("| SfcElementFlags.SqlAzureDatabase ");
            }
            sb.Append(")]");
        }

        if (sb.Length > 0)
        {
            f.WriteCodeLine(sb.ToString());
        }
    }

    /// <summary>
    /// Generates code for the override of GetPropertyDefaultValue(...) method
    /// that will return any default values for properties that specify them.
    /// </summary>
    /// <param name="f"></param>
    private static void GenerateGetPropertyDefaultValueMethod(CodeWriter f)
    {
        //Initial populate with all singleton properties
        IList<KeyValuePair<string, ObjectPropertyEx>> defaultValueProps =
            listSingletonProperties.Where(p => p.Value.DefaultValue != null).ToList();
        //Now add in cloud properties that don't already exist in our list
        defaultValueProps = defaultValueProps.Union(
            listCloudProperties.Where(
                p => p.Value.DefaultValue != null && defaultValueProps.Any(i => i.Key == p.Key) == false)).ToList();

        // add SQL DW properties that don't already exist in our list
        defaultValueProps = defaultValueProps.Union(
            listSqlDwProperties.Where(
                p => p.Value.DefaultValue != null && defaultValueProps.Any(i => i.Key == p.Key) == false)).ToList();

        if (defaultValueProps.Any())
        {
            f.WriteCodeLine("internal override object GetPropertyDefaultValue(string propname)");
            f.IncrementIndent();
            f.WriteCodeLine("switch (propname)");
            f.IncrementIndent();
            foreach (ObjectPropertyEx op in defaultValueProps.Select(p => p.Value))
            {
                if (op.DefaultValue != null)
                {
                    f.WriteCodeLine("case \"{0}\":", op.Name);
                    Type t = Type.GetType(op.Type);
                    if (t == typeof (String))
                    {
                        //String types should be surrounded with quotes, we'll do it here so
                        //they don't need to be escaped in the XML
                        f.WriteCodeLine("\treturn \"{0}\";", op.DefaultValue);
                    }
                    else if (NetCoreHelpers.IsEnum(t))
                    {
                        //Enums should be qualified with their type name first. SFC needs the DefaultValue to just be the
                        //name of the member (without the type name) so we'll add it on manually here instead of in cfg.xml
                        f.WriteCodeLine("\treturn {0}.{1};", t.Name, op.DefaultValue);
                    }
                    else
                    {
                        //All other types just write it out as is
                        f.WriteCodeLine("\treturn {0};", op.DefaultValue);
                    }
                }
            }
            f.WriteCodeLine("default:");
            f.WriteCodeLine("\treturn base.GetPropertyDefaultValue(propname);");
            f.DecrementIndent();
            f.DecrementIndent();
        }
    }

   static bool GenerateCodeProps(CodeWriter f)
    {
        List<string> readOnlyAfterCreationProperties = new List<string>();

        if(!GenerateSdkAttribution(f, readOnlyAfterCreationProperties, DatabaseEngineType.Standalone, DatabaseEngineEdition.Unknown))
        {
            return false;
        }
        if (!GenerateSdkAttribution(f, readOnlyAfterCreationProperties, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.Unknown))
        {
            return false;
        }
        if (!GenerateSdkAttribution(f, readOnlyAfterCreationProperties, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse))
        {
            return false;
        }

        Console.WriteLine();
        if (readOnlyAfterCreationProperties.Count != 0)
        {

            string propertiesString = string.Empty;
            foreach (string str in readOnlyAfterCreationProperties)
            {

                propertiesString += "\"" + str + "\", ";
            }

            propertiesString = propertiesString.Trim(", ".ToCharArray());

            f.WriteCodeLine("internal override string[] GetNonAlterableProperties()");
            f.IncrementIndent();

            f.WriteCodeLine("return new string[] { " + propertiesString + " };");

            f.DecrementIndent();
        }

        return true;
    }



    static bool GenerateSdkAttribution(CodeWriter f, List<string> readOnlyAfterCreationProperties, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
    {

        SortedList<string, ObjectPropertyEx> objectProperties = null;
        if (databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
        {
            if (databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
            {
                //Datawarehouse has its own separate properties
                objectProperties = listSqlDwProperties;
            }
            else
            {
                //Not datawarehouse, default to normal cloud properties
                objectProperties = listCloudProperties;
            }

        }
        else
        {
            objectProperties = listSingletonProperties;
        }

        foreach (ObjectPropertyEx op in objectProperties.Values)
        {
            // a property can only be defined once in the class definition
            // for properties that are supported on box and cloud, use the box property
            // for a properties that are supported for box and SQL DW, use box, for cloud and SQL DW, use cloud
            if (databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                if (
                    (databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse &&
                        (listSingletonProperties.ContainsKey(op.Name) || listCloudProperties.ContainsKey(op.Name)))
                    || listSingletonProperties.ContainsKey(op.Name))
                {
                    continue;
                }
            }
            if (!op.generate)
            {
                if (op.ReadOnlyAfterCreation)
                {
                    readOnlyAfterCreationProperties.Add(op.Name);
                }
                continue;
            }

            //SfcProperty is default set on properties; suppressSfcAttribute is true
            if(!op.suppressSfcAttribute)
            {
                 //write get

                 if (op.KeyIndex != -1)
                 {
                     f.WriteCodeLine(string.Format(CultureInfo.InvariantCulture, "[SfcKey({0})]", op.KeyIndex.ToString(CultureInfo.InvariantCulture)));
                 }

                 if (op.dmfIgnore)
                 {
                     f.WriteCodeLine(string.Format(CultureInfo.InvariantCulture, "[DmfIgnoreProperty]"));
                 }

                 string sfcPropertyFlag = string.Empty;
                 if (op.Expensive)
                 {
                     sfcPropertyFlag = "SfcPropertyFlags.Expensive |";
                 }

                 if (op.ReadOnlyAfterCreation)
                 {
                     sfcPropertyFlag += "SfcPropertyFlags.ReadOnlyAfterCreation |";
                     readOnlyAfterCreationProperties.Add(op.Name);
                 }
                 if ((op.PropertyMode & PropertyMode.Design) == PropertyMode.Design)
                 {
                     sfcPropertyFlag += "SfcPropertyFlags.Design |";
                 }
                 if ((op.PropertyMode & PropertyMode.Deploy) == PropertyMode.Deploy)
                 {
                     sfcPropertyFlag += "SfcPropertyFlags.Deploy |";
                 }

                 if (listCloudProperties.ContainsKey(op.Name))
                 {
                     sfcPropertyFlag += "SfcPropertyFlags.SqlAzureDatabase |";
                 }
                 if (listSingletonProperties.ContainsKey(op.Name))
                 {
                     sfcPropertyFlag += "SfcPropertyFlags.Standalone |";
                 }

                 sfcPropertyFlag = sfcPropertyFlag.Trim(" |".ToCharArray());

                 if (string.IsNullOrEmpty(sfcPropertyFlag) && string.IsNullOrEmpty(op.DefaultValue))
                 {
                     // No flags, no default value
                     f.WriteCodeLine("[SfcProperty]");
                 }
                 else
                 {

                     if (string.IsNullOrEmpty(op.DefaultValue))
                     {
                         f.WriteCodeLine(string.Format(CultureInfo.InvariantCulture, "[SfcProperty({0})]", sfcPropertyFlag));
                     }
                     else
                     {
                         // Default value specified

                         if (string.IsNullOrEmpty(sfcPropertyFlag))
                         {
                             // If we have no flags specified, assume default flags, which is 'Required'
                             sfcPropertyFlag = "SfcPropertyFlags.Required";
                         }

                         f.WriteCodeLine(string.Format(CultureInfo.InvariantCulture, "[SfcProperty({0}, \"{1}\")]", sfcPropertyFlag, op.DefaultValue));
                     }
                 }
                 if (!GenerateSfcReferenceCode(f, op))
                 {
                     return false;
                 }

                 WriteGetAndSetPropertyBody(f, op);
            }
            else
            {
                 WriteGetAndSetPropertyBody(f, op);
            }
        }
	return true;
    }

    static void WriteGetAndSetPropertyBody(CodeWriter f, ObjectPropertyEx op)
    {
         f.WriteCodeLine("public {0} {1}", op.Type, op.Name);
         f.IncrementIndent();

         f.WriteCodeLine("get");
         f.IncrementIndent();
         f.WriteCodeLine("return ({0})this.Properties.GetValueWithNullReplacement(\"{1}\");", op.Type, op.Name);
         f.DecrementIndent();
         //write set
         if (!op.ReadOnly)
         {
              f.WriteCodeLine("set");
              f.IncrementIndent();
              f.WriteCodeLine("Properties.SetValueWithConsistencyCheck(\"{0}\", value);", op.Name);
              f.DecrementIndent();
         }

         f.DecrementIndent();
    }

    static bool GenerateSfcReferenceCode(CodeWriter f, ObjectPropertyEx op)
    {
        // See if there is a reference attribute specified.
        if (!string.IsNullOrEmpty(op.ReferenceTemplate))
        {
            string[] referenceTemplates = op.ReferenceTemplate.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string[] referenceKeys = null;

            if (!string.IsNullOrEmpty(op.ReferenceKeys))
            {
                referenceKeys = op.ReferenceKeys.Split(new char[] { ';' }, StringSplitOptions.None);
            }

            string[] referenceTemplateParameters = null;
            if (!string.IsNullOrEmpty(op.ReferenceTemplateParameters))
            {
                referenceTemplateParameters = op.ReferenceTemplateParameters.Split(new char[] { ';' }, StringSplitOptions.None);
            }
            string[] referenceTypes = op.ReferenceType.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if ((referenceTemplateParameters != null && referenceTemplates.Length != referenceTemplateParameters.Length)
                || referenceTemplates.Length != referenceTypes.Length
                || (referenceKeys != null && referenceTemplates.Length != referenceKeys.Length))
            {
                EmitError("The number of reference template, reference type, reference template parameters and reference keys don't match.");
                return false;
            }

            int refCount = 0;
            foreach (string refTemplate in referenceTemplates)
            {
                string refKeys = null;
                string refTemplateParameters = null;
                if (referenceKeys != null)
                {
                    refKeys = referenceKeys[refCount];
                }
                if (referenceTemplateParameters != null)
                {
                    refTemplateParameters = referenceTemplateParameters[refCount];
                }
                string refType = referenceTypes[refCount];

                if (!string.IsNullOrEmpty(refTemplate))
                {
                    string[] keys = null;

                    // Keys are optional. These are only provided in a multi-key reference
                    if (!string.IsNullOrEmpty(refKeys))
                    {
                        keys = refKeys.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (keys.Length == 0)
                        {
                            EmitError("reference_keys incorrectly specified. No keys found");
                            return false;
                        }
                    }

                    // If this is a customer resolver the template has "resolver:" prefixed following with a type name

                    bool isCustomResolver = false;
                    string customResolverType = null;
                    if (refTemplate.StartsWith("resolver:",StringComparison.Ordinal))
                    {
                        isCustomResolver = true;
                        customResolverType = refTemplate.Substring(9);
                    }

                    string[] parameters = null;
                    if (!string.IsNullOrEmpty(refTemplateParameters))
                    {
                        parameters = refTemplateParameters.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    if (!isCustomResolver && (parameters == null || parameters.Length == 0))
                    {
                        EmitError("reference_template_parameters incorrectly specified. No parameters found");
                        return false;
                    }

                    if (string.IsNullOrEmpty(refType))
                    {
                        EmitError("reference_type incorrectly specified.");
                        return false;
                    }

                    StringBuilder sb = new StringBuilder();

                    // Format example of single key reference: [SfcReference(typeof(Login), "Server/Login[@Name = '{0}']", "Owner")]

                    sb.Append("[SfcReference(typeof(");
                    sb.Append(refType);
                    sb.Append("),");

                    if (keys != null)
                    {
                        // This is a multi-key reference (the only difference are the keys)
                        // Format example: [SfcReference(typeof(Table), new string[] { "ReferencingTableName", "ReferencingTableSchema" },
                        //              "Server/Database[@Name = '{0}']/Table[@Name = '{1}' AND @Schema = '{2}']",
                        //              "Parent.Name", "ReferencingTableName", "ReferencingTableSchema")]

                        sb.Append("new string[] {");
                        for (int i = 0; i < keys.Length; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append(",");
                            }
                            sb.Append("\"");
                            sb.Append(keys[i]);
                            sb.Append("\"");
                        }
                        sb.Append("},");
                    }

                    if (isCustomResolver)
                    {
                        sb.Append("typeof(");
                        sb.Append(customResolverType);
                        sb.Append("),");

                        sb.Append("\"");
                        sb.Append("Resolve"); // The resolver method
                        sb.Append("\"");
                    }
                    else
                    {
                        // Add the template
                        sb.Append("\"");
                        sb.Append(refTemplate);
                        sb.Append("\"");
                    }

                    // add the parameters
                    if (parameters != null && parameters.Length >= 0)
                    {
                        sb.Append(",");
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append(",");
                            }
                            sb.Append("\"");
                            sb.Append(parameters[i]);
                            sb.Append("\"");
                        }
                    }
                    sb.Append(")]");
                    f.WriteCodeLine(sb.ToString());
                    refCount++;
                }
            }

            if (refCount > 0)
            {
                // The SfcReference attribute is not CLS compliant. This prevents the CS3016 warning code.
                f.WriteCodeLine("[CLSCompliant(false)]");
            }
        }
        return true;
    }
    class Attribute
    {
        public string name;
        public string val;
        public string type;
    }

    static Attribute GetStoreAtt(SortedList list, string name)
    {
        return (Attribute)list[name];
    }

    static SortedList LoadGenAttrib(string atrib)
    {
        SortedList list = null;

        XmlNode node = m_gen.DocumentElement.SelectNodes("attributes[@id='" + atrib + "']")[0];
        if (null != GetAttribute("load_id", node, null))
        {
            list = LoadGenAttrib(GetAttribute("load_id", node, null));
        }
        else
        {
            list = new SortedList();
        }
        foreach (XmlNode n in node.ChildNodes)
        {
            if ("alias" == n.LocalName)
            {
                Attribute a = GetStoreAtt(list, GetAttribute("from", n, null));
                a.val = GetAttribute("v", n, null);
                a.type = GetAttribute("t", n, null);
                a.name = GetAttribute("to", n, null);
            }
            else
            {
                Attribute a = new Attribute();
                a.val = GetAttribute("v", n, null);
                a.type = GetAttribute("t", n, null);
                a.name = GetAttribute("n", n, null);
                list[a.name] = a;
            }
        }
        return list;
    }

    static string MakeListToString(StringCollection sc, string delim)
    {
        string res = "";
        bool bFirst = true;
        foreach (string s in sc)
        {
            if (bFirst)
            {
                bFirst = false;
            }
            else
            {
                res += delim;
            }
            res += s;
        }
        return res;
    }

    static void GenerateBody(CodeWriter f, string genInfo, bool generateInnerBody)
    {
        string[] scGen = genInfo.Split(new char[] { ';' });
        foreach (string s in scGen)
        {
            GenerateBodyInstance(f, s, generateInnerBody);
        }
    }

    static void GenerateBodyInstance(CodeWriter f, string genInfo, bool generateInnerBody)
    {
        string[] scGen = genInfo.Split(new char[] { ',' });

        SortedList list = LoadGenAttrib(scGen[0]);

        XmlNodeList listNoGen = m_gen.DocumentElement.SelectNodes(
                                        "body[@id='" + scGen[1] +
                                        "' and @generate_outside_class='" +
                                        generateInnerBody.ToString().ToLower() +
                                        "']/*");

        foreach (XmlNode n in listNoGen)
        {
            switch (n.LocalName)
            {
                case "t":
                    {
                        string s = GetAttribute("v", n, null);
                        if (null == s)
                        {
                            s = n.InnerText;
                        }
                        s = s.Replace("[NL]", "\r\n\t\t");
                        s = s.Replace("[T]", "\t");
                        f.Write(s);
                    } break;
                case "p":
                    {
                        StringCollection sc = new StringCollection();
                        foreach (XmlNode a in n.ChildNodes)
                        {
                            if (null != list[GetAttribute("n", a, null)])
                            {
                                string type = ((Attribute)(list[GetAttribute("n", a, null)])).type;
                                if (null != type)
                                {
                                    sc.Add(type + " " + ((Attribute)(list[GetAttribute("n", a, null)])).name);
                                }
                            }
                        }
                        f.Write("(");
                        f.Write(MakeListToString(sc, ", "));
                        f.Write(")");
                    } break;
                case "l":
                    {
                        StringCollection sc = new StringCollection();
                        foreach (XmlNode a in n.ChildNodes)
                        {
                            if (null != list[GetAttribute("n", a, null)])
                            {
                                sc.Add(((Attribute)(list[GetAttribute("n", a, null)])).val);
                            }
                        }
                        f.Write(MakeListToString(sc, GetAttribute("d", n, "")));
                    } break;
            }
        }

        f.WriteLine();
    }

    static private void EmitError(string message, params object[] obj)
    {
        // Add some bells and whistles to make the error show up prominently in the log
        Console.Error.WriteLine();
        Console.Error.WriteLine("***");
        string formattedMessage = string.Format("cfg.xml(0): error {1}: 'SmoCodeGen: {0}'", string.Format(message, obj), m_nRetValue);
        Console.Error.WriteLine(formattedMessage);
        Console.Error.WriteLine("***");
    }


}


