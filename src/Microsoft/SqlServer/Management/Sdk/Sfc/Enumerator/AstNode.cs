// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    internal  class   AstNode 
    {
        internal enum QueryType
        {
            Axis            = 0,
            Operator        = 1,
            Filter          = 2,
            ConstantOperand = 3,
            Function        = 4,
            Group           = 5,
            Root            = 6,
            Error           = 7
        };

        internal enum RType {
            Number      = 0,
            String      = 1,
            Boolean     = 2,
            NodeSet     = 3,
            Variable    = 4,
            Any         = 5,
            Error       = 6
        };

        internal virtual  QueryType TypeOfAst {  
            get {return QueryType.Error;}
        }
        
        internal virtual  RType ReturnType {
            get {return RType.Error;}
        }

        internal virtual double DefaultPriority {
            get {return 0.5;}
        }
    }
}
