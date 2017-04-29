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

using tResults = mList.tList<mStd.tAny>;

using tText_Parser = mParserGen.tParser<mStd.tTuple<char, mStd.tAction<string>>>;

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
			mSPO_Parser.MODULE.ParseText(
				aCode,
				aDebugStream
			).MATCH(out mSPO_AST.tModuleNode ModuleNode)
		);
		
		var ModuleConstructor = mSPO2IL.MapModule(ModuleNode);
		
		mIL_Interpreter.ParseModule(
			ModuleConstructor.Defs.ToLasyList()
				.MapWithIndex(
				(aIndex, aCommands) => {
					return mStd.Tuple(mSPO2IL.TempDef(aIndex), aCommands.ToLasyList());
				}
			),
			aDebugStream
		).MATCH(out var VMModule, out var ModuleMap);
		
		var Res = mIL_VM.EMPTY();
		
		// TODO: move to mIL_Interpreter.Run(...) ???
		var DefTuple = mIL_VM.EMPTY();
		var Defs = VMModule.Skip(1).Reverse();
		switch (Defs.Take(2).ToArrayList().Size()) {
			case 0: {
				break;
			}
			case 1: {
				DefTuple = mIL_VM.DEF(Defs._Head);
				break;
			}
			default: {
				while (Defs.MATCH(out var Def, out Defs)) {
					DefTuple = mIL_VM.PAIR(
						mIL_VM.DEF(Def),
						DefTuple
					);
				}
				break;
			}
		}
		var InitProc = VMModule._Head;
		
		#if TRACE
			var TraceOut = aDebugStream;
		#else
			var TraceOut = mStd.Action<tText>(_ => {});
		#endif
		
		mIL_VM.Run(
			mIL_VM.PROC(InitProc, DefTuple),
			mIL_VM.EMPTY(),
			aImport,
			Res,
			TraceOut
		);
		
		return Res;
	}
	
	#region TEST
	
	//================================================================================
	private static readonly mStd.tFunc<mIL_VM.tData, mIL_VM.tData, mIL_VM.tData, mIL_VM.tData, mStd.tAction<tText>>
	Mul = (
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<tText> aTraceOut
	//================================================================================
	) => {
		mIL_VM.tData Arg1;
		mIL_VM.tData Arg2;
		mStd.Assert(aArg.MATCH(mIL_VM.tDataType.PAIR, out Arg1, out Arg2));
		tInt32 Arg1_;
		mStd.Assert(Arg1.MATCH(mIL_VM.tDataType.INT, out Arg1_));
		mIL_VM.tData Arg2_;
		mIL_VM.tData _;
		mStd.Assert(Arg2.MATCH(mIL_VM.tDataType.PAIR, out Arg2_, out _));
		tInt32 Arg2__;
		mStd.Assert(Arg2_.MATCH(mIL_VM.tDataType.INT, out Arg2__));
		return mIL_VM.INT(Arg1_ * Arg2__);
	};
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>>
	Test = mTest.Tests(
		mStd.Tuple(
			"Run1",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					mStd.AssertEq(
						Run(
							mList.List(
								"§IMPORT (",
								"	...*...,",
								"	k",
								")",
								"",
								"x... := (a => (k .* a));",
								"y := (.x 5)",
								"",
								"§EXPORT y",
								""
							).Join((a1, a2) => a1 + "\n" + a2),
							mIL_VM.TUPLE(
								mIL_VM.EXTERN_PROC(Mul, mIL_VM.EMPTY()),
								mIL_VM.INT(2)
							),
							aDebugStream
						),
						mIL_VM.INT(10)
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"Run2",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					mStd.AssertEq(
						Run(
							mList.List(
								"§IMPORT (",
								"	...*...,",
								"	k",
								")",
								"",
								"y := (.(a => (k .* a)) 5)",
								"",
								"§EXPORT y",
								""
							).Join((a1, a2) => a1 + "\n" + a2),
							mIL_VM.TUPLE(
								mIL_VM.EXTERN_PROC(Mul, mIL_VM.EMPTY()),
								mIL_VM.INT(2)
							),
							aDebugStream
						),
						mIL_VM.INT(10)
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"Run3",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					mStd.AssertEq(
						Run(
							mList.List(
								"§IMPORT (",
								"	...*...,",
								"	k",
								")",
								"",
								"§EXPORT (.(a => (k .* a)) 5)",
								""
							).Join((a1, a2) => a1 + "\n" + a2),
							mIL_VM.TUPLE(
								mIL_VM.EXTERN_PROC(Mul, mIL_VM.EMPTY()),
								mIL_VM.INT(2)
							),
							aDebugStream
						),
						mIL_VM.INT(10)
					);
					
					return true;
				}
			)
		)
	);
	
	#endregion
}
