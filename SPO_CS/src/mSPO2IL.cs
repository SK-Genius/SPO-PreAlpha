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

using tScope = mStream.tStream<(System.String Id, mVM_Type.tType Type)>;

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
		mAssert.IsFalse(aDefConstructor.ArgIds.ToStream().Any(_ => _ == aId));
		mAssert.IsFalse(aDefConstructor.LocalIds.ToStream().Any(_ => _ == aId));
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
					ExtractEnv.Push(mIL_AST.GetSecond(aPos, Symbol, RestEnv));
					var NewRestEnv = aDefConstructor.CreateTempReg();
					ExtractEnv.Push(mIL_AST.GetFirst(aPos, NewRestEnv, RestEnv));
					RestEnv = NewRestEnv;
				}
				break;
			}
		}
		
		return ExtractEnv;
	}
	
	public static tText
	MapType<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		mVM_Type.tType aType
	) {
		switch (aType) {
			case var a when a.IsType(out _): {
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
			case var a when a.IsFree(out var Id_, out var Ref): {
				if (Ref.Kind == mVM_Type.tKind.Free) {
					aModuleConstructor.Types = aModuleConstructor.Types.Set(Id_, a);
					// TODO: aModuleConstructor.TypeDef.Push(...) ???
					return Id_;
				} else {
					return aModuleConstructor.MapType(Ref);
				}
			}
			case var a when a.IsPrefix(out var Prefix, out var Type): {
				var Id = aModuleConstructor.MapType(Type);
				var NewId = $"[#{Prefix}:{Id}]";
				aModuleConstructor.TypeDef.Push(mIL_AST.TypePrefix(default(tPos), NewId, Prefix, Id));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(NewId, a);
				return NewId;
			}
			case var a when a.IsRecord(out var Key, out var HeadType, out var TailType): {
				var IdHead = aModuleConstructor.MapType(HeadType);
				var IdTail = aModuleConstructor.MapType(TailType);
				
				var TempId = $"[#{Key}:{IdHead}]";
				aModuleConstructor.TypeDef.Push(mIL_AST.TypePrefix(default(tPos), TempId, Key, IdHead));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(TempId, mVM_Type.Prefix(Key, HeadType));
				
				var NewId = $"[{{{Key}:{IdHead};{IdTail}}}]";
				aModuleConstructor.TypeDef.Push(mIL_AST.TypeRecord(default(tPos), NewId, IdTail, TempId));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(NewId, a);
				return NewId;
			}
			case var a when a.IsPair(out var Type1, out var Type2): {
				var Id1 = aModuleConstructor.MapType(Type1);
				var Id2 = aModuleConstructor.MapType(Type2);
				var NewId = $"[{Id1};{Id2}]";
				aModuleConstructor.TypeDef.Push(mIL_AST.TypePair(default(tPos), NewId, Id1, Id2));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(NewId, a);
				return NewId;
			}
			case var a when a.IsSet(out var Type1, out var Type2): {
				var Id1 = aModuleConstructor.MapType(Type1);
				var Id2 = aModuleConstructor.MapType(Type2);
				var NewId = $"[{Id1}|{Id2}]";
				aModuleConstructor.TypeDef.Push(mIL_AST.TypeSet(default(tPos), NewId, Id1, Id2));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(NewId, a);
				return NewId;
			}
			case var a when a.IsProc(out var EnvType, out var ArgType, out var ResType): {
				var IdArg = aModuleConstructor.MapType(ArgType);
				var IdRes = aModuleConstructor.MapType(ResType);
				var IdFunc = $"[{IdArg}->{IdRes}]";
				aModuleConstructor.TypeDef.Push(mIL_AST.TypeFunc(default(tPos), IdFunc, IdArg, IdRes));
				
				if (EnvType.IsEmpty()) {
					aModuleConstructor.Types = aModuleConstructor.Types.Set(IdFunc, a);
					return IdFunc;
				}
				
				var IdEnv = aModuleConstructor.MapType(EnvType);
				var IdEnvFunc = $"[{IdEnv}:{IdFunc}]"; 
				aModuleConstructor.TypeDef.Push(mIL_AST.TypeMethod(default(tPos), IdEnvFunc, IdEnv, IdFunc));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(IdEnvFunc, a);
				return IdEnvFunc;
			}
			case var a when a.IsVar(out var InnerType): {
				var InnerId = aModuleConstructor.MapType(InnerType);
				var NewId = $"[§VAR {InnerId}]";
				
				aModuleConstructor.TypeDef.Push(mIL_AST.TypeVar(default(tPos), NewId, InnerId));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(NewId, a);				
				
				return NewId;
			}
			default: {
				throw new System.NotImplementedException("" + aType.Kind);
			}
		}
	}
	
	public static mVM_Type.tType
	CreateEnvType<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor
	) {
		var TypeDict = aDefConstructor.TypeDict;
		var EnvIds = aDefConstructor.EnvIds;
		
		return EnvIds.ToStream(
		).Reduce(
			mStream.Stream<mVM_Type.tType>([]),
			(aRes, aEnvId) => mStream.Stream(TypeDict.TryGet(aEnvId).AssertNotEmpty(), aRes)
		).Match(
			(aHead, aTail) => mVM_Type.Tuple(mStream.Stream(aHead, aTail).Reverse()),
			() => mVM_Type.Tuple(
				EnvIds.ToStream(
				).Map(
					_ => {
						if (!_.StartsWith("d_")) {
							throw new System.Exception();
						}
						var TypeName = aModuleConstructor.Defs.Get(tNat32.Parse(_[1..])).TypeId;
						return aModuleConstructor.Types.TryGet(
							TypeName
						).AssertNotEmpty(
							() => $"can't find type '{TypeName}'"
						);
					}
				)
			)
		);
	}
	
	public static mVM_Type.tType
	CreateDefType<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mVM_Type.tType aProcType
	) {
		var Type = mVM_Type.Proc(
			mVM_Type.Empty(),
			aDefConstructor.CreateEnvType(
				aModuleConstructor
			),
			aProcType
		);
		
		return Type;
	}
	
	public static (tNat32 DefIndex, mVM_Type.tType DefType)
	MapLambda<tPos>(
		// maps argument and body but not the environment, this is done in FinishMapProc(...)
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	) {
		aDefConstructor.MapMatch(aLambdaNode.Head, mIL_AST.cArg, aLambdaNode.Head.TypeAnnotation.AssertNotEmpty());
		
		var ResultReg = aDefConstructor.MapExpression(aModuleConstructor, aLambdaNode.Body);
		if (aLambdaNode.Body is not mSPO_AST.tBlockNode<tPos>) {
			aDefConstructor.Commands.Push(
				mIL_AST.ReturnIf(aLambdaNode.Body.Pos, mIL_AST.cTrue, ResultReg)
			);
		}
		
		var Type = aDefConstructor.CreateDefType(
			aModuleConstructor,
			aLambdaNode.TypeAnnotation.AssertNotEmpty()
		);
		
		var DefIndex = aDefConstructor.FinishMapProc(
			aLambdaNode.Pos,
			aModuleConstructor,
			Type
		);
		
		return (DefIndex, Type);
	}
	
	public static tNat32
	MapMethod<tPos>(
		// maps argument, object and body but not the environment, this is done in FinishMapProc(...)
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		aDefConstructor.MapMatch(aMethodNode.Arg, mIL_AST.cArg, aMethodNode.Arg.TypeAnnotation.AssertNotEmpty());
		aDefConstructor.MapMatch(aMethodNode.Obj, mIL_AST.cObj, aMethodNode.Obj.TypeAnnotation.AssertNotEmpty());
		
		var ResultReg = aDefConstructor.MapExpression(aModuleConstructor, aMethodNode.Body);
		aDefConstructor.Commands.Push(
			mIL_AST.ReturnIf(aMethodNode.Pos, mIL_AST.cTrue, ResultReg)
		);
		
		return aDefConstructor.FinishMapProc(
			aMethodNode.Pos,
			aModuleConstructor,
			aMethodNode.TypeAnnotation.AssertNotEmpty()
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
		
		return aModuleConstructor.Defs.Size() - 1;
	}
	
	public static (tNat32 Index, tScope EnvList)
	MapMethod<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		var TempMethodDef = NewDefConstructor<tPos>();
		var DefIndex = TempMethodDef.MapMethod(aModuleConstructor, aMethodNode);
		var EnvList = TempMethodDef.EnvIds.ToStream(
		).Map(
			_ => (
				_,
				TempMethodDef.TypeDict.TryGet(_).AssertNotEmpty()
			)
		);
		
		return (DefIndex, EnvList);
	}
	
	public static tText
	InitProc<tPos>(
		this ref tDefConstructor<tPos> aCallerDefConstructor,
		tPos aPos,
		tNat32 aDefIndex,
		mVM_Type.tType aDefType,
		tScope aEnvList,
		tBool aIsRecursiveFactory = false
	) {
		mAssert.IsTrue(aDefType.IsProc(out _, out _, out var FuncType));
		//mAssert.IsTrue(FuncType.IsProc(out _, out _, out _));
		
		var EnvReg = mIL_AST.cEmpty;
		if (!aEnvList.IsEmpty()) {
			foreach (var (EnvId, EnvType) in aEnvList) {
				if (aCallerDefConstructor.TypeDict.TryGet(EnvId).IsNone()) {
					aCallerDefConstructor.AddEnv(EnvId, EnvType);
				}
			}
			
			if (aEnvList.Count() is 1) {
				EnvReg = aEnvList.TryFirst().AssertNotEmpty().Id;
			} else {
				foreach (var (EnvId_, _) in aEnvList) {
					var NewArgReg = aCallerDefConstructor.CreateTempReg();
					aCallerDefConstructor.Commands.Push(mIL_AST.CreatePair(aPos, NewArgReg, EnvReg, EnvId_));
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
	
	public static tText
	MapExpression<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tExpressionNode<tPos> aExpressionNode
	) {
		var TypeDict = aDefConstructor.TypeDict;
		
		switch (aExpressionNode) {
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
				return mIL_AST.cEmpty;
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
			case mSPO_AST.tTypeTypeNode<tPos> TypeTypeNode: {
				return mIL_AST.cTypeType;
			}
			case mSPO_AST.tIntNode<tPos> { Pos: var Pos, Value: var Value }: {
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.AddLocal(ResultReg, mVM_Type.Int());
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(
						Pos,
						ResultReg,
						"" + Value
					)
				);
				return ResultReg;
			}
			case mSPO_AST.tIdNode<tPos> { Pos: var Pos, Id: var Id, TypeAnnotation: var Type }: {
				if (
					!aDefConstructor.TypeDict.ToStream().Any(_ => _.Key == Id) &&
					!aDefConstructor.Commands.ToStream(
					).Any(
						_ => _.GetResultReg().Match(
							aName => aName == Id,
							() => false
						)
					)
				) {
					aDefConstructor.AddEnv(Id, Type.AssertNotEmpty()); // TODO
				}
				return Id;
			}
			case mSPO_AST.tCallNode<tPos> { Pos: var Pos, Func: var Func, Arg: var Arg, TypeAnnotation: var Type }: {
				var FuncReg = aDefConstructor.MapExpression(aModuleConstructor, Func);
				var ArgReg = aDefConstructor.MapExpression(aModuleConstructor, Arg);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				aDefConstructor.Commands.Push(
					mIL_AST.CallFunc(Pos, ResultReg, FuncReg, ArgReg)
				);
				return ResultReg;
			}
			case mSPO_AST.tTupleNode<tPos> { Items: var Items, TypeAnnotation: var Type }: {
				switch (Items.Take(2).ToArrayList().Size()) {
					case 0: {
						throw mError.Error("impossible");
					}
					case 1: {
						mAssert.IsTrue(Items.Is(out var Head, out var _));
						return aDefConstructor.MapExpression(aModuleConstructor, Head);
					}
					default: {
						mAssert.IsTrue(Items.Is(out var Head, out var _));
						var TailReg = mIL_AST.cEmpty;
						foreach (var Item in Items) {
							var HeadReg = aDefConstructor.MapExpression(aModuleConstructor, Item);
							var TupleReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CreatePair(
									aExpressionNode.Pos,
									TupleReg,
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
			case mSPO_AST.tPrefixNode<tPos> { Pos: var Pos, Prefix: var Prefix, Element: var Element, TypeAnnotation: var Type }: {
				var ExpressionReg = aDefConstructor.MapExpression(aModuleConstructor, Element);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				aDefConstructor.Commands.Push(
					mIL_AST.AddPrefix(
						Pos,
						ResultReg,
						Prefix,
						ExpressionReg
					)
				);
				return ResultReg;
			}
			case mSPO_AST.tRecordNode<tPos> { Elements: var Elements, TypeAnnotation: var Type }: {
				var ResultReg = mIL_AST.cEmpty;
				foreach (var (Key, Value) in Elements) {
					var Expression = aDefConstructor.MapExpression(aModuleConstructor, Value);
					var PrefixReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.AddPrefix(Key.Pos, PrefixReg, Key.Id, Expression));
					var NewResultReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.ExtendRec(Key.Pos, NewResultReg, ResultReg, PrefixReg));
					ResultReg = NewResultReg;
				}
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tTextNode<tPos> { Pos: var Pos, Value: var Value }: {
				var TailReg = mIL_AST.cEmpty;
				var Index = Value.Length;
				while (Index --> 0) {
					var Char = Value[Index];
					var CharOrdReg = aDefConstructor.CreateTempReg();
					var HeadReg = aDefConstructor.CreateTempReg();
					var TextReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						[
							mIL_AST.CreateInt(Pos, CharOrdReg, ((tInt32)Char).ToString()),
							mIL_AST.AddPrefix(Pos, HeadReg, "Char", CharOrdReg),
							mIL_AST.CreatePair(Pos, TextReg, TailReg, HeadReg)
						]
					);
					TailReg = TextReg;
				}
				return TailReg;
			}
			case mSPO_AST.tLambdaNode<tPos> LambdaNode: {
				var LambdaDef = NewDefConstructor<tPos>();
				
				var (LambdaDefId, LambdaDefType) = LambdaDef.MapLambda(
					aModuleConstructor,
					LambdaNode
				);
				
				var LambdaEnvs = LambdaDef.EnvIds.ToStream(
				).Map(
					_ => (
						Id: _,
						Type: LambdaDef.TypeDict.TryGet(_).AssertNotEmpty()
					)
				);
				
				foreach (var (EnvId, EnvType) in LambdaEnvs) {
					if (
						!aDefConstructor.LocalIds.ToStream().Any(_ => _ == EnvId) &&
						!aDefConstructor.ArgIds.ToStream().Any(_ => _ == EnvId)
					) {
						aDefConstructor.AddEnv(EnvId, EnvType);
					}
				}
				
				return aDefConstructor.InitProc(
					LambdaNode.Pos,
					LambdaDefId,
					LambdaDefType,
					LambdaEnvs
				);
			}
			case mSPO_AST.tMethodNode<tPos> MethodNode: {
				var (NewDefIndex, EnvList) = aModuleConstructor.MapMethod(
					MethodNode
				);
				
				return aDefConstructor.InitProc(
					MethodNode.Pos,
					NewDefIndex,
					MethodNode.TypeAnnotation.AssertNotEmpty(),
					EnvList
				);
			}
			case mSPO_AST.tBlockNode<tPos> { Commands: var Commands }: {
				foreach (var Command in Commands) {
					aDefConstructor.MapCommand(aModuleConstructor, Command);
				}
				// TODO: remove created symbols from unknown symbols
				return mIL_AST.cEmpty;
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
					mSPO_AST.Match(
						Pos,
						new mSPO_AST.tMatchFreeIdNode<tPos> { Id = ResultReg },
						mStd.cEmpty
					),
					mSPO_AST.Call(
						Pos,
						mSPO_AST.Lambda(
							Pos,
							mStd.cEmpty,
							mSPO_AST.Match(
								Pos,
								mSPO_AST.Empty(Pos),
								mStd.cEmpty
							),
							mSPO_AST.Block(Pos, Ifs.ToStream())
						),
						mSPO_AST.Empty(Pos)
					)
				);
				
				mSPO_AST_Types.UpdateCommandTypes(Def, mStd.cEmpty).ElseThrow();
				
				aDefConstructor.MapCommand(
					aModuleConstructor,
					Def
				);
				
				return ResultReg;
			}
			case mSPO_AST.tIfMatchNode<tPos> { Pos: var Pos, Expression: var MatchExpression, Cases: var Cases, TypeAnnotation: var Type }: {
				var InputReg = aDefConstructor.MapExpression(aModuleConstructor, MatchExpression);
				
				var SwitchDef = NewDefConstructor<tPos>();
				
				var TestType = mVM_Type.Proc(
					mVM_Type.Empty(),
					MatchExpression.TypeAnnotation.AssertNotEmpty(),
					mVM_Type.Bool()
				);
				
				var CaseType = mVM_Type.Proc(
					mVM_Type.Empty(),
					MatchExpression.TypeAnnotation.AssertNotEmpty(),
					aExpressionNode.TypeAnnotation.AssertNotEmpty()
				);
					
				// TODO: add gard command for the case that no case matched the argument
				
				foreach (var Case in Cases) {
					var CasePos = aModuleConstructor.MergePos(Case.Match.Pos, Case.Expression.Pos);
					
					var TestAndCallCaseFunc = NewDefConstructor<tPos>();
					
					var CaseDefType = TestAndCallCaseFunc.CreateDefType(
						aModuleConstructor,
						CaseType
					);
					
					aModuleConstructor.MapIfCase(
						ref TestAndCallCaseFunc,
						ref SwitchDef,
						Case,
						CaseDefType,
						CasePos
					);
					
					var TestAndCallDefIndex = TestAndCallCaseFunc.FinishMapProc(
						Pos,
						aModuleConstructor,
						CaseDefType
					);
					
					var TestDefType = mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Empty(), // TODO: add env type
						TestType
					);
					
					var TypeDict_ = TestAndCallCaseFunc.TypeDict;
					var ProcId = SwitchDef.InitProc(
						Pos,
						TestAndCallDefIndex,
						TestDefType,
						TestAndCallCaseFunc.EnvIds.ToStream().Map(_ => (_, TypeDict_.TryGet(_).AssertNotEmpty()))
					);
					
					SwitchDef.Commands.Push(
						[
							mIL_AST.CallFunc(CasePos, SwitchDef.CreateTempReg(out var Res_), ProcId, mIL_AST.cArg),
							mIL_AST.ReturnIfNotEmpty(CasePos, Res_)
						]
					);
				}
				
				SwitchDef.Commands.Push(
					mIL_AST.ReturnIf(Pos, mIL_AST.cTrue, mIL_AST.cEmpty)
				);
				// TODO NOW: put expression and else/remaining cases as args into the case test
				
				// §DEF MyResult = §IF MyMaybeIntValue MATCH {
				//   §DEF MyIntValue € §INT => MyIntValue .* 2
				//   () => 0
				// }
				//
				// §DEF MyResult = a € [§INT | []] => {
				//   §RETURN .(a_ € §INT => a_ .* 2) a IF_ARG_IS_INT
				//   §RETURN .(a_ € [] => 0) a If_ARG_IS_EMPTY
				// }. MyIntValue
				
				
				var SwitchDefType = SwitchDef.CreateDefType(
					aModuleConstructor,
					CaseType
				);
				
				var SwitchDefId = GetDefId(aModuleConstructor.Defs.Size());
				
				var DefIndex = SwitchDef.FinishMapProc(aExpressionNode.Pos, aModuleConstructor, SwitchDefType);
				var SwitchProc = aDefConstructor.InitProc(
					aExpressionNode.Pos,
					DefIndex,
					SwitchDefType,
					SwitchDef.EnvIds.ToStream().Map(_ => (_, SwitchDef.TypeDict.TryGet(_).AssertNotEmpty()))
				);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.CallFunc(Pos, ResultReg, SwitchProc, InputReg));
				
				aDefConstructor.AddLocal(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tVarToValNode<tPos> { Pos: var Pos, Obj: var Obj, TypeAnnotation: var Type }: {
				var ObjReg = aDefConstructor.MapExpression(aModuleConstructor, Obj);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.VarGet(Pos, ResultReg, ObjReg));
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> { Pos: var Pos, HeadType: var HeadType, BodyType: var BodyType, TypeAnnotation: var Type }: {
				mAssert.IsFalse(aDefConstructor.EnvIds.ToStream().Any(_ => _ == HeadType.Id));
				aDefConstructor.Commands.Push(
					mIL_AST.TypeFree(HeadType.Pos, HeadType.Id)
				);
				var BodyTypeReg = aDefConstructor.MapExpression(aModuleConstructor, BodyType);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypeRecursive(
						Pos,
						ResultReg,
						HeadType.Id,
						BodyTypeReg
					)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tSetTypeNode<tPos> { Expressions: var Expressions, TypeAnnotation: var Type }: {
				mAssert.IsTrue(Expressions.Is(out var Head, out var Tail));
				var ResultReg = aDefConstructor.MapExpression(aModuleConstructor, Head);
				foreach (var Expression in Tail) {
					var ExprReg = aDefConstructor.MapExpression(aModuleConstructor, Expression);
					var SetTypeReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.TypeSet(Expression.Pos, SetTypeReg, ExprReg, ResultReg));
					ResultReg = SetTypeReg;
				}
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tTupleTypeNode<tPos> { Expressions: var Expressions, TypeAnnotation: var Type }: {
				if (!Expressions.Is(out var First, out var Rest)) {
					return mIL_AST.cEmptyType;
				} else {
					var Pos = First.Pos;
					var ResultReg = aDefConstructor.MapExpression(aModuleConstructor, First);
					foreach (var Head in Rest) {
						var PairTypeReg = aDefConstructor.CreateTempReg();
						var HeadReg = aDefConstructor.MapExpression(aModuleConstructor, Head);
						Pos = aModuleConstructor.MergePos(Pos, Head.Pos);
						aDefConstructor.Commands.Push(mIL_AST.TypePair(Pos, PairTypeReg, ResultReg, HeadReg));
						ResultReg = PairTypeReg;
					}
					aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
					return ResultReg;
				}
			}
			case mSPO_AST.tPrefixTypeNode<tPos> { Pos: var Pos, Prefix: var Prefix, Expressions: var Expressions, TypeAnnotation: var Type }: {
				var InnerType = aDefConstructor.MapExpression(
					aModuleConstructor,
					mSPO_AST.TupleType(Pos, Expressions)
				);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypePrefix(Pos, ResultReg, Prefix.Id, InnerType)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				return ResultReg;
			}
			case mSPO_AST.tPipeToRightNode<tPos> { Pos: var Pos, Left: var Left, Right: var Right, TypeAnnotation: var Type }: {
				switch (Right) {
					case mSPO_AST.tPipeToRightNode<tPos> { Left: var RightLeft, Right: var RightRight }: {
						return aDefConstructor.MapExpression(
							aModuleConstructor,
							mSPO_AST.PipeToRight(
								Pos,
								mSPO_AST.PipeToRight(
									Left.Pos, //TODO: mStd.Merge(PipeToRightNode.Left.Pos, PipeToRightNode_.Left.Pos),
									Left,
									RightLeft
								),
								Right
							)
						);
					}
					case mSPO_AST.tCallNode<tPos> { Pos: var Pos_, Func: var Func_, Arg: var Arg_ }: {
						var Func = (
							Func_ is mSPO_AST.tIdNode<tPos> IdNode
							? mSPO_AST.Id(IdNode.Pos, "..." + IdNode.Id[1..])
							: Func_
						);
						var FuncReg = aDefConstructor.MapExpression(aModuleConstructor, Func);
						var Arg = (
							Arg_ is mSPO_AST.tTupleNode<tPos> Tuple
							? mSPO_AST.Tuple(
								Tuple.Pos,
								mStream.Stream(Right, Tuple.Items)
							)
							: mSPO_AST.Tuple(
								Arg_.Pos,
								mStream.Stream(Right, mStream.Stream([Arg_]))
							)
						);
						var ArgReg = aDefConstructor.MapExpression(aModuleConstructor, Arg);
						var ResultReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(
							mIL_AST.CallFunc(Pos_, ResultReg, FuncReg, ArgReg)
						);
						aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
						return ResultReg;
					}
					default: {
						var FirstArgReg = aDefConstructor.MapExpression(aModuleConstructor, Left);
						var FuncReg = aDefConstructor.MapExpression(aModuleConstructor, Right);
						var ResultReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(
							mIL_AST.CallFunc(Pos, ResultReg, FuncReg, FirstArgReg)
						);
						aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
						return ResultReg;
					}
				}
			}
			case mSPO_AST.tPipeToLeftNode<tPos> { Pos: var Pos, Left: var Left, Right: var Right }: {
				switch (Left) {
					case mSPO_AST.tCallNode<tPos> { Pos: var LeftPos, Func: var LeftFunc, Arg: var LeftArg }: {
						var Func = (
							(LeftFunc is mSPO_AST.tIdNode<tPos> IdNode)
							? mSPO_AST.Id(IdNode.Pos, IdNode.Id[1..] + "...")
							: LeftFunc
						);
						var FuncReg = aDefConstructor.MapExpression(aModuleConstructor, Func);
						var Arg = (
							LeftArg is mSPO_AST.tTupleNode<tPos> Tuple
							? mSPO_AST.Tuple(
								Tuple.Pos,
								mStream.Concat(Tuple.Items, mStream.Stream([Right]))
							)
							: mSPO_AST.Tuple(
								LeftArg.Pos,
								mStream.Stream(LeftArg, mStream.Stream([Right]))
							)
						);
						var ArgReg = aDefConstructor.MapExpression(aModuleConstructor, Arg);
						var ResultReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(
							mIL_AST.CallFunc(LeftPos, ResultReg, FuncReg, ArgReg)
						);
						return ResultReg;
					}
					default: {
						var FirstArgReg = aDefConstructor.MapExpression(aModuleConstructor, Right);
						var FuncReg = aDefConstructor.MapExpression(aModuleConstructor, Left);
						var ResultReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(
							mIL_AST.CallFunc(Pos, ResultReg, FuncReg, FirstArgReg)
						);
						return ResultReg;
					}
				}
			}
			default: {
				throw mError.Error(
					$"not implemented: case {nameof(mSPO_AST)}.{aExpressionNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapExpression)}(...)"
				);
			}
		}
	}
	
	internal static void
	MapIfCase<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		ref tDefConstructor<tPos> aTestAndCallCaseFunc,
		ref tDefConstructor<tPos> aSwitchDef,
		(mSPO_AST.tMatchNode<tPos> Match, mSPO_AST.tExpressionNode<tPos> Expression) aCase,
		mVM_Type.tType aCaseType,
		tPos aCasePos
	) {
		switch (aCase.Match.Pattern) {
			case mSPO_AST.tIgnoreMatchNode<tPos> p: {
				var Res__ = aTestAndCallCaseFunc.MapExpression(aModuleConstructor, aCase.Expression);
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res__)
				);
				break;
			}
			case mSPO_AST.tMatchFreeIdNode<tPos> p: {
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.Alias(aCasePos, p.Id, mIL_AST.cArg)
				);
				aTestAndCallCaseFunc.AddLocal(p.Id, aCaseType);
				
				var Res__ = aTestAndCallCaseFunc.MapExpression(aModuleConstructor, aCase.Expression);
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res__)
				);
				break;
			}
			case mSPO_AST.tEmptyTypeNode<tPos> p: {
				throw new System.NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tBoolTypeNode<tPos> p: {
				var LazyCaseDef = NewDefConstructor<tPos>();
				
				LazyCaseDef.MapExpression(aModuleConstructor, aCase.Expression);
				
				var DefType = LazyCaseDef.CreateDefType(
					aModuleConstructor,
					mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Bool(),
						aCaseType
					)
				);
				
				var DefIndex = LazyCaseDef.FinishMapProc(aCasePos, aModuleConstructor, DefType);
				var LazyCaseDefId = aTestAndCallCaseFunc.InitProc(
					p.Pos,
					DefIndex,
					DefType,
					LazyCaseDef.EnvIds.ToStream(
					).Map(
						_ => (
							_,
							LazyCaseDef.TypeDict.TryGet(_).AssertNotEmpty()
						)
					)
				);
				
				var BoolArg = aSwitchDef.CreateTempReg();
				var Res = aSwitchDef.CreateTempReg();
				
				aTestAndCallCaseFunc.Commands.Push(
					[
						mIL_AST.TryAsBool(aCasePos, BoolArg, mIL_AST.cArg),
						mIL_AST.CallFunc(aCasePos, Res, LazyCaseDefId, BoolArg),
						mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res)
					]
				);
				break;
			}
			case mSPO_AST.tTrueNode<tPos> p: {
				throw new System.NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tFalseNode<tPos> p: {
				throw new System.NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tIntTypeNode<tPos> p: {
				throw new System.NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tIntNode<tPos> p: {
				var Type = mVM_Type.Proc(
					mVM_Type.Empty(),
					mVM_Type.Int(),
					aCase.Match.TypeAnnotation.AssertNotEmpty()
				);
				
				var LazyCaseDef = NewDefConstructor<tPos>();
				
				// TODO: map pattern as arg
				var Res = LazyCaseDef.MapExpression(aModuleConstructor, aCase.Expression);
				
				LazyCaseDef.Commands.Push(
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res)
				);
				
				var DefType =  LazyCaseDef.CreateDefType(
					aModuleConstructor,
					aCaseType
				);
				
				var DefIndex = LazyCaseDef.FinishMapProc(aCasePos, aModuleConstructor, DefType);
				
				var LazyCaseDefId = aTestAndCallCaseFunc.InitProc(
					p.Pos,
					DefIndex,
					DefType,
					LazyCaseDef.EnvIds.ToStream(
					).Map(
						_ => (
							_,
							LazyCaseDef.TypeDict.TryGet(_).AssertNotEmpty()
						)
					)
				);
				
				aTestAndCallCaseFunc.Commands.Push(
					[
						mIL_AST.TryAsInt(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var IntArg), mIL_AST.cArg),
						mIL_AST.CreateInt(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var Int), "" + p.Value),
						mIL_AST.IntsAreEq(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var Eq), IntArg, Int),
						mIL_AST.XOr(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var NotEq), Eq, mIL_AST.cTrue),
						mIL_AST.ReturnIf(aCasePos, NotEq, mIL_AST.cEmpty),
						mIL_AST.CallFunc(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var Res__), LazyCaseDefId, IntArg),
						mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res__)
					]
				);
				break;
			}
			case mSPO_AST.tTupleTypeNode<tPos> p: {
				throw new System.NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tMatchPrefixNode<tPos> p: {
				//mAssert.Fail("TODO");
				var LazyCaseDef = NewDefConstructor<tPos>();
				
				var Res = LazyCaseDef.MapExpression(aModuleConstructor, aCase.Expression);
				LazyCaseDef.Commands.Push(
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res)
				);
				
				var DefType = LazyCaseDef.CreateDefType(
					aModuleConstructor,
					mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Prefix(p.Prefix, p.Match.TypeAnnotation.AssertNotEmpty()),
						aCase.Match.TypeAnnotation.AssertNotEmpty()
					)
				);
				
				var DefIndex = LazyCaseDef.FinishMapProc(aCasePos, aModuleConstructor, DefType);
				
				var LazyCaseDefId = aTestAndCallCaseFunc.InitProc(
					p.Pos,
					DefIndex,
					DefType,
					LazyCaseDef.EnvIds.ToStream(
					).Map(
						_ => (
							_,
							LazyCaseDef.TypeDict.TryGet(_).AssertNotEmpty()
						)
					)
				);
				
				aTestAndCallCaseFunc.Commands.Push(
					[
						mIL_AST.TryRemovePrefixFrom(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var InnerArg), mIL_AST.cArg, p.Prefix),
						mIL_AST.CallFunc(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var Res__), LazyCaseDefId, InnerArg),
						mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res__)
					]
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> p: {
				var LazyCaseDef = NewDefConstructor<tPos>();
				
				//mAssert.Fail("TODO");
				// TODO NOW: get matches from §ARG
				
				var Res = LazyCaseDef.MapExpression(aModuleConstructor, aCase.Expression);
				LazyCaseDef.Commands.Push(
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res)
				);
				
				var DefType = LazyCaseDef.CreateDefType(
					aModuleConstructor,
					mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Tuple(p.Items.Map(_ => _.TypeAnnotation.AssertNotEmpty())),
						aCase.Expression.TypeAnnotation.AssertNotEmpty()
					)
				);
				
				var DefIndex = LazyCaseDef.FinishMapProc(aCasePos, aModuleConstructor, DefType);
				
				var LazyCaseDefId = aTestAndCallCaseFunc.InitProc(
					p.Pos,
					DefIndex,
					DefType,
					LazyCaseDef.EnvIds.ToStream(
					).Map(
						_ => (
							_,
							LazyCaseDef.TypeDict.TryGet(_).AssertNotEmpty()
						)
					)
				);
				
				aTestAndCallCaseFunc.Commands.Push(
					[
						mIL_AST.CallFunc(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var Res__), LazyCaseDefId, mIL_AST.cArg),
						mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res__)
					]
				);
				break;
			}
			case mSPO_AST.tMatchNode<tPos> p: {
				throw new System.NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tMatchGuardNode<tPos> p: {
				throw new System.NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			default: {
				throw new System.NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
		}
	}
	
	public static void
	MapMatch<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMatchNode<tPos> aMatchNode,
		tText aRegId,
		mVM_Type.tType aRegType
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.TypeExpression;
		
		switch (PatternNode) {
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
				// TODO: check left side
				break;
			}
			case mSPO_AST.tIntNode<tPos> IntNode: {
				// TODO: check left side
				break;
			}
			case mSPO_AST.tMatchFreeIdNode<tPos> { Pos: var Pos, Id: var Name, TypeAnnotation: var Type }: {
				mAssert.AreNotEquals(Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(aMatchNode.Pos, Name, aRegId));
				aDefConstructor.AddArg(Name, Type.AssertNotEmpty());
				break;
			}
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatchNode: {
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> { Pos: var Pos, Prefix: var Prefix, Match: var Match, TypeAnnotation: var Type }: {
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(
						Pos,
						ResultReg,
						Prefix,
						aRegId
					)
				);
				aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ResultReg, Type.AssertNotEmpty());
				mAssert.IsTrue(aRegType.IsPrefix(Prefix, out var ResType));
				
				aDefConstructor.MapMatch(
					Match,
					ResultReg,
					ResType
				);
				break;
			}
			case mSPO_AST.tMatchRecordNode<tPos> { Elements: var Elements, TypeAnnotation: var TypeAnnotation }: {
				var Type = aRegType;
				foreach (var (IdNode, Match) in Elements) {
					var Pos = IdNode.Pos;
					
					var Reg = aRegId;
					var Found = false;
					var RecType = Type;
					while (RecType.IsRecord(out var HeadId, out var HeadType, out RecType!)) {
						var HeadTailReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(mIL_AST.DivideRec(Pos, HeadTailReg, Reg));
						if (HeadId == IdNode.Id) {
							aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(Reg, TypeAnnotation.AssertNotEmpty());
							var TempValueReg_ = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(mIL_AST.GetSecond(Pos, TempValueReg_, HeadTailReg));
							var TempValueReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(mIL_AST.SubPrefix(Pos, TempValueReg, IdNode.Id, TempValueReg_));
							aDefConstructor.MapMatch(Match, TempValueReg, RecType);
							Found = true;
							break;
						} else {
							Reg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(mIL_AST.GetFirst(Pos, Reg, HeadTailReg));
						}
					}
					if (!Found) {
						throw mError.Error($"{Pos} ERROR: can't match type '{TypeAnnotation}'");
					}
				}
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> { Items: var Items }: {
				var RemainingReg = aRegId;
				var RemainingTypes = aRegType;
				mAssert.AreEquals(Items.Take(2).ToArrayList().Size(), 2u);
				foreach (var Item in Items.Reverse()) {
					mAssert.IsTrue(RemainingTypes.IsPair(out RemainingTypes, out var ItemType));
					var ItemReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetSecond(PatternNode.Pos, ItemReg, RemainingReg));
					aDefConstructor.TypeDict = aDefConstructor.TypeDict.Set(ItemReg, Item.TypeAnnotation.AssertNotEmpty());
					aDefConstructor.MapMatch(Item, ItemReg, ItemType);
					
					var NewRestReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetFirst(PatternNode.Pos, NewRestReg, RemainingReg));
					RemainingReg = NewRestReg;
				}
				mAssert.IsTrue(RemainingTypes.IsEmpty());
				break;
			}
			case mSPO_AST.tMatchGuardNode<tPos> { Match: var Match, Guard: var Guard }: {
				// TODO: ASSERT Guard
				aDefConstructor.MapMatch(Match, aRegId, aRegType);
				break;
			}
			case mSPO_AST.tMatchNode<tPos> MatchNode: {
				if (TypeNode.IsSome(out var TypeNode_)) {
					if (MatchNode.TypeExpression.IsSome(out var MatchTypeNode)) {
						throw mError.Error("not implemented"); //TODO: Unify MatchTypeNode & TypeNode_
					}
					
					aDefConstructor.MapMatch(
						mSPO_AST.Match(aMatchNode.Pos, MatchNode.Pattern, TypeNode),
						aRegId,
						aRegType
					);
				} else {
					aDefConstructor.MapMatch(MatchNode, aRegId, aRegType);
				}
				break;
			}
			default: {
				throw mError.Error(
					$"not implemented: {nameof(mSPO_AST)}.{PatternNode.GetType().Name} in {nameof(mSPO2IL)}.{nameof(MapMatch)}(...)"
				);
			}
		}
	}
	
	public static void
	MapDef<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tDefNode<tPos> aDefNode
	) {
		var ValueReg = aDefConstructor.MapExpression(aModuleConstructor, aDefNode.Src);
		aDefConstructor.MapMatch(aDefNode.Des, ValueReg, aDefNode.Src.TypeAnnotation.AssertNotEmpty());
	}
	
	public static void
	MapReturnIf<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tReturnIfNode<tPos> aReturnNode
	) {
		var CondReg = aDefConstructor.MapExpression(aModuleConstructor, aReturnNode.Condition);
		var ResReg = aDefConstructor.MapExpression(aModuleConstructor, aReturnNode.Result);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf(aReturnNode.Pos, CondReg, ResReg));
	}
	
	public static void
	MapRecursiveLambdas<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tRecLambdasNode<tPos> aRecLambdasNode
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
					mIL_AST.GetSecond(RecProc.Pos, RecFactoryFunc.CreateTempReg(out var TempReg), Arg)
				);
				Arg = TempReg;
			}
		}
		
		var ResultTupleReg = mIL_AST.cEmpty;
		
		foreach (var RecProc in aRecLambdasNode.List) {
			var RecProcConstructor = NewDefConstructor<tPos>();
			var (DefIndex, DefType) = RecProcConstructor.MapLambda(
				aModuleConstructor,
				RecProc.Lambda
			);
			
			var RecProcReg = RecFactoryFunc.InitProc(
				RecProc.Pos,
				DefIndex,
				DefType,
				RecProcConstructor.EnvIds.ToStream(
				).Map(
					_ => (
						Id: _,
						Type: RecProcConstructor.TypeDict.TryGet(_).AssertNotEmpty()
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
		
		var RecFactoryDefType = RecFactoryFunc.CreateDefType(
			aModuleConstructor,
			mVM_Type.Proc(
				mVM_Type.Empty(),
				RecProcsType,
				RecProcsType
			)
		);
		
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
				_ => (
					Id: _,
					Type: RecFactoryFunc.TypeDict.TryGet(_).AssertNotEmpty()
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
			}
		}
	}
	
	public static void
	MapDefVar<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tDefVarNode<tPos> aDefVarNode
	) {
		var Reg = aDefConstructor.MapExpression(aModuleConstructor, aDefVarNode.Expression);
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
	}
	
	public static void
	MapMethodCalls<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodCallsNode<tPos> aMethodCallsNode
	) {
		// TODO: set proper Def type ?
		
		var Object = aDefConstructor.MapExpression(aModuleConstructor, aMethodCallsNode.Object);
		foreach (var Call in aMethodCallsNode.MethodCalls) {
			var Arg = aDefConstructor.MapExpression(aModuleConstructor, Call.Argument);
			var MethodId = Call.Method.Id;
			if (MethodId == "_=...") {
				aDefConstructor.Commands.Push(mIL_AST.VarSet(aMethodCallsNode.Pos, Object, Arg));
				continue;
			}
			var Result = Call.Result.IsNone() ? mIL_AST.cEmpty : aDefConstructor.CreateTempReg();
			var ResultType = Call.Result.ThenDo(_ => _.TypeAnnotation.AssertNotEmpty()).Else(mVM_Type.Empty());
			var MethodReg = aDefConstructor.CreateTempReg();
			aDefConstructor.Commands.Push(
				[
					mIL_AST.CreatePair(aMethodCallsNode.Object.Pos, MethodReg, Object, MethodId),
					mIL_AST.CallProc(aMethodCallsNode.Pos, Result, MethodReg, Arg)
				]
			);
			if (Call.Result.IsSome(out var Result_)) {
				aDefConstructor.MapMatch(Result_, Result, ResultType);
			}
		}
	}
	
	public static void
	MapCommand<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tCommandNode<tPos> aCommandNode
	) {
		switch (aCommandNode) {
			case mSPO_AST.tDefNode<tPos> Node: {
				aDefConstructor.MapDef(aModuleConstructor, Node);
				break;
			}
			case mSPO_AST.tRecLambdasNode<tPos> Node: {
				aDefConstructor.MapRecursiveLambdas(aModuleConstructor, Node);
				break;
			}
			case mSPO_AST.tReturnIfNode<tPos> Node: {
				aDefConstructor.MapReturnIf(aModuleConstructor, Node);
				break;
			}
			case mSPO_AST.tDefVarNode<tPos> Node: {
				aDefConstructor.MapDefVar(aModuleConstructor, Node);
				break;
			}
			case mSPO_AST.tMethodCallsNode<tPos> Node: {
				aDefConstructor.MapMethodCalls(aModuleConstructor, Node);
				break;
			}
			default: {
				throw mError.Error("Impossible");
			}
		}
	}
	
	public static tModuleConstructor<tPos>
	MapModule<tPos>(
		mSPO_AST.tModuleNode<tPos> aModuleNode,
		mStd.tFunc<tPos, tPos, tPos> aMergePos,
		tScope aScope
	) {
		using var _ = mPerf.Measure();
		
		var Lambda = mSPO_AST.Lambda(
			aModuleNode.Pos,
			mStd.cEmpty,
			aModuleNode.Import.Match,
			mSPO_AST.Block(
				aMergePos(
					aModuleNode.Commands.TryFirst().ThenDo(_ => _.Pos).Else(default),
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
		
		mSPO_AST_Types.UpdateExpressionTypes(Lambda, aScope).ElseThrow();
		
		var ModuleConstructor = NewModuleConstructor(aMergePos);
		var TempLambdaDef = NewDefConstructor<tPos>();
		
		TempLambdaDef.MapLambda(ModuleConstructor, Lambda);
		
		var FistNonDef = TempLambdaDef.EnvIds.ToStream(
		).Where(
			_ => !_.StartsWith("d_")
		).TryFirst(
		);
		
		if (FistNonDef.IsSome(out var FirstNonDefId)) {
			throw mError.Error(
				$"expected definition symbol but was '{FirstNonDefId}'"
			);
		}
		
		if (TempLambdaDef.EnvIds.Size() != ModuleConstructor.Defs.Size() - 1) {
			throw mError.Error(
				$"expected {ModuleConstructor.Defs.Size() - 1} definitions but was {TempLambdaDef.EnvIds.Size()}"
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
