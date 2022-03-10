// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// PredValue represents is rvalue in PredCompareExpr or second parameter in PredFunctionExpr
    /// </summary>
    public sealed class PredValue : Predicate
    {
        private object value = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredValue"/> class with a Unicode string.
        /// </summary>
        /// <param name="value">The value.</param>
        public PredValue(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            this.value = value;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return this.value.ToString();
        }
    }
}
