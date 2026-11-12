// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;

    internal class Operand : Microsoft.SqlServer.Management.Sdk.Sfc.AstNode {
        private object _var;
        private String _prefix = String.Empty;
        private RType _type;

        internal Operand(String var) {
            _var = var;
            _type = RType.String;
        }

        internal Operand(double var) {
            _var = var;
            _type = RType.Number;
        }

        internal Operand(bool var) {
            _var = var;
            _type = RType.Boolean;
        }

        internal Operand(String var, String prefix) 
        {
            _var = var;
            _prefix = prefix;
            _type = RType.Variable;
        }

        internal override QueryType TypeOfAst {
            get {return QueryType.ConstantOperand;}
        }

        internal override RType ReturnType {
            get {return _type;}
        }

        internal object OperandValue {
            get {return _var;}
        }
    }
}
