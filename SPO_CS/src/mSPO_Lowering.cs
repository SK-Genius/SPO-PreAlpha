// IMPORT Common/mStd
// IMPORT Common/mStream
// IMPORT Common/mMaybe
// IMPORT Common/mError
// IMPORT Common/mAssert
// IMPORT Common/mResult
// IMPORT mSPO_AST

public static class
mSPO_Lowering {
	public static mResult.tResult<mSPO_AST.tExpressionNode<tPos>, (tPos Pos, tText ErrorText)>
	LowerExpression<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpr
	) => aExpr switch {
		mSPO_AST.tLambdaNode<tPos> { Pos: var Pos, Generic: var Generic, Head: var Head, Body: var Body, TypeAnnotation: var Type }
		=> Body.LowerExpression().Then(
			aBody => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Lambda(
				Pos,
				Generic,
				Head,
				aBody
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tMethodNode<tPos> { Pos: var Pos, Obj: var Obj, Arg: var Arg, Body: var Body, TypeAnnotation: var Type }
		=> Body.LowerExpression().Then(
			aBody => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Method(
				Pos,
				Obj,
				Arg,
				Body = (mSPO_AST.tBlockNode<tPos>)aBody
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tVarToValNode<tPos> { Pos: var Pos, Obj: var Obj, MethodCalls: var MethodCalls, TypeAnnotation: var Type }
		=> LowerExpression(Obj).ThenTry(
			aObj => MethodCalls.Map(
				aCall => LowerExpression(aCall.Argument).Then(
						aCallArg => mSPO_AST.MethodCall(
							aCall.Pos,
							aCall.Method,
							aCallArg,
							aCall.Result.Then(
								aCallResult => mSPO_AST.Match(
									aCallResult.Pos,
									aCallResult.Pattern,
									aCallResult.TypeExpression.Then(
										aCallResultTypeExpression => LowerExpression(aCallResultTypeExpression).ElseThrow(""))
								)
							)
						)
				)
			).WhenAllThen(
				aCalls => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.VarToVal(
					Pos,
					aObj,
					aCalls
				).Do(_ => { _.TypeAnnotation = Type; })
			)
		)
)
),
		mSPO_AST.tCallNode<tPos> { Pos: var Pos, Func: var Func, Arg: var Arg, TypeAnnotation: var Type }
		=> LowerExpression(Func).ThenTry(
			aFunc => LowerExpression(Arg).Then(
				aArg => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Call(
					Pos,
					aFunc,
					aArg
				).Do(_ => { _.TypeAnnotation = Type; })
			)
		),
		mSPO_AST.tPrefixNode<tPos> { Pos: var Pos, Prefix: var Prefix, Element: var Element, TypeAnnotation: var Type }
		=> LowerExpression(Element).Then(
			aElement => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Prefix(
				Pos,
				Prefix,
				aElement
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tTupleNode<tPos> { Pos: var Pos, Items: var Items, TypeAnnotation: var Type }
		=> Items.Map(LowerExpression).WhenAllThen(
			aItems => mSPO_AST.Tuple(
				Pos,
				aItems
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tRecordNode<tPos> { Pos: var Pos, Elements: var Elements, TypeAnnotation: var Type }
		=> Elements.Map(
			aElement => LowerExpression(aElement.Value).Then(
				aValue => (Key: aElement.Key, Value: aValue)
			)
		).WhenAllThen(
			aElements => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Record(
				Pos,
				aElements
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tIfNode<tPos> { Pos: var Pos, Cases: var Cases, TypeAnnotation: var Type }
		=> Cases.Map(
			aCase => LowerExpression(aCase.Cond).ThenTry(
				aCond => LowerExpression(aCase.Result).Then(
					aResult => (aCond, aResult)
				)
			)
		).WhenAllThen(
			aCases => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.If(
				Pos,
				aCases
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tIfMatchNode<tPos> { Pos: var Pos, Expression: var Expr, Cases: var Cases, TypeAnnotation: var Type }
		=> LowerExpression(Expr).ThenTry(
			aExpr => Cases.Map(
				aCase => LowerExpression(aCase.Expression).Then(
					aCaseExpression => (aCase.Match, aCaseExpression)
				)
			).WhenAllThen(
				aCases => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.IfMatch(
					Pos,
					aExpr,
					aCases
				).Do(_ => { _.TypeAnnotation = Type; })
			)
		),
		mSPO_AST.tPipeToRightNode<tPos> Pipe
		=> mStd.Call(
			() => {
				var Result = (mSPO_AST.tExpressionNode<tPos>)Pipe.Head;
				
				foreach (var Left in Pipe.Pipe) {
					if (Left is not mSPO_AST.tCallNode<tPos> Call) {
						return mResult.Fail((Pipe.Pos, $"expect call but is:\n{Left.ToText()}"));
					}
					
					var Func = Call.Func is mSPO_AST.tIdNode<tPos> Id
					? mSPO_AST.Id(Id.Pos, "..." + Id.Id[1..]).Do(_ => { _.TypeAnnotation = Id.TypeAnnotation; })
					: Call.Func;
					
					if (Call.Arg is mSPO_AST.tTupleNode<tPos> Args) {
						Result = mSPO_AST.Call(
							Pipe.Pos,
							Func,
							mSPO_AST.Tuple(
								Pipe.Pos,
								mStream.Stream(Result, Args.Items)
							)
						).Do(_ => { _.TypeAnnotation = Call.TypeAnnotation; });
					} else {
						Result = mSPO_AST.Call(
							Pipe.Pos,
							Func,
							mSPO_AST.Tuple(
								Pipe.Pos,
								mStream.Stream([Result, Call.Arg])
							)
						).Do(_ => { _.TypeAnnotation = Call.TypeAnnotation; });
					}
				}
				return mResult.OK(Result).WithErrorType<(tPos Pos, tText ErrorText)>();
			}
		),
		mSPO_AST.tPipeToLeftNode<tPos> Pipe
		=> mStd.Call(
			() => {
				var Result = (mSPO_AST.tExpressionNode<tPos>)Pipe.Head;
				
				foreach (var Left in Pipe.Pipe) {
					if (Left is not mSPO_AST.tCallNode<tPos> Call) {
						return mResult.Fail((Pipe.Pos, $"expect call but is:\n{Left.ToText()}"));
					}
					
					var Func = Call.Func is mSPO_AST.tIdNode<tPos> Id
					? mSPO_AST.Id(Id.Pos, Id.Id[1..] + "...").Do(_ => { _.TypeAnnotation = Id.TypeAnnotation; })
					: Call.Func;
					
					if (Call.Arg is mSPO_AST.tTupleNode<tPos> Args) {
						Result = mSPO_AST.Call(
							Pipe.Pos,
							Func,
							mSPO_AST.Tuple(
								Pipe.Pos,
								mStream.Concat(Args.Items, mStream.Stream([Result]))
							)
						).Do(_ => { _.TypeAnnotation = Call.TypeAnnotation; });
					} else {
						Result = mSPO_AST.Call(
							Pipe.Pos,
							Func,
							mSPO_AST.Tuple(
								Pipe.Pos,
								mStream.Stream([Call.Arg, Result])
							)
						).Do(_ => { _.TypeAnnotation = Call.TypeAnnotation; });
					}
				}
				return mResult.OK(Result).WithErrorType<(tPos Pos, tText ErrorText)>();
			}
		),
		mSPO_AST.tBlockNode<tPos> { Pos: var Pos, Commands: var Commands, TypeAnnotation: var Type }
		=> Commands.Map(LowerCommand).WhenAllThen(
			aCommands => (mSPO_AST.tExpressionNode<tPos>)mSPO_AST.Block(
				Pos,
				aCommands
			).Do(_ => { _.TypeAnnotation = Type; })
		),
		mSPO_AST.tIdNode<tPos> Node => Node, 
		mSPO_AST.tEmptyNode<tPos> Node => Node, 
		mSPO_AST.tTrueNode<tPos> Node => Node, 
		mSPO_AST.tFalseNode<tPos> Node => Node, 
		mSPO_AST.tIntNode<tPos> Node => Node, 
		mSPO_AST.tIntTypeNode<tPos> Node => Node, 
		mSPO_AST.tTypeTypeNode<tPos> Node => Node, 
		mSPO_AST.tSetTypeNode<tPos> Node => Node, 
		mSPO_AST.tVarTypeNode<tPos> Node => Node, 
		mSPO_AST.tBoolTypeNode<tPos> Node => Node, 
		mSPO_AST.tEmptyTypeNode<tPos> Node => Node, 
		mSPO_AST.tTupleTypeNode<tPos> Node => Node, 
		mSPO_AST.tLambdaTypeNode<tPos> Node => Node, 
		mSPO_AST.tPrefixTypeNode<tPos> Node => Node, 
		mSPO_AST.tGenericTypeNode<tPos> Node => Node, 
		mSPO_AST.tInterfaceTypeNode<tPos> Node => Node, 
		mSPO_AST.tRecursiveTypeNode<tPos> Node => Node, 
		mSPO_AST.tGenericApplyTypeNode<tPos> Node => Node, 
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
						aCall.Result.Then(
							aCallResult => mSPO_AST.Match(
								aCallResult.Pos,
								aCallResult.Pattern,
								aCallResult.TypeExpression.Then(
									aCallResultTypeExpression => LowerExpression(aCallResultTypeExpression).ElseThrow("") // TODO: remove ElseThrow()
								)
							)
						)
					)
				)
			).WhenAllThen(
				aCalls => (mSPO_AST.tCommandNode<tPos>)mSPO_AST.MethodCallStatement(Pos, NewObj, aCalls)
			)
		),
		mSPO_AST.tRecLambdasNode<tPos> => mResult.OK(aCmd),
		mSPO_AST.tDefVarNode<tPos> => mResult.OK(aCmd),
		_ => throw new System.NotImplementedException(aCmd.GetType().Name)
	};
	
	public static mResult.tResult<mSPO_AST.tModuleNode<tPos>, (tPos Pos, tText ErrorText)>
	LowerModule<tPos>(
		this mSPO_AST.tModuleNode<tPos> aModule
	) => LowerExpression(aModule.Export.Expression).ThenTry(
		aExpr => aModule.Commands.Map(LowerCommand).WhenAllThen(
			aCommands => mSPO_AST.Module(
				aModule.Pos,
				aModule.Import,
				aCommands,
				mSPO_AST.Export(aModule.Export.Pos, aExpr)
			)
		)
	);
}
