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

public static class
mVM_Type {
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
		foreach (var (Variable, Solution) in aMappings) {
			mAssert.IsTrue(Variable.IsTypeVariable(out _, out _));
			aType = aType.Substitute(Variable, Solution);
		}
		
		return aType;
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
	) => new() {
		Kind = tKind.Recursive,
		Refs = [aTypeHead, aTypeBody],
	};
	
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
	
	public static mStream.tStream<(tType Variable, tType Solution)>
	InferenceVariables(
		this tType aType
	) {
		static mStream.tStream<tType>
		Collect(
			tType aType,
			mStream.tStream<tType> aBound
		) {
			if (aType.Kind is tKind.TypeVariable) {
				return aBound.Any(__ => mStd.RefEq(__, aType))
				? mStd.cEmpty
				: mStream.Stream(aType);
			}
			if (aType.Kind is tKind.Record) {
				return aType.Fields.ToStream().Map(__ => Collect(__.Value, aBound)).Reduce(
					mStream.Stream<tType>(),
					(a1, a2) => mStream.Concat(a1, a2)
				);
			}
			if (aType.Kind is tKind.Recursive or tKind.Generic or tKind.Interface or tKind.Sig) {
				var Bound = mStream.Stream(aType.Refs[0], aBound);
				return mStream.Stream(System.MemoryExtensions.AsSpan(aType.Refs)[1..])
				.Map(__ => Collect(__, Bound)).Reduce(
					mStream.Stream<tType>(),
					(a1, a2) => mStream.Concat(a1, a2)
				);
			}
			return mStream.Stream(aType.Refs).Map(__ => Collect(__, aBound)).Reduce(
				mStream.Stream<tType>(),
				(a1, a2) => mStream.Concat(a1, a2)
			);
		}
		
		return Collect(aType, mStd.cEmpty).Map(__ => (Variable: __, Solution: __));
	}
	
	public static mResult.tResult<tInferenceState, tText>
	IsSubType(
		this tType aSubType,
		tType aSupType,
		tInferenceState aInferenceState
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
			}
			if (!mStd.RefEq(Entry.Solution, aVariable)) {
				if (aSolution.IsSubType(Entry.Solution, aInference).Match(out var RefinedInference, out _)) {
					return RefinedInference;
				}
				
				var WiderSolution = (
					Entry.Solution.IsSubType(aSolution, aInference).Match(out _, out _)
					? aSolution
					: Union(Entry.Solution, aSolution)
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
			
			return (
				!aSolution.KindOf().IsSubType(aVariable.KindOf(), mStd.cEmpty).Match(out _, out var KindError)
				? mResult.Fail(KindError)
				: mStream.Stream((aVariable, aSolution), aInference)
			);
		}
		
		if (
			mStd.RefEq(aSubType, aSupType) ||
			aSubType == aSupType
		) {
			return aTypeMappings;
		}
		
		var SubBaseType = aSubType.BaseType();
		
		if (SubBaseType.IsSet(out var SubType1, out var SubType2)) {
			return SubType1.IsSubType(aSupType, aTypeMappings).ThenTry(
				__ => SubType2.IsSubType(aSupType, __)
			).ModifyError(
				__ => ExtendError(__, aSubType, aSupType)
			);
		}

		if (aSupType.IsSet(out var SupChoice1, out var SupChoice2)) {
			return SubBaseType.IsSubType(SupChoice1, aTypeMappings).ElseTry(
				aError1 => SubBaseType.IsSubType(SupChoice2, aTypeMappings).ElseTry(
					aError2 => mResult.Fail(aError1 + "\n" + aError2)
				)
			);
		}

		if (aSupType.Kind is tKind.TypeVariable) {
			return Solve(aTypeMappings, aSupType, aSubType);
		}

		if (aSubType.Kind is tKind.TypeVariable) {
			return Solve(aTypeMappings, aSubType, aSupType);
		}
		
		if (
			aSupType.Kind is not tKind.Recursive &&
			SubBaseType.IsRecursive(out var Head, out var Body)
		) {
			return Body.Substitute(
				Head,
				Body
			).IsSubType(
				aSupType,
				aTypeMappings
			);
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
					}
					
					if (
						!SubField.IsSubType(SupField.Value, aTypeMappings).Match(
							out aTypeMappings,
							out var Error
						)
					) {
						return mResult.Fail(ExtendError(Error, aSubType, aSupType));
					}
				}
				return aTypeMappings;
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
			case tKind.Set: {
				mAssert.IsTrue(aSupType.IsSet(out var SupType1, out var SupType2));
				return SubBaseType.IsSubType(SupType1, aTypeMappings).ElseTry(
					aError1 => SubBaseType.IsSubType(SupType2, aTypeMappings).ElseTry(
						aError2 => mResult.Fail(aError1 + "\n" + aError2)
					)
				);
			}
			case tKind.Cond: {
				throw new System.NotImplementedException();
			}
			case tKind.Recursive: {
				mAssert.IsTrue(aSupType.IsRecursive(out var SupHead, out var SupBody));
				if (aSubType.IsRecursive(out var SubHead, out var SubBody)) {
					if (!mStd.RefEq(SubHead, SupHead)) {
						SubBody = SubBody.Substitute(SubHead, SupHead);
					}
					return SubBody.IsSubType(SupBody, aTypeMappings).ModifyError(
						__ => ExtendError(__, aSubType, aSupType)
					);
				} else {
					return aSubType.IsSubType(
						SupBody.Substitute(SupHead, aSupType),
						aTypeMappings
					).ModifyError(
						__ => ExtendError(__, aSubType, aSupType)
					);
				}
			}
			case tKind.Generic: {
				mAssert.IsTrue(aSupType.IsGeneric(out var SupHead, out var SupBody));
				if (aSubType.IsGeneric(out var SubHead, out var SubBody)) {
					if (!mStd.RefEq(SubHead, SupHead)) {
						SubBody = SubBody.Substitute(SubHead, SupHead);
					}
					return SubBody.IsSubType(SupBody, aTypeMappings).ModifyError(
						__ => ExtendError(__, aSubType, aSupType)
					);
				} else {
					return aSubType.IsSubType(SupBody, aTypeMappings).ModifyError(
						__ => ExtendError(__, aSubType, aSupType)
					);
				}
			}
			case tKind.Interface: {
				mAssert.IsTrue(aSupType.IsInterface(out var SupHead, out var SupBody));
				if (aSubType.IsInterface(out var SubHead, out var SubBody)) {
					if (!mStd.RefEq(SubHead, SupHead)) {
						SubBody = SubBody.Substitute(SubHead, SupHead);
					}
					return SubBody.IsSubType(SupBody, aTypeMappings).ModifyError(
						__ => ExtendError(__, aSubType, aSupType)
					);
				} else {
					return aSubType.IsSubType(SupBody, aTypeMappings).ModifyError(
						__ => ExtendError(__, aSubType, aSupType)
					);
				}
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
		if (aProc.IsGeneric(out var FreeType, out var InnerType)) {
			return Infer(InnerType, aObj, aArg, aTrace);
		}
		
		if (!aProc.IsProc(out var ObjType, out var ArgType, out var ResType)) {
			return mResult.Fail($"expect proc but is:\n{aProc.ToText()}");
		}
		
		// TODO:
		//if (aObj != ObjType) {
		//	return mResult.Fail($"{aObj.ToText()} != {ObjType.ToText()}");
		//}
		
		if (!aArg.IsSubType(ArgType, ArgType.InferenceVariables()).Match(out var TypeMappings, out var Error)) {
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
		}
		
		return ResType.ApplyMappings(TypeMappings);
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
		if (aType1.IsEmpty()) {
			return aType2;
		}
		
		if (aType2.IsEmpty()) {
			return aType1;
		}
		
		if (aType1 == aType2) {
			// TODO: special cases for UnionTypes
			return aType1;
		}
		
		return Set(aType1, aType2);
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
		if (aLimit is 0) {
			throw mError.Error(mStd.FileLine());
		}
		
		if (aType.IsSubType(aRemoved, mStd.cEmpty).Match(out _, out _)) {
			return mStd.cEmpty;
		} else if (aType.IsSet(out var Type1, out var Type2)) {
			return Union(Type1.Subtract(aRemoved, aLimit - 1), Type2.Subtract(aRemoved, aLimit - 1));
		} else if (aRemoved.IsSet(out Type1, out Type2)) {
			return aType.Subtract(Type1, aLimit - 1).ThenTry(__ => __.Subtract(Type2, aLimit - 1));
		} else if (aType.IsRecursive(out var Head1, out var Body1) && aRemoved.IsRecursive(out var Head2, out var Body2)) {
			if (mStd.RefEq(Head1, Head2)) {
				return Body1.Subtract(Body2, aLimit - 1).Then(__ => Recursive(Head1, __)).ElseUse(aType);
			} else {
				var H = TypeVariable();
				
				var B1 = Recursive(H, Body1.Substitute(Head1, H));
				var B2 = Recursive(H, Body2.Substitute(Head2, H));
				return B1.Subtract(B2, aLimit - 1);
			}
		} else if (aType.IsRecursive(out var Head, out var Body)) {
			return Body.Substitute(Head, aType).Subtract(aRemoved, aLimit - 1);
		} else if (aRemoved.IsRecursive(out Head, out Body)) {
			return aType.Subtract(Body.Substitute(Head, aRemoved), aLimit - 1);
		} else if (
			aType.IsPair(out var First, out var Second) &&
			aRemoved.IsPair(out var RemovedFirst, out var RemovedSecond)
		) {
			return Union(
				First.Subtract(RemovedFirst, aLimit - 1).Then(__ => Pair(__, Second)),
				Second.Subtract(RemovedSecond, aLimit - 1).Then(__ => Pair(First, __))
			);
		} else if (
			aType.IsPrefix(out var Prefix, out var Inner) &&
			aRemoved.IsPrefix(Prefix, out var RemovedInner)
		) {
			return Inner.Subtract(RemovedInner, aLimit - 1).Then(__ => mVM_Type.Prefix(Prefix, __));
		} else {
			return aType;
		}
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
