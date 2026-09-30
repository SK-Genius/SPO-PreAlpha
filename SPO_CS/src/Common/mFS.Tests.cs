#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mTest.cs
#:ref mAssert.cs
#:ref mFS.cs
#:ref mMaybe.cs

public static class
mFS_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mFS),
		[
			mTest.Test("Files and URIs",
				aDebug => {
					var Relative = mFS.File("Signature Tests/Int #1.sig");
					var Absolute = mFS.CWD().GetFile(Relative.Path);
					var Uri = Absolute.Path.ToUri();
					mAssert.AreEquals(Uri, mFS.CWD()._Path.ToUri().TrimEnd('/') + "/Signature%20Tests/Int%20%231.sig");
					mAssert.AreEquals(mFS.Path(Uri).ToText(), Absolute.Path.ToText());
					mAssert.IsTrue(Relative.IsSameFile(Absolute));
					mAssert.IsTrue(Relative.IsSameFile(mFS.File(Uri)));
					mAssert.IsTrue(!Relative.IsSameFile(mFS.File("Signature Tests/Other.sig")));
					mAssert.IsTrue(mFS.File("case.sig").IsSameFile(mFS.File("CASE.SIG")) == System.OperatingSystem.IsWindows());
					mAssert.AreEquals(Relative.Extension, "sig");
					mAssert.AreEquals(Relative.NameWithoutExtension, "Int #1");
					mAssert.AreEquals(mFS.File("README").Extension, "");
					mAssert.IsTrue(mFS.File("Test.SPO").Folder._Path == mFS.cIdentPath);
					mAssert.IsTrue(Absolute.Folder.GetFile("nested/../Int #1.sig").IsSameFile(Absolute));
					mAssert.IsTrue((mFS.CWD() / "other").GetFile(Absolute.Path).IsSameFile(Absolute));
					mAssert.IsTrue(Absolute.Folder.GetFile(Uri).IsSameFile(Absolute));
				}
			),
			mTest.Test("Absolute roots",
				aDebug => {
					if (System.OperatingSystem.IsWindows()) {
						mAssert.AreEquals(mFS.Path("C:/").ToText(), "C:/");
						mAssert.AreEquals((mFS.Path("C:/") / "..").ToText(), "C:/");
						mAssert.AreEquals((mFS.Path("C:/base") / "D:/other/file.sig").ToText(), "D:/other/file.sig");
						mAssert.AreEquals(mFS.Path("file://server/share/Folder%20Name/Type.sig").ToText(), "//server/share/Folder Name/Type.sig");
						mAssert.AreEquals(mFS.Path("//server/share/Folder Name/Type.sig").ToUri(), "file://server/share/Folder%20Name/Type.sig");
					} else {
						mAssert.AreEquals(mFS.Path("/").ToText(), "/");
						mAssert.AreEquals((mFS.Path("/") / "..").ToText(), "/");
						mAssert.AreEquals((mFS.Path("/base") / "/other/file.sig").ToText(), "/other/file.sig");
						mAssert.AreEquals(mFS.Path("file:///Folder%20Name/Type.sig").ToText(), "/Folder Name/Type.sig");
					}
				}
			),
			Test(
				() => mFS.Path("bla"),
				"bla"
			),
			Test(
				() => mFS.Path(".."),
				".."
			),
			Test(
				() => mFS.Path("../.."),
				"../.."
			),
			Test(
				() => mFS.Path("../../../folder"),
				"../../../folder"
			),
			Test(
				() => mFS.Path("../..") / "..",
				"../../.."
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
