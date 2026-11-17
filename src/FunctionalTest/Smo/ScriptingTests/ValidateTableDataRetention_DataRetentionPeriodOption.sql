create table tableName_Month ([dbdatetime2] datetime2(7)) with (DATA_DELETION = ON ( FILTER_COLUMN = [dbdatetime2], RETENTION_PERIOD = 1 Month ) )

create table tableName_Week ([dbdatetime2] datetime2(7)) with (DATA_DELETION = ON ( FILTER_COLUMN = [dbdatetime2], RETENTION_PERIOD = 1 Week ) )

create table tableName_Day ([dbdatetime2] datetime2(7)) with (DATA_DELETION = ON ( FILTER_COLUMN = [dbdatetime2], RETENTION_PERIOD = 1 Day ) )

create table tableName_Year ([dbdatetime2] datetime2(7)) with (DATA_DELETION = ON ( FILTER_COLUMN = [dbdatetime2], RETENTION_PERIOD = 1 Year ) )

create table tableName_Infinite ([dbdatetime2] datetime2(7)) with (DATA_DELETION = ON ( FILTER_COLUMN = [dbdatetime2], RETENTION_PERIOD = INFINITE ) )