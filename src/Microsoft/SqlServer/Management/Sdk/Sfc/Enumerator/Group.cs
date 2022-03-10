// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    internal class Group : AstNode {
        private AstNode _groupNode;

        internal Group(AstNode groupNode) {
            _groupNode = groupNode;
        }
        internal override QueryType TypeOfAst {
            get {return QueryType.Group;}
        }
        internal override RType ReturnType {
            get {return RType.NodeSet;}
        }

        internal AstNode GroupNode {
            get {return _groupNode;}
        }

        internal override double DefaultPriority {
            get {
                //return _groupNode.DefaultPriority;
                return 0;
            }
        }
    }
}


