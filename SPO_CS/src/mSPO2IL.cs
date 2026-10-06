#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mAssert.cs
#:ref Common/mError.cs
#:ref Common/mTreeMap.cs
#:ref Common/mMath.cs
#:ref Common/mMaybe.cs
#:ref Common/mResult.cs
#:ref Common/mArrayList.cs
#:ref Common/mStream.cs
#:ref Common/mPerf.cs
#:ref mVM_Type.cs
#:ref mIL_AST.cs
#:ref mIL_GenerateOpcodes.cs
#:ref mSPO_AST.cs
#:ref mSPO_AST_Types.cs

public static class
mSPO2IL {
	public struct
	tTypeDeclarations<tPos> {
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> TypeDef;
		public mTreeMap.tTree<tText, mVM_Type.tType> Types; // TypeText to TypeDef
		public mTreeMap.tTree<mVM_Type.tType, tText> TypeIds;
	}

	public sealed class
	tModuleConstructor<tPos> {
		public tTypeDeclarations<tPos> TypeDeclarations;
		public mArrayList.tArrayList<(tText? TypeId, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands)> Defs;
		internal mStd.tFunc<tPos, tPos, tPos> MergePos;
	}
	
	public struct
	tDefConstructor<tPos> {
		public mSPO_AST_Types.tTypeState<tPos> TypeState { get; init; }
		// TODO: add SubDefs
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands;
		public tNat32 LastTempReg;
		public mArrayList.tArrayList<tText> ArgIds;
		public mArrayList.tArrayList<tText> EnvIds;
		public mArrayList.tArrayList<tText> LocalIds;
		public mTreeMap.tTree<tText, mVM_Type.tType> TypeDict;
	}
	
	public static void
	AddEnv<t>(
		this ref tDefConstructor<t> aDefConstructor,
		tText aId,
		mVM_Type.tType aType
	) {
		mAssert.IsFalse(aDefConstructor.ArgIds.ToStream().Any(__ => __ == aId));
		mAssert.IsFalse(aDefConstructor.LocalIds.ToStream().Any(__ => __ == aId));
		aDefConstructor.EnvIds.Push(aId);
		aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(aId, aType);
	}
	
	public static void
	AddArg<t>(
		this ref tDefConstructor<t> aDefConstructor,
		tText aId,
		mVM_Type.tType aType
	) {
		aDefConstructor.ArgIds.Push(aId);
		aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(aId, aType);
	}
	
	public static void
	AddLocal<t>(
		this ref tDefConstructor<t> aDefConstructor,
		tText aId,
		mVM_Type.tType aType
	) {
		aDefConstructor.LocalIds.Push(aId);
		aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(aId, aType);
	}
	
	public static tModuleConstructor<tPos>
	NewModuleConstructor<tPos>(
		mStd.tFunc<tPos, tPos, tPos> aMergePos
	) => new() {
		Defs = mArrayList.List<(tText? Type, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Def)>(),
		TypeDeclarations = new() {
			TypeDef = mArrayList.List<mIL_AST.tCommandNode<tPos>>(),
			Types = mTreeMap.Tree<tText, mVM_Type.tType>((a1, a2) => mMath.Sign(tText.CompareOrdinal(a1, a2)), []),
			TypeIds = mTreeMap.Tree<mVM_Type.tType, tText>((A, B) => A.DebugId.CompareTo(B.DebugId), []),
		},
		MergePos = aMergePos,
	};
	
	public static tDefConstructor<tPos>
	NewDefConstructor<tPos>(
		mSPO_AST_Types.tTypeState<tPos> aTypeState
	) => new () {
		TypeState = aTypeState,
		Commands = mArrayList.List<mIL_AST.tCommandNode<tPos>>(),
		EnvIds = mArrayList.List<tText>(),
		ArgIds = mArrayList.List<tText>(),
		LocalIds = mArrayList.List<tText>(),
		TypeDict = mTreeMap.Tree<tText, mVM_Type.tType>((a1, a2) => mMath.Sign(tText.CompareOrdinal(a1, a2)), []),
	};
	
	public static tText GetRegId(tNat32 a) => "r_" + a;
	public static tText GetDefId(tNat32 a) => "d_" + a;
	public static tText GetTypeId(tNat32 a) => "t_" + a;
	public static tText GetId(tText a) => "_" + a;
	
	public static tText
	CreateTempReg<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor
	) {
		aDefConstructor.LastTempReg += 1;
		return GetRegId(aDefConstructor.LastTempReg);
	}
	
	public static tText
	CreateTempReg<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		out tText aTempReg
	) {
		aTempReg = aDefConstructor.CreateTempReg();
		return aTempReg;
	}
	
	public static mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>
	UnrollEnv<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tText aReg,
		mStream.tStream<tText> aEnvSymbols
	) {
		var ExtractEnv = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		
		switch (aEnvSymbols.Take(2).Count()) {
			case 0: {
				break;
			}
			case 1: {
				ExtractEnv.Push(
					mIL_AST.Alias(
						aPos,
						aEnvSymbols.TryFirst().AssertNotEmpty(),
						aReg
					)
				);
				
				break;
			}
			default: {
				var RestEnv = aReg;
				
				foreach (var Symbol in aEnvSymbols.Reverse()) {
					ExtractEnv.Push(
						[
							mIL_AST.GetSecond(aPos, Symbol, RestEnv),
							mIL_AST.GetFirst(aPos, aDefConstructor.CreateTempReg(out var NewRestEnv), RestEnv),
						]
					);
					RestEnv = NewRestEnv;
				}
				
				break;
			}
		}
		
		return ExtractEnv;
	}
	
	public static void
	SetType<tPos>(
		this ref tTypeDeclarations<tPos> aTypeDeclarations,
		tText aTypeId,
		mVM_Type.tType aType
	) {
		aTypeDeclarations.Types = aTypeDeclarations.Types.Set(aTypeId, aType);
		aTypeDeclarations.TypeIds = aTypeDeclarations.TypeIds.Set(aType, aTypeId);
	}
	
	public static void
	EnsureTypeDefinition<tPos>(
		this ref tTypeDeclarations<tPos> aTypeDeclarations,
		tText aTypeId,
		mVM_Type.tType aType,
		mStd.tFunc<mIL_AST.tCommandNode<tPos>> aCreateDefinition
	) {
		if (aTypeDeclarations.Types.TryGet(aTypeId).IsSome(out var ExistingType)) {
			mAssert.AreEquals(ExistingType, aType);
		} else {
			aTypeDeclarations.TypeDef.Push(aCreateDefinition());
		}
		aTypeDeclarations.SetType(aTypeId, aType);
	}
	
	public static tText
	MapType<tPos>(
		this ref tTypeDeclarations<tPos> aTypeDeclarations,
		mVM_Type.tType aType
	) {
		if (aTypeDeclarations.TypeIds.TryGet(aType).IsSome(out var Existing)) {
			return Existing;
		}
		
		switch (aType) {
			case var a when a.Kind is mVM_Type.tKind.Sig or mVM_Type.tKind.TypeApply: {
				var Head = aTypeDeclarations.MapType(a.Refs[0]);
				var Body = aTypeDeclarations.MapType(a.Refs[1]);
				var Id = $"[{a.Kind} {Head} {Body}]";
				
				aTypeDeclarations.EnsureTypeDefinition(
					Id, a, () => a.Kind is mVM_Type.tKind.Sig
						? mIL_AST.TypeSig(default(tPos)!, Id, Head, Body)
						: mIL_AST.TypeGenericApply(default(tPos)!, Id, Head, Body)
				);
				
				return Id;
			}
			case var a when a.IsType(): {
				aTypeDeclarations.SetType(mIL_GenerateOpcodes.cTypeType, a);
				return mIL_GenerateOpcodes.cTypeType;
			}
			case var a when a.IsEmpty(): {
				aTypeDeclarations.SetType(mIL_GenerateOpcodes.cEmptyType, a);
				return mIL_GenerateOpcodes.cEmptyType;
			}
			case var a when a.Kind is mVM_Type.tKind.True: {
				aTypeDeclarations.SetType(mIL_AST.cTrue, a);
				return mIL_AST.cTrue;
			}
			case var a when a.Kind is mVM_Type.tKind.False: {
				aTypeDeclarations.SetType(mIL_AST.cFalse, a);
				return mIL_AST.cFalse;
			}
			case var a when a.IsInt(): {
				aTypeDeclarations.SetType(mIL_GenerateOpcodes.cIntType, a);
				return mIL_GenerateOpcodes.cIntType;
			}
			case var a when a.IsAny(): {
				aTypeDeclarations.SetType(mIL_GenerateOpcodes.cAnyType, a);
				return mIL_GenerateOpcodes.cAnyType;
			}
			case var a when a.Kind is mVM_Type.tKind.Abstract: {
				var Kind = aTypeDeclarations.MapType(a.KindType());
				var Id = "head_" + aTypeDeclarations.TypeDef.Size;
				
				aTypeDeclarations.EnsureTypeDefinition(
					Id, a, () => mIL_AST.TypeAbstract(default(tPos)!, Id, Kind)
				);
				
				return Id;
			}
			case var a when a.Kind is mVM_Type.tKind.Free: {
				if (!mStd.RefEq(a, a.Refs[0])) {
					var ReferencedId = aTypeDeclarations.MapType(a.Refs[0]);
					aTypeDeclarations.SetType(ReferencedId, a);
					return ReferencedId;
				}
				
				var Kind = aTypeDeclarations.MapType(a.KindType());
				var Id = "free_" + aTypeDeclarations.TypeDef.Size;
				
				aTypeDeclarations.EnsureTypeDefinition(Id, a, () => mIL_AST.TypeFree(default(tPos)!, Id, Kind));
				
				return Id;
			}
			case var a when a.IsPrefix(out var Prefix, out var Type): {
				var Id = aTypeDeclarations.MapType(Type);
				var NewId = $"[#{Prefix}:{Id}]";
				
				aTypeDeclarations.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypePrefix(default(tPos)!, NewId, Prefix, Id));
				
				return NewId;
			}
			case var a when a.IsPair(out var Type1, out var Type2): {
				var Id1 = aTypeDeclarations.MapType(Type1);
				var Id2 = aTypeDeclarations.MapType(Type2);
				var NewId = $"[{Id1};{Id2}]";
				
				aTypeDeclarations.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypePair(default(tPos)!, NewId, Id1, Id2));
				
				return NewId;
			}
			case var a when a.IsRecord(out var Fields): {
				var RecTypeId = mIL_AST.cEmptyType;
				foreach (var Field in Fields.ToStream()) {
					var FieldTypeId = aTypeDeclarations.MapType(Field.Value);
					var PrefixedFieldTypeId = $"[#{Field.Key} {FieldTypeId}]";
					
					aTypeDeclarations.TypeDef.Push(mIL_AST.TypePrefix(default(tPos)!, PrefixedFieldTypeId, Field.Key.ToString(), FieldTypeId)); // TODO: remove .ToString() ???
					
					var NewRecTypeId = $"[{RecTypeId}, {PrefixedFieldTypeId}]";
					
					aTypeDeclarations.TypeDef.Push(mIL_AST.TypeRecord(default(tPos)!, NewRecTypeId, RecTypeId, PrefixedFieldTypeId));
					
					RecTypeId = NewRecTypeId;
				}
				aTypeDeclarations.SetType(RecTypeId, a);
				return RecTypeId;
			}
			case var a when a.IsSet(out var Type1, out var Type2): {
				var Id1 = aTypeDeclarations.MapType(Type1);
				var Id2 = aTypeDeclarations.MapType(Type2);
				var NewId = $"[{Id1}|{Id2}]";
				
				aTypeDeclarations.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeSet(default(tPos)!, NewId, Id1, Id2));
				
				return NewId;
			}
			case var a when a.IsProc(out var EnvType, out var ArgType, out var ResType): {
				var IdArg = aTypeDeclarations.MapType(ArgType);
				var IdRes = aTypeDeclarations.MapType(ResType);
				var IdFunc = $"[{IdArg}->{IdRes}]";
				var FuncType = mVM_Type.Proc(mVM_Type.Empty(), ArgType, ResType);
				
				aTypeDeclarations.EnsureTypeDefinition(IdFunc, FuncType, () => mIL_AST.TypeFunc(default(tPos)!, IdFunc, IdArg, IdRes));
				
				if (EnvType.IsEmpty()) {
					aTypeDeclarations.SetType(IdFunc, a);
					return IdFunc;
				}
				
				var IdEnv = aTypeDeclarations.MapType(EnvType);
				var IdEnvFunc = $"[{IdEnv}:{IdFunc}]";
				
				aTypeDeclarations.EnsureTypeDefinition(IdEnvFunc, a, () => mIL_AST.TypeMethod(default(tPos)!, IdEnvFunc, IdEnv, IdFunc));
				
				return IdEnvFunc;
			}
			case var a when a.IsVar(out var InnerType): {
				var InnerId = aTypeDeclarations.MapType(InnerType);
				var NewId = $"[§VAR {InnerId}]";
				
				aTypeDeclarations.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeVar(default(tPos)!, NewId, InnerId));
				
				return NewId;
			}
			case var a when a.IsRecursive(out var HeadType, out var BodyType): {
				var HeadId = aTypeDeclarations.MapType(HeadType);
				var BodyId = aTypeDeclarations.MapType(BodyType);
				var NewId = $"[§REC {HeadId} => {BodyId}]";
				
				aTypeDeclarations.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeRecursive(default(tPos)!, NewId, HeadId, BodyId));
				
				return NewId;
			}
			case var a when a.IsGeneric(out var HeadType, out var BodyType): {
				var HeadId = aTypeDeclarations.MapType(HeadType);
				var BodyId = aTypeDeclarations.MapType(BodyType);
				var NewId = $"[$ALL {HeadId} => {BodyId}]";
				
				aTypeDeclarations.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeGeneric(default(tPos)!, NewId, HeadId, BodyId));
				
				return NewId;
			}
			default: {
				throw new System.NotImplementedException("" + aType.Kind);
			}
		}
	}
	
	public static mResult.tResult<mVM_Type.tType, tText>
	CreateEnvType<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor
	) {
		var TypeDict = aDefConstructor.TypeDict;
		var EnvIds = aDefConstructor.EnvIds;
		
		return EnvIds.ToStream(
		).Map(
			__ => {
				var MaybeType = TypeDict.TryGet(__);
				
				if (MaybeType.IsSome(out var T)) {
					return T;
				}
				
				if (!__.StartsWith("d_")) {
					throw mError.Error($"'{__}' id not a Def");
				}
				
				var DefIndexText = __[2..];
				var TypeName = aModuleConstructor.Defs.Get(tNat32.Parse(DefIndexText)).TypeId;
				
				return aModuleConstructor.TypeDeclarations.Types.TryGet(TypeName).ElseFail(
					() => $"can't find type '{TypeName}'"
				);
			}
		).WhenAllThen(
			mVM_Type.Tuple
		);
	}
	
	public static mResult.tResult<mVM_Type.tType, tText>
	CreateDefType<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mVM_Type.tType aProcType
	) => aDefConstructor.CreateEnvType(
		aModuleConstructor
	).Then(
		__ => mVM_Type.Proc(
			mVM_Type.Empty(),
			__,
			aProcType
		)
	);
	
	public static mResult.tResult<(tNat32 DefIndex, mVM_Type.tType DefType), (tPos Pos, tText ErrorText)>
	MapLambda<tPos>(
		// maps argument and body but not the environment, this is done in FinishMapProc(...)
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	) {
		if (
			!aDefConstructor.MapPattern(ref aModuleConstructor.TypeDeclarations, aLambdaNode.Head, mIL_AST.cArg, out var Error) ||
			!aDefConstructor.TryMapExpression(aModuleConstructor, aLambdaNode.Body).Match(out var ResultReg, out Error)
		) {
			return mResult.Fail(Error);
		}
		
		if (aLambdaNode.Body is not mSPO_AST.tBlockNode<tPos>) {
			aDefConstructor.Commands.Push(
				mIL_AST.ReturnIf(aLambdaNode.Body.Pos, mIL_AST.cTrue, ResultReg)
			);
		}
		
		var Def = aDefConstructor;
		
		return aDefConstructor.CreateDefType(
			aModuleConstructor,
			aDefConstructor.TypeState.TryGetValidatedType(aLambdaNode).AssertNotEmpty()
		).Then(
			aType => {
				var DefIndex = Def.FinishMapProc(
					aLambdaNode.Pos,
					aModuleConstructor,
					aType
				);
				
				return (DefIndex, aType);
			}
		).ModifyError(
			__ => (aLambdaNode.Pos, __)
		);
	}
	
	public static mResult.tResult<(tNat32 DefIndex, mVM_Type.tType DefType), (tPos Pos, tText ErrorText)>
	MapMethod<tPos>(
		// maps argument, object and body but not the environment, this is done in FinishMapProc(...)
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		if (
			!aDefConstructor.MapPattern(ref aModuleConstructor.TypeDeclarations, aMethodNode.Arg, mIL_AST.cArg, out var Error) ||
			!aDefConstructor.MapPattern(ref aModuleConstructor.TypeDeclarations, aMethodNode.Obj, mIL_AST.cObj, out Error) ||
			!aDefConstructor.TryMapExpression(aModuleConstructor, aMethodNode.Body).Match(out var ResultReg, out Error)
		) {
			return mResult.Fail(Error);
		}
		
		aDefConstructor.Commands.Push(
			mIL_AST.ReturnIf(aMethodNode.Pos, mIL_AST.cTrue, ResultReg)
		);
		
		var Def = aDefConstructor;
		
		return aDefConstructor.CreateDefType(
			aModuleConstructor,
			aDefConstructor.TypeState.TryGetValidatedType(aMethodNode).AssertNotEmpty()
		).Then(
			aType => {
				var DefIndex = Def.FinishMapProc(
					aMethodNode.Pos,
					aModuleConstructor,
					aType
				);
				
				return (DefIndex, aType);
			}
		).ModifyError(
			__ => (aMethodNode.Pos, __)
		);
	}
	
	public static tNat32
	FinishMapProc<tPos>(
		// maps finally the environment and puts the finished def into the module
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tModuleConstructor<tPos> aModuleConstructor,
		mVM_Type.tType aDefType
	) {
		mAssert.IsTrue(aDefType.IsProc(out _, out _, out var FuncType));
		//mAssert.IsTrue(FuncType.IsProc(out _, out _, out _));
		
		aDefConstructor.Commands = mArrayList.Concat(
			aDefConstructor.UnrollEnv(
				aPos,
				mIL_AST.cEnv,
				aDefConstructor.EnvIds.ToStream()
			),
			aDefConstructor.Commands
		);
		
		aModuleConstructor.Defs.Push(
			(
				aModuleConstructor.TypeDeclarations.MapType(aDefType),
				aDefConstructor.Commands
			)
		);
		
		return aModuleConstructor.Defs.Size - 1;
	}
	
	public static mResult.tResult<
		(
			tNat32 Index,
			mStream.tStream<mSPO_AST_Types.tScopeItem> EnvList,
			mVM_Type.tType Type
		),
		(
			tPos Pos,
			tText ErrorText
		)
	>
	MapMethod<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode,
		mSPO_AST_Types.tTypeState<tPos> aTypeState
	) {
		var TempMethodDef = NewDefConstructor(aTypeState);
		return TempMethodDef.MapMethod(aModuleConstructor, aMethodNode).Then(
			aDef => {
				var EnvList = TempMethodDef.EnvIds.ToStream(
				).Map(
					__ => mSPO_AST_Types.ScopeItem(
						__,
						TempMethodDef.TypeDict.TryGet(__).AssertNotEmpty()
					)
				);
				
				return (aDef.DefIndex, EnvList, aDef.DefType);
			}
		);
	}
	
	public static tText
	InitProc<tPos>(
		this ref tDefConstructor<tPos> aCallerDefConstructor,
		tPos aPos,
		tNat32 aDefIndex,
		mVM_Type.tType aDefType,
		mStream.tStream<mSPO_AST_Types.tScopeItem> aEnvList,
		tBool aIsRecursiveFactory = false
	) {
		mAssert.IsTrue(aDefType.IsProc(out _, out _, out var FuncType));
		//mAssert.IsTrue(FuncType.IsProc(out _, out _, out _));
		
		var EnvReg = mIL_AST.cEmptyValue;
		if (!aEnvList.IsEmpty()) {
			foreach (var Env in aEnvList) {
				if (aCallerDefConstructor.TypeDict.TryGet(Env.Id).IsNone()) {
					aCallerDefConstructor.AddEnv(Env.Id, Env.Type);
				}
			}
			
			if (aEnvList.Count() is 1) {
				EnvReg = aEnvList.TryFirst().AssertNotEmpty().Id;
			} else {
				foreach (var Env in aEnvList) {
					var NewArgReg = aCallerDefConstructor.CreateTempReg();
					aCallerDefConstructor.Commands.Push(mIL_AST.CreatePair(aPos, NewArgReg, EnvReg, Env.Id));
					EnvReg = NewArgReg;
				}
			}
		}
		var DefId = GetDefId(aDefIndex);
		var ProcReg = aCallerDefConstructor.CreateTempReg();
		aCallerDefConstructor.Commands.Push(
			aIsRecursiveFactory
			? mIL_AST.DefRecProcs(aPos, ProcReg, DefId, EnvReg)
			: mIL_AST.CallFunc(aPos, ProcReg, DefId, EnvReg)
		);
		aCallerDefConstructor.AddEnv(DefId, aDefType);
		return ProcReg;
	}
	
	private static tText
	MapTypeValue<tPos>(
		this ref tDefConstructor<tPos> aDef,
		ref tTypeDeclarations<tPos> aTypeDeclarations,
		tPos aPos,
		mVM_Type.tType aType,
		mStream.tStream<(mVM_Type.tType Type, tText Reg)> aBindings = default
	) {
		foreach (var (Type, Reg) in aBindings) {
			if (mStd.RefEq(Type, aType)) {
				return Reg;
			}
		}
		
		if (
			aType.Kind is not (mVM_Type.tKind.Empty or mVM_Type.tKind.Int or mVM_Type.tKind.True or mVM_Type.tKind.False or mVM_Type.tKind.Type) &&
			!NeedsRuntimeValue(aType, mStd.cEmpty, mStd.cEmpty)
		) {
			if (aTypeDeclarations.TypeIds.TryGet(aType).IsSome(out var TypeId)) {
				return TypeId;
			}
			if (aTypeDeclarations.Types.ToStream().Where(__ => __.Value.SameType(aType)).TryFirst().IsSome(out var Existing)) {
				aTypeDeclarations.TypeIds = aTypeDeclarations.TypeIds.Set(aType, Existing.Key);
				return Existing.Key;
			}
			return aTypeDeclarations.MapType(aType);
		}

		static tBool NeedsRuntimeValue(
			mVM_Type.tType aValue,
			mStream.tStream<mVM_Type.tType> aBound,
			mStream.tStream<mVM_Type.tType> aVisited
		) {
			if (aBound.Any(__ => mStd.RefEq(__, aValue)) || aVisited.Any(__ => mStd.RefEq(__, aValue))) {
				return false;
			}
			aVisited = mStream.Stream(aValue, aVisited);
			if (aValue.Kind is mVM_Type.tKind.Abstract) {
				return true;
			}
			if (aValue.Kind is mVM_Type.tKind.Free) {
				return mStd.RefEq(aValue, aValue.Refs[0]) || NeedsRuntimeValue(aValue.Refs[0], aBound, aVisited);
			}
			if (aValue.Kind is mVM_Type.tKind.Sig or mVM_Type.tKind.Generic or mVM_Type.tKind.Interface or mVM_Type.tKind.Recursive) {
				var Head = aValue.Refs[0];
				return Head.Kind is not mVM_Type.tKind.Free ||
					NeedsRuntimeValue(Head.KindType(), aBound, aVisited) ||
					NeedsRuntimeValue(aValue.Refs[1], mStream.Stream(Head, aBound), aVisited);
			}
			return aValue.Kind is mVM_Type.tKind.Record
				? aValue.Fields.ToStream().Any(__ => NeedsRuntimeValue(__.Value, aBound, aVisited))
				: mStream.Stream(aValue.Refs).Any(__ => NeedsRuntimeValue(__, aBound, aVisited));
		}

		switch (aType.Kind) {
			case mVM_Type.tKind.Empty: return mIL_AST.cEmptyType;
			case mVM_Type.tKind.Int: return mIL_AST.cIntType;
			case mVM_Type.tKind.True: return mIL_AST.cTrue;
			case mVM_Type.tKind.False: return mIL_AST.cFalse;
			case mVM_Type.tKind.Type: return mIL_AST.cTypeType;
			case mVM_Type.tKind.Abstract:
			case mVM_Type.tKind.Free: {
				if (!aDef.TypeDict.TryGet(aType.Id!).IsSome(out _)) {
					aDef.AddEnv(aType.Id!, aType.KindType());
				}
				
				return aType.Id!;
			}
			case mVM_Type.tKind.Generic:
			case mVM_Type.tKind.Recursive:
			case mVM_Type.tKind.Sig: {
				var Head = aType.Refs[0];
				var HeadReg = aDef.CreateTempReg();
				
				var Kind = aDef.MapTypeValue(ref aTypeDeclarations, aPos, Head.KindType(), aBindings);
				aDef.Commands.Push(mIL_AST.TypeFree(aPos, HeadReg, Kind));
				
				var BodyReg = aDef.MapTypeValue(
					ref aTypeDeclarations,
					aPos,
					aType.Refs[1],
					mStream.Stream((Head, HeadReg), aBindings)
				);
				
				var Reg = aDef.CreateTempReg();
				
				aDef.Commands.Push(
					aType.Kind switch {
						mVM_Type.tKind.Generic => mIL_AST.TypeGeneric(aPos, Reg, HeadReg, BodyReg),
						mVM_Type.tKind.Recursive => mIL_AST.TypeRecursive(aPos, Reg, HeadReg, BodyReg),
						_ => mIL_AST.TypeSig(aPos, Reg, HeadReg, BodyReg),
					}
				);
				
				return Reg;
			}
			case mVM_Type.tKind.Proc: {
				var Arg = aDef.MapTypeValue(ref aTypeDeclarations, aPos, aType.Refs[1], aBindings);
				var Res = aDef.MapTypeValue(ref aTypeDeclarations, aPos, aType.Refs[2], aBindings);
				aDef.Commands.Push(mIL_AST.TypeFunc(aPos, aDef.CreateTempReg(out var Func), Arg, Res));
				
				if (aType.Refs[0].IsEmpty()) {
					return Func;
				}
				
				var Obj = aDef.MapTypeValue(ref aTypeDeclarations, aPos, aType.Refs[0], aBindings);
				aDef.Commands.Push(mIL_AST.TypeMethod(aPos, aDef.CreateTempReg(out var Method), Obj, Func));
				
				return Method;
			}
			case mVM_Type.tKind.Record: {
				var Reg = mIL_AST.cEmptyType;
				
				foreach (var Field in aType.Fields.ToStream()) {
					var Value = aDef.MapTypeValue(ref aTypeDeclarations, aPos, Field.Value, aBindings);
					aDef.Commands.Push(mIL_AST.TypePrefix(aPos, aDef.CreateTempReg(out var Prefix), Field.Key, Value));
					aDef.Commands.Push(mIL_AST.TypeRecord(aPos, aDef.CreateTempReg(out var Record), Reg, Prefix));
					Reg = Record;
				}
				
				return Reg;
			}
			default: {
				var Args = mArrayList.List<tText>();
				
				foreach (var Ref in aType.Refs) {
					Args.Push(aDef.MapTypeValue(ref aTypeDeclarations, aPos, Ref, aBindings));
				}
				
				var Reg = aDef.CreateTempReg();
				aDef.Commands.Push(
					aType.Kind switch {
						mVM_Type.tKind.Pair => mIL_AST.TypePair(aPos, Reg, Args.Get(0), Args.Get(1)),
						mVM_Type.tKind.Set => mIL_AST.TypeSet(aPos, Reg, Args.Get(0), Args.Get(1)),
						mVM_Type.tKind.TypeApply => mIL_AST.TypeGenericApply(aPos, Reg, Args.Get(0), Args.Get(1)),
						mVM_Type.tKind.Prefix => mIL_AST.TypePrefix(aPos, Reg, aType.Prefix!, Args.Get(0)),
						mVM_Type.tKind.Var => mIL_AST.TypeVar(aPos, Reg, Args.Get(0)),
						_ => throw mError.Error($"cannot emit type value: {aType}"),
					}
				);
				
				return Reg;
			}
		}
	}
	
	private static (tText Head, tText Body)
	MapSigPattern<tPos>(
		this ref tDefConstructor<tPos> aDef,
		ref tTypeDeclarations<tPos> aTypeDeclarations,
		mSPO_AST.tSigPatternNode<tPos> aPattern,
		tText aInput
	) {
		var Sig = aInput;
		var HeadValue = aPattern.HeadValue.AssertNotEmpty();
		
		var Head = aPattern.Head is mSPO_AST.tTypedPatternNode<tPos> Typed ? Typed.Pattern : aPattern.Head;
		
		if (Head is mSPO_AST.tTypePatternNode<tPos>) {
			var Expected = aDef.MapTypeValue(ref aTypeDeclarations, Head.Pos, HeadValue);
			aDef.Commands.Push(mIL_AST.TryHasHeadType(aPattern.Pos, aDef.CreateTempReg(out var MatchedSig), Sig, Expected));
			Sig = MatchedSig;
		}
		
		var HeadReg = aDef.CreateTempReg();
		aDef.Commands.Push(
			HeadValue.Kind is mVM_Type.tKind.Abstract
				? mIL_AST.GetSigHeadAs(aPattern.Pos, HeadReg, Sig, aTypeDeclarations.MapType(HeadValue))
				: mIL_AST.GetSigHead(aPattern.Pos, HeadReg, Sig),
			mIL_AST.GetSigBody(aPattern.Pos, aDef.CreateTempReg(out var BodyReg), Sig)
		);
		
		return (HeadReg, BodyReg);
	}
	
	public static mResult.tResult<tText, (tPos Pos, tText ErrorText)>
	TryMapExpression<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tExpressionNode<tPos> aExpressionNode
	) {
		var TypeDict = aDefConstructor.TypeDict;
		
		switch (aExpressionNode) {
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
				return mIL_AST.cEmptyValue;
			}
			case mSPO_AST.tFalseNode<tPos> FalseNode: {
				return mIL_AST.cFalse;
			}
			case mSPO_AST.tTrueNode<tPos> TrueNode: {
				return mIL_AST.cTrue;
			}
			case mSPO_AST.tTypeNode<tPos> Node when Node is not mSPO_AST.tIdNode<tPos>: {
				var Value = Node.AsVM_Value(mStd.cEmpty).AssertNotError(__ => __.ErrorText);
				return aDefConstructor.MapTypeValue(ref aModuleConstructor.TypeDeclarations, Node.Pos, Value);
			}
			case mSPO_AST.tIntNode<tPos> { Pos: var Pos, Value: var Value }: {
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(Pos, aDefConstructor.CreateTempReg(out var ResultReg), "" + Value)
				);
				
				aDefConstructor.AddLocal(ResultReg, mVM_Type.Int());
				
				return ResultReg;
			}
			case mSPO_AST.tIdNode<tPos> { Pos: var Pos, Id: var Id, TypeValue: var Value }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode);
				if (Value.IsSome(out var Known)) {
					return aDefConstructor.MapTypeValue(ref aModuleConstructor.TypeDeclarations, Pos, Known);
				}
				
				if (
					!aDefConstructor.TypeDict.ToStream().Any(__ => __.Key == Id) &&
					!aDefConstructor.Commands.ToStream(
					).Any(
						__ => __.GetResultReg().Match(
							aName => aName == Id,
							() => false
						)
					)
				) {
					if (!Type.IsSome(out var Type_)) {
						throw mError.Error($"type not set for '{Id}'");
					}
					
					aDefConstructor.AddEnv(Id, Type_);
				}
				
				return Id;
			}
			case mSPO_AST.tCallNode<tPos> { Pos: var Pos, Func: var Func, Arg: var Arg }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode);
				if (
					!aDefConstructor.TryMapExpression(aModuleConstructor, Func).Match(out var FuncReg, out var Error) ||
					!aDefConstructor.TryMapExpression(aModuleConstructor, Arg).Match(out var ArgReg, out Error)
				) {
					return mResult.Fail(Error);
				}
				
				aDefConstructor.Commands.Push(
					mIL_AST.CallFunc(Pos, aDefConstructor.CreateTempReg(out var ResultReg), FuncReg, ArgReg)
				);
				
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				
				return ResultReg;
			}
			case mSPO_AST.tTupleNode<tPos> { Items: var Items }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode);
				switch (Items.Take(2).ToArrayList().Size) {
					case 0: {
						throw mError.Error("impossible");
					}
					case 1: {
						mAssert.IsTrue(Items.Is(out var Head, out var _));
						return aDefConstructor.TryMapExpression(aModuleConstructor, Head);
					}
					default: {
						mAssert.IsTrue(Items.Is(out var Head, out var _));
						var TailReg = mIL_AST.cEmptyValue;
						foreach (var Item in Items) {
							if (!aDefConstructor.TryMapExpression(aModuleConstructor, Item).Match(out var HeadReg, out var Error)) {
								return mResult.Fail(Error);
							}
							
							aDefConstructor.Commands.Push(
								mIL_AST.CreatePair(
									aExpressionNode.Pos,
									aDefConstructor.CreateTempReg(out var TupleReg),
									TailReg,
									HeadReg
								)
							);
							TailReg = TupleReg;
						}
						aDefConstructor.AddLocal(TailReg, Type.AssertNotEmpty());
						return TailReg;
					}
				}
			}
			case mSPO_AST.tSigNode<tPos> Sig: {
				var Contract = aDefConstructor.MapTypeValue(ref aModuleConstructor.TypeDeclarations, Sig.Pos, aDefConstructor.TypeState.TryGetValidatedType(Sig).AssertNotEmpty());
				var Head = aDefConstructor.MapTypeValue(
					ref aModuleConstructor.TypeDeclarations,
					Sig.Head.Pos,
					Sig.Head.AsVM_Value(mStd.cEmpty).AssertNotError(__ => __.ErrorText)
				);
				
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, Sig.Body).Match(out var Body, out var Error)) {
					return mResult.Fail(Error);
				}
				
				aDefConstructor.Commands.Push(
					mIL_AST.CreatePair(Sig.Pos, aDefConstructor.CreateTempReg(out var Payload), Head, Body),
					mIL_AST.CreateSig(Sig.Pos, aDefConstructor.CreateTempReg(out var Result), Contract, Payload)
				);
				
				aDefConstructor.AddLocal(Result, aDefConstructor.TypeState.TryGetValidatedType(Sig).AssertNotEmpty());
				
				return Result;
			}
			case mSPO_AST.tPairNode<tPos> { Pos: var Pos, Tail: var Tail, Head: var Head }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode);
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, Tail).Match(out var TailReg, out var Error)) {
					return mResult.Fail(Error);
				}
				
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, Head).Match(out var HeadReg, out Error)) {
					return mResult.Fail(Error);
				}
				
				aDefConstructor.Commands.Push(mIL_AST.CreatePair(Pos, aDefConstructor.CreateTempReg(out var ResultReg), TailReg, HeadReg));
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				
				return ResultReg;
			}
			case mSPO_AST.tPrefixNode<tPos> { Pos: var Pos, Prefix: var Prefix, Element: var Element }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode);
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, Element).Match(out var ExpressionReg, out var Error)) {
					return mResult.Fail(Error);
				}
				
				aDefConstructor.Commands.Push(
					mIL_AST.AddPrefix(Pos, aDefConstructor.CreateTempReg(out var ResultReg), Prefix, ExpressionReg)
				);
				
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				
				return ResultReg;
			}
			case mSPO_AST.tRecordNode<tPos> { Elements: var Elements }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode);
				var ResultReg = mIL_AST.cEmptyValue;
				foreach (var (Key, Value) in Elements) {
					if (!aDefConstructor.TryMapExpression(aModuleConstructor, Value).Match(out var Expression, out var Error)) {
						return mResult.Fail(Error);
					}
					
					aDefConstructor.Commands.Push(
						[
							mIL_AST.AddPrefix(Key.Pos, aDefConstructor.CreateTempReg(out var PrefixReg), Key.Id, Expression),
							mIL_AST.AddField(Key.Pos, aDefConstructor.CreateTempReg(out var NewResultReg), ResultReg, PrefixReg),
						]
					);
					ResultReg = NewResultReg;
				}
				
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tCharNode<tPos> { Pos: var Pos, Value: var Value }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode);
				aDefConstructor.Commands.Push(
					[
						mIL_AST.CreateInt(Pos, aDefConstructor.CreateTempReg(out var OrdReg), ((tInt32)Value).ToString()),
						mIL_AST.AddPrefix(Pos, aDefConstructor.CreateTempReg(out var CharReg), "_Char...", OrdReg),
					]
				);
				
				aDefConstructor.AddLocal(
					CharReg,
					Type.Match(
						() => mVM_Type.Char(),
						__ => __
					)
				);
				
				return CharReg;
			}
			case mSPO_AST.tTextNode<tPos>: {
				throw mError.Error("text node should already be desugared");
			}
			case mSPO_AST.tLambdaNode<tPos> LambdaNode: {
				var LambdaDef = NewDefConstructor(aDefConstructor.TypeState);
				
				if (
					!LambdaDef.MapLambda(
						aModuleConstructor,
						LambdaNode
					).Match(out var Lambda, out var Error)
				) {
					return mResult.Fail(Error);
				}
				
				var LambdaEnvs = LambdaDef.EnvIds.ToStream(
				).Map(
					__ => mSPO_AST_Types.ScopeItem(
						__,
						LambdaDef.TypeDict.TryGet(__).AssertNotEmpty()
					)
				);
				
				foreach (var LambdaEnv in LambdaEnvs) {
					if (
						!aDefConstructor.LocalIds.ToStream().Any(__ => __ == LambdaEnv.Id) &&
						!aDefConstructor.ArgIds.ToStream().Any(__ => __ == LambdaEnv.Id)
					) {
						aDefConstructor.AddEnv(LambdaEnv.Id, LambdaEnv.Type);
					}
				}
				
				return aDefConstructor.InitProc(
					LambdaNode.Pos,
					Lambda.DefIndex,
					Lambda.DefType,
					LambdaEnvs
				);
			}
			case mSPO_AST.tMethodNode<tPos> MethodNode: {
				return aModuleConstructor.MapMethod(MethodNode, aDefConstructor.TypeState).Match(out var MethodDef, out var Error)
					? aDefConstructor.InitProc(
						MethodNode.Pos,
						MethodDef.Index,
						MethodDef.Type,
						MethodDef.EnvList
					)
					: mResult.Fail(Error);
			}
			case mSPO_AST.tBlockNode<tPos> { Commands: var Commands }: {
				foreach (var Command in Commands) {
					if (!aDefConstructor.MapCommand(aModuleConstructor, Command, out var Error)) {
						return mResult.Fail(Error);
					}
				}
				
				// TODO: remove created symbols from unknown symbols
				return mIL_AST.cEmptyValue;
			}
			case mSPO_AST.tIfNode<tPos> { Pos: var Pos, Cases: var Cases }: {
				var Ifs = mArrayList.List<mSPO_AST.tCommandNode<tPos>>();
				
				foreach (var (Test, Run) in Cases) {
					Ifs.Push(
						mSPO_AST.ReturnIf(
							aModuleConstructor.MergePos(Test.Pos, Run.Pos),
							Test,
							Run
						)
					);
				}
				
				Ifs.Push(
					mSPO_AST.ReturnIf(
						Pos,
						mSPO_AST.True(Pos),
						mSPO_AST.Empty(Pos)
					)
				); // TODO: ASSERT FALSE
				
				var ResultReg = aDefConstructor.CreateTempReg();
				
				var Head = mSPO_AST.Pattern(Pos, mSPO_AST.Empty(Pos), mStd.cEmpty);
				var Lambda = mSPO_AST.Lambda(Pos, mStd.cEmpty, Head, mSPO_AST.Block(Pos, Ifs.ToStream()));
				var TypeState = aDefConstructor.TypeState;
				var ResultTypes = Cases.Reduce(
					mStream.Stream<mVM_Type.tType>(),
					(aTypes, Case) => {
						var Type = TypeState.TryGetValidatedType(Case.Result).AssertNotEmpty();
						return aTypes.All(__ => !Equals(__, Type)) ? mStream.Stream(Type, aTypes) : aTypes;
					}
				);
				var EmptyType = mVM_Type.Empty();
				if (ResultTypes.All(__ => !Equals(__, EmptyType))) {
					ResultTypes = mStream.Stream(EmptyType, ResultTypes);
				}
				var ResultType = ResultTypes.Join((a1, a2) => mVM_Type.Set(a2, a1), EmptyType);
				var IfTypeState = TypeState
					.SetValidatedType(Head, mVM_Type.Empty())
					.SetValidatedType(Lambda, mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), ResultType));
				var IfDef = NewDefConstructor(IfTypeState);
				if (!IfDef.MapLambda(aModuleConstructor, Lambda).Match(out var Mapped, out var Error)) {
					return mResult.Fail(Error);
				}
				var EnvList = IfDef.EnvIds.ToStream().Map(
					__ => mSPO_AST_Types.ScopeItem(__, IfDef.TypeDict.TryGet(__).AssertNotEmpty())
				);
				var FuncReg = aDefConstructor.InitProc(Pos, Mapped.DefIndex, Mapped.DefType, EnvList);
				aDefConstructor.Commands.Push(
					mIL_AST.CallFunc(Pos, aDefConstructor.CreateTempReg(out var CallReg), FuncReg, mIL_AST.cEmptyValue),
					mIL_AST.Alias(Pos, ResultReg, CallReg)
				);
				aDefConstructor.AddLocal(CallReg, ResultType);
				aDefConstructor.AddArg(ResultReg, ResultType);
				return ResultReg;
			}
			case mSPO_AST.tIfMatchNode<tPos> {Pos: var Pos, Expression: var MatchExpression, Cases: var Cases }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode);
				if (
					!aDefConstructor.TryMapExpression(
						aModuleConstructor,
						MatchExpression
					).Match(
						out var InputReg,
						out var Error
					)
				) {
					return mResult.Fail(Error);
				}
				
				var SwitchDef = NewDefConstructor(aDefConstructor.TypeState);
				var Remaining = mMaybe.Some(aDefConstructor.TypeState.TryGetValidatedType(MatchExpression).AssertNotEmpty());
				
				foreach (var Case in Cases) {
					if (!Remaining.IsSome(out var RemainingType)) {
						break;
					}
					
					var Coverage = RemainingType.SplitForPatternType(Case.Pattern, aDefConstructor.TypeState);
					
					if (!Coverage.Matched.IsSome(out var MatchedType)) {
						return mResult.Fail(
							(
								Case.Pattern.Pos,
								$"pattern cannot match {RemainingType.ToText()}"
							)
						);
					}
					
					var CasePos = aModuleConstructor.MergePos(Case.Pattern.Pos, Case.Expression.Pos);
					var CaseFunc = NewDefConstructor(aDefConstructor.TypeState);
					var ErrorText_ = default(tText?);
					
					if (
						!aModuleConstructor.TryMapMatchedCase(
							ref CaseFunc,
							Case,
							CasePos,
							out Error
						) ||
						!CaseFunc.CreateDefType(
							aModuleConstructor,
							mVM_Type.Proc(
								mVM_Type.Empty(),
								MatchedType,
								mVM_Type.Prefix("Result", aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode).AssertNotEmpty())
							)
						).Match(out var CaseDefType, out ErrorText_)
					) {
						return mResult.Fail((CasePos, ErrorText_ ?? Error.ErrorText));
					}
					
					var CaseDefIndex = CaseFunc.FinishMapProc(
						Pos,
						aModuleConstructor,
						CaseDefType
					);
					
					var CaseTypeDict = CaseFunc.TypeDict;
					var ProcId = SwitchDef.InitProc(
						Pos,
						CaseDefIndex,
						CaseDefType,
						CaseFunc.EnvIds.ToStream(
						).Map(
							__ => mSPO_AST_Types.ScopeItem(
								__,
								CaseTypeDict.TryGet(__).AssertNotEmpty()
							)
						)
					);
					
					var GuardProcId = mMaybe.None<tText>();
					
					if (Case.Pattern.HasGuards()) {
						var GuardFunc = NewDefConstructor(aDefConstructor.TypeState);
						
						if (
							!GuardFunc.TryMapPatternGuard(
								aModuleConstructor,
								Case.Pattern,
								mIL_AST.cArg,
								out Error
							)
						) {
							return mResult.Fail(Error);
						}
						
						GuardFunc.Commands.Push(
							mIL_AST.ReturnIf(CasePos, mIL_AST.cTrue, mIL_AST.cTrue)
						);
						
						if (
							!GuardFunc.CreateDefType(
								aModuleConstructor,
								mVM_Type.Proc(mVM_Type.Empty(), MatchedType, mVM_Type.Bool())
							).Match(
								out var GuardDefType,
								out var GuardError
							)
						) {
							return mResult.Fail((CasePos, GuardError));
						}
						
						GuardProcId = SwitchDef.InitProc(
							CasePos,
							GuardFunc.FinishMapProc(CasePos, aModuleConstructor, GuardDefType),
							GuardDefType,
							GuardFunc.EnvIds.ToStream(
							).Map(
								__ => mSPO_AST_Types.ScopeItem(
									__,
									GuardFunc.TypeDict.TryGet(__).AssertNotEmpty()
								)
							)
						);
					}
					
					SwitchDef.Commands.Push(
						mIL_AST.TryReturn(
							CasePos,
							ProcId,
							mIL_AST.cArg,
							GuardProcId
						)
					);
					
					Remaining = Coverage.Remaining;
				}
				
				mAssert.IsTrue(
					Remaining.IsNone(),
					"match lowering must consume the complete input type"
				);
				
				SwitchDef.Commands.Push(
					mIL_AST.ReturnIf(Pos, mIL_AST.cTrue, mIL_AST.cEmptyValue)
				);
				// TODO NOW: put expression and else/remaining cases as args into the case test
				
				// §DEF MyResult = §IF MyMaybeIntValue MATCH {
				//   §DEF MyIntValue € §INT : MyIntValue .* 2
				//   () => 0
				// }
				//
				// §DEF MyResult = a € [§INT | []] => {
				//   §RETURN .(a_ € §INT => a_ .* 2) a IF_ARG_IS_INT
				//   §RETURN .(a_ € [] => 0) a If_ARG_IS_EMPTY
				// }. MyIntValue
				
				if (
					!SwitchDef.CreateDefType(
						aModuleConstructor,
						mVM_Type.Proc(
							mVM_Type.Empty(),
							aDefConstructor.TypeState.TryGetValidatedType(MatchExpression).AssertNotEmpty(),
							mVM_Type.Prefix("Result", aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode).AssertNotEmpty())
						)
					).Match(out var SwitchDefType, out var ErrorText)
				) {
					return mResult.Fail((Pos, ErrorText));
				}
				
				var SwitchDefId = GetDefId(aModuleConstructor.Defs.Size);
				var DefIndex = SwitchDef.FinishMapProc(aExpressionNode.Pos, aModuleConstructor, SwitchDefType);
				var SwitchProc = aDefConstructor.InitProc(
					aExpressionNode.Pos,
					DefIndex,
					SwitchDefType,
					SwitchDef.EnvIds.ToStream(
					).Map(
						__ => mSPO_AST_Types.ScopeItem(
							__,
							SwitchDef.TypeDict.TryGet(__).AssertNotEmpty()
						)
					)
				);
				
				aDefConstructor.Commands.Push(
					[
						mIL_AST.CallFunc(Pos, aDefConstructor.CreateTempReg(out var TempReg), SwitchProc, InputReg),
						mIL_AST.SubPrefix(Pos, aDefConstructor.CreateTempReg(out var ResultReg), "Result", TempReg),
					]
				);
				
				aDefConstructor.AddLocal(
					ResultReg,
					mVM_Type.Set( mVM_Type.Empty(), mVM_Type.Prefix("Result", Type.AssertNotEmpty()))
				);
				
				return ResultReg;
			}
			case mSPO_AST.tVarToValNode<tPos> { Pos: var Pos, Obj: var Obj }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aExpressionNode);
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, Obj).Match(out var ObjReg, out var Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.VarGet(Pos, aDefConstructor.CreateTempReg(out var ResultReg), ObjReg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tPipeToRightNode<tPos>: {
				throw mError.Error("Pipe should be desugared at this point!");
			}
			case mSPO_AST.tPipeToLeftNode<tPos>: {
				throw mError.Error("Pipe should be desugared at this point!");
			}
			default: {
				throw mError.Error(
					$"not implemented: case {nameof(mSPO_AST)}.{aExpressionNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(TryMapExpression)}(...)"
				);
			}
		}
	}
	
	internal static tBool
	TryMapMatchedCase<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		ref tDefConstructor<tPos> aCaseFunc,
		(mSPO_AST.tPatternNode<tPos> Match, mSPO_AST.tExpressionNode<tPos> Expression) aCase,
		tPos aCasePos,
		out (tPos Pos, tText ErrorText) aError
	) {
		if (
			!aCaseFunc.TryBindMatchedPattern(
				ref aModuleConstructor.TypeDeclarations,
				aCase.Match,
				mIL_AST.cArg,
				out aError
			) ||
			!aCaseFunc.TryMapExpression(
				aModuleConstructor,
				aCase.Expression
			).Match(
				out var Res,
				out aError
			)
		) {
			return false;
		}
		
		aCaseFunc.Commands.Push(
			[
				mIL_AST.AddPrefix(aCasePos, aCaseFunc.CreateTempReg(out var TemReg), "Result", Res),
				mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, TemReg),
			]
		);
		
		aError = default;
		return true;
	}
	
	internal static tBool
	TryBindMatchedPattern<tPos>(
		this ref tDefConstructor<tPos> aCaseFunc,
		ref tTypeDeclarations<tPos> aTypeDeclarations,
		mSPO_AST.tPatternNode<tPos> aMatch,
		tText aInputReg,
		out (tPos Pos, tText ErrorText) aError
	) {
		switch (aMatch) {
			case mSPO_AST.tTypePatternNode<tPos>: {
				break;
			}
			case mSPO_AST.tSigPatternNode<tPos> Sig: {
				var (Head, Body) = aCaseFunc.MapSigPattern(ref aTypeDeclarations, Sig, aInputReg);
				
				if (
					!aCaseFunc.TryBindMatchedPattern(ref aTypeDeclarations, Sig.Head, Head, out aError) ||
					!aCaseFunc.TryBindMatchedPattern(ref aTypeDeclarations, Sig.Body, Body, out aError)
				) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tCharNode<tPos>:
			case mSPO_AST.tTuplePatternNode<tPos>: {
				throw mError.Error($"{aMatch.GetType().Name} should already be desugared");
			}
			case mSPO_AST.tFreeIdPatternNode<tPos>:
			case mSPO_AST.tIdNode<tPos>:
			case mSPO_AST.tVarPatternNode<tPos>: {
				var Id = aMatch.TryGetId().AssertNotEmpty();
				
				aCaseFunc.Commands.Push(
					mIL_AST.Alias(aMatch.Pos, Id, aInputReg)
				);
				
				aCaseFunc.AddLocal(Id, aCaseFunc.TypeState.TryGetValidatedType(aMatch).AssertNotEmpty());
				
				break;
			}
			case mSPO_AST.tIgnorePatternNode<tPos>:
			case mSPO_AST.tEmptyNode<tPos>:
			case mSPO_AST.tTrueNode<tPos>:
			case mSPO_AST.tFalseNode<tPos>:
			case mSPO_AST.tIntNode<tPos>: {
				break;
			}
			case mSPO_AST.tPrefixPatternNode<tPos> Node: {
				aCaseFunc.Commands.Push(
					mIL_AST.SubPrefix(Node.Pos, aCaseFunc.CreateTempReg(out var InnerArg), Node.Prefix, aInputReg)
				);
				
				if (
					!aCaseFunc.TryBindMatchedPattern(
						ref aTypeDeclarations,
						Node.Pattern,
						InnerArg,
						out aError
					)
				) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tPairPatternNode<tPos> Node: {
				aCaseFunc.Commands.Push(
					[
						mIL_AST.GetFirst(Node.Pos, aCaseFunc.CreateTempReg(out var FirstReg), aInputReg),
						mIL_AST.GetSecond(Node.Pos, aCaseFunc.CreateTempReg(out var SecondReg), aInputReg),
					]
				);
				
				if (
					!aCaseFunc.TryBindMatchedPattern(
						ref aTypeDeclarations,
						Node.Tail,
						FirstReg,
						out aError
					) ||
					!aCaseFunc.TryBindMatchedPattern(
						ref aTypeDeclarations,
						Node.Head,
						SecondReg,
						out aError
					)
				) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tRecordPatternNode<tPos> Node: {
				foreach (var (IdNode, Pattern) in Node.Elements) {
					aCaseFunc.Commands.Push(
						mIL_AST.GetField(IdNode.Pos, aCaseFunc.CreateTempReg(out var FieldReg), aInputReg, IdNode.Id)
					);
					
					if (
						!aCaseFunc.TryBindMatchedPattern(
							ref aTypeDeclarations,
							Pattern,
							FieldReg,
							out aError
						)
					) {
						return false;
					}
				}
				
				break;
			}
			case mSPO_AST.tGuardPatternNode<tPos> Node: {
				if (
					!aCaseFunc.TryBindMatchedPattern(
						ref aTypeDeclarations,
						Node.Pattern,
						aInputReg,
						out aError
					)
				) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tTypedPatternNode<tPos> Node: {
				if (
					Node.TypeExpression.IsSome(out var TypeExpr) &&
					Node.Pattern is not mSPO_AST.tFreeIdPatternNode<tPos>
				) {
					aError = (
						TypeExpr.Pos,
						"TypeAnnotation in §IF...MATCH is currently only supported for '§DEF Id € Type'"
					);
					return false;
				}
				
				if (
					!aCaseFunc.TryBindMatchedPattern(
						ref aTypeDeclarations,
						Node.Pattern,
						aInputReg,
						out aError
					)
				) {
					return false;
				}
				
				break;
			}
			default: {
				throw new System.NotImplementedException(aMatch.GetType().Name); // TODO
			}
		}
		
		aError = default;
		return true;
	}
	
	static tBool
	HasGuards<tPos>(
		this mSPO_AST.tPatternNode<tPos> aPattern
	) => aPattern switch {
		mSPO_AST.tSigPatternNode<tPos> Sig =>
			Sig.Head is mSPO_AST.tTypePatternNode<tPos> || Sig.Body.HasGuards(),
		
		mSPO_AST.tTypePatternNode<tPos> => true,
		
		mSPO_AST.tIntNode<tPos> or
		mSPO_AST.tGuardPatternNode<tPos> => true,
		
		mSPO_AST.tFreeIdPatternNode<tPos> or
		mSPO_AST.tIdNode<tPos> or
		mSPO_AST.tVarPatternNode<tPos> or
		mSPO_AST.tIgnorePatternNode<tPos> or
		mSPO_AST.tEmptyNode<tPos> or
		mSPO_AST.tTrueNode<tPos> or
		mSPO_AST.tFalseNode<tPos> => false,
		
		mSPO_AST.tPairPatternNode<tPos> Pair =>
			Pair.Tail.HasGuards() || Pair.Head.HasGuards(),
		
		mSPO_AST.tPrefixPatternNode<tPos> Prefix =>
			Prefix.Pattern.HasGuards(),
		
		mSPO_AST.tRecordPatternNode<tPos> Record =>
			Record.Elements.Any(__ => __.Pattern.HasGuards()),
		
		mSPO_AST.tTypedPatternNode<tPos> Typed =>
			Typed.Pattern.HasGuards(),
		
		mSPO_AST.tCharNode<tPos> or
		mSPO_AST.tTuplePatternNode<tPos> =>
			throw mError.Error($"{aPattern.GetType().Name} should already be desugared"),
		
		_ => throw new System.NotImplementedException(aPattern.GetType().Name),
	};
	
	internal static tBool
	TryMapPatternGuard<tPos>(
		this ref tDefConstructor<tPos> aGuardFunc,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tPatternNode<tPos> aPattern,
		tText aInputReg,
		[MaybeNullWhen(false)] out (tPos Pos, tText ErrorText) aError
	) {
		switch (aPattern) {
			case mSPO_AST.tTypePatternNode<tPos>: {
				break;
			}
			case mSPO_AST.tSigPatternNode<tPos> Sig: {
				var (Head, Body) = aGuardFunc.MapSigPattern(ref aModuleConstructor.TypeDeclarations, Sig, aInputReg);
				
				if (
					!aGuardFunc.TryMapPatternGuard(aModuleConstructor, Sig.Head, Head, out aError) ||
					!aGuardFunc.TryMapPatternGuard(aModuleConstructor, Sig.Body, Body, out aError)
				) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tCharNode<tPos>:
			case mSPO_AST.tTuplePatternNode<tPos>: {
				mAssert.Fail($"{aPattern.GetType().Name} should already be desugared");
				break;
			}
			case mSPO_AST.tFreeIdPatternNode<tPos>:
			case mSPO_AST.tIdNode<tPos>:
			case mSPO_AST.tVarPatternNode<tPos>: {
				var Id = aPattern.TryGetId().AssertNotEmpty();
				
				aGuardFunc.Commands.Push(
					mIL_AST.Alias(aPattern.Pos, Id, aInputReg)
				);
				
				aGuardFunc.AddLocal(Id, aGuardFunc.TypeState.TryGetValidatedType(aPattern).AssertNotEmpty());
				
				break;
			}
			case mSPO_AST.tIgnorePatternNode<tPos>:
			case mSPO_AST.tEmptyNode<tPos>:
			case mSPO_AST.tTrueNode<tPos>:
			case mSPO_AST.tFalseNode<tPos>: {
				break;
			}
			case mSPO_AST.tIntNode<tPos> Node: {
				aGuardFunc.Commands.Push(
					[
						mIL_AST.CreateInt(Node.Pos, aGuardFunc.CreateTempReg(out var Int), "" + Node.Value),
						mIL_AST.IntsAreEq(Node.Pos, aGuardFunc.CreateTempReg(out var Eq), aInputReg, Int),
						mIL_AST.XOr(Node.Pos, aGuardFunc.CreateTempReg(out var NotEq), Eq, mIL_AST.cTrue),
						mIL_AST.ReturnIf(Node.Pos, NotEq, mIL_AST.cFalse),
					]
				);
				
				break;
			}
			case mSPO_AST.tPrefixPatternNode<tPos> Node: {
				aGuardFunc.Commands.Push(
					mIL_AST.SubPrefix(
						Node.Pos,
						aGuardFunc.CreateTempReg(out var InnerArg),
						Node.Prefix,
						aInputReg
					)
				);
				
				return aGuardFunc.TryMapPatternGuard(
					aModuleConstructor,
					Node.Pattern,
					InnerArg,
					out aError
				);
			}
			case mSPO_AST.tPairPatternNode<tPos> Node: {
				aGuardFunc.Commands.Push(
					[
						mIL_AST.GetFirst(Node.Pos, aGuardFunc.CreateTempReg(out var FirstReg), aInputReg),
						mIL_AST.GetSecond(Node.Pos, aGuardFunc.CreateTempReg(out var SecondReg), aInputReg),
					]
				);
				
				if (
					!aGuardFunc.TryMapPatternGuard(
						aModuleConstructor,
						Node.Tail,
						FirstReg,
						out aError
					) ||
					!aGuardFunc.TryMapPatternGuard(
						aModuleConstructor,
						Node.Head,
						SecondReg,
						out aError
					)
				) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tRecordPatternNode<tPos> Node: {
				foreach (var (IdNode, Pattern) in Node.Elements) {
					aGuardFunc.Commands.Push(
						mIL_AST.GetField(
							IdNode.Pos,
							aGuardFunc.CreateTempReg(out var FieldReg),
							aInputReg,
							IdNode.Id
						)
					);
					
					if (
						!aGuardFunc.TryMapPatternGuard(
							aModuleConstructor,
							Pattern,
							FieldReg,
							out aError
						)
					) {
						return false;
					}
				}
				
				break;
			}
			case mSPO_AST.tGuardPatternNode<tPos> Node: {
				if (
					!aGuardFunc.TryMapPatternGuard(
						aModuleConstructor,
						Node.Pattern,
						aInputReg,
						out aError
					)
				) {
					return false;
				}
				
				if (
					!aGuardFunc.TryMapExpression(
						aModuleConstructor,
						Node.Guard
					).Match(
						out var TestReg,
						out aError
					)
				) {
					return false;
				}
				
				aGuardFunc.Commands.Push(
					[
						mIL_AST.XOr(
							Node.Pos,
							aGuardFunc.CreateTempReg(out var TestInvertReg),
							TestReg,
							mIL_AST.cTrue
						),
						mIL_AST.ReturnIf(
							Node.Pos,
							TestInvertReg,
							mIL_AST.cFalse
						),
					]
				);
				break;
			}
			case mSPO_AST.tTypedPatternNode<tPos> Node: {
				return aGuardFunc.TryMapPatternGuard(
					aModuleConstructor,
					Node.Pattern,
					aInputReg,
					out aError
				);
			}
			default: {
				throw new System.NotImplementedException(aPattern.GetType().Name); // TODO
			}
		}
		
		aError = default;
		return true;
	}
	
	public static tBool
	MapPattern<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		ref tTypeDeclarations<tPos> aTypeDeclarations,
		mSPO_AST.tPatternNode<tPos> aPatternNode,
		tText aRegId,
		out (tPos, tText) aError
	) {
		switch (aPatternNode) {
			case mSPO_AST.tTypePatternNode<tPos>: {
				break;
			}
			case mSPO_AST.tSigPatternNode<tPos> Sig: {
				var (Head, Body) = aDefConstructor.MapSigPattern(ref aTypeDeclarations, Sig, aRegId);
				
				if (
					!aDefConstructor.MapPattern(ref aTypeDeclarations, Sig.Head, Head, out aError) ||
					!aDefConstructor.MapPattern(ref aTypeDeclarations, Sig.Body, Body, out aError)
				) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tIdNode<tPos> { Pos: var Pos, Id: var Name }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aPatternNode);
				mAssert.AreNotEquals(Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(aPatternNode.Pos, Name, aRegId));
				aDefConstructor.AddArg(Name, Type.AssertNotEmpty());
				break;
			}
			case mSPO_AST.tEmptyNode<tPos> { Pos: var Pos }: {
				aDefConstructor.Commands.Push(
					mIL_AST.ReturnIfNotEmpty(Pos, aRegId)
				);
				break;
			}
			case mSPO_AST.tIntNode<tPos> { Pos: var Pos, Value: var Value }: {
				aDefConstructor.Commands.Push(
					[
						mIL_AST.TryAsInt(Pos, aDefConstructor.CreateTempReg(out var IntReg), aRegId),
						mIL_AST.CreateInt(Pos, aDefConstructor.CreateTempReg(out var ExpectedIntReg), "" + Value),
						mIL_AST.IntsAreEq(Pos, aDefConstructor.CreateTempReg(out var EqReg), IntReg, ExpectedIntReg),
						mIL_AST.XOr(Pos, aDefConstructor.CreateTempReg(out var NotEqReg), EqReg, mIL_AST.cTrue),
						mIL_AST.ReturnIf(Pos, NotEqReg, mIL_AST.cEmptyValue)
					]
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(IntReg, mVM_Type.Int());
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ExpectedIntReg, mVM_Type.Int());
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(EqReg, mVM_Type.Bool());
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(NotEqReg, mVM_Type.Bool());
				break;
			}
			case mSPO_AST.tFreeIdPatternNode<tPos> { Pos: var Pos, Id: var Name }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aPatternNode);
				mAssert.AreNotEquals(Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(aPatternNode.Pos, Name, aRegId));
				aDefConstructor.AddArg(Name, Type.AssertNotEmpty());
				break;
			}
			case mSPO_AST.tVarPatternNode<tPos> { Pos: var Pos, Id: var Name }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aPatternNode);
				mAssert.AreNotEquals(Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(aPatternNode.Pos, Name, aRegId));
				aDefConstructor.AddArg(Name, Type.AssertNotEmpty());
				break;
			}
			case mSPO_AST.tIgnorePatternNode<tPos> IgnorePatternNode: {
				break;
			}
			case mSPO_AST.tPrefixPatternNode<tPos> { Pos: var Pos, Prefix: var Prefix, Pattern: var Pattern }: {
				var Type = aDefConstructor.TypeState.TryGetValidatedType(aPatternNode);
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(Pos, aDefConstructor.CreateTempReg(out var ResultReg), Prefix, aRegId)
				);
				
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				
				return aDefConstructor.MapPattern(ref aTypeDeclarations, Pattern, ResultReg, out aError);
			}
			case mSPO_AST.tRecordPatternNode<tPos> { Elements: var Elements }: {
				foreach (var (IdNode, Pattern) in Elements) {
					aDefConstructor.Commands.Push(
						mIL_AST.GetField(IdNode.Pos, aDefConstructor.CreateTempReg(out var FieldReg), aRegId, IdNode.Id)
					);
					
					if (!aDefConstructor.MapPattern(ref aTypeDeclarations, Pattern, FieldReg, out aError)) {
						return false;
					}
				}
				
				break;
			}
			case mSPO_AST.tTuplePatternNode<tPos> { Pos: var Pos, Items: var Items }: {
				var RemainingReg = aRegId;
				mAssert.AreEquals(Items.Take(2).ToArrayList().Size, 2u);
				
				foreach (var Item in Items.Reverse()) {
					aDefConstructor.Commands.Push(
						mIL_AST.GetSecond(Pos, aDefConstructor.CreateTempReg(out var ItemReg), RemainingReg)
					);
					
					aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ItemReg, aDefConstructor.TypeState.TryGetValidatedType(Item).AssertNotEmpty());
					
					if (!aDefConstructor.MapPattern(ref aTypeDeclarations, Item, ItemReg, out aError)) {
						return false;
					}
					
					aDefConstructor.Commands.Push(
						mIL_AST.GetFirst(Pos, aDefConstructor.CreateTempReg(out var NewRestReg), RemainingReg)
					);
					
					RemainingReg = NewRestReg;
				}
				
				break;
			}
			case mSPO_AST.tPairPatternNode<tPos> { Pos: var Pos, Tail: var Tail, Head: var Head }: {
				aDefConstructor.Commands.Push(
					mIL_AST.GetSecond(Pos, aDefConstructor.CreateTempReg(out var HeadReg), aRegId)
				);
				
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(HeadReg, aDefConstructor.TypeState.TryGetValidatedType(Head).AssertNotEmpty());
				
				if (!aDefConstructor.MapPattern(ref aTypeDeclarations, Head, HeadReg, out aError)) {
					return false;
				}
				
				aDefConstructor.Commands.Push(
					mIL_AST.GetFirst(Pos, aDefConstructor.CreateTempReg(out var TailReg), aRegId)
				);
				
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(TailReg, aDefConstructor.TypeState.TryGetValidatedType(Tail).AssertNotEmpty());
				
				if (!aDefConstructor.MapPattern(ref aTypeDeclarations, Tail, TailReg, out aError)) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tGuardPatternNode<tPos> { Pattern: var Pattern, Guard: var Guard }: {
				// TODO: ASSERT Guard
				return aDefConstructor.MapPattern(ref aTypeDeclarations, Pattern, aRegId, out aError);
			}
			case mSPO_AST.tTypedPatternNode<tPos> { Pattern: var Pattern }: {
				return aDefConstructor.MapPattern(ref aTypeDeclarations, Pattern, aRegId, out aError);
			}
			default: {
				throw mError.Error(
					$"not implemented: {nameof(mSPO_AST)}.{aPatternNode.GetType().Name} in {nameof(mSPO2IL)}.{nameof(MapPattern)}(...)"
				);
			}
		}
		
		aError = default;
		return true;
	}
	
	public static tBool
	MapDef<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tDefNode<tPos> aDefNode,
		out (tPos Pos, tText ErrorText) aError
	) => (
		aDefConstructor.TryMapExpression(aModuleConstructor, aDefNode.Src).Match(out var ValueReg, out aError) &&
		aDefConstructor.MapPattern(ref aModuleConstructor.TypeDeclarations, aDefNode.Des, ValueReg, out aError)
	);
	
	public static tBool
	MapReturnIf<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tReturnIfNode<tPos> aReturnNode,
		out (tPos, tText) aError
	) {
		if (
			aDefConstructor.TryMapExpression(aModuleConstructor, aReturnNode.Condition).Match(out var CondReg, out aError) &&
			aDefConstructor.TryMapExpression(aModuleConstructor, aReturnNode.Result).Match(out var ResReg, out aError)
		) {
			aDefConstructor.Commands.Push(mIL_AST.ReturnIf(aReturnNode.Pos, CondReg, ResReg));
			return true;
		} else {
			return false;
		}
	}
	
	public static tBool
	MapRecursiveLambdas<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tRecLambdasNode<tPos> aRecLambdasNode,
		out (tPos, tText) aError
	) {
		var TypeState = aDefConstructor.TypeState;
		var IsSingle = aRecLambdasNode.List.Count() is 1;
		var RecFactoryFunc = NewDefConstructor(aDefConstructor.TypeState);
		
		if (IsSingle) {
			var RecProc = aRecLambdasNode.List.TryFirst().AssertNotEmpty();
			RecFactoryFunc.AddArg(
				RecProc.Id.Id,
				TypeState.TryGetValidatedType(RecProc.Lambda).AssertNotEmpty()
			);
			RecFactoryFunc.Commands.Push(
				mIL_AST.Alias(RecProc.Pos, RecProc.Id.Id, mIL_AST.cArg)
			);
		} else {
			var Arg = mIL_AST.cArg;
			foreach (var RecProc in aRecLambdasNode.List) {
				RecFactoryFunc.AddArg(
					RecProc.Id.Id,
					TypeState.TryGetValidatedType(RecProc.Lambda).AssertNotEmpty()
				);
				RecFactoryFunc.Commands.Push(
					mIL_AST.GetSecond(RecProc.Pos, RecProc.Id.Id, Arg),
					mIL_AST.GetFirst(RecProc.Pos, RecFactoryFunc.CreateTempReg(out var TempReg), Arg)
				);
				Arg = TempReg;
			}
		}
		
		var ResultTupleReg = mIL_AST.cEmptyValue;
		
		foreach (var RecProc in aRecLambdasNode.List) {
			var RecProcConstructor = NewDefConstructor(aDefConstructor.TypeState);
			if (
				!RecProcConstructor.MapLambda(
					aModuleConstructor,
					RecProc.Lambda
				).Match(out var Def, out aError)
			) {
				return false;
			}
			
			var RecProcReg = RecFactoryFunc.InitProc(
				RecProc.Pos,
				Def.DefIndex,
				Def.DefType,
				RecProcConstructor.EnvIds.ToStream(
				).Map(
					__ => mSPO_AST_Types.ScopeItem(
						__,
						RecProcConstructor.TypeDict.TryGet(__).AssertNotEmpty()
					)
				)
			);
			
			if (IsSingle) {
				ResultTupleReg = RecProcReg;
			} else {
				RecFactoryFunc.Commands.Push(
					mIL_AST.CreatePair(
						RecProc.Pos,
						RecFactoryFunc.CreateTempReg(out var TempReg),
						ResultTupleReg,
						RecProcReg
					)
				);
				ResultTupleReg = TempReg;
			}
		}
		
		RecFactoryFunc.Commands.Push(
			mIL_AST.ReturnIf(
				aRecLambdasNode.Pos,
				mIL_AST.cTrue,
				ResultTupleReg
			)
		);
		
		var RecProcsType = (
			IsSingle
			? aDefConstructor.TypeState.TryGetValidatedType(aRecLambdasNode.List.TryFirst().AssertNotEmpty().Lambda).AssertNotEmpty()
			: aRecLambdasNode.List.Reduce(
				mVM_Type.Empty(),
				(Acc, RecProc) => mVM_Type.Pair(
					Acc,
					TypeState.TryGetValidatedType(RecProc.Lambda).AssertNotEmpty()
				)
			)
		);
		
		if (
			!RecFactoryFunc.CreateDefType(
				aModuleConstructor,
				mVM_Type.Proc(
					mVM_Type.Empty(),
					RecProcsType,
					RecProcsType
				)
			).Match(out var RecFactoryDefType, out var Error)
		) {
			aError = (aRecLambdasNode.Pos, Error);
			return false;
		}
		
		var RecFactoryDefIndex = RecFactoryFunc.FinishMapProc(
			aRecLambdasNode.Pos,
			aModuleConstructor,
			RecFactoryDefType
		);
		
		var RecProcsTupleReg = aDefConstructor.InitProc(
			aRecLambdasNode.Pos,
			RecFactoryDefIndex,
			RecFactoryDefType,
			RecFactoryFunc.EnvIds.ToStream(
			).Map(
				__ => mSPO_AST_Types.ScopeItem(
					__,
					RecFactoryFunc.TypeDict.TryGet(__).AssertNotEmpty()
				)
			),
			true
		);
		
		if (IsSingle) {
			var RecProc = aRecLambdasNode.List.TryFirst().AssertNotEmpty();
			aDefConstructor.Commands.Push(
				mIL_AST.Alias(
					RecProc.Pos,
					RecProc.Id.Id,
					RecProcsTupleReg
				)
			);
			aDefConstructor.AddLocal(
				RecProc.Id.Id,
				TypeState.TryGetValidatedType(RecProc.Lambda).AssertNotEmpty()
			);
		} else {
			foreach (var RecProc in aRecLambdasNode.List) {
				aDefConstructor.Commands.Push(
					mIL_AST.GetSecond(
						RecProc.Pos,
						RecProc.Id.Id,
						RecProcsTupleReg
					),
					mIL_AST.GetFirst(
						RecProc.Pos,
						aDefConstructor.CreateTempReg(out var TempReg),
						RecProcsTupleReg
					)
				);
				RecProcsTupleReg = TempReg;
				aDefConstructor.AddLocal(
					RecProc.Id.Id,
					TypeState.TryGetValidatedType(RecProc.Lambda).AssertNotEmpty()
				);
			}
		}
		
		aError = default;
		return true;
	}
	
	public static tBool
	MapDefVar<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tDefVarNode<tPos> aDefVarNode,
		out (tPos, tText) aError
	) {
		if (!aDefConstructor.TryMapExpression(aModuleConstructor, aDefVarNode.Expression).Match(out var Reg, out aError)) {
			return false;
		}
		
		aDefConstructor.Commands.Push(
			mIL_AST.VarDef(
				aDefVarNode.Pos,
				aDefVarNode.Id.Id,
				Reg
			)
		);
		
		aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(
			aDefVarNode.Id.Id,
			mVM_Type.Var(aDefConstructor.TypeState.TryGetValidatedType(aDefVarNode.Expression).AssertNotEmpty())
		);
		
		return aDefConstructor.MapMethodCalls(
			aModuleConstructor,
			mSPO_AST.MethodCallStatement(
				aDefVarNode.Pos,
				aDefVarNode.Id,
				aDefVarNode.MethodCalls
			),
			out aError
		);
	}
	
	public static tBool
	MapMethodCalls<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodCallsNode<tPos> aMethodCallsNode,
		out (tPos, tText) aError
	) {
		var TypeState = aDefConstructor.TypeState;
		// TODO: set proper Def type ?
		
		if (!aDefConstructor.TryMapExpression(aModuleConstructor, aMethodCallsNode.Object).Match(out var Object, out aError)) {
			return false;
		}
		foreach (var Call in aMethodCallsNode.MethodCalls) {
			if (!aDefConstructor.TryMapExpression(aModuleConstructor, Call.Argument).Match(out var Arg, out aError)) {
				return false;
			}
			
			if (aDefConstructor.TypeState.TryGetValidatedType(Call.Argument).IsSome(out var ArgType) && ArgType.IsVar(out var ArgInnerType)) {
				aDefConstructor.Commands.Push(
					mIL_AST.VarGet(Call.Argument.Pos, aDefConstructor.CreateTempReg(out var ArgValue), Arg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ArgValue, ArgInnerType);
				Arg = ArgValue;
			}
			
			var MethodId = Call.Method.Id;
			if (MethodId is "_=...") {
				aDefConstructor.Commands.Push(mIL_AST.VarSet(aMethodCallsNode.Pos, Object, Arg));
				continue;
			}
			
			var Result = Call.Result.IsNone() ? mIL_AST.cEmptyValue : aDefConstructor.CreateTempReg();
			var ResultType = Call.Result.Then(__ => TypeState.TryGetValidatedType(__).AssertNotEmpty()).ElseUse(mVM_Type.Empty());
			
			aDefConstructor.Commands.Push(
				[
					mIL_AST.CreatePair(aMethodCallsNode.Object.Pos, aDefConstructor.CreateTempReg(out var MethodReg), Object, MethodId),
					mIL_AST.CallProc(aMethodCallsNode.Pos, Result, MethodReg, Arg)
				]
			);
			
			if (Call.Result.IsSome(out var Result_)) {
				if (!aDefConstructor.MapPattern(ref aModuleConstructor.TypeDeclarations, Result_, Result, out aError)) {
					return false;
				}
			}
		}
		
		aError = default;
		return true;
	}
	
	public static tBool
	MapCommand<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tCommandNode<tPos> aCommandNode,
		out (tPos, tText) aError
	) => aCommandNode switch {
		mSPO_AST.tDefNode<tPos> Node
			=> aDefConstructor.MapDef(aModuleConstructor, Node, out aError),
			
		mSPO_AST.tRecLambdasNode<tPos> Node
			=> aDefConstructor.MapRecursiveLambdas(aModuleConstructor, Node, out aError),
			
		mSPO_AST.tReturnIfNode<tPos> Node
			=> aDefConstructor.MapReturnIf(aModuleConstructor, Node, out aError),
			
		mSPO_AST.tDefVarNode<tPos> Node
			=> aDefConstructor.MapDefVar(aModuleConstructor, Node, out aError),
			
		mSPO_AST.tMethodCallsNode<tPos> Node
			=> aDefConstructor.MapMethodCalls(aModuleConstructor, Node, out aError),
			
		_ => throw mError.Error("Impossible")
	};
	
	public static mResult.tResult<tModuleConstructor<tPos>, (tPos Pos, tText ErrorText)>
	MapModule<tPos>(
		mSPO_AST.tModuleNode<tPos> aModuleNode,
		mStd.tFunc<tPos, tPos, tPos> aMergePos,
		mStream.tStream<mSPO_AST_Types.tScopeItem> aScope,
		mSPO_AST_Types.tTypeState<tPos> aTypeState
	) {
		using var __Perf = mPerf.Measure();
		
		var Lambda = mSPO_AST.Lambda(
			aModuleNode.Pos,
			mStd.cEmpty,
			aModuleNode.Import.Pattern,
			mSPO_AST.Block(
				aMergePos(
					aModuleNode.Commands.TryFirst().Then(__ => __.Pos).ElseUse(default),
					aModuleNode.Export.Pos
				),
				mStream.Concat(
					aModuleNode.Commands,
					mStream.Stream<mSPO_AST.tCommandNode<tPos>>(
						[
							mSPO_AST.ReturnIf(
								aModuleNode.Export.Pos,
								mSPO_AST.True(aModuleNode.Export.Pos),
								aModuleNode.Export.Expression
							)
						]
					)
				)
			)
		);
		
		if (!Lambda.UpdateTypes(aScope, aTypeState).Match(out var Checked, out var Error)) {
			return mResult.Fail(Error);
		}
		
		var ModuleConstructor = NewModuleConstructor(aMergePos);
		var TempLambdaDef = NewDefConstructor(Checked.State);
		
		if (!TempLambdaDef.MapLambda(ModuleConstructor, Lambda).Match(out var _, out var Error_)) {
			return mResult.Fail(Error_);
		}
		
		var FirstNonDef = TempLambdaDef.EnvIds.ToStream(
		).Where(
			__ => !__.StartsWith("d_")
		).TryFirst(
		);
		
		if (FirstNonDef.IsSome(out var FirstNonDefId)) {
			throw mError.Error($"expected definition symbol but was '{FirstNonDefId}'");
		}
		
		if (TempLambdaDef.EnvIds.Size != ModuleConstructor.Defs.Size - 1) {
			throw mError.Error(
				$"expected {ModuleConstructor.Defs.Size - 1} definitions but was {TempLambdaDef.EnvIds.Size}"
			);
		}
		
		// TODO: set proper Def type ?
		//var DefSymbols = mArrayList.List<(tText Id, tPos Pos)>();
		//foreach (var (I, Def) in ModuleConstructor.Defs.ToLazyList().MapWithIndex().Skip(1)) {
		//	DefSymbols.Push(
		//		(
		//			GetDefId(I),
		//			aMergePos(Def.Commands.Get(0).Pos, Def.Commands.Get(Def.Commands.Size() - 1).Pos)
		//		)
		//	);
		//}
		return ModuleConstructor;
	}
}
