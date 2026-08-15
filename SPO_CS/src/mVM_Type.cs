#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mResult.cs
#:ref Common/mStream.cs
#:ref Common/mError.cs
#:ref Common/mAssert.cs
#:ref Common/mMaybe.cs
#:ref Common/mTreeMap.cs

using tInferenceState = mStream.tStream<(mVM_Type.tType Variable, mMaybe.tMaybe<mVM_Type.tType> Solution)>;

public static class
mVM_Type {
	public static tInferenceState
	NewInferenceState(
	) => mStd.cEmpty;
	
	public static tInferenceState
	AddVar(
		this tInferenceState aInferenceState,
		tType aVariable
	) {
		mAssert.IsTrue(aVariable.IsTypeVariable(out _, out _));
		return aInferenceState.Any(__ => mStd.RefEq(__.Variable, aVariable))
		? aInferenceState
		: mStream.Stream(
			(Variable: aVariable, Solution: mMaybe.None<tType>()),
			aInferenceState
		);
	}
	
	public static mMaybe.tMaybe<tType>
	TryGetSolution(
		this tInferenceState aInferenceState,
		tType aVariable
	) => aInferenceState.Where(
		__ => mStd.RefEq(__.Variable, aVariable)
	).TryFirst(
	).ThenTry(
		__ => __.Solution
	);
	
	public static tBool
	IsRegistered(
		this tInferenceState aInferenceState,
		tType aVariable
	) => aInferenceState.Any(__ => mStd.RefEq(__.Variable, aVariable));
	
	public enum
	tKind {
		TypeVariable,
		Any,
		Empty,
		True,
		False,
		Int,
		Type,
		Pair,
		Sig,
		TypeApply,
		Prefix,
		Record,
		Proc,
		Var,
		Ref,
		Set,
		Cond,
		Recursive,
		Generic, // Universal
		Interface, // Existential
	}
	
	public sealed class
	tType {
		public tKind Kind;
		public tText? Id;
		public tText? Prefix;
		public tType? VariableKind;
		public tType[] Refs = [];
		public mTreeMap.tTree<tText, tType> Fields;
		public readonly tNat64 DebugId = mStd.NewDebugId();
		public tNat64 Identity => DebugId;
		
		public override tBool
		Equals(
			tUnknown? a
		) => this == (tType)a!;
		
		public static tBool operator!=(
			tType a1,
			tType a2
		) => !(a1 == a2);
		
		public static tBool operator==(
			tType a1,
			tType a2
		) {
			if (mStd.RefEq(a1, a2)) {
				return true;
			}
			
			if (a1 is null || a2 is null) {
				return false;
			}
			
			if (a1.Kind is tKind.Set && a2.Kind is tKind.Set) {
				var Alternatives1 = mStream.Stream<tType>().AddUnionAlternatives(a1);
				var Alternatives2 = mStream.Stream<tType>().AddUnionAlternatives(a2);
				return Alternatives1.Count() == Alternatives2.Count() &&
					Alternatives1.All(
						a1 => Alternatives2.Any(a2 => a1 == a2)
					);
			}
			
			if (
				a1.Kind != a2.Kind ||
				a1.Id != a2.Id ||
				a1.Prefix != a2.Prefix ||
				a1.Refs.Length != a2.Refs.Length
			) {
				return false;
			}
			
			if (a1.Kind is tKind.TypeVariable) {
				return mStd.RefEq(a1, a2);
			}
			
			if (a1.Kind is tKind.Record) {
				if (a1.Fields.ToStream().Count() != a2.Fields.ToStream().Count()) {
					return false;
				}
				foreach (var Field in a1.Fields.ToStream()) {
					if (!a2.Fields.TryGet(Field.Key).IsSome(out var Other) || Field.Value != Other) {
						return false;
					}
				}
				return true;
			}
			
			if (a1.Kind is tKind.Recursive or tKind.Generic or tKind.Interface) {
				var Binder = TypeVariable("alpha", a1.Refs[0].KindOf());
				return a1.Refs[1].Substitute(a1.Refs[0], Binder) ==
					a2.Refs[1].Substitute(a2.Refs[0], Binder);
			}
			
			if (a1.Kind is tKind.Sig) {
				if (a1.Refs[0].KindOf() != a2.Refs[0].KindOf()) {
					return false;
				}
				var Binder = TypeVariable("alpha", a1.Refs[0].KindOf());
				return a1.Refs[1].Substitute(a1.Refs[0], Binder) ==
					a2.Refs[1].Substitute(a2.Refs[0], Binder);
			}
			
			for (var I = a1.Refs.Length; I --> 0;) {
				if (!a1.Refs[I].Equals(a2.Refs[I])) {
					return false;
				}
			}
			
			return true;
		}
		
		public override tText
		ToString(
		) => this.ToText();
	}
	
	private static mStream.tStream<tType>
	AddUnionAlternatives(
		this mStream.tStream<tType> aAlternatives,
		tType aType
	) {
		if (aType.IsSet(out var Type1, out var Type2)) {
			return aAlternatives
			.AddUnionAlternatives(Type1)
			.AddUnionAlternatives(Type2);
		}
		
		return (
			aAlternatives.Any(__ => __ == aType)
			? aAlternatives
			: mStream.Stream(aType, aAlternatives)
		);
	}
	
	public static readonly tText? cUnknownPrefix = null; // TODO
	
	public static tType
	Substitute(
		this tType aType,
		tType aVariable,
		tType aReplacement
	) {
		switch (aType.Kind) {
			case tKind.TypeVariable: {
				return mStd.RefEq(aType, aVariable)
				? aReplacement
				: aType;
			}
			case tKind.Empty:
			case tKind.True:
			case tKind.False:
			case tKind.Int:
			case tKind.Type:
			case tKind.Any: {
				return aType;
			}
			case tKind.Pair:
			case tKind.Prefix:
			case tKind.Proc:
			case tKind.Ref:
			case tKind.Var:
			case tKind.Set: {
				return new tType {
					Prefix = aType.Prefix,
					Kind = aType.Kind,
					Refs = System.Array.ConvertAll(aType.Refs, __ => __.Substitute(aVariable, aReplacement))
				};
			}
			case tKind.TypeApply: {
				return TypeApply(
					aType.Refs[0].Substitute(aVariable, aReplacement),
					aType.Refs[1].Substitute(aVariable, aReplacement)
				);
			}
			case tKind.Record: {
				mAssert.IsTrue(aType.IsRecord(out var Fields));
				return Record(
					Fields.ToStream(
					).Map(
						__ => (__.Key, __.Value.Substitute(aVariable, aReplacement))
					).ToArrayList(
					).ToArray(
					)
				);
			}
			case tKind.Recursive: {
				return (
					mStd.RefEq(aType.Refs[0], aVariable)
					? aType
					: Recursive(aType.Refs[0], aType.Refs[1].Substitute(aVariable, aReplacement))
				);
			}
			case tKind.Generic: {
				return (
					mStd.RefEq(aType.Refs[0], aVariable)
					? aType
					: Generic(aType.Refs[0], aType.Refs[1].Substitute(aVariable, aReplacement))
				);
			}
			case tKind.Interface: {
				return (
					mStd.RefEq(aType.Refs[0], aVariable)
					? aType
					: Interface(aType.Refs[0], aType.Refs[1].Substitute(aVariable, aReplacement))
				);
			}
			case tKind.Sig: {
				return (
					mStd.RefEq(aType.Refs[0], aVariable)
					? aType
					: Sig(
						aType.Refs[0],
						aType.Refs[1].Substitute(aVariable, aReplacement)
					)
				);
			}
			default: {
				throw new System.NotImplementedException($"aType.Kind '{aType.Kind}'"); // TODO
			}
		}
	}
	
	public static tType
	ApplyInference(
		this tType aType,
		tInferenceState aInferenceState
	) {
		static tType
		Apply(
			tType aCurrent,
			tInferenceState aInferenceState,
			mStream.tStream<tType> aResolving
		) {
			foreach (var (Variable, OptionalSolution) in aInferenceState) {
				mAssert.IsTrue(Variable.IsTypeVariable(out _, out _));
				if (
					OptionalSolution.IsSome(out var Solution) &&
					aCurrent.ContainsVariable(Variable) &&
					!aResolving.Any(__ => mStd.RefEq(__, Variable))
				) {
					aCurrent = aCurrent.Substitute(
						Variable,
						Apply(
							Solution,
							aInferenceState,
							mStream.Stream(Variable, aResolving)
						)
					);
				}
			}
			return aCurrent;
		}
		return Apply(aType, aInferenceState, mStd.cEmpty);
	}
	
	public static tBool
	HasUnresolved(
		this tInferenceState aInferenceState,
		tType aType
	) {
		var Type = aType.ApplyInference(aInferenceState);
		return aInferenceState.Any(
			__ => __.Solution.IsNone() && Type.ContainsVariable(__.Variable)
		);
	}
	
	public static tType
	TypeVariable(
		tText aDisplayName,
		tType? aKind = null
	) {
		return new tType {
			Kind = tKind.TypeVariable,
			Id = aDisplayName,
			VariableKind = aKind ?? Type()
		};
	}
	
	public static tType
	TypeVariable(
	) => TypeVariable("t" + mStd.NewDebugId());
	
	public static tType
	KindOf(
		this tType aType
	) {
		if (aType.Kind is tKind.TypeVariable) {
			return aType.VariableKind!;
		}
		if (aType.IsGeneric(out var Head, out var Body)) {
			return Proc(Empty(), Head.KindOf(), Body.KindOf());
		}
		if (aType.IsTypeApply(out var Constructor, out _)) {
			mAssert.IsTrue(Constructor.KindOf().IsProc(out _, out _, out var ResultKind));
			return ResultKind;
		}
		return Type();
	}
	
	public static tBool
	IsTypeVariable(
		this tType aType,
		[MaybeNullWhen(false)] out tText aDisplayName,
		[MaybeNullWhen(false)] out tType aKind
	) {
		if (aType.Kind is tKind.TypeVariable) {
			aDisplayName = aType.Id!;
			aKind = aType.VariableKind!;
			return true;
		} else {
			aDisplayName = default;
			aKind = default;
			return false;
		}
	}
	
	public static tType
	Any(
	) => new() { Kind = tKind.Any };
	
	public static tBool
	IsAny(
		this tType aType
	) => aType.Kind is tKind.Any;
	
	public static tType
	Empty(
	) => new() { Kind = tKind.Empty };
	
	public static tBool
	IsEmpty(
		this tType aType
	) => aType.Kind is tKind.Empty;
	
	public static tType
	True(
	) => new() { Kind = tKind.True };
	
	public static tType
	False(
	) => new() { Kind = tKind.False };
	
	public static tType
	Bool(
	) => Set(False(), True());
	
	public static tType
	Int(
	) => new() { Kind = tKind.Int };
	
	public static tBool
	IsInt(
		this tType aType
	) => aType.Kind is tKind.Int;
	
	public static tType
	Char(
	) => Prefix("_Char...", Int());
	
	public static tBool
	IsChar(
		this tType aType
	) => aType.IsPrefix("_Char...", out var CharType) && CharType.IsInt();
	
	public static tType
	Text(
	) => mStd.With(
		TypeVariable("tText"),
		__ => Recursive(
			__,
			Set(
				Pair(__, Char()),
				Empty()
			)
		)
	);
	
	public static tType
	Type(
	) => new() {
		Kind = tKind.Type,
	};
	
	public static tBool
	IsType(
		this tType aType
	) => aType.Kind is tKind.Type;
	
	public static tType
	Pair(
		tType aType1,
		tType aType2
	) => new() {
		Kind = tKind.Pair,
		Refs = [aType1, aType2]
	};
	
	public static tBool
	IsPair(
		this tType aType,
		[MaybeNullWhen(false)] out tType aType1,
		[MaybeNullWhen(false)] out tType aType2
	) {
		if (aType.Kind is tKind.Pair) {
			aType1 = aType.Refs[0];
			aType2 = aType.Refs[1];
			return true;
		} else {
			aType1 = default;
			aType2 = default;
			return false;
		}
	}
	
	public static tBool
	TryProjectPair(
		this tType aType,
		[MaybeNullWhen(false)] out tType aLeft,
		[MaybeNullWhen(false)] out tType aRight
	) {
		if (
			aType.SubSet(
				__ => __.IsPair(out var Left, out _) ? Left : mStd.cEmpty
			).IsSome(
				out aLeft
			) &&
			aType.SubSet(
				__ => __.IsPair(out _, out var Right) ? Right : mStd.cEmpty
			).IsSome(
				out aRight
			)
		) {
			return true;
		} else {
			aLeft = default;
			aRight = default;
			return false;
		}
	}
	
	public static tType
	Sig(
		tType aHead,
		tType aBody
	) => new() {
		Kind = tKind.Sig,
		Refs = [aHead, aBody],
	};
	
	public static tType
	Sig(
		tType aHead,
		tType aHeadKind,
		tType aBody
	) {
		mAssert.AreEquals(aHead.KindOf(), aHeadKind);
		return Sig(aHead, aBody);
	}
	
	public static tBool
	IsSig(
		this tType aType,
		[MaybeNullWhen(false)] out tType aHead,
		[MaybeNullWhen(false)] out tType aHeadKind,
		[MaybeNullWhen(false)] out tType aBody
	) {
		if (aType.Kind is tKind.Sig) {
			aHead = aType.Refs[0];
			aHeadKind = aHead.KindOf();
			aBody = aType.Refs[1];
			return true;
		}
		aHead = default;
		aHeadKind = default;
		aBody = default;
		return false;
	}
	
	public static tType
	TypeApply(
		tType aConstructor,
		tType aArgument
	) {
		if (aConstructor.IsGeneric(out var Head, out var Body)) {
			return Body.Substitute(Head, aArgument);
		}
		mAssert.IsTrue(aConstructor.KindOf().IsProc(out _, out var ArgumentKind, out _));
		mAssert.IsTrue(aArgument.KindOf().IsSubType(ArgumentKind).Match(out _, out _));
		return new() {
			Kind = tKind.TypeApply,
			Refs = [aConstructor, aArgument],
		};
	}
	
	public static tBool
	IsTypeApply(
		this tType aType,
		[MaybeNullWhen(false)] out tType aConstructor,
		[MaybeNullWhen(false)] out tType aArgument
	) {
		if (aType.Kind is tKind.TypeApply) {
			aConstructor = aType.Refs[0];
			aArgument = aType.Refs[1];
			return true;
		}
		aConstructor = default;
		aArgument = default;
		return false;
	}
	
	public static tType
	GetFieldType(
		this tType aType,
		tText aKey
	) {
		mAssert.IsTrue(aType.IsRecord(out var Fields));
		mAssert.IsTrue(
			Fields.TryGet(aKey).IsSome(out var FieldType),
			$"""
			Unknown field '{aKey}' in record [{Fields.ToStream().Map(__ => __.Key).Reduce("", (a1, a2) => a1 + "\n  " + a2)}
			]
			"""
		);
		
		return FieldType;
	}
	
	public static tType
	Tuple(
		System.Span<tType> aTypes
	) => Tuple(mStream.Stream(aTypes));
	
	public static tType
	Tuple(
		mStream.tStream<tType> aTypes
	) => aTypes.Take(2).Count() switch {
		0 => Empty(),
		1 => aTypes.TryFirst().AssertNotEmpty(),
		_ => aTypes.Reduce(Empty(), Pair),
	};
	
	public static tType
	Prefix(
		tText aPrefix,
		tType aType
	) => new() {
		Kind = tKind.Prefix,
		Prefix = mAssert.IsNotNull(aPrefix),
		Refs = [aType]
	};
	
	public static tBool
	IsPrefix(
		this tType aType,
		[MaybeNullWhen(false)] out tText aPrefix,
		[MaybeNullWhen(false)] out tType aTypeOut
	) {
		if (aType.Kind is tKind.Prefix) {
			aPrefix = aType.Prefix!;
			aTypeOut = aType.Refs[0];
			return true;
		} else {
			aPrefix = default;
			aTypeOut = default;
			return false;
		}
	}
	
	public static tBool
	IsPrefix(
		this tType aType,
		tText aPrefix,
		[MaybeNullWhen(false)] out tType aTypeOut
	) {
		return aType.IsPrefix(out var Prefix, out aTypeOut) && Prefix == aPrefix;
	}
	
	public static tType
	Record(
		tType aTailType,
		tType aHeadType
	) {
		mAssert.IsIn(aTailType.Kind, [tKind.Record, tKind.Empty]);
		mAssert.IsTrue(aHeadType.IsPrefix(out var Prefix, out var Type));
		
		mTreeMap.tTree<tText, tType> Fields;
		if (aTailType.IsEmpty()) {
			Fields = mTreeMap.Tree<tText, tType>((a1, a2) => a1.CompareTo(a2).Sign(), []);
		} else {
			mAssert.IsTrue(aTailType.IsRecord(out Fields));
		}
		mAssert.IsTrue(Fields.TryGet(Prefix).IsNone(),
			$"Field '{Prefix}' already exists in record [{Fields.ToStream().Map(__ => __.Key).Reduce("", (a1, a2) => a1 + "\n  " + a2)}]"
		);
		
		return new tType {
			Kind = tKind.Record,
			Fields = Fields.Set(Prefix, Type)
		};
	}
	
	public static tType
	Record(
		(tText Prefix, tType InnerType)[] aFieldTypes
	) {
		var Result = Empty();
		foreach (var FieldType in aFieldTypes) {
			Result = Record(Result, Prefix(FieldType.Prefix, FieldType.InnerType));
		}
		return Result;
	}
	
	public static tBool
	IsRecord(
		this tType aType,
		[NotNullWhen(true)]out mTreeMap.tTree<tText, tType> aFields
	) {
		if (aType.Kind is tKind.Record) {
			aFields = aType.Fields;
			return true;
		} else {
			aFields = default;
			return false;
		}
	}
	
	public static tType
	Proc(
		tType aObjType,
		tType aArgType,
		tType aResType
	) => new() {
		Kind = tKind.Proc,
		Refs = [aObjType, aArgType, aResType]
	};
	
	public static tBool
	IsProc(
		this tType aType,
		[MaybeNullWhen(false)] out tType aObjType,
		[MaybeNullWhen(false)] out tType aArgType,
		[MaybeNullWhen(false)] out tType aResType
	) {
		if (aType.IsRecursive(out var Head, out var Body)) {
			aType = Body;
		}
		
		if (aType.Kind is tKind.Proc) {
			aObjType = aType.Refs[0];
			aArgType = aType.Refs[1];
			aResType = aType.Refs[2];
			return true;
		} else {
			aObjType = default;
			aArgType = default;
			aResType = default;
			return false;
		}
	}
	
	public static tType
	Prefix(
		tText aPrefix
	) => Prefix(aPrefix, Empty());
	
	public static tBool
	IsPrefix(
		this tType aType,
		tText aPrefix
	) {
		return aType.Prefix == aPrefix && aType.Refs[0].IsEmpty();
	}
	
	public static tType
	Ref(
		tType aType
	) => new() {
		Kind = tKind.Ref,
		Refs = [aType]
	};
	
	public static tBool
	IsRef(
		this tType aType,
		[MaybeNullWhen(false)] out tType aOutType
	) {
		if (aType.Kind is tKind.Ref) {
			aOutType = aType.Refs[0];
			return true;
		} else {
			aOutType = default;
			return false;
		}
	}
	
	public static tType
	Var(
		tType aType
	) => new() {
		Kind = tKind.Var,
		Refs = [aType]
	};
	
	public static tBool
	IsVar(
		this tType aType,
		[MaybeNullWhen(false)] out tType aOutType
	) {
		if (aType.Kind is tKind.Var) {
			aOutType = aType.Refs[0];
			return true;
		} else {
			aOutType = default;
			return false;
		}
	}
	
	public static tType
	Set(
		tType aType1,
		tType aType2
	) {
		mAssert.IsNotNull(aType1);
		mAssert.IsNotNull(aType2);
		return new() {
			Kind = tKind.Set,
			Refs = [aType1, aType2]
		};
	}
	
	public static tBool
	IsSet(
		this tType aType,
		[MaybeNullWhen(false)] out tType aType1,
		[MaybeNullWhen(false)] out tType aType2
	) {
		if (aType.Kind is tKind.Set) {
			aType1 = aType.Refs[0];
			aType2 = aType.Refs[1];
			return true;
		} else {
			aType1 = default;
			aType2 = default;
			return false;
		}
	}
	
	public static tType
	Cond(
		tType aType
	) {
		mAssert.IsTrue(false); // TODO
		return new tType {
			Kind = tKind.Cond,
			Refs = [aType],
		};
	}
	
	public static tBool
	IsCond(
		this tType aType,
		out tType aSuperType
	) {
		mAssert.IsTrue(false); // TODO
		if (aType.Kind is tKind.Cond) {
			aSuperType = aType.Refs[0];
			return true;
		} else {
			aSuperType = default;
			return false;
		}
	}
	
	public static tType
	Recursive(
		tType aTypeHead,
		tType aTypeBody
	) {
		mAssert.IsTrue(aTypeHead.Kind is tKind.TypeVariable);
		
		static tBool
		IsGuarded(
			tType aType,
			tType aTypeHead,
			tBool aGuarded
		) {
			if (aType.Kind is tKind.TypeVariable) {
				return !mStd.RefEq(aType, aTypeHead) || aGuarded;
			} else if (aType.Kind is tKind.TypeApply) {
				mAssert.IsTrue(aType.IsTypeApply(out var Constructor, out var Argument));
				return (
					Constructor.IsGeneric(out var Parameter, out var Definition)
					? IsGuarded(Definition.Substitute(Parameter, Argument), aTypeHead, aGuarded)
					: IsGuarded(Constructor, aTypeHead, aGuarded) &&
						IsGuarded(Argument, aTypeHead, aGuarded)
				);
			} else if (aType.Kind is tKind.Recursive or tKind.Generic or tKind.Interface or tKind.Sig) {
				return (
					mStd.RefEq(aType.Refs[0], aTypeHead) ||
					IsGuarded(aType.Refs[1], aTypeHead, aGuarded)
				);
			} else {
				var ChildrenAreGuarded = (
					aGuarded ||
					aType.Kind is tKind.Record or tKind.Pair or tKind.Prefix or tKind.Proc
				);
				
				return (
					aType.Kind is tKind.Record
					? aType.Fields.ToStream().All(
						__ => IsGuarded(__.Value, aTypeHead, ChildrenAreGuarded)
					)
					: mStream.Stream(aType.Refs).All(
						__ => IsGuarded(__, aTypeHead, ChildrenAreGuarded)
					)
				);
			}
		}
		
		mAssert.IsTrue(
			IsGuarded(aTypeBody, aTypeHead, false),
			() => $"recursive type variable '{aTypeHead.ToText()}' is not guarded by a type constructor"
		);
		
		return new() {
			Kind = tKind.Recursive,
			Refs = [aTypeHead, aTypeBody],
		};
	}
	
	public static tBool
	IsRecursive(
		this tType aType,
		[MaybeNullWhen(false)]out tType aHeadType,
		[MaybeNullWhen(false)]out tType aBodyType
	) {
		if (aType.Kind is tKind.Recursive) {
			aHeadType = aType.Refs[0];
			mAssert.AreEquals(aHeadType.Kind, tKind.TypeVariable);
			aBodyType = aType.Refs[1];
			return true;
		} else {
			aHeadType = default;
			aBodyType = default;
			return false;
		}
	}
	
	public static tType
	Interface(
		tType aTypeHead,
		tType aTypeBody
	) => new() {
		Kind = tKind.Interface,
		Refs = [aTypeHead, aTypeBody],
	};
	
	public static tBool
	IsInterface(
		this tType aType,
		[MaybeNullWhen(false)] out tType aHeadType,
		[MaybeNullWhen(false)] out tType aBodyType
	) {
		if (aType.Kind is tKind.Interface) {
			aHeadType = aType.Refs[0];
			aBodyType = aType.Refs[1];
			return true;
		} else {
			aHeadType = default;
			aBodyType = default;
			return false;
		}
	}
	
	public static tType
	Generic(
		tType aTypeHead,
		tType aTypeBody
	) => new() {
		Kind = tKind.Generic,
		Refs = [aTypeHead, aTypeBody],
	};
	
	public static tBool
	IsGeneric(
		this tType aType,
		[MaybeNullWhen(false)] out tType aHeadType,
		[MaybeNullWhen(false)] out tType aBodyType
	) {
		if (aType.Kind is tKind.Generic ) {
			aHeadType = aType.Refs[0];
			aBodyType = aType.Refs[1];
			return true;
		} else {
			aHeadType = default;
			aBodyType = default;
			return false;
		}
	}
	
	private static tText
	ExtendError(
		tText aError,
		tType aSubType,
		tType aSupType
	) => (
		$"""
		{aError}
		in:
		{"  " + aSubType.ToText("\n  ")}
		!<
		{"  " + aSupType.ToText("\n  ")}
		
		"""
	);
	
	public static tBool
	ContainsVariable(
		this tType aType,
		tType aVariable
	) {
		if (aType.Kind is tKind.TypeVariable) {
			return mStd.RefEq(aType, aVariable);
		} else if (aType.Kind is tKind.Record) {
			return aType.Fields.ToStream().Any(__ => __.Value.ContainsVariable(aVariable));
		} else if (
			aType.Kind is tKind.Recursive or tKind.Generic or tKind.Interface or tKind.Sig &&
			mStd.RefEq(aType.Refs[0], aVariable)
		) {
			return false;
		} else {
			return mStream.Stream(aType.Refs).Any(__ => __.ContainsVariable(aVariable));
		}
	}
	
	public static mResult.tResult<tInferenceState, tText>
	IsSubType(
		this tType aSubType,
		tType aSupType
	) => IsSubTypeOf(
		aSubType,
		aSupType,
		NewInferenceState(),
		mStream.Stream<(tType SubType, tType SupType)>()
	);
	
	public static mResult.tResult<tInferenceState, tText>
	IsSubTypeOf(
		this tType aSubType,
		tType aSupType,
		tInferenceState aInferenceState
	) => IsSubTypeOf(
		aSubType,
		aSupType,
		aInferenceState,
		mStream.Stream<(tType SubType, tType SupType)>()
	);
	
	private static mResult.tResult<tInferenceState, tText>
	IsSubTypeOf(
		this tType aSubType,
		tType aSupType,
		tInferenceState aInferenceState,
		mStream.tStream<(tType SubType, tType SupType)> aRecursiveAssumptions
	) {
		static mResult.tResult<tInferenceState, tText>
		Solve(
			tInferenceState aCurrent,
			tType aVariable,
			tType aSolution
		) {
			if (
				!aCurrent.Where(
					__ => mStd.RefEq(__.Variable, aVariable)
				).TryFirst(
				).IsSome(
					out var Entry
				)
			) {
				return mResult.Fail(
					$"rigid type variable '{aVariable.ToText()}' cannot be unified with '{aSolution.ToText()}'"
				);
			} else if (!mStd.RefEq(aSolution, aVariable) && aSolution.ContainsVariable(aVariable)) {
				return mResult.Fail(
					$"occurs check failed for '{aVariable.ToText()}' in '{aSolution.ToText()}'"
				);
			} else if (Entry.Solution.IsSome(out var ExistingSolution)) {
				var Existing = ExistingSolution.ApplyInference(aCurrent);
				var Solution = aSolution.ApplyInference(aCurrent);
				var WiderSolution = (
					Solution.IsSubType(Existing).Match(out _, out _) ? Existing :
					Existing.IsSubType(Solution).Match(out _, out _) ? Solution :
					Union(Existing, Solution)
				);
				
				return mStream.Stream(
					(aVariable, mMaybe.Some(WiderSolution)),
					aCurrent.Where(a => !mStd.RefEq(a.Variable, aVariable))
				);
			} else {
				return (
					!aSolution.KindOf().IsSubType(aVariable.KindOf()).Match(out _, out var KindError)
					? mResult.Fail(KindError)
					: mStream.Stream(
						(aVariable, mMaybe.Some(aSolution)),
						aCurrent.Where(__ => !mStd.RefEq(__.Variable, aVariable))
					)
				);
			}
		}
		
		static mMaybe.tMaybe<(tType Type1, tType Type2)>
		SplitFirstNestedUnion(
			// Value constructors distribute over unions. Split one nested choice at a time so
			// subtype checks never have to materialize their full Cartesian product.
			tType aType
		) {
			static mMaybe.tMaybe<(tType Type1, tType Type2)>
			Split(
				tType aChild
			) => (
				aChild.IsSet(out var Type1, out var Type2)
				? (Type1, Type2)
				: SplitFirstNestedUnion(aChild)
			);
			
			if (aType.IsPair(out var First, out var Second)) {
				if (Split(First).IsSome(out var SplitFirst)) {
					return (Pair(SplitFirst.Type1, Second), Pair(SplitFirst.Type2, Second));
				}
				
				if (Split(Second).IsSome(out var SplitSecond)) {
					return (Pair(First, SplitSecond.Type1), Pair(First, SplitSecond.Type2));
				}
			}
			
			if (aType.IsPrefix(out var Prefix_, out var Inner)) {
				if (Split(Inner).IsSome(out var SplitInner)) {
					return (Prefix(Prefix_, SplitInner.Type1), Prefix(Prefix_, SplitInner.Type2));
				}
			}
			
			return mStd.cEmpty;
		}
		
		static mResult.tResult<tInferenceState, tText>
		Merge(
			tInferenceState aLeft,
			tInferenceState aRight,
			tInferenceState aInitial
		) {
			static tBool
			IsUnchanged(
				mMaybe.tMaybe<tType> aSolution,
				mMaybe.tMaybe<tType> aInitialSolution
			) => (
				!aInitialSolution.IsSome(out var Initial)
				? aSolution.IsNone()
				: aSolution.IsSome(out var Solution) && Solution == Initial
			);
			
			var Result = aInitial;
			foreach (var Entry in aInitial) {
				var Left = aLeft.Where(
					__ => mStd.RefEq(__.Variable, Entry.Variable)
				).TryFirst(
				).AssertNotEmpty(
				).Solution;
				
				var Right = aRight.Where(
					__ => mStd.RefEq(__.Variable, Entry.Variable)
				).TryFirst(
				).AssertNotEmpty(
				).Solution;
				
				if (
					IsUnchanged(Left, Entry.Solution) ||
					IsUnchanged(Right, Entry.Solution)
				) {
					continue;
				}
				
				foreach (var Solution in mStream.Stream(Left, Right)) {
					if (
						Solution.IsSome(out var Type) &&
						!Solve(Result, Entry.Variable, Type).Match(out Result, out var Error)
					) {
						return mResult.Fail(Error);
					}
				}
			}
			
			return Result;
		}
		
		if (
			mStd.RefEq(aSubType, aSupType) ||
			aSubType == aSupType
		) {
			return aInferenceState;
		}
		
		if (
			aSubType.Kind is tKind.Recursive &&
			aSupType.Kind is tKind.TypeVariable
		) {
			return Solve(aInferenceState, aSupType, aSubType);
		}
		
		if (
			aSubType.Kind is tKind.TypeVariable &&
			aSupType.Kind is tKind.Recursive
		) {
			return Solve(aInferenceState, aSubType, aSupType);
		}
		
		if (
			aRecursiveAssumptions.Any(
				__ => (
					mStd.RefEq(__.SubType, aSubType) &&
					mStd.RefEq(__.SupType, aSupType)
				)
			)
		) {
			return aInferenceState;
		}
		
		static tType
		Expand(
			tType aType
		) => (
			aType.IsRecursive(out var Head, out var Body)
			? Body.Substitute(Head, aType)
			: aType
		);
		
		if (aSubType.Kind is tKind.Recursive || aSupType.Kind is tKind.Recursive) {
			var ExpandedSubType = Expand(aSubType);
			var ExpandedSupType = Expand(aSupType);
			
			if (
				mStd.RefEq(ExpandedSubType, aSubType) &&
				mStd.RefEq(ExpandedSupType, aSupType)
			) {
				return mResult.Fail(
					$"recursive subtype comparison makes no progress between '{aSubType.ToText()}' and '{aSupType.ToText()}'"
				);
			} else {
				return ExpandedSubType.IsSubTypeOf(
					ExpandedSupType,
					aInferenceState,
					mStream.Stream(
						(SubType: aSubType, SupType: aSupType),
						aRecursiveAssumptions
					)
				).ModifyError(__ => ExtendError(__, aSubType, aSupType));
			}
		}
		
		var SubBaseType = aSubType.BaseType();
		if (SubBaseType.IsSet(out var SubChoice1, out var SubChoice2)) {
			return SubChoice1.IsSubTypeOf(
				aSupType,
				aInferenceState,
				aRecursiveAssumptions
			).ThenTry(
				__ => SubChoice2.IsSubTypeOf(aSupType, __, aRecursiveAssumptions)
			).ModifyError(
				__ => ExtendError(__, aSubType, aSupType)
			);
		} else if (
			aSubType.Kind is tKind.TypeVariable &&
			aSupType.Kind is tKind.Set &&
			aInferenceState.IsRegistered(aSubType) &&
			!aSupType.ContainsVariable(aSubType)
		) {
			return Solve(aInferenceState, aSubType, aSupType);
		} else if (aSupType.IsSet(out var SupChoice1, out var SupChoice2)) {
			var Match1 = SubBaseType.IsSubTypeOf(
				SupChoice1,
				aInferenceState,
				aRecursiveAssumptions
			);
			var Match2 = SubBaseType.IsSubTypeOf(
				SupChoice2,
				aInferenceState,
				aRecursiveAssumptions
			);
			var HasMatch1 = Match1.Match(out var Inference1, out var Error1);
			var HasMatch2 = Match2.Match(out var Inference2, out var Error2);
			
			if (HasMatch1 && HasMatch2) {
				return Merge(Inference1, Inference2, aInferenceState);
			} else if (HasMatch1) {
				return Inference1;
			} else if (HasMatch2) {
				return Inference2;
			} else {
				return SplitFirstNestedUnion(SubBaseType).Match(
					Split => Split.Type1.IsSubTypeOf(
						aSupType,
						aInferenceState,
						aRecursiveAssumptions
					).ThenTry(
						__ => Split.Type2.IsSubTypeOf(aSupType, __, aRecursiveAssumptions)
					),
					() => mResult.Fail(Error1 + "\n" + Error2)
				);
			}
		} else if (
			aSupType.Kind is tKind.TypeVariable &&
			aInferenceState.IsRegistered(aSupType)
		) {
			return Solve(aInferenceState, aSupType, aSubType);
		} else if (
			aSubType.Kind is tKind.TypeVariable &&
			aInferenceState.IsRegistered(aSubType)
		) {
			return Solve(aInferenceState, aSubType, aSupType);
		} else if (aSupType.Kind is tKind.TypeVariable) {
			return Solve(aInferenceState, aSupType, aSubType);
		} else if (aSubType.Kind is tKind.TypeVariable) {
			return Solve(aInferenceState, aSubType, aSupType);
		}
		
		// TODO: implement
		switch (aSupType.Kind) {
			case tKind.TypeVariable: {
				throw new System.NotImplementedException();
			}
			case tKind.Any: {
				return aInferenceState;
			}
			case tKind.Empty:
			case tKind.True:
			case tKind.False:
			case tKind.Int:
			case tKind.Type: {
				return SubBaseType.Kind == aSupType.Kind
					? aInferenceState
					: mResult.Fail(ExtendError("", aSubType, aSupType));
			}
			case tKind.Pair: {
				var TailSubType = SubBaseType;
				var TailSupType = aSupType;
				var Error = "";
				if (
					TailSubType.IsPair(out TailSubType, out var HeadSubType) &&
					TailSupType.IsPair(out TailSupType, out var HeadSupType) &&
					HeadSubType.IsSubTypeOf(
						HeadSupType,
						aInferenceState,
						aRecursiveAssumptions
					).Match(out var Inference, out Error) &&
					TailSubType.IsSubTypeOf(
						TailSupType,
						Inference,
						aRecursiveAssumptions
					).Match(out Inference, out Error)
				) {
					return Inference;
				} else {
					return mResult.Fail(ExtendError(Error, aSubType, aSupType));
				}
			}
			case tKind.Sig: {
				if (
					!SubBaseType.IsSig(out var SubHead, out var SubKind, out var SubBody) ||
					!aSupType.IsSig(out var SupHead, out var SupKind, out var SupBody)
				) {
					return mResult.Fail(ExtendError("Expected §SIG_WITH", aSubType, aSupType));
				}
				
				var Binder = TypeVariable("sig", SupKind);
				
				return (
					SubKind.IsSubTypeOf(SupKind, aInferenceState, aRecursiveAssumptions)
				).ThenTry(
					__ => SupKind.IsSubTypeOf(SubKind, __, aRecursiveAssumptions)
				).ThenTry(
					__ => (
						SubBody.Substitute(SubHead, Binder)
					).IsSubTypeOf(
						SupBody.Substitute(SupHead, Binder),
						__,
						aRecursiveAssumptions
					)
				).ModifyError(
					__ => ExtendError(__, aSubType, aSupType)
				);
			}
			case tKind.TypeApply: {
				if (
					!SubBaseType.IsTypeApply(out var SubConstructor, out var SubArgument) ||
					!aSupType.IsTypeApply(out var SupConstructor, out var SupArgument)
				) {
					return mResult.Fail(ExtendError("Expected type application", aSubType, aSupType));
				} else {
					return (
						SubConstructor.IsSubTypeOf(SupConstructor, aInferenceState, aRecursiveAssumptions)
					).ThenTry(
						__ => SupConstructor.IsSubTypeOf(SubConstructor, __, aRecursiveAssumptions)
					).ThenTry(
						__ => SubArgument.IsSubTypeOf(SupArgument, __, aRecursiveAssumptions))
					.ThenTry(
						__ => SupArgument.IsSubTypeOf(SubArgument, __, aRecursiveAssumptions)
					);
				}
			}
			case tKind.Prefix: {
				if (
					SubBaseType.IsPrefix(out var SubPrefix, out var Sub) &&
					aSupType.IsPrefix(out var SupPrefix, out var Sup) &&
					SubPrefix == SupPrefix
				) {
					return Sub.IsSubTypeOf(
						Sup,
						aInferenceState,
						aRecursiveAssumptions
					).ElseTry(
						__ => mResult.Fail(ExtendError(__, aSubType, aSupType))
					);
				} else {
					return mResult.Fail(ExtendError("", aSubType, aSupType));
				}
			}
			case tKind.Record: {
				if (!aSupType.IsRecord(out var SupFields)) {
					return mResult.Fail(
						ExtendError(
							$"Expected Record but is {aSupType}",
							aSubType,
							aSupType
						)
					);
				} else if (!aSubType.IsRecord(out var SubFields)) {
					return mResult.Fail(
						ExtendError(
							$"Expected Record but is {aSubType}",
							aSubType,
							aSupType
						)
					);
				} else {
					foreach (var SupField in SupFields.ToStream()) {
						if (!SubFields.TryGet(SupField.Key).IsSome(out var SubField)) {
							return mResult.Fail(
								ExtendError(
									$"Missing field '{SupField.Key}' in {aSubType}",
									aSubType,
									aSupType
								)
							);
						} else if (
							!SubField.IsSubTypeOf(
								SupField.Value,
								aInferenceState,
								aRecursiveAssumptions
							).Match(
								out aInferenceState,
								out var Error
							)
						) {
							return mResult.Fail(ExtendError(Error, aSubType, aSupType));
						}
					}
					return aInferenceState;
				}
			}
			case tKind.Proc: {
				if (
					!aSubType.IsProc(out var SubObj, out var SubArg, out var SubRes) ||
					!aSupType.IsProc(out var SupObj, out var SupArg, out var SupRes)
				) {
					return mResult.Fail(mStd.FileLine());
				} else {
					return (
						SubObj.IsSubTypeOf(SupObj, aInferenceState, aRecursiveAssumptions)
					).ThenTry(
						__ => SupObj.IsSubTypeOf(SubObj, __, aRecursiveAssumptions)
					).ThenTry(
						__ => SupArg.IsSubTypeOf(SubArg, __, aRecursiveAssumptions)
					).ThenTry(
						__ => SubRes.IsSubTypeOf(SupRes, __, aRecursiveAssumptions)
					).ElseTry(
						__ => mResult.Fail(ExtendError(__, aSubType, aSupType))
					);
				}
			}
			case tKind.Var: {
				throw new System.NotImplementedException();
			}
			case tKind.Ref: {
				throw new System.NotImplementedException();
			}
			case tKind.Cond: {
				throw new System.NotImplementedException();
			}
			case tKind.Recursive: {
				mAssert.Impossible();
				return default;
			}
			case tKind.Generic:
			case tKind.Interface: {
				var SupHead = aSupType.Refs[0];
				var SupBody = aSupType.Refs[1];
				var SubBody = aSubType;
				
				if (
					aSubType.Kind == aSupType.Kind &&
					!mStd.RefEq(aSubType.Refs[0], SupHead)
				) {
					SubBody = aSubType.Refs[1].Substitute(aSubType.Refs[0], SupHead);
				} else if (aSubType.Kind == aSupType.Kind) {
					SubBody = aSubType.Refs[1];
				}
				
				return SubBody.IsSubTypeOf(
					SupBody,
					aInferenceState,
					aRecursiveAssumptions
				).ModifyError(
					__ => ExtendError(__, aSubType, aSupType)
				);
			}
			default: {
				mAssert.Impossible();
				return default;
			}
		}
	}
	
	public static mMaybe.tMaybe<tType>
	SubSet(
		this tType aType,
		mStd.tFunc<tType, mMaybe.tMaybe<tType>> aSelect
	) {
		if (aSelect(aType).IsSome(out var Result)) {
			return Result;
		}
		
		if (aType.IsRecursive(out var Head, out var Body)) {
			var Expanded = Body.Substitute(Head, aType);
			
			return (
				mStd.RefEq(Expanded, aType)
				? mStd.cEmpty
				: Expanded.SubSet(aSelect)
			);
		}
		
		if (aType.IsSet(out var Type1, out var Type2)) {
			var Selected1 = Type1.SubSet(aSelect);
			var Selected2 = Type2.SubSet(aSelect);
			
			return (
				!Selected1.IsSome(out var SelectedType1) ? Selected2 :
				!Selected2.IsSome(out var SelectedType2) ? Selected1 :
				SelectedType1 == SelectedType2 ? Selected1 :
				Set(SelectedType1, SelectedType2)
			);
		}
		
		return mStd.cEmpty;
	}
	
	public static tType
	BaseType(
		this tType a
	) {
		if (a.Kind is tKind.Cond) {
			if (a.IsCond(out var Sup)) {
				return BaseType(Sup);
			} else {
				mAssert.Impossible();
				return default;
			}
		} else {
			return a;
		}
	}
	
	public static mResult.tResult<tType, tText>
	InferCallResultType(
		tType aProc,
		tType aObj,
		tType aArg
	) {
		var State = NewInferenceState();
		while (aProc.IsGeneric(out var Variable, out var InnerType)) {
			mAssert.IsTrue(Variable.IsTypeVariable(out var Name, out var Kind));
			var FreshVariable = TypeVariable(Name, Kind);
			State = State.AddVar(FreshVariable);
			aProc = InnerType.Substitute(Variable, FreshVariable);
		}
		
		if (!aProc.IsProc(out var ObjType, out var ArgType, out var ResType)) {
			return mResult.Fail($"expect proc but is:\n{aProc.ToText()}");
		} else if (!aObj.IsSubTypeOf(ObjType, State).Match(out State, out var Error)) {
			return mResult.Fail(ExtendError(Error, aObj, ObjType));
		} else if (!aArg.IsSubTypeOf(ArgType, State).Match(out State, out Error)) {
			return mResult.Fail(
				ExtendError(
					$"""
					can't convert:
					{aArg.ToText()}
					to:
					{ArgType.ToText()}
					because:
					{Error}
					""",
					aArg,
					ArgType
				)
			);
		} else {
			var Result = ResType.ApplyInference(State);
			
			return (
				State.HasUnresolved(Result)
				? mResult.Fail(
					$"""
					call result contains unresolved local type variables:
					{Result.ToText()}
					"""
				)
				: Result
			);
		}
	}
	
	public static (mMaybe.tMaybe<tType> Matched, mMaybe.tMaybe<tType> Remainder)
	SplitBy(
		this tType aType,
		mStd.tFunc<tType, tBool> aIsMatching
	) {
		if (aType.IsRecursive(out var Head, out var Body)) {
			var Expanded = Body.Substitute(Head, aType);
			
			return (
				mStd.RefEq(Expanded, aType)
				? (mStd.cEmpty, mMaybe.Some(aType))
				: Expanded.SplitBy(aIsMatching)
			);
		} else if (aType.IsSet(out var Type1, out var Type2)) {
			var (Matched1, Remainder1) = Type1.SplitBy(aIsMatching);
			var (Matched2, Remainder2) = Type2.SplitBy(aIsMatching);
			
			return (
				Union(Matched1, Matched2),
				Union(Remainder1, Remainder2)
			);
		} else {
			return (
				aIsMatching(aType)
				? (aType, mStd.cEmpty)
				: (mStd.cEmpty, aType)
			);
		}
	}
	
	public static tType
	Union(
		tType aType1,
		tType aType2
	) {
		var Alternatives = mStream.Stream<tType>(
		).AddUnionAlternatives(
			aType1
		).AddUnionAlternatives(
			aType2
		).Reverse();
		
		mAssert.IsTrue(Alternatives.Is(out var Result, out var Tail));
		
		return Tail.Reduce(Result, Set);
	}
	
	public static mMaybe.tMaybe<tType>
	Union(
		mMaybe.tMaybe<tType> aType1,
		mMaybe.tMaybe<tType> aType2
	) => (
		!aType1.IsSome(out var Type1) ? aType2 :
		!aType2.IsSome(out var Type2) ? aType1 :
		mMaybe.Some(Union(Type1, Type2))
	);
	
	public static mMaybe.tMaybe<tType>
	Subtract(
		this tType aType,
		tType aRemoved
	) {
		static mMaybe.tMaybe<tType>
		Difference(
			tType aCurrentType,
			tType aCurrentRemoved,
			mStream.tStream<(tNat64 Type, tNat64 Removed)> aActive
		) {
			if (aCurrentType.IsSubType(aCurrentRemoved).Match(out _, out _)) {
				return mStd.cEmpty;
			}
			
			var Pair_ = (aCurrentType.Identity, aCurrentRemoved.Identity);
			if (aActive.Any(__ => __ == Pair_)) {
				return aCurrentType;
			}
			
			var Active = mStream.Stream(Pair_, aActive);
			if (aCurrentType.IsSet(out var Type1, out var Type2)) {
				return Union(
					Difference(Type1, aCurrentRemoved, Active),
					Difference(Type2, aCurrentRemoved, Active)
				);
			} else if (aCurrentRemoved.IsSet(out Type1, out Type2)) {
				return Difference(aCurrentType, Type1, Active).ThenTry(
					__ => Difference(__, Type2, Active)
				);
			} else if (
				aCurrentType.IsRecursive(out var Head1, out var Body1) &&
				aCurrentRemoved.IsRecursive(out var Head2, out var Body2)
			) {
				var Binder = mStd.RefEq(Head1, Head2) ? Head1 : TypeVariable();
				return Difference(
					Body1.Substitute(Head1, Binder),
					Body2.Substitute(Head2, Binder),
					Active
				).Then(__ => Recursive(Binder, __));
			} else if (aCurrentType.IsRecursive(out var Head, out var Body)) {
				return Difference(
					Body.Substitute(Head, aCurrentType),
					aCurrentRemoved,
					Active
				);
			} else if (aCurrentRemoved.IsRecursive(out Head, out Body)) {
				return Difference(
					aCurrentType,
					Body.Substitute(Head, aCurrentRemoved),
					Active
				);
			} else if (
				aCurrentType.IsPair(out var First, out var Second) &&
				aCurrentRemoved.IsPair(out var RemovedFirst, out var RemovedSecond)
			) {
				var FirstRemainder = Difference(
					First,
					RemovedFirst,
					Active
				).Then(__ => Pair(__, Second));
				var SecondRemainder = Difference(
					Second,
					RemovedSecond,
					Active
				).Then(__ => Pair(First, __));
				return Union(FirstRemainder, SecondRemainder);
			} else if (
				aCurrentType.IsPrefix(out var Prefix_, out var Inner) &&
				aCurrentRemoved.IsPrefix(Prefix_, out var RemovedInner)
			) {
				return Difference(
					Inner,
					RemovedInner,
					Active
				).Then(__ => Prefix(Prefix_, __));
			} else {
				return aCurrentType;
			}
		}
		
		return Difference(
			aType,
			aRemoved,
			mStream.Stream<(tNat64 Type, tNat64 Removed)>()
		);
	}
	
	public static tText
	ToText(
		this tType aType,
		tText aIndent = "\n"
	) {
		tText __;
		tText ____;
		if (aIndent.Length is 0 || aIndent[0] is not '\n') {
			__ = " ";
			____ = " ";
		} else {
			var OneLiner = aType.ToText("");
			if (OneLiner.Length <= 80) {
				return OneLiner;
			}
			
			__ = aIndent;
			____ = __ + "  ";
		}
		
		return aType.Kind switch {
			tKind.Empty => "[]",
			tKind.True => "§TRUE",
			tKind.False => "§FALSE",
			tKind.Int => "§INT",
			tKind.Any => "§ANY",
			tKind.Type => "§TYPE",
			tKind.TypeVariable => "?" + aType.Id,
			tKind.TypeApply => $"[{____}.{aType.Refs[0].ToText(____)} {aType.Refs[1].ToText(____)}{__}]",
			tKind.Prefix => $"[{____}#{aType.Prefix} {aType.Refs[0].ToText(____)}{__}]",
			tKind.Record => mStd.Call(
				() => {
					var Text = "";
					var Type = aType;
					mAssert.IsTrue(Type.IsRecord(out var Fields));
					foreach (var Field in Fields.ToStream()) {
						Text += $"{____}{Field.Key} : {Field.Value.ToText(____)}";
						Text += ", ";
					}
					return "[{" + Text + __ + "}]";
				}
			),
			tKind.Pair => mStd.Call(
				() => {
					var Result = aType.Refs[1].ToText(____);
					
					var Temp = aType.Refs[0];
					
					if (Temp.Kind is tKind.Empty) {
						Result = Temp.ToText(____) + ";" + ____ + Result;
					} else {
						while (Temp.Kind is tKind.Pair) {
							Result = Temp.Refs[1].ToText(____) + "," + ____ + Result;
							Temp = Temp.Refs[0];
						}
						if (Temp.Kind is not tKind.Empty) {
							Result = Temp.ToText(____) + ";" + ____ + Result;
						}
					}
					return "[" + ____ + Result + __ + "]";
				}
			),
			tKind.Proc => mStd.Call(
				() => {
					var Result = "";
					if (aType.Refs[0].Kind is not tKind.Empty) {
						Result += ____ + aType.Refs[0].ToText(____) + " :";
					}
					if (aType.Refs[1].Kind is not tKind.Empty) {
						Result += ____ + aType.Refs[1].ToText(____);
					}
					if (aType.Refs[2].Kind is not tKind.Empty) {
						Result += ____ + "-> " + aType.Refs[2].ToText(____);
					}
					return "[" + Result + __ + "]";
				}
			),
			tKind.Ref => $"[{____}§REF {aType.Refs[0].ToText(____)}{__}]",
			tKind.Set => $"[{____}{mStream.Stream(System.MemoryExtensions.AsSpan(aType.Refs)).Map(aChild => aChild.ToText(____)).Join((a1, a2) => a1 + " |" + ____ + a2, "")}{__}]",
			tKind.Var => $"[{____}§VAR {aType.Refs[0].ToText(____)}{__}]",
			tKind.Recursive => $"[{____}§RECURSIVE {aType.Refs[0]} IN {aType.Refs[1].ToText(____)}{__}]",
			tKind.Generic => $"[{____}§GENERIC {aType.Refs[0]} IN {aType.Refs[1].ToText(____)}{__}]",
			tKind.Interface => $"[{____}§INTERFACE {aType.Refs[0]} IN {aType.Refs[1].ToText(____)}{__}]",
			tKind.Sig => $"[{____}§SIG_WITH {aType.Refs[0]} € {aType.Refs[0].KindOf().ToText(____)} IN {aType.Refs[1].ToText(____)}{__}]",
			tKind.Cond => $"[{____ + aType.Refs[0].ToText(____)} ? ...{__}]", // TODO
			_ => throw mError.Error("impossible")
		};
	}
}
