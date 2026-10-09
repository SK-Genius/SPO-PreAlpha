#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mAssert.cs
#:ref Common/mMaybe.cs
#:ref Common/mResult.cs
#:ref Common/mError.cs
#:ref Common/mStream.cs
#:ref Common/mTreeMap.cs
#:ref mVM_Type.cs
#:ref mSPO_AST.cs
#:ref mIL_GenerateOpcodes.cs

public static class
mSPO_AST_Types {
	// Updates share unchanged tree branches; only successful results publish a new snapshot.
	public sealed record
	tTypeState<tPos> {
		public mTreeMap.tTree<
			tInt32,
			mStream.tStream<(mSPO_AST.tNode<tPos> Node, mVM_Type.tType Type)>
		>
		ValidatedTypes { get; init; } = mTreeMap.Tree<
			tInt32,
			mStream.tStream<(mSPO_AST.tNode<tPos> Node, mVM_Type.tType Type)>
		>(
			(a1, a2) => a1.CompareTo(a2),
			[]
		);
	}
	
	public static mMaybe.tMaybe<mVM_Type.tType>
	TryGetValidatedType<tPos>(
		this tTypeState<tPos> aTypeState,
		mSPO_AST.tNode<tPos> aNode
	) {
		var Key = RuntimeHelpers.GetHashCode(aNode);
		if (!aTypeState.ValidatedTypes.TryGet(Key).IsSome(out var Bucket)) {
			return mStd.cEmpty;
		}
		return Bucket.Where(
			__ => mStd.RefEq(__.Node, aNode)
		).TryFirst(
		).Then(
			__ => __.Type
		);
	}
	
	public static tTypeState<tPos>
	SetValidatedType<tPos>(
		this tTypeState<tPos> aTypeState,
		mSPO_AST.tNode<tPos> aNode,
		mVM_Type.tType aType
	) {
		var Key = RuntimeHelpers.GetHashCode(aNode);
		var Bucket = mStream.Stream<(mSPO_AST.tNode<tPos> Node, mVM_Type.tType Type)>();
		if (aTypeState.ValidatedTypes.TryGet(Key).IsSome(out var Existing)) {
			Bucket = Existing.Where(
				__ => !mStd.RefEq(__.Node, aNode)
			);
		}
		return aTypeState with {
			ValidatedTypes = aTypeState.ValidatedTypes.Set(
				Key,
				mStream.Stream((aNode, aType), Bucket)
			)
		};
	}
	
	public struct tScopeItem {
		public tText Id;
		public mMaybe.tMaybe<mVM_Type.tType> TypeValue;
		public mVM_Type.tType Type;
	}
	
	public static tScopeItem
	ScopeItem(
		tText aId,
		mVM_Type.tType aType,
		mMaybe.tMaybe<mVM_Type.tType> aTypeValue
	) => new () {
		Id = aId,
		TypeValue = aTypeValue,
		Type = aType,
	};
	
	public static tScopeItem
	ScopeItem(
		tText aId,
		mVM_Type.tType aType
	) => new () {
		Id = aId,
		TypeValue = mStd.cEmpty,
		Type = aType,
	};
	
	public enum tTypeRelation {
		Sub,
		Equal,
		Super,
	}
	
	public static (
		mMaybe.tMaybe<mVM_Type.tType> Matched,
		mMaybe.tMaybe<mVM_Type.tType> Remaining
	)
	SplitForPatternType<tPos>(
		this mVM_Type.tType aType,
		mSPO_AST.tPatternNode<tPos> aPattern,
		tTypeState<tPos> aTypeState
	) {
		switch (aPattern) {
			case mSPO_AST.tGuardPatternNode<tPos> Guard: {
				return (
					aType.SplitForPatternType(Guard.Pattern, aTypeState).Matched,
					aType
				);
			}
			case mSPO_AST.tFreeIdPatternNode<tPos>:
			case mSPO_AST.tIgnorePatternNode<tPos>:
			case mSPO_AST.tIdNode<tPos>:
			case mSPO_AST.tVarPatternNode<tPos>: {
				return (aType, mStd.cEmpty);
			}
			case mSPO_AST.tPatternNode<tPos> when aType.IsAny():
			case mSPO_AST.tPatternNode<tPos> when aType.IsFree(out _, out var Ref) && ReferenceEquals(aType, Ref): {
				mVM_Type.tType
				PatternType(
					mSPO_AST.tPatternNode<tPos> aPattern_,
					mVM_Type.tType aFallback
				) => aTypeState.TryGetValidatedType(aPattern_).ElseUse(
					aPattern_ switch {
						mSPO_AST.tTypedPatternNode<tPos> Typed_ => PatternType(Typed_.Pattern, aFallback),
						mSPO_AST.tGuardPatternNode<tPos> Guard_ => PatternType(Guard_.Pattern, aFallback),
						mSPO_AST.tEmptyNode<tPos> => mVM_Type.Empty(),
						mSPO_AST.tTrueNode<tPos> => mVM_Type.True(),
						mSPO_AST.tFalseNode<tPos> => mVM_Type.False(),
						mSPO_AST.tIntNode<tPos> => mVM_Type.Int(),
						mSPO_AST.tPairPatternNode<tPos> Pair_ => mVM_Type.Pair(
							PatternType(Pair_.Tail, aFallback),
							PatternType(Pair_.Head, aFallback)
						),
						mSPO_AST.tPrefixPatternNode<tPos> Prefix_ => mVM_Type.Prefix(
							Prefix_.Prefix,
							PatternType(Prefix_.Pattern, aFallback)
						),
						mSPO_AST.tRecordPatternNode<tPos> Record_ => Record_.Elements.Reduce(
							mVM_Type.Empty(),
							(Type_, Field) => mVM_Type.Record(
								Type_,
								mVM_Type.Prefix(
									Field.Id.Id,
									PatternType(Field.Pattern, aFallback)
								)
							)
						),
						_ => aFallback,
					}
				);
				
				var Matched = PatternType(aPattern, aType);
				return (
					Matched,
					Matched == aType ? mStd.cEmpty : aType
				);
			}
			case mSPO_AST.tPatternNode<tPos> when aType.IsRecursive(out var RecursiveHead, out var RecursiveBody): {
				var Expanded = RecursiveBody.Substitute(RecursiveHead, aType);
				
				return (
					ReferenceEquals(Expanded, aType)
					? (aType, aType)
					: Expanded.SplitForPatternType(aPattern, aTypeState)
				);
			}
			case mSPO_AST.tPatternNode<tPos> when aType.IsSet(out var Type1, out var Type2): {
				var Coverage1 = Type1.SplitForPatternType(aPattern, aTypeState);
				var Coverage2 = Type2.SplitForPatternType(aPattern, aTypeState);
				
				return (
					mVM_Type.Union(Coverage1.Matched, Coverage2.Matched),
					mVM_Type.Union(Coverage1.Remaining, Coverage2.Remaining)
				);
			}
			case mSPO_AST.tSigPatternNode<tPos> Sig: {
				if (!aType.IsSig(out _, out _)) {
					return (mStd.cEmpty, aType);
				}
				if (!Sig.Contract.AsVM_Type(mStd.cEmpty).Match(out var Contract, out _)) {
					return (aType, aType);
				}
				if (!aType.IsSubType(Contract).Match(out _, out _)) {
					return (mStd.cEmpty, aType);
				}
				var Head = Sig.Head is mSPO_AST.tTypedPatternNode<tPos> TypedHead ? TypedHead.Pattern : Sig.Head;
				if (!Sig.HeadValue.IsSome(out var HeadValue) || !aTypeState.TryGetValidatedType(Sig.Body).IsSome(out _)) {
					return (aType, aType);
				}
				var Body = Contract.Refs[1].Substitute(Contract.Refs[0], HeadValue);
				var Coverage = Body.SplitForPatternType(Sig.Body, aTypeState);
				if (Coverage.Matched.IsNone()) {
					return (mStd.cEmpty, aType);
				}
				return (
					Contract,
					Head is mSPO_AST.tTypePatternNode<tPos> || Coverage.Remaining.IsSome(out _)
						? aType
						: mStd.cEmpty
				);
			}
			case mSPO_AST.tTypedPatternNode<tPos> Typed: {
				if (
					Typed.TypeExpression.IsSome(out _) &&
					Typed.Pattern is mSPO_AST.tFreeIdPatternNode<tPos> &&
					aTypeState.TryGetValidatedType(Typed).IsSome(out var MatchType)
				) {
					if (aType.IsSubType(MatchType).Match(out _, out _)) {
						return (aType, mStd.cEmpty);
					}
					
					return (
						MatchType.IsSubType(aType).Match(out _, out _)
						? (MatchType, aType)
						: (mStd.cEmpty, aType)
					);
				}
				return aType.SplitForPatternType(Typed.Pattern, aTypeState);
			}
			case mSPO_AST.tPairPatternNode<tPos> Pair: {
				if (!aType.IsPair(out var FirstType, out var SecondType)) {
					return (mStd.cEmpty, aType);
				}
				
				var FirstCoverage = FirstType.SplitForPatternType(Pair.Tail, aTypeState);
				var SecondCoverage = SecondType.SplitForPatternType(Pair.Head, aTypeState);
				
				if (
					!FirstCoverage.Matched.IsSome(out var MatchedFirst) ||
					!SecondCoverage.Matched.IsSome(out var MatchedSecond)
				) {
					return (mStd.cEmpty, aType);
				}
				
				var MatchedPair = mVM_Type.Pair(MatchedFirst, MatchedSecond);
				
				if (
					(FirstCoverage.Remaining.IsSome(out var RemainingFirst) && RemainingFirst == FirstType) ||
					(SecondCoverage.Remaining.IsSome(out var RemainingSecond) && RemainingSecond == SecondType)
				) {
					return (MatchedPair, aType);
				}
				
				var Remaining = FirstCoverage.Remaining.Then(__ => mVM_Type.Pair(__, SecondType));
				if (SecondCoverage.Remaining.IsSome(out RemainingSecond)) {
					Remaining = mVM_Type.Union(
						Remaining,
						mVM_Type.Pair(MatchedFirst, RemainingSecond)
					);
				}
				
				return (MatchedPair, Remaining);
			}
			case mSPO_AST.tPrefixPatternNode<tPos> Prefix: {
				if (!aType.IsPrefix(Prefix.Prefix, out var InnerType)) {
					return (mStd.cEmpty, aType);
				}
				
				var Coverage = InnerType.SplitForPatternType(Prefix.Pattern, aTypeState);
				
				return (
					Coverage.Matched.Then(__ => mVM_Type.Prefix(Prefix.Prefix, __)),
					(
						Coverage.Remaining.IsSome(out var RemainingInner) && RemainingInner == InnerType
						? mMaybe.Some(aType)
						: Coverage.Remaining.Then(__ => mVM_Type.Prefix(Prefix.Prefix, __))
					)
				);
			}
			case mSPO_AST.tRecordPatternNode<tPos> Record: {
				if (!aType.IsRecord(out var Fields)) {
					return (
						aType.IsFree(out _, out _)
						? (aType, aType)
						: (mStd.cEmpty, aType)
					);
				}
				
				var MatchedFields = Fields;
				var Remaining = mMaybe.None<mVM_Type.tType>();
				
				foreach (var (Id, Pattern) in Record.Elements) {
					if (!Fields.TryGet(Id.Id).IsSome(out var FieldType)) {
						return (mStd.cEmpty, aType);
					}
					
					var Coverage = FieldType.SplitForPatternType(Pattern, aTypeState);
					if (!Coverage.Matched.IsSome(out var MatchedField)) {
						return (mStd.cEmpty, aType);
					}
					
					if (Coverage.Remaining.IsSome(out var RemainingField)) {
						Remaining = mVM_Type.Union(
							Remaining,
							mVM_Type.Record(
								MatchedFields.Set(Id.Id, RemainingField).ToStream(
								).Map(
									__ => (__.Key, __.Value)
								).ToArrayList(
								).ToArray()
							)
						);
					}
					
					MatchedFields = MatchedFields.Set(Id.Id, MatchedField);
				}
				
				return (
					mVM_Type.Record(
						MatchedFields.ToStream(
						).Map(
							__ => (__.Key, __.Value)
						).ToArrayList(
						).ToArray()
					),
					Remaining
				);
			}
			case mSPO_AST.tIntNode<tPos>: {
				return (
					aType.IsInt() ? (aType, aType) :
					aType.IsFree(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			case mSPO_AST.tEmptyNode<tPos>: {
				return (
					aType.IsEmpty() ? (aType, mStd.cEmpty) :
					aType.IsFree(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			case mSPO_AST.tTrueNode<tPos>: {
				return (
					aType.Kind is mVM_Type.tKind.True ? (aType, mStd.cEmpty) :
					aType.IsFree(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			case mSPO_AST.tFalseNode<tPos>: {
				return (
					aType.Kind is mVM_Type.tKind.False ? (aType, mStd.cEmpty) :
					aType.IsFree(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			default: {
				return (aType, aType);
			}
		}
	}
	
	private static (tPos Pos, tText ErrorText)
	TypeErrorAt<tPos>(
		mSPO_AST.tExpressionNode<tPos> aExpression,
		mVM_Type.tType aExpected,
		tTypeState<tPos> aState,
		tText aError
	) {
		tBool TryChild(
			mSPO_AST.tExpressionNode<tPos> aChild,
			mVM_Type.tType aChildExpected,
			out (tPos Pos, tText ErrorText) aChildError
		) {
			if (
				aState.TryGetValidatedType(aChild).IsSome(out var Actual) &&
				!Actual.IsSubType(aChildExpected).Match(out _, out var Detail)
			) {
				aChildError = TypeErrorAt(aChild, aChildExpected, aState, Detail);
				return true;
			}
			aChildError = default;
			return false;
		}
		
		if (aExpression is mSPO_AST.tRecordNode<tPos> Record && aExpected.IsRecord(out var Fields)) {
			foreach (var (Key, Value) in Record.Elements) {
				if (Fields.TryGet(Key.Id).IsSome(out var Expected) && TryChild(Value, Expected, out var Error)) {
					return (Error.Pos, $"Field '{Key.Id}': {Error.ErrorText}");
				}
			}
		}
		if (aExpression is mSPO_AST.tTupleNode<tPos> Tuple) {
			var Expected = aExpected;
			foreach (var Item in Tuple.Items.Reverse()) {
				if (!Expected.IsPair(out var Tail, out var Head)) {
					break;
				}
				if (TryChild(Item, Head, out var Error)) {
					return Error;
				}
				Expected = Tail;
			}
		}
		if (
			aExpression is mSPO_AST.tPairNode<tPos> Pair &&
			aExpected.IsPair(out var PairTail, out var PairHead)
		) {
			if (TryChild(Pair.Head, PairHead, out var Error) || TryChild(Pair.Tail, PairTail, out Error)) {
				return Error;
			}
		}
		return (aExpression.Pos, aError);
	}
	
	private static mResult.tResult<
		(
			mVM_Type.tType Type,
			mTreeMap.tTree<mVM_Type.tType, mVM_Type.tType> Mappings,
			tTypeState<tPos> State
		),
		(tPos Pos, tText ErrorText)
	>
	TryInferArgument<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aArgument,
		mVM_Type.tType aExpectedType,
		mTreeMap.tTree<mVM_Type.tType, mVM_Type.tType> aMappings,
		mStream.tStream<tScopeItem> aScope,
		tTypeState<tPos> aTypeState
	) {
		static tBool
		HasUnresolvedFreeType(
			mVM_Type.tType aType,
			mStream.tStream<mVM_Type.tType> aBoundTypes
		) {
			switch (aType.Kind) {
				case mVM_Type.tKind.Abstract: return false;
				case mVM_Type.tKind.Free: {
					return ReferenceEquals(aType, aType.Refs[0])
					? !aBoundTypes.Any(__ => ReferenceEquals(__, aType))
					: HasUnresolvedFreeType(aType.Refs[0], aBoundTypes);
				}
				case mVM_Type.tKind.Recursive:
				case mVM_Type.tKind.Generic:
				case mVM_Type.tKind.Sig:
				case mVM_Type.tKind.Interface: {
					return HasUnresolvedFreeType(
						aType.Refs[1],
						mStream.Stream(aType.Refs[0], aBoundTypes)
					);
				}
				case mVM_Type.tKind.Record: {
					return aType.Fields.ToStream().Any(
						__ => HasUnresolvedFreeType(__.Value, aBoundTypes)
					);
				}
				default: {
					return mStream.Stream(
						System.MemoryExtensions.AsSpan(aType.Refs)
					).Any(
						__ => HasUnresolvedFreeType(__, aBoundTypes)
					);
				}
			}
		}
		
		var MappedExpectedType = aExpectedType.ApplyMappings(aMappings);
		if (!aArgument.UpdateTypes(aScope, aTypeState).Match(out var Checked, out var Error)) {
			var LambdaType = MappedExpectedType;
			while (LambdaType.IsGeneric(out _, out var InnerType)) {
				LambdaType = InnerType;
			}
			
			if (
				aArgument is not mSPO_AST.tLambdaNode<tPos> Lambda ||
				Lambda.Generic.IsSome(out _) ||
				!LambdaType.IsProc(out _, out var LambdaArgType, out var LambdaExpectedResultType) ||
				HasUnresolvedFreeType(LambdaArgType, mStd.cEmpty)
			) {
				return mResult.Fail(Error);
			}
			
			if (
				!UpdatePatternTypes(
					Lambda.Head,
					LambdaArgType,
					tTypeRelation.Sub,
					aScope,
					aTypeState
				).Match(out var LambdaArg, out Error) ||
				!Lambda.Body.TryInferArgument(
					LambdaExpectedResultType,
					aMappings,
					LambdaArg.Scope,
					LambdaArg.State
				).Match(out var LambdaResult, out Error)
			) {
				return mResult.Fail(Error);
			}
			
			Checked = (
				mVM_Type.Proc(
					mVM_Type.Empty(),
					LambdaArg.Type,
					LambdaResult.Type
				),
				LambdaResult.State
			);
			Checked.State = Checked.State.SetValidatedType(Lambda, Checked.Type);
			aMappings = LambdaResult.Mappings;
		}
		
		return Checked.Type.IsSubType(
			MappedExpectedType,
			aMappings
		).Then(
			__ => (Checked.Type, __, Checked.State)
		).ModifyError(
			__ => TypeErrorAt(aArgument, MappedExpectedType, Checked.State, __)
		);
	}
	
	private static mResult.tResult<
		(
			mVM_Type.tType Type,
			mTreeMap.tTree<mVM_Type.tType, mVM_Type.tType> Mappings,
			tTypeState<tPos> State
		),
		(tPos Pos, tText ErrorText)
	>
	TryInferArguments<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aArgument,
		mVM_Type.tType aExpectedType,
		mStream.tStream<tScopeItem> aScope,
		tTypeState<tPos> aTypeState
	) {
		var Arguments = mStream.Stream(aArgument);
		var ExpectedTypes = mStream.Stream(aExpectedType);
		
		if (aArgument is mSPO_AST.tTupleNode<tPos> Tuple) {
			var TupleTypes = mStream.Stream<mVM_Type.tType>();
			var Type = aExpectedType;
			
			while (Type.IsPair(out var TailType, out var Head)) {
				TupleTypes = mStream.Stream(Head, TupleTypes);
				Type = TailType;
			}
			
			if (Type.IsEmpty() && TupleTypes.Count() == Tuple.Items.Count()) {
				Arguments = Tuple.Items;
				ExpectedTypes = TupleTypes;
			}
		}
		
		var ArgumentCount = Arguments.Count();
		var Done = new tBool[ArgumentCount];
		var ArgumentTypes = new mVM_Type.tType[ArgumentCount];
		var Errors = new mMaybe.tMaybe<(tPos Pos, tText ErrorText)>[ArgumentCount];
		var Checked = (Mappings: default(mTreeMap.tTree<mVM_Type.tType, mVM_Type.tType>), State: aTypeState);
		var Remaining = ArgumentCount;
		var ArgumentsWithTypes = mStream.ZipShort(Arguments, ExpectedTypes).MapWithIndex().Reverse();
		
		while (Remaining > 0) {
			var Progress = false;
			foreach (var (Index, ArgumentAndType) in ArgumentsWithTypes) {
				if (Done[Index]) {
					continue;
				}
				
				if (
					ArgumentAndType._1.TryInferArgument(
						ArgumentAndType._2,
						Checked.Mappings,
						aScope,
						Checked.State
					).Match(out var Inferred, out var Error)
				) {
					ArgumentTypes[Index] = Inferred.Type;
					Checked = (Inferred.Mappings, Inferred.State);
					Done[Index] = true;
					Errors[Index] = mStd.cEmpty;
					Remaining -= 1;
					Progress = true;
				} else {
					Errors[Index] = Error;
				}
			}
			
			if (!Progress) {
				foreach (var Error in Errors) {
					if (Error.IsSome(out var Error_)) {
						return mResult.Fail(Error_);
					}
				}
				throw mError.Error("missing inference error");
			}
		}
		
		var ExpressionType = mVM_Type.Tuple(System.MemoryExtensions.AsSpan(ArgumentTypes));
		return (ExpressionType, Checked.Mappings, Checked.State.SetValidatedType(aArgument, ExpressionType));
	}
	
	public static mResult.tResult<
		(mVM_Type.tType Type, tTypeState<tPos> State),
		(tPos Pos, tText ErrorText)
	>
	UpdateTypes<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aNode,
		mStream.tStream<tScopeItem> aScope,
		tTypeState<tPos> aTypeState
	) {
		var TypeAnnotation = aNode.TypeAnnotation;
		
		mResult.tResult<(mVM_Type.tType Type, tTypeState<tPos> State), (tPos Pos, tText ErrorText)> Result = aNode switch {
			mSPO_AST.tEmptyNode<tPos> => (mVM_Type.Empty(), aTypeState),
			mSPO_AST.tTrueNode<tPos> => (mVM_Type.True(), aTypeState),
			mSPO_AST.tFalseNode<tPos> => (mVM_Type.False(), aTypeState),
			mSPO_AST.tIntNode<tPos> => (mVM_Type.Int(), aTypeState),
			mSPO_AST.tTextNode<tPos> => (mVM_Type.Text(), aTypeState),
			mSPO_AST.tCharNode<tPos> => (mVM_Type.Char(), aTypeState),
			mSPO_AST.tIdNode<tPos> IdNode => (
				TypeAnnotation.Match(
					Annotation => aScope.Where(
						__ => __.Id == IdNode.Id || (__.TypeValue.IsSome(out _) && __.Id + "..." == IdNode.Id)
					).TryFirst(
					).Then(
						__ => { IdNode.TypeValue = __.TypeValue; return __.Type; }
					).ElseUse(
						Annotation
					),
					() => (
						IdNode.Id == "_=..."
					) ? (
						mStd.With(mVM_Type.Free(mVM_Type.Type()), TypeValue => mVM_Type.Proc(TypeValue, TypeValue, mVM_Type.Empty()))
					) : (
						aScope.Where(
							__ => __.Id == IdNode.Id || (__.TypeValue.IsSome(out _) && __.Id + "..." == IdNode.Id)
						).TryFirst(
						).ElseFail(
							() => (IdNode.Pos, $"No Identifier '{IdNode.Id}' in scope")
						).Then(
							__ => { IdNode.TypeValue = __.TypeValue; return __.Type; }
						)
					)
				)
			).Then(__ => (__, aTypeState)),
			mSPO_AST.tTypeNode<tPos> Type => (
				Type.AsVM_Value(aScope).Then(__ => (__.KindType(), aTypeState))
			),
			mSPO_AST.tSigNode<tPos> Sig => mStd.Call(
				() => {
					if (
						!Sig.Contract.AsVM_Type(aScope).Match(out var Contract, out var Error) ||
						!Sig.Head.AsVM_Value(aScope).Match(out var Head, out Error) ||
						!Sig.Body.UpdateTypes(aScope, aTypeState).Match(out var Body, out Error)
					) {
						return mResult.Fail(Error);
					}
					if (!Contract.IsSig(out var Binder, out var BodyType)) {
						return mResult.Fail((Sig.Contract.Pos, "expected SIG contract"));
					}
					if (!Head.KindType().SameType(Binder.KindType())) {
						return mResult.Fail((Sig.Head.Pos, "SIG head has the wrong kind"));
					}
					return Body.Type.IsSubType(BodyType.Substitute(Binder, Head)).Then(
						_ => (Contract, Body.State.SetValidatedType(Sig.Head, Head.KindType()))
					).ModifyError(__ => (Sig.Body.Pos, __));
				}
			),
			mSPO_AST.tTupleNode<tPos> Tuple => (
				Tuple.Items.Reduce(
					mResult.OK((Types: mStream.Stream<mVM_Type.tType>(), State: aTypeState)).WithErrorType<(tPos Pos, tText ErrorText)>(),
					(aChecked, Item) => aChecked.ThenTry(
						__ => Item.UpdateTypes(aScope, __.State).Then(
							aItem => (mStream.Stream(aItem.Type, __.Types), aItem.State)
						)
					)
				).Then(
					__ => (mVM_Type.Tuple(__.Types.Reverse()), __.State)
				)
			),
			mSPO_AST.tPairNode<tPos> Pair => (
				Pair.Tail.UpdateTypes(
					aScope,
					aTypeState
				).ThenTry(
					aTail => Pair.Head.UpdateTypes(aScope, aTail.State).Then(
						aHead => (mVM_Type.Pair(aTail.Type, aHead.Type), aHead.State)
					)
				)
			),
			mSPO_AST.tPrefixNode<tPos> Prefix => (
				Prefix.Element.UpdateTypes(
					aScope,
					aTypeState
				).Then(
					__ => (mVM_Type.Prefix(Prefix.Prefix, __.Type), __.State)
				)
			),
			mSPO_AST.tRecordNode<tPos> Record => (
				Record.Elements.Reduce(
					mResult.OK((Types: mStream.Stream<mVM_Type.tType>(), State: aTypeState)).WithErrorType<(tPos Pos, tText ErrorText)>(),
					(aChecked, Item) => aChecked.ThenTry(
						__ => Item.Value.UpdateTypes(aScope, __.State).Then(
							aItem => (mStream.Stream(mVM_Type.Prefix(Item.Key.Id, aItem.Type), __.Types), aItem.State)
						)
					)
				).Then(
					__ => (
						__.Types.Reverse().Reduce(
							mVM_Type.Empty(),
							(aTail, aHead) => mVM_Type.Record(aTail, aHead)
						),
						__.State
					)
				)
			),
			mSPO_AST.tLambdaNode<tPos> Lambda => mStd.Call(
				() => {
					if (Lambda.Generic.IsSome(out var GenericPattern)) {
						// TODO: AI generated code has to be reviewed
						return UpdatePatternTypes(GenericPattern, mVM_Type.Type(), tTypeRelation.Equal, aScope, aTypeState).ThenTry(
							aGenTypeScope => UpdatePatternTypes(
								Lambda.Head,
								mStd.cEmpty,
								tTypeRelation.Sub,
								aGenTypeScope.Scope,
								aGenTypeScope.State
							).ThenTry(
								aArgTypeScope => Lambda.Body.UpdateTypes(
									aArgTypeScope.Scope,
									aArgTypeScope.State
								).Then(
									aResTypeScope => {
										var Proc = mVM_Type.Proc(mVM_Type.Empty(), aArgTypeScope.Type, aResTypeScope.Type);
										
										if (aGenTypeScope.Type.IsType()) {
											var T = aGenTypeScope.Scope.Where(
												__ => __.Id == GenericPattern.TryGetId().AssertNotEmpty()
											).TryFirst(
											).AssertNotEmpty(
											).TypeValue.AssertNotEmpty(
											);
											
											return (mVM_Type.Generic(T, Proc), aResTypeScope.State);
										} else {
											return (Proc, aResTypeScope.State);
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
						aScope,
						aTypeState
					).ThenTry(
						aArg => Lambda.Body.UpdateTypes(
							aArg.Scope,
							aArg.State
						).Then(
							aRes => (mVM_Type.Proc(
								mVM_Type.Empty(),
								aArg.Type,
								aRes.Type
							), aRes.State)
						)
					);
				}
			),
			mSPO_AST.tMethodNode<tPos> Method => (
				UpdatePatternTypes(
					Method.Obj,
					mStd.cEmpty,
					tTypeRelation.Equal,
					aScope,
					aTypeState
				).ThenTry(
					aObj => UpdatePatternTypes(
						Method.Arg,
						mStd.cEmpty,
						tTypeRelation.Sub,
						aObj.Scope,
						aObj.State
					).ThenTry(
						aArg => Method.Body.UpdateTypes(
							aArg.Scope,
							aArg.State
						).Then(
							aResType => (mVM_Type.Proc(aObj.Type, aArg.Type, aResType.Type), aResType.State)
						)
					)
				)
			),
			mSPO_AST.tBlockNode<tPos> Block => (
				mStd.Call(
					() => {
						var Types = mStream.Stream<mVM_Type.tType>([]);
						var Checked = (Scope: aScope, State: aTypeState);
						foreach (var Command in Block.Commands) {
							if (!UpdateCommandTypes(Command, Checked.Scope, Checked.State).Match(out Checked, out var Error)) {
								return mResult.Fail(Error);
							}
							
							if (Command is mSPO_AST.tReturnIfNode<tPos> ReturnIf) {
								var Type = Checked.State.TryGetValidatedType(ReturnIf.Result).AssertNotEmpty();
								if (Types.All(__ => !Equals(__, Type))) {
									Types = mStream.Stream(Type, Types);
								}
							}
						}
						return mResult.OK(
							(Types.Join((a1, a2) => mVM_Type.Set(a2, a1), mVM_Type.Empty()), Checked.State)
						).WithErrorType<(tPos Pos, tText ErrorText)>(
						);
					}
				)
			),
			mSPO_AST.tCallNode<tPos> Call => Call.Func.UpdateTypes(
				aScope,
				aTypeState
			).ThenTry(
				aFuncType => mStd.Call(
					() => {
						var ProcType = aFuncType.Type;
						while (ProcType.IsGeneric(out _, out var InnerType)) {
							ProcType = InnerType;
						}
						
						if (!ProcType.IsProc(out _, out var FormalArgType, out var FormalResultType)) {
							return mResult.Fail(
								(Call.Func.Pos, $"expect proc but is:\n{aFuncType.Type.ToText()}")
							);
						}
						
						return Call.Arg.TryInferArguments(
							FormalArgType,
							aScope,
							aFuncType.State
						).Then(
							aArg => (FormalResultType.ApplyMappings(aArg.Mappings), aArg.State)
						);
					}
				)
			),
			mSPO_AST.tIfMatchNode<tPos> IfMatch => (
				IfMatch.Expression.UpdateTypes(aScope, aTypeState).ThenTry(
					aTypePattern => mStd.Call(
						() => {
							var Remaining = mMaybe.Some(aTypePattern.Type);
							var Checked = (Types: mStream.Stream<mVM_Type.tType>(), State: aTypePattern.State);
							
							foreach (var Case in IfMatch.Cases) {
								var CaseInputType = Remaining.ElseUse(aTypePattern.Type);
								
								// first try
								var CoverageCandidate = CaseInputType.SplitForPatternType(Case.Pattern, Checked.State);
								if (!CoverageCandidate.Matched.IsSome(out var CandidateType)) {
									return mResult.Fail(
										(
											Case.Pattern.Pos,
											$"pattern '{Case.Pattern.ToText()}' cannot match '{CaseInputType.ToText()}'"
										)
									);
								}
								
								// update
								if (
									!UpdatePatternTypes(
										Case.Pattern,
										CandidateType,
										tTypeRelation.Super,
										aScope,
										Checked.State
									).Match(out var Pattern, out var Error)
								) {
									return mResult.Fail(Error);
								}
								
								// final try
								var Coverage = CaseInputType.SplitForPatternType(Case.Pattern, Pattern.State);
								if (!Coverage.Matched.IsSome(out _)) {
									return mResult.Fail(
										(
											Case.Pattern.Pos,
											$"pattern '{Case.Pattern.ToText()}' cannot match '{CaseInputType.ToText()}'"
										)
									);
								}
								
								// update
								if (!Case.Expression.UpdateTypes(Pattern.Scope, Pattern.State).Match(out var CaseType, out Error)) {
									return mResult.Fail(Error);
								}
								
								Checked = (mStream.Stream(CaseType.Type, Checked.Types), CaseType.State);
								
								if (Remaining.IsSome(out _)) {
									Remaining = Coverage.Remaining;
								}
							}
							
							if (Remaining.IsSome(out var RemainingType)) {
								return mResult.Fail(
									(
										IfMatch.Pos,
										$"""
										non-exhaustive match; unmatched type:
										{RemainingType.ToText()}
										"""
									)
								);
							}
							
							return mResult.OK(
								(
									Checked.Types.Reduce(
										(mVM_Type.tType)null!,
										(aTypes, aType) => (
											aTypes is null || aTypes == aType
											? aType
											: mVM_Type.Set(aType, aTypes)
										)
									),
									Checked.State
								)
							).WithErrorType<(tPos Pos, tText ErrorText)>();
						}
					)
				)
			),
			mSPO_AST.tIsNode<tPos> Is => (
				Is.Expression.UpdateTypes(
					aScope,
					aTypeState
				).ThenTry(
					aValueType => UpdatePatternTypes(
						Is.Pattern,
						aValueType.Type,
						tTypeRelation.Super,
						aScope,
						aValueType.State
					)
				).Then(
					__ => (mVM_Type.Bool(), __.State)
				)
			),
			mSPO_AST.tVarToValNode<tPos> VarToVal => (
				VarToVal.Obj.UpdateTypes(
					aScope,
					aTypeState
				).ThenTry(
					__ => (
						__.Type.IsVar(out var ValType)
						? mResult.OK((ValType, __.State)).WithErrorType<(tPos Pos, tText ErrorText)>()
						: mResult.Fail((VarToVal.Pos, $"the type '{__.Type}' in not from type '[§VAR ...]'"))
					)
				)
			),
			mSPO_AST.tIfNode<tPos> If => (
				If.Cases.Reduce(
					mResult.OK((Types: mStream.Stream<mVM_Type.tType>(), State: aTypeState)).WithErrorType<(tPos Pos, tText ErrorText)>(),
					(aChecked, Case) => aChecked.ThenTry(
						__ => Case.Cond.UpdateTypes(aScope, __.State).FailIfNot(
							aCondition => aCondition.Type.IsSubType(
								mVM_Type.Bool()
							).Match(out _, out _),
							aCondition => (Case.Cond.Pos, $"condition '{Case.Cond.ToText()}' has to be [§TRUE | §FALSE] but is of type:\n  {aCondition.Type.ToText()}")
						).ThenTry(
							aCondition => Case.Result.UpdateTypes(aScope, aCondition.State)
						).Then(
							aCase => (mStream.Stream(aCase.Type, __.Types), aCase.State)
						)
					)
				).Then(
					aChecked => {
						var X = aChecked.Types.Reverse().Reduce(
							mStream.Stream<mVM_Type.tType>([]),
							(aList, aItem) => aList.All(__ => __ != aItem)
							? mStream.Stream(aItem, aList)
							: aList
						);
						
						var Type = X.Count() switch {
							0 => mVM_Type.Empty(),
							1 => X.TryFirst().AssertNotEmpty(),
							_ => X.Reduce(
								mVM_Type.Empty(),
								(aTypeSet, aType) => mVM_Type.Set(aType, aTypeSet)
							)
						};
						return (Type, aChecked.State);
					}
				)
			),
			mSPO_AST.tPipeToLeftNode<tPos> Pipe => throw mError.Error($"'{aNode.GetType().Name}' should be desugared at this point!"),
			_ => throw mError.Error("not implemented: " + aNode.GetType().Name),
		};
		
		return Result.FailIfNot(
			aInferred => !aInferred.Type.HasChildWith(
				aHead => (
					aHead.Kind is mVM_Type.tKind.Abstract &&
					!aScope.Any(
						aItem => (
							aItem.Type.HasChildWith(__ => mStd.RefEq(__, aHead)) ||
							aItem.TypeValue.Match(
								aValue => aValue.HasChildWith(__ => mStd.RefEq(__, aHead)),
								() => false
							)
						)
					)
				)
			),
			aInferred => (
				aNode.Pos,
				$"abstract SIG head escapes its scope in '{aInferred.Type.ToText()}'; pack it with §SIG"
			)
		).ThenTry(
			Inferred => TypeAnnotation.Match(
				Annotation => Inferred.Type.IsSubType(
					Annotation
				).Then(
					_ => Inferred
				).ModifyError(
					aError => TypeErrorAt(aNode, Annotation, Inferred.State, "Type annotation: " + aError)
				),
				() => mResult.OK(Inferred).WithErrorType<(tPos Pos, tText ErrorText)>()
			)
		).Then(
			Inferred => (Inferred.Type, Inferred.State.SetValidatedType(aNode, Inferred.Type))
		);
	}
	
	private static tBool
	HasChildWith(
		this mVM_Type.tType aType,
		mStd.tFunc<mVM_Type.tType, tBool> aTest,
		mStream.tStream<mVM_Type.tType> aVisited = default
	) {
		if (aVisited.Any(__ => mStd.RefEq(__, aType))) {
			return false;
		}
		if (aTest(aType)) {
			return true;
		}
		aVisited = mStream.Stream(aType, aVisited);
		return (
			aType.Kind is mVM_Type.tKind.Record
			? aType.Fields.ToStream().Any(__ => __.Value.HasChildWith(aTest, aVisited))
			: mStream.Stream(aType.Refs).Any(__ => __.HasChildWith(aTest, aVisited))
		);
	}
	
	public static mResult.tResult<
		(
			mVM_Type.tType Type,
			mStream.tStream<tScopeItem> Scope,
			tTypeState<tPos> State
		),
		(tPos Pos, tText ErrorText)
	>
	UpdatePatternTypes<tPos>(
		mSPO_AST.tPatternNode<tPos> aPattern,
		mMaybe.tMaybe<mVM_Type.tType> aType,
		tTypeRelation aTypeRelation,
		mStream.tStream<tScopeItem> aScope,
		tTypeState<tPos> aTypeState
	) {
		mResult.tResult<(mVM_Type.tType Type, mStream.tStream<tScopeItem> Scope, tTypeState<tPos> State), (tPos Pos, tText ErrorText)> Result;
		switch (aPattern) {
			case mSPO_AST.tTypedPatternNode<tPos> Pattern: {
				Result = Pattern.TypeExpression.Match(
					__ => mStd.Call(
						() => __.AsVM_Type(aScope).ThenTry(
							aType => UpdatePatternTypes(
								Pattern.Pattern,
								aType,
								aTypeRelation,
								aScope,
								aTypeState
							)
						)
					),
					() => UpdatePatternTypes(Pattern.Pattern, aType, aTypeRelation, aScope, aTypeState)
				);
				break;
			}
			case mSPO_AST.tSigPatternNode<tPos> Sig: {
				if (!Sig.Contract.AsVM_Type(aScope).Match(out var Contract, out var Error)) {
					return mResult.Fail(Error);
				}
				
				if (!Contract.IsSig(out var Binder, out var BodyType)) {
					return mResult.Fail((Sig.Contract.Pos, "expected SIG contract"));
				}
				
				var HeadPattern = Sig.Head;
				
				if (HeadPattern is mSPO_AST.tTypedPatternNode<tPos> Typed) {
					if (Typed.TypeExpression.IsSome(out var KindExpr)) {
						if (!KindExpr.AsVM_Type(aScope).Match(out var Kind, out Error)) {
							return mResult.Fail(Error);
						}
						if (!Kind.SameType(Binder.KindType())) {
							return mResult.Fail((KindExpr.Pos, "SIG head binding has the wrong kind"));
						}
					}
					HeadPattern = Typed.Pattern;
				}
				
				var Head = mVM_Type.Abstract(Binder.Id!, Binder.KindType());
				var Scope = aScope;
				
				if (HeadPattern is mSPO_AST.tTypePatternNode<tPos> Concrete) {
					if (!Concrete.Type.AsVM_Value(aScope).Match(out Head, out Error)) {
						return mResult.Fail(Error);
					}
					if (!Head.KindType().SameType(Binder.KindType())) {
						return mResult.Fail((Concrete.Pos, "SIG head pattern has the wrong kind"));
					}
				} else if (HeadPattern is mSPO_AST.tFreeIdPatternNode<tPos> Binding) {
					Head = mVM_Type.Abstract(Binding.Id, Binder.KindType());
					Scope = mStream.Stream(ScopeItem(Binding.Id, Head.KindType(), Head), Scope);
				}
				
				var HeadState = aTypeState.SetValidatedType(HeadPattern, Head.KindType()).SetValidatedType(Sig.Head, Head.KindType());
				
				Sig.HeadValue = Head;
				
				var ExpectedBody = BodyType.Substitute(Binder, Head);
				
				Result = UpdatePatternTypes(
					Sig.Body,
					ExpectedBody,
					aTypeRelation,
					Scope,
					HeadState
				).ThenTry(
					__ => ExpectedBody.SplitForPatternType(Sig.Body, __.State).Matched.IsSome(out _)
						? mResult.OK((Contract, __.Scope, __.State)).WithErrorType<(tPos Pos, tText ErrorText)>()
						: mResult.Fail((Sig.Body.Pos, "SIG body pattern cannot match the body type"))
				);
				
				break;
			}
			case mSPO_AST.tFreeIdPatternNode<tPos> FreePatternId: {
				Result = aType.Then(
					__ => (
						__.IsType()
						? (
							__,
							mStream.Stream(
								ScopeItem(
									FreePatternId.Id,
									__,
									mVM_Type.Free(FreePatternId.Id, __)
								),
								aScope
							),
							aTypeState
						)
						: (
							__,
							mStream.Stream(
								ScopeItem(
									FreePatternId.Id,
									__
								),
								aScope
							),
							aTypeState
						)
					)
				).ElseFail(
					() => (FreePatternId.Pos, $"missing type for '{FreePatternId.Id}'")
				);
				break;
			}
			
			case mSPO_AST.tVarPatternNode<tPos> VarPattern: {
				Result = aType.Then(
					__ => mStd.Call(
						() => {
							var NewTypeScope = aScope.Where(
								__ => __.Id == VarPattern.Id
							).TryFirst(
							).Match(
								() => {
									var NewType = __.IsVar(out _)
										? __
										: mVM_Type.Var(__);
									return (Type: NewType, Scope: mStream.Stream(ScopeItem(VarPattern.Id, NewType), aScope), State: aTypeState);
								},
								aScopeItem => {
									var ExistingType = aScopeItem.Type;
									mAssert.IsTrue(
										ExistingType.IsVar(out _),
										() => $"'{ExistingType.ToText()}' is not a var type"
									);
									return (Type: ExistingType, Scope: aScope, State: aTypeState);
								}
							);
							
							return NewTypeScope;
						}
					)
				).ElseFail(
					() => (VarPattern.Pos, $"missing type for '{VarPattern.Id}'")
				);
				break;
			}
			
			case mSPO_AST.tIgnorePatternNode<tPos> IgnorePattern: {
				Result = aType.Then(__ => (__, aScope, aTypeState)).ElseFail(() => (IgnorePattern.Pos, "unknown type"));
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
				Result = UpdatePatternTypes(PrefixPattern.Pattern, SubType, aTypeRelation, aScope, aTypeState).Then(
					__ => (mVM_Type.Prefix(PrefixPattern.Prefix, __.Type), __.Scope, __.State)
				);
				break;
			}
			case mSPO_AST.tTuplePatternNode<tPos> TuplePattern: {
				var Types = mStream.Stream<mVM_Type.tType>([]);
				var Checked = (Scope: aScope, State: aTypeState);
				if (!aType.IsSome(out var Type)) {
					foreach (var Pattern in TuplePattern.Items) {
						if (!UpdatePatternTypes(Pattern, mStd.cEmpty, aTypeRelation, Checked.Scope, Checked.State).Match(out var TS, out var Error)) {
							return mResult.Fail(Error);
						}
						
						Checked = (TS.Scope, TS.State);
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
							if (!UpdatePatternTypes(Pattern, Type, aTypeRelation, Checked.Scope, Checked.State).Match(out var TS, out var Error)) {
								return mResult.Fail(Error);
							}
							
							Types = mStream.Stream(TS.Type, Types);
							Checked = (TS.Scope, TS.State);
						}
					} else {
						if (!WalkType.IsEmpty() || TypeStack.Count() != TuplePattern.Items.Count()) {
							return mResult.Fail((TuplePattern.Pos, $"can't unify '{TuplePattern.ToText()} and '{Type.ToText()}'"));
						}
						
						foreach (var (Pattern, ItemType) in mStream.ZipShort(TuplePattern.Items, TypeStack)) {
							if (!UpdatePatternTypes(Pattern, ItemType, aTypeRelation, Checked.Scope, Checked.State).Match(out var TS, out var Error)) {
								return mResult.Fail(Error);
							}
							
							Types = mStream.Stream(TS.Type, Types);
							Checked = (TS.Scope, TS.State);
						}
					}
				}
				Result = (mVM_Type.Tuple(Types.Reverse()), Checked.Scope, Checked.State);
				break;
			}
			case mSPO_AST.tPairPatternNode<tPos> PairPattern: {
				var TailType = mMaybe.None<mVM_Type.tType>();
				var HeadType = mMaybe.None<mVM_Type.tType>();
				
				if (aType.IsSome(out var Type)) {
					if (!Type.TryProjectPair(out var Tail, out var Head)) {
						return mResult.Fail(
							(
								PairPattern.Pos,
								$"cant unify '{PairPattern.ToText()}' and '{Type.ToText()}'"
							)
						);
					}
					
					TailType = Tail;
					HeadType = Head;
				}
				
				if (
					!UpdatePatternTypes(
						PairPattern.Tail,
						TailType,
						aTypeRelation,
						aScope,
						aTypeState
					).Match(
						out var TailRes,
						out var Error
					) ||
					!UpdatePatternTypes(
						PairPattern.Head,
						HeadType,
						aTypeRelation,
						TailRes.Scope,
						TailRes.State
					).Match(
						out var HeadRes,
						out Error
					)
				) {
					return mResult.Fail(Error);
				}
				
				Result = (mVM_Type.Pair(TailRes.Type, HeadRes.Type), HeadRes.Scope, HeadRes.State);
				break;
			}
			case mSPO_AST.tRecordPatternNode<tPos> RecordPattern: {
				Result = (mVM_Type.Empty(), aScope, aTypeState);
				foreach (var Item in RecordPattern.Elements) {
					var Type = aType.IsSome(out var RecordType)
					? RecordType.GetFieldType(Item.Id.Id)
					: mMaybe.None<mVM_Type.tType>();
					
					Result = Result.ThenTry(
						a1 => UpdatePatternTypes(
							Item.Pattern,
							Type,
							aTypeRelation,
							a1.Scope,
							a1.State
						).Then(
							a2 => (
								mVM_Type.Record(
									a1.Type,
									mVM_Type.Prefix(Item.Id.Id, a2.Type)
								),
								a2.Scope,
								a2.State
							)
						)
					);
				}
				break;
			}
			case mSPO_AST.tGuardPatternNode<tPos> GuardPattern: {
				if (
					!UpdatePatternTypes(
						GuardPattern.Pattern,
						aType,
						tTypeRelation.Super,
						aScope,
						aTypeState
					).Match(
						out var Res,
						out var Error
					) ||
					!GuardPattern.Guard.UpdateTypes(
						Res.Scope,
						Res.State
					).Match(
						out var BoolRes,
						out Error
					)
				) {
					return mResult.Fail(Error);
				}
				
				if (
					!BoolRes.Type.IsSubType(
						mVM_Type.Bool()
					).Match(out _, out _)
				) {
					return mResult.Fail(
						(
							GuardPattern.Pos,
							$"""
							return type has to be boolean but is:
								{BoolRes.Type.ToText()}
							"""
						)
					);
				}
				
				Result = (Res.Type, Res.Scope, BoolRes.State);
				// TODO: Result = mVM_Type.Guard(Result, ...);
				break;
			}
			case mSPO_AST.tIdNode<tPos> Id: {
				var Type = aType.IsSome(out var T) ? T : mVM_Type.Free(mVM_Type.Type());
				
				Result = (
					Type,
					mStream.Stream(
						Type.IsType()
						? ScopeItem(Id.Id, Type, mVM_Type.Free(Id.Id, Type))
						: ScopeItem(Id.Id, Type),
						aScope
					),
					aTypeState
				);
				break;
			}
			case mSPO_AST.tExpressionNode<tPos> Expression: {
				Result = Expression.UpdateTypes(aScope, aTypeState).Then(__ => (__.Type, aScope, __.State));
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aPattern.GetType().Name);
			}
		}
		return Result.Then(__ => (__.Type, __.Scope, __.State.SetValidatedType(aPattern, __.Type)));
	}
	
	public static mResult.tResult<
		(
			mStream.tStream<tScopeItem> Scope,
			tTypeState<tPos> State
		),
		(tPos Pos, tText ErrorText)
	>
	UpdateMethodCallTypes<tPos>(
		mSPO_AST.tMethodCallNode<tPos> aMethodCall,
		mStream.tStream<tScopeItem> aScope,
		tTypeState<tPos> aTypeState
	) {
		return aMethodCall.Method.UpdateTypes(aScope, aTypeState).ThenTry(
			aMethodType => mStd.Call(
				() => {
					var ProcType = aMethodType.Type;
					while (ProcType.IsGeneric(out _, out var InnerType)) {
						ProcType = InnerType;
					}
					
					if (!ProcType.IsProc(out _, out var MethArgType, out var MethResType)) {
						return mResult.Fail(
							(aMethodCall.Argument.Pos, $"'{aMethodType.Type.ToText()}' is not a Proc")
						);
					}
					
					return aMethodCall.Argument.TryInferArguments(
						MethArgType,
						aScope,
						aMethodType.State
					).ThenTry(
						aArgument => (
							!aMethodCall.Result.IsSome(out var Result)
							? mResult.OK((Scope: aScope, State: aArgument.State)).WithErrorType<(tPos Pos, tText ErrorText)>()
							: UpdatePatternTypes(
								Result,
								MethResType.ApplyMappings(aArgument.Mappings),
								tTypeRelation.Sub,
								aScope,
								aArgument.State
							).Then(__ => (__.Scope, __.State))
						)
					);
				}
			)
		);
	}
	
	public static mResult.tResult<(mStream.tStream<tScopeItem> Scope, tTypeState<tPos> State), (tPos Pos, tText ErrorText)>
	UpdateCommandTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<tScopeItem> aScope,
		tTypeState<tPos> aTypeState
	) {
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				return Def.Src.UpdateTypes(aScope, aTypeState).ThenTry(
					aSource => UpdatePatternTypes(
						Def.Des,
						aSource.Type,
						tTypeRelation.Equal,
						aScope,
						aSource.State
					).ThenTry(
						aPattern => aSource.Type.IsSubType(
							aPattern.Type
						).Then(
							Mappings => (
								Scope: Def.Src is mSPO_AST.tTypeNode<tPos> &&
								Def.Des.TryGetId().IsSome(out var Id) &&
								Def.Src.AsVM_Value(aScope).Match(out var Value, out _)
								? mStream.Stream(ScopeItem(Id, aSource.Type, Value), aPattern.Scope.Where(__ => __.Id != Id))
								: aPattern.Scope,
								State: aPattern.State
							)
						).ModifyError(
							__ => TypeErrorAt(Def.Src, aPattern.Type, aSource.State, __)
						)
					)
				);
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnIf: {
				return ReturnIf.Condition.UpdateTypes(
					aScope,
					aTypeState
				).FailIfNot(
					aCondition => aCondition.Type.IsSubType(
						mVM_Type.Bool()
					).Match(out _, out _),
					__ => (ReturnIf.Condition.Pos, $"{__.Type.ToText()} != [§TRUE | §FALSE]")
				).ThenTry(
					aCondition => ReturnIf.Result.UpdateTypes(aScope, aCondition.State)
				).Then(
					aResult => (aScope, aResult.State)
				);
			}
			case mSPO_AST.tDefVarNode<tPos> DefVar: {
				return DefVar.Expression.UpdateTypes(aScope, aTypeState).ThenTry(
					aValue => DefVar.MethodCalls.Reduce(
						mResult.OK((Scope: aScope, State: aValue.State)).WithErrorType<(tPos Pos, tText ErrorText)>(),
						(aChecked, MethodCall) => aChecked.ThenTry(
							__ => UpdateMethodCallTypes(MethodCall, __.Scope, __.State)
						)
					).ThenTry(
						aChecked => {
							var Type = mVM_Type.Var(aValue.Type);
							var NewScope = mStream.Stream(ScopeItem(DefVar.Id.Id, Type), aChecked.Scope);
							return DefVar.Id.UpdateTypes(NewScope, aChecked.State).Then(
								__ => (NewScope, __.State)
							);
						}
					)
				);
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdas: {
				var Checked = (Scope: aScope, State: aTypeState);
				foreach (var Item in RecLambdas.List) {
					var Head = Checked;
					if (Item.Lambda.Generic.IsSome(out var GenericPattern)) {
						if (!UpdatePatternTypes(GenericPattern, mVM_Type.Type(), tTypeRelation.Equal, Head.Scope, Head.State).Match(out var GenScope, out var GenError)) {
							return mResult.Fail(GenError);
						}
						Head = (GenScope.Scope, GenScope.State);
					}
					
					if (!UpdatePatternTypes(Item.Lambda.Head, mStd.cEmpty, tTypeRelation.Equal, Head.Scope, Head.State).Match(out var Result, out var Error)) {
						return mResult.Fail(Error);
					}
					
					Checked = (mStream.Stream(
						ScopeItem(
							Item.Id.Id,
							mVM_Type.Proc(
								mVM_Type.Free("__" + Item.Id.Id + "_Obj__", mVM_Type.Type()),
								Result.Type,
								mVM_Type.Free("__" + Item.Id.Id + "_Res__", mVM_Type.Type())
							)
						),
						Checked.Scope
					), Result.State);
				}
				
				foreach (var Item in RecLambdas.List) {
					if (
						!Checked.Scope.Where(
							__ => __.Id == Item.Id.Id
						).TryFirst(
						).ElseFail(
							() => (Item.Pos, $"unknown Id '{Item.Id.Id}'")
						).Then(
							__ => __.Type
						).ThenTry(
							DesType => Item.Lambda.UpdateTypes(Checked.Scope, Checked.State).ThenDo(
								SrcType => {
									DesType.Kind = SrcType.Type.Kind;
									DesType.Id = SrcType.Type.Id;
									DesType.Prefix = SrcType.Type.Prefix;
									DesType.Refs = SrcType.Type.Refs;
								}
							)
						).Match(out var Typed, out var Error)
					) {
						return mResult.Fail(Error);
					}
					Checked = (Checked.Scope, Typed.State);
				}
				
				foreach (var Item in RecLambdas.List) {
					if (
						!Item.Lambda.UpdateTypes(Checked.Scope, Checked.State).Then(
							__ => (mStream.Stream(
								ScopeItem(
									Item.Id.Id,
									__.Type
								),
								Checked.Scope
							), __.State)
						).Match(out Checked, out var Error)
					) {
						return mResult.Fail(Error);
					}
				}
				
				return Checked;
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCalls: {
				return MethodCalls.Object.UpdateTypes(aScope, aTypeState).ThenTry(
					aObject => MethodCalls.MethodCalls.Reduce(
						mResult.OK((Scope: aScope, State: aObject.State)).WithErrorType<(tPos Pos, tText ErrorText)>(),
						(aChecked, MethodCall) => aChecked.ThenTry(
							__ => UpdateMethodCallTypes(MethodCall, __.Scope, __.State)
						)
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
	
	// Accept a value signature without changing the kind of its type abstraction.
	public static mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>
	AsVM_Type<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpression,
		mStream.tStream<tScopeItem> aScope
	) {
		return aExpression.AsVM_Value(aScope).ThenTry(
			Value => Value.IsSignature()
				? mResult.OK(Value).WithErrorType<(tPos Pos, tText ErrorText)>()
				: mResult.Fail((aExpression.Pos, "expected a type or declared generic signature"))
		);
	}
	
	public static mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>
	AsVM_Value<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpression,
		mStream.tStream<tScopeItem> aScope
	) {
		mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)> Result;
		
		switch (aExpression) {
			case mSPO_AST.tEmptyTypeNode<tPos>: {
				Result = mVM_Type.Empty();
				break;
			}
			case mSPO_AST.tTrueNode<tPos>: {
				Result = mVM_Type.True();
				break;
			}
			case mSPO_AST.tFalseNode<tPos>: {
				Result = mVM_Type.False();
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
				Result = mVM_Type.Text();
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
			case mSPO_AST.tRecordTypeNode<tPos> RecordType: {
				Result = RecordType.Elements.Reduce(
					mResult.OK(
						mStream.Stream<(tText Key, mVM_Type.tType Type)>()
					).WithErrorType<(tPos Pos, tText ErrorText)>(
					),
					(aResult, aElement) => aResult.ThenTry(
						aStream => aElement.Type.AsVM_Type(aScope).Then(
							aType => mStream.Stream((Key: aElement.Key.Id, Type: aType), aStream)
						)
					)
				).Then(__ => mVM_Type.Record(__.ToArrayList().ToArray()));
				break;
			}
			case mSPO_AST.tIdNode<tPos> IdNode: {
				Result = aScope.Where(
					__ => __.Id == IdNode.Id || (__.TypeValue.IsSome(out _) && __.Id + "..." == IdNode.Id)
				).TryFirst().Match(
					__ => __.TypeValue,
					() => IdNode.TypeValue
				).ElseFail(
					() => (IdNode.Pos, $"unknown type value '{IdNode.Id}'")
				).ThenDo(__ => { IdNode.TypeValue = __; });
				break;
			}
			case mSPO_AST.tProcTypeNode<tPos> ProcType: {
				Result = ProcType.ObjType.AsVM_Type(aScope).ThenTry(
					aObjType => ProcType.ArgType.AsVM_Type(aScope).ThenTry(
						aArgType => ProcType.ResType.AsVM_Type(aScope).Then(
							aResType => mVM_Type.Proc(aObjType, aArgType, aResType)
						)
					)
				);
				break;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursiveType: {
				var Name = RecursiveType.HeadType.Id;
				var RecursiveVar = mVM_Type.Free(Name, mVM_Type.Type());
				
				Result = RecursiveType.BodyType.AsVM_Type(
					mStream.Stream(
						ScopeItem(Name, mVM_Type.Type(), RecursiveVar),
						aScope
					)
				).Then(
					_ => mVM_Type.Recursive(RecursiveVar, _)
				);
				break;
			}
			case mSPO_AST.tSetTypeNode<tPos> SetType: {
				Result = SetType.Expressions.Map(
					__ => __.AsVM_Type(aScope)
				).WhenAllThen(
					__ => __.Reduce(
						mStream.Stream<mVM_Type.tType>([]),
						(aList, aItem) => aList.All(__ => __ != aItem) ? mStream.Stream(aItem, aList) : aList
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
					__ => __.AsVM_Type(aScope)
				).WhenAllThen(
					__ => mVM_Type.Prefix(PrefixType.Prefix.Id, mVM_Type.Tuple(__))
				);
				break;
			}
			case mSPO_AST.tVarTypeNode<tPos> VarType: {
				Result = VarType.Type.AsVM_Type(aScope).Then(
					mVM_Type.Var
				);
				break;
			}
			case mSPO_AST.tSigTypeNode<tPos> Sig: {
				if (!Sig.HeadType.AsVM_Type(aScope).Match(out var Kind, out var Error)) {
					return mResult.Fail(Error);
				}
				if (!Kind.KindType().IsType()) {
					return mResult.Fail((Sig.HeadType.Pos, "SIG head kind must be a type"));
				}
				var Head = mVM_Type.Free(Sig.Head.Id, Kind);
				Result = Sig.BodyType.AsVM_Type(
					mStream.Stream(ScopeItem(Sig.Head.Id, Head.KindType(), Head), aScope)
				).Then(__ => mVM_Type.Sig(Head, __));
				break;
			}
			case mSPO_AST.tGenericTypeNode<tPos> GenericType: {
				var Name = GenericType.HeadType.Id;
				var GenericVar = mVM_Type.Free(Name, mVM_Type.Type());
				
				Result = GenericType.BodyType.AsVM_Value(
					mStream.Stream(
						ScopeItem(Name, mVM_Type.Type(), GenericVar),
						aScope
					)
				).Then(
					__ => mVM_Type.Generic(GenericVar, __)
				);
				break;
			}
			case mSPO_AST.tGenericApplyTypeNode<tPos> GenericApplyType: {
				Result = GenericApplyType.GenericType.AsVM_Value(aScope).ThenTry(
					aGenericType => GenericApplyType.ArgType.AsVM_Value(aScope).ThenTry(
						aArgType => (
							aGenericType.KindType().IsProc(out var Obj, out var Arg, out _) &&
							Obj.IsEmpty() && Arg.SameType(aArgType.KindType())
							? mResult.OK(aGenericType.ApplyType(aArgType)).WithErrorType<(tPos Pos, tText ErrorText)>()
							: mResult.Fail((GenericApplyType.Pos, "invalid type constructor or argument kind"))
						)
					)
				);
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aExpression.GetType().Name);
			}
		}
		return Result;
	}
}
