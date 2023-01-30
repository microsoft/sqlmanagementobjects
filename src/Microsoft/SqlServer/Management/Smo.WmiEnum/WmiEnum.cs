using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	public enum ServiceStartMode
	{
		Boot = 0,
		System,
		Auto,
		Manual,
		Disabled
	}

    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ServiceState
	{
		Stopped = 1,
		StartPending,
		StopPending,
		Running,
		ContinuePending,
		PausePending,
		Paused,
		Unknown
	}

	public enum ServiceErrorControl
	{
		Ignore = 0,		// User is not notified.
		Normal,		// User is notified.
		Severe,		// System is restarted with the last-known-good configuration.
		Critical,		// System attempts to restart with a good configuration.
		Unknown 	// Identifies a unknown value
	}

    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ManagedServiceType
	{
		SqlServer = 1,
		SqlAgent = 2,
		Search = 3,
		SqlServerIntegrationService = 4,
		AnalysisServer = 5,
		ReportServer = 6,
		SqlBrowser = 7,
		NotificationServer = 8
	}



	public enum PropertyType
	{
		StringValue = 0,
		NumericValue,
		FlagValue
	}
}
