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
	private const tInt32
	cNoUnnamedArgs = -1;
	
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
		mSPO_AST.tIdNode<tPos> Node => Node,
		mSPO_AST.tSigTypeNode<tPos> Node => mSPO_AST.SigType(
			Node.Pos,
			Node.Head,
			Node.HeadType.DesugarType(),
			Node.BodyType.DesugarType()
		).Do(__ => { __.TypeAnnotation = Node.TypeAnnotation; }),
		
		mSPO_AST.tEmptyTypeNode<tPos> Node => Node,
		mSPO_AST.tTrueNode<tPos> Node => Node,
		mSPO_AST.tFalseNode<tPos> Node => Node,
		mSPO_AST.tIntTypeNode<tPos> Node => Node,
		mSPO_AST.tTypeTypeNode<tPos> Node => Node,
		_ => throw new System.NotImplementedException(aType.GetType().Name)
	};
	
	public static mResult.tResult<
		mSPO_AST.tTypedPatternNode<tPos>,
		(tPos Pos, tText ErrorText)
	>
	DesugarTypedPattern<tPos>(
		this mSPO_AST.tTypedPatternNode<tPos> aTypedPattern
	) => aTypedPattern.Pattern.DesugarPattern().Then(
		aDesugaredPattern => mSPO_AST.Pattern(
			aTypedPattern.Pos,
			aDesugaredPattern,
			aTypedPattern.TypeExpression,
			aTypedPattern.TypeAnnotation
		)
	);
	
	public static mResult.tResult<
		mSPO_AST.tPatternNode<tPos>,
		(tPos Pos, tText ErrorText)
	>
	DesugarPattern<tPos>(
		this mSPO_AST.tPatternNode<tPos> aPattern
	) => aPattern switch {
		mSPO_AST.tIdNode<tPos> Pattern
		=> Pattern,
		
		mSPO_AST.tTypedPatternNode<tPos> Pattern
		=> Pattern.DesugarTypedPattern().Then(__ => (mSPO_AST.tPatternNode<tPos>)__),
		
		mSPO_AST.tGuardPatternNode<tPos> Pattern
		=> Pattern.Pattern.DesugarPattern().ThenTry(
			aDesugaredPattern => Pattern.Guard.DesugarExpression().Then(
				aDesugaredGuard => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.GuardPattern(
					Pattern.Pos,
					aDesugaredPattern,
					aDesugaredGuard,
					Pattern.TypeAnnotation
				)
			)
		),
		
		mSPO_AST.tPairPatternNode<tPos> Pattern
		=> Pattern.Tail.DesugarPattern().ThenTry(
			aDesugaredTail => Pattern.Head.DesugarPattern().Then(
				aDesugaredHead => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.PairPattern(
					Pattern.Pos,
					aDesugaredTail,
					aDesugaredHead,
					Pattern.TypeAnnotation
				)
			)
		),

		mSPO_AST.tSigPatternNode<tPos> Pattern
		=> Pattern.Head.DesugarPattern().ThenTry(
			aHead => Pattern.Body.DesugarPattern().Then(
				aBody => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.SigPattern(
					Pattern.Pos,
					Pattern.Contract.DesugarType(),
					aHead,
					aBody,
					Pattern.TypeAnnotation
				)
			)
		),

		mSPO_AST.tTypePatternNode<tPos> Pattern
		=> mSPO_AST.TypePattern(Pattern.Pos, Pattern.Type.DesugarType()).Do(
			__ => { __.TypeAnnotation = Pattern.TypeAnnotation; }
		),
		
		mSPO_AST.tPrefixPatternNode<tPos> Pattern 
		=> Pattern.Pattern.DesugarPattern().Then(
			aDesugaredPattern => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.PrefixPattern(
				Pattern.Pos,
				Pattern.Prefix,
				aDesugaredPattern,
				Pattern.TypeAnnotation
			)
		),
		
		mSPO_AST.tRecordPatternNode<tPos> Pattern
		=> Pattern.Elements.Map(
			aElement => aElement.Pattern.DesugarPattern().Then(
				aDesugaredPattern => (aElement.Id, aDesugaredPattern)
			)
		).WhenAllThen(
			aDesugaredElements => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.RecordPattern(
				Pattern.Pos,
				aDesugaredElements,
				Pattern.TypeAnnotation
			)
		),
		
		mSPO_AST.tTuplePatternNode<tPos> Pattern
		=> Pattern.Items.Map(
			DesugarPattern
		).WhenAllThen(
			aDesugaredItems => mSPO_AST.WithTypeAnnotation(
				aDesugaredItems.Reduce(
					(mSPO_AST.tPatternNode<tPos>)mSPO_AST.Empty(Pattern.Pos),
					(aAccu, aDesugaredItem) => mSPO_AST.PairPattern(Pattern.Pos, aAccu, aDesugaredItem)
				),
				Pattern.TypeAnnotation
			)
		),
		
		mSPO_AST.tTextNode<tPos> Pattern
		=> mResult.OK(
			mSPO_AST.WithTypeAnnotation(
				mStream.Stream(
					Pattern.Value.ToCharArray()
				).Map(
					__ => (mSPO_AST.tPatternNode<tPos>)mSPO_AST.PrefixPattern(
						Pattern.Pos,
						"_Char...",
						mSPO_AST.Int(Pattern.Pos, __)
					)
				).Reduce(
					(mSPO_AST.tPatternNode<tPos>)mSPO_AST.Empty(Pattern.Pos),
					(aTail, aHead) => mSPO_AST.PairPattern(Pattern.Pos, aTail, aHead)
				),
				Pattern.TypeAnnotation
			)
		),
		
		mSPO_AST.tCharNode<tPos> Pattern
		=> mSPO_AST.PrefixPattern(
			Pattern.Pos,
			"_Char...",
			mSPO_AST.Int(Pattern.Pos, Pattern.Value),
			Pattern.TypeAnnotation
		),
		
		mSPO_AST.tLiteralNode<tPos> Pattern
		=> mResult.OK(
			(mSPO_AST.tPatternNode<tPos>)Pattern
		),
		
		mSPO_AST.tFreeIdPatternNode<tPos> Pattern
		=> Pattern,
		
		mSPO_AST.tVarPatternNode<tPos> Pattern
		=> Pattern,
		
		mSPO_AST.tIgnorePatternNode<tPos> Pattern
		=> Pattern,
		
		_ => throw new System.NotImplementedException(aPattern.GetType().FullName),
	};
	
	public static mResult.tResult<
		mSPO_AST.tExpressionNode<tPos>,
		(tPos Pos, tText ErrorText)
	>
	DesugarExpression<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpr
	) => aExpr.DesugarExpression(cNoUnnamedArgs).Then(__ => __.Expression);
	
	private static mResult.tResult<
		(mStream.tStream<tOut> Values, tInt32 NextArgIndex),
		(tPos Pos, tText ErrorText)
	>
	DesugarAll<tIn, tOut, tPos>(
		mStream.tStream<tIn> aValues,
		tInt32 aNextArgIndex,
		mStd.tFunc<
			tIn,
			tInt32,
			mResult.tResult<
				(tOut Value, tInt32 NextArgIndex),
				(tPos Pos, tText ErrorText)
			>
		> aDesugar
	) {
		var Values = mStream.Stream<tOut>();
		foreach (var Value in aValues) {
			if (!aDesugar(Value, aNextArgIndex).Match(out var Desugared, out var Error)) {
				return mResult.Fail(Error);
			}
			Values = mStream.Stream(Desugared.Value, Values);
			aNextArgIndex = Desugared.NextArgIndex;
		}
		return (Values.Reverse(), aNextArgIndex);
	}
	
	private static mResult.tResult<
		(mSPO_AST.tExpressionNode<tPos> Expression, tInt32 NextArgIndex),
		(tPos Pos, tText ErrorText)
	>
	DesugarExpression<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpr,
		tInt32 aNextArgIndex
	) => aExpr switch {
		mSPO_AST.tShortLambdaNode<tPos> { Pos: var Pos, Body: var Body, TypeAnnotation: var Type }
		=> Body.DesugarExpression(0).ThenTry(
			aDesugaredBody => {
				var ArgumentPatterns = mStream.Stream<mSPO_AST.tPatternNode<tPos>>();
				for (var Index = 0; Index < aDesugaredBody.NextArgIndex; Index += 1) {
					ArgumentPatterns = mStream.Stream(
						mSPO_AST.UnnamedArgId(Pos, Index),
						ArgumentPatterns
					);
				}
				
				return (
					aDesugaredBody.NextArgIndex is 0
					? mSPO_AST.Empty(Pos)
					: mSPO_AST.TuplePattern(Pos, ArgumentPatterns.Reverse())
				).DesugarPattern().Then(
					aDesugaredHead => (
						(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Lambda(
							Pos,
							mStd.cEmpty,
							aDesugaredHead,
							aDesugaredBody.Expression,
							Type
						),
						aNextArgIndex
					)
				);
			}
		),
		
		mSPO_AST.tLambdaNode<tPos> { Pos: var Pos, Generic: var Generic, Head: var Head, Body: var Body, TypeAnnotation: var Type }
		=> Body.DesugarExpression(aNextArgIndex).ThenTry(
			aDesugaredBody => Head.DesugarPattern().Then(
				aDesugaredHead => (
					(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Lambda(
						Pos,
						Generic,
						aDesugaredHead,
						aDesugaredBody.Expression,
						Type
					),
					aDesugaredBody.NextArgIndex
				)
			)
		),
		
		mSPO_AST.tMethodNode<tPos> { Pos: var Pos, Obj: var Obj, Arg: var Arg, Body: var Body, TypeAnnotation: var Type }
		=> Body.DesugarExpression(aNextArgIndex).ThenTry(
			aDesugaredBody => Obj.DesugarPattern().ThenTry(
				aDesugaredObj => Arg.DesugarPattern().Then(
					aDesugaredArg => (
						(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Method(
							Pos,
							aDesugaredObj,
							aDesugaredArg,
							(mSPO_AST.tBlockNode<tPos>)aDesugaredBody.Expression,
							Type
						),
						aDesugaredBody.NextArgIndex
					)
				)
			)
		),
		
		mSPO_AST.tVarToValNode<tPos> { Pos: var Pos, Obj: var Obj, TypeAnnotation: var Type }
		=> Obj.DesugarExpression(aNextArgIndex).Then(
			aDesugaredObj => (
				(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.VarToVal(
					Pos,
					aDesugaredObj.Expression,
					Type
				),
				aDesugaredObj.NextArgIndex
			)
		),
		
		mSPO_AST.tCallNode<tPos> { Pos: var Pos, Func: var Func, Arg: var Arg, TypeAnnotation: var Type }
		=> Func.DesugarExpression(aNextArgIndex).ThenTry(
			aDesugaredFunc => Arg.DesugarExpression(aDesugaredFunc.NextArgIndex).Then(
				aDesugaredArg => (
					(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Call(
						Pos,
						aDesugaredFunc.Expression,
						aDesugaredArg.Expression,
						Type
					),
					aDesugaredArg.NextArgIndex
				)
			)
		),
		
		mSPO_AST.tPrefixNode<tPos> { Pos: var Pos, Prefix: var Prefix, Element: var Element, TypeAnnotation: var Type }
		=> Element.DesugarExpression(aNextArgIndex).Then(
			aDesugaredElement => (
				(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Prefix(
					Pos,
					Prefix,
					aDesugaredElement.Expression,
					Type
				),
				aDesugaredElement.NextArgIndex
			)
		),
		
		mSPO_AST.tTupleNode<tPos> { Pos: var Pos, Items: var Items, TypeAnnotation: var Type }
		=> DesugarAll(Items, aNextArgIndex, (aItem, aNext) => aItem.DesugarExpression(aNext)).Then(
			__ => (
				mSPO_AST.Tuple(Pos, __.Values, Type),
				__.NextArgIndex
			)
		),
		
		mSPO_AST.tPairNode<tPos> { Pos: var Pos, Tail: var Tail, Head: var Head, TypeAnnotation: var Type }
		=> Tail.DesugarExpression(aNextArgIndex).ThenTry(
			aDesugaredTail => Head.DesugarExpression(aDesugaredTail.NextArgIndex).Then(
				aDesugaredHead => (
					(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Pair(
						Pos,
						aDesugaredTail.Expression,
						aDesugaredHead.Expression,
						Type
					),
					aDesugaredHead.NextArgIndex
				)
			)
		),

		mSPO_AST.tSigNode<tPos> { Pos: var Pos, Contract: var Contract, Head: var Head, Body: var Body, TypeAnnotation: var Type }
		=> Head.DesugarExpression(aNextArgIndex).ThenTry(
			aHead => Body.DesugarExpression(aHead.NextArgIndex).Then(
				aBody => (
					(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Sig(
						Pos,
						Contract.DesugarType(),
						aHead.Expression,
						aBody.Expression,
						Type
					),
					aBody.NextArgIndex
				)
			)
		),
		
		mSPO_AST.tRecordNode<tPos> { Pos: var Pos, Elements: var Elements, TypeAnnotation: var Type }
		=> DesugarAll(
			Elements,
			aNextArgIndex,
			(aElement, aNextArgIndex) => aElement.Value.DesugarExpression(aNextArgIndex).Then(
				aDesugaredValue => (
					(Key: aElement.Key, Value: aDesugaredValue.Expression),
					aDesugaredValue.NextArgIndex
				)
			)
		).Then(
			__ => (
				(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Record(
					Pos,
					__.Values,
					Type
				),
				__.NextArgIndex
			)
		),
		
		mSPO_AST.tIfNode<tPos> { Pos: var Pos, Cases: var Cases, TypeAnnotation: var Type }
		=> DesugarAll(
			Cases,
			aNextArgIndex,
			(aCase, aNext) => aCase.Cond.DesugarExpression(aNext).ThenTry(
				aDesugaredCond => aCase.Result.DesugarExpression(aDesugaredCond.NextArgIndex).Then(
					aDesugaredResult => (
						(aDesugaredCond.Expression, aDesugaredResult.Expression),
						aDesugaredResult.NextArgIndex
					)
				)
			)
		).Then(
			__ => (
				(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.If(
					Pos,
					__.Values,
					Type
				),
				__.NextArgIndex
			)
		),
		
		mSPO_AST.tIfMatchNode<tPos> { Pos: var Pos, Expression: var Expr, Cases: var Cases, TypeAnnotation: var Type }
		=> Expr.DesugarExpression(aNextArgIndex).ThenTry(
			aDesugaredExpr => DesugarAll(
				Cases,
				aDesugaredExpr.NextArgIndex,
				(aCase, aNext) => aCase.Expression.DesugarExpression(aNext).ThenTry(
					aDesugaredCaseExpression => aCase.Pattern.DesugarPattern().Then(
						aDesugaredCasePattern => (
							(aDesugaredCasePattern, aDesugaredCaseExpression.Expression),
							aDesugaredCaseExpression.NextArgIndex
						)
					)
				)
			).Then(
				__ => (
					(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.IfMatch(
						Pos,
						aDesugaredExpr.Expression,
						__.Values,
						Type
					),
					__.NextArgIndex
				)
			)
		),
		
		mSPO_AST.tIsNode<tPos> { Pos: var Pos, Expression: var Expression, Pattern: var Pattern, TypeAnnotation: var Type }
		=> mSPO_AST.IfMatch(
			Pos,
			Expression,
			mStream.Stream(
				(Pattern, (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.True(Pos)),
				(mSPO_AST.Pattern(Pos, mSPO_AST.IgnorePattern(Pos), mStd.cEmpty), mSPO_AST.False(Pos))
			),
			Type
		).DesugarExpression(aNextArgIndex),
		
		mSPO_AST.tPipeToRightNode<tPos> { Pos: var Pos, Head: var Head, Pipe: var Pipe, TypeAnnotation: var TypeAnnotation }
		=> mStd.Call(
			() => {
				var Result = Head;
				
				foreach (var Left in Pipe) {
					if (Left is not mSPO_AST.tCallNode<tPos> Call) {
						return mResult.Fail((Pos, $"expect call but is:\n{Left.ToText()}"));
					}
					var Id = Call.Func as mSPO_AST.tIdNode<tPos>;
					
					Result = mSPO_AST.Call(
						Pos,
						(
							Id is not null
							? mSPO_AST.Id(Id.Pos, "..." + Id.Id[1..], Id.TypeAnnotation)
							: Call.Func
						),
						(
							Id is not null && !Id.Id.Contains("...")
							? Result
							: Call.Arg is mSPO_AST.tTupleNode<tPos> Args
							? mSPO_AST.Tuple(
								Pos,
								mStream.Stream(Result, Args.Items)
							)
							: mSPO_AST.Tuple(
								Pos,
								mStream.Stream(Result, Call.Arg)
							)
						),
						Call.TypeAnnotation
					);
				}
				return Result.DesugarExpression(aNextArgIndex);
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
					var Id = Call.Func as mSPO_AST.tIdNode<tPos>;
					
					Result = mSPO_AST.Call(
						Pos,
						(
							Id is not null
							? mSPO_AST.Id(Id.Pos, Id.Id[1..] + "...", Id.TypeAnnotation)
							: Call.Func
						),
						(
							Id is not null && !Id.Id.Contains("...")
							? Result
							: mSPO_AST.Tuple(
								Pos,
								(
									Call.Arg is mSPO_AST.tTupleNode<tPos> Args
									? mStream.Concat(Args.Items, mStream.Stream(Result))
									: mStream.Stream(Call.Arg, Result)
								)
							)
						),
						Call.TypeAnnotation
					);
				}
				
				return Result.DesugarExpression(aNextArgIndex);
			}
		),
		
		mSPO_AST.tBlockNode<tPos> { Pos: var Pos, Commands: var Commands, TypeAnnotation: var Type }
		=> DesugarAll(Commands, aNextArgIndex, (aCommand, aNext) => aCommand.DesugarCommand(aNext)).Then(
			aDesugaredCommands => (
				(mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Block(
					Pos,
					aDesugaredCommands.Values,
					Type
				),
				aDesugaredCommands.NextArgIndex
			)
		),
		
		mSPO_AST.tTextNode<tPos> Node
		=> (
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
			),
			aNextArgIndex
		),
		
		mSPO_AST.tIdNode<tPos> Node
		=> (
			aNextArgIndex is not cNoUnnamedArgs && Node.Id == "_..."
			? (
				mSPO_AST.UnnamedArgId(
					Node.Pos,
					aNextArgIndex,
					Node.TypeAnnotation
				),
				aNextArgIndex + 1
			)
			: (Node, aNextArgIndex)
		),
		
		mSPO_AST.tEmptyNode<tPos> Node
		=> (Node, aNextArgIndex),
		
		mSPO_AST.tTrueNode<tPos> Node
		=> (Node, aNextArgIndex),
		
		mSPO_AST.tFalseNode<tPos> Node
		=> (Node, aNextArgIndex),
		
		mSPO_AST.tIntNode<tPos> Node
		=> (Node, aNextArgIndex),
		
		mSPO_AST.tCharNode<tPos> Node
		=> (Node, aNextArgIndex),
		
		mSPO_AST.tTypeNode<tPos> Node
		=> (
			Node.DesugarType(),
			aNextArgIndex
		),
		
		_ => throw new System.NotImplementedException(aExpr.GetType().Name)
	};
	
	public static mResult.tResult<
		mSPO_AST.tCommandNode<tPos>,
		(tPos Pos, tText ErrorText)
	>
	DesugarCommand<tPos>(
		this mSPO_AST.tCommandNode<tPos> aCmd
	) => aCmd.DesugarCommand(cNoUnnamedArgs).Then(__ => __.Command);
	
	private static mResult.tResult<
		(mSPO_AST.tCommandNode<tPos> Command, tInt32 NextArgIndex),
		(tPos Pos, tText ErrorText)
	>
	DesugarCommand<tPos>(
		this mSPO_AST.tCommandNode<tPos> aCmd,
		tInt32 aNextArgIndex
	) => aCmd switch {
		mSPO_AST.tDefNode<tPos> { Pos: var Pos, Des: var Des, Src: var Src }
		=> Src.DesugarExpression(aNextArgIndex).Then(
			aDesugaredSrc => (
				(mSPO_AST.tCommandNode<tPos>)mSPO_AST.Def(
					Pos,
					Des,
					aDesugaredSrc.Expression
				),
				aDesugaredSrc.NextArgIndex
			)
		),
		
		mSPO_AST.tReturnIfNode<tPos> { Pos: var Pos, Condition: var Cond, Result: var Res }
		=> Cond.DesugarExpression(aNextArgIndex).ThenTry(
			aDesugaredCond => Res.DesugarExpression(aDesugaredCond.NextArgIndex).Then(
				aDesugaredRes => (
					(mSPO_AST.tCommandNode<tPos>)mSPO_AST.ReturnIf(
						Pos,
						aDesugaredCond.Expression,
						aDesugaredRes.Expression
					),
					aDesugaredRes.NextArgIndex
				)
			)
		),
		
		mSPO_AST.tMethodCallsNode<tPos> { Pos: var Pos, Object: var Obj, MethodCalls: var Calls }
		=> Obj.DesugarExpression(aNextArgIndex).ThenTry(
			aDesugaredObj => DesugarAll(
				Calls,
				aDesugaredObj.NextArgIndex,
				(aCall, aNextArgIndex) => aCall.Argument.DesugarExpression(aNextArgIndex).Then(
					aDesugaredArg => (
						mSPO_AST.MethodCall(
							aCall.Pos,
							aCall.Method,
							aDesugaredArg.Expression,
							aCall.Result
						),
						aDesugaredArg.NextArgIndex
					)
				)
			).Then(
				__ => (
					(mSPO_AST.tCommandNode<tPos>)mSPO_AST.MethodCallStatement(
						Pos,
						aDesugaredObj.Expression,
						__.Values
					),
					__.NextArgIndex
				)
			)
		),
		
		mSPO_AST.tRecLambdasNode<tPos> { Pos: var Pos, List: var List }
		=> DesugarAll(
			List,
			aNextArgIndex,
			(aListItem, aNext) => aListItem.Lambda.DesugarExpression(aNext).Then(
				aDesugaredLambda => (
					mSPO_AST.RecLambdaItem(
						aListItem.Pos,
						aListItem.Id,
						(mSPO_AST.tLambdaNode<tPos>)aDesugaredLambda.Expression
					),
					aDesugaredLambda.NextArgIndex
				)
			)
		).Then(
			__ => (
				(mSPO_AST.tCommandNode<tPos>)mSPO_AST.RecLambdas(
					Pos,
					__.Values
				),
				__.NextArgIndex
			)
		),
		
		mSPO_AST.tDefVarNode<tPos>
		=> (aCmd, aNextArgIndex),
		
		_ => throw new System.NotImplementedException(aCmd.GetType().Name)
	};
	
	public static mResult.tResult<
		mSPO_AST.tModuleNode<tPos>,
		(tPos Pos, tText ErrorText)
	>
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
