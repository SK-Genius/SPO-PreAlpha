#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mStream.cs
#:ref Common/mMaybe.cs
#:ref Common/mError.cs
#:ref Common/mAssert.cs
#:ref Common/mResult.cs
#:ref mSPO_AST.cs

public static class
mSPO_Desugar {
	public static mSPO_AST.tTypeNode<tPos>
	DesugarType<tPos>(
		this mSPO_AST.tTypeNode<tPos> aType
	) => aType switch {
		mSPO_AST.tTupleTypeNode<tPos> Node
		=> Node.ItemTypes.Map(
				DesugarType
		).Reduce(
			(mSPO_AST.tTypeNode<tPos>)mSPO_AST.EmptyType(Node.Pos),
			(aAccu, aDesugaredItem) => mSPO_AST.PairType(Node.Pos, aAccu, aDesugaredItem)
		),
		mSPO_AST.tCharTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tTextTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tSetTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tVarTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tProcTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tPrefixTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tRecordTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tGenericTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tInterfaceTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tRecursiveTypeNode<tPos> Node => Node, // TODO
		mSPO_AST.tGenericApplyTypeNode<tPos> Node => Node, // TODO
		
		mSPO_AST.tEmptyTypeNode<tPos> Node => Node,
		mSPO_AST.tTrueNode<tPos> Node => Node,
		mSPO_AST.tFalseNode<tPos> Node => Node,
		mSPO_AST.tIntTypeNode<tPos> Node => Node,
		mSPO_AST.tTypeTypeNode<tPos> Node => Node,
		_ => throw new System.NotImplementedException(aType.GetType().Name)
	};
	
	public static mResult.tResult<mSPO_AST.tTypedPatternNode<tPos>, (tPos Pos, tText ErrorText)>
	DesugarTypedPattern<tPos>(
		this mSPO_AST.tTypedPatternNode<tPos> aTypedPattern
	) => aTypedPattern.Pattern.DesugarPattern().Then(
		aDesugaredPattern => mSPO_AST.Pattern(
			aTypedPattern.Pos,
			aDesugaredPattern,
			aTypedPattern.TypeExpression
		).Do(
			__ => { __.TypeAnnotation = aTypedPattern.TypeAnnotation; }
		)
	);
	
	public static mResult.tResult<mSPO_AST.tPatternNode<tPos>, (tPos Pos, tText ErrorText)>
	DesugarPattern<tPos>(
		this mSPO_AST.tPatternNode<tPos> aPattern
	) => aPattern switch {
		mSPO_AST.tTypedPatternNode<tPos> Pattern
		=> Pattern.DesugarTypedPattern().Then(__ => (mSPO_AST.tPatternNode<tPos>)__),
		
		mSPO_AST.tGuardPatternNode<tPos> Pattern
		=> Pattern.Pattern.DesugarPattern().ThenTry(
			aDesugaredPattern => Pattern.Guard.DesugarExpression().Then(
				aDesugaredGuard => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.GuardPattern(
					Pattern.Pos,
					aDesugaredPattern,
					aDesugaredGuard
				).Do(__ => { __.TypeAnnotation = Pattern.TypeAnnotation; })
			)
		),
		
		mSPO_AST.tPairPatternNode<tPos> Pattern
		=> Pattern.Tail.DesugarPattern().ThenTry(
			aDesugaredTail => Pattern.Head.DesugarPattern().Then(
				aDesugaredHead => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.PairPattern(
					Pattern.Pos,
					aDesugaredTail,
					aDesugaredHead
				).Do(__ => { __.TypeAnnotation = Pattern.TypeAnnotation; })
			)
		),
		
		mSPO_AST.tPrefixPatternNode<tPos> Pattern 
		=> Pattern.Pattern.DesugarPattern().Then(
			aDesugaredPattern => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.PrefixPattern(
				Pattern.Pos,
				Pattern.Prefix,
				aDesugaredPattern
			).Do(__ => { __.TypeAnnotation = Pattern.TypeAnnotation; })
		),
		
		mSPO_AST.tRecordPatternNode<tPos> Pattern
		=> Pattern.Elements.Map(
			aElement => aElement.Pattern.DesugarPattern().Then(
				aDesugaredPattern => (aElement.Id, aDesugaredPattern)
			)
		).WhenAllThen(
			aDesugaredElements => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.RecordPattern(
				Pattern.Pos,
				aDesugaredElements
			).Do(__ => { __.TypeAnnotation = Pattern.TypeAnnotation; })
		),
		
		mSPO_AST.tTuplePatternNode<tPos> Pattern
		=> Pattern.Items.Map(
			DesugarPattern
		).WhenAllThen(
			aDesugaredItems => aDesugaredItems.Reduce(
				(mSPO_AST.tPatternNode<tPos>)mSPO_AST.Empty(Pattern.Pos),
				(aAccu, aDesugaredItem) => mSPO_AST.PairPattern(Pattern.Pos, aAccu, aDesugaredItem)
			).Do(
				__ => { __.TypeAnnotation = Pattern.TypeAnnotation; }
			)
		),
		
		mSPO_AST.tTextNode<tPos> Pattern
		=> mResult.OK(
			mStream.Stream(
			Pattern.Value.ToCharArray()
		).Map(
			__ => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.PrefixPattern(Pattern.Pos, "_Char...", mSPO_AST.Int(Pattern.Pos, __))
		).Reduce(
			(mSPO_AST.tPatternNode<tPos>)mSPO_AST.Empty(Pattern.Pos),
			(aTail, aHead) => mSPO_AST.PairPattern(Pattern.Pos, aTail, aHead)
		).Do(
			__ => { __.TypeAnnotation = Pattern.TypeAnnotation; }
			)
		),
		
		mSPO_AST.tCharNode<tPos> Pattern
		=> mSPO_AST.PrefixPattern(
			Pattern.Pos,
			"_Char...",
			mSPO_AST.Int(Pattern.Pos, Pattern.Value)
		).Do(
			__ => { __.TypeAnnotation = Pattern.TypeAnnotation; }
		),
		
		mSPO_AST.tLiteralNode<tPos> Pattern
		=> mResult.OK(
			(mSPO_AST.tPatternNode<tPos>)Pattern
		),
		
		mSPO_AST.tFreeIdPatternNode<tPos> Pattern => Pattern,
		
		mSPO_AST.tVarPatternNode<tPos> Pattern => Pattern,
		
		mSPO_AST.tIgnorePatternNode<tPos> Pattern => Pattern,
		
		_ => throw new System.NotImplementedException(aPattern.GetType().FullName),
	};
	
	public static mResult.tResult<mSPO_AST.tExpressionNode<tPos>, (tPos Pos, tText ErrorText)>
	DesugarExpression<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpr
	) => aExpr switch {
		mSPO_AST.tLambdaNode<tPos> { Pos: var Pos, Generic: var Generic, Head: var Head, Body: var Body, TypeAnnotation: var Type }
		=> Body.DesugarExpression().ThenTry(
			aDesugaredBody => Head.DesugarPattern().Then(
				aDesugaredHead => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Lambda(
					Pos,
					Generic,
					aDesugaredHead,
					aDesugaredBody
				).Do(__ => { __.TypeAnnotation = Type; })
			)
		),
		
		mSPO_AST.tMethodNode<tPos> { Pos: var Pos, Obj: var Obj, Arg: var Arg, Body: var Body, TypeAnnotation: var Type }
		=> Body.DesugarExpression().ThenTry(
			aDesugaredBody => Obj.DesugarPattern().ThenTry(
				aDesugaredObj => Arg.DesugarPattern().Then(
					aDesugaredArg => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Method(
						Pos,
						aDesugaredObj,
						aDesugaredArg,
						(mSPO_AST.tBlockNode<tPos>)aDesugaredBody
					).Do(__ => { __.TypeAnnotation = Type; })
				)
			)
		),
		
		mSPO_AST.tVarToValNode<tPos> { Pos: var Pos, Obj: var Obj, TypeAnnotation: var Type }
		=> Obj.DesugarExpression().Then(
			aDesugaredObj => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.VarToVal(
				Pos,
				aDesugaredObj
			).Do(__ => { __.TypeAnnotation = Type; })
		),
		
		mSPO_AST.tCallNode<tPos> { Pos: var Pos, Func: var Func, Arg: var Arg, TypeAnnotation: var Type }
		=> Func.DesugarExpression().ThenTry(
			aDesugaredFunc => Arg.DesugarExpression().Then(
				aDesugaredArg => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Call(
					Pos,
					aDesugaredFunc,
					aDesugaredArg
				).Do(__ => { __.TypeAnnotation = Type; })
			)
		),
		
		mSPO_AST.tPrefixNode<tPos> { Pos: var Pos, Prefix: var Prefix, Element: var Element, TypeAnnotation: var Type }
		=> Element.DesugarExpression().Then(
			aDesugaredElement => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Prefix(
				Pos,
				Prefix,
				aDesugaredElement
			).Do(__ => { __.TypeAnnotation = Type; })
		),
		
		mSPO_AST.tTupleNode<tPos> { Pos: var Pos, Items: var Items, TypeAnnotation: var Type }
		=> Items.Map(DesugarExpression).WhenAllThen(
			aDesugaredItems => mSPO_AST.Tuple(
				Pos,
				aDesugaredItems
			).Do(__ => { __.TypeAnnotation = Type; })
		),
		
		mSPO_AST.tPairNode<tPos> { Pos: var Pos, Tail: var Tail, Head: var Head, TypeAnnotation: var Type }
		=> Tail.DesugarExpression().ThenTry(
			aDesugaredTail => Head.DesugarExpression().Then(
				aDesugaredHead => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Pair(
					Pos,
					aDesugaredTail,
					aDesugaredHead
				).Do(__ => { __.TypeAnnotation = Type; })
			)
		),
		
		mSPO_AST.tRecordNode<tPos> { Pos: var Pos, Elements: var Elements, TypeAnnotation: var Type }
		=> Elements.Map(
			aElement => aElement.Value.DesugarExpression().Then(
				aDesugaredValue => (Key: aElement.Key, Value: aDesugaredValue)
			)
		).WhenAllThen(
			aDesugaredElements => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Record(
				Pos,
				aDesugaredElements
			).Do(__ => { __.TypeAnnotation = Type; })
		),
		
		mSPO_AST.tIfNode<tPos> { Pos: var Pos, Cases: var Cases, TypeAnnotation: var Type }
		=> Cases.Map(
			aCase => aCase.Cond.DesugarExpression().ThenTry(
				aDesugaredCond => aCase.Result.DesugarExpression().Then(
					aDesugaredResult => (aDesugaredCond, aDesugaredResult)
				)
			)
		).WhenAllThen(
			aDesugaredCases => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.If(
				Pos,
				aDesugaredCases
			).Do(__ => { __.TypeAnnotation = Type; })
		),
		
		mSPO_AST.tIfMatchNode<tPos> { Pos: var Pos, Expression: var Expr, Cases: var Cases, TypeAnnotation: var Type }
		=> Expr.DesugarExpression().ThenTry(
			aDesugaredExpr => Cases.Map(
				aCase => aCase.Expression.DesugarExpression().ThenTry(
					aDesugaredCaseExpression => aCase.Pattern.DesugarPattern().Then(
						aDesugaredCasePattern => (aDesugaredCasePattern, aDesugaredCaseExpression)
					)
				)
			).WhenAllThen(
				aDesugaredCases => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.IfMatch(
					Pos,
					aDesugaredExpr,
					aDesugaredCases
				).Do(__ => { __.TypeAnnotation = Type; })
			)
		),
		
		mSPO_AST.tIsNode<tPos> { Pos: var Pos, Expression: var Expression, Pattern: var Pattern, TypeAnnotation: var Type }
		=> mSPO_AST.IfMatch(
			Pos,
			Expression,
			mStream.Stream(
				(Pattern, (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.True(Pos)),
				(mSPO_AST.Pattern(Pos, mSPO_AST.IgnorePattern(Pos), mStd.cEmpty), mSPO_AST.False(Pos))
			)
		).Do(
			__ => { __.TypeAnnotation = Type; }
		).DesugarExpression(
		),
		
		mSPO_AST.tPipeToRightNode<tPos> { Pos: var Pos, Head: var Head, Pipe: var Pipe, TypeAnnotation: var TypeAnnotation }
		=> mStd.Call(
			() => {
				var Result = Head;
				
				foreach (var Left in Pipe) {
					if (Left is not mSPO_AST.tCallNode<tPos> Call) {
						return mResult.Fail((Pos, $"expect call but is:\n{Left.ToText()}"));
					}
					
					var Func = Call.Func is mSPO_AST.tIdNode<tPos> Id
					? mSPO_AST.Id(Id.Pos, "..." + Id.Id[1..]).Do(__ => { __.TypeAnnotation = Id.TypeAnnotation; })
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
							__ => { __.TypeAnnotation = Call.TypeAnnotation; }
						)
						;
					} else {
						Result = mSPO_AST.Call(
							Pos,
							Func,
							mSPO_AST.Tuple(
								Pos,
								mStream.Stream(Result, Call.Arg)
							)
						).Do(__ => { __.TypeAnnotation = Call.TypeAnnotation; });
					}
				}
				return Result.DesugarExpression();
			}
		),
		
		mSPO_AST.tPipeToLeftNode<tPos> { Pos: var Pos, Head: var Head, Pipe: var Pipe, TypeAnnotation: var TypeAnnotation }
		=> mStd.Call(
			() => {
				var Result = Head;
				
				foreach (var Left in Pipe) {
					if (Left is not mSPO_AST.tCallNode<tPos> Call) {
						return mResult.Fail((Pos, $"expect call but is:\n{Left.ToText()}"));
					}
					
					var Func = Call.Func is mSPO_AST.tIdNode<tPos> Id
					? mSPO_AST.Id(Id.Pos, Id.Id[1..] + "...").Do(__ => { __.TypeAnnotation = Id.TypeAnnotation; })
					: Call.Func;
					
					if (Call.Arg is mSPO_AST.tTupleNode<tPos> Args) {
						Result = mSPO_AST.Call(
							Pos,
							Func,
							mSPO_AST.Tuple(
								Pos,
								mStream.Concat(Args.Items, mStream.Stream(Result))
							)
						).Do(__ => { __.TypeAnnotation = Call.TypeAnnotation; });
					} else {
						Result = mSPO_AST.Call(
							Pos,
							Func,
							mSPO_AST.Tuple(
								Pos,
								mStream.Stream(Call.Arg, Result)
							)
						).Do(__ => { __.TypeAnnotation = Call.TypeAnnotation; });
					}
				}
				return Result.DesugarExpression();
			}
		),
		
		mSPO_AST.tBlockNode<tPos> { Pos: var Pos, Commands: var Commands, TypeAnnotation: var Type }
		=> Commands.Map(DesugarCommand).WhenAllThen(
			aDesugaredCommands => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Block(
				Pos,
				aDesugaredCommands
			).Do(__ => { __.TypeAnnotation = Type; })
		),
		
		mSPO_AST.tTextNode<tPos> Node
		=> mResult.OK(
			mStream.Stream(
				Node.Value.ToCharArray()
			).Map(
				__ => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Prefix(
					Node.Pos,
					"_Char...",
					mSPO_AST.Int(Node.Pos, __)
				)
			).Reduce(
				(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Empty(Node.Pos),
				(aTail, aHead) => mSPO_AST.Pair(Node.Pos, aTail, aHead)
			)
		),
		
		mSPO_AST.tIdNode<tPos> Node => Node,
		
		mSPO_AST.tEmptyNode<tPos> Node => Node,
		
		mSPO_AST.tTrueNode<tPos> Node => Node,
		
		mSPO_AST.tFalseNode<tPos> Node => Node,
		
		mSPO_AST.tIntNode<tPos> Node => Node,
		
		mSPO_AST.tCharNode<tPos> Node => Node,
		
		mSPO_AST.tTypeNode<tPos> Node => mResult.OK((mSPO_AST.tExpressionNode<tPos>)Node.DesugarType()),
		
		_ => throw new System.NotImplementedException(aExpr.GetType().Name)
	};
	
	public static mResult.tResult<mSPO_AST.tCommandNode<tPos>, (tPos Pos, tText ErrorText)>
	DesugarCommand<tPos>(
		this mSPO_AST.tCommandNode<tPos> aCmd
	) => aCmd switch {
		mSPO_AST.tDefNode<tPos> { Pos: var Pos, Des: var Des, Src: var Src }
		=> DesugarExpression(Src).Then(
			aSrc => (mSPO_AST.tCommandNode<tPos>)mSPO_AST.Def(Pos, Des, aSrc)
		),
		
		mSPO_AST.tReturnIfNode<tPos> { Pos: var Pos, Condition: var Cond, Result: var Res }
		=> DesugarExpression(Cond).ThenTry(
			aCond => DesugarExpression(Res).Then(
				aRes => (mSPO_AST.tCommandNode<tPos>)mSPO_AST.ReturnIf(Pos, aCond, aRes)
			)
		),
		
		mSPO_AST.tMethodCallsNode<tPos> { Pos: var Pos, Object: var Obj, MethodCalls: var Calls }
		=> DesugarExpression(Obj).ThenTry(
			NewObj => Calls.Map(
				aCall => DesugarExpression(aCall.Argument).Then(
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
			aListItem => aListItem.Lambda.DesugarExpression(
			).Then(
				aDesugaredLambda => mSPO_AST.RecLambdaItem(
					aListItem.Pos,
					aListItem.Id,
					(mSPO_AST.tLambdaNode<tPos>)aDesugaredLambda
				)
			)
		).WhenAllThen(
			aDesugaredList => (mSPO_AST.tCommandNode<tPos>)mSPO_AST.RecLambdas(
				Pos,
				aDesugaredList
			)
		),
		
		mSPO_AST.tDefVarNode<tPos> => mResult.OK(aCmd),
		_ => throw new System.NotImplementedException(aCmd.GetType().Name)
	};
	
	public static mResult.tResult<mSPO_AST.tModuleNode<tPos>, (tPos Pos, tText ErrorText)>
	DesugarModule<tPos>(
		this mSPO_AST.tModuleNode<tPos> aModule
	) => aModule.Export.Expression.DesugarExpression().ThenTry(
		aDesugaredExpr => aModule.Commands.Map(DesugarCommand).WhenAllThenTry(
			aDesugaredCommands => aModule.Import.Pattern.DesugarPattern().Then(
				aDesugaredPattern => mSPO_AST.Module(
					aModule.Pos,
					mSPO_AST.Import(aModule.Import.Pos, aDesugaredPattern),
					aDesugaredCommands,
					mSPO_AST.Export(aModule.Export.Pos, aDesugaredExpr)
				)
			)
		)
	);
}
