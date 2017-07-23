using tBool = System.Boolean;

using tNat8 = System.Byte;
using tNat16 = System.UInt16;
using tNat32 = System.UInt32;
using tNat64 = System.UInt64;

using tInt8 = System.SByte;
using tInt16 = System.Int16;
using tInt32 = System.Int32;
using tInt64 = System.Int64;

using tChar = System.Char;
using tText = System.String;

public static class mSPO_Interpreter {

	//================================================================================
	public static mIL_VM.tData
	Run(
		tText aCode,
		mIL_VM.tData aImport,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		mStd.Assert(
			mSPO_Parser.Module.ParseText(
				aCode,
				aDebugStream
			).Match(out mSPO_AST.tModuleNode ModuleNode)
		);
		
		var ModuleConstructor = mSPO2IL.MapModule(ModuleNode);
		
		var (VMModule, ModuleMap) = mIL_Interpreter.ParseModule(
			ModuleConstructor.Defs.ToLasyList(
			).MapWithIndex(
				(aIndex, aCommands) => (mSPO2IL.TempDef(aIndex), aCommands.ToLasyList())
			),
			aDebugStream
		);
		
		var Res = mIL_VM.Empty();
		
		// TODO: move to mIL_Interpreter.Run(...) ???
		var DefTuple = mIL_VM.Empty();
		var Defs = VMModule.Skip(1).Reverse();
		switch (Defs.Take(2).ToArrayList().Size()) {
			case 0: {
				break;
			}
			case 1: {
				DefTuple = mIL_VM.Def(Defs.First());
				break;
			}
			default: {
				while (Defs.Match(out var Def, out Defs)) {
					DefTuple = mIL_VM.Pair(
						mIL_VM.Def(Def),
						DefTuple
					);
				}
				break;
			}
		}
		var InitProc = VMModule.First();
		
		#if TRACE
			var TraceOut = mStd.Action(
				(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
			);
		#else
			var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
		#endif
		
		mIL_VM.Run(
			mIL_VM.Proc(InitProc, DefTuple),
			mIL_VM.Empty(),
			aImport,
			Res,
			TraceOut
		);
		
		return Res;
	}
	
	#region TEST
	
	//================================================================================
	private static mIL_VM.tData
	Mul(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.Match(mIL_VM.tDataType.Pair, out mIL_VM.tData Arg1, out mIL_VM.tData Arg2));
		mStd.Assert(Arg1.Match(mIL_VM.tDataType.Int, out tInt32 IntArg1));
		mStd.Assert(Arg2.Match(mIL_VM.tDataType.Pair, out mIL_VM.tData Arg2_, out mIL_VM.tData _));
		mStd.Assert(Arg2_.Match(mIL_VM.tDataType.Int, out tInt32 IntArg2));
		return mIL_VM.Int(IntArg1 * IntArg2);
	}
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_Interpreter),
		mTest.Test(
			"Run1",
			aDebugStream => {
				mStd.AssertEq(
					Run(
						mList.List(
							"§IMPORT (",
							"	...*...",
							"	k",
							")",
							"",
							"§DEF x... = (a => (k .* a))",
							"§DEF y = (.x 5)",
							"",
							"§EXPORT y"
						).Join((a1, a2) => a1 + "\n" + a2),
						mIL_VM.Tuple(
							mIL_VM.ExternProc(Mul, mIL_VM.Empty()),
							mIL_VM.Int(2)
						),
						aDebugStream
					),
					mIL_VM.Int(10)
				);
			}
		),
		mTest.Test(
			"Run2",
			aDebugStream => {
				mStd.AssertEq(
					Run(
						mList.List(
							"§IMPORT (",
							"	...*...",
							"	k",
							")",
							"",
							"§DEF y = (.(a => (k .* a)) 5)",
							"",
							"§EXPORT y"
						).Join((a1, a2) => a1 + "\n" + a2),
						mIL_VM.Tuple(
							mIL_VM.ExternProc(Mul, mIL_VM.Empty()),
							mIL_VM.Int(2)
						),
						aDebugStream
					),
					mIL_VM.Int(10)
				);
			}
		),
		mTest.Test(
			"Run3",
			aDebugStream => {
				mStd.AssertEq(
					Run(
						mList.List(
							"§IMPORT (",
							"	...*...",
							"	k",
							")",
							"",
							"§EXPORT .(a => (k .* a)) 5"
						).Join((a1, a2) => a1 + "\n" + a2),
						mIL_VM.Tuple(
							mIL_VM.ExternProc(Mul, mIL_VM.Empty()),
							mIL_VM.Int(2)
						),
						aDebugStream
					),
					mIL_VM.Int(10)
				);
			}
		)
	);
	
	#endregion
}
