#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = Exe
#:property PublishAot = false
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../SPO_CS/src/Common/mJNI.cs
#:ref ../../SPO_CS/src/Common/mFS.cs

public static class
mProgram {
	public static tInt32
	Main(
		tText[] aArgs
	) {
		var Args = mStream.Stream(System.MemoryExtensions.AsSpan(aArgs));
		if (Args.Any(__ => __ == "--help")) {
			System.Console.WriteLine(
				"""
				SPO [--check] [app-folder | startup.jni]
				The startup.jni runtime path selects a .cs, .dll or .exe runtime plugin.
				Options are forwarded to the runtime; --check validates without opening a window.
				Default app: Apps/Snake/startup.jni
				"""
			);
			return 0;
		}
		
		var Options = Args.Where(__ => __ == "--check");
		var Paths = Args.Where(__ => __ != "--check");
		if (Paths.Count() > 1 || Paths.Any(__ => __.StartsWith("--"))) {
			System.Console.Error.WriteLine("Unknown option or multiple app paths; use --help.");
			return 1;
		}
		
		try {
			var StartupFile = mFS.File(
				Paths.TryFirst().Then(mFS.Path).ElseUse(
					mFS.File(mStd.File()).Folder._Path / "../Apps/Snake/startup.jni"
				).ToAbsolute()
			);
			if (mFS.Folder(StartupFile.Path).Exists()) {
				StartupFile = mFS.Folder(StartupFile.Path).GetFile("startup.jni");
			}
			
			if (
				!StartupFile.TryReadText().ThenTry(
					aText => mJNI.TryParseTree(
						mJNI.ToLines(aText, 0),
						StartupFile.Path.ToText()
					)
				).Match(
					out var Startup,
					out var ParseError
				)
			) {
				System.Console.Error.WriteLine(ParseError);
				return 1;
			}
			
			if (
				!Startup.TryGet("runtime").Then(mJNI.ToText).IsSome(out var RuntimePath) ||
				tText.IsNullOrWhiteSpace(RuntimePath)
			) {
				throw new System.FormatException("Startup requires a nonempty 'runtime' plugin path.");
			}
			
			var Folder = StartupFile.Folder;
			var RuntimeFile = Folder.GetFile(RuntimePath.Trim());
			var Launcher = mFS.File(typeof(mProgram).Assembly.Location);
			if (
				mStream.Stream(
					mFS.File(mStd.File()),
					Launcher,
					Launcher.Folder.GetFile(Launcher.NameWithoutExtension + ".exe")
				).Any(aFile => RuntimeFile.IsSameFile(aFile))
			) {
				throw new System.FormatException("The runtime must be a plugin, not the launcher itself.");
			}
			
			if (!RuntimeFile.Exists()) {
				System.Console.Error.WriteLine("Runtime plugin was not found: " + RuntimeFile.Path.ToText());
				return 1;
			}
			
			var Extension = RuntimeFile.Extension.ToLowerInvariant();
			if (Extension is not ("cs" or "dll" or "exe")) {
				throw new System.FormatException(
					"Runtime plugins must be .cs, .dll or .exe files: " +
					RuntimeFile.Path
				);
			}
			
			var Start = new System.Diagnostics.ProcessStartInfo {
				FileName = (
					Extension == "exe"
					? RuntimeFile.Path.ToText()
					: System.Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet"
				),
				WorkingDirectory = Folder._Path.ToText(),
				UseShellExecute = false
			};
			if (Extension != "exe") {
				Start.ArgumentList.Add(RuntimeFile.Path.ToText());
				if (Extension == "cs") {
					Start.ArgumentList.Add("--");
				}
			}
			
			Start.ArgumentList.Add(StartupFile.Path.ToText());
			foreach (var Option in Options) {
				Start.ArgumentList.Add(Option);
			}
			
			using var Runtime = System.Diagnostics.Process.Start(Start) ?? throw new System.InvalidOperationException(
				"Runtime plugin could not be started: " +
				RuntimeFile.Path
			);
			Runtime.WaitForExit();
			return Runtime.ExitCode;
		} catch (System.Exception Error) {
			System.Console.Error.WriteLine(Error.Message);
			return 1;
		}
	}
}