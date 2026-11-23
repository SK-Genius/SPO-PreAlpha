#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Exe"
#:property PublishAot = "false"
#:include ../../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../src/mStartup.cs
#:ref ../../Runtime/Terminal/mTerminal.cs

public static class
mSnake_Compare {
	public static tInt32
	Main(
		tText[] aArgs
	) => mRuntime.WithCompilerStack(
		() => {
			var Mode = aArgs.Length > 0 ? aArgs[0] : "move";
			var Eat = Mode.StartsWith("eat", System.StringComparison.Ordinal);
			var Frame = Mode is "frame" or "eat-frame";
			mAssert.IsTrue(Mode is "move" or "eat" or "frame" or "eat-frame");
			var Length = aArgs.Length > 1 ? tInt32.Parse(aArgs[1]) : 16;
			var Iterations = aArgs.Length > 2 ? tInt32.Parse(aArgs[2]) : 100;
			mAssert.IsTrue(Length >= 2 && Length <= (Eat ? 446 : 24) && Iterations > 0);
			
			var Startup = mStartup.Load(
				System.IO.Path.Combine(System.IO.Path.GetDirectoryName(mStd.File())!, "../../Apps/Snake/startup.jni")
			);
			
			var Logic = Startup.Modules.Where(__ => __.Key == "Logic").Init(_ => {}).ElseThrow();
			var After = Logic.Data.Field("Logic").Field("...After...");
			var Body = mVM_Data.Empty();
			
			for (var Index = 0; Index < Length; Index += 1) {
				var X = Eat ? Index == Length - 1 ? 2 : (Index + 2) % 28 + 1 : Index + 3;
				var Y = Eat ? (Index + 2) / 28 + 1 : 8;
				if (Eat && Index == Length - 1) { Y = 1; }
				Body = mVM_Data.Pair(Body, mVM_Data.Tuple([mVM_Data.Int(X), mVM_Data.Int(Y)]));
			}
			
			var Direction = mVM_Data.Prefix(Eat ? "_Left" : "_Right", mVM_Data.Empty());
			
			var State = mVM_Data.Record(
				[
					("_SnakeBody", Body),
					("_MoveDirection", Direction),
					("_PendingDirection", Direction),
					("_FoodPosition", mVM_Data.Tuple([mVM_Data.Int(Eat ? 1 : 28), mVM_Data.Int(Eat ? 1 : 16)])),
					("_RandomState", mVM_Data.Int(16807)),
					("_Phase", mVM_Data.Prefix("_Playing", mVM_Data.Empty()))
				]
			);
			
			if (Frame) {
				var App = Startup.Entry.Init(_ => {}).ElseThrow();
				mAssert.IsTrue(App.OpenSig(out _, out _, out var AppBody));
				After = AppBody.Field("Frame...");
			}
			
			var Argument = mVM_Data.Tuple(
				[
					State,
					Frame
					? mVM_Data.Prefix("_Tick...", mVM_Data.Int(140))
					: mVM_Data.Prefix("_Advance", mVM_Data.Empty())
				]
			);
			
			var Terminal = new mTerminal.tTerminal {
				Size = () => new((80, 25), (0, 0), (80, 25)),
				Cursor = (_, _) => {},
				CursorVisible = () => true,
				SetCursorVisible = _ => {},
				Draw = (_, _, _) => {},
				Clear = () => {},
				Present = () => {},
				ReadEvent = _ => mVM_Data.Empty()
			};
			
			System.Console.WriteLine("mode,length,iterations,sample,ms/op,bytes/op");
			
			Terminal.WithCapability(
				aCapability => {
					var Receiver = Frame ? aCapability : mVM_Data.Empty();
					
					for (var Index = 0; Index < 20; Index += 1) {
						After.Call(Argument, Receiver);
					}
					
					for (var Sample = 1; Sample <= 5; Sample += 1) {
						var Result = State;
						var Watch = new System.Diagnostics.Stopwatch();
						var Before = System.GC.GetAllocatedBytesForCurrentThread();
						Watch.Start();
						
						for (var Index = 0; Index < Iterations; Index += 1) {
							Result = After.Call(Argument, Receiver);
						}
						
						Watch.Stop();
						var Allocated = System.GC.GetAllocatedBytesForCurrentThread() - Before;
						
						if (Frame) {
							mAssert.IsTrue(Result.IsTuple(out Result, out var Continue));
							mAssert.IsTrue(Continue.IsBool(out var Running) && Running);
							// The benchmark checks its known concrete state after timing.
							Result = Result._Value.To<mVM_Data.tOpaqueValue>().Data;
						}
						
						mAssert.IsTrue(Result.Field("Phase").IsPrefix("_Playing", out _));
						mAssert.IsTrue(Result.Field("SnakeBody").IsPair(out _, out var Head));
						
						var Point = mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]);
						
						var ExpectedHead = mVM_Data.Tuple(
							[mVM_Data.Int(Eat ? 1 : Length + 3), mVM_Data.Int(Eat ? 1 : 8)]
						);
						
						mAssert.IsTrue((Head, Point).SameVisibleValue((ExpectedHead, Point)));
						
						mAssert.IsTrue(Result.Field("RandomState").IsInt(out var Random));
						mAssert.AreEquals(Eat ? 282475249 : 16807, Random);
						
						System.Console.WriteLine(
							$"{Mode},{Length},{Iterations},{Sample}," +
							$"{Watch.Elapsed.TotalMilliseconds / Iterations:F3},{Allocated / Iterations}"
						);
					}
				}
			);
			
			return 0;
		}
	);
}