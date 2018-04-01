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
	public static (
		mList.tList<mVM_Data.tProcDef>,
		mMap.tMap<tText, tInt32>
	)
	ParseModule(
		tText aSourceCode,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var ParserResult = mIL_Parser.Module.ParseText(aSourceCode, aDebugStream);
		
		mDebug.Assert(
			ParserResult.Match(
				out mList.tList<(tText, mList.tList<mIL_AST.tCommandNode>)> Defs
			)
		);
		
		return ParseModule(Defs, aDebugStream);
	}
	
	//================================================================================
	public static (
		mList.tList<mVM_Data.tProcDef>,
		mMap.tMap<tText, tInt32>
	)
	ParseModule(
		mList.tList<(tText, mList.tList<mIL_AST.tCommandNode>)> aDefs,
		mStd.tAction<tText> aTrace
	//================================================================================
	) {
		#if TRACE
			aTrace(nameof(ParseModule));
		#endif
		var ModuleMap = mMap.Map<tText, tInt32>((a1, a2) => a1.Equals(a2));
		var Module = mList.List<mVM_Data.tProcDef>();
		
		var RestDefs = aDefs;
		while (RestDefs.Match(out var Def, out RestDefs)) {
			var (DefName, Commands) = Def;
			#if TRACE
				aTrace($"    {DefName}:");
			#endif
			var NewProc = new mVM_Data.tProcDef();
			// TODO: set type if it known
			var NextIndex = Module.Reduce(0, (aSum, _) => aSum + 1);
			ModuleMap = ModuleMap.Set(DefName, NextIndex);
			
			var DefEnvType = mVM_Type.Unknown();
			var DefObjType = mVM_Type.Unknown();
			var DefArgType = mVM_Type.Unknown();
			var DefResType = mVM_Type.Unknown();
			mVM_Type.Unify(
				NewProc.DefType,
				mVM_Type.Proc(
					mVM_Type.Empty(),
					DefEnvType,
					mVM_Type.Proc(
						DefObjType,
						DefArgType,
						DefResType
					)
				),
				aTrace
			);
			Module = mList.Concat(Module, mList.List(NewProc));
			
			var Regs = mMap.Map<tText, tInt32>((a, b) => a == b)
			.Set(mIL_AST.cEmpty, mVM_Data.tProcDef.cEmptyReg)
			.Set(mIL_AST.cOne, mVM_Data.tProcDef.cOneReg)
			.Set(mIL_AST.cFalse, mVM_Data.tProcDef.cFalseReg)
			.Set(mIL_AST.cTrue, mVM_Data.tProcDef.cTrueReg)
			.Set(mIL_AST.cEmptyType, mVM_Data.tProcDef.cEmptyTypeReg)
			.Set(mIL_AST.cBoolType, mVM_Data.tProcDef.cBoolTypeReg)
			.Set(mIL_AST.cIntType, mVM_Data.tProcDef.cIntTypeReg)
			.Set(mIL_AST.cEnv, mVM_Data.tProcDef.cEnvReg)
			.Set(mIL_AST.cObj, mVM_Data.tProcDef.cObjReg)
			.Set(mIL_AST.cArg, mVM_Data.tProcDef.cArgReg)
			.Set(mIL_AST.cRes, mVM_Data.tProcDef.cResReg);
			
			var Types = NewProc.Types;
			Types
			.Push(mVM_Type.Empty())
			.Push(mVM_Type.Int())
			.Push(mVM_Type.Bool())
			.Push(mVM_Type.Bool())
			.Push(mVM_Type.Type(mVM_Type.Empty()))
			.Push(mVM_Type.Type(mVM_Type.Bool()))
			.Push(mVM_Type.Type(mVM_Type.Int()))
			.Push(mVM_Type.Type(mVM_Type.Type()))
			.Push(DefEnvType)
			.Push(DefObjType)
			.Push(DefArgType)
			.Push(DefResType);
			
			var ObjStack = mArrayList.List<tInt32>();
			var CurrObj = Regs.Get(mIL_AST.cEmpty);
			var CurrObjType = Types.Get(CurrObj);
			
			var RestCommands = Commands;
			while (RestCommands.Match(out var Command, out RestCommands)) {
				#if TRACE
					aTrace($"  {Command._NodeType} {Command._1} {Command._2} {Command._3}:");
				#endif
				if (Command.Match(mIL_AST.tCommandNodeType.Call, out var RegId1, out var RegId2, out var RegId3)) {
					var ResType = mVM_Type.Unknown();
					var ProcReg = Regs.Get(RegId2);
					var ArgReg = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(ProcReg), mVM_Type.Proc(mVM_Type.Empty(), Types.Get(ArgReg), ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Call(ProcReg, ArgReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Exec, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Unknown();
					var ProcReg = Regs.Get(RegId2);
					var ArgReg = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(ProcReg), mVM_Type.Proc(CurrObjType, Types.Get(ArgReg), ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Exec(ProcReg, ArgReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Alias, out RegId1, out RegId2)) {
					Regs = Regs.Set(RegId1, Regs.Get(RegId2));
				} else if (Command.Match(mIL_AST.tCommandNodeType.Int, out RegId1, out RegId2)) {
					var ResType = mVM_Type.Int();
					Regs = Regs.Set(RegId1, NewProc.Int(int.Parse(RegId2)));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.BoolAnd, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.And(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.BoolOr, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Or(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.BoolXOr, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.XOr(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsAreEq, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsAreEq(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsComp, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsComp(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsAdd, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsAdd(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsSub, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsSub(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsMul, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsMul(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Pair, out RegId1, out RegId2, out RegId3)) {
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					var ResType = mVM_Type.Pair(Types.Get(Reg1), Types.Get(Reg2));
					Regs = Regs.Set(RegId1, NewProc.Pair(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.First, out RegId1, out RegId2)) {
					var ResType = mVM_Type.Unknown();
					var ArgReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(ArgReg), mVM_Type.Pair(ResType, mVM_Type.Unknown()), aTrace);
					Regs = Regs.Set(RegId1, NewProc.First(ArgReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Second, out RegId1, out RegId2)) {
					var ResType = mVM_Type.Unknown();
					var ArgReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(ArgReg), mVM_Type.Pair(mVM_Type.Unknown(), ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Second(ArgReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.ReturnIf, out RegId1, out RegId2)) {
					var ResReg = Regs.Get(RegId1);
					var CondReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(ResReg), DefResType, aTrace);
					NewProc.ReturnIf(CondReg, ResReg);
				} else if (Command.Match(mIL_AST.tCommandNodeType.RepeatIf, out RegId1, out RegId2)) {
					var ArgReg = Regs.Get(RegId1);
					var CondReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(ArgReg), DefArgType, aTrace);
					NewProc.ContinueIf(CondReg, ArgReg);
				} else if (Command.Match(mIL_AST.tCommandNodeType.AddPrefix, out RegId1, out var Prefix, out RegId3)) {
					var Reg = Regs.Get(RegId3);
					var ResType = mVM_Type.Prefix(Prefix, Types.Get(Reg));
					Regs = Regs.Set(RegId1, NewProc.AddPrefix(Prefix.GetHashCode(), Reg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.SubPrefix, out RegId1, out Prefix, out RegId3)) {
					var Reg = Regs.Get(RegId3);
					var ResType = mVM_Type.Unknown();
					mVM_Type.Unify(Types.Get(Reg), mVM_Type.Prefix(Prefix, ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.DelPrefix(Prefix.GetHashCode(), Reg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.HasPrefix, out RegId1, out Prefix, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg), mVM_Type.Prefix(mVM_Type.cUnknownPrefix, mVM_Type.Unknown()), aTrace);
					Regs = Regs.Set(RegId1, NewProc.HasPrefix(Prefix.GetHashCode(), Reg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Assert, out RegId1, out RegId2)) {
					var Reg1 = Regs.Get(RegId1);
					var Reg2 = Regs.Get(RegId2);
					// TODO
					NewProc.Assert(Reg1, Reg2);
				} else if (Command.Match(mIL_AST.tCommandNodeType.VarDef, out RegId1, out RegId2)) {
					var Reg = Regs.Get(RegId2);
					var ResType = mVM_Type.Var(Types.Get(Reg));
					Regs = Regs.Set(RegId1, NewProc.VarDef(Reg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.VarSet, out RegId1, out RegId2)) {
					var VarReg = Regs.Get(RegId1);
					var ValReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(VarReg), mVM_Type.Var(Types.Get(ValReg)), aTrace);
					NewProc.VarSet(VarReg, ValReg);
				} else if (Command.Match(mIL_AST.tCommandNodeType.VarGet, out RegId1, out RegId2)) {
					var ResType = mVM_Type.Unknown();
					var VarReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(VarReg), mVM_Type.Var(ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.VarGet(VarReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Push, out RegId1)) {
					ObjStack.Push(CurrObj);
					CurrObj = Regs.Get(RegId1);
					NewProc.SetObj(CurrObj);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Pop)) {
					CurrObj = ObjStack.Pop();
					NewProc.SetObj(CurrObj);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeCond, out RegId1, out RegId2, out RegId3)) {
					throw mStd.Error("TODO");
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeFunc, out RegId1, out RegId2, out RegId3)) {
					var ArgTypeReg = Regs.Get(RegId2);
					var ResTypeReg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeFunc(ArgTypeReg, ResTypeReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Proc(
								mVM_Type.Empty(),
								Types.Get(ArgTypeReg).Value(),
								Types.Get(ResTypeReg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeMethod, out RegId1, out RegId2, out RegId3)) {
					var ObjTypeReg = Regs.Get(RegId2);
					var FuncTypeReg = Regs.Get(RegId3);
					var ArgType = mVM_Type.Unknown();
					var ResType = mVM_Type.Unknown();
					Regs = Regs.Set(RegId1, NewProc.TypeMeth(ObjTypeReg, FuncTypeReg));
					mVM_Type.Unify(
						Types.Get(FuncTypeReg),
						mVM_Type.Proc(mVM_Type.Empty(), ArgType, ResType),
						aTrace
					);
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Proc(
								Types.Get(ObjTypeReg).Value(),
								ArgType,
								ResType
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypePair, out RegId1, out RegId2, out RegId3)) {
					var Type1Reg = Regs.Get(RegId2);
					var Type2Reg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypePair(Type1Reg, Type2Reg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Pair(
								Types.Get(Type1Reg).Value(),
								Types.Get(Type2Reg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypePrefix, out RegId1, out RegId2, out RegId3)) {
					throw mStd.Error("TODO");
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeSet, out RegId1, out RegId2, out RegId3)) {
					var Type1Reg = Regs.Get(RegId2);
					var Type2Reg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypePair(Type1Reg, Type2Reg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Set(
								Types.Get(Type1Reg).Value(),
								Types.Get(Type2Reg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeVar, out RegId1, out RegId2)) {
					var TypeReg = Regs.Get(RegId2);
					Regs = Regs.Set(RegId1, NewProc.TypeVar(TypeReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Var(
								Types.Get(TypeReg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeIs, out RegId1, out RegId2)) {
					var VarReg = Regs.Get(RegId1);
					var TypeReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(VarReg), Types.Get(TypeReg).Value(), aTrace);
				} else {
					throw mStd.Error($"impossible  (missing: {Command.NodeType})");
				}
			}
		}
		
		#if !true
		{
			var Rest = ModuleMap._KeyValuePairs;
			var Module_ = Module.ToArrayList();
			while (Rest.Match(out var KeyValue, out Rest)) {
				var (Name, Index) = KeyValue;
				aTrace($@"{Name} @ {Module_.Get(Index)._DefType}");
			}
		}
		#endif
		
		return (Module, ModuleMap);
	}
	
	//================================================================================
	public static mVM_Data.tData
	Run(
		tText aSourceCode,
		mVM_Data.tData aImport,
		mStd.tAction<tText> aTrace
	//================================================================================
	) => Run(ParseModule(aSourceCode, aTrace), aImport, aTrace);
	
	//================================================================================
	public static mVM_Data.tData
	Run(
		mList.tList<(tText, mList.tList<mIL_AST.tCommandNode>)> aDefs,
		mVM_Data.tData aImport,
		mStd.tAction<tText> aTrace
	//================================================================================
	) => Run(ParseModule(aDefs, aTrace), aImport, aTrace);
	
	//================================================================================
	public static mVM_Data.tData
	Run(
		(mList.tList<mVM_Data.tProcDef>, mMap.tMap<tText, tInt32>) aModule,
		mVM_Data.tData aImport,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var (VMModule, ModuleMap) = aModule;
		var Res = mVM_Data.Empty();
		var Defs = VMModule.Skip(1).Reverse();
		
		var DefTuple = mVM_Data.Empty();
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
	Eq (
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
				var (Module, ModuleMap) = ParseModule(
					"DEF ...++\n" +
					"	1_ := 1\n" +
					"	res := §INT ARG + 1_\n" +
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
				var (Module, ModuleMap) = ParseModule(
					"DEF ...++\n" +
					"	add := .ENV EMPTY\n" +
					"	1_ := 1\n" +
					
					"	arg := -VECTOR ARG\n" +
					"	arg_1 := arg, 1_\n" +
					"	inc := .add arg_1\n" +
					"	res := +VECTOR inc\n" +
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
				var (Module, ModuleMap) = ParseModule(
					"DEF AssertInt...\n" +
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
				var (Module, ModuleMap) = ParseModule(
					"DEF ...++\n" +
					"	...=...? := . ENV EMPTY\n" +
					"	1_ := 1\n" + 
					"	arg_1 := ARG, 1_\n" +
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
				var (Module, ModuleMap) = ParseModule(
					"DEF bla\n" +
					"	_1 := 1\n" +
					"	add_ := §1ST ENV\n" +
					
					"	add := .add_ EMPTY\n" + // add_ :: €EMPTY => (€Int, €Int) => €Int
					
					"	p := _1, _1\n" +
					"	r := .add p\n" +
					"	§RETURN r IF TRUE\n" +
					
					"DEF bla2\n" +
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
					
					"DEF ...!!\n" +
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
					
					"	1_1 := _1, _1\n" +
					"	0   := .sub 1_1\n" +
					
					"	arg    := §1ST ARG\n" +
					"	res    := §2ND ARG\n" +
					"	arg_0  := arg, 0\n" +
					"	areEq0 := .eq arg_0\n" +
					"	§RETURN res IF areEq0\n" +
					
					"	res_arg := res, arg\n" +
					"	newRes  := .mul res_arg\n" +
					"	arg_1   := arg, _1\n" +
					"	newArg  := .sub arg_1\n" +
					"	newArg_newRes := newArg, newRes\n" +
					"	§REPEAT newArg_newRes IF TRUE\n" +
					
					"DEF ...!\n" +
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
	
	#endregion
}