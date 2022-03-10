// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    internal class Root : AstNode {
        internal Root() {
        }

        internal override QueryType TypeOfAst {
            get {return QueryType.Root;}
        }

        internal override RType ReturnType {
            get {return RType.NodeSet;}
        }
    }
}
