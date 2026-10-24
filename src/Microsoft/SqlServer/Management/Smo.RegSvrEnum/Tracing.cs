#if STRACE
using Microsoft.SqlServer.Management.Diagnostics;
#endif

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
	/// <summary>
	/// Holds constants for use with the Managed Trace Provider
	/// </summary>
	internal sealed class Tracing
	{
		public const string ConnDialog = "ConnectionDialog";
		public const string RegProvider = "RegistrationProvider";
#if STRACE
        public const uint NormalTrace = SQLToolsCommonTraceLvl.L2;
        public const uint Error = SQLToolsCommonTraceLvl.Error;
        public const uint Warning = SQLToolsCommonTraceLvl.Warning;
#endif
    }
}
