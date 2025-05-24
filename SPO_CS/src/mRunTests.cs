// IMPORT Common/mStd
// IMPORT Common/mTest
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

const tText cMatchAllCommand = "--matchAll";
const tText cMatchAllCommandShort = "-&";

const tText cMatchAnyCommand = "--matchOne";
const tText cMatchAnyCommandShort = "-|";

const tText cOutputLevelCommand = "--outputLevel";
const tText cOutputLevelCommandShort = "-o";

const tText cTreeLevelCommand = "--treeLevel";
const tText cTreeLevelCommandShort = "-t";

const tText cStopOnFirstFail = "--stopOnFirstFail";
const tText cStopOnFirstFailShort = "-1";

const tText cPlainText = "--plainText";
const tText cPlainTextShort = "-p";

const tText cDebugger = "--debugger";
const tText cDebuggerShort = "-d";

static mMaybe.tMaybe<tText>
GetArgParam(
	mStream.tStream<tText> aArgs,
	tText aShortArgName,
	tText aLongArgNAme
) => aArgs.SkipUntil(
	_ => _ == aLongArgNAme || _ == aShortArgName
).Skip(
	1
).TryFirst(
).ElseTry(
	() => aArgs.Where(
		_ => _.StartsWith(aShortArgName)
	).TryFirst(
	).ThenDo(
		_ => _[aShortArgName.Length..]
	)
);

if (Args.Any(_ => _ is cHelpCommand or cHelpCommandShort)) {
	System.Console.WriteLine(
		$"""
		{cHelpCommandShort} {cHelpCommand}
		{cListCommandShort} {cListCommand}
		{cShowSkippedTestsCommandShort} {cShowSkippedTestsCommand}
		{cOutputLevelCommandShort} <level> {cOutputLevelCommand} <level>
		{cTreeLevelCommandShort} <level> {cTreeLevelCommand} <level>
		{cStopOnFirstFailShort} {cStopOnFirstFail}
		{cDebuggerShort} {cDebugger}
		{cPlainTextShort} {cPlainText}
		{cMatchAllCommandShort} <filter text> {cMatchAllCommand} <filter text>
		{cMatchAnyCommandShort} <filter text> {cMatchAnyCommand} <filter text>
		"""
	);
	return 0;
}

var MatchAll = Args.SkipUntil(
	_ => _ is cMatchAllCommand or cMatchAllCommandShort
).Skip(
	1
);

var MatchAny = Args.SkipUntil(
	_ => _ is cMatchAnyCommand or cMatchAnyCommandShort
).Skip(
	1
);

if (Args.Any(_ => _ is cListCommand or cListCommandShort)) {
	Tests.List(
		PrintLn,
		MatchAll.IsEmpty() ? MatchAny : MatchAll,
		!MatchAll.IsEmpty()
	);
	return 0;
}

var OutputLevel = GetArgParam(
	Args,
	cOutputLevelCommandShort,
	cOutputLevelCommand
).Match(
	tInt32.Parse,
	() => tInt32.MaxValue
);

var TreeLevel = GetArgParam(
	Args,
	cTreeLevelCommandShort,
	cTreeLevelCommand
).Match(
	tInt32.Parse,
	() => tInt32.MaxValue
);

var HideSkippedTests = !Args.Any(_ => _ is cShowSkippedTestsCommand or cShowSkippedTestsCommandShort);

var PlainText = Args.Any(_ => _ is cPlainText or cPlainTextShort);

var DebuggerBreak = Args.Any(_ => _ is cDebugger or cDebuggerShort);

var StopOnFirstFail = Args.Any(_ => _ is cStopOnFirstFail or cStopOnFirstFailShort);

return Tests.Run(
	PlainText ? PrintLnNoFormat : PrintLn,
	MatchAll.IsEmpty() ? MatchAny : MatchAll,
	!MatchAll.IsEmpty(),
	HideSkippedTests,
	OutputLevel,
	TreeLevel,
	DebuggerBreak,
	StopOnFirstFail
).Result == mTest.tResult.Fail ? -1 : 0;
