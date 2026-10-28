#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mError.cs
#:ref Common/mAssert.cs
#:ref Common/mMath.cs
#:ref Common/mMaybe.cs
#:ref Common/mResult.cs
#:ref Common/mStream.cs
#:ref Common/mTreeMap.cs
#:ref Common/mArrayList.cs
#:ref Common/mFS.cs
#:ref Common/mSpan.cs
#:ref Common/mTextStream.cs
#:ref mIL_AST.cs
#:ref mVM_Type.cs
#:ref mVM_Data.cs

using tCommand = mIL_AST.tCommandNodeType;

public static class
mIL_TypeChecker {
	public static readonly tText cEmptyType = "EMPTY_TYPE";
	public static readonly tText cIntType = "INT";
	public static readonly tText cAnyType = "ANY";
	public static readonly tText cTypeType = "TYPE";
	
	private static mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>
	CreateTypeExpression<tPos>(
		mIL_AST.tCommandNode<tPos> aCommand,
		mStd.tFunc<tText, mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>> aType,
		mStd.tFunc<tText, (tPos Pos, tText ErrorText)> aError
	) {
		mVM_Type.tType Type2 = null!;
		if (
			aCommand.NodeType is not mIL_AST.tCommandNodeType.TypePrefix &&
			aCommand._2.IsSome(out var Id2) &&
			aType(Id2).IsFail(out var Error, out Type2)
		) {
			return Error;
		}
		
		mVM_Type.tType Type3 = null!;
		if (
			aCommand._3.IsSome(out var Id3) &&
			aType(Id3).IsFail(out Error, out Type3)
		) {
			return Error;
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
						mIL_AST.tCommandNodeType.TypeFree or
						mIL_AST.tCommandNodeType.TypeAbstract => (
							Value.KindType().IsType()
						),
						mIL_AST.tCommandNodeType.TypeInterface => Value.Kind is mVM_Type.tKind.Free && Value.KindType().IsType(),
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
		if (aCommand.NodeType is mIL_AST.tCommandNodeType.TypeCond) {
			return mResult.Fail(aError("unsupported IL type command: " + aCommand.NodeType));
		}
		if (aCommand.NodeType is tCommand.TypeInterface && !mVM_Type.IsInterfaceBody(Type2, Type3)) {
			return mResult.Fail(aError("interface methods must use the hidden type only as their object argument"));
		}
		return TypeExpression(aCommand, Type2, Type3);
	}
	
	public static mVM_Type.tType
	TypeExpression<tPos>(
		mIL_AST.tCommandNode<tPos> aCommand,
		mVM_Type.tType aType2,
		mVM_Type.tType aType3
	) {
		if (aCommand.NodeType is mIL_AST.tCommandNodeType.TypeMethod) {
			if (aType3.IsProc(out _, out var Argument, out var Result)) {
				return mVM_Type.Proc(aType2, Argument, Result);
			}
			throw mError.Error("expected function type");
		}
		
		return aCommand.NodeType switch {
			mIL_AST.tCommandNodeType.TypeSig => mVM_Type.Sig(aType2, aType3),
			mIL_AST.tCommandNodeType.TypeGenericApply => aType2.ApplyType(aType3),
			mIL_AST.tCommandNodeType.TypeFunc => mVM_Type.Proc(mVM_Type.Empty(), aType2, aType3),
			mIL_AST.tCommandNodeType.TypePair => mVM_Type.Pair(aType2, aType3),
			mIL_AST.tCommandNodeType.TypeSet => mVM_Type.Set(aType2, aType3),
			mIL_AST.tCommandNodeType.TypePrefix => mVM_Type.Prefix(aCommand._2.AssertNotEmpty(), aType3),
			mIL_AST.tCommandNodeType.TypeRecord => mVM_Type.Record(aType2, aType3),
			mIL_AST.tCommandNodeType.TypeFree => mVM_Type.Free(aCommand._1, aType2),
			mIL_AST.tCommandNodeType.TypeAbstract => mVM_Type.Abstract(aCommand._1, aType2),
			mIL_AST.tCommandNodeType.TypeGeneric => mVM_Type.Generic(aType2, aType3),
			mIL_AST.tCommandNodeType.TypeRecursive => mVM_Type.Recursive(aType2, aType3),
			mIL_AST.tCommandNodeType.TypeVar => mVM_Type.Var(aType2),
			mIL_AST.tCommandNodeType.TypeInterface => mVM_Type.Interface(aType2, aType3),
			mIL_AST.tCommandNodeType.TypeCond => throw mError.Error("not implemented: " + aCommand.NodeType),
			_ => throw mError.Error("not implemented: " + aCommand.NodeType),
		};
	}
	
	public static tText
	ErrorText<tPos>(
		(tPos Pos, tText ErrorText) aError
	) {
		var (Pos, Message) = aError;
		if (Pos is mSpan.tSpan<mTextStream.tPos> Span && !tText.IsNullOrEmpty(Span.Start.Id)) {
			var File = mFS.File(Span.Start.Id);
			if (File.Exists()) {
				return (Span.Start, Message).ToText(File.TryReadText().ElseThrow().Split('\n'));
			}
		}
		return $"{Pos}: {Message}";
	}
	
	public static mResult.tResult<
		mTreeMap.tTree<tText, mVM_Type.tType>,
		(tPos Pos, tText ErrorText)
	>
	ModuleTypes<tPos>(
		mIL_AST.tModule<tPos> aModule
	) {
		var Types = mTreeMap.Tree<tText, mVM_Type.tType>(
			(a1, a2) => tText.CompareOrdinal(a1, a2).Sign(),
			[
				(cEmptyType, mVM_Type.Empty()),
				(cAnyType, mVM_Type.Any()),
				(cIntType, mVM_Type.Int()),
				(cTypeType, mVM_Type.Type()),
				(mIL_AST.cTrue, mVM_Type.True()),
				(mIL_AST.cFalse, mVM_Type.False()),
			]
		);
		
		foreach (var Command in aModule.TypeDef) {
			if (Command.NodeType is < mIL_AST.tCommandNodeType._BeginTypes_ or >= mIL_AST.tCommandNodeType._EndTypes_) {
				return mResult.Fail((Command.Pos, "expected type declaration"));
			}
			
			if (
				CreateTypeExpression(
					Command,
					aId => Types.TryGet(
						aId
					).ElseFail(
						() => (Command.Pos, $"type '{aId}' not found")
					),
					__ => (Command.Pos, __)
				).IsFail(
					out var Error,
					out var Type
				)
			) {
				return Error;
			}
			
			Types = Types.Set(Command._1, Type);
		}
		
		return Types;
	}
	
	public static mResult.tResult<
		(mVM_Type.tType Import, mVM_Type.tType Export),
		(tPos Pos, tText ErrorText)
	>
	ModuleSignature<tPos>(
		mIL_AST.tModule<tPos> aModule
	) {
		if (!aModule.Defs.TryLast().IsSome(out var Entry)) {
			return mResult.Fail((default(tPos)!, "module has no entry definition"));
		}
		if (ModuleTypes(aModule).IsFail(out var TypesError, out var Types)) {
			return TypesError;
		}
		var Position = Entry.Commands.TryFirst().Then(__ => __.Pos).ElseUse(default!);
		if (!Types.TryGet(Entry.Type).IsSome(out var Type)) {
			return mResult.Fail((Position, $"type '{Entry.Type}' not found"));
		}
		if (Type.IsRecursive(out var Head, out var Body)) {
			Type = Body.Substitute(Head, Type);
		}
		if (!Type.IsProc(out _, out _, out var Proc)) {
			return mResult.Fail((Position, "expected definition type"));
		}
		while (Proc.IsGeneric(out _, out var GenericBody)) {
			Proc = GenericBody;
		}
		return (
			Proc.IsProc(out _, out var Import, out var Export)
			? (Import, Export)
			: mResult.Fail((Position, $"expected proc but is: {Proc.ToText()}"))
		);
	}
	
	public static tBool
	AreImportTypesValid<tPos>(
		mIL_AST.tModule<tPos> aModule,
		mVM_Type.tType aImport,
		out (tPos Pos, tText ErrorText) aError
	) {
		if (!ModuleSignature(aModule).Match(out var Signature, out aError)) {
			return false;
		}
		
		var Position = aModule.Defs.TryLast(
		).AssertNotEmpty(
		).Commands.TryFirst(
		).Then(
			__ => __.Pos
		).ElseUse(
			default!
		);
		
		return aImport.IsSubType(
			Signature.Import
		).ModifyError(
			__ => (Position, __)
		).Match(
			out _,
			out aError
		);
	}
	
	public static tBool
	AreModuleTypesValid<tPos>(
		mIL_AST.tModule<tPos> aModule,
		out (tPos Pos, tText ErrorText) aError
	) {
		var Definitions = aModule.Defs;
		
		if (Definitions.IsEmpty()) {
			aError = (default(tPos)!, "module has no entry definition");
			return false;
		}
		
		if (!ModuleTypes(aModule).Match(out var Types, out aError)) {
			return false;
		}
		
		while (Definitions.TryFirst().IsSome(out var Def)) {
			Definitions = Definitions.Skip(1);
			if (
				!TypeDef(Def, Types, Definitions.IsEmpty()).Match(out var Body, out aError) ||
				!AreDefAccessesValid(Body, out aError)
			) {
				return false;
			}
		}
		
		return true;
	}
	
	private static tBool
	HasUnboundParameter(
		mVM_Type.tType aExpression,
		mStream.tStream<mVM_Type.tType> aBoundParameters = default
	) {
		return aExpression.Kind switch {
			mVM_Type.tKind.Abstract => false,
			mVM_Type.tKind.Free => !aBoundParameters.Any(__ => mStd.RefEq(__, aExpression)),
			
			mVM_Type.tKind.Sig
			or mVM_Type.tKind.Generic
			or mVM_Type.tKind.Interface
			or mVM_Type.tKind.Recursive => HasUnboundParameter(
				aExpression.Refs[1],
				mStream.Stream(aExpression.Refs[0], aBoundParameters)
			),
			
			_ => (
				aExpression.Kind is mVM_Type.tKind.Record
				? aExpression.Fields.ToStream(
				).Any(
					__ => HasUnboundParameter(__.Value, aBoundParameters)
				)
				: mStream.Stream(
					aExpression.Refs
				).Any(
					__ => HasUnboundParameter(__, aBoundParameters)
				)
			),
		};
	}
	
	// These facts belong to typing only; no access state is retained here.
	private sealed record tValueType(
		mVM_Type.tType Type,
		mMaybe.tMaybe<mVM_Data.tData> Data = default
	);
	
	private sealed record tReg(
		tNat32 Index,
		tText Id,
		mVM_Type.tType Type,
		mMaybe.tMaybe<mVM_Data.tData> Data
	);
	
	private readonly record struct tTypedReg(
		tNat32 Id,
		mVM_Type.tType Type
	);
	
	private sealed record tTypeStep(
		mMaybe.tMaybe<tValueType> Result = default,
		mStream.tStream<(tNat32 Id, mVM_Type.tType Type)> Refinements = default,
		mMaybe.tMaybe<(mVM_Type.tType SuccessType, tBool UsesObject)> TryReturn = default
	);
	
	// Type snapshots describe this use, not the final type of the value's ID.
	private sealed record tTypedCommand<tPos>(
		mIL_AST.tCommandNode<tPos> AST_Node,
		mStream.tStream<tTypedReg> TypedArgs,
		mMaybe.tMaybe<tTypedReg> TypedRes,
		mMaybe.tMaybe<(mVM_Type.tType SuccessType, tBool UsesObject)> TryReturn = default
	);
	
	private sealed record tTypedDef<tPos>(
		tPos Pos,
		tBool IsEntry,
		tTypedReg Env,
		tTypedReg Obj,
		tTypedReg Arg,
		tTypedReg Res,
		tNat32 ValueCount,
		mStream.tStream<tTypedCommand<tPos>> Commands
	);
	
	private static mResult.tResult<tTypedDef<tPos>, (tPos Pos, tText ErrorText)>
	TypeDef<tPos>(
		mIL_AST.tDef<tPos> aDef,
		mTreeMap.tTree<tText, mVM_Type.tType> aModuleTypes,
		tBool aIsEntry
	) {
		var Position = aDef.Commands.TryFirst().Then(__ => __.Pos).ElseUse(default!);
		if (!aModuleTypes.TryGet(aDef.Type).IsSome(out var DefType)) {
			return mResult.Fail((Position, $"type '{aDef.Type}' not found"));
		}
		if (DefType.IsRecursive(out var Head, out var Body)) {
			DefType = Body.Substitute(Head, DefType);
		}
		if (!DefType.IsProc(out var NullType, out var EnvType, out var ProcType)) {
			return mResult.Fail((Position, "expected definition type"));
		}
		if (!NullType.IsEmpty()) {
			return mResult.Fail((Position, "definition must have an empty object type"));
		}
		while (ProcType.IsGeneric(out _, out var InnerType)) {
			ProcType = InnerType;
		}
		if (!ProcType.IsProc(out var ObjType, out var ArgType, out var ResType)) {
			return mResult.Fail((Position, $"expected proc but is: {ProcType.ToText()}"));
		}
		
		var IdToIndex = mTreeMap.Tree<tText, tNat32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign(), []);
		var Values = mArrayList.List<tValueType>();
		var TypedCommands = mArrayList.List<tTypedCommand<tPos>>();
		
		tTypedReg
		DefineReg(
			tText aRegId,
			tValueType aValueType
		) {
			var Index = Values.Size;
			Values.Push(aValueType);
			IdToIndex = IdToIndex.Set(aRegId, Index);
			return new(Index, aValueType.Type);
		}
		
		DefineReg(mIL_AST.cEmptyValue, new(mVM_Type.Empty()));
		DefineReg(mIL_AST.cOne, new(mVM_Type.Int()));
		DefineReg(mIL_AST.cFalse, new(mVM_Type.False(), mVM_Data.Bool(false)));
		DefineReg(mIL_AST.cTrue, new(mVM_Type.True(), mVM_Data.Bool(true)));
		DefineReg(mIL_AST.cEmptyType, new(mVM_Type.Type(), mVM_Data.TypeEmpty()));
		DefineReg(mIL_AST.cIntType, new(mVM_Type.Type(), mVM_Data.TypeInt()));
		DefineReg(mIL_AST.cTypeType, new(mVM_Type.Type(), mVM_Data.TypeType()));
		var Env = DefineReg(mIL_AST.cEnv, new(EnvType));
		var Obj = DefineReg(mIL_AST.cObj, new(ObjType));
		var Arg = DefineReg(mIL_AST.cArg, new(ArgType));
		var Res = DefineReg(mIL_AST.cRes, new(ResType));
		
		foreach (var Command in aDef.Commands) {
			mResult.tResult<tReg, (tPos Pos, tText ErrorText)>
			Resolve(
				tText aRegId,
				tBool aIsValueRequired
			) {
				if (!IdToIndex.TryGet(aRegId).IsSome(out var Id)) {
					if (!aModuleTypes.TryGet(aRegId).IsSome(out var Type)) {
						return mResult.Fail((Command.Pos, $"no '{aRegId}' defined"));
					}
					if (HasUnboundParameter(Type)) {
						return mResult.Fail((Command.Pos, "unbound type parameter used as a value"));
					}
					Id = DefineReg(aRegId, new(Type.KindType(), mVM_Data.TypeExpression(Type))).Id;
				}
				
				var Value = Values.Get(Id);
				
				if (
					aIsValueRequired &&
					Value.Data.IsSome(out var Data) &&
					Data._DataType is mVM_Data.tDataType.Type &&
					HasUnboundParameter(Data.TypeExpressionValue())
				) {
					return mResult.Fail((Command.Pos, "unbound type parameter used as a value"));
				}
				
				return new tReg(Id, aRegId, Value.Type, Value.Data);
			}
			
			static mResult.tResult<mVM_Type.tType, tText>
			TypeValue(
				tReg aOperand,
				mArrayList.tArrayList<tValueType> aValues
			) {
				if (!aOperand.Type.IsType() && !aOperand.Type.IsTypeFunctionKind()) {
					return mResult.Fail("expected type or type function");
				}
				if (aOperand.Data.IsSome(out var Known) && !Known.IsEmpty()) {
					return Known.TypeExpressionValue();
				}
				
				var Symbol = mVM_Type.Abstract(aOperand.Id, aOperand.Type);
				aValues.Update(aOperand.Index, __ => __ with { Data = mVM_Data.TypeExpression(Symbol) });
				
				return Symbol;
			}
			
			if (Command.NodeType is >= tCommand._BeginTypes_ and < tCommand._EndTypes_) {
				var TypeOperands = mArrayList.List<tTypedReg>();
				var Checked = CreateTypeExpression(
					Command,
					aName => Resolve(
						aName,
						false
					).ThenTry(
						aOperand => {
							TypeOperands.Push(new tTypedReg(aOperand.Index, aOperand.Type));
							return TypeValue(aOperand, Values).ModifyError(__ => (Command.Pos, __));
						}
					),
					__ => (Command.Pos, __)
				);
				
				if (!Checked.Match(out var Type, out var Error)) {
					return mResult.Fail(Error);
				}
				
				var Result = DefineReg(Command._1, new(Type.KindType(), mVM_Data.TypeExpression(Type)));
				TypedCommands.Push(new tTypedCommand<tPos>(Command, mStream.Stream(TypeOperands.ToArray()), Result));
				
				continue;
			}
			
			// Only value roles are resolved. Labels and named contracts stay in the source command.
			var OperandNames = Command.NodeType switch {
				tCommand.Int => mStream.Stream<mMaybe.tMaybe<tText>>(),
				tCommand.VarSet or tCommand.ReturnIf or tCommand.Assert => mStream.Stream<mMaybe.tMaybe<tText>>(Command._1, Command._2),
				tCommand.TryReturn => mStream.Stream<mMaybe.tMaybe<tText>>(Command._1, Command._2, Command._3).Where(__ => __.IsSome(out _)),
				tCommand.PrefixApply or tCommand.PrefixRemove => mStream.Stream(Command._3),
				tCommand.Alias or tCommand.First or tCommand.Second or tCommand.GetField
				or tCommand.SigHead or tCommand.SigHeadAs or tCommand.SigBody or tCommand.TryAsSig
				or tCommand.ReturnIfNotEmpty or tCommand.TryAsEmpty or tCommand.TryAsBool
				or tCommand.TryAsInt or tCommand.TryAsType or tCommand.TryAsRecord
				or tCommand.TryAsPair or tCommand.TryAsVar or tCommand.TryAsRef
				or tCommand.TryRemovePrefixFrom or tCommand.VarDef or tCommand.VarGet => mStream.Stream(Command._2),
				tCommand.BoolAnd or tCommand.BoolOr or tCommand.BoolXOr
				or tCommand.IntsAreEq or tCommand.IntsComp or tCommand.IntsAdd or tCommand.IntsSub
				or tCommand.IntsMul or tCommand.IntsDiv or tCommand.Pair or tCommand.AddField
				or tCommand.CallFunc or tCommand.Closure or tCommand.CallProc or tCommand.DefRecProcs
				or tCommand.Sig or tCommand.Interface or tCommand.SubInterface or tCommand.TryHasHeadType => mStream.Stream(Command._2, Command._3),
				_ => mStream.Stream<mMaybe.tMaybe<tText>>(),
			};
			
			var Inputs = mArrayList.List<tReg>();
			foreach (var Name in OperandNames) {
				if (!Resolve(Name.AssertNotEmpty(), true).Match(out var Operand, out var Error)) {
					return mResult.Fail(Error);
				}
				Inputs.Push(Operand);
			}
			
			var Operands = Inputs.ToArray();
			
			if (Command.NodeType is tCommand.Alias) {
				// Later uses already carry the same value ID; access checking needs no name alias.
				IdToIndex = IdToIndex.Set(Command._1, Operands[0].Index);
				continue;
			}
			
			var TypeOperand = Command.NodeType switch {
				tCommand.Sig or tCommand.Interface or tCommand.SubInterface => 0,
				tCommand.TryHasHeadType => 1,
				tCommand.Pair when Operands[0].Type.IsType() || Operands[0].Type.IsTypeFunctionKind() => 0,
				_ => -1,
			};
			
			if (TypeOperand >= 0) {
				var Operand = Operands[TypeOperand];
				if (!TypeValue(Operand, Values).Match(out _, out var Error)) {
					return mResult.Fail((Command.Pos, Error));
				}
				Operands[TypeOperand] = Operand with { Data = Values.Get(Operand.Index).Data };
			}
			
			mMaybe.tMaybe<mVM_Type.tType> NamedType = default;
			
			if (Command.NodeType is tCommand.TryAsSig or tCommand.SigHeadAs) {
				var Name = Command._3.AssertNotEmpty();
				NamedType = aModuleTypes.TryGet(Name);
				if (NamedType.IsNone()) {
					return mResult.Fail((Command.Pos, $"type '{Name}' not found"));
				}
			}
			
			var Check = Command.NodeType switch {
				tCommand.Int or tCommand.BoolAnd or tCommand.BoolOr or tCommand.BoolXOr
				or tCommand.IntsAreEq or tCommand.IntsComp or tCommand.IntsAdd or tCommand.IntsSub
				or tCommand.IntsMul or tCommand.IntsDiv => PrimitiveTypes(Command, Operands),
				tCommand.Sig or tCommand.TryAsSig or tCommand.TryHasHeadType
				or tCommand.SigHead or tCommand.SigHeadAs or tCommand.SigBody => SigTypes(Command, Operands, NamedType),
				tCommand.CallFunc or tCommand.Closure or tCommand.CallProc or tCommand.DefRecProcs
				or tCommand.TryReturn => CallTypes(Command, Operands, ObjType, Values.Get(Res.Id).Type),
				tCommand.ReturnIf or tCommand.ReturnIfNotEmpty or tCommand.TryAsRef
				or tCommand.Assert => ControlTypes(Command, Operands, Res.Id, Values.Get(Res.Id).Type),
				_ => StructureTypes(Command, Operands),
			};
			if (!Check.Match(out var Step, out var TypeError)) {
				return mResult.Fail((Command.Pos, TypeError));
			}
			
			// Only this driver installs bindings and refinements returned by the local rules.
			var Uses = mStream.Stream(Operands).Map(
				__ => new tTypedReg(__.Index, __.Type)
			).ToArrayList(
			).ToArray();
			
			var Output = Step.Result.Then(__ => DefineReg(Command._1, __));
			
			TypedCommands.Push(new tTypedCommand<tPos>(Command, mStream.Stream(Uses), Output, Step.TryReturn));
			
			foreach (var (Id, Type) in Step.Refinements) {
				Values.Update(Id, __ => __ with { Type = Type });
			}
		}
		
		return new tTypedDef<tPos>(Position, aIsEntry, Env, Obj, Arg, Res, Values.Size, mStream.Stream(TypedCommands.ToArray()));
	}
	
	private static mResult.tResult<tTypeStep, tText>
	PrimitiveTypes<tPos>(mIL_AST.tCommandNode<tPos> aCommand, tReg[] aInputs) {
		if (aCommand.NodeType is tCommand.Int) {
			return (
				tInt32.TryParse(aCommand._2.AssertNotEmpty(), out _)
				? new tTypeStep(new tValueType(mVM_Type.Int()))
				: mResult.Fail("integer literal is outside INT range")
			);
		}
		if (aCommand.NodeType is tCommand.BoolAnd or tCommand.BoolOr or tCommand.BoolXOr) {
			foreach (var Input in aInputs) {
				if (!Input.Type.IsSubType(mVM_Type.Bool()).Match(out _, out var Error)) {
					return mResult.Fail(Error);
				}
			}
			return new tTypeStep(new tValueType(mVM_Type.Bool()));
		}
		foreach (var Input in aInputs) {
			if (!Equals(Input.Type, mVM_Type.Int())) {
				return mResult.Fail($"expected §INT but found {Input.Type.ToText()}");
			}
		}
		return new tTypeStep(
			new tValueType(
				aCommand.NodeType switch {
					tCommand.IntsAreEq => mVM_Type.Bool(),
					tCommand.IntsDiv => mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Int()),
					_ => mVM_Type.Int(),
				}
			)
		);
	}
	
	private static mResult.tResult<tTypeStep, tText>
	StructureTypes<tPos>(mIL_AST.tCommandNode<tPos> aCommand, tReg[] aInputs) {
		if (aInputs.Length == 0) {
			return mResult.Fail($"unsupported IL command: {aCommand.NodeType}");
		}
		var Input = aInputs[0];
		switch (aCommand.NodeType) {
			case tCommand.Pair: {
				var Left = Input.Data.Else(mVM_Data.Empty);
				var Right = aInputs[1].Data.Else(mVM_Data.Empty);
				return new tTypeStep(
					new tValueType(
						mVM_Type.Pair(Input.Type, aInputs[1].Type),
						!Left.IsEmpty() || !Right.IsEmpty() ? mMaybe.Some(mVM_Data.Pair(Left, Right)) : default
					)
				);
			}
			case tCommand.First:
			case tCommand.Second: {
				if (!Input.Type.TryProjectPair(out var FirstType, out var SecondType)) {
					return mResult.Fail(
						aCommand.NodeType is tCommand.First
						? $"{aCommand._1} := FIRST {aCommand._2} :: {Input.Type.ToText()}"
						: "expected pair"
					);
				}
				mMaybe.tMaybe<mVM_Data.tData> Known = default;
				if (Input.Data.IsSome(out var Pair) && Pair.IsPair(out var First, out var Second)) {
					Known = aCommand.NodeType is tCommand.First ? First : Second;
				}
				return new tTypeStep(new tValueType(aCommand.NodeType is tCommand.First ? FirstType : SecondType, Known));
			}
			case tCommand.PrefixApply:
				return new tTypeStep(new tValueType(mVM_Type.Prefix(aCommand._2.AssertNotEmpty(), Input.Type)));
			case tCommand.PrefixRemove: {
				if (!Input.Type.IsPrefix(aCommand._2.AssertNotEmpty(), out var Type)) {
					return mResult.Fail("wrong prefix");
				}
				return new tTypeStep(new tValueType(Type));
			}
			case tCommand.AddField: {
				if (!aInputs[1].Type.IsPrefix(out _, out _)) {
					return mResult.Fail("expected prefixed record field");
				}
				return new tTypeStep(new tValueType(mVM_Type.Record(Input.Type, aInputs[1].Type)));
			}
			case tCommand.GetField: {
				if (!Input.Type.IsRecord(out var Fields)) {
					return mResult.Fail("expected record");
				}
				var Key = aCommand._3.AssertNotEmpty();
				if (!Fields.TryGet(Key).IsSome(out var Type)) {
					var Names = Fields.ToStream().Map(__ => __.Key).Reduce("", (aText, aName) => aText + "\n  " + aName);
					return mResult.Fail($"Unknown field '{Key}' in record [{Names}\n]");
				}
				return new tTypeStep(new tValueType(Type));
			}
			case tCommand.Interface: {
				var Contract = Input.Data.AssertNotEmpty().TypeExpressionValue();
				if (!Contract.IsInterface(out var Parameter, out var Body)) {
					return mResult.Fail("expected interface contract");
				}
				if (!aInputs[1].Type.IsPair(out var Object, out var Methods)) {
					return mResult.Fail("expected interface payload (object, methods)");
				}
				return Contract.CheckInterfaceImplementation(Object, Methods).Then(
					_ => new tTypeStep(new tValueType(Contract))
				);
			}
			case tCommand.SubInterface: {
				var Type = Input.Data.AssertNotEmpty().TypeExpressionValue();
				return aInputs[1].Type.IsSubType(Type).Then(
					_ => new tTypeStep(new tValueType(Type))
				);
			}
			case tCommand.VarDef:
				return new tTypeStep(new tValueType(mVM_Type.Var(Input.Type)));
			case tCommand.VarGet: {
				if (!Input.Type.IsVar(out var Type)) {
					return mResult.Fail("expected VAR");
				}
				return new tTypeStep(new tValueType(Type));
			}
			case tCommand.VarSet: {
				var Expected = mVM_Type.Var(aInputs[1].Type);
				if (!Equals(Input.Type, Expected)) {
					return mResult.Fail($"found:\n\t{Input.Type.ToText()}\nexpected:\n\t{Expected.ToText()}");
				}
				return new tTypeStep();
			}
			case tCommand.TryRemovePrefixFrom: {
				var Prefix = aCommand._3.AssertNotEmpty();
				var (Matched, _) = Input.Type.SplitBy(__ => __.IsPrefix(Prefix, out _));
				if (!Matched.IsSome(out var Type) || !Type.IsPrefix(Prefix, out var Inner)) {
					return mResult.Fail($"TRY_REMOVE expects type with prefix #{Prefix} but is {Input.Type.ToText()}");
				}
				return new tTypeStep(new tValueType(Inner));
			}
			case tCommand.TryAsEmpty:
			case tCommand.TryAsBool:
			case tCommand.TryAsInt:
			case tCommand.TryAsType:
			case tCommand.TryAsRecord:
			case tCommand.TryAsPair:
			case tCommand.TryAsVar: {
				var (Matched, _) = Input.Type.SplitBy(
					__ => aCommand.NodeType switch {
						tCommand.TryAsEmpty => __.IsEmpty(),
						tCommand.TryAsBool => __.Kind is mVM_Type.tKind.True or mVM_Type.tKind.False,
						tCommand.TryAsInt => __.IsInt(),
						tCommand.TryAsType => __.IsType(),
						tCommand.TryAsRecord => __.IsRecord(out _),
						tCommand.TryAsPair => __.IsPair(out _, out _),
						_ => __.IsVar(out _),
					}
				);
				if (!Matched.IsSome(out var Type)) {
					var Name = aCommand.NodeType switch {
						tCommand.TryAsEmpty => "Empty", tCommand.TryAsBool => "BOOL", tCommand.TryAsInt => "INT",
						tCommand.TryAsType => "TYPE", tCommand.TryAsRecord => "RECORD", tCommand.TryAsPair => "PAIR", _ => "VAR",
					};
					return mResult.Fail($"TRY_AS_{Name} expects type with {(Name == "Empty" ? "[]" : Name)} but is {Input.Type.ToText()}");
				}
				return new tTypeStep(new tValueType(Type));
			}
			default:
				return mResult.Fail($"unsupported IL command: {aCommand.NodeType}");
		}
	}
	
	private static mResult.tResult<tTypeStep, tText>
	SigTypes<tPos>(
		mIL_AST.tCommandNode<tPos> aCommand,
		tReg[] aInputs,
		mMaybe.tMaybe<mVM_Type.tType> aNamedType
	) {
		var Input = aInputs[0];
		switch (aCommand.NodeType) {
			case tCommand.Sig: {
				var Contract = Input.Data.AssertNotEmpty().TypeExpressionValue();
				if (!Contract.IsSig(out var Binder, out var BodyType)) {
					return mResult.Fail("expected SIG contract");
				}
				var Payload = aInputs[1];
				if (!Payload.Type.IsPair(out _, out var Body)) {
					return mResult.Fail("expected SIG payload");
				}
				if (!Payload.Data.IsSome(out var Known) || !Known.IsPair(out var HeadData, out _)) {
					return mResult.Fail("SIG payload must have a known type head");
				}
				var Head = HeadData.TypeExpressionValue();
				if (!Head.KindType().SameType(Binder.KindType())) {
					return mResult.Fail("wrong SIG head kind");
				}
				if (!Body.IsSubType(BodyType.Substitute(Binder, Head)).Match(out _, out var Error)) {
					return mResult.Fail(Error);
				}
				return new tTypeStep(new tValueType(Contract));
			}
			case tCommand.TryAsSig: {
				var NamedType = aNamedType.AssertNotEmpty();
				if (!NamedType.IsSig(out var Binder, out var Body)) {
					return mResult.Fail("expected SIG contract");
				}
				if (Binder.Kind is not mVM_Type.tKind.Free) {
					return mResult.Fail("SIG contract head must be FREE");
				}
				if (HasUnboundParameter(NamedType)) {
					return mResult.Fail("unbound type parameter in SIG contract");
				}
				var Witness = mVM_Type.Abstract(aCommand._1, Binder.KindType());
				return new tTypeStep(new tValueType(mVM_Type.Sig(Witness, Body.Substitute(Binder, Witness))));
			}
			case tCommand.TryHasHeadType: {
				if (!Input.Type.IsSig(out var Head, out var Body)) {
					return mResult.Fail("expected SIG");
				}
				var Expected = aInputs[1].Data.AssertNotEmpty().TypeExpressionValue();
				if (!Head.KindType().SameType(Expected.KindType())) {
					return mResult.Fail("wrong SIG head kind");
				}
				return new tTypeStep(new tValueType(mVM_Type.Sig(Expected, Body.Substitute(Head, Expected))));
			}
			default: {
				if (!Input.Type.IsSig(out var Head, out var Body)) {
					return mResult.Fail("expected SIG");
				}
				var Refined = mStream.Stream<(tNat32, mVM_Type.tType)>();
				if (aCommand.NodeType is tCommand.SigHeadAs) {
					var NamedType = aNamedType.AssertNotEmpty();
					if (NamedType.Kind is not mVM_Type.tKind.Abstract) {
						return mResult.Fail("expected abstract SIG witness");
					}
					if (!(Head.Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract && Head.KindType().SameType(NamedType.KindType()))) {
						return mResult.Fail("wrong SIG witness kind");
					}
					Body = Body.Substitute(Head, NamedType);
					Head = NamedType;
					Refined = mStream.Stream((Input.Index, mVM_Type.Sig(Head, Body)));
				} else if (Head.Kind is mVM_Type.tKind.Free) {
					var Witness = mVM_Type.Abstract(Input.Id, Head.KindType());
					Body = Body.Substitute(Head, Witness);
					Head = Witness;
					Refined = mStream.Stream((Input.Index, mVM_Type.Sig(Head, Body)));
				}
				var IsHead = aCommand.NodeType is tCommand.SigHead or tCommand.SigHeadAs;
				return new tTypeStep(
					new tValueType(
						IsHead ? Head.KindType() : Body,
						IsHead ? mMaybe.Some(mVM_Data.TypeExpression(Head)) : default
					),
					Refined
				);
			}
		}
	}
	
	private static mResult.tResult<tTypeStep, tText>
	CallTypes<tPos>(
		mIL_AST.tCommandNode<tPos> aCommand,
		tReg[] aInputs,
		mVM_Type.tType aObjectType,
		mVM_Type.tType aResultType
	) {
		var Function = aInputs[0].Type;
		var Argument = aInputs[1].Type;
		if (aCommand.NodeType is tCommand.CallFunc or tCommand.Closure or tCommand.CallProc) {
			var Object = mVM_Type.Empty();
			if (aCommand.NodeType is tCommand.CallProc && !Function.IsPair(out Object, out Function)) {
				return mResult.Fail("expected object/method pair");
			}
			if (aCommand.NodeType is tCommand.CallProc && Function.IsPrefix(out var Name, out var Marker) && Marker.IsEmpty()) {
				if (!Object.FirstObjectType().TryGetInterfaceMethod(Name).IsSome(out Function)) {
					return mResult.Fail($"Unknown interface method '{Name}'");
				}
			}
			if (!mVM_Type.Infer(Function, Object, Argument, __ => {}).Match(out var Result, out var Error)) {
				return mResult.Fail(Error);
			}
			return new tTypeStep(new tValueType(Result));
		}
		if (aCommand.NodeType is tCommand.DefRecProcs) {
			if (!Function.IsProc(out var Empty, out var Env, out var Recursive)) {
				return mResult.Fail("expected recursive procedure constructor");
			}
			if (!Recursive.IsProc(out var InnerEmpty, out var Input, out var Output)) {
				return mResult.Fail("expected recursive procedure function");
			}
			if (!Equals(Input, Output)) {
				return mResult.Fail("recursive procedure input and output types must match");
			}
			if (!InnerEmpty.IsEmpty() || !Empty.IsEmpty()) {
				return mResult.Fail($"{aInputs[0].Index} is not a Proc with Empty Env");
			}
			if (!Argument.IsSubType(Env).Match(out _, out var Error) || !Env.IsSubType(Argument).Match(out _, out Error)) {
				return mResult.Fail(Error);
			}
			var Result = Output;
			while (Result.IsGeneric(out _, out var Body)) {
				Result = Body;
			}
			if (!Result.IsProc(out _, out _, out _)) {
				while (!Result.IsEmpty()) {
					if (!Result.IsPair(out Result, out var Proc)) {
						return mResult.Fail("expected recursive procedure tuple");
					}
					while (Proc.IsGeneric(out _, out var Body)) {
						Proc = Body;
					}
					if (!Proc.IsProc(out _, out _, out _)) {
						return mResult.Fail("expected recursive procedure");
					}
				}
			}
			return new tTypeStep(new tValueType(Output));
		}
		
		while (Function.IsGeneric(out var Parameter, out var FunctionBody)) {
			Function = FunctionBody.Substitute(Parameter, mVM_Type.Free(Parameter.KindType()));
		}
		if (!Function.IsProc(out var ObjectType, out var ExpectedArgument, out var ResultType)) {
			return mResult.Fail("§TRY_RETURN expects a function");
		}
		var UsesObject = !ObjectType.IsEmpty();
		if (UsesObject && !aObjectType.IsSubType(ObjectType).Match(out _, out var ObjectError)) {
			return mResult.Fail(ObjectError);
		}
		if (!ExpectedArgument.IsSubType(Argument).Match(out _, out _)) {
			return mResult.Fail("§TRY_RETURN function argument type cannot match the argument");
		}
		var (_, Success) = ResultType.SplitBy(__ => __.IsEmpty());
		if (!Success.IsSome(out var SuccessType) || !SuccessType.IsPrefix("Result", out _)) {
			return mResult.Fail("§TRY_RETURN function result must contain #Result");
		}
		if (!SuccessType.IsSubType(aResultType).Match(out _, out var ResultError)) {
			return mResult.Fail(ResultError);
		}
		if (aInputs.Length == 3) {
			var Guard = aInputs[2].Type;
			while (Guard.IsGeneric(out _, out var Body)) {
				Guard = Body;
			}
			if (!Guard.IsProc(out var GuardObject, out var GuardArgument, out var GuardResult)) {
				return mResult.Fail("§TRY_RETURN guard expects a function");
			}
			UsesObject |= !GuardObject.IsEmpty();
			if (!GuardObject.IsEmpty() && !aObjectType.IsSubType(GuardObject).Match(out _, out var Error)) {
				return mResult.Fail(Error);
			}
			if (!ExpectedArgument.IsSubType(GuardArgument).Match(out _, out var ArgumentError)) {
				return mResult.Fail(ArgumentError);
			}
			if (!GuardResult.IsSubType(mVM_Type.Bool()).Match(out _, out var GuardError)) {
				return mResult.Fail(GuardError);
			}
		}
		var Refinements = (
			aInputs.Length == 2 && Argument.Subtract(ExpectedArgument).IsSome(out var Remainder)
			? mStream.Stream((aInputs[1].Index, Remainder))
			: default
		);
		return new tTypeStep(Refinements: Refinements, TryReturn: (SuccessType, UsesObject));
	}
	
	private static mResult.tResult<tTypeStep, tText>
	ControlTypes<tPos>(
		mIL_AST.tCommandNode<tPos> aCommand,
		tReg[] aInputs,
		tNat32 aResultId,
		mVM_Type.tType aResultType
	) {
		switch (aCommand.NodeType) {
			case tCommand.ReturnIf: {
				if (!aInputs[0].Type.IsSubType(mVM_Type.Bool()).Match(out _, out var Error)) {
					return mResult.Fail(Error);
				}
				if (!aInputs[1].Type.IsSubType(mVM_Type.Set(aResultType, mVM_Type.Empty())).Match(out _, out Error)) {
					return mResult.Fail(Error);
				}
				return new tTypeStep();
			}
			case tCommand.ReturnIfNotEmpty:
				return new tTypeStep(Refinements: mStream.Stream((aInputs[0].Index, mVM_Type.Empty())));
			case tCommand.TryAsRef: {
				var (Matched, Remainder) = aInputs[0].Type.SplitBy(__ => __.IsRef(out _));
				if (!Matched.IsSome(out var Type)) {
					return mResult.Fail($"TRY_AS_REF expects type with REF but is {aInputs[0].Type.ToText()}");
				}
				return new tTypeStep(
					new tValueType(Type),
					mStream.Stream(
						(
							aResultId,
							Remainder.IsSome(out var Other) ? mVM_Type.Set(aResultType, Other) : aResultType
						)
					)
				);
			}
			default:
				// ASSERT currently only requires its operands to be defined.
				return new tTypeStep();
		}
	}
	
	// A composite has one access summary. Projections never recover component-specific rights.
	private sealed record tAccessValue(
		tBool Mutable = false,
		tBool Opaque = false,
		mMaybe.tMaybe<tNat32> Cell = default,
		tBool BoundReceiver = false
	);
	
	private static tAccessValue
	Join(
		tAccessValue aLeft,
		tAccessValue aRight
	) => new(
		aLeft.Mutable || aRight.Mutable,
		aLeft.Opaque || aRight.Opaque,
		aLeft.Cell.Eq(aRight.Cell, (a1, a2) => a1 == a2) ? aLeft.Cell : default
	);
	
	private static tAccessValue
	FromType(
		mVM_Type.tType aType,
		tBool aOpaque,
		tBool aImmutable = false
	) {
		if (aType.IsImmutable) {
			return new();
		}
		return aType.Kind switch {
			mVM_Type.tKind.Pair or mVM_Type.tKind.Set => Join(
				FromType(aType.Refs[0], aOpaque, aImmutable),
				FromType(aType.Refs[1], aOpaque, aImmutable)
			),
			mVM_Type.tKind.Prefix => FromType(aType.Refs[0], aOpaque, aImmutable),
			mVM_Type.tKind.Var => new(true, aOpaque),
			mVM_Type.tKind.Interface => new(true, aOpaque, BoundReceiver: true),
			mVM_Type.tKind.Record => aType.Fields.ToStream().Reduce(
				new tAccessValue(!aImmutable, aOpaque && !aImmutable),
				(aSummary, aField) => Join(aSummary, FromType(aField.Value, aOpaque, aImmutable))
			),
			mVM_Type.tKind.Sig or mVM_Type.tKind.Recursive
			or mVM_Type.tKind.Generic => FromType(aType.Refs[1], aOpaque, aImmutable),
			_ => new(!aImmutable, aOpaque && !aImmutable),
		};
	}
	
	private static tBool
	AreDefAccessesValid<tPos>(
		tTypedDef<tPos> aDef,
		out (tPos Pos, tText ErrorText) aError
	) {
		aError = default;
		var Values = mArrayList.List<tAccessValue>();
		for (tNat32 Index = 0; Index < aDef.ValueCount; Index++) {
			Values.Push(new tAccessValue());
		}
		// Only direct VAR references retain cell identity; composites have no component map.
		var Cells = mTreeMap.Tree<tNat32, tAccessValue>((a1, a2) => a1.CompareTo(a2).Sign(), []);
		var NextCell = 0u;
		
		tAccessValue
		Bind(
			mVM_Type.tType aType,
			tBool aOpaque,
			tBool aImmutable = false
		) {
			var Access = FromType(aType, aOpaque, aImmutable);
			if (aType.IsVar(out var Content)) {
				var Cell = NextCell++;
				Cells = Cells.Set(Cell, FromType(Content, aOpaque, aImmutable));
				Access = Access with { Cell = Cell };
			}
			return Access;
		}
		
		tBool
		Has(
			tAccessValue aValue,
			tBool aOpaque,
			mStream.tStream<tNat32> aVisited = default
		) => (
			(aOpaque ? aValue.Opaque : aValue.Mutable) || (
				aValue.Cell.IsSome(out var Cell) && !aVisited.Any(__ => __ == Cell) &&
				Has(Cells.TryGet(Cell).AssertNotEmpty(), aOpaque, mStream.Stream(Cell, aVisited))
			)
		);
		
		tAccessValue
		Aggregate(
			tAccessValue aValue
		) => new(
			Has(aValue, false),
			Has(aValue, true)
		);
		
		tAccessValue
		Project(
			tAccessValue aValue,
			mVM_Type.tType aType
		) => (
			aType.IsImmutable || !Has(aValue, false) ? new() : Bind(aType, Has(aValue, true))
		);
		
		void
		InvalidateContents(
		) {
			// An opaque write or method call can affect any retained cell alias.
			foreach (var (Cell, Content) in Cells.ToStream()) {
				if (Has(Content, false)) {
					Cells = Cells.Set(Cell, Content with { Opaque = true });
				}
			}
			// Composites stay opaque; bound interfaces retain their fixed receiver identity.
			for (tNat32 Id = 0; Id < Values.Size; Id++) {
				var Value = Values.Get(Id);
				if (Value.Cell.IsNone() && Value.Mutable && !Value.BoundReceiver) {
					Values.Set(Id, Value with { Opaque = true });
				}
			}
		}
		
		tAccessValue
		CallAccess(
			mVM_Type.tType aType,
			tAccessValue aArgument,
			mMaybe.tMaybe<tAccessValue> aObject = default
		) {
			var Denied = Has(aArgument, true);
			if (aObject.IsSome(out var ObjectAccess) && Has(ObjectAccess, false) && Denied) {
				InvalidateContents();
			}
			return aType.IsImmutable ? new() : Bind(aType, Denied);
		}
		
		foreach (
			var (Value, Opaque, Immutable) in mStream.Stream(
				(aDef.Env, false, true),
				(aDef.Obj, false, false),
				(aDef.Arg, true, aDef.IsEntry),
				(aDef.Res, false, false)
			)
		) {
			Values.Set(Value.Id, Bind(Value.Type, Opaque, Immutable));
		}
		
		if (Has(Values.Get(aDef.Env.Id), false)) {
			aError = (aDef.Pos, "mutable closure environment is not allowed");
			return false;
		}
		mMaybe.tMaybe<tPos> MutableExport = default;
		
		foreach (var Command in aDef.Commands) {
			var AST_Node = Command.AST_Node;
			var Inputs = Command.TypedArgs.ToArrayList().ToArray();
			var Access = new tAccessValue();
			var ResultType = Command.TypedRes.Then(__ => __.Type);
			if (AST_Node.NodeType is >= tCommand._BeginTypes_ and < tCommand._EndTypes_) {
				continue;
			}
			// Use-site type refinements also apply to the value's access facts.
			foreach (var Input in Inputs) {
				if (Input.Type.IsImmutable) {
					Values.Set(Input.Id, new());
				}
			}
			switch (AST_Node.NodeType) {
				case tCommand.Int:
				case tCommand.BoolAnd:
				case tCommand.BoolOr:
				case tCommand.BoolXOr:
				case tCommand.IntsAreEq:
				case tCommand.IntsComp:
				case tCommand.IntsAdd:
				case tCommand.IntsSub:
				case tCommand.IntsMul:
				case tCommand.IntsDiv:
				case tCommand.TryAsEmpty:
				case tCommand.TryAsBool:
				case tCommand.TryAsInt:
				case tCommand.TryAsType:
				case tCommand.SigHead:
				case tCommand.SigHeadAs:
				case tCommand.Assert:
					break;
				case tCommand.Pair:
				case tCommand.AddField:
					Access = Join(
						Aggregate(Values.Get(Inputs[0].Id)),
						Aggregate(Values.Get(Inputs[1].Id))
					);
					break;
				case tCommand.PrefixApply:
					Access = Aggregate(Values.Get(Inputs[0].Id));
					break;
				case tCommand.Sig:
				case tCommand.Interface:
					Access = Aggregate(Values.Get(Inputs[1].Id)) with {
						BoundReceiver = AST_Node.NodeType is tCommand.Interface
					};
					break;
				case tCommand.SubInterface:
					Access = Project(Values.Get(Inputs[1].Id), ResultType.AssertNotEmpty());
					break;
				case tCommand.First:
				case tCommand.Second:
				case tCommand.PrefixRemove:
				case tCommand.TryRemovePrefixFrom:
				case tCommand.SigBody:
				case tCommand.GetField:
					Access = Project(Values.Get(Inputs[0].Id), ResultType.AssertNotEmpty());
					break;
				case tCommand.TryAsSig:
				case tCommand.TryHasHeadType:
				case tCommand.TryAsRecord:
				case tCommand.TryAsPair:
				case tCommand.TryAsRef:
					Access = ResultType.AssertNotEmpty().IsImmutable ? new() : Values.Get(Inputs[0].Id);
					break;
				case tCommand.Closure:
				case tCommand.DefRecProcs:
					if (Has(Values.Get(Inputs[1].Id), false)) {
						aError = (AST_Node.Pos, "mutable or opaque closure environment is not allowed");
						return false;
					}
					Access = CallAccess(ResultType.AssertNotEmpty(), Values.Get(Inputs[1].Id));
					break;
				case tCommand.CallFunc:
					Access = CallAccess(ResultType.AssertNotEmpty(), Values.Get(Inputs[1].Id));
					break;
				case tCommand.CallProc: {
					mAssert.IsTrue(Inputs[0].Type.IsPair(out var ObjectType, out _));
					var Object = Project(Values.Get(Inputs[0].Id), ObjectType);
					if (Has(Object, true)) {
						aError = (AST_Node.Pos, "opaque mutable access is not allowed");
						return false;
					}
					Access = CallAccess(ResultType.AssertNotEmpty(), Values.Get(Inputs[1].Id), Object);
					break;
				}
				case tCommand.TryReturn: {
					var (SuccessType, UsesObject) = Command.TryReturn.AssertNotEmpty();
					var Success = CallAccess(
						SuccessType,
						Values.Get(Inputs[1].Id),
						UsesObject ? mMaybe.Some(Values.Get(aDef.Obj.Id)) : default
					);
					if (aDef.IsEntry && Success.Mutable) {
						MutableExport = AST_Node.Pos;
					}
					break;
				}
				case tCommand.ReturnIf:
				case tCommand.ReturnIfNotEmpty: {
					var Value = Inputs[AST_Node.NodeType is tCommand.ReturnIf ? 1 : 0].Id;
					if (aDef.IsEntry && Values.Get(Value).Mutable) {
						MutableExport = AST_Node.Pos;
					}
					if (AST_Node.NodeType is tCommand.ReturnIfNotEmpty) {
						Values.Set(Value, new());
					}
					break;
				}
				case tCommand.TryAsVar: {
					var Input = Values.Get(Inputs[0].Id);
					Access = Input.Cell.IsSome(out _) ? Input : Bind(ResultType.AssertNotEmpty(), Has(Input, true));
					Values.Set(Inputs[0].Id, Access);
					break;
				}
				case tCommand.VarDef:
					Cells = Cells.Set(NextCell, Values.Get(Inputs[0].Id));
					Access = new(true, Cell: NextCell++);
					break;
				case tCommand.VarGet:
				case tCommand.VarSet: {
					var Variable = Values.Get(Inputs[0].Id);
					if (Variable.Opaque) {
						aError = (AST_Node.Pos, "opaque mutable access is not allowed");
						return false;
					}
					if (AST_Node.NodeType is tCommand.VarGet) {
						Access = (
							ResultType.AssertNotEmpty().IsImmutable ? new() :
							Variable.Cell.IsSome(out var Cell) ? Cells.TryGet(Cell).AssertNotEmpty() :
							Project(Variable, ResultType.AssertNotEmpty())
						);
					} else {
						var Value = Values.Get(Inputs[1].Id);
						if (Has(Value, true)) {
							InvalidateContents();
						}
						if (Variable.Cell.IsSome(out var Cell)) {
							Cells = Cells.Set(Cell, Join(Cells.TryGet(Cell).AssertNotEmpty(), Value));
						}
					}
					break;
				}
				default:
					throw mError.Error($"missing access rule for typed command: {AST_Node.NodeType}");
			}
			if (Command.TypedRes.IsSome(out var Output)) {
				Values.Set(Output.Id, Access);
			}
		}
		if (MutableExport.IsSome(out var Position)) {
			aError = (Position, "mutable module export is not allowed");
			return false;
		}
		return true;
	}
}
