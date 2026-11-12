USE [msdb]
GO

DECLARE @id1 INT, @id2 INT, @id3 INT, @id4 INT, @id5 INT, @id6 INT
SET @id1 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='591E2543-77CD-4710-90EE-EFF499963810')
SET @id2 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='CFE28E39-BECA-4B12-A9D4-71B4CD493B8C')
SET @id3 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='422D0DAE-2FED-41C3-8E22-6B4884D70A85')
SET @id4 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='84BC7B34-140A-437D-9348-B15B8B6036B4')
SET @id5 = (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='13D05B2A-972D-49BD-8E8C-0E0ADB27FD18')

DELETE FROM [dbo].[backupset] WHERE media_set_id IN (@id1, @id2, @id3, @id4, @id5)

DELETE FROM [dbo].[backupmediafamily] WHERE media_set_id IN (@id1, @id2, @id3, @id4, @id5)

DELETE FROM [dbo].[backupmediaset] WHERE media_set_id IN (@id1, @id2, @id3, @id4, @id5)
GO