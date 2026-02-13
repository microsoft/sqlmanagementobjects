// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Instance class encapsulating SQL Server Configuration object
    /// </summary>
    public class Configuration : ConfigurationBase
{
    internal Configuration( Server server ) : base(server)
    {
    }

    ConfigPropertyCollection m_prop;
    public ConfigPropertyCollection Properties
    {
        get
        {
            if( null == m_prop )
            {
                m_prop = new ConfigPropertyCollection(this);
            }
            return m_prop;
        }
    }

    /// To get Config Properties' Configuration IDs run the following commands 
    /// replacing the like condition to find the configuration you need. 
    /// select configuration_id, name, description 
    /// from sys.configurations 
    /// where  name LIKE '%checksum%'

    /// <summary>
    /// Gets a ConfigProperty object that is used to enable or disable contained
    /// databases and authentication across the service instance.
    /// </summary>
    public ConfigProperty ContainmentEnabled
    {
        get
        {
            if (VersionUtils.IsSql11OrLater(this.Parent.ServerVersion))
            {
                return new ConfigProperty(this, 16393);
            }
            //Containment is not supported for Version Earlier than Denali(SQL11)
            throw new UnsupportedVersionException(ExceptionTemplates.ContainmentNotSupported(this.Parent.ServerVersion.ToString()));
        }
    }

    public ConfigProperty RecoveryInterval
    {
        get
        {
            return new ConfigProperty( this, 101 );
        }
    }

    public ConfigProperty AllowUpdates
    {
        get
        {
            return new ConfigProperty( this, 102 );
        }
    }

    public ConfigProperty UserConnections
    {
        get
        {
            return new ConfigProperty( this, 103 );
        }
    }

    public ConfigProperty Locks
    {
        get
        {
            return new ConfigProperty( this, 106 );
        }
    }

    public ConfigProperty OpenObjects
    {
        get
        {
            return new ConfigProperty( this, 107 );
        }
    }

    public ConfigProperty FillFactor
    {
        get
        {
            return new ConfigProperty( this, 109 );
        }
    }

    public ConfigProperty NestedTriggers
    {
        get
        {
            return new ConfigProperty( this, 115 );
        }
    }

    public ConfigProperty RemoteAccess
    {
        get
        {
            return new ConfigProperty( this, 117 );
        }
    }

    public ConfigProperty DefaultLanguage
    {
        get
        {
            return new ConfigProperty( this, 124 );
        }
    }

    public ConfigProperty DefaultTraceEnabled
    {
        get
        {
            return new ConfigProperty(this, 1568);
        }
    }

    public ConfigProperty CrossDBOwnershipChaining
    {
        get
        {
            return new ConfigProperty( this, 400 );
        }
    }

    public ConfigProperty MaxWorkerThreads
    {
        get
        {
            return new ConfigProperty( this, 503 );
        }
    }

    public ConfigProperty NetworkPacketSize
    {
        get
        {
            return new ConfigProperty( this, 505 );
        }
    }

    public ConfigProperty ShowAdvancedOptions
    {
        get
        {
            return new ConfigProperty( this, 518 );
        }
    }

    public ConfigProperty RemoteProcTrans
    {
        get
        {
            return new ConfigProperty( this, 542 );
        }
    }

    public ConfigProperty C2AuditMode
    {
        get
        {
            return new ConfigProperty( this, 544 );
        }
    }

    public ConfigProperty DefaultFullTextLanguage
    {
        get
        {
            return new ConfigProperty( this, 1126 );
        }
    }

    public ConfigProperty TwoDigitYearCutoff
    {
        get
        {
            return new ConfigProperty( this, 1127 );
        }
    }

    public ConfigProperty IndexCreateMemory
    {
        get
        {
            return new ConfigProperty( this, 1505 );
        }
    }

    public ConfigProperty PriorityBoost
    {
        get
        {
            return new ConfigProperty( this, 1517 );
        }
    }

    public ConfigProperty RemoteLoginTimeout
    {
        get
        {
            return new ConfigProperty( this, 1519 );
        }
    }

    public ConfigProperty RemoteQueryTimeout
    {
        get
        {
            return new ConfigProperty( this, 1520 );
        }
    }

    public ConfigProperty CursorThreshold
    {
        get
        {
            return new ConfigProperty( this, 1531 );
        }
    }

    public ConfigProperty SetWorkingSetSize
    {
        get
        {
            return new ConfigProperty( this, 1532 );
        }
    }

    public ConfigProperty UserOptions
    {
        get
        {
            return new ConfigProperty( this, 1534 );
        }
    }

    public ConfigProperty AffinityMask
    {
        get
        {
            return new ConfigProperty( this, 1535 );
        }
    }

    public ConfigProperty ReplicationMaxTextSize
    {
        get
        {
            return new ConfigProperty( this, 1536 );
        }
    }

    public ConfigProperty MediaRetention
    {
        get
        {
            return new ConfigProperty( this, 1537 );
        }
    }

    public ConfigProperty CostThresholdForParallelism
    {
        get
        {
            return new ConfigProperty( this, 1538 );
        }
    }

    public ConfigProperty MaxDegreeOfParallelism
    {
        get
        {
            return new ConfigProperty( this, 1539 );
        }
    }

    public ConfigProperty MinMemoryPerQuery
    {
        get
        {
            return new ConfigProperty( this, 1540 );
        }
    }

    public ConfigProperty QueryWait
    {
        get
        {
            return new ConfigProperty( this, 1541 );
        }
    }

    public ConfigProperty MinServerMemory
    {
        get
        {
            return new ConfigProperty( this, 1543 );
        }
    }

    public ConfigProperty MaxServerMemory
    {
        get
        {
            return new ConfigProperty( this, 1544 );
        }
    }

    public ConfigProperty QueryGovernorCostLimit
    {
        get
        {
            return new ConfigProperty( this, 1545 );
        }
    }

    public ConfigProperty LightweightPooling
    {
        get
        {
            return new ConfigProperty( this, 1546 );
        }
    }

    public ConfigProperty ScanForStartupProcedures
    {
        get
        {
            return new ConfigProperty( this, 1547 );
        }
    }
    [Obsolete]
    public ConfigProperty AweEnabled
    {
        get
        {
            if (!VersionUtils.IsSql11OrLater(this.Parent.ServerVersion))
            {
                return new ConfigProperty(this, 1548);
            }
            throw new UnsupportedVersionException(ExceptionTemplates.AweEnabledNotSupported(this.Parent.ServerVersion.Major.ToString(SmoApplication.DefaultCulture)));
        }
    }

    public ConfigProperty Affinity64Mask
    {
        get
        {
            return new ConfigProperty( this, 1549 );
        }
    }

    public ConfigProperty AffinityIOMask
    {
        get
        {
            return new ConfigProperty( this, 1550 );
        }
    }

    public ConfigProperty TransformNoiseWords
    {
        get
        {
            return new ConfigProperty( this, 1555 );
        }
    }

    public ConfigProperty PrecomputeRank
    {
        get
        {
            return new ConfigProperty( this, 1556 );
        }
    }

    public ConfigProperty ProtocolHandlerTimeout
    {
        get
        {
            return new ConfigProperty( this, 1557 );
        }
    }

    /// This option allows the ability to run user assemblies in SQL server
    public ConfigProperty IsSqlClrEnabled
    {
        get
        {
            return new ConfigProperty( this, 1562 );
        }
    }


    /// This option opens the admin port to be callable over the network. By default the DAC port 
    /// listens only on the loopback address. 
    public ConfigProperty RemoteDacConnectionsEnabled
    {
        get
        {
            return new ConfigProperty(this, 1576);
        }
    }

    /// <summary>
    /// The "common criteria compliance enabled" server option
    /// </summary>
    public ConfigProperty CommonCriteriaComplianceEnabled
    {
        get
        {
            return new ConfigProperty(this, 1577);
        }
    }

    /// <summary>
    /// The "EKM provider enabled" option is to enable creation of Cryptographic Providers
    /// </summary>
    public ConfigProperty ExtensibleKeyManagementEnabled
    {
        get
        {
            return new ConfigProperty(this, 1578);
        }
    }

    /// <summary>
    /// This public property gets or sets the default backup compression value and returns
    /// a ConfigProperty object representing 'backup compression default' server configuration.
    /// </summary>
    public ConfigProperty DefaultBackupCompression
    {
        get
        {
            return new ConfigProperty(this, 1579);
        }
    }

    /// <summary>
    /// This public property gets or sets the filestream access level and returns
    /// a ConfigProperty object representing 'filestream access level' server configuration.
    /// </summary>
    public ConfigProperty FilestreamAccessLevel
    {
        get
        {
            return new ConfigProperty(this, 1580);
        }
    }

    /// <summary>
    /// This public property gets or sets the optimize for ad hoc workloads and returns
    /// a ConfigProperty object representing 'optimize for ad hoc workloads' server configuration.
    /// </summary>
    public ConfigProperty OptimizeAdhocWorkloads
    {
        get
        {
            return new ConfigProperty(this, 1581);
        }
    }

    /// <summary>
    /// This public property gets or sets the default backup checksum value and returns
    /// a ConfigProperty object representing 'backup checksum default' server configuration.
    /// </summary>
    public ConfigProperty DefaultBackupChecksum
    {
        get
        {
            return new ConfigProperty(this, 1584);
        }
    }

    /// This is the set of XPs that the agent component requires for its functioning. 
    /// If the Agent service is turned on it will turn on this option automatically. 
    /// If you are not using the agent component, you can keep this option off.
    public ConfigProperty AgentXPsEnabled
    {
        get
        {
            return new ConfigProperty( this, 16384 );
        }
    }

    /// OpenRowset and Opendatasource allow connecting to and accessing remote data using OLE DB 
    /// in an ad hoc manner without requiring the creation of linked servers. This option enables 
    /// or disables the ability to invoke these two functions
    public ConfigProperty AdHocDistributedQueriesEnabled

    {
        get
        {
            return new ConfigProperty( this, 16391 );
        }
    }

    /// Xp_cmdshell executes a given command string as an operating-system command shell and returns 
    /// any output as rows of text. This option enables or disables the ability to call xp_cmdshell
    public ConfigProperty XPCmdShellEnabled
    {
        get
        {
            return new ConfigProperty( this, 16390 );
        }
    }

    /// This is a set of XPs that are required by SMO and DMO for its functioning. And are required by 
    /// the SQL server management studio and profiler as well. For that reason, this option can only be 
    /// changed by calling sp_configure directly
    public ConfigProperty SmoAndDmoXPsEnabled
    {
        get
        {
            return new ConfigProperty( this, 16387 );
        }
    }

    /// SQL Mail provides a way to send and receive e-mail messages from SQL server using a MAPI profile. 
    /// Use this option to turn on or off the set of XPs used by SQLMail. However, SQLMail is being 
    /// deprecated by the new DatabaseMail functionality.
    public ConfigProperty SqlMailXPsEnabled
    {
        get
        {
            return new ConfigProperty( this, 16385 );
        }
    }

    /// DatabaseMail provides a way to send email messages over SMTP from SQL server. This option turns on or 
    /// off the ability to call the XPs used by DatabaseMail. Turning off this option will prevent the ability 
    /// to send email messages using DatabaseMail.
    public ConfigProperty DatabaseMailEnabled
    {
        get
        {
            return new ConfigProperty( this, 16386 );
        }
    }

    /// This option captures the ability to turn on or off the set of stored procedures that allow 
    /// standard OLE automation objects to be used within a standard Transact-SQL batch
    public ConfigProperty OleAutomationProceduresEnabled
    {
        get
        {
            return new ConfigProperty( this, 16388 );
        }
    }

    /// This option represents the set of XPs that are intended to be used by the replication component. 
    /// The replication component does not require this option to be turned on. However, turn this 
    /// component on to be able to invoke these XPs directly.
    public ConfigProperty ReplicationXPsEnabled
    {
        get
        {
            return new ConfigProperty( this, 16392 );
        }
    }

    /// Web Assitant XPs allows the server to serve HTML pages. This option allows the ability to 
    /// invoke these XPs.
    public ConfigProperty WebXPsEnabled
    {
        get
        {
            Version serverVersion = Parent.Version;
            //web assistant XPs have been removed since SQL Server 2008.
            throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersion(serverVersion.Major.ToString()));
        }
    }

    /// <summary>
    /// The blocked process threshold option specifies the 
    /// threshold, in seconds, at which blocked process reports 
    /// are generated. The threshold can be set from 0 to 86,400. 
    /// By default, no blocked process reports are produced.
    /// </summary>
    public ConfigProperty BlockedProcessThreshold
    {
        get
        {
            return new ConfigProperty(this, 1569);
        }
    }

    /// <summary>
    /// Binds SQL Server disk I/O to a specified subset of CPUs.  
    /// Use AffinityIOMask to bind the first 32 processors, and 
    /// use Affinity64IOMask to bind the remaining processors on 
    /// the computer.  Only available on 64-bit version of SQL Server.
    /// </summary>
    public ConfigProperty Affinity64IOMask
    {
        get
        {
            return new ConfigProperty(this, 1551);
        }
    }

    /// <summary>
    /// Use the disallow results from triggers option to control 
    /// whether triggers return result sets. Triggers that return 
    /// result sets may cause unexpected behavior in applications 
    /// that are not designed to work with them.
    /// When set to 1, the disallow results from triggers option 
    /// is set to ON. The default setting for this option is 0 (OFF).
    /// </summary>
    public ConfigProperty DisallowResultsFromTriggers
    {
        get
        {
            return new ConfigProperty(this, 114);
        }
    }

    /// <summary>
    /// Use FullTextCrawlBandwidth[min,max] options to specify 
    /// the size to which the pool of large memory buffers can 
    /// grow. Large memory buffers are 4 megabytes (MB) in size. 
    /// The FullTextCrawlBandwidthMin parameter specifies the 
    /// minimum number of memory buffers that must be maintained 
    /// in the pool of large memory buffers.  If, however, the min 
    /// value specified is zero, then all memory buffers are released.
    /// </summary>
    public ConfigProperty FullTextCrawlBandwidthMin
    {
        get
        {
            return new ConfigProperty(this, 1566);
        }
    }

    /// <summary>
    /// Use the FullTextCrawlBandwidth[min,max] options to 
    /// specify the size to which the pool of large memory buffers 
    /// can grow. Large memory buffers are 4 megabytes (MB) in size. 
    /// The FullTextCrawlBandwidthMax maximum number of buffers 
    /// that the full-text memory manager should maintain in a 
    /// large buffer pool.  If the max value is zero, then there 
    /// is no upper limit to the number of buffers that can be in 
    /// a large buffer pool.
    /// </summary>
    public ConfigProperty FullTextCrawlBandwidthMax
    {
        get
        {
            return new ConfigProperty(this, 1567);
        }
    }

    /// <summary>
    /// Use the FullTextNotifyBandwidth[min, max] options to 
    /// specify the size to which the pool of small memory 
    /// buffers can grow. Small memory buffers are 64 kilobytes(KB)
    /// in size. The FullTextNotifyBandwidthMin parameter specifies 
    /// the minimum number of memory buffers that must be maintained 
    /// in the pool of small memory buffers. Upon request from the 
    /// Microsoft SQL Server memory manager, all extra buffer pools 
    /// will be released but this minimum number of buffers will be 
    /// maintained. If, however, the min value specified is zero, 
    /// then all memory buffers are released.
    /// </summary>
    public ConfigProperty FullTextNotifyBandwidthMin
    {
        get
        {
            return new ConfigProperty(this, 1564);
        }
    }

    /// <summary>
    /// Use the FullTextNotifyBandwidth[min, max] options to 
    /// specify the size to which the pool of small memory buffers 
    /// can grow. Small memory buffers are 64 kilobytes (KB) in size. 
    /// The FullTextNotifyBandwidthMax parameter value specifies the 
    /// maximum number of buffers that the full-text memory manager 
    /// should maintain in a small buffer pool. If the max value is 
    /// zero, then there is no upper limit to the number of buffers 
    /// that can be in a small buffer pool.
    /// </summary>
    public ConfigProperty FullTextNotifyBandwidthMax
    {
        get
        {
            return new ConfigProperty(this, 1565);
        }
    }

    /// <summary>
    /// Use the in-doubt xact resolution option to control the default 
    /// outcome of transactions that the Microsoft Distributed 
    /// Transaction Coordinator (MS DTC) is unable to resolve. 
    /// Inability to resolve transactions may be related to the 
    /// MS DTC down time or an unknown transaction outcome at the 
    /// time of recovery. 
    /// 0 - No presumption. Recovery fails if MS DTC cannot resolve any in-doubt transactions.
    /// 1 - Presume commit. Any MS DTC in-doubt transactions are presumed to have committed.
    /// 2 - Presume abort. Any MS DTC in-doubt transactions are presumed to have aborted.
    /// </summary>
    public ConfigProperty InDoubtTransactionResolution
    {
        get
        {
            return new ConfigProperty(this, 1570);
        }
    }

    /// <summary>
    /// Use the max full-text crawl range option to optimize CPU 
    /// utilization, which improves crawl performance during a full 
    /// crawl. Using this option, you can specify the number of 
    /// partitions that Microsoft SQL Server should use during a 
    /// full index crawl. For example, if there are many CPUs and 
    /// their utilization is not optimal, you can increase the 
    /// maximum value of this option. In addition to this option, 
    /// SQL Server uses a number of other factors, such as the number 
    /// of rows in the table and the number of CPUs, to determine 
    /// the actual number of partitions used. 
    /// </summary>
    public ConfigProperty FullTextCrawlRangeMax
    {
        get
        {
            return new ConfigProperty(this, 1563);
        }
    }


    /// <summary>
    /// Use the server trigger recursion option to specify whether 
    /// to allow server-level triggers to fire recursively. When 
    /// this option is set to 1 (ON), server-level triggers will be 
    /// allowed to fire recursively. When set to 0 (OFF), 
    /// server-level triggers cannot be fired recursively. Only 
    /// direct recursion is prevented when the server trigger 
    /// recursion option is set to 0 (OFF). (To disable indirect 
    /// recursion, set the nested triggers option to 0.) The 
    /// default value for this option is 1 (ON).
    /// </summary>
    public ConfigProperty ServerTriggerRecursionEnabled
    {
        get
        {
            return new ConfigProperty(this, 116);
        }
    }

    /// <summary>
    /// The User Instance Timeout option that you can access 
    /// through sp_configure is not supported in Microsoft 
    /// SQL Server 2005. This option works only with 
    /// SQL Server 2005 Express Edition (SQL Server Express). 
    /// </summary>
    public ConfigProperty UserInstanceTimeout
    {
        get
        {
            if (Parent.IsExpressSku())
            {
                return new ConfigProperty(this, 1573);
            }
            throw new UnsupportedFeatureException(ExceptionTemplates.UnsupportedFeature("UserInstanceTimeout"));
        }
    }

    /// <summary>
    /// The user instance enabled option that you can access 
    /// through sp_configure is not supported in 
    /// Microsoft SQL Server 2005. This option works only 
    /// with SQL Server 2005 Express Edition (SQL Server Express). 
    /// </summary>
    public ConfigProperty UserInstancesEnabled
    {
        get
        {
            if (Parent.IsExpressSku())
            {
                return new ConfigProperty(this, 1575);
            }
            throw new UnsupportedFeatureException(ExceptionTemplates.UnsupportedFeature("UserInstancesEnabled"));
        }
    }

    /// <summary>
    /// Use the remote data archive option to check whether databases and tables on the server can be enabled for Stretch
    /// https://msdn.microsoft.com/en-us/library/mt143175.aspx
    /// </summary>
    public ConfigProperty RemoteDataArchiveEnabled
    {
        get
        {
            return new ConfigProperty(this, 16396);
        }
    }
}
}

