// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SqlServer.Management.Smo.Broker
{

    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum DialogEndPointState
	{
		Open = 1,
		OpenWait = 2,
		Disabled = 6,
		ClosingWait = 7,
		ErrorWait = 8,
		ClosedWait = 9
	}

	public enum DialogType
	{
		Regular2Way = 0,
		MonologPublish,
		MonologReceive
	}

	public enum MessageTypeValidation
	{
		None = 0,
        XmlSchemaCollection,
		Empty,
		Xml
	}

	public enum MessageSource
	{
		Initiator = 0,
		Target,
		InitiatorAndTarget
	}
}
