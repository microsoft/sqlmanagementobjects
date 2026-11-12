// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    internal class Filter : AstNode {
        private AstNode _input;
        private AstNode _condition;

        internal Filter( AstNode input, AstNode condition) {
            _input = input;
            _condition = condition;
        }

        internal override QueryType TypeOfAst {
            get {return  QueryType.Filter;}
        }

        internal override RType ReturnType {
            get {return RType.NodeSet;}
        }

        internal AstNode Input {
            get { return _input;}
        }

        internal AstNode Condition {
            get {return _condition;}
        }
    }
}
