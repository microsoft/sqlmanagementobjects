/****** Object:  Table [dbo].[Person_Temporal_History]    Script Date: 12/9/2015 5:28:18 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Person_Temporal_History](
    [BusinessEntityID] [int] NOT NULL,
    [PersonType] [nchar](2) NOT NULL,
    [Title] [nvarchar](8) NULL,
    [FirstName] nvarchar(64) NOT NULL,
    [MiddleName] nvarchar(64) NULL,
    [LastName] nvarchar(64) NOT NULL,
    [Suffix] [nvarchar](10) NULL,
    [EmailPromotion] [int] NOT NULL,
    [ValidFrom] [datetime2](7) NOT NULL,
    [ValidTo] [datetime2](7) NOT NULL
) ON [PRIMARY]

GO
/****** Object:  Index [ix_Person_Temporal_History]    Script Date: 12/9/2015 5:28:19 PM ******/
CREATE CLUSTERED INDEX [ix_Person_Temporal_History] ON [dbo].[Person_Temporal_History]
(
    [BusinessEntityID] ASC,
    [ValidFrom] ASC,
    [ValidTo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE VIEW vwPerson_Temporal_History
AS
SELECT TOP 1 [LastName]
FROM [dbo].[Person_Temporal_History]

GO

/****** Object:  Table [dbo].[Person_Temporal]    Script Date: 12/9/2015 5:28:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Person_Temporal](
    [BusinessEntityID] [int] NOT NULL,
    [PersonType] [nchar](2) NOT NULL,
    [Title] [nvarchar](8) NULL,
    [FirstName] nvarchar(64) NOT NULL,
    [MiddleName] nvarchar(64) NULL,
    [LastName] nvarchar(64) NOT NULL,
    [Suffix] [nvarchar](10) NULL,
    [EmailPromotion] [int] NOT NULL,
    [ValidFrom] [datetime2](7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [ValidTo] [datetime2](7) GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
 CONSTRAINT [PK_Person_Temporal_BusinessEntityID] PRIMARY KEY CLUSTERED 
(
    [BusinessEntityID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
) ON [PRIMARY]
WITH
(
SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Person_Temporal_History] , DATA_CONSISTENCY_CHECK = ON )
)

GO
/****** Object:  Table [dbo].[Employee_Temporal_History]    Script Date: 12/9/2015 5:28:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Employee_Temporal_History](
    [BusinessEntityID] [int] NOT NULL,
    [NationalIDNumber] [nvarchar](15) NOT NULL,
    [LoginID] [nvarchar](256) NOT NULL,
    [OrganizationNode] [hierarchyid] NULL,
    [OrganizationLevel] [smallint] NULL,
    [JobTitle] [nvarchar](50) NOT NULL,
    [BirthDate] [date] NOT NULL,
    [MaritalStatus] [nchar](1) NOT NULL,
    [Gender] [nchar](1) NOT NULL,
    [HireDate] [date] NOT NULL,
    [VacationHours] [smallint] NOT NULL,
    [SickLeaveHours] [smallint] NOT NULL,
    [ValidFrom] [datetime2](7) NOT NULL,
    [ValidTo] [datetime2](7) NOT NULL
) ON [PRIMARY]

GO
/****** Object:  Index [ix_Employee_Temporal_History]    Script Date: 12/9/2015 5:28:21 PM ******/
CREATE CLUSTERED INDEX [ix_Employee_Temporal_History] ON [dbo].[Employee_Temporal_History]
(
    [BusinessEntityID] ASC,
    [ValidFrom] ASC,
    [ValidTo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Employee_Temporal]    Script Date: 12/9/2015 5:28:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Employee_Temporal](
    [BusinessEntityID] [int] NOT NULL,
    [NationalIDNumber] [nvarchar](15) NOT NULL,
    [LoginID] [nvarchar](256) NOT NULL,
    [OrganizationNode] [hierarchyid] NULL,
    [OrganizationLevel]  AS ([OrganizationNode].[GetLevel]()),
    [JobTitle] [nvarchar](50) NOT NULL,
    [BirthDate] [date] NOT NULL,
    [MaritalStatus] [nchar](1) NOT NULL,
    [Gender] [nchar](1) NOT NULL,
    [HireDate] [date] NOT NULL,
    [VacationHours] [smallint] NOT NULL,
    [SickLeaveHours] [smallint] NOT NULL,
    [ValidFrom] [datetime2](7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [ValidTo] [datetime2](7) GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
 CONSTRAINT [PK_Employee_History_BusinessEntityID] PRIMARY KEY CLUSTERED 
(
    [BusinessEntityID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
) ON [PRIMARY]
WITH
(
SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Employee_Temporal_History] , DATA_CONSISTENCY_CHECK = ON )
)

GO
/****** Object:  View [HumanResources].[vEmployeePersonTemporalInfo]    Script Date: 12/9/2015 5:28:22 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
    View that joins [dbo].[Person_Temporal] [dbo].[Employee_Temporal]
    This view can be later used in temporal querying which is extremely flexible and convenient
    given that participating tables are temporal and can be changed independently
*/
CREATE VIEW [dbo].[vEmployeePersonTemporalInfo]
AS
SELECT P.BusinessEntityID, P.Title, P. FirstName, P.LastName, P.MiddleName
, E.JobTitle, E.MaritalStatus, E.Gender, E.VacationHours, E.SickLeaveHours
FROM [dbo].Person_Temporal P
JOIN  [dbo].[Employee_Temporal] E
ON P.[BusinessEntityID] = E.[BusinessEntityID]


GO

CREATE TABLE [dbo].[A](
    [BusinessEntityID] [int] NOT NULL,
    [NationalIDNumber] [nvarchar](15) NOT NULL,
    [ValidFrom] [datetime2](7) NOT NULL,
    [ValidTo] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE VIEW vw_dbo_A AS SELECT * FROM [dbo].[A]
GO

CREATE TABLE [dbo].[B](
    [BusinessEntityID] [int] NOT NULL,
    [NationalIDNumber] [nvarchar](15) NOT NULL,
    [ValidFrom] [datetime2](7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [ValidTo] [datetime2](7) GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
 CONSTRAINT [PK_B] PRIMARY KEY CLUSTERED 
(
    [BusinessEntityID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
) ON [PRIMARY]
WITH
(
SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[A] , DATA_CONSISTENCY_CHECK = ON )
)
GO


CREATE TABLE [dbo].[C](
    [BusinessEntityID] [int] NOT NULL,
    [NationalIDNumber] [nvarchar](15) NOT NULL,
    [ValidFrom] [datetime2](7) NOT NULL,
    [ValidTo] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE VIEW vw_dbo_C AS SELECT * FROM [dbo].[C]
GO

CREATE CLUSTERED INDEX [ix_C] ON [dbo].[C]
(
    [BusinessEntityID] ASC,
    [ValidFrom] ASC,
    [ValidTo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE TABLE [dbo].[D](
    [BusinessEntityID] [int] NOT NULL,
    [NationalIDNumber] [nvarchar](15) NOT NULL,
    [ValidFrom] [datetime2](7) GENERATED ALWAYS AS ROW START NOT NULL,
    [ValidTo] [datetime2](7) GENERATED ALWAYS AS ROW END NOT NULL,
 CONSTRAINT [PK_D] PRIMARY KEY CLUSTERED 
(
    [BusinessEntityID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
) ON [PRIMARY]
WITH
(
SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[C] , DATA_CONSISTENCY_CHECK = ON )
)
GO

CREATE VIEW vw_dbo_D AS SELECT * FROM [dbo].[D]
GO