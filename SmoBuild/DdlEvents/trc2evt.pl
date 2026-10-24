# Copyright (c) Microsoft Corporation.
# @File: trc2evt.pl
# @Owner: jayc
#
# Purpose:
#	This script reads SQLTrace metadata and writes SQL Event and ETW defintions
#
# Usage:
#	perl trc2evt.pl $(NTDBMS) $(O) option
#		trccomn.h	Full path to trccomn.h
#		trcmeta.cpp	Full path to trcmeta.cpp
#		$(O)		Object directory
#		option		0: create normal output files
#					1: create sql server etwsetup definition
#		build_type	SQLSERVER_MATRIXDISABLED: skip matrix events/event categories
#					SQLSERVER: include matrix events/categories
#
#	This creates following files in $(O) directory.
#		grpenum.inc		group enumeration
#		grpdefs.inc		group definition array
#		parinfo.inc		parameter definition arrays
#		schema.inc		schema array
#		hierarch.txt		event hierarchy graph
#		etwcls.mof		ETW description mof file
#		etwguid.inc		ETW class guids
# @EndHeader@
use strict;
use warnings "all";

# Get full path to trccommn.h
my $trccomn_h = $ARGV[0];

# Get full path to trcmeta.cpp
my $trcmeta_cpp = $ARGV[1];

# Get output directory
my $OutputDir = $ARGV[2];

# Get option
my $option = $ARGV[3];

# Get build type
my $strBuildType = $ARGV[4];

# Starting event class id
# EVENT_CLASS_START = 10
my $EventClassStart = 10;

# Globals
#
my (@rgsColumnNames, @rgsColumnTypes);
my (@rgsEventIDs, @rgsEventNames, @rgsEventRawNames, @rgwEvNotifFlags, @rgsColumnIDs, @rgsCategoryIDs, @rgsCategoryNames);
my (%asiEventIDs, %asiColumnIDs, %asiCategoryIDs, %assEventColumnBindings, %assEventCategories, %asfCategoryUsed);

# Prototypes
#
sub GetPermissionString($);
sub ParamDef($$$);
sub ETWParamDef($$);
sub ETWParamDefEscaped($$);
sub XMLParamDef($$);

###################################
# reading trccomn.h.
###################################
open(TRCCOMNH, $trccomn_h) || die "Cannot open ". $trccomn_h . "\n";

# These status variables have 3 states: 0 (not started), 1 (started reading), 2 (done reading)
my $EventClassEnumStatus = 0;
my $ColumnEnumStatus = 0;
my $CategoryEnumStatus = 0;

while(<TRCCOMNH>)
{
	# Look event class enumeration.
	# %asiEvnetIDs defines mapping from event class to value.	$asiEventIDs{"POST_RPC_EVENT_CLASS"} = 10
	# @rgsEventIDs defines mapping from value to event class.	$rgsEventIDs[10] = "POST_RPC_EVENT_CLASS"
	# POST_RPC_EVENT_CLASS 	= 10,
	if($EventClassEnumStatus != 2)
	{
		if($EventClassEnumStatus == 1)
		{
			# Reading is already started. Match pattern and watch for END
			if(/^\s*(\w+)\s*=\s*(\d+)\s*,/)
			{
				$asiEventIDs{$1} = $2;
				$rgsEventIDs[$2] = $1;
			}
			elsif(/\/\/\$\$EVENT_CLASS_END/)
			{
				$EventClassEnumStatus = 2;		#done
			}
		}
		elsif(/\/\/\$\$EVENT_CLASS_START/)
		{
			$EventClassEnumStatus = 1;
		}
	}
	
	# Look column enumeration
	# %asiColumnIDs defines mapping from column enum to value.	$asiColumnIDs{"TRACE_COLUMN_TEXT"} = 1
	# @rgsColumnIDs defines mapping from value to column enu.		$rgsColumnIDs[1] = "TRACE_COLUMN_TEXT"
	#	TRACE_COLUMN_TEXT = 1, 
	if($ColumnEnumStatus != 2)
	{
		if($ColumnEnumStatus == 1)
		{
			# Reading is already started. Match pattern and watch for END
			if(/(TRACE_COLUMN_\w+)\s*=\s*(\d+),/)
			{
				$asiColumnIDs{$1} = $2;
				$rgsColumnIDs[$2] = $1;
			}
			elsif(/\/\/\$\$TRCEVT_COLUMN_ENUM_END/)
			{
				$ColumnEnumStatus = 2;
			}
		}
		elsif(/\/\/\$\$TRCEVT_COLUMN_ENUM_START/)
		{
			$ColumnEnumStatus = 1;
		}
	}
	
	# Look category enumeration.
	# %asiCategoryIDs defines mapping from category id to value.	$asiCategoryIDs{"TRCAT_CURSORS"} = 1
	# @rgsCategoryIDs defines mapping from value to id.	$rgsCategoryIDs[1] = "TRCAT_CURSORS"
	#	TRCAT_CURSORS = 1,
	if($CategoryEnumStatus != 2)
	{
		if($CategoryEnumStatus == 1)
		{
			# Reading is already started. Match pattern and watch for END
			if(/(TRCAT_\w+)\s*=\s*(\d+),/)
			{
				$asiCategoryIDs{$1} = $2;
				$rgsCategoryIDs[$2] = $1;
			}
			elsif(/\/\/\$\$EVENT_CATEGORY_END/)
			{
				$CategoryEnumStatus = 2;
			}
		}
		elsif(/\/\/\$\$EVENT_CATEGORY_START/)
		{
			$CategoryEnumStatus = 1;
		}
	}
}
close(TRCCOMNH);

# At this point, all status variables should be 2 (done).
die "Failed to read Event Class Enumeration.\n" if $EventClassEnumStatus != 2;
die "Failed to read Event Column Enumeration.\n" if $ColumnEnumStatus != 2;
die "Failed to read Event Category Enumeration.\n", if $CategoryEnumStatus != 2;



###########################################
# reading trcmeta.cpp
###########################################
open(TRCMETACPP,$trcmeta_cpp) || die "Cannot open ". $trcmeta_cpp . "\n";

my $CategoryMapStatus = 0;
my $ColumnInfoStatus = 0;
my $EventInfoStatus = 0;

while(<TRCMETACPP>)
{
	#Read category map.
	#@rgsCategoryNames holds category names.		$rgsCategoryNames[1] = "TRC_CURSORS"
	#	{L"Cursors", 				TRCAT_CURSORS,				TRCAT_TYPE_NORMAL},
	if($CategoryMapStatus != 2)
	{
		if($CategoryMapStatus == 1)
		{
			if(/\{\s*L\"([a-zA-Z ]+)\"\s*,\s*(TRCAT_\w+)\s*,\s*TRCAT_TYPE_\w+\s*\}/)
			{
				my $name = $1;
				$name =~ tr/a-z /A-Z_/;	# to UpperCase. Space to Underline
				$rgsCategoryNames[$asiCategoryIDs{$2}] = "TRC_" . $name;
			}
			elsif(/\/\/\$\$CATEGORY_MAP_END/)
			{
				$CategoryMapStatus = 2;	#done
			}
		}
		elsif(/\/\/\$\$CATEGORY_MAP_START/)
		{
			$CategoryMapStatus = 1;	#start
		}
	}

	#Read column info array
	# $rgsColumnNames[1] = "TextData"
	# $rgsColumnTypes[1] = "TRACE_NTEXT"
	# 	{TRACE_COLUMN_TEXT,		TRACE_NTEXT,		1,	0,	0,	L"TextData"},
	if($ColumnInfoStatus != 2)
	{
		if($ColumnInfoStatus == 1)
		{
			if(/\{\s*(\w+)\s*,\s*(\w+)\s*,\s*\d\s*,\s*\d\s*,\s*\d\s*,\s*L\"(\w+)\"\s*.*\}\s*,/)
			{
				$rgsColumnNames[$asiColumnIDs{$1}] = $3;
				$rgsColumnTypes[$asiColumnIDs{$1}] = $2;
			}
			elsif(/\/\/\$\$TRCCOLUMNINFO_END/)
			{
				$ColumnInfoStatus = 2;		#done
			}
		}
		elsif(/\/\/\$\$TRCCOLUMNINFO_START/)
		{
			$ColumnInfoStatus = 1;		#start
		}
	}

	#Read event info array
	# $assEventColumnBindings{"PRE_LANG_EVENT_CLASS"} = "0,1,0,1,0,0,.."
	# $assEventCategories{"PRE_LANG_EVENT_CLASS"}  = "TRCAT_TSQL"
	# $rgsEventNames[13] = "SQL_BATCH_STARTING"
	# $asfCategoryUsed{"TRCAT_TSQL"} = 1	if this category is used
	#{{0,1,0,1,0,0,1,1,1,1,1,1,1,0,1,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,0,1,1,1},	PRE_LANG_EVENT_CLASS,	TRCAT_TSQL,	L"SQL:BatchStarting",	1,1},
	if($EventInfoStatus != 2)
	{
		if($EventInfoStatus == 1)
		{
			# Skip internal server diagnostics
			if(/^.*TRCAT_PSS_DIAG.*$/)
			{
				#Do nothing.
			}
			elsif(/^\s*\{\{([01,]+)\}\s*,\s*(\w+)\s*,\s*(\w+)\s*,\s*L\"(.+)\"\s*,\s*(\w+)\s*\}/)
			{
				my $colmap = $1;
				my $classDef = $2;
				my $categoryDef = $3;
				my $className = $4;
				my $wEvNotifFlags = $5;
				
				$assEventColumnBindings{$classDef} = $colmap;
				$assEventCategories{$classDef} = $categoryDef;
				$asfCategoryUsed{$categoryDef} = 1;

				# store raw name
				$rgsEventRawNames[$asiEventIDs{$classDef}] = $className;
				
				#normalize name
				$className =~ tr/a-z: \/\.(/A-Z_/; #uppercase, space, etc to underline.
				if($className =~ /(.*)\)$/)  #special case when name ends with ')'
				{
					$className = $1;
				}
				$rgsEventNames[$asiEventIDs{$classDef}] = $className;
				$rgwEvNotifFlags[$asiEventIDs{$classDef}] = $wEvNotifFlags;
			}
			elsif(/^\s*\{\{([01,]+)\}\s*,\s*(\w+)\s*,\s*(\w+)\s*,\s*L\"(.+)\"\s*\}/)
			{
				my $colmap = $1;
				my $classDef = $2;
				my $categoryDef = $3;
				my $className = $4;

				next if( $classDef eq "TRACE_INVALID_EVENT");

				$assEventColumnBindings{$classDef} = $colmap;
				$assEventCategories{$classDef} = $categoryDef;
				$asfCategoryUsed{$categoryDef} = 1;

				# store raw name
				$rgsEventRawNames[$asiEventIDs{$classDef}] = $className;
				
				#normalize name
				#Make upper case.
				#Substitute non-alpha-numeric with underline
				$className =~ tr/a-z: \/\.(>/A-Z_/; 
				if($className =~ /(.*)\)$/)  #special case when name ends with ')'
				{
					$className = $1;
				}
				$rgsEventNames[$asiEventIDs{$classDef}] = $className;
				$rgwEvNotifFlags[$asiEventIDs{$classDef}] = 0;
			}
			elsif(/\/\/\$\$TRCEVENTINFO_END/)
			{
				$EventInfoStatus = 2;
			}
		}
		elsif(/\/\/\$\$TRCEVENTINFO_START/)
		{
			$EventInfoStatus = 1;
		}
	}
}
close(TRCMETACPP);

# At this point, all status variables should be set to 2 (done)
die "Failed to read Event Category Array.\n" if $CategoryMapStatus != 2;
die "Failed to read Event Column Info Array.\n" if $ColumnInfoStatus != 2;
die "Failed to read Event Info Array.\n" if $EventInfoStatus != 2;

#
# print event group enumeration
if($option == 0)
{
	open(FOUT, ">" . $OutputDir . "\\grpenum.inc") || die "Cannot write to " . $OutputDir . "\\grpenum.inc\n";
	print FOUT "// Extended Event Group for SQLTrace\n";
	print FOUT "enum ETraceEventGroup {\n";
	print FOUT "\tEGROUP_TRCAT_START = EGROUP_TRCAT_ALL,  // Setting beginning\n";
	for (my $i = 1; $i <= $#rgsCategoryIDs; $i++)
	{
		# DEVNOTE: We cannot remove unused groups because events.h cannot include generated file.
		#if($asfCategoryUsed{$rgsCategoryIDs[$i]} == 1)
			print FOUT "\tEGROUP_" , $rgsCategoryIDs[$i], ",\n";
	}
	print FOUT "};";
	close(FOUT);

#
# print event group name
	open(FOUT, ">" . $OutputDir . "\\grpdefs.inc") || die "Cannot write to " . $OutputDir . "\\grpdefs.inc\n";
	print FOUT "// Extended Event Groups for SQLTrace\n";
	for (my $i = 1; $i <= $#rgsCategoryIDs; $i++)
	{
		# DEVNOTE: We cannot remove unused groups because events.h cannot include generated file.
		#if($asfCategoryUsed{$rgsCategoryIDs[$i]} == 1)
			print FOUT "\t\{(EEventGroup)EGROUP_", $rgsCategoryIDs[$i], ",\tEGROUP_TRCAT_ALL,\tL\"", $rgsCategoryNames[$i], "\",\t(EMDEventType)(x_eet_Group_Traceat_All + $i),\tETYP_ON_INVALID,\t0,\t0,\t0\},\n";
	}
	close(FOUT);

#
# print paraminfo array
	open(FOUT, ">" . $OutputDir . "\\parinfo.inc") || die "Cannot write to " . $OutputDir . "\\parinfo.inc\n";
	my $ColumnCount = @rgsColumnNames;
	print FOUT "//----------------------------------------------------------------\n";
	print FOUT "// TraceEventTag Enum\n";
	print FOUT "//----------------------------------------------------------------\n";
	print FOUT "enum {\n";
	for (my $col = 1; $col < $ColumnCount; $col++)
	{
		#print param defs for active cols except for SPID/EventClass
		if($col != 12 && $col != 27 )
		{
			my $tag = "TraceEventTag_" . $rgsColumnNames[$col];
			my $coli = $col;
			$coli-- if($coli >= 12);
			$coli-- if($coli >= 27);
			print FOUT "\t$tag = $coli, \n";
		}
	}
	print FOUT "\tTraceEventTag_TextDataXml = " . ($ColumnCount - 2 ). ",\n";

	print FOUT "} TraceEventEnum;\n\n";

	print FOUT "// Parameter Info arrays for SQLTrace\n";
	for (my $i = $EventClassStart; $i <= $#rgsEventIDs; $i++)
	{
		if(defined($rgsEventNames[$i]) && $rgsEventNames[$i] ne "" && ($rgwEvNotifFlags[$i] & 1))
		{
			print FOUT "//----------------------------------------------------------------\n";
			print FOUT "// ", $rgsEventIDs[$i], " (", $i, ") Event Instance Schema\n";
			print FOUT "//----------------------------------------------------------------\n";
			print FOUT "STATIC EVENT_PARAM_INFO	s_rgParamInfo",$rgsEventIDs[$i],"[] =\n{\n";
			my $binding = $assEventColumnBindings{$rgsEventIDs[$i]};
			my @cols = split(/,/,$binding);
			for (my $col = 1; $col <= $#cols && $col <= $#rgsColumnNames; $col++)
			{
				#print param defs for active cols except for SPID/EventClass
				if($col != 12 && $col != 27 && $cols[$col] == 1)
				{
					print FOUT "\t", ParamDef($col, ($rgwEvNotifFlags[$i] & 0x0010) != 0, 0);
				}
			}

			print FOUT "};\n\n";
		}
	}

	# Dump All Columns
	#
	print FOUT "//----------------------------------------------------------------\n";
	print FOUT "// TraceAllColumns Event Instance Schema\n";
	print FOUT "//----------------------------------------------------------------\n";
	print FOUT "STATIC EVENT_PARAM_INFO_DEF	s_rgParamInfoTraceAllColumns[] =\n{\n";
	for (my $col = 1; $col < $ColumnCount; $col++)
	{
		#print param defs for active cols except for SPID/EventClass
		if($col != 12 && $col != 27 )
		{
			#print "h". ParamDef($col, 0). "\n";
			print FOUT "\t",ParamDef($col, 0, 1);
		}
	}
	# TextData XML
	print FOUT "\t",ParamDef(1, 1, 1),"\n";
	print FOUT "};\n\n";
	print FOUT "STATIC ULONG s_cParamInfoTraceAllColumns = NUMELEM(s_rgParamInfoTraceAllColumns);\n";

	close(FOUT);

	#
# print schema array
	open(FOUT, ">" . $OutputDir . "\\schema.inc") || die "Cannot write to " . $OutputDir . "\\schema.inc\n";
	print FOUT "// Schema array extended for SQLTrace\n";
	for (my $i = $EventClassStart; $i <= $#rgsEventIDs; $i++)
	{
		my $eventkey = $rgsEventIDs[$i];
		if( !defined($eventkey))
		{
			print FOUT "//----------\n// Missing Event (", $i, ") Schema\n//-----------\n";
			print FOUT "\t{ETYP_ON_INVALID},\n";
			next;
		}

		print FOUT "//----------\n// ", $eventkey,"(", $i, ") Schema\n//-----------\n";
		if(defined($rgsEventNames[$i]) && $rgsEventNames[$i] ne "" && ($rgwEvNotifFlags[$i] & 1))
		{
			print FOUT "\t{\t(EEventType)(ETYP_TRACE_START + ", $i -$EventClassStart,"),\n";
			print FOUT "\t\t(EMDEventType)(x_eet_Trace_Start + ", $i,"),\n";
			print FOUT "\t\tEOBJTYP_SERVER,\n\t\tEFLAG_ASYNC_ONLY | EFLAG_MAX_DIALOGS";
			if($rgwEvNotifFlags[$i] & 2)
			{
				print FOUT " | EFLAG_SECURITY_EVT";
			}
			if($rgwEvNotifFlags[$i] & 4)
			{
				print FOUT " | EFLAG_MANAGEMENT_EVT";
			}
			if($rgwEvNotifFlags[$i] & 8)
			{
				print FOUT " | EFLAG_USER_EVT";
			}
			print FOUT ",\n";
			print FOUT "\t\tL\"",$rgsEventNames[$i],"\",\n";
			print FOUT "\t\t0,\n";
			print FOUT "\t\ts_rgParamInfo",$eventkey,",\n";
			print FOUT "\t\tNUMELEM(s_rgParamInfo",$eventkey,"),\n";
			print FOUT "\t\t(EEventGroup)EGROUP_",$assEventCategories{$eventkey},"\n";
			print FOUT "\t},\n";
		}
		else
		{
			print FOUT "\t{ETYP_ON_INVALID},\n";
		}
	}
	close(FOUT);

#
# Generate event hierarchy graph for reference.  This file is not used for compilation.
	open(FOUT, ">" . $OutputDir . "\\hierarchy.txt") || die "Cannot write to " . $OutputDir . "\\hierarchy.txt.\n";
	print FOUT "==========  SQL Trace event hierarchy ==================\n\n";
	print FOUT "ALL_EVENTS\n";
	print FOUT "|____ TRC_ALL_EVENTS\n";
	for (my $i = 1; $i <= $#rgsCategoryIDs; $i++)
	{
		print FOUT "|       |____ ", $rgsCategoryNames[$i], "\n";
		for (my $j = $EventClassStart; $j <= $#rgsEventIDs; $j++)
		{
			if(defined($rgsEventNames[$j]) && $rgsEventNames[$j] ne "" && ($rgwEvNotifFlags[$j] & 1) && $assEventCategories{$rgsEventIDs[$j]} eq $rgsCategoryIDs[$i])
			{
				print FOUT "|       |       |____ ", $rgsEventNames[$j], " (", $j, ")\n";
			}
		}
	}

	print FOUT "\n\n=========  List of supported SQL Event Classes (Permissions) ==============\n\n";
	for (my $i = $EventClassStart; $i <= $#rgsEventIDs; $i++)
	{
		if(defined($rgsEventNames[$i]) && $rgsEventNames[$i] ne "" && ($rgwEvNotifFlags[$i] & 1))
		{
			print FOUT $i, "\t", $rgsEventNames[$i], "\t(", &GetPermissionString($i),")\n";
		}
	}

	close(FOUT);

#
# MOF file for Sql Trace ETW
#

	# ----------------------------------------------------------------
	# To get UTF16-LE output you need to play a few games with perl...
	# Form more information, take a look at:
	# 	http://blogs.msdn.com/brettsh/archive/2006/06/07/620986.aspx
	# ----------------------------------------------------------------
	open(FOUT, ">:raw:encoding(UTF16-LE):crlf:utf8", $OutputDir . "\\etwcls.mof")
		|| die "Cannot write to " . $OutputDir . "\\etwcls.mof\n";
	print FOUT "\x{FEFF}";  # print BOM (Byte Order Mark) for the unicode file
	# ----------------------------------------------------------------

	print FOUT "// ************************************************************************\n";
	print FOUT "//	\tCopyrights (c) Microsoft Corporation\n//\n";
	print FOUT "// ************************************************************************\n\n";
	print FOUT "#pragma classflags(\"forceupdate\")\n";
	print FOUT "#pragma namespace (\"\\\\\\\\.\\\\Root\\\\WMI\")\n";
	print FOUT "#pragma autorecover\n\n";

	print FOUT "#pragma deleteclass (\"REPLACE_INSTNAMETrace\", NOFAIL )\n\n";
	print FOUT "[ Dynamic,\n  Description(\"REPLACE_INSTNAME Trace\"),\n";
	print FOUT "  Guid(\"REPLACE_INSTGUID\"),\n";
	print FOUT "  DisplayName(\"REPLACE_INSTNAME Trace\")\n";
	print FOUT "]\n";
	print FOUT "class REPLACE_INSTNAMETrace:EventTrace\n{\n};\n\n";

	for (my $i = $EventClassStart; $i <= $#rgsEventIDs; $i++)
	{
		if($rgsEventNames[$i] ne "")
		{
			print FOUT "//----------------------------------------------------------------\n";
			print FOUT "// ", $rgsEventRawNames[$i], " (", $i, ") Event Instance Schema\n";
			print FOUT "//----------------------------------------------------------------\n";

			#generic class
			print FOUT "#pragma deleteclass (\"SQLTraceEvent", $i ,"\", NOFAIL )\n\n";
			print FOUT "[ Dynamic,\n";
			print FOUT "  Description(\"", $rgsEventRawNames[$i], "\"),\n";
			printf FOUT "  Guid(\"84e993e6-%04x-4d70-97ef-3824ed2c4e93\"),\n", $i;
			print FOUT "  DisplayName(\"", $rgsEventRawNames[$i], "\")\n";
			print FOUT "]\n";
			print FOUT "class SQLTraceEvent", $i, " : REPLACE_INSTNAMETrace\n{\n};\n\n";

			#type 0 class
			print FOUT "#pragma deleteclass (\"SQLTraceEvent", $i ,"_TYPE0\", NOFAIL )\n\n";
			print FOUT "[ Dynamic,\n";
			print FOUT "  Description(\"", $rgsEventRawNames[$i], " Type0\"),\n";
			print FOUT "  EventType(0),\n";
			print FOUT "  EventTypeName(\"0\")\n";
			print FOUT "]\n";
			print FOUT "class SQLTraceEvent", $i, "_TYPE0 : SQLTraceEvent", $i, "\n{\n";
			my $binding = $assEventColumnBindings{$rgsEventIDs[$i]};
			my @cols = split(/,/,$binding);
			my $idx = 0;
			for (my $col = 1; $col <= $#cols; $col++)
			{
				#print param defs for active cols except for ServerName
				if($col != 26 && $cols[$col] == 1)
				{
					$idx = $idx + 1;
					print FOUT ETWParamDef($col, $idx), "\n";
				}
			}

			print FOUT "};\n";
		}
	}
	close(FOUT);

#
# GUID include file which lists all ETW event class GUIDs
#
	open(FOUT, ">" .$OutputDir . "\\etwguid.inc") || die "Cannot write to " . $OutputDir . "\\etwguid.inc\n";
	print FOUT "// Array of Event Class Info\n\n";

	for (my $i = $EventClassStart; $i <= $#rgsEventIDs; $i++)
	{
		if(defined($rgsEventNames[$i]) && $rgsEventNames[$i] ne "")
		{
			print FOUT "//----------------------------------------------------------------\n";
			print FOUT "// ", $rgsEventIDs[$i], " (", $i, ") Event Class GUID\n";
			printf FOUT "// {b0e2%04x-4387-4e47-a835-215da163f298}\n", $i;
			print FOUT "//----------------------------------------------------------------\n";
			printf FOUT "\t{{0xb0e2%04x, 0x4387, 0x4e47, {0xa8, 0x35, 0x21, 0x5d, 0xa1, 0x63, 0xf2, 0x98}}, %d},\n", $i, $i;
		}
	}
	close(FOUT);

	# Dump All Columns to XML
	#
	open(FOUT, ">" .$OutputDir . "\\events_trace_template.xst") || die "Cannot write to " . $OutputDir . "\\events_trace_template.xst\n";
	for (my $col = 1; $col < $ColumnCount; $col++)
	{
		#print param defs for active cols except for SPID/EventClass
		if($col != 12 && $col != 27 )
		{
			print FOUT XMLParamDef($col, 0) . "\n";
		}
	}
	close (FOUT);
}


# etwsetup include file
# 0: no parameter (NOPARAM)
# 1: instance name (INSTNAME)
# 2: instance guid (INSTGUID)
if($option == 1)
{
	open(FOUT, ">" . $OutputDir . "\\etwsetup.inc") || die "Cannot write to " . $OutputDir . "\\etwsetup.inc\n";
	print FOUT "{NOPARAM, L\"// ************************************************************************\\n\"},\n";
	print FOUT "{NOPARAM, L\"//	\\tCopyrights (c) Microsoft Corporation\\n//\\n\"},\n";
	print FOUT "{NOPARAM, L\"// ************************************************************************\\n\\n\"},\n";
	print FOUT "{NOPARAM, L\"#pragma classflags(\\\"forceupdate\\\")\\n\"},\n";
	print FOUT "{NOPARAM, L\"#pragma namespace (\\\"\\\\\\\\\\\\\\\\.\\\\\\\\Root\\\\\\\\WMI\\\")\\n\"},\n";
	print FOUT "{NOPARAM, L\"#pragma autorecover\\n\\n\"},\n";

	print FOUT "{INSTNAME, L\"[ Dynamic,\\n  Description(\\\"%s Trace\\\"),\\n\"},\n";
	print FOUT "{INSTGUID, L\"  Guid(\\\"%s\\\"),\\n\"},\n";
	print FOUT "{INSTNAME, L\"  DisplayName(\\\"%s Trace\\\")\\n\"},\n";
	print FOUT "{NOPARAM, L\"]\\n\"},\n";
	print FOUT "{INSTNAME, L\"class %sTrace:EventTrace\\n{\\n};\\n\\n\"},\n";

	for (my $i = $EventClassStart; $i <= $#rgsEventIDs; $i++)
	{
		if($rgsEventNames[$i] ne "")
		{
			print FOUT "{NOPARAM, L\"//----------------------------------------------------------------\\n\"},\n";
			print FOUT "{NOPARAM, L\"// ", $rgsEventRawNames[$i], " (", $i, ") Event Instance Schema\\n\"},\n";
			print FOUT "{NOPARAM, L\"//----------------------------------------------------------------\\n\"},\n";

			#generic class
			print FOUT "{NOPARAM, L\"[ Dynamic,\\n\"},\n";
			print FOUT "{NOPARAM, L\"  Description(\\\"", $rgsEventRawNames[$i], "\\\"),\\n\"},\n";
			printf FOUT "{NOPARAM, L\"  Guid(\\\"84e993e6-%04x-4d70-97ef-3824ed2c4e93\\\"),\\n\"},\n", $i;
			print FOUT "{NOPARAM, L\"  DisplayName(\\\"", $rgsEventRawNames[$i], "\\\")\\n\"},\n";
			print FOUT "{NOPARAM, L\"]\\n\"},\n";
			print FOUT "{INSTNAME, L\"class SQLTraceEvent", $i, " : %sTrace\\n{\\n};\\n\\n\"},\n";

			#type 0 class
			print FOUT "{NOPARAM, L\"[ Dynamic,\\n\"},\n";
			print FOUT "{NOPARAM, L\"  Description(\\\"", $rgsEventRawNames[$i], " Type0\\\"),\\n\"},\n";
			print FOUT "{NOPARAM, L\"  EventType(0),\\n\"},\n";
			print FOUT "{NOPARAM, L\"  EventTypeName(\\\"0\\\")\\n\"},\n";
			print FOUT "{NOPARAM, L\"]\\n\"},\n";
			print FOUT "{NOPARAM, L\"class SQLTraceEvent", $i, "_TYPE0 : SQLTraceEvent", $i, "\\n{\\n\"},\n";
			my $binding = $assEventColumnBindings{$rgsEventIDs[$i]};
			my @cols = split(/,/,$binding);
			my $idx = 0;
			for (my $col = 1; $col <= $#cols; $col++)
			{
				#print param defs for active cols except for ServerName
				if($col != 26 && $cols[$col] == 1)
				{
					$idx = $idx + 1;
					print FOUT "{NOPARAM, L\"", ETWParamDefEscaped($col, $idx), "\"},\n";
				}
			}

			print FOUT "{NOPARAM, L\"};\\n\"},\n";
		}
	}
	close(FOUT);
}

#
# Get param def from col info
sub ParamDef($$$)
{
	my ($col, $xmlTextData, $FullTypeInfo) = @_;
	
	my $tag = "TraceEventTag_" . $rgsColumnNames[$col];
	$tag =~ s/TextData/TextDataXml/ if( $xmlTextData == 1);

	if ($FullTypeInfo == 0)
	{
		return "\{$tag\}, //$rgsColumnNames[$col]\n";
	}

	my $def = "\{$tag,L\"" . $rgsColumnNames[$col] . "\",0,0,NULL,";

	if($rgsColumnTypes[$col] eq "TRACE_I4")
	{
		$def = $def . "XVT_I4, PrecDefault(XVT_I4), ScaleDefault(XVT_I4), 4, 0\},\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_I8")
	{
		$def = $def . "XVT_I8, PrecDefault(XVT_I8), ScaleDefault(XVT_I8), 8, 0\},\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_NTEXT")
	{
		$def = $def . "XVT_VARWSTR, PrecDefault(XVT_VARWSTR), ScaleDefault(XVT_VARWSTR), VARTYPE_UNLIMITED_LENGTH, ";
		if($xmlTextData == 1)
		{
			$def = $def . "EVENT_PARAM_XML_VALUE\},\n";
		}
		else
		{
			$def = $def . "0\},\n";
		}
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_WSTR")
	{
		$def = $def . "XVT_VARWSTR, PrecDefault(XVT_VARWSTR), ScaleDefault(XVT_VARWSTR), x_cbMAXSSWNAME*2, 0\},\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_DATETIME")
	{
		$def = $def . "XVT_SSDATE, PrecDefault(XVT_SSDATE), ScaleDefault(XVT_SSDATE), sizeof(SQLDATE), 0\},\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_BYTES")
	{
		$def = $def . "XVT_VARBYTES, PrecDefault(XVT_VARBYTES), ScaleDefault(XVT_VARBYTES), VARTYPE_UNLIMITED_LENGTH, 0\},\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_GUID")
	{
		$def = $def . "XVT_SSGUID, PrecDefault(XVT_SSGUID), ScaleDefault(XVT_SSGUID), sizeof(GUID), 0\},\n";
	}

	return $def;
}


#
# ETW paramdef
sub ETWParamDef($$)
{
	my ($col, $idx) = @_;
	my $def;
	if($rgsColumnTypes[$col] eq "TRACE_I4")
	{
		$def =  "\t[WmiDataId(" . $idx . "),\n\t Description(\"" . $rgsColumnNames[$col] . "\"),\n\t read\n\t]\n";
		$def = $def . "\tsint32\t" . $rgsColumnNames[$col] . ";\n\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_I8")
	{
		$def =  "\t[WmiDataId(" . $idx . "),\n\t Description(\"" . $rgsColumnNames[$col] . "\"),\n\t read\n\t]\n";
		$def = $def . "\tsint64\t" . $rgsColumnNames[$col] . ";\n\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_DATETIME")
	{
		$def =  "\t[WmiDataId(" . $idx . "),\n\t Description(\"" . $rgsColumnNames[$col] . "\"),\n\t read\n\t]\n";
		$def = $def . "\tuint64\t" . $rgsColumnNames[$col] . ";\n\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_NTEXT")
	{
		$def =  "\t[WmiDataId(" . $idx . "),\n\t Description(\"" . $rgsColumnNames[$col] . "\"),\n\t format(\"w\"),\n\t StringTermination(\"Counted\"),\n\t read\n\t]\n";
		$def = $def . "\tstring\t" . $rgsColumnNames[$col] . ";\n\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_WSTR")
	{
		$def =  "\t[WmiDataId(" . $idx . "),\n\t Description(\"" . $rgsColumnNames[$col] . "\"),\n\t format(\"w\"),\n\t StringTermination(\"Counted\"),\n\t read\n\t]\n";
		$def = $def . "\tstring\t" . $rgsColumnNames[$col] . ";\n\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_BYTES")
	{
		$def =  "\t[WmiDataId(" . $idx . "),\n\t Description(\"" . $rgsColumnNames[$col] . "\"),\n\t format(\"c\"),\n\t StringTermination(\"Counted\"),\n\t read\n\t]\n";
		$def = $def . "\tstring\t" . $rgsColumnNames[$col] . ";\n\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_GUID")
	{
		$def =  "\t[WmiDataId(" . $idx . "),\n\t Description(\"" . $rgsColumnNames[$col] . "\"),\n\t extension(\"Guid\"),\n\t read\n\t]\n";
		$def = $def . "\tobject\t" . $rgsColumnNames[$col] . ";\n\n";
	}

	return $def;
}

sub ETWParamDefEscaped($$)
{
	my ($col, $idx) = @_;
	my $def;
	if($rgsColumnTypes[$col] eq "TRACE_I4")
	{
		$def =  "\\t[WmiDataId(" . $idx . "),\\n\\t Description(\\\"" . $rgsColumnNames[$col] . "\\\"),\\n\\t read\\n\\t]\\n";
		$def = $def . "\\tsint32\\t" . $rgsColumnNames[$col] . ";\\n\\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_I8")
	{
		$def =  "\\t[WmiDataId(" . $idx . "),\\n\\t Description(\\\"" . $rgsColumnNames[$col] . "\\\"),\\n\\t read\\n\\t]\\n";
		$def = $def . "\\tsint64\\t" . $rgsColumnNames[$col] . ";\\n\\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_DATETIME")
	{
		$def =  "\\t[WmiDataId(" . $idx . "),\\n\\t Description(\\\"" . $rgsColumnNames[$col] . "\\\"),\\n\\t read\\n\\t]\\n";
		$def = $def . "\\tuint64\\t" . $rgsColumnNames[$col] . ";\\n\\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_NTEXT")
	{
		$def =  "\\t[WmiDataId(" . $idx . "),\\n\\t Description(\\\"" . $rgsColumnNames[$col] . "\\\"),\\n\\t format(\\\"w\\\"),\\n\\t StringTermination(\\\"Counted\\\"),\\n\\t read\\n\\t]\\n";
		$def = $def . "\\tstring\\t" . $rgsColumnNames[$col] . ";\\n\\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_WSTR")
	{
		$def =  "\\t[WmiDataId(" . $idx . "),\\n\\t Description(\\\"" . $rgsColumnNames[$col] . "\\\"),\\n\\t format(\\\"w\\\"),\\n\\t StringTermination(\\\"Counted\\\"),\\n\\t read\\n\\t]\\n";
		$def = $def . "\\tstring\\t" . $rgsColumnNames[$col] . ";\\n\\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_BYTES")
	{
		$def =  "\\t[WmiDataId(" . $idx . "),\\n\\t Description(\\\"" . $rgsColumnNames[$col] . "\\\"),\\n\\t format(\\\"c\\\"),\\n\\t StringTermination(\\\"Counted\\\"),\\n\\t read\\n\\t]\\n";
		$def = $def . "\\tstring\\t" . $rgsColumnNames[$col] . ";\\n\\n";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_GUID")
	{
		$def =  "\\t[WmiDataId(" . $idx . "),\\n\\t Description(\\\"" . $rgsColumnNames[$col] . "\\\"),\\n\\t extension(\\\"Guid\\\"),\\n\\t read\\n\\t]\\n";
		$def = $def . "\\tobject\\t" . $rgsColumnNames[$col] . ";\\n\\n";
	}

	return $def;
}

#
# Construct human-readable string from the event permission flags.
#
sub GetPermissionString($)
{
	my ($i) = @_;
	my $flag = "";
	
	if($rgwEvNotifFlags[$i] & 2)
	{
		$flag = "SECURITY";
	}
	
	if($rgwEvNotifFlags[$i] & 4)
	{
		$flag = $flag . " | "	 if ($flag ne "");
		$flag = $flag . "MANAGEMENT";
	}
	
	if($rgwEvNotifFlags[$i] & 8)
	{
		$flag = $flag . " | " if ($flag ne "");
		$flag = $flag . "USER";
	}

	return $flag;
}

#
# Get param def from col info
sub XMLParamDef($$)
{
	my ($col, $xmlTextData) = @_;
	
	my $tag = "TraceEventTag_" . $rgsColumnNames[$col];
	$tag =~ s/TextData/TextDataXml/ if( $xmlTextData == 1);

	my $def = "\t\t\t<xs:element name=\"$rgsColumnNames[$col]\" type=\"";

	if($rgsColumnTypes[$col] eq "TRACE_I4")
	{
		# Certain trace events send out nullable columns like DeprecationAnnoucement
		my @nullableCols = ('Offset','IsSystem','IntegerData2');
		my $column_name = $rgsColumnNames[$col];
		my @matches = grep(/$column_name/, @nullableCols);
		
		if( scalar(@matches) > 0 )
		{
			$def .= "emptiableInt" 
		}
		else
		{
			$def .= "xs:int";
		}
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_I8")
	{
		# Certain trace events send out nullable columns like DeprecationAnnoucement
		my @nullableCols = ('TransactionID');
		my $column_name = $rgsColumnNames[$col];
		my @matches = grep(/$column_name/, @nullableCols);
		
		if( scalar(@matches) > 0 )
		{
			$def .= "emptiableLong"
		}
		else
		{
			$def .= "xs:long";
		}
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_NTEXT")
	{
		if($xmlTextData == 1)
		{
			$def .= "xs:string";
		}
		else
		{
			$def .= "xs:anyType";
		}
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_WSTR")
	{
		$def .= "SqlTraceNameType";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_DATETIME")
	{
		$def .= "xs:string";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_BYTES")
	{
		$def .= "xs:base64Binary";
	}
	elsif($rgsColumnTypes[$col] eq "TRACE_GUID")
	{
		$def .= "uniqueidentifier";
	}
	$def .= "\"/>";

	return $def;
}
