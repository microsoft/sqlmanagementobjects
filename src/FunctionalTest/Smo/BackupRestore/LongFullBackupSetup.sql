USE [msdb]
GO

INSERT INTO [dbo].[backupmediaset]
           ([media_uuid]
           ,[media_family_count]
           ,[name]
           ,[description]
           ,[software_name]
           ,[software_vendor_id]
           ,[MTF_major_version]
           ,[mirror_count]
           ,[is_password_protected]
           ,[is_compressed]
           ,[is_encrypted])
     VALUES
('591E2543-77CD-4710-90EE-EFF499963810', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL),
('CFE28E39-BECA-4B12-A9D4-71B4CD493B8C', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL),
('422D0DAE-2FED-41C3-8E22-6B4884D70A85', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL),
('84BC7B34-140A-437D-9348-B15B8B6036B4', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL),
('13D05B2A-972D-49BD-8E8C-0E0ADB27FD18', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL)
GO

INSERT INTO [dbo].[backupmediafamily]
           ([media_set_id]
           ,[family_sequence_number]
           ,[media_family_id]
           ,[media_count]
           ,[logical_device_name]
           ,[physical_device_name]
           ,[device_type]
           ,[physical_block_size]
           ,[mirror])
     VALUES
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='591E2543-77CD-4710-90EE-EFF499963810'), 1, '39AC4DAD-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_DIFF_2021_04_18_201500.bak', 9, 65536, 0),
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='CFE28E39-BECA-4B12-A9D4-71B4CD493B8C'), 1, '3B333306-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_LOG_2021_04_18_203000.trn', 9, 65536, 0),
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='422D0DAE-2FED-41C3-8E22-6B4884D70A85'), 1, '6BF8211C-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_LOG_2021_04_19_003000.trn', 9, 65536, 0),
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='84BC7B34-140A-437D-9348-B15B8B6036B4'), 1, '0071B2F1-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_FULL_2021_04_18_211501.bak', 9, 65536, 0),
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='13D05B2A-972D-49BD-8E8C-0E0ADB27FD18'), 1, '16B99737-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_LOG_2021_04_19_043000.trn', 9, 65536, 0)

GO

INSERT INTO [dbo].[backupset]
           ([backup_set_uuid]
           ,[media_set_id]
           ,[first_family_number]
           ,[first_media_number]
           ,[last_family_number]
           ,[last_media_number]
           ,[catalog_family_number]
           ,[catalog_media_number]
           ,[position]
           ,[expiration_date]
           ,[software_vendor_id]
           ,[name]
           ,[description]
           ,[user_name]
           ,[software_major_version]
           ,[software_minor_version]
           ,[software_build_version]
           ,[time_zone]
           ,[mtf_minor_version]
           ,[first_lsn]
           ,[last_lsn]
           ,[checkpoint_lsn]
           ,[database_backup_lsn]
           ,[database_creation_date]
           ,[backup_start_date]
           ,[backup_finish_date]
           ,[type]
           ,[sort_order]
           ,[code_page]
           ,[compatibility_level]
           ,[database_version]
           ,[backup_size]
           ,[database_name]
           ,[server_name]
           ,[machine_name]
           ,[flags]
           ,[unicode_locale]
           ,[unicode_compare_style]
           ,[collation_name]
           ,[is_password_protected]
           ,[recovery_model]
           ,[has_bulk_logged_data]
           ,[is_snapshot]
           ,[is_readonly]
           ,[is_single_user]
           ,[has_backup_checksums]
           ,[is_damaged]
           ,[begins_log_chain]
           ,[has_incomplete_metadata]
           ,[is_force_offline]
           ,[is_copy_only]
           ,[first_recovery_fork_guid]
           ,[last_recovery_fork_guid]
           ,[fork_point_lsn]
           ,[database_guid]
           ,[family_guid]
           ,[differential_base_lsn]
           ,[differential_base_guid]
           ,[compressed_backup_size]
           ,[key_algorithm]
           ,[encryptor_thumbprint]
           ,[encryptor_type])
     VALUES
('2DF60F1A-55FF-48D8-A971-EFEFB7F7E434', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='591E2543-77CD-4710-90EE-EFF499963810'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 8, 0, 315000007756600000, 322000000237600000, 322000000236800000, 289000004658900000, '3/18/2021 15:06:04.000', '4/18/2021 20:15:00.000', '4/18/2021 20:15:02.000', 'I', 0, 0, 150, 904, 399729664, 'Data', 'Sample', 'Sample', 2576, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '857B55F7-2E1A-43E8-84C1-848D68D52EF9', '857B55F7-2E1A-43E8-84C1-848D68D52EF9', NULL, '355F59DF-59A9-4CA6-8269-22299704E340', '68C4BBF2-3AC5-4535-BEE1-95CEA76FE4C3', 289000004658900000, 'A713999E-4089-4AC7-B353-160B09B72632', 40681515, NULL, NULL, NULL),
('1B270B12-8013-42F5-A2EA-C603C5D5B866', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='CFE28E39-BECA-4B12-A9D4-71B4CD493B8C'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 8, 0, 320000000231800000, 322000000252300000, 322000000236800000, 289000004658900000, '3/18/2021 15:06:04.000', '4/18/2021 20:30:00.000', '4/18/2021 20:30:00.000', 'L', 0, 0, 150, 904, 67401728, 'Data', 'Sample', 'Sample', 528, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '857B55F7-2E1A-43E8-84C1-848D68D52EF9', '857B55F7-2E1A-43E8-84C1-848D68D52EF9', NULL, '355F59DF-59A9-4CA6-8269-22299704E340', '68C4BBF2-3AC5-4535-BEE1-95CEA76FE4C3', NULL, NULL, 1118579, NULL, NULL, NULL),
('338BEACA-31A6-4C11-892A-4AE1CA137490', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='422D0DAE-2FED-41C3-8E22-6B4884D70A85'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 8, 0, 322000000252300000, 324000000242400000, 324000000004300000, 289000004658900000, '3/18/2021 15:06:04.000', '4/19/2021 0:30:00.000', '4/19/2021 0:30:00.000', 'L', 0, 0, 150, 904, 66746368, 'Data', 'Sample', 'Sample', 528, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '857B55F7-2E1A-43E8-84C1-848D68D52EF9', '857B55F7-2E1A-43E8-84C1-848D68D52EF9', NULL, '355F59DF-59A9-4CA6-8269-22299704E340', '68C4BBF2-3AC5-4535-BEE1-95CEA76FE4C3', NULL, NULL, 850827, NULL, NULL, NULL),
('A713999E-4089-4AC7-B353-160B09B72632', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='84BC7B34-140A-437D-9348-B15B8B6036B4'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 8, 0, 321000000231800000, 325000000242400000, 289000004658900000, 250000005055600000, '3/18/2021 15:06:04.000', '4/18/2021 21:15:01.000', '4/19/2021 0:45:50.000', 'D', 0, 0, 150, 904, 2286685184, 'Data', 'Sample', 'Sample', 528, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '857B55F7-2E1A-43E8-84C1-848D68D52EF9', '857B55F7-2E1A-43E8-84C1-848D68D52EF9', NULL, '355F59DF-59A9-4CA6-8269-22299704E340', '68C4BBF2-3AC5-4535-BEE1-95CEA76FE4C3', NULL, NULL, 591167805, NULL, NULL, NULL),
('943F4841-022F-4640-9D27-CDD3BE5918FA', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='13D05B2A-972D-49BD-8E8C-0E0ADB27FD18'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 8, 0, 324000000242400000, 327000000391800000, 327000000158300000, 289000004658900000, '3/18/2021 15:06:04.000', '4/19/2021 4:30:00.000', '4/19/2021 4:30:01.000', 'L', 0, 0, 150, 904, 197817344, 'Data', 'Sample', 'Sample', 528, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '857B55F7-2E1A-43E8-84C1-848D68D52EF9', '857B55F7-2E1A-43E8-84C1-848D68D52EF9', NULL, '355F59DF-59A9-4CA6-8269-22299704E340', '68C4BBF2-3AC5-4535-BEE1-95CEA76FE4C3', NULL, NULL, 13566920, NULL, NULL, NULL)


GO