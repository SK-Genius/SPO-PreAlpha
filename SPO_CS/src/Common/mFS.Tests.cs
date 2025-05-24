// IMPORT mStd
// IMPORT mTest
// IMPORT mAssert
// IMPORT mFS
// IMPORT mMaybe

public static class
mFS_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mFS),
		[
			Test(
				() => mFS.Path("bla"),
				"bla"
			),
			Test(
				() => mFS.Path(".."),
				".."
			),
			Test(
				() => mFS.Path("."),
				"."
			),
			Test(
				() => mFS.Path("bla/blub"),
				"bla/blub"
			),
			Test(
				() => mFS.Path(@"bla\blub"),
				"bla/blub"
			),
			Test(
				() => mFS.Path("bla/blub/.."),
				"bla"
			),
			Test(
				() => mFS.Path("bla") / "blub",
				"bla/blub"
			),
			Test(
				() => mFS.Path("bla/blub") / "..",
				"bla"
			),
			Test(
				() => mFS.Path("bla") / ".." / ".." / "blub",
				"../blub"
			),
			Test(
				() => mFS.Path("bla") / "../.." / "blub",
				"../blub"
			),
			Test(
				() => mFS.Path("..") / "blub",
				"../blub"
			),
			Test(
				() => mFS.Path("..") / "../blub",
				"../../blub"
			),
			Test(
				() => mFS.Path("bla/./blub"),
				"bla/blub"
			),
			Test(
				() => mFS.Path("bla/blub") / mFS.Path("../foo"),
				"bla/foo"
			),
			Test(
				() => mFS.Path("bla/blub/foo")[..],
				"bla/blub/foo"
			),
			Test(
				() => mFS.Path("bla/blub/foo")[0..],
				"bla/blub/foo"
			),
			Test(
				() => mFS.Path("bla/blub/foo")[1..],
				"blub/foo"
			),
			Test(
				() => mFS.Path("bla/blub/foo")[..^1],
				"bla/blub"
			),
			Test(
				() => mFS.Path("bla/blub/foo")[1..^1],
				"blub"
			),
			Test(
				() => mFS.Path("bla/blub/foo")[^2..^1],
				"blub"
			),
			Test(
				() => mFS.Path("bla") >> mFS.Path("bla/blub"),
				"blub"
			),
			Test(
				() => mFS.Path("bla/blub") >> mFS.Path("bla"),
				".."
			),
			Test(
				() => mFS.Path("bla/blub") >> mFS.Path("bla/foo"),
				"../foo"
			),
			Test(
				() => mFS.Path(".proj") >> mFS.Path("src/mIL_AST.cs"),
				"../src/mIL_AST.cs"
			),
			Test(
				() => mFS.Path(".proj/Common") >> mFS.Path("src/Common/mTextParser.cs"),
				"../../src/Common/mTextParser.cs"
			),
		]
	);
	
	private static mTest.tTest
	Test(
		mStd.tFunc<mFS.tPath> aPath,
		tText aText,
		[CallerArgumentExpression(nameof(aPath))]tText aExptession = "",
		[CallerFilePath]tText aFilePath = "",
		[CallerLineNumber]tInt32 aLine = 0
	) => mTest.Test(
		$"{aExptession} => {aText}",
		aStreamOut => { mAssert.AreEquals(aPath().ToText(), aText); },
		aFilePath,
		aLine
	);
	
	private static mTest.tTest
	Test(
		mStd.tFunc<mMaybe.tMaybe<mFS.tPath>> aPath,
		tText aText,
		[CallerArgumentExpression(nameof(aPath))]tText aExptession = "",
		[CallerFilePath]tText aFilePath = "",
		[CallerLineNumber]tInt32 aLine = 0
	) => mTest.Test(
		$"{aExptession} => {aText}",
		aStreamOut => {
			mAssert.IsTrue(aPath().IsSome(out var Path));
			mAssert.AreEquals(Path.ToText(), aText);
		},
		aFilePath,
		aLine
	);
}
