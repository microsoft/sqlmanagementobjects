USE [msdb]
GO

DECLARE @id1 INT, @id2 INT, @id3 INT, @id4 INT, @id5 INT, @id6 INT
SET @id1 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='4B540F0F-80A2-4DDB-AC3F-15A12CC0B582')
SET @id2 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='F67AEB07-082B-4859-80A4-A3BD9A9EA2BE')
SET @id3 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='09188E25-3F16-40DF-96A6-F13E8CBBA2CD')
SET @id4 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='54BA97B4-3A45-4705-AEDD-774069F072B5')
SET @id5 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='0C6113E2-8517-412F-ABF4-1132F1723CE3')
SET @id6 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='D79AD6D6-2FE0-4903-AEA5-F0856D4F551C')

DELETE FROM [dbo].[backupset] WHERE media_set_id IN (@id1, @id2, @id3, @id4, @id5, @id6)

DELETE FROM [dbo].[backupmediafamily] WHERE media_set_id IN (@id1, @id2, @id3, @id4, @id5, @id6)

DELETE FROM [dbo].[backupmediaset] WHERE media_set_id IN (@id1, @id2, @id3, @id4, @id5, @id6)
GO