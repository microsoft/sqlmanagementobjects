// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Methods for enumerating, granting, and denying permissions on an object.
    /// </summary>
    public interface IObjectPermission
	{
		/// <summary>
		/// The Deny method negates granted user permission for one or more users or roles.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		void Deny(ObjectPermissionSet permissions, System.String[] granteeNames);

		/// <summary>
		/// The Deny method negates granted user permission for one or more users or roles.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		void Deny(ObjectPermissionSet permissions, System.String granteeName);

		/// <summary>
		/// The Deny method negates granted user permission for one or more users or roles.
		/// Cascade specifies that permissions are denied from granteeNames as well as any
		/// other security accounts granted permissions by granteeNames. Use Cascade when
		/// denying a grantable permission.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="cascade"></param>
		void Deny(ObjectPermissionSet permissions, System.String[] granteeNames, System.Boolean cascade);

		/// <summary>
		/// The Deny method negates granted user permission for one or more users or roles.
		/// Cascade specifies that permissions are denied from granteeName as well as any
		/// other security accounts granted permissions by granteeName. Use Cascade when
		/// denying a grantable permission.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="cascade"></param>
		void Deny(ObjectPermissionSet permissions, System.String granteeName, System.Boolean cascade);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		void Grant(ObjectPermissionSet permissions, System.String[] granteeNames);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		void Grant(ObjectPermissionSet permissions, System.String granteeName);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles.
		/// When grantGrant is true, the grantee(s) specified are granted the ability
		/// to execute the GRANT statement referencing the object.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="grantGrant"></param>
		void Grant(ObjectPermissionSet permissions, System.String[] granteeNames, System.Boolean grantGrant);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles.
		/// When grantGrant is true, the grantee(s) specified are granted the ability
		/// to execute the GRANT statement referencing the object.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="grantGrant"></param>
		void Grant(ObjectPermissionSet permissions, System.String granteeName, System.Boolean grantGrant);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles.
		/// When grantGrant is true, the grantee(s) specified are granted the ability
		/// to execute the GRANT statement referencing the object.
		/// Use the asRole argument to specify the role under which permission to
		/// execute the grant.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="grantGrant"></param>
		/// <param name="asRole"></param>
		void Grant(ObjectPermissionSet permissions, System.String[] granteeNames,
			System.Boolean grantGrant, System.String asRole);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles.
		/// When grantGrant is true, the grantee(s) specified are granted the ability
		/// to execute the GRANT statement referencing the object.
		/// Use the asRole argument to specify the role under which permission to
		/// execute the grant.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="grantGrant"></param>
		/// <param name="asRole"></param>
		void Grant(ObjectPermissionSet permissions, System.String granteeName,
			System.Boolean grantGrant, System.String asRole);

		/// <summary>
		/// Removes a previously granted or denied permission for the specified granteeNames.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		void Revoke(ObjectPermissionSet permissions, System.String[] granteeNames);

		/// <summary>
		/// Removes a previously granted or denied permission for the specified granteeName.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		void Revoke(ObjectPermissionSet permissions, System.String granteeName);

		/// <summary>
		/// Removes a previously granted or denied permission for the specified granteeNames.
		/// When revokeGrant is true, the ability to extend permissions is revoked.
		/// Cascade specifies that permissions are removed from granteeNames as well as
		/// any other security accounts granted permissions by granteeNames.
		/// Use Cascade when revoking a grantable permission.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="revokeGrant"></param>
		/// <param name="cascade"></param>
		void Revoke(ObjectPermissionSet permissions, System.String[] granteeNames, System.Boolean revokeGrant,
			System.Boolean cascade);

		/// <summary>
		///	Removes a previously granted or denied permission for the specified granteeName.
		///	When revokeGrant is true, the ability to extend permissions is revoked.
		///	Cascade specifies that permissions are removed from granteeName as well as
		///	any other security accounts granted permissions by granteeName. Use Cascade when
		///	revoking a grantable permission. Use the AsRole argument to specify the role under
		///	which permission to execute the revoke.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="revokeGrant"></param>
		/// <param name="cascade"></param>
		/// <param name="asRole"></param>
		void Revoke(ObjectPermissionSet permissions, System.String granteeName, System.Boolean revokeGrant,
			System.Boolean cascade, System.String asRole);

		/// <summary>
		/// Returns an array of Permission objects identifying all explicitly granted
		/// object access permissions.
		/// </summary>
		/// <returns>ObjectPermissionInfo[]</returns>
		ObjectPermissionInfo[] EnumObjectPermissions();

		/// <summary>
		/// Returns an array of Permission objects identifying explicitly granted object
		/// access permissions for the grantee specified with the granteeName parameter.
		/// </summary>
		/// <param name="granteeName"></param>
		/// <returns>ObjectPermissionInfo[]</returns>
		ObjectPermissionInfo[] EnumObjectPermissions(System.String granteeName);

		/// <summary>
		/// Returns an array of Permission objects identifying explicitly granted object
		/// access permissions. The permissions parameter specifies the object access
		/// permissions enumerated for the referenced object.
		/// </summary>
		/// <param name="permissions"></param>
		/// <returns>ObjectPermissionInfo[]</returns>
		ObjectPermissionInfo[] EnumObjectPermissions(ObjectPermissionSet permissions);

		/// <summary>
		/// Returns an array of Permission objects identifying explicitly granted object
		/// access permissions for the grantee specified with the granteeName parameter,
		/// restricted to the object access permissions specified with the privilegeTypes
		/// parameter.
		/// </summary>
		/// <param name="granteeName"></param>
		/// <param name="permissions"></param>
		/// <returns>ObjectPermissionInfo[]</returns>
		ObjectPermissionInfo[] EnumObjectPermissions(System.String granteeName, ObjectPermissionSet permissions);
	}


	/// <summary>
	/// Interface for column-level object permissions.
	/// </summary>
	public interface IColumnPermission : IObjectPermission
	{
		/// <summary>
		/// The Deny method negates granted user permission for one or more users
		/// or roles, for the specified columnNames.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="columnNames"></param>
		void Deny(ObjectPermissionSet permissions, System.String[] granteeNames,
			System.String[] columnNames);

		/// <summary>
		/// The Deny method negates granted user permission for one or more users
		/// or roles, for the specified columnNames.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="columnNames"></param>
		void Deny(ObjectPermissionSet permissions, System.String granteeName,
			System.String[] columnNames);

		/// <summary>
		/// The Deny method negates granted user permission for one or more users
		/// or roles, for the specified columnNames. Cascade specifies that
		/// permissions are denied from granteeNames as well as any other
		/// security accounts granted permissions by granteeNames. Use Cascade
		/// when denying a grantable permission.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="columnNames"></param>
		/// <param name="cascade"></param>
		void Deny(ObjectPermissionSet permissions, System.String[] granteeNames,
			System.String[] columnNames, System.Boolean cascade);

		/// <summary>
		/// The Deny method negates granted user permission for one or more users
		/// or roles, for the specified columnNames. Cascade specifies that
		/// permissions are denied from granteeName as well as any other
		/// security accounts granted permissions by granteeName. Use Cascade
		/// when denying a grantable permission.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="columnNames"></param>
		/// <param name="cascade"></param>
		void Deny(ObjectPermissionSet permissions, System.String granteeName,
			System.String[] columnNames, System.Boolean cascade);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles
		/// for the specified columns.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="columnNames"></param>
		void Grant(ObjectPermissionSet permissions, System.String[] granteeNames,
			System.String[] columnNames);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles
		/// for the specified columns.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="columnNames"></param>
		void Grant(ObjectPermissionSet permissions, System.String granteeName,
			System.String[] columnNames);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles
		/// for the specified columns. When grantGrant is true, the grantee(s)
		/// specified are granted the ability to execute the GRANT statement
		/// referencing the object.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="columnNames"></param>
		/// <param name="grantGrant"></param>
		void Grant(ObjectPermissionSet permissions, System.String[] granteeNames,
			System.String[] columnNames, System.Boolean grantGrant);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles
		/// for the specified columns. When grantGrant is true, the grantee(s)
		/// specified are granted the ability to execute the GRANT statement
		/// referencing the object.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="columnNames"></param>
		/// <param name="grantGrant"></param>
		void Grant(ObjectPermissionSet permissions, System.String granteeName,
			System.String[] columnNames, System.Boolean grantGrant);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles
		/// for the specified columns. When grantGrant is true, the grantee(s)
		/// specified are granted the ability to execute the GRANT statement
		/// referencing the object. Use the AsRole argument to specify the role
		/// under which role to execute the grant.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="columnNames"></param>
		/// <param name="grantGrant"></param>
		/// <param name="asRole"></param>
		void Grant(ObjectPermissionSet permissions, System.String[] granteeNames,
			System.String[] columnNames, System.Boolean grantGrant, System.String asRole);

		/// <summary>
		/// The Grant method assigns permissions to one or more users or roles
		/// for the specified columns. When grantGrant is true, the grantee(s)
		/// specified are granted the ability to execute the GRANT statement
		/// referencing the object. Use the AsRole argument to specify the role
		/// under which role to execute the grant.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="columnNames"></param>
		/// <param name="grantGrant"></param>
		/// <param name="asRole"></param>
		void Grant(ObjectPermissionSet permissions, System.String granteeName,
			System.String[] columnNames, System.Boolean grantGrant, System.String asRole);

		/// <summary>
		/// Removes previously granted or denied column permission for
		/// the specified granteeNames.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="columnNames"></param>
		void Revoke(ObjectPermissionSet permissions, System.String[] granteeNames,
			System.String[] columnNames);

		/// <summary>
		/// Removes previously granted or denied column permission for
		/// the specified granteeName.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="columnNames"></param>
		void Revoke(ObjectPermissionSet permissions, System.String granteeName,
			System.String[] columnNames);

		/// <summary>
		/// Removes a previously granted or denied permission for the specified
		/// granteeNames. When revokeGrant is true, the ability to extend
		/// permissions is revoked. Cascade specifies that permissions are
		/// removed from granteeNames as well as any other security accounts
		/// granted permissions by granteeNames. Use Cascade when revoking a
		/// grantable permission.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="columnNames"></param>
		/// <param name="revokeGrant"></param>
		/// <param name="cascade"></param>
		void Revoke(ObjectPermissionSet permissions, System.String[] granteeNames,
			System.String[] columnNames, System.Boolean revokeGrant, System.Boolean cascade);

		/// <summary>
		/// Removes a previously granted or denied permission for the specified
		/// granteeName. When revokeGrant is true, the ability to extend
		/// permissions is revoked. Cascade specifies that permissions are
		/// removed from granteeName as well as any other security accounts
		/// granted permissions by granteeName. Use Cascade when revoking a
		/// grantable permission.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="columnNames"></param>
		/// <param name="revokeGrant"></param>
		/// <param name="cascade"></param>
		void Revoke(ObjectPermissionSet permissions, System.String granteeName,
			System.String[] columnNames, System.Boolean revokeGrant, System.Boolean cascade);


		/// <summary>
		/// Removes a previously granted or denied permission for the specified
		/// granteeNames. When revokeGrant is true, the ability to extend
		/// permissions is revoked. Cascade specifies that permissions are
		/// removed from granteeNames as well as any other security accounts
		/// granted permissions by granteeNames. Use Cascade when revoking a
		/// grantable permission. Use the AsRole argument to specify the role
		/// under which permission to execute the revoke.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeNames"></param>
		/// <param name="columnNames"></param>
		/// <param name="revokeGrant"></param>
		/// <param name="cascade"></param>
		/// <param name="asRole"></param>
		void Revoke(ObjectPermissionSet permissions, System.String[] granteeNames,
			System.String[] columnNames, System.Boolean revokeGrant, System.Boolean cascade,
			System.String asRole);

		/// <summary>
		/// Removes a previously granted or denied permission for the specified
		/// granteeName. When revokeGrant is true, the ability to extend
		/// permissions is revoked. Cascade specifies that permissions are
		/// removed from granteeName as well as any other security accounts
		/// granted permissions by granteeName. Use Cascade when revoking a
		/// grantable permission. Use the AsRole argument to specify the role
		/// under which permission to execute the revoke.
		/// </summary>
		/// <param name="permissions"></param>
		/// <param name="granteeName"></param>
		/// <param name="columnNames"></param>
		/// <param name="revokeGrant"></param>
		/// <param name="cascade"></param>
		/// <param name="asRole"></param>
		void Revoke(ObjectPermissionSet permissions, System.String granteeName,
			System.String[] columnNames, System.Boolean revokeGrant, System.Boolean cascade,
			System.String asRole);

		/// <summary>
		/// Returns an array of Permission objects identifying explicitly granted
		/// column access permissions for the grantee specified with the granteeName
		/// parameter.
		/// </summary>
		/// <param name="granteeName"></param>
		/// <returns>ObjectPermissionInfo[]</returns>
		ObjectPermissionInfo[] EnumColumnPermissions(System.String granteeName);

		/// <summary>
		/// Returns an array of Permission objects identifying explicitly granted
		/// column access permissions for the grantee specified with the granteeName
		/// parameter, restricted to the column access permissions specified with
		/// the privilegeTypes parameter.
		/// </summary>
		/// <param name="granteeName"></param>
		/// <param name="permissions"></param>
		/// <returns>ObjectPermissionInfo[]</returns>
		ObjectPermissionInfo[] EnumColumnPermissions(System.String granteeName, ObjectPermissionSet permissions);
	}

}
