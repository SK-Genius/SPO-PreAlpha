﻿
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

using tPos = mTextParser.tPos;
using tSpan = mStd.tSpan<mTextParser.tPos>;

public static class mIL_Interpreter {
	
	//================================================================================
	public static (
		mList.tList<mVM_Data.tProcDef<tSpan>>,
		mMap.tMap<tText, tInt32>
	)
	ParseModule(
		tText aSourceCode,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) => ParseModule(mIL_Parser.Module.ParseText(aSourceCode, aDebugStream), aDebugStream);
	
	//================================================================================
	public static (
		mList.tList<mVM_Data.tProcDef<tSpan>>,
		mMap.tMap<tText, tInt32>
	)
	ParseModule(
		mList.tList<(tText, mList.tList<mIL_AST.tCommandNode<tSpan>>)> aDefs,
		mStd.tAction<tText> aTrace
	//================================================================================
	) {
		#if TRACE
			aTrace(nameof(ParseModule));
		#endif
		var ModuleMap = mMap.Map<tText, tInt32>((a1, a2) => tText.CompareOrdinal(a1, a2) == 0);
		var Module = mList.List<mVM_Data.tProcDef<tSpan>>();
		
		var RestDefs = aDefs;
		while (RestDefs.Match(out var Def, out RestDefs)) {
			var (DefName, Commands) = Def;
			#if TRACE
				aTrace($"{DefName}:");
			#endif
			var NewProc = new mVM_Data.tProcDef<tSpan>();
			// TODO: set type if it known
			var NextIndex = Module.Reduce(0, (aSum, _) => aSum + 1);
			ModuleMap = ModuleMap.Set(DefName, NextIndex);
			
			var DefEnvType = mVM_Type.Free("§ENV");
			var DefObjType = mVM_Type.Free("§OBJ");
			var DefArgType = mVM_Type.Free("§ARG");
			var DefResType = mVM_Type.Free("§RES");
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
			.Set(mIL_AST.cEmpty, mVM_Data.tProcDef<tSpan>.cEmptyReg)
			.Set(mIL_AST.cOne, mVM_Data.tProcDef<tSpan>.cOneReg)
			.Set(mIL_AST.cFalse, mVM_Data.tProcDef<tSpan>.cFalseReg)
			.Set(mIL_AST.cTrue, mVM_Data.tProcDef<tSpan>.cTrueReg)
			.Set(mIL_AST.cEmptyType, mVM_Data.tProcDef<tSpan>.cEmptyTypeReg)
			.Set(mIL_AST.cBoolType, mVM_Data.tProcDef<tSpan>.cBoolTypeReg)
			.Set(mIL_AST.cIntType, mVM_Data.tProcDef<tSpan>.cIntTypeReg)
			.Set(mIL_AST.cEnv, mVM_Data.tProcDef<tSpan>.cEnvReg)
			.Set(mIL_AST.cObj, mVM_Data.tProcDef<tSpan>.cObjReg)
			.Set(mIL_AST.cArg, mVM_Data.tProcDef<tSpan>.cArgReg)
			.Set(mIL_AST.cRes, mVM_Data.tProcDef<tSpan>.cResReg);
			
			var Types = NewProc.Types
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
					if (Command.NodeType >= mIL_AST.tCommandNodeType._Commands_) {
						aTrace($"  {Command.NodeType} {Command._1} {Command._2} {Command._3}:");
					} else {
						aTrace($"  {Command._1} := {Command.NodeType} {Command._2} {Command._3}:");
					}
				#endif
				//--------------------------------------------------------------------------------
				if (Command.Match(mIL_AST.tCommandNodeType.Call, out var Span, out var RegId1, out var RegId2, out var RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Free();
					var ProcReg = Regs.Get(RegId2);
					var ArgReg = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(ProcReg), mVM_Type.Proc(mVM_Type.Empty(), Types.Get(ArgReg), ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Call<tSpan>(Span, ProcReg, ArgReg));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.Exec, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Free();
					var ProcReg = Regs.Get(RegId2);
					var ArgReg = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(ProcReg), mVM_Type.Proc(CurrObjType, Types.Get(ArgReg), ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Exec(Span, ProcReg, ArgReg));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.Alias, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					Regs = Regs.Set(RegId1, Regs.Get(RegId2));
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.Int, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Int();
					Regs = Regs.Set(RegId1, NewProc.Int(Span, int.Parse(RegId2)));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.BoolAnd, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.And(Span, Reg1, Reg2));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.BoolOr, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Or(Span, Reg1, Reg2));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.BoolXOr, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.XOr(Span, Reg1, Reg2));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsAreEq, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Bool();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsAreEq(Span, Reg1, Reg2));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsComp, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsComp(Span, Reg1, Reg2));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsAdd, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsAdd(Span, Reg1, Reg2));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsSub, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsSub(Span, Reg1, Reg2));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.IntsMul, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Int();
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace);
					mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace);
					Regs = Regs.Set(RegId1, NewProc.IntsMul(Span, Reg1, Reg2));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.Pair, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var Reg1 = Regs.Get(RegId2);
					var Reg2 = Regs.Get(RegId3);
					var ResType = mVM_Type.Pair(Types.Get(Reg1), Types.Get(Reg2));
					Regs = Regs.Set(RegId1, NewProc.Pair(Span, Reg1, Reg2));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.First, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Free();
					var ArgReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(ArgReg), mVM_Type.Pair(ResType, mVM_Type.Free()), aTrace);
					Regs = Regs.Set(RegId1, NewProc.First(Span, ArgReg));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.Second, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Free();
					var ArgReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(ArgReg), mVM_Type.Pair(mVM_Type.Free(), ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.Second(Span, ArgReg));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.ReturnIf, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var ResReg = Regs.Get(RegId1);
					var CondReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(ResReg), DefResType, aTrace);
					NewProc.ReturnIf(Span, CondReg, ResReg);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.RepeatIf, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var ArgReg = Regs.Get(RegId1);
					var CondReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace);
					mVM_Type.Unify(Types.Get(ArgReg), DefArgType, aTrace);
					NewProc.ContinueIf(Span, CondReg, ArgReg);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TailCallIf, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var ResReg = Regs.Get(RegId1);
					var CondReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace);
					var ArgType = mVM_Type.Free();
					mVM_Type.Unify(
						Types.Get(ResReg),
						mVM_Type.Pair(
							mVM_Type.Proc(mVM_Type.Empty(), ArgType, DefResType),
							ArgType
						),
						aTrace
					);
					NewProc.TailCallIf(Span, CondReg, ResReg);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.AddPrefix, out Span, out RegId1, out var Prefix, out RegId3)) {
				//--------------------------------------------------------------------------------
					var Reg = Regs.Get(RegId3);
					var ResType = mVM_Type.Prefix(Prefix, Types.Get(Reg));
					Regs = Regs.Set(RegId1, NewProc.AddPrefix(Span, Prefix.GetHashCode(), Reg));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.SubPrefix, out Span, out RegId1, out Prefix, out RegId3)) {
				//--------------------------------------------------------------------------------
					var Reg = Regs.Get(RegId3);
					var ResType = mVM_Type.Free();
					mVM_Type.Unify(Types.Get(Reg), mVM_Type.Prefix(Prefix, ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.DelPrefix(Span, Prefix.GetHashCode(), Reg));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.HasPrefix, out Span, out RegId1, out Prefix, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Bool();
					var Reg = Regs.Get(RegId3);
					mVM_Type.Unify(Types.Get(Reg), mVM_Type.Prefix(mVM_Type.cUnknownPrefix, mVM_Type.Free()), aTrace);
					Regs = Regs.Set(RegId1, NewProc.HasPrefix(Span, Prefix.GetHashCode(), Reg));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.Assert, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var Reg1 = Regs.Get(RegId1);
					var Reg2 = Regs.Get(RegId2);
					// TODO
					NewProc.Assert(Span, Reg1, Reg2);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.VarDef, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var Reg = Regs.Get(RegId2);
					var ResType = mVM_Type.Var(Types.Get(Reg));
					Regs = Regs.Set(RegId1, NewProc.VarDef(Span, Reg));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.VarSet, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var VarReg = Regs.Get(RegId1);
					var ValReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(VarReg), mVM_Type.Var(Types.Get(ValReg)), aTrace);
					NewProc.VarSet(Span, VarReg, ValReg);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.VarGet, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var ResType = mVM_Type.Free();
					var VarReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(VarReg), mVM_Type.Var(ResType), aTrace);
					Regs = Regs.Set(RegId1, NewProc.VarGet(Span, VarReg));
					Types.Push(ResType);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.Push, out Span, out RegId1)) {
				//--------------------------------------------------------------------------------
					ObjStack.Push(CurrObj);
					CurrObj = Regs.Get(RegId1);
					NewProc.SetObj(Span, CurrObj);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.Pop, out Span)) {
				//--------------------------------------------------------------------------------
					CurrObj = ObjStack.Pop();
					NewProc.SetObj(Span, CurrObj);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeCond, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					throw mStd.Error("TODO"); // TODO
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeFunc, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ArgTypeReg = Regs.Get(RegId2);
					var ResTypeReg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeFunc(Span, ArgTypeReg, ResTypeReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Proc(
								mVM_Type.Empty(),
								Types.Get(ArgTypeReg).Value(),
								Types.Get(ResTypeReg).Value()
							)
						)
					);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeMethod, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var ObjTypeReg = Regs.Get(RegId2);
					var FuncTypeReg = Regs.Get(RegId3);
					var ArgType = mVM_Type.Free();
					var ResType = mVM_Type.Free();
					Regs = Regs.Set(RegId1, NewProc.TypeMeth(Span, ObjTypeReg, FuncTypeReg));
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
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypePair, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var Type1Reg = Regs.Get(RegId2);
					var Type2Reg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypePair(Span, Type1Reg, Type2Reg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Pair(
								Types.Get(Type1Reg).Value(),
								Types.Get(Type2Reg).Value()
							)
						)
					);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypePrefix, out Span, out RegId1, out Prefix, out RegId2)) {
				//--------------------------------------------------------------------------------
					var TypeReg = Regs.Get(RegId2);
					Regs = Regs.Set(RegId1, NewProc.TypePrefix(Span, Prefix.GetHashCode(), TypeReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Prefix(
								Prefix,
								Types.Get(TypeReg).Value()
							)
						)
					);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeSet, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var Type1Reg = Regs.Get(RegId2);
					var Type2Reg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeSet(Span, Type1Reg, Type2Reg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Set(
								Types.Get(Type1Reg).Value(),
								Types.Get(Type2Reg).Value()
							)
						)
					);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeVar, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var TypeReg = Regs.Get(RegId2);
					Regs = Regs.Set(RegId1, NewProc.TypeVar(Span, TypeReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Var(
								Types.Get(TypeReg).Value()
							)
						)
					);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeFree, out Span, out RegId1)) {
				//--------------------------------------------------------------------------------
					Regs = Regs.Set(RegId1, NewProc.TypeFree(Span));
					Types.Push(mVM_Type.Type(mVM_Type.Free(RegId1)));
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeRecursive, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var FreeTypeReg = Regs.Get(RegId2);
					mStd.AssertEq(Types.Get(FreeTypeReg), mVM_Type.Type(mVM_Type.Free(RegId2)));
					var TypeBodyReg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeRecursive(Span, FreeTypeReg, TypeBodyReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Recursive(
								Types.Get(TypeBodyReg).Value()
							)
						)
					);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeInterface, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var FreeTypeReg = Regs.Get(RegId2);
					var TypeBodyReg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeInterface(Span, FreeTypeReg, TypeBodyReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Interface(
								Types.Get(TypeBodyReg).Value(),
								Types.Get(FreeTypeReg).Value()
							)
						)
					);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeGeneric, out Span, out RegId1, out RegId2, out RegId3)) {
				//--------------------------------------------------------------------------------
					var FreeTypeReg = Regs.Get(RegId2);
					var TypeBodyReg = Regs.Get(RegId3);
					Regs = Regs.Set(RegId1, NewProc.TypeGeneric(Span, FreeTypeReg, TypeBodyReg));
					Types.Push(
						mVM_Type.Type(
							mVM_Type.Generic(
								Types.Get(TypeBodyReg).Value(),
								Types.Get(FreeTypeReg).Value()
							)
						)
					);
				//--------------------------------------------------------------------------------
				} else if (Command.Match(mIL_AST.tCommandNodeType.TypeIs, out Span, out RegId1, out RegId2)) {
				//--------------------------------------------------------------------------------
					var VarReg = Regs.Get(RegId1);
					var TypeReg = Regs.Get(RegId2);
					mVM_Type.Unify(Types.Get(VarReg), Types.Get(TypeReg).Value(), aTrace);
				//--------------------------------------------------------------------------------
				} else {
				//--------------------------------------------------------------------------------
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
		mList.tList<(tText, mList.tList<mIL_AST.tCommandNode<tSpan>>)> aDefs,
		mVM_Data.tData aImport,
		mStd.tAction<tText> aTrace
	//================================================================================
	) => Run(ParseModule(aDefs, aTrace), aImport, aTrace);
	
	//================================================================================
	public static mVM_Data.tData
	Run(
		(mList.tList<mVM_Data.tProcDef<tSpan>>, mMap.tMap<tText, tInt32>) aModule,
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
		
		mVM.Run<tSpan>(
			mVM_Data.Proc(InitProc, DefTuple),
			mVM_Data.Empty(),
			aImport,
			Res,
			TraceOut
		);
		
		return Res;
	}
}
