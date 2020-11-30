//IMPORT mVM_Data.cs
//IMPORT mIL_AST.cs
//IMPORT mMap.cs
//IMPORT mVM.cs
//IMPORT mPerf.cs

#nullable enable

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
mIL_GenerateOpcodes {
	
	public static (
		mStream.tStream<mVM_Data.tProcDef<tPos>>?,
		mTreeMap.tTree<tText, tInt32>
	)
	GenerateOpcodes<tPos>(
		mStream.tStream<(tText, mVM_Type.tType, mStream.tStream<mIL_AST.tCommandNode<tPos>>?)>? aDefs,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		using var _ = mPerf.Measure();
		#if MY_TRACE
			aTrace(() => nameof(GenerateOpcodes));
		#endif
		var ModuleMap = mTreeMap.Tree<tText, tInt32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign());
		var Module = mStream.Stream<mVM_Data.tProcDef<tPos>>();
		
		foreach (var (DefName, DefType, Commands) in aDefs) {
			var NewProc = new mVM_Data.tProcDef<tPos>();
			// TODO: set type if it known
			var NextIndex = Module.Reduce(0, (aSum, _) => aSum + 1);
			ModuleMap = ModuleMap.Set(DefName, NextIndex);
			
			if (!DefType.MatchProc(out var NullType, out var DefEnvType, out var DefProcType)) {
				throw mError.Error("impossible");
			}
			mAssert.AreEquals(NullType.Kind, mVM_Type.tKind.Empty);
			if (!DefProcType.MatchProc(out var DefObjType, out var DefArgType, out var DefResType)) {
				throw mError.Error("impossible");
			}
			
			mAssert.IsTrue(
				mVM_Type.Unify(
					NewProc.DefType,
					mVM_Type.Proc(
						mVM_Type.Empty(),
						DefEnvType!,
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
			
			var Regs = mTreeMap.Tree<tText, tInt32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign())
			.Set(mIL_AST.cEmpty, mVM_Data.cEmptyReg)
			.Set(mIL_AST.cOne, mVM_Data.cOneReg)
			.Set(mIL_AST.cFalse, mVM_Data.cFalseReg)
			.Set(mIL_AST.cTrue, mVM_Data.cTrueReg)
			.Set(mIL_AST.cEmptyType, mVM_Data.cEmptyTypeReg)
			.Set(mIL_AST.cBoolType, mVM_Data.cBoolTypeReg)
			.Set(mIL_AST.cIntType, mVM_Data.cIntTypeReg)
			.Set(mIL_AST.cEnv, mVM_Data.cEnvReg)
			.Set(mIL_AST.cObj, mVM_Data.cObjReg)
			.Set(mIL_AST.cArg, mVM_Data.cArgReg)
			.Set(mIL_AST.cRes, mVM_Data.cResReg);
			
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
			
			foreach (var Command in Commands) {
				switch (0) {
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.CallFunc, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var ProcReg = Regs.ForceGet(RegId2);
						var ArgReg = Regs.ForceGet(RegId3);
						mAssert.IsTrue(
							mVM_Type.Unify(
								Types.Get(ProcReg),
								mVM_Type.Proc(mVM_Type.Empty(), Types.Get(ArgReg), ResType),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.Call(Span, ProcReg, ArgReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.CallProc, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var ProcReg = Regs.ForceGet(RegId2);
						var ArgReg = Regs.ForceGet(RegId3);
						var ObjType = mVM_Type.Free();
						mAssert.IsTrue(
							mVM_Type.Unify(
								Types.Get(ProcReg),
								mVM_Type.Pair(ObjType, mVM_Type.Proc(ObjType, Types.Get(ArgReg), ResType)),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.Exec(Span, ProcReg, ArgReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.Alias, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						Regs = Regs.Set(RegId1, Regs.ForceGet(RegId2));
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IsInt, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsInt(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.Int, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						Regs = Regs.Set(RegId1, NewProc.Int(Span, tInt32.Parse(RegId2!)));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IsBool, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsBool(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.BoolAnd, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.And(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.BoolOr, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.Or(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.BoolXOr, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Bool(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Bool(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.XOr(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IntsAreEq, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsAreEq(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IntsComp, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsComp(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IntsAdd, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsAdd(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IntsSub, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsSub(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IntsMul, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg1), mVM_Type.Int(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(Reg2), mVM_Type.Int(), aTrace));
						Regs = Regs.Set(RegId1, NewProc.IntsMul(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IsPair, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsPair(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.Pair, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var Reg1 = Regs.ForceGet(RegId2);
						var Reg2 = Regs.ForceGet(RegId3);
						var ResType = mVM_Type.Pair(Types.Get(Reg1), Types.Get(Reg2));
						Regs = Regs.Set(RegId1, NewProc.Pair(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.First, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var ArgReg = Regs.ForceGet(RegId2);
						mAssert.IsTrue(
							mVM_Type.Unify(
								Types.Get(ArgReg),
								mVM_Type.Pair(ResType, mVM_Type.Free()),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.First(Span, ArgReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.Second, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var ArgReg = Regs.ForceGet(RegId2);
						mAssert.IsTrue(
							mVM_Type.Unify(
								Types.Get(ArgReg),
								mVM_Type.Pair(mVM_Type.Free(), ResType),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.Second(Span, ArgReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.ReturnIf, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResReg = Regs.ForceGet(RegId1);
						var CondReg = Regs.ForceGet(RegId2);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(ResReg), DefResType, aTrace));
						NewProc.ReturnIf(Span, CondReg, ResReg);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.RepeatIf, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ArgReg = Regs.ForceGet(RegId1);
						var CondReg = Regs.ForceGet(RegId2);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace));
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(ArgReg), DefArgType, aTrace));
						NewProc.ContinueIf(Span, CondReg, ArgReg);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TailCallIf, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResReg = Regs.ForceGet(RegId1);
						var CondReg = Regs.ForceGet(RegId2);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(CondReg), mVM_Type.Bool(), aTrace));
						var ArgType = mVM_Type.Free();
						mAssert.IsTrue(
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IsPrefix, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsPrefix(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.AddPrefix, out var Span, out var RegId1, out var Prefix, out var RegId3): {
					//--------------------------------------------------------------------------------
						var Reg = Regs.ForceGet(RegId3);
						var ResType = mVM_Type.Prefix(Prefix, Types.Get(Reg));
						Regs = Regs.Set(RegId1, NewProc.AddPrefix(Span, Prefix.GetHashCode(), Reg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.SubPrefix, out var Span, out var RegId1, out var Prefix, out var RegId3): {
					//--------------------------------------------------------------------------------
						var Reg = Regs.ForceGet(RegId3);
						var ResType = mVM_Type.Free();
						mAssert.IsTrue(
							mVM_Type.Unify(
								Types.Get(Reg),
								mVM_Type.Prefix(Prefix, ResType),
								aTrace
							)
						);
						Regs = Regs.Set(RegId1, NewProc.DelPrefix(Span, Prefix.GetHashCode(), Reg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.HasPrefix, out var Span, out var RegId1, out var Prefix, out var RegId3): {
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IsRecord, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsRecord(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.ExtendRec, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var OldRecordReg = Regs.ForceGet(RegId2);
						var NewElementReg = Regs.ForceGet(RegId3);
						Regs = Regs.Set(RegId1, NewProc.ExtendRec(Span, OldRecordReg, NewElementReg));
						Types.Push(mVM_Type.Record(Types.Get(NewElementReg), Types.Get(OldRecordReg)));
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.DivideRec, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var RecordReg = Regs.ForceGet(RegId2);
						var RecordType = Types.Get(RecordReg);
						mAssert.AreNotEquals(RecordType.Kind, mVM_Type.tKind.Empty);
						Regs = Regs.Set(RegId1, NewProc.DivideRec(Span, RecordReg));
						Types.Push(mVM_Type.Pair(RecordType.Refs[0], RecordType.Refs[1]));
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.Assert, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var Reg1 = Regs.ForceGet(RegId1);
						var Reg2 = Regs.ForceGet(RegId2);
						// TODO
						NewProc.Assert(Span, Reg1, Reg2);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IsVar, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsVar(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.VarDef, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var Reg = Regs.ForceGet(RegId2);
						var ResType = mVM_Type.Var(Types.Get(Reg));
						Regs = Regs.Set(RegId1, NewProc.VarDef(Span, Reg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.VarSet, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var VarReg = Regs.ForceGet(RegId1);
						var ValReg = Regs.ForceGet(RegId2);
						mAssert.IsTrue(
							mVM_Type.Unify(
								Types.Get(VarReg),
								mVM_Type.Var(Types.Get(ValReg)),
								aTrace
							)
						);
						NewProc.VarSet(Span, VarReg, ValReg);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.VarGet, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Free();
						var VarReg = Regs.ForceGet(RegId2);
						mAssert.IsTrue(mVM_Type.Unify(Types.Get(VarReg), mVM_Type.Var(ResType), aTrace));
						Regs = Regs.Set(RegId1, NewProc.VarGet(Span, VarReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.IsType, out var Span, out var RegId1, out var RegId2): {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.Set(RegId1, NewProc.IsType(Span, Regs.ForceGet(RegId2)));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeEmpty, out var Span, out var RegId1): {
					//--------------------------------------------------------------------------------
						Regs = Regs.Set(RegId1, NewProc.TypeEmpty(Span));
						Types.Push(mVM_Type.Empty());
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeAny, out var Span, out var RegId1): {
					//--------------------------------------------------------------------------------
						Regs = Regs.Set(RegId1, NewProc.TypeAny(Span));
						Types.Push(mVM_Type.Any());
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeInt, out var Span, out var RegId1): {
					//--------------------------------------------------------------------------------
						Regs = Regs.Set(RegId1, NewProc.TypeInt(Span));
						Types.Push(mVM_Type.Int());
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeCond, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						throw mError.Error("TODO"); // TODO
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeFunc, out var Span, out var RegId1, out var RegId2, out var RegId3): {
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeMethod, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var ObjTypeReg = Regs.ForceGet(RegId2);
						var FuncTypeReg = Regs.ForceGet(RegId3);
						var ArgType = mVM_Type.Free();
						var ResType = mVM_Type.Free();
						Regs = Regs.Set(RegId1, NewProc.TypeMeth(Span, ObjTypeReg, FuncTypeReg));
						mAssert.IsTrue(
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypePair, out var Span, out var RegId1, out var RegId2, out var RegId3): {
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypePrefix, out var Span, out var RegId1, out var Prefix, out var RegId2): {
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeSet, out var Span, out var RegId1, out var RegId2, out var RegId3): {
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeVar, out var Span, out var RegId1, out var RegId2): {
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeFree, out var Span, out var RegId1): {
					//--------------------------------------------------------------------------------
						Regs = Regs.Set(RegId1, NewProc.TypeFree(Span));
						Types.Push(mVM_Type.Type(mVM_Type.Free(RegId1)));
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeRecursive, out var Span, out var RegId1, out var RegId2, out var RegId3): {
					//--------------------------------------------------------------------------------
						var FreeTypeReg = Regs.ForceGet(RegId2);
						mAssert.AreEquals(Types.Get(FreeTypeReg), mVM_Type.Type(mVM_Type.Free(RegId2)), null, _ => _.ToText(10));
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeInterface, out var Span, out var RegId1, out var RegId2, out var RegId3): {
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
						break;
					}
					//--------------------------------------------------------------------------------
					case 0 when Command.Match(mIL_AST.tCommandNodeType.TypeGeneric, out var Span, out var RegId1, out var RegId2, out var RegId3): {
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
						break;
					}
					//--------------------------------------------------------------------------------
					default: {
					//--------------------------------------------------------------------------------
						throw mError.Error($"impossible  (missing: {Command.NodeType})");
					}
				}
				mAssert.AreEquals(Types.Size() - 1, NewProc._LastReg);
			}
			mAssert.AreEquals(NewProc.Commands.Size(), NewProc.PosList.Size());
		}
#if MY_TRACE
		PrintILModule(aDefs, Module, a => { aTrace(() => a); });
#endif

#if !true
		{
			var Module_ = Module.ToArrayList();
			foreach (var KeyValue in ModuleMap._KeyValuePairs) {
				var (Name, Index) = KeyValue;
				aTrace($@"{Name} @ {Module_.Get(Index)._DefType}");
			}
		}
#endif

		return (Module, ModuleMap);
	}
	
	public static void
	PrintILModule<tPos>(
		mStream.tStream<(tText, mVM_Type.tType, mStream.tStream<mIL_AST.tCommandNode<tPos>>?)>? aDefs,
		mStream.tStream<mVM_Data.tProcDef<tPos>>? aModule,
		mStd.tAction<tText> aTrace
	) {
		foreach (var ((Name, _, Commands), VM_Def) in mStream.Zip(aDefs, aModule)) {
			var RegIndex = mVM_Data.cResReg;
			aTrace($"{Name} € {VM_Def.DefType.ToText(10)}:");
			foreach (var Command in Commands) {
				if (Command.NodeType >= mIL_AST.tCommandNodeType._Commands_) {
					aTrace($"\t{Command.NodeType} {Command._1} {Command._2} {Command._3}:");
				} else {
					if (Command.NodeType != mIL_AST.tCommandNodeType.Alias) {
						RegIndex += 1;
					}
					aTrace($"\t({RegIndex}) {Command._1} := {Command.NodeType} {Command._2} {Command._3}:");
					try {
						aTrace($"\t\t€ {VM_Def.Types.Get(RegIndex).ToText(10)}");
					} catch {
						aTrace($"\t\t€ ERROR: out of index");
					}
				}
			}
		}
	}
	
	public static mVM_Data.tData
	Run<tPos>(
		mStream.tStream<(tText, mVM_Type.tType, mStream.tStream<mIL_AST.tCommandNode<tPos>>?)>? aDefs,
		mVM_Data.tData aImport,
		mStd.tFunc<tText, tPos> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) => Run(GenerateOpcodes(aDefs, aTrace), aImport, aPosToText, aTrace);
	
	public static mVM_Data.tData
	Run<tPos>(
		(mStream.tStream<mVM_Data.tProcDef<tPos>>?, mTreeMap.tTree<tText, tInt32>) aModule,
		mVM_Data.tData aImport,
		mStd.tFunc<tText, tPos> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		var (VMModule, ModuleMap) = aModule;
		var Res = mVM_Data.Empty();
		var Defs = VMModule.Skip(1).Reverse();
		
		var DefTuple = Defs.Take(2).ToArrayList().Size() switch {
			0 => mVM_Data.Empty(),
			1 => mVM_Data.Def(Defs!.ForceFirst()),
			_ => Defs.Reduce(
				mVM_Data.Empty(),
				(aTuple, aDef) => mVM_Data.Pair(
					mVM_Data.Def(aDef),
					aTuple
				)
			),
		};
		var InitProc = VMModule!.ForceFirst();
		
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
