// IMPORT Common/mStd
// IMPORT Common/mAssert
// IMPORT Common/mError
// IMPORT Common/mMath
// IMPORT Common/mMaybe
// IMPORT Common/mResult
// IMPORT Common/mStream
// IMPORT Common/mTreeMap
// IMPORT Common/mPerf
// IMPORT Common/mArrayList
// IMPORT mIL_AST
// IMPORT mVM_Type
// IMPORT mVM_Data

//#define MY_TRACE_IL

public static class
mIL_GenerateOpcodes {
	public static readonly tText cEmptyType = "EMPTY_TYPE";
	public static readonly tText cBoolType = "BOOL";
	public static readonly tText cIntType = "INT";
	public static readonly tText cAnyType = "ANY";
	public static readonly tText cTypeType = "TYPE";
	
	// TODO: return tResult
	public static (
		mStream.tStream<mVM_Data.tProcDef<tPos>> Module,
		mTreeMap.tTree<tText, tNat32> ModuleMap
	)
	GenerateOpcodes<tPos>(
		mIL_AST.tModule<tPos> aModule,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		using var _m_ = mPerf.Measure();
		#if MY_TRACE_IL
			aTrace(() => nameof(GenerateOpcodes));
		#endif
		var ModuleMap = mTreeMap.Tree<tText, tNat32>(
			(a1, a2) => tText.CompareOrdinal(a1, a2).Sign(),
			[]
		);
		var Module = mStream.Stream<mVM_Data.tProcDef<tPos>>([]);
		
		var TypeMap = mTreeMap.Tree<tText, tNat32>(
			(a1, a2) => tText.CompareOrdinal(a1, a2).Sign(),
			[
				(cEmptyType, 0u),
				(cAnyType, 1u),
				(cBoolType, 2u),
				(cIntType, 3u),
				(cTypeType, 4u),
			]
		);
		
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
			
			var Type = TypeDef.NodeType switch {
				mIL_AST.tCommandNodeType.TypeFunc => mVM_Type.Proc(
					mVM_Type.Empty(),
					TypeDef._2.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO"), // TODO
					TypeDef._3.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO") // TODO
				),
				mIL_AST.tCommandNodeType.TypePair => mVM_Type.Pair(
					TypeDef._2.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO"), // TODO
					TypeDef._3.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO") // TODO
				),
				mIL_AST.tCommandNodeType.TypeSet => mVM_Type.Set(
					TypeDef._2.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO"), // TODO
					TypeDef._3.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO") // TODO
				),
				mIL_AST.tCommandNodeType.TypePrefix => mVM_Type.Prefix(
					TypeDef._2.AssertNotEmpty(() => "TODO"), // TODO
					TypeDef._3.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO") // TODO
				),
				mIL_AST.tCommandNodeType.TypeRecord => mVM_Type.Record(
					TypeDef._2.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO"), // TODO
					TypeDef._3.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO") // TODO
				),
				mIL_AST.tCommandNodeType.TypeFree => mVM_Type.Free(TypeDef._1), // TODO
				mIL_AST.tCommandNodeType.TypeGeneric => mVM_Type.Generic(
					TypeDef._2.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO"), // TODO
					TypeDef._3.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO") // TODO
				),
				mIL_AST.tCommandNodeType.TypeRecursive => mVM_Type.Recursive(
					TypeDef._2.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO"), // TODO
					TypeDef._3.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO") // TODO
				),
				mIL_AST.tCommandNodeType.TypeVar => mVM_Type.Var(
					TypeDef._2.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO") // TODO
				),
				mIL_AST.tCommandNodeType.TypeMethod => mStd.Call(
					() => {
						var ObjType = TypeDef._2.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO"); // TODO
						var FuncType = TypeDef._3.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO"); // TODO
						
						mAssert.IsTrue(FuncType.IsProc(out var EmptyType, out var ArgType, out var ResType));
						mAssert.IsTrue(EmptyType.IsEmpty());
						
						return mVM_Type.Proc(ObjType, ArgType, ResType);
					}
				),
				mIL_AST.tCommandNodeType.TypeInterface => mVM_Type.Interface(
					TypeDef._2.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO"), // TODO
					TypeDef._3.ThenTry(_ => TypeMap.TryGet(_)).ThenTry(_ => Types_.TryGet(_)).AssertNotEmpty(() => "TODO") // TODO
				),
				mIL_AST.tCommandNodeType.TypeCond => throw new System.NotImplementedException(),
				_ => throw mError.Error("not implemented: " + TypeDef.NodeType),
			};
			Types_ = mStream.Concat(Types_, mStream.Stream(Type));
			TypeMap = TypeMap.Set(TypeDef._1, NextTypeIndex);
			NextTypeIndex += 1;
		}
		
		foreach (var (DefName, TypeName, Commands, _) in aModule.Defs) {
			#if MY_TRACE_IL
			
			aTrace(() => "§DEF " + DefName);
			
			#endif
			
			// TODO: set type if it known
			var NextIndex = Module.Count();
			ModuleMap = ModuleMap.Set(DefName, NextIndex);
			
			var DefType = TypeMap.TryGet(
				TypeName
			).ThenTry(
				_ => Types_.TryGet(_)
			).AssertNotEmpty(
				() => $"type '{TypeName}' not found"
			);
			
			if (DefType.IsRecursive(out var FreeVar, out var TempType)) {
				DefType = TempType;
				FreeVar.Refs[0] = TempType;
			}
			
			mAssert.IsTrue(DefType.IsProc(out var NullType, out var DefEnvType, out var DefProcType));
			
			if (DefProcType.IsGeneric(out var FreeType, out var InnerType)) {
				DefProcType = InnerType;
			}
			
			mAssert.IsTrue(
				DefProcType.IsProc(out var DefObjType, out var DefArgType, out var DefResType),
				$"expected proc but is: {DefProcType.ToText()}"
			);
			
			while (DefType.IsGeneric(out _, out var BodyType)) {
				DefType = BodyType;	
			}
			
			var NewProc = new mVM_Data.tProcDef<tPos>(DefType);
			
			Module = mStream.Concat(Module, mStream.Stream(NewProc));
			
			var Regs = mTreeMap.Tree<tText, tNat32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign(), [])
			.Set(mIL_AST.cEmptyValue, mVM_Data.cEmptyReg)
			.Set(mIL_AST.cOne, mVM_Data.cOneReg)
			.Set(mIL_AST.cFalse, mVM_Data.cFalseReg)
			.Set(mIL_AST.cTrue, mVM_Data.cTrueReg)
			//.Set(mIL_AST.cSelfFunc, TypeMap.TryGet(TypeName).AssertNotEmpty(Fail_))
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
			// self type
			.Push(mVM_Type.Type()) // mVM_Type.Empty()
			.Push(mVM_Type.Type()) // mVM_Type.Bool()
			.Push(mVM_Type.Type()) // mVM_Type.Int()
			.Push(mVM_Type.Type()) // mVM_Type.Type()
			.Push(DefEnvType)
			.Push(DefObjType)
			.Push(DefArgType)
			.Push(DefResType);
			
			mAssert.AreEquals(Types.Size - 1, NewProc._LastReg);
			
			var ReturnType = Types.Get(mVM_Data.cResReg);
			
			foreach (var Command in Commands) {
				tText Fail_(tText a) => $"{Command.Pos}: {Command.ToText()}\n{a}";
				
				#if MY_TRACE_IL
				
				aTrace(() => Command.ToText());
				aTrace(
					() => ("  :: " +
						Command._2.ThenTry(_ => Regs.TryGet(_)).Then(_ => Types.Get(_).ToText("\n  ")).ElseUse("") + " ; " + 
						Command._3.ThenTry(_ => Regs.TryGet(_)).Then(_ => Types.Get(_).ToText("\n  ")).ElseUse("")
					)
				);
				
				#endif
				
				switch (Command) {
					case { NodeType: mIL_AST.tCommandNodeType.Alias, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						Regs = Regs.Set(RegId1, Regs.GetOrThrow(RegId2, Command));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.CallFunc, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
						var ProcReg = Regs.GetOrThrow(RegId2, Command);
						var ArgReg = Regs.GetOrThrow(RegId3, Command);
						var ResType = mVM_Type.Infer(
							Types.Get(ProcReg),
							mVM_Type.Empty(),
							Types.Get(ArgReg),
							aTrace
						).AssertNotError(
							Fail_
						);
						Regs = Regs.Set(RegId1, NewProc.Call(Span, ProcReg, ArgReg));
						Types.Push(ResType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.CallProc, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
						var ObjMethodPair = Regs.GetOrThrow(RegId2, Command);
						mAssert.IsTrue(Types.Get(ObjMethodPair).IsPair(out var ObjType, out var MethType));
						
						var ArgReg  = Regs.GetOrThrow(RegId3, Command);
						var ResType = mVM_Type.Infer(
							MethType,
							ObjType,
							Types.Get(ArgReg),
							aTrace
						).AssertNotError(
							Fail_
						);
						Regs = Regs.Set(RegId1, NewProc.Exec(Span, ObjMethodPair, ArgReg));
						Types.Push(ResType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.Int, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						Regs = Regs.Set(RegId1, NewProc.Int(Span, tInt32.Parse(RegId2.AssertNotEmpty())));
						Types.Push(mVM_Type.Int());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.BoolAnd, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Bool());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Bool());
						Regs = Regs.Set(RegId1, NewProc.And(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.BoolOr, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Bool());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Bool());
						Regs = Regs.Set(RegId1, NewProc.Or(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.BoolXOr, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Bool());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Bool());
						Regs = Regs.Set(RegId1, NewProc.XOr(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsAreEq, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsAreEq(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsComp, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsComp(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Int());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsAdd, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsAdd(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Int());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsSub, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsSub(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Int());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsMul, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsMul(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Int());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsDiv, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(Reg1), mVM_Type.Int());
						mAssert.AreEquals(Types.Get(Reg2), mVM_Type.Int());
						Regs = Regs.Set(RegId1, NewProc.IntsDiv(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Int()));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.Pair, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						Regs = Regs.Set(RegId1, NewProc.Pair(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Pair(Types.Get(Reg1), Types.Get(Reg2)));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.First, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg  = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						mAssert.IsTrue(ArgType.IsPair(out var ResType, out var __), () => $"{Span} {RegId1} := FIRST {RegId2} :: {ArgType.ToText()}");
						Regs = Regs.Set(RegId1, NewProc.First(Span, ArgReg));
						Types.Push(ResType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.Second, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						mAssert.IsTrue(Types.Get(ArgReg).IsPair(out _, out  var ResType));
						Regs = Regs.Set(RegId1, NewProc.Second(Span, ArgReg));
						Types.Push(ResType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.PrefixApply, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Prefix = RegId2.AssertNotEmpty();
						var Reg = Regs.GetOrThrow(RegId3, Command);
						Regs = Regs.Set(RegId1, NewProc.AddPrefix(Span, Prefix.PrefixHash(), Reg)); // TODO: avoid Hash collisions
						Types.Push(mVM_Type.Prefix(Prefix, Types.Get(Reg)));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.PrefixRemove, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Prefix = RegId2.AssertNotEmpty();
						var Reg = Regs.GetOrThrow(RegId3, Command);
						mAssert.IsTrue(Types.Get(Reg).IsPrefix(Prefix, out var ResType), Span.ToString());
						Regs = Regs.Set(RegId1, NewProc.DelPrefix(Span, Prefix.PrefixHash(), Reg)); // TODO: avoid Hash collisions
						Types.Push(ResType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.AddField, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var OldRecordReg = Regs.GetOrThrow(RegId2, Command);
						var NewElementReg = Regs.GetOrThrow(RegId3, Command);
						Regs = Regs.Set(RegId1, NewProc.ExtendRec(Span, OldRecordReg, NewElementReg));
						Types.Push(mVM_Type.Record(Types.Get(OldRecordReg), Types.Get(NewElementReg)));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.GetField, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var RecordReg = Regs.GetOrThrow(RegId2, Command);
						var Key = RegId3.AssertNotEmpty();
						var RecordType = Types.Get(RecordReg);
						mAssert.IsTrue(RecordType.IsRecord(out var Fields));
						mAssert.IsTrue(
							Fields.TryGet(Key).IsSome(out var FieldType),
							$"""
							{Span} Unknown field '{Key}' in record [{Fields.ToStream().Map(__ => __.Key).Reduce("", (a1, a2) => a1 + "\n  " + a2)}
							]
							"""
						);
						Regs = Regs.Set(RegId1, NewProc.GetField(Span, RecordReg, Key.PrefixHash()));
						Types.Push(FieldType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.Assert, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var Reg1 = Regs.GetOrThrow(RegId1, Command);
						var Reg2 = Regs.GetOrThrow(RegId2, Command);
						// TODO
						NewProc.Assert(Span, Reg1, Reg2);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.ReturnIf, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var CondReg = Regs.GetOrThrow(RegId1, Command);
						var ResReg = Regs.GetOrThrow(RegId2, Command);
						
						var ResType = Types.Get(ResReg);
						
						// ResType.IsSubType(DefResType, mStd.cEmpty)
						ResType.IsSubType(mVM_Type.Set(DefResType, mVM_Type.Empty()), mStd.cEmpty) // TODO: remove workaround; see line above
						.AssertNotError(
							_ => (
								$"""
								{Span}: {_}
								{ResType.ToText()}
								!<
								{DefResType.ToText()}
								{Command.ToText()}
								"""
							)
						);
						
						NewProc.ReturnIf(Span, CondReg, ResReg);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.ReturnIfNotEmpty, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ResReg = Regs.GetOrThrow(RegId2, Command);
						var ResType = Types.Get(ResReg);
						
						//ResType.IsSubType(DefResType, mStd.cEmpty)
						//.ElseThrow(
						//	_ => (
						//		$"""
						//		{Command.Pos}
						//		{_}
						//		{ResType.ToText()}
						//		!<
						//		{DefResType.ToText()}
						//		{Command.ToText()}
						//		"""
						//	)
						//);
						
						var (_, ReturnSubset) = ResType.SplitBy(
							__ => __.IsEmpty()
						);
						
						if (ReturnSubset.IsSome(out var T)) {
							ReturnType = mVM_Type.Union(ReturnType, T);
						}
						
						NewProc.ReturnIfNotEmpty(Span, ResReg);
						Types.Set(ResReg, mVM_Type.Empty());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsEmpty, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType ) = ArgType.SplitBy(
							__ => __.IsEmpty()
						);
						
						mAssert.IsTrue(
							SuccessType.IsSome(out var SuccessType_),
							() => $"{Span} TRY_AS_Empty expects type with [] but is {ArgType.ToText()}"
						);
						
						if (FailureType.IsSome(out var FailureType_)) {
							ReturnType = mVM_Type.Union(ReturnType, FailureType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsEmpty(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsBool, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							_ => _.IsBool()
						);
						
						mAssert.IsTrue(
							SuccessType.IsSome(out var SuccessType_),
							() => $"{Span} TRY_AS_BOOL expects type with BOOL but is {ArgType.ToText()}"
						);
						
						if (FailureType.IsSome(out var T)) {
							ReturnType = mVM_Type.Union(ReturnType, T);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsBool(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsInt, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						
						var (MatchedType, RestType) = ArgType.SplitBy(
							_ => _.IsInt()
						);
						
						mAssert.IsTrue(
							MatchedType.IsSome(out var MatchedType_),
							() => $"{Span} TRY_AS_INT expects type with INT but is {ArgType.ToText()}"
						);
						
						if (RestType.IsSome(out var RestType_)) {
							ReturnType = mVM_Type.Union(ReturnType, RestType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsInt(Span, ArgReg));
						Types.Push(MatchedType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsType, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							_ => _.IsType() || _.IsTypeFunc()
						);
						
						mAssert.IsTrue(
							SuccessType.IsSome(out var SuccessType_),
							() => $"{Span} TRY_AS_TYPE expects type with TYPE but is {ArgType.ToText()}"
						);
						
						if (FailureType.IsSome(out var FailureType_)) {
							ReturnType = mVM_Type.Union(ReturnType, FailureType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsType(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryRemovePrefixFrom, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						var Prefix = RegId3.AssertNotEmpty();
						var ArgType = Types.Get(ArgReg);
						
						var (PrefixType, RestType) = ArgType.SplitBy(
							__ => __.IsPrefix(Prefix, out _)
						);
						
						mAssert.IsTrue(
							PrefixType.IsSome(out var PrefixType_),
							() => $"{Span} TRY_REMOVE expects type with prefix #{Prefix} but is {ArgType.ToText()}"
						);
						mAssert.IsTrue(
							PrefixType_.IsPrefix(Prefix, out var InnerType_),
							() => $"{Span} TRY_REMOVE expects type with prefix #{Prefix} but is {ArgType.ToText()}"
						);
						
						if (RestType.IsSome(out var RestType_)) {
							ReturnType = mVM_Type.Union(ReturnType, RestType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryRemovePrefixFrom(Span, Prefix.PrefixHash(), ArgReg));
						Types.Push(InnerType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsRecord, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							_ => _.IsRecord(out var _)
						);
						
						mAssert.IsTrue(
							SuccessType.IsSome(out var SuccessType_),
							() => $"{Span} TRY_AS_RECORD expects type with RECORD but is {ArgType.ToText()}"
						);
						
						if (FailureType.IsSome(out var FailureType_)) {
							ReturnType = mVM_Type.Union(ReturnType, FailureType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsRecord(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsPair, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						
						var (PairType, OtherType) = ArgType.SplitBy(
							__ => __.IsPair(out _, out _)
						);
						
						mAssert.IsTrue(
							PairType.IsSome(out var PairType_),
							() => $"{Span} TRY_AS_PAIR expects type with PAIR but is {ArgType.ToText()}"
						);
						
						if (OtherType.IsSome(out var OtherType_)) {
							ReturnType = mVM_Type.Union(ReturnType, OtherType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsPair(Span, ArgReg));
						Types.Push(PairType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsVar, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							__ => __.IsVar(out _)
						);
						
						mAssert.IsTrue(
							SuccessType.IsSome(out var SuccessType_),
							() => $"{Span} TRY_AS_VAR expects type with VAR but is {ArgType.ToText()}"
						);
						
						if (FailureType.IsSome(out var FailureType_)) {
							ReturnType = mVM_Type.Union(ReturnType, FailureType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsVar(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsRef, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							__ => __.IsRef(out _)
						);
						
						mAssert.IsTrue(
							SuccessType.IsSome(out var SuccessType_),
							() => $"{Span} TRY_AS_REF expects type with REF but is {ArgType.ToText()}"
						);
						
						if (FailureType.IsSome(out var FailureType_)) {
							DefResType = mVM_Type.Union(DefResType, FailureType_);
						}
						
						Types.Set(mVM_Data.cResReg, DefResType);
						
						Regs = Regs.Set(RegId1, NewProc.TryAsRef(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.VarDef, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var Reg = Regs.GetOrThrow(RegId2, Command);
						Regs = Regs.Set(RegId1, NewProc.VarDef(Span, Reg));
						Types.Push(mVM_Type.Var(Types.Get(Reg)));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.VarSet, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var VarReg = Regs.GetOrThrow(RegId1, Command);
						var ValReg = Regs.GetOrThrow(RegId2, Command);
						mAssert.AreEquals(
							Types.Get(VarReg),
							mVM_Type.Var(Types.Get(ValReg))
						);
						NewProc.VarSet(Span, VarReg, ValReg);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.VarGet, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var VarReg = Regs.GetOrThrow(RegId2, Command);
						mAssert.IsTrue(Types.Get(VarReg).IsVar(out var ResType));
						Regs = Regs.Set(RegId1, NewProc.VarGet(Span, VarReg));
						Types.Push(ResType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.DefRecProcs, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
						var FuncReg = Regs.GetOrThrow(RegId2, Command);
						var ArgReg = Regs.GetOrThrow(RegId3, Command);
						var ArgType = Types.Get(ArgReg);
						mAssert.IsTrue(Types.Get(FuncReg).IsProc(out var EmptyType, out var EnvType, out var RecTypeInOut));
						mAssert.IsTrue(RecTypeInOut.IsProc(out var EmptyType_, out var RecTypeIn, out var RecTypeOut));
						mAssert.AreEquals(RecTypeIn, RecTypeOut);
						mAssert.IsTrue(EmptyType_.IsEmpty(), () => $"{Span} {FuncReg} is not a Proc with Empty Env");
						mAssert.IsTrue(EmptyType.IsEmpty(), () => $"{Span} {FuncReg} is not a Proc with Empty Env");
						ArgType.IsSubType(EnvType, mStd.cEmpty).AssertNotError(_ => $"{Span}: {_}");
						EnvType.IsSubType(ArgType, mStd.cEmpty).AssertNotError(_ => $"{Span}: {_}");
						if (!RecTypeOut.IsProc(out _, out _, out _)) {
							var PairType = RecTypeInOut;
							while (!PairType.IsEmpty()) {
								mAssert.IsTrue(PairType.IsPair(out var ProcType, out PairType));
								mAssert.IsTrue(ProcType.IsProc(out _, out _, out _));
								mAssert.AreEquals(RecTypeIn, RecTypeOut);
							}
						}
						
						Regs = Regs.Set(RegId1, NewProc.DefRecProcs(Span, FuncReg, ArgReg));
						Types.Push(RecTypeOut);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypeCond, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
						throw new System.NotImplementedException(nameof(mIL_AST.tCommandNodeType.TypeCond));
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypeFunc, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
						var ArgTypeReg = Regs.GetOrThrow(RegId2, Command);
						var ResTypeReg = Regs.GetOrThrow(RegId3, Command);
						Regs = Regs.Set(RegId1, NewProc.TypeFunc(Span, ArgTypeReg, ResTypeReg));
						Types.Push(mVM_Type.Type());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypeMethod, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var ObjTypeReg = Regs.GetOrThrow(RegId2, Command);
						var FuncTypeReg = Regs.GetOrThrow(RegId3, Command);
						mAssert.IsTrue(Types.Get(FuncTypeReg).IsProc(out var EmptyType, out var ArgType, out var ResType));
						mAssert.AreEquals(EmptyType, mVM_Type.Empty());
						Regs = Regs.Set(RegId1, NewProc.TypeMeth(Span, ObjTypeReg, FuncTypeReg));
						Types.Push(mVM_Type.Type());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypePair, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Type1Reg = Regs.GetOrThrow(RegId2, Command);
						var Type2Reg = Regs.GetOrThrow(RegId3, Command);
						Regs = Regs.Set(RegId1, NewProc.TypePair(Span, Type1Reg, Type2Reg));
						Types.Push(mVM_Type.Type());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypePrefix, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Prefix = RegId2.AssertNotEmpty();
						var TypeReg = Regs.GetOrThrow(RegId3, Command);
						Regs = Regs.Set(RegId1, NewProc.TypePrefix(Span, Prefix.PrefixHash(), TypeReg)); // TODO: avoid Hash collisions
						Types.Push(mVM_Type.Type());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypeSet, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Type1Reg = Regs.GetOrThrow(RegId2, Command);
						var Type2Reg = Regs.GetOrThrow(RegId3, Command);
						Regs = Regs.Set(RegId1, NewProc.TypeSet(Span, Type1Reg, Type2Reg));
						Types.Push(mVM_Type.Type());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypeVar, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var TypeReg = Regs.GetOrThrow(RegId2, Command);
						Regs = Regs.Set(RegId1, NewProc.TypeVar(Span, TypeReg));
						Types.Push(mVM_Type.Type());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypeFree, Pos: var Span, _1: var RegId1 }: {
						Regs = Regs.Set(RegId1, NewProc.TypeFree(Span));
						Types.Push(mVM_Type.Type());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypeRecursive, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var FreeTypeReg = Regs.GetOrThrow(RegId2, Command);
						var TypeBodyReg = Regs.GetOrThrow(RegId3, Command);
						mAssert.AreEquals(Types.Get(FreeTypeReg), mVM_Type.Type(), null, _ => _.ToText());
						Regs = Regs.Set(RegId1, NewProc.TypeRecursive(Span, FreeTypeReg, TypeBodyReg));
						Types.Push(mVM_Type.Type());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypeInterface, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var FreeTypeReg = Regs.GetOrThrow(RegId2, Command);
						var TypeBodyReg = Regs.GetOrThrow(RegId3, Command);
						Regs = Regs.Set(RegId1, NewProc.TypeInterface(Span, FreeTypeReg, TypeBodyReg));
						Types.Push(mVM_Type.Type());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TypeGeneric, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var FreeTypeReg = Regs.GetOrThrow(RegId2, Command);
						var TypeBodyReg = Regs.GetOrThrow(RegId3, Command);
						Regs = Regs.Set(RegId1, NewProc.TypeGeneric(Span, FreeTypeReg, TypeBodyReg));
                                                Types.Push(mVM_Type.TypeFunc());
						break;
					}
					default: {
						throw mError.Error($"impossible  (missing: {Command.NodeType})");
					}
				}
				#if MY_TRACE_IL
				
				aTrace(() => "  => " + Regs.TryGet(Command._1).Then(_ => Types.Get(_).ToText("\n  ")).ElseUse("???"));
				
				#endif
				
				mAssert.AreEquals(Types.Size - 1, NewProc._LastReg);
			}
			Types.Set(mVM_Data.cResReg, ReturnType);
			mAssert.AreEquals(NewProc.Commands.Size, NewProc.PosList.Size);
		}
		#if MY_TRACE_IL
		//PrintILModule(aDefs, Module, _ => { aTrace(() => _); });
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
	
	public static tNat32 GetOrThrow<tPos>(
		this mTreeMap.tTree<tText, tNat32> aRegs,
		mMaybe.tMaybe<tText> aRegId,
		mIL_AST.tCommandNode<tPos> aCommand
	) => aRegs.TryGet(
		aRegId.AssertNotEmpty()
	).AssertNotEmpty(
		() => (
			$"""
			no '{aRegId}' defined
			{aCommand.Pos}: {aCommand.ToText()}
			"""
		)
	);
	
	public static tNat32 GetOrThrow<tPos>(
		this mTreeMap.tTree<tText, tNat32> aRegs,
		tText aRegId,
		mIL_AST.tCommandNode<tPos> aCommand
	) => aRegs.TryGet(aRegId).AssertNotEmpty(
		() => (
			$"""
			no '{aRegId}' defined
			{aCommand.Pos}: {aCommand.ToText()}
			"""
		)
	);
	
	public static void
	PrintILModule<tPos>(
		mStream.tStream<(tText, mVM_Type.tType, mStream.tStream<mIL_AST.tCommandNode<tPos>>)> aDefs,
		mStream.tStream<mVM_Data.tProcDef<tPos>> aModule,
		mStd.tAction<tText> aTrace
	) {
		foreach (var ((Name, _, Commands), VM_Def) in mStream.ZipShort(aDefs, aModule)) {
			var RegIndex = mVM_Data.cResReg;
			aTrace($"{Name} € {VM_Def.DefType.ToText()}:");
			foreach (var Command in Commands) {
				if (Command.NodeType >= mIL_AST.tCommandNodeType._BeginCommands_) {
					aTrace($"\t{Command.NodeType} {Command._1} {Command._2} {Command._3}:");
				} else {
					RegIndex += 1;
					aTrace($"\t({RegIndex}) {Command._1} := {Command.NodeType} {Command._2} {Command._3}:");
					try {
						aTrace($"\t\t€ {VM_Def.Types.Get(RegIndex).ToText("\n\t\t")}");
					} catch {
						aTrace($"\t\t€ ERROR: out of index");
					}
				}
			}
		}
	}
}
