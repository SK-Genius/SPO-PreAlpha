#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = Exe
#:property PublishAot = false
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../src/Program.cs
#:ref ../../SPO_CS/src/Common/mAssert.cs
#:ref ../../SPO_CS/src/Common/mFS.cs

public static class
mLauncher_Tests {
	public static tInt32
	Main(
		tText[] aArgs
	) {
		// This executable is also an independent plugin, with no launcher registration.
		if (aArgs.Length > 0) {
			var Folder = System.IO.Path.GetDirectoryName(aArgs[0])!;
			System.IO.File.WriteAllLines(System.IO.Path.Combine(Folder, "observed.txt"), aArgs);
			return 23;
		}
		
		var FolderPath = System.IO.Path.Combine(
			System.IO.Path.GetTempPath(),
			"SPO launcher " +
			System.Guid.NewGuid()
		);
		System.IO.Directory.CreateDirectory(FolderPath);
		var Config = System.IO.Path.Combine(FolderPath, "startup.jni");
		var Observed = System.IO.Path.Combine(FolderPath, "observed.txt");
		try {
			var Assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
			foreach (var Plugin in new[] {
					Assembly,
					System.IO.Path.ChangeExtension(Assembly, ".exe")
				}
			
			) {
				System.IO.File.WriteAllText(Config, "runtime=" + System.IO.Path.GetRelativePath(FolderPath, Plugin));
				mAssert.AreEquals(23, mProgram.Main([FolderPath, "--check"]));
				var Actual = System.IO.File.ReadAllLines(Observed);
				mAssert.AreEquals(2, Actual.Length);
				mAssert.IsTrue(mFS.File(Config).IsSameFile(mFS.File(Actual[0])));
				mAssert.AreEquals("--check", Actual[1]);
			}
			
			System.Console.WriteLine(
				"OK: unregistered DLL/EXE plugins, paths with spaces, arguments and exit codes."
			);
			var Source = System.IO.Path.Combine(FolderPath, "Runtime plugin.cs");
			System.IO.File.WriteAllText(Source, "return 29;");
			System.IO.File.WriteAllText(Config, "runtime=Runtime plugin.cs");
			mAssert.AreEquals(29, mProgram.Main([Config]));
			System.Console.WriteLine("OK: a standalone C# runtime plugin.");
			var Errors = new System.IO.StringWriter();
			var PreviousError = System.Console.Error;
			try {
				System.Console.SetError(Errors);
				foreach (var (Text, Message) in new[] {
						("|orphan", "startup.jni:1: Expected key=value"),
						("runtime=first.cs\nruntime=second.cs", "startup.jni:2: Duplicate key 'runtime'"),
						("entry=Demo", "nonempty 'runtime'"),
						("runtime= ", "nonempty 'runtime'"),
						("runtime=missing.cs", "Runtime plugin was not found"),
						("runtime=startup.jni", "must be .cs, .dll or .exe"),
						(
							"runtime=" +
							System.IO.Path.GetFullPath(
								System.IO.Path.Combine(
									System.IO.Path.GetDirectoryName(mStd.File())!,
									"../src/Program.cs"
								)
							),
							"launcher itself"
						),
						("runtime=" + typeof(mProgram).Assembly.Location, "launcher itself"),
						(
							"runtime=" +
							System.IO.Path.ChangeExtension(typeof(mProgram).Assembly.Location, ".exe"),
							"launcher itself"
						)
					}
				
				) {
					Errors.GetStringBuilder().Clear();
					System.IO.File.WriteAllText(Config, Text);
					mAssert.AreEquals(1, mProgram.Main([Config]));
					mAssert.IsTrue(Errors.ToString().Contains(Message), Errors.ToString());
				}
			} finally {
				System.Console.SetError(PreviousError);
			}
			
			System.Console.WriteLine("OK: missing/invalid runtime plugins are reported before launch.");
		} finally {
			System.IO.File.Delete(Config);
			System.IO.File.Delete(Observed);
			System.IO.File.Delete(System.IO.Path.Combine(FolderPath, "Runtime plugin.cs"));
			System.IO.Directory.Delete(FolderPath);
		}
		
		return 0;
	}
}