USE [master]
RESTORE DATABASE [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_FULL_2021_04_18_211501.bak' WITH  FILE = 1,  NORECOVERY,  NOUNLOAD
RESTORE LOG [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_LOG_2021_04_19_043000.trn' WITH  FILE = 1,  NOUNLOAD
