USE [master]
RESTORE DATABASE [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_FULL_2021_04_14_201501.bak' WITH  FILE = 1,  NORECOVERY,  NOUNLOAD
RESTORE DATABASE [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_DIFF_2021_04_18_201500.bak' WITH  FILE = 1,  NORECOVERY,  NOUNLOAD
RESTORE LOG [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_LOG_2021_04_18_203000.trn' WITH  FILE = 1,  NORECOVERY,  NOUNLOAD
RESTORE LOG [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_LOG_2021_04_19_003000.trn' WITH  FILE = 1,  NOUNLOAD,  STOPAT = N'2021-04-19T00:15:00'
