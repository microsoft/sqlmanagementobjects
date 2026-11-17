--=======================================================
--This file contains statements to run that are valid
--for any On-Prem SQL Server version 14.0 (2017) and above
--=======================================================

CREATE EXTERNAL LIBRARY lazyeval FROM (content = 0xa0000000) WITH (language = 'R');
GO
