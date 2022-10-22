//IMPORT mSPO_AST.cs
//IMPORT mSPO_AST_Types.cs
//IMPORT mIL_AST.cs
//IMPORT mArrayList.cs
//IMPORT mPerf.cs
//IMPORT mError.cs
//IMPORT mMaybe.cs
//IMPORT mAssert.cs

using System;

#nullable enable

public static class
mSPO2IL {
	
	public class
	tModuleConstructor<tPos> {
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> TypeDef;
		public mTreeMap.tTree<tText, mVM_Type.tType> Types;
		public mArrayList.tArrayList<(tText Type, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands)> Defs;
		internal mStd.tFunc<tPos, tPos, tPos> MergePos;
	}
	
	public struct
	tDefConstructor<tPos> {
		public tInt32 Index;
		public tText Id; // TODO: remove because redundant, see ModuleConstructor
		public tText? TypeId; // TODO: remove because redundant, see ModuleConstructor
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands;
		public tInt32 LastTempReg;
		public mArrayList.tArrayList<tText> FreeIds;
		public mArrayList.tArrayList<(tText Id, tPos Pos)> EnvIds;
		public tModuleConstructor<tPos> ModuleConstructor; // TODO: remove back ref
	}
	
	public static tModuleConstructor<tPos>
	NewModuleConstructor<tPos>(
		mStd.tFunc<tPos, tPos, tPos> aMergePos
	) => new() {
		Defs = mArrayList.List<(tText Type, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Def)>(),
		TypeDef = mArrayList.List<mIL_AST.tCommandNode<tPos>>(),
		Types = mTreeMap.Tree<string, mVM_Type.tType>((a1, a2) => mMath.Sign(tText.CompareOrdinal(a1, a2))),
		MergePos = aMergePos,
	};
	
	public static tDefConstructor<tPos>
	NewDefConstructor<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor
	) {
		var DefIndex = aModuleConstructor.Defs.Size();
		var Commands = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		
		aModuleConstructor.Defs.Push((default(tText), Commands));
		return new tDefConstructor<tPos> {
			TypeId = default,
			Commands = Commands,
			FreeIds = mArrayList.List<tText>(),
			EnvIds = mArrayList.List<(tText Id, tPos Pos)>(),
			Index = DefIndex,
			Id = DefId(DefIndex),
			ModuleConstructor = aModuleConstructor,
		};
	}
	
	public static tText RegId(tInt32 a) => "t_" + a;
	public static tText DefId(tInt32 a) => "d_" + a;
	public static tText Id(tText a) => "_" + a;
	
	public static tText
	CreateTempReg<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor
	) {
		aDefConstructor.LastTempReg += 1;
		return RegId(aDefConstructor.LastTempReg);
	}
	
	public static mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>
	UnrollList<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tText aReg,
		mStream.tStream<(tText Id, tPos Pos)>? aSymbols
	) {
		var ExtractEnv = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		var RestEnv = aReg;
		foreach (var Symbol in aSymbols.Reverse()) {
			ExtractEnv.Push(mIL_AST.GetSecond(aPos, Symbol.Id, RestEnv));
			var NewRestEnv = aDefConstructor.CreateTempReg();
			ExtractEnv.Push(mIL_AST.GetFirst(aPos, NewRestEnv, RestEnv));
			RestEnv = NewRestEnv;
		}
		return ExtractEnv;
	}
	
	public static tText
	MapType<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		mVM_Type.tType aType
	) {
		switch (aType.Normalize()) {
			case var a when (a.MatchType(out _)):{
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cTypeType, a);
				return mIL_GenerateOpcodes.cTypeType;
			}
			case var a when a.MatchEmpty(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cEmptyType, a);
				return mIL_GenerateOpcodes.cEmptyType;
			}
			case var a when a.MatchBool(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cBoolType, a);
				return mIL_GenerateOpcodes.cBoolType;
			}
			case var a when a.MatchInt(): {
				aModuleConstructor.Types = aModuleConstructor.Types.Set(mIL_GenerateOpcodes.cIntType, a);
				return mIL_GenerateOpcodes.cIntType;
			}
			case var a when a.MatchFree(out var Id_, out var Ref): {
				if (Ref.Kind == mVM_Type.tKind.Free) {
					aModuleConstructor.Types = aModuleConstructor.Types.Set(Id_, a);
					return Id_;
				} else {
					return aModuleConstructor.MapType(Ref);
				}
			}
			case var a when a.MatchPrefix(out var Prefix, out var Id): {
				var NewId = $"[#{Prefix} {Id}]";
				aModuleConstructor.Types = aModuleConstructor.Types.Set(NewId, a);
				return NewId;
			}
			case var a when a.MatchRecord(out var Key, out var HeadId, out var TailId): {
				var NewId = $"[{{{Key}: {HeadId}, {TailId}}}]";
				aModuleConstructor.Types = aModuleConstructor.Types.Set(NewId, a);
				return NewId;
			}
			case var a when a.MatchPair(out var Type1, out var Type2) && !Type2.MatchEmpty(): {
				var Id1 = aModuleConstructor.MapType(Type1);
				var Id2 = aModuleConstructor.MapType(Type2);
				var NewId = $"[{Id1},{Id2}]";
				aModuleConstructor.TypeDef.Push(mIL_AST.TypePair(default(tPos), NewId, Id1, Id2));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(NewId, a);
				return NewId;
			}
			case var a when a.MatchSet(out var Type1, out var Type2): {
				var Id1 = aModuleConstructor.MapType(Type1);
				var Id2 = aModuleConstructor.MapType(Type2);
				var NewId = $"[{Id1}|{Id2}]";
				aModuleConstructor.TypeDef.Push(mIL_AST.TypeSet(default(tPos), NewId, Id1, Id2));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(NewId, a);
				return NewId;
			}
			case var a when a.MatchProc(out var EnvType, out var ArgType, out var ResType): {
				var IdArg = aModuleConstructor.MapType(ArgType);
				var IdRes = aModuleConstructor.MapType(ResType);
				var IdFunc = $"[{IdArg}->{IdRes}]";
				aModuleConstructor.TypeDef.Push(mIL_AST.TypeFunc(default(tPos), IdFunc, IdArg, IdRes));
				
				if (EnvType.MatchEmpty()) {
					aModuleConstructor.Types = aModuleConstructor.Types.Set(IdFunc, a);
					return IdFunc;
				}
				
				var IdEnv = aModuleConstructor.MapType(EnvType);
				var IdEnvFunc = $"[{IdEnv}:{IdFunc}]"; 
				aModuleConstructor.TypeDef.Push(mIL_AST.TypeMethod(default(tPos), IdEnvFunc, IdEnv, IdFunc));
				aModuleConstructor.Types = aModuleConstructor.Types.Set(IdEnvFunc, a);
				return IdEnvFunc;
			}
			default: {
				throw new NotImplementedException("" + aType.Kind);
			}
		}
	}
	
	public static mVM_Type.tType
	CreateDefType<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mVM_Type.tType aResType,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		var FreeIds = aDefConstructor.FreeIds.ToStream();
		aDefConstructor.EnvIds = aDefConstructor.EnvIds.ToStream(
		).Where(
			a => !FreeIds.Any(a_ => a_ == a.Id)
		).ToArrayList();
		
		var Defs = aDefConstructor.ModuleConstructor.Defs;
		var Types = aDefConstructor.ModuleConstructor.Types;
		var EnvIds = aDefConstructor.EnvIds;
		
		var EnvType = EnvIds.ToStream(
		).Reduce(
			mStream.Stream<mVM_Type.tType>(),
			(aRes, aEnvId) => mStream.Stream(
				aScope.Where(
					a => a.Id == aEnvId.Id
				).TryFirst(
				).Else(
					() => {
						if (!aEnvId.Id.StartsWith("d_")) {
							throw new Exception();
						}
						var TypeName = Defs.Get(tInt32.Parse(aEnvId.Id[2..])).Type;
						return (
							Id: aEnvId.Id,
							Type: Types.TryGet(
								TypeName
							).ElseThrow(
								() => $"can't find type '{TypeName}' for '{aEnvId.Id} at {aEnvId.Pos}'"
							)
						);
					}
				).Type,
				aRes
			)
		).Match(
			aOnAny: a => mVM_Type.Tuple(a.Reverse()),
			aOnNone: () => mVM_Type.Tuple(
				EnvIds.ToStream(
				).Map(
					a => {
						if (!a.Id.StartsWith("d_")) {
							throw new Exception();
						}
						var TypeName = Defs.Get(tInt32.Parse(a.Id[2..])).Type;
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
		
		aDefConstructor.TypeId = aDefConstructor.ModuleConstructor.MapType(
			Type
		);
		
		aDefConstructor.ModuleConstructor.Types = aDefConstructor.ModuleConstructor.Types.Set(
			aDefConstructor.TypeId,
			Type
		);
		
		return Type;
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	InitMapLambda<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		mSPO_AST_Types.UpdateExpressionTypes(aLambdaNode, aScope).ElseThrow();
		
		var Scope = mStream.Concat(aDefConstructor.MapMatch(aLambdaNode.Head, mIL_AST.cArg), aScope);
		
		aDefConstructor.TypeId = aDefConstructor.ModuleConstructor.MapType(aLambdaNode.TypeAnnotation.ElseThrow());
		aDefConstructor.ModuleConstructor.Defs.Set(
			aDefConstructor.Index,
			(aDefConstructor.TypeId, aDefConstructor.Commands)
		);
		
		var ResultReg = aDefConstructor.MapExpresion(aLambdaNode.Body, Scope);
		if (aLambdaNode.Body is not mSPO_AST.tBlockNode<tPos>) {
			aDefConstructor.Commands.Push(
				mIL_AST.ReturnIf(aLambdaNode.Body.Pos, ResultReg, mIL_AST.cTrue)
			);
		}
		
		var DefType = aDefConstructor.CreateDefType(
			aLambdaNode.TypeAnnotation.ElseThrow(),
			aScope
		);
		
		return mStream.Stream((Id: aDefConstructor.Id, Type: DefType), aScope);
	}
	
	public static void
	InitMapMethod<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		aDefConstructor.MapMatch(aMethodNode.Arg, mIL_AST.cArg);
		aDefConstructor.MapMatch(aMethodNode.Obj, mIL_AST.cObj);
		
		var ResultReg = aDefConstructor.MapExpresion(aMethodNode.Body, aScope);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf(aMethodNode.Pos, ResultReg, mIL_AST.cTrue));
		var FreeIds = aDefConstructor.FreeIds.ToStream();
		var NewEnvIds = aDefConstructor.EnvIds.ToStream(
		).Where(
			S1 => FreeIds.All(S2 => S1.Id != S2)
		).ToArrayList();
		aDefConstructor.EnvIds = NewEnvIds;
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	FinishMapProc<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		mVM_Type.tType aResType,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		var EnvType = aDefConstructor.CreateDefType(aResType, aScope);
		
		var Def = mArrayList.Concat(
			aDefConstructor.UnrollList(
				aPos,
				mIL_AST.cEnv,
				aDefConstructor.EnvIds.ToStream() // .Reverse() // TODO
			),
			aDefConstructor.Commands
		);
		
		aDefConstructor.Commands = Def;
		aDefConstructor.ModuleConstructor.Defs.Set(
			aDefConstructor.Index,
			(aDefConstructor.TypeId, Def)
		);
		
		return mStream.Stream(
			(aDefConstructor.Id, EnvType)
		);
	}
	
	public static (tText Id, mArrayList.tArrayList<(tText Id, tPos Pos)> EnvIds)
	MapLambda<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope 
	) {
		var TempLambdaDef = aModuleConstructor.NewDefConstructor();
		TempLambdaDef.InitMapLambda(aLambdaNode, aScope);
		var Scope = TempLambdaDef.FinishMapProc(
			aLambdaNode.Pos,
			aLambdaNode.TypeAnnotation.ElseThrow(),
			aScope
		);
		return (TempLambdaDef.Id, TempLambdaDef.EnvIds);
	}
	
	public static (
		tInt32 Index,
		mArrayList.tArrayList<(tText Id, tPos Pos)> EnvIds,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? Scope
	)
	MapMethod<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		var TempMethodDef = aModuleConstructor.NewDefConstructor();
		TempMethodDef.InitMapMethod(aMethodNode, aScope);
		var Scope = TempMethodDef.FinishMapProc(
			aMethodNode.Pos,
			aMethodNode.TypeAnnotation.ElseThrow(),
			aScope
		);
		return (
			TempMethodDef.Index,
			TempMethodDef.EnvIds,
			Scope
		);
	}
	
	public static tText
	InitProc<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tText aDefName,
		mArrayList.tArrayList<(tText Id, tPos Pos)> aEnv,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		// TODO: set proper Def type ?
		
		var ArgReg = mIL_AST.cEmpty;
		if (!aEnv.IsEmpty()) {
			foreach (var Symbol in aEnv.ToStream()) {
				if (aDefConstructor.EnvIds.ToStream().All(a => a.Id != Symbol.Id)) {
					aDefConstructor.EnvIds.Push(Symbol);
				}
			}
			
			if (aEnv.Size() == 1) {
				ArgReg = aEnv.Get(0).Id;
			} else {
				foreach (var Symbol_ in aEnv.ToStream()) {
					var NewArgReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.CreatePair(aPos, NewArgReg, ArgReg, Symbol_.Id));
					ArgReg = NewArgReg;
				}
			}
		}
		var Proc = aDefConstructor.CreateTempReg();
		aDefConstructor.Commands.Push(mIL_AST.CallFunc(aPos, Proc, aDefName, ArgReg));
		aDefConstructor.EnvIds.Push((aDefName, aPos)); // TODO
		return Proc;
	}
	
	public static tText
	MapExpresion<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tExpressionNode<tPos> aExpressionNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		switch (aExpressionNode) {
			//--------------------------------------------------------------------------------
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cEmpty;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tFalseNode<tPos> FalseNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cFalse;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTrueNode<tPos> TrueNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cTrue;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tEmptyTypeNode<tPos> EmptyTypeNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cEmptyType;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tBoolTypeNode<tPos> BoolTypeNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cBoolType;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIntTypeNode<tPos> IntTypeNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cIntType;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTypeTypeNode<tPos> TypeTypeNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cTypeType;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIntNode<tPos> { Pos: var Pos, Value: var Value }: {
			//--------------------------------------------------------------------------------
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(
						Pos,
						ResultReg,
						"" + Value
					)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIdNode<tPos> { Pos: var Pos, Id: var Id }: {
			//--------------------------------------------------------------------------------
				if (
					aDefConstructor.EnvIds.ToStream().All(a => a.Id != Id) &&
					!aDefConstructor.Commands.ToStream(
					).Any(
						a => a.GetResultReg().Match(
							Some: aName => aName == Id,
							None: () => false
						)
					)
				) {
					// TODO: set proper Def type ?
					aDefConstructor.EnvIds.Push((Id, Pos)); // TODO
				}
				return Id;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tCallNode<tPos>{ Pos: var Pos, Func: var Func, Arg: var Arg }: {
			//--------------------------------------------------------------------------------
				var FuncReg = aDefConstructor.MapExpresion(Func, aScope);
				var ArgReg = aDefConstructor.MapExpresion(Arg, aScope);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.CallFunc(Pos, ResultReg, FuncReg, ArgReg)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTupleNode<tPos>{ Items: var Items }: {
			//--------------------------------------------------------------------------------
				switch (Items.Take(2).ToArrayList().Size()) {
					case 0: {
						throw mError.Error("impossible");
					}
					case 1: {
						mAssert.IsTrue(Items.Match(out var Head, out var _));
						return aDefConstructor.MapExpresion(Head, aScope);
					}
					default: {
						mAssert.IsTrue(Items.Match(out var Head, out var _));
						var TailReg = mIL_AST.cEmpty;
						foreach (var Item in Items) {
							var HeadReg = aDefConstructor.MapExpresion(Item, aScope);
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
						return TailReg;
					}
				}
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPrefixNode<tPos>{ Pos: var Pos, Prefix: var Prefix, Element: var Element }: {
			//--------------------------------------------------------------------------------
				var ExpresionReg = aDefConstructor.MapExpresion(Element, aScope);
				var ResultReg = aDefConstructor.CreateTempReg();
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
			//--------------------------------------------------------------------------------
			case mSPO_AST.tRecordNode<tPos>{ Elements: var Elements }: {
			//--------------------------------------------------------------------------------
				var ResultReg = mIL_AST.cEmpty;
				foreach (var (Key, Value) in Elements) {
					var Expression = aDefConstructor.MapExpresion(Value, aScope);
					var PrefixReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.AddPrefix(Key.Pos, PrefixReg, Key.Id, Expression));
					var NewResultReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.ExtendRec(Key.Pos, NewResultReg, ResultReg, PrefixReg));
					ResultReg = NewResultReg;
				}
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTextNode<tPos>{ Pos: var Pos, Value: var Value }: {
			//--------------------------------------------------------------------------------
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
			//--------------------------------------------------------------------------------
			case mSPO_AST.tLambdaNode<tPos> LambdaNode: {
			//--------------------------------------------------------------------------------
				var (DefId, EnvIds) = aDefConstructor.ModuleConstructor.MapLambda(
					LambdaNode,
					aScope
				);
				
				return aDefConstructor.InitProc(
					LambdaNode.Pos,
					DefId,
					EnvIds,
					aScope
				);
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMethodNode<tPos> MethodNode: {
			//--------------------------------------------------------------------------------
				var (NewDefIndex, EnvIds, Scope) = aDefConstructor.ModuleConstructor.MapMethod(
					MethodNode,
					aScope
				);
				return aDefConstructor.InitProc(
					MethodNode.Pos,
					DefId(NewDefIndex),
					EnvIds,
					Scope
				);
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tBlockNode<tPos>{ Commands: var Commands }: {
			//--------------------------------------------------------------------------------
				var Scope = aScope;
				foreach (var Command in Commands) {
					Scope = aDefConstructor.MapCommand(Command, Scope);
				}
				// TODO: remove created symbols from unknown symbols
				return mIL_AST.cEmpty;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIfNode<tPos>{ Pos: var Pos, Cases: var Cases }: {
			//--------------------------------------------------------------------------------
				var Ifs = mArrayList.List<mSPO_AST.tCommandNode<tPos>>();
				foreach (var (Test, Run) in Cases) {
					Ifs.Push(mSPO_AST.ReturnIf(aDefConstructor.ModuleConstructor.MergePos(Test.Pos, Run.Pos), Run, Test));
				}
				Ifs.Push(
					mSPO_AST.ReturnIf(
						Pos,
						mSPO_AST.Empty(Pos),
						mSPO_AST.True(Pos)
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
				
				var Scope = mSPO_AST_Types.UpdateCommandTypes(Def, aScope).ElseThrow();
				
				aDefConstructor.MapCommand(
					Def,
					Scope
				);
				
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIfMatchNode<tPos>{ Pos: var Pos, Expression: var MatchExpression, Cases: var Cases }: {
			//--------------------------------------------------------------------------------
				var InputReg = aDefConstructor.MapExpresion(MatchExpression, aScope);
				var ModuleConstructor = aDefConstructor.ModuleConstructor;
				var TypeId =  ModuleConstructor.MapType(aExpressionNode.TypeAnnotation.ElseThrow());
				
				var SwitchDef = ModuleConstructor.NewDefConstructor(); // TODO
				
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
					RunDef.Commands.Push(mIL_AST.ReturnIf(CasePos, TempReg, mIL_AST.cTrue));
					RunDef.FinishMapProc(CasePos, CaseType, aScope);
					var RunProc = SwitchDef.InitProc(CasePos, RunDef.Id, RunDef.EnvIds, aScope);
					var CallerArgPair = SwitchDef.CreateTempReg();
					SwitchDef.Commands.Push(mIL_AST.CreatePair(CasePos, CallerArgPair, RunProc, mIL_AST.cArg));
					
					SwitchDef.Commands.Push(mIL_AST.TailCallIf(CasePos, CallerArgPair, TestResult));
				}
				// TODO: add gard command for the case that no case matched the argument
				// SwitchDef.Commands.Push(mIL_AST.ReturnIf(aExpressionNode.Pos, mIL_AST.cFalse, mIL_AST.cTrue));
				
				SwitchDef.FinishMapProc(aExpressionNode.Pos, CaseType, aScope);
				var SwitchProc = aDefConstructor.InitProc(
					aExpressionNode.Pos,
					SwitchDef.Id,
					SwitchDef.EnvIds,
					aScope
				);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.CallFunc(Pos, ResultReg, SwitchProc, InputReg));
				
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tVarToValNode<tPos>{ Pos: var Pos, Obj: var Obj }: {
			//--------------------------------------------------------------------------------
				var ObjReg = aDefConstructor.MapExpresion(Obj, aScope);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.VarGet(Pos, ResultReg, ObjReg));
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tRecursiveTypeNode<tPos>{ Pos: var Pos, HeadType: var HeadType, BodyType: var BodyType }: {
			//--------------------------------------------------------------------------------
				mAssert.IsFalse(aDefConstructor.EnvIds.ToStream().Any(a => a.Id == HeadType.Id));
				mAssert.IsFalse(aDefConstructor.FreeIds.ToStream().Any(a => a == HeadType.Id));
				aDefConstructor.Commands.Push(
					mIL_AST.TypeFree(HeadType.Pos, HeadType.Id)
				);
				var BodyTypeReg = aDefConstructor.MapExpresion(BodyType, aScope);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypeRecursive(
						Pos,
						ResultReg,
						HeadType.Id,
						BodyTypeReg
					)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tSetTypeNode<tPos>{ Expressions: var Expressions }: {
			//--------------------------------------------------------------------------------
				Expressions.Match(out var Head, out var Tail);
				var ResultReg = aDefConstructor.MapExpresion(Head, aScope);
				foreach (var Expression in Tail) {
					var ExprReg = aDefConstructor.MapExpresion(Expression, aScope);
					var SetTypeReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.TypeSet(Expression.Pos, SetTypeReg, ExprReg, ResultReg));
					ResultReg = SetTypeReg;
				}
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTupleTypeNode<tPos>{Expressions: var Expressions }: {
			//--------------------------------------------------------------------------------
				if (!Expressions.Match(out var First, out var Rest)) {
					return mIL_AST.cEmptyType;
				} else {
					var Pos = First.Pos;
					var ResultReg = aDefConstructor.MapExpresion(First, aScope);
					foreach (var Head in Rest) {
						var PairTypeReg = aDefConstructor.CreateTempReg();
						var HeadReg = aDefConstructor.MapExpresion(Head, aScope);
						Pos = aDefConstructor.ModuleConstructor.MergePos(Pos, Head.Pos);
						aDefConstructor.Commands.Push(mIL_AST.TypePair(Pos, PairTypeReg, ResultReg, HeadReg));
						ResultReg = PairTypeReg;
					}
					return ResultReg;
				}
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPrefixTypeNode<tPos> PrefixTypeNode: {
			//--------------------------------------------------------------------------------
				var InnerType = aDefConstructor.MapExpresion(
					mSPO_AST.TupleType(PrefixTypeNode.Pos, PrefixTypeNode.Expressions),
					aScope
				);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypePrefix(PrefixTypeNode.Pos, ResultReg, PrefixTypeNode.Prefix.Id, InnerType)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPipeToRightNode<tPos>{ Pos: var Pos, Left: var Left, Right: var Right }: {
			//--------------------------------------------------------------------------------
					switch (Right) {
						case mSPO_AST.tPipeToRightNode<tPos>{ Left: var RightLeft, Right: var RightRight }: {
							return aDefConstructor.MapExpresion(
								mSPO_AST.PipeToRight(
									Pos,
									mSPO_AST.PipeToRight(
										Left.Pos, //TODO: mStd.Merge(PipeToRightNode.Left.Pos, PipeToRightNode_.Left.Pos),
										Left,
										RightLeft
									),
									Right
								),
								aScope
							);
						}
						case mSPO_AST.tCallNode<tPos> CallNode: {
							var Func = (
								CallNode.Func is mSPO_AST.tIdNode<tPos> IdNode
								? mSPO_AST.Id(IdNode.Pos, "..." + IdNode.Id[1..])
								: CallNode.Func
							);
							var FuncReg = aDefConstructor.MapExpresion(Func, aScope);
							var Arg = (
								CallNode.Arg is mSPO_AST.tTupleNode<tPos> Tuple
								? mSPO_AST.Tuple(
									Tuple.Pos,
									mStream.Stream(Right, Tuple.Items)
								)
								: mSPO_AST.Tuple(
									CallNode.Arg.Pos,
									mStream.Stream(Right, mStream.Stream(CallNode.Arg))
								)
							);
							var ArgReg = aDefConstructor.MapExpresion(Arg, aScope);
							var ResultReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CallFunc(CallNode.Pos, ResultReg, FuncReg, ArgReg)
							);
							return ResultReg;
						}
						default: {
							var FirstArgReg = aDefConstructor.MapExpresion(Left, aScope);
							var FuncReg = aDefConstructor.MapExpresion(Right, aScope);
							var ResultReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CallFunc(Pos, ResultReg, FuncReg, FirstArgReg)
							);
							return ResultReg;
						}
					}
				}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPipeToLeftNode<tPos>{ Pos: var Pos, Left: var Left, Right: var Right }: {
			//--------------------------------------------------------------------------------
				switch (Left) {
					case mSPO_AST.tCallNode<tPos>{ Pos: var LeftPos, Func: var LeftFunc, Arg: var LeftArg }: {
						var Func = (
							(LeftFunc is mSPO_AST.tIdNode<tPos> IdNode)
							? mSPO_AST.Id(IdNode.Pos, IdNode.Id[1..] + "...")
							: LeftFunc
						);
						var FuncReg = aDefConstructor.MapExpresion(Func, aScope);
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
						var ArgReg = aDefConstructor.MapExpresion(Arg, aScope);
						var ResultReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(
							mIL_AST.CallFunc(LeftPos, ResultReg, FuncReg, ArgReg)
						);
						return ResultReg;
					}
					default: {
						var FirstArgReg = aDefConstructor.MapExpresion(Right, aScope);
						var FuncReg = aDefConstructor.MapExpresion(Left, aScope);
						var ResultReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(
							mIL_AST.CallFunc(Pos, ResultReg, FuncReg, FirstArgReg)
						);
						return ResultReg;
					}
				}
			}
			//--------------------------------------------------------------------------------
			default: {
			//--------------------------------------------------------------------------------
				throw mError.Error(
					$"not implemented: case {nameof(mSPO_AST)}.{aExpressionNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapExpresion)}(...)"
				);
			}
		}
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	MapMatch<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMatchNode<tPos> aMatchNode,
		tText aReg
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			//--------------------------------------------------------------------------------
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
			//--------------------------------------------------------------------------------
				return mStd.cEmpty;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchFreeIdNode<tPos>{ Pos: var Pos, Id: var Name }: {
			//--------------------------------------------------------------------------------
				mAssert.AreNotEquals(Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(aMatchNode.Pos, Name, aReg));
				aDefConstructor.FreeIds.Push(Name);
				return mStream.Stream((Id: Name, Type: aMatchNode.TypeAnnotation.ElseThrow()));
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatchNode: {
			//--------------------------------------------------------------------------------
				return mStd.cEmpty;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchPrefixNode<tPos>{ Pos: var Pos, Prefix: var Prefix, Match: var Match }: {
			//--------------------------------------------------------------------------------
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(
						Pos,
						ResultReg,
						Prefix,
						aReg
					)
				);
				return  aDefConstructor.MapMatch(
					Match,
					ResultReg
				);
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchRecordNode<tPos>{ Elements: var Elements, TypeAnnotation: var TypeAnnotation }: {
			//--------------------------------------------------------------------------------
				var Scope = mStream.Stream<(tText Id, mVM_Type.tType Type)>();
				foreach (var (IdNode, Match) in Elements) {
					var Pos = IdNode.Pos;
					
					var Reg = aReg;
					var Found = false;
					var RecType = TypeAnnotation.ElseThrow();
					while (RecType.MatchRecord(out var HeadId, out var HeadType, out RecType!)) {
						var HeadTailReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(mIL_AST.DivideRec(Pos, HeadTailReg, Reg));
						if (HeadId == IdNode.Id) {
							var TempValueReg_ = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(mIL_AST.GetSecond(Pos, TempValueReg_, HeadTailReg));
							var TempValueReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(mIL_AST.SubPrefix(Pos, TempValueReg, IdNode.Id, TempValueReg_));
							Scope = mStream.Concat(aDefConstructor.MapMatch(Match, TempValueReg), Scope);
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
				return Scope;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchTupleNode<tPos>{ Items: var Items }: {
				//--------------------------------------------------------------------------------
				var Scope = mStream.Stream<(tText Id, mVM_Type.tType Type)>();
				var RestReg = aReg;
				mAssert.AreEquals(Items.Take(2).ToArrayList().Size(), 2);
				foreach (var Item in Items.Reverse()) {
					var ItemReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetSecond(PatternNode.Pos, ItemReg, RestReg));
					
					Scope = mStream.Concat(aDefConstructor.MapMatch(Item, ItemReg), Scope);
					
					var NewRestReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetFirst(PatternNode.Pos, NewRestReg, RestReg));
					RestReg = NewRestReg;
				}
				return Scope;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchGuardNode<tPos>{ Match: var Match, Guard: var Guard }: {
			//--------------------------------------------------------------------------------
				// TODO: ASSERT Guard
				return aDefConstructor.MapMatch(Match, aReg);
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchNode<tPos> MatchNode: {
			//--------------------------------------------------------------------------------
				if (!TypeNode.IsSome(out var TypeNode_)) {
					return aDefConstructor.MapMatch(MatchNode, aReg);
				} else if (!MatchNode.Type.IsSome(out var MatchTypeNode)) {
					return aDefConstructor.MapMatch(
						mSPO_AST.Match(aMatchNode.Pos, MatchNode.Pattern, TypeNode),
						aReg
					);
				} else {
					throw mError.Error("not implemented"); //TODO: Unify MatchTypeNode & TypeNode_
				}
			}
			//--------------------------------------------------------------------------------
			default: {
			//--------------------------------------------------------------------------------
				throw mError.Error(
					$"not implemented: {nameof(mSPO_AST)}.{PatternNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapMatch)}(...)"
				);
			}
		}
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	MapMatchTest<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tText aInReg,
		mSPO_AST.tMatchNode<tPos> aMatchNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchFreeIdNode<tPos>{ Pos: var Pos, Id: var Id }: {
			//--------------------------------------------------------------------------------
				mAssert.AreNotEquals(Id, "_");
				mAssert.IsTrue(
					aDefConstructor.FreeIds.ToStream().All(a => a != Id)
				);
				aDefConstructor.Commands.Push(
					mIL_AST.Alias(Pos, Id, aInReg)
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatchNode: {
			//--------------------------------------------------------------------------------
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchPrefixNode<tPos>{Pos: var Pos, Prefix: var Prefix, Match: var Match }: {
			//--------------------------------------------------------------------------------
				var IsPrefix = aDefConstructor.CreateTempReg();
				var IsNotPrefix = aDefConstructor.CreateTempReg();
				var Reg = aDefConstructor.CreateTempReg();
				var InvReg = aDefConstructor.CreateTempReg();
				var SubValue = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.IsPrefix(Pos, IsPrefix, aInReg),
					mIL_AST.XOr(Pos, IsNotPrefix, IsPrefix, mIL_AST.cTrue),
					mIL_AST.ReturnIf(Pos, mIL_AST.cFalse, IsNotPrefix),
					mIL_AST.HasPrefix(Pos, Reg, Prefix, aInReg),
					mIL_AST.XOr(Pos, InvReg, Reg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(Pos, mIL_AST.cFalse, InvReg),
					mIL_AST.SubPrefix(Pos, SubValue, Prefix, aInReg)
				);
				aDefConstructor.MapMatchTest(SubValue, Match, aScope);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchGuardNode<tPos>{ Pos: var Pos, Match: var Match, Guard: var Guard }: {
			//--------------------------------------------------------------------------------
				aDefConstructor.MapMatchTest(aInReg, Match, aScope);
				
				var InvReg = aDefConstructor.CreateTempReg();
				var TestReg = aDefConstructor.MapExpresion(Guard, aScope);
				aDefConstructor.Commands.Push(
					// TODO: check type
					mIL_AST.XOr(Pos, InvReg, TestReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(Pos, mIL_AST.cFalse, InvReg)
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchTupleNode<tPos>{Pos: var Pos, Items: var Items }: {
			//--------------------------------------------------------------------------------
				var RestReg = aInReg;
				foreach (var Item in Items) {
					var IsPairReg = aDefConstructor.CreateTempReg();
					var IsNotPairReg = aDefConstructor.CreateTempReg();
					var HeadReg = aDefConstructor.CreateTempReg();
					var TailReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.IsPair(Pos, IsPairReg, aInReg),
						mIL_AST.XOr(Pos, IsNotPairReg, IsPairReg, mIL_AST.cTrue),
						mIL_AST.ReturnIf(Pos, mIL_AST.cFalse, IsNotPairReg),
						mIL_AST.GetFirst(Item.Pos, TailReg, RestReg),
						mIL_AST.GetSecond(Item.Pos, HeadReg, RestReg)
					);
					aDefConstructor.MapMatchTest(HeadReg, Item, aScope);
					RestReg = TailReg;
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIntNode<tPos>{Pos: var Pos, Value: var Value }: {
			//--------------------------------------------------------------------------------
				var IsIntReg = aDefConstructor.CreateTempReg();
				var IsNotIntReg = aDefConstructor.CreateTempReg();
				var IntReg = aDefConstructor.CreateTempReg();
				var CondReg = aDefConstructor.CreateTempReg();
				var InvCondReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.IsPair(Pos, IsIntReg, aInReg),
					mIL_AST.XOr(Pos, IsNotIntReg, IsIntReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(Pos, mIL_AST.cFalse, IsNotIntReg),
					mIL_AST.CreateInt(Pos, IntReg, $"{Value}"),
					mIL_AST.IntsAreEq(Pos, CondReg, aInReg, IntReg),
					mIL_AST.XOr(Pos, InvCondReg, CondReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(Pos, mIL_AST.cFalse, InvCondReg)
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchNode<tPos> MatchNode: {
			//--------------------------------------------------------------------------------
				aDefConstructor.MapMatch(MatchNode, aInReg);
				break;
			}
			//--------------------------------------------------------------------------------
			default: {
			//--------------------------------------------------------------------------------
				throw mError.Error(
					$"not implemented: {nameof(mSPO_AST)}.{PatternNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapMatchTest)}(...)"
				);
			}
		}
		
		return aScope; // TODO: need to add types to Scope !!!
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	MapDef<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tDefNode<tPos> aDefNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		var ValueReg = aDefConstructor.MapExpresion(aDefNode.Src, aScope);
		aDefConstructor.MapMatch(aDefNode.Des, ValueReg);
		return aScope;
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	MapReturnIf<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tReturnIfNode<tPos> aReturnNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		var ResReg = aDefConstructor.MapExpresion(aReturnNode.Result, aScope);
		var CondReg = aDefConstructor.MapExpresion(aReturnNode.Condition, aScope);
		aDefConstructor.Commands.Push(
			mIL_AST.ReturnIf(
				aReturnNode.Pos,
				ResReg,
				CondReg
			)
		);
		return aScope;
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	MapRecursiveLambdas<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tRecLambdasNode<tPos> aRecLambdasNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		// TODO: set proper Def type ?
		
		var NewDefIndices = mArrayList.List<tInt32>();
		var SPODefNodes = mArrayList.List<mSPO_AST.tRecLambdaItemNode<tPos>>();
		var TempLambdaDefs = mArrayList.List<tDefConstructor<tPos>>();
		var AllEnvIds = mArrayList.List<(tText Id, tPos Pos)>();
		
		foreach (var RecLambdaItemNode in aRecLambdasNode.List) {
			var NewDefIndex = aDefConstructor.ModuleConstructor.Defs.Size();
			NewDefIndices.Push(NewDefIndex);
			AllEnvIds.Push((DefId(NewDefIndex), RecLambdaItemNode.Pos)); // TODO
			var Type = mSPO_AST_Types.UpdateExpressionTypes(RecLambdaItemNode.Lambda, aScope).ElseThrow();
			var TypeId = aDefConstructor.ModuleConstructor.MapType(Type);
			TempLambdaDefs.Push(aDefConstructor.ModuleConstructor.NewDefConstructor());
			SPODefNodes.Push(RecLambdaItemNode);
		}
		
		var Max = NewDefIndices.Size();
		
		// create all rec. func. in each rec. func.
		var FuncNames = aRecLambdasNode.List.Map(a => a.Id.Id).ToArrayList();
		foreach (var TempLambdaDef in TempLambdaDefs.ToArray()) {
			for (var J = 0; J < Max; J += 1) {
				var Definition = AllEnvIds.Get(J);
				TempLambdaDef.Commands.Push(
					mIL_AST.CallFunc(Definition.Pos, FuncNames.Get(J), Definition.Id, mIL_AST.cEnv)
				);
			}
		}
		
		// InitMapLambda(...) for all rec. func.
		var Scope = aScope;
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempLambdaDef = TempLambdaDefs.Get(I);
			
			Scope = TempLambdaDef.InitMapLambda(RecLambdaItemNode.Lambda, Scope);
			var FreeIds = TempLambdaDef.FreeIds.ToStream();
			var TempEnvIds = TempLambdaDef.EnvIds.ToStream(
			).Where(
				aUnsolved => (
					FreeIds.All(a => a != aUnsolved.Id) &&
					aRecLambdasNode.List.All(a => a.Id.Id != aUnsolved.Id)
				)
			);
			
			foreach (var Symbol in TempEnvIds) {
				AllEnvIds.Push(Symbol);
			}
		}
		
		// FinishMapProc(...) for all rec. func.
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempDefConstructor = TempLambdaDefs.Get(I);
			
			TempDefConstructor.FinishMapProc(
				RecLambdaItemNode.Pos,
				RecLambdaItemNode.Lambda.TypeAnnotation.ElseThrow(),
				aScope
			);
			
			var ArgReg = mIL_AST.cEmpty;
			if (!AllEnvIds.IsEmpty()) {
				foreach (var UnsolvedSymbol in AllEnvIds.ToStream()) {
					if (aDefConstructor.EnvIds.ToStream().All(a => a.Id != UnsolvedSymbol.Id)) {
						aDefConstructor.EnvIds.Push(UnsolvedSymbol);
					}
				}
				
				foreach (var Symbol_ in AllEnvIds.ToStream()) {
					var NewArgReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.CreatePair(RecLambdaItemNode.Pos, NewArgReg, ArgReg, Symbol_.Id)
					);
					ArgReg = NewArgReg;
				}
			}
			
			aDefConstructor.Commands.Push(
				mIL_AST.CallFunc(
					RecLambdaItemNode.Pos,
					RecLambdaItemNode.Id.Id,
					TempDefConstructor.Id,
					ArgReg
				)
			);
			
			aDefConstructor.FreeIds.Push(RecLambdaItemNode.Id.Id);
		}
		
		return Scope;
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	MapVar<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tDefVarNode<tPos> aVarNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		var Reg = aDefConstructor.MapExpresion(aVarNode.Expression, aScope);
		aDefConstructor.Commands.Push(
			mIL_AST.VarDef(
				aVarNode.Pos,
				aVarNode.Id.Id,
				Reg
			)
		);
		return mStream.Stream(
			(
				Id: aVarNode.Id.Id,
				Type: aVarNode.TypeAnnotation.ElseThrow()
			),
			aScope
		);
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	MapMethodCalls<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMethodCallsNode<tPos> aMethodCallsNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		// TODO: set proper Def type ?
		
		var Object = aDefConstructor.MapExpresion(aMethodCallsNode.Object, aScope);
		foreach (var Call in aMethodCallsNode.MethodCalls) {
			var Arg = aDefConstructor.MapExpresion(Call.Argument, aScope);
			var MethodId = Call.Method.Id;
			if (MethodId == "_=...") {
				aDefConstructor.Commands.Push(mIL_AST.VarSet(aMethodCallsNode.Pos, Object, Arg));
				continue;
			}
			var Result = Call.Result.IsNone() ? mIL_AST.cEmpty : aDefConstructor.CreateTempReg();
			var MethodReg = aDefConstructor.CreateTempReg();
			aDefConstructor.Commands.Push(
				mIL_AST.CreatePair(aMethodCallsNode.Object.Pos, MethodReg, Object, MethodId),
				mIL_AST.CallProc(aMethodCallsNode.Pos, Result, MethodReg, Arg)
			);
			if (Call.Result.IsSome(out var Result_)) {
				aDefConstructor.MapMatch(Result_, Result);
			}
			
			var FreeIds = aDefConstructor.FreeIds.ToStream();
			if (FreeIds.All(a => a != MethodId)) {
				aDefConstructor.EnvIds.Push((MethodId, Call.Method.Pos)); // TODO
			}
		}
		
		return aScope;
	}
	
	public static mStream.tStream<(tText Id, mVM_Type.tType Type)>?
	MapCommand<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tCommandNode<tPos> aCommandNode,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) => aCommandNode switch {
		mSPO_AST.tDefNode<tPos> Node => aDefConstructor.MapDef(Node, aScope),
		mSPO_AST.tRecLambdasNode<tPos> Node => aDefConstructor.MapRecursiveLambdas(Node, aScope),
		mSPO_AST.tReturnIfNode<tPos> Node => aDefConstructor.MapReturnIf(Node, aScope),
		mSPO_AST.tDefVarNode<tPos> Node => aDefConstructor.MapVar(Node, aScope),
		mSPO_AST.tMethodCallsNode<tPos> Node => aDefConstructor.MapMethodCalls(Node, aScope),
		_ => throw mError.Error("Impossible")
	};
	
	public static tModuleConstructor<tPos>
	MapModule<tPos>(
		mSPO_AST.tModuleNode<tPos> aModuleNode,
		mStd.tFunc<tPos, tPos, tPos> aMergePos,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		using var _ = mPerf.Measure();
		
		var Lambda = mSPO_AST.Lambda(
			aModuleNode.Pos,
			mStd.cEmpty,
			aModuleNode.Import.Match,
			mSPO_AST.Block(
				aMergePos(
					aModuleNode.Commands.IsEmpty() ? default : aModuleNode.Commands.TryFirst().Then(a => a.Pos).ElseDefault(),
					aModuleNode.Export.Pos
				),
				mStream.Concat(
					aModuleNode.Commands,
					mStream.Stream<mSPO_AST.tCommandNode<tPos>>(
						mSPO_AST.ReturnIf(
							aModuleNode.Export.Pos,
							aModuleNode.Export.Expression,
							mSPO_AST.True(aModuleNode.Export.Pos)
						)
					)
				)
			)
		);
		
		var ModuleConstructor = NewModuleConstructor(aMergePos);
		var Type = mSPO_AST_Types.UpdateExpressionTypes(Lambda, null).ElseThrow();
		var TypeId = ModuleConstructor.MapType(Type);
		var TempLambdaDef = ModuleConstructor.NewDefConstructor();
		
		TempLambdaDef.InitMapLambda(Lambda, aScope);
		
		if (TempLambdaDef.EnvIds.Size() != ModuleConstructor.Defs.Size() - 1) {
			throw TempLambdaDef.EnvIds.ToStream(
			).Where(
				a => !a.Id.StartsWith("d_")
			).TryFirst(
			).Then(
				a => mError.Error($"Unknown symbol '{a.Id}' @ {a.Pos}", a.Pos)
			).ElseDefault();
		}
		
		string.Create(10, 1, (span, i) => span[0] = (char)i);
		
		// TODO: set proper Def type ?
		var DefSymbols = mArrayList.List<(tText Id, tPos Pos)>();
		foreach (var (I, Def) in ModuleConstructor.Defs.ToLazyList().MapWithIndex().Skip(1)) {
			DefSymbols.Push(
				(
					DefId(I),
					aMergePos(Def.Commands.Get(0).Pos, Def.Commands.Get(Def.Commands.Size() - 1).Pos)
				)
			);
		}
		TempLambdaDef.FinishMapProc(
			aModuleNode.Pos,
			Lambda.TypeAnnotation.ElseThrow(),
			aScope
		);
		
		// TODO: Defs needs Types
		
		return ModuleConstructor;
	}
}
