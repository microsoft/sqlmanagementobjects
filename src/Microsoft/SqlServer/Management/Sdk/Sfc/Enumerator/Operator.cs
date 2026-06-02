// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    internal class Operator : AstNode {
        internal enum Op {
            PLUS = 1,
            MINUS = 2,
            MUL  = 3 ,
            MOD = 4,
            DIV = 5,
            NEGATE = 6,
            LT = 7,
            GT = 8,
            LE = 9,
            GE = 10,
            EQ = 11,
            NE = 12,
            OR = 13,
            AND = 14,
            UNION = 15,
            INVALID
        };

        private String[] str = {
            "+",
            "-",
            "multiply",
            "mod",
            "divde",
            "negate",
            "<",
            ">",
            "<=",
            ">=",
            "=",
            "!=", 
            "or",
            "and",
            "union"
        };

        private Op _operatorType;
        private AstNode _opnd1;
        private AstNode _opnd2;

        internal Operator(Op op, AstNode opnd1, AstNode opnd2) {
            _operatorType = op;    
            _opnd1 = opnd1;
            _opnd2 = opnd2;
        }

        internal override QueryType TypeOfAst {
            get {return  QueryType.Operator;}
        }

        internal override RType ReturnType {
            get {
                if (_operatorType < Op.LT)
                {
                    return RType.Number;
                }

                if (_operatorType < Op.UNION)
                {
                    return RType.Boolean;
                }

                return RType.NodeSet;
            }
        }

        internal Op OperatorType {
            get { return _operatorType;}
        }

        internal AstNode Operand1
        {
            get {return _opnd1;}
        }

        internal AstNode Operand2
        {
            get {return _opnd2;}
        }

        internal override double DefaultPriority {
            get {
                if (_operatorType == Op.UNION) {
                    double pri1 = _opnd1.DefaultPriority;
                    double pri2 = _opnd2.DefaultPriority;

                    if (pri1 > pri2)
                    {
                        return pri1;
                    }

                    return pri2;
                }
                else
                {
                    return 0.5;
                }
            }

        }


    }
}
