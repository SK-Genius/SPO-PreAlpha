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
	public static mVM_Data.tData
	Run(
		tText aCode,
		mVM_Data.tData aImport,
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
		
		var Res = mVM_Data.Empty();
		
		// TODO: move to mIL_Interpreter.Run(...) ???
		var DefTuple = mVM_Data.Empty();
		var Defs = VMModule.Skip(1).Reverse();
		switch (Defs.Take(2).ToArrayList().Size()) {
			case 0: {
				break;
			}
			case 1: {
				DefTuple = mVM_Data.Def(Defs.First());
				break;
			}
			default: {
				while (Defs.Match(out var Def, out Defs)) {
					DefTuple = mVM_Data.Pair(
						mVM_Data.Def(Def),
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
		
		mVM.Run(
			mVM_Data.Proc(InitProc, DefTuple),
			mVM_Data.Empty(),
			aImport,
			Res,
			TraceOut
		);
		
		return Res;
	}
	
	#region TEST
	
	//================================================================================
	private static mVM_Data.tData
	Mul(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mStd.Assert(Arg1.MatchInt(out var IntArg1));
		mStd.Assert(Arg2.MatchPair(out var Arg2_, out var _));
		mStd.Assert(Arg2_.MatchInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 * IntArg2);
	}
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_Interpreter),
		mTest.Test(
			"Run1",
			aDebugStream => {
				mStd.AssertEq(
					Run(
						@"
							§IMPORT (
								...*...
								k
							)
							
							§DEF x... = (a => (k .* a))
							§DEF y = (.x 5)
							
							§EXPORT y
						",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						aDebugStream
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test(
			"Run2",
			aDebugStream => {
				mStd.AssertEq(
					Run(
						@"
							§IMPORT (
								...*...
								k
							)
							
							§DEF y = (.(a => (k .* a)) 5)
							
							§EXPORT y
						",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						aDebugStream
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test(
			"Run3",
			aDebugStream => {
				mStd.AssertEq(
					Run(
						@"
							§IMPORT (
								...*...
								k
							)
							
							§EXPORT .(a => (k .* a)) 5
						",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						aDebugStream
					),
					mVM_Data.Int(10)
				);
			}
		)
	);
	
	#endregion
}
