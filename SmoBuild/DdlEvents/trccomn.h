// ***************************************************************************
//               Copyright (C) Microsoft Corporation.
// @File: trccomn.h
// @Owner: ivanpe, fvoznika, jhalmans
// @Test: pedrou
//
// PURPOSE: Contains event and column definitions
// AUTHOR:  NC Oct. 1999
//
// @EndHeader@
// ***************************************************************************


#ifndef TRCCOMN_H_
#define TRCCOMN_H_

#define HRESULT_WARNING_FROM_SQL(exnumber)	(MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_SQL, (exnumber)))

// SQLTrace eTraceError conversions
#define EX_MINOR_FROM_TRC_ERROR(err) 	((err) + 50)
#define EX_NUMBER_FROM_TRC_ERROR(err) (EX_NUMBER(SQLTRACE_ERR,EX_MINOR_FROM_TRC_ERROR(err)))
#define HRESULT_FROM_TRC_ERROR(err)	(HRESULT_FROM_SQL(EX_NUMBER_FROM_TRC_ERROR(err)))
#define HRESULT_WARNING_FROM_TRC_ERROR(err)	(HRESULT_WARNING_FROM_SQL(EX_NUMBER_FROM_TRC_ERROR(err)))
#define TRC_ERROR_FROM_HRESULT(hr)	((EX_NUMBER_FROM_HRESULT(hr)>0 && EX_MAJOR(EX_NUMBER_FROM_HRESULT(hr)) == SQLTRACE_ERR)? \
	EX_MINOR(EX_NUMBER_FROM_HRESULT(hr)) - 50 : 0)


// useful defines
#define BITS_PER_DWORD			(sizeof(DWORD)*8)
#define RELEVANT_DWORD(ec)		((ec)/BITS_PER_DWORD)
#define BIT_IN_DWORD(ec)		(0x1 << ((ec)%BITS_PER_DWORD))

// Maximum file path length for server file writing
#define MAX_TRACE_FILE_PATH 245
 
// a few useful enums
enum ETraceStatus
	{
	eTraceStopped,
	eTraceStarted,
	eTraceClose,
	eTracePause,
	};

// Enum for CTraceFilter booleans
enum EFilterBool
	{
	// Logical operator will be first
	eFilterBoolAnd = 0,	// AND
	eFilterBoolOr,		// OR
	};

// Enum for CTraceFilter operations.
enum EFilterOp
	{
	// Filter operations
	eFilterOpEq = 0,	// equal
	eFilterOpNe,		// not equal
	eFilterOpGt,		// greater than
	eFilterOpLt,		// less than
	eFilterOpGe,		// greater than or equal
	eFilterOpLe,		// less than or equal
	eFilterOpLike,		// like
	eFilterOpNotLike	// not like
	};

// Enum for Options that can be set on a server.
enum ETraceOptions
{
	eTraceDestRowset =	0x1,		// data written to a rowset
	eTraceRollover =	0x2,		// rollover files at some max file size
	eTraceShutdown =	0x4,		// shutdown server on write error
	eTraceFlightRec =	0x8,		// Trace is being used as the flight recorder.

	// Extended option occupies high word, and is sequential number.
	// Combined option would look like 0x10002
	eTraceNoExtended = 0,		// No extended option this time
};

#define TRACEOPTIONS_NORMAL	(eTraceDestRowset|eTraceRollover|eTraceShutdown|eTraceFlightRec)
#define TRACEOPTIONS_EXTENDED_MAX	(eTraceNoExtended)

inline ULONG UlTraceOptionNormalPart(__in ULONG ulOptions)
{
	return (ulOptions & 0xFFFF);
}

inline ULONG UlTraceOptionExtendedPart(__in ULONG ulOptions)
{
	return (ulOptions >> 16);
}

inline BOOL FIsTraceOptionWithinValidRange(__in ULONG ulOptions)
{
	return ((UlTraceOptionNormalPart(ulOptions) & (~TRACEOPTIONS_NORMAL)) == 0)
		&& (UlTraceOptionExtendedPart(ulOptions) <= TRACEOPTIONS_EXTENDED_MAX);
}

// Enum for get options as returned by ::fn_trace_getTraceInfo
enum ETraceProperties
	{
	etpRowsetOpened = 0,
	etpOptions,
	etpFileName,
	etpMaxsize,
	etpStopTime,
	etpStatus,
	};


// NOTE:  These values are identical to the corresponding XVT_* values.
typedef enum ETraceDataTypes
{
	TRACE_I4 = 1,			// XVT_I4
	TRACE_DATETIME,			// XVT_SSDATE
	TRACE_I8,				// XVT_I8
	TRACE_BYTES,			// XVT_SSBYTES
	TRACE_WSTR,				// XVT_VARWSTR
	TRACE_NTEXT,			// XVT_NTEXT
	TRACE_GUID,			// XVT_SSGUID
} ENUM_TRACE_DATA_TYPES;

// ===================================================================================
// Events
// ===================================================================================

// User Events	0x1 - 0xf3fe
// Replay events	0xf3ff - 0xf7fe
// Profiler events	0xf7ff - 0xfbfe
// Trace special events	0xfbff - 0xfffe

#define IS_SPECIAL_EVENT(ec)	(((ULONG)ec) > 0xF3FF)
#define TRACE_INVALID_EVENT  0xffff

typedef enum
{
	TRACE_START_EVENT		= 0XFFFE,
	TRACE_STOP_EVENT		= 0XFFFD,
	TRACE_ERROR_EVENT		= 0XFFFC,
	TRACE_SKIPPED_EVENT	= 0XFFFB,
	TRACE_NOP				= 0XFFFA,
	TRACE_PAUSE_EVENT	= 0xFFF9,
	TRACE_HEADER_EVENT	= 0xFFF8,
	TRACE_ROLLOVER_EVENT = 0xFFF7,
} CONTROL_EVENTS;


// Event classes
// See DEVNOTE at the end of this enumeration!!!
typedef enum
{
	EVENT_CLASS_UNUSED  = 0,	// 0 to 9 Unused by server and blocked for compatiblity
	EVENT_CLASS_START	= 10,
	//$$EVENT_CLASS_START		do not remove this comment
	POST_RPC_EVENT_CLASS 	= 10,		// pingwang, jayc
	PRE_RPC_EVENT_CLASS = 11,			// pingwang, jayc
	POST_LANG_EVENT_CLASS = 12,		// pingwang, jayc
	PRE_LANG_EVENT_CLASS	= 13	,		// pingwang, jayc
	AUDIT_LOGIN_EVENT_CLASS = 14,		// sashwin, pingwang
	AUDIT_LOGOUT_EVENT_CLASS = 15,	// sashwin, pingwang
	ATTENTION_EVENT_CLASS = 16,		// sashwin, pingwang
	ACTIVE_EVENT_CLASS = 17,			// jayc
	AUDIT_SERVER_START_STOP_EVENT_CLASS = 18,	// peterbyr
	DTC_EVENT_CLASS = 19,				// mikepurt
	AUDIT_LOGIN_FAILED_EVENT_CLASS = 20,		// sashwin
	EVENTLOG_EVENT_CLASS = 21,		// jayc
	ERRORLOG_EVENT_CLASS = 22,		// jayc
	LCK_RELEASE_EVENT_CLASS = 23,		// santeriv
	LCK_ACQUIRE_EVENT_CLASS = 24,		// santeriv
	LCK_DEADLOCK_EVENT_CLASS = 25,	// santeriv
	LCK_CANCEL_EVENT_CLASS = 26,		// santeriv
	LCK_TIMEOUT_EVENT_CLASS = 27,		// santeriv
	DOP_EVENT_CLASS = 28,				// ddavison
	DOP_UPDATE_EVENT_CLASS = 29,		// ddavison
	DOP_DELETE_EVENT_CLASS = 30,		// ddavison
	DOP_SELECT_EVENT_CLASS = 31,		// ddavison
	RESERVED_32,					// Can be reused
	EXCEPTION_EVENT_CLASS = 33,		// jayc
	CP_MISS_EVENT_CLASS = 34,			// ganakris
	CP_INSERT_EVENT_CLASS = 35,		// ganakris
	CP_REMOVE_EVENT_CLASS = 36,		// ganakris
	CP_RECOMPILE_EVENT_CLASS = 37,	// ganakris, eriki
	CP_HIT_EVENT_CLASS = 38,			// ganakris
	RESERVED_39 = 39,				// Can not be reused. This used to be EC_EVENT_CLASS (in Shiloh)
	STMT_START_EVENT_CLASS = 40,		// eriki, jayc
	STMT_END_EVENT_CLASS = 41,		// eriki, jayc
	SP_START_EVENT_CLASS = 42,		// eriki, jayc
	SP_END_EVENT_CLASS = 43,			// eriki, jayc
	SP_STMT_START_EVENT_CLASS = 44,	// eriki, jayc
	SP_STMT_END_EVENT_CLASS = 45,	// eriki, jayc
	OBJECT_CREATE_EVENT_CLASS = 46,	// sameerv, deepakp
	OBJECT_DELETE_EVENT_CLASS = 47,	// sameerv, deepakp
	OBJECT_OPEN_EVENT_CLASS = 48,		// sameerv, deepakp
	OBJECT_CLOSE_EVENT_CLASS = 49,	// UNUSED?
	TRANS_EVENT_CLASS = 50,			// mikepurt
	SCAN_START_EVENT_CLASS = 51,		// srikumar
	SCAN_STOP_EVENT_CLASS = 52,		// srikumar
	CURSOR_CREATED_EVENT_CLASS = 53,	// ganakris
	LOG_EVENT_CLASS = 54,				// peterbyr
	HASH_WARNINGS_EVENT_CLASS = 55,		// asurna
	RESERVED_56,				// THIS SHOULD BE USABLE IN THE FUTURE!!!
	RESERVED_57, // used to be RECOMPILE_NOHINTS_EVENT_CLASS
	AUTO_STATS_EVENT_CLASS = 58,		// jjchen
	LCK_DEADLOCKCHAIN_EVENT_CLASS = 59,	// alexverb
	LCK_ESCALATION_EVENT_CLASS = 60,	// santeriv
	OLEDB_ERRORS_EVENT_CLASS = 61,	// shailv
	// 62 - 66 Used by profiler replay. 
	QRY_EXEC_WARNINGS_EVENT_CLASS = 67,		// weyg
	SHOWPLAN_EVENT_CLASS = 68,	// pingwang, alexisb
	SORT_WARNING_EVENT_CLASS = 69,		// weyg
	CURSOR_PREPARE_EVENT_CLASS = 70,	// sashwin, eriki, ganakris
	PREPARE_EVENT_CLASS = 71,		// sashwin, eriki, ganakris
	EXECUTE_EVENT_CLASS = 72,		// sashwin, eriki, ganakris
	UNPREPARE_EVENT_CLASS = 73,	// sashwin, eriki, ganakris
	CURSOR_EXECUTE_EVENT_CLASS = 74,		// gnakris
	CURSOR_RECOMPILE_EVENT_CLASS = 75,	// gnakris
	CURSOR_IMPLCTCNV_EVENT_CLASS = 76,	// gnakris
	CURSOR_UNPREPARE_EVENT_CLASS = 77,	// gnakris
	CURSOR_CLOSE_EVENT_CLASS = 78,		// gnakris
	NO_STATISTICS_EVENT_CLASS = 79,			// jjchen
	NO_JOIN_PREDICATE_EVENT_CLASS = 80,		// cesarg
	MEMORY_CHANGE_EVENT_CLASS = 81,	// slavao
	// User defined event classes start here
	USER_CONFIGURABLE_0_EVENT_CLASS = 82,		// ganakris
	USER_CONFIGURABLE_1_EVENT_CLASS = 83,		// ganakris
	USER_CONFIGURABLE_2_EVENT_CLASS = 84,		// ganakris
	USER_CONFIGURABLE_3_EVENT_CLASS = 85,		// ganakris
	USER_CONFIGURABLE_4_EVENT_CLASS = 86,		// ganakris
	USER_CONFIGURABLE_5_EVENT_CLASS = 87,		// ganakris
	USER_CONFIGURABLE_6_EVENT_CLASS = 88,		// ganakris
	USER_CONFIGURABLE_7_EVENT_CLASS = 89,		// ganakris
	USER_CONFIGURABLE_8_EVENT_CLASS = 90,		// ganakris
	USER_CONFIGURABLE_9_EVENT_CLASS = 91,		// ganakris

	// NOTE:  START OF 8.0 EVENTS.  CHANGE FIRST_8X_EVENT_CLASS ACCORDINGLY!
	DATAFILE_AUTOGROW_EVENT_CLASS = 92,		// peterbyr
	LOGFILE_AUTOGROW_EVENT_CLASS = 93,		// peterbyr
	DATAFILE_AUTOSHRINK_EVENT_CLASS = 94,	// peterbyr
	LOGFILE_AUTOSHRINK_EVENT_CLASS = 95,		// peterbyr
	SHOWPLAN_TEXT_EVENT_CLASS = 96,			// pingwang, alexisb
	SHOWPLAN_ALL_EVENT_CLASS = 97,			// pingwang, alexisb
	SHOWPLAN_STATISTICS_EVENT_CLASS = 98,	// pingwang, alexisb
	OLAP_NOTIFICATION_EVENT_CLASS = 99,		// jayc
	RPC_OUTPARAM_EVENT_CLASS = 100,			// pingwang, jayc
	// NOTE:  START OF AUDIT EVENTS.  THESE MUST REMAIN CONSTANT!
	RESERVED_101,
	SECURITY_GDR_DB_SCOPE_EVENT_CLASS = 102,		// ruslano
	SECURITY_GDR_SCH_OBJECT_EVENT_CLASS = 103,		// ruslano
	AUDIT_ADDLOGIN_EVENT_CLASS = 104,				// ruslano, deprecated
	AUDIT_LOGIN_GDR_EVENT_CLASS = 105,				// ruslano, deprecated
	SECURITY_SRV_LOGIN_PROP_EVENT_CLASS = 106,		// ruslano
	SECURITY_SRV_LOGIN_PWD_EVENT_CLASS = 107,		// ruslano
	SECURITY_SRV_ROLE_ADDPRIN_EVENT_CLASS = 108,	// ruslano
	AUDIT_ADD_DBUSER_EVENT_CLASS = 109,				// ruslano, deprecated
	SECURITY_DB_ROLE_ADDPRIN_EVENT_CLASS = 110,		// ruslano
	AUDIT_ADDROLE_EVENT_CLASS = 111,				// ruslano, deprecated
	SECURITY_DB_APPROLE_PWD_EVENT_CLASS = 112,		// ruslano
	AUDIT_STMT_PERM_EVENT_CLASS = 113,				// ruslano, deprecated
	SECURITY_SCH_OBJECT_ACCESS_EVENT_CLASS = 114,	// ruslano
	AUDIT_DMPLD_EVENT_CLASS = 115,					// ruslano
	AUDIT_DBCC_EVENT_CLASS = 116,				// ryanston
	AUDIT_CHANGE_AUDIT_EVENT_CLASS = 117,	// jayc
	AUDIT_OBJECT_DERIVED_PERM_EVENT_CLASS = 118,	// ruslano, deprecated

	// YUKON events starts here
	OLEDB_CALL_EVENT_CLASS = 119,		// shailv
	OLEDB_QUERYINTERFACE_EVENT_CLASS = 120,		// shailv
	OLEDB_DATAREAD_EVENT_CLASS = 121,	// shailv
	SHOWPLAN_XML_EVENT_CLASS = 122,		// pingwang, alexisb
	FULLTEXTQUERY_EVENT_CLASS = 123,		// winfredw

	BROKER_DIALOG_ENDPOINT_EVENT_CLASS      = 124,	// ivantrin, augusthi

	// Deprecation events
	DEPRECATION_ANNOUNCEMENT_EVENT_CLASS = 125,	// ivanpe, jayc
	DEPRECATION_FINAL_SUPPORT_EVENT_CLASS = 126, 	// ivanpe, jayc

	EXCHANGE_SPILL_EVENT_CLASS = 127, // martineu

	// Security audit events (con't)
	// database and database objects
	SECURITY_DB_MANAGE_EVENT_CLASS					= 128,	// ruslano
	SECURITY_DB_OBJECT_MANAGE_EVENT_CLASS			= 129,	// ruslano
	SECURITY_DB_PRINCIPAL_MANAGE_EVENT_CLASS		= 130,	// ruslano

	// schema objects
	SECURITY_SCH_OBJECT_MANAGE_EVENT_CLASS			= 131,	// ruslano

	// impersonation
	SECURITY_SRV_PRINCIPAL_IMPERSONATE_EVENT_CLASS	= 132,	// ruslano
	SECURITY_DB_PRINCIPAL_IMPERSONATE_EVENT_CLASS	= 133,	// ruslano

	// take ownership
	SECURITY_SRV_OBJECT_TAKEOWNERSHIP_EVENT_CLASS	= 134,	// ruslano
	SECURITY_DB_OBJECT_TAKEOWNERSHIP_EVENT_CLASS	= 135,	// ruslano

    BROKER_CONVERSATION_GROUP_EVENT_CLASS			= 136,	// ivantrin, augusthi
	BLOCKED_PROCESS_REPORT_EVENT_CLASS		 		= 137,	// alexverb
	BROKER_CONNECTION_EVENT_CLASS					= 138,	// micn, augusthi
	BROKER_FORWARDED_MESSAGE_SENT_EVENT_CLASS		= 139,	// micn
	BROKER_FORWARDED_MESSAGE_DROPPED_EVENT_CLASS	= 140,	// micn
	BROKER_MESSAGE_CLASSIFY_EVENT_CLASS				= 141,	// ivantrin, augusthi
	BROKER_TRANSMISSION_EVENT_CLASS  				= 142,	// micn
	BROKER_QUEUE_DISABLED_EVENT_CLASS				= 143,	// augusthi
	BROKER_MIRROR_ROUTE_EVENT_CLASS 				= 144,  // rushid
	RESERVED_FOR_BROKER_EVENT_CLASS_145				= 145,	// augusthi

	SHOWPLAN_XML_STATISTICS_EVENT_CLASS 	= 146,	// pingwang, alexisb
	RESERVED_FOR_BROKER_EVENT_CLASS_147				= 147,	// augusthi
	SQLOS_XML_DEADLOCK_EVENT_CLASS          = 148,		// alexverb
	BROKER_MESSAGE_ACK_EVENT_CLASS        = 149,	// micn, augusthi
	TRACE_FILE_CLOSE_EVENT_CLASS			= 150,	// jayc

	// Database mirroring events
	DBMIRRORING_CONNECTION_EVENT_CLASS			= 151,			// remusr, steveli

	// yukon security audit events (con't)
	SECURITY_DB_TAKEOWNERSHIP_EVENT_CLASS			= 152,		// tamoyd, ruslano
	SECURITY_SCH_OBJECT_TAKEOWNERSHIP_EVENT_CLASS	= 153,		// tamoyd, ruslano
	AUDIT_DBMIRRORING_LOGIN_EVENT_CLASS			= 154,	// remusr, steveli

	// full-text events
	FULLTEXT_CRAWL_START_EVENT_CLASS = 155,		// nimishk
	FULLTEXT_CRAWL_END_EVENT_CLASS = 156,		// nimishk
	FULLTEXT_CRAWL_ERROR_EVENT_CLASS = 157,		// nimishk

	// service broker security audit events
	AUDIT_BROKER_CONVERSATION_EVENT_CLASS	= 158,	// micn, augusthi
	AUDIT_BROKER_LOGIN_EVENT_CLASS			= 159,	// micn, augusthi

	BROKER_MESSAGE_DROP_EVENT_CLASS			= 160,	// scottkon, augusthi
	BROKER_CORRUPTED_MSG_EVENT_CLASS		= 161,	// scottkon, augusthi

	USER_ERROR_MSG_EVENT_CLASS = 162,			// jayc

	BROKER_ACTIVATION_EVENT_CLASS = 163,			// geraldh, augusthi

	OBJECT_ALTER_EVENT_CLASS = 164,				// jayc	

	// Performance event classes
	STMT_PERFSTAT_EVENT_CLASS = 165,		// ganakris, jayc

	STMT_RECOMPILE_EVENT_CLASS = 166,		// eriki, ganakris

	// Database mirroring events (continued)
	DBMIRRORING_STATE_CHANGE_EVENT_CLASS = 167,	// steveli

	// Events on (re)compile
	SHOWPLAN_XML_COMPILE_EVENT_CLASS = 168,			// alexisb, pingwang
	SHOWPLAN_ALL_COMPILE_EVENT_CLASS = 169,			// alexisb, pingwang

	// Yukon security events (cont'd)
	// GDR events
	SECURITY_GDR_SRV_SCOPE_EVENT_CLASS		= 170,	// ruslano
	SECURITY_GDR_SRV_OBJECT_EVENT_CLASS		= 171,	// ruslano
	SECURITY_GDR_DB_OBJECT_EVENT_CLASS		= 172,	// ruslano

	// server and server objects
	SECURITY_SRV_OPERATION_EVENT_CLASS		= 173,		// ruslano
	FREE_TO_USE_174							= 174,		// ruslano
	SECURITY_SRV_ALTERTRACE_EVENT_CLASS		= 175,		// ruslano
	SECURITY_SRV_OBJECT_MANAGE_EVENT_CLASS		= 176,	// ruslano
	SECURITY_SRV_PRINCIPAL_MANAGE_EVENT_CLASS	= 177,	// ruslano

	// database and database objects
	SECURITY_DB_OPERATION_EVENT_CLASS			= 178,	// ruslano
	FREE_TO_USE_179								= 179,
	SECURITY_DB_OBJECT_ACCESS_EVENT_CLASS		= 180,	// ruslano

	PRE_XACTEVENT_BEGIN_TRAN_EVENT_CLASS = 181,			// pingwang, ganakris
	POST_XACTEVENT_BEGIN_TRAN_EVENT_CLASS = 182,		// pingwang, ganakris
	PRE_XACTEVENT_PROMOTE_TRAN_EVENT_CLASS = 183,		// pingwang, ganakris
	POST_XACTEVENT_PROMOTE_TRAN_EVENT_CLASS = 184,		// pingwang, ganakris
	PRE_XACTEVENT_COMMIT_TRAN_EVENT_CLASS = 185,		// pingwang, ganakris
	POST_XACTEVENT_COMMIT_TRAN_EVENT_CLASS = 186,		// pingwang, ganakris
	PRE_XACTEVENT_ROLLBACK_TRAN_EVENT_CLASS = 187,		// pingwang, ganakris
	POST_XACTEVENT_ROLLBACK_TRAN_EVENT_CLASS = 188,		// pingwang, ganakris

	// More lock events
	LCK_TIMEOUT_NP_EVENT_CLASS = 189,		// santeriv

	// Online index operation events	
	ONLINE_INDEX_PROGRESS_EVENT_CLASS = 190,				// weyg
	

	PRE_XACTEVENT_SAVE_TRAN_EVENT_CLASS = 191,		// pingwang, ganakris
	POST_XACTEVENT_SAVE_TRAN_EVENT_CLASS = 192,		// pingwang, ganakris
	QP_JOB_ERROR_EVENT_CLASS = 193,					// marcfr

	// More oledb events
	OLEDB_PROVIDERINFORMATION_EVENT_CLASS = 194,	// shailv

	BACKUP_TAPE_MOUNT_EVENT_CLASS	= 195,			// sschmidt

	// CLR events
	SQLCLR_ASSEMBLY_LOAD_CLASS = 196,  		// raviraj
	FREE_TO_USE_197 = 197, 					// raviraj
	
	XQUERY_STATIC_TYPE_EVENT_CLASS  = 198,			// brunode

	// Query notifications
	QN_SUBSCRIPTION_EVENT_CLASS		= 199,		// torsteng, florianw
	QN_TABLE_EVENT_CLASS			= 200,		// torsteng, florianw
	QN_QUERYTEMPLATE_EVENT_CLASS	= 201,		// torsteng, florianw
	QN_DYNAMICS_EVENT_CLASS			= 202,		// torsteng, florianw


	// Internal server diagnostics
	PSS_QUERY_MEMGRANT_EVENT_CLASS = 203,	// jayc
	PSS_PAGE_PREFETCH_EVENT_CLASS = 204,	// craigfr
	PSS_BATCH_SORT_EVENT_CLASS = 205,		// craigfr
	PSS_EXCHNG_DOP_EVENT_CLASS = 206,		// martineu
	PSS_QE_VERBOSE_EVENT_CLASS = 207,		// asurna

	PSS_CXROWSET_EVENT_CLASS = 208,			// craigfr

	MATRIXDB_QUERY_ACTIVATION_SESSION_CLASS = 209,			//naveenp
	MATRIXDB_QUERY_ACTIVATION_BATCH_AND_LEVEL_CLASS = 210,			//naveenp
	MATRIXDB_QUERY_ACTIVATION_STMT_AND_QUERY_CLASS = 211,			//naveenp

	BITMAP_WARNINGS_EVENT_CLASS = 212,	// asurna

	DATABASE_SUSPECT_DATA_PAGE_EVENT_CLASS = 213,	// kaloianm

	SQLOS_CPU_LIMIT_VIOLATION_EVENT_CLASS          = 214,		// alexverb
	PRECONNECT_PRE_EVENT_CLASS		= 215,	// IvanPe
	PRECONNECT_POST_EVENT_CLASS		= 216,	// IvanPe

	PLAN_GUIDE_SUCCESSFUL_EVENT_CLASS = 217,			// vadimt
	PLAN_GUIDE_UNSUCCESSFUL_EVENT_CLASS = 218,			// vadimt

	MATRIXDB_TRANSACTION_START_END_CLASS = 219,	// pingwang, tommill
	MATRIXDB_TRANSACTION_STATE_TRANSITION_CLASS = 220,	// pingwang, tommill
	MATRIXDB_TRANSACTION_SHIPPING_CLASS = 221,		// pingwang, tommill
	MATRIXDB_TCM_EVENT_CLASS = 222,		// tommill, pingwang

	MATRIXDB_CM_ENLISTMENT_CLASS = 223,                             // stefanr
	MATRIXDB_CMA_ENLISTMENT_CLASS = 224,                            // stefanr

	MATRIXDB_ERROR_EVENT_CLASS = 225,      //sanson    

	MATRIXDB_QUERY_REMOTE_EXCHANGE_EVENT_CLASS = 226,			//martineu
	MATRIXDB_QUERY_RPC_EVENT_CLASS = 227,			//martineu
	
	MATRIXDB_CLONE_REFRESH_EVENT_CLASS = 228,			//vishalk


	MATRIXDB_CM_EVENT_CLASS  = 229,		// stefanr
	MATRIXDB_CMA_EVENT_CLASS = 230,		// stefanr

	XEVENT_EVENT_CLASS = 231,		// alexverb

	MATRIXDB_CLONE_TRANSITION_CLASS = 232,	//andrewz

	MATRIXDB_CM_HEARTBEAT_EVENT_CLASS = 233,	// sanson
	MATRIXDB_CM_FAILURE_MONITOR_EVENT_CLASS = 234,	// sanson

	AUDIT_FULLTEXT_EVENT_CLASS = 235, // sunnar

	//$$EVENT_CLASS_END	do not remove this comment.

	// DEVNOTE!!!!!!
	// ALL NEW RETAIL EVENTS MUST BE ADDED ABOVE THIS COMMENT!!!!!
	// You need to follow the following format because this file is processed by perl script!!!!
	//
	// XXX_EVENT_CLASS = ddd,	// list of owner alias
	//

	TOTAL_RETAIL_EVENTS,
	LAST_RETAIL_EVENT = TOTAL_RETAIL_EVENTS - 1,	// dummy enum. do not remove.

	// CloudDB async transport trace events
	//
	ASYNC_TRANSPORT_CONNECTION_ERROR_EVENT_CLASS, // tomtal
	ASYNC_TRANSPORT_CONNECT_EVENT_CLASS, // tomtal
	ASYNC_TRANSPORT_SEND_EVENT_CLASS, // tomtal
	ASYNC_TRANSPORT_RECEIVED_EVENT_CLASS, // tomtal
	ASYNC_TRANSPORT_LOST_EVENT_CLASS, // tomtal
	ASYNC_TRANSPORT_CORRUPTED_EVENT_CLASS, // tomtal
	ASYNC_TRANSPORT_DISCONNECT_EVENT_CLASS, //tomtal
	ASYNC_TRANSPORT_DEQUEUE_EVENT_CLASS, // tomtal
	ASYNC_TRANSPORT_STATUS_EVENT_CLASS, // tomtal

	// CloudDB SE replication trace events
	//
	SEREPL_CAPTURE_ENLISTED_EVENT_CLASS, // tomtal
	SEREPL_CAPTURE_COMMIT_WAIT_EVENT_CLASS, // tomtal
	SEREPL_CAPTURE_COMMIT_FLUSHED_EVENT_CLASS, // tomtal
	SEREPL_CAPTURE_COMMIT_ACKED_EVENT_CLASS, // tomtal

	SEREPL_APPLY_COMMIT_WAIT_EVENT_CLASS, // tomtal
	SEREPL_APPLY_COMMIT_FLUSHED_EVENT_CLASS, // tomtal
	SEREPL_APPLY_ENLISTED_EVENT_CLASS, // tomtal

	ASYNC_TRANSPORT_MESSAGE_CONTENT_CLASS, //santeriv, tomtal

	// Partition manager events
	//
	CLOUD_PM_REMOVE_PARTITION_EVENT_CLASS, // tomtal
	CLOUD_PM_DELETE_PARTITION_EVENT_CLASS, // tomtal
	CLOUD_PM_BECOME_NOTHING_EVENT_CLASS, // tomtal
	CLOUD_PM_ADD_SECONDARY_EVENT_CLASS, // tomtal
	CLOUD_PM_CHANGE_SECONDARY_EVENT_CLASS, // tomtal
	CLOUD_PM_BECOME_PRIMARY_EVENT_CLASS, // tomtal
	CLOUD_PM_ADD_PARTITION_EVENT_CLASS, // tomtal
	CLOUD_PM_REMOVE_SECONDARY_EVENT_CLASS, // tomtal

	CLOUD_PM_START_NEW_EPOCH_EVENT_CLASS, // tomtal

	CLOUD_FABRIC_DB_PAIRING, // ajayk
	CLOUD_FABRIC_DB_UN_PAIRING, // ajayk
	WORKER_WAIT_STATS_EVENT_CLASS, // gangche

	CLOUD_PM_PARTITION_QUORUM_LOSS_EVENT_CLASS, // tomtal
	CLOUD_PM_KILL_SECONDARY_EVENT_CLASS, // tomtal
	CLOUD_PM_KILL_USER_TRANSACTIONS_EVENT_CLASS, // tomtal
	CLOUD_PM_DELETE_PARTITION_CONTENT_EVENT_CLASS, // tomtal
	CLOUD_PM_KILL_PRIMARY_EVENT_CLASS, // tomtal
	CLOUD_PM_BECOME_SECONDARY_EVENT_CLASS, // tomtal
	CLOUD_PM_SECONDARY_CATCHUP_REQUEST_EVENT_CLASS, // tomtal
	CLOUD_PM_SECONDARY_CATCHUP_COMPLETE_EVENT_CLASS, // tomtal
	CLOUD_PM_START_SECONDARY_COPY_EVENT_CLASS, // tomtal
	CLOUD_PM_END_SECONDARY_COPY_EVENT_CLASS, // tomtal
	CLOUD_PM_START_COPY_FROM_PRIMARY_EVENT_CLASS, // tomtal
	CLOUD_PM_START_CATCHUP_FROM_PRIMARY_EVENT_CLASS, // tomtal
	CLOUD_PM_CATCHUP_FROM_PRIMARY_COMPLETE_EVENT_CLASS, // tomtal
	CLOUD_PM_SECONDARY_FAILURE_REPORT_EVENT_CLASS, // tomtal
	CLOUD_PM_PRIMARY_FAILURE_REPORT_EVENT_CLASS, // tomtal
	CLOUD_PM_RETURN_CSN, // tomtal
	CLOUD_PM_START_SECONDARY_PERSISTENT_CATCHUP_EVENT_CLASS, // tomtal
	CLOUD_PM_END_SECONDARY_PERSISTENT_CATCHUP_EVENT_CLASS, // tomtal
	CLOUD_PM_ESTABLISH_SECONDARY_PERSISTENT_CATCHUP_EVENT_CLASS, // tomtal
	SEREPL_EXCEPTION_EVENT_CLASS,	

	ASYNC_TRANSPORT_CONNECTION_EVENT_CLASS, // tomtal
	ASYNC_TRANSPORT_LOGIN_EVENT_CLASS, // tomtal

	CLOUD_PM_BECOME_FORWARDER_PENDING_EVENT_CLASS, // micn
	CLOUD_PM_BECOME_FORWARDER_EVENT_CLASS, // micn
	SFW_STMT_BLOCK_EVENT_CLASS, // balnee
	CLOUD_PM_SET_PARTITION_COMMIT_MODE_EVENT_CLASS, // tomtal
	SEREPL_SECONDARY_WORKER_LONG_SUSPEND_EVENT_CLASS, // tomtal

	CLOUD_PM_DUMMY_TRANSACTION_EVENT_CLASS, // tomtal
	CLOUD_PM_SET_PARTITION_LOCK_MODE_EVENT_CLASS,
	CLOUD_PM_SET_PARTITION_PREPARE_FULL_COMMIT_MODE_EVENT_CLASS,
	CLOUD_PM_SET_PARTITION_THROTTLING_MODE_EVENT_CLASS,
	
	CLUSTER_PROXY_CONNECTION_EVENT_CLASS, // pporwal
	CLUSTER_PROXY_LOGIN_EVENT_CLASS, // pporwal
	
	CLOUD_PM_PREFER_COPY_OVER_CATCHUP_EVENT_CLASS, // krishnib

	CLOUD_PM_DBSEEDING_BACKUP_PROGRESS_EVENT_CLASS, // nithinm
	CLOUD_PM_DBSEEDING_RESTORE_PROGRESS_EVENT_CLASS, // nithinm
	CLOUD_PM_DBSEEDING_VDICLIENT_EVENT_CLASS, // nithinm

	CLOUD_PM_DBSEEDING_INITIATE_BACKUP_EVENT_CLASS, // nithinm
	CLOUD_PM_DBSEEDING_INITIATE_RESTORE_EVENT_CLASS, // nithinm

	GLOBAL_TRANSACTIONS_CONNECTION_EVENT_CLASS, // rosant
	AUDIT_GLOBAL_TRANSACTIONS_LOGIN_EVENT_CLASS, // rosant

	STORAGE_CONNECTION_EVENT_CLASS, // tomtal
	AUDIT_STORAGE_LOGIN_EVENT_CLASS, // tomtal

	// DEVNOTE!!!!!!
	// ALL NEW TEMP (non GOLDEN_BITS) EVENTS MUST BE ADDED BELLOW THIS COMMENT!!!!!
	// You should not assign values explicitly. The event IDs will change
	// after more events are added, so all debug only events should be
	// enabled (in test scripts) by name not by ID. These are also parsed
	// by the Perl script so, please use the following format:
	//
	// TEMP_XXX_EVENT_CLASS,	// list of owner alias
	//

#ifndef GOLDEN_BITS
	TEMP_SCALABILITY_TEST_EVENT_CLASS,	// ivanpe
#endif

	// DEVNOTE!!!!!!
	// ALL NEW DEBUG EVENTS MUST BE ADDED BELLOW THIS COMMENT!!!!!
	// You should not assign values explicitly. The event IDs will change
	// after more events are added, so all debug only events should be
	// enabled (in test scripts) by name not by ID. These are also parsed
	// by the Perl script so, please use the following format:
	//
	// DBG_ONLY_XXX_EVENT_CLASS,	// list of owner alias
	//
	// If you want to see your new events in Profier you will have to delete the
	// trace definition xml file for your build number. It is located under:
	// %ProgramFiles%\Microsoft SQL Server\90\Tools\Profiler\TraceDefinitions
	//

#ifdef DEBUG
	DBG_ONLY_TEST_EVENT_CLASS,	// ivanpe
#endif DEBUG

	TOTAL_EVENTS,
} TRACE_EVENTS;
#define TOTAL_EVENTS_FOR_SAL TOTAL_EVENTS
// 7.x events
#define FIRST_8X_EVENT_CLASS		USER_CONFIGURABLE_5_EVENT_CLASS

/////// SUB CLASSES start here /////////////////
#define AUDIT_NO_SUBCLASS 0

/////////////////////////////////////////////////////////////////////////
// Security subclasses
//
// Event sub class definitions for Auditing Permission GDR Events
#define SECURITY_PERM_GRANT_SUBCLASS 1
#define SECURITY_PERM_REVOKE_SUBCLASS 2
#define SECURITY_PERM_DENY_SUBCLASS 3

// Event sub class definitions for principal management events
#define SECURITY_PRIN_ADD_SUBCLASS 1
#define SECURITY_PRIN_DROP_SUBCLASS 2
#define SECURITY_PRIN_CHANGE_GROUP_SUBCLASS 3

// event sub class specially for sp_grantdbaccess/sp_revokedbaccess
#define SECURITY_PRIN_GRANTDBACCESS_SUBCLASS 3
#define SECURITY_PRIN_REVOKEDBACCESS_SUBCLASS 4

// Event sub class definitions for Auditing Backup/Restore Events
#define AUDIT_BACKUP_SUBCLASS 1
#define AUDIT_RESTORE_SUBCLASS 2
#define AUDIT_BACKUPLOG_SUBCLASS 3

// Event sub classes for Auditing changes of login properties
#define SECURITY_DEFDB_PROP_CHANGE_SUBCLASS 1
#define SECURITY_DEFLANG_PROP_CHANGE_SUBCLASS 2
#define SECURITY_NAME_PROP_CHANGE_SUBCLASS 3
#define SECURITY_CREDENTIAL_PROP_CHANGE_SUBCLASS 4
#define SECURITY_PWD_POLICY_PROP_CHANGE_SUBCLASS 5
#define SECURITY_PWD_EXPIRATION_PROP_CHANGE_SUBCLASS 6

// Event sub classes for Auditing changes of login password
#define SECURITY_PWD_SELF_CHANGE_SUBCLASS 1
#define SECURITY_PWD_CHANGE_SUBCLASS 2
#define SECURITY_PWD_SELF_RESET_SUBCLASS 3
#define SECURITY_PWD_RESET_SUBCLASS 4
#define SECURITY_PWD_UNLOCK_SUBCLASS 5
#define SECURITY_PWD_MUSTCHANGE_SUBCLASS 6

// Event sub classes for Auditing changes in audit.
#define AUDIT_START_AUDIT_SUBCLASS 1
#define AUDIT_STOP_AUDIT_SUBCLASS 2
#define AUDIT_C2MODE_CHANGED_ON 3
#define AUDIT_C2MODE_CHANGED_OFF 4

// Event sub classes for security audit of securable management
#define SECURITY_CREATE_SECURABLE_SUBCLASS			1
#define SECURITY_ALTER_SECURABLE_SUBCLASS			2
#define SECURITY_DROP_SECURABLE_SUBCLASS			3
#define SECURITY_BACKUP_SECURABLE_SUBCLASS			4 // used for certificate only
#define SECURITY_DISABLE_SECURABLE_SUBCLASS			5 // used for logins only
#define SECURITY_ENABLE_SECURABLE_SUBCLASS			6 // used for logins only
#define SECURITY_CREDENTIAL_MAP_TO_CREATELOGIN_SUBCLASS	7
#define SECURITY_TRANSFER_SECURABLE_SUBCLASS		8
#define SECURITY_NOCREDENTIAL_MAP_TO_LOGIN_SUBCLASS	9
#define SECURITY_OPEN_SECURABLE_SUBCLASS			10 
#define SECURITY_RESTORE_SECURABLE_SUBCLASS			11 
#define SECURITY_ACCESS_SECURABLE_SUBCLASS			12 
#define SECURITY_CHANGEUSERSLOGIN_UPDATEONE_SUBCLASS 13
#define SECURITY_CHANGEUSERSLOGIN_AUTOFIX_SUBCLASS 	14
#define SECURITY_AUDIT_SHUTDOWN_ON_FAILURE_SUBCLASS 	15

// Event sub classese for db operation events
#define SECURITY_DB_CHECKPOINT_SUBCLASS				1
#define SECURITY_DB_SUBSCRIBEQNOTIF_SUBCLASS		2
#define SECURITY_DB_AUTHENTICATE_SUBCLASS			3
#define SECURITY_DB_SHOWPLAN_SUBCLASS				4
#define SECURITY_DB_CONNECT_SUBCLASS				5
#define SECURITY_DB_VIEWDBSTATE_SUBCLASS			6
#define SECURITY_DB_ADMINBULKOPS_SUBCLASS			7

// Event sub classese for srv operation events
#define SECURITY_SRV_ADMINBULKOPS_SUBCLASS			1
#define SECURITY_SRV_ALTERSETTINGS_SUBCLASS			2
#define SECURITY_SRV_ALTERRESOURCES_SUBCLASS		3
#define SECURITY_SRV_AUTHENTICATE_SUBCLASS			4
#define SECURITY_SRV_EXTERNALACCESS_SUBCLASS 		5
#define SECURITY_SRV_ALTERSRVSTATE_SUBCLASS         6
#define SECURITY_SRV_UNSAFE_SUBCLASS 				7
#define SECURITY_SRV_ALTERCONN_SUBCLASS				8
#define SECURITY_SRV_ALTERRESGOVERNOR_SUBCLASS      9
#define SECURITY_SRV_USEANYWORKLOADGRP_SUBCLASS     10
#define SECURITY_SRV_VIEWSERVERSTATE_SUBCLASS		11

// 
// End of Security subclasses
//
/////////////////////////////////////////////////////////////////////////

// Event sub classes for auditing service broker dialog security
#define AUDIT_NO_DIALOG_HDR_SUBCLASS		1
#define AUDIT_CERT_NOT_FOUND_SUBCLASS		2
#define AUDIT_INVALID_SIGNATURE_SUBCLASS	3
#define AUDIT_RUN_AS_TARGET_FAIL_SUBCLASS	4
#define AUDIT_BAD_DATA_SUBCLASS				5

// Event sub classes for auditing service broker login
#define AUDIT_LOGIN_SUCCESS_SUBCLASS 		1
#define AUDIT_PROTOCOL_ERROR_SUBCLASS 		2
#define AUDIT_BAD_MSG_FORMAT_SUBCLASS 		3
#define AUDIT_NEGOTIATE_FAIL_SUBCLASS 		4
#define AUDIT_AUTHENTICATION_FAIL_SUBCLASS 	5
#define AUDIT_AUTHORIZATION_FAIL_SUBCLASS 	6

// Event sub class definitions for lock acquired class
#define LOCK_NL_SUB_CLASS		0
#define LOCK_SCH_S_SUB_CLASS	1
#define LOCK_SCH_M_SUB_CLASS	2
#define LOCK_IS_SUB_CLASS		3
#define LOCK_NL_S_SUB_CLASS		4
#define LOCK_IS_S_SUB_CLASS		5
#define LOCK_IX_SUB_CLASS		6
#define LOCK_SIX_SUB_CLASS		7
#define LOCK_S_SUB_CLASS		8
#define LOCK_U_SUB_CLASS		9
#define LOCK_II_NL_SUB_CLASS	10
#define LOCK_II_X_SUB_CLASS		11
#define LOCK_IU_X_SUB_CLASS		12
#define LOCK_ID_NL_SUB_CLASS	13
#define LOCK_X_SUB_CLASS		14
#define LOCK_LAST_MODE_SUB_CLASS	LOCK_X_SUB_CLASS

#define XACT_COMMIT_XACT_SUB_CLASS	1
#define XACT_COMMIT_AND_BEGIN_XACT_SUB_CLASS	2

#define XACT_ROLLBACK_XACT_SUB_CLASS	1
#define XACT_ROLLBACK_AND_BEGIN_XACT_SUB_CLASS	2


// Event sub class definitions for Matrix transaction shipping
#define MATRIX_XACT_SHIPPING_SUBCLASS_SERIALIZE	1
#define MATRIX_XACT_SHIPPING_SUBCLASS_DESERIALIZE		2
#define MATRIX_XACT_SHIPPING_SUBCLASS_REPORT	3
#define MATRIX_XACT_SHIPPING_SUBCLASS_DEREPORT	4

// Event sub class definitions for TCM age broadcast
enum
{
	MATRIX_TCM_AGE_CLOSE_BROADCAST	= 1,
	MATRIX_TCM_AGE_CLOSE_RECEIVE	= 2,

	MATRIX_TCM_BRICK_STATUS_SEND	= 3,
	MATRIX_TCM_BRICK_STATUS_RECEIVE	= 4,
};


// Event sub class definitions for Matrix transaction begin/commit/rollback
#define MATRIX_XACT_SUBCLASS_BEGIN	1
#define MATRIX_XACT_SUBCLASS_COMMIT	2
#define MATRIX_XACT_SUBCLASS_ABORT	3

// Event sub class definitions for Matrix error reporting, eventually have categories
#define MATRIX_BRICK_COMPONENT_ERROR_SUBCLASS	1

// Event sub class definitions for Matrix failure monitor heartbeat event class
//
#define MATRIX_CM_HEARTBEAT_BROADCAST_SEND_SUBCLASS			1
#define MATRIX_CM_HEARTBEAT_BROADCAST_COMPLETE_SUBCLASS		2
#define MATRIX_CM_HEARTBEAT_BROADCAST_REPLY_SUBCLASS		3

// Event subclass definitions for Matrix failure monitor brick failure event
//
#define MATRIX_CM_FAILURE_MONITOR_ADD_FAILURE_SUBCLASS		1
#define MATRIX_CM_FAILURE_MONITOR_MARK_OFFLINE_SUBCLASS		2

// Event sub class definitions for bitmap warnings
#define BITMAP_WARNINGS_DISABLED_SUBCLASS 0

// Event sub class definitions for execution warnings (QRY_EXEC_WARNINGS_EVENT_CLASS)
enum QRY_EXEC_WARNING_TYPE
{
	QRY_EXEC_WARNINGS_QRYWAIT_SUBCLASS = 1,
	QRY_EXEC_WARNINGS_QRYTIMEOUT_SUBCLASS = 2,
};

// Event sub class definitions for LCK_DEADLOCKCHAIN_EVENT_CLASS events
#define LCK_DEADLOCKCHAIN_RESOURCE_TYPE_LOCK		101
#define LCK_DEADLOCKCHAIN_RESOURCE_TYPE_EXCHANGE	102
#define LCK_DEADLOCKCHAIN_RESOURCE_TYPE_THREAD		103
#define LCK_DEADLOCKCHAIN_RESOURCE_TYPE_PAGESUPP	104

// Event subclass definitions for BROKER_CONNECTION_EVENT_CLASS
#define BROKER_CONNECTION_CONNECTING_SUBCLASS		1
#define BROKER_CONNECTION_CONNECTED_SUBCLASS		2
#define BROKER_CONNECTION_CONNECT_FAILED_SUBCLASS	3
#define BROKER_CONNECTION_CLOSING_SUBCLASS			4
#define BROKER_CONNECTION_CLOSED_SUBCLASS			5
#define BROKER_CONNECTION_ACCEPT_SUBCLASS			6
#define BROKER_CONNECTION_SEND_ERROR_SUBCLASS		7
#define BROKER_CONNECTION_RECEIVE_ERROR_SUBCLASS	8

// Event subclass definitions for BROKER_TRANSMISSION_EVENT_CLASS
#define BROKER_TRANSMISSION_EXCEPTION_SUBCLASS		1

// Is system process?
#define 	SYSTEM_PROCESS 1

// Event subclass definitions for EXCHANGE_SPILL_EVENT_CLASS
#define EXCHANGE_SPILL_BEGIN_SUBCLASS 1
#define EXCHANGE_SPILL_END_SUBCLASS 2

// Event subclass definitions for BACKUP_TAPE_MOUNT_EVENT_CLASS
#define BACKUP_TAPE_MOUNT_REQUEST_SUBCLASS	1	// mount for tape drive is pending operator intervention
#define BACKUP_TAPE_MOUNT_COMPLETE_SUBCLASS	2	// mount for tape drive was completed successfully
#define BACKUP_TAPE_MOUNT_CANCEL_SUBCLASS	3	// mount for tape drive was cancelled


// Event sub classes for auditing fulltext
#define AUDIT_FDHOST_CONNECT_SUCCESS_SUBCLASS 		1
#define AUDIT_FDHOST_CONNECT_ERROR_SUBCLASS 		2
#define AUDIT_FDLAUNCHER_CONNECT_SUCCESS_SUBCLASS 	3
#define AUDIT_FDLAUNCHER_CONNECT_ERROR_SUBCLASS 	4
#define AUDIT_ISM_CORRUPT_SUBCLASS					5
#define AUDIT_PIPE_MESSAGE_CORRUPT_SUBCLASS			6

// Subclass for perf stat event class
enum STMT_PERFSTAT_SUBCLASSES
{
	STMT_PERFSTAT_SQL_SUBCLASS = 0,
	STMT_PERFSTAT_SPPLAN_SUBCLASS = 1,
	STMT_PERFSTAT_BATCHPLAN_SUBCLASS = 2,
	STMT_PERFSTAT_STAT_SUBCLASS = 3,
	STMT_PERFSTAT_PROC_STAT_SUBCLASS = 4,
	STMT_PERFSTAT_TRIG_STAT_SUBCLASS = 5
};

// Event subclass definition for Audit Login / Audit Logout
#define NONPOOLED_CONNECTION_SUBCLASS 1
#define POOLED_CONNECTION_SUBCLASS 2

// Connection type for Audit Login / Audit Logout
#define CONNECTION_NON_DAC 1
#define CONNECTION_DAC 2

// Event subclass for MatrixDB Clone Refresh
#define CLONE_REFRESH_START_SUBCLASS 1
#define CLONE_REFRESH_TRANSITION_START_SUBCLASS 2
#define CLONE_REFRESH_TRANSITION_END_SUBCLASS 3
#define CLONE_REFRESH_SCAN_START_SUBCLASS 4
#define CLONE_REFRESH_SCAN_END_SUBCLASS 5
#define CLONE_REFRESH_BATCH_START_SUBCLASS 6
#define CLONE_REFRESH_DELETED_STALE_ROWS_SUBCLASS 7
#define CLONE_REFRESH_BATCH_END_SUBCLASS 8
#define CLONE_REFRESH_END_SUBCLASS 9
#define CLONE_REFRESH_ERROR_SUBCLASS 10

// Event sub class for clone transition
enum CloneTransitionEventSubClass
{
	CLONE_TRANSITION_INSTALL		= 1,
	CLONE_TRANSITION_UNINSTALL	= 2,
	CLONE_TRANSITION_LOCK		= 3,
	CLONE_TRANSITION_COMMIT		= 4,	
	CLONE_TRANSITION_ACTIVATE	= 5,
	CLONE_TRANSITION_DEACTIVATE	= 6,
};

// =============== Object Types Start Here =======================
// These object types should not be used for audit
// instead use enum EObjType defined in objinfo.h
typedef enum
{
	INDEX_OBJECT_TYPE			= 1,
	DB_OBJECT_TYPE			= 2,
	AP_OBJECT_TYPE			= 3,
	CHECK_CNST_OBJECT_TYPE	= 4,
	DEFAULT_CNST_OBJECT_TYPE	= 5,
	FORKEY_CNST_OBJECT_TYPE	= 6,
	PRIKEY_CNST_OBJECT_TYPE	= 7,
	SP_OBJECT_TYPE				= 8,
	FN_OBJECT_TYPE			= 9,
	RULE_OBJECT_TYPE			= 10,
	REPLPROC_OBJECT_TYPE		= 11,
	SYS_TAB_OBJECT_TYPE		= 12,
	TRIG_OBJECT_TYPE			= 13,
	INLINE_FN_OBJECT_TYPE		= 14,
	TAB_FN_OBJECT_TYPE		= 15,
	UNIQUE_CNST_OBJECT_TYPE	= 16,
	USER_TAB_OBJECT_TYPE		= 17,
	VIEW_OBJECT_TYPE			= 18,
	XPROC_OBJECT_TYPE			= 19,
	ADHOC_OBJECT_TYPE			= 20,
	PREPARED_OBJECT_TYPE		= 21,
	STATISTICS_OBJECT_TYPE	= 22,
	// End of Shiloh compatible types.
	
	// Add new type enumerations only if there is no definitions in the server
	ASSEMBLY_OBJECT_TYPE		= 23,
	UDT_OBJECT_TYPE			= 24,
	SCHEMA_OBJECT_TYPE		= 25,
	XML_SCHEMA_OBJECT_TYPE	= 26,
	PARTITION_FUNCTION_OBJECT_TYPE = 27,
	PARTITION_SCHEME_OBJECT_TYPE = 28,
	SERVICE_OBJECT_TYPE		= 29,
	MSGTYPE_OBJECT_TYPE		= 30,
	CONTRACT_OBJECT_TYPE		= 31,
	ROUTE_OBJECT_TYPE			= 32,
	BINDING_OBJECT_TYPE		= 33,
} TRACE_OBJTYPE;

// ===================================================================================
// Columns
// ===================================================================================
typedef enum
{
	//$$TRCEVT_COLUMN_ENUM_START	Do not remove this comment.
	TRACE_COLUMN_TEXT = 1,
	TRACE_COLUMN_BINARYDATA = 2,
	TRACE_COLUMN_DBID = 3,
	TRACE_COLUMN_XACTID = 4,
	TRACE_COLUMN_LINENO = 5,
	TRACE_COLUMN_NTUSERNAME = 6,
	TRACE_COLUMN_NTDOMAIN = 7,
	TRACE_COLUMN_HOST = 8,
	TRACE_COLUMN_CPID = 9,
	TRACE_COLUMN_APPNAME = 10,
	TRACE_COLUMN_LOGINNAME = 11,
	TRACE_COLUMN_SPID = 12,
	TRACE_COLUMN_DURATION = 13,
	TRACE_COLUMN_STARTTIME = 14,
	TRACE_COLUMN_ENDTIME = 15,
	TRACE_COLUMN_READS = 16,
	TRACE_COLUMN_WRITES = 17,
	TRACE_COLUMN_CPU = 18,
	TRACE_COLUMN_PERMISSIONS = 19,
	TRACE_COLUMN_SEVERITY = 20,
	TRACE_COLUMN_SUBCLASS = 21,
	TRACE_COLUMN_OBJID = 22,
	TRACE_COLUMN_SUCCESS = 23,
	TRACE_COLUMN_INDID = 24,
	TRACE_COLUMN_INTDATA = 25,
	TRACE_COLUMN_SERVER = 26,
	TRACE_COLUMN_CLASS = 27,
	TRACE_COLUMN_OBJTYPE = 28,
	TRACE_COLUMN_NESTLEVEL = 29,
	TRACE_COLUMN_STATE = 30,
	TRACE_COLUMN_ERROR = 31,
	TRACE_COLUMN_MODE = 32,
	TRACE_COLUMN_HANDLE = 33,
	TRACE_COLUMN_OBJNAME = 34,
	TRACE_COLUMN_DBNAME = 35,
	TRACE_COLUMN_FILENAME = 36,
	TRACE_COLUMN_OWNERNAME = 37,
	TRACE_COLUMN_ROLENAME = 38,
	TRACE_COLUMN_TARGET_USERNAME = 39,
	TRACE_COLUMN_DBUSER = 40,
	TRACE_COLUMN_LOGINSID = 41,
	TRACE_COLUMN_TARGET_LOGINNAME = 42,
	TRACE_COLUMN_TARGET_LOGINSID = 43,
	TRACE_COLUMN_COLPERMS = 44,

	TOTAL_COLUMNS_V8, 	// Shiloh total columns
	// Start of New Yukon trace columns
	// OLEDB
	TRACE_COLUMN_LINKEDSERVERNAME = 45,
	TRACE_COLUMN_PROVIDERNAME = 46,
	TRACE_COLUMN_METHODNAME = 47,

	TRACE_COLUMN_ROWCOUNTS = 48,
	TRACE_COLUMN_BATCHID = 49,
	TRACE_COLUMN_XACTSEQNO = 50,

	TRACE_COLUMN_EVENTSEQ = 51,

	TRACE_COLUMN_BIGINT1 = 52,
	TRACE_COLUMN_BIGINT2 = 53,
	TRACE_COLUMN_GUID = 54,

	TRACE_COLUMN_INTDATA2 = 55,
	TRACE_COLUMN_OBJID2 = 56,
	TRACE_COLUMN_TYPE = 57,
	TRACE_COLUMN_OWNERID = 58,
	TRACE_COLUMN_PARENTNAME = 59,
	TRACE_COLUMN_ISSYSTEM = 60,

	TRACE_COLUMN_OFFSET = 61,
	TRACE_COLUMN_SOURCE_DBID = 62,
	
	TRACE_COLUMN_SQLHANDLE = 63,
	TRACE_COLUMN_SESSLOGINNAME = 64,
	TRACE_COLUMN_PLANHANDLE = 65,

	TRACE_COLUMN_GROUPID = 66,

	//$$TRCEVT_COLUMN_ENUM_END	Do not remove this comment.
	// DEVNOTE!!!!!!
	// ALL NEW RETAIL COLUMNS MUST BE ADDED ABOVE THIS COMMENT!!!!!
	// You need to follow the following format because this file is processed by perl script!!!!
	//
	// TRACE_COLUMN_XXX = dd,
	//	
	TRACE_TOTAL_RETAIL_COLUMNS,
	TRACE_LAST_RETAIL_COLUMN = TRACE_TOTAL_RETAIL_COLUMNS - 1,	// dummy enum. do not remove.
	
	TRACE_COLUMN_CONTEXT_INFO = 67,
	TRACE_COLUMN_GUID2 = 68,
	TRACE_COLUMN_PARTITION_LOW_KEY = 69,
	TRACE_COLUMN_PARTITION_TABLE_GROUP = 70,
	TRACE_COLUMN_PHYSICAL_DBNAME = 71,
	TRACE_COLUMN_PARTITION_APP_NAME = 72,
	
	// ALL COLUMNS MUST BE ADDED BEFORE TOTAL_COLUMNS!!
	TOTAL_COLUMNS
} TRACE_DATA_COLUMNS;

// Need to expand column map if we reach the following limit.
C_ASSERT(TOTAL_COLUMNS < 256);

// Boolean representation of column bitmap.
// Make sure member defintions matches TRACE_DATA_COLUMNS enumeration!!!
class ColBitmap;

#pragma pack(4)	// Need to match DWORD array in ColBitmap
class ColBoolmap
{
public:
	FORCEINLINE ColBitmap* PBitmap() const { return reinterpret_cast<ColBitmap*>(const_cast<ColBoolmap*>(this)); }

	BOOL bTRACE_INVALID:1;
	BOOL bTRACE_COLUMN_TEXT:1;
	BOOL bTRACE_COLUMN_BINARYDATA:1;
	BOOL bTRACE_COLUMN_DBID:1;
	BOOL bTRACE_COLUMN_XACTID:1;
	BOOL bTRACE_COLUMN_LINENO:1;
	BOOL bTRACE_COLUMN_NTUSERNAME:1;
	BOOL bTRACE_COLUMN_NTDOMAIN:1;
	BOOL bTRACE_COLUMN_HOST:1;
	BOOL bTRACE_COLUMN_CPID:1;
	BOOL bTRACE_COLUMN_APPNAME:1;
	BOOL bTRACE_COLUMN_LOGINNAME:1;
	BOOL bTRACE_COLUMN_SPID:1;
	BOOL bTRACE_COLUMN_DURATION:1;
	BOOL bTRACE_COLUMN_STARTTIME:1;
	BOOL bTRACE_COLUMN_ENDTIME:1;
	BOOL bTRACE_COLUMN_READS:1;
	BOOL bTRACE_COLUMN_WRITES:1;
	BOOL bTRACE_COLUMN_CPU:1;
	BOOL bTRACE_COLUMN_PERMISSIONS:1;
	BOOL bTRACE_COLUMN_SEVERITY:1;
	BOOL bTRACE_COLUMN_SUBCLASS:1;
	BOOL bTRACE_COLUMN_OBJID:1;
	BOOL bTRACE_COLUMN_SUCCESS:1;
	BOOL bTRACE_COLUMN_INDID:1;
	BOOL bTRACE_COLUMN_INTDATA:1;
	BOOL bTRACE_COLUMN_SERVER:1;
	BOOL bTRACE_COLUMN_CLASS:1;
	BOOL bTRACE_COLUMN_OBJTYPE:1;
	BOOL bTRACE_COLUMN_NESTLEVEL:1;
	BOOL bTRACE_COLUMN_STATE:1;
	BOOL bTRACE_COLUMN_ERROR:1;
	BOOL bTRACE_COLUMN_MODE:1;
	BOOL bTRACE_COLUMN_HANDLE:1;
	BOOL bTRACE_COLUMN_OBJNAME:1;
	BOOL bTRACE_COLUMN_DBNAME:1;
	BOOL bTRACE_COLUMN_FILENAME:1;
	BOOL bTRACE_COLUMN_OWNERNAME:1;
	BOOL bTRACE_COLUMN_ROLENAME:1;
	BOOL bTRACE_COLUMN_TARGET_USERNAME:1;
	BOOL bTRACE_COLUMN_DBUSER:1;
	BOOL bTRACE_COLUMN_LOGINSID:1;
	BOOL bTRACE_COLUMN_TARGET_LOGINNAME:1;
	BOOL bTRACE_COLUMN_TARGET_LOGINSID:1;
	BOOL bTRACE_COLUMN_COLPERMS:1;
	BOOL bTRACE_COLUMN_LINKEDSERVERNAME:1;
	BOOL bTRACE_COLUMN_PROVIDERNAME:1;
	BOOL bTRACE_COLUMN_METHODNAME:1;
	BOOL bTRACE_COLUMN_ROWCOUNTS:1;
	BOOL bTRACE_COLUMN_BATCHID:1;
	BOOL bTRACE_COLUMN_XACTSEQNO:1;
	BOOL bTRACE_COLUMN_EVENTSEQ:1;
	BOOL bTRACE_COLUMN_BIGINT1:1;
	BOOL bTRACE_COLUMN_BIGINT2:1;
	BOOL bTRACE_COLUMN_GUID:1;
	BOOL bTRACE_COLUMN_INTDATA2:1;
	BOOL bTRACE_COLUMN_OBJID2:1;
	BOOL bTRACE_COLUMN_TYPE:1;
	BOOL bTRACE_COLUMN_OWNERID:1;
	BOOL bTRACE_COLUMN_PARENTNAME:1;
	BOOL bTRACE_COLUMN_ISSYSTEM:1;
	BOOL bTRACE_COLUMN_OFFSET:1;
	BOOL bTRACE_COLUMN_SOURCE_DBID:1;
	BOOL bTRACE_COLUMN_SQLHANDLE:1;
	BOOL bTRACE_COLUMN_SESSLOGINNAME:1;
	BOOL bTRACE_COLUMN_PLANHANDLE:1;
	BOOL bTRACE_COLUMN_GROUPID:1;
	BOOL bTRACE_COLUMN_CONTEXT_INFO:1;
	BOOL bTRACE_COLUMN_GUID2:1;
	BOOL bTRACE_COLUMN_PARTITION_LOW_KEY:1;
	BOOL bTRACE_COLUMN_PARTITION_TABLE_GROUP:1;
	BOOL bTRACE_COLUMN_PHYSICAL_DBNAME:1;
	BOOL bTRACE_COLUMN_PARTITION_APP_NAME:1;
};
#pragma pack()

// Special columns will begin at 0xfffe which is the highest valid USHORT (colid) and
// work down.  All new special columns should be added to the bottem of this list.
// DEVNOTE: These special columns are placed at the top of the USHORT range so that we can add
//			add new columns to our heart's delight, and we will not need to worry about
//			problems until we approach 64k columns.
typedef enum
{
 TRACE_VERSION			=	0xfffe,
 TRACED_OPTIONS			=	0xfffd,
 TRACED_EVENTS			=	0xfffc,
 TRACED_FILTERS			=	0xfffb,
 TRACE_START			=	0xfffa,
 TRACE_STOP				=	0xfff9,
 TRACE_ERROR			=	0xfff8,
 TRACE_SKIPPED_RECORDS	=	0xfff7,
 TRACE_BEGIN_RECORD		=	0xfff6,
 TRACE_TEXT_FILTERED	=	0xfff5,
 TRACE_REPEATED_BASE	=	0xfff4,
 TRACE_REPEATED_DATA	=	0xfff3,
 TRACE_FLUSH			=	0xfff2,
} TRACE_SPECIAL_COLUMNS;

#define IS_SPECIAL_COLUMN(icol)	(((icol) & 0xFFF0) == 0xFFF0)

typedef struct tagTraceCategory
{
	WCHAR*	pwszCategory_Name;
	SHORT	sCategory_Id;
	BYTE	bCategory_Type;
	bool	fDisabled;
} TRACE_CATEGORY;

//
// Event Categories
//
typedef enum
{
	//$$EVENT_CATEGORY_START	Do not remove this comment.
	TRCAT_INVALID = 0,
	TRCAT_CURSORS = 1,
	TRCAT_DATABASE = 2,
	TRCAT_ERRORSANDWARNINGS = 3,
	TRCAT_LOCKS = 4,
	TRCAT_OBJECTS = 5,
	TRCAT_PERFORMANCE = 6,
	TRCAT_SCANS = 7,
	TRCAT_SECURITYAUDIT = 8,
	TRCAT_SERVER = 9,
	TRCAT_SESSIONS = 10,
	TRCAT_STOREDPROCEDURES = 11,
	TRCAT_TRANSACTIONS = 12,
	TRCAT_TSQL = 13,
	TRCAT_USERCONFIGURABLE = 14,
	TRCAT_OLEDB = 15,
	TRCAT_BROKER = 16,
	TRCAT_FULLTEXT = 17,
	TRCAT_DEPRECATION = 18,
	TRCAT_PROGRESS = 19,
	TRCAT_CLR = 20,
	TRCAT_QNOTIF = 21,
	TRCAT_PSS_DIAG = 22,
	//$$EVENT_CATEGORY_END		Do not remove this comment.
	// DEVNOTE!!!!!!
	// ALL NEW RETAIL CATEGORIES MUST BE ADDED ABOVE THIS COMMENT!!!!!
	// You need to follow the following format because this file is processed by perl script!!!!
	//
	// TRCAT_XXX = dd,
	//

	TRC_NEXT_RETAIL,		// Dummy enum. Do not remove.
	TRC_TOTAL_RETAIL = TRC_NEXT_RETAIL -1,  // Total number of VALID categories (beginning at 1)
	
	// DEVNOTE!!!!!!
	// ALL NEW TEMP (NON GOLDEN_BITS) CATEGORIES MUST BE ADDED BELLOW THIS COMMENT!!!!!
	// You should not assign values explicitly. The category IDs will change
	// after more categories are added. Please use the following format:
	//
	// TRCAT_XXX,
	//
	TRCAT_SEREPL,
	TRCAT_ASYNCTRANSPORT,
	TRCAT_CLOUDMGMT,
	TRCAT_CLOUD_PM,
	
#ifndef GOLDEN_BITS
	TRCAT_MATRIXDB,
	TRCAT_TEMP,
#endif

	// DEVNOTE!!!!!!
	// ALL NEW DEBUG CATEGORIES MUST BE ADDED BELLOW THIS COMMENT!!!!!
	// You should not assign values explicitly. The category IDs will change
	// after more categories are added. Please use the following format:
	//
	// TRCAT_XXX,
	//
	
#ifdef DEBUG	
	TRCAT_DBG_ONLY,
#endif DEBUG

	TRC_NEXT,		// Dummy enum. Do not remove.
	TRC_TOTAL = TRC_NEXT-1,	// Total number of VALID categories (beginning at 1)
	
	// Special trace category, used for events disabled in certian build flavors.
	//
	TRCAT_DISABLED = TRC_NEXT,
} TRACE_CATEGORY_ID;

// Non-GOLDEN_BITS trace categories having events with IDs lower than
// TOTAL_RETAIL_EVENTS must be listed here as equivalent to TRCAT_DISABLED.
// Otherwise GOLDEN_BITS build will break.
//
#ifdef GOLDEN_BITS
#define TRCAT_MATRIXDB  TRCAT_DISABLED
#define TRCAT_TEMP      TRCAT_DISABLED
#endif

// Debug trace categories having events with IDs lower than
// TOTAL_RETAIL_EVENTS must be listed here as equivalent to TRC_DISABLED.
// Otherwise RETAIL build will break.
//
#ifndef DEBUG
#define TRCAT_DBG_ONLY  TRCAT_DISABLED
#endif


// TRACE_BEGIN_RECORD
// Shiloh
// USHORT event id
// USHORT logical columns

// Yukon
// USHORT event id
// ULONG record length


//NOTE: if DataType is TRACE_BYTES, bFilterable has to be false
typedef struct trace_column_info
{
	USHORT					id;
	ETraceDataTypes			eDataType;
	BYTE					bFilterable;
	BYTE					bRepeatable;
	BYTE					bRepeatedBase;
	WCHAR*					pwszColumnName;
	bool					fPreFilterable:1;
	bool					fMandatory:1;
	bool					fDisabled:1;
} TRACE_COLUMN_INFO;

//
#define TRACE_REPEATABLE_COLUMNS	9 // Increase this if you add repeatable data!

enum eTraceHeaderOptions
	{
	eTraceFileRolledOver = 0x1,
	eTraceFirstFile		 = 0x2,
	};

#pragma pack(1)
// Header of a file.
// Shiloh Header
typedef  struct V8TrcFileHeader
{
	long	lVersion;
	ULONG	lHdrSize;
	ULONG	lOptions;
	USHORT	ColumnOrder[TOTAL_COLUMNS_V8];
	USHORT	ColumnAggregation[TOTAL_COLUMNS_V8];
} V8TrcFileHeader;

// Yukon header
typedef  struct TrcFileHeader
{
	USHORT	usUnicodeTag;	// 0xFEFF		(2)
	USHORT	usHeaderSize;	// sizeof(struct TrcFIleHeader)	(4)
	USHORT	usTraceVersion;	// Trace file format version	(6)
	WCHAR	wszProviderName[128];	// L"Microsoft SQL Server"	(262)
	WCHAR	wszDefinitionType[64];	// Unused		(390)
	BYTE	bMajorVersion;	// (391)
	BYTE	bMinorVersion;	// (392)
	USHORT	usBuildNumber;	// (394)
	ULONG	u_ulHeaderOptions;	// UNALLIGNED (398)
	WCHAR	wszServer[128];	// server name	(654)
	USHORT	usRepeatBase;	// spid	(656)

	// We use MEMCOPY to prevent Win64 data misalignment fault.
	inline void SetHeaderOptions(__in ULONG ulHeaderOption)
	{
		MEMCOPY(&ulHeaderOption, &u_ulHeaderOptions, sizeof(ULONG));
	}

	inline ULONG UlHeaderOptions() const
	{
		ULONG ulRet;
		MEMCOPY(&u_ulHeaderOptions, &ulRet, sizeof(ULONG));
		return ulRet;
	}
} TrcFileHeader;

// Begin Record Data
typedef struct TrcBeginRecordData
{
	USHORT	usEventId;
	LONG	u_lRecordLength;	// unaligned!!

	// We use MEMCOPY to prevent Win64 data misalignment fault.
	inline LONG LRecordLength() const
	{
		LONG lRet;
		MEMCOPY(&u_lRecordLength, &lRet, sizeof(LONG));
		return lRet;
	}

	inline void SetRecordLength(__in LONG lRecordLength)
	{
		MEMCOPY(&lRecordLength, &u_lRecordLength, sizeof(LONG));
	}
	
} TrcBeginRecordData;

typedef struct TracedOptionsData
{
	BYTE	betpOptions;
	ULONG	u_ulOptions;		// unaligned
	BYTE	betpMaxSize;
	INT64	u_i64MaxSize;	// unaligned
	BYTE	betpStopTime;
	SYSTEMTIME	stStopTime;

	// We use MEMCOPY to prevent Win64 data misalignment fault.
	inline void SetOptions(__in ULONG ulOptions)
	{
		MEMCOPY(&ulOptions, &u_ulOptions, sizeof(ULONG));
	}

	inline void SetMaxSize(__in INT64 i64MaxSize)
	{
		MEMCOPY(&i64MaxSize, &u_i64MaxSize, sizeof(INT64));
	}

	inline void SetStopTime(__in SYSTEMTIME* pst)
	{
		MEMCOPY(pst, &stStopTime, sizeof(SYSTEMTIME));
	}
	
} TracedOptionsData;
#pragma pack()

#define UNICODETAG	0xFEFF
#define TRACE_FORMAT_VERSION	1

// Enumeration of trace event flags which describe notification attributes. Values have to be or'able.

enum TRCEventFlags
{
	TRCEF_OFF_EVT	= 0x0000, // Not supported for Event Notifications
	TRCEF_ON_EVT	= 0x0001, // Supported for Event Notifications
	TRCEF_SEC_EVT	= 0x0002, // Event requiring "Monitor Security Events" permission
	TRCEF_MGT_EVT	= 0x0004, // Event requiring "Monitor Management Events" permission
	TRCEF_USR_EVT	= 0x0008, // Event requiring "Monitor User Events" permission
	TRCEF_TXT_XML	= 0x0010, // TextData is already in XML format.
};

// Map event classes with names
typedef struct tagTrace_event_info
{
	ColBoolmap				colmap;
	USHORT					id;
	SHORT					sCategory_Id;
	WCHAR*					pwszEventName;
	USHORT					wEvNotifFlags;		// notification and XML flags
	bool					fDisabled;
} TRACE_EVENT_INFO;

// Max trace column size is 1GB
const ULONG g_cbMaxTraceColumnSize = 0x40000000;
// Max size of a TVPs trace binary format 
const ULONG x_cbMaxTVPSize = g_cbMaxTraceColumnSize/4;

extern TRACE_EVENT_INFO g_rgTraceEventInfo[];

#endif // TRCCOMN_H_



