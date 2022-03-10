// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    internal enum XPathNodeType {
        Root,
        Element,
        Attribute,
#if SupportNamespaces
        Namespace,
#endif
        Text,
        SignificantWhitespace,
        Whitespace,
        ProcessingInstruction,
        Comment,
        All,
    }
}
