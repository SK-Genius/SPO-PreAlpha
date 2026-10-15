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
#:ref Common/mFS.cs
#:ref Common/mSpan.cs
#:ref Common/mSpan.cs
#:ref Common/mTextStream.cs
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
	
	private static mResult.tResult<mVM_Type.tType, tText>
	CreateTypeExpression<tPos>(
		mIL_AST.tCommandNode<tPos> aCommand,
		mStd.tFunc<tText, mResult.tResult<mVM_Type.tType, tText>> aType,
		mStd.tFunc<tText, tText> aError
	) {
		mVM_Type.tType Type2 = null!;
		if (aCommand._2.IsSome(out var Id2) && aCommand.NodeType is not mIL_AST.tCommandNodeType.TypePrefix) {
			if (aType(Id2).IsFail(out var Error, out Type2)) {
				return Error;
			}
		}
		
		mVM_Type.tType Type3 = null!;
		if (aCommand._3.IsSome(out var Id3)) {
			if (aType(Id3).IsFail(out var Error, out Type3)) {
				return Error;
			}
		}
		
		// Type construction accepts types and declared generic signatures, never opaque type functions.
		if (
			aCommand._2.IsSome(out _) &&
			aCommand.NodeType is not (
				mIL_AST.tCommandNodeType.TypePrefix or
				mIL_AST.tCommandNodeType.TypeSig or
				mIL_AST.tCommandNodeType.TypeGenericApply
			)
		) {
			var Value = Type2;
			if (
				!(
					aCommand.NodeType switch {
						mIL_AST.tCommandNodeType.TypeFree or mIL_AST.tCommandNodeType.TypeAbstract => (
							Value.KindType().IsType()
						),
						mIL_AST.tCommandNodeType.TypeGeneric => Value.Kind is mVM_Type.tKind.Free,
						_ => Value.IsSignature(),
					}
				)
			) {
				return mResult.Fail(aError("expected type operand"));
			}
		}
		if (aCommand._3.IsSome(out _) && aCommand.NodeType is not mIL_AST.tCommandNodeType.TypeGeneric) {
			var Value = Type3;
			if (
				!(
					aCommand.NodeType is mIL_AST.tCommandNodeType.TypeGenericApply
					? (Value.KindType().IsType() || Value.KindType().IsTypeFunctionKind())
					: Value.IsSignature()
				)
			) {
				return mResult.Fail(aError("expected type operand"));
			}
		}
		if (aCommand.NodeType is mIL_AST.tCommandNodeType.TypeMethod) {
			if (!Type3.IsProc(out var EmptyType, out var ArgType, out var ResType)) {
				return mResult.Fail(aError("expected function type"));
			}
			if (!EmptyType.IsEmpty()) {
				return mResult.Fail(aError("expected function type without object"));
			}
			return mVM_Type.Proc(Type2, ArgType, ResType);
		}
		if (
			aCommand.NodeType is mIL_AST.tCommandNodeType.TypeGenericApply && !(
				Type2.KindType().IsProc(out var Obj, out var Arg, out _) &&
				Obj.IsEmpty() &&
				Arg.SameType(Type3.KindType())
			)
		) {
			return mResult.Fail(aError($"invalid type application: {Type2} to {Type3}"));
		}
		
		return aCommand.NodeType switch {
			mIL_AST.tCommandNodeType.TypeSig => mVM_Type.Sig(Type2, Type3),
			mIL_AST.tCommandNodeType.TypeGenericApply => Type2.ApplyType(Type3),
			mIL_AST.tCommandNodeType.TypeFunc => mVM_Type.Proc(mVM_Type.Empty(), Type2, Type3),
			mIL_AST.tCommandNodeType.TypePair => mVM_Type.Pair(Type2, Type3),
			mIL_AST.tCommandNodeType.TypeSet => mVM_Type.Set(Type2, Type3),
			mIL_AST.tCommandNodeType.TypePrefix => mVM_Type.Prefix(aCommand._2.AssertNotEmpty(), Type3),
			mIL_AST.tCommandNodeType.TypeRecord => mVM_Type.Record(Type2, Type3),
			mIL_AST.tCommandNodeType.TypeFree => mVM_Type.Free(aCommand._1, Type2),
			mIL_AST.tCommandNodeType.TypeAbstract => mVM_Type.Abstract(aCommand._1, Type2),
			mIL_AST.tCommandNodeType.TypeGeneric => mVM_Type.Generic(Type2, Type3),
			mIL_AST.tCommandNodeType.TypeRecursive => mVM_Type.Recursive(Type2, Type3),
			mIL_AST.tCommandNodeType.TypeVar => mVM_Type.Var(Type2),
			mIL_AST.tCommandNodeType.TypeInterface => mVM_Type.Interface(Type2, Type3),
			mIL_AST.tCommandNodeType.TypeCond => throw new System.NotImplementedException(),
			_ => throw mError.Error("not implemented: " + aCommand.NodeType),
		};
	}
	
	public static mResult.tResult<
		(
			mStream.tStream<mVM_Data.tProcDef<tPos>> Module,
			mTreeMap.tTree<tText, tNat32> ModuleMap
		),
		tText
	>
	GenerateOpcodes<tPos>(
		mIL_AST.tModule<tPos> aModule,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		using var _m_ = mPerf.Measure();
		
		#if MY_TRACE_IL
			aTrace(() => nameof(GenerateOpcodes));
		#endif
		
		tText FormatError(tPos aPos, tText aError) {
			if (aPos is mSpan.tSpan<mTextStream.tPos> Span && !string.IsNullOrEmpty(Span.Start.Id)) {
				var File = mFS.File(Span.Start.Id);
				if (File.Exists()) {
					return (Span.Start, aError).ToText(File.TryReadText().ElseThrow().Split('\n'));
				}
			}
			return $"{aPos}: {aError}";
		}
		
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
			
			var TypeResult = CreateTypeExpression(
				TypeDef,
				aId => TypeMap.TryGet(
					aId
				).ElseFail(
					() => FormatError(TypeDef.Pos, $"type '{aId}' not found")
				).Then(
					aIndex => Types_.TryGet(aIndex).AssertNotEmpty()
				),
				aError => FormatError(TypeDef.Pos, aError)
			);
			if (TypeResult.IsFail(out var TypeError, out var Type)) {
				return TypeError;
			}
			Types_ = mStream.Concat(Types_, mStream.Stream(Type));
			TypeMap = TypeMap.Set(TypeDef._1, NextTypeIndex);
			NextTypeIndex += 1;
		}
		
		foreach (var Def in aModule.Defs) {
			var (DefName, TypeName, Commands, _) = Def;
			#if MY_TRACE_IL
			
			aTrace(() => "§DEF " + DefName);
			
			#endif
			
			// TODO: set type if it known
			var NextIndex = Module.Count();
			ModuleMap = ModuleMap.Set(DefName, NextIndex);
			
			var DefTypeResult = TypeMap.TryGet(
				TypeName
			).ThenTry(
				__ => Types_.TryGet(__)
			).ElseFail(
				() => FormatError(
					Commands.TryFirst().Then(a => a.Pos).ElseUse(default!),
					$"type '{TypeName}' not found"
				)
			);
			
			if (DefTypeResult.IsFail(out var DefError, out var DefType)) {
				return DefError;
			}
			
			if (DefType.IsRecursive(out var FreeVar, out var TempType)) {
				DefType = TempType;
				FreeVar.Refs[0] = TempType;
			}
			
			if (!DefType.IsProc(out var NullType, out var DefEnvType, out var DefProcType)) {
				return mResult.Fail(
					FormatError(
						Commands.TryFirst().Then(a => a.Pos).ElseUse(default!),
						"expected definition type"
					)
				);
			}
			
			if (DefProcType.IsGeneric(out var FreeType, out var InnerType)) {
				DefProcType = InnerType;
			}
			
			if (!DefProcType.IsProc(out var DefObjType, out var DefArgType, out var DefResType)) {
				return mResult.Fail(
					FormatError(
						Commands.TryFirst().Then(a => a.Pos).ElseUse(default!),
						$"expected proc but is: {DefProcType.ToText()}"
					)
				);
			}
			
			while (DefType.IsGeneric(out _, out var BodyType)) {
				DefType = BodyType;	
			}
			
			var NewProc = new mVM_Data.tProcDef<tPos>(DefType) { TypeDefinitions = Types_ };
			
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
			
			static mResult.tResult<mVM_Type.tType, tText>
			TypeExpressionValue(
				tText aId,
				tNat32 aReg,
				mArrayList.tArrayList<mVM_Type.tType> aTypes,
				ref mTreeMap.tTree<tNat32, mVM_Data.tData> aValues,
				mStd.tFunc<tText, tText> aError
			) {
				if (aValues.TryGet(aReg).IsSome(out var Value) && !Value.IsEmpty()) {
					return Value.TypeExpressionValue();
				}
				
				var Kind = aTypes.Get(aReg);
				if (!Kind.IsType() && !Kind.IsTypeFunctionKind()) {
					return mResult.Fail(aError("expected type or type function"));
				}
				
				var Symbol = mVM_Type.Abstract(aId, Kind);
				aValues = aValues.Set(aReg, mVM_Data.TypeExpression(Symbol));
				return Symbol;
			}
			
			static tBool
			HasUnboundParameter(
				mVM_Type.tType aExpression,
				mStream.tStream<mVM_Type.tType> aBoundParameters = default,
				mStream.tStream<mVM_Type.tType> aVisited = default
			) {
				if (
					aExpression.Kind is mVM_Type.tKind.Abstract ||
					aVisited.Any(__ => mStd.RefEq(__, aExpression))
				) {
					return false;
				}
				
				aVisited = mStream.Stream(aExpression, aVisited);
				
				return aExpression.Kind switch {
					mVM_Type.tKind.Free => (
						!aBoundParameters.Any(__ => mStd.RefEq(__, aExpression)) && (
							mStd.RefEq(aExpression, aExpression.Refs[0]) ||
							HasUnboundParameter(aExpression.Refs[0], aBoundParameters, aVisited)
						)
					),
					mVM_Type.tKind.Sig or
					mVM_Type.tKind.Generic or
					mVM_Type.tKind.Interface or
					mVM_Type.tKind.Recursive => HasUnboundParameter(
						aExpression.Refs[1],
						mStream.Stream(aExpression.Refs[0], aBoundParameters),
						aVisited
					),
					_ => (
						aExpression.Kind is mVM_Type.tKind.Record
						? aExpression.Fields.ToStream(
						).Any(
							__ => HasUnboundParameter(__.Value, aBoundParameters, aVisited)
						)
						: mStream.Stream(
							aExpression.Refs
						).Any(
							__ => HasUnboundParameter(__, aBoundParameters, aVisited)
						)
					),
				};
			}
			
			var ReturnType = Types.Get(mVM_Data.cResReg);
			
			foreach (var Command in Commands) {
				tText Fail_(tText __) => FormatError(Command.Pos, __);
				
				mResult.tResult<tNat32, tText> TryGetReg(mMaybe.tMaybe<tText> aId) {
					var Id = aId.AssertNotEmpty();
					
					if (Regs.TryGet(Id).IsSome(out var Existing)) {
						return Existing;
					}
					
					if (
						TypeMap.TryGet(
							Id
						).ElseFail(
							() => Fail_($"no '{Id}' defined")
						).IsFail(
							out var Error,
							out var TypeIndex
						)
					) {
						return Error;
					}
					
					var Value = Types_.TryGet(TypeIndex).AssertNotEmpty();
					if (HasUnboundParameter(Value)) {
						return mResult.Fail(Fail_("unbound type parameter used as a value"));
					}
					
					var Reg = NewProc.LoadType(Command.Pos, TypeIndex);
					Types.Push(Value.KindType());
					KnownValues = KnownValues.Set(Reg, mVM_Data.TypeExpression(Value));
					Regs = Regs.Set(Id, Reg);
					
					return Reg;
				}
				
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
					var ValueResult = CreateTypeExpression(
						Command,
						aId => TryGetReg(
							aId
						).ThenTry(
							aReg => TypeExpressionValue(aId, aReg, Types, ref KnownValues, Fail_)
						),
						Fail_
					);
					
					if (ValueResult.IsFail(out var Error, out var Value)) {
						return Error;
					}
					
					var A = Command._2.Match(__ => Regs.TryGet(__).ElseUse(0u), () => mVM_Data.cTypeTypeReg);
					var B = Command._3.Match(__ => Regs.TryGet(__).ElseUse(0u), () => 0u);
					var Reg = Command.NodeType switch {
						mIL_AST.tCommandNodeType.TypeFree => NewProc.TypeFree(Command.Pos, A),
						mIL_AST.tCommandNodeType.TypeAbstract => NewProc.TypeAbstract(Command.Pos, A),
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
				
				// Type parameters are only usable as values inside a complete bound type.
				foreach (
					var Operand in Command.NodeType is
					mIL_AST.tCommandNodeType.TryAsSig or mIL_AST.tCommandNodeType.SigHeadAs
					? mStream.Stream(Command._2)
					: mStream.Stream(Command._2, Command._3)
				) {
					if (
						Operand.IsSome(out var Id) && Regs.TryGet(Id).IsSome(out var Reg) &&
						KnownValues.TryGet(Reg).IsSome(out var Value) &&
						Value._DataType is mVM_Data.tDataType.Type
					) {
						if (HasUnboundParameter(Value.TypeExpressionValue())) {
							return mResult.Fail(Fail_("unbound type parameter used as a value"));
						}
					}
				}
				
				switch (Command) {
					case { NodeType: mIL_AST.tCommandNodeType.Sig, _1: var Id, _2: var ContractId, _3: var PayloadId }: {
						if (TryGetReg(ContractId).IsFail(out var ContractRegError, out var ContractReg)) {
							return ContractRegError;
						}
						if (TryGetReg(PayloadId).IsFail(out var PayloadRegError, out var PayloadReg)) {
							return PayloadRegError;
						}
						if (TypeExpressionValue(ContractId.AssertNotEmpty(), ContractReg, Types, ref KnownValues, Fail_).IsFail(out var ContractError, out var Contract)) {
							return ContractError;
						}
						if (!Contract.IsSig(out var Binder, out var BodyType)) {
							return mResult.Fail(Fail_("expected SIG contract"));
						}
						if (!Types.Get(PayloadReg).IsPair(out var HeadValue, out var Body)) {
							return mResult.Fail(Fail_("expected SIG payload"));
						}
						
						mAssert.IsTrue(KnownValues.TryGet(PayloadReg).AssertNotEmpty().IsPair(out var HeadData, out _));
						var Head = HeadData.TypeExpressionValue();
						if (!Head.KindType().SameType(Binder.KindType())) {
							return mResult.Fail(Fail_("wrong SIG head kind"));
						}
						if (
							Body.IsSubType(
								BodyType.Substitute(Binder, Head)
							).ModifyError(
								Fail_
							).IsFail(
								out var PayloadError,
								out _
							)
						) {
							return PayloadError;
						}
						
						Regs = Regs.Set(Id, NewProc.Sig(Command.Pos, ContractReg, PayloadReg));
						Types.Push(Contract);
						break;
					}
					case {
						NodeType: mIL_AST.tCommandNodeType.TryAsSig,
						_1: var Id,
						_2: var InputId,
						_3: var ContractId
					}: {
						if (TryGetReg(InputId).IsFail(out var InputError, out var Input)) {
							return InputError;
						}
						if (
							TypeMap.TryGet(
								ContractId.AssertNotEmpty()
							).ElseFail(
								() => Fail_($"type '{ContractId}' not found")
							).IsFail(
								out var Error,
								out var TypeIndex
							)
						) {
							return Error;
						}
						
						var Contract = Types_.TryGet(TypeIndex).AssertNotEmpty();
						if (!Contract.IsSig(out var Binder, out var Body)) {
							return mResult.Fail(Fail_("expected SIG contract"));
						}
						if (!(Binder.Kind is mVM_Type.tKind.Free)) {
							return mResult.Fail(Fail_("SIG contract head must be FREE"));
						}
						if (HasUnboundParameter(Contract)) {
							return mResult.Fail(Fail_("unbound type parameter in SIG contract"));
						}
						
						var Witness = mVM_Type.Abstract(Id, Binder.KindType());
						Regs = Regs.Set(Id, NewProc.TryAsSig(Command.Pos, Input, TypeIndex));
						Types.Push(mVM_Type.Sig(Witness, Body.Substitute(Binder, Witness)));
						
						break;
					}
					case {
						NodeType: mIL_AST.tCommandNodeType.TryHasHeadType,
						_1: var Id,
						_2: var InputId,
						_3: var ExpectedId
					}: {
						if (TryGetReg(InputId).IsFail(out var InputError, out var Input)) {
							return InputError;
						}
						if (TryGetReg(ExpectedId).IsFail(out var ExpectedRegError, out var ExpectedReg)) {
							return ExpectedRegError;
						}
						if (!Types.Get(Input).IsSig(out var Head, out var Body)) {
							return mResult.Fail(Fail_("expected SIG"));
						}
						if (
							TypeExpressionValue(
								ExpectedId.AssertNotEmpty(),
								ExpectedReg,
								Types,
								ref KnownValues,
								Fail_
							).IsFail(
								out var ExpectedError,
								out var Expected
							)
						) {
							return ExpectedError;
						}
						if (!Head.KindType().SameType(Expected.KindType())) {
							return mResult.Fail(Fail_("wrong SIG head kind"));
						}
						
						Regs = Regs.Set(Id, NewProc.TryHasHeadType(Command.Pos, Input, ExpectedReg));
						Types.Push(mVM_Type.Sig(Expected, Body.Substitute(Head, Expected)));
						
						break;
					}
					case {
						NodeType: mIL_AST.tCommandNodeType.SigHead or
						mIL_AST.tCommandNodeType.SigHeadAs or mIL_AST.tCommandNodeType.SigBody,
						_1: var Id,
						_2: var InputId
					}: {
						if (TryGetReg(InputId).IsFail(out var InputError, out var Input)) {
							return InputError;
						}
						if (!Types.Get(Input).IsSig(out var Head, out var Body)) {
							return mResult.Fail(Fail_("expected SIG"));
						}
						if (Command.NodeType is mIL_AST.tCommandNodeType.SigHeadAs) {
							var Witness = Types_.TryGet(
								TypeMap.TryGet(Command._3.AssertNotEmpty()).AssertNotEmpty()
							).AssertNotEmpty();
							if (!(Witness.Kind is mVM_Type.tKind.Abstract)) {
								return mResult.Fail(Fail_("expected abstract SIG witness"));
							}
							if (!(Head.Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract &&
								Head.KindType().SameType(Witness.KindType()))) {
								return mResult.Fail(Fail_("wrong SIG witness kind"));
							}
							Body = Body.Substitute(Head, Witness);
							Head = Witness;
							Types.Set(Input, mVM_Type.Sig(Head, Body));
						} else if (Head.Kind is mVM_Type.tKind.Free) {
							var Witness = mVM_Type.Abstract(InputId.AssertNotEmpty(), Head.KindType());
							Body = Body.Substitute(Head, Witness);
							Head = Witness;
							Types.Set(Input, mVM_Type.Sig(Head, Body));
						}
						
						var IsHead = Command.NodeType is mIL_AST.tCommandNodeType.SigHead or mIL_AST.tCommandNodeType.SigHeadAs;
						Regs = Regs.Set(
							Id,
							IsHead
							? NewProc.SigHead(Command.Pos, Input)
							: NewProc.SigBody(Command.Pos, Input)
						);
						Types.Push(IsHead ? Head.KindType() : Body);
						
						if (IsHead) {
							KnownValues = KnownValues.Set(NewProc._LastReg, mVM_Data.TypeExpression(Head));
						}
						
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.Alias, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var Error, out var Reg)) {
							return Error;
						}
						Regs = Regs.Set(RegId1, Reg);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.CallFunc, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var ProcRegError, out var ProcReg)) {
							return ProcRegError;
						}
						if (TryGetReg(RegId3).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						if (
							mVM_Type.Infer(
								Types.Get(ProcReg),
								mVM_Type.Empty(),
								Types.Get(ArgReg),
								aTrace
							).ModifyError(
								Fail_
							).IsFail(
								out var ResTypeError,
								out var ResType
							)
						) {
							return ResTypeError;
						}
						Regs = Regs.Set(RegId1, NewProc.Call(Span, ProcReg, ArgReg));
						Types.Push(ResType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryReturn, Pos: var Span, _1: var FuncId, _2: var ArgId, _3: var GuardId }: {
						if (TryGetReg(FuncId).IsFail(out var ProcRegError, out var ProcReg)) {
							return ProcRegError;
						}
						if (TryGetReg(ArgId).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						var ProcType = Types.Get(ProcReg);
						
						while (ProcType.IsGeneric(out var GenericHead, out var GenericBody)) {
							ProcType = GenericBody.Substitute(GenericHead, mVM_Type.Free(GenericHead.KindType()));
						}
						
						if (!ProcType.IsProc(out var ObjType, out var ExpectedArgType, out var ResultType)) {
							return mResult.Fail(Fail_("§TRY_RETURN expects a function"));
						}
						if (!ObjType.IsEmpty()) {
							return mResult.Fail(Fail_("§TRY_RETURN does not accept a method"));
						}
						if (!ExpectedArgType.IsSubType(ArgType).Match(out _, out _)) {
							return mResult.Fail(Fail_("§TRY_RETURN function argument type cannot match the argument"));
						}
						
						var (_, SuccessResult) = ResultType.SplitBy(__ => __.IsEmpty());
						
						if (!(SuccessResult.IsSome(out var SuccessType) && SuccessType.IsPrefix("Result", out _))) {
							return mResult.Fail(Fail_("§TRY_RETURN function result must contain #Result"));
						}
						
						if (SuccessType.IsSubType(DefResType).ModifyError(Fail_).IsFail(out var ResultError, out _)) {
							return ResultError;
						}
						
						var ProcOrProcGuardPairReg = ProcReg;
						if (GuardId.IsSome(out var GuardId_)) {
							if (TryGetReg(GuardId_).IsFail(out var GuardRegError, out var GuardReg)) {
								return GuardRegError;
							}
							var GuardType = Types.Get(GuardReg);
							
							while (GuardType.IsGeneric(out _, out var GenericBody)) {
								GuardType = GenericBody;
							}
							
							if (!GuardType.IsProc(out var GuardObjType, out var GuardArgType, out var GuardResultType)) {
								return mResult.Fail(Fail_("§TRY_RETURN guard expects a function"));
							}
							if (!GuardObjType.IsEmpty()) {
								return mResult.Fail(Fail_("§TRY_RETURN guard does not accept a method"));
							}
							if (ExpectedArgType.IsSubType(GuardArgType).ModifyError(Fail_).IsFail(out var GuardArgumentError, out _)) {
								return GuardArgumentError;
							}
							if (GuardResultType.IsSubType(mVM_Type.Bool()).ModifyError(Fail_).IsFail(out var GuardResultError, out _)) {
								return GuardResultError;
							}
							
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
						if (TryGetReg(RegId2).IsFail(out var ObjMethodPairError, out var ObjMethodPair)) {
							return ObjMethodPairError;
						}
						if (!Types.Get(ObjMethodPair).IsPair(out var ObjType, out var MethType)) {
							return mResult.Fail(Fail_("expected object/method pair"));
						}
						
						if (TryGetReg(RegId3).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						if (mVM_Type.Infer(
							MethType,
							ObjType,
							Types.Get(ArgReg),
							aTrace
						).ModifyError(Fail_).IsFail(out var ResTypeError, out var ResType)) {
							return ResTypeError;
						}
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
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (Types.Get(Reg1).IsSubType(mVM_Type.Bool()).ModifyError(Fail_).IsFail(out var FirstOperandError, out _)) {
							return FirstOperandError;
						}
						if (Types.Get(Reg2).IsSubType(mVM_Type.Bool()).ModifyError(Fail_).IsFail(out var SecondOperandError, out _)) {
							return SecondOperandError;
						}
						Regs = Regs.Set(RegId1, NewProc.And(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.BoolOr, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (Types.Get(Reg1).IsSubType(mVM_Type.Bool()).ModifyError(Fail_).IsFail(out var FirstOperandError, out _)) {
							return FirstOperandError;
						}
						if (Types.Get(Reg2).IsSubType(mVM_Type.Bool()).ModifyError(Fail_).IsFail(out var SecondOperandError, out _)) {
							return SecondOperandError;
						}
						Regs = Regs.Set(RegId1, NewProc.Or(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.BoolXOr, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (Types.Get(Reg1).IsSubType(mVM_Type.Bool()).ModifyError(Fail_).IsFail(out var FirstOperandError, out _)) {
							return FirstOperandError;
						}
						if (Types.Get(Reg2).IsSubType(mVM_Type.Bool()).ModifyError(Fail_).IsFail(out var SecondOperandError, out _)) {
							return SecondOperandError;
						}
						Regs = Regs.Set(RegId1, NewProc.XOr(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsAreEq, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (!Equals(Types.Get(Reg1), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg1).ToText()}"));
						}
						if (!Equals(Types.Get(Reg2), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg2).ToText()}"));
						}
						Regs = Regs.Set(RegId1, NewProc.IntsAreEq(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Bool());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsComp, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (!Equals(Types.Get(Reg1), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg1).ToText()}"));
						}
						if (!Equals(Types.Get(Reg2), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg2).ToText()}"));
						}
						Regs = Regs.Set(RegId1, NewProc.IntsComp(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Int());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsAdd, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (!Equals(Types.Get(Reg1), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg1).ToText()}"));
						}
						if (!Equals(Types.Get(Reg2), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg2).ToText()}"));
						}
						Regs = Regs.Set(RegId1, NewProc.IntsAdd(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Int());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsSub, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (!Equals(Types.Get(Reg1), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg1).ToText()}"));
						}
						if (!Equals(Types.Get(Reg2), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg2).ToText()}"));
						}
						Regs = Regs.Set(RegId1, NewProc.IntsSub(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Int());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsMul, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (!Equals(Types.Get(Reg1), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg1).ToText()}"));
						}
						if (!Equals(Types.Get(Reg2), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg2).ToText()}"));
						}
						Regs = Regs.Set(RegId1, NewProc.IntsMul(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Int());
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.IntsDiv, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (!Equals(Types.Get(Reg1), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg1).ToText()}"));
						}
						if (!Equals(Types.Get(Reg2), mVM_Type.Int())) {
							return mResult.Fail(Fail_($"expected §INT but found {Types.Get(Reg2).ToText()}"));
						}
						Regs = Regs.Set(RegId1, NewProc.IntsDiv(Span, Reg1, Reg2));
						Types.Push(mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Int()));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.Pair, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId3).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						if (Types.Get(Reg1).IsType() || Types.Get(Reg1).IsTypeFunctionKind()) {
							if (
								TypeExpressionValue(
									RegId2.AssertNotEmpty(),
									Reg1,
									Types,
									ref KnownValues,
									Fail_
								).IsFail(
									out var ValueError,
									out _
								)
							) {
								return ValueError;
							}
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
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						if (!ArgType.TryProjectPair(out var ResType, out _)) {
							return mResult.Fail(Fail_($"{RegId1} := FIRST {RegId2} :: {ArgType.ToText()}"));
						}
						Regs = Regs.Set(RegId1, NewProc.First(Span, ArgReg));
						Types.Push(ResType);
						if (KnownValues.TryGet(ArgReg).IsSome(out var Pair) && Pair.IsPair(out var First, out var Second)) {
							KnownValues = KnownValues.Set(NewProc._LastReg, First);
						}
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.Second, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						if (!Types.Get(ArgReg).TryProjectPair(out _, out var ResType)) {
							return mResult.Fail(Fail_("expected pair"));
						}
						Regs = Regs.Set(RegId1, NewProc.Second(Span, ArgReg));
						Types.Push(ResType);
						if (KnownValues.TryGet(ArgReg).IsSome(out var Pair) && Pair.IsPair(out var First, out var Second)) {
							KnownValues = KnownValues.Set(NewProc._LastReg, Second);
						}
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.PrefixApply, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Prefix = RegId2.AssertNotEmpty();
						if (TryGetReg(RegId3).IsFail(out var RegError, out var Reg)) {
							return RegError;
						}
						Regs = Regs.Set(RegId1, NewProc.AddPrefix(Span, Prefix.PrefixHash(), Reg)); // TODO: avoid Hash collisions
						Types.Push(mVM_Type.Prefix(Prefix, Types.Get(Reg)));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.PrefixRemove, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						var Prefix = RegId2.AssertNotEmpty();
						if (TryGetReg(RegId3).IsFail(out var RegError, out var Reg)) {
							return RegError;
						}
						if (!Types.Get(Reg).IsPrefix(Prefix, out var ResType)) {
							return mResult.Fail(Fail_("wrong prefix"));
						}
						Regs = Regs.Set(RegId1, NewProc.DelPrefix(Span, Prefix.PrefixHash(), Reg)); // TODO: avoid Hash collisions
						Types.Push(ResType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.AddField, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var OldRecordRegError, out var OldRecordReg)) {
							return OldRecordRegError;
						}
						if (TryGetReg(RegId3).IsFail(out var NewElementRegError, out var NewElementReg)) {
							return NewElementRegError;
						}
						Regs = Regs.Set(RegId1, NewProc.ExtendRec(Span, OldRecordReg, NewElementReg));
						Types.Push(mVM_Type.Record(Types.Get(OldRecordReg), Types.Get(NewElementReg)));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.GetField, Pos: var Span, _1: var RegId1, _2: var RegId2 , _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var RecordRegError, out var RecordReg)) {
							return RecordRegError;
						}
						var Key = RegId3.AssertNotEmpty();
						var RecordType = Types.Get(RecordReg);
						if (!RecordType.IsRecord(out var Fields)) {
							return mResult.Fail(Fail_("expected record"));
						}
						if (!Fields.TryGet(Key).IsSome(out var FieldType)) {
							return mResult.Fail(Fail_($"""
							Unknown field '{Key}' in record [{Fields.ToStream().Map(__ => __.Key).Reduce("", (a1, a2) => a1 + "\n  " + a2)}
							]
							"""));
						}
						Regs = Regs.Set(RegId1, NewProc.GetField(Span, RecordReg, Key.PrefixHash()));
						Types.Push(FieldType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.Assert, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId1).IsFail(out var Reg1Error, out var Reg1)) {
							return Reg1Error;
						}
						if (TryGetReg(RegId2).IsFail(out var Reg2Error, out var Reg2)) {
							return Reg2Error;
						}
						// TODO
						NewProc.Assert(Span, Reg1, Reg2);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.ReturnIf, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId1).IsFail(out var CondRegError, out var CondReg)) {
							return CondRegError;
						}
						if (TryGetReg(RegId2).IsFail(out var ResRegError, out var ResReg)) {
							return ResRegError;
						}
						
						var ResType = Types.Get(ResReg);
						
						// ResType.IsSubType(DefResType)
						if (ResType.IsSubType(mVM_Type.Set(DefResType, mVM_Type.Empty())).ModifyError(Fail_).IsFail(out var ResultError, out _)) {
							return ResultError;
						}
						
						NewProc.ReturnIf(Span, CondReg, ResReg);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.ReturnIfNotEmpty, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var ResRegError, out var ResReg)) {
							return ResRegError;
						}
						var ResType = Types.Get(ResReg);
						
						//ResType.IsSubType(DefResType)
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
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType ) = ArgType.SplitBy(
							__ => __.IsEmpty()
						);
						
						if (!SuccessType.IsSome(out var SuccessType_)) {
							return mResult.Fail(Fail_($"TRY_AS_Empty expects type with [] but is {ArgType.ToText()}"));
						}
						
						if (FailureType.IsSome(out var FailureType_)) {
							ReturnType = mVM_Type.Union(ReturnType, FailureType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsEmpty(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsBool, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							__ => __.Kind is mVM_Type.tKind.True or mVM_Type.tKind.False
						);
						
						if (!SuccessType.IsSome(out var SuccessType_)) {
							return mResult.Fail(Fail_($"TRY_AS_BOOL expects type with BOOL but is {ArgType.ToText()}"));
						}
						
						if (FailureType.IsSome(out var T)) {
							ReturnType = mVM_Type.Union(ReturnType, T);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsBool(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsInt, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						
						var (MatchedType, RestType) = ArgType.SplitBy(
							__ => __.IsInt()
						);
						
						if (!MatchedType.IsSome(out var MatchedType_)) {
							return mResult.Fail(Fail_($"TRY_AS_INT expects type with INT but is {ArgType.ToText()}"));
						}
						
						if (RestType.IsSome(out var RestType_)) {
							ReturnType = mVM_Type.Union(ReturnType, RestType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsInt(Span, ArgReg));
						Types.Push(MatchedType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsType, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							__ => __.IsType()
						);
						
						if (!SuccessType.IsSome(out var SuccessType_)) {
							return mResult.Fail(Fail_($"TRY_AS_TYPE expects type with TYPE but is {ArgType.ToText()}"));
						}
						
						if (FailureType.IsSome(out var FailureType_)) {
							ReturnType = mVM_Type.Union(ReturnType, FailureType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsType(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryRemovePrefixFrom, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var Prefix = RegId3.AssertNotEmpty();
						var ArgType = Types.Get(ArgReg);
						
						var (PrefixType, RestType) = ArgType.SplitBy(
							__ => __.IsPrefix(Prefix, out _)
						);
						
						if (!PrefixType.IsSome(out var PrefixType_)) {
							return mResult.Fail(Fail_($"TRY_REMOVE expects type with prefix #{Prefix} but is {ArgType.ToText()}"));
						}
						if (!PrefixType_.IsPrefix(Prefix, out var InnerType_)) {
							return mResult.Fail(Fail_($"TRY_REMOVE expects type with prefix #{Prefix} but is {ArgType.ToText()}"));
						}
						
						if (RestType.IsSome(out var RestType_)) {
							ReturnType = mVM_Type.Union(ReturnType, RestType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryRemovePrefixFrom(Span, Prefix.PrefixHash(), ArgReg));
						Types.Push(InnerType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsRecord, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							__ => __.IsRecord(out var _)
						);
						
						if (!SuccessType.IsSome(out var SuccessType_)) {
							return mResult.Fail(Fail_($"TRY_AS_RECORD expects type with RECORD but is {ArgType.ToText()}"));
						}
						
						if (FailureType.IsSome(out var FailureType_)) {
							ReturnType = mVM_Type.Union(ReturnType, FailureType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsRecord(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsPair, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						
						var (PairType, OtherType) = ArgType.SplitBy(
							__ => __.IsPair(out _, out _)
						);
						
						if (!PairType.IsSome(out var PairType_)) {
							return mResult.Fail(Fail_($"TRY_AS_PAIR expects type with PAIR but is {ArgType.ToText()}"));
						}
						
						if (OtherType.IsSome(out var OtherType_)) {
							ReturnType = mVM_Type.Union(ReturnType, OtherType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsPair(Span, ArgReg));
						Types.Push(PairType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsVar, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							__ => __.IsVar(out _)
						);
						
						if (!SuccessType.IsSome(out var SuccessType_)) {
							return mResult.Fail(Fail_($"TRY_AS_VAR expects type with VAR but is {ArgType.ToText()}"));
						}
						
						if (FailureType.IsSome(out var FailureType_)) {
							ReturnType = mVM_Type.Union(ReturnType, FailureType_);
						}
						
						Regs = Regs.Set(RegId1, NewProc.TryAsVar(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.TryAsRef, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						
						var (SuccessType, FailureType) = ArgType.SplitBy(
							__ => __.IsRef(out _)
						);
						
						if (!SuccessType.IsSome(out var SuccessType_)) {
							return mResult.Fail(Fail_($"TRY_AS_REF expects type with REF but is {ArgType.ToText()}"));
						}
						
						if (FailureType.IsSome(out var FailureType_)) {
							DefResType = mVM_Type.Union(DefResType, FailureType_);
						}
						
						Types.Set(mVM_Data.cResReg, DefResType);
						
						Regs = Regs.Set(RegId1, NewProc.TryAsRef(Span, ArgReg));
						Types.Push(SuccessType_);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.VarDef, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var RegError, out var Reg)) {
							return RegError;
						}
						Regs = Regs.Set(RegId1, NewProc.VarDef(Span, Reg));
						Types.Push(mVM_Type.Var(Types.Get(Reg)));
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.VarSet, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId1).IsFail(out var VarRegError, out var VarReg)) {
							return VarRegError;
						}
						if (TryGetReg(RegId2).IsFail(out var ValRegError, out var ValReg)) {
							return ValRegError;
						}
						if (!Equals(Types.Get(VarReg), mVM_Type.Var(Types.Get(ValReg)))) {
							return mResult.Fail(
								Fail_(
									$"""
									found:
										{Types.Get(VarReg).ToText()}
									expected:
										{mVM_Type.Var(Types.Get(ValReg)).ToText()}
									"""
								)
							);
						}
						NewProc.VarSet(Span, VarReg, ValReg);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.VarGet, Pos: var Span, _1: var RegId1, _2: var RegId2 }: {
						if (TryGetReg(RegId2).IsFail(out var VarRegError, out var VarReg)) {
							return VarRegError;
						}
						if (!Types.Get(VarReg).IsVar(out var ResType)) {
							return mResult.Fail(Fail_("expected VAR"));
						}
						Regs = Regs.Set(RegId1, NewProc.VarGet(Span, VarReg));
						Types.Push(ResType);
						break;
					}
					case { NodeType: mIL_AST.tCommandNodeType.DefRecProcs, Pos: var Span, _1: var RegId1, _2: var RegId2, _3: var RegId3 }: {
						if (TryGetReg(RegId2).IsFail(out var FuncRegError, out var FuncReg)) {
							return FuncRegError;
						}
						if (TryGetReg(RegId3).IsFail(out var ArgRegError, out var ArgReg)) {
							return ArgRegError;
						}
						var ArgType = Types.Get(ArgReg);
						if (!Types.Get(FuncReg).IsProc(out var EmptyType, out var EnvType, out var RecTypeInOut)) {
							return mResult.Fail(Fail_("expected recursive procedure constructor"));
						}
						if (!RecTypeInOut.IsProc(out var EmptyType_, out var RecTypeIn, out var RecTypeOut)) {
							return mResult.Fail(Fail_("expected recursive procedure function"));
						}
						if (!Equals(RecTypeIn, RecTypeOut)) {
							return mResult.Fail(Fail_("recursive procedure input and output types must match"));
						}
						if (!EmptyType_.IsEmpty()) {
							return mResult.Fail(Fail_($"{FuncReg} is not a Proc with Empty Env"));
						}
						if (!EmptyType.IsEmpty()) {
							return mResult.Fail(Fail_($"{FuncReg} is not a Proc with Empty Env"));
						}
						if (ArgType.IsSubType(EnvType).ModifyError(Fail_).IsFail(out var ArgumentError, out _)) {
							return ArgumentError;
						}
						if (EnvType.IsSubType(ArgType).ModifyError(Fail_).IsFail(out var EnvironmentError, out _)) {
							return EnvironmentError;
						}
						var Result = RecTypeOut;
						while (Result.IsGeneric(out _, out var GenericBody)) {
							Result = GenericBody;
						}
						if (!Result.IsProc(out _, out _, out _)) {
							while (!Result.IsEmpty()) {
								if (!Result.IsPair(out Result, out var ProcType)) {
									return mResult.Fail(Fail_("expected recursive procedure tuple"));
								}
								while (ProcType.IsGeneric(out _, out var GenericBody)) {
									ProcType = GenericBody;
								}
								if (!ProcType.IsProc(out _, out _, out _)) {
									return mResult.Fail(Fail_("expected recursive procedure"));
								}
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
				
				aTrace(
					() => "  => " + Regs.TryGet(Command._1).Then(__ => Types.Get(__).ToText("\n  ")).ElseUse("???")
				);
				
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
