// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;

//1591 is for verifying all public members have xml documentation. It's really heavy handed though - requiring
//docs for things as specific as each enum value. Disabling since the documentation we have is enough. 
#pragma warning disable 1591

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// type converter for QueryStoreOperationMode
    /// </summary>
    public class QueryStoreOperationModeConverter : EnumToDisplayNameConverter
    {
        public QueryStoreOperationModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreOperationMode))
        { }
    }

    /// <summary>
    /// type converter for QueryStoreCaptureMode
    /// </summary>
    public class QueryStoreCaptureModeConverter : EnumToDisplayNameConverter
    {
        public QueryStoreCaptureModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreCaptureMode))
        { }
    }

    /// <summary>
    /// type converter for QueryStoreSizeBasedCleanupMode
    /// </summary>
    public class QueryStoreSizeBasedCleanupModeConverter : EnumToDisplayNameConverter
    {
        public QueryStoreSizeBasedCleanupModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreSizeBasedCleanupMode))
        { }

        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">An optional System.Globalization.CultureInfo. If not supplied, the current culture is assumed.</param>
        /// <param name="value">The System.Object to convert.</param>
        /// <param name="destinationType">The System.Type to convert the value to.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value == null)
            {
                return string.Empty;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// type converter for QueryStoreWaitStatsCaptureMode
    /// </summary>
    public class QueryStoreWaitStatsCaptureModeConverter : EnumToDisplayNameConverter
    {
        public QueryStoreWaitStatsCaptureModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreWaitStatsCaptureMode))
        { }

        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">An optional System.Globalization.CultureInfo. If not supplied, the current culture is assumed.</param>
        /// <param name="value">The System.Object to convert.</param>
        /// <param name="destinationType">The System.Type to convert the value to.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value == null)
            {
                return string.Empty;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// Operation Mode values for Query Store
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreOperationModeConverter))]
    public enum QueryStoreOperationMode
    {
        [LocDisplayName("Off")]
        [TsqlSyntaxString("OFF")]
        Off = 0,
        [LocDisplayName("ReadOnly")]
        [TsqlSyntaxString("READ_ONLY")]
        ReadOnly = 1,
        [LocDisplayName("ReadWrite")]
        [TsqlSyntaxString("READ_WRITE")]
        ReadWrite = 2,
        [LocDisplayName("Error")]
        Error = 3,
    }

    /// <summary>
    /// Capture Mode values for Query Store
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreCaptureModeConverter))]
    public enum QueryStoreCaptureMode
    {
        [LocDisplayName("All")]
        [TsqlSyntaxString("ALL")]
        All = 1,
        [LocDisplayName("Auto")]
        [TsqlSyntaxString("AUTO")]
        Auto = 2,
        [LocDisplayName("None")]
        [TsqlSyntaxString("NONE")]
        None = 3,
        [LocDisplayName("Custom")]
        [TsqlSyntaxString("CUSTOM")]
        Custom = 4,
    }

    /// <summary>
    /// Size Based Cleanup Mode values for Query Store
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreSizeBasedCleanupModeConverter))]
    public enum QueryStoreSizeBasedCleanupMode
    {
        [LocDisplayName("Off")]
        [TsqlSyntaxString("OFF")]
        Off = 0,
        [LocDisplayName("Auto")]
        [TsqlSyntaxString("AUTO")]
        Auto = 1,
    }

    /// <summary>
    /// Wait Statistics Capture Mode values for Query Store
    /// </summary>
    [TypeConverter(typeof(Microsoft.SqlServer.Management.Smo.QueryStoreWaitStatsCaptureModeConverter))]
    public enum QueryStoreWaitStatsCaptureMode
    {
        [LocDisplayName("Off")]
        [TsqlSyntaxString("OFF")]
        Off = 0,
        [LocDisplayName("On")]
        [TsqlSyntaxString("ON")]
        On = 1,
    }


}


#pragma warning restore 1591