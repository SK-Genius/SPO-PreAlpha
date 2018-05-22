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

// TODO: maybe use nested functions to cleanup ???

// TODO: replace default span by somthing meaning full

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
				ExtractEnv.Push(mIL_AST.Alias<tPos>(default, Head, aReg));
				break;
			}
			default: {
				var RestEnv = aReg;
				while (aSymbols.Match(out var Symbol, out aSymbols)) {
					ExtractEnv.Push(mIL_AST.GetFirst<tPos>(default, Symbol, RestEnv));
					var NewRestEnv = aDefConstructor.CreateTempReg();
					ExtractEnv.Push(mIL_AST.GetSecond<tPos>(default, NewRestEnv, RestEnv));
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
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf<tPos>(default, ResultReg, mIL_AST.cTrue));
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
		var ArgumentSymbols = mList.List<tText>();
		aDefConstructor.MapMatch(aMethodNode.Arg, mIL_AST.cArg);
		aDefConstructor.MapMatch(aMethodNode.Obj, mIL_AST.cObj);
		
		var ResultReg = aDefConstructor.MapExpresion(aMethodNode.Body);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf<tPos>(default, ResultReg, mIL_AST.cTrue));
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
		mArrayList.tArrayList<tText> aUnsolveSymbols
	//================================================================================
	) {
		var Def = mArrayList.Concat(
			aTempDefConstructor.UnrollList(
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
			TempMethodDef.UnsolvedSymbols
		);
		return (TempMethodDef.Index, TempMethodDef.UnsolvedSymbols);
	}
	
	//================================================================================
	public static tText
	InitProc<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
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
					aDefConstructor.Commands.Push(mIL_AST.CreatePair<tPos>(default, NewArgReg, Symbol_, ArgReg));
					ArgReg = NewArgReg;
				}
			}
		}
		
		var Proc = aDefConstructor.CreateTempReg();
		aDefConstructor.Commands.Push(mIL_AST.Call<tPos>(default, Proc, aDefName, ArgReg));
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
			case mSPO_AST.tNumberNode<tPos> NumberNode: {
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt<tPos>(
						default,
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
					mIL_AST.Call<tPos>(default, ResultReg, FuncReg, ArgReg)
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
								mIL_AST.CreatePair<tPos>(
									default,
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
					mIL_AST.AddPrefix<tPos>(
						default,
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
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			case mSPO_AST.tMethodNode<tPos> MethodNode: {
				var(NewDefIndex, UnsolvedSymbols) = aDefConstructor.ModuleConstructor.MapMethod(
					MethodNode
				);
				return aDefConstructor.InitProc(
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
					Ifs.Push(mSPO_AST.ReturnIf<tPos>(default, Run, Test));
				}
				Ifs.Push(
					mSPO_AST.ReturnIf<tPos>(
						default,
						mSPO_AST.Empty<tPos>(default),
						mSPO_AST.True<tPos>(default)
					)
				); // TODO: ASSERT FALSE
				
				var ResultReg = aDefConstructor.CreateTempReg();
				
				aDefConstructor.MapCommand(
					mSPO_AST.Def<tPos>(
						default,
						mSPO_AST.Match<tPos>(
							default,
							new mSPO_AST.tIdentNode<tPos>{Name = ResultReg},
							null
						),
						mSPO_AST.Call<tPos>(
							default,
							mSPO_AST.Lambda<tPos>(
								default,
								mSPO_AST.Match<tPos>(
									default,
									mSPO_AST.Empty<tPos>(default),
									null
								),
								mSPO_AST.Block<tPos>(default, Ifs.ToLasyList())
							),
							mSPO_AST.Empty<tPos>(default)
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
					var TestDef = NewDefConstructor(ModuleConstructor);
					
					TestDef.MapMatchTest(mIL_AST.cArg, Match);
					TestDef.FinishMapProc(TestDef.UnsolvedSymbols);
					TestDef.Commands.Push(mIL_AST.ReturnIf<tPos>(default, mIL_AST.cTrue, mIL_AST.cTrue));
					
					Ifs.Push(
						mSPO_AST.ReturnIf<tPos>(
							default,
							mSPO_AST.Call<tPos>(
								default,
								mSPO_AST.Lambda<tPos>(default, Match, Run),
								Imput
							),
							mSPO_AST.Call<tPos>(
								default,
								mSPO_AST.Call<tPos>(
									default,
									new mSPO_AST.tIdentNode<tPos>{ Name = TempDef(TestDef.Index) },
									mSPO_AST.Tuple<tPos>(
										default,
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
					mSPO_AST.ReturnIf<tPos>(
						default,
						mSPO_AST.Empty<tPos>(default),
						mSPO_AST.True<tPos>(default)
					)
				); // TODO: ASSERT FALSE
				
				var ResultReg = aDefConstructor.CreateTempReg();
				
				aDefConstructor.MapCommand(
					mSPO_AST.Def<tPos>(
						default,
						mSPO_AST.Match<tPos>(
							default,
							new mSPO_AST.tIdentNode<tPos>{Name = ResultReg},
							null
						),
						mSPO_AST.Call<tPos>(
							default,
							mSPO_AST.Lambda<tPos>(
								default,
								mSPO_AST.Match<tPos>(
									default,
									mSPO_AST.Empty<tPos>(default),
									null
								),
								mSPO_AST.Block<tPos>(default, Ifs.ToLasyList())
							),
							mSPO_AST.Empty<tPos>(default)
						)
					)
				);
				return ResultReg; 
			}
			case mSPO_AST.tVarToValNode<tPos> VarToValNode: {
				var ObjReg = aDefConstructor.MapExpresion(VarToValNode.Obj);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.VarGet<tPos>(default, ResultReg, ObjReg));
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
				aDefConstructor.Commands.Push(mIL_AST.Alias<tPos>(default, IdentNode.Name, aReg));
				aDefConstructor.KnownSymbols.Push(IdentNode.Name);
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> PrefixNode: {
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix<tPos>(
						default,
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
					aDefConstructor.Commands.Push(mIL_AST.GetFirst<tPos>(default, ItemReg, RestReg));
					
					aDefConstructor.MapMatch(Item, ItemReg);
					
					var NewRestReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetSecond<tPos>(default, NewRestReg, RestReg));
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
						mSPO_AST.Match(default, MatchNode.Pattern, TypeNode),
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
					mIL_AST.Alias<tPos>(default, IdentNode.Name, aInReg)
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
					mIL_AST.HasPrefix<tPos>(default, Reg, Prefix, aInReg),
					mIL_AST.XOr<tPos>(default, InvReg, Reg, mIL_AST.cTrue),
					mIL_AST.ReturnIf<tPos>(default, mIL_AST.cFalse, InvReg),
					mIL_AST.SubPrefix<tPos>(default, SubValue, Prefix, aInReg)
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
					mIL_AST.XOr<tPos>(default, InvReg, TestReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf<tPos>(default, mIL_AST.cFalse, InvReg)
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
					mIL_AST.CreateInt<tPos>(default, IntReg, $"{NumberNode.Value}"),
					mIL_AST.IntsAreEq<tPos>(default, CondReg, aInReg, IntReg),
					mIL_AST.XOr<tPos>(default, InvCondReg, CondReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf<tPos>(default, mIL_AST.cFalse, InvCondReg)
				);
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
			mIL_AST.ReturnIf<tPos>(
				default,
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
					mIL_AST.Call<tPos>(default, FuncNames.Get(J), AllUnsolvedSymbols.Get(J), mIL_AST.cEnv)
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
					aDefConstructor.Commands.Push(mIL_AST.CreatePair<tPos>(default, NewArgReg, Symbol_, ArgReg));
					ArgReg = NewArgReg;
				}
			}
			
			aDefConstructor.Commands.Push(mIL_AST.Call<tPos>(default, FuncName, TempDef(DefIndex), ArgReg));
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
			mIL_AST.VarDef<tPos>(
				default,
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
				aDefConstructor.Commands.Push(mIL_AST.VarSet<tPos>(default, Object, Arg));
				continue;
			}
			tText Result;
			if (Call.Result == null) {
				Result = mIL_AST.cEmpty;
			} else {
				Result = aDefConstructor.CreateTempReg();
			}
			aDefConstructor.Commands.Push(
				mIL_AST.Push<tPos>(default, Object),
				mIL_AST.Exec<tPos>(default, Result, MethodName, Arg),
				mIL_AST.Pop<tPos>(default)
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
			mSPO_AST.Lambda<tPos>(
				default,
				aModuleNode.Import.Match,
				mSPO_AST.Block<tPos>(
					default,
					mList.Concat(
						aModuleNode.Commands,
						mList.List<mSPO_AST.tCommandNode<tPos>>(
							mSPO_AST.ReturnIf<tPos>(default, aModuleNode.Export.Expression, mSPO_AST.True<tPos>(default))
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
		TempLambdaDef.FinishMapProc(DefSymbols);
		
		return ModuleConstructor;
	}
	
	#region TEST
	
	//================================================================================
	private static mStd.tSpan<mTextParser.tPos> Span(
		(tInt32 Row, tInt32 Col) aStart,
		(tInt32 Row, tInt32 Col) aEnd
	//================================================================================
	) => new mStd.tSpan<mTextParser.tPos> {
		Start = {
			Row = aStart.Row,
			Col = aStart.Col
		},
		End = {
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	};
	
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
					).Match(out mSPO_AST.tExpressionNode<mTextParser.tPos> ExpressionNode)
				);
				
				var Def = NewDefConstructor(NewModuleConstructor<mTextParser.tPos>());
				mStd.AssertEq(Def.MapExpresion(ExpressionNode), TempReg(11));
				
				mStd.AssertEq(
					Def.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(1), "3"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(3), "3"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(4), TempReg(3), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(5), "4"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(6), TempReg(5), TempReg(4)),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(7), Ident("...+..."), TempReg(6)),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(8), TempReg(7), TempReg(2)),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(9), "2"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(10), TempReg(9), TempReg(8)),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(11), Ident("...<...<..."), TempReg(10))
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
					).Match(out mSPO_AST.tDefNode<mTextParser.tPos> DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor<mTextParser.tPos>());
				DefConstructor.MapDef(DefNode);
				
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(1), "2"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(3), "1"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(4), TempReg(3), TempReg(2)),
						
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("a"), TempReg(4))
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
					).Match(out mSPO_AST.tDefNode<mTextParser.tPos> DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor<mTextParser.tPos>());
				DefConstructor.MapDef(DefNode);
				
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(1), "3"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(3), "2"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(4), TempReg(3), TempReg(2)),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(5), TempReg(4), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(6), "1"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(7), TempReg(6), TempReg(5)),
						
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(8), TempReg(7)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("a"), TempReg(8)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(9), TempReg(7)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(10), TempReg(9)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(11), TempReg(10)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("b"), TempReg(11)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(12), TempReg(10)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(13), TempReg(12)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("c"), TempReg(13)),
						
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(14), TempReg(12)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(15), TempReg(9))
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
					).Match(out mSPO_AST.tDefNode<mTextParser.tPos> DefNode)
				);
				
				var Module = NewDefConstructor(NewModuleConstructor<mTextParser.tPos>());
				Module.MapDef(DefNode);
				
				mStd.AssertEq(
					Module.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(1), "3"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(3), "2"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(4), TempReg(3), TempReg(2)),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(5), "1"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(6), TempReg(5), TempReg(4)),
						
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(7), TempReg(6)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("a"), TempReg(7)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(8), TempReg(6)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(9), TempReg(8)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("b"), TempReg(9)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(10), TempReg(8)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(11), TempReg(10)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("c"), TempReg(11)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(12), TempReg(10))
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
					).Match(out mSPO_AST.tDefNode<mTextParser.tPos> DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor<mTextParser.tPos>());
				DefConstructor.MapDef(DefNode);
				
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(1), "4"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(2), TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(3), "3"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(4), TempReg(3), TempReg(2)),
						mIL_AST.AddPrefix(Span((0, 0), (0, 0)), TempReg(5), Ident("bla..."), TempReg(4)),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(6), TempReg(5), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(7), "2"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(8), TempReg(7), TempReg(6)),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(9), "1"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(10), TempReg(9), TempReg(8)),
						
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(11), TempReg(10)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("a"), TempReg(11)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(12), TempReg(10)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(13), TempReg(12)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("b"), TempReg(13)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(14), TempReg(12)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(15), TempReg(14)),
						mIL_AST.SubPrefix(Span((0, 0), (0, 0)), TempReg(16), Ident("bla..."), TempReg(15)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(17), TempReg(16)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("c"), TempReg(17)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(18), TempReg(16)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(19), TempReg(18)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("d"), TempReg(19)),
						
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(20), TempReg(18)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(21), TempReg(14))
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
					).Match(out mSPO_AST.tDefNode<mTextParser.tPos> DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor<mTextParser.tPos>());
				DefConstructor.MapDef(DefNode);
				
				mStd.AssertEq(DefConstructor.ModuleConstructor.Defs.Size(), 2);
				mStd.AssertEq(
					DefConstructor.ModuleConstructor.Defs.Get(1),
					mArrayList.List(
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("...*..."), mIL_AST.cEnv),
						
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("a"), mIL_AST.cArg),
						
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(1), Ident("a"), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(2), "2"),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(3), TempReg(2), TempReg(1)),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(4), Ident("...*..."), TempReg(3)),
						mIL_AST.ReturnIf(Span((0, 0), (0, 0)), TempReg(4), mIL_AST.cTrue)
					)
				);
				
				mStd.AssertEq(DefConstructor.Index, 0);
				
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(1), TempDef(1), Ident("...*...")),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("x"), TempReg(1))
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
					).Match(out mSPO_AST.tDefNode<mTextParser.tPos> DefNode)
				);
				
				var DefConstructor = NewDefConstructor(NewModuleConstructor<mTextParser.tPos>());
				DefConstructor.MapDef(DefNode);
				
				mStd.AssertEq(DefConstructor.ModuleConstructor.Defs.Size(), 2);
				mStd.AssertEq(
					DefConstructor.ModuleConstructor.Defs.Get(1),
					mArrayList.List(
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), Ident("...+..."), mIL_AST.cEnv),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(13), mIL_AST.cEnv),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), Ident("...*..."), TempReg(13)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(14), TempReg(13)),
						
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(1), mIL_AST.cArg),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("a"), TempReg(1)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(2), mIL_AST.cArg),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(3), TempReg(2)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("b"), TempReg(3)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(4), TempReg(2)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(5), TempReg(4)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("c"), TempReg(5)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(6), TempReg(4)),
						
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(7), Ident("c"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(8), Ident("b"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(9), Ident("a"), TempReg(8)),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(10), Ident("...*..."), TempReg(9)),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(11), TempReg(10), TempReg(7)),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(12), Ident("...+..."), TempReg(11)),
						mIL_AST.ReturnIf(Span((0, 0), (0, 0)), TempReg(12), mIL_AST.cTrue)
					)
				);
				
				mStd.AssertEq(DefConstructor.Index, 0);
				mStd.AssertEq(
					DefConstructor.Commands,
					mArrayList.List(
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(1), Ident("...*..."), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(2), Ident("...+..."), TempReg(1)),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(3), TempDef(1), TempReg(2)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("...*...+..."), TempReg(3))
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
					).Match(out mSPO_AST.tLambdaNode<mTextParser.tPos> LambdaNode)
				);
				
				var ModuleConstructor = NewModuleConstructor<mTextParser.tPos>();
				var (DefIndex, UnsolvedSymbols) = ModuleConstructor.MapLambda(
					LambdaNode
				);
				
				mStd.AssertEq(ModuleConstructor.Defs.Size(), 1);
				mStd.AssertEq(DefIndex, 0);
				mStd.AssertEq(
					ModuleConstructor.Defs.Get(DefIndex),
					mArrayList.List(
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("...*..."), mIL_AST.cEnv),
							
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(1), mIL_AST.cArg),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("a"), TempReg(1)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(2), mIL_AST.cArg),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(3), TempReg(2)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("b"), TempReg(3)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(4), TempReg(2)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(5), TempReg(4)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(6), TempReg(5)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("x"), TempReg(6)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(7), TempReg(5)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(8), TempReg(7)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("y"), TempReg(8)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(9), TempReg(7)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(10), TempReg(9)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("z"), TempReg(10)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(11), TempReg(9)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(12), TempReg(4)),
						
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(13), Ident("z"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(14), Ident("a"), TempReg(13)),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(15), Ident("...*..."), TempReg(14)),
						mIL_AST.ReturnIf(Span((0, 0), (0, 0)), TempReg(15), mIL_AST.cTrue)
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
							"	T € [[]]",
							"	...*... € [[T, T] => T]",
							"	k € T",
							")",
							"",
							"§DEF x... = a => k .* a",
							"§DEF y = .x 1",
							"",
							"§EXPORT y"
						).Join((a1, a2) => a1 + "\n" + a2),
						aStreamOut
					).Match(out mSPO_AST.tModuleNode<mTextParser.tPos> ModuleNode)
				);
				
				var ModuleConstructor = MapModule(ModuleNode);
				
				mStd.AssertEq(ModuleConstructor.Defs.Size(), 2);
				
				mStd.AssertEq(
					ModuleConstructor.Defs.Get(0),
					mArrayList.List(
						mIL_AST.Alias(Span((0, 0), (0, 0)), TempDef(1), mIL_AST.cEnv),
						
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(1), mIL_AST.cArg),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("T"), TempReg(1)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(2), mIL_AST.cArg),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(3), TempReg(2)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("...*..."), TempReg(3)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(4), TempReg(2)),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), TempReg(5), TempReg(4)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("k"), TempReg(5)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(6), TempReg(4)),
						
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(7), Ident("k"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(8), Ident("...*..."), TempReg(7)),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(9), TempDef(1), TempReg(8)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("x..."), TempReg(9)),
						mIL_AST.CreateInt(Span((0, 0), (0, 0)), TempReg(10), "1"),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(11), Ident("x..."), TempReg(10)),
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("y"), TempReg(11)),
						mIL_AST.ReturnIf(Span((0, 0), (0, 0)), Ident("y"), mIL_AST.cTrue),
						mIL_AST.ReturnIf(Span((0, 0), (0, 0)), mIL_AST.cEmpty, mIL_AST.cTrue) // TODO: remove this line
					)
				);
				
				mStd.AssertEq(
					ModuleConstructor.Defs.Get(1),
					mArrayList.List(
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), Ident("...*..."), mIL_AST.cEnv),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(4), mIL_AST.cEnv),
						mIL_AST.GetFirst(Span((0, 0), (0, 0)), Ident("k"), TempReg(4)),
						mIL_AST.GetSecond(Span((0, 0), (0, 0)), TempReg(5), TempReg(4)),
						
						mIL_AST.Alias(Span((0, 0), (0, 0)), Ident("a"), mIL_AST.cArg),
						
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(1), Ident("a"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((0, 0), (0, 0)), TempReg(2), Ident("k"), TempReg(1)),
						mIL_AST.Call(Span((0, 0), (0, 0)), TempReg(3), Ident("...*..."), TempReg(2)),
						mIL_AST.ReturnIf(Span((0, 0), (0, 0)), TempReg(3), mIL_AST.cTrue)
					)
				);
			}
		)
	);
	
	#endregion
}