//IMPORT mVM_Data.cs
//IMPORT mIL_AST.cs
//IMPORT mMap.cs
//IMPORT mVM.cs
//IMPORT mPerf.cs

#nullable enable

#define noMY_TRACE

public static class
mIL_GenerateOpcodes {
	public static readonly tText cEmptyType = "EMPTY";
	public static readonly tText cBoolType = "BOOL";
	public static readonly tText cIntType = "INT";
	public static readonly tText cAnyType = "ANY";
	public static readonly tText cTypeType = "TYPE";
	
	// TODO: return tResult
	public static (
		mStream.tStream<mVM_Data.tProcDef<tPos>>?,
		mTreeMap.tTree<tText, tNat32>
	)
	GenerateOpcodes<tPos>(
		mIL_AST.tModule<tPos> aModule,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		using var _m_ = mPerf.Measure();
		#if MY_TRACE
			aTrace(() => nameof(GenerateOpcodes));
		#endif
		var ModuleMap = mTreeMap.Tree<tText, tNat32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign());
		var Module = mStream.Stream<mVM_Data.tProcDef<tPos>>();
		
		var TypeMap = mTreeMap.Tree<tText, tNat32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign())
		.Set(cEmptyType, (tNat32)0)
		.Set(cAnyType, (tNat32)1)
		.Set(cBoolType, (tNat32)2)
		.Set(cIntType, (tNat32)3)
		.Set(cTypeType, (tNat32)4);
		
		var Types_ = mStream.Stream(
			mVM_Type.Empty(),
			mVM_Type.Any(),
			mVM_Type.Bool(),
			mVM_Type.Int(),
			mVM_Type.Type()
		);
		
		var NextTypeIndex = Types_.Count();
		foreach (var TypeDef in aModule.TypeDef) {
			if (
				TypeDef.NodeType is < mIL_AST.tCommandNodeType._BeginTypes_ or
				>= mIL_AST.tCommandNodeType._EndTypes_
			) {
				throw mError.Error($"{TypeDef.NodeType} is not a Type Command");
			}
			
			var Type = mVM_Type.Empty();
			switch (TypeDef.NodeType) {
				case mIL_AST.tCommandNodeType.TypeFunc: {
					Type = mVM_Type.Proc(
						mVM_Type.Empty(),
						TypeDef._2.ThenTry(a => TypeMap.TryGet(a)).ThenTry(Types_.TryGet).ElseThrow(() => "TODO"), // TODO
						TypeDef._3.ThenTry(a => TypeMap.TryGet(a)).ThenTry(Types_.TryGet).ElseThrow(() => "TODO") // TODO
					);
					break;
				}
				case mIL_AST.tCommandNodeType.TypePair: {
					Type = mVM_Type.Pair(
						TypeDef._2.ThenTry(a => TypeMap.TryGet(a)).ThenTry(a => Types_.TryGet(a)).ElseThrow(() => "TODO"), // TODO
						TypeDef._3.ThenTry(a => TypeMap.TryGet(a)).ThenTry(a => Types_.TryGet(a)).ElseThrow(() => "TODO") // TODO
					);
					break;
				}
				case mIL_AST.tCommandNodeType.TypeSet: {
					Type = mVM_Type.Set(
						TypeDef._2.ThenTry(a => TypeMap.TryGet(a)).ThenTry(a => Types_.TryGet(a)).ElseThrow(() => "TODO"), // TODO
						TypeDef._3.ThenTry(a => TypeMap.TryGet(a)).ThenTry(a => Types_.TryGet(a)).ElseThrow(() => "TODO") // TODO
					);
					break;
				}
				case mIL_AST.tCommandNodeType.TypePrefix: {
					Type = mVM_Type.Prefix(
						TypeDef._2.ElseThrow(() => "TODO"), // TODO
						TypeDef._3.ThenTry(a => TypeMap.TryGet(a)).ThenTry(a => Types_.TryGet(a)).ElseThrow(() => "TODO") // TODO
					);
					break;
				}
				case mIL_AST.tCommandNodeType.TypeRec: {
					Type = mVM_Type.Record(
						TypeDef._2.ThenTry(a => TypeMap.TryGet(a)).ThenTry(a => Types_.TryGet(a)).ElseThrow(() => "TODO"), // TODO
						TypeDef._3.ThenTry(a => TypeMap.TryGet(a)).ThenTry(a => Types_.TryGet(a)).ElseThrow(() => "TODO") // TODO
					);
					break;
				}
				case mIL_AST.tCommandNodeType.TypeFree: {
					Type = mVM_Type.Free("TODO"); // TODO
					break;
				}
				case mIL_AST.tCommandNodeType.TypeGeneric: {
					Type = mVM_Type.Generic(
						TypeDef._2.ThenTry(a => TypeMap.TryGet(a)).ThenTry(a => Types_.TryGet(a)).ElseThrow(() => "TODO"), // TODO
						TypeDef._3.ThenTry(a => TypeMap.TryGet(a)).ThenTry(a => Types_.TryGet(a)).ElseThrow(() => "TODO") // TODO
					);
					break;
				}
				default: {
					throw mError.Error("not implemented: " + TypeDef.NodeType);
				}
			}
			Types_ = mStream.Concat(Types_, mStream.Stream(Type));
			TypeMap = TypeMap.Set(TypeDef._1, NextTypeIndex);
			NextTypeIndex += 1;
		}
		
		foreach (var (DefName, TypeName, Commands) in aModule.Defs) {
			aTrace(() => "§DEF " + DefName);
			// TODO: set type if it known
			var NextIndex = Module.Count();
			ModuleMap = ModuleMap.Set(DefName, NextIndex);
			
			var DefType = TypeMap.TryGet(TypeName).ThenTry(Types_.TryGet).ElseThrow(() => $"type '{TypeName}' not found");
			
			if (!DefType.IsProc(out var NullType, out var DefEnvType, out var DefProcType)) {
				throw mError.Error("impossible");
			}
			if (DefProcType.IsGeneric(out var FreeType, out var InnerType)) {
				DefProcType = InnerType;	
			}
			if (!DefProcType.IsProc(out var DefObjType, out var DefArgType, out var DefResType)) {
				throw mError.Error("impossible");
			}
			
			var NewProc = new mVM_Data.tProcDef<tPos>(DefType);
			
			Module = mStream.Concat(Module, mStream.Stream(NewProc));
			
			var Regs = mTreeMap.Tree<tText, tNat32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign())
			.Set(mIL_AST.cEmpty, mVM_Data.cEmptyReg)
			.Set(mIL_AST.cOne, mVM_Data.cOneReg)
			.Set(mIL_AST.cFalse, mVM_Data.cFalseReg)
			.Set(mIL_AST.cTrue, mVM_Data.cTrueReg)
			.Set(mIL_AST.cEmptyType, mVM_Data.cEmptyTypeReg)
			.Set(mIL_AST.cBoolType, mVM_Data.cBoolTypeReg)
			.Set(mIL_AST.cIntType, mVM_Data.cIntTypeReg)
			.Set(mIL_AST.cTypeType, mVM_Data.cTypeTypeReg)
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
				mStd.tFunc<tText, tText> Fail_ = (tText a) => $"{Command.Pos}: {Command.ToText()}\n{a}";
				mStd.tFunc<tText> Fail = () => $"{Command.Pos}: {Command.ToText()}";
				
				aTrace(() => Command.ToText());
				aTrace(
					() => ("  :: " +
						Command._2.ThenTry(aReg => Regs.TryGet(aReg)).ThenDo(a => Types.Get(a).ToText(10)).Else("") + " ; " + 
						Command._3.ThenTry(aReg => Regs.TryGet(aReg)).ThenDo(a => Types.Get(a).ToText(10)).Else("")
					)
				);
				switch (Command) {
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.CallFunc, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ProcReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var ArgReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						var ResType = mVM_Type.Infer(
							Types.Get(ProcReg),
							mVM_Type.Empty(),
							Types.Get(ArgReg),
							aTrace
						).ElseThrow(Fail_);
						Regs = Regs.Set(RegId1, NewProc.Call(Span, ProcReg, ArgReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.CallProc, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ProcReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var ArgReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						var ObjType = mVM_Type.Free();
						var ResType = mVM_Type.Infer(
							Types.Get(ProcReg),
							ObjType,
							Types.Get(ArgReg),
							aTrace
						).ElseThrow(Fail_);
						Regs = Regs.Set(RegId1, NewProc.Exec(Span, ProcReg, ArgReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.Alias, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						Regs = Regs.TryGet(RegId2.ElseThrow()).ThenDo(a => Regs.Set(RegId1, a)).ElseThrow(Fail);;
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IsInt, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.TryGet(RegId2.ElseThrow()).ThenDo(a => Regs.Set(RegId1, NewProc.IsInt(Span, (tInt32)a))).ElseThrow(Fail);
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.Int, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						Regs = Regs.Set(RegId1, NewProc.Int(Span, tInt32.Parse(RegId2.ElseThrow())));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IsBool, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.TryGet(RegId2.ElseThrow()).ThenDo(a => Regs.Set(RegId1, NewProc.IsBool(Span, a))).ElseThrow(Fail);
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.BoolAnd, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Bool());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Bool());
						Regs = Regs.Set(RegId1, NewProc.And(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.BoolOr, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Bool());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Bool());
						Regs = Regs.Set(RegId1, NewProc.Or(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.BoolXOr, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Bool());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Bool());
						Regs = Regs.Set(RegId1, NewProc.XOr(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IntsAreEq, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsAreEq(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IntsComp, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsComp(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IntsAdd, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsAdd(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IntsSub, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsSub(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IntsMul, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Int();
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsMul(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IntsDiv, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Int());
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsDiv(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IsPair, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.TryGet(RegId2.ElseThrow()).ThenDo(a => Regs.Set(RegId1, NewProc.IsPair(Span, a))).ElseThrow(Fail);
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.Pair, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var Reg1 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						var ResType = mVM_Type.Pair(Types.Get(Reg1), Types.Get(Reg2));
						Regs = Regs.Set(RegId1, NewProc.Pair(Span, Reg1, Reg2));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.First, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ArgReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var ArgType = Types.Get(ArgReg);
						mAssert.IsTrue(ArgType.IsPair(out var ResType, out var __), () => $"{Span} {RegId1} := FIRST {RegId2} :: {ArgType.ToText(10)}");
						Regs = Regs.Set(RegId1, NewProc.First(Span, ArgReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.Second, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ArgReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						mAssert.IsTrue(Types.Get(ArgReg).IsPair(out _, out  var ResType));
						Regs = Regs.Set(RegId1, NewProc.Second(Span, ArgReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.ReturnIf, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var CondReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var ResReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.IsTrue(Types.Get(CondReg).IsBool(), $"expect $Bool but was {Types.Get(CondReg).Kind}");
						var ResType = Types.Get(ResReg);
						
						ResType.IsSubType(DefResType, mStd.cEmpty)
						.ElseThrow(
							_ => tText.Concat(
								_,
								"\n",
								ResType.ToText(1),
								"\n!<\n",
								DefResType.ToText(1),
								"\n",
								Command
							)
						);
						
						NewProc.ReturnIf(Span, CondReg, ResReg);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.RepeatIf, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ArgReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						var CondReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						mAssert.IsTrue(Types.Get(CondReg).IsBool());
						DefArgType.IsSubType(
							Types.Get(ArgReg),
							mStd.cEmpty
						).ElseThrow(
						);
						NewProc.ContinueIf(Span, CondReg, ArgReg);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.TailCallIf, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						//throw new System.NotImplementedException();
						var ResReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						var CondReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(CondReg), mVM_Type.Bool());
						var ArgType = mVM_Type.Free();
						Types.Get(
							ResReg
						).IsSubType(
							mVM_Type.Tuple(
								mVM_Type.Proc(mVM_Type.Empty(), ArgType, DefResType),
								ArgType
							),
							mStd.cEmpty
						).ElseThrow();
						NewProc.TailCallIf(Span, CondReg, ResReg);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IsPrefix, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.TryGet(RegId2.ElseThrow()).ThenDo(a => Regs.Set(RegId1, NewProc.IsPrefix(Span, a))).ElseThrow(Fail);
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.AddPrefix, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var Prefix = RegId2.ElseThrow();
						var Reg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						var ResType = mVM_Type.Prefix(Prefix, Types.Get(Reg));
						Regs = Regs.Set(RegId1, NewProc.AddPrefix(Span, (tNat32)Prefix.GetHashCode(), Reg)); // TODO: avoid Hash collisions
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.SubPrefix, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var Prefix = RegId2.ElseThrow();
						var Reg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.IsTrue(Types.Get(Reg).IsPrefix(Prefix, out var ResType));
						Regs = Regs.Set(RegId1, NewProc.DelPrefix(Span, (tNat32)Prefix.GetHashCode(), Reg)); // TODO: avoid Hash collisions
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.HasPrefix, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var Prefix = RegId2.ElseThrow();
						var ResType = mVM_Type.Bool();
						var Reg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						//mAssert.Assert(
						//	mVM_Type.Unify(
						//		Types.Get(Reg),
						//		mVM_Type.Prefix(mVM_Type.cUnknownPrefix, mVM_Type.Free()),
						//		aTrace
						//	)
						//);
						Regs = Regs.Set(RegId1, NewProc.HasPrefix(Span, (tNat32)Prefix.GetHashCode(), Reg)); // TODO: avoid Hash collisions
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IsRecord, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.TryGet(RegId2.ElseThrow()).ThenDo(a => Regs.Set(RegId1, NewProc.IsRecord(Span, a))).ElseThrow(Fail);
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.ExtendRec, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var OldRecordReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var NewElementReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						Regs = Regs.Set(RegId1, NewProc.ExtendRec(Span, OldRecordReg, NewElementReg));
						Types.Push(mVM_Type.Record(Types.Get(OldRecordReg), Types.Get(NewElementReg)));
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.DivideRec, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var RecordReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var RecordType = Types.Get(RecordReg);
						mAssert.AreNotEquals(RecordType.Kind, mVM_Type.tKind.Empty);
						Regs = Regs.Set(RegId1, NewProc.DivideRec(Span, RecordReg));
						Types.Push(mVM_Type.Pair(RecordType.Refs[0], RecordType.Refs[1]));
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.Assert, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var Reg1 = Regs.TryGet(RegId1).ElseThrow(Fail);
						var Reg2 = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						// TODO
						NewProc.Assert(Span, Reg1, Reg2);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IsVar, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.TryGet(RegId2.ElseThrow()).ThenDo(a => Regs.Set(RegId1, NewProc.IsVar(Span, a))).ElseThrow(Fail);
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.VarDef, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var Reg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var ResType = mVM_Type.Var(Types.Get(Reg));
						Regs = Regs.Set(RegId1, NewProc.VarDef(Span, Reg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.VarSet, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var VarReg = Regs.TryGet(RegId1).ElseThrow(Fail);
						var ValReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(
							Types.Get(VarReg),
							mVM_Type.Var(Types.Get(ValReg))
						);
						NewProc.VarSet(Span, VarReg, ValReg);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.VarGet, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var VarReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						mAssert.IsTrue(Types.Get(VarReg).IsVar(out var ResType));
						Regs = Regs.Set(RegId1, NewProc.VarGet(Span, VarReg));
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.IsType, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var ResType = mVM_Type.Bool();
						Regs = Regs.TryGet(RegId2.ElseThrow()).ThenDo(a => Regs.Set(RegId1, NewProc.IsType(Span, a))).ElseThrow(Fail);
						Types.Push(ResType);
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.TypeCond, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						throw mError.Error("TODO"); // TODO
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.TypeFunc, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ArgTypeReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var ResTypeReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
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
					case { NodeType: mIL_AST.tCommandNodeType.TypeMethod, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var ObjTypeReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var FuncTypeReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						mAssert.IsTrue(Types.Get(FuncTypeReg).IsProc(out var EmptyType, out var ArgType, out var ResType));
						mAssert.AreEquals(EmptyType, mVM_Type.Empty());
						Regs = Regs.Set(RegId1, NewProc.TypeMeth(Span, ObjTypeReg, FuncTypeReg));
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
					case { NodeType: mIL_AST.tCommandNodeType.TypePair, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var Type1Reg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Type2Reg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
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
					case { NodeType: mIL_AST.tCommandNodeType.TypePrefix, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var Prefix = RegId2.ElseThrow();
						var TypeReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
						Regs = Regs.Set(RegId1, NewProc.TypePrefix(Span, (tNat32)Prefix.GetHashCode(), TypeReg)); // TODO: avoid Hash collisions
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
					case { NodeType: mIL_AST.tCommandNodeType.TypeSet, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var Type1Reg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var Type2Reg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
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
					case { NodeType: mIL_AST.tCommandNodeType.TypeVar, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
					//--------------------------------------------------------------------------------
						var TypeReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
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
					case { NodeType: mIL_AST.tCommandNodeType.TypeFree, Pos: var Span, _1: var RegId1 }: {
					//--------------------------------------------------------------------------------
						Regs = Regs.Set(RegId1, NewProc.TypeFree(Span));
						Types.Push(mVM_Type.Type(mVM_Type.Free(RegId1)));
						break;
					}
					//--------------------------------------------------------------------------------
					case { NodeType: mIL_AST.tCommandNodeType.TypeRecursive, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var FreeTypeReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						mAssert.AreEquals(Types.Get(FreeTypeReg), mVM_Type.Type(mVM_Type.Free(RegId2.ElseThrow())), null, a => a.ToText(10));
						var TypeBodyReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
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
					case { NodeType: mIL_AST.tCommandNodeType.TypeInterface, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var FreeTypeReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var TypeBodyReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
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
					case { NodeType: mIL_AST.tCommandNodeType.TypeGeneric, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
					//--------------------------------------------------------------------------------
						var FreeTypeReg = Regs.TryGet(RegId2.ElseThrow()).ElseThrow(Fail);
						var TypeBodyReg = Regs.TryGet(RegId3.ElseThrow()).ElseThrow(Fail);
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
				//aTrace(() => "  => " + Regs.TryGet(Command._1).Then(a => Types.Get(a).ToText(10)).Else("???"));
				mAssert.AreEquals(Types.Size() - 1, NewProc._LastReg);
			}
			mAssert.AreEquals(NewProc.Commands.Size(), NewProc.PosList.Size());
		}
#if MY_TRACE
		//PrintILModule(aDefs, Module, a => { aTrace(() => a); });
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
		foreach (var ((Name, _, Commands), VM_Def) in mStream.ZipShort(aDefs, aModule)) {
			var RegIndex = mVM_Data.cResReg;
			aTrace($"{Name} € {VM_Def.DefType.ToText(10)}:");
			foreach (var Command in Commands) {
				if (Command.NodeType >= mIL_AST.tCommandNodeType._BeginCommands_) {
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
}
