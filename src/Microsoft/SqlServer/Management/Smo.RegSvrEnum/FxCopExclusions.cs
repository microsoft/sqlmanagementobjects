using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.SqlServer.Management.Smo.RegSvrEnum.DataProtection+CryptoNativeMethods")]

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "namespace", Target = "Microsoft.SqlServer.Management.Smo.RegSvrEnum", MessageId = "Svr")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "Microsoft.SqlServer.Management.Smo.RegSvrEnum.SRError.resources", MessageId = "Svr")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "Microsoft.SqlServer.Management.Smo.RegSvrEnum.SRError.resources", MessageId = "datafile")]

[module: SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", Scope = "member", Target = "Microsoft.SqlServer.Management.Smo.RegSvrEnum.SRError+Keys.GetString(System.String,System.Object[]):System.String", MessageId = "System.String.Format(System.String,System.Object[])")]
[module: SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Scope="member", Target="Microsoft.SqlServer.Management.Smo.RegSvrEnum.DataProtection+CryptoNativeMethods.#CryptUnprotectData(Microsoft.SqlServer.Management.Smo.RegSvrEnum.DataProtection+CryptoNativeMethods+DATA_BLOB&,System.Text.StringBuilder,System.IntPtr,System.IntPtr,System.IntPtr,System.Int32,Microsoft.SqlServer.Management.Smo.RegSvrEnum.DataProtection+CryptoNativeMethods+DATA_BLOB&)", MessageId="1", Justification="Temporary suppression: Revisit in SQL 11")]
[module: SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Scope="member", Target="Microsoft.SqlServer.Management.Smo.RegSvrEnum.DataProtection+CryptoNativeMethods.#CryptProtectData(Microsoft.SqlServer.Management.Smo.RegSvrEnum.DataProtection+CryptoNativeMethods+DATA_BLOB&,System.String,System.IntPtr,System.IntPtr,System.IntPtr,System.Int32,Microsoft.SqlServer.Management.Smo.RegSvrEnum.DataProtection+CryptoNativeMethods+DATA_BLOB&)", MessageId="1", Justification="Temporary suppression: Revisit in SQL 11")]
