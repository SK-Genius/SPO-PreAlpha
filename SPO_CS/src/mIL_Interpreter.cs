//IMPORT mVM_Data.cs
//IMPORT mIL_AST.cs
//IMPORT mMap.cs
//IMPORT mVM.cs
//IMPORT mPerf.cs

//#define MY_TRACE

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

public static class
mIL_Interpreter<tPos> {
	
	public static (
		mStream.tStream<mVM_Data.tProcDef<tPos>>,
		mMap.tMap<tText, tInt32>
	)
	ParseModule(
		mStream.tStream<(tText, mStream.tStream<mIL_AST.tCommandNode<tPos>>)> aDefs,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		using (mPerf.Measure()) {
			#if MY_TRACE
				aTrace(() => nameof(ParseModule));
			#endif
			var ModuleMap = mMap.Map<tText, tInt32>((a1, a2) => tText.CompareOrdinal(a1, a2) == 0);
			var Module = mStream.Stream<mVM_Data.tProcDef<tPos>>();
			
			var RestDefs = aDefs;
			while (RestDefs.Match(out var Def, out RestDefs)) {
				var (DefName, Commands) = Def;
				var NewProc = new mVM_Data.tProcDef<tPos>();
				// TODO: set type if it known
				var NextIndex = Module.Reduce(0, (aSum, _) => aSum + 1);
				ModuleMap = ModuleMap.Set(DefName, NextIndex);
				
				var DefEnvType = mVM_Type.Free("§ENV");
				var DefObjType = mVM_Type.Free("§OBJ");
				var DefArgType = mVM_Type.Free("§ARG");
				var DefResType = mVM_Type.Free("§RES");
				mAssert.Assert(
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
					)
				);
				Module = mStream.Concat(Module, mStream.Stream(NewProc));
				
				var Regs = mMap.Map<tText, tInt32>((a, b) => a == b)
				.Set(mIL_AST.cEmpty, mVM_Data.tProcDef<tPos>.cEmptyReg)
				.Set(mIL_AST.cOne, mVM_Data.tProcDef<tPos>.cOneReg)
				.Set(mIL_AST.cFalse, mVM_Data.tProcDef<tPos>.cFalseReg)
				.Set(mIL_AST.cTrue, mVM_Data.tProcDef<tPos>.cTrueReg)
				.Set(mIL_AST.cEmptyType, mVM_Data.tProcDef<tPos>.cEmptyTypeReg)
				.Set(mIL_AST.cBoolType, mVM_Data.tProcDef<tPos>.cBoolTypeReg)
				.Set(mIL_AST.cIntType, mVM_Data.tProcDef<tPos>.cIntTypeReg)
				.Set(mIL_AST.cEnv, mVM_Data.tProcDef<tPos>.cEnvReg)
				.Set(mIL_AST.cObj, mVM_Data.tProcDef<tPos>.cObjReg)
				.Set(mIL_AST.cArg, mVM_Data.tProcDef<tPos>.cArgReg)
				.Set(mIL_AST.cRes, mVM_Data.tProcDef<tPos>.cResReg);
				
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
				
				var RestCommands = Commands;
				while (RestCommands.Match(out var Command, out RestCommands)) {
					//--------------------------------------------------------------------------------
					if (Command.Match(mIL_AST.tCommandNodeType.CallFunc, out var Span, out var RegId1, out var RegId2, out var RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var ProcReg = Regs.ForceGet(RegId2);
						var ArgReg = Regs.ForceGet(RegId3);
						mAssert.Assert(
							mVM_Type.Unify(
								Types.Get(ProcReg),
								mVM_Type.Proc(mVM_Type.Empty(), Types.Get(ArgReg), ResType),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.Call(Span, ProcReg, ArgReg));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.CallProc, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var ProcReg = Regs.ForceGet(RegId2);
						var ArgReg = Regs.ForceGet(RegId3);
						var ObjType = mVM_Type.Free();
						mAssert.Assert(
							mVM_Type.Unify(
								Types.Get(ProcReg),
								mVM_Type.Pair(ObjType, mVM_Type.Proc(ObjType, Types.Get(ArgReg), ResType)),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.Exec(Span, ProcReg, ArgReg));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.Alias, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						Regs = Regs.Set(RegId1, Regs.ForceGet(RegId2));
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IsInt, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsInt(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.Int, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						Regs = Regs.Set(RegId1, NewProc.Int(Span, int.Parse(RegId2)));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IsBool, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsBool(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.BoolAnd, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.And(Span, Reg1, Reg2));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.BoolOr, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.Or(Span, Reg1, Reg2));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.BoolXOr, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.XOr(Span, Reg1, Reg2));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IntsAreEq, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsAreEq(Span, Reg1, Reg2));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IntsComp, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsComp(Span, Reg1, Reg2));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IntsAdd, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsAdd(Span, Reg1, Reg2));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IntsSub, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsSub(Span, Reg1, Reg2));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IntsMul, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsMul(Span, Reg1, Reg2));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IsPair, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsPair(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.Pair, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						var ResType = mVM_Type.Pair(Types.Get(Reg1), Types.Get(Reg2));
						Regs = Regs.Set(RegId1, NewProc.Pair(Span, Reg1, Reg2));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.First, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var ArgReg = Regs.ForceGet(RegId2);
						mAssert.Assert(
							mVM_Type.Unify(
								Types.Get(ArgReg),
								mVM_Type.Pair(ResType, mVM_Type.Free()),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.First(Span, ArgReg));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.Second, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var ArgReg = Regs.ForceGet(RegId2);
						mAssert.Assert(
							mVM_Type.Unify(
								Types.Get(ArgReg),
								mVM_Type.Pair(mVM_Type.Free(), ResType),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.Second(Span, ArgReg));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.ReturnIf, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResReg = Regs.ForceGet(RegId1);
						var CondReg = Regs.ForceGet(RegId2);
						mAssert.Assert(mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(ResReg), DefResType, aTrace));
						NewProc.ReturnIf(Span, CondReg, ResReg);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.RepeatIf, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ArgReg = Regs.ForceGet(RegId1);
						var CondReg = Regs.ForceGet(RegId2);
						mAssert.Assert(mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace));
						mAssert.Assert(mVM_Type.Unify(Types.Get(ArgReg), DefArgType, aTrace));
						NewProc.ContinueIf(Span, CondReg, ArgReg);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.TailCallIf, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResReg = Regs.ForceGet(RegId1);
						var CondReg = Regs.ForceGet(RegId2);
						mAssert.Assert(mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace));
						var ArgType = mVM_Type.Free();
						mAssert.Assert(
							mVM_Type.Unify(
								Types.Get(ResReg),
								mVM_Type.Pair(
									mVM_Type.Proc(mVM_Type.Empty(), ArgType, DefResType),
									ArgType
								),
								aTrace
							)
						);
						NewProc.TailCallIf(Span, CondReg, ResReg);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IsPrefix, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsPrefix(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.AddPrefix, out Span, out RegId1, out var Prefix, out RegId3)) {
					//--------------------------------------------------------------------------------
						var Reg = Regs.ForceGet(RegId3);
						var ResType = mVM_Type.Prefix(Prefix, Types.Get(Reg));
						Regs = Regs.Set(RegId1, NewProc.AddPrefix(Span, Prefix.GetHashCode(), Reg));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.SubPrefix, out Span, out RegId1, out Prefix, out RegId3)) {
					//--------------------------------------------------------------------------------
						var Reg = Regs.ForceGet(RegId3);
						var ResType = mVM_Type.Free();
						mAssert.Assert(
							mVM_Type.Unify(
								Types.Get(Reg),
								mVM_Type.Prefix(Prefix, ResType),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.DelPrefix(Span, Prefix.GetHashCode(), Reg));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.HasPrefix, out Span, out RegId1, out Prefix, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg = Regs.ForceGet(RegId3);
						//mAssert.Assert(
						//	mVM_Type.Unify(
						//		Types.Get(Reg),
						//		mVM_Type.Prefix(mVM_Type.cUnknownPrefix, mVM_Type.Free()),
						//		aTrace
						//	)
						//);
						Regs = Regs.Set(RegId1, NewProc.HasPrefix(Span, Prefix.GetHashCode(), Reg));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IsRecord, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsRecord(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.ExtendRec, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var OldRecordReg = Regs.ForceGet(RegId2);
						var NewElementReg = Regs.ForceGet(RegId3);
						Regs = Regs.Set(RegId1, NewProc.ExtendRec(Span, OldRecordReg, NewElementReg));
						Types.Push(mVM_Type.Record(Types.Get(NewElementReg), Types.Get(OldRecordReg)));
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.DivideRec, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var RecordReg = Regs.ForceGet(RegId2);
						var RecordType = Types.Get(RecordReg);
						mAssert.AssertNotEq(RecordType.Kind, mVM_Type.tKind.Empty);
						Regs = Regs.Set(RegId1, NewProc.DivideRec(Span, RecordReg));
						Types.Push(mVM_Type.Pair(RecordType.Refs[0], RecordType.Refs[1]));
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.Assert, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var Reg1 = Regs.ForceGet(RegId1);
						var Reg2 = Regs.ForceGet(RegId2);
						// TODO
						NewProc.Assert(Span, Reg1, Reg2);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IsVar, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsVar(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.VarDef, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var Reg = Regs.ForceGet(RegId2);
						var ResType = mVM_Type.Var(Types.Get(Reg));
						Regs = Regs.Set(RegId1, NewProc.VarDef(Span, Reg));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.VarSet, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var VarReg = Regs.ForceGet(RegId1);
						var ValReg = Regs.ForceGet(RegId2);
						mAssert.Assert(
							mVM_Type.Unify(
								Types.Get(VarReg),
								mVM_Type.Var(Types.Get(ValReg)),
								aTrace
							)
						);
						NewProc.VarSet(Span, VarReg, ValReg);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.VarGet, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var VarReg = Regs.ForceGet(RegId2);
						mAssert.Assert(mVM_Type.Unify(Types.Get(VarReg), mVM_Type.Var(ResType), aTrace));
						Regs = Regs.Set(RegId1, NewProc.VarGet(Span, VarReg));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.IsType, out Span, out RegId1, out RegId2)) {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsType(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.TypeCond, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						throw mError.Error("TODO"); // TODO
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.TypeFunc, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var ArgTypeReg = Regs.ForceGet(RegId2);
						var ResTypeReg = Regs.ForceGet(RegId3);
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
						var ObjTypeReg = Regs.ForceGet(RegId2);
						var FuncTypeReg = Regs.ForceGet(RegId3);
						var ArgType = mVM_Type.Free();
						var ResType = mVM_Type.Free();
						Regs = Regs.Set(RegId1, NewProc.TypeMeth(Span, ObjTypeReg, FuncTypeReg));
						mAssert.Assert(
							mVM_Type.Unify(
								Types.Get(FuncTypeReg),
								mVM_Type.Proc(mVM_Type.Empty(), ArgType, ResType),
								aTrace
							)
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
						var Type1Reg = Regs.ForceGet(RegId2);
						var Type2Reg = Regs.ForceGet(RegId3);
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
						var TypeReg = Regs.ForceGet(RegId2);
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
						var Type1Reg = Regs.ForceGet(RegId2);
						var Type2Reg = Regs.ForceGet(RegId3);
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
						var TypeReg = Regs.ForceGet(RegId2);
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
						var FreeTypeReg = Regs.ForceGet(RegId2);
						mAssert.AssertEq(Types.Get(FreeTypeReg), mVM_Type.Type(mVM_Type.Free(RegId2)), null, _ => mVM_Type.ToText(_, 10));
						var TypeBodyReg = Regs.ForceGet(RegId3);
						Regs = Regs.Set(RegId1, NewProc.TypeRecursive(Span, FreeTypeReg, TypeBodyReg));
						Types.Push(
							mVM_Type.Type(
								mVM_Type.Recursive(
									Types.Get(FreeTypeReg).Value(),
									Types.Get(TypeBodyReg).Value()
								)
							)
						);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.TypeInterface, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var FreeTypeReg = Regs.ForceGet(RegId2);
						var TypeBodyReg = Regs.ForceGet(RegId3);
						Regs = Regs.Set(RegId1, NewProc.TypeInterface(Span, FreeTypeReg, TypeBodyReg));
						Types.Push(
							mVM_Type.Type(
								mVM_Type.Interface(
									Types.Get(FreeTypeReg).Value(),
									Types.Get(TypeBodyReg).Value()
								)
							)
						);
					//--------------------------------------------------------------------------------
					} else if (Command.Match(mIL_AST.tCommandNodeType.TypeGeneric, out Span, out RegId1, out RegId2, out RegId3)) {
					//--------------------------------------------------------------------------------
						var FreeTypeReg = Regs.ForceGet(RegId2);
						var TypeBodyReg = Regs.ForceGet(RegId3);
						Regs = Regs.Set(RegId1, NewProc.TypeGeneric(Span, FreeTypeReg, TypeBodyReg));
						Types.Push(
							mVM_Type.Type(
								mVM_Type.Generic(
									Types.Get(FreeTypeReg).Value(),
									Types.Get(TypeBodyReg).Value()
								)
							)
						);
					//--------------------------------------------------------------------------------
					} else {
					//--------------------------------------------------------------------------------
						throw mError.Error($"impossible  (missing: {Command.NodeType})");
					}
					
					mDebug.AssertEq(Types.Size() - 1, NewProc._LastReg);
				}
			}
			#if MY_TRACE
			PrintILModule(aDefs, Module, a => { aTrace(() => a); });
			#endif
			
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
	}
	
	public static void
	PrintILModule(
		mStream.tStream<(tText, mStream.tStream<mIL_AST.tCommandNode<tPos>>)> aDefs,
		mStream.tStream<mVM_Data.tProcDef<tPos>> aModule,
		mStd.tAction<tText> aTrace
	) {
		var RestDefsModules = mStream.Zip(aDefs, aModule);
		while (RestDefsModules.Match(out var Def, out RestDefsModules)) {
			var RegIndex = mVM_Data.tProcDef<mStd.tEmpty>.cResReg;
			var (IL_Def, VM_Def) = Def; 
			var (Name, Commands) = IL_Def; 
			aTrace($"{Name} € {VM_Def.DefType.ToText(10)}:");
			while (Commands.Match(out var Command, out Commands)) {
				if (Command.NodeType >= mIL_AST.tCommandNodeType._Commands_) {
					aTrace($"\t{Command.NodeType} {Command._1} {Command._2} {Command._3}:");
				} else {
					aTrace($"\t{Command._1} := {Command.NodeType} {Command._2} {Command._3}:");
					if (Command.NodeType != mIL_AST.tCommandNodeType.Alias) {
						RegIndex += 1;
						try {
							aTrace($"\t\t€ {VM_Def.Types.Get(RegIndex).ToText(10)}");
						} catch {
							aTrace($"\t\t€ ERROR: out of index");
						}
					}
				}
			}
		}
	}
	
	public static mVM_Data.tData
	Run(
		mStream.tStream<(tText, mStream.tStream<mIL_AST.tCommandNode<tPos>>)> aDefs,
		mVM_Data.tData aImport,
		mStd.tFunc<tText, tPos> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) => Run(ParseModule(aDefs, aTrace), aImport, aPosToText, aTrace);
	
	public static mVM_Data.tData
	Run(
		(mStream.tStream<mVM_Data.tProcDef<tPos>>, mMap.tMap<tText, tInt32>) aModule,
		mVM_Data.tData aImport,
		mStd.tFunc<tText, tPos> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
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
				DefTuple = mVM_Data.Def(Defs.ForceFirst());
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
		var InitProc = VMModule.ForceFirst();
		
		#if MY_TRACE
			var TraceOut = aDebugStream;
		#else
			var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
		#endif
		
		mVM.Run(
			mVM_Data.Proc(InitProc, DefTuple),
			mVM_Data.Empty(),
			aImport,
			Res,
			aPosToText,
			TraceOut
		);
		
		return Res;
	}
}
