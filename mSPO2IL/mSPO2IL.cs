﻿using tBool = System.Boolean;

using tNat8 = System.Byte;
using tNat16 = System.UInt16;
using tNat32 = System.UInt32;
using tNat64 = System.UInt64;

using tInt8 = System.SByte;
using tInt16 = System.Int16;
using tInt32 = System.Int32;
using tInt64 = System.Int64;

using tChar = System.Char;
using tText = System.String;

// TODO: maybe use nested functions to cleanup ???

public static class mSPO2IL {
	public struct tModuleConstructor<tPos> {
		public mArrayList.tArrayList<mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>> Defs;
	}
	
	public struct tDefConstructor<tPos> {
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands;
		public tInt32 LastTempReg;
		public mArrayList.tArrayList<tText> KnownSymbols;
		public mArrayList.tArrayList<tText> UnsolvedSymbols;
		public tInt32 Index;
		public tModuleConstructor<tPos> ModuleConstructor;
	}
	
	//================================================================================
	public static tModuleConstructor<tPos>
	NewModuleConstructor<tPos>(
	//================================================================================
	) => new tModuleConstructor<tPos> {
		Defs = mArrayList.List<mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>>()
	};
	
	//================================================================================
	public static tDefConstructor<tPos>
	NewDefConstructor<tPos>(
		tModuleConstructor<tPos> aModuleConstructor
	//================================================================================
	) {
		var DefIndex = aModuleConstructor.Defs.Size();
		var Commands = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		aModuleConstructor.Defs.Push(Commands);
		return new tDefConstructor<tPos> {
			Commands = Commands,
			KnownSymbols = mArrayList.List<tText>(),
			UnsolvedSymbols = mArrayList.List<tText>(),
			Index = DefIndex,
			ModuleConstructor = aModuleConstructor
		};
	}
	
	public static tText TempReg(tInt32 a) => "t_" + a;
	public static tText TempDef(tInt32 a) => "d_" + a;
	public static tText Ident(tText a) => "_" + a;
	
	//================================================================================
	public static tText
	CreateTempReg<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor
	//================================================================================
	) {
		aDefConstructor.LastTempReg += 1;
		return TempReg(aDefConstructor.LastTempReg);
	}
	
	//================================================================================
	public static mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>
	UnrollList<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mStd.tSpan<tPos> aSpan,
		tText aReg,
		mList.tList<tText> aSymbols
	//================================================================================
	) {
		var ExtractEnv = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		switch (aSymbols.Take(2).ToArrayList().Size()) {
			case 0: {
				break;
			}
			case 1: {
				mDebug.Assert(aSymbols.Match(out var Head, out var _));
				ExtractEnv.Push(mIL_AST.Alias(aSpan, Head, aReg));
				break;
			}
			default: {
				var RestEnv = aReg;
				while (aSymbols.Match(out var Symbol, out aSymbols)) {
					ExtractEnv.Push(mIL_AST.GetFirst(aSpan, Symbol, RestEnv));
					var NewRestEnv = aDefConstructor.CreateTempReg();
					ExtractEnv.Push(mIL_AST.GetSecond(aSpan, NewRestEnv, RestEnv));
					RestEnv = NewRestEnv;
				}
				break;
			}
		}
		return ExtractEnv;
	}
	
	//================================================================================
	public static void
	InitMapLambda<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	//================================================================================
	) {
		aDefConstructor.MapMatch(aLambdaNode.Head, mIL_AST.cArg);
		var ResultReg = aDefConstructor.MapExpresion(aLambdaNode.Body);
		if (!(aLambdaNode.Body is mSPO_AST.tBlockNode<tPos>)) {
			aDefConstructor.Commands.Push(
				mIL_AST.ReturnIf(aLambdaNode.Body.Span, ResultReg, mIL_AST.cTrue)
			);
		}
		var KnownSymbols = aDefConstructor.KnownSymbols.ToLasyList();
		var NewUnsolvedSymbols = aDefConstructor.UnsolvedSymbols.ToLasyList(
		).Where(
			S1 => KnownSymbols.All(S2 => S1 != S2)
		).ToArrayList();
		aDefConstructor.UnsolvedSymbols = NewUnsolvedSymbols;
	}
	
	//================================================================================
	public static void
	InitMapMethod<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	//================================================================================
	) {
		aDefConstructor.MapMatch(aMethodNode.Arg, mIL_AST.cArg);
		aDefConstructor.MapMatch(aMethodNode.Obj, mIL_AST.cObj);
		
		var ResultReg = aDefConstructor.MapExpresion(aMethodNode.Body);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf(aMethodNode.Span, ResultReg, mIL_AST.cTrue));
		var KnownSymbols = aDefConstructor.KnownSymbols.ToLasyList();
		var NewUnsolvedSymbols = aDefConstructor.UnsolvedSymbols.ToLasyList(
		).Where(
			S1 => KnownSymbols.All(S2 => S1 != S2)
		).ToArrayList();
		aDefConstructor.UnsolvedSymbols = NewUnsolvedSymbols;
	}
	
	//================================================================================
	public static void
	FinishMapProc<tPos>(
		this ref tDefConstructor<tPos> aTempDefConstructor,
		mStd.tSpan<tPos> aSpan,
		mArrayList.tArrayList<tText> aUnsolveSymbols
	//================================================================================
	) {
		var Def = mArrayList.Concat(
			aTempDefConstructor.UnrollList(
				aSpan,
				mIL_AST.cEnv,
				aUnsolveSymbols.ToLasyList()
			),
			aTempDefConstructor.Commands
		);
		
		aTempDefConstructor.Commands = Def;
		aTempDefConstructor.ModuleConstructor.Defs.Set(
			aTempDefConstructor.Index,
			Def
		);
	}
	
	//================================================================================
	public static (tInt32, mArrayList.tArrayList<tText>)
	MapLambda<tPos>(
		this ref tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	//================================================================================
	) {
		var TempLambdaDef = NewDefConstructor(aModuleConstructor);
		TempLambdaDef.InitMapLambda(aLambdaNode);
		TempLambdaDef.FinishMapProc(
			aLambdaNode.Span,
			TempLambdaDef.UnsolvedSymbols
		);
		return (TempLambdaDef.Index, TempLambdaDef.UnsolvedSymbols);
	}
	
	//================================================================================
	public static (tInt32, mArrayList.tArrayList<tText>)
	MapMethod<tPos>(
		this ref tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	//================================================================================
	) {
		var TempMethodDef = NewDefConstructor(aModuleConstructor);
		TempMethodDef.InitMapMethod(aMethodNode);
		TempMethodDef.FinishMapProc(
			aMethodNode.Span,
			TempMethodDef.UnsolvedSymbols
		);
		return (TempMethodDef.Index, TempMethodDef.UnsolvedSymbols);
	}
	
	//================================================================================
	public static tText
	InitProc<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mStd.tSpan<tPos> aSpan,
		tText aDefName,
		mArrayList.tArrayList<tText> aEnv
	//================================================================================
	) {
		var ArgReg = mIL_AST.cEmpty;
		if (!aEnv.IsEmpty()) {
			var UnsolvedSymbols = aEnv.ToLasyList();
			while (UnsolvedSymbols.Match(out var Symbol, out UnsolvedSymbols)) {
				if (aDefConstructor.UnsolvedSymbols.ToLasyList().All(_ => _ != Symbol)) {
					aDefConstructor.UnsolvedSymbols.Push(Symbol);
				}
			}
			
			if (aEnv.Size() == 1) {
				ArgReg = aEnv.Get(0);
			} else {
				UnsolvedSymbols = aEnv.ToLasyList().Reverse();
				while (UnsolvedSymbols.Match(out var Symbol_, out UnsolvedSymbols)) {
					var NewArgReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.CreatePair(aSpan, NewArgReg, Symbol_, ArgReg));
					ArgReg = NewArgReg;
				}
			}
		}
		
		var Proc = aDefConstructor.CreateTempReg();
		aDefConstructor.Commands.Push(mIL_AST.Call<tPos>(aSpan, Proc, aDefName, ArgReg));
		aDefConstructor.UnsolvedSymbols.Push(aDefName);
		return Proc;
	}
	
	//================================================================================
	public static tText
	MapExpresion<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tExpressionNode<tPos> aExpressionNode
	//================================================================================
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
			case mSPO_AST.tNumberNode<tPos> NumberNode: {
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(
						NumberNode.Span,
						ResultReg,
						NumberNode.Value.ToString()
					)
				);
				return ResultReg;
			}
			case mSPO_AST.tIdentNode<tPos> IdentNode: {
				if (
					aDefConstructor.UnsolvedSymbols.ToLasyList().All(_ => _ != IdentNode.Name) &&
					aDefConstructor.Commands.ToLasyList(
					).All(
						_ => !_.TryGetResultReg(out var Name) || Name != IdentNode.Name
					)
				) {
					aDefConstructor.UnsolvedSymbols.Push(IdentNode.Name);
				}
				return IdentNode.Name;
			}
			case mSPO_AST.tCallNode<tPos> CallNode: {
				var FuncReg = aDefConstructor.MapExpresion(CallNode.Func);
				var ArgReg = aDefConstructor.MapExpresion(CallNode.Arg);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.Call(CallNode.Span, ResultReg, FuncReg, ArgReg)
				);
				return ResultReg;
			}
			case mSPO_AST.tTupleNode<tPos> TupleNode: {
				switch (TupleNode.Items.Take(2).ToArrayList().Size()) {
					case 0: {
						throw mStd.Error("impossible");
					}
					case 1: {
						mDebug.Assert(TupleNode.Items.Match(out var Head, out var _));
						return aDefConstructor.MapExpresion(Head);
					}
					default: {
						var List = TupleNode.Items.Reverse();
						var ResultReg = mIL_AST.cEmpty;
						while (List.Match(out var Item, out List)) {
							var ItemReg = aDefConstructor.MapExpresion(Item);
							var TupleReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CreatePair(
									Item.Span,
									TupleReg,
									ItemReg,
									ResultReg
								)
							);
							ResultReg = TupleReg;
						}
						return ResultReg;
					}
				}
			}
			case mSPO_AST.tPrefixNode<tPos> PrefixNode: {
				var ExpresionReg = aDefConstructor.MapExpresion(PrefixNode.Element);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.AddPrefix(
						PrefixNode.Span,
						ResultReg,
						PrefixNode.Prefix,
						ExpresionReg
					)
				);
				return ResultReg;
			}
			case mSPO_AST.tTextNode<tPos> TextNode: {
				throw mStd.Error("impossible");
			}
			case mSPO_AST.tLambdaNode<tPos> LambdaNode: {
				var(NewDefIndex, UnsolvedSymbols) = aDefConstructor.ModuleConstructor.MapLambda(
					LambdaNode
				);
				return aDefConstructor.InitProc(
					LambdaNode.Span,
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			case mSPO_AST.tMethodNode<tPos> MethodNode: {
				var(NewDefIndex, UnsolvedSymbols) = aDefConstructor.ModuleConstructor.MapMethod(
					MethodNode
				);
				return aDefConstructor.InitProc(
					MethodNode.Span,
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			case mSPO_AST.tBlockNode<tPos> BlockNode: {
				var CommandNodes = BlockNode.Commands;
				while (CommandNodes.Match(out var CommandNode, out CommandNodes)) {
					aDefConstructor.MapCommand(CommandNode);
				}
				// TODO: remove created symbols from unknown symbols
				return mIL_AST.cEmpty;
			}
			case mSPO_AST.tIfNode<tPos> IfNode: {
				var Ifs = mArrayList.List<mSPO_AST.tCommandNode<tPos>>();
				var Pairs = IfNode.Cases;
				while (Pairs.Match(out var Pair, out Pairs)) {
					var (Test, Run) = Pair;
					Ifs.Push(mSPO_AST.ReturnIf(mStd.Merge(Test.Span, Run.Span), Run, Test));
				}
				Ifs.Push(
					mSPO_AST.ReturnIf(
						IfNode.Span,
						mSPO_AST.Empty(IfNode.Span),
						mSPO_AST.True(IfNode.Span)
					)
				); // TODO: ASSERT FALSE
				
				var ResultReg = aDefConstructor.CreateTempReg();
				
				aDefConstructor.MapCommand(
					mSPO_AST.Def(
						IfNode.Span,
						mSPO_AST.Match(
							IfNode.Span,
							new mSPO_AST.tIdentNode<tPos>{Name = ResultReg},
							null
						),
						mSPO_AST.Call(
							IfNode.Span,
							mSPO_AST.Lambda(
								IfNode.Span,
								mSPO_AST.Match(
									IfNode.Span,
									mSPO_AST.Empty(IfNode.Span),
									null
								),
								mSPO_AST.Block(IfNode.Span, Ifs.ToLasyList())
							),
							mSPO_AST.Empty(IfNode.Span)
						)
					)
				);
				return ResultReg; 
			}
			case mSPO_AST.tIfMatchNode<tPos> IfMatchNode: {
				var Imput = new mSPO_AST.tIdentNode<tPos>{
					Name = aDefConstructor.MapExpresion(IfMatchNode.Expression)
				};
				
				var ModuleConstructor = aDefConstructor.ModuleConstructor;
				var Ifs = mArrayList.List<mSPO_AST.tCommandNode<tPos>>();
				var Rest = IfMatchNode.Cases;
				
				while (Rest.Match(out var Case, out Rest)) {
					var (Match, Run) = Case;
					var CaseSpan = mStd.Merge(Match.Span, Run.Span);
					var TestDef = NewDefConstructor(ModuleConstructor);
					
					TestDef.MapMatchTest(mIL_AST.cArg, Match);
					TestDef.FinishMapProc(CaseSpan, TestDef.UnsolvedSymbols);
					TestDef.Commands.Push(mIL_AST.ReturnIf(CaseSpan, mIL_AST.cTrue, mIL_AST.cTrue));
					
					Ifs.Push(
						mSPO_AST.ReturnIf(
							CaseSpan,
							mSPO_AST.Call(
								CaseSpan,
								mSPO_AST.Lambda(CaseSpan, Match, Run),
								Imput
							),
							mSPO_AST.Call(
								CaseSpan,
								mSPO_AST.Call(
									CaseSpan,
									new mSPO_AST.tIdentNode<tPos>{ Name = TempDef(TestDef.Index) },
									mSPO_AST.Tuple(
										CaseSpan,
										TestDef.UnsolvedSymbols.ToLasyList(
										).Map(
											Symbol => (mSPO_AST.tExpressionNode<tPos>)new mSPO_AST.tIdentNode<tPos>{ Name = Symbol }
										)
									)
								),
								Imput
							)
						)
					);
				}
				Ifs.Push(
					mSPO_AST.ReturnIf(
						IfMatchNode.Span,
						mSPO_AST.Empty(IfMatchNode.Span),
						mSPO_AST.True(IfMatchNode.Span)
					)
				); // TODO: ASSERT FALSE
				
				var ResultReg = aDefConstructor.CreateTempReg();
				
				aDefConstructor.MapCommand(
					mSPO_AST.Def(
						IfMatchNode.Span,
						mSPO_AST.Match(
							IfMatchNode.Span,
							new mSPO_AST.tIdentNode<tPos>{Name = ResultReg},
							null
						),
						mSPO_AST.Call(
							IfMatchNode.Span,
							mSPO_AST.Lambda(
								IfMatchNode.Span,
								mSPO_AST.Match(
									IfMatchNode.Span,
									mSPO_AST.Empty(IfMatchNode.Span),
									null
								),
								mSPO_AST.Block(IfMatchNode.Span, Ifs.ToLasyList())
							),
							mSPO_AST.Empty(IfMatchNode.Span)
						)
					)
				);
				return ResultReg; 
			}
			case mSPO_AST.tVarToValNode<tPos> VarToValNode: {
				var ObjReg = aDefConstructor.MapExpresion(VarToValNode.Obj);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.VarGet(VarToValNode.Span, ResultReg, ObjReg));
				return ResultReg;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursiveTypeNode: {
				mStd.AssertNot(aDefConstructor.UnsolvedSymbols.ToLasyList().Any(a => a == RecursiveTypeNode.HeadType.Name));
				mStd.AssertNot(aDefConstructor.KnownSymbols.ToLasyList().Any(a => a == RecursiveTypeNode.HeadType.Name));
				var BodyTypeReg = aDefConstructor.MapExpresion(RecursiveTypeNode.BodyType);
				aDefConstructor.UnsolvedSymbols = aDefConstructor.UnsolvedSymbols
					.ToLasyList()
					.Where(a => a != RecursiveTypeNode.HeadType.Name)
					.ToArrayList();
				
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypeRecursive(
						RecursiveTypeNode.Span,
						ResultReg,
						RecursiveTypeNode.HeadType.Name,
						BodyTypeReg
					)
				);
				return ResultReg;
			}
			case mSPO_AST.tSetTypeNode<tPos> SetTypeNode: {
				SetTypeNode.Expressions.Match(out var First, out var Rest);
				var ResultReg = aDefConstructor.MapExpresion(First);
				while (Rest.Match(out var Head, out Rest)) {
					var ExprReg = aDefConstructor.MapExpresion(Head);
					var SetTypeReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.TypeSet(Head.Span, SetTypeReg, ExprReg, ResultReg));
					ResultReg = SetTypeReg;
				}
				return ResultReg;
			}
			case mSPO_AST.tTupleTypeNode<tPos> TupleTypeNode: {
				if (!TupleTypeNode.Expressions.Match(out var First, out var Rest)) {
					return mIL_AST.cEmptyType;
				} else {
					var ResultReg = aDefConstructor.MapExpresion(First);
					while (Rest.Match(out var Head, out Rest)) {
						var PairTypeReg = aDefConstructor.CreateTempReg();
						var ExprReg = aDefConstructor.MapExpresion(Head);
						aDefConstructor.Commands.Push(mIL_AST.TypePair(Head.Span, PairTypeReg, ExprReg, ResultReg));
						ResultReg = PairTypeReg;
					}
					return ResultReg;
				}
			}
			case mSPO_AST.tPrefixTypeNode<tPos> PrefixTypeNode: {
				var InnerType = aDefConstructor.MapExpresion(
					mSPO_AST.TupleType(PrefixTypeNode.Span, PrefixTypeNode.Expressions)
				);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypePrefix(PrefixTypeNode.Span, ResultReg, PrefixTypeNode.Prefix.Name, InnerType)
				);
				return ResultReg;
			}
			default: {
				throw mStd.Error(
					$"not implemented: case {nameof(mSPO_AST)}.{aExpressionNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapExpresion)}(...)"
				);
			}
		}
	}
	
	//================================================================================
	public static void
	MapMatch<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMatchNode<tPos> aMatchNode,
		tText aReg
	//================================================================================
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
				break;
			}
			case mSPO_AST.tIdentNode<tPos> IdentNode: {
				if (IdentNode.Name == Ident("_")) {
					break;
				}
				aDefConstructor.Commands.Push(mIL_AST.Alias(IdentNode.Span, IdentNode.Name, aReg));
				aDefConstructor.KnownSymbols.Push(IdentNode.Name);
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> PrefixNode: {
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(
						PrefixNode.Span,
						ResultReg,
						PrefixNode.Prefix,
						aReg
					)
				);
				aDefConstructor.MapMatch(
					PrefixNode.Match,
					ResultReg
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> TupleNode: {
				var RestReg = aReg;
				var Items = TupleNode.Items;
				mDebug.AssertEq(Items.Take(2).ToArrayList().Size(), 2);
				while (Items.Match(out var Item, out Items)) {
					var ItemReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetFirst(Item.Span, ItemReg, RestReg));
					
					aDefConstructor.MapMatch(Item, ItemReg);
					
					var NewRestReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetSecond(Item.Span, NewRestReg, RestReg));
					RestReg = NewRestReg;
				}
				break;
			}
			case mSPO_AST.tMatchGuardNode<tPos> GuardNode: {
				// TODO: ASSERT GuardNode._Guard
				aDefConstructor.MapMatch(GuardNode.Match, aReg);
				break;
			}
			case mSPO_AST.tMatchNode<tPos> MatchNode: {
				if (TypeNode is null) {
					aDefConstructor.MapMatch(MatchNode, aReg);
				} else if (MatchNode.Type is null) {
					aDefConstructor.MapMatch(
						mSPO_AST.Match(MatchNode.Span, MatchNode.Pattern, TypeNode),
						aReg
					);
				} else {
					throw mStd.Error("not implemented"); //TODO: Unify MatchNode.Type & TypeNode
				}
				break;
			}
			default: {
				throw mStd.Error(
					$"not implemented: {nameof(mSPO_AST)}.{PatternNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapMatch)}(...)"
				);
			}
		}
	}
	
	//================================================================================
	public static void
	MapMatchTest<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tText aInReg,
		mSPO_AST.tMatchNode<tPos> aMatchNode
	//================================================================================
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			case mSPO_AST.tIdentNode<tPos> IdentNode: {
				if (IdentNode.Name == Ident("_")) {
					break;
				}
				mDebug.Assert(
					aDefConstructor.KnownSymbols.ToLasyList().All(_ => _ != IdentNode.Name)
				);
				aDefConstructor.Commands.Push(
					mIL_AST.Alias(IdentNode.Span, IdentNode.Name, aInReg)
				);
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> PrefixNode: {
				var Prefix = PrefixNode.Prefix;
				var SubMatch = PrefixNode.Match;
				var Reg = aDefConstructor.CreateTempReg();
				var InvReg = aDefConstructor.CreateTempReg();
				var SubValue = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.HasPrefix(PrefixNode.Span, Reg, Prefix, aInReg),
					mIL_AST.XOr(PrefixNode.Span, InvReg, Reg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(PrefixNode.Span, mIL_AST.cFalse, InvReg),
					mIL_AST.SubPrefix(PrefixNode.Span, SubValue, Prefix, aInReg)
				);
				aDefConstructor.MapMatchTest(SubValue, SubMatch);
				break;
			}
			case mSPO_AST.tMatchGuardNode<tPos> GuardNode: {
				var SubMatch = GuardNode.Match;
				var Guard = GuardNode.Guard;
				
				aDefConstructor.MapMatchTest(aInReg, SubMatch);
				
				var InvReg = aDefConstructor.CreateTempReg();
				
				var TestReg = aDefConstructor.MapExpresion(Guard);
				aDefConstructor.Commands.Push(
					mIL_AST.XOr(GuardNode.Span, InvReg, TestReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(GuardNode.Span, mIL_AST.cFalse, InvReg)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> TupleNode: {
				var Items = TupleNode.Items;
				while (Items.Match(out var Item, out Items)) {
					aDefConstructor.MapMatchTest(aInReg, Item);
				}
				break;
			}
			case mSPO_AST.tNumberNode<tPos> NumberNode: {
				var IntReg = aDefConstructor.CreateTempReg();
				var CondReg = aDefConstructor.CreateTempReg();
				var InvCondReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(NumberNode.Span, IntReg, $"{NumberNode.Value}"),
					mIL_AST.IntsAreEq(NumberNode.Span, CondReg, aInReg, IntReg),
					mIL_AST.XOr(NumberNode.Span, InvCondReg, CondReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(NumberNode.Span, mIL_AST.cFalse, InvCondReg)
				);
				break;
			}
			case mSPO_AST.tMatchNode<tPos> MatchNode: {
				aDefConstructor.MapMatch(MatchNode, aInReg);
				break;
			}
			default: {
				throw mStd.Error(
					$"not implemented: {nameof(mSPO_AST)}.{PatternNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapMatchTest)}(...)"
				);
			}
		}
	}
	
	//================================================================================
	public static void
	MapDef<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tDefNode<tPos> aDefNode
	//================================================================================
	) {
		var ValueReg = aDefConstructor.MapExpresion(aDefNode.Src);
		aDefConstructor.MapMatch(aDefNode.Des, ValueReg);
	}
	
	//================================================================================
	public static void
	MapReturnIf<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tReturnIfNode<tPos> aReturnNode
	//================================================================================
	) {
		aDefConstructor.Commands.Push(
			mIL_AST.ReturnIf(
				aReturnNode.Span,
				aDefConstructor.MapExpresion(aReturnNode.Result),
				aDefConstructor.MapExpresion(aReturnNode.Condition)
			)
		);
	}
	
	//================================================================================
	public static void
	MapRecursiveLambdas<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tRecLambdasNode<tPos> aRecLambdasNode
	//================================================================================
	) {
		var NewDefIndices = mArrayList.List<tInt32>();
		var SPODefNodes = mArrayList.List<mSPO_AST.tRecLambdaItemNode<tPos>>();
		var TempLambdaDefs = mArrayList.List<tDefConstructor<tPos>>();
		var AllUnsolvedSymbols = mArrayList.List<tText>();
		
		var List = aRecLambdasNode.List;
		while (List.Match(out var RecLambdaItemNode, out List)) {
			var NewDefIndex = aDefConstructor.ModuleConstructor.Defs.Size();
			NewDefIndices.Push(NewDefIndex);
			AllUnsolvedSymbols.Push(TempDef(NewDefIndex));
			var TempLambdaDef = NewDefConstructor(aDefConstructor.ModuleConstructor);
			TempLambdaDefs.Push(TempLambdaDef);
			SPODefNodes.Push(RecLambdaItemNode);
		}
		
		var Max = NewDefIndices.Size();
		
		var FuncNames = aRecLambdasNode.List.Map(a => a.Ident.Name).ToArrayList();
		for (var I = 0; I < Max; I += 1) {
			var TempLambdaDef = TempLambdaDefs.Get(I);
			for (var J = 0; J < Max; J += 1) {
				TempLambdaDef.Commands.Push(
					mIL_AST.Call(aRecLambdasNode.Span, FuncNames.Get(J), AllUnsolvedSymbols.Get(J), mIL_AST.cEnv)
				);
			}
		}
		
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempLambdaDef= TempLambdaDefs.Get(I);
			
			TempLambdaDef.InitMapLambda(RecLambdaItemNode.Lambda);
			var KnownSymbols = TempLambdaDef.KnownSymbols.ToLasyList();
			var TempUnsolvedSymbols = TempLambdaDef.UnsolvedSymbols.ToLasyList(
			).Where(
				aUnsolved => (
					KnownSymbols.All(_ => _ != aUnsolved) &&
					aRecLambdasNode.List.All(_ => _.Ident.Name != aUnsolved)
				)
			);
			
			while (TempUnsolvedSymbols.Match(out var Symbol, out TempUnsolvedSymbols)) {
				AllUnsolvedSymbols.Push(Symbol);
			}
		}
		
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempDefConstructor = TempLambdaDefs.Get(I);
			
			TempDefConstructor.FinishMapProc(
				RecLambdaItemNode.Span,
				AllUnsolvedSymbols
			);
			var DefIndex = TempDefConstructor.Index;
			
			aDefConstructor.ModuleConstructor.Defs.Set(
				DefIndex,
				aDefConstructor.ModuleConstructor.Defs.Get(DefIndex)
			);
			
			var FuncName = RecLambdaItemNode.Ident.Name;
			
			var ArgReg = mIL_AST.cEmpty;
			if (!AllUnsolvedSymbols.IsEmpty()) {
				var Iterator = AllUnsolvedSymbols.ToLasyList();
				while (Iterator.Match(out tText UnsolvedSymbol, out Iterator)) {
					if (aDefConstructor.UnsolvedSymbols.ToLasyList().All(_ => _ != UnsolvedSymbol)) {
						aDefConstructor.UnsolvedSymbols.Push(UnsolvedSymbol);
					}
				}
				
				Iterator = AllUnsolvedSymbols.ToLasyList().Reverse();
				while (Iterator.Match(out var Symbol_, out Iterator)) {
					var NewArgReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.CreatePair<tPos>(RecLambdaItemNode.Span, NewArgReg, Symbol_, ArgReg)
					);
					ArgReg = NewArgReg;
				}
			}
			
			aDefConstructor.Commands.Push(
				mIL_AST.Call(RecLambdaItemNode.Span, FuncName, TempDef(DefIndex), ArgReg)
			);
		}
	}
	
	//================================================================================
	public static void
	MapVar<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tDefVarNode<tPos> aVarNode
	//================================================================================
	) {
		aDefConstructor.Commands.Push(
			mIL_AST.VarDef(
				aVarNode.Span,
				aVarNode.Ident.Name,
				aDefConstructor.MapExpresion(aVarNode.Expression)
			)
		);
	}
	
	//================================================================================
	public static void
	MapMethodCalls<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMethodCallsNode<tPos> aMethodCallsNode
	//================================================================================
	) {
		var Rest = aMethodCallsNode.MethodCalls;
		var Object = aDefConstructor.MapExpresion(aMethodCallsNode.Object);
		while (Rest.Match(out var Call, out Rest)) {
			var Arg = aDefConstructor.MapExpresion(Call.Argument);
			var MethodName = Call.Method.Name;
			if (MethodName == "_=...") {
				aDefConstructor.Commands.Push(mIL_AST.VarSet(aMethodCallsNode.Span, Object, Arg));
				continue;
			}
			tText Result;
			if (Call.Result == null) {
				Result = mIL_AST.cEmpty;
			} else {
				Result = aDefConstructor.CreateTempReg();
			}
			aDefConstructor.Commands.Push(
				mIL_AST.Push(aMethodCallsNode.Object.Span, Object),
				mIL_AST.Exec(aMethodCallsNode.Span, Result, MethodName, Arg),
				mIL_AST.Pop(mStd.Merge(aMethodCallsNode.Object.Span, aMethodCallsNode.Span))
			);
			if (Call.Result != null) {
				aDefConstructor.MapMatch(Call.Result.Value, Result);
			}
			
			var KnownSymbols = aDefConstructor.KnownSymbols.ToLasyList();
			if (KnownSymbols.All(_ => _ != MethodName)) {
				aDefConstructor.UnsolvedSymbols.Push(MethodName);
			}
		}
	}
	
	//================================================================================
	public static void
	MapCommand<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tCommandNode<tPos> aCommandNode
	//================================================================================
	) {
		switch (aCommandNode) {
			case mSPO_AST.tDefNode<tPos> DefNode: {
				aDefConstructor.MapDef(DefNode);
				break;
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdasNode: {
				aDefConstructor.MapRecursiveLambdas(RecLambdasNode);
				break;
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnNode: {
				aDefConstructor.MapReturnIf(ReturnNode);
				break;
			}
			case mSPO_AST.tDefVarNode<tPos> VarNode: {
				aDefConstructor.MapVar(VarNode);
				break;
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCallsNode: {
				aDefConstructor.MapMethodCalls(MethodCallsNode);
				break;
			}
			default: {
				throw mStd.Error("impossible");
			}
		}
	}
	
	//================================================================================
	public static tModuleConstructor<tPos>
	MapModule<tPos>(
		mSPO_AST.tModuleNode<tPos> aModuleNode
	//================================================================================
	) {
		var ModuleConstructor = NewModuleConstructor<tPos>();
		var TempLambdaDef = NewDefConstructor(ModuleConstructor);
		TempLambdaDef.InitMapLambda(
			mSPO_AST.Lambda(
				aModuleNode.Span,
				aModuleNode.Import.Match,
				mSPO_AST.Block(
					mStd.Merge(
						aModuleNode.Commands?.First().Span ?? default,
						aModuleNode.Export.Span
					),
					mList.Concat(
						aModuleNode.Commands,
						mList.List<mSPO_AST.tCommandNode<tPos>>(
							mSPO_AST.ReturnIf(
								aModuleNode.Export.Span,
								aModuleNode.Export.Expression,
								mSPO_AST.True(aModuleNode.Export.Span)
							)
						)
					)
				)
			)
		);
		mDebug.AssertEq(
			TempLambdaDef.UnsolvedSymbols.Size(),
			ModuleConstructor.Defs.Size() - 1
		);
		
		var DefSymbols = mArrayList.List<tText>();
		for (var I = 1; I < ModuleConstructor.Defs.Size(); I += 1) {
			DefSymbols.Push(TempDef(I));
		}
		TempLambdaDef.FinishMapProc(aModuleNode.Span, DefSymbols);
		
		return ModuleConstructor;
	}
}