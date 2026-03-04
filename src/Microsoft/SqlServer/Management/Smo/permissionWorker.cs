// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Abstract class for all Permission classes.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")]
    public abstract class PermissionSetBase
    {
        private BitArray m_storage;

        internal BitArray Storage
        {
            get { return m_storage; }
            set { m_storage = value; }
        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PermissionSetBase()
        {
            m_storage = new BitArray(this.NumberOfElements);
        }

        public PermissionSetBase(PermissionSetBase permissionSetBase)
        {
            m_storage = (BitArray)(permissionSetBase.m_storage.Clone());
        }

        internal abstract int NumberOfElements
        {
            get;
        }

        internal void SetBitAt(int idx)
        {
            m_storage[idx] = true;
        }

        internal abstract string PermissionCodeToPermissionName(int permissionCode);
        internal abstract string PermissionCodeToPermissionType(int permissionCode);

        int YukonToShilohPermission(string permCode)
        {
            switch (permCode.TrimEnd(new char[] { ' ' }))
            {
                case "RF": return 26;		//References
                case "CRFN": return 178;		//Create Function
                case "SL": return 193;		//Select
                case "IN": return 195;		//Insert
                case "DL": return 196;		//Delete
                case "UP": return 197;		//Update
                case "CRTB": return 198;		//Create Table
                case "CRDB": return 203;		//Create Database
                case "CRVW": return 207;		//Create View
                case "CRPR": return 222;		//Create Procedure
                case "EX": return 224;		//Execute
                case "BADB": return 228;		//Backup Database
                case "CRDF": return 233;		//Create Default
                case "BALO": return 235;		//Backup Transaction ( LOG )
                case "CRRU": return 236;		//Create Rule       
            }
            return -1;
        }

        //assumes only on permission is set
        internal bool IsValidPermissionForVersion(SqlServerVersion ver)
        {
            if (ver >= SqlServerVersion.Version90)
            {
                return true;
            }
            for (int i = 0; i < m_storage.Length; i++)
            {
                if (m_storage[i])
                {
                    int code = YukonToShilohPermission(PermissionCodeToPermissionType(i));
                    if (0 > code)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal void AddPermissionFilter(StringBuilder sb, ServerVersion ver)
        {
            string attribute = "@StringCode = ";

            bool bFirst = true;
            for (int i = 0; i < m_storage.Length; i++)
            {
                if (m_storage[i])
                {
                    if (bFirst)
                    {
                        bFirst = false;
                    }
                    else
                    {
                        sb.Append(" or ");
                    }
                    sb.Append(attribute);
                    string s = PermissionCodeToPermissionType(i);
                    sb.Append("'" + s + "'");
                }
            }
        }

        internal bool AddPermissionList(StringBuilder sb)
        {
            bool bFirst = true;
            for (int i = 0; i < m_storage.Length; i++)
            {
                if (m_storage[i])
                {
                    if (bFirst)
                    {
                        bFirst = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append(PermissionCodeToPermissionName(i));
                }
            }

            // if no permission was specified we need to throw an exception 
            // to tell the user s/he needs to set a permission 
            return !bFirst;
        }

        /// <summary>
        /// Returns count of how many permissions have been added to the PermissionSet.
        /// </summary>
        /// <returns></returns>
        internal int GetPermissionCount()
        {
            int permissionCount = 0;
            for (int i = 0; i < this.Storage.Length; i++)
            {
                if (this.Storage[i])
                {
                    permissionCount++;
                }
            }

            return permissionCount;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            AddPermissionList(sb);
            return sb.ToString();
        }

        // because we need them to implement equality operators (== and !=)
        public override bool Equals(object o)
        {
            if (null == o)
            {
                return false;
            }
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            PermissionSetBase psb = o as PermissionSetBase;
            for (int i = 0; i < this.NumberOfElements; i++)
            {
                if (m_storage[i] != psb.m_storage[i])
                {
                    return false;
                }
            }
            return true;
        }

        // Override the Object.GetHashCode() method:
        public override int GetHashCode()
        {
            return m_storage.GetHashCode();
        }
    }

    public class PermissionInfo
    {
        string grantee;
        PrincipalType granteeType;
        string grantor;
        PrincipalType grantorType;
        PermissionState permissionState;
        PermissionSetBase permissionSet;
        string columnName;
        ObjIdent objIdent;

        internal class ObjIdent
        {
            ObjectClass objectClass;
            string objectName;
            string objectSchema;
            int objectId;

            internal ObjIdent(ObjectClass objectClass, string objectName, string objectSchema, int objectId)
            {
                this.objectClass = objectClass;
                this.objectName = objectName;
                this.objectSchema = objectSchema;
                this.objectId = objectId;
            }

            internal ObjIdent(ObjectClass objectClass)
            {
                this.objectClass = objectClass;
            }

            internal void SetData(SqlSmoObject obj)
            {
                this.objectName = ((SimpleObjectKey)obj.key).Name;
                this.objectId = obj as Server == null ? (int)obj.GetPropValue("ID") : 0;
                ScriptSchemaObjectBase oSchema = obj as ScriptSchemaObjectBase;
                if (null != oSchema)
                {
                    this.objectSchema = oSchema.Schema;
                }
            }

            public ObjectClass ObjectClass
            {
                get { return objectClass; }
            }

            public string ObjectName
            {
                get { return objectName; }
            }

            public string ObjectSchema
            {
                get { return objectSchema; }
            }

            public int ObjectID
            {
                get { return objectId; }
            }
        }

        internal PermissionInfo()
        {
        }

        internal void SetPermissionInfoData(string grantee, PrincipalType granteeType, string grantor, PrincipalType grantorType, PermissionState permissionState, PermissionSetBase permissionSet, string columnName, ObjIdent objIdent)
        {
            this.grantee = grantee;
            this.granteeType = granteeType;
            this.grantor = grantor;
            this.grantorType = grantorType;
            this.permissionState = permissionState;
            this.permissionSet = permissionSet;
            this.columnName = columnName;
            this.objIdent = objIdent;
        }

        public string Grantee
        {
            get { return grantee; }
        }

        public PrincipalType GranteeType
        {
            get { return granteeType; }
        }

        public string Grantor
        {
            get { return grantor; }
        }

        public PrincipalType GrantorType
        {
            get { return grantorType; }
        }

        public PermissionState PermissionState
        {
            get { return permissionState; }
        }

        internal void SetPermissionState(PermissionState ps)
        {
            permissionState = ps;
        }

        internal protected PermissionSetBase PermissionTypeInternal
        {
            get { return permissionSet; }
        }

        public string ColumnName
        {
            get { return columnName; }
        }

        public ObjectClass ObjectClass
        {
            get { return objIdent.ObjectClass; }
        }

        public string ObjectName
        {
            get { return objIdent.ObjectName; }
        }

        public string ObjectSchema
        {
            get { return objIdent.ObjectSchema; }
        }

        public int ObjectID
        {
            get { return objIdent.ObjectID; }
        }

        public override string ToString()
        {
            if (null == this.ColumnName)
            {
                return String.Format(SmoApplication.DefaultCulture,
                    "{0} {1}: {2}, {3}, {4}",
                    SqlSmoObject.MakeSqlBraket(ObjectName),
                    this.ObjectClass.ToString(),
                    this.Grantee,
                    this.PermissionState,
                    this.PermissionTypeInternal);
            }
            else
            {
                return String.Format(SmoApplication.DefaultCulture,
                    "{0}.{5} {1}: {2}, {3}, {4}",
                    SqlSmoObject.MakeSqlBraket(ObjectName),
                    this.ObjectClass.ToString(),
                    this.Grantee,
                    this.PermissionState,
                    this.PermissionTypeInternal,
                    SqlSmoObject.MakeSqlBraket(ColumnName));
            }
        }
    }

    public class ObjectPermissionInfo : PermissionInfo
    {
        public ObjectPermissionSet PermissionType
        {
            get { return (ObjectPermissionSet)PermissionTypeInternal; }
        }
    }

    public class DatabasePermissionInfo : PermissionInfo
    {
        public DatabasePermissionSet PermissionType
        {
            get { return (DatabasePermissionSet)PermissionTypeInternal; }
        }
    }

    public class ServerPermissionInfo : PermissionInfo
    {
        public ServerPermissionSet PermissionType
        {
            get { return (ServerPermissionSet)PermissionTypeInternal; }
        }
    }

    internal class PermissionWorker
    {
        internal enum PermissionEnumKind
        {
            Column,
            Object,
            Database,
            Server
        }

        static void AddArrayToStringBuider(StringBuilder sb, string[] list)
        {
            bool bFirst = true;
            foreach (string col in list)
            {
                if (bFirst)
                {
                    bFirst = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(SqlSmoObject.MakeSqlBraket((string)col));
            }
        }

        /// <summary>
        /// Check if permission is allowed for the server version. If not allowed, throw 
        /// UnsupportedVersionException.
        /// </summary>
        /// <param name="obj"></param>
        internal static void CheckPermissionsAllowed(SqlSmoObject obj)
        {
        }

        internal static string ScriptPermissionInfo(SqlSmoObject obj, PermissionInfo pi, ScriptingPreferences sp, bool grantGrant, bool cascade)
        {
            if (PermissionState.GrantWithGrant == pi.PermissionState)
            {
                grantGrant = true;
                pi.SetPermissionState(PermissionState.Grant);
            }
            return Script(pi.PermissionState, obj, pi.PermissionTypeInternal, new String[] { pi.Grantee },
                pi.ColumnName != null ? new String[] { pi.ColumnName } : null, grantGrant, cascade, pi.Grantor, sp);
        }

        internal static string ScriptPermissionInfo(SqlSmoObject obj, PermissionInfo pi, ScriptingPreferences sp)
        {
            return PermissionWorker.ScriptPermissionInfo(obj, pi, sp, false, false);
        }

        static string GetObjectName(SqlSmoObject obj, ScriptingPreferences sp)
        {
            if (null == obj || obj is Smo.Database || obj is Smo.Server)
            {
                return null;
            }

            NamedSmoObject namedObj = obj as NamedSmoObject;
            if (null != namedObj)
            {
                return namedObj.PermissionPrefix + namedObj.FormatFullNameForScripting(sp);
            }

            return SqlSmoObject.MakeSqlBraket(((SimpleObjectKey)obj.key).Name);
        }

        static string Script(PermissionState ps,
                        SqlSmoObject obj,
                        PermissionSetBase pb,
                        System.String[] granteeNames,
                        System.String[] columnNames,
                        System.Boolean grantGrant,
                        bool cascade,
                        System.String asRole,
                        ScriptingPreferences sp)
        {
            string objName = GetObjectName(obj, sp);
            StringBuilder sb = new StringBuilder();
            bool asClauseSupported = true;

            switch (ps)
            {
                case PermissionState.Grant: sb.Append("GRANT "); break;
                case PermissionState.Deny:
                    sb.Append("DENY ");
                    // Deny cannot take an "AS" Clause for sql server versions below 90.
                    // However, Grant and Revoke can take an "AS" clause on all versions.-anchals
                    if (sp.TargetServerVersion < SqlServerVersion.Version90)
                    {
                        asClauseSupported = false;
                    }
                    break;
                case PermissionState.Revoke: sb.Append("REVOKE "); break;
            }

            if (grantGrant && PermissionState.Revoke == ps)
            {
                sb.Append("GRANT OPTION FOR ");
            }

            if (!pb.AddPermissionList(sb))
            {
                throw new SmoException(ExceptionTemplates.NoPermissions);
            }

            if (null != objName)
            {
                sb.Append(" ON ");
                sb.Append(objName);
            }

            if (null != columnNames)
            {
                sb.Append(" (");
                AddArrayToStringBuider(sb, columnNames);
                sb.Append(")");
            }

            if (null == granteeNames)
            {
                throw new ArgumentNullException("granteeNames");
            }

            if (granteeNames.Length == 0)
            {
                throw new SmoException(ExceptionTemplates.EmptyInputParam("granteeNames", "StringCollection"));
            }

            sb.Append(" TO ");
            AddArrayToStringBuider(sb, granteeNames);

            if (grantGrant && PermissionState.Grant == ps)
            {
                sb.Append(" WITH GRANT OPTION ");
            }
            if (cascade)
            {
                sb.Append(" CASCADE");
            }

            // Script out the as clause only if there is some role specified or
            // as clause is supported for the targetted sql server version.-anchals
            if (!string.IsNullOrEmpty(asRole) && asClauseSupported)
			{
				sb.Append(" AS ");
				sb.Append( SqlSmoObject.MakeSqlBraket(asRole));
			}

            return sb.ToString();
        }

        internal static PermissionSetBase GetPermissionSetBase(PermissionEnumKind kind, int i)
        {
            PermissionSetBase p = null;
            switch (kind)
            {
                case PermissionEnumKind.Column: p = new ObjectPermissionSet(); break;
                case PermissionEnumKind.Object: p = new ObjectPermissionSet(); break;
                case PermissionEnumKind.Database: p = new DatabasePermissionSet(); break;
                case PermissionEnumKind.Server: p = new ServerPermissionSet(); break;
            }
            p.SetBitAt(i);
	    return p;
        }

        internal static ObjectClass GetObjectClass(SqlSmoObject obj)
        {
            string type = obj.GetType().Name;
            ObjectClass result;

            switch (type)
            {
                case nameof(XmlSchemaCollection):
                    result = ObjectClass.XmlNamespace;
                    break;
                case "BrokerService":
                    result = ObjectClass.Service;
                    break;
                case nameof(UserDefinedDataType):
                case nameof(UserDefinedTableType):
                    result = ObjectClass.UserDefinedType;
                    break;
                case nameof(ExtendedStoredProcedure):
                case "ServiceQueue":
                case nameof(StoredProcedure):
                case nameof(Synonym):
                case nameof(Table):
                case nameof(UserDefinedAggregate):                
                case nameof(UserDefinedFunction):
                case nameof(View):
                case nameof(Column):
                    result = ObjectClass.ObjectOrColumn;
                    break;
                default:
                    result = (ObjectClass)(Enum.Parse(typeof(ObjectClass), type, true));
                    break;
            }
            return result;
        }

        internal static string GetObjectOwner(SqlSmoObject smoObj)
        {
            string result = string.Empty;

            SqlSmoObject obj = smoObj;

            if (smoObj is Column)
            {
                obj = ((Column)smoObj).Parent;
            }

            if (obj.Properties.Contains("Owner"))
            {
                result = obj.GetPropValueOptionalAllowNull("Owner") as string;
                if (string.IsNullOrEmpty(result))
                {
                    if (obj.Properties.Contains("IsSchemaOwned") && obj is ScriptSchemaObjectBase)
                    {                        
                        string schema = ((ScriptSchemaObjectBase)obj).Schema;
                        if (!string.IsNullOrEmpty(schema))
                        {
                            Database db = SfcResolverHelper.GetDatabase(obj);
                            Schema schemaObj = db.Schemas[schema];
                            if (schemaObj != null)
                            {
                                result = schemaObj.GetPropValueOptionalAllowNull("Owner") as string;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                result = "dbo";
            }

            return result;
        }

        internal static void AddPermission(PermissionState ps,
                                SqlSmoObject obj,
                                PermissionSetBase pb,
                                System.String[] granteeNames,
                                System.Boolean grantGrant,
                                bool cascade,
                                System.String asRole)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < pb.Storage.Length; i++)
            {
                if (pb.Storage[i])
                {
                    foreach (string granteeName in granteeNames)
                    {
                        PrincipalType granteeType = PrincipalType.None;
                        PrincipalType grantorType = PrincipalType.None;

                        ObjectClass objClass = GetObjectClass(obj);

                        string grantor = string.Empty;
                        if (!string.IsNullOrEmpty(asRole))
                        {
                            grantor = asRole;
                        }
                        else
                        {
                            grantor = GetObjectOwner(obj);
                        }

                        sb.Length = 0;
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0}_{1}_{2}_{3}_{4}_{5}_{6}", granteeName, (int)granteeType, grantor,
                            (int)grantorType, (int)objClass, (int)ps, (ObjectPermissionSetValue)i);

                        UserPermission userPerm = new UserPermission();
                        userPerm.Parent = obj;

                        //userPerm.Name = granteeName + (int)granteeType + grantor
                        //    + (int)grantorType + (int)objClass + (int)ps + i;

                        userPerm.Name = sb.ToString();

                        userPerm.PermissionState = ps;
                        if (grantGrant)
                        {
                            if (userPerm.PermissionState == PermissionState.Grant)
                            {
                                userPerm.PermissionState = PermissionState.GrantWithGrant;
                            }
                            else if (userPerm.PermissionState == PermissionState.Revoke)
                            {
                                //TODO: need to have a new state
                                //userPerm.PermissionState = PermissionState.GrantWithGrant;
                            }
                        }

                        userPerm.Grantor = grantor;
                        userPerm.ObjectClass = objClass;
                        userPerm.GrantorType = grantorType;
                        userPerm.GranteeType = granteeType;
                        userPerm.Code = (ObjectPermissionSetValue)i;
                        userPerm.Grantee = granteeName;

                        obj.Permissions.AddExisting(userPerm);
                    }
                }
            }
        }


        internal static void Execute(PermissionState ps, 
								SqlSmoObject obj, 
								PermissionSetBase pb, 
								System.String[] granteeNames, 
								System.String[] columnNames, 
								System.Boolean grantGrant, 
								bool cascade, 
								System.String asRole)
		{
			CheckPermissionsAllowed(obj);
			try
			{
				StringCollection sc = new StringCollection();
				string db = null != obj ? obj.GetDBName() : null;
				if( null == db || db.Length <= 0 ||  obj is ExtendedStoredProcedure)
				{
					db = "master";
				}

				SqlSmoObject obj2 = obj;
				if( obj is Database || obj is Server )
				{
					obj2 = null;
				}

                var sp = new ScriptingPreferences
                {
                    ForDirectExecution = true
                };

                if (obj.DatabaseEngineType == DatabaseEngineType.Standalone)
                {
                    sc.Add(String.Format(SmoApplication.DefaultCulture, "use {0}", SqlSmoObject.MakeSqlBraket(db)));
                }

				sc.Add(Script(ps, obj2, pb, granteeNames, columnNames, grantGrant, cascade, asRole, sp));

                if (!obj.IsDesignMode)
                {
                    obj.ExecutionManager.ExecuteNonQuery(sc);
                    // invalidate the permissions cache on succesful execution
                    obj.ClearUserPemissions();
                }
                else
                {
                    if (columnNames != null && columnNames.Length != 0)
                    {
                        var columnCol = ((IColumns)obj).Columns;

                        foreach (var column in columnNames)
                        {
                            var col = columnCol[column];

                            if (col != null)
                            {
                                AddPermission(ps, col, pb, granteeNames, grantGrant, cascade, asRole);
                            }
                            else
                            {
                                //throw exception
                                throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist(nameof(Column), column));
                            }
                        }
                    }
                    else
                    {
                        AddPermission(ps, obj, pb, granteeNames, grantGrant, cascade, asRole);
                    }
                }
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ps.ToString(), obj, e).SetHelpContext(ps.ToString());
            }
        }

		internal static PermissionInfo GetPermissionInfo(PermissionEnumKind kind)
		{
			PermissionInfo p = null;
			switch(kind)
			{
				case PermissionEnumKind.Column:p = new ObjectPermissionInfo();break;
				case PermissionEnumKind.Object:p = new ObjectPermissionInfo();break;
				case PermissionEnumKind.Database:p = new DatabasePermissionInfo();break;
				case PermissionEnumKind.Server:p = new ServerPermissionInfo();break;
			}
			return p;
		}

		internal static PermissionInfo[] GetPermissionInfoArray(PermissionEnumKind kind, int count)
		{
			PermissionInfo [] p = null;
			switch(kind)
			{
				case PermissionEnumKind.Column:p = new ObjectPermissionInfo[count];break;
				case PermissionEnumKind.Object:p = new ObjectPermissionInfo[count];break;
				case PermissionEnumKind.Database:p = new DatabasePermissionInfo[count];break;
				case PermissionEnumKind.Server:p = new ServerPermissionInfo[count];break;
			}
			return p;
		}

		static string GetFilter(ServerVersion ver, System.String granteeName, PermissionSetBase permissions)
		{
			StringBuilder sb = new StringBuilder();

			if( null != granteeName )
			{
				sb.Append("[@Grantee='" + SqlSmoObject.SqlString(granteeName) + "'");
			}

			if( null != permissions )
			{
				StringBuilder sbPermissions = new StringBuilder();
				permissions.AddPermissionFilter(sbPermissions, ver);
				// Add the filter only if there are permissions to be added
				if (sbPermissions.Length != 0)
				{
					sb.Append(sb.Length > 0 ? " and (" : "[(");
					sb.Append( sbPermissions.ToString() );
					sb.Append(")");
				}
			}
			if( sb.Length > 0 )
			{
				sb.Append("]");
			}
			return sb.ToString();
		}

		internal static PermissionInfo[] EnumPermissions(PermissionEnumKind kind, SqlSmoObject obj, 
			System.String granteeName, PermissionSetBase permissions)
		{
            ArrayList ar = new ArrayList();
            PermissionInfo.ObjIdent objectIdent = null;

            if (obj.IsDesignMode)
            {
                UserPermissionCollection userPermCollection = null;

                if (PermissionEnumKind.Column == kind)
                {
                    ColumnCollection columnCol = null;
                    switch (obj.GetType().Name)
                    {
                        case "Table":
                            columnCol = ((Table)obj).Columns;
                            break;
                        case "View":
                            columnCol = ((View)obj).Columns;
                            break;
                        case "UserDefinedFunction":
                            columnCol = ((UserDefinedFunction)obj).Columns;
                            break;
                    }

                    foreach (Column col in columnCol)
                    {
                        userPermCollection = col.GetUserPermissions();
                        objectIdent = RetrievePermission(userPermCollection, kind, col.Name, granteeName, permissions, ar, objectIdent);
                    }
                }
                else
                {
                    userPermCollection = obj.GetUserPermissions();
                    objectIdent = RetrievePermission(userPermCollection, kind, string.Empty, granteeName, permissions, ar, objectIdent);
                }
            }
            else
            {
                Request req = new Request();
                if (PermissionEnumKind.Column == kind)
                {
                    req.Urn = obj.Urn.Value + "/Column/Permission";
                    req.Fields = new String[] { 
                    "Grantee", 
                    "Grantor", 
                    "PermissionState", 
                    "Code", 
                    "ObjectClass", 
                    "GranteeType", 
                    "GrantorType", 
                    "ColumnName" };
                }
                else
                {
                    req.Urn = obj.Urn.Value + "/Permission";
                    req.Fields = new String[] { 
                    "Grantee", 
                    "Grantor", 
                    "PermissionState", 
                    "Code", 
                    "ObjectClass", 
                    "GranteeType", 
                    "GrantorType" };
                }
                req.Urn = req.Urn.Value + GetFilter(obj.ServerVersion, granteeName, permissions);
                System.Data.IDataReader dr = obj.ExecutionManager.GetEnumeratorDataReader(req);

                try
                {
                    while (dr.Read())
                    {
                        if (null == objectIdent)
                        {
                            objectIdent = new PermissionInfo.ObjIdent((ObjectClass)dr.GetInt32(4));
                        }


                    int permissionIndex = dr.GetInt32(3);

                    //For Server object, from SQL 11 onwards, the permission urn provides permissions on Server object too
                    //But Server Permissions are not ObjectPermissions and hence presently we can't return Server Permissions
                    //as Object permission in this method.
                    //(Visit PostProcessPermissionCode in Enumerator for more context.)
                    if (permissionIndex >= 0)
                    {
                        PermissionInfo p = GetPermissionInfo(kind);
                        p.SetPermissionInfoData(
                            dr.GetString(0),                    // "Grantee"
                            (PrincipalType)dr.GetInt32(5),      // "GranteeType"
                            dr.GetString(1),                    // "Grantor"
                            (PrincipalType)dr.GetInt32(6),      // "GrantorType"
                            (PermissionState)dr.GetInt32(2),    // "PermissionState"
                            GetPermissionSetBase(kind, permissionIndex),     // "Code"
                            PermissionEnumKind.Column == kind ? dr.GetString(7) : null,     // "ColumnName"
                            objectIdent);
                        ar.Add(p);
                    }
				}			
                }
                finally
                {
                    dr.Close();
                }
            }
			if( null != objectIdent )
			{
				objectIdent.SetData(obj);
			}

			PermissionInfo [] per = GetPermissionInfoArray(kind, ar.Count);
			ar.CopyTo(per);
			return per;
		}

        private static PermissionInfo.ObjIdent RetrievePermission(UserPermissionCollection userPermCollection, PermissionEnumKind kind, string columnName, System.String granteeName, PermissionSetBase permissions, ArrayList ar, PermissionInfo.ObjIdent objectIdent)
        {
            foreach (UserPermission userPerm in userPermCollection)
            {
                if ((granteeName != null && userPerm.Grantee != granteeName) || (permissions != null && !permissions.Storage[(int)userPerm.Code]))
                {
                    continue;
                }

                if (null == objectIdent)
                {
                    objectIdent = new PermissionInfo.ObjIdent(userPerm.ObjectClass);
                }

                PermissionInfo p = GetPermissionInfo(kind);
                p.SetPermissionInfoData(
                    userPerm.Grantee,                    // "Grantee"
                    userPerm.GranteeType,      // "GranteeType"
                    userPerm.Grantor,                    // "Grantor"
                    userPerm.GrantorType,      // "GrantorType"
                    userPerm.PermissionState,    // "PermissionState"
                    GetPermissionSetBase(kind, (int)userPerm.Code),     // "Code"
                    columnName,     // "ColumnName"
                    objectIdent);
                ar.Add(p);
            }
            return objectIdent;
        }

		internal static PermissionInfo[] EnumAllPermissions(SqlSmoObject obj, System.String granteeName, 
			ObjectPermissionSet permissions)
		{
			Request req = new Request();
			req.Urn = obj.Urn.Value + "/LevelPermission" + GetFilter(obj.ServerVersion, granteeName, permissions);
            req.Fields = new String[] { 
                "Grantee", 
                "Grantor", 
                "PermissionState", 
                "Code", 
                "ObjectClass", 
                "ColumnName", 
                "ObjectName", 
                "ObjectSchema", 
                "ObjectID", 
                "GranteeType", 
                "GrantorType" };

            System.Data.IDataReader dr = obj.ExecutionManager.GetEnumeratorDataReader(req);


			ArrayList ar = new ArrayList();
			try
			{
				while( dr.Read() )
				{
                    int permissionIndex = dr.GetInt32(3);

                    //For Server object, from SQL 11 onwards, the permission urn provides permissions on Server object too
                    //But Server Permissions are not ObjectPermissions and hence presently we can't return Server Permissions
                    //as Object permission in this method.
                    //(Visit PostProcessPermissionCode in Enumerator for more context.)
                    if (permissionIndex >= 0)
                    {
                        PermissionInfo.ObjIdent objectIdent =
                                    new PermissionInfo.ObjIdent(
                                                    (ObjectClass)dr.GetInt32(4),
                                                    dr.GetString(6),
                                                    dr.GetValue(7) as string,
                                                    dr.GetInt32(8));

                        ObjectPermissionInfo p = new ObjectPermissionInfo();

                        p.SetPermissionInfoData(
                            dr.GetString(0),
                            (PrincipalType)dr.GetInt32(9),
                            dr.GetString(1),
                            (PrincipalType)dr.GetInt32(10),
                            (PermissionState)dr.GetInt32(2),
                            GetPermissionSetBase(PermissionEnumKind.Object, permissionIndex),
                            dr.GetValue(5) as string,
                            objectIdent);
                        ar.Add(p);
                    }
				}
			}
			finally
			{
				dr.Close();
			}
			ObjectPermissionInfo [] per = new ObjectPermissionInfo[ar.Count];
			ar.CopyTo(per);
			return per;
		}


        static internal Urn[] EnumOwnedObjects(SqlSmoObject obj)
        {
            Request req = new Request();
            req.Urn = obj.Urn.Value + "/OwnedObject";
            req.Fields = new String[] { "Urn" };

            System.Data.IDataReader dr = obj.ExecutionManager.GetEnumeratorDataReader(req);

            ArrayList ar = new ArrayList();
            try
            {
                while (dr.Read())
                {
                    ar.Add((Urn)dr.GetString(0));
                }
            }
            finally
            {
                dr.Close();
            }
            Urn[] urn = new Urn[ar.Count];
            ar.CopyTo(urn);
            return urn;

        }
    }
}

