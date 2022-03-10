// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System.Collections;

    internal class FilterTranslate
	{
		static FilterNodeOperator.Type XPathOpToFilterOp(Operator.Op op)
		{
			switch(op)
			{
				case Operator.Op.LT: return FilterNodeOperator.Type.LT;
				case Operator.Op.GT: return FilterNodeOperator.Type.GT;
				case Operator.Op.LE: return FilterNodeOperator.Type.LE;
				case Operator.Op.GE: return FilterNodeOperator.Type.GE;
				case Operator.Op.EQ: return FilterNodeOperator.Type.EQ;
				case Operator.Op.NE: return FilterNodeOperator.Type.NE;
				case Operator.Op.OR: return FilterNodeOperator.Type.OR;
				case Operator.Op.AND:return FilterNodeOperator.Type.And;
                case Operator.Op.NEGATE: return FilterNodeOperator.Type.NEG;
				default: throw new InvalidQueryExpressionEnumeratorException(SfcStrings.UnknownOperator);
			}
		}

		static FilterNodeFunction.Type XPathFuncToFilterFunction(Function.FunctionType tfunc)
		{
			switch(tfunc)
			{
				case Function.FunctionType.FuncTrue: return FilterNodeFunction.Type.True;
				case Function.FunctionType.FuncFalse: return FilterNodeFunction.Type.False;
				case Function.FunctionType.FuncString: return FilterNodeFunction.Type.String;
				case Function.FunctionType.FuncContains: return FilterNodeFunction.Type.Contains;
				case Function.FunctionType.FuncNot: return FilterNodeFunction.Type.Not;
				case Function.FunctionType.FuncBoolean: return FilterNodeFunction.Type.Boolean;
                case Function.FunctionType.FuncLike: return FilterNodeFunction.Type.Like;
                case Function.FunctionType.FuncIn: return FilterNodeFunction.Type.In;
                case Function.FunctionType.FuncUserDefined: return FilterNodeFunction.Type.UserDefined;
				default: throw new InvalidQueryExpressionEnumeratorException(SfcStrings.UnknownFunction);
			}
		}
	
		public static FilterNode decode(AstNode node)
		{
			if( null == node )
            {
                return null;
            }

            switch (node.TypeOfAst)
			{
				case AstNode.QueryType.Filter:
					Filter ft = (Filter)node;
					return decode(ft.Condition);

				case AstNode.QueryType.Operator:
					Operator op = (Operator)node;
					FilterNodeOperator fno = new FilterNodeOperator(XPathOpToFilterOp(op.OperatorType));
					fno.Add(decode(op.Operand1));
					fno.Add(decode(op.Operand2));
					return fno;

				case AstNode.QueryType.ConstantOperand:
					Operand opd = (Operand)node;
					if( Operand.RType.Number == opd.ReturnType )
                    {
                        return new FilterNodeConstant(opd.OperandValue, FilterNodeConstant.ObjectType.Number);
                    }
                    else if( Operand.RType.String == opd.ReturnType )
                    {
                        return new FilterNodeConstant(opd.OperandValue, FilterNodeConstant.ObjectType.String);
                    }
                    else if( Operand.RType.Boolean == opd.ReturnType )
                    {
                        return new FilterNodeConstant(opd.OperandValue, FilterNodeConstant.ObjectType.Boolean);
                    }
                    else
                    {
                        throw new InvalidQueryExpressionEnumeratorException(SfcStrings.VariablesNotSupported);
                    }

                case AstNode.QueryType.Group:
					Group gp = (Group)node;
					FilterNodeGroup fng = new FilterNodeGroup();
					fng.Add(decode(gp.GroupNode));
					return fng;

				case AstNode.QueryType.Axis:
					return decode((Axis)node);

				case AstNode.QueryType.Function:
					return decode((Function)node);

				default:
					throw new InvalidQueryExpressionEnumeratorException(SfcStrings.UnknownElemType);
			}
		}

		static FilterNode decode(Axis ax)
		{
			if( Axis.AxisType.Attribute == ax.TypeOfAxis )
			{
				return new FilterNodeAttribute(ax.Name);
			}
			else if( Axis.AxisType.Child == ax.TypeOfAxis )
			{
				throw new InvalidQueryExpressionEnumeratorException(SfcStrings.ChildrenNotSupported);
			}
			else
            {
                throw new InvalidQueryExpressionEnumeratorException(SfcStrings.UnsupportedExpresion);
            }
        }

		static FilterNode decode(Function func)
		{
			FilterNodeFunction fnf = new FilterNodeFunction(XPathFuncToFilterFunction(func.TypeOfFunction), func.Name);
			ArrayList arList = func.ArgumentList;
			for(int i = 0; i < arList.Count; i++)
            {
                fnf.Add(decode((AstNode)arList[i]));
            }

            return fnf;
		}
	}
}	
