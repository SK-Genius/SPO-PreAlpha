#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mAssert.cs
#:ref Common/mError.cs
#:ref Common/mMath.cs
#:ref Common/mMaybe.cs
#:ref Common/mResult.cs
#:ref Common/mStream.cs
#:ref Common/mTreeMap.cs
#:ref Common/mPerf.cs
#:ref Common/mArrayList.cs
#:ref mIL_AST.cs
#:ref mVM_Type.cs
#:ref mVM_Data.cs

//#define MY_TRACE_IL

public static class
mIL_GenerateOpcodes {
	public static readonly tText cEmptyType = "EMPTY_TYPE";
	public static readonly tText cIntType = "INT";
	public static readonly tText cAnyType = "ANY";
	public static readonly tText cTypeType = "TYPE";
	
	private static mVM_Type.tType
	CreateTypeExpression<tPos>(
		mIL_AST.tCommandNode<tPos> aCommand,
		mStd.tFunc<tText, mVM_Type.tType> aType
	) {
		// Type construction accepts types and declared generic signatures, never opaque type functions.
		if (aCommand._2.IsSome(out var A) && aCommand.NodeType is not (
			mIL_AST.tCommandNodeType.TypePrefix or
			mIL_AST.tCommandNodeType.TypeSig or
			mIL_AST.tCommandNodeType.TypeGenericApply
		)) {
			var Value = aType(A);
			mAssert.IsTrue(
				aCommand.NodeType is mIL_AST.tCommandNodeType.TypeGeneric or mIL_AST.tCommandNodeType.TypeSigHead
					? Value.KindType().IsType() : Value.IsSignature(),
				$"{aCommand}: expected type operand"
			);
		}
		if (aCommand._3.IsSome(out var B) && aCommand.NodeType is not mIL_AST.tCommandNodeType.TypeGeneric) {
			var Value = aType(B);
			mAssert.IsTrue(
				aCommand.NodeType is mIL_AST.tCommandNodeType.TypeGenericApply
					? Value.KindType().IsType() : Value.IsSignature(),
				$"{aCommand}: expected type operand"
			);
		}
		return aCommand.NodeType switch {
			mIL_AST.tCommandNodeType.TypeSig => mVM_Type.Sig(aType(aCommand._2.AssertNotEmpty()), aType(aCommand._3.AssertNotEmpty())),
			mIL_AST.tCommandNodeType.TypeGenericApply => aType(aCommand._2.AssertNotEmpty()).ApplyType(aType(aCommand._3.AssertNotEmpty())),
			mIL_AST.tCommandNodeType.TypeFunc => mVM_Type.Proc(
				mVM_Type.Empty(),
				aType(aCommand._2.AssertNotEmpty()),
				aType(aCommand._3.AssertNotEmpty())
			),
			mIL_AST.tCommandNodeType.TypePair => mVM_Type.Pair(
				aType(aCommand._2.AssertNotEmpty()),
				aType(aCommand._3.AssertNotEmpty())
			),
			mIL_AST.tCommandNodeType.TypeSet => mVM_Type.Set(
				aType(aCommand._2.AssertNotEmpty()),
				aType(aCommand._3.AssertNotEmpty())
			),
			mIL_AST.tCommandNodeType.TypePrefix => mVM_Type.Prefix(
				aCommand._2.AssertNotEmpty(),
				aType(aCommand._3.AssertNotEmpty())
			),
			mIL_AST.tCommandNodeType.TypeRecord => mVM_Type.Record(
				aType(aCommand._2.AssertNotEmpty()),
				aType(aCommand._3.AssertNotEmpty())
			),
			mIL_AST.tCommandNodeType.TypeFree => mVM_Type.Free(aCommand._1),
			mIL_AST.tCommandNodeType.TypeSigHead => mVM_Type.SigHead(aCommand._1, aType(aCommand._2.AssertNotEmpty())),
			mIL_AST.tCommandNodeType.TypeGeneric => mVM_Type.Generic(
				aType(aCommand._2.AssertNotEmpty()),
				aType(aCommand._3.AssertNotEmpty())
			),
			mIL_AST.tCommandNodeType.TypeRecursive => mVM_Type.Recursive(
				aType(aCommand._2.AssertNotEmpty()),
				aType(aCommand._3.AssertNotEmpty())
			),
			mIL_AST.tCommandNodeType.TypeVar => mVM_Type.Var(
				aType(aCommand._2.AssertNotEmpty())
			),
			mIL_AST.tCommandNodeType.TypeMethod => mStd.Call(
				() => {
					var ObjType = aType(aCommand._2.AssertNotEmpty());
					var FuncType = aType(aCommand._3.AssertNotEmpty());
					
					mAssert.IsTrue(FuncType.IsProc(out var EmptyType, out var ArgType, out var ResType));
					mAssert.IsTrue(EmptyType.IsEmpty());
					
					return mVM_Type.Proc(ObjType, ArgType, ResType);
				}
			),
			mIL_AST.tCommandNodeType.TypeInterface => mVM_Type.Interface(
				aType(aCommand._2.AssertNotEmpty()),
				aType(aCommand._3.AssertNotEmpty())
			),
			mIL_AST.tCommandNodeType.TypeCond => throw new System.NotImplementedException(),
			_ => throw mError.Error("not implemented: " + aCommand.NodeType),
		};
	}
	
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
				(cIntType, 2u),
				(cTypeType, 3u),
				(mIL_AST.cTrue, 4u),
				(mIL_AST.cFalse, 5u),
			]
		);
		
		var Types_ = mStream.Stream(
			mVM_Type.Empty(),
			mVM_Type.Any(),
			mVM_Type.Int(),
			mVM_Type.Type(),
			mVM_Type.True(),
			mVM_Type.False()
		);
		
		var NextTypeIndex = Types_.Count();
		foreach (var TypeDef in aModule.TypeDef) {
			if (
				TypeDef.NodeType is < mIL_AST.tCommandNodeType._BeginTypes_ or
				>= mIL_AST.tCommandNodeType._EndTypes_
			) {
				throw mError.Error($"{TypeDef.NodeType} is not a Type Command");
			}
			
			var Type = CreateTypeExpression(TypeDef, __ => Types_.TryGet(TypeMap.TryGet(__).AssertNotEmpty()).AssertNotEmpty());
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
				__ => Types_.TryGet(__)
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
			.Set(mIL_AST.cIntType, mVM_Data.cIntTypeReg)
			.Set(mIL_AST.cTypeType, mVM_Data.cTypeTypeReg)
			.Set(mIL_AST.cEnv, mVM_Data.cEnvReg)
			.Set(mIL_AST.cObj, mVM_Data.cObjReg)
			.Set(mIL_AST.cArg, mVM_Data.cArgReg)
			.Set(mIL_AST.cRes, mVM_Data.cResReg);
			
			var Types = NewProc.Types
			.Push(mVM_Type.Empty())
			.Push(mVM_Type.Int())
			.Push(mVM_Type.False())
			.Push(mVM_Type.True())
			// self type
			.Push(mVM_Type.Type()) // mVM_Type.Empty()
			.Push(mVM_Type.Type()) // mVM_Type.Int()
			.Push(mVM_Type.Type()) // mVM_Type.Type()
			.Push(DefEnvType)
			.Push(DefObjType)
			.Push(DefArgType)
			.Push(DefResType);
			
			mAssert.AreEquals(Types.Size - 1, NewProc._LastReg);
			
			var KnownValues = mTreeMap.Tree<tNat32, mVM_Data.tData>((A, B) => A.CompareTo(B).Sign(), [])
			.Set(mVM_Data.cEmptyTypeReg, mVM_Data.TypeEmpty())
			.Set(mVM_Data.cIntTypeReg, mVM_Data.TypeInt())
			.Set(mVM_Data.cTypeTypeReg, mVM_Data.TypeType())
			.Set(mVM_Data.cTrueReg, mVM_Data.Bool(true))
			.Set(mVM_Data.cFalseReg, mVM_Data.Bool(false));
			
			static mVM_Type.tType
			TypeExpressionValue(
				tText aId,
				tNat32 aReg,
				mArrayList.tArrayList<mVM_Type.tType> aTypes,
				ref mTreeMap.tTree<tNat32, mVM_Data.tData> aValues
			) {
				if (aValues.TryGet(aReg).IsSome(out var Value) && !Value.IsEmpty()) {
					return Value.TypeExpressionValue();
				}
				var Kind = aTypes.Get(aReg);
				mAssert.IsTrue(Kind.IsType() || Kind.IsTypeFunctionKind(), "expected type or type function");
				var Symbol = mVM_Type.Abstract(aId, Kind);
				aValues = aValues.Set(aReg, mVM_Data.TypeExpression(Symbol));
				return Symbol;
			}
			
			static tBool
			HasUnboundSigHead(
				mVM_Type.tType aExpression,
				mStream.tStream<mVM_Type.tType> aBoundHeads = default
			) {
				if (aExpression.Kind is mVM_Type.tKind.SigHead) {
					return !aBoundHeads.Any(__ => mStd.RefEq(__, aExpression));
				}
				if (aExpression.Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract) {
					return false;
				}
				if (aExpression.IsSig(out var Head, out var Body)) {
					return HasUnboundSigHead(Body, mStream.Stream(Head, aBoundHeads));
				}
				return aExpression.Kind is mVM_Type.tKind.Record
					? aExpression.Fields.ToStream().Any(__ => HasUnboundSigHead(__.Value, aBoundHeads))
					: mStream.Stream(aExpression.Refs).Any(__ => HasUnboundSigHead(__, aBoundHeads));
			}
			
			var ReturnType = Types.Get(mVM_Data.cResReg);
			
			foreach (var Command in Commands) {
				tText Fail_(tText a) => $"{Command.Pos}: {Command.ToText()}\n{a}";
				
				#if MY_TRACE_IL
				
				aTrace(() => Command.ToText());
				aTrace(
					() => ("  :: " +
						Command._2.ThenTry(__ => Regs.TryGet(__)).Then(__ => Types.Get(__).ToText("\n  ")).ElseUse("") + " ; " + 
						Command._3.ThenTry(__ => Regs.TryGet(__)).Then(__ => Types.Get(__).ToText("\n  ")).ElseUse("")
					)
				);
				
				#endif
				
				if (Command.NodeType is >= mIL_AST.tCommandNodeType._BeginTypes_ and < mIL_AST.tCommandNodeType._EndTypes_) {
					var Value = CreateTypeExpression(Command, __ => TypeExpressionValue(__, Regs.GetOrThrow(__, Command), Types, ref KnownValues));
					var A = Command._2.Match(__ => Regs.TryGet(__).ElseUse(0u), () => mVM_Data.cTypeTypeReg);
					var B = Command._3.Match(__ => Regs.TryGet(__).ElseUse(0u), () => 0u);
					var Reg = Command.NodeType switch {
						mIL_AST.tCommandNodeType.TypeFree => NewProc.TypeFree(Command.Pos),
						mIL_AST.tCommandNodeType.TypeSigHead => NewProc.TypeSigHead(Command.Pos, A),
						mIL_AST.tCommandNodeType.TypeSig => NewProc.TypeSig(Command.Pos, A, B),
						mIL_AST.tCommandNodeType.TypeGenericApply => NewProc.TypeGenericApply(Command.Pos, A, B),
						mIL_AST.tCommandNodeType.TypeFunc => NewProc.TypeFunc(Command.Pos, A, B),
						mIL_AST.tCommandNodeType.TypeMethod => NewProc.TypeMeth(Command.Pos, A, B),
						mIL_AST.tCommandNodeType.TypePair => NewProc.TypePair(Command.Pos, A, B),
						mIL_AST.tCommandNodeType.TypePrefix => NewProc.TypePrefix(Command.Pos, Command._2.AssertNotEmpty(), B),
						mIL_AST.tCommandNodeType.TypeRecord => NewProc.TypeRecord(Command.Pos, A, B),
						mIL_AST.tCommandNodeType.TypeSet => NewProc.TypeSet(Command.Pos, A, B),
						mIL_AST.tCommandNodeType.TypeVar => NewProc.TypeVar(Command.Pos, A),
						mIL_AST.tCommandNodeType.TypeRecursive => NewProc.TypeRecursive(Command.Pos, A, B),
						mIL_AST.tCommandNodeType.TypeGeneric => NewProc.TypeGeneric(Command.Pos, A, B),
						mIL_AST.tCommandNodeType.TypeInterface => NewProc.TypeInterface(Command.Pos, A, B),
						_ => throw mError.Error($"unsupported type command: {Command.NodeType}"),
					};
					Regs = Regs.Set(Command._1, Reg);
					Types.Push(Value.KindType());
					KnownValues = KnownValues.Set(Reg, mVM_Data.TypeExpression(Value));
					mAssert.AreEquals(Types.Size - 1, NewProc._LastReg);
					continue;
				}
				// A declaration of a SIG parameter is only usable inside its contract.
				foreach (var Operand in mStream.Stream(Command._2, Command._3)) {
					if (
						Operand.IsSome(out var Id) && Regs.TryGet(Id).IsSome(out var Reg) &&
						KnownValues.TryGet(Reg).IsSome(out var Value) &&
						Value._DataType is mVM_Data.tDataType.Type or mVM_Data.tDataType.TypeFunction or mVM_Data.tDataType.SigBinding
					) {
						mAssert.IsFalse(HasUnboundSigHead(Value.TypeExpressionValue()), Fail_("unbound SIG head used as a value"));
					}
				}
				switch (Command) {
					case { NodeType: mIL_AST.tCommandNodeType.Sig, _1: var Id, _2: var ContractId, _3: var PayloadId }: {
						var ContractReg = Regs.GetOrThrow(ContractId, Command);
						var PayloadReg = Regs.GetOrThrow(PayloadId, Command);
						var Contract = TypeExpressionValue(ContractId.AssertNotEmpty(), ContractReg, Types, ref KnownValues);
						mAssert.IsTrue(Contract.IsSig(out var Binder, out var BodyType), Fail_("expected SIG contract"));
						mAssert.IsTrue(Types.Get(PayloadReg).IsPair(out var HeadValue, out var Body), Fail_("expected SIG payload"));
						mAssert.IsTrue(KnownValues.TryGet(PayloadReg).AssertNotEmpty().IsPair(out var HeadData, out _));
						var Head = HeadData.TypeExpressionValue();
						mAssert.IsTrue(Head.KindType().SameType(Binder.KindType()), Fail_("wrong SIG head kind"));
						Body.IsSubType(BodyType.Substitute(Binder, Head), mStd.cEmpty).AssertNotError(Fail_);
						Regs = Regs.Set(Id, NewProc.Sig(Command.Pos, ContractReg, PayloadReg));
						Types.Push(Contract);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsSig, _1: var Id, _2: var InputId, _3: var TestId }: {
						var Input = Regs.GetOrThrow(InputId, Command);
						var Test = Regs.GetOrThrow(TestId, Command);
						var TestValue = KnownValues.TryGet(Test).AssertNotEmpty();
						var Expected = mMaybe.None<mVM_Type.tType>();
						if (TestValue.IsPair(out var Signature, out var Head)) {
							TestValue = Signature;
							Expected = Head.TypeExpressionValue();
						}
						var Contract = TestValue.TypeValue();
						mAssert.IsTrue(Contract.IsSig(out var Binder, out var Body), Fail_("expected SIG contract"));
						var Witness = Expected.ElseUse(mVM_Type.Abstract(Id, Binder.KindType()));
						mAssert.IsTrue(Witness.KindType().SameType(Binder.KindType()), Fail_("wrong SIG head kind"));
						Regs = Regs.Set(Id, NewProc.TryAsSig(Command.Pos, Input, Test));
						Types.Push(mVM_Type.Sig(Witness, Body.Substitute(Binder, Witness)));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.SigHead or mIL_AST.tCommandNodeType.SigBody, _1: var Id, _2: var InputId }: {
						var Input = Regs.GetOrThrow(InputId, Command);
						mAssert.IsTrue(Types.Get(Input).IsSig(out var Head, out var Body), Fail_("expected SIG"));
						if (Head.Kind is mVM_Type.tKind.SigHead) {
							var Witness = mVM_Type.Abstract(InputId.AssertNotEmpty(), Head.KindType());
							Body = Body.Substitute(Head, Witness);
							Head = Witness;
							Types.Set(Input, mVM_Type.Sig(Head, Body));
						}
						var IsHead = Command.NodeType is mIL_AST.tCommandNodeType.SigHead;
						Regs = Regs.Set(Id, IsHead ? NewProc.SigHead(Command.Pos, Input) : NewProc.SigBody(Command.Pos, Input));
						Types.Push(IsHead ? Head.KindType() : Body);
						if (IsHead) {
							KnownValues = KnownValues.Set(NewProc._LastReg, mVM_Data.TypeExpression(Head));
						}
						break;
					}
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
					case { NodeType: mIL_AST.tCommandNodeType.TryReturn, Pos: var Span, _1: var FuncId, _2: var ArgId, _3: var GuardId }: {
						var ProcReg = Regs.GetOrThrow(FuncId, Command);
						var ArgReg = Regs.GetOrThrow(ArgId, Command);
						var ArgType = Types.Get(ArgReg);
						var ProcType = Types.Get(ProcReg);
						
						while (ProcType.IsGeneric(out var GenericHead, out var GenericBody)) {
							ProcType = GenericBody.Substitute(GenericHead, mVM_Type.Free());
						}
						
						mAssert.IsTrue(
							ProcType.IsProc(out var ObjType, out var ExpectedArgType, out var ResultType),
							Fail_("§TRY_RETURN expects a function")
						);
						mAssert.IsTrue(
							ObjType.IsEmpty(),
							Fail_("§TRY_RETURN does not accept a method")
						);
						mAssert.IsTrue(
							ExpectedArgType.IsSubType(ArgType, mStd.cEmpty).Match(out _, out _),
							Fail_("§TRY_RETURN function argument type cannot match the argument")
						);
						
						var (_, SuccessResult) = ResultType.SplitBy(__ => __.IsEmpty());
						
						mAssert.IsTrue(
							SuccessResult.IsSome(out var SuccessType) && SuccessType.IsPrefix("Result", out _),
							Fail_("§TRY_RETURN function result must contain #Result")
						);
						
						SuccessType.IsSubType(DefResType, mStd.cEmpty).AssertNotError(Fail_);
						
						var ProcOrProcGuardPairReg = ProcReg;
						if (GuardId.IsSome(out var GuardId_)) {
							var GuardReg = Regs.GetOrThrow(GuardId_, Command);
							var GuardType = Types.Get(GuardReg);
							
							while (GuardType.IsGeneric(out _, out var GenericBody)) {
								GuardType = GenericBody;
							}
							
							mAssert.IsTrue(
								GuardType.IsProc(out var GuardObjType, out var GuardArgType, out var GuardResultType),
								Fail_("§TRY_RETURN guard expects a function")
							);
							mAssert.IsTrue(
								GuardObjType.IsEmpty(),
								Fail_("§TRY_RETURN guard does not accept a method")
							);
							ExpectedArgType.IsSubType(GuardArgType, mStd.cEmpty).AssertNotError(Fail_);
							GuardResultType.IsSubType(mVM_Type.Bool(), mStd.cEmpty).AssertNotError(Fail_);
							
							ProcOrProcGuardPairReg = NewProc.Pair(Span, ProcReg, GuardReg);
							Types.Push(mVM_Type.Pair(ProcType, GuardType));
						}
						
						var ResReg = NewProc.TryReturn(Span, ProcOrProcGuardPairReg, ArgReg);
						Types.Push(ResultType);
						NewProc.ReturnIfNotEmpty(Span, ResReg);
						Types.Set(ResReg, mVM_Type.Empty());
						ReturnType = mVM_Type.Union(ReturnType, SuccessType);
						
						if (
							GuardId.IsNone() &&
							ArgType.Subtract(ExpectedArgType).IsSome(out var RemainingArgType)
						) {
							Types.Set(ArgReg, RemainingArgType);
						}
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
						mAssert.IsTrue(Types.Get(Reg1).IsSubType(mVM_Type.Bool(), mStd.cEmpty).Match(out _, out _));
						mAssert.IsTrue(Types.Get(Reg2).IsSubType(mVM_Type.Bool(), mStd.cEmpty).Match(out _, out _));
						Regs = Regs.Set(RegId1, NewProc.And(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.BoolOr, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.IsTrue(Types.Get(Reg1).IsSubType(mVM_Type.Bool(), mStd.cEmpty).Match(out _, out _));
						mAssert.IsTrue(Types.Get(Reg2).IsSubType(mVM_Type.Bool(), mStd.cEmpty).Match(out _, out _));
						Regs = Regs.Set(RegId1, NewProc.Or(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.BoolXOr, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Reg1 = Regs.GetOrThrow(RegId2, Command);
						var Reg2 = Regs.GetOrThrow(RegId3, Command);
						mAssert.IsTrue(Types.Get(Reg1).IsSubType(mVM_Type.Bool(), mStd.cEmpty).Match(out _, out _));
						mAssert.IsTrue(Types.Get(Reg2).IsSubType(mVM_Type.Bool(), mStd.cEmpty).Match(out _, out _));
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
						if (Types.Get(Reg1).IsType() || Types.Get(Reg1).IsTypeFunctionKind()) {
							TypeExpressionValue(RegId2.AssertNotEmpty(), Reg1, Types, ref KnownValues);
						}
						Regs = Regs.Set(RegId1, NewProc.Pair(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Pair(Types.Get(Reg1), Types.Get(Reg2)));
						var Left = KnownValues.TryGet(Reg1).ElseUse(mVM_Data.Empty());
						var Right = KnownValues.TryGet(Reg2).ElseUse(mVM_Data.Empty());
						if (!Left.IsEmpty() || !Right.IsEmpty()) {
							KnownValues = KnownValues.Set(NewProc._LastReg, mVM_Data.Pair(Left, Right));
						}
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.First, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg  = Regs.GetOrThrow(RegId2, Command);
						var ArgType = Types.Get(ArgReg);
						mAssert.IsTrue(ArgType.TryProjectPair(out var ResType, out _), () => $"{Span} {RegId1} := FIRST {RegId2} :: {ArgType.ToText()}");
						Regs = Regs.Set(RegId1, NewProc.First(Span, ArgReg));
						Types.Push(ResType);
						if (KnownValues.TryGet(ArgReg).IsSome(out var Pair) && Pair.IsPair(out var First, out var Second)) {
							KnownValues = KnownValues.Set(NewProc._LastReg, First);
						}
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.Second, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						var ArgReg = Regs.GetOrThrow(RegId2, Command);
						mAssert.IsTrue(Types.Get(ArgReg).TryProjectPair(out _, out var ResType));
						Regs = Regs.Set(RegId1, NewProc.Second(Span, ArgReg));
						Types.Push(ResType);
						if (KnownValues.TryGet(ArgReg).IsSome(out var Pair) && Pair.IsPair(out var First, out var Second)) {
							KnownValues = KnownValues.Set(NewProc._LastReg, Second);
						}
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
							__ => (
								$"""
								{Span}: {__}
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
							__ => __.Kind is mVM_Type.tKind.True or mVM_Type.tKind.False
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
							__ => __.IsInt()
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
							__ => __.IsType()
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
							__ => __.IsRecord(out var _)
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
						ArgType.IsSubType(EnvType, mStd.cEmpty).AssertNotError(__ => $"{Span}: {__}");
						EnvType.IsSubType(ArgType, mStd.cEmpty).AssertNotError(__ => $"{Span}: {__}");
						var Result = RecTypeOut;
						while (Result.IsGeneric(out _, out var GenericBody)) {
							Result = GenericBody;
						}
						if (!Result.IsProc(out _, out _, out _)) {
							while (!Result.IsEmpty()) {
								mAssert.IsTrue(Result.IsPair(out Result, out var ProcType));
								while (ProcType.IsGeneric(out _, out var GenericBody)) {
									ProcType = GenericBody;
								}
								mAssert.IsTrue(ProcType.IsProc(out _, out _, out _));
							}
						}
						
						Regs = Regs.Set(RegId1, NewProc.DefRecProcs(Span, FuncReg, ArgReg));
						Types.Push(RecTypeOut);
						break;
					}
					default: {
						throw mError.Error($"impossible  (missing: {Command.NodeType})");
					}
				}
				#if MY_TRACE_IL
				
				aTrace(() => "  => " + Regs.TryGet(Command._1).Then(__ => Types.Get(__).ToText("\n  ")).ElseUse("???"));
				
				#endif
				
				mAssert.AreEquals(Types.Size - 1, NewProc._LastReg);
			}
			Types.Set(mVM_Data.cResReg, ReturnType);
			mAssert.AreEquals(NewProc.Commands.Size, NewProc.PosList.Size);
		}
		#if MY_TRACE_IL
		//PrintILModule(aDefs, Module, __ => { aTrace(() => __); });
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
