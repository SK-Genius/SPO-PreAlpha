using System;
using System.Text.RegularExpressions;

var Tests = mTest.Tests(
	"All",
	[
		mAny_Tests.Tests,
		mMaybe_Tests.Tests,
		mResult_Tests.Tests,
		mStream_Tests.Tests,
		mMap_Tests.Tests,
		mTreeMap_Tests.Tests,
		//mMath_Test.Test,
		mArrayList_Tests.Tests,
		mParserGen_Tests.Tests,
		//mTextParser_Test.Test,
		//mVM_Data_Test.Test,
		//mIL_AST_Test.Test, 
		mTokenizer_Tests.Tests,
		mIL_Parser_Tests.Tests,
		mVM_Type_Tests.Tests,
		mVM_Tests.Tests,
		mIL_GenerateOpcodes_Tests.Tests, 
		//mSPO_AST_Test.Test,
		mSPO_AST_Types_Tests.Tests,
		mSPO_Parser_Tests.Tests,
		mSPO2IL_Tests.Tests,
		mSPO_Interpreter_Tests.Tests,
		mStdLib_Tests.Tests
	]
);

var Args = mStream.Stream(args.AsSpan());

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
	System.Console.WriteLine(Regex.Replace(aLine.Replace("\t", "  "), "[\x1b]\\[\\d+m", ""));
	System.Console.Out.Flush();
}

const tText cHelpCommand = "--help";
const tText cHelpCommandShort = "-h";

const tText cListCommand = "--list";
const tText cListCommandShort = "-l";

const tText cShowSkippedTestsCommand = "--showSkippedTests";
const tText cShowSkippedTestsCommandShort = "-s";

const tText cFilterCommand = "--filter";
const tText cFilterCommandShort = "-f";

const tText cOutputLevelCommand = "--outputLevel";
const tText cOutputLevelCommandShort = "-o";

const tText cStopOnFirstFail = "--stopOnFirstFail";
const tText cStopOnFirstFailShort = "-1";

const tText cPlainText = "--plainText";
const tText cPlainTextShort = "-p";

if (Args.Any(_ => _ is cHelpCommand or cHelpCommandShort)) {
	Console.WriteLine(
		$"""
		{cHelpCommandShort} {cHelpCommand}
		{cListCommandShort} {cListCommand}
		{cShowSkippedTestsCommandShort} {cShowSkippedTestsCommand}
		{cFilterCommandShort} <filter text> {cFilterCommand} <filter text>
		{cOutputLevelCommandShort} <level> {cOutputLevelCommand} <level>
		{cStopOnFirstFailShort} {cStopOnFirstFail}
		{cPlainTextShort} {cPlainText}
		"""
	);
	return 0;
}

var Filter = Args.SkipUntil(
	_ => _ is cFilterCommand or cFilterCommandShort
).Skip(
	1
);

if (Args.Any(_ => _ is cListCommand or cListCommandShort)) {
	Tests.List(
		PrintLn,
		Filter
	);
	return 0;
}

var HideSkippedTests = !Args.Any(_ => _ is cShowSkippedTestsCommand or cShowSkippedTestsCommandShort);

var OutputLevel = Args.SkipUntil(
	_ => _ is cOutputLevelCommand or cOutputLevelCommandShort
).SkipUntil(
	_ => _ is cOutputLevelCommand or cOutputLevelCommandShort
).Skip(
	1
).TryFirst(
).Match(
	tInt32.Parse,
	() => tInt32.MaxValue
);

var PlainText = Args.Any(_ => _ is cPlainText or cPlainTextShort);

var StopOnFirstFail = Args.Any(_ => _ is cStopOnFirstFail or cStopOnFirstFailShort);

return Tests.Run(
	PlainText ? PrintLnNoFormat : PrintLn,
	Filter,
	HideSkippedTests,
	OutputLevel,
	StopOnFirstFail
).Result == mTest.tResult.Fail ? -1 : 0;
