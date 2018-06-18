//#define TRACE

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
	) => ParseModule(mIL_Parser.Module.ParseText(aSourceCode, aDebugStream), aDebugStream);
	
	//================================================================================
	public static (
		mList.tList<mVM_Data.tProcDef>,
		mMap.tMap<tText, tInt32>
	)
	ParseModule<tPos>(
		mList.tList<(tText, mList.tList<mIL_AST.tCommandNode<tPos>>)> aDefs,
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
			
			var DefEnvType = mVM_Type.Free();
			var DefObjType = mVM_Type.Free();
			var DefArgType = mVM_Type.Free();
			var DefResType = mVM_Type.Free();
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
					aTrace($"  {Command.NodeType} {Command._1} {Command._2} {Command._3}:");
				#endif
				if (Command.Match(mIL_AST.tCommandNodeType.Call, out var Pos, out var RegId1, out var RegId2, out var RegId3)) {
					var ResType = mVM_Type.Free();
					var ProcReg = Regs.Get(RegId2);
					var ArgReg = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(ProcReg), mVM_Type.Proc(mVM_Type.Empty(), Types.Get(ArgReg), ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Call(ProcReg, ArgReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Exec, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Free();
					var ProcReg = Regs.Get(RegId2);
					var ArgReg = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(ProcReg), mVM_Type.Proc(CurrObjType, Types.Get(ArgReg), ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Exec(ProcReg, ArgReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Alias, out Pos, out RegId1, out RegId2)) {
					Regs = Regs.Set(RegId1, Regs.Get(RegId2));
				} else if (Command.Match(mIL_AST.tCommandNodeType.Int, out Pos, out RegId1, out RegId2)) {
					var ResType = mVM_Type.Int();
					Regs = Regs.Set(RegId1, NewProc.Int(int.Parse(RegId2)));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.BoolAnd, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.And(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.BoolOr, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Or(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.BoolXOr, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.XOr(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsAreEq, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsAreEq(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsComp, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsComp(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsAdd, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsAdd(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsSub, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsSub(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsMul, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsMul(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Pair, out Pos, out RegId1, out RegId2, out RegId3)) {
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					var ResType = mVM_Type.Pair(Types.Get(Reg1), Types.Get(Reg2));
					Regs = Regs.Set(RegId1, NewProc.Pair(Reg1, Reg2));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.First, out Pos, out RegId1, out RegId2)) {
					var ResType = mVM_Type.Free();
					var ArgReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(ArgReg), mVM_Type.Pair(ResType, mVM_Type.Free()), aTrace);
					Regs = Regs.Set(RegId1, NewProc.First(ArgReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Second, out Pos, out RegId1, out RegId2)) {
					var ResType = mVM_Type.Free();
					var ArgReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(ArgReg), mVM_Type.Pair(mVM_Type.Free(), ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Second(ArgReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.ReturnIf, out Pos, out RegId1, out RegId2)) {
					var ResReg = Regs.Get(RegId1);
					var CondReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(ResReg), DefResType, aTrace);
					NewProc.ReturnIf(CondReg, ResReg);
				} else if (Command.Match(mIL_AST.tCommandNodeType.RepeatIf, out Pos, out RegId1, out RegId2)) {
					var ArgReg = Regs.Get(RegId1);
					var CondReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(ArgReg), DefArgType, aTrace);
					NewProc.ContinueIf(CondReg, ArgReg);
				} else if (Command.Match(mIL_AST.tCommandNodeType.AddPrefix, out Pos, out RegId1, out var Prefix, out RegId3)) {
					var Reg = Regs.Get(RegId3);
					var ResType = mVM_Type.Prefix(Prefix, Types.Get(Reg));
					Regs = Regs.Set(RegId1, NewProc.AddPrefix(Prefix.GetHashCode(), Reg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.SubPrefix, out Pos, out RegId1, out Prefix, out RegId3)) {
					var Reg = Regs.Get(RegId3);
					var ResType = mVM_Type.Free();
					mVM_Type.Unify(Types.Get(Reg), mVM_Type.Prefix(Prefix, ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.DelPrefix(Prefix.GetHashCode(), Reg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.HasPrefix, out Pos, out RegId1, out Prefix, out RegId3)) {
					var ResType = mVM_Type.Bool();
					var Reg = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg), mVM_Type.Prefix(mVM_Type.cUnknownPrefix, mVM_Type.Free()), aTrace);
					Regs = Regs.Set(RegId1, NewProc.HasPrefix(Prefix.GetHashCode(), Reg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Assert, out Pos, out RegId1, out RegId2)) {
					var Reg1 = Regs.Get(RegId1);
					var Reg2 = Regs.Get(RegId2);
					// TODO
					NewProc.Assert(Reg1, Reg2);
				} else if (Command.Match(mIL_AST.tCommandNodeType.VarDef, out Pos, out RegId1, out RegId2)) {
					var Reg = Regs.Get(RegId2);
					var ResType = mVM_Type.Var(Types.Get(Reg));
					Regs = Regs.Set(RegId1, NewProc.VarDef(Reg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.VarSet, out Pos, out RegId1, out RegId2)) {
					var VarReg = Regs.Get(RegId1);
					var ValReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(VarReg), mVM_Type.Var(Types.Get(ValReg)), aTrace);
					NewProc.VarSet(VarReg, ValReg);
				} else if (Command.Match(mIL_AST.tCommandNodeType.VarGet, out Pos, out RegId1, out RegId2)) {
					var ResType = mVM_Type.Free();
					var VarReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(VarReg), mVM_Type.Var(ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.VarGet(VarReg));
					Types.Push(ResType);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Push, out Pos, out RegId1)) {
					ObjStack.Push(CurrObj);
					CurrObj = Regs.Get(RegId1);
					NewProc.SetObj(CurrObj);
				} else if (Command.Match(mIL_AST.tCommandNodeType.Pop, out Pos)) {
					CurrObj = ObjStack.Pop();
					NewProc.SetObj(CurrObj);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeCond, out Pos, out RegId1, out RegId2, out RegId3)) {
					throw mStd.Error("TODO"); // TODO
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeFunc, out Pos, out RegId1, out RegId2, out RegId3)) {
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
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeMethod, out Pos, out RegId1, out RegId2, out RegId3)) {
					var ObjTypeReg = Regs.Get(RegId2);
					var FuncTypeReg = Regs.Get(RegId3);
					var ArgType = mVM_Type.Free();
					var ResType = mVM_Type.Free();
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
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypePair, out Pos, out RegId1, out RegId2, out RegId3)) {
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
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypePrefix, out Pos, out RegId1, out Prefix, out RegId2)) {
					var TypeReg = Regs.Get(RegId2);
					Regs = Regs.Set(RegId1, NewProc.TypePrefix(Prefix.GetHashCode(), TypeReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Prefix(
								Prefix,
								Types.Get(TypeReg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeSet, out Pos, out RegId1, out RegId2, out RegId3)) {
					var Type1Reg = Regs.Get(RegId2);
					var Type2Reg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeSet(Type1Reg, Type2Reg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Set(
								Types.Get(Type1Reg).Value(),
								Types.Get(Type2Reg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeVar, out Pos, out RegId1, out RegId2)) {
					var TypeReg = Regs.Get(RegId2);
					Regs = Regs.Set(RegId1, NewProc.TypeVar(TypeReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Var(
								Types.Get(TypeReg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeRecursive, out Pos, out RegId1, out RegId2, out RegId3)) {
					var FreeTypeReg = Regs.Get(RegId2);
					var TypeBodyReg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeRecursive(FreeTypeReg, TypeBodyReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Recursive(
								Types.Get(TypeBodyReg).Value(),
								Types.Get(FreeTypeReg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeInterface, out Pos, out RegId1, out RegId2, out RegId3)) {
					var FreeTypeReg = Regs.Get(RegId2);
					var TypeBodyReg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeInterface(FreeTypeReg, TypeBodyReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Interface(
								Types.Get(TypeBodyReg).Value(),
								Types.Get(FreeTypeReg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeGeneric, out Pos, out RegId1, out RegId2, out RegId3)) {
					var FreeTypeReg = Regs.Get(RegId2);
					var TypeBodyReg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeGeneric(FreeTypeReg, TypeBodyReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Generic(
								Types.Get(TypeBodyReg).Value(),
								Types.Get(FreeTypeReg).Value()
							)
						)
					);
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeIs, out Pos, out RegId1, out RegId2)) {
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
	Run<tPos>(
		mList.tList<(tText, mList.tList<mIL_AST.tCommandNode<tPos>>)> aDefs,
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
}
