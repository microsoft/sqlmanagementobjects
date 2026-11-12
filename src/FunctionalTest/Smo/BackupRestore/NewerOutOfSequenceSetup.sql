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
('4B540F0F-80A2-4DDB-AC3F-15A12CC0B582', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL),
('F67AEB07-082B-4859-80A4-A3BD9A9EA2BE', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL),
('09188E25-3F16-40DF-96A6-F13E8CBBA2CD', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL),
('54BA97B4-3A45-4705-AEDD-774069F072B5', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL),
('0C6113E2-8517-412F-ABF4-1132F1723CE3', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL),
('D79AD6D6-2FE0-4903-AEA5-F0856D4F551C', 1, NULL, NULL, 'Microsoft SQL Server', 4608, 1, 1, 0, 1, NULL)
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
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='4B540F0F-80A2-4DDB-AC3F-15A12CC0B582'), 1, 'DF2CF001-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_FULL_2021_03_24_201551.bak', 9, 65536, 0),
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='F67AEB07-082B-4859-80A4-A3BD9A9EA2BE'), 1, 'C27CDD31-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_DIFF_2021_03_26_201508.bak', 9, 65536, 0),
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='09188E25-3F16-40DF-96A6-F13E8CBBA2CD'), 1, 'B6E65A10-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_LOG_2021_03_26_203010.trn', 9, 65536, 0),
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='54BA97B4-3A45-4705-AEDD-774069F072B5'), 1, 'BE480E20-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_LOG_2021_03_27_003008.trn', 9, 65536, 0),
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='0C6113E2-8517-412F-ABF4-1132F1723CE3'), 1, '853E1D8A-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_LOG_2021_03_29_083001.trn', 9, 65536, 0),
((SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='D79AD6D6-2FE0-4903-AEA5-F0856D4F551C'), 1, '69D8A9BC-0000-0000-0000-000000000000', 1, NULL, 'https://test.windows.net/sample/FULL/Data_LOG_2021_03_29_083632.trn', 9, 65536, 0)

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
('F3127A6A-5DBE-46D2-867B-43C3D5693584', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='4B540F0F-80A2-4DDB-AC3F-15A12CC0B582'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 4, 0, 153385000004237000000, 153385000004238000000, 153385000004237000000, 153225000010774000000, '11/24/2020 7:24:52.000', '3/24/2021 20:15:51.000', '3/24/2021 20:16:25.000', 'D', 0, 0, 150, 904, 3750102016, 'Data', 'Sample', 'Sample', 528, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '9319F59E-702A-47E4-83AC-4FBAE0C9D9F7', '9319F59E-702A-47E4-83AC-4FBAE0C9D9F7', NULL, 'A51B2F55-E442-43A9-8DA1-42E72950BDB0', 'C1D08C7F-0D1F-4A92-B3BC-06D932519553', NULL, NULL, 761988426, NULL, NULL, NULL),
('EB378E11-A86C-45AD-8E44-845BA11EDABC', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='F67AEB07-082B-4859-80A4-A3BD9A9EA2BE'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 4, 0, 154152000006482000000, 154152000006482000000, 154152000006482000000, 153385000004237000000, '11/24/2020 7:24:52.000', '3/26/2021 20:15:08.000', '3/26/2021 20:15:11.000', 'I', 0, 0, 150, 904, 247863296, 'Data', 'Sample', 'Sample', 2576, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '9319F59E-702A-47E4-83AC-4FBAE0C9D9F7', '9319F59E-702A-47E4-83AC-4FBAE0C9D9F7', NULL, 'A51B2F55-E442-43A9-8DA1-42E72950BDB0', 'C1D08C7F-0D1F-4A92-B3BC-06D932519553', 153385000004237000000, 'F3127A6A-5DBE-46D2-867B-43C3D5693584', 39828859, NULL, NULL, NULL),
('D3D5DCFD-684F-4501-909F-FBF788516E70', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='09188E25-3F16-40DF-96A6-F13E8CBBA2CD'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 4, 0, 154093000002340000000, 154157000007000000000, 154157000000598000000, 153385000004237000000, '11/24/2020 7:24:52.000', '3/26/2021 20:30:10.000', '3/26/2021 20:30:44.000', 'L', 0, 0, 150, 904, 3961460736, 'Data', 'Sample', 'Sample', 528, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '9319F59E-702A-47E4-83AC-4FBAE0C9D9F7', '9319F59E-702A-47E4-83AC-4FBAE0C9D9F7', NULL, 'A51B2F55-E442-43A9-8DA1-42E72950BDB0', 'C1D08C7F-0D1F-4A92-B3BC-06D932519553', NULL, NULL, 870083921, NULL, NULL, NULL),
('501A44F8-77C1-406A-8917-2BEAB711C57E', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='54BA97B4-3A45-4705-AEDD-774069F072B5'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 4, 0, 154157000007000000000, 154187000004732000000, 154187000002440000000, 153385000004237000000, '11/24/2020 7:24:52.000', '3/27/2021 0:30:08.000', '3/27/2021 0:30:21.000', 'L', 0, 0, 150, 904, 1695583232, 'Data', 'Sample', 'Sample', 528, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '9319F59E-702A-47E4-83AC-4FBAE0C9D9F7', '9319F59E-702A-47E4-83AC-4FBAE0C9D9F7', NULL, 'A51B2F55-E442-43A9-8DA1-42E72950BDB0', 'C1D08C7F-0D1F-4A92-B3BC-06D932519553', NULL, NULL, 363724342, NULL, NULL, NULL),
('F46FA8A6-591D-4821-9BBB-34137943C9A5', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='0C6113E2-8517-412F-ABF4-1132F1723CE3'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 8, 0, 154157000007000000000, 154176000007131000000, 154176000007123000000, 153385000004237000000, '11/24/2020 7:24:52.000', '3/29/2021 8:30:01.000', '3/29/2021 8:30:12.000', 'L', 0, 0, 150, 904, 986298368, 'Data', 'Sample', 'Sample', 528, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, '9319F59E-702A-47E4-83AC-4FBAE0C9D9F7', 'E5C06515-4559-413E-87A6-64DC928F598B', 154176000007060000000, 'A51B2F55-E442-43A9-8DA1-42E72950BDB0', 'C1D08C7F-0D1F-4A92-B3BC-06D932519553', NULL, NULL, 206666505, NULL, NULL, NULL),
('03B24566-D8F2-49BE-8ADE-907AB5172415', (SELECT media_set_id FROM dbo.backupmediaset WHERE media_uuid='D79AD6D6-2FE0-4903-AEA5-F0856D4F551C'), 1, 1, 1, 1, 1, 1, 1, NULL, 4608, NULL, NULL, 'GLOBAL\test$', 15, 0, 4102, 8, 0, 154176000007131000000, 154176000007150000000, 154176000007137000000, 154176000007137000000, '11/24/2020 7:24:52.000', '3/29/2021 8:36:32.000', '3/29/2021 8:36:32.000', 'L', 0, 0, 150, 904, 328704, 'Data', 'Sample', 'Sample', 528, 1033, 196609, 'Coll', 0, 'FULL', 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 'E5C06515-4559-413E-87A6-64DC928F598B', 'E5C06515-4559-413E-87A6-64DC928F598B', NULL, 'A51B2F55-E442-43A9-8DA1-42E72950BDB0', 'C1D08C7F-0D1F-4A92-B3BC-06D932519553', NULL, NULL, 105255, NULL, NULL, NULL)

GO