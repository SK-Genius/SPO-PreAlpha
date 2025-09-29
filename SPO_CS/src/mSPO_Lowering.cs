// IMPORT Common/mStd
// IMPORT Common/mStream
// IMPORT Common/mMaybe
// IMPORT Common/mError
// IMPORT Common/mAssert
// IMPORT Common/mResult
// IMPORT mSPO_AST

public static class
mSPO_Lowering {
	
	public static mSPO_AST.tTypeNode<tPos>
	LowerType<tPos>(
		this mSPO_AST.tTypeNode<tPos> aType
	) => aType switch {
		mSPO_AST.tTupleTypeNode<tPos> Node
		=> Node.ItemTypes.Map(
				LowerType
		).Reduce(
			(mSPO_AST.tTypeNode<tPos>)mSPO_AST.EmptyType(Node.Pos),
			(aAccu, aLoweredItem) => mSPO_AST.PairType(Node.Pos, aAccu, aLoweredItem)
		),
		mSPO_AST.tTextTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tSetTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tVarTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tLambdaTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tPrefixTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tGenericTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tInterfaceTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tRecursiveTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tGenericApplyTypeNode<tPos> Node => Node, // TODO
		
		mSPO_AST.tEmptyTypeNode<tPos> Node => Node,
		mSPO_AST.tBoolTypeNode<tPos> Node => Node,
		mSPO_AST.tIntTypeNode<tPos> Node => Node,
		mSPO_AST.tCharTypeNode<tPos> Node => Node,
		mSPO_AST.tTypeTypeNode<tPos> Node => Node,
		_ => throw new System.NotImplementedException(aType.GetType().Name)
	};
	
	public static mResult.tResult<mSPO_AST.tTypedMatchNode<tPos>, (tPos Pos, tText ErrorText)>
	LowerTypedMatch<tPos>(
		this mSPO_AST.tTypedMatchNode<tPos> aTypedMatch
	) => aTypedMatch.Pattern.LowerMatch().Then(
		aLoweredMatch => mSPO_AST.Match(
			aTypedMatch.Pos,
			aLoweredMatch,
			aTypedMatch.TypeExpression
		).Do(
			_ => { _.TypeAnnotation = aTypedMatch.TypeAnnotation; }
		)
	);
	
	public static mResult.tResult<mSPO_AST.tMatchNode<tPos>, (tPos Pos, tText ErrorText)>
	LowerMatch<tPos>(
		this mSPO_AST.tMatchNode<tPos> aMatch
	) => aMatch switch {
		mSPO_AST.tTypedMatchNode<tPos> Match
		=> Match.LowerTypedMatch().Then(_ => (mSPO_AST.tMatchNode<tPos>)_),
		mSPO_AST.tMatchGuardNode<tPos> Match
		=> Match.Match.LowerMatch().ThenTry(
			aLoweredMatch => Match.Guard.LowerExpression().Then(
				aLoweredGuard => (mSPO_AST.tMatchNode<tPos>)mSPO_AST.MatchGuard(
					Match.Pos,
					aLoweredMatch,
					aLoweredGuard
				).Do(_ => { _.TypeAnnotation = Match.TypeAnnotation; })
			)
		),
		mSPO_AST.tMatchPairNode<tPos> Match
		=> Match.Tail.LowerMatch().ThenTry(
			aLoweredTail => Match.Head.LowerMatch().Then(
				aLoweredHead => (mSPO_AST.tMatchNode<tPos>)mSPO_AST.MatchPair(
					Match.Pos,
					aLoweredTail,
					aLoweredHead
				).Do(_ => { _.TypeAnnotation = Match.TypeAnnotation; })
			)
		),
		mSPO_AST.tMatchPrefixNode<tPos> Match 
		=> Match.Match.LowerMatch().Then(
			aLoweredMatch => (mSPO_AST.tMatchNode<tPos>)mSPO_AST.MatchPrefix(
				Match.Pos,
				Match.Prefix,
				aLoweredMatch
			).Do(_ => { _.TypeAnnotation = Match.TypeAnnotation; })
		),
		mSPO_AST.tMatchRecordNode<tPos> Match
		=> Match.Elements.Map(
			aElement => aElement.Match.LowerMatch().Then(
				aLoweredMatch => (aElement.Id, aLoweredMatch)
			)
		).WhenAllThen(
			aLoweredElements => (mSPO_AST.tMatchNode<tPos>)mSPO_AST.MatchRecord(
				Match.Pos,
				aLoweredElements
			).Do(_ => { _.TypeAnnotation = Match.TypeAnnotation; })
		),
		mSPO_AST.tMatchTupleNode<tPos> Match
		=> Match.Items.Map(
			LowerMatch
		).WhenAllThen(
			aLoweredItems => (mSPO_AST.tMatchNode<tPos>)aLoweredItems.Reduce(
				(mSPO_AST.tMatchNode<tPos>)mSPO_AST.Empty(Match.Pos),
				(aAccu, aLoweredItem) => (mSPO_AST.tMatchNode<tPos>)mSPO_AST.MatchPair(Match.Pos, aAccu, aLoweredItem)
			).Do(
				_ => { _.TypeAnnotation = Match.TypeAnnotation; }
			)
		),
		mSPO_AST.tTextNode<tPos> Match
		=> mSPO_AST.MatchTuple(
			Match.Pos,
			mStream.Stream(
				Match.Value.ToCharArray()
			).Map(
				_ => (mSPO_AST.tMatchNode<tPos>)mSPO_AST.MatchPrefix(Match.Pos, "_Char...", mSPO_AST.Int(Match.Pos, (tInt32)_))
			)
		).Do(
			_ => { _.TypeAnnotation = Match.TypeAnnotation; }
		).LowerMatch(
		),
		mSPO_AST.tLiteralNode<tPos> Match
		=> mResult.OK(
			(mSPO_AST.tMatchNode<tPos>)Match
		),
		mSPO_AST.tMatchFreeIdNode<tPos> Match => Match,
		mSPO_AST.tMatchVarNode<tPos> Match => Match,
		mSPO_AST.tIgnoreMatchNode<tPos> Match => Match,
		_ => throw new System.NotImplementedException(aMatch.GetType().FullName),
	};
	
	public static mResult.tResult<mSPO_AST.tExpressionNode<tPos>, (tPos Pos, tText ErrorText)>
	LowerExpression<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpr
	) => aExpr switch {
		mSPO_AST.tLambdaNode<tPos> { Pos: var Pos, Generic: var Generic, Head: var Head, Body: var Body, TypeAnnotation: var Type }
		=> Body.LowerExpression().ThenTry(
			aLoweredBody => Head.LowerMatch().Then(
				aLoweredHead => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Lambda(
					Pos,
					Generic,
					aLoweredHead,
					aLoweredBody
				).Do(_ => { _.TypeAnnotation = Type; })
			)
		),
		mSPO_AST.tMethodNode<tPos> { Pos: var Pos, Obj: var Obj, Arg: var Arg, Body: var Body, TypeAnnotation: var Type }
		=> Body.LowerExpression().ThenTry(
			aLoweredBody => Obj.LowerMatch().ThenTry(
				aLoweredObj => Arg.LowerMatch().Then(
					aLoweredArg => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Method(
						Pos,
						aLoweredObj,
						aLoweredArg,
						(mSPO_AST.tBlockNode<tPos>)aLoweredBody
					).Do(_ => { _.TypeAnnotation = Type; })
				)
			)
		),
		mSPO_AST.tVarToValNode<tPos> { Pos: var Pos, Obj: var Obj, TypeAnnotation: var Type }
		=> Obj.LowerExpression().Then(
			aLoweredObj => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.VarToVal(
				Pos,
				aLoweredObj
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tCallNode<tPos> { Pos: var Pos, Func: var Func, Arg: var Arg, TypeAnnotation: var Type }
		=> Func.LowerExpression().ThenTry(
			aLoweredFunc => Arg.LowerExpression().Then(
				aLoweredArg => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Call(
					Pos,
					aLoweredFunc,
					aLoweredArg
				).Do(_ => { _.TypeAnnotation = Type; })
			)
		),
		mSPO_AST.tPrefixNode<tPos> { Pos: var Pos, Prefix: var Prefix, Element: var Element, TypeAnnotation: var Type }
		=> Element.LowerExpression().Then(
			aLoweredElement => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Prefix(
				Pos,
				Prefix,
				aLoweredElement
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tTupleNode<tPos> { Pos: var Pos, Items: var Items, TypeAnnotation: var Type }
		=> Items.Map(LowerExpression).WhenAllThen(
			aLoweredItems => mSPO_AST.Tuple(
				Pos,
				aLoweredItems
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tPairNode<tPos> { Pos: var Pos, Tail: var Tail, Head: var Head, TypeAnnotation: var Type }
		=> Tail.LowerExpression().ThenTry(
			aLoweredTail => Head.LowerExpression().Then(
				aLoweredHead => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Pair(
					Pos,
					aLoweredTail,
					aLoweredHead
				).Do(_ => { _.TypeAnnotation = Type; })
			)
		),
		mSPO_AST.tRecordNode<tPos> { Pos: var Pos, Elements: var Elements, TypeAnnotation: var Type }
		=> Elements.Map(
			aElement => aElement.Value.LowerExpression().Then(
				aLoweredValue => (Key: aElement.Key, Value: aLoweredValue)
			)
		).WhenAllThen(
			aLoweredElements => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Record(
				Pos,
				aLoweredElements
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tIfNode<tPos> { Pos: var Pos, Cases: var Cases, TypeAnnotation: var Type }
		=> Cases.Map(
			aCase => aCase.Cond.LowerExpression().ThenTry(
				aLoweredCond => aCase.Result.LowerExpression().Then(
					aLoweredResult => (aLoweredCond, aLoweredResult)
				)
			)
		).WhenAllThen(
			aLoweredCases => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.If(
				Pos,
				aLoweredCases
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tIfMatchNode<tPos> { Pos: var Pos, Expression: var Expr, Cases: var Cases, TypeAnnotation: var Type }
		=> Expr.LowerExpression().ThenTry(
			aLoweredExpr => Cases.Map(
				aCase => aCase.Expression.LowerExpression().ThenTry(
					aLoweredCaseExpression => aCase.Match.LowerMatch().Then(
						aLoweredCaseMatch => (aLoweredCaseMatch, aLoweredCaseExpression)
					)
				)
			).WhenAllThen(
				aLoweredCases => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.IfMatch(
					Pos,
					aLoweredExpr,
					aLoweredCases
				).Do(_ => { _.TypeAnnotation = Type; })
			)
		),
		mSPO_AST.tIsNode<tPos> { Pos: var Pos, Expression: var Expression, Match: var Match, TypeAnnotation: var Type }
		=> mSPO_AST.IfMatch(
			Pos,
			Expression,
			mStream.Stream([
				(Match, (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.True(Pos)),
				(mSPO_AST.Match(Pos, mSPO_AST.IgnoreMatch(Pos), mStd.cEmpty), mSPO_AST.False(Pos))
			])
		).Do(
			_ => { _.TypeAnnotation = Type; }
		).LowerExpression(
		),
		mSPO_AST.tPipeToRightNode<tPos> { Pos: var Pos, Head: var Head, Pipe: var Pipe, TypeAnnotation: var TypeAnnotation }
		=> mStd.Call(
			() => {
				var Result = (mSPO_AST.tExpressionNode<tPos>)Head;
				
				foreach (var Left in Pipe) {
					if (Left is not mSPO_AST.tCallNode<tPos> Call) {
						return mResult.Fail((Pos, $"expect call but is:\n{Left.ToText()}"));
					}
					
					var Func = Call.Func is mSPO_AST.tIdNode<tPos> Id
					? mSPO_AST.Id(Id.Pos, "..." + Id.Id[1..]).Do(_ => { _.TypeAnnotation = Id.TypeAnnotation; })
					: Call.Func;
					
					if (Call.Arg is mSPO_AST.tTupleNode<tPos> Args) {
						Result = mSPO_AST.Call(
							Pos,
							Func,
							mSPO_AST.Tuple(
								Pos,
								mStream.Stream(Result, Args.Items)
							)
						).Do(
							_ => { _.TypeAnnotation = Call.TypeAnnotation; }
						)
						;
					} else {
						Result = mSPO_AST.Call(
							Pos,
							Func,
							mSPO_AST.Tuple(
								Pos,
								mStream.Stream([Result, Call.Arg])
							)
						).Do(_ => { _.TypeAnnotation = Call.TypeAnnotation; });
					}
				}
				return Result.LowerExpression();
			}
		),
		mSPO_AST.tPipeToLeftNode<tPos> { Pos: var Pos, Head: var Head, Pipe: var Pipe, TypeAnnotation: var TypeAnnotation }
		=> mStd.Call(
			() => {
				var Result = (mSPO_AST.tExpressionNode<tPos>)Head;
				
				foreach (var Left in Pipe) {
					if (Left is not mSPO_AST.tCallNode<tPos> Call) {
						return mResult.Fail((Pos, $"expect call but is:\n{Left.ToText()}"));
					}
					
					var Func = Call.Func is mSPO_AST.tIdNode<tPos> Id
					? mSPO_AST.Id(Id.Pos, Id.Id[1..] + "...").Do(_ => { _.TypeAnnotation = Id.TypeAnnotation; })
					: Call.Func;
					
					if (Call.Arg is mSPO_AST.tTupleNode<tPos> Args) {
						Result = mSPO_AST.Call(
							Pos,
							Func,
							mSPO_AST.Tuple(
								Pos,
								mStream.Concat(Args.Items, mStream.Stream([Result]))
							)
						).Do(_ => { _.TypeAnnotation = Call.TypeAnnotation; });
					} else {
						Result = mSPO_AST.Call(
							Pos,
							Func,
							mSPO_AST.Tuple(
								Pos,
								mStream.Stream([Call.Arg, Result])
							)
						).Do(_ => { _.TypeAnnotation = Call.TypeAnnotation; });
					}
				}
				return Result.LowerExpression();
			}
		),
		mSPO_AST.tBlockNode<tPos> { Pos: var Pos, Commands: var Commands, TypeAnnotation: var Type }
		=> Commands.Map(LowerCommand).WhenAllThen(
			aLoweredCommands => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Block(
				Pos,
				aLoweredCommands
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tTextNode<tPos> Node
		=> mResult.OK(
			mSPO_AST.Tuple(
				Node.Pos,
				mStream.Stream(
					Node.Value.ToCharArray()
				).Map(
					_ => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Prefix(
						Node.Pos,
						"_Char...",
						mSPO_AST.Int(Node.Pos, (tInt32)_)
					)
				)
			)
		),
		mSPO_AST.tIdNode<tPos> Node => Node,
		mSPO_AST.tEmptyNode<tPos> Node => Node,
		mSPO_AST.tTrueNode<tPos> Node => Node,
		mSPO_AST.tFalseNode<tPos> Node => Node,
		mSPO_AST.tIntNode<tPos> Node => Node,
		mSPO_AST.tCharNode<tPos> Node => Node,
		mSPO_AST.tTypeNode<tPos> Node => mResult.OK((mSPO_AST.tExpressionNode<tPos>)Node.LowerType()),
		_ => throw new System.NotImplementedException(aExpr.GetType().Name)
	};
	
	public static mResult.tResult<mSPO_AST.tCommandNode<tPos>, (tPos Pos, tText ErrorText)>
	LowerCommand<tPos>(
		this mSPO_AST.tCommandNode<tPos> aCmd
	) => aCmd switch {
		mSPO_AST.tDefNode<tPos> { Pos: var Pos, Des: var Des, Src: var Src }
		=> LowerExpression(Src).Then(
			aSrc => (mSPO_AST.tCommandNode<tPos>)mSPO_AST.Def(Pos, Des, aSrc)
		),
		mSPO_AST.tReturnIfNode<tPos> { Pos: var Pos, Condition: var Cond, Result: var Res }
		=> LowerExpression(Cond).ThenTry(
			aCond => LowerExpression(Res).Then(
				aRes => (mSPO_AST.tCommandNode<tPos>)mSPO_AST.ReturnIf(Pos, aCond, aRes)
			)
		),
		mSPO_AST.tMethodCallsNode<tPos> { Pos: var Pos, Object: var Obj, MethodCalls: var Calls }
		=> LowerExpression(Obj).ThenTry(
			NewObj => Calls.Map(
				aCall => LowerExpression(aCall.Argument).Then(
					aCallArg => mSPO_AST.MethodCall(
						aCall.Pos,
						aCall.Method,
						aCallArg,
						aCall.Result
					)
				)
			).WhenAllThen(
				aCalls => (mSPO_AST.tCommandNode<tPos>)mSPO_AST.MethodCallStatement(Pos, NewObj, aCalls)
			)
		),
		mSPO_AST.tRecLambdasNode<tPos> { Pos: var Pos, List: var List }
		=> List.Map(
			aListItem => aListItem.Lambda.LowerExpression(
			).Then(
				aLoweredLambda => mSPO_AST.RecLambdaItem(
					aListItem.Pos,
					aListItem.Id,
					(mSPO_AST.tLambdaNode<tPos>)aLoweredLambda
				)
			)
		).WhenAllThen(
			aLoweredList => (mSPO_AST.tCommandNode<tPos>)mSPO_AST.RecLambdas(
				Pos,
				aLoweredList
			)
		),
		mSPO_AST.tDefVarNode<tPos> => mResult.OK(aCmd),
		_ => throw new System.NotImplementedException(aCmd.GetType().Name)
	};
	
	public static mResult.tResult<mSPO_AST.tModuleNode<tPos>, (tPos Pos, tText ErrorText)>
	LowerModule<tPos>(
		this mSPO_AST.tModuleNode<tPos> aModule
	) => aModule.Export.Expression.LowerExpression().ThenTry(
		aLoweredExpr => aModule.Commands.Map(LowerCommand).WhenAllThenTry(
			aLoweredCommands => aModule.Import.Match.LowerMatch().Then(
				aLoweredMatch => mSPO_AST.Module(
					aModule.Pos,
					mSPO_AST.Import(aModule.Import.Pos, aLoweredMatch),
					aLoweredCommands,
					mSPO_AST.Export(aModule.Export.Pos, aLoweredExpr)
				)
			)
		)
	);
}
