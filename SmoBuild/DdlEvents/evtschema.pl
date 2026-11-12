#****************************************************************************************
# Copyright (c) Microsoft Corporation.
# @File: evtschema.pl
# @Owner: jayc
#
# Purpose:
#	This script generates the temporary eventsschema.tmp file from eventsschema.cpp and other SQLTrace
#	generated files.  This new file is used by mofgen.pl.
#
# Usage:
#	perl evtschema.pl eventschema.cpp $(O)
# @EndHeader@
#****************************************************************************************

# Get ful path to eventsschema.cpp
$eventsschema_cpp = $ARGV[0];

# Get output directory
$OutputDir = $ARGV[1];

# Eventsschema.cpp
open(EVTSCHEMACPP, $eventsschema_cpp) || die "Cannot open $eventsschema_cpp\n";

# New destination file
open(OUTF, ">" . $OutputDir . "\\eventsschema.tmp") || die "Cannot write to eventsschema.tmp\n";

# read and copy
while(<EVTSCHEMACPP>)
{
	# search for pattern like #include "abc.inc"
	if(/^\s*#include\s+\"(\w+\.inc)\"\s*$/)
	{
		print OUTF "// " , $1,  "\n";
		&IncludeFile($1);
	}
	else
	{
		print OUTF $_;
	}
}

close(EVTSCHEMACPP);
close(OUTF);


sub IncludeFile
{
	local($IncName) = @_;
	open(INCFILE, $OutputDir . "\\" . $IncName) || die "Cannot open " . $IncName . "\n";
	while(<INCFILE>)
	{
		print OUTF $_;
	}

	print OUTF "\n";
	close(INCFILE);
}
