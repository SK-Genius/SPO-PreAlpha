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

static class mSPO_Interpreter {

	//================================================================================
	public static mIL_VM.tData
	Run(
		tText aCode,
		mIL_VM.tData aImport,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		mStd.Assert(mSPO_Parser.MODULE.ParseText(aCode, aDebugStream).MATCH(out mSPO_AST.tModuleNode ModuleNode));
		
		var Module = mSPO2IL.NewModuleConstructor();
		mSPO2IL.MapModule(ref Module, ModuleNode);
		
		var X = mIL_Interpreter.ParseModule( //TODO: rename X
			Module.Defs.ToLasyList().Map(
				D => {
					D.MATCH(out var Name, out var Commands);	
					return mStd.Tuple(Name, Commands.ToLasyList());
				}
			)
		);
		
		X.MATCH(out var VMModule, out var ModuleMap);
		var InitProc = VMModule._Tail._Head; // TODO: get last
		var Res = mIL_VM.EMPTY();
		mIL_VM.Run(
			mIL_VM.PROC(InitProc, mIL_VM.DEF(VMModule._Head)), // TODO: all defs as list
			mIL_VM.EMPTY(),
			aImport,
			Res
		);
		
		return Res;
	}
	
	#region TEST
	
	//================================================================================
	private static readonly mStd.tFunc<mIL_VM.tData, mIL_VM.tData, mIL_VM.tData, mIL_VM.tData>
	Mul = (
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) => {
		mIL_VM.tData Arg1;
		mIL_VM.tData Arg2;
		mStd.Assert(aArg.MATCH(mIL_VM.tDataType.PAIR, out Arg1, out Arg2));
		tInt32 Arg1_;
		mStd.Assert(Arg1.MATCH(mIL_VM.tDataType.INT, out Arg1_));
		tInt32 Arg2_;
		mStd.Assert(Arg2.MATCH(mIL_VM.tDataType.INT, out Arg2_));
		return mIL_VM.INT(Arg1_ * Arg2_);
	};
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"SPO_Interpreter",
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
							mIL_VM.PAIR(
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
			"SPO_Interpreter2",
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
							mIL_VM.PAIR(
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
			"SPO_Interpreter3",
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
							mIL_VM.PAIR(
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
