// IMPORT Common/mStd
// IMPORT Common/mAssert
// IMPORT Common/mMaybe
// IMPORT Common/mResult
// IMPORT Common/mError
// IMPORT Common/mStream
// IMPORT mVM_Type
// IMPORT mSPO_AST
// IMPORT mIL_GenerateOpcodes

using tScope = mStream.tStream<(System.String Id, mVM_Type.tType Type)>;

public static class
mSPO_AST_Types {
	public enum tTypeRelation {
		Sub,
		Equal,
		Super,
	}
	
	public static mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>
	UpdateTypes<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aNode,
		tScope aScope
	) => (
		aNode switch {
			mSPO_AST.tEmptyNode<tPos> => mVM_Type.Empty(),
			mSPO_AST.tTrueNode<tPos> => mVM_Type.Bool(),
			mSPO_AST.tFalseNode<tPos> => mVM_Type.Bool(),
			mSPO_AST.tIntNode<tPos> => mVM_Type.Int(),
			mSPO_AST.tIdNode<tPos> IdNode => (
				IdNode.TypeAnnotation.Match(
					_ => _,
					() => (
						IdNode.Id == "_=..."
					) ? (
						mStd.With(mVM_Type.Free(), FreeType => mVM_Type.Proc(FreeType, FreeType, mVM_Type.Empty()))
					) : (
						aScope.Where(
							_ => _.Id == IdNode.Id
						).TryFirst(
						).ElseFail(
							() => (IdNode.Pos, $"No Identifier '{IdNode.Id}' in scope")
						).Then(
							_ => _.Type
						)
					)
				)
			),
			mSPO_AST.tTypeNode<tPos> Type => (
				Type.AsVM_Type(aScope)
			),
			mSPO_AST.tTupleNode<tPos> Tuple => (
				Tuple.Items.Map(
					_ => _.UpdateTypes(aScope)
				).WhenAllThen(
					mVM_Type.Tuple
				)
			),
			mSPO_AST.tPrefixNode<tPos> Prefix => (
				Prefix.Element.UpdateTypes(
					aScope
				).Then(
					_ => mVM_Type.Prefix(Prefix.Prefix, _)
				)
			),
			mSPO_AST.tRecordNode<tPos> Record => (
				Record.Elements.Map(
					_ => _.Value.UpdateTypes(
						aScope
					).Then(
						aType => mVM_Type.Prefix(_.Key.Id, aType)
					)
				).WhenAllThen(
					_ => _.Reduce(
						mVM_Type.Empty(),
						(aTail, aHead) => mVM_Type.Record(aTail, aHead)
					)
				)
			),
			mSPO_AST.tLambdaNode<tPos> Lambda => mStd.Call(
				() => {
					if (Lambda.Generic.IsSome(out var GenericMatch)) {
						// TODO: AI generated code has to be reviewed
						return UpdateMatchTypes(GenericMatch, mVM_Type.Type(), tTypeRelation.Equal, aScope).ThenTry(
							aGen => UpdateMatchTypes(
								Lambda.Head,
								mStd.cEmpty,
								tTypeRelation.Sub,
								aGen.Scope
							).ThenTry(
								aArg => Lambda.Body.UpdateTypes(
									aArg.Scope
								).Then(
									aRes => {
										var Proc = mVM_Type.Proc(mVM_Type.Empty(), aArg.Type, aRes);
										
										if (aGen.Type.IsType(out var OfType) && OfType.IsSome(out var FreeType)) {
											return mVM_Type.Generic(FreeType, Proc);
										} else {
											return Proc;
										}
									}
								)
							)
						);
					}
					
					return UpdateMatchTypes(
						Lambda.Head,
						mStd.cEmpty,
						tTypeRelation.Sub,
						aScope
					).ThenTry(
						aArg => Lambda.Body.UpdateTypes(
							aArg.Scope
						).Then(
							aRes => mVM_Type.Proc(
								mVM_Type.Empty(),
								aArg.Type,
								aRes
							)
						)
					);
				}
			),
			mSPO_AST.tMethodNode<tPos> Method => (
				UpdateMatchTypes(
					Method.Obj,
					mStd.cEmpty,
					tTypeRelation.Equal,
					aScope
				).ThenTry(
					aObj => UpdateMatchTypes(
						Method.Arg,
						mStd.cEmpty,
						tTypeRelation.Sub,
						aObj.Scope
					).ThenTry(
						aArg => Method.Body.UpdateTypes(
							aArg.Scope
						).Then(
							aResType => mVM_Type.Proc(aObj.Type, aArg.Type, aResType)
						)
					)
				)
			),
			mSPO_AST.tBlockNode<tPos> Block => (
				mStd.Call(
					() => {
						var Types = mStream.Stream<mVM_Type.tType>([]);
						var BlockScope = aScope;
						foreach (var Command in Block.Commands) {
							if (!UpdateCommandTypes(Command, BlockScope).Match(out BlockScope, out var Error)) {
								return mResult.Fail(Error);
							}
							
							if (Command is mSPO_AST.tReturnIfNode<tPos> ReturnIf) {
								var Type = ReturnIf.Result.TypeAnnotation.AssertNotEmpty();
								if (Types.All(_ => !Equals(_, Type))) {
									Types = mStream.Stream(Type, Types);
								}
							}
						}
						return mResult.OK(
							Types.Join((a1, a2) => mVM_Type.Set(a2, a1), mVM_Type.Empty())
						).WithErrorType<(tPos Pos, tText ErrorText)>(
						);
					}
				)
			),
			mSPO_AST.tCallNode<tPos> Call => (
				Call.Arg.UpdateTypes(
					aScope
				).ThenTry(
					aArgType => Call.Func.UpdateTypes(
						aScope
					).ThenTry(
						aFuncType => mVM_Type.Infer(
							aFuncType,
							mVM_Type.Empty(),
							aArgType,
							_ => {} // no Tracing
						).ModifyError(
							_ => (Call.Func.Pos, _)
						)
					)
				)
			),
			mSPO_AST.tIfMatchNode<tPos> IfMatch => (
				IfMatch.Expression.UpdateTypes(aScope).ThenTry(
					aMatchType => IfMatch.Cases.Map(
						aCase => UpdateMatchTypes(
							aCase.Match,
							aMatchType,
							tTypeRelation.Super,
							aScope
						).ThenTry(
							_ => aCase.Expression.UpdateTypes(_.Scope)
						)
					).WhenAllThen(
						aCaseTypes => aCaseTypes.Reduce(
							mStream.Stream<mVM_Type.tType>([]),
							(aList, aItem) => aList.All(_ => _ != aItem) ? mStream.Stream(aItem, aList) : aList
						).Reduce(
							(mVM_Type.tType)null!,
							(aTypeSet, aType) => aTypeSet is null ? aType : mVM_Type.Set(aType, aTypeSet)
						)
					)
				)
			),
                        mSPO_AST.tVarToValNode<tPos> VarToVal => (
                                VarToVal.Obj.UpdateTypes(
                                        aScope
                                ).ThenTry(
                                        aObjType => VarToVal.MethodCalls.Reduce(
                                                mResult.OK((Scope: aScope, ObjType: aObjType)).WithErrorType<(tPos Pos, tText ErrorText)>(),
                                                (State, MethodCall) => State.ThenTry(
                                                        aState => UpdateMethodCallTypes(MethodCall, aState.Scope).ThenTry(
                                                                aScopeAfter => {
                                                                        var MethodType = MethodCall.Method.TypeAnnotation.AssertNotEmpty();
                                                                        return MethodType.IsProc(out var _, out var _, out var MethResType)
                                                                                ? mResult.OK((Scope: aScopeAfter, ObjType: MethResType)).WithErrorType<(tPos Pos, tText ErrorText)>()
                                                                                : mResult.Fail((MethodCall.Pos, $"'{MethodType.ToText()}' is not a Proc"));
                                                                }
                                                        )
                                                )
                                        ).ThenTry(
                                                aState => (
                                                        aState.ObjType.IsVar(out var ValType)
                                                        ? mResult.OK(ValType).WithErrorType<(tPos Pos, tText ErrorText)>()
                                                        : mResult.Fail((VarToVal.Pos, $"the type '{aState.ObjType.ToText()}' in not from type '[§VAR ..]'"))
                                                )
                                        )
                                )
                        ),
			mSPO_AST.tIfNode<tPos> If => (
				If.Cases.Map(
					aCase => aCase.Cond.UpdateTypes(
						aScope
					).FailIfNot(
						_ => _ == mVM_Type.Bool(),
						_ => (aCase.Cond.Pos, $"condition '{aCase.Cond.ToText()}' has to be {mVM_Type.Bool().ToText()} but is of type:\n  {_.ToText()}")
					).ThenTry(
						_ => aCase.Result.UpdateTypes(aScope)
					)
				).WhenAllThen(
					a => {
						var X = a.Reduce(
							mStream.Stream<mVM_Type.tType>([]),
							(aList, aItem) => aList.All(_ => _ != aItem)
							? mStream.Stream(aItem, aList)
							: aList
						);
						
						return X.Count() switch {
							0 => mVM_Type.Empty(),
							1 => X.TryFirst().AssertNotEmpty(),
							_ => X.Reduce(
								mVM_Type.Empty(),
								(aTypeSet, aType) => mVM_Type.Set(aType, aTypeSet)
							)
						};
					}
				)
			),
			mSPO_AST.tPipeToLeftNode<tPos> Pipe => throw mError.Error($"'{aNode.GetType().Name}' should be lowered at this point!"),
			_ => throw mError.Error("not implemented: " + aNode.GetType().Name),
		}
	).ThenDo(
		_ => { aNode.TypeAnnotation = _; }
	);
	
	public static mResult.tResult<(mVM_Type.tType Type, tScope Scope), (tPos Pos, tText ErrorText)>
	UpdateMatchTypes<tPos>(
		mSPO_AST.tMatchItemNode<tPos> aMatch,
		mMaybe.tMaybe<mVM_Type.tType> aType,
		tTypeRelation aTypeRelation,
		tScope aScope
	) {
		mResult.tResult<(mVM_Type.tType Type, tScope Scope), (tPos Pos, tText ErrorText)> Result;
		switch (aMatch) {
			case mSPO_AST.tMatchNode<tPos> Match: {
				Result = Match.TypeExpression.Match(
					aType_ => mStd.Call(
						() => aType_.AsVM_Type(aScope).ThenTry(
							aType => UpdateMatchTypes(
								Match.Pattern,
								aType,
								aTypeRelation,
								aScope
							)
						)
					),
					() => UpdateMatchTypes(Match.Pattern, aType, aTypeRelation, aScope)
				);
				break;
			}
			case mSPO_AST.tMatchFreeIdNode<tPos> MatchFreeId: {
				Result = aType.Then(
					a => mStd.Call(
						() => {
							if (a.IsType(out var OfType)) {
								a = OfType.Match(
									() => mVM_Type.Type(mVM_Type.Free(MatchFreeId.Id)),
									aType => a
								);
							}
							
							return aScope.Where(
								_ => _.Id == MatchFreeId.Id
							).TryFirst(
							).Match(
								() => (a, mStream.Stream((MatchFreeId.Id, a), aScope)),
								_ => (
									_.Type.ToText() == a.ToText()
									? (a, aScope)
									: (a, mStream.Stream((MatchFreeId.Id, a), aScope))
								)
							);
						}
					)
				).ElseFail(
					() => (MatchFreeId.Pos, $"missing type for '{MatchFreeId.Id}'")
				);
				break;
			}
			
			case mSPO_AST.tMatchVarNode<tPos> MatchVar: {
				Result = aType.Then(
					a => mStd.Call(
						() => {
							var NewTypeScope = aScope.Where(
								_ => _.Id == MatchVar.Id
							).TryFirst(
							).Match(
								() => {
									var NewType = mVM_Type.Var(a);
									return (Type: NewType, Scope: mStream.Stream((MatchVar.Id, NewType), aScope));
								},
								aScopeItem => {
									var ExistingType = aScopeItem.Type;
									mAssert.IsTrue(
										ExistingType.IsVar(out _),
										() => $"'{ExistingType.ToText()}' is not a var type"
									);
									return (Type: ExistingType, Scope: aScope);
								}
							);
							
							MatchVar.TypeAnnotation = NewTypeScope.Type;
							
							return NewTypeScope;
						}
					)
				).ElseFail(
					() => (MatchVar.Pos, $"missing type for '{MatchVar.Id}'")
				);
				break;
			}
			
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatch: {
				Result = aType.Then(_ => (_, aScope)).ElseFail(() => (IgnoreMatch.Pos, "unknown type"));
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> MatchPrefix: {
				var SubType = mMaybe.None<mVM_Type.tType>();
				if (aType.IsSome(out var Type_)) {
					while (Type_.IsSet(out var Type, out var Types)) {
						if (Type.IsPrefix(out var Prefix, out var SubType_) && Prefix == MatchPrefix.Prefix) {
							SubType = SubType_;
							Type_ = Type;
							break;
						}
						Type_ = Types;
					}
					{
						mAssert.IsTrue(Type_.IsPrefix(out var Prefix, out var SubType__));
						SubType = SubType__;
						mAssert.AreEquals(Prefix, MatchPrefix.Prefix);
					}
				}
				Result = UpdateMatchTypes(MatchPrefix.Match, SubType, aTypeRelation, aScope).Then(
					_ => (mVM_Type.Prefix(MatchPrefix.Prefix, _.Type), _.Scope)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> MatchTuple: {
				var Types = mStream.Stream<mVM_Type.tType>([]);
				var NewScope = aScope;
				if (!aType.IsSome(out var Type)) {
					foreach (var Match in MatchTuple.Items) {
						if (!UpdateMatchTypes(Match, mStd.cEmpty, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
							return mResult.Fail(Error);
						}
						
						NewScope = TS.Scope;
						Types = mStream.Stream(TS.Type, Types);
					}
				} else {
					var WalkType = Type;
					var TypeStack = mStream.Stream<mVM_Type.tType>();
					while (WalkType.IsPair(out var Tail_, out var Head_)) {
						TypeStack = mStream.Stream(Head_, TypeStack);
						WalkType = Tail_;
					}
					if (TypeStack.IsEmpty()) {
						// TODO: this part looks wrong. i expect TypeStack is never empty.
						//   and why should i use aType for each item in the list?
						foreach (var Match in MatchTuple.Items) {
							if (!UpdateMatchTypes(Match, Type, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
								return mResult.Fail(Error);
							}
							
							Types = mStream.Stream(TS.Type, Types);
							NewScope = TS.Scope;
						}
					} else {
						if (!WalkType.IsEmpty() || TypeStack.Count() != MatchTuple.Items.Count()) {
							mResult.Fail((MatchTuple.Pos, $"cant unify '{MatchTuple.ToText()} and '{Type.ToText()}'"));
						}
						
						foreach (var (Match, ItemType) in mStream.ZipShort(MatchTuple.Items, TypeStack)) {
							if (!UpdateMatchTypes(Match, ItemType, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
								return mResult.Fail(Error);
							}
							
							Types = mStream.Stream(TS.Type, Types);
							NewScope = TS.Scope;
						}
					}
				}
				Result = (mVM_Type.Tuple(Types.Reverse()), NewScope);
				break;
			}
			case mSPO_AST.tMatchRecordNode<tPos> MatchRecord: {
				Result = (mVM_Type.Empty(), aScope);
				foreach (var Item in MatchRecord.Elements) {
					var Type = mMaybe.None<mVM_Type.tType>();
					if (aType.IsSome(out var RecordType)) {
						Type = RecordType.GetFieldType(Item.Id.Id);
					}
					
					Result = Result.ThenTry(
						a1 => UpdateMatchTypes(
							Item.Match,
							Type,
							aTypeRelation,
							a1.Scope
						).Then(
							a2 => (mVM_Type.Record(a1.Type, mVM_Type.Prefix(Item.Id.Id, a2.Type)), a2.Scope)
						)
					);
				}
				break;
			}
			case mSPO_AST.tMatchGuardNode<tPos> MatchGuard: {
				if (
					!UpdateMatchTypes(MatchGuard.Match, aType, tTypeRelation.Super, aScope).Match(out var Res, out var Error) ||
					!MatchGuard.Guard.UpdateTypes(Res.Scope).Match(out var BoolRes, out Error)
				) {
					return mResult.Fail(Error);
				}
				
				if (!BoolRes.IsBool()) {
					return mResult.Fail((MatchGuard.Pos, $"return type has to be boolean but is:\n{BoolRes.ToText()}"));
				}
				
				Result = Res;
				// TODO: Result = mVM_Type.Guard(Result, ...);
				break;
			}
			case mSPO_AST.tIdNode<tPos> Id: {
				var Type = aType.IsSome(out var T) ? T : mVM_Type.Free(Id.Id);
				
				Result = (
					Type,
					mStream.Stream((Id: Id.Id, Type: Type), aScope)
				);
				break;
			}
			case mSPO_AST.tExpressionNode<tPos> Expression: {
				Result = Expression.UpdateTypes(aScope).Then(_ => (_, aScope));
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aMatch.GetType().Name);
			}
		}
		return Result.ThenDo(_ => { aMatch.TypeAnnotation = _.Type; });
	}
	
	public static mResult.tResult<tScope, (tPos Pos, tText ErrorText)>
	UpdateMethodCallTypes<tPos>(
		mSPO_AST.tMethodCallNode<tPos> aMethodCall,
		tScope aScope
	) => aMethodCall.Argument.UpdateTypes(aScope).ThenTry(
		aArgType => aMethodCall.Method.UpdateTypes(aScope).ThenTry(
			aMethodType => (
				aMethodType.IsProc(out var MethObjType, out var MethArgType, out var MethResType)
				? mResult.OK((MethObjType, MethArgType, MethResType)).WithErrorType<(tPos, tText)>()
				: mResult.Fail((aMethodCall.Argument.Pos, $"'{aMethodType.ToText()}' is not a Proc"))
			)
		).ThenTry(
			aTypes => aArgType.IsSubType(
				aTypes.MethArgType,
				mStd.cEmpty
			).Then(
				_ => aTypes
			).ModifyError(
				_ => (aMethodCall.Argument.Pos, _)
			)
		).ThenTry(
			_ => (
				!aMethodCall.Result.IsSome(out var T)
				? aScope
				: UpdateMatchTypes(
					T,
					_.MethResType,
					tTypeRelation.Sub,
					aScope
				).Then(_ => _.Scope)
			)
		)
	);
	
	public static mResult.tResult<tScope, (tPos Pos, tText ErrorText)>
	UpdateCommandTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		tScope aScope
	) {
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				return Def.Src.UpdateTypes(aScope).ThenTry(
					aSrcType => {
						var BoundType = Def.Src is mSPO_AST.tTypeNode<tPos>
							? mVM_Type.Type(aSrcType)
							: aSrcType;
						
						return UpdateMatchTypes(
							Def.Des,
							BoundType,
							tTypeRelation.Equal,
							aScope
						).ThenTry(
							aType => BoundType.IsSubType(
								aType.Type,
								mStd.cEmpty
							).Then(
								_ => aType.Scope
							).ModifyError(
								_ => (Def.Src.Pos, _)
							)
						);
					}
				);
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnIf: {
				return ReturnIf.Condition.UpdateTypes(
					aScope
				).FailIfNot(
					aConditionType => aConditionType == mVM_Type.Bool(),
					_ => (ReturnIf.Pos, $"{_.ToText()} != {mIL_GenerateOpcodes.cBoolType}")
				).ThenTry(
					_ => ReturnIf.Result.UpdateTypes(aScope)
				).Then(
					_ => aScope
				);
			}
			case mSPO_AST.tDefVarNode<tPos> DefVar: {
				return DefVar.Expression.UpdateTypes(aScope).ThenTry(
					aValueType => DefVar.MethodCalls.Reduce(
						mResult.OK(aScope).WithErrorType<(tPos Pos, tText ErrorText)>(),
						(Scope, MethodCall) => Scope.ThenTry(a => UpdateMethodCallTypes(MethodCall, a))
					).Then(
						aScope => {
							var Type = mVM_Type.Var(aValueType);
							var NewScope = mStream.Stream((DefVar.Id.Id, Type), aScope);
							DefVar.Id.UpdateTypes(NewScope);
							return NewScope;
						}
					)
				);
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdas: {
				var NewScope = aScope;
				foreach (var Item in RecLambdas.List) {
					var HeadScope = NewScope;
					if (Item.Lambda.Generic.IsSome(out var GenericMatch)) {
						if (!UpdateMatchTypes(GenericMatch, mVM_Type.Type(), tTypeRelation.Equal, HeadScope).Match(out var GenScope, out var GenError)) {
							return mResult.Fail(GenError);
						}
						HeadScope = GenScope.Scope;
					}
					
					if (!UpdateMatchTypes(Item.Lambda.Head, mStd.cEmpty, tTypeRelation.Equal, HeadScope).Match(out var Result, out var Error)) {
						return mResult.Fail(Error);
					}
					
					NewScope = mStream.Stream(
						(
							Item.Id.Id,
							mVM_Type.Proc(
								mVM_Type.Free("__" + Item.Id.Id + "_Obj__"),
								Result.Type,
								mVM_Type.Free("__" + Item.Id.Id + "_Res__")
							)
						),
						NewScope
					);
				}
				
				foreach (var Item in RecLambdas.List) {
					if (
						!NewScope.Where(
							_ => _.Id == Item.Id.Id
						).TryFirst(
						).ElseFail(
							() => (Item.Pos, $"unknown Id '{Item.Id.Id}'")
						).Then(
							_ => _.Type
						).ThenTry(
							DesType => Item.Lambda.UpdateTypes(NewScope).ThenDo(
								SrcType => {
									DesType.Kind = SrcType.Kind;
									DesType.Id = SrcType.Id;
									DesType.Prefix = SrcType.Prefix;
									DesType.Refs = SrcType.Refs;
								}
							)
						).Match(out _, out var Error)
					) {
						return mResult.Fail(Error);
					}
				}
				
				foreach (var Item in RecLambdas.List) {
					if (
						!Item.Lambda.UpdateTypes(NewScope).Then(
							Type => mStream.Stream(
								(
									Item.Id.Id,
									Type
								),
								NewScope
							)
						).Match(out NewScope, out var Error)
					) {
						return mResult.Fail(Error);
					}
				}
				
				return NewScope;
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCalls: {
				return MethodCalls.Object.UpdateTypes(aScope).ThenTry(
					aObjType => MethodCalls.MethodCalls.Reduce(
						mResult.OK(aScope).WithErrorType<(tPos Pos, tText ErrorText)>(),
						(Scope, MethodCall) => Scope.ThenTry(_ => UpdateMethodCallTypes(MethodCall, _))
					)
				);
			}
			default: {
				throw mError.Error("not implemented: " + aCommand.GetType().Name);
			}
		}
	}
	
	public static mMaybe.tMaybe<tText>
	TryGetId<tPos>(
		this mSPO_AST.tMatchNode<tPos> aMatch
	) => TryGetId(aMatch.Pattern);
	
	public static mMaybe.tMaybe<tText>
	TryGetId<tPos>(
		this mSPO_AST.tMatchItemNode<tPos> aMatch
	) => aMatch switch {
		mSPO_AST.tMatchFreeIdNode<tPos> Free => Free.Id,
		mSPO_AST.tIdNode<tPos> IdNode => IdNode.Id,
		mSPO_AST.tMatchNode<tPos> Match => TryGetId(Match),
		_ => mStd.cEmpty,
	};
	
	public static mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>
	AsVM_Type<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpression,
		tScope aScope
	) {
		mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)> Result;
		
		switch (aExpression) {
			case mSPO_AST.tEmptyTypeNode<tPos>: {
				Result = mVM_Type.Empty();
				break;
			}
			case mSPO_AST.tBoolTypeNode<tPos>: {
				Result = mVM_Type.Bool();
				break;
			}
			case mSPO_AST.tIntTypeNode<tPos>: {
				Result = mVM_Type.Int();
				break;
			}
			case mSPO_AST.tTypeTypeNode<tPos>: {
				Result = mVM_Type.Type();
				break;
			}
			case mSPO_AST.tAnyTypeNode<tPos>: {
				Result = mVM_Type.Any();
				break;
			}
			case mSPO_AST.tTupleTypeNode<tPos> TupleType: {
				var Types = mStream.Stream<mVM_Type.tType>([]);
				foreach (var Expression in TupleType.Expressions.Reverse()) {
					if (Expression.AsVM_Type(aScope).Match(out var Type, out var Error)) {
						Types = mStream.Stream(Type, Types);
					} else {
						return mResult.Fail(Error);
					}
				}
				Result = mVM_Type.Tuple(Types);
				break;
			}
			case mSPO_AST.tIdNode<tPos> IdNode: {
				Result = aScope.Where(
					_ => _.Id == IdNode.Id
				).TryFirst(
				).ElseFail(
					() => (IdNode.Pos, $"unknown type of Identifier '{IdNode.Id}'")
				).ThenTry(
					_ => _.Type.IsType(out var OfType)
					? OfType.ElseFail(() => (IdNode.Pos, $"missing type for '{IdNode.Id}'"))
					: mResult.Fail((IdNode.Pos, $"'{IdNode.Id}' is not a type"))
				);
				break;
			}
			case mSPO_AST.tLambdaTypeNode<tPos> LambdaType: {
				Result = LambdaType.ArgType.AsVM_Type(aScope).ThenTry(
					aArgType => LambdaType.ResType.AsVM_Type(aScope).Then(
						aResType => mVM_Type.Proc(mVM_Type.Empty(), aArgType, aResType)
					)
				);
				break;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursiveType: {
				var Name = RecursiveType.HeadType.Id;
				var RecursiveVar = mVM_Type.Free(Name);
				var TempScope = mStream.Stream((Name, mVM_Type.Type(RecursiveVar)), aScope);
				
				Result = RecursiveType.BodyType.UpdateTypes(TempScope).Then(
					_ => mVM_Type.Recursive(RecursiveVar, _)
				);
				break;
			}
			case mSPO_AST.tSetTypeNode<tPos> SetType: {
				Result = SetType.Expressions.Map(
					_ => _.AsVM_Type(aScope)
				).WhenAllThen(
					_ => _.Reduce(
						mStream.Stream<mVM_Type.tType>([]),
						(aList, aItem) => aList.All(_ => _ != aItem) ? mStream.Stream(aItem, aList) : aList
					).Match(
						() => mVM_Type.Empty(),
						(aHead1, aTail1) => aTail1.Match(
							() => aHead1,
							(aHead2, aTail2) => aTail2.Reduce(
								mVM_Type.Set(aHead1, aHead2),
								(aSet, aItem) => mVM_Type.Set(aItem, aSet)
							)
						)
					)
				);
				break;
			}
			case mSPO_AST.tPrefixTypeNode<tPos> PrefixType: {
				Result = PrefixType.Expressions.Map(
					_ => _.AsVM_Type(aScope)
				).WhenAllThen(
					_ => mVM_Type.Prefix(PrefixType.Prefix.Id, mVM_Type.Tuple(_))
				);
				break;
			}
			case mSPO_AST.tVarTypeNode<tPos> VarType: {
				Result = VarType.Type.AsVM_Type(aScope).Then(
					mVM_Type.Var
				);
				break;
			}
			case mSPO_AST.tGenericTypeNode<tPos> GenericType: {
				var Name = GenericType.HeadType.Id;
				var GenericVar = mVM_Type.Free(Name);
				var TempScope = mStream.Stream((Name, mVM_Type.Type(GenericVar)), aScope);
				
				Result = GenericType.BodyType.UpdateTypes(TempScope).Then(
					_ => mVM_Type.Generic(GenericVar, _)
				);
				break;
			}
			case mSPO_AST.tGenericApplyTypeNode<tPos> GenericApplyType: {
				Result = GenericApplyType.GenericType.AsVM_Type(aScope).ThenTry(
					aGenericType => GenericApplyType.ArgType.AsVM_Type(aScope).ThenTry(
						aArgType => aGenericType.IsGeneric(out var Head, out var Body)
						? mResult.OK(Body.Substitute(Head.Id, aArgType)).WithErrorType<(tPos Pos, tText ErrorText)>()
						: mResult.Fail((GenericApplyType.GenericType.Pos, $"expected generic type but '{GenericApplyType.GenericType.ToText()}'"))
					)
				);
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aExpression.GetType().Name);
			}
		}
		return Result.ThenDo(
			_ => { aExpression.TypeAnnotation = _; }
		);
	}
}

