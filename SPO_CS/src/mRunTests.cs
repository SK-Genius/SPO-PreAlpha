// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mMaybe
// IMPORT Common/mStream
// IMPORT Common/mCommon.Tests
// IMPORT mSPO.Tests

var Tests = mTest.Tests(
	"All",
	[
		mCommon_Tests.Tests,
		mSPO_Tests.Tests,
	]
);

var Args = mStream.Stream(System.MemoryExtensions.AsSpan(args));

static void
PrintLn(
	tText aLine
) {
	System.Console.WriteLine(aLine.Replace("\t", "  "));
	System.Console.Out.Flush();
}

static void
PrintLnNoFormat(
	tText aLine
) {
	System.Console.WriteLine(
		System.Text.RegularExpressions.Regex.Replace(
			aLine.Replace("\t", "  "),
			"[\x1b]\\[\\d+m", ""
		)
	);
	System.Console.Out.Flush();
}

const tText cHelpCommand = "--help";
const tText cHelpCommandShort = "-h";

const tText cListCommand = "--list";
const tText cListCommandShort = "-l";

const tText cShowSkippedTestsCommand = "--showSkippedTests";
const tText cShowSkippedTestsCommandShort = "-s";

const tText cShowPassedGroupsCommand = "--showPassedGroups";
const tText cShowPassedGroupsCommandShort = "-g";

const tText cShowPassedTestsCommand = "--showPassedTests";
const tText cShowPassedTestsCommandShort = "-t";

const tText cMatchAllCommand = "--matchAll";
const tText cMatchAllCommandShort = "-M";

const tText cMatchAnyCommand = "--matchAny";
const tText cMatchAnyCommandShort = "-m";

const tText cOutputLevelCommand = "--outputLevel";
const tText cOutputLevelCommandShort = "-o";

const tText cTreeLevelCommand = "--treeLevel";
const tText cTreeLevelCommandShort = "-d";

const tText cStopOnFirstFail = "--stopOnFirstFail";
const tText cStopOnFirstFailShort = "-1";

const tText cPlainText = "--plainText";
const tText cPlainTextShort = "-p";

const tText cDebugger = "--debugger";
const tText cDebuggerShort = "-D";

const tText cDebugId = "--debugId";
const tText cDebugIdShort = "-i";

static mMaybe.tMaybe<tText>
GetArgParam(
	mStream.tStream<tText> aArgs,
	tText aShortArgName,
	tText aLongArgName
) => aArgs.SkipUntil(
	__ => __ == aLongArgName || __ == aShortArgName
).Skip(
	1
).TryFirst(
).ElseTry(
	() => aArgs.Where(
		__ => __.StartsWith(aShortArgName)
	).TryFirst(
	).Then(
		__ => __[aShortArgName.Length..]
	)
);

if (Args.Any(__ => __ is cHelpCommand or cHelpCommandShort)) {
	System.Console.WriteLine(
		$"""
			{cHelpCommandShort} {cHelpCommand} - show this help
			{cListCommandShort} {cListCommand} - list all tests without executing them
			{cShowPassedGroupsCommandShort} {cShowPassedGroupsCommand} - shows passed groups (works only in combination with {cShowPassedTestsCommand} for now)
			{cShowPassedTestsCommandShort} {cShowPassedTestsCommand} - shows also passed tests (but only if not all tests in the group passed)
			{cShowSkippedTestsCommandShort} {cShowSkippedTestsCommand} - shows skipped tests
			{cOutputLevelCommandShort} <level> {cOutputLevelCommand} <level> - 0=none, 1=errors, 2=summary, 3=details, 4=all (default: all)
			{cTreeLevelCommandShort} <level> {cTreeLevelCommand} <level> - 1=root (default: all)
			{cStopOnFirstFailShort} {cStopOnFirstFail} - stop on first failed test
			{cDebuggerShort} {cDebugger} - launch debugger
			{cPlainTextShort} {cPlainText} - do not use colors or other formatting in output
			{cDebugIdShort} <DebugId> {cDebugId} <DebugId> - breaks when DebugId is created (debugIds are shown in the traces)
			{cMatchAllCommandShort} <filter text> {cMatchAllCommand} <filter text> - must be the last argument
			{cMatchAnyCommandShort} <filter text> {cMatchAnyCommand} <filter text> - must be the last argument
		"""
	);
	return 0;
}

var MatchAll = Args.SkipUntil(
	__ => __ is cMatchAllCommand or cMatchAllCommandShort
).Skip(
	1
);

var MatchAny = Args.SkipUntil(
	__ => __ is cMatchAnyCommand or cMatchAnyCommandShort
).Skip(
	1
);

if (Args.Any(__ => __ is cListCommand or cListCommandShort)) {
	Tests.List(
		PrintLn,
		MatchAll.IsEmpty() ? MatchAny : MatchAll,
		!MatchAll.IsEmpty()
	);
	return 0;
}

mStd.gDebugId = GetArgParam(
	Args,
	cDebugIdShort,
	cDebugId
).Match(
	tNat64.Parse,
	() => 0u
);

return Tests.Run(
	Args.Any(__ => __ is cPlainText or cPlainTextShort) || System.Console.IsOutputRedirected ? PrintLnNoFormat : PrintLn,
	new mTest.tTestSettings {
		Filters = MatchAll.IsEmpty() ? MatchAny : MatchAll,
		HasToMatchAll = !MatchAll.IsEmpty(),
		HideSkippedTests = !Args.Any(__ => __ is cShowSkippedTestsCommand or cShowSkippedTestsCommandShort),
		HidePassedGroups = !Args.Any(__ => __ is cShowPassedGroupsCommand or cShowPassedGroupsCommandShort),
		HidePassedTests = !Args.Any(__ => __ is cShowPassedTestsCommand or cShowPassedTestsCommandShort),
		OutputLevel = GetArgParam(
			Args,
			cOutputLevelCommandShort,
			cOutputLevelCommand
		).Match(
			tInt32.Parse,
			() => tInt32.MaxValue
		),
		TreeLevel = GetArgParam(
			Args,
			cTreeLevelCommandShort,
			cTreeLevelCommand
		).Match(
			tInt32.Parse,
			() => tInt32.MaxValue
		),
		DebuggerBreak = Args.Any(__ => __ is cDebugger or cDebuggerShort),
		StopOnFirstFail = Args.Any(__ => __ is cStopOnFirstFail or cStopOnFirstFailShort) ? 1
		: Args.Any(__ => __ is "-1") ? 1
		: Args.Any(__ => __ is "-2") ? 2
		: Args.Any(__ => __ is "-3") ? 3
		: Args.Any(__ => __ is "-4") ? 4
		: Args.Any(__ => __ is "-5") ? 5
		: Args.Any(__ => __ is "-6") ? 6
		: Args.Any(__ => __ is "-7") ? 7
		: Args.Any(__ => __ is "-8") ? 7
		: Args.Any(__ => __ is "-9") ? 8
		: tInt32.MaxValue,
	}
).Result is mTest.tResult.Fail
? -1
: 0;
