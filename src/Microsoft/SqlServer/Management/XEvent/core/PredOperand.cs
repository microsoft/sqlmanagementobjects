// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Class for predicate operand. A predicate operand can be an event column or
    /// a pred_source.
    /// </summary>
    public sealed class PredOperand : Predicate
    {
        /// <summary>
        /// name of the event field. 
        /// </summary>
        internal string name = null;
        private readonly Guid typePackageID;
        private readonly string typeName;

        /// <summary>
        /// package has meaningful value only when the operand is a pred_source.
        /// </summary>
        internal Package package = null;

        /// <summary>
        /// Store the DataEventColumnInfo or PredSourceInfo.
        /// </summary>
        internal Object operandObject = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredOperand"/> class with event column.
        /// </summary>
        /// <param name="eventColumn">The event column.</param>
        public PredOperand(DataEventColumnInfo eventColumn)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("PredOperand:Event column"))
            {
                tm.TraceParameterIn("event column", eventColumn);
                if (eventColumn == null)
                {
                    tm.TraceError("event column can not be null.");
                    throw new ArgumentNullException("eventColumn");
                }
                this.name = eventColumn.Name;
                this.typePackageID = eventColumn.TypePackageID;
                this.typeName = eventColumn.TypeName;
                //package is not used when the operand is a event column
                this.package = null;
                this.operandObject = eventColumn;
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="PredOperand"/> class with Pred_source.
        /// </summary>
        /// <param name="sourceInfo">The source info.</param>
        public PredOperand(PredSourceInfo sourceInfo)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("PredOperand:Pred Source"))
            {
                tm.TraceParameterIn("pred source", sourceInfo);
                if (sourceInfo == null)
                {
                    tm.TraceError("pred source can not be null.");
                    throw new ArgumentNullException("sourceInfo");
                }
                this.name = sourceInfo.Name;
                this.typePackageID = sourceInfo.TypePackageID;
                this.typeName = sourceInfo.TypeName;
                this.package = sourceInfo.Parent;
                this.operandObject = sourceInfo;
            }
        }

        /// <summary>
        /// Gets TypeName of the Operand.
        /// </summary>
        public string TypeName
        {
            get { return this.typeName; }
        }
 
        /// <summary>
        /// Gets TypePackageID of the Operand.
        /// </summary>
        public Guid TypePackageId
        {
            get { return this.typePackageID; }
        }

        /// <summary>
        /// Get the object used to construct the operand. The object should be an instance of DataEventColumnInfo or PredSourceInfo.
        /// </summary>
        public Object OperandObject
        {
            get 
            {
                return this.operandObject;
            }
        }


        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current PredOperand.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current PredOperand.
        /// </returns>
        public override string ToString()
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("ToString"))
            {
                if (this.package == null) // event column
                    return SfcTsqlProcFormatter.MakeSqlBracket(this.name);

                PredSourceInfo sourceInfo = this.operandObject as PredSourceInfo;
                if (sourceInfo != null)
                {
                    // if pkgName.objName is not unique
                    if (this.package.Parent.ObjectInfoSet.GetAll<PredSourceInfo>
                                                   (this.package.Name, this.name).Count > 1)
                    {
                        return string.Format(CultureInfo.InvariantCulture, "[{0}].{1}.{2}",
                             this.package.ModuleID, SfcTsqlProcFormatter.MakeSqlBracket(this.package.Name), SfcTsqlProcFormatter.MakeSqlBracket(this.name));
                    }

                    return string.Format(CultureInfo.InvariantCulture, "{0}.{1}",
                         SfcTsqlProcFormatter.MakeSqlBracket(this.package.Name), SfcTsqlProcFormatter.MakeSqlBracket(this.name));
                }

                tm.Assert(false, "unexpected operand object.");
                return null;
            }
        }
    }
}
