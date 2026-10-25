#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref mIL_TypeChecker.cs

using tIL_CommandType = mIL_AST.tCommandNodeType;

public static class
mIL_GenerateOpcodes {
	// The caller validates the IL and its import types before generating code.
	public static (
		mStream.tStream<mVM_Data.tProcDef<tPos>> Defs,
		mTreeMap.tTree<tText, tNat32> DefIdToIndex
	)
	GenerateOpcodes<tPos>(
		mIL_AST.tModule<tPos> aModule,
		(mVM_Data.tData Data, mVM_Type.tType Type) aImport
	) {
		mAssert.IsFalse(aImport.Data._IsMutable, "mutable module import is not allowed");
		
		var Types = mIL_TypeChecker.ModuleTypes(aModule).ModifyError(mIL_TypeChecker.ErrorText).ElseThrow();
		var TypeIdToIndex = mTreeMap.Tree<tText, tNat32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign(), []);
		var TypeDefinitions = mArrayList.List<mVM_Type.tType>();
		
		foreach (var (Id, Type) in Types.ToStream()) {
			TypeIdToIndex = TypeIdToIndex.Set(Id, TypeDefinitions.Size);
			TypeDefinitions.Push(Type);
		}
		
		var Definitions = mArrayList.List<mVM_Data.tProcDef<tPos>>();
		var DefIdToIndex = mTreeMap.Tree<tText, tNat32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign(), []);
		
		foreach (var Def in aModule.Defs) {
			var DefType = Types.TryGet(Def.Type).AssertNotEmpty();
			if (DefType.IsRecursive(out var RecursiveHead, out var RecursiveBody)) {
				DefType = RecursiveBody.Substitute(RecursiveHead, DefType);
			}
			
			mAssert.IsTrue(DefType.IsProc(out _, out var EnvType, out var FunctionType));
			
			while (FunctionType.IsGeneric(out _, out var Body)) {
				FunctionType = Body;
			}
			
			mAssert.IsTrue(FunctionType.IsProc(out var ObjectType, out var ArgumentType, out var ReturnType));
			
			var Proc = new mVM_Data.tProcDef<tPos>(DefType) { TypeDefinitions = TypeDefinitions.ToStream() };
			var RegIdToIndex = mTreeMap.Tree<tText, tNat32>((a1, a2) => tText.CompareOrdinal(a1, a2).Sign(), []);
			var RegBuffer = mArrayList.List<(mVM_Type.tType Type, mVM_Data.tData? Data)>();
			
			void
			Builtin(
				tText aId,
				mVM_Type.tType aType,
				mVM_Data.tData? aKnown = null
			) {
				RegIdToIndex = RegIdToIndex.Set(aId, RegBuffer.Size);
				RegBuffer.Push((aType, aKnown));
			}
			
			Builtin(mIL_AST.cEmptyValue, mVM_Type.Empty(), mVM_Data.Empty());
			Builtin(mIL_AST.cOne, mVM_Type.Int(), mVM_Data.Int(1));
			Builtin(mIL_AST.cFalse, mVM_Type.False(), mVM_Data.Bool(false));
			Builtin(mIL_AST.cTrue, mVM_Type.True(), mVM_Data.Bool(true));
			Builtin(mIL_AST.cEmptyType, mVM_Type.Type(), mVM_Data.TypeEmpty());
			Builtin(mIL_AST.cIntType, mVM_Type.Type(), mVM_Data.TypeInt());
			Builtin(mIL_AST.cTypeType, mVM_Type.Type(), mVM_Data.TypeType());
			Builtin(mIL_AST.cEnv, EnvType);
			Builtin(mIL_AST.cObj, ObjectType);
			Builtin(mIL_AST.cArg, ArgumentType);
			Builtin(mIL_AST.cRes, ReturnType);
			
			foreach (var Command in Def.Commands) {
				tNat32
				Emit(
					tIL_CommandType aKind,
					tNat32 aReg1Index = 0,
					tNat32 aReg2Index = 0,
					mVM_Type.tType? aType = null,
					mVM_Data.tData? aData = null
				) {
					var Opcode = aKind switch {
						tIL_CommandType.AddField => mVM_Data.tOpCode.AddField,
						tIL_CommandType.Assert => mVM_Data.tOpCode.Assert,
						tIL_CommandType.BoolAnd => mVM_Data.tOpCode.And,
						tIL_CommandType.BoolOr => mVM_Data.tOpCode.Or,
						tIL_CommandType.BoolXOr => mVM_Data.tOpCode.XOr,
						tIL_CommandType.CallFunc or tIL_CommandType.Closure => mVM_Data.tOpCode.CallFunc,
						tIL_CommandType.CallProc => mVM_Data.tOpCode.CallProc,
						tIL_CommandType.DefRecProcs => mVM_Data.tOpCode.DefRecProcs,
						tIL_CommandType.First => mVM_Data.tOpCode.First,
						tIL_CommandType.GetField => mVM_Data.tOpCode.GetField,
						tIL_CommandType.Int => mVM_Data.tOpCode.NewInt,
						tIL_CommandType.IntsAdd => mVM_Data.tOpCode.IntsAdd,
						tIL_CommandType.IntsAreEq => mVM_Data.tOpCode.IntsAreEq,
						tIL_CommandType.IntsComp => mVM_Data.tOpCode.IntsComp,
						tIL_CommandType.IntsDiv => mVM_Data.tOpCode.IntsDiv,
						tIL_CommandType.IntsMul => mVM_Data.tOpCode.IntsMul,
						tIL_CommandType.IntsSub => mVM_Data.tOpCode.IntsSub,
						tIL_CommandType.Pair => mVM_Data.tOpCode.NewPair,
						tIL_CommandType.PrefixApply => mVM_Data.tOpCode.AddPrefix,
						tIL_CommandType.PrefixRemove => mVM_Data.tOpCode.DelPrefix,
						tIL_CommandType.ReturnIf => mVM_Data.tOpCode.ReturnIf,
						tIL_CommandType.ReturnIfNotEmpty => mVM_Data.tOpCode.ReturnIfNotEmpty,
						tIL_CommandType.Second => mVM_Data.tOpCode.Second,
						tIL_CommandType.Sig => mVM_Data.tOpCode.NewSig,
						tIL_CommandType.Interface => mVM_Data.tOpCode.NewInterface,
						tIL_CommandType.InterfaceView => mVM_Data.tOpCode.InterfaceView,
						tIL_CommandType.SigBody => mVM_Data.tOpCode.SigBody,
						tIL_CommandType.SigHead or tIL_CommandType.SigHeadAs => mVM_Data.tOpCode.SigHead,
						tIL_CommandType.TryAsBool => mVM_Data.tOpCode.TryAsBool,
						tIL_CommandType.TryAsEmpty => mVM_Data.tOpCode.TryAsEmpty,
						tIL_CommandType.TryAsInt => mVM_Data.tOpCode.TryAsInt,
						tIL_CommandType.TryAsPair => mVM_Data.tOpCode.TryAsPair,
						tIL_CommandType.TryAsRecord => mVM_Data.tOpCode.TryAsRecord,
						tIL_CommandType.TryAsRef => mVM_Data.tOpCode.TryAsRef,
						tIL_CommandType.TryAsSig => mVM_Data.tOpCode.TryAsSig,
						tIL_CommandType.TryAsType => mVM_Data.tOpCode.TryAsType,
						tIL_CommandType.TryAsVar => mVM_Data.tOpCode.TryAsVar,
						tIL_CommandType.TryHasHeadType => mVM_Data.tOpCode.TryHasHeadType,
						tIL_CommandType.TryRemovePrefixFrom => mVM_Data.tOpCode.TryRemovePrefixFrom,
						tIL_CommandType.TryReturn => mVM_Data.tOpCode.TryReturn,
						tIL_CommandType.TypeAbstract => mVM_Data.tOpCode.TypeAbstract,
						tIL_CommandType.TypeFree => mVM_Data.tOpCode.TypeFree,
						tIL_CommandType.TypeFunc => mVM_Data.tOpCode.TypeFunc,
						tIL_CommandType.TypeGeneric => mVM_Data.tOpCode.TypeGeneric,
						tIL_CommandType.TypeGenericApply => mVM_Data.tOpCode.TypeGenericApply,
						tIL_CommandType.TypeInterface => mVM_Data.tOpCode.TypeInterface,
						tIL_CommandType.TypeMethod => mVM_Data.tOpCode.TypeMeth,
						tIL_CommandType.TypePair => mVM_Data.tOpCode.TypePair,
						tIL_CommandType.TypePrefix => mVM_Data.tOpCode.TypePrefix,
						tIL_CommandType.TypeRecord => mVM_Data.tOpCode.TypeRecord,
						tIL_CommandType.TypeRecursive => mVM_Data.tOpCode.TypeRecursive,
						tIL_CommandType.TypeSet => mVM_Data.tOpCode.TypeSet,
						tIL_CommandType.TypeSig => mVM_Data.tOpCode.TypeSig,
						tIL_CommandType.TypeVar => mVM_Data.tOpCode.TypeVar,
						tIL_CommandType.VarDef => mVM_Data.tOpCode.VarDef,
						tIL_CommandType.VarGet => mVM_Data.tOpCode.VarGet,
						tIL_CommandType.VarSet => mVM_Data.tOpCode.VarSet,
						_ => throw mError.Error($"invalid IL command: {aKind}"),
					};
					
					if (aKind is tIL_CommandType.CallFunc or tIL_CommandType.Closure or tIL_CommandType.CallProc) {
						var Function = RegBuffer.Get(aReg1Index).Type;
						var Object = mVM_Type.Empty();
						if (aKind is tIL_CommandType.CallProc) {
							mAssert.IsTrue(Function.IsPair(out Object, out Function));
							if (Function.IsPrefix(out var Name, out var Marker) && Marker.IsEmpty()) {
								Function = Object.FirstObjectType().TryGetInterfaceMethod(Name).AssertNotEmpty();
							}
						}
						var Call = mVM_Type.InferCall(Function, Object, RegBuffer.Get(aReg2Index).Type, __ => {}).ElseThrow();
						if (Call.Object.HasInterfaceValues() || Call.Argument.HasInterfaceValues() || Call.Result.HasInterfaceValues()) {
							// Later MATCH refinements may change the register types, not this call's contract.
							Proc.CallTypes = Proc.CallTypes.Set(Proc.Commands.Size, mVM_Type.Proc(Call.Object, Call.Argument, Call.Result));
						}
					}
					Proc.PosList.Push(Command.Pos);
					Proc.Commands.Push((Opcode, aReg1Index, aReg2Index, mStd.NewDebugId()));
					
					if (aType is null) {
						return Proc._LastReg;
					}
					
					var RegIndex = RegBuffer.Size;
					RegBuffer.Push((aType, aData));
					Proc._LastReg = RegIndex;
					
					return RegIndex;
				}
				
				tNat32
				GetRegIndex(
					tText aRegId
				) {
					if (RegIdToIndex.TryGet(aRegId).IsSome(out var Reg)) {
						return Reg;
					}
					
					var Type = Types.TryGet(aRegId).AssertNotEmpty();
					Reg = Proc.LoadType(Command.Pos, TypeIdToIndex.TryGet(aRegId).AssertNotEmpty());
					RegBuffer.Push((Type.KindType(), mVM_Data.TypeExpression(Type)));
					RegIdToIndex = RegIdToIndex.Set(aRegId, Reg);
					
					return Reg;
				}
				
				mVM_Type.tType
				TypeValue(
					tText aRegId
				) {
					var RegIndex = GetRegIndex(aRegId);
					var (Type, Data) = RegBuffer.Get(RegIndex);
					
					if (Data is not null && !Data.IsEmpty()) {
						return Data.TypeExpressionValue();
					}
					
					var AbstractType = mVM_Type.Abstract(aRegId, Type);
					RegBuffer.Set(RegIndex, (Type, mVM_Data.TypeExpression(AbstractType)));
					
					return AbstractType;
				}
				
				var ResId = Command._1;
				var IL_CommandType = Command.NodeType;
				
				if (IL_CommandType is >= tIL_CommandType._BeginTypes_ and < tIL_CommandType._EndTypes_) {
					var (Reg1Index_, Reg1_Type_) = (
						Command._2.IsSome(out var Id2) && IL_CommandType is not tIL_CommandType.TypePrefix
						? (GetRegIndex(Id2), TypeValue(Id2))
						: (mVM_Data.cTypeTypeReg, null!)
					);
					
					var (Reg2Index_, Reg2_Type_) = (
						Command._3.IsSome(out var Id3)
						? (GetRegIndex(Id3), TypeValue(Id3))
						: (0, null!)
					);
					
					var Type = mIL_TypeChecker.TypeExpression(Command, Reg1_Type_, Reg2_Type_);
					
					if (IL_CommandType is tIL_CommandType.TypePrefix) {
						Reg1Index_ = Proc.TypePrefixes.Size;
						Proc.TypePrefixes.Push(Command._2.AssertNotEmpty());
					}
					
					RegIdToIndex = RegIdToIndex.Set(
						ResId,
						Emit(IL_CommandType, Reg1Index_, Reg2Index_, Type.KindType(), mVM_Data.TypeExpression(Type))
					);
					
					continue;
				}
				if (IL_CommandType is tIL_CommandType.Alias) {
					RegIdToIndex = RegIdToIndex.Set(ResId, GetRegIndex(Command._2.AssertNotEmpty()));
					continue;
				}
				if (IL_CommandType is tIL_CommandType.Int) {
					RegIdToIndex = RegIdToIndex.Set(
						ResId,
						Emit(IL_CommandType, (tNat32)tInt32.Parse(Command._2.AssertNotEmpty()), aType: mVM_Type.Int())
					);
					continue;
				}
				if (IL_CommandType is tIL_CommandType.VarSet or tIL_CommandType.Assert or tIL_CommandType.ReturnIf) {
					Emit(IL_CommandType, GetRegIndex(ResId), GetRegIndex(Command._2.AssertNotEmpty()));
					continue;
				}
				if (IL_CommandType is tIL_CommandType.TryReturn) {
					var Function = GetRegIndex(ResId);
					var Argument = GetRegIndex(Command._2.AssertNotEmpty());
					var Type = RegBuffer.Get(Function).Type;
					
					while (Type.IsGeneric(out var Parameter, out var Body)) {
						Type = Body.Substitute(Parameter, mVM_Type.Free(Parameter.KindType()));
					}
					
					mAssert.IsTrue(Type.IsProc(out _, out var ExpectedArgument, out var Result));
					
					if (Command._3.IsSome(out var GuardId)) {
						var Guard = GetRegIndex(GuardId);
						Function = Emit(tIL_CommandType.Pair, Function, Guard, mVM_Type.Pair(Type, RegBuffer.Get(Guard).Type));
					} else if (RegBuffer.Get(Argument).Type.Subtract(ExpectedArgument).IsSome(out var Remainder)) {
						RegBuffer.Update(Argument, __ => (Remainder, __.Data));
					}
					
					var Temporary = Emit(tIL_CommandType.TryReturn, Function, Argument, mVM_Type.Empty());
					Emit(tIL_CommandType.ReturnIfNotEmpty, Temporary);
					var (_, Success) = Result.SplitBy(__ => __.IsEmpty());
					
					if (Success.IsSome(out var SuccessType)) {
						ReturnType = mVM_Type.Set(ReturnType, SuccessType);
					}
					continue;
				}
				var Reg1 = Command._2.AssertNotEmpty();
				var Reg2 = Command._3.ElseUse("");
				var Reg1_Id = (
					IL_CommandType is tIL_CommandType.PrefixApply or tIL_CommandType.PrefixRemove
					? GetRegIndex(Reg2)
					: GetRegIndex(Reg1)
				);
				var (Reg1_Type, Reg1_Data) = RegBuffer.Get(Reg1_Id);
				
				mVM_Type.tType Res_Type;
				mVM_Data.tData? Res_Value = null;
				var Reg2_Id = 0u;
				switch (IL_CommandType) {
					case tIL_CommandType.ReturnIfNotEmpty: {
						var (_, Returned) = Reg1_Type.SplitBy(__ => __.IsEmpty());
						if (Returned.IsSome(out var Type)) {
							ReturnType = mVM_Type.Set(ReturnType, Type);
						}
						Emit(IL_CommandType, Reg1_Id);
						RegBuffer.Update(Reg1_Id, __ => (mVM_Type.Empty(), __.Data));
						continue;
					}
					case tIL_CommandType.First:
					case tIL_CommandType.Second: {
						mAssert.IsTrue(Reg1_Type.TryProjectPair(out var First, out var Second));
						Res_Type = IL_CommandType is tIL_CommandType.First ? First : Second;
						if (Reg1_Data is not null && Reg1_Data.IsPair(out var FirstData, out var SecondData)) {
							Res_Value = IL_CommandType is tIL_CommandType.First ? FirstData : SecondData;
						}
						break;
					}
					case tIL_CommandType.PrefixApply:
					case tIL_CommandType.PrefixRemove: {
						if (IL_CommandType is tIL_CommandType.PrefixApply) {
							Res_Type = mVM_Type.Prefix(Reg1, Reg1_Type);
						} else {
							mAssert.IsTrue(Reg1_Type.IsPrefix(Reg1, out Res_Type!));
						}
						
						Reg2_Id = Reg1_Id;
						Reg1_Id = Reg1.PrefixHash();
						break;
					}
					case tIL_CommandType.GetField: {
						Res_Type = Reg1_Type.GetFieldType(Reg2);
						Reg2_Id = Reg2.PrefixHash();
						break;
					}
					case tIL_CommandType.VarDef: {
						Res_Type = mVM_Type.Var(Reg1_Type);
						break;
					}
					case tIL_CommandType.VarGet: {
						mAssert.IsTrue(Reg1_Type.IsVar(out Res_Type!));
						break;
					}
					case tIL_CommandType.DefRecProcs: {
						mAssert.IsTrue(Reg1_Type.IsProc(out _, out _, out var RecFunction));
						mAssert.IsTrue(RecFunction.IsProc(out _, out _, out Res_Type!));
						Reg2_Id = GetRegIndex(Reg2);
						break;
					}
					case tIL_CommandType.SigHead:
					case tIL_CommandType.SigHeadAs:
					case tIL_CommandType.SigBody: {
						mAssert.IsTrue(Reg1_Type.IsSig(out var Head, out var Body));
						if (IL_CommandType is tIL_CommandType.SigHeadAs) {
							var Witness = Types.TryGet(Reg2).AssertNotEmpty();
							Body = Body.Substitute(Head, Witness);
							Head = Witness;
						} else if (Head.Kind is mVM_Type.tKind.Free) {
							var Witness = mVM_Type.Abstract(Reg1, Head.KindType());
							Body = Body.Substitute(Head, Witness);
							Head = Witness;
						}
						
						RegBuffer.Update(Reg1_Id, __ => (mVM_Type.Sig(Head, Body), __.Data));
						(Res_Type, Res_Value) = (
							IL_CommandType is tIL_CommandType.SigBody
							? (Body, null)
							: (Head.KindType(), mVM_Data.TypeExpression(Head))
						);
						
						break;
					}
					case tIL_CommandType.TryAsSig: {
						var Contract = Types.TryGet(Reg2).AssertNotEmpty();
						mAssert.IsTrue(Contract.IsSig(out var Binder, out var Body));
						var AbstractType = mVM_Type.Abstract(ResId, Binder.KindType());
						Res_Type = mVM_Type.Sig(AbstractType, Body.Substitute(Binder, AbstractType));
						Reg2_Id = TypeIdToIndex.TryGet(Reg2).AssertNotEmpty();
						break;
					}
					case tIL_CommandType.TryHasHeadType: {
						mAssert.IsTrue(Reg1_Type.IsSig(out var Head, out var Body));
						var Expected = TypeValue(Reg2);
						Res_Type = mVM_Type.Sig(Expected, Body.Substitute(Head, Expected));
						Reg2_Id = GetRegIndex(Reg2);
						break;
					}
					case tIL_CommandType.CallProc: {
						Reg2_Id = GetRegIndex(Reg2);
						mAssert.IsTrue(Reg1_Type.IsPair(out var Object, out var Method));
						if (Method.IsPrefix(out var Name, out var Marker) && Marker.IsEmpty()) {
							Method = Object.FirstObjectType().TryGetInterfaceMethod(Name).AssertNotEmpty();
						}
						Res_Type = mVM_Type.Infer(Method, Object, RegBuffer.Get(Reg2_Id).Type, __ => {}).ElseThrow();
						break;
					}
					case tIL_CommandType.TryAsEmpty:
					case tIL_CommandType.TryAsBool:
					case tIL_CommandType.TryAsInt:
					case tIL_CommandType.TryAsType:
					case tIL_CommandType.TryAsPair:
					case tIL_CommandType.TryAsRecord:
					case tIL_CommandType.TryAsVar:
					case tIL_CommandType.TryAsRef:
					case tIL_CommandType.TryRemovePrefixFrom: {
						var (Success, Failure) = Reg1_Type.SplitBy(
							__ => IL_CommandType switch {
								tIL_CommandType.TryAsEmpty => __.IsEmpty(),
								tIL_CommandType.TryAsBool => __.Kind is mVM_Type.tKind.True or mVM_Type.tKind.False,
								tIL_CommandType.TryAsInt => __.IsInt(),
								tIL_CommandType.TryAsType => __.IsType(),
								tIL_CommandType.TryAsPair => __.IsPair(out _, out _),
								tIL_CommandType.TryAsRecord => __.IsRecord(out _),
								tIL_CommandType.TryAsVar => __.IsVar(out _),
								tIL_CommandType.TryAsRef => __.IsRef(out _),
								_ => __.IsPrefix(Reg2, out _),
							}
						);
						
						Res_Type = Success.AssertNotEmpty();
						
						if (Failure.IsSome(out var FailureType)) {
							ReturnType = mVM_Type.Set(ReturnType, FailureType);
						}
						
						if (IL_CommandType is tIL_CommandType.TryRemovePrefixFrom) {
							mAssert.IsTrue(Res_Type.IsPrefix(Reg2, out Res_Type!));
							Reg2_Id = Reg1_Id;
							Reg1_Id = Reg2.PrefixHash();
						}
						
						break;
					}
					default: {
						Reg2_Id = GetRegIndex(Reg2);
						var Reg2_Type = RegBuffer.Get(Reg2_Id).Type;
						
						Res_Type = IL_CommandType switch {
							tIL_CommandType.BoolAnd or
							tIL_CommandType.BoolOr or
							tIL_CommandType.BoolXOr or
							tIL_CommandType.IntsAreEq => mVM_Type.Bool(),
							
							tIL_CommandType.IntsComp or
							tIL_CommandType.IntsAdd or
							tIL_CommandType.IntsSub or
							tIL_CommandType.IntsMul => mVM_Type.Int(),
							
							tIL_CommandType.IntsDiv => mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Int()),
							
							tIL_CommandType.Pair => mVM_Type.Pair(Reg1_Type, Reg2_Type),
							
							tIL_CommandType.AddField => mVM_Type.Record(Reg1_Type, Reg2_Type),
							
							tIL_CommandType.CallFunc or
							tIL_CommandType.Closure => mVM_Type.Infer(Reg1_Type, mVM_Type.Empty(), Reg2_Type, __ => {}).ElseThrow(),
							
							tIL_CommandType.Sig or tIL_CommandType.Interface or tIL_CommandType.InterfaceView => TypeValue(Reg1),
							
							_ => throw mError.Error("unsupported valid IL command: " + IL_CommandType),
						};
						
						if (IL_CommandType is tIL_CommandType.Pair) {
							if (Reg1_Type.IsType() || Reg1_Type.IsTypeFunctionKind()) {
								TypeValue(Reg1);
							}
							var Data1 = RegBuffer.Get(Reg1_Id).Data ?? mVM_Data.Empty();
							var Data2 = RegBuffer.Get(Reg2_Id).Data ?? mVM_Data.Empty();
							Res_Value = !Data1.IsEmpty() || !Data2.IsEmpty() ? mVM_Data.Pair(Data1, Data2) : null;
						}
						
						break;
					}
				}
				RegIdToIndex = RegIdToIndex.Set(ResId, Emit(IL_CommandType, Reg1_Id, Reg2_Id, Res_Type, Res_Value));
			}
			
			RegBuffer.Update(mVM_Data.cResReg, __ => (ReturnType, __.Data));
			Proc.Types.Push(RegBuffer.ToStream().Map(__ => __.Type).ToArrayList().ToArray());
			DefIdToIndex = DefIdToIndex.Set(Def.Id, Definitions.Size);
			Definitions.Push(Proc);
		}
		
		return (Definitions.ToStream(), DefIdToIndex);
	}
}
