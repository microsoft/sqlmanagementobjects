// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using Microsoft.SqlServer.Management.Sdk.Sfc;
namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliant(false)]
    public class PlanGuideExtender : SmoObjectExtender<PlanGuide>, ISfcValidate
    {
        StringCollection schemaNames;

        public PlanGuideExtender() : base() { }

        public PlanGuideExtender(PlanGuide planGuide) : base(planGuide) { }
     
        [ExtendedPropertyAttribute()]
        public string Name
        {
            get
            {
                return this.Parent.Name;
            }
            set
            {
                this.Parent.Name = value;
            }
        }

        [ExtendedPropertyAttribute()]
        public StringCollection SchemaNames
        {
            get
            {
                if (this.schemaNames == null)
                {
                    this.schemaNames = new StringCollection();
                    Database db = this.Parent.Parent;
                    if (db != null)
                    {
                        DataTable dt = db.EnumObjects(DatabaseObjectTypes.Schema);
                        foreach (DataRow dr in dt.Rows)
                        {
                            this.schemaNames.Add(dr["Name"].ToString());
                        }
                    }
                }
                return this.schemaNames;
            }
        }

        #region ISfcValidate Members

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            return this.Parent.Validate(methodName, arguments);
        }

        #endregion
    }
}
