// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;


namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// PredLogicalExpr class can apply NOT on one PredExpr or AND/OR on two PredExprs
    /// </summary>
    public sealed class PredLogicalExpr : PredExpr
    {
        /// <summary>
        /// Define logical operator that could be used in PredLogicalExpr
        /// </summary>
        public enum LogicalOperatorType
        {
            /// <summary>
            /// Not
            /// </summary>
            Not,
            /// <summary>
            /// Logical and
            /// </summary>
            And,
            /// <summary>
            /// Logical or
            /// </summary>
            Or
        }

        private readonly LogicalOperatorType  type;
        private readonly PredExpr leftPredExpr;
        private readonly PredExpr rightPredExpr;


        /// <summary>
        /// Initializes a new instance of the <see cref="PredLogicalExpr"/> class.
        /// </summary>
        /// <param name="type">The logical operator that will be applied on predExpr1 and predExpr2.</param>
        /// <param name="predExpr1">PredExpr1.</param>
        /// <param name="predExpr2">PredExpr2.</param>
        public PredLogicalExpr(LogicalOperatorType type, PredExpr predExpr1, PredExpr predExpr2)
        {
            if (predExpr1 == null)
            {
                throw new ArgumentNullException("predExpr1");
            }
            if (type == LogicalOperatorType.Not)
            {
                if (predExpr2 != null)
                {
                    throw new XEventException(ExceptionTemplates.Expression2NotNull);
                }
            }
            else if (predExpr2 == null)
            {
                throw new ArgumentNullException("predExpr2");
            }

            this.type = type;
            this.leftPredExpr = predExpr1;
            this.rightPredExpr = predExpr2;
        }

        
        /// <summary>
        /// Get the left sub-expression.
        /// </summary>
        public PredExpr LeftExpr
        {
            get 
            { 
                return this.leftPredExpr; 
            }
        }

        /// <summary>
        /// Get the right sub-expression.
        /// </summary>
        public PredExpr RightExpr
        {
            get 
            { 
                return this.rightPredExpr; 
            }
        }

        /// <summary>
        /// Get the logic operator.
        /// </summary>
        public LogicalOperatorType Operator
        {
            get 
            { 
                return this.type; 
            }
        }
    }
}
