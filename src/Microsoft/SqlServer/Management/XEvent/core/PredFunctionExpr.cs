// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;


namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Class for the function expression in a predicate. A function expression is like this:
    /// Pred_Compare(operand, value) where operand is an instance of PredOperand and value is an
    /// instance of PredValue.
    /// </summary>
    public sealed class PredFunctionExpr : PredExpr
    {
        private PredOperand operand = null;
        private PredValue opvalue = null;
        private PredCompareInfo func = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredFunctionExpr"/> class.
        /// </summary>
        /// <param name="func">The PredCompareInfo object represent the function name.</param>
        /// <param name="operand">The operand.</param>
        /// <param name="value">The value.</param>
        public PredFunctionExpr(PredCompareInfo func, PredOperand operand, PredValue value)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Constructor"))
            {
                tm.TraceParameterIn("func", func);
                tm.TraceParameterIn("operand", operand);
                tm.TraceParameterIn("value", value);
                CheckNotNull(tm, func, "func");
                CheckNotNull(tm, operand, "operand");
                CheckNotNull(tm, value, "value");
                this.operand = operand;
                this.func = func;
                this.opvalue = value;
            }
        }

     
        /// <summary>
        /// Helper function to check if a parameter is null.
        /// </summary>
        /// <param name="tm">The Trace context.</param>
        /// <param name="param">The parameter.</param>
        /// <param name="paramName">Name of the parameter.</param>
        private void CheckNotNull(MethodTraceContext tm, object param, string paramName)
        {
            if (null == param)
            {
                tm.TraceError(String.Format(CultureInfo.InvariantCulture, "{0} should not be null", paramName));
                throw new ArgumentNullException(paramName, "should not be null.");
            }
        }

        /// <summary>
        /// Get the operand.
        /// </summary>
        public PredOperand Operand
        {
            get 
            { 
                return this.operand; 
            }
        }

        /// <summary>
        /// Get the value.
        /// </summary>
        public PredValue Value
        {
            get 
            {
                return this.opvalue; 
            }
        }

        /// <summary>
        /// Get the pred_Compare instance.
        /// </summary>
        public PredCompareInfo Operator
        {
            get
            {
                return this.func; 
            }
        }
    }
}
