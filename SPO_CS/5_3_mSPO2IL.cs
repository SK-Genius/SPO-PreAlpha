using tBool = System.Boolean;

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

public static class mSPO2IL {
	public struct tModuleConstructor {
		public mArrayList.tArrayList<mArrayList.tArrayList<mIL_AST.tCommandNode>> Defs;
	}
	
	public struct tDefConstructor {
		public mArrayList.tArrayList<mIL_AST.tCommandNode> Commands;
		public tInt32 LastTempReg;
		public mArrayList.tArrayList<tText> KnownSymbols;
		public mArrayList.tArrayList<tText> UnsolvedSymbols;
		public tInt32 Index;
		public tModuleConstructor ModuleConstructor;
	}
	
	//================================================================================
	public static tModuleConstructor
	NewModuleConstructor (
	//================================================================================
	) => new tModuleConstructor {
		Defs = mArrayList.List<mArrayList.tArrayList<mIL_AST.tCommandNode>>()
	};
	
	//================================================================================
	public static tDefConstructor
	NewDefConstructor (
		tModuleConstructor aModuleConstructor
	//================================================================================
	) {
		var DefIndex = aModuleConstructor.Defs.Size();
		var Commands = mArrayList.List<mIL_AST.tCommandNode>();
		aModuleConstructor.Defs.Push(Commands);
		return new tDefConstructor {
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
	
	public static tText
	CreateTempReg(
		ref tDefConstructor aDefConstructor
	) {
		aDefConstructor.LastTempReg += 1;
		return TempReg(aDefConstructor.LastTempReg);
	}
	
	//================================================================================
	public static mArrayList.tArrayList<mIL_AST.tCommandNode>
	UnrollList(
		ref tDefConstructor aDefConstructor,
		tText aReg,
		mList.tList<tText> aSymbols
	//================================================================================
	) {
		var ExtractEnv = mArrayList.List<mIL_AST.tCommandNode>();
		switch (aSymbols.Take(2).ToArrayList().Size()) {
			case 0: {
				break;
			}
			case 1: {
				mStd.Assert(aSymbols.Match(out var Head, out var _));
				ExtractEnv.Push(mIL_AST.Alias(Head, aReg));
				break;
			}
			default: {
				var RestEnv = aReg;
				while (aSymbols.Match(out var Symbol, out aSymbols)) {
					ExtractEnv.Push(mIL_AST.GetFirst(Symbol, RestEnv));
					var NewRestEnv = CreateTempReg(ref aDefConstructor);
					ExtractEnv.Push(mIL_AST.GetSecond(NewRestEnv, RestEnv));
					RestEnv = NewRestEnv;
				}
				break;
			}
		}
		return ExtractEnv;
	}
	
	//================================================================================
	public static void
	InitMapLambda(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tLambdaNode aLambdaNode
	//================================================================================
	) {
		var ArgumentSymbols = mList.List<tText>();
		MapMatch(ref aDefConstructor, aLambdaNode.Head, mIL_AST.cArg);
		
		var ResultReg = MapExpresion(ref aDefConstructor, aLambdaNode.Body);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf(ResultReg, mIL_AST.cTrue));
		var KnownSymbols = aDefConstructor.KnownSymbols.ToLasyList();
		var NewUnsolvedSymbols = aDefConstructor.UnsolvedSymbols.ToLasyList(
		).Where(
			S1 => KnownSymbols.Where(S2 => S1 == S2).IsEmpty()
		).ToArrayList();
		aDefConstructor.UnsolvedSymbols = NewUnsolvedSymbols;
	}
	
	//================================================================================
	public static void
	InitMapMethod(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tMethodNode aMethodNode
	//================================================================================
	) {
		var ArgumentSymbols = mList.List<tText>();
		MapMatch(ref aDefConstructor, aMethodNode.Arg, mIL_AST.cArg);
		MapMatch(ref aDefConstructor, aMethodNode.Obj, mIL_AST.cObj);
		
		var ResultReg = MapExpresion(ref aDefConstructor, aMethodNode.Body);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf(ResultReg, mIL_AST.cTrue));
		var KnownSymbols = aDefConstructor.KnownSymbols.ToLasyList();
		var NewUnsolvedSymbols = aDefConstructor.UnsolvedSymbols.ToLasyList(
		).Where(
			S1 => KnownSymbols.Where(S2 => S1 == S2).IsEmpty()
		).ToArrayList();
		aDefConstructor.UnsolvedSymbols = NewUnsolvedSymbols;
	}
	
	//================================================================================
	public static void
	FinishMapProc(
		ref tDefConstructor aTempDefConstructor,
		mArrayList.tArrayList<tText> aUnsolveSymbols
	//================================================================================
	) {
		var Def = mArrayList.Concat(
			UnrollList(
				ref aTempDefConstructor,
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
	MapLambda(
		ref tModuleConstructor aModuleConstructor,
		mSPO_AST.tLambdaNode aLambdaNode
	//================================================================================
	) {
		var TempLambdaDef = NewDefConstructor(aModuleConstructor);
		InitMapLambda(ref TempLambdaDef, aLambdaNode);
		FinishMapProc(
			ref TempLambdaDef,
			TempLambdaDef.UnsolvedSymbols
		);
		return (TempLambdaDef.Index, TempLambdaDef.UnsolvedSymbols);
	}
	
	//================================================================================
	public static (tInt32, mArrayList.tArrayList<tText>)
	MapMethod(
		ref tModuleConstructor aModuleConstructor,
		mSPO_AST.tMethodNode aMethodNode
	//================================================================================
	) {
		var TempMethodDef = NewDefConstructor(aModuleConstructor);
		InitMapMethod(ref TempMethodDef, aMethodNode);
		FinishMapProc(
			ref TempMethodDef,
			TempMethodDef.UnsolvedSymbols
		);
		return (TempMethodDef.Index, TempMethodDef.UnsolvedSymbols);
	}
	
	//================================================================================
	public static tText
	InitProc(
		ref tDefConstructor aDefConstructor,
		tText aDefName,
		mArrayList.tArrayList<tText> aEnv
	//================================================================================
	) {
		var ArgReg = mIL_AST.cEmpty;
		if (!aEnv.IsEmpty()) {
			var UnsolvedSymbols = aEnv.ToLasyList();
			while (UnsolvedSymbols.Match(out var Symbol, out UnsolvedSymbols)) {
				if (aDefConstructor.UnsolvedSymbols.ToLasyList().Where(S => S == Symbol).IsEmpty()) {
					aDefConstructor.UnsolvedSymbols.Push(Symbol);
				}
			}
			
			if (aEnv.Size() == 1) {
				ArgReg = aEnv.Get(0);
			} else {
				UnsolvedSymbols = aEnv.ToLasyList().Reverse();
				while (UnsolvedSymbols.Match(out var Symbol_, out UnsolvedSymbols)) {
					var NewArgReg = CreateTempReg(ref aDefConstructor);
					aDefConstructor.Commands.Push(mIL_AST.CreatePair(NewArgReg, Symbol_, ArgReg));
					ArgReg = NewArgReg;
				}
			}
		}
		
		var Proc = CreateTempReg(ref aDefConstructor);
		aDefConstructor.Commands.Push(mIL_AST.Call(Proc, aDefName, ArgReg));
		aDefConstructor.UnsolvedSymbols.Push(aDefName);
		return Proc;
	}
	
	//================================================================================
	public static tText
	MapExpresion(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tExpressionNode aExpressionNode
	//================================================================================
	) {
		switch (aExpressionNode) {
			case mSPO_AST.tEmptyNode EmptyNode: {
				return mIL_AST.cEmpty;
			}
			case mSPO_AST.tFalseNode FalseNode: {
				return mIL_AST.cFalse;
			}
			case mSPO_AST.tTrueNode TrueNode: {
				return mIL_AST.cTrue;
			}
			case mSPO_AST.tNumberNode NumberNode: {
				var ResultReg = CreateTempReg(ref aDefConstructor);
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(
						ResultReg, NumberNode.Value.ToString()
					)
				);
				return ResultReg;
			}
			case mSPO_AST.tIdentNode IdentNode: {
				if (
					aDefConstructor.UnsolvedSymbols.ToLasyList(
					).Where(
						a => a == IdentNode.Name
					).IsEmpty() &&
					aDefConstructor.Commands.ToLasyList(
					).Where(
						a => a.TryGetResultReg(out var Name) ? Name == IdentNode.Name : false
					).IsEmpty()
				) {
					aDefConstructor.UnsolvedSymbols.Push(IdentNode.Name);
				}
				return IdentNode.Name;
			}
			case mSPO_AST.tCallNode CallNode: {
				var FuncReg = MapExpresion(ref aDefConstructor, CallNode.Func);
				var ArgReg = MapExpresion(ref aDefConstructor, CallNode.Arg);
				var ResultReg = CreateTempReg(ref aDefConstructor);
				aDefConstructor.Commands.Push(
					mIL_AST.Call(ResultReg, FuncReg, ArgReg)
				);
				return ResultReg;
			}
			case mSPO_AST.tTupleNode TupleNode: {
				switch (TupleNode.Items.Take(2).ToArrayList().Size()) {
					case 0: {
						throw null;
					}
					case 1: {
						mStd.Assert(TupleNode.Items.Match(out var Head, out var _));
						return MapExpresion(ref aDefConstructor, Head);
					}
					default: {
						var List = TupleNode.Items.Reverse();
						var ResultReg = mIL_AST.cEmpty;
						while (List.Match(out var Item, out List)) {
							var ItemReg = MapExpresion(ref aDefConstructor, Item);
							var TupleReg = CreateTempReg(ref aDefConstructor);
							aDefConstructor.Commands.Push(
								mIL_AST.CreatePair(
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
			case mSPO_AST.tPrefixNode PrefixNode: {
				var ExpresionReg = MapExpresion(ref aDefConstructor, PrefixNode.Element);
				var ResultReg = CreateTempReg(ref aDefConstructor);
				aDefConstructor.Commands.Push(
					mIL_AST.AddPrefix(
						ResultReg,
						PrefixNode.Prefix,
						ExpresionReg
					)
				);
				return ResultReg;
			}
			case mSPO_AST.tTextNode TextNode: {
				throw null;
			}
			case mSPO_AST.tLambdaNode LambdaNode: {
				var(NewDefIndex, UnsolvedSymbols) = MapLambda(
					ref aDefConstructor.ModuleConstructor,
					LambdaNode
				);
				return InitProc(
					ref aDefConstructor,
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			case mSPO_AST.tMethodNode MethodNode: {
				var(NewDefIndex, UnsolvedSymbols) = MapMethod(
					ref aDefConstructor.ModuleConstructor,
					MethodNode
				);
				return InitProc(
					ref aDefConstructor,
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			case mSPO_AST.tBlockNode BlockNode: {
				var CommandNodes = BlockNode.Commands;
				while (CommandNodes.Match(out var CommandNode, out CommandNodes)) {
					MapCommand(ref aDefConstructor, CommandNode);
				}
				// TODO: remove created symbols from unknown symbols
				return null;
			}
			case mSPO_AST.tIfNode IfNode: {
				var Ifs = mArrayList.List<mSPO_AST.tCommandNode>();
				var Pairs = IfNode.Cases;
				while (Pairs.Match(out var Pair, out Pairs)) {
					var (Test, Run) = Pair;
					Ifs.Push(mSPO_AST.ReturnIf(Run, Test));
				}
				Ifs.Push(mSPO_AST.ReturnIf(mSPO_AST.Empty(), mSPO_AST.True())); // TODO: ASSERT FALSE
				
				var ResultReg = CreateTempReg(ref aDefConstructor);
				
				MapCommand(
					ref aDefConstructor,
					mSPO_AST.Def(
						mSPO_AST.Match(
							new mSPO_AST.tIdentNode{Name = ResultReg},
							null
						),
						mSPO_AST.Call(
							mSPO_AST.Lambda(
								mSPO_AST.Match(
									mSPO_AST.Empty(),
									null
								),
								mSPO_AST.Block(Ifs.ToLasyList())
							),
							mSPO_AST.Empty()
						)
					)
				);
				return ResultReg; 
			}
			case mSPO_AST.tIfMatchNode IfMatchNode: {
				var Imput = new mSPO_AST.tIdentNode{
					Name = MapExpresion(ref aDefConstructor, IfMatchNode.Expression)
				};
				
				var ModuleConstructor = aDefConstructor.ModuleConstructor;
				var KnownSymbols = aDefConstructor.KnownSymbols.ToLasyList();
				
				var Ifs = mArrayList.List<mSPO_AST.tCommandNode>();
				
				var Rest = IfMatchNode.Cases;
				while (Rest.Match(out var Case, out Rest)) {
					var (Match, Run) = Case;
					var TestDef = NewDefConstructor(ModuleConstructor);
					
					MapMatchTest(ref TestDef, mIL_AST.cArg, Match);
					FinishMapProc(ref TestDef, TestDef.UnsolvedSymbols);
					TestDef.Commands.Push(mIL_AST.ReturnIf(mIL_AST.cTrue, mIL_AST.cTrue));
					
					Ifs.Push(
						mSPO_AST.ReturnIf(
							mSPO_AST.Call(
								mSPO_AST.Lambda(Match, Run),
								Imput
							),
							mSPO_AST.Call(
								mSPO_AST.Call(
									new mSPO_AST.tIdentNode{ Name = TempDef(TestDef.Index) },
									mSPO_AST.Tuple(
										TestDef.UnsolvedSymbols.ToLasyList(
										).Map(
											Symbol => (mSPO_AST.tExpressionNode)new mSPO_AST.tIdentNode{ Name = Symbol }
										)
									)
								),
								Imput
							)
						)
					);
				}
				Ifs.Push(mSPO_AST.ReturnIf(mSPO_AST.Empty(), mSPO_AST.True())); // TODO: ASSERT FALSE
				
				var ResultReg = CreateTempReg(ref aDefConstructor);
				
				MapCommand(
					ref aDefConstructor,
					mSPO_AST.Def(
						mSPO_AST.Match(
							new mSPO_AST.tIdentNode{Name = ResultReg},
							null
						),
						mSPO_AST.Call(
							mSPO_AST.Lambda(
								mSPO_AST.Match(
									mSPO_AST.Empty(),
									null
								),
								mSPO_AST.Block(Ifs.ToLasyList())
							),
							mSPO_AST.Empty()
						)
					)
				);
				return ResultReg; 
			}
			case mSPO_AST.tVarToValNode VarToValNode: {
				var ObjReg = MapExpresion(ref aDefConstructor, VarToValNode.Obj);
				var ResultReg = CreateTempReg(ref aDefConstructor);
				aDefConstructor.Commands.Push(mIL_AST.VarGet(ResultReg, ObjReg));
				return ResultReg;
			}
			default: {
				throw new System.Exception(
					$"not implemented: case {nameof(mSPO_AST)}.{aExpressionNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapExpresion)}(...)"
				);
			}
		}
	}
	
	//================================================================================
	public static void
	MapMatch(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tMatchNode aMatchNode,
		tText aReg
	//================================================================================
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			case mSPO_AST.tEmptyNode EmptyNode: {
				break;
			}
			case mSPO_AST.tIdentNode IdentNode: {
				aDefConstructor.Commands.Push(mIL_AST.Alias(IdentNode.Name, aReg));
				aDefConstructor.KnownSymbols.Push(IdentNode.Name);
				break;
			}
			case mSPO_AST.tMatchPrefixNode PrefixNode: {
				var ResultReg = CreateTempReg(ref aDefConstructor);
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(
						ResultReg,
						PrefixNode.Prefix,
						aReg
					)
				);
				MapMatch(
					ref aDefConstructor,
					PrefixNode.Match,
					ResultReg
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode TupleNode: {
				var RestReg = aReg;
				var Items = TupleNode.Items;
				mStd.AssertEq(Items.Take(2).ToArrayList().Size(), 2);
				while (Items.Match(out var Item, out Items)) {
					var ItemReg = CreateTempReg(ref aDefConstructor);
					aDefConstructor.Commands.Push(mIL_AST.GetFirst(ItemReg, RestReg));
					
					MapMatch(ref aDefConstructor, Item, ItemReg);
					
					var NewRestReg = CreateTempReg(ref aDefConstructor);
					aDefConstructor.Commands.Push(mIL_AST.GetSecond(NewRestReg, RestReg));
					RestReg = NewRestReg;
				}
				break;
			}
			case mSPO_AST.tMatchGuardNode GuardNode: {
				// TODO: ASSERT GuardNode._Guard
				MapMatch(ref aDefConstructor, GuardNode.Match, aReg);
				break;
			}
			default: {
				throw new System.Exception(
					$"not implemented: {nameof(mSPO_AST)}.{PatternNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapMatch)}(...)"
				);
			}
		}
	}
	
	//================================================================================
	public static void
	MapMatchTest(
		ref tDefConstructor aDefConstructor,
		tText aInReg,
		mSPO_AST.tMatchNode aMatchNode
	//================================================================================
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			case mSPO_AST.tIdentNode IdentNode: {
				if (
					aDefConstructor.KnownSymbols.ToLasyList(
					).Where(
						Symbol => Symbol == IdentNode.Name
					).IsEmpty()
				) {
					aDefConstructor.Commands.Push(
						mIL_AST.Alias(IdentNode.Name, aInReg)
					);
				} else {
					throw null;
				}
				break;
			}
			case mSPO_AST.tMatchPrefixNode PrefixNode: {
				var Prefix = PrefixNode.Prefix;
				var SubMatch = PrefixNode.Match;
				var Reg = CreateTempReg(ref aDefConstructor);
				var InvReg = CreateTempReg(ref aDefConstructor);
				var SubValue = CreateTempReg(ref aDefConstructor);
				aDefConstructor.Commands.Push(
					mIL_AST.HasPrefix(Reg, Prefix, aInReg),
					mIL_AST.XOr(InvReg, Reg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(mIL_AST.cFalse, InvReg),
					mIL_AST.SubPrefix(SubValue, Prefix, aInReg)
				);
				MapMatchTest(ref aDefConstructor, SubValue, SubMatch);
				break;
			}
			case mSPO_AST.tMatchGuardNode GuardNode: {
				var SubMatch = GuardNode.Match;
				var Guard = GuardNode.Guard;
				
				MapMatchTest(ref aDefConstructor, aInReg, SubMatch);
				
				var InvReg = CreateTempReg(ref aDefConstructor);
				
				var TestReg = MapExpresion(ref aDefConstructor, Guard);
				aDefConstructor.Commands.Push(
					mIL_AST.XOr(InvReg, TestReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(mIL_AST.cFalse, InvReg)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode TupleNode: {
				var Items = TupleNode.Items;
				while (Items.Match(out var Item, out Items)) {
					var Reg = CreateTempReg(ref aDefConstructor);
					var NotReg = CreateTempReg(ref aDefConstructor);
					
					MapMatchTest(ref aDefConstructor, aInReg, Item);
					aDefConstructor.Commands.Push(
						mIL_AST.XOr(NotReg, Reg, mIL_AST.cTrue),
						mIL_AST.ReturnIf(mIL_AST.cFalse, NotReg)
					);
				}
				break;
			}
			case mSPO_AST.tNumberNode NumberNode: {
				var IntReg = CreateTempReg(ref aDefConstructor);
				var CondReg = CreateTempReg(ref aDefConstructor);
				var InvCondReg = CreateTempReg(ref aDefConstructor);
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(IntReg, $"{NumberNode.Value}"),
					mIL_AST.IntsAreEq(CondReg, aInReg, IntReg),
					mIL_AST.XOr(InvCondReg, CondReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(mIL_AST.cFalse, InvCondReg)
				);
				break;
			}
			default: {
				throw new System.Exception(
					$"not implemented: {nameof(mSPO_AST)}.{PatternNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapMatchTest)}(...)"
				);
			}
		}
	}
	
	//================================================================================
	public static void
	MapDef(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tDefNode aDefNode
	//================================================================================
	) {
		var ValueReg = MapExpresion(ref aDefConstructor, aDefNode.Src);
		MapMatch(ref aDefConstructor, aDefNode.Des, ValueReg);
	}
	
	//================================================================================
	public static void
	MapReturnIf(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tReturnIfNode aReturnNode
	//================================================================================
	) {
		aDefConstructor.Commands.Push(
			mIL_AST.ReturnIf(
				MapExpresion(ref aDefConstructor, aReturnNode.Result),
				MapExpresion(ref aDefConstructor, aReturnNode.Condition)
			)
		);
	}
	
	//================================================================================
	public static void
	MapRecursiveLambdas(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tRecLambdasNode aRecLambdasNode
	//================================================================================
	) {
		var NewDefIndices = mArrayList.List<tInt32>();
		var SPODefNodes = mArrayList.List<mSPO_AST.tRecLambdaItemNode>();
		var TempLambdaDefs = mArrayList.List<tDefConstructor>();
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
					mIL_AST.Call(FuncNames.Get(J), AllUnsolvedSymbols.Get(J), mIL_AST.cEnv)
				);
			}
		}
		
		for (var I = 0; I < Max; I += 1) {
			var DefIndex = NewDefIndices.Get(I);
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempLambdaDef= TempLambdaDefs.Get(I);
			
			InitMapLambda(ref TempLambdaDef, RecLambdaItemNode.Lambda);
			var KnownSymbols = TempLambdaDef.KnownSymbols.ToLasyList();
			var TempUnsolvedSymbols = TempLambdaDef.UnsolvedSymbols.ToLasyList(
			).Where(
				aUnsolved => (
					KnownSymbols.Where(aKnown => aKnown == aUnsolved).IsEmpty() &&
					aRecLambdasNode.List.Where(
						aRecLambda => aRecLambda.Ident.Name == aUnsolved
					).IsEmpty()
				)
			);
			
			while (TempUnsolvedSymbols.Match(out var Symbol, out TempUnsolvedSymbols)) {
				AllUnsolvedSymbols.Push(Symbol);
			}
		}
		
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempDefConstructor = TempLambdaDefs.Get(I);
			
			var Reg = CreateTempReg(ref TempDefConstructor);
			FinishMapProc(
				ref TempDefConstructor,
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
					if (
						aDefConstructor.UnsolvedSymbols.ToLasyList(
						).Where(
							a => a == UnsolvedSymbol
						).IsEmpty()
					) {
						aDefConstructor.UnsolvedSymbols.Push(UnsolvedSymbol);
					}
				}
				
				Iterator = AllUnsolvedSymbols.ToLasyList().Reverse();
				while (Iterator.Match(out var Symbol_, out Iterator)) {
					var NewArgReg = CreateTempReg(ref aDefConstructor);
					aDefConstructor.Commands.Push(mIL_AST.CreatePair(NewArgReg, Symbol_, ArgReg));
					ArgReg = NewArgReg;
				}
			}
			
			aDefConstructor.Commands.Push(mIL_AST.Call(FuncName, TempDef(DefIndex), ArgReg));
		}
	}
	
	//================================================================================
	public static void
	MapVar(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tDefVarNode aVarNode
	//================================================================================
	) {
		aDefConstructor.Commands.Push(
			mIL_AST.VarDef(
				aVarNode.Ident.Name,
				MapExpresion(ref aDefConstructor, aVarNode.Expression)
			)
		);
	}
	
	//================================================================================
	public static void
	MapMethodCalls(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tMethodCallsNode aMethodCallsNode
	//================================================================================
	) {
		var Rest = aMethodCallsNode.MethodCalls;
		var Object = MapExpresion(ref aDefConstructor, aMethodCallsNode.Object);
		while (Rest.Match(out var Call, out Rest)) {
			var Arg = MapExpresion(ref aDefConstructor, Call.Argument);
			var MethodName = Call.Method.Name;
			if (MethodName == "_=...") {
				aDefConstructor.Commands.Push(mIL_AST.VarSet(Object, Arg));
				continue;
			}
			tText Result;
			if (Call.Result == null) {
				Result = mIL_AST.cEmpty;
			} else {
				Result = CreateTempReg(ref aDefConstructor);
			}
			aDefConstructor.Commands.Push(
				mIL_AST.Push(Object),
				mIL_AST.Exec(Result, MethodName, Arg),
				mIL_AST.Pop()
			);
			if (Call.Result != null) {
				MapMatch(ref aDefConstructor, Call.Result.Value, Result);
			}
			
			var KnownSymbols = aDefConstructor.KnownSymbols.ToLasyList();
			if (KnownSymbols.Where(a => MethodName == a).IsEmpty()) {
				aDefConstructor.UnsolvedSymbols.Push(MethodName);
			}
		}
	}
	
	//================================================================================
	public static void
	MapCommand(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tCommandNode aCommandNode
	//================================================================================
	) {
		switch (aCommandNode) {
			case mSPO_AST.tDefNode DefNode: {
				MapDef(ref aDefConstructor, DefNode);
				break;
			}
			case mSPO_AST.tRecLambdasNode RecLambdasNode: {
				MapRecursiveLambdas(ref aDefConstructor, RecLambdasNode);
				break;
			}
			case mSPO_AST.tReturnIfNode ReturnNode: {
				MapReturnIf(ref aDefConstructor, ReturnNode);
				break;
			}
			case mSPO_AST.tDefVarNode VarNode: {
				MapVar(ref aDefConstructor, VarNode);
				break;
			}
			case mSPO_AST.tMethodCallsNode MethodCallsNode: {
				MapMethodCalls(ref aDefConstructor, MethodCallsNode);
				break;
			}
			default: {
				throw null;
			}
		}
	}
	
	//================================================================================
	public static tModuleConstructor
	MapModule(
		mSPO_AST.tModuleNode aModuleNode
	//================================================================================
	) {
		var ModuleConstructor = NewModuleConstructor();
		var TempLambdaDef = NewDefConstructor(ModuleConstructor);
		InitMapLambda(
			ref TempLambdaDef,
			mSPO_AST.Lambda(
				aModuleNode.Import.Match,
				mSPO_AST.Block(
					mList.Concat(
						aModuleNode.Commands,
						mList.List<mSPO_AST.tCommandNode>(
							mSPO_AST.ReturnIf(aModuleNode.Export.Expression, mSPO_AST.True())
						)
					)
				)
			)
		);
		mStd.AssertEq(
			TempLambdaDef.UnsolvedSymbols.Size(),
			ModuleConstructor.Defs.Size() - 1
		);
		
		var DefSymbols = mArrayList.List<tText>();
		for (var I = 1; I < ModuleConstructor.Defs.Size(); I += 1) {
			DefSymbols.Push(TempDef(I));
		}
		FinishMapProc(ref TempLambdaDef, DefSymbols);
		
		return ModuleConstructor;
	}
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO2IL),
		mTest.Test(
			"MapExpresion",
			aStreamOut => {
				mStd.Assert(
					mSPO_Parser.Expression.ParseText(
						"2 .< (4 .+ 3) < 3",
						aStreamOut
					).Match(out mSPO_AST.tExpressionNode ExpressionNode)
				);
				
				var Def = NewDefConstructor(NewModuleConstructor());
				mStd.AssertEq(MapExpresion(ref Def, ExpressionNode), TempReg(11));
				
				mStd.AssertEq(
					Def.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(TempReg(1), "3"),
						mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(TempReg(3), "3"),
						mIL_AST.CreatePair(TempReg(4), TempReg(3), mIL_AST.cEmpty),
						mIL_AST.CreateInt(TempReg(5), "4"),
						mIL_AST.CreatePair(TempReg(6), TempReg(5), TempReg(4)),
						mIL_AST.Call(TempReg(7), Ident("...+..."), TempReg(6)),
						mIL_AST.CreatePair(TempReg(8), TempReg(7), TempReg(2)),
						mIL_AST.CreateInt(TempReg(9), "2"),
						mIL_AST.CreatePair(TempReg(10), TempReg(9), TempReg(8)),
						mIL_AST.Call(TempReg(11), Ident("...<...<..."), TempReg(10))
					)
				);
			}
		),
		mTest.Test(
			"MapDef1",
			aStreamOut => {
				mStd.Assert(
					mSPO_Parser.Def.ParseText(
						"§DEF a = (1, 2)",
						aStreamOut
					).Match(out mSPO_AST.tDefNode DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor());
				MapDef(ref DefConstructor, DefNode);
				
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(TempReg(1), "2"),
						mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(TempReg(3), "1"),
						mIL_AST.CreatePair(TempReg(4), TempReg(3), TempReg(2)),
						
						mIL_AST.Alias(Ident("a"), TempReg(4))
					)
				);
			}
		),
		mTest.Test(
			"MapDefMatch",
			aStreamOut => {
				mStd.Assert(
					mSPO_Parser.Def.ParseText(
						"§DEF (a, (b, c)) = (1, (2, 3))",
						aStreamOut
					).Match(out mSPO_AST.tDefNode DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor());
				MapDef(ref DefConstructor, DefNode);
				
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(TempReg(1), "3"),
						mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(TempReg(3), "2"),
						mIL_AST.CreatePair(TempReg(4), TempReg(3), TempReg(2)),
						mIL_AST.CreatePair(TempReg(5), TempReg(4), mIL_AST.cEmpty),
						mIL_AST.CreateInt(TempReg(6), "1"),
						mIL_AST.CreatePair(TempReg(7), TempReg(6), TempReg(5)),
						
						mIL_AST.GetFirst(TempReg(8), TempReg(7)),
						mIL_AST.Alias(Ident("a"), TempReg(8)),
						mIL_AST.GetSecond(TempReg(9), TempReg(7)),
						mIL_AST.GetFirst(TempReg(10), TempReg(9)),
						mIL_AST.GetFirst(TempReg(11), TempReg(10)),
						mIL_AST.Alias(Ident("b"), TempReg(11)),
						mIL_AST.GetSecond(TempReg(12), TempReg(10)),
						mIL_AST.GetFirst(TempReg(13), TempReg(12)),
						mIL_AST.Alias(Ident("c"), TempReg(13)),
						
						mIL_AST.GetSecond(TempReg(14), TempReg(12)),
						mIL_AST.GetSecond(TempReg(15), TempReg(9))
					)
				);
			}
		),
		mTest.Test(
			"MatchTuple",
			aStreamOut => {
				mStd.Assert(
					mSPO_Parser.Def.ParseText(
						"§DEF (a, b, c) = (1, 2, 3)",
						aStreamOut
					).Match(out mSPO_AST.tDefNode DefNode)
				);
				
				var Module = NewDefConstructor(NewModuleConstructor());
				MapDef(ref Module, DefNode);
				
				mStd.AssertEq(
					Module.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(TempReg(1), "3"),
						mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(TempReg(3), "2"),
						mIL_AST.CreatePair(TempReg(4), TempReg(3), TempReg(2)),
						mIL_AST.CreateInt(TempReg(5), "1"),
						mIL_AST.CreatePair(TempReg(6), TempReg(5), TempReg(4)),
						
						mIL_AST.GetFirst(TempReg(7), TempReg(6)),
						mIL_AST.Alias(Ident("a"), TempReg(7)),
						mIL_AST.GetSecond(TempReg(8), TempReg(6)),
						mIL_AST.GetFirst(TempReg(9), TempReg(8)),
						mIL_AST.Alias(Ident("b"), TempReg(9)),
						mIL_AST.GetSecond(TempReg(10), TempReg(8)),
						mIL_AST.GetFirst(TempReg(11), TempReg(10)),
						mIL_AST.Alias(Ident("c"), TempReg(11)),
						mIL_AST.GetSecond(TempReg(12), TempReg(10))
					)
				);
			}
		),
		mTest.Test(
			"MapMatchPrefix",
			aStreamOut => {
				mStd.Assert(
					mSPO_Parser.Def.ParseText(
						"§DEF (a, b, (#bla (c , d))) = (1, 2, (#bla (3, 4)))",
						aStreamOut
					).Match(out mSPO_AST.tDefNode DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor());
				MapDef(ref DefConstructor, DefNode);
				
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(TempReg(1), "4"),
						mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(TempReg(3), "3"),
						mIL_AST.CreatePair(TempReg(4), TempReg(3), TempReg(2)),
						mIL_AST.AddPrefix(TempReg(5), Ident("bla..."), TempReg(4)),
						mIL_AST.CreatePair(TempReg(6), TempReg(5), mIL_AST.cEmpty),
						mIL_AST.CreateInt(TempReg(7), "2"),
						mIL_AST.CreatePair(TempReg(8), TempReg(7), TempReg(6)),
						mIL_AST.CreateInt(TempReg(9), "1"),
						mIL_AST.CreatePair(TempReg(10), TempReg(9), TempReg(8)),
						
						mIL_AST.GetFirst(TempReg(11), TempReg(10)),
						mIL_AST.Alias(Ident("a"), TempReg(11)),
						mIL_AST.GetSecond(TempReg(12), TempReg(10)),
						mIL_AST.GetFirst(TempReg(13), TempReg(12)),
						mIL_AST.Alias(Ident("b"), TempReg(13)),
						mIL_AST.GetSecond(TempReg(14), TempReg(12)),
						mIL_AST.GetFirst(TempReg(15), TempReg(14)),
						mIL_AST.SubPrefix(TempReg(16), Ident("bla..."), TempReg(15)),
						mIL_AST.GetFirst(TempReg(17), TempReg(16)),
						mIL_AST.Alias(Ident("c"), TempReg(17)),
						mIL_AST.GetSecond(TempReg(18), TempReg(16)),
						mIL_AST.GetFirst(TempReg(19), TempReg(18)),
						mIL_AST.Alias(Ident("d"), TempReg(19)),
						
						mIL_AST.GetSecond(TempReg(20), TempReg(18)),
						mIL_AST.GetSecond(TempReg(21), TempReg(14))
					)
				);
			}
		),
		mTest.Test(
			"MapLambda1",
			aStreamOut => {
				mStd.Assert(
					mSPO_Parser.Def.ParseText(
						"§DEF x = a => 2 .* a",
						aStreamOut
					).Match(out mSPO_AST.tDefNode DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor());
				MapDef(ref DefConstructor, DefNode);
				
				mStd.AssertEq(DefConstructor.ModuleConstructor.Defs.Size(), 2);
				mStd.AssertEq(
					DefConstructor.ModuleConstructor.Defs.Get(1),
					mArrayList.List(
						mIL_AST.Alias(Ident("...*..."), mIL_AST.cEnv),
						
						mIL_AST.Alias(Ident("a"), mIL_AST.cArg),
						
						mIL_AST.CreatePair(TempReg(1), Ident("a"), mIL_AST.cEmpty),
						mIL_AST.CreateInt(TempReg(2), "2"),
						mIL_AST.CreatePair(TempReg(3), TempReg(2), TempReg(1)),
						mIL_AST.Call(TempReg(4), Ident("...*..."), TempReg(3)),
						mIL_AST.ReturnIf(TempReg(4), mIL_AST.cTrue)
					)
				);
				
				mStd.AssertEq(DefConstructor.Index, 0);
				
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.Call(TempReg(1), TempDef(1), Ident("...*...")),
						mIL_AST.Alias(Ident("x"), TempReg(1))
					)
				);
				
				mStd.AssertEq(
					DefConstructor.UnsolvedSymbols,
					mArrayList.List(Ident("...*..."), TempDef(1))
				);
			}
		),
		mTest.Test(
			"MapLambda2",
			aStreamOut => {
				mStd.Assert(
					mSPO_Parser.Def.ParseText(
						"§DEF ...*...+... = (a, b, c) => (a .* b) .+ c",
						aStreamOut
					).Match(out mSPO_AST.tDefNode DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor());
				MapDef(ref DefConstructor, DefNode);
				
				mStd.AssertEq(DefConstructor.ModuleConstructor.Defs.Size(), 2);
				mStd.AssertEq(
					DefConstructor.ModuleConstructor.Defs.Get(1),
					mArrayList.List(
						mIL_AST.GetFirst(Ident("...+..."), mIL_AST.cEnv),
						mIL_AST.GetSecond(TempReg(13), mIL_AST.cEnv),
						mIL_AST.GetFirst(Ident("...*..."), TempReg(13)),
						mIL_AST.GetSecond(TempReg(14), TempReg(13)),
						
						mIL_AST.GetFirst(TempReg(1), mIL_AST.cArg),
						mIL_AST.Alias(Ident("a"), TempReg(1)),
						mIL_AST.GetSecond(TempReg(2), mIL_AST.cArg),
						mIL_AST.GetFirst(TempReg(3), TempReg(2)),
						mIL_AST.Alias(Ident("b"), TempReg(3)),
						mIL_AST.GetSecond(TempReg(4), TempReg(2)),
						mIL_AST.GetFirst(TempReg(5), TempReg(4)),
						mIL_AST.Alias(Ident("c"), TempReg(5)),
						mIL_AST.GetSecond(TempReg(6), TempReg(4)),
						
						mIL_AST.CreatePair(TempReg(7), Ident("c"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(TempReg(8), Ident("b"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(TempReg(9), Ident("a"), TempReg(8)),
						mIL_AST.Call(TempReg(10), Ident("...*..."), TempReg(9)),
						mIL_AST.CreatePair(TempReg(11), TempReg(10), TempReg(7)),
						mIL_AST.Call(TempReg(12), Ident("...+..."), TempReg(11)),
						mIL_AST.ReturnIf(TempReg(12), mIL_AST.cTrue)
					)
				);
				
				mStd.AssertEq(DefConstructor.Index, 0);
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.CreatePair(TempReg(1), Ident("...*..."), mIL_AST.cEmpty),
						mIL_AST.CreatePair(TempReg(2), Ident("...+..."), TempReg(1)),
						mIL_AST.Call(TempReg(3), TempDef(1), TempReg(2)),
						mIL_AST.Alias(Ident("...*...+..."), TempReg(3))
					)
				);
				
					mStd.AssertEq(
					DefConstructor.UnsolvedSymbols,
					mArrayList.List(Ident("...+..."), Ident("...*..."), TempDef(1))
				);
			}
		),
		mTest.Test(
			"MapNestedMatch",
			aStreamOut => {
				mStd.Assert(
					mSPO_Parser.Lambda.ParseText(
						"(a, b, (x, y, z)) => a .* z",
						aStreamOut
					).Match(out mSPO_AST.tLambdaNode LambdaNode)
				);
				
				var ModuleConstructor = NewModuleConstructor();
				var (DefIndex, UnsolvedSymbols) = MapLambda(
					ref ModuleConstructor,
					LambdaNode
				);
				
				mStd.AssertEq(ModuleConstructor.Defs.Size(), 1);
				mStd.AssertEq(DefIndex, 0);
				mStd.AssertEq(
					ModuleConstructor.Defs.Get(DefIndex),
					mArrayList.List(
						mIL_AST.Alias(Ident("...*..."), mIL_AST.cEnv),
							
						mIL_AST.GetFirst(TempReg(1), mIL_AST.cArg),
						mIL_AST.Alias(Ident("a"), TempReg(1)),
						mIL_AST.GetSecond(TempReg(2), mIL_AST.cArg),
						mIL_AST.GetFirst(TempReg(3), TempReg(2)),
						mIL_AST.Alias(Ident("b"), TempReg(3)),
						mIL_AST.GetSecond(TempReg(4), TempReg(2)),
						mIL_AST.GetFirst(TempReg(5), TempReg(4)),
						mIL_AST.GetFirst(TempReg(6), TempReg(5)),
						mIL_AST.Alias(Ident("x"), TempReg(6)),
						mIL_AST.GetSecond(TempReg(7), TempReg(5)),
						mIL_AST.GetFirst(TempReg(8), TempReg(7)),
						mIL_AST.Alias(Ident("y"), TempReg(8)),
						mIL_AST.GetSecond(TempReg(9), TempReg(7)),
						mIL_AST.GetFirst(TempReg(10), TempReg(9)),
						mIL_AST.Alias(Ident("z"), TempReg(10)),
						mIL_AST.GetSecond(TempReg(11), TempReg(9)),
						mIL_AST.GetSecond(TempReg(12), TempReg(4)),
						
						mIL_AST.CreatePair(TempReg(13), Ident("z"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(TempReg(14), Ident("a"), TempReg(13)),
						mIL_AST.Call(TempReg(15), Ident("...*..."), TempReg(14)),
						mIL_AST.ReturnIf(TempReg(15), mIL_AST.cTrue)
					)
				);
				
				mStd.AssertEq(
					UnsolvedSymbols,
					mArrayList.List(Ident("...*..."))
				);
			}
		),
		mTest.Test(
			"MapModule",
			aStreamOut => {
				mStd.Assert(
					mSPO_Parser.Module.ParseText(
						mList.List(
							"§IMPORT (",
#if !true
							"	T € [[]]",
							"	...*... € [[T, T] => T]",
							"	k € T",
#else
							"	T",
							"	...*...",
							"	k",
#endif
							")",
							"",
							"§DEF x... = a => k .* a",
							"§DEF y = .x 1",
							"",
							"§EXPORT y"
						).Join((a1, a2) => a1 + "\n" + a2),
						aStreamOut
					).Match(out mSPO_AST.tModuleNode ModuleNode)
				);
				
				var ModuleConstructor = MapModule(ModuleNode);
				
				mStd.AssertEq(ModuleConstructor.Defs.Size(), 2);
				
				mStd.AssertEq(
					ModuleConstructor.Defs.Get(0),
					mArrayList.List(
						mIL_AST.Alias(TempDef(1), mIL_AST.cEnv),
						
						mIL_AST.GetFirst(TempReg(1), mIL_AST.cArg),
						mIL_AST.Alias(Ident("T"), TempReg(1)),
						mIL_AST.GetSecond(TempReg(2), mIL_AST.cArg),
						mIL_AST.GetFirst(TempReg(3), TempReg(2)),
						mIL_AST.Alias(Ident("...*..."), TempReg(3)),
						mIL_AST.GetSecond(TempReg(4), TempReg(2)),
						mIL_AST.GetFirst(TempReg(5), TempReg(4)),
						mIL_AST.Alias(Ident("k"), TempReg(5)),
						mIL_AST.GetSecond(TempReg(6), TempReg(4)),
						
						mIL_AST.CreatePair(TempReg(7), Ident("k"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(TempReg(8), Ident("...*..."), TempReg(7)),
						mIL_AST.Call(TempReg(9), TempDef(1), TempReg(8)),
						mIL_AST.Alias(Ident("x..."), TempReg(9)),
						mIL_AST.CreateInt(TempReg(10), "1"),
						mIL_AST.Call(TempReg(11), Ident("x..."), TempReg(10)),
						mIL_AST.Alias(Ident("y"), TempReg(11)),
						mIL_AST.ReturnIf(Ident("y"), mIL_AST.cTrue),
						mIL_AST.ReturnIf(null, mIL_AST.cTrue) // TODO: remove this line
					)
				);
				
				mStd.AssertEq(
					ModuleConstructor.Defs.Get(1),
					mArrayList.List(
						mIL_AST.GetFirst(Ident("...*..."), mIL_AST.cEnv),
						mIL_AST.GetSecond(TempReg(4), mIL_AST.cEnv),
						mIL_AST.GetFirst(Ident("k"), TempReg(4)),
						mIL_AST.GetSecond(TempReg(5), TempReg(4)),
						
						mIL_AST.Alias(Ident("a"), mIL_AST.cArg),
						
						mIL_AST.CreatePair(TempReg(1), Ident("a"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(TempReg(2), Ident("k"), TempReg(1)),
						mIL_AST.Call(TempReg(3), Ident("...*..."), TempReg(2)),
						mIL_AST.ReturnIf(TempReg(3), mIL_AST.cTrue)
					)
				);
			}
		)
	);
	
	#endregion
}