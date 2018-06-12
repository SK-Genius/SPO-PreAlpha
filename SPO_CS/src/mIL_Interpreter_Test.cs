﻿using tBool = System.Boolean;

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

using xTest = Xunit.TheoryAttribute;
using xArg = Xunit.InlineDataAttribute;
using xTrait = Xunit.TraitAttribute;

public static class mIL_Interpreter_Test {
	//================================================================================
	private static mVM_Data.tData
	Add (
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mStd.Assert(Arg1.MatchInt(out var IntArg1));
		mStd.Assert(Arg2.MatchInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 + IntArg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Sub (
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mStd.Assert(Arg1.MatchInt(out var IntArg1));
		mStd.Assert(Arg2.MatchInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 - IntArg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Mul (
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mStd.Assert(Arg1.MatchInt(out var IntArg1));
		mStd.Assert(Arg2.MatchInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 * IntArg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Eq(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mStd.Assert(Arg1.MatchInt(out var IntArg1));
		mStd.Assert(Arg2.MatchInt(out var IntArg2));
		return mVM_Data.Bool(IntArg1.Equals(IntArg2));
	}
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mIL_Interpreter),
		mTest.Test(
			"Call",
			aDebugStream => {
				var (Module, ModuleMap) = mIL_Interpreter.ParseModule(
					"§DEF ...++\n" +
					"	_1 := 1\n" +
					"	res := §INT ARG + _1\n" +
					"	§RETURN res IF TRUE\n",
					aDebugStream
				);
				
				#if TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Proc = Module.Skip(ModuleMap.Get("...++")).First();
				var Res = mVM_Data.Empty();
				mVM.Run(
					mVM_Data.Proc(Proc, mVM_Data.Empty()),
					mVM_Data.Empty(),
					mVM_Data.Int(5),
					Res,
					TraceOut
				);
				mStd.AssertEq(Res, mVM_Data.Int(6));
			}
		),
		mTest.Test(
			"Prefix",
			aDebugStream => {
				var (Module, ModuleMap) = mIL_Interpreter.ParseModule(
					"§DEF ...++\n" +
					"	add := .ENV EMPTY\n" +
					"	_1 := 1\n" +
					
					"	arg := -#VECTOR ARG\n" +
					"	arg_1 := arg, _1\n" +
					"	inc := .add arg_1\n" +
					"	res := +#VECTOR inc\n" +
					"	§RETURN res IF TRUE\n",
					aDebugStream
				);
				
				#if TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Proc = Module.Skip(ModuleMap.Get("...++")).First();
				var Env = mVM_Data.ExternDef(Add);
				var Res = mVM_Data.Empty();
				mVM.Run(
					mVM_Data.Proc(Proc, Env),
					mVM_Data.Empty(),
					mVM_Data.Prefix("VECTOR", mVM_Data.Int(12)),
					Res,
					TraceOut
				);
				mStd.AssertEq(Res, mVM_Data.Prefix("VECTOR", mVM_Data.Int(13)));
			}
		),
		mTest.Test(
			"§TYPE_OF...IS...",
			aDebugStream => {
				var (Module, ModuleMap) = mIL_Interpreter.ParseModule(
					"§DEF AssertInt...\n" +
					"	§TYPE_OF ENV IS EMPTY_TYPE\n" +
					"	§TYPE_OF OBJ IS EMPTY_TYPE\n" +
					"	§TYPE_OF ARG IS INT_TYPE\n" +
					"	§RETURN ONE IF TRUE\n",
					aDebugStream
				);
				
				#if TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				mStd.AssertEq(
					Module.Skip(ModuleMap.Get("AssertInt...")).First().DefType,
					mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Empty(),
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Int(),
							mVM_Type.Int()
						)
					)
				);
			}
		),
		mTest.Test(
			"Assert",
			aDebugStream => {
				var (Module, ModuleMap) = mIL_Interpreter.ParseModule(
					"§DEF ...++\n" +
					"	...=...? := . ENV EMPTY\n" +
					"	_1 := 1\n" + 
					"	arg_1 := ARG, _1\n" +
					"	arg_eq_1? := . ...=...? arg_1\n" +
					"	§ASSERT TRUE => arg_eq_1?\n" +
					"	§RETURN arg_eq_1? IF TRUE\n",
					aDebugStream
				);
				
				var Proc = Module.Skip(ModuleMap.Get("...++")).First();
				var Env = mVM_Data.ExternDef(Eq);
				var Res = mVM_Data.Empty();
				
				#if TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var CallStack = mVM.NewCallStack(
					null,
					Proc,
					Env,
					mVM_Data.Empty(),
					mVM_Data.Int(1),
					Res,
					TraceOut
				);
				while (CallStack != null) {
					CallStack = CallStack.Step();
				}
				mStd.AssertEq(Res, mVM_Data.Bool(true));
				mStd.AssertError(
					() => {
						Res = mVM_Data.Empty();
						mVM.Run(
							mVM_Data.Proc(Proc, Env),
							mVM_Data.Empty(),
							mVM_Data.Int(2),
							Res,
							TraceOut
						);
					}
				);
			}
		),
		mTest.Test(
			"ParseModule",
			aDebugStream => {
				var (Module, ModuleMap) = mIL_Interpreter.ParseModule(
					"§DEF bla\n" +
					"	_1 := 1\n" +
					"	add_ := §1ST ENV\n" +
					
					"	add := .add_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
					
					"	p := _1, _1\n" +
					"	r := .add p\n" +
					"	§RETURN r IF TRUE\n" +
					
					"§DEF bla2\n" +
					"	_1 := 1\n" +
					"	add_  := §1ST ENV\n" +
					"	rest1 := §2ND ENV\n" +
					"	sub_  := §1ST rest1\n" +
					"	rest2 := §2ND rest1\n" +
					"	mul_  := §1ST rest2\n" +
					
					"	add := .add_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
					"	sub := .sub_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
					"	mul := .mul_ EMPTY\n" + // mul_ :: €EMPTY => (€Int, €Int) => €Int
					
					"	_1_1 := _1, _1\n" +
					"	_2   := .add _1_1\n" +
					"	_2_1 := _2, _1\n" +
					"	_3   := .add _2_1\n" +
					"	_2_2 := _2, _2\n" +
					"	_4   := .add _2_2\n" +
					"	_3_4 := _3, _4\n" +
					"	_12  := .mul _3_4\n" +
					"	§RETURN _12 IF TRUE\n" +
					
					"§DEF ...!!\n" +
					"	_1 := 1\n" +
					"	add_  := §1ST ENV\n" +
					"	rest1 := §2ND ENV\n" +
					"	sub_  := §1ST rest1\n" +
					"	rest2 := §2ND rest1\n" +
					"	mul_  := §1ST rest2\n" +
					"	rest3 := §2ND rest2\n" +
					"	eq_   := §1ST rest3\n" +
					
					"	add := .add_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
					"	sub := .sub_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
					"	mul := .mul_ EMPTY\n" + // mul_ :: €EMPTY => (€Int, €Int) => €Int
					"	eq  := .eq_ EMPTY\n" + // mul_ :: €EMPTY => (€Int, €Int) => €Bool
					
					"	_1_1 := _1, _1\n" +
					"	_0   := .sub _1_1\n" +
					
					"	arg    := §1ST ARG\n" +
					"	res    := §2ND ARG\n" +
					"	arg_0  := arg, _0\n" +
					"	areEq0 := .eq arg_0\n" +
					"	§RETURN res IF areEq0\n" +
					
					"	res_arg := res, arg\n" +
					"	newRes  := .mul res_arg\n" +
					"	arg_1   := arg, _1\n" +
					"	newArg  := .sub arg_1\n" +
					"	newArg_newRes := newArg, newRes\n" +
					"	§REPEAT newArg_newRes IF TRUE\n" +
					
					"§DEF ...!\n" +
					"	_1 := 1\n" +
					"	add_  := §1ST ENV\n" +
					"	rest1 := §2ND ENV\n" +
					"	sub_  := §1ST rest1\n" +
					"	rest2 := §2ND rest1\n" +
					"	mul_  := §1ST rest2\n" +
					"	rest3 := §2ND rest2\n" +
					"	eq_   := §1ST rest3\n" +
					"	rest4 := §2ND rest3\n" +
					"	...!!_ := §1ST rest4\n" +
					"	...!! := . ...!!_ ENV\n" +
					
					"	arg_1 := ARG, _1\n" +
					"	res   := . ...!! arg_1\n" +
					"	§RETURN res IF TRUE\n",
					aDebugStream
				);
				
				var Proc1 = Module.Skip(ModuleMap.Get("bla")).First();
				var Proc2 = Module.Skip(ModuleMap.Get("bla2")).First();
				var Proc3 = Module.Skip(ModuleMap.Get("...!!")).First();
				var Proc4 = Module.Skip(ModuleMap.Get("...!")).First();
				
				var Env = mVM_Data.Tuple(
					mVM_Data.ExternDef(Add),
					mVM_Data.ExternDef(Sub),
					mVM_Data.ExternDef(Mul),
					mVM_Data.ExternDef(Eq),
					mVM_Data.Def(Proc3)
				);
				#if TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				{
					var Res = mVM_Data.Empty();
					mVM.Run(
						mVM_Data.Proc(Proc1, Env),
						mVM_Data.Empty(),
						mVM_Data.Empty(),
						Res,
						TraceOut
					);
					mStd.AssertEq(Res, mVM_Data.Int(2));
				}
				{
					var Res = mVM_Data.Empty();
					mVM.Run(
						mVM_Data.Proc(Proc2, Env),
						mVM_Data.Empty(),
						mVM_Data.Empty(),
						Res,
						TraceOut
					);
					mStd.AssertEq(Res, mVM_Data.Int(12));
				}
				{
					var Res = mVM_Data.Empty();
					mVM.Run(
						mVM_Data.Proc(Proc3, Env),
						mVM_Data.Empty(),
						mVM_Data.Pair(mVM_Data.Int(3), mVM_Data.Int(1)),
						Res,
						TraceOut
					);
					mStd.AssertEq(Res, mVM_Data.Int(6));
				}
				{
					var Res = mVM_Data.Empty();
					mVM.Run(
						mVM_Data.Proc(Proc4, Env),
						mVM_Data.Empty(),
						mVM_Data.Int(3),
						Res,
						TraceOut
					);
					mStd.AssertEq(Res, mVM_Data.Int(6));
				}
			}
		)
	);
	
	[xArg("Call")]
	[xArg("Prefix")]
	[xArg("Assert")]
	[xArg("§TYPE_OF...IS...")]
	[xArg("ParseModule")]
	[xTest] public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
