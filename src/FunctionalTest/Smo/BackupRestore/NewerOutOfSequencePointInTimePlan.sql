USE [master]
RESTORE DATABASE [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_FULL_2021_03_24_201551.bak' WITH  FILE = 1,  NORECOVERY,  NOUNLOAD
RESTORE DATABASE [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_DIFF_2021_03_26_201508.bak' WITH  FILE = 1,  NORECOVERY,  NOUNLOAD
RESTORE LOG [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_LOG_2021_03_26_203010.trn' WITH  FILE = 1,  NORECOVERY,  NOUNLOAD
RESTORE LOG [Data] FROM  URL = N'https://test.windows.net/sample/FULL/Data_LOG_2021_03_27_003008.trn' WITH  FILE = 1,  NOUNLOAD,  STOPAT = N'2021-03-26T21:30:00'
