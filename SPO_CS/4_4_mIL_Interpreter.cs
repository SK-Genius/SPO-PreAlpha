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

public static class mIL_Interpreter {
	
	//================================================================================
	private static mStd.tTuple<
		mList.tList<mIL_VM.tProcDef>,
		mMap.tMap<tText, tInt32>
	>
	ParseModule(
		tText aSourceCode,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var ParserResult = mIL_Parser.MODULE.ParseText(aSourceCode, aDebugStream);
		
		ParserResult.MATCH(out mList.tList<mStd.tTuple<tText, mList.tList<mIL_AST.tCommandNode>>> Defs);
		
		return ParseModule(Defs);
	}
	
	//================================================================================
	internal static mStd.tTuple<
		mList.tList<mIL_VM.tProcDef>,
		mMap.tMap<tText, tInt32>
	>
	ParseModule(
		mList.tList<mStd.tTuple<tText, mList.tList<mIL_AST.tCommandNode>>> Defs
	//================================================================================
	) {
		var ModuleMap = mMap.Map<tText, tInt32>((a1, a2) => a1.Equals(a2));
		var Module = mList.List<mIL_VM.tProcDef>();
		
		var RestDefs = Defs;
		while (RestDefs.MATCH(out var Def, out RestDefs)) {
			Def.MATCH(out var DefName, out var Commands);
			var NewProc = new mIL_VM.tProcDef();
			var NextIndex = Module.Reduce(0, (aSum, _) => aSum + 1);
			ModuleMap = ModuleMap.Set(DefName, NextIndex);
			Module = mList.Concat(Module, mList.List(NewProc));
			
			var Reg = mMap.Map<tText, tInt32>((a, b) => a.Equals(b))
				.Set("ENV"   , mIL_VM.tProcDef.ENV_Reg)
				.Set("OBJ"   , mIL_VM.tProcDef.OBJ_Reg)
				.Set("ARG"   , mIL_VM.tProcDef.ARG_Reg)
				.Set("RES"   , mIL_VM.tProcDef.RES_Reg)
				.Set("EMPTY" , mIL_VM.tProcDef.EMPTY_Reg)
				.Set("FALSE" , mIL_VM.tProcDef.FALSE_Reg)
				.Set("TRUE"  , mIL_VM.tProcDef.TRUE_Reg);
			
			var RestCommands = Commands;
			while (RestCommands.MATCH(out var Command, out RestCommands)) {
				if (Command.MATCH(mIL_AST.tCommandNodeType.Call, out var RegId1, out var RegId2, out var RegId3)) {
					Reg = Reg.Set(RegId1, NewProc.Call(Reg.Get(RegId2), Reg.Get(RegId3)));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.Alias, out RegId1, out RegId2)) {
					Reg = Reg.Set(RegId1, Reg.Get(RegId2));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.Int, out RegId1, out RegId2)) {
					Reg = Reg.Set(RegId1, NewProc.Int(int.Parse(RegId2)));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.Pair, out RegId1, out RegId2, out RegId3)) {
					Reg = Reg.Set(RegId1, NewProc.Pair(Reg.Get(RegId2), Reg.Get(RegId3)));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.First, out RegId1, out RegId2)) {
					Reg = Reg.Set(RegId1, NewProc.First(Reg.Get(RegId2)));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.Second, out RegId1, out RegId2)) {
					Reg = Reg.Set(RegId1, NewProc.Second(Reg.Get(RegId2)));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.ReturnIf, out RegId1, out RegId2)) {
					NewProc.ReturnIf(Reg.Get(RegId2), Reg.Get(RegId1 ?? "EMPTY"));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.RepeatIf, out RegId1, out RegId2)) {
					NewProc.ContinueIf(Reg.Get(RegId2), Reg.Get(RegId1));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.AddPrefix, out RegId1, out var Prefix, out RegId3)) {
					Reg = Reg.Set(RegId1,  NewProc.AddPrefix(Prefix.GetHashCode(), Reg.Get(RegId3)));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.SubPrefix, out RegId1, out Prefix, out RegId3)) {
					Reg = Reg.Set(RegId1, NewProc.DelPrefix(Prefix.GetHashCode(), Reg.Get(RegId3)));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.HasPrefix, out RegId1, out Prefix, out RegId3)) {
					Reg = Reg.Set(RegId1, NewProc.HasPrefix(Prefix.GetHashCode(), Reg.Get(RegId3)));
				} else if (Command.MATCH(mIL_AST.tCommandNodeType.Assert, out RegId1, out RegId2)) {
					NewProc.Assert(Reg.Get(RegId1), Reg.Get(RegId2));
				} else {
					mStd.Assert(false);
				}
			}
		}
		
		return mStd.Tuple(Module, ModuleMap);
	}
	
	#region TEST
	
	//================================================================================
	private static readonly mStd.tFunc<mIL_VM.tData, mIL_VM.tData, mIL_VM.tData, mIL_VM.tData, mStd.tAction<tText>>
	Add = (
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
		tInt32 Arg2_;
		mStd.Assert(Arg2.MATCH(mIL_VM.tDataType.INT, out Arg2_));
		return mIL_VM.INT(Arg1_ + Arg2_);
	};
	
	//================================================================================
	private static readonly mStd.tFunc<mIL_VM.tData, mIL_VM.tData, mIL_VM.tData, mIL_VM.tData, mStd.tAction<tText>>
	Sub = (
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
		tInt32 Arg2_;
		mStd.Assert(Arg2.MATCH(mIL_VM.tDataType.INT, out Arg2_));
		return mIL_VM.INT(Arg1_ - Arg2_);
	};
	
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
		tInt32 Arg2_;
		mStd.Assert(Arg2.MATCH(mIL_VM.tDataType.INT, out Arg2_));
		return mIL_VM.INT(Arg1_ * Arg2_);
	};
	
	//================================================================================
	private static readonly mStd.tFunc<mIL_VM.tData, mIL_VM.tData, mIL_VM.tData, mIL_VM.tData, mStd.tAction<tText>>
	Eq = (
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
		tInt32 Arg2_;
		mStd.Assert(Arg2.MATCH(mIL_VM.tDataType.INT, out Arg2_));
		return mIL_VM.BOOL(Arg1_.Equals(Arg2_));
	};
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"Call",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var X = ParseModule(
						"DEF ...++\n" +
						"	add := ENV EMPTY\n" +
						"	1_ := 1\n" +
						
						"	arg_1 := ARG, 1_\n" +
						"	res := add arg_1\n" +
						"	§RETURN res IF TRUE\n",
						aDebugStream
					);
					
					mList.tList<mIL_VM.tProcDef> Module;
					mMap.tMap<tText, tInt32> ModuleMap;
					X.MATCH(out Module, out ModuleMap);
					
					#if TRACE
						var TraceOut = aDebugStream;
					#else
						var TraceOut = mStd.Action<tText>(_ => {});
					#endif
					
					var Proc = Module.Skip(ModuleMap.Get("...++"))._Head;
					var Env = mIL_VM.EXTERN_DEF(Add);
					var Res = mIL_VM.EMPTY();
					mIL_VM.Run(mIL_VM.PROC(Proc, Env), mIL_VM.EMPTY(), mIL_VM.INT(5), Res, TraceOut);
					mStd.AssertEq(Res, mIL_VM.INT(6));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"Prefix",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var X = ParseModule(
						"DEF ...++\n" +
						"	add := ENV EMPTY\n" +
						"	1_ := 1\n" +
						
						"	arg := -VECTOR ARG\n" +
						"	arg_1 := arg, 1_\n" +
						"	inc := add arg_1\n" +
						"	res := +VECTOR inc\n" +
						"	§RETURN res IF TRUE\n",
						aDebugStream
					);
					
					mList.tList<mIL_VM.tProcDef> Module;
					mMap.tMap<tText, tInt32> ModuleMap;
					X.MATCH(out Module, out ModuleMap);
					
					#if TRACE
						var TraceOut = aDebugStream;
					#else
						var TraceOut = mStd.Action<tText>(_ => {});
					#endif
					
					var Proc = Module.Skip(ModuleMap.Get("...++"))._Head;
					var Env = mIL_VM.EXTERN_DEF(Add);
					var Res = mIL_VM.EMPTY();
					mIL_VM.Run(
						mIL_VM.PROC(Proc, Env),
						mIL_VM.EMPTY(),
						mIL_VM.PREFIX("VECTOR", mIL_VM.INT(12)),
						Res,
						TraceOut
					);
					mStd.AssertEq(Res, mIL_VM.PREFIX("VECTOR", mIL_VM.INT(13)));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"Assert",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var X = ParseModule(
						"DEF ...++\n" +
						"	...=...? := ENV EMPTY\n" +
						"	1_ := 1\n" + 
						"	arg_1 := ARG, 1_\n" +
						"	arg_eq_1? := ...=...? arg_1\n" +
						"	§ASSERT TRUE => arg_eq_1?\n" +
						"	§RETURN arg_eq_1? IF TRUE\n",
						aDebugStream
					);
					
					mList.tList<mIL_VM.tProcDef> Module;
					mMap.tMap<tText, tInt32> ModuleMap;
					X.MATCH(out Module, out ModuleMap);
					
					var Proc = Module.Skip(ModuleMap.Get("...++"))._Head;
					var Env = mIL_VM.EXTERN_DEF(Eq);
					var Res = mIL_VM.EMPTY();
					
					#if TRACE
						var TraceOut = aDebugStream;
					#else
						var TraceOut = mStd.Action<tText>(_ => {});
					#endif
					
					var CallStack = new mIL_VM.tCallStack(
						null,
						Proc,
						Env,
						mIL_VM.EMPTY(), 
						mIL_VM.INT(1),
						Res,
						TraceOut
					);
					while (CallStack != null) {
						CallStack = CallStack.Step();
					}
					mStd.AssertEq(Res, mIL_VM.BOOL(true));
					
					var HasThrowException = false;
					try {
						Res = mIL_VM.EMPTY();
						mIL_VM.Run(mIL_VM.PROC(Proc, Env), mIL_VM.EMPTY(), mIL_VM.INT(2), Res, TraceOut);
					} catch {
						HasThrowException = true;
					}
					
					mStd.Assert(HasThrowException);
					return true;
				}
			)
		),
		mStd.Tuple(
			"ParseModule",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var X = ParseModule(
						"DEF bla\n" +
						"	_1 := 1\n" +
						"	add_ := §1st ENV\n" +
						
						"	add := add_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
						
						"	p := _1, _1\n" +
						"	r := add p\n" +
						"	§RETURN r IF TRUE\n" +
						
						"DEF bla2\n" +
						"	_1 := 1\n" +
						"	add_  := §1st ENV\n" +
						"	rest1 := §2nd ENV\n" +
						"	sub_  := §1st rest1\n" +
						"	rest2 := §2nd rest1\n" +
						"	mul_  := §1st rest2\n" +
						
						"	add := add_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
						"	sub := sub_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
						"	mul := mul_ EMPTY\n" + // mul_ :: €EMPTY => (€Int, €Int) => €Int
						
						"	_1_1 := _1, _1\n" +
						"	_2   := add _1_1\n" +
						"	_2_1 := _2, _1\n" +
						"	_3   := add _2_1\n" +
						"	_2_2 := _2, _2\n" +
						"	_4   := add _2_2\n" +
						"	_3_4 := _3, _4\n" +
						"	_12  := mul _3_4\n" +
						"	§RETURN _12 IF TRUE\n" +
						
						"DEF ...!!\n" +
						"	_1 := 1\n" +
						"	add_  := §1st ENV\n" +
						"	rest1 := §2nd ENV\n" +
						"	sub_  := §1st rest1\n" +
						"	rest2 := §2nd rest1\n" +
						"	mul_  := §1st rest2\n" +
						"	rest3 := §2nd rest2\n" +
						"	eq_   := §1st rest3\n" +
						
						"	add := add_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
						"	sub := sub_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
						"	mul := mul_ EMPTY\n" + // mul_ :: €EMPTY => (€Int, €Int) => €Int
						"	eq  := eq_ EMPTY\n" + // mul_ :: €EMPTY => (€Int, €Int) => €Bool
						
						"	1_1 := _1, _1\n" +
						"	0   := sub 1_1\n" +
						
						"	arg    := §1st ARG\n" +
						"	res    := §2nd ARG\n" +
						"	arg_0  := arg, 0\n" +
						"	areEq0 := eq arg_0\n" +
						"	§RETURN res IF areEq0\n" +
						
						"	res_arg := res, arg\n" +
						"	newRes  := mul res_arg\n" +
						"	arg_1   := arg, _1\n" +
						"	newArg  := sub arg_1\n" +
						"	newArg_newRes := newArg, newRes\n" +
						"	§REPEAT newArg_newRes IF TRUE\n" +
						
						"DEF ...!\n" +
						"	_1 := 1\n" +
						"	add_  := §1st ENV\n" +
						"	rest1 := §2nd ENV\n" +
						"	sub_  := §1st rest1\n" +
						"	rest2 := §2nd rest1\n" +
						"	mul_  := §1st rest2\n" +
						"	rest3 := §2nd rest2\n" +
						"	eq_   := §1st rest3\n" +
						"	rest4 := §2nd rest3\n" +
						"	...!!_ := §1st rest4\n" +
						"	...!! := ...!!_ ENV\n" +
						
						"	arg_1 := ARG, _1\n" +
						"	res   := ...!! arg_1\n" +
						"	§RETURN res IF TRUE\n",
						aDebugStream
					);
					
					mList.tList<mIL_VM.tProcDef> Module;
					mMap.tMap<tText, tInt32> ModuleMap;
					X.MATCH(out Module, out ModuleMap);
					
					var Proc1 = Module.Skip(ModuleMap.Get("bla"))._Head;
					var Proc2 = Module.Skip(ModuleMap.Get("bla2"))._Head;
					var Proc3 = Module.Skip(ModuleMap.Get("...!!"))._Head;
					var Proc4 = Module.Skip(ModuleMap.Get("...!"))._Head;
					
					var Env = mIL_VM.PAIR(
						mIL_VM.EXTERN_DEF(Add),
						mIL_VM.PAIR(
							mIL_VM.EXTERN_DEF(Sub),
							mIL_VM.PAIR(
								mIL_VM.EXTERN_DEF(Mul),
								mIL_VM.PAIR(
									mIL_VM.EXTERN_DEF(Eq),
									mIL_VM.PAIR(
										mIL_VM.DEF(Proc3),
										mIL_VM.EMPTY()
									)
								)
							)
						)
					);
					#if TRACE
						var TraceOut = aDebugStream;
					#else
						var TraceOut = mStd.Action<tText>(_ => {});
					#endif
					{
						var Res = mIL_VM.EMPTY();
						mIL_VM.Run(mIL_VM.PROC(Proc1, Env), mIL_VM.EMPTY(), mIL_VM.EMPTY(), Res, TraceOut);
						mStd.AssertEq(Res, mIL_VM.INT(2));
					}
					{
						var Res = mIL_VM.EMPTY();
						mIL_VM.Run(mIL_VM.PROC(Proc2, Env), mIL_VM.EMPTY(), mIL_VM.EMPTY(), Res, TraceOut);
						mStd.AssertEq(Res, mIL_VM.INT(12));
					}
					{
						var Res = mIL_VM.EMPTY();
						mIL_VM.Run(
							mIL_VM.PROC(Proc3, Env),
							mIL_VM.EMPTY(),
							mIL_VM.PAIR(mIL_VM.INT(3), mIL_VM.INT(1)),
							Res,
							TraceOut
						);
						mStd.AssertEq(Res, mIL_VM.INT(6));
					}
					{
						var Res = mIL_VM.EMPTY();
						mIL_VM.Run(mIL_VM.PROC(Proc4, Env), mIL_VM.EMPTY(), mIL_VM.INT(3), Res, TraceOut);
						mStd.AssertEq(Res, mIL_VM.INT(6));
					}
					return true;
				}
			)
		)
	);
	
	#endregion
}