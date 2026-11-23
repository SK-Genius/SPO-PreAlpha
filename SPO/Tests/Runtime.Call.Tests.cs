#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Exe"
#:property PublishAot = "false"
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../SPO_CS/src/mSPO_Interpreter.cs
#:ref ../Runtime/Terminal/mTerminal.cs

public static class
mRuntime_Call_Tests {
	public static tInt32
	Main(
	) => mRuntime.WithCompilerStack(
		() => {
			static mVM_Data.tData
			Compile(
				tText aSource
			) => mSPO_Interpreter.Run(
				aSource,
				mStd.File(),
				(mVM_Data.Empty(), mVM_Type.Empty()),
				_ => {}
			).ElseThrow(
			).Data;
			
			static tText
			Reject(mStd.tAction aCall) {
				try {
					aCall();
				} catch (mError.tError Error) {
					mAssert.IsTrue(Error.Message.Contains("Erwarteter Typ:"), Error.Message);
					mAssert.IsTrue(Error.Message.Contains("Aktueller Typ:"), Error.Message);
					return Error.Message;
				}
				
				throw new System.InvalidOperationException("Invalid argument was accepted.");
			}
			
			var Integer = Compile("§EXPORT (§DEF aValue € §INT) => 7");
			mAssert.IsTrue(Integer.Call(mVM_Data.Int(42)).IsInt(out var Result) && Result == 7);
			var Message = Reject(() => Integer.Call(mVM_Data.Bool(true)));
			mAssert.IsTrue(Message.Contains("§INT") && Message.Contains("§TRUE"));
			var Pair = Compile("§EXPORT (§DEF aNumber € §INT, §DEF aFlag € §BOOL) => aNumber");
			Reject(() => Pair.Call(mVM_Data.Tuple([mVM_Data.Int(1)])));
			mAssert.IsTrue(Pair.Call(mVM_Data.Tuple([mVM_Data.Int(1), mVM_Data.Bool(false)])).IsInt(out _));
			
			var Record = Compile(
				"""
				§EXPORT (§DEF aRecord € [<
					Value: §INT
				>]) => 7
				"""
			);
			
			mAssert.IsTrue(
				Record.Call(
					mVM_Data.Record(
						[("_Value", mVM_Data.Int(1)), ("_Extra", mVM_Data.Bool(true))]
					)
				).IsInt(
					out _
				)
			);
			
			Reject(() => Record.Call(mVM_Data.Record([("_Value", mVM_Data.Bool(false))])));
			
			System.Console.WriteLine(
				"OK: scalar, tuple and record arguments; extra record fields remain valid."
			);
			
			var Event = Compile(
				"""
				§EXPORT (
					§DEF aEvent € [§LOAD "../Libs/Event.SIG"]
				) => 7
				"""
			);
			
			var InvalidKey = mVM_Data.Prefix(
				"_Key...",
				mVM_Data.Record(
					[
						("_Key", mVM_Data.Prefix("_ArrowLeft", mVM_Data.Empty())),
						("_IsDown", mVM_Data.Bool(false)),
						("_Modifiers", mVM_Data.Record([("_Control", mVM_Data.Bool(false))]))
					]
				)
			);
			
			Message = Reject(() => Event.Call(InvalidKey));
			mAssert.IsTrue(Message.Contains("_IsRepeat") && Message.Contains("_ArrowLeft"));
			var Tick = mVM_Data.Prefix("_Tick...", mVM_Data.Int(140));
			mAssert.IsTrue(Event.Call(Tick).IsInt(out Result) && Result == 7);
			System.Console.WriteLine(Message);
			
			var Clears = 0;
			var Reads = 0;
			
			var Terminal = new mTerminal.tTerminal {
				Size = () => {
					Reads += 1;
					return new((80, 25), (0, 0), (80, 25));
				},
				Cursor = (_, _) => {},
				CursorVisible = () => true,
				SetCursorVisible = _ => {},
				Draw = (_, _, _) => {},
				Clear = () => Clears += 1,
				Present = () => {},
				ReadEvent = _ => mVM_Data.Empty()
			};
			
			var Method = Compile(
				"""
				§EXPORT (
					§DEF aTerminal € [§LOAD "../Libs/Terminal.SIG"]
				) : (
					§DEF aEvent € [§LOAD "../Libs/Event.SIG"]
				) {
					aTerminal : Clear .
					§RETURN 7
				}
				"""
			);
			
			var CallbackMethod = Compile(
				"""
				§EXPORT (
					§DEF aTerminal € [§LOAD "../Libs/Terminal.SIG"]
				) : (
					§DEF aCallback € [§INT => §INT]
				) {
					aTerminal : Clear .
					§RETURN 7
				}
				"""
			);
			
			var ValidCallback = Compile("§EXPORT (§DEF aValue € §INT) => aValue");
			
			var Package = mSPO_Interpreter.Run(
				"""
				§DEF tTerminal = [§LOAD "../Libs/Terminal.SIG"]
				§DEF tContract = [§SIG_WITH t € §TYPE IN [[tTerminal] : t => §INT]]
				§DEF Operation... = (
					§DEF aTerminal € tTerminal
				) : (
					§DEF aValue € §INT
				) {
					aTerminal : Clear .
					§RETURN 7
				}
				§EXPORT §SIG tContract WITH §INT IN Operation...
				""",
				mStd.File(),
				(mVM_Data.Empty(), mVM_Type.Empty()),
				_ => {}
			).ElseThrow();
			
			mAssert.IsTrue(Package.OpenSig(out _, out _, out var OpaqueMethod));
			var InvalidCallback = Compile("§EXPORT (§DEF aValue € §BOOL) => 7");
			var InvalidResult = Compile("§EXPORT (§DEF aValue € §INT) => §TRUE");
			
			Terminal.WithCapability(
				aCapability => {
					Reject(() => OpaqueMethod.Call(mVM_Data.Bool(true), aCapability));
					
					var InvalidOpaque = new mVM_Data.tData {
						_Value = mAny.Any(new mVM_Data.tOpaqueValue(mVM_Data.Bool(true)))
					};
					
					Reject(() => OpaqueMethod.Call(InvalidOpaque, aCapability));
					mAssert.AreEquals(0, Clears);
					mAssert.IsTrue(OpaqueMethod.Call(mVM_Data.Int(42), aCapability).IsInt(out Result) && Result == 7);
					mAssert.AreEquals(1, Clears);
					Clears = 0;
					Reject(() => CallbackMethod.Call(InvalidCallback, aCapability));
					Reject(() => CallbackMethod.Call(InvalidResult, aCapability));
					mAssert.AreEquals(0, Clears);
					mAssert.IsTrue(CallbackMethod.Call(ValidCallback, aCapability).IsInt(out Result) && Result == 7);
					mAssert.AreEquals(1, Clears);
					Clears = 0;
					Reject(() => Method.Call(InvalidKey, aCapability));
					Message = Reject(() => Method.Call(Tick, mVM_Data.Int(0)));
					mAssert.IsTrue(Message.Contains("Empfänger"));
					mAssert.AreEquals(0, Clears);
					mAssert.IsTrue(Method.Call(Tick, aCapability).IsInt(out Result) && Result == 7);
					mAssert.AreEquals(1, Clears);
					Reject(() => aCapability.CallMethod("Size", mVM_Data.Int(1)));
					mAssert.AreEquals(0, Reads);
					mAssert.IsTrue(aCapability.CallMethod("Size", mVM_Data.Empty()).IsRecord(out _));
					mAssert.AreEquals(1, Reads);
				}
			);
			
			System.Console.WriteLine("OK: invalid arguments/receivers fail before SPO or native side effects.");
			
			var Deep = mVM_Data.Int(1);
			
			for (var Index = 0; Index < 100; Index += 1) {
				Deep = mVM_Data.Pair(Deep, Deep);
			}
			
			Message = Reject(() => Integer.Call(Deep));
			mAssert.IsTrue(Message.Contains("gekürzt"));
			var Cycle = mVM_Data.Var(mVM_Data.Empty());
			mAssert.IsTrue(Cycle._Value.Is(out mRef.tRefBox<mVM_Data.tData> CycleCell));
			CycleCell._Value = Cycle;
			Message = Reject(() => Integer.Call(Cycle));
			mAssert.IsTrue(Message.Contains("gekürzt"));
			System.Console.WriteLine("OK: bounded diagnostic traversal for deep/shared and cyclic values.");
			return 0;
		}
	);
}