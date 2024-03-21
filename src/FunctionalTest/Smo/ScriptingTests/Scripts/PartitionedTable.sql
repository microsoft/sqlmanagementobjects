CREATE PARTITION FUNCTION [AgePartFunc](int) AS RANGE RIGHT FOR VALUES (18)
GO

CREATE PARTITION SCHEME [AgePartScheme] AS PARTITION [AgePartFunc] TO ([PRIMARY], [PRIMARY])
GO

CREATE TABLE [dbo].[Customers]
(
    id int identity,
    name varchar(255),
    age int
    constraint PK_CUSTOMERS
        primary key (id, age)
    ) ON AgePartScheme (age);
GO