// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public class ScriptSchemaObjectBase : ScriptNameObjectBase
    {
        internal ScriptSchemaObjectBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState eState) :
            base(parentColl, key, eState)
        {
        }

        internal protected ScriptSchemaObjectBase() : base() { }

        private string m_sScriptSchema = String.Empty;
        internal virtual string ScriptSchema
        {
            get
            {
                CheckObjectState();
                return m_sScriptSchema;
            }
            set
            {
                CheckObjectState();
                if (null == value)
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("ScriptSchema"));
                }

                m_sScriptSchema = value;
            }
        }

        internal override string FormatFullNameForScripting(ScriptingPreferences sp)
        {
            CheckObjectState();
            // format full object name for scripting
            string sFullNameForScripting = String.Empty;
            if (sp.IncludeScripts.SchemaQualify) // pre-qualify object name with an owner name
            {
                string schema = GetSchema(sp);
                if (schema.Length > 0)
                {
                    sFullNameForScripting = MakeSqlBraket(schema);
                    sFullNameForScripting += Globals.Dot;
                }
            }
            sFullNameForScripting += base.FormatFullNameForScripting(sp);

            return sFullNameForScripting;
        }

        internal override void ScriptChangeOwner(StringCollection queries, ScriptingPreferences sp)
        {
            Property prop = GetPropertyOptional("Owner");

            if (!prop.IsNull && (prop.Dirty || !sp.ScriptForAlter))
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                if (sp.TargetServerVersion > SqlServerVersion.Version80)
                {
                    bool schemaOwned = true;
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER AUTHORIZATION ON {0}", PermissionPrefix);
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", FormatFullNameForScripting(sp));
                    sb.AppendFormat(SmoApplication.DefaultCulture, " TO ");

                    if (prop.Dirty)
                    {
                        schemaOwned = ((string)prop.Value == string.Empty);
                    }
                    else if (ServerVersion.Major > 8)
                    {
                        Property isSchemaOwnedProp = Properties.Get("IsSchemaOwned");
                        if (!isSchemaOwnedProp.IsNull)
                        {
                            schemaOwned = (bool)isSchemaOwnedProp.Value;
                        }
                    }
                    else
                    {
                        schemaOwned = false;
                    }

                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", schemaOwned ? " SCHEMA OWNER " : MakeSqlBraket((string)prop.Value));
                }
                else
                {
                   ScriptOwnerForShiloh(sb,sp,(string)prop.Value);
                }
                if (sb.Length > 0)
                {
                    queries.Add(sb.ToString());
                }
            }
        }

        internal string GetSchema(ScriptingPreferences sp)
        {
            // use script name only if we are strictly in scripting mode and script name has some value
            if (!sp.ForDirectExecution && 0 < ScriptSchema.Length)
            {
                return ScriptSchema;
            }
            // use owner name only if it's available
            else if (null != Schema)
            {
                return Schema;
            }

            return string.Empty;
        }

        [SfcKey(0)]
        [SfcReference(typeof(Schema), typeof(SchemaCustomResolver), "Resolve")]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone | SfcPropertyFlags.Design)]
        [CLSCompliant(false)]
		public virtual System.String Schema
		{
			get
			{
				return ((SchemaObjectKey)key).Schema;
			}
			set
			{
				if( null == value )
                {
                    throw new SmoException(ExceptionTemplates.InvalidSchema);
                }

                if (State == SqlSmoState.Pending)
                {
                    // if the object is in Pending state we can set the schema
                    ((SchemaObjectKey)key).Schema = value;
                    return;
                }
                else if (State == SqlSmoState.Creating)
                {
                    // if the object is in Existing state we can set the schema only if the object
                    // has not been added to the collection
                    if (ObjectInSpace)
                    {
                        ((SchemaObjectKey)key).Schema = value;
                        return;
                    }
                }

                // all other cases are not valid, we have to throw
                throw new FailedOperationException(ExceptionTemplates.SetSchema, this, new InvalidSmoOperationException(ExceptionTemplates.SetSchema, State));
            }
        }

        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone | SfcPropertyFlags.Design)]
        public override string Name
        {
            get
            {
                return ((SimpleObjectKey)key).Name;
            }
            set
            {
                try
                {
                    ValidateName(value);
                    if (ShouldNotifyPropertyChange)
                    {
                        if (Name != value)
                        {
                            ((SimpleObjectKey)key).Name = value;
                            OnPropertyChanged("Name");
                        }
                    }
                    else
                    {
                        ((SimpleObjectKey)key).Name = value;
                    }
                    UpdateObjectState();
                }
                catch (Exception e)
                {
                    FilterException(e);

                    throw new FailedOperationException(ExceptionTemplates.SetName, this, e);
                }

            }
        }

        internal void SetSchema(string newSchema)
        {
            ChangeSchema(newSchema, true);
        }

        internal void ChangeSchema(string newSchema, bool bCheckExisting)
        {
            //check is valid string
            if (null == newSchema || 0 == newSchema.Length)
            {
                throw new SmoException(ExceptionTemplates.InvalidSchema);
            }

            if (newSchema == ((SchemaObjectKey)key).Schema)
            {
                return;
            }

            if (!string.IsNullOrEmpty(Schema) && ParentColl is ISchemaObjectCollection col && col.Contains(Name, Schema))
            {
                if (bCheckExisting && SqlSmoState.Existing != State)
                {
                    throw new SmoException(ExceptionTemplates.FailedToChangeSchema);
                }

                //if object is created try to change the schema
                if (SqlSmoState.Existing == State && !IsDesignMode)
                {
                    StringCollection queries = ScriptChangeSchema(Schema, newSchema);
                    ExecutionManager.ExecuteNonQuery(queries);
                }
                //execution succeeded
                //rearange in collection: for now for backward compatibility also for existing objects
                if (!ExecutionManager.Recording)
                {
                    ((ISchemaObjectCollectionInternal)col).RemoveObject(Name, Schema);
                    ((SchemaObjectKey)key).Schema = newSchema;
                    ParentColl.AddExisting(this);
                }
            }
            else
            {
                ((SchemaObjectKey)key).Schema = newSchema;
            }
        }

        // to be called
        private StringCollection ScriptChangeSchema(String oldSchema, String newSchema)
        {
            StringCollection queries = new StringCollection();
            // buidl the full name
            string fullName = string.Format(SmoApplication.DefaultCulture, "[{0}].[{1}]", SqlBraket(oldSchema), SqlBraket(Name));
            queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));

            if (this is UserDefinedType)
            {
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER SCHEMA {0} TRANSFER TYPE :: {1}", MakeSqlBraket(newSchema), fullName));
            }
            else
            {
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER SCHEMA {0} TRANSFER {1}", MakeSqlBraket(newSchema), fullName));
            }
            return queries;
        }

        internal override string FullQualifiedName
        {
            get
            {
                return string.Format(SmoApplication.DefaultCulture, "[{0}].[{1}]", SqlBraket(Schema), SqlBraket(Name));
            }
        }

        #region utility_functions

        internal override ObjectKeyBase GetEmptyKey()
        {
            return new SchemaObjectKey(null, null);
        }
        #endregion
    }
}


