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
	public sealed class
	tModuleConstructor<tPos> {
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> TypeDef;
		public mTreeMap.tTree<tText, mVM_Type.tType> Types; // TypeText to TypeDef
		public mArrayList.tArrayList<(tText? TypeId, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands)> Defs;
		internal mStd.tFunc<tPos, tPos, tPos> MergePos;
	}
	
	public struct
	tDefConstructor<tPos> {
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
		TypeDef = mArrayList.List<mIL_AST.tCommandNode<tPos>>(),
		Types = mTreeMap.Tree<tText, mVM_Type.tType>((a1, a2) => mMath.Sign(tText.CompareOrdinal(a1, a2)), []),
		MergePos = aMergePos,
	};
	
	public static tDefConstructor<tPos>
	NewDefConstructor<tPos>(
	) => new () {
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
	EnsureTypeDefinition<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		tText aTypeId,
		mVM_Type.tType aType,
		mStd.tFunc<mIL_AST.tCommandNode<tPos>> aCreateDefinition
	) {
		if (aModuleConstructor.Types.TryGet(aTypeId).IsSome(out var ExistingType)) {
			mAssert.AreEquals(ExistingType.ToText(), aType.ToText());
			return;
		}
		aModuleConstructor.TypeDef.Push(aCreateDefinition());
		aModuleConstructor.Types = aModuleConstructor.Types.Set(aTypeId, aType);
	}
	
	public static tText
	MapType<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		mVM_Type.tType aType
	) {
		switch (aType) {
			case var a when a.IsType(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cTypeType, a);
				return mIL_GenerateOpcodes.cTypeType;
			}
			case var a when a.IsEmpty(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cEmptyType, a);
				return mIL_GenerateOpcodes.cEmptyType;
			}
			case var a when a.Kind is mVM_Type.tKind.True: {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_AST.cTrue, a);
				return mIL_AST.cTrue;
			}
			case var a when a.Kind is mVM_Type.tKind.False: {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_AST.cFalse, a);
				return mIL_AST.cFalse;
			}
			case var a when a.IsInt(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cIntType, a);
				return mIL_GenerateOpcodes.cIntType;
			}
			case var a when a.IsAny(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cAnyType, a);
				return mIL_GenerateOpcodes.cAnyType;
			}
			case var a when a.IsTypeVariable(out var Id_, out var Kind): {
				if (aModuleConstructor.Types.TryGet(Id_).IsSome(out var Existing)) {
					mAssert.AreEquals(Existing.KindOf(), Kind);
					return Id_;
				}
				if (Kind.IsType()) {
					aModuleConstructor.EnsureTypeDefinition(Id_, a, () => mIL_AST.TypeFree(default(tPos)!, Id_));
				} else {
					var KindId = aModuleConstructor.MapType(Kind);
					aModuleConstructor.EnsureTypeDefinition(Id_, a, () => mIL_AST.TypeFree(default(tPos)!, Id_, KindId));
				}
				return Id_;
			}
			case var a when a.IsPrefix(out var Prefix, out var Type): {
				var Id = aModuleConstructor.MapType(Type);
				var NewId = $"[#{Prefix}:{Id}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypePrefix(default(tPos)!, NewId, Prefix, Id));
				return NewId;
			}
			case var a when a.IsPair(out var Type1, out var Type2): {
				var Id1 = aModuleConstructor.MapType(Type1);
				var Id2 = aModuleConstructor.MapType(Type2);
				var NewId = $"[{Id1};{Id2}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypePair(default(tPos)!, NewId, Id1, Id2));
				return NewId;
			}
			case var a when a.IsSig(out var Binder, out var BinderKind, out var Body): {
				var BinderId = aModuleConstructor.MapType(Binder);
				var BodyId = aModuleConstructor.MapType(Body);
				var NewId = $"[§SIG_WITH {BinderId} IN {BodyId}]";
				aModuleConstructor.EnsureTypeDefinition(
					NewId,
					a,
					() => mIL_AST.TypeSig(default(tPos)!, NewId, BinderId, BodyId)
				);
				return NewId;
			}
			case var a when a.IsRecord(out var Fields): {
				var RecTypeId = mIL_AST.cEmptyType;
				foreach (var Field in Fields.ToStream()) {
					var FieldTypeId = aModuleConstructor.MapType(Field.Value);
					var PrefixedFieldTypeId = $"[#{Field.Key} {FieldTypeId}]";
					aModuleConstructor.TypeDef.Push(mIL_AST.TypePrefix(default(tPos)!, PrefixedFieldTypeId, Field.Key.ToString(), FieldTypeId)); // TODO: remove .ToString() ???
					var NewRecTypeId = $"[{RecTypeId}, {PrefixedFieldTypeId}]";
					aModuleConstructor.TypeDef.Push(mIL_AST.TypeRecord(default(tPos)!, NewRecTypeId, RecTypeId, PrefixedFieldTypeId));
					RecTypeId = NewRecTypeId;
				}
				aModuleConstructor.Types = aModuleConstructor.Types.Set(RecTypeId, a);
				return RecTypeId;
			}
			case var a when a.IsSet(out var Type1, out var Type2): {
				var Id1 = aModuleConstructor.MapType(Type1);
				var Id2 = aModuleConstructor.MapType(Type2);
				var NewId = $"[{Id1}|{Id2}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeSet(default(tPos)!, NewId, Id1, Id2));
				return NewId;
			}
			case var a when a.IsProc(out var EnvType, out var ArgType, out var ResType): {
				var IdArg = aModuleConstructor.MapType(ArgType);
				var IdRes = aModuleConstructor.MapType(ResType);
				var IdFunc = $"[{IdArg}->{IdRes}]";
				var FuncType = mVM_Type.Proc(mVM_Type.Empty(), ArgType, ResType);
				aModuleConstructor.EnsureTypeDefinition(IdFunc, FuncType, () => mIL_AST.TypeFunc(default(tPos)!, IdFunc, IdArg, IdRes));
				
				if (EnvType.IsEmpty()) {
					return IdFunc;
				}
				
				var IdEnv = aModuleConstructor.MapType(EnvType);
				var IdEnvFunc = $"[{IdEnv}:{IdFunc}]";
				aModuleConstructor.EnsureTypeDefinition(IdEnvFunc, a, () => mIL_AST.TypeMethod(default(tPos)!, IdEnvFunc, IdEnv, IdFunc));
				return IdEnvFunc;
			}
			case var a when a.IsVar(out var InnerType): {
				var InnerId = aModuleConstructor.MapType(InnerType);
				var NewId = $"[§VAR {InnerId}]";
				
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeVar(default(tPos)!, NewId, InnerId));
				
				return NewId;
			}
			case var a when a.IsRecursive(out var HeadType, out var BodyType): {
				var HeadId = aModuleConstructor.MapType(HeadType);
				var BodyId = aModuleConstructor.MapType(BodyType);
				var NewId = $"[§REC {HeadId} => {BodyId}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeRecursive(default(tPos)!, NewId, HeadId, BodyId));
				return NewId;
			}
			case var a when a.IsGeneric(out var HeadType, out var BodyType): {
				var HeadId = aModuleConstructor.MapType(HeadType);
				var BodyId = aModuleConstructor.MapType(BodyType);
				var NewId = $"[$ALL {HeadId} => {BodyId}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeGeneric(default(tPos)!, NewId, HeadId, BodyId));
				return NewId;
			}
			case var a when a.IsTypeApply(out var Constructor, out var Argument): {
				var ConstructorId = aModuleConstructor.MapType(Constructor);
				var ArgumentId = aModuleConstructor.MapType(Argument);
				var NewId = $"[.{ConstructorId} {ArgumentId}]";
				aModuleConstructor.EnsureTypeDefinition(
					NewId,
					a,
					() => mIL_AST.TypeGenericApply(default(tPos)!, NewId, ConstructorId, ArgumentId)
				);
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
				return aModuleConstructor.Types.TryGet(TypeName).ElseFail(
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
			!aDefConstructor.MapPattern(aModuleConstructor, aLambdaNode.Head, mIL_AST.cArg, out var Error) ||
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
			aLambdaNode.TypeAnnotation.AssertNotEmpty()
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
			!aDefConstructor.MapPattern(aModuleConstructor, aMethodNode.Arg, mIL_AST.cArg, out var Error) ||
			!aDefConstructor.MapPattern(aModuleConstructor, aMethodNode.Obj, mIL_AST.cObj, out Error) ||
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
			aMethodNode.TypeAnnotation.AssertNotEmpty()
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
				aModuleConstructor.MapType(aDefType),
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
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		var TempMethodDef = NewDefConstructor<tPos>();
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
			case mSPO_AST.tEmptyTypeNode<tPos> EmptyTypeNode: {
				return mIL_AST.cEmptyType;
			}
			case mSPO_AST.tIntTypeNode<tPos> IntTypeNode: {
				return mIL_AST.cIntType;
			}
			case mSPO_AST.tCharTypeNode<tPos> CharTypeNode: {
				return aModuleConstructor.MapType(mVM_Type.Char());
			}
			case mSPO_AST.tTextTypeNode<tPos> TextTypeNode: {
				return aModuleConstructor.MapType(mVM_Type.Text());
			}
			case mSPO_AST.tTypeTypeNode<tPos> TypeTypeNode: {
				return mIL_AST.cTypeType;
			}
			case mSPO_AST.tIntNode<tPos> { Pos: var Pos, Value: var Value }: {
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(Pos, aDefConstructor.CreateTempReg(out var ResultReg), "" + Value)
				);
				aDefConstructor.AddLocal(ResultReg, mVM_Type.Int());
				return ResultReg;
			}
			case mSPO_AST.tIdNode<tPos> { Pos: var Pos, Id: var Id, TypeAnnotation: var Type }: {
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
			case mSPO_AST.tCallNode<tPos> { Pos: var Pos, Func: var Func, Arg: var Arg, TypeAnnotation: var Type }: {
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
			case mSPO_AST.tTupleNode<tPos> { Items: var Items, TypeAnnotation: var Type }: {
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
			case mSPO_AST.tPairNode<tPos> { Pos: var Pos, Tail: var Tail, Head: var Head, TypeAnnotation: var Type }: {
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
			case mSPO_AST.tPrefixNode<tPos> { Pos: var Pos, Prefix: var Prefix, Element: var Element, TypeAnnotation: var Type }: {
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, Element).Match(out var ExpressionReg, out var Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.AddPrefix(Pos, aDefConstructor.CreateTempReg(out var ResultReg), Prefix, ExpressionReg)
				);
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tRecordNode<tPos> { Elements: var Elements, TypeAnnotation: var Type }: {
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
			case mSPO_AST.tCharNode<tPos> { Pos: var Pos, Value: var Value, TypeAnnotation: var Type }: {
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
			case mSPO_AST.tTextNode<tPos>:
				throw mError.Error("text node should already be desugared");
			case mSPO_AST.tLambdaNode<tPos> LambdaNode: {
				var LambdaDef = NewDefConstructor<tPos>();
				
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
				return aModuleConstructor.MapMethod(MethodNode).Match(out var MethodDef, out var Error)
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
				var ResultType = aExpressionNode.TypeAnnotation.AssertNotEmpty();
				var ArgumentType = mVM_Type.Empty();
				var Lambda = mSPO_AST.Lambda(
					Pos,
					mStd.cEmpty,
					mSPO_AST.Pattern(
						Pos,
						mSPO_AST.WithTypeAnnotation(mSPO_AST.Empty(Pos), ArgumentType),
						mStd.cEmpty,
						ArgumentType
					),
					mSPO_AST.Block(Pos, Ifs.ToStream(), ResultType),
					mVM_Type.Proc(mVM_Type.Empty(), ArgumentType, ResultType)
				);
				var Def = mSPO_AST.Def(
					Pos,
					mSPO_AST.Pattern(
						Pos,
						new mSPO_AST.tFreeIdPatternNode<tPos> {
							Id = ResultReg,
							Pos = Pos,
							NamePos = Pos,
							TypeAnnotation = ResultType,
						},
						mStd.cEmpty,
						ResultType
					),
					mSPO_AST.Call(Pos, Lambda, mSPO_AST.Empty(Pos), ResultType)
				);
				
				return (
					aDefConstructor.MapCommand(aModuleConstructor, Def, out var Error)
					? ResultReg
					: mResult.Fail(Error)
				);
			}
			case mSPO_AST.tIfMatchNode<tPos> {Pos: var Pos, Expression: var MatchExpression, Cases: var Cases, TypeAnnotation: var Type }: {
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
				
				var SwitchDef = NewDefConstructor<tPos>();
				var Remaining = mMaybe.Some(MatchExpression.TypeAnnotation.AssertNotEmpty());
				
				foreach (var Case in Cases) {
					if (!Remaining.IsSome(out var RemainingType)) {
						break;
					}
					
					var Coverage = RemainingType.SplitForPatternType(Case.Pattern);
					
					if (!Coverage.Matched.IsSome(out var MatchedType)) {
						return mResult.Fail(
							(
								Case.Pattern.Pos,
								$"pattern cannot match {RemainingType.ToText()}"
							)
						);
					}
					
					var CasePos = aModuleConstructor.MergePos(Case.Pattern.Pos, Case.Expression.Pos);
					var CaseFunc = NewDefConstructor<tPos>();
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
								mVM_Type.Prefix("Result", aExpressionNode.TypeAnnotation.AssertNotEmpty())
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
						var GuardFunc = NewDefConstructor<tPos>();
						
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
							MatchExpression.TypeAnnotation.AssertNotEmpty(),
							mVM_Type.Prefix("Result", aExpressionNode.TypeAnnotation.AssertNotEmpty())
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
			case mSPO_AST.tVarToValNode<tPos> { Pos: var Pos, Obj: var Obj, TypeAnnotation: var Type }: {
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, Obj).Match(out var ObjReg, out var Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.VarGet(Pos, aDefConstructor.CreateTempReg(out var ResultReg), ObjReg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tSigNode<tPos> { Pos: var Pos, Contract: var Contract, Head: var Head, Body: var Body, TypeAnnotation: var Type }: {
				if (
					!aDefConstructor.TryMapKnownTypeValue(aModuleConstructor, Contract, out var ContractReg, out var Error) ||
					!aDefConstructor.TryMapExpression(aModuleConstructor, Head).Match(out var HeadReg, out Error) ||
					!aDefConstructor.TryMapExpression(aModuleConstructor, Body).Match(out var BodyReg, out Error)
				) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.CreatePair(Pos, aDefConstructor.CreateTempReg(out var PayloadReg), HeadReg, BodyReg),
					mIL_AST.CreateSig(
						Pos,
						aDefConstructor.CreateTempReg(out var ResultReg),
						ContractReg,
						PayloadReg
					)
				);
				aDefConstructor.AddLocal(PayloadReg, mVM_Type.Pair(Head.TypeAnnotation.AssertNotEmpty(), Body.TypeAnnotation.AssertNotEmpty()));
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> { Pos: var Pos, HeadType: var HeadType, BodyType: var BodyType, TypeAnnotation: var Type }: {
				mAssert.IsFalse(aDefConstructor.EnvIds.ToStream().Any(__ => __ == HeadType.Id));
				aDefConstructor.Commands.Push(
					mIL_AST.TypeFree(HeadType.Pos, HeadType.Id)
				);
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, BodyType).Match(out var BodyTypeReg, out var Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypeRecursive(Pos, aDefConstructor.CreateTempReg(out var ResultReg), HeadType.Id, BodyTypeReg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tSetTypeNode<tPos> { Expressions: var Expressions, TypeAnnotation: var Type }: {
				mAssert.IsTrue(Expressions.Is(out var Head, out var Tail));
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, Head).Match(out var ResultReg, out var Error)) {
					return mResult.Fail(Error);
				}
				foreach (var Expression in Tail) {
					if (!aDefConstructor.TryMapExpression(aModuleConstructor, Expression).Match(out var ExprReg, out Error)) {
						return mResult.Fail(Error);
					}
					aDefConstructor.Commands.Push(
						mIL_AST.TypeSet(Expression.Pos, aDefConstructor.CreateTempReg(out var SetTypeReg), ExprReg, ResultReg)
					);
					ResultReg = SetTypeReg;
				}
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tTupleTypeNode<tPos> { ItemTypes: var Expressions, TypeAnnotation: var Type }: {
				if (!Expressions.Is(out var First, out var Rest)) {
					return mIL_AST.cEmptyType;
				} else if (Rest.IsEmpty()) {
					return aDefConstructor.TryMapExpression(aModuleConstructor, First);
				} else {
					var ResultReg = mIL_AST.cEmptyType;
					foreach (var Head in Expressions) {
						if (!aDefConstructor.TryMapExpression(aModuleConstructor, Head).Match(out var HeadReg, out var Error)) {
							return mResult.Fail(Error);
						}
						aDefConstructor.Commands.Push(
							mIL_AST.TypePair(Head.Pos, aDefConstructor.CreateTempReg(out var PairTypeReg), ResultReg, HeadReg)
						);
						ResultReg = PairTypeReg;
					}
					aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
					return ResultReg;
				}
			}
			case mSPO_AST.tPairTypeNode<tPos> { Pos: var Pos, HeadType: var HeadType, TailType: var TailType, TypeAnnotation: var Type }: {
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, TailType).Match(out var TailTypeReg, out var Error)) {
					return mResult.Fail(Error);
				}
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, HeadType).Match(out var HeadTypeReg, out Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypePair(Pos, aDefConstructor.CreateTempReg(out var ResultReg), TailTypeReg, HeadTypeReg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tRecordTypeNode<tPos> { Pos: var Pos, Elements: var Elements, TypeAnnotation: var Type }: {
				var ResultReg = mIL_AST.cEmptyType;
				foreach (var (Key, FieldType) in Elements) {
					if (!aDefConstructor.TryMapExpression(aModuleConstructor, FieldType).Match(out var FieldReg, out var Error)) {
						return mResult.Fail(Error);
					}
					aDefConstructor.Commands.Push(
						mIL_AST.TypePrefix(Key.Pos, aDefConstructor.CreateTempReg(out var PrefixReg), Key.Id, FieldReg),
						mIL_AST.TypeRecord(Pos, aDefConstructor.CreateTempReg(out var RecordReg), ResultReg, PrefixReg)
					);
					ResultReg = RecordReg;
				}
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tPrefixTypeNode<tPos> { Pos: var Pos, Prefix: var Prefix, Expressions: var Expressions, TypeAnnotation: var Type }: {
				mAssert.IsTrue(Type.AssertNotEmpty().IsPrefix(out _, out var Inner));
				var InnerNode = mSPO_AST.TupleType(Pos, Expressions);
				InnerNode.TypeAnnotation = Inner;
				if (
					!aDefConstructor.TryMapExpression(
						aModuleConstructor,
						InnerNode
					).Match(out var InnerType, out var Error)
				) {
					return mResult.Fail(Error);
				}
				
				aDefConstructor.Commands.Push(
					mIL_AST.TypePrefix(Pos, aDefConstructor.CreateTempReg(out var ResultReg), Prefix.Id, InnerType)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tProcTypeNode<tPos> { Pos: var Pos, ObjType: var EnvType, ArgType: var ArgType, ResType: var ResType, TypeAnnotation: var TypeAnnotation }: {
				if (
					!aDefConstructor.TryMapExpression(aModuleConstructor, ArgType).Match(out var ArgReg, out var Error) ||
					!aDefConstructor.TryMapExpression(aModuleConstructor, ResType).Match(out var ResReg, out Error)
				) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypeFunc(Pos, aDefConstructor.CreateTempReg(out var ResultReg), ArgReg, ResReg)
				);
				if (EnvType.TypeAnnotation.AssertNotEmpty().IsEmpty()) {
					aDefConstructor.AddLocal(ResultReg, mVM_Type.Type());
					return ResultReg;
				}
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, EnvType).Match(out var EnvReg, out Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypeMethod(Pos, aDefConstructor.CreateTempReg(out var MethodReg), EnvReg, ResultReg)
				);
				aDefConstructor.AddLocal(MethodReg, mVM_Type.Type());
				return MethodReg;
			}
			case mSPO_AST.tGenericTypeNode<tPos> { Pos: var Pos, HeadType: var HeadType, BodyType: var BodyType, TypeAnnotation: var Type }: {
				mAssert.IsFalse(aDefConstructor.EnvIds.ToStream().Any(__ => __ == HeadType.Id));
				aDefConstructor.Commands.Push(
					mIL_AST.TypeFree(HeadType.Pos, HeadType.Id)
				);
				
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, BodyType).Match(out var BodyTypeReg, out var Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypeGeneric(Pos, aDefConstructor.CreateTempReg(out var ResultReg), HeadType.Id, BodyTypeReg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tGenericApplyTypeNode<tPos> { Pos: var Pos, GenericType: var GenericType, ArgType: var ArgType, TypeAnnotation: var Type }: {
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, GenericType).Match(out var GenericTypeReg, out var Error)) {
					return mResult.Fail(Error);
				}
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, ArgType).Match(out var ArgTypeReg, out Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypeGenericApply(Pos, aDefConstructor.CreateTempReg(out var ResultReg), GenericTypeReg, ArgTypeReg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tSigTypeNode<tPos> { Pos: var Pos, Head: var Head, HeadType: var HeadType, BodyType: var BodyType, TypeAnnotation: var Type }: {
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, HeadType).Match(out var KindReg, out var Error)) {
					return mResult.Fail(Error);
				}
				mAssert.IsTrue(Type.AssertNotEmpty().IsSig(out _, out var BinderKind, out _));
				var BinderId = BinderKind.IsProc(out _, out _, out _) ? Head.Id + "..." : Head.Id;
				aDefConstructor.Commands.Push(mIL_AST.TypeFree(Head.Pos, BinderId, KindReg));
				if (!aDefConstructor.TryMapExpression(aModuleConstructor, BodyType).Match(out var BodyReg, out Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypeSig(Pos, aDefConstructor.CreateTempReg(out var ResultReg), BinderId, BodyReg)
				);
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty().KindOf());
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

	private static tBool
	TryMapKnownTypeValue<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tTypeNode<tPos> aNode,
		[MaybeNullWhen(false)]out tText aReg,
		[MaybeNullWhen(true)]out (tPos Pos, tText ErrorText) aError
	) {
		if (!aNode.TypeAnnotation.IsSome(out var Value) || Value.KindOf() == Value) {
			return aDefConstructor.TryMapExpression(aModuleConstructor, aNode).Match(out aReg, out aError);
		}
		
		var Def = aDefConstructor;
		var Binders = mTreeMap.Tree<tNat64, tText>((a1, a2) => a1.CompareTo(a2).Sign(), []);
		tText Map(mVM_Type.tType aType) {
			tText Unary(
				mVM_Type.tType aInner,
				mStd.tFunc<tText, tText, mIL_AST.tCommandNode<tPos>> aCreate
			) {
				var Inner = Map(aInner);
				var Result = Def.CreateTempReg();
				Def.Commands.Push(aCreate(Result, Inner));
				Def.AddLocal(Result, aType.KindOf());
				return Result;
			}
			
			tText Binary(
				mVM_Type.tType aLeft,
				mVM_Type.tType aRight,
				mStd.tFunc<tText, tText, tText, mIL_AST.tCommandNode<tPos>> aCreate
			) {
				var Left = Map(aLeft);
				var Right = Map(aRight);
				var Result = Def.CreateTempReg();
				Def.Commands.Push(aCreate(Result, Left, Right));
				Def.AddLocal(Result, aType.KindOf());
				return Result;
			}
			
			switch (aType.Kind) {
				case mVM_Type.tKind.Empty: return mIL_AST.cEmptyType;
				case mVM_Type.tKind.Int: return mIL_AST.cIntType;
				case mVM_Type.tKind.Type: return mIL_AST.cTypeType;
				case mVM_Type.tKind.True: return mIL_AST.cTrue;
				case mVM_Type.tKind.False: return mIL_AST.cFalse;
				case mVM_Type.tKind.TypeVariable: {
					if (Binders.TryGet(aType.DebugId).IsSome(out var Binder)) {
						return Binder;
					}
					var Id = aType.Id ?? throw mError.Error("type variable without display name");
					return aType.KindOf().IsProc(out _, out _, out _) ? Id + "..." : Id;
				}
				case mVM_Type.tKind.Pair:
					return Binary(aType.Refs[0], aType.Refs[1], (aResult, aLeft, aRight) => mIL_AST.TypePair(aNode.Pos, aResult, aLeft, aRight));
				case mVM_Type.tKind.Set:
					return Binary(aType.Refs[0], aType.Refs[1], (aResult, aLeft, aRight) => mIL_AST.TypeSet(aNode.Pos, aResult, aLeft, aRight));
				case mVM_Type.tKind.Proc: {
					var Func = Binary(aType.Refs[1], aType.Refs[2], (aResult, aArg, aResultType) => mIL_AST.TypeFunc(aNode.Pos, aResult, aArg, aResultType));
					if (aType.Refs[0].IsEmpty()) {
						return Func;
					}
					var Env = Map(aType.Refs[0]);
					var Result = Def.CreateTempReg();
					Def.Commands.Push(mIL_AST.TypeMethod(aNode.Pos, Result, Env, Func));
					Def.AddLocal(Result, aType.KindOf());
					return Result;
				}
				case mVM_Type.tKind.Prefix: {
					var Inner = Map(aType.Refs[0]);
					var Result = Def.CreateTempReg();
					Def.Commands.Push(mIL_AST.TypePrefix(aNode.Pos, Result, aType.Prefix ?? throw mError.Error("prefix type without prefix"), Inner));
					Def.AddLocal(Result, aType.KindOf());
					return Result;
				}
				case mVM_Type.tKind.Record: {
					var Result = mIL_AST.cEmptyType;
					foreach (var Field in aType.Fields.ToStream()) {
						var FieldType = Map(Field.Value);
						var Prefix = Def.CreateTempReg();
						var Record = Def.CreateTempReg();
						Def.Commands.Push(
							mIL_AST.TypePrefix(aNode.Pos, Prefix, Field.Key, FieldType),
							mIL_AST.TypeRecord(aNode.Pos, Record, Result, Prefix)
						);
						Def.AddLocal(Prefix, mVM_Type.Type());
						Def.AddLocal(Record, mVM_Type.Type());
						Result = Record;
					}
					return Result;
				}
				case mVM_Type.tKind.Var:
					return Unary(aType.Refs[0], (aResult, aInner) => mIL_AST.TypeVar(aNode.Pos, aResult, aInner));
				case mVM_Type.tKind.TypeApply:
					return Binary(aType.Refs[0], aType.Refs[1], (aResult, aConstructor, aArgument) => mIL_AST.TypeGenericApply(aNode.Pos, aResult, aConstructor, aArgument));
				case mVM_Type.tKind.Sig:
				case mVM_Type.tKind.Generic:
				case mVM_Type.tKind.Recursive:
				case mVM_Type.tKind.Interface: {
					var Variable = aType.Refs[0];
					var Kind = Variable.KindOf();
					var Binder = Def.CreateTempReg();
					if (Kind.IsType()) {
						Def.Commands.Push(mIL_AST.TypeFree(aNode.Pos, Binder));
					} else {
						var KindReg = Map(Kind);
						Def.Commands.Push(mIL_AST.TypeFree(aNode.Pos, Binder, KindReg));
					}
					Def.AddLocal(Binder, Kind);
					Binders = Binders.Set(Variable.DebugId, Binder);
					var Body = Map(aType.Refs[1]);
					var Result = Def.CreateTempReg();
					Def.Commands.Push(aType.Kind switch {
						mVM_Type.tKind.Sig => mIL_AST.TypeSig(aNode.Pos, Result, Binder, Body),
						mVM_Type.tKind.Generic => mIL_AST.TypeGeneric(aNode.Pos, Result, Binder, Body),
						mVM_Type.tKind.Recursive => mIL_AST.TypeRecursive(aNode.Pos, Result, Binder, Body),
						mVM_Type.tKind.Interface => mIL_AST.TypeInterface(aNode.Pos, Result, Binder, Body),
						_ => throw mError.Error("impossible")
					});
					Def.AddLocal(Result, aType.KindOf());
					return Result;
				}
				default: throw new System.NotImplementedException($"runtime type value {aType.Kind}");
			}
		}

		aReg = Map(Value);
		aDefConstructor = Def;
		aError = default;
		return true;
	}

	private static tBool
	TryMapSigSpec<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tSigPatternNode<tPos> aPattern,
		[MaybeNullWhen(false)]out tText aSpecReg,
		[MaybeNullWhen(true)]out (tPos Pos, tText ErrorText) aError
	) {
		if (!aDefConstructor.TryMapKnownTypeValue(aModuleConstructor, aPattern.Contract, out aSpecReg, out aError)) {
			return false;
		}
		if (aPattern.Head is not mSPO_AST.tTypePatternNode<tPos> Concrete) {
			return true;
		}
		if (!aDefConstructor.TryMapKnownTypeValue(aModuleConstructor, Concrete.Type, out var ExpectedReg, out aError)) {
			return false;
		}
		aDefConstructor.Commands.Push(
			mIL_AST.TypePair(aPattern.Pos, aDefConstructor.CreateTempReg(out var SpecReg), aSpecReg, ExpectedReg)
		);
		aDefConstructor.AddLocal(SpecReg, mVM_Type.Type());
		aSpecReg = SpecReg;
		return true;
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
				aModuleConstructor,
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
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tPatternNode<tPos> aMatch,
		tText aInputReg,
		out (tPos Pos, tText ErrorText) aError
	) {
		switch (aMatch) {
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
				aCaseFunc.AddLocal(Id, aMatch.TypeAnnotation.AssertNotEmpty());
				break;
			}
			case mSPO_AST.tIgnorePatternNode<tPos>:
			case mSPO_AST.tEmptyNode<tPos>:
			case mSPO_AST.tTrueNode<tPos>:
			case mSPO_AST.tFalseNode<tPos>:
			case mSPO_AST.tIntNode<tPos>: {
				break;
			}
			case mSPO_AST.tSigPatternNode<tPos> Node: {
				if (!aCaseFunc.TryMapSigSpec(aModuleConstructor, Node, out var Spec, out aError)) {
					return false;
				}
				var Opened = aCaseFunc.CreateTempReg();
				aCaseFunc.Commands.Push(mIL_AST.TryAsSig(Node.Pos, Opened, aInputReg, Spec));
				aCaseFunc.Commands.Push(
					mIL_AST.GetSigHead(Node.Pos, aCaseFunc.CreateTempReg(out var Head), Opened),
					mIL_AST.GetSigBody(Node.Pos, aCaseFunc.CreateTempReg(out var Body), Opened)
				);
				if (
					!aCaseFunc.TryBindMatchedPattern(aModuleConstructor, Node.Head, Head, out aError) ||
					!aCaseFunc.TryBindMatchedPattern(aModuleConstructor, Node.Body, Body, out aError)
				) {
					return false;
				}
				if (
					Node.Head.TryGetId().IsSome(out var HeadId) &&
					Node.Head.TypeAnnotation.AssertNotEmpty().IsProc(out _, out _, out _)
				) {
					aCaseFunc.Commands.Push(mIL_AST.Alias(Node.Head.Pos, HeadId + "...", Head));
					aCaseFunc.AddLocal(HeadId + "...", Node.Head.TypeAnnotation.AssertNotEmpty());
				}
				break;
			}
			case mSPO_AST.tTypePatternNode<tPos>: {
				break;
			}
			case mSPO_AST.tPrefixPatternNode<tPos> Node: {
				aCaseFunc.Commands.Push(
					mIL_AST.SubPrefix(Node.Pos, aCaseFunc.CreateTempReg(out var InnerArg), Node.Prefix, aInputReg)
				);
				if (
					!aCaseFunc.TryBindMatchedPattern(
						aModuleConstructor,
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
						aModuleConstructor,
						Node.Tail,
						FirstReg,
						out aError
					) ||
					!aCaseFunc.TryBindMatchedPattern(
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
					aCaseFunc.Commands.Push(
						mIL_AST.GetField(IdNode.Pos, aCaseFunc.CreateTempReg(out var FieldReg), aInputReg, IdNode.Id)
					);
					
					if (
						!aCaseFunc.TryBindMatchedPattern(
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
					!aCaseFunc.TryBindMatchedPattern(
						aModuleConstructor,
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
						aModuleConstructor,
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
		mSPO_AST.tIntNode<tPos> or
		mSPO_AST.tGuardPatternNode<tPos> =>
			true,
		mSPO_AST.tFreeIdPatternNode<tPos> or
		mSPO_AST.tIdNode<tPos> or
		mSPO_AST.tVarPatternNode<tPos> or
		mSPO_AST.tIgnorePatternNode<tPos> or
		mSPO_AST.tEmptyNode<tPos> or
		mSPO_AST.tTrueNode<tPos> or
		mSPO_AST.tFalseNode<tPos> =>
			false,
		mSPO_AST.tPairPatternNode<tPos> Pair =>
			Pair.Tail.HasGuards() || Pair.Head.HasGuards(),
		mSPO_AST.tPrefixPatternNode<tPos> Prefix =>
			Prefix.Pattern.HasGuards(),
		mSPO_AST.tRecordPatternNode<tPos> Record =>
			Record.Elements.Any(__ => __.Pattern.HasGuards()),
		mSPO_AST.tSigPatternNode<tPos> => true,
		mSPO_AST.tTypePatternNode<tPos> => false,
		mSPO_AST.tTypedPatternNode<tPos> Typed =>
			Typed.Pattern.HasGuards(),
		mSPO_AST.tCharNode<tPos> or
		mSPO_AST.tTuplePatternNode<tPos> =>
			throw mError.Error($"{aPattern.GetType().Name} should already be desugared"),
		_ =>
			throw new System.NotImplementedException(aPattern.GetType().Name),
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
				aGuardFunc.AddLocal(Id, aPattern.TypeAnnotation.AssertNotEmpty());
				break;
			}
			case mSPO_AST.tIgnorePatternNode<tPos>:
			case mSPO_AST.tEmptyNode<tPos>:
			case mSPO_AST.tTrueNode<tPos>:
			case mSPO_AST.tFalseNode<tPos>: {
				break;
			}
			case mSPO_AST.tSigPatternNode<tPos> Node: {
				if (!aGuardFunc.TryMapSigSpec(aModuleConstructor, Node, out var Spec, out aError)) {
					return false;
				}
				var Opened = aGuardFunc.CreateTempReg();
				aGuardFunc.Commands.Push(mIL_AST.TryAsSig(Node.Pos, Opened, aInputReg, Spec));
				aGuardFunc.Commands.Push(
					mIL_AST.GetSigHead(Node.Pos, aGuardFunc.CreateTempReg(out var Head), Opened),
					mIL_AST.GetSigBody(Node.Pos, aGuardFunc.CreateTempReg(out var Body), Opened)
				);
				if (
					!aGuardFunc.TryMapPatternGuard(aModuleConstructor, Node.Head, Head, out aError) ||
					!aGuardFunc.TryMapPatternGuard(aModuleConstructor, Node.Body, Body, out aError)
				) {
					return false;
				}
				if (
					Node.Head.TryGetId().IsSome(out var HeadId) &&
					Node.Head.TypeAnnotation.AssertNotEmpty().IsProc(out _, out _, out _)
				) {
					aGuardFunc.Commands.Push(mIL_AST.Alias(Node.Head.Pos, HeadId + "...", Head));
					aGuardFunc.AddLocal(HeadId + "...", Node.Head.TypeAnnotation.AssertNotEmpty());
				}
				break;
			}
			case mSPO_AST.tTypePatternNode<tPos>: {
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
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tPatternNode<tPos> aPatternNode,
		tText aRegId,
		out (tPos, tText) aError
	) {
		switch (aPatternNode) {
			case mSPO_AST.tIdNode<tPos> { Pos: var Pos, Id: var Name, TypeAnnotation: var Type }: {
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
			case mSPO_AST.tFreeIdPatternNode<tPos> { Pos: var Pos, Id: var Name, TypeAnnotation: var Type }: {
				mAssert.AreNotEquals(Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(aPatternNode.Pos, Name, aRegId));
				aDefConstructor.AddArg(Name, Type.AssertNotEmpty());
				break;
			}
			case mSPO_AST.tVarPatternNode<tPos> { Pos: var Pos, Id: var Name, TypeAnnotation: var Type }: {
				mAssert.AreNotEquals(Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(aPatternNode.Pos, Name, aRegId));
				aDefConstructor.AddArg(Name, Type.AssertNotEmpty());
				break;
			}
			case mSPO_AST.tIgnorePatternNode<tPos> IgnorePatternNode: {
				break;
			}
			case mSPO_AST.tSigPatternNode<tPos> Node: {
				if (!aDefConstructor.TryMapSigSpec(aModuleConstructor, Node, out var Spec, out aError)) {
					return false;
				}
				var Opened = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.TryAsSig(Node.Pos, Opened, aRegId, Spec));
				aDefConstructor.Commands.Push(
					mIL_AST.GetSigHead(Node.Pos, aDefConstructor.CreateTempReg(out var Head), Opened),
					mIL_AST.GetSigBody(Node.Pos, aDefConstructor.CreateTempReg(out var Body), Opened)
				);
				if (
					!aDefConstructor.MapPattern(aModuleConstructor, Node.Head, Head, out aError) ||
					!aDefConstructor.MapPattern(aModuleConstructor, Node.Body, Body, out aError)
				) {
					return false;
				}
				if (
					Node.Head.TryGetId().IsSome(out var HeadId) &&
					Node.Head.TypeAnnotation.AssertNotEmpty().IsProc(out _, out _, out _)
				) {
					aDefConstructor.Commands.Push(mIL_AST.Alias(Node.Head.Pos, HeadId + "...", Head));
					aDefConstructor.AddArg(HeadId + "...", Node.Head.TypeAnnotation.AssertNotEmpty());
				}
				break;
			}
			case mSPO_AST.tTypePatternNode<tPos>: {
				break;
			}
			case mSPO_AST.tPrefixPatternNode<tPos> { Pos: var Pos, Prefix: var Prefix, Pattern: var Pattern, TypeAnnotation: var Type }: {
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(Pos, aDefConstructor.CreateTempReg(out var ResultReg), Prefix, aRegId)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return aDefConstructor.MapPattern(aModuleConstructor, Pattern, ResultReg, out aError);
			}
			case mSPO_AST.tRecordPatternNode<tPos> { Elements: var Elements, TypeAnnotation: var TypeAnnotation }: {
				foreach (var (IdNode, Pattern) in Elements) {
					aDefConstructor.Commands.Push(mIL_AST.GetField(IdNode.Pos, aDefConstructor.CreateTempReg(out var FieldReg), aRegId, IdNode.Id));
					if (!aDefConstructor.MapPattern(aModuleConstructor, Pattern, FieldReg, out aError)) {
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
					aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ItemReg, Item.TypeAnnotation.AssertNotEmpty());
					if (!aDefConstructor.MapPattern(aModuleConstructor, Item, ItemReg, out aError)) {
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
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(HeadReg, Head.TypeAnnotation.AssertNotEmpty());
				if (!aDefConstructor.MapPattern(aModuleConstructor, Head, HeadReg, out aError)) {
					return false;
				}
				
				aDefConstructor.Commands.Push(
					mIL_AST.GetFirst(Pos, aDefConstructor.CreateTempReg(out var TailReg), aRegId)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(TailReg, Tail.TypeAnnotation.AssertNotEmpty());
				if (!aDefConstructor.MapPattern(aModuleConstructor, Tail, TailReg, out aError)) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tGuardPatternNode<tPos> { Pattern: var Pattern, Guard: var Guard }: {
				// TODO: ASSERT Guard
				return aDefConstructor.MapPattern(aModuleConstructor, Pattern, aRegId, out aError);
			}
			case mSPO_AST.tTypedPatternNode<tPos> { Pattern: var Pattern }: {
				return aDefConstructor.MapPattern(aModuleConstructor, Pattern, aRegId, out aError);
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
		aDefConstructor.MapPattern(aModuleConstructor, aDefNode.Des, ValueReg, out aError)
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
		var IsSingle = aRecLambdasNode.List.Count() is 1;
		
		var RecFactoryFunc = NewDefConstructor<tPos>();
		
		if (IsSingle) {
			var RecProc = aRecLambdasNode.List.TryFirst().AssertNotEmpty();
			RecFactoryFunc.AddArg(
				RecProc.Id.Id,
				RecProc.Lambda.TypeAnnotation.AssertNotEmpty()
			);
			RecFactoryFunc.Commands.Push(
				mIL_AST.Alias(RecProc.Pos, RecProc.Id.Id, mIL_AST.cArg)
			);
		} else {
			var Arg = mIL_AST.cArg;
			foreach (var RecProc in aRecLambdasNode.List) {
				RecFactoryFunc.AddArg(
					RecProc.Id.Id,
					RecProc.Lambda.TypeAnnotation.AssertNotEmpty()
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
			var RecProcConstructor = NewDefConstructor<tPos>();
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
			? aRecLambdasNode.List.TryFirst().AssertNotEmpty().Lambda.TypeAnnotation.AssertNotEmpty()
			: aRecLambdasNode.List.Reduce(
				mVM_Type.Empty(),
				(Acc, RecProc) => mVM_Type.Pair(
					Acc,
					RecProc.Lambda.TypeAnnotation.AssertNotEmpty()
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
				RecProc.Lambda.TypeAnnotation.AssertNotEmpty()
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
					RecProc.Lambda.TypeAnnotation.AssertNotEmpty()
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
			mVM_Type.Var(aDefVarNode.Expression.TypeAnnotation.AssertNotEmpty())
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
		// TODO: set proper Def type ?
		
		if (!aDefConstructor.TryMapExpression(aModuleConstructor, aMethodCallsNode.Object).Match(out var Object, out aError)) {
			return false;
		}
		foreach (var Call in aMethodCallsNode.MethodCalls) {
			if (!aDefConstructor.TryMapExpression(aModuleConstructor, Call.Argument).Match(out var Arg, out aError)) {
				return false;
			}
			if (Call.Argument.TypeAnnotation.IsSome(out var ArgType) && ArgType.IsVar(out var ArgInnerType)) {
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
			var ResultType = Call.Result.Then(__ => __.TypeAnnotation.AssertNotEmpty()).ElseUse(mVM_Type.Empty());
			
			aDefConstructor.Commands.Push(
				[
					mIL_AST.CreatePair(aMethodCallsNode.Object.Pos, aDefConstructor.CreateTempReg(out var MethodReg), Object, MethodId),
					mIL_AST.CallProc(aMethodCallsNode.Pos, Result, MethodReg, Arg)
				]
			);
			if (Call.Result.IsSome(out var Result_)) {
				if (!aDefConstructor.MapPattern(aModuleConstructor, Result_, Result, out aError)) {
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
		mStream.tStream<mSPO_AST_Types.tScopeItem> aScope
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
		if (!Lambda.UpdateTypes(aScope).Match(out _, out var Error)) {
			return mResult.Fail(Error);
		}
		
		var ModuleConstructor = NewModuleConstructor(aMergePos);
		var TempLambdaDef = NewDefConstructor<tPos>();
		
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
