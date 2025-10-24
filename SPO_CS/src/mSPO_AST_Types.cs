// IMPORT Common/mStd
// IMPORT Common/mAssert
// IMPORT Common/mMaybe
// IMPORT Common/mResult
// IMPORT Common/mError
// IMPORT Common/mStream
// IMPORT mVM_Type
// IMPORT mSPO_AST
// IMPORT mIL_GenerateOpcodes

public static class
mSPO_AST_Types {
	public struct tScopeItem {
		public System.String Id;
		public mMaybe.tMaybe<mVM_Type.tType> FreeType;
		public mVM_Type.tType Type;
	}
	
	public static tScopeItem
	ScopeItem(
		tText aId,
		mVM_Type.tType aType,
		mMaybe.tMaybe<mVM_Type.tType> aFreeType
	) => new () {
		Id = aId,
		FreeType = aFreeType,
		Type = aType,
	};
	
	public static tScopeItem
	ScopeItem(
		tText aId,
		mVM_Type.tType aType
	) => new () {
		Id = aId,
		FreeType = mStd.cEmpty,
		Type = aType,
	};
	
	public enum tTypeRelation {
		Sub,
		Equal,
		Super,
	}
	
	static tBool
	TryExtractPairType(
		mVM_Type.tType aType,
		[MaybeNullWhen(false)]out mVM_Type.tType aTailType,
		[MaybeNullWhen(false)]out mVM_Type.tType aHeadType
	) {
		var ToVisit = new System.Collections.Generic.Stack<mVM_Type.tType>();
		var Visited = new System.Collections.Generic.HashSet<tNat64>();
		ToVisit.Push(aType);
		while (ToVisit.Count > 0) {
			var Current = ToVisit.Pop();
			if (!Visited.Add(Current.DebugId)) {
				continue;
			}
			if (Current.IsPair(out aTailType, out aHeadType)) {
				return true;
			}
			if (Current.IsRecursive(out var RecHead, out var RecBody)) {
				mAssert.IsNotNull(RecHead.Id);
				ToVisit.Push(RecBody.Substitute(RecHead.Id, Current));
				continue;
			}
			if (Current.IsSet(out var SetHead, out var SetTail)) {
				ToVisit.Push(SetHead);
				ToVisit.Push(SetTail);
				continue;
			}
			if (Current.IsFree(out _, out var RefType)) {
				ToVisit.Push(RefType);
				continue;
			}
		}
		aTailType = default!;
		aHeadType = default!;
		return false;
	}
	
	public static mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>
	UpdateTypes<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aNode,
		mStream.tStream<tScopeItem> aScope
	) => (
		aNode switch {
			mSPO_AST.tEmptyNode<tPos> => mVM_Type.Empty(),
			mSPO_AST.tTrueNode<tPos> => mVM_Type.Bool(),
			mSPO_AST.tFalseNode<tPos> => mVM_Type.Bool(),
			mSPO_AST.tIntNode<tPos> => mVM_Type.Int(),
			mSPO_AST.tTextNode<tPos> => mVM_Type.Text(),
			mSPO_AST.tCharNode<tPos> => mVM_Type.Char(),
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
			mSPO_AST.tPairNode<tPos> Pair => (
				Pair.Tail.UpdateTypes(
					aScope
				).ThenTry(
					aTail => Pair.Head.UpdateTypes(aScope).Then(
						aHead => mVM_Type.Pair(aTail, aHead)
					)
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
					if (Lambda.Generic.IsSome(out var GenericPattern)) {
						// TODO: AI generated code has to be reviewed
						return UpdatePatternTypes(GenericPattern, mVM_Type.Type(), tTypeRelation.Equal, aScope).ThenTry(
							aGenTypeScope => UpdatePatternTypes(
								Lambda.Head,
								mStd.cEmpty,
								tTypeRelation.Sub,
								aGenTypeScope.Scope
							).ThenTry(
								aArgTypeScope => Lambda.Body.UpdateTypes(
									aArgTypeScope.Scope
								).Then(
									aResTypeScope => {
										var Proc = mVM_Type.Proc(mVM_Type.Empty(), aArgTypeScope.Type, aResTypeScope);
										
										if (aGenTypeScope.Type.IsType()) {
											var T = aGenTypeScope.Scope.Where(
												_ => _.Id == GenericPattern.TryGetId().AssertNotEmpty()
											).TryFirst(
											).AssertNotEmpty(
											).FreeType.AssertNotEmpty(
											);
											
											return mVM_Type.Generic(T, Proc);
										} else {
											return Proc;
										}
									}
								)
							)
						);
					}
					
					return UpdatePatternTypes(
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
				UpdatePatternTypes(
					Method.Obj,
					mStd.cEmpty,
					tTypeRelation.Equal,
					aScope
				).ThenTry(
					aObj => UpdatePatternTypes(
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
					aTypePattern => IfMatch.Cases.Map(
						aCase => UpdatePatternTypes(
							aCase.Pattern,
							aTypePattern,
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
			mSPO_AST.tIsNode<tPos> Is => (
				Is.Expression.UpdateTypes(
					aScope
				).ThenTry(
					aValueType => UpdatePatternTypes(
						Is.Pattern,
						aValueType,
						tTypeRelation.Super,
						aScope
					)
				).Then(
					_ => mVM_Type.Bool()
				)
			),
			mSPO_AST.tVarToValNode<tPos> VarToVal => (
				VarToVal.Obj.UpdateTypes(
					aScope
				).ThenTry(
					_ => (
						_.IsVar(out var ValType)
						? mResult.OK(ValType).WithErrorType<(tPos Pos, tText ErrorText)>()
						: mResult.Fail((VarToVal.Pos, $"the type '{_}' in not from type '[§VAR ...]'"))
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
			mSPO_AST.tPipeToLeftNode<tPos> Pipe => throw mError.Error($"'{aNode.GetType().Name}' should be desugared at this point!"),
			_ => throw mError.Error("not implemented: " + aNode.GetType().Name),
		}
	).ThenDo(
		_ => { aNode.TypeAnnotation = _; }
	);
	
	public static mResult.tResult<(mVM_Type.tType Type, mStream.tStream<tScopeItem> Scope), (tPos Pos, tText ErrorText)>
	UpdatePatternTypes<tPos>(
		mSPO_AST.tPatternNode<tPos> aPattern,
		mMaybe.tMaybe<mVM_Type.tType> aType,
		tTypeRelation aTypeRelation,
		mStream.tStream<tScopeItem> aScope
	) {
		mResult.tResult<(mVM_Type.tType Type, mStream.tStream<tScopeItem> Scope), (tPos Pos, tText ErrorText)> Result;
		switch (aPattern) {
			case mSPO_AST.tTypedPatternNode<tPos> Pattern: {
				Result = Pattern.TypeExpression.Match(
					aType_ => mStd.Call(
						() => aType_.AsVM_Type(aScope).ThenTry(
							aType => UpdatePatternTypes(
								Pattern.Pattern,
								aType,
								aTypeRelation,
								aScope
							)
						)
					),
					() => UpdatePatternTypes(Pattern.Pattern, aType, aTypeRelation, aScope)
				);
				break;
			}
			case mSPO_AST.tFreeIdPatternNode<tPos> FreePatternId: {
				Result = aType.Then(
					a => (
						a.IsType()
						? (
							a,
							mStream.Stream(
								ScopeItem(
									FreePatternId.Id,
									a,
									mVM_Type.Free(FreePatternId.Id)
								),
								aScope
							)
						)
						: (
							a,
							mStream.Stream(
								ScopeItem(
									FreePatternId.Id,
									a
								),
								aScope
							)
						)
					)
				).ElseFail(
					() => (FreePatternId.Pos, $"missing type for '{FreePatternId.Id}'")
				);
				break;
			}
			
			case mSPO_AST.tVarPatternNode<tPos> VarPattern: {
				Result = aType.Then(
					a => mStd.Call(
						() => {
							var NewTypeScope = aScope.Where(
								_ => _.Id == VarPattern.Id
							).TryFirst(
							).Match(
								() => {
									var NewType = mVM_Type.Var(a);
									return (Type: NewType, Scope: mStream.Stream(ScopeItem(VarPattern.Id, NewType), aScope));
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
							
							VarPattern.TypeAnnotation = NewTypeScope.Type;
							
							return NewTypeScope;
						}
					)
				).ElseFail(
					() => (VarPattern.Pos, $"missing type for '{VarPattern.Id}'")
				);
				break;
			}
			
			case mSPO_AST.tIgnorePatternNode<tPos> IgnorePattern: {
				Result = aType.Then(_ => (_, aScope)).ElseFail(() => (IgnorePattern.Pos, "unknown type"));
				break;
			}
			case mSPO_AST.tPrefixPatternNode<tPos> PrefixPattern: {
				var SubType = mMaybe.None<mVM_Type.tType>();
				if (aType.IsSome(out var Type_)) {
					while (Type_.IsSet(out var Type, out var Types)) {
						if (Type.IsPrefix(out var Prefix, out var SubType_) && Prefix == PrefixPattern.Prefix) {
							SubType = SubType_;
							Type_ = Type;
							break;
						}
						Type_ = Types;
					}
					{
						mAssert.IsTrue(Type_.IsPrefix(out var Prefix, out var SubType__));
						SubType = SubType__;
						mAssert.AreEquals(Prefix, PrefixPattern.Prefix);
					}
				}
				Result = UpdatePatternTypes(PrefixPattern.Pattern, SubType, aTypeRelation, aScope).Then(
					_ => (mVM_Type.Prefix(PrefixPattern.Prefix, _.Type), _.Scope)
				);
				break;
			}
			case mSPO_AST.tTuplePatternNode<tPos> TuplePattern: {
				var Types = mStream.Stream<mVM_Type.tType>([]);
				var NewScope = aScope;
				if (!aType.IsSome(out var Type)) {
					foreach (var Pattern in TuplePattern.Items) {
						if (!UpdatePatternTypes(Pattern, mStd.cEmpty, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
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
						foreach (var Pattern in TuplePattern.Items) {
							if (!UpdatePatternTypes(Pattern, Type, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
								return mResult.Fail(Error);
							}
							
							Types = mStream.Stream(TS.Type, Types);
							NewScope = TS.Scope;
						}
					} else {
						if (!WalkType.IsEmpty() || TypeStack.Count() != TuplePattern.Items.Count()) {
							mResult.Fail((TuplePattern.Pos, $"can't unify '{TuplePattern.ToText()} and '{Type.ToText()}'"));
						}
						
						foreach (var (Pattern, ItemType) in mStream.ZipShort(TuplePattern.Items, TypeStack)) {
							if (!UpdatePatternTypes(Pattern, ItemType, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
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
			case mSPO_AST.tPairPatternNode<tPos> PairPattern: {
				var TailType = mMaybe.None<mVM_Type.tType>();
				var HeadType = mMaybe.None<mVM_Type.tType>();
				if (aType.IsSome(out var Type)) {
					if (!TryExtractPairType(Type, out var Tail, out var Head)) {
						return mResult.Fail((PairPattern.Pos, $"cant unify '{PairPattern.ToText()}' and '{Type.ToText()}'"));
					}
					
					TailType = Tail;
					HeadType = Head;
				}
				
				if (!UpdatePatternTypes(PairPattern.Tail, TailType, aTypeRelation, aScope).Match(out var TailRes, out var Error)) {
					return mResult.Fail(Error);
				}
				
				if (!UpdatePatternTypes(PairPattern.Head, HeadType, aTypeRelation, TailRes.Scope).Match(out var HeadRes, out Error)) {
					return mResult.Fail(Error);
				}
				
				Result = (mVM_Type.Pair(TailRes.Type, HeadRes.Type), HeadRes.Scope);
				break;
			}
			case mSPO_AST.tRecordPatternNode<tPos> RecordPattern: {
				Result = (mVM_Type.Empty(), aScope);
				foreach (var Item in RecordPattern.Elements) {
					var Type = mMaybe.None<mVM_Type.tType>();
					if (aType.IsSome(out var RecordType)) {
						Type = RecordType.GetFieldType(Item.Id.Id);
					}
					
					Result = Result.ThenTry(
						a1 => UpdatePatternTypes(
							Item.Pattern,
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
			case mSPO_AST.tGuardPatternNode<tPos> GuardPattern: {
				if (
					!UpdatePatternTypes(GuardPattern.Pattern, aType, tTypeRelation.Super, aScope).Match(out var Res, out var Error) ||
					!GuardPattern.Guard.UpdateTypes(Res.Scope).Match(out var BoolRes, out Error)
				) {
					return mResult.Fail(Error);
				}
				
				if (!BoolRes.IsBool()) {
					return mResult.Fail((GuardPattern.Pos, $"return type has to be boolean but is:\n{BoolRes.ToText()}"));
				}
				
				Result = Res;
				// TODO: Result = mVM_Type.Guard(Result, ...);
				break;
			}
			case mSPO_AST.tIdNode<tPos> Id: {
				var Type = aType.IsSome(out var T) ? T : mVM_Type.Free();
				
				Result = (
					Type,
					mStream.Stream(ScopeItem(Id.Id, Type), aScope)
				);
				break;
			}
			case mSPO_AST.tExpressionNode<tPos> Expression: {
				Result = Expression.UpdateTypes(aScope).Then(_ => (_, aScope));
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aPattern.GetType().Name);
			}
		}
		return Result.ThenDo(_ => { aPattern.TypeAnnotation = _.Type; });
	}
	
	public static mResult.tResult<mStream.tStream<tScopeItem>, (tPos Pos, tText ErrorText)>
	UpdateMethodCallTypes<tPos>(
		mSPO_AST.tMethodCallNode<tPos> aMethodCall,
		mStream.tStream<tScopeItem> aScope
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
				: UpdatePatternTypes(
					T,
					_.MethResType,
					tTypeRelation.Sub,
					aScope
				).Then(_ => _.Scope)
			)
		)
	);
	
	public static mResult.tResult<mStream.tStream<tScopeItem>, (tPos Pos, tText ErrorText)>
	UpdateCommandTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<tScopeItem> aScope
	) {
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				return Def.Src.UpdateTypes(aScope).ThenTry(
					aSrcType => UpdatePatternTypes(
						Def.Des,
						aSrcType,
						tTypeRelation.Equal,
						aScope
					).ThenTry(
						aType => aSrcType.IsSubType(
							aType.Type,
							mStd.cEmpty
						).Then(
							_ => aType.Scope
						).ModifyError(
							_ => (Def.Src.Pos, _)
						)
					)
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
							var NewScope = mStream.Stream(ScopeItem(DefVar.Id.Id, Type), aScope);
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
					if (Item.Lambda.Generic.IsSome(out var GenericPattern)) {
						if (!UpdatePatternTypes(GenericPattern, mVM_Type.Type(), tTypeRelation.Equal, HeadScope).Match(out var GenScope, out var GenError)) {
							return mResult.Fail(GenError);
						}
						HeadScope = GenScope.Scope;
					}
					
					if (!UpdatePatternTypes(Item.Lambda.Head, mStd.cEmpty, tTypeRelation.Equal, HeadScope).Match(out var Result, out var Error)) {
						return mResult.Fail(Error);
					}
					
					NewScope = mStream.Stream(
						ScopeItem(
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
								ScopeItem(
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
		this mSPO_AST.tTypedPatternNode<tPos> aPattern
	) => TryGetId(aPattern.Pattern);
	
	public static mMaybe.tMaybe<tText>
	TryGetId<tPos>(
		this mSPO_AST.tPatternNode<tPos> aPattern
	) => aPattern switch {
		mSPO_AST.tFreeIdPatternNode<tPos> Free => Free.Id,
		mSPO_AST.tIdNode<tPos> IdNode => IdNode.Id,
		mSPO_AST.tTypedPatternNode<tPos> Pattern => TryGetId(Pattern),
		_ => mStd.cEmpty,
	};
	
	public static mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>
	AsVM_Type<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpression,
		mStream.tStream<tScopeItem> aScope
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
			case mSPO_AST.tCharTypeNode<tPos>: {
				Result = mVM_Type.Prefix("_Char...", mVM_Type.Int());
				break;
			}
			case mSPO_AST.tTextTypeNode<tPos>: {
				var FreeType = mVM_Type.Free();
				Result = mVM_Type.Recursive(
					FreeType,
					mVM_Type.Set(
						mVM_Type.Empty(),
						mVM_Type.Pair(
							FreeType,
							mVM_Type.Prefix(
								"_Char...",
								mVM_Type.Int()
							)
						)
					)
				);
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
				foreach (var Expression in TupleType.ItemTypes.Reverse()) {
					if (Expression.AsVM_Type(aScope).Match(out var Type, out var Error)) {
						Types = mStream.Stream(Type, Types);
					} else {
						return mResult.Fail(Error);
					}
				}
				Result = mVM_Type.Tuple(Types);
				break;
			}
			case mSPO_AST.tPairTypeNode<tPos> PairType: {
				Result = PairType.TailType.AsVM_Type(
					aScope
				).ThenTry(
					aTail => PairType.HeadType.AsVM_Type(
						aScope
					).Then(
						aHead => mVM_Type.Pair(aTail, aHead)
					)
				);
				break;
			}
			case mSPO_AST.tIdNode<tPos> IdNode: {
				Result = aScope.Where(
					_ => _.Id == IdNode.Id
				).TryFirst(
				).ElseFail(
					() => (IdNode.Pos, $"unknown type of Identifier '{IdNode.Id}'")
				).ThenTry(
					_ => _.Type.IsType()
					? _.FreeType.ElseFail(() => (IdNode.Pos, "impossible ???"))
					: _.Type
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
				
				Result = RecursiveType.BodyType.UpdateTypes(
					mStream.Stream(
						ScopeItem(Name, RecursiveVar),
						aScope
					)
				).Then(
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
				
				Result = GenericType.BodyType.UpdateTypes(
					mStream.Stream(
						ScopeItem(Name, GenericVar),
						aScope
					)
				).Then(
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
