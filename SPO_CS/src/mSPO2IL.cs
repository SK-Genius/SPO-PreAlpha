//IMPORT mSPO_AST.cs
//IMPORT mSPO_AST_Types.cs
//IMPORT mIL_AST.cs
//IMPORT mArrayList.cs
//IMPORT mPerf.cs
//IMPORT mError.cs
//IMPORT mMaybe.cs
//IMPORT mAssert.cs

#nullable enable

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

public static class
mSPO2IL {
	
	public struct
	tModuleConstructor<tPos> {
		public mArrayList.tArrayList<(mVM_Type.tType Type, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands)> Defs;
		internal mStd.tFunc<tPos, tPos, tPos> MergePos;
	}
	
	public struct
	tDefConstructor<tPos> {
		public mVM_Type.tType Type; 
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands;
		public tInt32 LastTempReg;
		public mArrayList.tArrayList<tText> KnownSymbols;
		public mArrayList.tArrayList<(tText Ident, tPos Pos)> UnsolvedSymbols;
		public tInt32 Index;
		public tModuleConstructor<tPos> ModuleConstructor;
	}
	
	public static tModuleConstructor<tPos>
	NewModuleConstructor<tPos>(
		mStd.tFunc<tPos, tPos, tPos> aMergePos
	) => new tModuleConstructor<tPos> {
		Defs = mArrayList.List<(mVM_Type.tType, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>)>(),
		MergePos = aMergePos
	};
	
	public static tDefConstructor<tPos>
	NewDefConstructor<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor
	) {
		var DefIndex = aModuleConstructor.Defs.Size();
		var Commands = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		var Type = mVM_Type.Proc(
			mVM_Type.Empty(),
			mVM_Type.Free("ENV"),
			mVM_Type.Proc(
				mVM_Type.Free("OBJ"),
				mVM_Type.Free("ARG"),
				mVM_Type.Free("RES")
			)
		);
		
		aModuleConstructor.Defs.Push((Type, Commands));
		return new tDefConstructor<tPos> {
			Type = Type,
			Commands = Commands,
			KnownSymbols = mArrayList.List<tText>(),
			UnsolvedSymbols = mArrayList.List<(tText Ident, tPos Pos)>(),
			Index = DefIndex,
			ModuleConstructor = aModuleConstructor
		};
	}
	
	public static tText TempReg(tInt32 a) => "t_" + a;
	public static tText TempDef(tInt32 a) => "d_" + a;
	public static tText Ident(tText a) => "_" + a;
	
	public static tText
	CreateTempReg<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor
	) {
		aDefConstructor.LastTempReg += 1;
		return TempReg(aDefConstructor.LastTempReg);
	}
	
	public static mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>
	UnrollList<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tText aReg,
		mStream.tStream<(tText Ident, tPos Pos)>? aSymbols
	) {
		var ExtractEnv = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		switch (aSymbols.Take(2).ToArrayList().Size()) {
			case 0: {
				break;
			}
			case 1: {
				mAssert.IsTrue(aSymbols.Match(out var Head, out var _));
				ExtractEnv.Push(mIL_AST.Alias(aPos, Head.Ident, aReg));
				break;
			}
			default: {
				var RestEnv = aReg;
				foreach (var Symbol in aSymbols) {
					ExtractEnv.Push(mIL_AST.GetFirst(aPos, Symbol.Ident, RestEnv));
					var NewRestEnv = aDefConstructor.CreateTempReg();
					ExtractEnv.Push(mIL_AST.GetSecond(aPos, NewRestEnv, RestEnv));
					RestEnv = NewRestEnv;
				}
				break;
			}
		}
		return ExtractEnv;
	}
	
	public static void
	InitMapLambda<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	) {
		aDefConstructor.MapMatch(aLambdaNode.Head, mIL_AST.cArg, mStd.cEmpty);
		var ResultReg = aDefConstructor.MapExpresion(aLambdaNode.Body);
		if (aLambdaNode.Body is not mSPO_AST.tBlockNode<tPos>) {
			aDefConstructor.Commands.Push(
				mIL_AST.ReturnIf(aLambdaNode.Body.Pos, ResultReg, mIL_AST.cTrue)
			);
		}
		var KnownSymbols = aDefConstructor.KnownSymbols.ToStream();
		aDefConstructor.UnsolvedSymbols = aDefConstructor.UnsolvedSymbols.ToStream(
		).Where(
			S1 => KnownSymbols.All(S2 => S1.Ident != S2)
		).ToArrayList();
	}
	
	public static void
	InitMapMethod<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		aDefConstructor.MapMatch(aMethodNode.Arg, mIL_AST.cArg, mStd.cEmpty);
		aDefConstructor.MapMatch(aMethodNode.Obj, mIL_AST.cObj, mStd.cEmpty);
		
		var ResultReg = aDefConstructor.MapExpresion(aMethodNode.Body);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf(aMethodNode.Pos, ResultReg, mIL_AST.cTrue));
		var KnownSymbols = aDefConstructor.KnownSymbols.ToStream();
		var NewUnsolvedSymbols = aDefConstructor.UnsolvedSymbols.ToStream(
		).Where(
			S1 => KnownSymbols.All(S2 => S1.Ident != S2)
		).ToArrayList();
		aDefConstructor.UnsolvedSymbols = NewUnsolvedSymbols;
	}
	
	public static void
	FinishMapProc<tPos>(
		this ref tDefConstructor<tPos> aTempDefConstructor,
		tPos aPos,
		mArrayList.tArrayList<(tText Ident, tPos Pos)> aUnsolvedSymbols
	) {
		var Def = mArrayList.Concat(
			aTempDefConstructor.UnrollList(
				aPos,
				mIL_AST.cEnv,
				aUnsolvedSymbols.ToStream()
			),
			aTempDefConstructor.Commands
		);
		
		aTempDefConstructor.Commands = Def;
		aTempDefConstructor.ModuleConstructor.Defs.Set(
			aTempDefConstructor.Index,
			(aTempDefConstructor.Type, Def)
		);
	}
	
	public static (tInt32, mArrayList.tArrayList<(tText Ident, tPos Pos)>)
	MapLambda<tPos>(
		this ref tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	) {
		var TempLambdaDef = aModuleConstructor.NewDefConstructor();
		TempLambdaDef.InitMapLambda(aLambdaNode);
		TempLambdaDef.FinishMapProc(
			aLambdaNode.Pos,
			TempLambdaDef.UnsolvedSymbols
		);
		return (TempLambdaDef.Index, TempLambdaDef.UnsolvedSymbols);
	}
	
	public static (tInt32, mArrayList.tArrayList<(tText Ident, tPos Pos)>)
	MapMethod<tPos>(
		this ref tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		var TempMethodDef = aModuleConstructor.NewDefConstructor();
		TempMethodDef.InitMapMethod(aMethodNode);
		TempMethodDef.FinishMapProc(
			aMethodNode.Pos,
			TempMethodDef.UnsolvedSymbols
		);
		return (TempMethodDef.Index, TempMethodDef.UnsolvedSymbols);
	}
	
	public static tText
	InitProc<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tText aDefName,
		mArrayList.tArrayList<(tText Ident, tPos Pos)> aEnv
	) {
		var ArgReg = mIL_AST.cEmpty;
		if (!aEnv.IsEmpty()) {
			foreach (var Symbol in aEnv.ToStream()) {
				if (aDefConstructor.UnsolvedSymbols.ToStream().All(_ => _.Ident != Symbol.Ident)) {
					aDefConstructor.UnsolvedSymbols.Push(Symbol);
				}
			}
			
			if (aEnv.Size() == 1) {
				ArgReg = aEnv.Get(0).Ident;
			} else {
				foreach (var Symbol_ in aEnv.ToStream().Reverse()) {
					var NewArgReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.CreatePair(aPos, NewArgReg, Symbol_.Ident, ArgReg));
					ArgReg = NewArgReg;
				}
			}
		}
		
		var Proc = aDefConstructor.CreateTempReg();
		aDefConstructor.Commands.Push(mIL_AST.CallFunc(aPos, Proc, aDefName, ArgReg));
		aDefConstructor.UnsolvedSymbols.Push((aDefName, aPos));
		return Proc;
	}
	
	public static tText
	MapExpresion<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tExpressionNode<tPos> aExpressionNode
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
			case mSPO_AST.tIdentNode<tPos> { Pos: var Pos, Name: var Name }: {
			//--------------------------------------------------------------------------------
				if (
					aDefConstructor.UnsolvedSymbols.ToStream().All(_ => _.Ident != Name) &&
					!aDefConstructor.Commands.ToStream(
					).Any(
						_ => _.GetResultReg().ThenTry(aName =>  mMaybe.Some(aName == Name)).Else(() => false)
					)
				) {
					aDefConstructor.UnsolvedSymbols.Push((Name, Pos));
				}
				return Name;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tCallNode<tPos>{ Pos: var Pos, Func: var Func, Arg: var Arg }: {
			//--------------------------------------------------------------------------------
				var FuncReg = aDefConstructor.MapExpresion(Func);
				var ArgReg = aDefConstructor.MapExpresion(Arg);
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
						return aDefConstructor.MapExpresion(Head);
					}
					default: {
						var ResultReg = mIL_AST.cEmpty;
						foreach (var Item in Items.Reverse()) {
							var ItemReg = aDefConstructor.MapExpresion(Item);
							var TupleReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CreatePair(
									Item.Pos,
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
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPrefixNode<tPos>{ Pos: var Pos, Prefix: var Prefix, Element: var Element }: {
			//--------------------------------------------------------------------------------
				var ExpresionReg = aDefConstructor.MapExpresion(Element);
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
					var Expression = aDefConstructor.MapExpresion(Value);
					var PrefixReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.AddPrefix(Key.Pos, PrefixReg, Key.Name, Expression));
					var NewResultReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.ExtendRec(Key.Pos, NewResultReg, ResultReg, PrefixReg));
					ResultReg = NewResultReg;
				}
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTextNode<tPos>{ Pos: var Pos, Value: var Value }: {
			//--------------------------------------------------------------------------------
				var TextReg = mIL_AST.cEmpty;
				var Index = Value.Length;
				while (Index --> 0) {
					var Char = Value[Index];
					var CharOrdReg = aDefConstructor.CreateTempReg();
					var CharReg = aDefConstructor.CreateTempReg();
					var TextReg_ = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.CreateInt(Pos, CharOrdReg, ((int)Char).ToString()),
						mIL_AST.AddPrefix(Pos, CharReg, "Char", CharOrdReg),
						mIL_AST.CreatePair(Pos, TextReg_, CharReg, TextReg)
					);
					TextReg = TextReg_;
				}
				return TextReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tLambdaNode<tPos> LambdaNode: {
			//--------------------------------------------------------------------------------
				var(NewDefIndex, UnsolvedSymbols) = aDefConstructor.ModuleConstructor.MapLambda(
					LambdaNode
				);
				return aDefConstructor.InitProc(
					LambdaNode.Pos,
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMethodNode<tPos> MethodNode: {
			//--------------------------------------------------------------------------------
				var(NewDefIndex, UnsolvedSymbols) = aDefConstructor.ModuleConstructor.MapMethod(
					MethodNode
				);
				return aDefConstructor.InitProc(
					MethodNode.Pos,
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tBlockNode<tPos>{ Commands: var Commands }: {
			//--------------------------------------------------------------------------------
				foreach (var Command in Commands) {
					aDefConstructor.MapCommand(Command);
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
				
				aDefConstructor.MapCommand(
					mSPO_AST.Def(
						Pos,
						mSPO_AST.Match(
							Pos,
							new mSPO_AST.tMatchFreeIdentNode<tPos>{Name = ResultReg},
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
					)
				);
				return ResultReg; 
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIfMatchNode<tPos>{ Pos: var Pos, Expression: var MatchExpression, Cases: var Cases }: {
			//--------------------------------------------------------------------------------
				var Input = aDefConstructor.MapExpresion(MatchExpression);
				var ModuleConstructor = aDefConstructor.ModuleConstructor;
				var SwitchDef = ModuleConstructor.NewDefConstructor();
				
				foreach (var (Match, Expression) in Cases) {
					var CasePos = ModuleConstructor.MergePos(Match.Pos, Expression.Pos);
					
					var TestDef = ModuleConstructor.NewDefConstructor();
					TestDef.MapMatchTest(mIL_AST.cArg, Match);
					TestDef.Commands.Push(mIL_AST.ReturnIf(CasePos, mIL_AST.cTrue, mIL_AST.cTrue));
					TestDef.FinishMapProc(CasePos, TestDef.UnsolvedSymbols);
					var TestProc = SwitchDef.InitProc(CasePos, TempDef(TestDef.Index), TestDef.UnsolvedSymbols);
					var TestResult = SwitchDef.CreateTempReg();
					SwitchDef.Commands.Push(mIL_AST.CallFunc(CasePos, TestResult, TestProc, mIL_AST.cArg));
					
					var RunDef = ModuleConstructor.NewDefConstructor();
					RunDef.MapMatch(Match, mIL_AST.cArg, mStd.cEmpty);
					var Result = RunDef.MapExpresion(Expression);
					RunDef.Commands.Push(mIL_AST.ReturnIf(CasePos, Result, mIL_AST.cTrue));
					RunDef.FinishMapProc(CasePos, RunDef.UnsolvedSymbols);
					var RunProc = SwitchDef.InitProc(CasePos, TempDef(RunDef.Index), RunDef.UnsolvedSymbols);
					var CallerArgPair = SwitchDef.CreateTempReg();
					SwitchDef.Commands.Push(mIL_AST.CreatePair(CasePos, CallerArgPair, RunProc, mIL_AST.cArg));
					
					SwitchDef.Commands.Push(mIL_AST.TailCallIf(CasePos, CallerArgPair, TestResult));
				}
				SwitchDef.Commands.Push(mIL_AST.ReturnIf(aExpressionNode.Pos, mIL_AST.cFalse, mIL_AST.cTrue));
				SwitchDef.FinishMapProc(aExpressionNode.Pos, SwitchDef.UnsolvedSymbols);
				var SwitchProc = aDefConstructor.InitProc(aExpressionNode.Pos, TempDef(SwitchDef.Index), SwitchDef.UnsolvedSymbols);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.CallFunc(Pos, ResultReg, SwitchProc, Input));
				
				return ResultReg; 
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tVarToValNode<tPos>{ Pos: var Pos, Obj: var Obj }: {
			//--------------------------------------------------------------------------------
				var ObjReg = aDefConstructor.MapExpresion(Obj);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.VarGet(Pos, ResultReg, ObjReg));
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tRecursiveTypeNode<tPos>{ Pos: var Pos, HeadType: var HeadType, BodyType: var BodyType }: {
			//--------------------------------------------------------------------------------
				mAssert.IsFalse(aDefConstructor.UnsolvedSymbols.ToStream().Any(a => a.Ident == HeadType.Name));
				mAssert.IsFalse(aDefConstructor.KnownSymbols.ToStream().Any(a => a == HeadType.Name));
				aDefConstructor.Commands.Push(
					mIL_AST.TypeFree(HeadType.Pos, HeadType.Name)
				);
				var BodyTypeReg = aDefConstructor.MapExpresion(BodyType);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypeRecursive(
						Pos,
						ResultReg,
						HeadType.Name,
						BodyTypeReg
					)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tSetTypeNode<tPos>{ Expressions: var Expressions }: {
			//--------------------------------------------------------------------------------
				Expressions.Match(out var Head, out var Tail);
				var ResultReg = aDefConstructor.MapExpresion(Head);
				foreach (var Expression in Tail) {
					var ExprReg = aDefConstructor.MapExpresion(Expression);
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
					var ResultReg = aDefConstructor.MapExpresion(First);
					foreach (var Head in Rest) {
						var PairTypeReg = aDefConstructor.CreateTempReg();
						var ExprReg = aDefConstructor.MapExpresion(Head);
						aDefConstructor.Commands.Push(mIL_AST.TypePair(Head.Pos, PairTypeReg, ExprReg, ResultReg));
						ResultReg = PairTypeReg;
					}
					return ResultReg;
				}
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPrefixTypeNode<tPos> PrefixTypeNode: {
			//--------------------------------------------------------------------------------
				var InnerType = aDefConstructor.MapExpresion(
					mSPO_AST.TupleType(PrefixTypeNode.Pos, PrefixTypeNode.Expressions)
				);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypePrefix(PrefixTypeNode.Pos, ResultReg, PrefixTypeNode.Prefix.Name, InnerType)
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
								)
							);
						}
						case mSPO_AST.tCallNode<tPos> CallNode: {
							var Func = (
								CallNode.Func is mSPO_AST.tIdentNode<tPos> IdentNode
								? mSPO_AST.Ident(IdentNode.Pos, "..." + IdentNode.Name[1..])
								: CallNode.Func
							);
							var FuncReg = aDefConstructor.MapExpresion(Func);
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
							var ArgReg = aDefConstructor.MapExpresion(Arg);
							var ResultReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CallFunc(CallNode.Pos, ResultReg, FuncReg, ArgReg)
							);
							return ResultReg;
						}
						default: {
							var FirstArgReg = aDefConstructor.MapExpresion(Left);
							var FuncReg = aDefConstructor.MapExpresion(Right);
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
							(LeftFunc is mSPO_AST.tIdentNode<tPos> IdentNode)
							? mSPO_AST.Ident(IdentNode.Pos, IdentNode.Name[1..] + "...")
							: LeftFunc
						);
						var FuncReg = aDefConstructor.MapExpresion(Func);
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
						var ArgReg = aDefConstructor.MapExpresion(Arg);
						var ResultReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(
							mIL_AST.CallFunc(LeftPos, ResultReg, FuncReg, ArgReg)
						);
						return ResultReg;
					}
					default: {
						var FirstArgReg = aDefConstructor.MapExpresion(Right);
						var FuncReg = aDefConstructor.MapExpresion(Left);
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
	
	public static void
	MapMatch<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMatchNode<tPos> aMatchNode,
		tText aReg,
		mMaybe.tMaybe<mVM_Type.tType> aRegType
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			//--------------------------------------------------------------------------------
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
			//--------------------------------------------------------------------------------
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchFreeIdentNode<tPos>{ Pos: var Pos, Name: var Name }: {
			//--------------------------------------------------------------------------------
				mAssert.AreNotEquals(Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(Pos, Name, aReg));
				aDefConstructor.KnownSymbols.Push(Name);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatchNode: {
			//--------------------------------------------------------------------------------
				break;
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
				aDefConstructor.MapMatch(
					Match,
					ResultReg,
					mStd.cEmpty
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchRecordNode<tPos>{ Elements: var Elements, TypeAnnotation: var TypeAnnotation }: {
			//--------------------------------------------------------------------------------
				foreach (var (Key, Match) in Elements) {
					var Pos = Key.Pos;
					
					var Reg = aReg;
					var Found = false;
					var RecType = aRegType.ElseThrow("");
					while (RecType.MatchRecord(out var HeadKey, out var HeadType, out RecType!)) {
						var HeadTailReg = aDefConstructor.CreateTempReg();
						aDefConstructor.Commands.Push(mIL_AST.DivideRec(Pos, HeadTailReg, Reg));
						if (HeadKey == Key.Name) {
							var TempValueReg_ = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(mIL_AST.GetSecond(Pos, TempValueReg_, HeadTailReg));
							var TempValueReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(mIL_AST.SubPrefix(Pos, TempValueReg, Key.Name, TempValueReg_));
							aDefConstructor.MapMatch(Match, TempValueReg, mStd.cEmpty);
							Found = true;
							break;
						} else {
							Reg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(mIL_AST.GetFirst(Pos, Reg, HeadTailReg));
						}
					}
					
					if (!Found) {
						throw mError.Error($"{Pos} ERROR: can't match type '{aRegType}' to type'{TypeAnnotation}'");
					}
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchTupleNode<tPos>{ Items: var Items }: {
			//--------------------------------------------------------------------------------
				var RestReg = aReg;
				mAssert.AreEquals(Items.Take(2).ToArrayList().Size(), 2);
				foreach (var Item in Items) {
					var ItemReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetFirst(Item.Pos, ItemReg, RestReg));
					
					aDefConstructor.MapMatch(Item, ItemReg, Item.TypeAnnotation);
					
					var NewRestReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetSecond(Item.Pos, NewRestReg, RestReg));
					RestReg = NewRestReg;
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchGuardNode<tPos>{ Match: var Match, Guard: var Guard }: {
			//--------------------------------------------------------------------------------
				// TODO: ASSERT Guard
				aDefConstructor.MapMatch(Match, aReg, mStd.cEmpty);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchNode<tPos> MatchNode: {
			//--------------------------------------------------------------------------------
				if (!TypeNode.Match(out var TypeNode_)) {
					aDefConstructor.MapMatch(MatchNode, aReg, mStd.cEmpty);
				} else if (!MatchNode.Type.Match(out var MatchTypeNode)) {
					aDefConstructor.MapMatch(
						mSPO_AST.Match(MatchNode.Pos, MatchNode.Pattern, TypeNode),
						aReg,
						mStd.cEmpty
					);
				} else {
					throw mError.Error("not implemented"); //TODO: Unify MatchTypeNode & TypeNode_
				}
				break;
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
	
	public static void
	MapMatchTest<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tText aInReg,
		mSPO_AST.tMatchNode<tPos> aMatchNode
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchFreeIdentNode<tPos>{ Pos: var Pos, Name: var Name }: {
			//--------------------------------------------------------------------------------
				mAssert.AreNotEquals(Name, "_");
				mAssert.IsTrue(
					aDefConstructor.KnownSymbols.ToStream().All(_ => _ != Name)
				);
				aDefConstructor.Commands.Push(
					mIL_AST.Alias(Pos, Name, aInReg)
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
				aDefConstructor.MapMatchTest(SubValue, Match);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchGuardNode<tPos>{ Pos: var Pos, Match: var Match, Guard: var Guard }: {
			//--------------------------------------------------------------------------------
				aDefConstructor.MapMatchTest(aInReg, Match);
				
				var InvReg = aDefConstructor.CreateTempReg();
				var TestReg = aDefConstructor.MapExpresion(Guard);
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
					var TempReg = aDefConstructor.CreateTempReg();
					var RestReg_ = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.IsPair(Pos, IsPairReg, aInReg),
						mIL_AST.XOr(Pos, IsNotPairReg, IsPairReg, mIL_AST.cTrue),
						mIL_AST.ReturnIf(Pos, mIL_AST.cFalse, IsNotPairReg),
						mIL_AST.GetFirst(Item.Pos, TempReg, RestReg),
						mIL_AST.GetSecond(Item.Pos, RestReg_, RestReg)
					);
					aDefConstructor.MapMatchTest(TempReg, Item);
					RestReg = RestReg_;
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
				aDefConstructor.MapMatch(MatchNode, aInReg, mStd.cEmpty);
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
	}
	
	public static void
	MapDef<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tDefNode<tPos> aDefNode
	) {
		var ValueReg = aDefConstructor.MapExpresion(aDefNode.Src);
		aDefConstructor.MapMatch(aDefNode.Des, ValueReg, aDefNode.Src.TypeAnnotation);
	}
	
	public static void
	MapReturnIf<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tReturnIfNode<tPos> aReturnNode
	) {
		aDefConstructor.Commands.Push(
			mIL_AST.ReturnIf(
				aReturnNode.Pos,
				aDefConstructor.MapExpresion(aReturnNode.Result),
				aDefConstructor.MapExpresion(aReturnNode.Condition)
			)
		);
	}
	
	public static void
	MapRecursiveLambdas<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tRecLambdasNode<tPos> aRecLambdasNode
	) {
		var NewDefIndices = mArrayList.List<tInt32>();
		var SPODefNodes = mArrayList.List<mSPO_AST.tRecLambdaItemNode<tPos>>();
		var TempLambdaDefs = mArrayList.List<tDefConstructor<tPos>>();
		var AllUnsolvedSymbols = mArrayList.List<(tText Ident, tPos Pos)>();
		
		foreach (var RecLambdaItemNode in aRecLambdasNode.List) {
			var NewDefIndex = aDefConstructor.ModuleConstructor.Defs.Size();
			NewDefIndices.Push(NewDefIndex);
			AllUnsolvedSymbols.Push((TempDef(NewDefIndex), RecLambdaItemNode.Pos));
			TempLambdaDefs.Push(aDefConstructor.ModuleConstructor.NewDefConstructor());
			SPODefNodes.Push(RecLambdaItemNode);
		}
		
		var Max = NewDefIndices.Size();
		
		// create all rec. func. in each rec. func.
		var FuncNames = aRecLambdasNode.List.Map(a => a.Ident.Name).ToArrayList();
		foreach (var TempLambdaDef in TempLambdaDefs.ToArray()) {
			for (var J = 0; J < Max; J += 1) {
				var Definition = AllUnsolvedSymbols.Get(J);
				TempLambdaDef.Commands.Push(
					mIL_AST.CallFunc(Definition.Pos, FuncNames.Get(J), Definition.Ident, mIL_AST.cEnv)
				);
			}
		}
		
		// InitMapLambda(...) for all rec. func.
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempLambdaDef = TempLambdaDefs.Get(I);
			
			TempLambdaDef.InitMapLambda(RecLambdaItemNode.Lambda);
			var KnownSymbols = TempLambdaDef.KnownSymbols.ToStream();
			var TempUnsolvedSymbols = TempLambdaDef.UnsolvedSymbols.ToStream(
			).Where(
				aUnsolved => (
					KnownSymbols.All(_ => _ != aUnsolved.Ident) &&
					aRecLambdasNode.List.All(_ => _.Ident.Name != aUnsolved.Ident)
				)
			);
			
			foreach (var Symbol in TempUnsolvedSymbols) {
				AllUnsolvedSymbols.Push(Symbol);
			}
		}
		
		// FinishMapProc(...) for all rec. func.
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempDefConstructor = TempLambdaDefs.Get(I);
			
			TempDefConstructor.FinishMapProc(
				RecLambdaItemNode.Pos,
				AllUnsolvedSymbols
			);
			
			var ArgReg = mIL_AST.cEmpty;
			if (!AllUnsolvedSymbols.IsEmpty()) {
				foreach (var UnsolvedSymbol in AllUnsolvedSymbols.ToStream()) {
					if (aDefConstructor.UnsolvedSymbols.ToStream().All(_ => _.Ident != UnsolvedSymbol.Ident)) {
						aDefConstructor.UnsolvedSymbols.Push(UnsolvedSymbol);
					}
				}
				
				foreach (var Symbol_ in AllUnsolvedSymbols.ToStream().Reverse()) {
					var NewArgReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.CreatePair(RecLambdaItemNode.Pos, NewArgReg, Symbol_.Ident, ArgReg)
					);
					ArgReg = NewArgReg;
				}
			}
			
			aDefConstructor.Commands.Push(
				mIL_AST.CallFunc(
					RecLambdaItemNode.Pos,
					RecLambdaItemNode.Ident.Name,
					TempDef(TempDefConstructor.Index),
					ArgReg
				)
			);
			
			aDefConstructor.KnownSymbols.Push(RecLambdaItemNode.Ident.Name);
		}
	}
	
	public static void
	MapVar<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tDefVarNode<tPos> aVarNode
	) {
		aDefConstructor.Commands.Push(
			mIL_AST.VarDef(
				aVarNode.Pos,
				aVarNode.Ident.Name,
				aDefConstructor.MapExpresion(aVarNode.Expression)
			)
		);
	}
	
	public static void
	MapMethodCalls<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMethodCallsNode<tPos> aMethodCallsNode
	) {
		var Object = aDefConstructor.MapExpresion(aMethodCallsNode.Object);
		foreach (var Call in aMethodCallsNode.MethodCalls) {
			var Arg = aDefConstructor.MapExpresion(Call.Argument);
			var MethodName = Call.Method.Name;
			if (MethodName == "_=...") {
				aDefConstructor.Commands.Push(mIL_AST.VarSet(aMethodCallsNode.Pos, Object, Arg));
				continue;
			}
			var Result = Call.Result.Match(out var _) ? aDefConstructor.CreateTempReg() : mIL_AST.cEmpty;
			var MethodReg = aDefConstructor.CreateTempReg();
			aDefConstructor.Commands.Push(
				mIL_AST.CreatePair(aMethodCallsNode.Object.Pos, MethodReg, Object, MethodName),
				mIL_AST.CallProc(aMethodCallsNode.Pos, Result, MethodReg, Arg)
			);
			if (Call.Result.Match(out var Result_)) {
				aDefConstructor.MapMatch(Result_, Result, mStd.cEmpty);
			}
			
			var KnownSymbols = aDefConstructor.KnownSymbols.ToStream();
			if (KnownSymbols.All(_ => _ != MethodName)) {
				aDefConstructor.UnsolvedSymbols.Push((MethodName, Call.Method.Pos));
			}
		}
	}
	
	public static void
	MapCommand<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tCommandNode<tPos> aCommandNode
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
				throw mError.Error("impossible");
			}
		}
	}
	
	public static mResult.tResult<tModuleConstructor<tPos>, tText>
	MapModule<tPos>(
		mSPO_AST.tModuleNode<tPos> aModuleNode,
		mStd.tFunc<tPos, tPos, tPos> aMergePos
	) {
		using var _ = mPerf.Measure();
		var ModuleConstructor = NewModuleConstructor(aMergePos);
		var TempLambdaDef = ModuleConstructor.NewDefConstructor();
		var Lambda = mSPO_AST.Lambda(
			aModuleNode.Pos,
			mStd.cEmpty,
			aModuleNode.Import.Match,
			mSPO_AST.Block(
				aMergePos(
					aModuleNode.Commands.IsEmpty() ? default : aModuleNode.Commands.ForceFirst().Pos,
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
		return mSPO_AST_Types.UpdateExpressionTypes(Lambda, mStd.cEmpty).Then(
			_ => {
				TempLambdaDef.InitMapLambda(Lambda);
				if (TempLambdaDef.UnsolvedSymbols.Size() != ModuleConstructor.Defs.Size() - 1) {
					var First = TempLambdaDef.UnsolvedSymbols.ToStream(
					).Where(
						_ => !_.Ident.StartsWith("d_")
					).ForceFirst();
					throw mError.Error($"Unknown symbol '{First.Ident}' @ {First.Pos}", First.Pos);
				}
				
				var DefSymbols = mArrayList.List<(tText Ident, tPos Pos)>();
				for (var I = 1; I < ModuleConstructor.Defs.Size(); I += 1) {
					DefSymbols.Push((TempDef(I), default));
				}
				TempLambdaDef.FinishMapProc(aModuleNode.Pos, DefSymbols);
				
				return ModuleConstructor;
			}
		);
	}
}
