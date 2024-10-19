using System;

using tScope = mStream.tStream<(string Id, mVM_Type.tType Type)>;

public static class
mSPO2IL {
	public class
	tModuleConstructor<tPos> {
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> TypeDef;
		public mTreeMap.tTree<tText, mVM_Type.tType> Types;
		public mArrayList.tArrayList<(tText? TypeId, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands)> Defs;
		internal mStd.tFunc<tPos, tPos, tPos> MergePos;
	}
	
	public struct
	tDefConstructor<tPos> {
		public tNat32 Index; // TODO: replace by Id
		// TODO: add SubDefs
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands;
		public tNat32 LastTempReg;
		public mArrayList.tArrayList<tText> UnknownIds;
		public mArrayList.tArrayList<tText> ArgIds;
		public mArrayList.tArrayList<tText> EnvIds;
		public mArrayList.tArrayList<(tText Id, mVM_Type.tType Type)> Types;
	}
	
	public static tModuleConstructor<tPos>
	NewModuleConstructor<tPos>(
		mStd.tFunc<tPos, tPos, tPos> aMergePos
	) => new() {
		Defs = mArrayList.List<(tText? Type, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Def)>(),
		TypeDef = mArrayList.List<mIL_AST.tCommandNode<tPos>>(),
		Types = mTreeMap.Tree<string, mVM_Type.tType>((a1, a2) => mMath.Sign(tText.CompareOrdinal(a1, a2))),
		MergePos = aMergePos,
	};
	
	public static tDefConstructor<tPos>
	NewDefConstructor<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		string aTypeId
	) {
		var DefIndex = aModuleConstructor.Defs.Size();
		var Commands = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		
		// TODO NOW: set TypeId
		aModuleConstructor.Defs.Push((aTypeId, Commands));
		return new tDefConstructor<tPos> {
			Commands = Commands,
			UnknownIds = mArrayList.List<tText>(),
			ArgIds = mArrayList.List<tText>(),
			EnvIds = mArrayList.List<tText>(),
			Types = mArrayList.List<(tText Id, mVM_Type.tType Type)>(),
			Index = DefIndex,
		};
	}
	
	public static tText GetRegId(tNat32 a) => "t_" + a;
	public static tText GetDefId(tNat32 a) => "d_" + a;
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
		mStream.tStream<tText>? aEnvSymbols
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
						aEnvSymbols.TryFirst().ElseThrow(),
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
		switch (aType.Normalize()) {
			case var a when a.IsType(out _):{
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
			case var a when a.IsPair(out var Type1, out var Type2) && !Type2.IsEmpty(): {
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
				throw new NotImplementedException("" + aType.Kind);
			}
		}
	}
	
	public static mVM_Type.tType
	CreateDefType<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mVM_Type.tType aResType
	) {
		var EnvIds = aDefConstructor.EnvIds;
		
		foreach (var UnknownId in aDefConstructor.UnknownIds.ToStream()) {
			EnvIds.Push(UnknownId);
		}
		
		var Defs = aModuleConstructor.Defs;
		var Types = aModuleConstructor.Types;
		var DefTypes = aDefConstructor.Types;
		
		var EnvType = EnvIds.ToStream(
		).Reduce(
			mStream.Stream<mVM_Type.tType>(),
			(aRes, aEnvId) => {
				mVM_Type.tType Type;
				if (aEnvId.StartsWith("d_")) {
					var TypeId = Defs.Get(tNat32.Parse(aEnvId[2..])).TypeId;
					Type = Types.TryGet(
						TypeId
					).ElseThrow(
						() => $"can't find type '{TypeId}' for '{aEnvId}'"
					);
				} else {
					Type = DefTypes.ToStream().Where(_ => _.Id == aEnvId).TryFirst().ElseThrow().Type;
				}
				return mStream.Stream(Type, aRes);
			}
		).Match(
			aOnAny: _ => mVM_Type.Tuple(_.Reverse()),
			aOnNone: () => mVM_Type.Tuple(
				EnvIds.ToStream(
				).Map(
					_ => {
						if (!_.StartsWith("d_")) {
							throw new Exception();
						}
						var TypeName = Defs.Get(tNat32.Parse(_[1..])).TypeId;
						return Types.TryGet(
							TypeName
						).ElseThrow(
							() => $"can't find type '{TypeName}'"
						);
					}
				)
			)
		);
		
		var Type = mVM_Type.Proc(
			mVM_Type.Empty(),
			EnvType,
			aResType
		);
		
		var TypeId = aModuleConstructor.MapType(
			Type
		);
		
		aModuleConstructor.Types = aModuleConstructor.Types.Set(
			TypeId,
			Type
		);
		
		return Type;
	}
	
	public static void
	StartMapLambda<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	) {
		aDefConstructor.MapMatch(aLambdaNode.Head, mIL_AST.cArg, aLambdaNode.Head.TypeAnnotation.ElseThrow());
		
		var ResultReg = aDefConstructor.MapExpression(aModuleConstructor, aLambdaNode.Body);
		if (aLambdaNode.Body is not mSPO_AST.tBlockNode<tPos>) {
			aDefConstructor.Commands.Push(
				mIL_AST.ReturnIf(aLambdaNode.Body.Pos, mIL_AST.cTrue, ResultReg)
			);
		}
	}
	
	public static void
	StartMapMethod<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		aDefConstructor.MapMatch(aMethodNode.Arg, mIL_AST.cArg, aMethodNode.Arg.TypeAnnotation.ElseThrow());
		aDefConstructor.MapMatch(aMethodNode.Obj, mIL_AST.cObj, aMethodNode.Obj.TypeAnnotation.ElseThrow());
		
		var ResultReg = aDefConstructor.MapExpression(aModuleConstructor, aMethodNode.Body);
		aDefConstructor.Commands.Push(
			mIL_AST.ReturnIf(aMethodNode.Pos, mIL_AST.cTrue, ResultReg)
		);
	}
	
	public static void
	FinishMapProc<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tModuleConstructor<tPos> aModuleConstructor,
		mVM_Type.tType aResType
	) {
		var DefType = aDefConstructor.CreateDefType(aModuleConstructor, aResType);
		var DefTypeId = aModuleConstructor.MapType(DefType);
		
		var Def = mArrayList.Concat(
			aDefConstructor.UnrollEnv(
				aPos,
				mIL_AST.cEnv,
				aDefConstructor.EnvIds.ToStream()
			),
			aDefConstructor.Commands
		);
		
		aDefConstructor.Commands = Def;
		aModuleConstructor.Defs.Set(
			aDefConstructor.Index,
			(DefTypeId, Def) // TODO NOW: set type
		);
		
		return;
	}
	
	public static (tNat32 DefIndex, mArrayList.tArrayList<tText> EnvIds)
	MapLambda<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	) {
		var TypeId = aModuleConstructor.MapType(aLambdaNode.TypeAnnotation.ElseThrow());
		var TempLambdaDef = aModuleConstructor.NewDefConstructor(TypeId);
		TempLambdaDef.StartMapLambda(aModuleConstructor, aLambdaNode);
		TempLambdaDef.FinishMapProc(
			aLambdaNode.Pos,
			aModuleConstructor,
			aLambdaNode.TypeAnnotation.ElseThrow()
		);
		
		return (TempLambdaDef.Index, TempLambdaDef.EnvIds);
	}
	
	public static (
		tNat32 Index,
		mArrayList.tArrayList<tText> EnvIds
	)
	MapMethod<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		var TypeId = aModuleConstructor.MapType(aMethodNode.TypeAnnotation.ElseThrow());
		var TempMethodDef = aModuleConstructor.NewDefConstructor(TypeId);
		TempMethodDef.StartMapMethod(aModuleConstructor, aMethodNode);
		TempMethodDef.FinishMapProc(
			aMethodNode.Pos,
			aModuleConstructor,
			aMethodNode.TypeAnnotation.ElseThrow()
		);
		return (
			TempMethodDef.Index,
			TempMethodDef.EnvIds
		);
	}
	
	public static tText
	InitProc<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tNat32 aDefIndex,
		mArrayList.tArrayList<tText> aEnv
	) {
		var ArgReg = mIL_AST.cEmpty;
		if (!aEnv.IsEmpty()) {
			foreach (var Symbol in aEnv.ToStream()) {
				if (
					!aDefConstructor.ArgIds.ToStream().Any(_ => _ == Symbol) &&
					!aDefConstructor.EnvIds.ToStream().Any(_ => _ == Symbol)
				) {
					aDefConstructor.EnvIds.Push(Symbol);
				}
			}
			
			if (aEnv.Size() == 1) {
				ArgReg = aEnv.Get(0);
			} else {
				foreach (var Symbol_ in aEnv.ToStream()) {
					var NewArgReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.CreatePair(aPos, NewArgReg, ArgReg, Symbol_));
					ArgReg = NewArgReg;
				}
			}
		}
		var DefId = GetDefId(aDefIndex);
		var Proc = aDefConstructor.CreateTempReg();
		aDefConstructor.Commands.Push(mIL_AST.CallFunc(aPos, Proc, DefId, ArgReg));
		aDefConstructor.EnvIds.Push(DefId); // TODO
		return Proc;
	}
	
	public static tText
	MapExpression<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tExpressionNode<tPos> aExpressionNode
	) {
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
				aDefConstructor.Types.Push((ResultReg, mVM_Type.Int()));
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
					!aDefConstructor.EnvIds.ToStream().Any(_ => _ == Id) &&
					!aDefConstructor.ArgIds.ToStream().Any(_ => _ == Id) &&
					!aDefConstructor.Commands.ToStream(
					).Any(
						_ => _.GetResultReg().Match(
							aOnSome: aName => aName == Id,
							aOnNone: () => false
						)
					)
				) {
					aDefConstructor.EnvIds.Push(Id); // TODO
					aDefConstructor.Types.Push((Id, Type.ElseThrow()));
				}
				return Id;
			}
			case mSPO_AST.tCallNode<tPos>{ Pos: var Pos, Func: var Func, Arg: var Arg, TypeAnnotation: var Type }: {
				var FuncReg = aDefConstructor.MapExpression(aModuleConstructor, Func);
				var ArgReg = aDefConstructor.MapExpression(aModuleConstructor, Arg);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
				aDefConstructor.Commands.Push(
					mIL_AST.CallFunc(Pos, ResultReg, FuncReg, ArgReg)
				);
				return ResultReg;
			}
			case mSPO_AST.tTupleNode<tPos>{ Items: var Items, TypeAnnotation: var Type }: {
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
						aDefConstructor.Types.Push((TailReg, Type.ElseThrow()));
						return TailReg;
					}
				}
			}
			case mSPO_AST.tPrefixNode<tPos>{ Pos: var Pos, Prefix: var Prefix, Element: var Element, TypeAnnotation: var Type }: {
				var ExpresionReg = aDefConstructor.MapExpression(aModuleConstructor, Element);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
				aDefConstructor.Commands.Push(
					mIL_AST.AddPrefix(
						Pos,
						ResultReg,
						Prefix,
						ExpresionReg
					)
				);
				return ResultReg;
			}
			case mSPO_AST.tRecordNode<tPos>{ Elements: var Elements, TypeAnnotation: var Type }: {
				var ResultReg = mIL_AST.cEmpty;
				foreach (var (Key, Value) in Elements) {
					var Expression = aDefConstructor.MapExpression(aModuleConstructor, Value);
					var PrefixReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.AddPrefix(Key.Pos, PrefixReg, Key.Id, Expression));
					var NewResultReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.ExtendRec(Key.Pos, NewResultReg, ResultReg, PrefixReg));
					ResultReg = NewResultReg;
				}
				aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
				return ResultReg;
			}
			case mSPO_AST.tTextNode<tPos>{ Pos: var Pos, Value: var Value }: {
				var TailReg = mIL_AST.cEmpty;
				var Index = Value.Length;
				while (Index --> 0) {
					var Char = Value[Index];
					var CharOrdReg = aDefConstructor.CreateTempReg();
					var HeadReg = aDefConstructor.CreateTempReg();
					var TextReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.CreateInt(Pos, CharOrdReg, ((int)Char).ToString()),
						mIL_AST.AddPrefix(Pos, HeadReg, "Char", CharOrdReg),
						mIL_AST.CreatePair(Pos, TextReg, TailReg, HeadReg)
					);
					TailReg = TextReg;
				}
				return TailReg;
			}
			case mSPO_AST.tLambdaNode<tPos> LambdaNode: {
				var (DefId, EnvIds) = aModuleConstructor.MapLambda(
					LambdaNode
				);
				
				return aDefConstructor.InitProc(
					LambdaNode.Pos,
					DefId,
					EnvIds
				);
			}
			case mSPO_AST.tMethodNode<tPos> MethodNode: {
				var (NewDefIndex, EnvIds) = aModuleConstructor.MapMethod(
					MethodNode
				);
				return aDefConstructor.InitProc(
					MethodNode.Pos,
					NewDefIndex,
					EnvIds
				);
			}
			case mSPO_AST.tBlockNode<tPos>{ Commands: var Commands }: {
				foreach (var Command in Commands) {
					aDefConstructor.MapCommand(aModuleConstructor, Command);
				}
				// TODO: remove created symbols from unknown symbols
				return mIL_AST.cEmpty;
			}
			case mSPO_AST.tIfNode<tPos>{ Pos: var Pos, Cases: var Cases }: {
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
						new mSPO_AST.tMatchFreeIdNode<tPos>{Id = ResultReg},
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
				
				aDefConstructor.MapDef(
					aModuleConstructor,
					Def
				);
				
				return ResultReg;
			}
			case mSPO_AST.tIfMatchNode<tPos>{ Pos: var Pos, Expression: var MatchExpression, Cases: var Cases, TypeAnnotation: var Type }: {
				var InputReg = aDefConstructor.MapExpression(aModuleConstructor, MatchExpression);
				var TypeId = aModuleConstructor.MapType(aExpressionNode.TypeAnnotation.ElseThrow());
				
				var SwitchDef = aModuleConstructor.NewDefConstructor(TypeId);
				
				var TestType = mVM_Type.Proc(
					mVM_Type.Empty(),
					MatchExpression.TypeAnnotation.ElseThrow(),
					mVM_Type.Bool()
				);
				
				var CaseType = mVM_Type.Proc(
					mVM_Type.Empty(),
					MatchExpression.TypeAnnotation.ElseThrow(),
					aExpressionNode.TypeAnnotation.ElseThrow()
				);
				
				#if !true
				foreach (var (Match, Expression) in Cases) {
					var CasePos = ModuleConstructor.MergePos(Match.Pos, Expression.Pos);
					
					var TestDef = ModuleConstructor.NewDefConstructor();
					TestDef.MapMatchTest(mIL_AST.cArg, Match, aScope);
					TestDef.Commands.Push(mIL_AST.ReturnIf(CasePos, mIL_AST.cTrue, mIL_AST.cTrue));
					TestDef.FinishMapProc(CasePos, TestType, aScope);
					var TestProc = SwitchDef.InitProc(CasePos, TestDef.Id, TestDef.EnvIds, aScope);
					var TestResult = SwitchDef.CreateTempReg();
					SwitchDef.Commands.Push(mIL_AST.CallFunc(CasePos, TestResult, TestProc, mIL_AST.cArg));
					
					var RunDef = ModuleConstructor.NewDefConstructor();
					RunDef.MapMatch(Match, mIL_AST.cArg);
					var TempReg = RunDef.MapExpresion(Expression, aScope);
					RunDef.Commands.Push(mIL_AST.ReturnIf(CasePos, mIL_AST.cTrue, TempReg));
					RunDef.FinishMapProc(CasePos, CaseType, aScope);
					var RunProc = SwitchDef.InitProc(CasePos, RunDef.Id, RunDef.EnvIds, aScope);
					var CallerArgPair_ = SwitchDef.CreateTempReg();
					SwitchDef.Commands.Push(mIL_AST.CreatePair(CasePos, CallerArgPair_, mIL_AST.cEmpty, RunProc));
					var CallerArgPair = SwitchDef.CreateTempReg();
					SwitchDef.Commands.Push(mIL_AST.CreatePair(CasePos, CallerArgPair, CallerArgPair_, mIL_AST.cArg));
					
					SwitchDef.Commands.Push(mIL_AST.TailCallIf(CasePos, CallerArgPair, TestResult));
				}
				// TODO: add gard command for the case that no case matched the argument
				// SwitchDef.Commands.Push(mIL_AST.ReturnIf(aExpressionNode.Pos, mIL_AST.cTrue, mIL_AST.cFalse));
				#else
				foreach (var Case in Cases) {
					var CasePos = aModuleConstructor.MergePos(Case.Match.Pos, Case.Expression.Pos);
					
					var TestAndCallCaseFunc = aModuleConstructor.NewDefConstructor(
						aModuleConstructor.MapType(
							mVM_Type.Proc(
								mVM_Type.Empty(), // TODO
								MatchExpression.TypeAnnotation.ElseThrow(),
								aExpressionNode.TypeAnnotation.ElseThrow()
							)
						)
					);
				
					aModuleConstructor.MapIfCase(
						TestAndCallCaseFunc,
						SwitchDef,
						Case,
						CaseType,
						CasePos
					);
					
					TestAndCallCaseFunc.FinishMapProc(
						Pos,
						aModuleConstructor,
						CaseType
					);
					
					var ProcId = SwitchDef.InitProc(Pos, TestAndCallCaseFunc.Index, TestAndCallCaseFunc.EnvIds);
					
					SwitchDef.Commands.Push(
						mIL_AST.CallFunc(CasePos, SwitchDef.CreateTempReg(out var Res_), ProcId, mIL_AST.cArg),
						mIL_AST.ReturnIfNotEmpty(CasePos, Res_)
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
				#endif
				
				SwitchDef.FinishMapProc(aExpressionNode.Pos, aModuleConstructor, CaseType);
				var SwitchProc = aDefConstructor.InitProc(
					aExpressionNode.Pos,
					SwitchDef.Index,
					SwitchDef.EnvIds
				);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.CallFunc(Pos, ResultReg, SwitchProc, InputReg));
				
				aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
				return ResultReg;
			}
			case mSPO_AST.tVarToValNode<tPos>{ Pos: var Pos, Obj: var Obj, TypeAnnotation: var Type }: {
				var ObjReg = aDefConstructor.MapExpression(aModuleConstructor, Obj);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.VarGet(Pos, ResultReg, ObjReg));
				aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
				return ResultReg;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos>{ Pos: var Pos, HeadType: var HeadType, BodyType: var BodyType, TypeAnnotation: var Type }: {
				mAssert.IsFalse(aDefConstructor.EnvIds.ToStream().Any(_ => _ == HeadType.Id));
				mAssert.IsFalse(aDefConstructor.UnknownIds.ToStream().Any(_ => _ == HeadType.Id));
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
				aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
				return ResultReg;
			}
			case mSPO_AST.tSetTypeNode<tPos>{ Expressions: var Expressions, TypeAnnotation: var Type }: {
				mAssert.IsTrue(Expressions.Is(out var Head, out var Tail));
				var ResultReg = aDefConstructor.MapExpression(aModuleConstructor, Head);
				foreach (var Expression in Tail) {
					var ExprReg = aDefConstructor.MapExpression(aModuleConstructor, Expression);
					var SetTypeReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.TypeSet(Expression.Pos, SetTypeReg, ExprReg, ResultReg));
					ResultReg = SetTypeReg;
				}
				aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
				return ResultReg;
			}
			case mSPO_AST.tTupleTypeNode<tPos>{Expressions: var Expressions, TypeAnnotation: var Type }: {
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
					aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
					return ResultReg;
				}
			}
			case mSPO_AST.tPrefixTypeNode<tPos>{ Pos: var Pos, Prefix: var Prefix, Expressions: var Expressions, TypeAnnotation: var Type}: {
				var InnerType = aDefConstructor.MapExpression(
					aModuleConstructor,
					mSPO_AST.TupleType(Pos, Expressions)
				);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypePrefix(Pos, ResultReg, Prefix.Id, InnerType)
				);
				aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
				return ResultReg;
			}
			case mSPO_AST.tPipeToRightNode<tPos>{ Pos: var Pos, Left: var Left, Right: var Right, TypeAnnotation: var Type }: {
				switch (Right) {
					case mSPO_AST.tPipeToRightNode<tPos>{ Left: var RightLeft, Right: var RightRight }: {
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
					case mSPO_AST.tCallNode<tPos>{ Pos: var Pos_, Func: var Func_, Arg: var Arg_ }: {
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
								mStream.Stream(Right, mStream.Stream(Arg_))
							)
						);
						var ArgReg = aDefConstructor.MapExpression(aModuleConstructor, Arg);
						var ResultReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(
							mIL_AST.CallFunc(Pos_, ResultReg, FuncReg, ArgReg)
						);
						aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
						return ResultReg;
					}
					default: {
						var FirstArgReg = aDefConstructor.MapExpression(aModuleConstructor, Left);
						var FuncReg = aDefConstructor.MapExpression(aModuleConstructor, Right);
						var ResultReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(
							mIL_AST.CallFunc(Pos, ResultReg, FuncReg, FirstArgReg)
						);
						aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
						return ResultReg;
					}
				}
			}
			case mSPO_AST.tPipeToLeftNode<tPos>{ Pos: var Pos, Left: var Left, Right: var Right }: {
				switch (Left) {
					case mSPO_AST.tCallNode<tPos>{ Pos: var LeftPos, Func: var LeftFunc, Arg: var LeftArg }: {
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
								mStream.Concat(Tuple.Items, mStream.Stream(Right))
							)
							: mSPO_AST.Tuple(
								LeftArg.Pos,
								mStream.Stream(LeftArg, mStream.Stream(Right))
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
		tDefConstructor<tPos> aTestAndCallCaseFunc,
		tDefConstructor<tPos> aSwitchDef,
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
				var Res__ = aTestAndCallCaseFunc.MapExpression(aModuleConstructor, aCase.Expression);
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res__)
				);
				break;
			}
			case mSPO_AST.tEmptyTypeNode<tPos> p: {
				throw new NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tBoolTypeNode<tPos> p: {
				var LazyCaseDef = aModuleConstructor.NewDefConstructor(
					aModuleConstructor.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Bool(),
							aCase.Match.TypeAnnotation.ElseThrow()
						)
					)
				);
				// TODO: map pattern as arg
				LazyCaseDef.MapExpression(aModuleConstructor, aCase.Expression);
				LazyCaseDef.FinishMapProc(aCasePos, aModuleConstructor, aCaseType);
				var LazyCaseDefId = aTestAndCallCaseFunc.InitProc(
					p.Pos,
					LazyCaseDef.Index,
					LazyCaseDef.EnvIds
				);
				
				var BoolArg = aSwitchDef.CreateTempReg();
				var Res = aSwitchDef.CreateTempReg();
				
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.TryAsBool(aCasePos, BoolArg, mIL_AST.cArg),
					mIL_AST.CallFunc(aCasePos, Res, LazyCaseDefId, BoolArg),
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res)
				);
				break;
			}
			case mSPO_AST.tTrueNode<tPos> p: {
				throw new NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tFalseNode<tPos> p: {
				throw new NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tIntTypeNode<tPos> p: {
				throw new NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tIntNode<tPos> p: {
				var LazyCaseDef = aModuleConstructor.NewDefConstructor(
					aModuleConstructor.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Int(),
							aCase.Match.TypeAnnotation.ElseThrow()
						)
					)
				);
				// TODO: map pattern as arg
				var Res = LazyCaseDef.MapExpression(aModuleConstructor, aCase.Expression);
				LazyCaseDef.Commands.Push(
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res)
				);
				LazyCaseDef.FinishMapProc(aCasePos, aModuleConstructor, aCaseType);
				var LazyCaseDefId = aTestAndCallCaseFunc.InitProc(
					p.Pos,
					LazyCaseDef.Index,
					LazyCaseDef.EnvIds
				);
				
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.TryAsInt(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var IntArg), mIL_AST.cArg),
					mIL_AST.CreateInt(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var Int), "" + p.Value),
					mIL_AST.IntsAreEq(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var Eq), IntArg, Int),
					mIL_AST.XOr(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var NotEq), Eq, mIL_AST.cTrue),
					mIL_AST.ReturnIf(aCasePos, NotEq, mIL_AST.cEmpty),
					mIL_AST.CallFunc(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var Res__), LazyCaseDefId, IntArg),
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res__)
				);
				break;
			}
			case mSPO_AST.tTupleTypeNode<tPos> p: {
				throw new NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tMatchPrefixNode<tPos> p: {
				var LazyCaseDef = aModuleConstructor.NewDefConstructor(
					aModuleConstructor.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Prefix(p.Prefix, p.Match.TypeAnnotation.ElseThrow()),
							aCase.Match.TypeAnnotation.ElseThrow()
						)
					)
				);
				
				var Res = LazyCaseDef.MapExpression(aModuleConstructor, aCase.Expression);
				LazyCaseDef.Commands.Push(
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res)
				);
				LazyCaseDef.FinishMapProc(aCasePos, aModuleConstructor, aCaseType);
				var LazyCaseDefId = aTestAndCallCaseFunc.InitProc(
					p.Pos,
					LazyCaseDef.Index,
					LazyCaseDef.EnvIds
				);
				
				aTestAndCallCaseFunc.Commands.Push(
					mIL_AST.TryRemovePrefixFrom(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var InnerArg), mIL_AST.cArg, p.Prefix),
					mIL_AST.CallFunc(aCasePos, aTestAndCallCaseFunc.CreateTempReg(out var Res__), LazyCaseDefId, InnerArg),
					mIL_AST.ReturnIf(aCasePos, mIL_AST.cTrue, Res__)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> p: {
				// TODO: Restructure for complex matches
				throw new NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tMatchNode<tPos> p: {
				throw new NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			case mSPO_AST.tMatchGuardNode<tPos> p: {
				throw new NotImplementedException(aCase.Match.Pattern.GetType().Name);
			}
			default: {
				throw new NotImplementedException(aCase.Match.Pattern.GetType().Name);
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
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
				break;
			}
			case mSPO_AST.tIntNode<tPos> IntNode: {
				// TODO: ???
				break;
			}
			case mSPO_AST.tMatchFreeIdNode<tPos>{ Pos: var Pos, Id: var Name, TypeAnnotation: var Type }: {
				mAssert.AreNotEquals(Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(aMatchNode.Pos, Name, aRegId));
				aDefConstructor.Types.Push((Name, Type.ElseThrow()));
				aDefConstructor.ArgIds.Push(Name);
				break;
			}
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatchNode: {
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos>{ Pos: var Pos, Prefix: var Prefix, Match: var Match, TypeAnnotation: var Type }: {
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(
						Pos,
						ResultReg,
						Prefix,
						aRegId
					)
				);
				aDefConstructor.Types.Push((ResultReg, Type.ElseThrow()));
				mAssert.IsTrue(aRegType.IsPrefix(Prefix, out var ResType));
				
				aDefConstructor.MapMatch(
					Match,
					ResultReg,
					ResType
				);
				break;
			}
			case mSPO_AST.tMatchRecordNode<tPos>{ Elements: var Elements, TypeAnnotation: var TypeAnnotation }: {
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
							aDefConstructor.Types.Push((Reg, TypeAnnotation.ElseThrow()));
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
			case mSPO_AST.tMatchTupleNode<tPos>{ Items: var Items }: {
				var RemainingReg = aRegId;
				var RemainingTypes = aRegType;
				mAssert.AreEquals(Items.Take(2).ToArrayList().Size(), 2u);
				foreach (var Item in Items.Reverse()) {
					mAssert.IsTrue(RemainingTypes.IsPair(out RemainingTypes, out var ItemType));
					var ItemReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetSecond(PatternNode.Pos, ItemReg, RemainingReg));
					aDefConstructor.Types.Push((ItemReg, Item.TypeAnnotation.ElseThrow()));
					aDefConstructor.MapMatch(Item, ItemReg, ItemType);
					
					var NewRestReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetFirst(PatternNode.Pos, NewRestReg, RemainingReg));
					RemainingReg = NewRestReg;
				}
				mAssert.IsTrue(RemainingTypes.IsEmpty());
				break;
			}
			case mSPO_AST.tMatchGuardNode<tPos>{ Match: var Match, Guard: var Guard }: {
				// TODO: ASSERT Guard
				aDefConstructor.MapMatch(Match, aRegId, aRegType);
				break;
			}
			case mSPO_AST.tMatchNode<tPos> MatchNode: {
				if (TypeNode.IsSome(out var TypeNode_)) {
					if (MatchNode.Type.IsSome(out var MatchTypeNode)) {
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
		aDefConstructor.MapMatch(aDefNode.Des, ValueReg, aDefNode.Src.TypeAnnotation.ElseThrow());
	}
	
	public static void
	MapReturnIf<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tReturnIfNode<tPos> aReturnNode
	) {
		var ResReg = aDefConstructor.MapExpression(aModuleConstructor, aReturnNode.Result);
		var CondReg = aDefConstructor.MapExpression(aModuleConstructor, aReturnNode.Condition);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf(aReturnNode.Pos, CondReg, ResReg));
	}
	
	public static void
	MapRecursiveLambdas<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tRecLambdasNode<tPos> aRecLambdasNode
	) {
		// TODO NOW: set proper Def type ?
		
		var NewDefIndices = mArrayList.List<tNat32>();
		var SPODefNodes = mArrayList.List<mSPO_AST.tRecLambdaItemNode<tPos>>();
		var TempLambdaDefs = mArrayList.List<tDefConstructor<tPos>>();
		var AllEnvIds = mArrayList.List<tText>();
		
		foreach (var RecLambdaItemNode in aRecLambdasNode.List) {
			var NewDefIndex = aModuleConstructor.Defs.Size();
			NewDefIndices.Push(NewDefIndex);
			AllEnvIds.Push(GetDefId(NewDefIndex)); // TODO
			var Type = RecLambdaItemNode.Lambda.TypeAnnotation.ElseThrow();
			var TypeId = aModuleConstructor.MapType(Type);
			TempLambdaDefs.Push(aModuleConstructor.NewDefConstructor(TypeId));
			SPODefNodes.Push(RecLambdaItemNode);
		}
		
		var Max = NewDefIndices.Size();
		
		// create all rec. func. in each rec. func.
		var FuncNames = aRecLambdasNode.List.Map(_ => _.Id.Id).ToArrayList();
		foreach (var TempLambdaDef in TempLambdaDefs.ToArray()) {
			foreach (var J in mStream.NatStartWith(0).Take(Max)) {
				var Definition = AllEnvIds.Get(J);
				TempLambdaDef.Commands.Push(
					mIL_AST.CallFunc(default(tPos), FuncNames.Get(J), Definition, mIL_AST.cEnv)
				);
			}
		}
		
		// InitMapLambda(...) for all rec. func.
		foreach (var I in mStream.NatStartWith(0).Take(Max)) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempLambdaDef = TempLambdaDefs.Get(I);
			
			TempLambdaDef.StartMapLambda(aModuleConstructor, RecLambdaItemNode.Lambda);
			var UnknownIds = TempLambdaDef.UnknownIds.ToStream();
			var TempEnvIds = TempLambdaDef.EnvIds.ToStream(
			).Where(
				aEnvId => (
					UnknownIds.All(_ => _ != aEnvId) &&
					aRecLambdasNode.List.All(_ => _.Id.Id != aEnvId)
				)
			);
			
			foreach (var Symbol in TempEnvIds) {
				AllEnvIds.Push(Symbol);
			}
		}
		
		// FinishMapProc(...) for all rec. func.
		foreach (var I in mStream.NatStartWith(0).Take(Max)) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempDefConstructor = TempLambdaDefs.Get(I);
			
			TempDefConstructor.FinishMapProc(
				RecLambdaItemNode.Pos,
				aModuleConstructor,
				RecLambdaItemNode.Lambda.TypeAnnotation.ElseThrow()
			);
			
			var ArgReg = mIL_AST.cEmpty;
			if (!AllEnvIds.IsEmpty()) {
				foreach (var UnsolvedSymbol in AllEnvIds.ToStream()) {
					if (aDefConstructor.EnvIds.ToStream().All(_ => _ != UnsolvedSymbol)) {
						aDefConstructor.EnvIds.Push(UnsolvedSymbol);
					}
				}
				
				foreach (var Symbol_ in AllEnvIds.ToStream()) {
					aDefConstructor.Commands.Push(
						mIL_AST.CreatePair(
							RecLambdaItemNode.Pos,
							aDefConstructor.CreateTempReg(out var NewArgReg),
							ArgReg,
							Symbol_
						)
					);
					ArgReg = NewArgReg;
				}
			}
			
			aDefConstructor.Commands.Push(
				mIL_AST.CallFunc(
					RecLambdaItemNode.Pos,
					RecLambdaItemNode.Id.Id,
					GetDefId(TempDefConstructor.Index),
					ArgReg
				)
			);
			
			aDefConstructor.UnknownIds.Push(RecLambdaItemNode.Id.Id);
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
		aDefConstructor.Types.Push((aDefVarNode.Id.Id, aDefVarNode.TypeAnnotation.ElseThrow()));
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
			var ResultType = Call.Result.ThenDo(_ => _.TypeAnnotation.ElseThrow()).Else(mVM_Type.Empty());
			var MethodReg = aDefConstructor.CreateTempReg();
			aDefConstructor.Commands.Push(
				mIL_AST.CreatePair(aMethodCallsNode.Object.Pos, MethodReg, Object, MethodId),
				mIL_AST.CallProc(aMethodCallsNode.Pos, Result, MethodReg, Arg)
			);
			if (Call.Result.IsSome(out var Result_)) {
				aDefConstructor.MapMatch(Result_, Result, ResultType);
			}
			
			var UnknownIds = aDefConstructor.UnknownIds.ToStream();
			if (UnknownIds.All(_ => _ != MethodId)) {
				aDefConstructor.EnvIds.Push(MethodId); // TODO
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
		tScope? aScope
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
						mSPO_AST.ReturnIf(
							aModuleNode.Export.Pos,
							mSPO_AST.True(aModuleNode.Export.Pos),
							aModuleNode.Export.Expression
						)
					)
				)
			)
		);
		
		var ModuleConstructor = NewModuleConstructor(aMergePos);
		var Type = mSPO_AST_Types.UpdateExpressionTypes(Lambda, null).ElseThrow();
		var TypeId = ModuleConstructor.MapType(Type);
		var TempLambdaDef = ModuleConstructor.NewDefConstructor(TypeId);
		
		TempLambdaDef.StartMapLambda(ModuleConstructor, Lambda);
		
		if (TempLambdaDef.EnvIds.Size() != ModuleConstructor.Defs.Size() - 1) {
			throw TempLambdaDef.EnvIds.ToStream(
			).Where(
				_ => !_.StartsWith("d_")
			).TryFirst(
			).ThenDo(
				_ => mError.Error($"Unknown symbol '{_}'", default(tPos))
			).ElseDo(
				() => mError.Error($"unknown error", default(tPos))
			);
		}
		
		var EnvIds = TempLambdaDef.EnvIds.ToStream();
		TempLambdaDef.EnvIds = mStream.NatStartWith(
			1
		).Take(
			TempLambdaDef.EnvIds.Size()
		).Reverse( // TODO: remove
		).Map(
			aNr => EnvIds.Where(
				_ => _ == mSPO2IL.GetDefId(aNr)
			).TryFirst(
			).ElseThrow(
			)
		).ToArrayList(
		);
		
		// TODO: set proper Def type ?
		var DefSymbols = mArrayList.List<(tText Id, tPos Pos)>();
		foreach (var (I, Def) in ModuleConstructor.Defs.ToLazyList().MapWithIndex().Skip(1)) {
			DefSymbols.Push(
				(
					GetDefId(I),
					aMergePos(Def.Commands.Get(0).Pos, Def.Commands.Get(Def.Commands.Size() - 1).Pos)
				)
			);
		}
		TempLambdaDef.FinishMapProc(
			aModuleNode.Pos,
			ModuleConstructor,
			Lambda.TypeAnnotation.ElseThrow()
		);
		
		return ModuleConstructor;
	}
}
