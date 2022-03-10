// Copyright (c) Microsoft.
// Licensed under the MIT license.

#if NETSTANDARD2_0
//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    public class XmlTextReader : XmlReader
    {
        //
        // Member fields
        //
        XmlReader impl;
        //
        //
        // Constructors
        //      

        public XmlTextReader(Stream input, XmlReaderSettings setting)
        {
            impl =  XmlReader.Create(input, setting);
        }
        //
        // XmlReader members
        //
        public override XmlNodeType NodeType
        {
            get { return impl.NodeType; }
        }

        public override string Name
        {
            get { return impl.Name; }
        }

        public override string LocalName
        {
            get { return impl.LocalName; }
        }

        public override string NamespaceURI
        {
            get { return impl.NamespaceURI; }
        }

        public override string Prefix
        {
            get { return impl.Prefix; }
        }

        public override bool HasValue
        {
            get { return impl.HasValue; }
        }

        public override string Value
        {
            get { return impl.Value; }
        }

        public override int Depth
        {
            get { return impl.Depth; }
        }

        public string implURI
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsEmptyElement
        {
            get { return impl.IsEmptyElement; }
        }

        public override bool IsDefault
        {
            get { return impl.IsDefault; }
        }    

        public override XmlSpace XmlSpace
        {
            get { return impl.XmlSpace; }
        }

        public override string XmlLang
        {
            get { return impl.XmlLang; }
        }

        public override int AttributeCount { get { return impl.AttributeCount; } }

        public override string GetAttribute(string name)
        {
            return impl.GetAttribute(name);
        }

        public override string GetAttribute(string localName, string namespaceURI)
        {
            return impl.GetAttribute(localName, namespaceURI);
        }

        public override string GetAttribute(int i)
        {
            return impl.GetAttribute(i);
        }

        public override bool MoveToAttribute(string name)
        {
            return impl.MoveToAttribute(name);
        }

        public override bool MoveToAttribute(string localName, string namespaceURI)
        {
            return impl.MoveToAttribute(localName, namespaceURI);
        }

        public override void MoveToAttribute(int i)
        {
            impl.MoveToAttribute(i);
        }

        public override bool MoveToFirstAttribute()
        {
            return impl.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute()
        {
            return impl.MoveToNextAttribute();
        }

        public override bool MoveToElement()
        {
            return impl.MoveToElement();
        }

        public override bool ReadAttributeValue()
        {
            return impl.ReadAttributeValue();
        }

        public override bool Read()
        {
            return impl.Read();
        }

        public override bool EOF
        {
            get { return impl.EOF; }
        }

        public override void Close()
        {
            impl.Dispose();
        }

        public override ReadState ReadState
        {
            get { return impl.ReadState; }
        }

        public override void Skip()
        {
            impl.Skip();
        }

        public override XmlNameTable NameTable
        {
            get { return impl.NameTable; }
        }

        public override String LookupNamespace(String prefix)
        {
            string ns = impl.LookupNamespace(prefix);
            if (ns != null && ns.Length == 0)
            {
                ns = null;
            }
            return ns;
        }

        public override bool CanResolveEntity
        {
            get { return true; }
        }

        public override void ResolveEntity()
        {
            impl.ResolveEntity();
        }

        // Binary content access methods
        public override bool CanReadBinaryContent
        {
            get { return true; }
        }

        public int ReadContentAsimpl64(byte[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public int ReadElementContentAsimpl64(byte[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            return impl.ReadContentAsBinHex(buffer, index, count);
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            return impl.ReadElementContentAsBinHex(buffer, index, count);
        }

        // Text streaming methods

        // XmlTextReader does do support streaming of Value (there are backwards compatibility issues when enabled)
        public override bool CanReadValueChunk
        {
            get { return false; }
        }      

        //
        // IXmlLineInfo members
        //
        public bool HasLineInfo() { return true; }

        [Obsolete("Use DtdProcessing property instead.")]
        public bool ProhibitDtd
        {
            get { return impl.Settings.DtdProcessing == DtdProcessing.Prohibit; }
            set { impl.Settings.DtdProcessing = value ? DtdProcessing.Prohibit : DtdProcessing.Ignore; }
        }

        public DtdProcessing DtdProcessing
        {
            get { return impl.Settings.DtdProcessing; }
            set { impl.Settings.DtdProcessing = value; }
        }

        public override string this[string name]
        {
            get { return impl[name]; }
        }

        public override string this[string name, string namespaceURI]
        {
            get { return impl[name, namespaceURI]; }
        }

        public override string this[int i]
        {
            get { return impl[i]; }
        }

        protected override void Dispose(bool disposing)
        {
            impl.Dispose();
        }

        public override bool Equals(object obj)
        {
            return impl.Equals(obj);
        }

        public override int GetHashCode()
        {
            return impl.GetHashCode();
        }

        public override Task<string> GetValueAsync()
        {
            return impl.GetValueAsync();
        }

        public override bool HasAttributes
        {
            get
            {
                return impl.HasAttributes;
            }
        }

        public override bool IsStartElement()
        {
            return impl.IsStartElement();
        }

        public override bool IsStartElement(string localname, string ns)
        {
            return impl.IsStartElement(localname, ns);
        }

        public override bool IsStartElement(string name)
        {
            return impl.IsStartElement(name);
        }

        public override XmlNodeType MoveToContent()
        {
            return impl.MoveToContent();
        }

        public override Task<XmlNodeType> MoveToContentAsync()
        {
            return impl.MoveToContentAsync();
        }

        public override Task<bool> ReadAsync()
        {
            return impl.ReadAsync();
        }

        public override object ReadContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            return impl.ReadContentAs(returnType, namespaceResolver);
        }

        public override Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            return impl.ReadContentAsAsync(returnType, namespaceResolver);
        }

        public Task<int> ReadContentAsimpl64Async(byte[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            return impl.ReadContentAsBinHexAsync(buffer, index, count);
        }

        public override bool ReadContentAsBoolean()
        {
            return impl.ReadContentAsBoolean();
        }

        public override DateTimeOffset ReadContentAsDateTimeOffset()
        {
            return impl.ReadContentAsDateTimeOffset();
        }

        public override decimal ReadContentAsDecimal()
        {
            return impl.ReadContentAsDecimal();
        }

        public override double ReadContentAsDouble()
        {
            return impl.ReadContentAsDouble();
        }

        public override float ReadContentAsFloat()
        {
            return impl.ReadContentAsFloat();
        }

        public override int ReadContentAsInt()
        {
            return impl.ReadContentAsInt();
        }

        public override long ReadContentAsLong()
        {
            return impl.ReadContentAsLong();
        }

        public override object ReadContentAsObject()
        {
            return impl.ReadContentAsObject();
        }

        public override Task<object> ReadContentAsObjectAsync()
        {
            return impl.ReadContentAsObjectAsync();
        }

        public override string ReadContentAsString()
        {
            return impl.ReadContentAsString();
        }

        public override Task<string> ReadContentAsStringAsync()
        {
            return impl.ReadContentAsStringAsync();
        }

        public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            return impl.ReadElementContentAs(returnType, namespaceResolver);
        }

        public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI)
        {
            return impl.ReadElementContentAs(returnType, namespaceResolver, localName, namespaceURI);
        }

        public override Task<object> ReadElementContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            return impl.ReadElementContentAsAsync(returnType, namespaceResolver);
        }

        public Task<int> ReadElementContentAsimpl64Async(byte[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            return impl.ReadElementContentAsBinHexAsync(buffer, index, count);
        }

        public override bool ReadElementContentAsBoolean()
        {
            return impl.ReadElementContentAsBoolean();
        }

        public override bool ReadElementContentAsBoolean(string localName, string namespaceURI)
        {
            return impl.ReadElementContentAsBoolean(localName, namespaceURI);
        }

        public override decimal ReadElementContentAsDecimal()
        {
            return impl.ReadElementContentAsDecimal();
        }

        public override decimal ReadElementContentAsDecimal(string localName, string namespaceURI)
        {
            return impl.ReadElementContentAsDecimal(localName, namespaceURI);
        }

        public override double ReadElementContentAsDouble()
        {
            return impl.ReadElementContentAsDouble();
        }

        public override double ReadElementContentAsDouble(string localName, string namespaceURI)
        {
            return impl.ReadElementContentAsDouble(localName, namespaceURI);
        }

        public override float ReadElementContentAsFloat()
        {
            return impl.ReadElementContentAsFloat();
        }

        public override float ReadElementContentAsFloat(string localName, string namespaceURI)
        {
            return impl.ReadElementContentAsFloat(localName, namespaceURI);
        }

        public override int ReadElementContentAsInt()
        {
            return impl.ReadElementContentAsInt();
        }

        public override int ReadElementContentAsInt(string localName, string namespaceURI)
        {
            return impl.ReadElementContentAsInt(localName, namespaceURI);
        }

        public override long ReadElementContentAsLong()
        {
            return impl.ReadElementContentAsLong();
        }

        public override long ReadElementContentAsLong(string localName, string namespaceURI)
        {
            return impl.ReadElementContentAsLong(localName, namespaceURI);
        }

        public override object ReadElementContentAsObject()
        {
            return impl.ReadElementContentAsObject();
        }

        public override object ReadElementContentAsObject(string localName, string namespaceURI)
        {
            return impl.ReadElementContentAsObject(localName, namespaceURI);
        }

        public override Task<object> ReadElementContentAsObjectAsync()
        {
            return impl.ReadElementContentAsObjectAsync();
        }

        public override string ReadElementContentAsString()
        {
            return impl.ReadElementContentAsString();
        }

        public override string ReadElementContentAsString(string localName, string namespaceURI)
        {
            return impl.ReadElementContentAsString(localName, namespaceURI);
        }

        public override Task<string> ReadElementContentAsStringAsync()
        {
            return impl.ReadElementContentAsStringAsync();
        }

        public override void ReadEndElement()
        {
            impl.ReadEndElement();
        }

        public override string ReadInnerXml()
        {
            return impl.ReadInnerXml();
        }

        public override Task<string> ReadInnerXmlAsync()
        {
            return impl.ReadInnerXmlAsync();
        }

        public override string ReadOuterXml()
        {
            return impl.ReadOuterXml();
        }

        public override Task<string> ReadOuterXmlAsync()
        {
            return impl.ReadOuterXmlAsync();
        }

        public override void ReadStartElement()
        {
            impl.ReadStartElement();
        }

        public override void ReadStartElement(string localname, string ns)
        {
            impl.ReadStartElement(localname, ns);
        }

        public override void ReadStartElement(string name)
        {
            impl.ReadStartElement(name);
        }

        public override XmlReader ReadSubtree()
        {
            return impl.ReadSubtree();
        }

        public override bool ReadToDescendant(string localName, string namespaceURI)
        {
            return impl.ReadToDescendant(localName, namespaceURI);
        }

        public override bool ReadToDescendant(string name)
        {
            return impl.ReadToDescendant(name);
        }

        public override bool ReadToFollowing(string localName, string namespaceURI)
        {
            return impl.ReadToFollowing(localName, namespaceURI);
        }

        public override bool ReadToFollowing(string name)
        {
            return impl.ReadToFollowing(name);
        }

        public override bool ReadToNextSibling(string localName, string namespaceURI)
        {
            return impl.ReadToNextSibling(localName, namespaceURI);
        }

        public override bool ReadToNextSibling(string name)
        {
            return impl.ReadToNextSibling(name);
        }

        public override int ReadValueChunk(char[] buffer, int index, int count)
        {
            return impl.ReadValueChunk(buffer, index, count);
        }

        public override Task<int> ReadValueChunkAsync(char[] buffer, int index, int count)
        {
            return impl.ReadValueChunkAsync(buffer, index, count);
        }

        public override XmlReaderSettings Settings
        {
            get
            {
                return impl.Settings;
            }
        }

        public override Task SkipAsync()
        {
            return impl.SkipAsync();
        }

        public override string ToString()
        {
            return impl.ToString();
        }

        public override Type ValueType
        {
            get
            {
                return impl.ValueType;
            }
        }

        public override string BaseURI
        {
            get
            {
                return impl.BaseURI;
            }
        }
    }
}
#endif