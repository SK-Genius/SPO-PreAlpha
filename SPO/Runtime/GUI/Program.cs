#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Exe
#:property PublishAot = false
#:include ../../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../src/mStartup.cs
#:ref mGUI.cs
#:ref mSDL3.cs

public static class
mGUI_Program {
	public static tInt32
	Main(
		tText[] aArgs
	) => mRuntime.WithCompilerStack(
		() => {
			var Args = mStream.Stream(System.MemoryExtensions.AsSpan(aArgs));
			if (Args.Any(a => a == "--help")) {
				System.Console.WriteLine("SPO GUI [--check] [app-folder | startup.jni]");
				System.Console.WriteLine(
					"Default app: Apps/GUI_Demo. SDL3_LIBRARY may name an SDL3 native library."
				);
				return 0;
			}
			
			var Paths = Args.Where(a => a != "--check");
			if (Paths.Count() > 1 || Paths.Any(a => a.StartsWith("--"))) {
				System.Console.Error.WriteLine("Unknown option or multiple app paths; use --help.");
				return 1;
			}
			
			var Path = System.IO.Path.GetFullPath(
				(
					Paths.TryFirst().IsSome(out var AppPath)
					? AppPath
					: System.IO.Path.Combine(System.IO.Path.GetDirectoryName(mStd.File())!, "../../Apps/GUI_Demo")
				)
			);
			if (System.IO.Directory.Exists(Path)) {
				Path = System.IO.Path.Combine(Path, "startup.jni");
			}
			
			try {
				void
				Run(
					mGUI.tSurface aSurface
				) => aSurface.WithCapability(
					aCapability => {
						var Startup = mStartup.Load(Path);
						var App = Startup.Entry.Init(
							_ => {}
						).ElseThrow();
						mAssert.IsTrue(App.Data.IsSig(out var Contract, out _, out var Body));
						mAssert.IsTrue(Contract.SameType(mGUI.Signature("App")), "Expected GUI/App.SIG export.");
						if (Args.Any(a => a == "--check")) {
							return;
						}
						
						var State = Body.Field("Initial");
						var Frame = Body.Field("Frame...");
						var Event = mVM_Data.Prefix("_Redraw", mVM_Data.Empty());
						while (true) {
							var Result = Frame.Call(mVM_Data.Tuple([State, Event]), aCapability);
							mAssert.IsTrue(Result.IsTuple(out State, out var Continue));
							mAssert.IsTrue(Continue.IsBool(out var Running));
							if (!Running) {
								break;
							}
							
							aSurface.Present();
							// Rebuild once after input so earlier widgets reflect state changed later in the frame.
							Event = (
								Event.IsPrefix("_Redraw", out _)
								? aSurface.ReadEvent()
								: mVM_Data.Prefix("_Redraw", mVM_Data.Empty())
							);
						}
					}
				);
				
				if (Args.Any(a => a == "--check")) {
					// Checking module contracts does not load SDL or open a window.
					Run(
						new() {
							Size = () => (1024, 768),
							Clear = _ => {},
							Rectangle = (_, _, _) => {},
							Text = (_, _, _) => {},
							Clip = _ => {},
							Title = _ => {},
							Present = () => {},
							ReadEvent = () => mVM_Data.Prefix("_Quit", mVM_Data.Empty())
						}
					);
					System.Console.WriteLine("OK: GUI startup, modules and application contract.");
				} else {
					mSDL3.WithWindow(Run);
				}
				
				return 0;
			} catch (System.Exception Error) {
				System.Console.Error.WriteLine(Error);
				return 1;
			}
		}
	);
}