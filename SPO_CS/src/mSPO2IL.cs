// IMPORT Common/mStd
// IMPORT Common/mAssert
// IMPORT Common/mError
// IMPORT Common/mTreeMap
// IMPORT Common/mMath
// IMPORT Common/mMaybe
// IMPORT Common/mResult
// IMPORT Common/mArrayList
// IMPORT Common/mStream
// IMPORT Common/mPerf
// IMPORT mVM_Type
// IMPORT mIL_AST
// IMPORT mIL_GenerateOpcodes
// IMPORT mSPO_AST
// IMPORT mSPO_AST_Types

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
			mAssert.AreEquals(ExistingType, aType);
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
			case var a when a.IsBool(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cBoolType, a);
				return mIL_GenerateOpcodes.cBoolType;
			}
			case var a when a.IsInt(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cIntType, a);
				return mIL_GenerateOpcodes.cIntType;
			}
			case var a when a.IsAny(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cAnyType, a);
				return mIL_GenerateOpcodes.cAnyType;
			}
			case var a when a.IsFree(out var Id_, out var Ref): {
				if (Ref.Kind is mVM_Type.tKind.Free) {
					aModuleConstructor.EnsureTypeDefinition(Id_, a, () => mIL_AST.TypeFree(default(tPos), Id_));
					return Id_;
				} else {
					return aModuleConstructor.MapType(Ref);
				}
			}
			case var a when a.IsPrefix(out var Prefix, out var Type): {
				var Id = aModuleConstructor.MapType(Type);
				var NewId = $"[#{Prefix}:{Id}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypePrefix(default(tPos), NewId, Prefix, Id));
				return NewId;
			}
			case var a when a.IsPair(out var Type1, out var Type2): {
				var Id1 = aModuleConstructor.MapType(Type1);
				var Id2 = aModuleConstructor.MapType(Type2);
				var NewId = $"[{Id1};{Id2}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypePair(default(tPos), NewId, Id1, Id2));
				return NewId;
			}
			case var a when a.IsRecord(out var Fields): {
				var RecTypeId = mIL_AST.cEmptyType;
				foreach (var Field in Fields.ToStream()) {
					var FieldTypeId = aModuleConstructor.MapType(Field.Value);
					var PrefixedFieldTypeId = $"[#{Field.Key} {FieldTypeId}]";
					aModuleConstructor.TypeDef.Push(mIL_AST.TypePrefix(default(tPos), PrefixedFieldTypeId, Field.Key.ToString(), FieldTypeId)); // TODO: remove .ToString() ???
					var NewRecTypeId = $"[{RecTypeId}, {PrefixedFieldTypeId}]";
					aModuleConstructor.TypeDef.Push(mIL_AST.TypeRecord(default(tPos), NewRecTypeId, RecTypeId, PrefixedFieldTypeId));
					RecTypeId = NewRecTypeId;
				}
				aModuleConstructor.Types = aModuleConstructor.Types.Set(RecTypeId, a);
				return RecTypeId;
			}
			case var a when a.IsSet(out var Type1, out var Type2): {
				var Id1 = aModuleConstructor.MapType(Type1);
				var Id2 = aModuleConstructor.MapType(Type2);
				var NewId = $"[{Id1}|{Id2}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeSet(default(tPos), NewId, Id1, Id2));
				return NewId;
			}
			case var a when a.IsProc(out var EnvType, out var ArgType, out var ResType): {
				var IdArg = aModuleConstructor.MapType(ArgType);
				var IdRes = aModuleConstructor.MapType(ResType);
				var IdFunc = $"[{IdArg}->{IdRes}]";
				var FuncType = mVM_Type.Proc(mVM_Type.Empty(), ArgType, ResType);
				aModuleConstructor.EnsureTypeDefinition(IdFunc, FuncType, () => mIL_AST.TypeFunc(default(tPos), IdFunc, IdArg, IdRes));
				
				if (EnvType.IsEmpty()) {
					return IdFunc;
				}
				
				var IdEnv = aModuleConstructor.MapType(EnvType);
				var IdEnvFunc = $"[{IdEnv}:{IdFunc}]";
				aModuleConstructor.EnsureTypeDefinition(IdEnvFunc, a, () => mIL_AST.TypeMethod(default(tPos), IdEnvFunc, IdEnv, IdFunc));
				return IdEnvFunc;
			}
			case var a when a.IsVar(out var InnerType): {
				var InnerId = aModuleConstructor.MapType(InnerType);
				var NewId = $"[§VAR {InnerId}]";
				
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeVar(default(tPos), NewId, InnerId));
				
				return NewId;
			}
			case var a when a.IsRecursive(out var HeadType, out var BodyType): {
				var HeadId = aModuleConstructor.MapType(HeadType);
				var BodyId = aModuleConstructor.MapType(BodyType);
				var NewId = $"[§REC {HeadId} => {BodyId}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeRecursive(default(tPos), NewId, HeadId, BodyId));
				return NewId;
			}
			case var a when a.IsGeneric(out var HeadType, out var BodyType): {
				var HeadId = aModuleConstructor.MapType(HeadType);
				var BodyId = aModuleConstructor.MapType(BodyType);
				var NewId = $"[$ALL {HeadId} => {BodyId}]";
				aModuleConstructor.EnsureTypeDefinition(NewId, a, () => mIL_AST.TypeGeneric(default(tPos), NewId, HeadId, BodyId));
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
			_ => {
				var MaybeType = TypeDict.TryGet(_);
				if (MaybeType.IsSome(out var T)) {
					return T;
				}
				if (!_.StartsWith("d_")) {
					throw mError.Error($"'{_}' id not a Def");
				}
				var DefIndexText = _[2..];
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
		_ => mVM_Type.Proc(
			mVM_Type.Empty(),
			_,
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
			!aDefConstructor.MapPattern(ref aModuleConstructor, aLambdaNode.Head, mIL_AST.cArg, out var Error) ||
			!aDefConstructor.MapExpression(aModuleConstructor, aLambdaNode.Body).Match(out var ResultReg, out Error)
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
			_ => (aLambdaNode.Pos, _)
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
			!aDefConstructor.MapPattern(ref aModuleConstructor, aMethodNode.Arg, mIL_AST.cArg, out var Error) ||
			!aDefConstructor.MapPattern(ref aModuleConstructor, aMethodNode.Obj, mIL_AST.cObj, out Error) ||
			!aDefConstructor.MapExpression(aModuleConstructor, aMethodNode.Body).Match(out var ResultReg, out Error)
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
			_ => (aMethodNode.Pos, _)
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
					_ => mSPO_AST_Types.ScopeItem(
						_,
						TempMethodDef.TypeDict.TryGet(_).AssertNotEmpty()
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
	MapExpression<tPos>(
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
			case mSPO_AST.tBoolTypeNode<tPos> BoolTypeNode: {
				return mIL_AST.cBoolType;
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
						_ => _.GetResultReg().Match(
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
					!aDefConstructor.MapExpression(aModuleConstructor, Func).Match(out var FuncReg, out var Error) ||
					!aDefConstructor.MapExpression(aModuleConstructor, Arg).Match(out var ArgReg, out Error)
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
						return aDefConstructor.MapExpression(aModuleConstructor, Head);
					}
					default: {
						mAssert.IsTrue(Items.Is(out var Head, out var _));
						var TailReg = mIL_AST.cEmptyValue;
						foreach (var Item in Items) {
							if (!aDefConstructor.MapExpression(aModuleConstructor, Item).Match(out var HeadReg, out var Error)) {
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
				if (!aDefConstructor.MapExpression(aModuleConstructor, Tail).Match(out var TailReg, out var Error)) {
					return mResult.Fail(Error);
				}
				if (!aDefConstructor.MapExpression(aModuleConstructor, Head).Match(out var HeadReg, out Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(mIL_AST.CreatePair(Pos, aDefConstructor.CreateTempReg(out var ResultReg), TailReg, HeadReg));
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tPrefixNode<tPos> { Pos: var Pos, Prefix: var Prefix, Element: var Element, TypeAnnotation: var Type }: {
				if (!aDefConstructor.MapExpression(aModuleConstructor, Element).Match(out var ExpressionReg, out var Error)) {
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
					if (!aDefConstructor.MapExpression(aModuleConstructor, Value).Match(out var Expression, out var Error)) {
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
						_ => _
					)
				);
				return CharReg;
			}
			case mSPO_AST.tTextNode<tPos> { Pos: var Pos, Value: var Value }: {
				var TailReg = mIL_AST.cEmptyValue;
				var Index = Value.Length;
				while (Index --> 0) {
					var Char = Value[Index];
					aDefConstructor.Commands.Push(
						[
							mIL_AST.CreateInt(Pos, aDefConstructor.CreateTempReg(out var CharOrdReg), ((tInt32)Char).ToString()),
							mIL_AST.AddPrefix(Pos, aDefConstructor.CreateTempReg(out var HeadReg), "_Char...", CharOrdReg),
							mIL_AST.CreatePair(Pos, aDefConstructor.CreateTempReg(out var TextReg), TailReg, HeadReg)
						]
					);
					TailReg = TextReg;
				}
				return TailReg;
			}
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
					_ => mSPO_AST_Types.ScopeItem(
						_,
						LambdaDef.TypeDict.TryGet(_).AssertNotEmpty()
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
				if (!aModuleConstructor.MapMethod(MethodNode).Match(out var MethodDef, out var Error)) {
					return mResult.Fail(Error);
				}
				return aDefConstructor.InitProc(
					MethodNode.Pos,
					MethodDef.Index,
					MethodDef.Type,
					MethodDef.EnvList
				);
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
					Ifs.Push(mSPO_AST.ReturnIf(aModuleConstructor.MergePos(Test.Pos, Run.Pos), Test, Run));
				}
				Ifs.Push(
					mSPO_AST.ReturnIf(
						Pos,
						mSPO_AST.True(Pos),
						mSPO_AST.Empty(Pos)
					)
				); // TODO: ASSERT FALSE
				
				var ResultReg = aDefConstructor.CreateTempReg();
				
				var Def = mSPO_AST.Def(
					Pos,
					mSPO_AST.Pattern(
						Pos,
						new mSPO_AST.tFreeIdPatternNode<tPos> { Id = ResultReg },
						mStd.cEmpty
					),
					mSPO_AST.Call(
						Pos,
						mSPO_AST.Lambda(
							Pos,
							mStd.cEmpty,
							mSPO_AST.Pattern(
								Pos,
								mSPO_AST.Empty(Pos),
								mStd.cEmpty
							),
							mSPO_AST.Block(Pos, Ifs.ToStream())
						),
						mSPO_AST.Empty(Pos)
					)
				);
				
				if (
					!mSPO_AST_Types.UpdateCommandTypes(Def, mStd.cEmpty).Match(out _, out var Error) ||
					!aDefConstructor.MapCommand(aModuleConstructor, Def, out Error)
				) {
					return mResult.Fail(Error);
				}
				
				return ResultReg;
			}
			case mSPO_AST.tIfMatchNode<tPos> {Pos: var Pos, Expression: var MatchExpression, Cases: var Cases, TypeAnnotation: var Type }: {
				if (!aDefConstructor.MapExpression(aModuleConstructor, MatchExpression).Match(out var InputReg, out var Error)) {
					return mResult.Fail(Error);
				}
				
				var SwitchDef = NewDefConstructor<tPos>();
				
				var CaseType = mVM_Type.Proc(
					mVM_Type.Empty(),
					MatchExpression.TypeAnnotation.AssertNotEmpty(),
					mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Prefix("Result", aExpressionNode.TypeAnnotation.AssertNotEmpty()))
				);
					
				// TODO: add gard command for the case that no case matched the argument
				
				foreach (var Case in Cases) {
					var CasePos = aModuleConstructor.MergePos(Case.Pattern.Pos, Case.Expression.Pos);
					
					var TestAndCallCaseFunc = NewDefConstructor<tPos>();
					
					var ErrorText_ = default(tText?);
					if (
						!aModuleConstructor.MapIfCase(
							ref TestAndCallCaseFunc,
							Case,
							MatchExpression.TypeAnnotation.AssertNotEmpty(),
							CasePos,
							out Error
						) ||
						!TestAndCallCaseFunc.CreateDefType(
							aModuleConstructor,
							CaseType
						).Match(out var CaseDefType, out ErrorText_)
					) {
						return mResult.Fail((CasePos, ErrorText_ ?? Error.ErrorText));
					}
					
					var TestAndCallDefIndex = TestAndCallCaseFunc.FinishMapProc(
						Pos,
						aModuleConstructor,
						CaseDefType
					);
					
					mAssert.IsTrue(
						CaseDefType.IsProc(out var Empty1, out var DefEnvType, out var DefLambdaType),
						$"expected proc but is: {CaseDefType.ToText()}"
					);
					mAssert.IsTrue(
						Empty1.IsEmpty(),
						$"expected proc but is: {CaseDefType.ToText()}"
					);
					mAssert.IsTrue(
						DefLambdaType.IsProc(out var DefObjType, out var DefArgType, out var DefResType),
						$"expected proc but is: {CaseDefType.ToText()}"
					);
					mAssert.IsTrue(
						DefResType.IsSet(out var Empty2, out var Prefix__),
						$"expected proc but is: {CaseDefType.ToText()}"
					);
					mAssert.IsTrue(
						Empty2.IsEmpty(),
						$"expected proc but is: {CaseDefType.ToText()}"
					);
					mAssert.IsTrue(
						Prefix__.IsPrefix("Result", out var DefResType__),
						$"expected proc but is: {CaseDefType.ToText()}"
					);
					
					var SwitchDefType_ = mVM_Type.Proc(
						mVM_Type.Empty(),
						DefEnvType,
						mVM_Type.Proc(
							DefObjType,
							DefArgType,
							DefResType__
						)
					);
					
					var TypeDict_ = TestAndCallCaseFunc.TypeDict;
					var ProcId = SwitchDef.InitProc(
						Pos,
						TestAndCallDefIndex,
						SwitchDefType_,
						TestAndCallCaseFunc.EnvIds.ToStream(
						).Map(
							_ => mSPO_AST_Types.ScopeItem(
								_,
								TypeDict_.TryGet(_).AssertNotEmpty()
							)
						)
					);
					
					SwitchDef.Commands.Push(
						[
							mIL_AST.CallFunc(CasePos, SwitchDef.CreateTempReg(out var Res_), ProcId, mIL_AST.cArg),
							mIL_AST.ReturnIfNotEmpty(CasePos, Res_)
						]
					);
				}
				
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
						_ => mSPO_AST_Types.ScopeItem(
							_,
							SwitchDef.TypeDict.TryGet(_).AssertNotEmpty()
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
				if (!aDefConstructor.MapExpression(aModuleConstructor, Obj).Match(out var ObjReg, out var Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.VarGet(Pos, aDefConstructor.CreateTempReg(out var ResultReg), ObjReg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> { Pos: var Pos, HeadType: var HeadType, BodyType: var BodyType, TypeAnnotation: var Type }: {
				mAssert.IsFalse(aDefConstructor.EnvIds.ToStream().Any(__ => __ == HeadType.Id));
				aDefConstructor.Commands.Push(
					mIL_AST.TypeFree(HeadType.Pos, HeadType.Id)
				);
				if (!aDefConstructor.MapExpression(aModuleConstructor, BodyType).Match(out var BodyTypeReg, out var Error)) {
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
				if (!aDefConstructor.MapExpression(aModuleConstructor, Head).Match(out var ResultReg, out var Error)) {
					return mResult.Fail(Error);
				}
				foreach (var Expression in Tail) {
					if (!aDefConstructor.MapExpression(aModuleConstructor, Expression).Match(out var ExprReg, out Error)) {
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
				} else {
					var Pos = First.Pos;
					if (!aDefConstructor.MapExpression(aModuleConstructor, First).Match(out var ResultReg, out var Error)) {
						return mResult.Fail(Error);
					}
					foreach (var Head in Rest) {
						if (!aDefConstructor.MapExpression(aModuleConstructor, Head).Match(out var HeadReg, out Error)) {
							return mResult.Fail(Error);
						}
						Pos = aModuleConstructor.MergePos(Pos, Head.Pos);
						aDefConstructor.Commands.Push(
							mIL_AST.TypePair(Pos, aDefConstructor.CreateTempReg(out var PairTypeReg), ResultReg, HeadReg)
						);
						ResultReg = PairTypeReg;
					}
					aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
					return ResultReg;
				}
			}
			case mSPO_AST.tPairTypeNode<tPos> { Pos: var Pos, HeadType: var HeadType, TailType: var TailType, TypeAnnotation: var Type }: {
				if (!aDefConstructor.MapExpression(aModuleConstructor, TailType).Match(out var TailTypeReg, out var Error)) {
					return mResult.Fail(Error);
				}
				if (!aDefConstructor.MapExpression(aModuleConstructor, HeadType).Match(out var HeadTypeReg, out Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypePair(Pos, aDefConstructor.CreateTempReg(out var ResultReg), TailTypeReg, HeadTypeReg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tPrefixTypeNode<tPos> { Pos: var Pos, Prefix: var Prefix, Expressions: var Expressions, TypeAnnotation: var Type }: {
				if (
					!aDefConstructor.MapExpression(
						aModuleConstructor,
						mSPO_AST.TupleType(Pos, Expressions)
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
			case mSPO_AST.tGenericTypeNode<tPos> { Pos: var Pos, HeadType: var HeadType, BodyType: var BodyType, TypeAnnotation: var Type }: {
				mAssert.IsFalse(aDefConstructor.EnvIds.ToStream().Any(__ => __ == HeadType.Id));
				aDefConstructor.Commands.Push(
					mIL_AST.TypeFree(HeadType.Pos, HeadType.Id)
				);
				
				if (!aDefConstructor.MapExpression(aModuleConstructor, BodyType).Match(out var BodyTypeReg, out var Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypeGeneric(Pos, aDefConstructor.CreateTempReg(out var ResultReg), HeadType.Id, BodyTypeReg)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tGenericApplyTypeNode<tPos> { Pos: var Pos, GenericType: var GenericType, ArgType: var ArgType, TypeAnnotation: var Type }: {
				if (!aDefConstructor.MapExpression(aModuleConstructor, GenericType).Match(out var GenericTypeReg, out var Error)) {
					return mResult.Fail(Error);
				}
				if (!aDefConstructor.MapExpression(aModuleConstructor, ArgType).Match(out var ArgTypeReg, out Error)) {
					return mResult.Fail(Error);
				}
				aDefConstructor.Commands.Push(
					mIL_AST.TypeGenericApply(Pos, aDefConstructor.CreateTempReg(out var ResultReg), GenericTypeReg, ArgTypeReg)
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
					$"in {nameof(mSPO2IL)}.{nameof(MapExpression)}(...)"
				);
			}
		}
	}
	
	internal static tBool
	MapIfCase<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		ref tDefConstructor<tPos> aTestAndCallCaseFunc,
		(mSPO_AST.tPatternNode<tPos> Match, mSPO_AST.tExpressionNode<tPos> Expression) aCase,
		mVM_Type.tType aArgType,
		tPos aCasePos,
		[MaybeNullWhen(false)]out (tPos, tText) aError
	) {
		if (
			!aTestAndCallCaseFunc.MapIfCaseRecursive(
				aModuleConstructor,
				aCase.Match,
				mIL_AST.cArg,
				aArgType,
				out aError
			) ||
			!aTestAndCallCaseFunc.MapExpression(aModuleConstructor, aCase.Expression).Match(out var Res, out aError)
		) {
			return false;
		}
		
		aTestAndCallCaseFunc.Commands.Push(
			[
				mIL_AST.AddPrefix(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var TemReg), "Result", Res),
				mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, TemReg),
			]
		);
		
		aError = default;
		return true;
	}
	
	internal static tBool
	MapIfCaseRecursive<tPos>(
		this ref tDefConstructor<tPos> aTestAndCallCaseFunc,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tPatternNode<tPos> aMatch,
		tText aInputReg,
		mVM_Type.tType aArgType,
		out (tPos, tText) aError
	) {
		switch (aMatch) {
			case mSPO_AST.tCharNode<tPos>:
			case mSPO_AST.tTuplePatternNode<tPos>: {
				throw mError.Error($"{aMatch.GetType().Name} should already be desugared");
			}
			case mSPO_AST.tFreeIdPatternNode<tPos> Node: {
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.Alias(Node.Pos, Node.Id, aInputReg)
				);
				
				aTestAndCallCaseFunc.AddLocal(Node.Id, aArgType);
				
				break;
			}
			case mSPO_AST.tIgnorePatternNode<tPos> Node: {
				break;
			}
			case mSPO_AST.tEmptyNode<tPos> Node: {
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.TryAsEmpty(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(), aInputReg)
				);
				
				break;
			}
			case mSPO_AST.tTrueNode<tPos> Node: {
				aTestAndCallCaseFunc.Commands.Push(
					[
						mIL_AST.TryAsBool(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var BoolReg), aInputReg),
						mIL_AST.XOr(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var NotReg), BoolReg, mIL_AST.cTrue),
						mIL_AST.ReturnIf(Node.Pos, NotReg, mIL_AST.cEmptyValue),
					]
				);
				break;
			}
			case mSPO_AST.tFalseNode<tPos> Node: {
				aTestAndCallCaseFunc.Commands.Push(
					[
						mIL_AST.TryAsBool(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var BoolReg), aInputReg),
						mIL_AST.ReturnIf(Node.Pos, BoolReg, mIL_AST.cEmptyValue),
					]
				);
				break;
			}
			case mSPO_AST.tIntNode<tPos> Node: {
				aTestAndCallCaseFunc.Commands.Push(
					[
						mIL_AST.TryAsInt(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var IntArg), aInputReg),
						mIL_AST.CreateInt(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var Int), "" + Node.Value),
						mIL_AST.IntsAreEq(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var Eq), IntArg, Int),
						mIL_AST.XOr(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var NotEq), Eq, mIL_AST.cTrue),
						mIL_AST.ReturnIf(Node.Pos, NotEq, mIL_AST.cEmptyValue),
					]
				);
				break;
			}
			case mSPO_AST.tPrefixPatternNode<tPos> Node: {
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.TryRemovePrefixFrom(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var InnerArg), aInputReg, Node.Prefix)
				);
				
				if (
					!aArgType.SubSet(
						_ => _.IsPrefix(Node.Prefix, out var T) ? mMaybe.Some(T) : mStd.cEmpty
					).IsSome(out var Type)
				) {
					if (
						Node.TypeAnnotation.AssertNotEmpty()
						.IsSubType(aArgType, mStd.cEmpty)
						.Match(out var T_, out var Error)
					) {
						throw mError.Error("Impossible");
					} else {
						aError = (Node.Pos, Error);
						return false;
					}
				}
				
				if (
					!aTestAndCallCaseFunc.MapIfCaseRecursive(
						aModuleConstructor,
						Node.Pattern,
						InnerArg,
						Type,
						out aError
					)
				) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tPairPatternNode<tPos> Node: {
				aTestAndCallCaseFunc.Commands.Push(
					[
						mIL_AST.TryAsPair(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var PairArgReg), aInputReg),
						mIL_AST.GetFirst(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var FirstReg), PairArgReg),
						mIL_AST.GetSecond(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var SecondReg), PairArgReg),
					]
				);
				
				var (T, NotT) = aArgType.SplitBy(__ => __.IsPair(out _, out _));
				
				if (
					!T.IsSome(out var T_)
				) {
					if (
						Node.TypeAnnotation.AssertNotEmpty()
						.IsSubType(aArgType, mStd.cEmpty)
						.Match(out _, out var Error)
					) {
						throw mError.Error("Impossible");
					} else {
						aError = (Node.Pos, Error);
						return false;
					}
				}
				
				if (!T_.IsPair(out var Type1, out var Type2)) {
					throw new System.NotImplementedException();
				}
				
				if (
					!aTestAndCallCaseFunc.MapIfCaseRecursive(
						aModuleConstructor,
						Node.Tail,
						FirstReg,
						Type1,
						out aError
					) ||
					!aTestAndCallCaseFunc.MapIfCaseRecursive(
						aModuleConstructor,
						Node.Head,
						SecondReg,
						Type2,
						out aError
					)
				) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tRecordPatternNode<tPos> Node: {
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.TryAsRecord(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var RecordReg), aInputReg)
				);
				
				var (T, NotT) = aArgType.SplitBy(__ => __.IsRecord(out _));
				
				if (!T.IsSome(out var RecordType)) {
					if (
						Node.TypeAnnotation.AssertNotEmpty()
						.IsSubType(aArgType, mStd.cEmpty)
						.Match(out _, out var Error)
					) {
						throw mError.Error("Impossible");
					} else {
						aError = (Node.Pos, Error);
						return false;
					}
				}
				
				foreach (var (IdNode, Pattern) in Node.Elements) {
					var FieldType = RecordType.GetFieldType(IdNode.Id);
					aTestAndCallCaseFunc.Commands.Push(
						mIL_AST.GetField(IdNode.Pos, aTestAndCallCaseFunc.CreateTempReg(out var FieldReg), RecordReg, IdNode.Id)
					);
					
					if (
						!aTestAndCallCaseFunc.MapIfCaseRecursive(
							aModuleConstructor,
							Pattern,
							FieldReg,
							FieldType,
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
					!aTestAndCallCaseFunc.MapIfCaseRecursive(
						aModuleConstructor,
						Node.Pattern,
						aInputReg,
						aArgType,
						out aError
					)
				) {
					return false;
				}
				
				if (
					!aTestAndCallCaseFunc.MapExpression(
						aModuleConstructor,
						Node.Guard
					).Match(out var TestReg, out aError)
				) {
					return false;
				}
				
				aTestAndCallCaseFunc.Commands.Push(
					[
						mIL_AST.XOr(Node.Pos, aTestAndCallCaseFunc.CreateTempReg(out var TestInvertReg), TestReg, mIL_AST.cTrue),
						mIL_AST.ReturnIf(Node.Pos, TestInvertReg, mIL_AST.cEmptyValue),
					]
				);
				
				break;
			}
			case mSPO_AST.tTypedPatternNode<tPos> Node: {
				if (Node.TypeExpression.IsSome(out var TypeExpr)) {
					aError = (TypeExpr.Pos, "TypeAnnotation not supported in §IF...Match... conditions");
					return false;
				}
				
				if (
					!aTestAndCallCaseFunc.MapIfCaseRecursive(
						aModuleConstructor,
						Node.Pattern,
						aInputReg,
						aArgType,
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
	
	public static tBool
	MapTypedPattern<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		ref tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tTypedPatternNode<tPos> aPatternNode,
		tText aRegId,
		out (tPos, tText) aError
	) => aDefConstructor.MapPattern(ref aModuleConstructor, aPatternNode.Pattern, aRegId, out aError);
	
	public static tBool
	MapPattern<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		ref tModuleConstructor<tPos> aModuleConstructor,
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
			case mSPO_AST.tPrefixPatternNode<tPos> { Pos: var Pos, Prefix: var Prefix, Pattern: var Pattern, TypeAnnotation: var Type }: {
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(Pos, aDefConstructor.CreateTempReg(out var ResultReg), Prefix, aRegId)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return aDefConstructor.MapPattern(ref aModuleConstructor, Pattern, ResultReg, out aError);
			}
			case mSPO_AST.tRecordPatternNode<tPos> { Elements: var Elements, TypeAnnotation: var TypeAnnotation }: {
				foreach (var (IdNode, Pattern) in Elements) {
					aDefConstructor.Commands.Push(mIL_AST.GetField(IdNode.Pos, aDefConstructor.CreateTempReg(out var FieldReg), aRegId, IdNode.Id));
					if (!aDefConstructor.MapPattern(ref aModuleConstructor, Pattern, FieldReg, out aError)) {
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
					if (!aDefConstructor.MapPattern(ref aModuleConstructor, Item, ItemReg, out aError)) {
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
				if (!aDefConstructor.MapPattern(ref aModuleConstructor, Head, HeadReg, out aError)) {
					return false;
				}
				
				aDefConstructor.Commands.Push(
					mIL_AST.GetFirst(Pos, aDefConstructor.CreateTempReg(out var TailReg), aRegId)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(TailReg, Tail.TypeAnnotation.AssertNotEmpty());
				if (!aDefConstructor.MapPattern(ref aModuleConstructor, Tail, TailReg, out aError)) {
					return false;
				}
				
				break;
			}
			case mSPO_AST.tGuardPatternNode<tPos> { Pattern: var Pattern, Guard: var Guard }: {
				if (
					!aDefConstructor.MapPattern(ref aModuleConstructor, Pattern, aRegId, out aError) ||
					!aDefConstructor.MapExpression(aModuleConstructor, Guard).Match(out var TestReg, out aError)
				) {
					return false;
				}
				
				aDefConstructor.Commands.Push(
					[
						mIL_AST.XOr(Guard.Pos, aDefConstructor.CreateTempReg(out var TestInvertReg), TestReg, mIL_AST.cTrue),
						mIL_AST.ReturnIf(Guard.Pos, TestInvertReg, mIL_AST.cEmptyValue),
					]
				);
				break;
			}
			case mSPO_AST.tTypedPatternNode<tPos> PatternNode: {
				return aDefConstructor.MapTypedPattern(ref aModuleConstructor, PatternNode, aRegId, out aError);
			}
			default: {
				throw mError.Error(
					$"not implemented: {nameof(mSPO_AST)}.{aPatternNode.GetType().Name} in {nameof(mSPO2IL)}.{nameof(MapTypedPattern)}(...)"
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
		aDefConstructor.MapExpression(aModuleConstructor, aDefNode.Src).Match(out var ValueReg, out aError) &&
		aDefConstructor.MapPattern(ref aModuleConstructor, aDefNode.Des, ValueReg, out aError)
	);
	
	public static tBool
	MapReturnIf<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tReturnIfNode<tPos> aReturnNode,
		out (tPos, tText) aError
	) {
		if (
			aDefConstructor.MapExpression(aModuleConstructor, aReturnNode.Condition).Match(out var CondReg, out aError) &&
			aDefConstructor.MapExpression(aModuleConstructor, aReturnNode.Result).Match(out var ResReg, out aError)
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
					_ => mSPO_AST_Types.ScopeItem(
						_,
						RecProcConstructor.TypeDict.TryGet(_).AssertNotEmpty()
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
				_ => mSPO_AST_Types.ScopeItem(
					_,
					RecFactoryFunc.TypeDict.TryGet(_).AssertNotEmpty()
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
		if (!aDefConstructor.MapExpression(aModuleConstructor, aDefVarNode.Expression).Match(out var Reg, out aError)) {
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
		
		if (!aDefConstructor.MapExpression(aModuleConstructor, aMethodCallsNode.Object).Match(out var Object, out aError)) {
			return false;
		}
		foreach (var Call in aMethodCallsNode.MethodCalls) {
			if (!aDefConstructor.MapExpression(aModuleConstructor, Call.Argument).Match(out var Arg, out aError)) {
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
				if (!aDefConstructor.MapPattern(ref aModuleConstructor, Result_, Result, out aError)) {
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
			_ => !_.StartsWith("d_")
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
