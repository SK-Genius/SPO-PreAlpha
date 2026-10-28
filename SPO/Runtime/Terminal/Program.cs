#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Exe
#:property PublishAot = false
#:include ../../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../src/mStartup.cs
#:ref mTerminal.cs
#:ref mWindowsTerminal.cs

public static class
mTerminal_Program {
	public static tInt32
	Main(
		tText[] aArgs
	) => mRuntime.WithCompilerStack(
		() => {
			var Args = mStream.Stream(System.MemoryExtensions.AsSpan(aArgs));
			if (Args.Any(__ => __ == "--help")) {
				System.Console.WriteLine("SPO Terminal [--check] [app-folder | startup.jni]");
				System.Console.WriteLine("Default app: Apps/Snake/startup.jni");
				return 0;
			}
			
			var CheckOnly = Args.Any(__ => __ == "--check");
			var Paths = Args.Where(__ => __ != "--check");
			if (Paths.Count() > 1 || Paths.Any(__ => __.StartsWith("--"))) {
				System.Console.Error.WriteLine("Expected [--check] [app-folder | startup.jni].");
				return 1;
			}
			
			var Path = (
				Paths.TryFirst().IsSome(out var AppPath)
				? System.IO.Path.GetFullPath(AppPath)
				: System.IO.Path.GetFullPath(
					System.IO.Path.Combine(
						System.IO.Path.GetDirectoryName(mStd.File())!,
						"../../Apps/Snake/startup.jni"
					)
				)
			);
			if (System.IO.Directory.Exists(Path)) {
				Path = System.IO.Path.Combine(Path, "startup.jni");
			}
			
			try {
				void
				Run(
					mTerminal.tTerminal aTerminal
				) => aTerminal.WithCapability(
					aCapability => {
						var Startup = mStartup.Load(Path);
						var App = Startup.Entry.Init(
							_ => {}
						).ElseThrow();
						mAssert.IsTrue(App.Data.IsSig(out var Contract, out _, out var Body));
						mAssert.IsTrue(
							Contract.SameType(mTerminal.LoadSignature("App")),
							"Expected App.SIG export."
						);
						mAssert.IsTrue(Body.Field("TickMilliseconds").IsInt(out var Interval));
						if (Interval is < 1 or > 60000) {
							throw new System.ArgumentOutOfRangeException(
								nameof(Interval),
								"Tick interval must be 1..60000 ms."
							);
						}
						
						if (CheckOnly) {
							return;
						}
						
						var State = Body.Field("Initial");
						var Frame = Body.Field("Frame...");
						
						tBool
						Send(
							mVM_Data.tData aEvent
						) {
							var Result = Frame.Call(mVM_Data.Tuple([State, aEvent]), aCapability);
							mAssert.IsTrue(Result.IsTuple(out State, out var Continue));
							mAssert.IsTrue(Continue.IsBool(out var Running));
							aTerminal.Present();
							return Running;
						}
						
						if (!Send(mVM_Data.Prefix("_Resize...", aTerminal.Size().ToData()))) {
							return;
						}
						
						var Clock = System.Diagnostics.Stopwatch.StartNew();
						var NextTick = Clock.ElapsedMilliseconds + Interval;
						while (true) {
							var Event = aTerminal.ReadEvent(
								(tInt32)System.Math.Max(
									0,
									NextTick - Clock.ElapsedMilliseconds
								)
							);
							if (!Event.IsEmpty() && !Send(Event)) {
								break;
							}
							
							var Now = Clock.ElapsedMilliseconds;
							if (Now >= NextTick) {
								var ElapsedMilliseconds = mVM_Data.Int(
									(tInt32)System.Math.Min(Now, tInt32.MaxValue)
								);
								if (!Send(mVM_Data.Prefix("_Tick...", ElapsedMilliseconds))) {
									break;
								}
								
								// Service input between ticks and skip missed ticks after slow frames.
								NextTick = Clock.ElapsedMilliseconds + Interval;
							}
						}
					}
				);
				
				if (CheckOnly) {
					var CursorVisible = true;
					Run(
						new() {
							Size = () => new((80, 25), (0, 0), (80, 25)),
							Cursor = (_, _) => {},
							CursorVisible = () => CursorVisible,
							SetCursorVisible = aVisible => CursorVisible = aVisible,
							Draw = (_, _, _) => {},
							Clear = () => {},
							Present = () => {},
							ReadEvent = _ => mVM_Data.Empty()
						}
					);
					System.Console.WriteLine("OK: " + Path);
				} else {
					mWindowsTerminal.Run(Run);
				}
				
				return 0;
			} catch (System.Exception Error) {
				System.Console.Error.WriteLine(CheckOnly ? Error.ToString() : Error.Message);
				return 1;
			}
		}
	);
}