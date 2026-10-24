#!/usr/bin/perl
#****************************************************************************
#		Copyright (c) Microsoft Corporation.
#
# @File: smoevt.pl
# @Owner: macies
#
# Purpose:
#
#	This script is used for automatic generation of smoevt.cs
#	from eventsschema.cpp
#
#	Adapted from mofgen.pl by mwories
#
# History:
#
#	@Version: Yukon
#	00000 MJW  09/10/03 This file was created
#
# @EndHeader@
#****************************************************************************
use strict;
use warnings "all";

########################################################
#	EVENT SCHEMA VARIABLES
########################################################

# The result after the parsing will be kept in the following two scalars.
#
my $class_select_list = "";

# the events that are defined here are routed events
my @server_events;
my @database_events	= ("DROP_DATABASE");
my @table_events;
my @view_events;
my @function_events;
my @sp_events;
my @asm_events;
my @svcq_events		= ("ALTER_QUEUE", "DROP_QUEUE");
my @object_events	= ("ALTER", "DROP");
my @all_events;
my @trace_events;
my @server_level_triggers;
my @database_level_triggers;

########################################################
#	EVENT GROUPS' VARIABLES
########################################################

# The parse result for the groups will be kept in the following scalar
my $group_def = "";

# If 0, then we are outside of the group definition.
my $f_inside_group_def = 0;
my $f_inside_schema_def = 0;

# The following hash holds the group names
my %h_group_name;
$h_group_name{ "EGROUP_INVALID" } = "Envelope";
my %h_group_children;
my %h_event_parent;

# The following hash holds the group-parent relationship
my %h_group_parent;

# Prototypes
#
sub CamelCase($);
sub CreateEnum(@);
sub CreateStaticProps($@);
sub CreateProps($@);
sub CreateStringFromEvent($@);
sub CreateEventFromString($@);
sub GetEventsInGroupRecurse($);
sub GetGroupsInGroupRecurse($);
sub CreateEventsGroupProp($$);
sub CreateEventsGroupPropInit($$);
sub CreateElementCount(@);

########################################################
#
#	TEMPLATE GENERATION
#
#	The following section generates a template
#	file based on a class template file.
#	This is done as we do not want to copy/paste
#	identical class definitions into the overall
#	template file.
#
########################################################

my $gen_for_enum_or_main = $ARGV[0];
my $event_schema = $ARGV[1];
my $class_template = $ARGV[2];			# class template - class_template.cs
my $template = $ARGV[3];			# overall template - smoevt_template.cs
my $output_template=$ARGV[4] ? $ARGV[4] : "smoevt_gen_template.cs";
						# optional input:  generated template temp file

my @classnames;

if( $gen_for_enum_or_main eq "main" )
{
	@classnames = qw(ServerEvent ServerTraceEvent DatabaseEvent TableEvent ViewEvent ServiceQueueEvent ObjectEvent UserDefinedFunctionEvent StoredProcedureEvent SqlAssemblyEvent);
}
else
{
	@classnames = qw(ServerDdlTriggerEvent DatabaseDdlTriggerEvent);
}

my $template_code;

# Generate class definitions
open(CLASS_TEMPLATE_FILE, $class_template) || die "Cannot open class template: $class_template $!";

foreach my $classname (@classnames)
{
	while (<CLASS_TEMPLATE_FILE>)
	{
		s/CLASSNAME/$classname/g;
		$template_code .= $_;
	}
	seek CLASS_TEMPLATE_FILE, 0, 0;
}
close CLASS_TEMPLATE_FILE || die "Close failed.";

# Write template file
open(TEMPLATE, $template) || die "Cannot open template: $template $!";
open(OUTPUT_TEMPLATE, ">$output_template") || die "Cannot open $output_template: $!";

while (<TEMPLATE>)
{
	s/\r//;

	if (/^\@\@\@CLASS_TEMPLATE/)
	{
		print OUTPUT_TEMPLATE $template_code
	}
	else
	{
		print OUTPUT_TEMPLATE $_
	}
}

close TEMPLATE || die "Close failed.";
close OUTPUT_TEMPLATE || die "Close failed.";

########################################################
#	PARSING
########################################################

open(EVENT_SCHEMA_FILE, $event_schema) || die "Cannot open event_schema: $event_schema $!";

# Perform for every line on input file
#
while ( <EVENT_SCHEMA_FILE> )
{
	#################################################################################
	#
	#	EVENT GROUP PARSING
	#
	#################################################################################
	
	# The following pattern indicates the begining of the event group hierarchy.
	#
	if ( /EVENT_GROUP\s+g_rgEventGroup\s*\[\s*\]\s*=/ )
	{
		$f_inside_group_def = 1;
	}
	
	# The following pattern indicates a group definition.
	# If we are inside of a group definition block, then collect the group properties.
	#
	#---------------------------------------------------------------------------------------
	#                  $1           $2             $3
	#             {    GROUP_ID,    PARENT_ID,  L "GroupName" ,	 0 ,   0     }    ,
	#---------------------------------------------------------------------------------------
	elsif (	$f_inside_group_def && (/^\s*\{\s*EGROUP_.*/ || /^\s*\{\s*\(EEventGroup\)\s*EGROUP_.*/) )
	{
		# strip { },
		s/\s*\{\s*//;
		s/\s*\}\s*\,\s*//;
		s/\s*//g;
		s/,L/,/g;
		s/\"//g;
		s/EGROUP_//g;

		my ($l_group_id, $l_parent_id, $l_group_name) = split /\s*,\s*/;
		my ($camel_group) = CamelCase($l_group_name);

		$h_group_name{$l_group_id} = $l_group_name;
		$h_group_parent{$l_group_id} = $l_parent_id;
		push @{$h_group_children{$l_parent_id}}, $l_group_id if( !($l_parent_id =~ m/INVALID/));

		# AllEvents is base class
		if ($camel_group ne "AllEvents")
		{
			$group_def .= "\tclass $camel_group : "
				. CamelCase($h_group_name{$h_group_parent{$l_group_id}}) . "\n"
				. "\t{\n"
				. "\t}\n\n";
		}
	}

	# The following pattern indicates the end of the event group hierarchy.
	#
	elsif ( /^};\s*$/ && $f_inside_group_def )
	{
		$f_inside_group_def = 0;
	}
	
	#################################################################################
	#
	#	EVENT SCHEMA PARSING
	#
	#################################################################################
	
	# The following pattern indicates the begining of the event schema definitions.
	# We need to find the group to which each event type belogs.
	#
	elsif ( /EVENT_SCHEMA\s+s_rgEventSchema\s*\[\s*\]\s*=/ )
	{
		$f_inside_schema_def = 1;
	}
	
	# The following pattern indicates a group definition.
	# If we are inside of a group definition block, then collect the group properties.
	#
	#---------------------------------------------------------------------------------------
	#             {   ETYP_ID  ,
	#---------------------------------------------------------------------------------------
	elsif (( /^\s*\{\s*(ETYP_\w+)\s*\,\s*$/ ||
			/^\s*\{\s*\(EEventType\)\((ETYP_TRACE_\w+)/) && $f_inside_schema_def )
	{
		my $sync_only = 0;
		my $etyp_name = "";
		my $group_id = "";
		my @types = "";
		my $is_event_notif = 0;
		
		while ( <EVENT_SCHEMA_FILE> )
		{
			if ( /^\s*\}\s*\,?\s*$/ ) { last; };

			# Need to skip EFLAG_SYNC_ONLY event types.
			if ( /EFLAG_SYNC_ONLY/ )
			{	
				$sync_only = 1;
				next;
			}
				
			# Pattern for ETYP name
			#                        L " $1      " ,
			if ( !$etyp_name && /\s*L\"([^\"]*)\"\,\s*/ )
			{
				$etyp_name = $1;
				next;
			}
			
			# Pattern for group id
			#                       EGROUP_GROUP_ID  ,
			if ( !$group_id && (/\s*(EGROUP_[^\,\s]*)\,?\s*/ ||
					/^\s*\(EEventGroup\)\s*(EGROUP_TRCAT_\w+)/))
			{

				$group_id = $1;
				next;
			}

			if (/\s*EOBJTYP_.*/)
			{
				# strip spaces and comma
				s/[\s\,]*//g;
				# split types
				@types = split /\|/;
				next;
			}

			$is_event_notif = 1 if (/EFLAG_ASYNC_ONLY/);
		}
		
		if ( !$group_id || !$etyp_name || !@types )
		{
			print STDERR "smoevt.pl : error : Can't parse eventsschema.cpp.\n smoevt.pl needs to be updated.\n";
			die 4; 
		}

		# Build up the server & database trigger lists
		#
		if ( $is_event_notif == 0 )
		{
			foreach (@types)
			{
				if (/EOBJTYP_DATABASE/)
				{
					push @database_level_triggers, $etyp_name;
				}
			}

			foreach (@types)
			{
				if (/EOBJTYP_SERVER/)
				{
					# if just SERVER is specified, it is a server-level trigger
					push @server_level_triggers, $etyp_name;
				}
			}
		}
		
		# Need to skip EFLAG_SYNC_ONLY event types.
		if ( $sync_only )
		{
			next;
		}
		
		# Remove the prefix from the group_id
		$group_id =~ s/EGROUP_//;

		push @{$h_group_children{$group_id}}, $etyp_name;
	
		# This section creates mappings for object-level events
		#
		if ($group_id eq "DDL_TABLE" && $etyp_name ne "CREATE_TABLE")
		{
			@types = (@types, "EOBJTYP_TABLE");
		}
		elsif ($group_id eq "DDL_VIEW" && $etyp_name ne "CREATE_VIEW")
		{
			@types = (@types, "EOBJTYP_VIEW");
		}
		elsif ($group_id eq "DDL_INDEX")
		{
			@types = (@types, "EOBJTYP_TABLE", "EOBJTYP_VIEW");
		}
		elsif ($group_id eq "DDL_STATS")
		{
			@types = (@types, "EOBJTYP_TABLE", "EOBJTYP_VIEW");
		}
		elsif ($group_id eq "DDL_STOREDPROC" && $etyp_name ne "CREATE_PROCEDURE")
		{
			@types = (@types, "EOBJTYP_STOREDPROC");
		}
		elsif ($group_id eq "DDL_ASSEMBLY" && $etyp_name ne "CREATE_ASSEMBLY")
		{
			@types = (@types, "EOBJTYP_ASSEMBLY");
		}
		elsif ($group_id eq "DDL_FUNCTION" && $etyp_name ne "CREATE_FUNCTION")
		{
			@types = (@types, "EOBJTYP_FUNCTION");
		}
		

		if ($group_id =~ /^TRCAT.*/)
		{
			@trace_events = (@trace_events, $etyp_name);
		}
		else
		{
			@all_events = (@all_events, $etyp_name);

			foreach (@types)
			{
				if (/EOBJTYP_FUNCTION/)
				{
					@function_events = (@function_events, $etyp_name);
				}
				elsif (/EOBJTYP_SERVER/)
				{
					@server_events = (@server_events, $etyp_name);
				}
				elsif (/EOBJTYP_DATABASE/)
				{
					@database_events = (@database_events, $etyp_name);
				}
				elsif (/EOBJTYP_TABLE/)
				{
					@table_events = (@table_events, $etyp_name);
				}
				elsif (/EOBJTYP_VIEW/)
				{
					@view_events = (@view_events, $etyp_name);
				}
				elsif (/EOBJTYP_STOREDPROC/)
				{
					@sp_events = (@sp_events, $etyp_name);
				}
				elsif (/EOBJTYP_ASSEMBLY/)
				{
					@asm_events = (@asm_events, $etyp_name);
				}
				elsif (/EOBJTYP_SVCQ/)
				{
					@svcq_events = (@svcq_events, $etyp_name);
				}
				else
				{
					die "Unknown type: $_. You need to add this type.";
				}
			}
		}
	}
	
	# The following pattern indicates the end of the event group hierarchy.
	#
	elsif ( /^};\s*$/ && $f_inside_group_def )
	{
		$f_inside_schema_def = 0;
	}


}

close (EVENT_SCHEMA_FILE);

##################################################
#
#	This adds event groups to the select list
#	and additionally prepares the @groups
#	list that will be used to construct
#	the server event enumeration.
#
##################################################
my @groups;
my @trace_groups;
my $key;

foreach $key ( sort (keys %h_group_name) )
{
  
   if ($h_group_name{$key} =~ /Envelope/) { next };

   if ($h_group_name{$key} =~ m/^TRC/)
   {
            my $group_id = $key;
            $group_id =~ s/\(EEventGroup\)//;
            
            # trace groups ( skip empty trace groups - they could not be raised anyway )
            @trace_groups = (@trace_groups, $h_group_name{$key}) if( defined($h_group_children{ $group_id }));
   }
   else
   {
           # regular event groups
           @groups = (@groups, $h_group_children{$key});
   }

}

##################################################
#
#           Generates group -> parent mapping array
#
##################################################
my $group_mapping_table;
$group_mapping_table = "";
foreach ( keys %h_group_name )
{
	if ($_ ne "Envelope" && $_ ne "ALL" && $_ ne "EGROUP_INVALID")
	{
		# Add the current group in the class select list.
		#
		$group_mapping_table .= "\t\t\tmapping.Add(\"";
		$group_mapping_table .= $h_group_name{$_};
		$group_mapping_table .= "\", \"";
		$group_mapping_table .= $h_group_name{$h_group_parent{$_}};
		$group_mapping_table .= "\");\n";
	}
}
$group_mapping_table .= "\n";

##################################################
#
#	OUTPUT THE RESULT
#
##################################################

# Open the template
#
if ( !open( SMOEVT_TEMPLATE, $output_template ) )
{
	print STDERR "smoevt.pl : error : Can't open $output_template.\n";
	die 1;
}

# Replace the class definition mark and the class select list mark with
# the real stuff and print out the result.
#
while ( <SMOEVT_TEMPLATE> )
{
	# remove sparse \r
	s/\r*//g;

	# AUTOGENERATED comment
	#
	if ( /^\/\/\s*\@File:\s*smoevt_template.cs\s*$/ )
	{
		print "// \@File:\tsmoevt.cs\n";
	}
	# AUTOGENERATED comment
	#
	elsif ( /^\@\@\@\s*autogenerated$/ )
	{
		print	"//\t\tThis file is AUTOGENERATED. Please, don't do any manual changes here.\n".
				"//\t\tIf you need to do some changes, change one of the following files:\n".
				"//\t\tsmoevt_template.cs, class_template.cs, smoevt.pl\n";
	}

	# Group mapping table (maps group name -> parent)
	elsif ( /\@\@\@group_mapping$/ )
	{
		print $group_mapping_table
	}

	# Server Events
	elsif ( /\@\@\@server_events$/ )
	{
		print CreateEnum(@server_events);
	}
	elsif ( /\@\@\@ServerEvent_static_props$/ )
	{
		print CreateStaticProps("ServerEvent", @server_events);
	}
	elsif ( /\@\@\@ServerEvent_props$/ )
	{
		print CreateProps("ServerEvent", @server_events);
	}
	elsif ( /\@\@\@ServerEvent_count$/ )
	{
		print "\t\t\tget { return " . ($#server_events + 1) . "; }\n";
	}

	# Server Triggers
	elsif ( /\@\@\@ServerDdlTriggerEvent_enum$/ )
	{
		print CreateEnum(@server_level_triggers);
	}
	elsif ( /\@\@\@ServerDdlTriggerEvent_string_mapping$/ )
	{
		print CreateStringFromEvent("ServerDdlTriggerEventValues" , @server_level_triggers);
	}

	# Database Triggers
	elsif ( /\@\@\@DatabaseDdlTriggerEvent_enum$/ )
	{
		print CreateEnum(@database_level_triggers);
	}
	elsif ( /\@\@\@DatabaseDdlTriggerEvent_string_mapping$/ )
	{
		print CreateStringFromEvent("DatabaseDdlTriggerEventValues" , @database_level_triggers);
	}

	# Enumerator Generator
	#
	elsif ( /\@\@\@ServerDdlTriggerEvent_static_props$/ )
	{
		print CreateStaticProps("ServerDdlTriggerEvent", @server_level_triggers);
	}
	elsif ( /\@\@\@ServerDdlTriggerEvent_props$/ )
	{
		print CreateProps("ServerDdlTriggerEvent", @server_level_triggers);
	}
	elsif ( /\@\@\@ServerDdlTriggerEvent_group_static_props$/ )
	{
		print CreateEventsGroupProp("ServerDdlTriggerEventSet", "DDL_SERVER_LEVEL");
	}
	elsif ( /\@\@\@ServerDdlTriggerEvent_group_static_props_init$/ )
	{
		print CreateEventsGroupPropInit("ServerDdlTriggerEvent", "DDL_SERVER_LEVEL");
	}
	elsif ( /\@\@\@ServerDdlTriggerEvent_string_offset_mapping$/ )
	{
		print CreateEventFromString("ServerDdlTriggerEvent", @server_level_triggers);
	}
	elsif ( /\@\@\@ServerDdlTriggerEvent_elements_count$/ )
	{
		print CreateElementCount( @server_level_triggers);
	}
	elsif ( /\@\@\@DatabaseDdlTriggerEvent_static_props$/ )
	{
		print CreateStaticProps("DatabaseDdlTriggerEvent", @database_level_triggers);
	}
	elsif ( /\@\@\@DatabaseDdlTriggerEvent_props$/ )
	{
		print CreateProps("DatabaseDdlTriggerEvent", @database_level_triggers);
	}
	elsif ( /\@\@\@DatabaseDdlTriggerEvent_group_static_props$/ )
	{
		print CreateEventsGroupProp("DatabaseDdlTriggerEventSet", "DDL_DATABASE_LEVEL");
	}
	elsif ( /\@\@\@DatabaseDdlTriggerEvent_group_static_props_init$/ )
	{
		print CreateEventsGroupPropInit("DatabaseDdlTriggerEvent", "DDL_DATABASE_LEVEL");
	}
	elsif ( /\@\@\@DatabaseDdlTriggerEvent_string_offset_mapping$/ )
	{
		print CreateEventFromString("DatabaseDdlTriggerEvent", @database_level_triggers);
	}
	elsif ( /\@\@\@DatabaseDdlTriggerEvent_elements_count$/ )
	{
		print CreateElementCount( @database_level_triggers);
	}

	# Event groups - commented out for now MKS 3/4/2004
#	elsif ( /\@\@\@event_groups/ )
#	{
#		print CreateEnum(@groups);
#	}
#	elsif ( /\@\@\@ServerEventGroup_static_props$/ )
#	{
#		print CreateStaticProps(("ServerEventGroup", @groups));
#	}
#	elsif ( /\@\@\@ServerEventGroup_props$/ )
#	{
#		print CreateProps(("ServerEventGroup", @groups));
#	}
#	elsif ( /\@\@\@ServerEventGroup_count$/ )
#	{
#		print "\t\t\tget { return " . ($#groups + 1) . "; }\n";
#	}

	# Database events
	elsif ( /\@\@\@database_events$/ )
	{
		print CreateEnum(@database_events);
	}
	elsif ( /\@\@\@DatabaseEvent_static_props$/ )
	{
		print CreateStaticProps("DatabaseEvent", @database_events);
	}
	elsif ( /\@\@\@DatabaseEvent_props$/ )
	{
		print CreateProps("DatabaseEvent", @database_events);
	}
	elsif ( /\@\@\@DatabaseEvent_count$/ )
	{
		print "\t\t\tget { return " . ($#database_events + 1) . "; }\n";
	}

	# Table events
	elsif ( /\@\@\@table_events$/ )
	{
		print CreateEnum(@table_events);
	}
	elsif ( /\@\@\@TableEvent_static_props$/ )
	{
		print CreateStaticProps("TableEvent", @table_events);
	}
	elsif ( /\@\@\@TableEvent_props$/ )
	{
		print CreateProps("TableEvent", @table_events);
	}
	elsif ( /\@\@\@TableEvent_count$/ )
	{
		print "\t\t\tget { return " . ($#table_events + 1) . "; }\n";
	}

	# View events
	elsif ( /\@\@\@view_events/ )
	{
		print CreateEnum(@view_events);
	}
	elsif ( /\@\@\@ViewEvent_static_props$/ )
	{
		print CreateStaticProps("ViewEvent", @view_events);
	}
	elsif ( /\@\@\@ViewEvent_props$/ )
	{
		print CreateProps("ViewEvent", @view_events);
	}
	elsif ( /\@\@\@ViewEvent_count$/ )
	{
		print "\t\t\tget { return " . ($#view_events + 1) . "; }\n";
	}

	# Function events
	elsif ( /\@\@\@function_events/ )
	{
		print CreateEnum(@function_events);
	}
	elsif ( /\@\@\@UserDefinedFunctionEvent_static_props$/ )
	{
		print CreateStaticProps("UserDefinedFunctionEvent", @function_events);
	}
	elsif ( /\@\@\@UserDefinedFunctionEvent_props$/ )
	{
		print CreateProps("UserDefinedFunctionEvent", @function_events);
	}
	elsif ( /\@\@\@UserDefinedFunctionEvent_count$/ )
	{
		print "\t\t\tget { return " . ($#function_events + 1) . "; }\n";
	}

	# SP events
	elsif ( /\@\@\@sp_events/ )
	{
		print CreateEnum(@sp_events);
	}
	elsif ( /\@\@\@StoredProcedureEvent_static_props$/ )
	{
		print CreateStaticProps("StoredProcedureEvent", @sp_events);
	}
	elsif ( /\@\@\@StoredProcedureEvent_props$/ )
	{
		print CreateProps("StoredProcedureEvent", @sp_events);
	}
	elsif ( /\@\@\@StoredProcedureEvent_count$/ )
	{
		print "\t\t\tget { return " . ($#sp_events + 1) . "; }\n";
	}

	# Assembly events
	elsif ( /\@\@\@asm_events/ )
	{
		print CreateEnum(@asm_events);
	}
	elsif ( /\@\@\@SqlAssemblyEvent_static_props$/ )
	{
		print CreateStaticProps("SqlAssemblyEvent", @asm_events);
	}
	elsif ( /\@\@\@SqlAssemblyEvent_props$/ )
	{
		print CreateProps("SqlAssemblyEvent", @asm_events);
	}
	elsif ( /\@\@\@SqlAssemblyEvent_count$/ )
	{
		print "\t\t\tget { return " . ($#asm_events + 1) . "; }\n";
	}

	# Queue events
	elsif ( /\@\@\@svcq_events/ )
	{
		print CreateEnum(@svcq_events);
	}
	elsif ( /\@\@\@ServiceQueueEvent_static_props$/ )
	{
		print CreateStaticProps("ServiceQueueEvent", @svcq_events);
	}
	elsif ( /\@\@\@ServiceQueueEvent_props$/ )
	{
		print CreateProps("ServiceQueueEvent", @svcq_events);
	}
	elsif ( /\@\@\@ServiceQueueEvent_count$/ )
	{
		print "\t\t\tget { return " . ($#svcq_events + 1) . "; }\n";
	}

	# Object events
	elsif ( /\@\@\@object_events/ )
	{
		print CreateEnum(@object_events);
	}
	elsif ( /\@\@\@ObjectEvent_static_props$/ )
	{
		print CreateStaticProps("ObjectEvent", @object_events);
	}
	elsif ( /\@\@\@ObjectEvent_props$/ )
	{
		print CreateProps("ObjectEvent", @object_events);
	}
	elsif ( /\@\@\@ObjectEvent_count$/ )
	{
		print "\t\t\tget { return " . ($#object_events + 1) . "; }\n";
	}

	# All events (used by event args)
	elsif ( /\@\@\@all_events/ )
	{
		print CreateEnum(sort (@all_events, @trace_events));
	}

	# Trace events
	elsif ( /\@\@\@trace_events/ )
	{
		print CreateEnum(sort (@trace_events, @trace_groups));
	}
	elsif ( /\@\@\@ServerTraceEvent_static_props$/ )
	{
		print CreateStaticProps("ServerTraceEvent", sort (@trace_events, @trace_groups));
	}
	elsif ( /\@\@\@ServerTraceEvent_props$/ )
	{
		print CreateProps("ServerTraceEvent", sort (@trace_events, @trace_groups));
	}
	elsif ( /\@\@\@ServerTraceEvent_count$/ )
	{
		my @list = (@trace_events, @trace_groups);
		print "\t\t\tget { return " . ($#list + 1) . "; }\n";
	}

	# Where to place the class select list.
	#
	elsif ( /\@\@\@class_select_list/ )
	{
		print $class_select_list;
	}
	# Fix the file header.
	#
	elsif ( /\/\/ File\:  smoevt\.txt/ )
	{
		print "// File:  smoevt.cs\n";
	}
	elsif ( /\/\/    This is a template file used by smoevt\.pl/ )
	{
	}
	# No replacement. Print out the current line from the template.
	#
	else
	{
		print $_;
	}
}

#########################################################################
# sub CamelCase($val)
#
# RETURNS
#	c# style formatted identifier
#########################################################################
sub CamelCase($)
{
	if (!$_[0])
	{
		return undef;
	}
	if ($_[1])
	{
		# only camelcase strings that contain "_"
		return $_[0] if ($_[0] == /_/);
	}


	my (@chars) = split //, $_[0];
	my ($camel_group) = "";
	my ($first) = 1;
	foreach (@chars)
	{
		#print STDERR "Processing $_\n";
		if ($first)
		{
			$first = 0;
		}
		else
		{
			if (/\_/)
			{
				$first = 1;
				next
			}
			tr/A-Z/a-z/
		}
		$camel_group .= $_;
	}

	# do some cleaning of names
	$camel_group =~ s/^Trc/Trace/; # TraceGroup
	$camel_group =~ s/Db/DB/; # Db should be spelled DB per FxCop
	$camel_group =~ s/DBcc/Dbcc/; # Fix the Dbcc string altered with previous line
	
	return $camel_group;
}

#########################################################################
# sub CreateEnum(@list)
#
# RETURNS
#	values formatted to be stuffed in enum template
#########################################################################
sub CreateEnum(@)
{
	my $count = 0;
	my @list = @_;
	my $output = "";
	foreach (sort @list)
	{
		$count++;

		if ($count > 1)
		{
			$output .= ",\n";
		}
		$output .= "\t\t" . CamelCase($_);
	}
	$output .= "\n";
	return $output;
}

#########################################################################
# sub CreateStringFromEvent($prefix, @list)
#
# RETURNS
#	values formatted to be stuffed in string from event function
#########################################################################
sub CreateStringFromEvent($@)
{
	my ($name, @list) = @_;
	my $output = "";
	foreach (sort @list)
	{
		$output .= "\t\t\t\tcase (int)$name." . CamelCase($_) . " : return \"$_\";\n";
	}
	$output .= "\n";
	return $output;
}

#########################################################################
# sub CreateEventFromString($prefix, @list)
#
# RETURNS
#	values formatted to be stuffed in event from string function
#########################################################################
sub CreateEventFromString($@)
{
	my ($name, @list) = @_;
	my $output = "";
	foreach (sort @list)
	{
		$output .= "\t\t\t\tcase \"$_\" : return $name." . CamelCase($_) . ";\n";
	}
	return $output;
}

#########################################################################
# sub CreateStaticProps(@list)
#
# RETURNS
#	values formatted to be stuffed in custom bitset class
#########################################################################
sub CreateStaticProps($@)
{
	my ($class_name, @list) = @_;
	my $count = 0;
	my $output = "";
	foreach (sort @list)
	{
		my $value = CamelCase($_);
		$count++;
		$output .=	"\t\t///<summary>define static property for $value</summary>\n";
		$output .=	"\t\tpublic static $class_name $value\n"
			.	"\t\t{\n"
			.	"\t\t\tget { return new $class_name(${class_name}Values.$value); }\n"
			.	"\t\t}\n\n";
	}
	return $output;
}

#########################################################################
# sub CreateProps(@list)
#
# RETURNS
#	values formatted to be stuffed in custom bitset class
#########################################################################
sub CreateProps($@)
{
	my ($class_name, @list) = @_;
	my $count = 0;
	my $output = "";
	foreach (sort @list)
	{
		my $value = CamelCase($_);
		$count++;
		$output .=	"\t\t///<summary>define property for $value</summary>\n";
		$output .=	"\t\tpublic bool $value\n"
			.	"\t\t{\n"
			.	"\t\t\tget { return this.Storage[(int) " . $class_name . "Values.$value]; }\n"
			.	"\t\t\tset { this.Storage[(int) $class_name" . "Values.$value] = value; }\n"
			.	"\t\t}\n\n";
	}
	return $output;
}


#########################################################################
# sub CreateEventsGroupProp($class_name $group_id)
#
# RETURNS
#	values formatted to be stuffed in custom bitset class
#########################################################################
sub CreateEventsGroupProp($$)
{

	my ($class_name, $group_id) = @_;

	my @list = GetGroupsInGroupRecurse($group_id);
	my $output = "";

	foreach my $group_name (sort @list)
	{
		my $name = CamelCase($h_group_name{$group_name}) . "Events";
		my $lcc = lc($name);

$output .= << "EOF"
		///<summary>define event set for name</summary>
		private static $class_name $lcc;

		///<summary>get/set all events for $lcc</summary>
		public bool $name
		{
			get{return FitsMask($class_name.$lcc);}
			set{SetValue($class_name.$lcc, value); dirty=true; }
		}
EOF
	}
	return $output;

}

#########################################################################
# sub CreateEventsGroupPropInit($class_name $group_id)
#
# RETURNS
#	values formatted to be stuffed in custom bitset class
#########################################################################
sub CreateEventsGroupPropInit($$)
{

	my ($class_name, $group_id) = @_;

	my @list = GetGroupsInGroupRecurse($group_id);
	my $output = "";

	foreach my $group_name (sort @list)
	{
		my $name = CamelCase($h_group_name{$group_name}) . "Events";
		my $lcc = lc($name);

		$output .= "\t\t$lcc = new $class_name" . "Set(";

		my @child_events = GetEventsInGroupRecurse($group_name);
		for (my $i = 0; $i < (scalar(@child_events) - 1); $i++ )
		{
			$name = CamelCase($child_events[$i]);
			$output .= "\n\t\t\t$class_name.$name,"
		}
		$name = CamelCase($child_events[scalar(@child_events) - 1]);
		$output .= "\n\t\t\t$class_name.$name);\n"

	}
	return $output;

}

#########################################################################
# sub GetEventsInGroupRecurse($group_name)
#
# RETURNS
#	list of leaf nodes in tree
#########################################################################
sub GetEventsInGroupRecurse($)
{
	my ($group_id) = @_;
	if( defined($h_group_children{$group_id}))
	{
		my @ret;
		foreach my $g (@{$h_group_children{$group_id}})
		{
			push @ret, GetEventsInGroupRecurse($g);
		}
		return @ret;
	}
	else
	{
		return $group_id;
	}
}

#########################################################################
# sub GetGroupsInGroupRecurse($group_name)
#
# RETURNS
#	list of interior nodes in tree
#########################################################################
sub GetGroupsInGroupRecurse($)
{
	my ($group_id) = @_;
	my @ret;

	push @ret, $group_id;

	foreach my $g (@{$h_group_children{$group_id}})
	{
		if( defined($h_group_children{$g}))
		{
			push @ret, GetGroupsInGroupRecurse($g);
		}
	}

	return @ret;
}

#########################################################################
# sub CreateElementCount(@list)
#
# RETURNS
#	generate getter with element count
#########################################################################
sub CreateElementCount($@)
{
	my (@list) = @_;
	my $count = scalar @list;
	return "\t\t\t\tget { return $count; }\n";
}

