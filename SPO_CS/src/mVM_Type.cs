#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
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
		Free,
		Abstract,
		Opaque, // Closed identity of an opaque view; never attached to ordinary data.
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
	
	public delegate tType tTypeResolver(tType aType);
	private static readonly tType cAny = NewType(tKind.Any, []);
	private static readonly tType cEmpty = NewType(tKind.Empty, []);
	private static readonly tType cTrue = NewType(tKind.True, []);
	private static readonly tType cFalse = NewType(tKind.False, []);
	private static readonly tType cInt = NewType(tKind.Int, []);
	private static readonly tType cType = NewType(tKind.Type, []);
	private static readonly tType cBool = NewType(tKind.Set, [cTrue, cFalse]);
	
	public static tType Resolve(this tType aType, tTypeResolver? aTypes) => aTypes is null ? aType : aTypes(aType);
	public static tType Opaque(tType aIdentity) => aIdentity.Kind is tKind.Opaque ? aIdentity : aIdentity.OpaqueView ??= NewType(tKind.Opaque, [aIdentity]);
	public static tTypeResolver? WithWitness(this tTypeResolver? aTypes, tType aSymbol, tType aIdentity) {
		if (aSymbol.Kind is not (tKind.Free or tKind.Abstract or tKind.TypeApply)) { return aTypes; }
		return Bind(aTypes, aSymbol, aIdentity);
		static tTypeResolver Bind(tTypeResolver? aTypes, tType aSymbol, tType aIdentity) => aType => aType.Resolve(aTypes).Substitute(aSymbol, Opaque(aIdentity));
	}
	
	public static tTypeResolver? WithProof(this tTypeResolver? aTypes, tWitnessMap? aProof) {
		if (aProof is null) { return aTypes; }
		return Bind(aTypes, aProof);
		
		static tTypeResolver Bind(tTypeResolver? aTypes, tWitnessMap aProof) => aType => {
			aType = aType.Resolve(aTypes);
		
			for (var Proof = aProof; Proof is not null; Proof = Proof.Parent) {
				if (Proof.Opaque.Kind is not (tKind.Free or tKind.Abstract or tKind.TypeApply)) { continue; }
				if (Proof.Opaque.Kind is tKind.Abstract && !aType.AbstractParameters().Contains(Proof.Opaque.DebugId)) { continue; }
				if (Proof.Opaque.IsFree() && aType.FreeParameters().TryGet(Proof.Opaque.DebugId).IsNone()) { continue; }
				aType = aType.Substitute(Proof.Opaque, Opaque(Proof.Known.Resolve(aTypes)));
			}
		
			return aType;
		};
	}
	
	public sealed class
	tType {
		public tKind Kind { get; init; }
		public tText? Id { get; init; }
		public tText? Prefix { get; init; }
		public tType[] Refs { get; init; } = [];
		public mTreeMap.tTree<tText, tType> Fields { get; init; }
		public tBool IsImmutable { get; init; } // Every value of this type is transitively immutable.
		public tNat64 DebugId { get; } = mStd.NewDebugId();
		public tType? ErasedWitness;
		public tInt32? ShapeHash;
		public mMaybe.tMaybe<mTreeMap.tTree<tNat64, tType>> FreeVariables;
		public tBool? CodeContent;
		public System.Collections.Generic.HashSet<tNat64>? AbstractSymbols;
		public tType? OpaqueView;
		public System.Tuple<tType, tType, tType>? LastSubstitution;
		// Keep the type application cache bounded when programs create TYPE values.
		public System.Tuple<tType, tType>? LastApplication;
		public ConditionalWeakTable<tType, tType>? EqualTypes;
		public ConditionalWeakTable<tType, System.Tuple<tBool>>? MatchSubtypes;
		public ConditionalWeakTable<tType, System.Tuple<tBool>>? SchemaMatches;
		
		public override tBool
		Equals(
			tUnknown? a
		) => a is tType Type && this.SameType(Type);
		
		public override tInt32
		GetHashCode(
		) => this.TypeHash();
		
		public static tBool operator!=(
			tType? a1,
			tType? a2
		) => !(a1 == a2);
		
		public static tBool operator==(
			tType? a1,
			tType? a2
		) => a1.SameType(a2);
		
		public override tText
		ToString(
		) => this.ToText();
	}
	
	public static tBool CarriesCode(this tType aType) {
		if (aType.CodeContent is { } Previous) { return Previous; }
		
		var Result = aType.Kind switch {
			tKind.Proc or tKind.Generic or tKind.Any or tKind.Free or tKind.Abstract or tKind.Opaque or tKind.TypeApply or tKind.Sig or tKind.Interface => true,
			tKind.Recursive => aType.Refs[1].Substitute(aType.Refs[0], Empty()).CarriesCode(),
			tKind.Record => aType.Fields.ToStream().Any(__ => __.Value.CarriesCode()),
			tKind.Type => false,
			_ => mStream.Stream(aType.Refs).Any(CarriesCode),
		};
		
		aType.CodeContent = Result;
		return Result;
	}
	
	private static System.Collections.Generic.HashSet<tNat64> AbstractParameters(this tType aType) {
		if (aType.AbstractSymbols is { } Previous) { return Previous; }
		var Result = new System.Collections.Generic.HashSet<tNat64>();
		if (aType.Kind is tKind.Abstract) { Result.Add(aType.DebugId); }
		foreach (var Child in aType.Refs) { Result.UnionWith(Child.AbstractParameters()); }
		if (aType.IsRecord(out var Fields)) { foreach (var Field in Fields.ToStream()) { Result.UnionWith(Field.Value.AbstractParameters()); } }
		aType.AbstractSymbols = Result;
		return Result;
	}
	
	public static tBool HasAbstractSymbols(this tType aType) => aType.AbstractParameters().Count != 0;
	
	private static tInt32
	TypeHash(this tType aType) {
		if (aType.ShapeHash is { } Previous) { return Previous; }
		var Hash = System.HashCode.Combine(aType.Kind, aType.Prefix);
		
		if (aType.IsSet(out _, out _)) {
			var Members = new System.Collections.Generic.HashSet<tInt32>();
			
			void Add(tType aPart) {
				if (aPart.IsSet(out var Left, out var Right)) { Add(Left); Add(Right); }
				else { Members.Add(aPart.TypeHash()); }
			}
			
			Add(aType);
			foreach (var Member in Members) { Hash = unchecked(Hash + Member); }
		} else if (aType.IsRecord(out var Fields)) {
			foreach (var Field in Fields.ToStream()) { Hash = System.HashCode.Combine(Hash, Field.Key, Field.Value.TypeHash()); }
		} else {
			foreach (var Child in aType.Refs) { Hash = System.HashCode.Combine(Hash, Child.TypeHash()); }
		}
		
		aType.ShapeHash = Hash;
		return Hash;
	}
	
	public static readonly tText? cUnknownPrefix = null; // TODO
	
	private static tInt32 NextPlaceholderId = 1; // TODO: remove static var
	
	private static tType
	NewType(
		tKind aKind,
		tType[] aRefs,
		tText? aPrefix = null
	) => new() {
		Kind = aKind,
		Refs = aRefs,
		Prefix = aPrefix,
		IsImmutable = aKind switch {
			tKind.Empty or tKind.True or tKind.False or tKind.Int or tKind.Type or tKind.Proc => true,
			tKind.Pair or tKind.Prefix or tKind.Set => mStream.Stream(aRefs).All(__ => __.IsImmutable),
			tKind.Sig or tKind.Generic => aRefs[1].IsImmutable,
			tKind.Recursive => aRefs[1].Substitute(aRefs[0], Empty()).IsImmutable,
			_ => false,
		},
	};
	
	public static tType
	Substitute(
		this tType aType,
		tType aFree,
		tType aReplacement
	) {
		if (ReferenceEquals(aFree, aReplacement) || (aFree.IsFree() && aType.FreeParameters().TryGet(aFree.DebugId).IsNone()) || aFree.Kind is tKind.Abstract && !aType.AbstractParameters().Contains(aFree.DebugId)) { return aType; }
		if (aType.LastSubstitution is { } Previous && ReferenceEquals(Previous.Item1, aFree) && ReferenceEquals(Previous.Item2, aReplacement)) { return Previous.Item3; }
		var Result = ReplaceClosed(aType, aFree, aReplacement);
		aType.LastSubstitution = System.Tuple.Create(aFree, aReplacement, Result);
		return Result;
		
		static tType ReplaceClosed(tType aType, tType aFree, tType aReplacement) {
	
			var Cache = mTreeMap.Tree<tNat64, tType>((a1, a2) => a1.CompareTo(a2), []);
		
			tType Visit(tType aValue) {
				if (ReferenceEquals(aValue, aFree)) { return aReplacement; }
				if (Cache.TryGet(aValue.DebugId).IsSome(out var Previous)) { return Previous; }
				var Result = Replace(aValue);
				Cache = Cache.Set(aValue.DebugId, Result);
				return Result;
			}
		
			tType Replace(tType aType) {
				switch (aType.Kind) {
					case tKind.Abstract:
					case tKind.Free: {
						return ReferenceEquals(aType, aFree)
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
					case tKind.Opaque: {
						var Identity = Visit(aType.Refs[0]);
						return ReferenceEquals(Identity, aType.Refs[0]) ? aType : Opaque(Identity);
					}
					case tKind.Pair:
					case tKind.Prefix:
					case tKind.Proc:
					case tKind.Ref:
					case tKind.Var:
					case tKind.Set: {
						var Refs = mStream.Stream(aType.Refs).Map(
							__ => Visit(__)
						).ToArrayList().ToArray();
					
						return (
							mStream.ZipShort(mStream.Stream(Refs), mStream.Stream(aType.Refs)).All(__ => mStd.RefEq(__.Item1, __.Item2)) ? aType :
							aType.Kind is tKind.Set ? Set(Refs[0], Refs[1]) :
							NewType(aType.Kind, Refs, aType.Prefix)
						);
					}
					case tKind.TypeApply: {
						var Function = Visit(aType.Refs[0]);
						var Argument = Visit(aType.Refs[1]);
					
						return mStd.RefEq(Function, aType.Refs[0]) && mStd.RefEq(Argument, aType.Refs[1])
						? aType
						: Function.ApplyType(Argument);
					}
					case tKind.Record: {
						var Fields = aType.Fields.ToStream().Map(
							__ => (__.Key, Value: Visit(__.Value))
						).ToArrayList().ToArray();
					
						return mStream.Stream(Fields).All(
							__ => mStd.RefEq(__.Value, aType.Fields.TryGet(__.Key).AssertNotEmpty())
						) ? aType : Record(Fields);
					}
					case tKind.Sig:
					case tKind.Recursive:
					case tKind.Generic:
					case tKind.Interface: {
						if (mStd.RefEq(aType.Refs[0], aFree)) {
							return aType;
						}
					
						var Body = Visit(aType.Refs[1]);
					
						return (
							mStd.RefEq(Body, aType.Refs[1])
							? aType
							: NewType(aType.Kind, [aType.Refs[0], Body])
						);
					}
					default: {
						throw new System.NotImplementedException($"aType.Kind '{aType.Kind}'"); // TODO
					}
				}
			}
		
			return Visit(aType);
		}
	}
	
	public static tType
	ApplyMappings(
		this tType aType,
		mTreeMap.tTree<tType, tType> aMappings
	) {
		if (aMappings.Deep() == 0) { return aType; }
		var Parameters = aType.FreeParameters();
		if (Parameters.Deep() == 0) { return aType; }
		
		foreach (var (Free, Ref) in aMappings.ToStream()) {
			mAssert.IsTrue(Free.IsFree());
			if (ReferenceEquals(Free, Ref) || Parameters.TryGet(Free.DebugId).IsNone()) { continue; }
			var Replaced = aType.Substitute(Free, Ref);
			
			if (!mStd.RefEq(Replaced, aType)) {
				// Resolve only bindings used here; a cycle leaves its free type unresolved.
				var Resolved = Ref.ApplyMappings(aMappings.Set(Free, Free));
				aType = mStd.RefEq(Resolved, Ref) ? Replaced : aType.Substitute(Free, Resolved);
			}
		}
		
		return aType;
	}
	
	public static tType
	Free(
		tText aId,
		tType aKind
	) {
		mAssert.IsTrue(aKind.IsType() || aKind.IsTypeFunctionKind(), "invalid free parameter kind");
		
		return new() {
			Kind = tKind.Free,
			Id = aId,
			Refs = [aKind]
		};
	}
	
	// A fixed type-level value, e.g. a head obtained by opening a SIG.
	public static tType
	Abstract(
		tText aId,
		tType aKind
	) {
		mAssert.IsTrue(aKind.IsType() || aKind.IsTypeFunctionKind(), "invalid abstract type kind");
		return new() { Kind = tKind.Abstract, Id = aId, Refs = [aKind] };
	}
	
	public static tType
	Free(
		tType aKind
	) {
		var Id = "?"+NextPlaceholderId;
		NextPlaceholderId += 1;
		return Free(Id, aKind);
	}
	
	public static tBool
	IsFree(
		this tType aType
	) => aType.Kind is tKind.Free;
	
	public static tType
	Any(
	) => cAny;
	
	public static tBool
	IsAny(
		this tType aType
	) {
		return aType.Kind is tKind.Any;
	}
	
	public static tType
	Empty(
	) => cEmpty;
	
	public static tBool
	IsEmpty(
		this tType aType
	) {
		return aType.Kind is tKind.Empty;
	}
	
	public static tType
	True(
	) => cTrue;

	public static tType
	False(
	) => cFalse;

	public static tType
	Bool(
	) => cBool;
	
	public static tType
	Int(
	) => cInt;
	
	public static tBool
	IsInt(
		this tType aType
	) {
		return aType.Kind is tKind.Int;
	}
	
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
		Free("tText", Type()),
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
	) => cType;
	
	public static tBool
	IsType(
		this tType aType
	) => aType.Kind is tKind.Type;
	
	public static tType
	KindType(
		this tType aType
	) => aType.Kind switch {
		tKind.Free or tKind.Abstract => aType.Refs[0],
		tKind.Opaque => aType.Refs[0].KindType(),
		tKind.Generic => Proc(Empty(), aType.Refs[0].KindType(), aType.Refs[1].KindType()),
		tKind.TypeApply => aType.Refs[0].KindType().Refs[2],
		_ => Type(),
	};
	
	// A declared abstraction can describe a family of value signatures.
	// This does not change the function kind of the abstraction itself.
	public static tBool
	IsSignature(
		this tType aType
	) => (
		aType.Kind is tKind.Generic
		? aType.Refs[1].IsSignature()
		: aType.KindType().IsType()
	);
	
	public static tBool
	IsTypeFunctionKind(
		this tType aType
	) => (
		aType.IsProc(out var Obj, out var Arg, out var Res) && Obj.IsEmpty() &&
		(Arg.IsType() || Arg.IsTypeFunctionKind()) &&
		(Res.IsType() || Res.IsTypeFunctionKind())
	);
	
	public static tType
	ApplyType(
		this tType aConstructor,
		tType aArgument
	) {
		var Previous = aConstructor.LastApplication;
		if (Previous is not null && ReferenceEquals(Previous.Item1, aArgument)) { return Previous.Item2; }
		
		mAssert.IsTrue(
			aConstructor.KindType().IsProc(out var Obj, out var Arg, out _) &&
			Obj.IsEmpty() &&
			Arg.SameType(aArgument.KindType()),
			$"invalid type application: {aConstructor} to {aArgument}"
		);
		
		var Result = aConstructor.Kind is tKind.Generic ? aConstructor.Refs[1].Substitute(aConstructor.Refs[0], aArgument) :
		aConstructor.Kind is tKind.Opaque ? Opaque(aConstructor.Refs[0].ApplyType(aArgument)) : new() {
			Kind = tKind.TypeApply,
			Refs = [aConstructor, aArgument]
		};
		
		aConstructor.LastApplication = System.Tuple.Create(aArgument, Result);
		return Result;
	}
	
	public static tType
	Sig(
		tType aHead,
		tType aBody
	) {
		mAssert.IsTrue(aBody.IsSignature(), "SIG body must be a type or generic signature");
		return NewType(tKind.Sig, [aHead, aBody]);
	}
	
	public static tBool
	IsSig(
		this tType aType,
		out tType aHead,
		out tType aBody
	) {
		aHead = default!;
		aBody = default!;
		
		if (aType.Kind is not tKind.Sig) {
			return false;
		}
		
		aHead = aType.Refs[0];
		aBody = aType.Refs[1];
		return true;
	}
	
	// Equality of type values: binder names do not matter; free variables keep their identity.
	public static tBool
	SameType(
		this tType? aLeft,
		tType? aRight
	) {
		if (ReferenceEquals(aLeft, aRight)) { return true; }
		if (aLeft is null || aRight is null || aLeft.Kind != aRight.Kind || aLeft.Prefix != aRight.Prefix) { return false; }
		if (aLeft.Kind is tKind.Free or tKind.Abstract) { return false; }
		if (aLeft.Refs.Length == 0 && aLeft.Kind is not tKind.Record) { return true; }
		if (aLeft.EqualTypes is { } Known && Known.TryGetValue(aRight, out _)) { return true; }
		if (aLeft.TypeHash() != aRight.TypeHash()) { return false; }
		return CompareClosed(aLeft, aRight);
		
		static tBool CompareClosed(tType aLeft, tType aRight) {
			var Cache = new System.Collections.Generic.Dictionary<(tNat64, tNat64), tBool>();
		
			tBool Equal(tType? aLeft, tType? aRight) {
				if (ReferenceEquals(aLeft, aRight)) { return true; }
				if (aLeft is null || aRight is null) { return false; }
				if (aLeft.TypeHash() != aRight.TypeHash()) { return false; }
				var Key = (aLeft.DebugId, aRight.DebugId);
				if (Cache.TryGetValue(Key, out var Previous)) { return Previous; }
				var Result = Compare(aLeft, aRight);
				Cache.Add(Key, Result);
				return Result;
			}
		
			tBool Compare(tType aLeft, tType aRight) {
				if (ReferenceEquals(aLeft, aRight)) {
					return true;
				}
		
				if (
					aLeft is null ||
					aRight is null ||
					aLeft.Kind != aRight.Kind ||
					aLeft.Prefix != aRight.Prefix
				) {
					return false;
				}
		
				switch (aLeft.Kind) {
					case tKind.Free:
					case tKind.Abstract: {
						return false;
					}
					case tKind.Generic:
					case tKind.Recursive:
					case tKind.Interface:
					case tKind.Sig: {
						if (ReferenceEquals(aLeft.Refs[0], aRight.Refs[0])) {
							return Equal(aLeft.Refs[1], aRight.Refs[1]);
						}
				
						return (
							(
								aLeft.Refs[0].Kind is tKind.Free && aRight.Refs[0].Kind == aLeft.Refs[0].Kind
								? Equal(aLeft.Refs[0].KindType(), aRight.Refs[0].KindType())
								: Equal(aLeft.Refs[0], aRight.Refs[0])
							) &&
							Equal(aLeft.Refs[1].Substitute(aLeft.Refs[0], aRight.Refs[0]), aRight.Refs[1])
						);
					}
					case tKind.Record: {
						return (
							aLeft.Fields.ToStream().Count() == aRight.Fields.ToStream().Count() &&
							aLeft.Fields.ToStream().All(
								__ => aRight.Fields.TryGet(__.Key).Match(aValue => Equal(__.Value, aValue), () => false)
							)
						);
					}
					case tKind.Set: {
						static mStream.tStream<tType>
						Members(tType aType) => (
							aType.IsSet(out var A, out var B)
							? mStream.Concat(Members(A), Members(B))
							: mStream.Stream(aType)
						);
				
						var Left = Members(aLeft);
						var Right = Members(aRight);
				
						return (
							Left.All(aType => Right.Any(aOther => Equal(aType, aOther))) &&
							Right.All(aType => Left.Any(aOther => Equal(aType, aOther)))
						);
					}
					default: {
						return (
							aLeft.Refs.Length == aRight.Refs.Length &&
							mStream.ZipShort(
								mStream.Stream(aLeft.Refs),
								mStream.Stream(aRight.Refs)
							).All(
								__ => Equal(__.Item1, __.Item2)
							)
						);
					}
				}
			}
		
			var Result = Equal(aLeft, aRight);
		
			if (Result) {
				(aLeft.EqualTypes ??= new()).GetValue(aRight, static aType => aType);
				(aRight.EqualTypes ??= new()).GetValue(aLeft, static aType => aType);
			}
		
			return Result;
		}
	}
	
	public static tType
	Pair(
		tType aType1,
		tType aType2
	) => NewType(tKind.Pair, [aType1, aType2]);
	
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
			aType1 = default!;
			aType2 = default!;
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
	) => NewType(tKind.Prefix, [aType], mAssert.IsNotNull(aPrefix));
	
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
			aPrefix = default!;
			aTypeOut = default!;
			return false;
		}
	}
	
	public static tBool
	IsPrefix(
		this tType aType,
		tText aPrefix,
		[MaybeNullWhen(false)] out tType aTypeOut
	) {
		return aType.IsPrefix(out var Prefix, out aTypeOut!) && Prefix == aPrefix;
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
		
		mAssert.IsTrue(
			Fields.TryGet(Prefix).IsNone(),
			$"Field '{Prefix}' already exists in record [{Fields.ToStream().Map(__ => __.Key).Reduce("", (a1, a2) => a1 + "\n  " + a2)}]"
		);
		
		return new tType {
			Kind = tKind.Record,
			Fields = Fields.Set(Prefix, Type),
			IsImmutable = aTailType.IsImmutable && Type.IsImmutable
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
			aFields = default!;
			return false;
		}
	}
	
	public static tType
	Proc(
		tType aObjType,
		tType aArgType,
		tType aResType
	) => NewType(tKind.Proc, [aObjType, aArgType, aResType]);
	
	public static tBool
	IsProc(
		this tType aType,
		[MaybeNullWhen(false)] out tType aObjType,
		[MaybeNullWhen(false)] out tType aArgType,
		[MaybeNullWhen(false)] out tType aResType
	) {
		if (aType.IsRecursive(out _, out var Body)) {
			aType = Body;
		}
		
		if (aType.Kind is tKind.Proc) {
			aObjType = aType.Refs[0];
			aArgType = aType.Refs[1];
			aResType = aType.Refs[2];
			return true;
		} else {
			aObjType = default!;
			aArgType = default!;
			aResType = default!;
			return false;
		}
	}
	
	public static tType
	Prefix(
		tText aId
	) => new() {
		Kind = tKind.Prefix,
		Id = aId,
		Refs = [Empty()],
		IsImmutable = true,
	};
	
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
			aOutType = default!;
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
			aOutType = default!;
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
		return aType1.SameType(aType2) ? aType1 : NewType(tKind.Set, [aType1, aType2]);
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
			aType1 = default!;
			aType2 = default!;
			return false;
		}
	}
	
	public static tType
	Cond(
		tType aType
		// aCond
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
		// aCond
	) {
		mAssert.IsTrue(false); // TODO
		
		if (aType.Kind is tKind.Cond) {
			aSuperType = aType.Refs[0];
			return true;
		} else {
			aSuperType = default!;
			return false;
		}
	}
	
	public static tType
	Recursive(
		tType aTypeHead,
		tType aTypeBody
	) => NewType(tKind.Recursive, [aTypeHead, aTypeBody]);
	
	public static tBool
	IsRecursive(
		this tType aType,
		out tType aHeadType,
		out tType aBodyType
		// aCond
	) {
		if (aType.Kind is tKind.Recursive) {
			aHeadType = aType.Refs[0];
			mAssert.AreEquals(aHeadType.Kind, tKind.Free);
			aBodyType = aType.Refs[1];
			return true;
		} else {
			aHeadType = default!;
			aBodyType = default!;
			return false;
		}
	}
	
	public static tType
	Interface(
		tType aTypeHead,
		tType aTypeBody
	) => NewType(tKind.Interface, [aTypeHead, aTypeBody]);
	
	public static tBool
	IsInterfaceBody(tType aParameter, tType aBody) {
		static tInt32 Occurrences(tType aType, tType aParameter)
		=> mStd.RefEq(aType, aParameter) ? 1 : (
			aType.IsRecord(out var Fields)
			? Fields.ToStream().Reduce(0, (Count, Field) => Count + Occurrences(Field.Value, aParameter))
			: mStream.Stream(aType.Refs).Reduce(0, (Count, Child) => Count + Occurrences(Child, aParameter))
		);
		
		if (aBody.IsEmpty()) {
			return true;
		}
		
		return aBody.IsRecord(out var Methods) && Methods.ToStream().All(
			Field => {
				var Method = Field.Value;
				
				while (Method.IsGeneric(out _, out var Body)) {
					Method = Body;
				}
				
				return Method.IsProc(out var Object, out var Argument, out var Result) &&
				Object.FirstObjectType().SameType(aParameter) && Occurrences(Object, aParameter) == 1 &&
				Occurrences(Argument, aParameter) == 0 && Occurrences(Result, aParameter) == 0;
			}
		);
	}
	
	public static tType
	FirstObjectType(this tType aType) {
		while (aType.IsPair(out var Tail, out var Head)) {
			if (Tail.IsEmpty()) {
				return Head;
			}
			
			aType = Tail;
		}
		
		return aType;
	}
	
	public static mMaybe.tMaybe<tType>
	TryGetInterfaceMethod(this tType aType, tText aName)
	=> aType.IsInterface(out var Parameter, out var Body) && Body.IsRecord(out var Methods)
	? Methods.TryGet(aName).Then(__ => __.Substitute(Parameter, aType))
	: mStd.cEmpty;
	
	public static tBool
	ContainsInterface(
		this tType aType
	) => aType.TypeOrChildsMatch(__ => __.Kind is tKind.Interface);
	
	public static tBool
	HasUnboundParameter(
		this tType aType,
		mStream.tStream<tType> aBound = default
	) {
		foreach (var Parameter in aType.FreeParameters().ToStream()) {
			var Bound = false;
			foreach (var Symbol in aBound) { if (ReferenceEquals(Symbol, Parameter.Value)) { Bound = true; break; } }
			if (!Bound) { return true; }
		}
		
		return false;
	}
	
	public static mTreeMap.tTree<tNat64, tType>
	FreeParameters(this tType aType) {
		mTreeMap.tTree<tNat64, tType> Free(tType aCurrent) {
			if (aCurrent.FreeVariables.IsSome(out var Previous)) { return Previous; }
			var Result = mTreeMap.Tree<tNat64, tType>((a1, a2) => a1.CompareTo(a2), []);
			
			if (aCurrent.Kind is tKind.Free) { Result = Result.Set(aCurrent.DebugId, aCurrent); }
			else if (aCurrent.Kind is tKind.Recursive or tKind.Sig or tKind.Interface or tKind.Generic) {
				Result = Free(aCurrent.Refs[1]).Remove(aCurrent.Refs[0].DebugId);
			} else {
				var Children = aCurrent.IsRecord(out var Fields) ? Fields.ToStream().Map(__ => __.Value) : mStream.Stream(aCurrent.Refs);
				
				foreach (var Child in Children) {
					foreach (var Parameter in Free(Child).ToStream()) { Result = Result.Set(Parameter.Key, Parameter.Value); }
				}
			}
			
			aCurrent.FreeVariables = Result;
			return Result;
		}
		
		return Free(aType);
	}
	
	public static tBool
	TypeOrChildsMatch(
		this tType aType,
		mStd.tFunc<tType, tBool> aPredicate,
		mStream.tStream<tNat64> aVisited = default
	) {
		if (aPredicate(aType)) {
			return true;
		}
		
		if (aVisited.Any(__ => __ == aType.DebugId)) {
			return false;
		}
		
		aVisited = mStream.Stream(aType.DebugId, aVisited);
		
		return aType.Kind switch {
			tKind.Record
			=> aType.Fields.ToStream().Any(__ => __.Value.TypeOrChildsMatch(aPredicate, aVisited)),
			
			tKind.Pair or
			tKind.Prefix or
			tKind.Set or
			tKind.Proc
			=> mStream.Stream(aType.Refs).Any(__ => __.TypeOrChildsMatch(aPredicate, aVisited)),
			
			tKind.Recursive or
			tKind.Sig or
			tKind.Generic
			=> aType.Refs[1].TypeOrChildsMatch(aPredicate, aVisited),
			
			_
			=> false,
		};
	}
	
	public static mResult.tResult<tType, tText>
	CheckInterfaceImplementation(this tType aContract, tType aObject, tType aMethods) {
		if (!aContract.IsInterface(out var Head, out var Body)) {
			return mResult.Fail("expected interface contract");
		}
		
		var Parameter = Free(Head.KindType());
		
		return aMethods.IsSubType(Body.Substitute(Head, Parameter)).ThenTry(
			Mappings => {
				var Object = Parameter.ApplyMappings(Mappings);
				
				if (mStd.RefEq(Object, Parameter)) {
					Object = aObject;
				}
				
				return aMethods.IsSubType(Body.Substitute(Head, Object)).ThenTry(
					_ => aObject.IsSubType(Object)
				).Then(_ => aContract);
			}
		);
	}
	
	public static tBool
	IsInterface(
		this tType aType,
		[MaybeNullWhen(false)] out tType aHeadType,
		[MaybeNullWhen(false)] out tType aBodyType
		// aCond
	) {
		if (aType.Kind is tKind.Interface) {
			aHeadType = aType.Refs[0];
			aBodyType = aType.Refs[1];
			return true;
		} else {
			aHeadType = default!;
			aBodyType = default!;
			return false;
		}
	}
	
	public static tType
	Generic(
		tType aTypeHead,
		tType aTypeBody
	) {
		mAssert.IsTrue(aTypeHead.Kind is tKind.Free, "ALL requires a free type parameter");
		return NewType(tKind.Generic, [aTypeHead, aTypeBody]);
	}
	
	public static tBool
	IsGeneric(
		this tType aType,
		[MaybeNullWhen(false)] out tType aHeadType,
		[MaybeNullWhen(false)] out tType aBodyType
		// aCond
	) {
		if (aType.Kind is tKind.Generic ) {
			aHeadType = aType.Refs[0];
			aBodyType = aType.Refs[1];
			return true;
		} else {
			aHeadType = default!;
			aBodyType = default!;
			return false;
		}
	}
	
	public static mResult.tResult<mTreeMap.tTree<tType, tType>, tText>
	IsSubType(
		this tType aSubType,
		tType aSupType,
		mTreeMap.tTree<tType, tType> aTypeMappings = default,
		tBool aDescribeError = true
	) {
		var Result = aSubType.CheckSubType(aSupType, aTypeMappings);
		
		return aDescribeError ? Result.ModifyError(
			aError => (
				(tText.IsNullOrWhiteSpace(aError) ? "" : aError + "\n") +
				"found:\n  " + aSubType.ToText("\n  ", aSupType) +
				"\nexpected:\n  " + aSupType.ToText("\n  ", aSubType)
			)
		) : Result;
	}
	
	private static mResult.tResult<mTreeMap.tTree<tType, tType>, tText>
	CheckSubType(
		this tType aSubType,
		tType aSupType,
		mTreeMap.tTree<tType, tType> aTypeMappings,
		mStream.tStream<(tNat64 Source, tNat64 Target, tInt32 Depth)> aAssumed = default,
		tInt32 aDepth = 0
	) {
		if (aTypeMappings.Deep() == 0) {
			aTypeMappings = mTreeMap.Tree<tType, tType>((A, B) => A.DebugId.CompareTo(B.DebugId), []);
		}

		static mResult.tResult<mTreeMap.tTree<tType, tType>, tText>
		MapFree(
			mTreeMap.tTree<tType, tType> aTypeMappings,
			tType aFree,
			tType aRef,
			tBool aUpperBound = false
		) {
			aRef = aRef.ApplyMappings(aTypeMappings);
			var Aliases = new System.Collections.Generic.HashSet<tNat64>();
			
			while (aTypeMappings.TryGet(aFree).IsSome(out var Alias) && Alias.Kind is tKind.Free && !ReferenceEquals(Alias, aFree)) {
				if (!Aliases.Add(aFree.DebugId)) { aTypeMappings = aTypeMappings.Remove(aFree); break; }
				aFree = Alias;
			}
			
			if (Aliases.Count != 0) { aRef = aRef.ApplyMappings(aTypeMappings); }
			
			if (mStd.RefEq(aFree, aRef)) {
				return aTypeMappings;
			}
			
			if (aRef.FreeParameters().TryGet(aFree.DebugId).IsSome(out _)) {
				static tBool Includes(tType aSet, tType aMember) => aSet.SameType(aMember) || aSet.IsSet(out var Left, out var Right) && (Includes(Left, aMember) || Includes(Right, aMember));
				if (aUpperBound && Includes(aRef, aFree)) { return aTypeMappings; }
				return mResult.Fail("cyclic type binding for " + aFree.Id);
			}
			
			if (aTypeMappings.TryGet(aFree).IsSome(out var Previous)) {
				if (Previous.Kind is tKind.Free) {
					return aTypeMappings.Set(aFree, aRef);
				}
				
				if (!aFree.KindType().IsType() && !aRef.SameType(Previous)) {
					return mResult.Fail("different type function bindings");
				}
				
				aRef = aFree.KindType().IsType() ? Set(aRef, Previous) : Previous;
			}
			
			return aTypeMappings.Set(aFree, aRef);
		}
		
		if (aSubType.SameType(aSupType)) {
			return aTypeMappings;
		}
		
		if (aSupType.IsSet(out _, out _)) {
			static tBool Includes(tType aSet, tType aMember) => aSet.SameType(aMember) || aSet.IsSet(out var Left, out var Right) && (Includes(Left, aMember) || Includes(Right, aMember));
			if (Includes(aSupType, aSubType)) { return aTypeMappings; }
		}
		
		if (aSupType.Kind is tKind.Free) {
			return aSubType.KindType().SameType(aSupType.KindType())
			? MapFree(aTypeMappings, aSupType, aSubType)
			: mResult.Fail("different free parameter kinds");
		}
		
		if (aSubType.Kind is tKind.Free) {
			if (aSupType.IsSet(out _, out _)) {
				static mStream.tStream<tType> Parameters(tType aType) => aType.IsSet(out var Left, out var Right) ? mStream.Concat(Parameters(Left), Parameters(Right)) : aType.IsFree() ? mStream.Stream(aType) : default;
				
				foreach (var Parameter in Parameters(aSupType)) {
					if (aSubType.CheckSubType(Parameter, aTypeMappings, aAssumed, aDepth).Match(out var Bound, out _)) { return Bound; }
				}
			}
			
			return aSupType.KindType().SameType(aSubType.KindType())
			? MapFree(aTypeMappings, aSubType, aSupType, true)
			: mResult.Fail("different free parameter kinds");
		}
		
		if (!aSubType.IsSignature() || !aSupType.IsSignature()) {
			return mResult.Fail("comparison requires types or declared generic signatures");
		}
		
		var SubBaseType = aSubType.BaseType();
		
		if (SubBaseType.Kind is tKind.Recursive || aSupType.Kind is tKind.Recursive) {
			if (aAssumed.Where(__ => __.Source == SubBaseType.DebugId && __.Target == aSupType.DebugId).TryFirst().IsSome(out var Previous)) {
				return aDepth > Previous.Depth ? aTypeMappings : mResult.Fail("unguarded recursive type comparison");
			}
			
			aAssumed = mStream.Stream((SubBaseType.DebugId, aSupType.DebugId, aDepth), aAssumed);
		}
		
		if (SubBaseType.Kind == aSupType.Kind && SubBaseType.Kind is tKind.Pair or tKind.Prefix or tKind.Record or tKind.Proc or tKind.Var or tKind.Ref or tKind.Sig or tKind.Interface) { aDepth += 1; }
		
		if (SubBaseType.IsSet(out var SubType1, out var SubType2)) {
			return SubType1.CheckSubType(aSupType, aTypeMappings, aAssumed, aDepth).ThenTry(
				__ => SubType2.CheckSubType(aSupType, __, aAssumed, aDepth)
			);
		}
		
		if (
			aSupType.Kind is not tKind.Recursive &&
			SubBaseType.IsRecursive(out var Head, out var Body)
		) {
			return Body.Substitute(
				Head,
				aSubType
			).CheckSubType(
				aSupType,
				aTypeMappings,
				aAssumed,
				aDepth
			);
		}
		
		// TODO: implement
		switch (aSupType.Kind) {
			case tKind.Free: {
				throw new System.NotImplementedException();
			}
			case tKind.Any: {
				return aTypeMappings;
			}
			case tKind.Empty:
			case tKind.True:
			case tKind.False:
			case tKind.Int:
			case tKind.Type: {
				return SubBaseType.Kind == aSupType.Kind
				? aTypeMappings
				: mResult.Fail("");
			}
			case tKind.Abstract: {
				return mResult.Fail("different bound types");
			}
			case tKind.Opaque: {
				return mResult.Fail("different opaque views");
			}
			case tKind.TypeApply: {
				if (SubBaseType.Kind is not tKind.TypeApply) {
					return mResult.Fail("different type constructors");
				}
				
				static mResult.tResult<mTreeMap.tTree<tType, tType>, tText>
				MatchApplication(
					tType aLeft,
					tType aRight,
					mTreeMap.tTree<tType, tType> aMappings,
					mStream.tStream<(tNat64 Source, tNat64 Target, tInt32 Depth)> aAssumed,
					tInt32 aDepth
				) {
					if (aLeft.SameType(aRight)) {
						return aMappings;
					}
					
					if (aLeft.Kind is tKind.Free || aRight.Kind is tKind.Free) {
						return aLeft.CheckSubType(aRight, aMappings, aAssumed, aDepth);
					}
					
					if (aLeft.Kind is not tKind.TypeApply || aRight.Kind is not tKind.TypeApply) {
						return mResult.Fail("different type function bindings");
					}
					
					return MatchApplication(aLeft.Refs[0], aRight.Refs[0], aMappings, aAssumed, aDepth).ThenTry(
						__ => aLeft.Refs[1].ApplyMappings(__).CheckSubType(aRight.Refs[1].ApplyMappings(__), __, aAssumed, aDepth)
					).ThenTry(
						__ => aRight.Refs[1].ApplyMappings(__).CheckSubType(aLeft.Refs[1].ApplyMappings(__), __, aAssumed, aDepth)
					);
				}
				
				return MatchApplication(SubBaseType, aSupType, aTypeMappings, aAssumed, aDepth);
			}
			case tKind.Sig: {
				if (!SubBaseType.IsSig(out var SubHead, out var SubBody)) {
					return mResult.Fail("expected SIG");
				}
				
				var SupHead = aSupType.Refs[0];
				
				if (!SubHead.KindType().SameType(SupHead.KindType())) {
					return mResult.Fail("different SIG head kinds");
				}
				
				if (SupHead.Kind is not tKind.Free && !SubHead.SameType(SupHead)) {
					return mResult.Fail("different SIG heads");
				}
				
				var Witness = SubHead.Kind is tKind.Free
				? Abstract(SubHead.Id!, SubHead.KindType())
				: SubHead;
				
				return SubBody.Substitute(SubHead, Witness).CheckSubType(
					aSupType.Refs[1].Substitute(SupHead, Witness),
					aTypeMappings,
					aAssumed,
					aDepth
				);
			}
			case tKind.Pair: {
				var TailSubType = SubBaseType;
				var TailSupType = aSupType;
				var Error = "";
				
				if (
					TailSubType.IsPair(out TailSubType, out var HeadSubType) &&
					TailSupType.IsPair(out TailSupType, out var HeadSupType) &&
					TailSubType.CheckSubType(TailSupType, aTypeMappings, aAssumed, aDepth).Match(out aTypeMappings, out Error) &&
					HeadSubType.CheckSubType(HeadSupType, aTypeMappings, aAssumed, aDepth).Match(out aTypeMappings, out Error)
				) {
					return aTypeMappings;
				} else {
					return mResult.Fail(Error);
				}
			}
			case tKind.Prefix: {
				if (
					SubBaseType.IsPrefix(out var SubPrefix, out var Sub) &&
					aSupType.IsPrefix(out var SupPrefix, out var Sup) &&
					SubPrefix == SupPrefix
				) {
					return Sub.CheckSubType(
						Sup,
						aTypeMappings,
						aAssumed,
						aDepth
					);
				} else {
					return mResult.Fail("");
				}
			}
			case tKind.Record: {
				if (!aSupType.IsRecord(out var SupFields)) {
					return mResult.Fail("expected a record");
				}
				
				if (!aSubType.IsRecord(out var SubFields)) {
					return mResult.Fail("expected a record");
				}
				
				foreach (var SupField in SupFields.ToStream()) {
					if (!SubFields.TryGet(SupField.Key).IsSome(out var SubField)) {
						return mResult.Fail($"missing field '{SupField.Key}'");
					}
					
					if (
						!SubField.CheckSubType(SupField.Value, aTypeMappings, aAssumed, aDepth).Match(
							out aTypeMappings,
							out var Error
						)
					) {
						return mResult.Fail(Error);
					}
				}
				
				return aTypeMappings;
			}
			case tKind.Proc: {
				if (
					!aSubType.IsProc(out var SubObj, out var SubArg, out var SubRes) ||
					!aSupType.IsProc(out var SupObj, out var SupArg, out var SupRes)
				) {
					return mResult.Fail("expected a procedure");
				}
				
				return SubObj.CheckSubType(SupObj, aTypeMappings, aAssumed, aDepth)
				.ThenTry(__ => SupObj.CheckSubType(SubObj, __, aAssumed, aDepth))
				.ThenTry(__ => SupArg.CheckSubType(SubArg, __, aAssumed, aDepth))
				.ThenTry(__ => SubRes.ApplyMappings(__).CheckSubType(SupRes.ApplyMappings(__), __, aAssumed, aDepth));
			}
			case tKind.Var: {
				if (!SubBaseType.IsVar(out var SubValue)) {
					return mResult.Fail("expected VAR");
				}
				
				var SupValue = aSupType.Refs[0];
				
				return SubValue.CheckSubType(SupValue, aTypeMappings, aAssumed, aDepth).ThenTry(
					__ => SupValue.ApplyMappings(
						__
					).CheckSubType(
						SubValue.ApplyMappings(__),
						__,
						aAssumed,
						aDepth
					)
				);
			}
			case tKind.Ref: {
				throw new System.NotImplementedException();
			}
			case tKind.Set: {
				mAssert.IsTrue(aSupType.IsSet(out var SupType1, out var SupType2));
				
				return SubBaseType.CheckSubType(SupType1, aTypeMappings, aAssumed, aDepth).ElseTry(
					aError1 => SubBaseType.CheckSubType(SupType2, aTypeMappings, aAssumed, aDepth).ElseTry(
						aError2 => mResult.Fail(aError1 == aError2 ? aError1 : aError1 + "\n" + aError2)
					)
				);
			}
			case tKind.Cond: {
				throw new System.NotImplementedException();
			}
			case tKind.Recursive: {
				mAssert.IsTrue(aSupType.IsRecursive(out var SupHead, out var SupBody));
				return aSubType.CheckSubType(SupBody.Substitute(SupHead, aSupType), aTypeMappings, aAssumed, aDepth);
			}
			case tKind.Generic: {
				var Source = aSubType;
				var Target = aSupType;
				var Local = new System.Collections.Generic.HashSet<tNat64>();
				var Rigid = new System.Collections.Generic.HashSet<tNat64>();
				
				// Infer an instance of the implementation against arbitrary contract
				// parameters. Collect the whole prefix so binder order is irrelevant.
				while (Source.IsGeneric(out var Parameter, out var SourceBody)) {
					var Fresh = Free(Parameter.Id!, Parameter.KindType());
					Local.Add(Fresh.DebugId);
					Source = SourceBody.Substitute(Parameter, Fresh);
				}
				
				while (Target.IsGeneric(out var Parameter, out var TargetBody)) {
					var Witness = Abstract(Parameter.Id!, Parameter.KindType());
					Rigid.Add(Witness.DebugId);
					Target = TargetBody.Substitute(Parameter, Witness);
				}
				
				if (!Source.CheckSubType(Target, aTypeMappings, aAssumed, aDepth).Match(out var Found, out var Error)) { return mResult.Fail(Error); }
				if (!Source.ApplyMappings(Found).CheckSubType(Target.ApplyMappings(Found), default).Match(out _, out Error)) { return mResult.Fail(Error); }
				var Result = aTypeMappings;
				
				foreach (var Mapping in Found.ToStream()) {
					if (Local.Contains(Mapping.Key.DebugId)) { continue; }
					var Value = Mapping.Value.ApplyMappings(Found);
					if (Value.TypeOrChildsMatch(__ => Rigid.Contains(__.DebugId))) { return mResult.Fail("generic parameter escapes its signature through " + Mapping.Key.Id + ": " + Value.ToText()); }
					Result = Result.Set(Mapping.Key, Value);
				}
				
				return Result;
			}
			case tKind.Interface: {
				mAssert.IsTrue(aSupType.IsInterface(out var SupHead, out var SupBody));
				
				if (!aSubType.IsInterface(out var SubHead, out var SubBody)) {
					return mResult.Fail("expected interface object");
				}
				
				var Object = Abstract(SupHead.Id!, SupHead.KindType());
				return SubBody.Substitute(SubHead, Object).CheckSubType(SupBody.Substitute(SupHead, Object), aTypeMappings, aAssumed, aDepth);
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
			
			return ReferenceEquals(Expanded, aType)
			? mStd.cEmpty
			: Expanded.SubSet(aSelect);
		}
		
		if (aType.IsSet(out var Type1, out var Type2)) {
			return Union(Type1.SubSet(aSelect), Type2.SubSet(aSelect));
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
	Infer(
		tType aProc,
		tType aObj,
		tType aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) => InferCall(aProc, aObj, aArg, aTrace).Then(__ => __.Result);

	public static mResult.tResult<(tType Object, tType Argument, tType Result), tText>
	InferCall(
		tType aProc,
		tType aObj,
		tType aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) => BindCall(aProc, aObj, aArg, aTrace).Then(__ => (__.Object, __.Argument, __.Result));
	
	public static mResult.tResult<(tType Object, tType Argument, tType Result, mTreeMap.tTree<tType, tType> Mappings), tText>
	BindCall(
		tType aProc,
		tType aObj,
		tType aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace,
		tBool aDescribeError = true
	) {
		if (aProc.IsGeneric(out var FreeType, out var InnerType)) {
			var Parameter = Free(FreeType.Id!, FreeType.KindType());
			return BindCall(InnerType.Substitute(FreeType, Parameter), aObj, aArg, aTrace, aDescribeError);
		}
		
		if (!aProc.IsProc(out var ObjType, out var ArgType, out var ResType)) {
			return mResult.Fail($"expect proc but is:\n{aProc.ToText()}");
		}
		
		if (!aObj.IsSubType(ObjType, aDescribeError: aDescribeError).Match(out var TypeMappings, out var Error)) {
			return mResult.Fail("can't convert receiver:\n" + Error);
		}
		
		if (!aArg.IsSubType(ArgType, TypeMappings, aDescribeError).Match(out TypeMappings, out Error)) {
			return mResult.Fail("can't convert argument:\n" + Error);
		}
		
		// Later operands may refine an earlier binding; validate the instantiated contract.
		if (!aObj.ApplyMappings(TypeMappings).IsSubType(ObjType.ApplyMappings(TypeMappings), aDescribeError: aDescribeError).Match(out _, out Error)) {
			return mResult.Fail("can't convert receiver:\n" + Error);
		}
		
		if (!aArg.ApplyMappings(TypeMappings).IsSubType(ArgType.ApplyMappings(TypeMappings), aDescribeError: aDescribeError).Match(out _, out Error)) {
			return mResult.Fail("can't convert argument:\n" + Error);
		}
		
		return (ObjType.ApplyMappings(TypeMappings), ArgType.ApplyMappings(TypeMappings), ResType.ApplyMappings(TypeMappings), TypeMappings);
	}
	
	public static (mMaybe.tMaybe<tType> Matched, mMaybe.tMaybe<tType> Remainder)
	SplitBy(
		this tType aType,
		mStd.tFunc<tType, tBool> aIsMatching
	) {
		if (aType.IsRecursive(out var Head, out var Body)) {
			var Expanded = Body.Substitute(Head, aType);
			
			return ReferenceEquals(Expanded, aType)
			? (mStd.cEmpty, mMaybe.Some(aType))
			: Expanded.SplitBy(aIsMatching);
		}
		
		if (aType.IsSet(out var Type1, out var Type2)) {
			var (Matched1, Remainder1) = Type1.SplitBy(aIsMatching);
			var (Matched2, Remainder2) = Type2.SplitBy(aIsMatching);
			
			return (
				Union(Matched1, Matched2),
				Union(Remainder1, Remainder2)
			);
		}
		
		return aIsMatching(aType)
		? (aType, mStd.cEmpty)
		: (mStd.cEmpty, aType);
	}
	
	public static mMaybe.tMaybe<tType>
	Union(
		mMaybe.tMaybe<tType> aType1,
		mMaybe.tMaybe<tType> aType2
	) => (
		!aType1.IsSome(out var Type1) ? aType2 :
		!aType2.IsSome(out var Type2) ? aType1 :
		mMaybe.Some(Set(Type1, Type2))
	);
	
	public static mMaybe.tMaybe<tType>
	Subtract(
		this tType aType,
		tType aRemoved
	) {
		if (aType.IsSubType(aRemoved).Match(out _, out _)) {
			return mStd.cEmpty;
		} else if (aType.IsSet(out var Type1, out var Type2)) {
			return Union(Type1.Subtract(aRemoved), Type2.Subtract(aRemoved));
		} else if (aRemoved.IsSet(out Type1, out Type2)) {
			return aType.Subtract(Type1).ThenTry(__ => __.Subtract(Type2));
		} else if (aType.IsRecursive(out var Head, out var Body)) {
			return Body.Substitute(Head, aType).Subtract(aRemoved);
		} else if (aRemoved.IsRecursive(out Head, out Body)) {
			return aType.Subtract(Body.Substitute(Head, aRemoved));
		} else if (
			aType.IsPair(out var First, out var Second) &&
			aRemoved.IsPair(out var RemovedFirst, out var RemovedSecond)
		) {
			return Union(
				First.Subtract(RemovedFirst).Then(__ => Pair(__, Second)),
				Second.Subtract(RemovedSecond).Then(__ => Pair(RemovedFirst, __))
			);
		} else if (
			aType.IsPrefix(out var Prefix, out var Inner) &&
			aRemoved.IsPrefix(Prefix, out var RemovedInner)
		) {
			return Inner.Subtract(RemovedInner).Then(__ => mVM_Type.Prefix(Prefix, __));
		} else {
			return aType;
		}
	}
	
	public static tText
	ToText(
		this tType aType,
		tText aIndent = "\n"
	) => aType.ToText(aIndent, null);
	
	private static tText
	ToText(
		this tType aType,
		tText aIndent,
		tType? aComparedType
	) {
		if (aComparedType is not null && aType.SameType(aComparedType)) {
			return "...";
		}
		
		if (aComparedType?.Kind != aType.Kind) {
			aComparedType = null;
		}
		
		var ComparedRefs = aComparedType?.Refs;
		
		if (
			aComparedType is not null &&
			aType.Kind is tKind.Generic or tKind.Recursive or tKind.Sig or tKind.Interface &&
			aType.Refs[0].Kind is tKind.Free && ComparedRefs![0].Kind is tKind.Free &&
			aType.Refs[0].KindType().SameType(ComparedRefs[0].KindType())
		) {
			ComparedRefs = [aType.Refs[0], ComparedRefs[1].Substitute(ComparedRefs[0], aType.Refs[0])];
		}
		
		tText __;
		tText ____;
		
		if (aIndent.Length is 0 || aIndent[0] is not '\n') {
			__ = " ";
			____ = " ";
		} else {
			var OneLiner = aType.ToText("", aComparedType);
			
			if (OneLiner.Length <= 80) {
				return OneLiner;
			}
			
			__ = aIndent;
			____ = __ + "  ";
		}
		
		tText Child(tInt32 aIndex) => aType.Refs[aIndex].ToText(____, ComparedRefs?[aIndex]);
		
		return aType.Kind switch {
			tKind.Empty => "[]",
			tKind.True => "§TRUE",
			tKind.False => "§FALSE",
			tKind.Int => "§INT",
			tKind.Any => "§ANY",
			tKind.Type => "§TYPE",
			tKind.Free => "?" + aType.Id,
			tKind.Abstract => "^" + aType.Id,
			tKind.Opaque => "[§OPAQUE " + Child(0) + "]",
			tKind.TypeApply => $"[.{Child(0)} {Child(1)}]",
			tKind.Sig => $"[§SIG_WITH {Child(0)} € " +
			aType.Refs[0].KindType().ToText(____, ComparedRefs?[0].KindType()) + $" IN {Child(1)}]",
			tKind.Prefix => $"[{____}#{aType.Prefix} {Child(0)}{__}]",
			tKind.Record => mStd.Call(
				() => {
					var Text = "";
					var Type = aType;
					mAssert.IsTrue(Type.IsRecord(out var Fields));
					var Omitted = false;
					
					foreach (var Field in Fields.ToStream()) {
						var ComparedField = (
							aComparedType is not null && aComparedType.Fields.TryGet(Field.Key).IsSome(out var Other)
							? Other
							: null
						);
							
						if (ComparedField is not null && Field.Value.SameType(ComparedField)) {
							if (!Omitted) {
								Text += ____ + "..., ";
							}
							
							Omitted = true;
							continue;
						}
						
						Omitted = false;
						Text += ____ + $"{Field.Key} : {Field.Value.ToText(____, ComparedField)}";
						Text += ", ";
					}
					
					return "[{" + (aComparedType is null ? Text : Text.TrimEnd(' ', ',')) + __ + "}]";
				}
			),
			tKind.Pair => mStd.Call(
				() => {
					if (aComparedType is not null) {
						static (tType Tail, mStream.tStream<tType> Items)
						TupleParts(
							tType aPair
						) {
							var Items = mStream.Stream<tType>();
							
							while (aPair.Kind is tKind.Pair) {
								Items = mStream.Stream(aPair.Refs[1], Items);
								aPair = aPair.Refs[0];
							}
							
							return (aPair, Items);
						}
						
						var Own = TupleParts(aType);
						var Compared = TupleParts(aComparedType);
						
						if (
							Own.Tail.IsEmpty() && Compared.Tail.IsEmpty() &&
							Own.Items.Count() > 1 && Compared.Items.Count() > 1
						) {
							var Items = Own.Items.MapWithIndex(
								(aIndex, aItem) => aItem.ToText(
									____,
									Compared.Items.Skip(aIndex).TryFirst().IsSome(out var Other) ? Other : null
								)
							);
							
							return "[" + ____ + Items.Join((a1, a2) => a1 + "," + ____ + a2, "") + __ + "]";
						}
					}
					
					var Result = Child(1);
					
					var Temp = aType.Refs[0];
					var ComparedTail = ComparedRefs?[0];
					
					if (Temp.Kind is tKind.Empty) {
						Result = Temp.ToText(____, ComparedTail) + ";" + ____ + Result;
					} else {
						while (Temp.Kind is tKind.Pair) {
							Result = Temp.Refs[1].ToText(
								____,
								ComparedTail?.Kind is tKind.Pair ? ComparedTail.Refs[1] : null
							) + "," + ____ + Result;
							
							Temp = Temp.Refs[0];
							ComparedTail = ComparedTail?.Kind is tKind.Pair ? ComparedTail.Refs[0] : null;
						}
						
						if (Temp.Kind is not tKind.Empty) {
							Result = Temp.ToText(____, ComparedTail) + ";" + ____ + Result;
						}
					}
					
					return "[" + ____ + Result + __ + "]";
				}
			),
			tKind.Proc => mStd.Call(
				() => {
					var Result = "";
					
					if (
						aType.Refs[0].Kind is not tKind.Empty ||
						ComparedRefs?[0].Kind is not null and not tKind.Empty
					) {
						Result += ____ + Child(0) + " :";
					}
					
					if (
						aType.Refs[1].Kind is not tKind.Empty ||
						ComparedRefs?[1].Kind is not null and not tKind.Empty
					) {
						Result += ____ + Child(1);
					}
					
					if (
						aType.Refs[2].Kind is not tKind.Empty ||
						ComparedRefs?[2].Kind is not null and not tKind.Empty
					) {
						Result += ____ + "-> " + Child(2);
					}
					
					return "[" + Result + __ + "]";
				}
			),
			tKind.Ref => $"[{____}§REF {Child(0)}{__}]",
			tKind.Set => mStd.Call(
				() => {
					static mStream.tStream<tType>
					Members(
						tType aMember
					) => (
						aMember.IsSet(out var Left, out var Right)
						? mStream.Concat(Members(Left), Members(Right)) : mStream.Stream(aMember)
					);
					
					var Own = aComparedType is null ? mStream.Stream(aType.Refs) : Members(aType);
					var Compared = aComparedType is null ? mStream.Stream<tType>() : Members(aComparedType);
					var Omitted = false;
					var Parts = mStream.Stream<tText>();
					
					foreach (var Member in Own) {
						if (Compared.Any(Member.SameType)) {
							if (!Omitted) {
								Parts = mStream.Stream("...", Parts);
								Omitted = true;
							}
							
							continue;
						}
						
						var Candidates = Compared.Where(
							__ => (
								__.Kind == Member.Kind &&
								__.Prefix == Member.Prefix &&
								!Own.Any(__.SameType)
							)
						).Take(
							2
						);
						
						var OwnCandidates = Own.Where(
							__ => (
								__.Kind == Member.Kind &&
								__.Prefix == Member.Prefix &&
								!Compared.Any(__.SameType)
							)
						).Take(
							2
						);
						
						Parts = mStream.Stream(
							Member.ToText(
								____,
								Candidates.Count() == 1 && OwnCandidates.Count() == 1
								? Candidates.TryFirst().AssertNotEmpty()
								: null
							),
							Parts
						);
					}
					
					return "[" + ____ + Parts.Reverse().Join((a1, a2) => a1 + " |" + ____ + a2, "") + __ + "]";
				}
			),
			tKind.Var => $"[{____}§VAR {Child(0)}{__}]",
			tKind.Recursive => $"[{____}§RECURSIVE {Child(0)} = {Child(1)}{__}]",
			tKind.Generic => $"[{____}§ALL {Child(0)} => {Child(1)}{__}]",
			tKind.Interface => $"[{____}§INTERFACE {Child(0)} {Child(1)}{__}]",
			tKind.Cond => $"[{____ + Child(0)} ? ...{__}]", // TODO
			_ => throw mError.Error("impossible")
		};
	}
	public sealed class tWitnessMap(tType aOpaque, tType aKnown, tBool aSeal, tWitnessMap? aParent = null) {
		public readonly tType Opaque = aOpaque;
		public readonly tType Known = aKnown;
		public readonly tBool Seal = aSeal;
		public readonly tWitnessMap? Parent = aParent;
	}
	
	public static tType ErasedHead(this tType aHead) => aHead.IsFree() ? aHead.ErasedWitness ??= Abstract("sealed", aHead.KindType()) : aHead;
	
	public static tBool SameWitnesses(this tWitnessMap? aLeft, tWitnessMap? aRight) {
		while (aLeft is not null && aRight is not null) {
			if (!ReferenceEquals(aLeft.Opaque, aRight.Opaque) || !aLeft.Known.SameType(aRight.Known) || aLeft.Seal != aRight.Seal) { return false; }
			aLeft = aLeft.Parent;
			aRight = aRight.Parent;
		}
		
		return aLeft is null && aRight is null;
	}
	
	public static tType ResolveWitnesses(this tType aType, tWitnessMap? aProof, tBool aSealing) {
		for (; aProof is not null; aProof = aProof.Parent) {
			if (aProof.Seal != aSealing) { continue; }
			var Proof = aProof;
			var Cache = new System.Collections.Generic.Dictionary<tNat64, tType>();
			
			tType Visit(tType aValue) {
				if (ReferenceEquals(aValue, Proof.Opaque)) { return Proof.Known; }
				if (Cache.TryGetValue(aValue.DebugId, out var Previous)) { return Previous; }
				if (aValue.Kind is tKind.Abstract or tKind.Free or tKind.Opaque) { return aValue; }
				tType Result;
				
				if (aValue.Kind is tKind.TypeApply) {
					// The type argument describes the stored element view. Opening the
					// outer constructor does not also open a nested opaque argument.
					var Constructor = Visit(aValue.Refs[0]);
					Result = ReferenceEquals(Constructor, aValue.Refs[0]) ? aValue : Constructor.ApplyType(aValue.Refs[1]);
				} else if (aValue.IsRecord(out var Fields)) {
					var Changed = false;
					var Mapped = Fields.ToStream().Map(__ => { var Type = Visit(__.Value); Changed |= !ReferenceEquals(Type, __.Value); return (__.Key, Type); }).ToArrayList().ToArray();
					Result = Changed ? Record(Mapped) : aValue;
				} else if (aValue.Kind is tKind.Generic or tKind.Sig or tKind.Interface or tKind.Recursive) {
					var Body = ReferenceEquals(aValue.Refs[0], Proof.Opaque) ? aValue.Refs[1] : Visit(aValue.Refs[1]);
					Result = ReferenceEquals(Body, aValue.Refs[1]) ? aValue : NewType(aValue.Kind, [aValue.Refs[0], Body]);
				} else {
					var Changed = false;
					var Children = mStream.Stream(aValue.Refs).Map(__ => { var Type = Visit(__); Changed |= !ReferenceEquals(Type, __); return Type; }).ToArrayList().ToArray();
					Result = Changed ? NewType(aValue.Kind, Children, aValue.Prefix) : aValue;
				}
				
				Cache.Add(aValue.DebugId, Result);
				return Result;
			}
			
			aType = Visit(aType);
		}
		
		return aType;
	}
	public static tWitnessMap? ReverseWitnesses(this tWitnessMap? aProof) => aProof is null ? null : new(aProof.Opaque, aProof.Known, !aProof.Seal, aProof.Parent.ReverseWitnesses());
	
	public static tType SealVisibility(this tType aSource, tType aTarget, tWitnessMap? aProof) {
		if (aTarget.IsAny() || aSource.SameType(aTarget)) { return aSource; }
		var Known = aTarget.ResolveWitnesses(aProof, true);
		if (aTarget.Kind is tKind.Free or tKind.Abstract or tKind.TypeApply && !ReferenceEquals(Known, aTarget)) { return aTarget; }
		if (ReferenceEquals(Known, aTarget)) { return aSource.LimitVisibility(aTarget); }
		
		if (aSource.IsRecord(out var Fields) && aTarget.IsRecord(out var TargetFields)) {
			return Record(TargetFields.ToStream().Map(__ => (__.Key, Fields.TryGet(__.Key).AssertNotEmpty().SealVisibility(__.Value, aProof))).ToArrayList().ToArray());
		}
		
		if (aSource.Kind == aTarget.Kind && aTarget.Kind is tKind.Pair or tKind.Prefix or tKind.Proc) {
			var Children = mStream.ZipShort(mStream.Stream(aSource.Refs), mStream.Stream(aTarget.Refs)).Map(__ => __.Item1.SealVisibility(__.Item2, aProof)).ToArrayList().ToArray();
			return NewType(aTarget.Kind, Children, aTarget.Prefix);
		}
		
		if (aSource.IsGeneric(out var Parameter, out var Body) && aTarget.IsGeneric(out var Other, out var TargetBody)) {
			return Generic(Other, Body.Substitute(Parameter, Other).SealVisibility(TargetBody, aProof));
		}
		
		return aTarget;
	}
	
	public static tBool
	CanMatch(mVM_Type.tType aSource, mVM_Type.tType aTarget, mStream.tStream<(tNat64, tNat64)> aSeen = default) {
		if (aSource.SameType(aTarget) || aSource.IsAny() || aTarget.Kind is tKind.Any or tKind.Free or tKind.Abstract or tKind.TypeApply) { return true; }
		if (aSeen.Any(__ => __ == (aSource.DebugId, aTarget.DebugId))) { return true; }
		aSeen = mStream.Stream((aSource.DebugId, aTarget.DebugId), aSeen);
		if (aSource.IsSet(out var A, out var B)) { return CanMatch(A, aTarget, aSeen) || CanMatch(B, aTarget, aSeen); }
		if (aTarget.IsSet(out A, out B)) { return CanMatch(aSource, A, aSeen) || CanMatch(aSource, B, aSeen); }
		if (aSource.IsRecursive(out var Head, out var Body)) { return CanMatch(Body.Substitute(Head, aSource), aTarget, aSeen); }
		if (aTarget.IsRecursive(out Head, out Body)) { return CanMatch(aSource, Body.Substitute(Head, aTarget), aSeen); }
		if (aSource.Kind != aTarget.Kind) { return false; }
		
		if (aTarget.IsRecord(out var Fields)) {
			return Fields.ToStream().All(__ => aSource.Fields.TryGet(__.Key).IsSome(out var Source) && CanMatch(Source, __.Value, aSeen));
		}
		
		if (aSource.Kind is mVM_Type.tKind.Pair or mVM_Type.tKind.Prefix) {
			return aSource.Prefix == aTarget.Prefix && mStream.ZipShort(mStream.Stream(aSource.Refs), mStream.Stream(aTarget.Refs)).All(__ => CanMatch(__.Item1, __.Item2, aSeen));
		}
		
		return aSource.IsSubType(aTarget).Match(out _, out _);
	}
	
	public static mVM_Type.tType
	LimitVisibility(
		this mVM_Type.tType aSource,
		mVM_Type.tType aTarget
	) {
		if (aTarget.IsAny() || aSource.SameType(aTarget)) {
			return aSource;
		}
		
		if (aSource.IsAny()) { return aTarget; }
		if (aTarget.Kind is tKind.Abstract or tKind.Free or tKind.TypeApply or tKind.Opaque or tKind.Proc or tKind.Generic or tKind.Recursive) { return aTarget; }
		if (aSource.IsRecursive(out var Head, out var Body)) { return Body.Substitute(Head, aSource).LimitVisibility(aTarget); }
		
		if (aSource.IsSet(out var Left, out var Right)) {
			var First = CanMatch(Left, aTarget);
			var Second = CanMatch(Right, aTarget);
			mAssert.IsTrue(First || Second, "No visible source alternative.");
			return First && Second ? Set(Left.LimitVisibility(aTarget), Right.LimitVisibility(aTarget)) : First ? Left.LimitVisibility(aTarget) : Right.LimitVisibility(aTarget);
		}
		
		if (aTarget.IsSet(out Left, out Right)) {
			var First = CanMatch(aSource, Left);
			var Second = CanMatch(aSource, Right);
			mAssert.IsTrue(First || Second, "No visible union alternative.\nSource: " + aSource.ToText() + "\nTarget: " + aTarget.ToText());
			
			return First && Second ? mVM_Type.Set(aSource.LimitVisibility(Left), aSource.LimitVisibility(Right)) :
			First ? aSource.LimitVisibility(Left) : aSource.LimitVisibility(Right);
		}
		
		if (aSource.Kind == aTarget.Kind) {
			switch (aSource.Kind) {
				case mVM_Type.tKind.Record:
				return mVM_Type.Record(
					aTarget.Fields.ToStream().Map(
						__ => (__.Key, aSource.GetFieldType(__.Key).LimitVisibility(__.Value))
					).ToArrayList().ToArray()
				);
				case mVM_Type.tKind.Interface:
				return aTarget;
				case mVM_Type.tKind.Pair:
				return mVM_Type.Pair(aSource.Refs[0].LimitVisibility(aTarget.Refs[0]), aSource.Refs[1].LimitVisibility(aTarget.Refs[1]));
				case mVM_Type.tKind.Prefix:
				return mVM_Type.Prefix(aTarget.Prefix!, aSource.Refs[0].LimitVisibility(aTarget.Refs[0]));
			}
		}
		
		return aSource;
	}
	
}