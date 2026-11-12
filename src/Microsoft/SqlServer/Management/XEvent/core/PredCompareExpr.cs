// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// PredCompareExpr class represents expression like sqlserver.database_id=7
    /// </summary>
    public sealed class PredCompareExpr : PredExpr
    {
        /// <summary>
        /// Define what comparator could be used between PredOperand and PredValue
        /// </summary>
        public enum ComparatorType
        {
            /// <summary>
            /// =
            /// </summary>
            EQ,
            /// <summary>
            /// &lt;&gt;
            /// </summary>
            NE,
            /// <summary>
            /// &gt;
            /// </summary>
            GT,
            /// <summary>
            /// &gt;=
            /// </summary>
            GE,
            /// <summary>
            /// &lt;
            /// </summary>
            LT,
            /// <summary>
            /// &lt;=
            /// </summary>
            LE
        }

        private ComparatorType  type;
        private PredOperand operand = null;
        private PredValue value = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredCompareExpr"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="operand">The operand.</param>
        /// <param name="value">The value.</param>
        public PredCompareExpr(ComparatorType type, PredOperand operand, PredValue value)
        {
            if (operand == null)
            {
                throw new ArgumentNullException("operand");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            this.type = type;
            this.operand = operand;
            this.value = value;
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
                return this.value; 
            }
        }

        /// <summary>
        /// Get the compare operator.
        /// </summary>
        public ComparatorType Operator
        {
            get 
            { 
                return this.type; 
            }
        }
    }
}
