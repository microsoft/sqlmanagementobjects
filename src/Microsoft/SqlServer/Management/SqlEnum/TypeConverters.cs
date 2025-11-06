// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Smo.SqlEnum;

namespace Microsoft.SqlServer.Management.Smo
{

    /// <summary>
    /// Attribute class that is used in converters below to convert Enum to Resource strings
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class LocDisplayNameAttribute : DisplayNameAttribute
    {
        string name;

        public LocDisplayNameAttribute(string name)
        {
            this.name = name;
        }

        public override string DisplayName
        {
            get
            {
                string result = StringSqlEnumerator.Keys.GetString(name);
                if (result == null)
                {
                    result = this.name;
                }
                return result;
            }
        }
    }

    /// <summary>
    /// Attribute class that is used in converters below to convert Enum to culture invariant TSQL Strings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class TsqlSyntaxStringAttribute : DisplayNameAttribute
    {
        string syntaxString;

        public TsqlSyntaxStringAttribute(string syntaxString)
        {
            this.syntaxString = syntaxString;
        }

        public override string DisplayName
        {
            get
            {
                return syntaxString;
            }
        }
    }

        
    /// <summary>
    ///  Converts the specified value object to an enumeration object.
    /// </summary>
    public abstract class EnumToDisplayNameConverter : EnumConverter
    {

        Type type = null;

         /// <summary>
        /// </summary>
         protected EnumToDisplayNameConverter(Type type) 
                    :base(type)
        {
            this.type = type;
        }

        
        /// <summary>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }


        private object LocalizedStringToEnumValue(string value, Type enumType, CultureInfo culture)
        {
            CultureInfo cultureLocal = CultureInfo.CurrentCulture;
            if (culture != null)
            {
                cultureLocal = culture;
            }
            foreach (MemberInfo member in enumType.GetMembers())
            {
                //VSTS 537608:first try if member name matches to value;ignore case
                if (0 == string.Compare(member.Name, value, true, cultureLocal))
                {
                    return (Enum.Parse(enumType, member.Name, true));
                }

                object[] attributes = SqlEnumNetCoreExtension.GetCustomAttributes(member, typeof(LocDisplayNameAttribute), true);
                foreach (LocDisplayNameAttribute attribute in attributes)
                {
                    if (0 == string.Compare(attribute.DisplayName, value, true, cultureLocal))
                    {
                        return (Enum.Parse(enumType, member.Name, true));
                    }
                }

            }

            return Enum.Parse(enumType, value, true);
        }


        /// <summary>
        ///  Converts the specified value object to an enumeration object.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">An optional System.Globalization.CultureInfo. If not supplied, the current culture is assumed.</param>
        /// <param name="value"> The System.Object to convert.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            string val = value as string;

            if (!string.IsNullOrEmpty(val))
            {
                return this.LocalizedStringToEnumValue(val, this.type, culture);
            }

            return base.ConvertFrom(context, culture, value);
        }


        /// <summary>
        /// Helper method that converts the given value object to string.
        /// </summary>
        private string EnumValueToLocString(string enumMemberName, Type enumType)
        {
            foreach (MemberInfo member in enumType.GetMembers())
            {
                if (member.Name == enumMemberName)
                {
                    object[] attributes = SqlEnumNetCoreExtension.GetCustomAttributes(member, typeof(LocDisplayNameAttribute), true);
                    foreach (LocDisplayNameAttribute attribute in attributes)
                    {
                        return attribute.DisplayName;
                    }

                    break;
                }
            }

            return enumMemberName;
        }

        /// <summary>
        /// Helper method that converts enum, to the equivalent TSQL Syntax string if it exists, null otherwise.
        /// </summary>
        private string EnumValueToTsqlSyntax(string enumMemberName, Type enumType)
        {
            foreach (MemberInfo member in enumType.GetMembers())
            {
                if (member.Name == enumMemberName)
                {
                    object[] attributes = SqlEnumNetCoreExtension.GetCustomAttributes(member, typeof(TsqlSyntaxStringAttribute), true);
                    foreach (TsqlSyntaxStringAttribute attribute in attributes)
                    {
                        return attribute.DisplayName;
                    }

                    // Found the enum member, but it did not have a TsqlSyntaxStringAttribute specified.
                    //
                    break;
                }
            }

            return null;
        }

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
            if (destinationType == typeof(string))
            {
                // If the culture info is invariant culture, attempt to find the TSQL syntax string attribute, otherwise return the localized string.
                //
                if (culture == CultureInfo.InvariantCulture)
                {
                    string tsqlSyntaxString = this.EnumValueToTsqlSyntax(value.ToString(), this.type);
                    if (tsqlSyntaxString != null)
                    {
                        return tsqlSyntaxString;
                    }
                }
                
                return this.EnumValueToLocString(value.ToString(), this.type);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }



    /// <summary>
    /// Type converter for JobExecutionStatus
    /// </summary>
    internal class JobExecutionStatusConverter : EnumToDisplayNameConverter
    {


        /// <summary>
        /// ctor for Type converter for JobExecutionStatus
        /// </summary>
        public JobExecutionStatusConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.Agent.JobExecutionStatus))
        {
        }

    }

    /// <summary>
    /// Type converter for ContainmentType
    /// </summary>
    internal class ContainmentTypeConverter : EnumToDisplayNameConverter
    {

        /// <summary>
        /// ctor for Type converter for ContainmentType
        /// </summary>
        public ContainmentTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ContainmentType))
        {
        }

    }

    /// <summary>
    /// Type converter for RecoveryModel
    /// </summary>
    internal class RecoveryModelConverter : EnumToDisplayNameConverter
    {

        /// <summary>
        /// ctor for Type converter for RecoveryModel
        /// </summary>
        public RecoveryModelConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.RecoveryModel))
        {
        }

    }

    /// <summary>
    /// Type converter for MirroringStatus
    /// </summary>
    internal class MirroringStatusConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for Type converter for MirroringStatus
        /// </summary>
            public MirroringStatusConverter()
                     : base(typeof(Microsoft.SqlServer.Management.Smo.MirroringStatus))
            {}

    }

    /// <summary>
    /// Type converter for AvailabilityGroupRollupSynchronizationState
    /// </summary>
    internal class AvailabilityGroupRollupSynchronizationStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for the AvailabilityReplicaRollupSynchronizationStateConverter class
        /// </summary>
        public AvailabilityGroupRollupSynchronizationStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupRollupSynchronizationState))
        {
        }
    }

    /*
     * Commented until the connection director work is implemented
    /// <summary>
    /// Type converter for AvailabilityGroupVirtualNameHealth
    /// </summary>
    internal class AvailabilityGroupVirtualNameHealthConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for the AvailabilityGroupVirtualNameHealthConverter class
        /// </summary>
        public AvailabilityGroupVirtualNameHealthConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupVirtualNameHealth))
        {
        }
    }
    */

    /// <summary>
    /// Type converter for AvailabilityReplicaOperationalState
    /// </summary>
    internal class AvailabilityReplicaOperationalStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for the AvailabilityReplicaOperationalStateConverter class
        /// </summary>
        public AvailabilityReplicaOperationalStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaOperationalState))
        {
        }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaRollupRecoveryState
    /// </summary>
    internal class AvailabilityReplicaRollupRecoveryStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for the AvailabilityReplicaRollupRecoveryStateConverter class
        /// </summary>
        public AvailabilityReplicaRollupRecoveryStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaRollupRecoveryState))
        {
        }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaRollupSynchronizationState
    /// </summary>
    internal class AvailabilityReplicaRollupSynchronizationStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for the AvailabilityReplicaRollupSynchronizationStateConverter class
        /// </summary>
        public AvailabilityReplicaRollupSynchronizationStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaRollupSynchronizationState))
        {
        }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaRole
    /// </summary>
    internal class AvailabilityReplicaRoleConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for Type converter for AvailabilityReplicaRole
        /// </summary>
        public AvailabilityReplicaRoleConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaRole))
        { }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaConnectionState
    /// </summary>
    internal class AvailabilityReplicaConnectionStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for Type converter for AvailabilityReplicaConnectionState
        /// </summary>
        public AvailabilityReplicaConnectionStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaConnectionState))
        { }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaConnectionModeInSecondaryRole enum
    /// </summary>
    internal class AvailabilityReplicaConnectionModeInSecondaryRoleConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AvailabilityReplicaConnectionModeInSecondaryRoleConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaConnectionModeInSecondaryRole))
        { }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaSeedingMode enum
    /// </summary>
    internal class AvailabilityReplicaSeedingModeConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AvailabilityReplicaSeedingModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaSeedingMode))
        { }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaConnectionModeInPrimaryRole enum
    /// </summary>
    internal class AvailabilityReplicaConnectionModeInPrimaryRoleConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AvailabilityReplicaConnectionModeInPrimaryRoleConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaConnectionModeInPrimaryRole))
        { }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaAvailabilityMode enum
    /// </summary>
    internal class AvailabilityReplicaAvailabilityModeConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AvailabilityReplicaAvailabilityModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaAvailabilityMode))
        { }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaFailoverMode enum
    /// </summary>
    internal class AvailabilityReplicaFailoverModeConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AvailabilityReplicaFailoverModeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaFailoverMode))
        { }
    }

    /// <summary>
    /// Type converter for AvailabilityReplicaJoinState enum
    /// </summary>
    internal class AvailabilityReplicaJoinStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AvailabilityReplicaJoinStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityReplicaJoinStateConverter))
        { }
    }

    /// <summary>
    /// Type converter for AvailabilityDatabaseSynchronizationState enum
    /// </summary>
    internal class AvailabilityDatabaseSynchronizationStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for Type converter for AvailabilityDatabaseSynchronizationState
        /// ctor for the AvailabilityReplicaRollupSynchronizationStateConverter class
        /// </summary>
        public AvailabilityDatabaseSynchronizationStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityDatabaseSynchronizationState))
        {}
    }

    /// <summary>
    /// Type converter for AvailabilityGroupListenerIPState
    /// </summary>
    internal class AvailabilityGroupListenerIPStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// ctor for the AvailabilityGroupListenerIPState class
        /// </summary>
        public AvailabilityGroupListenerIPStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupListenerIPState))
        {
        }
    }
    /// <summary>
    /// Type converter for DatabaseReplicaSuspendReason enum
    /// </summary>
    internal class DatabaseReplicaSuspendReasonConverter : EnumToDisplayNameConverter
    {
        public DatabaseReplicaSuspendReasonConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.DatabaseReplicaSuspendReason))
        {}
    }

    /// <summary>
    /// Type converter for HADRMangerStatus enum
    /// </summary>
    internal class HADRManagerStatusConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public HADRManagerStatusConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.HadrManagerStatus))
        { }
    }

    /// <summary>
    /// Type converter for the ClusterQuorumType enum
    /// </summary>
    internal class ClusterQuorumTypeConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ClusterQuorumTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ClusterQuorumType))
        { }
    }

    /// <summary>
    /// Type converter for the ClusterQuorumState enum
    /// </summary>
    internal class ClusterQuorumStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ClusterQuorumStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ClusterQuorumState))
        { }
    }

    /// <summary>
    /// Type converter for the ClusterMemberType enum
    /// </summary>
    internal class ClusterMemberTypeConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ClusterMemberTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ClusterMemberType))
        { }
    }

    /// <summary>
    /// Type converter for the ClusterMemberState enum
    /// </summary>
    internal class ClusterMemberStateConverter : EnumToDisplayNameConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ClusterMemberStateConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ClusterMemberState))
        { }
    }

    /// <summary>
    /// Type converter for the AvailabilityGroupAutomatedBackupPreference enum
    /// </summary>
    internal class AvailabilityGroupAutomatedBackupPreferenceConverter : EnumToDisplayNameConverter
    {
        public AvailabilityGroupAutomatedBackupPreferenceConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupAutomatedBackupPreference))
        { }
    }

    /// <summary>
    /// Type converter for the AvailabilityGroupFailureConditionLevel enum
    /// </summary>
    internal class AvailabilityGroupFailureConditionLevelConverter : EnumToDisplayNameConverter
    {
        public AvailabilityGroupFailureConditionLevelConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupFailureConditionLevel))
        { }
    }

    /// <summary>
    /// Type converter for the AvailabilityGroupClusterType enum
    /// </summary>
    internal class AvailabilityGroupClusterTypeConverter : EnumToDisplayNameConverter
    {
        public AvailabilityGroupClusterTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AvailabilityGroupClusterType))
        { }
    }

    internal class FileGroupTypeConverter : EnumToDisplayNameConverter
    {
        public FileGroupTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.FileGroupType))
        { }
    }

    public class SecurityPredicateTypeConverter : EnumToDisplayNameConverter
    {
        public SecurityPredicateTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.SecurityPredicateType))
        { }
    }

    public class SecurityPredicateOperationConverter : EnumToDisplayNameConverter
    {
        public SecurityPredicateOperationConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.SecurityPredicateOperation))
        { }
    }

    public class DatabaseScopedConfigurationOnOffConverter : EnumToDisplayNameConverter
    {
        public DatabaseScopedConfigurationOnOffConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.DatabaseScopedConfigurationOnOff))
        { }
    }

    public class ResumableOperationStateTypeConverter : EnumToDisplayNameConverter
    {
        public ResumableOperationStateTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ResumableOperationStateType))
        { }
    }

    public class AbortAfterWaitConverter : EnumToDisplayNameConverter
    {
        public AbortAfterWaitConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.AbortAfterWait))
        { }
    }

    public class ExternalDataSourcePushdownOptionConverter : EnumToDisplayNameConverter
    {
        public ExternalDataSourcePushdownOptionConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ExternalDataSourcePushdownOption))
        { }
    }

    public class ExternalLanguageFilePlatformConverter : EnumToDisplayNameConverter
    {
        public ExternalLanguageFilePlatformConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ExternalLanguageFilePlatform))
        { }
    }

    public class ExternalDataSourceTypeConverter : EnumToDisplayNameConverter
    {
        public ExternalDataSourceTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ExternalDataSourceType))
        { }
    }

    public class ExternalTableDistributionConverter : EnumToDisplayNameConverter
    {
        public ExternalTableDistributionConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ExternalTableDistributionType))
        { }
    }

    public class ExternalTableRejectTypeConverter : EnumToDisplayNameConverter
    {
        public ExternalTableRejectTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ExternalTableRejectType))
        { }
    }

    public class ExternalFileFormatTypeConverter : EnumToDisplayNameConverter
    {
        public ExternalFileFormatTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ExternalFileFormatType))
        { }
    }

    public class ExternalStreamingJobStatusTypeConverter : EnumToDisplayNameConverter
    {
        public ExternalStreamingJobStatusTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.ExternalStreamingJobStatusType))
        { }
    }

    public class DwTableDistributionConverter : EnumToDisplayNameConverter
    {
        public DwTableDistributionConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.DwTableDistributionType))
        { }
    }

    public class DwViewDistributionConverter : EnumToDisplayNameConverter
    {
        public DwViewDistributionConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.DwViewDistributionType))
        { }
    }

    public class IndexTypeConverter : EnumToDisplayNameConverter
    {
        public IndexTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.IndexType))
        { }
    }

    public class RangeTypeConverter : EnumToDisplayNameConverter
    {
        public RangeTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.RangeType))
        { }
    }

    public class TemporalHistoryRetentionPeriodUnitTypeConverter : EnumToDisplayNameConverter
    {
        public TemporalHistoryRetentionPeriodUnitTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.TemporalHistoryRetentionPeriodUnit))
        { }
    }

    public class DataRetentionPeriodUnitTypeConverter : EnumToDisplayNameConverter
    {
        public DataRetentionPeriodUnitTypeConverter()
            : base(typeof(Microsoft.SqlServer.Management.Smo.DataRetentionPeriodUnit))
        { }
    }

    public class AgentSubSystemTypeConverter : EnumToDisplayNameConverter
    {
        public AgentSubSystemTypeConverter() : base(typeof(AgentSubSystem))
        {
        }
    }

    public class CatalogCollationTypeConverter : EnumToDisplayNameConverter
    {
        public CatalogCollationTypeConverter() : base(typeof(CatalogCollationType))
        {
        }
    }

    public class AuditDestinationTypeConverter : EnumToDisplayNameConverter
    {
        public AuditDestinationTypeConverter() : base(typeof(AuditDestinationType))
        { }
    }

    public class AuditOnFailureActionConverter : EnumToDisplayNameConverter
    {
        public AuditOnFailureActionConverter() : base(typeof(OnFailureAction))
        { }
    }

    public class AuditActionTypeConverter : EnumToDisplayNameConverter
    {
        public AuditActionTypeConverter() : base(typeof(AuditActionType))
        { }
    }

    public class SensitivityRankConverter : EnumToDisplayNameConverter
    {
        public SensitivityRankConverter() : base(typeof(SensitivityRank))
        { }
    }

    public class WorkloadManagementImportanceConverter : EnumToDisplayNameConverter
    {
        public WorkloadManagementImportanceConverter() : base(typeof(WorkloadManagementImportance))
        { }
    }
}
