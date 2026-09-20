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
		Free,
		Abstract,
		SigHead,
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
		public tType[] Refs = [];
		public mTreeMap.tTree<tText, tType> Fields;
		public tNat64 DebugId = mStd.NewDebugId();
		
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
			
			if (a1.Kind is tKind.Abstract or tKind.SigHead) {
				return false;
			}
			if (a1.Kind is tKind.Free) {
				return true;
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
	
	private static tInt32 NextPlaceholderId = 1; // TODO: remove static var
	
	public static tType
	Substitute(
		this tType aType,
		tType aFree,
		tType aReplacement
	) {
		switch (aType.Kind) {
			case tKind.Abstract:
			case tKind.SigHead:
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
			case tKind.Pair:
			case tKind.Prefix:
			case tKind.Proc:
			case tKind.Ref:
			case tKind.Var:
			case tKind.Set: {
				var Refs = mStream.Stream(aType.Refs).Map(
					__ => __.Substitute(aFree, aReplacement)
				).ToArrayList().ToArray();
				return mStream.ZipShort(mStream.Stream(Refs), mStream.Stream(aType.Refs)).All(
					__ => mStd.RefEq(__.Item1, __.Item2)
				)
					? aType
					: new tType { Prefix = aType.Prefix, Kind = aType.Kind, Refs = Refs };
			}
			case tKind.TypeApply: {
				var Function = aType.Refs[0].Substitute(aFree, aReplacement);
				var Argument = aType.Refs[1].Substitute(aFree, aReplacement);
				return mStd.RefEq(Function, aType.Refs[0]) && mStd.RefEq(Argument, aType.Refs[1])
					? aType
					: Function.ApplyType(Argument);
			}
			case tKind.Record: {
				var Fields = aType.Fields.ToStream().Map(
					__ => (__.Key, Value: __.Value.Substitute(aFree, aReplacement))
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
				var Body = aType.Refs[1].Substitute(aFree, aReplacement);
				return mStd.RefEq(Body, aType.Refs[1])
					? aType
					: new tType { Kind = aType.Kind, Refs = [aType.Refs[0], Body] };
			}
			default: {
				throw new System.NotImplementedException($"aType.Kind '{aType.Kind}'"); // TODO
			}
		}
	}
	
	public static tType
	ApplyMappings(
		this tType aType,
		mStream.tStream<(tType Free, tType Ref)> aMappings
	) {
		foreach (var (Free, Ref) in aMappings) {
			mAssert.IsTrue(Free.IsFree(out _, out _));
			aType = aType.Substitute(Free, Ref);
		}
		
		return aType;
	}
	
	public static tType
	Free(
		tText aId
	) {
		var Type = new tType {
			Kind = tKind.Free,
			Id = aId
		};
		Type.Refs = [Type];
		return Type;
	}
	
	// Fixed unknown values, e.g. opened SIG heads or parameters when comparing signatures.
	public static tType
	Abstract(
		tText aId,
		tType aKind
	) => new() { Kind = tKind.Abstract, Id = aId, Refs = [aKind] };
	
	public static tType
	SigHead(
		tText aId,
		tType aKind
	) => new() { Kind = tKind.SigHead, Id = aId, Refs = [aKind] };
	
	public static tType
	Free(
	) {
		var Id = "?"+NextPlaceholderId;
		NextPlaceholderId += 1;
		return Free(Id);
	}
	
	public static tBool
	IsFree(
		this tType aType,
		[MaybeNullWhen(false)] out tText aId,
		[MaybeNullWhen(false)] out tType aRef
	) {
		if (aType.Kind is tKind.Free) {
			aId = aType.Id!;
			aRef = aType.Refs[0];
			return true;
		} else {
			aId = default!;
			aRef = default;
			return false;
		}
	}
	
	public static tType
	Any(
	) => new() { Kind = tKind.Any };
	
	public static tBool
	IsAny(
		this tType aType
	) {
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
		return aType.Kind is tKind.Any;
	}
	
	public static tType
	Empty(
	) => new() { Kind = tKind.Empty };
	
	public static tBool
	IsEmpty(
		this tType aType
	) {
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
		return aType.Kind is tKind.Empty;
	}
	
	public static tType
	True(
	) => new() { Kind = tKind.True };

	public static tType
	False(
	) => new() { Kind = tKind.False };

	public static tType
	Bool(
	) => Set(True(), False());
	
	public static tType
	Int(
	) => new() { Kind = tKind.Int };
	
	public static tBool
	IsInt(
		this tType aType
	) {
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		Free("tText"),
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
	KindType(
		this tType aType
	) => aType.Kind switch {
		tKind.Abstract or tKind.SigHead => aType.Refs[0],
		tKind.Generic => Proc(Empty(), Type(), aType.Refs[1].KindType()),
		tKind.TypeApply => aType.Refs[0].KindType().Refs[2],
		_ => Type(),
	};
	
	// A declared abstraction can describe a family of value signatures.
	// This does not change the function kind of the abstraction itself.
	public static tBool
	IsSignature(
		this tType aType
	) => aType.Kind is tKind.Generic ? aType.Refs[1].IsSignature() : aType.KindType().IsType();
	
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
		mAssert.IsTrue(
			aConstructor.KindType().IsProc(out var Obj, out var Arg, out _) &&
			Obj.IsEmpty() && Arg.SameType(aArgument.KindType()),
			$"invalid type application: {aConstructor} to {aArgument}"
		);
		if (aConstructor.Kind is tKind.Generic) {
			return aConstructor.Refs[1].Substitute(aConstructor.Refs[0], aArgument);
		}
		return new() { Kind = tKind.TypeApply, Refs = [aConstructor, aArgument] };
	}
	
	public static tType
	Sig(
		tType aHead,
		tType aBody
	) {
		mAssert.IsTrue(aBody.IsSignature(), "SIG body must be a type or generic signature");
		return new() { Kind = tKind.Sig, Refs = [aHead, aBody] };
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
		this tType aLeft,
		tType aRight
	) {
		if (mStd.RefEq(aLeft, aRight)) {
			return true;
		}
		if (aLeft.Kind != aRight.Kind || aLeft.Prefix != aRight.Prefix) {
			return false;
		}
		if (aLeft.Kind is tKind.Free or tKind.Abstract or tKind.SigHead) {
			return false;
		}
		if (aLeft.Kind is tKind.Generic or tKind.Recursive or tKind.Interface or tKind.Sig) {
			return (
				(
					aLeft.Refs[0].Kind is tKind.Free or tKind.SigHead &&
					aRight.Refs[0].Kind == aLeft.Refs[0].Kind
					? aLeft.Refs[0].KindType().SameType(aRight.Refs[0].KindType())
					: aLeft.Refs[0].SameType(aRight.Refs[0])
				) &&
				aLeft.Refs[1].Substitute(aLeft.Refs[0], aRight.Refs[0]).SameType(aRight.Refs[1])
			);
		}
		if (aLeft.Kind is tKind.Record) {
			return (
				aLeft.Fields.ToStream().Count() == aRight.Fields.ToStream().Count() &&
				aLeft.Fields.ToStream().All(
					__ => aRight.Fields.TryGet(__.Key).Match(__.Value.SameType, () => false)
				)
			);
		}
		if (aLeft.Kind is tKind.Set) {
			static mStream.tStream<tType>
			Members(tType aType) => aType.IsSet(out var A, out var B)
				? mStream.Concat(Members(A), Members(B))
				: mStream.Stream(aType);
			var Left = Members(aLeft);
			var Right = Members(aRight);
			return Left.All(__ => Right.Any(__.SameType)) && Right.All(__ => Left.Any(__.SameType));
		}
		return (
			aLeft.Refs.Length == aRight.Refs.Length &&
			mStream.ZipShort(mStream.Stream(aLeft.Refs), mStream.Stream(aRight.Refs)).All(
				__ => __.Item1.SameType(__.Item2)
			)
		);
	}
	
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
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
			mAssert.IsNotNull(Head.Refs[0]);
			aType = Body;
		}
		
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
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
	};
	
	public static tBool
	IsPrefix(
		this tType aType,
		tText aPrefix
	) {
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
	) => new() {
		Kind = tKind.Recursive,
		Refs = [aTypeHead, aTypeBody],
	};
	
	public static tBool
	IsRecursive(
		this tType aType,
		out tType aHeadType,
		out tType aBodyType
		// aCond
	) {
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
	) => new() {
		Kind = tKind.Interface,
		Refs = [aTypeHead, aTypeBody],
	};
	
	public static tBool
	IsInterface(
		this tType aType,
		[MaybeNullWhen(false)] out tType aHeadType,
		[MaybeNullWhen(false)] out tType aBodyType
		// aCond
	) {
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
		return new() { Kind = tKind.Generic, Refs = [aTypeHead, aTypeBody] };
	}
	
	public static tBool
	IsGeneric(
		this tType aType,
		[MaybeNullWhen(false)] out tType aHeadType,
		[MaybeNullWhen(false)] out tType aBodyType
		// aCond
	) {
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
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
	
	private static tText
	ExtendError(
		tText aError,
		tType aSubType,
		tType aSupType
	) => $"""
		{aError}
		in:
		{"  " + aSubType.ToText("\n  ")}
		!<
		{"  " + aSupType.ToText("\n  ")}
		
		""";
	
	public static mResult.tResult<mStream.tStream<(tType Free, tType Ref)>, tText>
	IsSubType(
		this tType aSubType,
		tType aSupType,
		mStream.tStream<(tType Free, tType Ref)> aTypeMappings
	) {
		static tBool
		HasFreeType(
			tType aType
		) => aType.Kind switch {
			tKind.Free => true,
			tKind.Abstract or tKind.SigHead => false,
			tKind.Record => aType.Fields.ToStream().Any(__ => HasFreeType(__.Value)),
			_ => mStream.Stream(aType.Refs).Any(HasFreeType)
		};
		
		static mStream.tStream<(tType Free, tType Ref)>
		MapFree(
			mStream.tStream<(tType Free, tType Ref)> aTypeMappings,
			tType aFree,
			tType aRef
		) => mStream.Stream(
			(
				aFree,
				aTypeMappings.Where(
					__ => ReferenceEquals(__.Free, aFree)
				).TryFirst(
				).Match(
					__ => Union(aRef, __.Ref),
					() => aRef
				)
			),
			aTypeMappings
		);
		
		if (aSubType.Kind is tKind.Free) {
			aSubType = aSubType.Refs[0];
		}
		
		if (aSupType.Kind is tKind.Free) {
			aSupType = aSupType.Refs[0];
		}
		
		if (!aSubType.IsSignature() || !aSupType.IsSignature()) {
			return mResult.Fail("comparison requires types or declared generic signatures");
		}
		
		if (
			ReferenceEquals(aSubType, aSupType) ||
			(aSubType.SameType(aSupType) && !HasFreeType(aSubType))
		) {
			return aTypeMappings;
		}
		
		var SubBaseType = aSubType.BaseType();
		
		if (aSupType.Kind is tKind.Free) {
			return aSubType.KindType().IsType()
				? MapFree(aTypeMappings, aSupType, aSubType)
				: mResult.Fail("a free type variable cannot hold a type abstraction");
		}
		
		if (aSubType.Kind is tKind.Free) {
			return aSupType.KindType().IsType()
				? MapFree(aTypeMappings, aSubType, aSupType)
				: mResult.Fail("a free type variable cannot hold a type abstraction");
		}
		
		if (SubBaseType.IsSet(out var SubType1, out var SubType2)) {
			return SubType1.IsSubType(aSupType, aTypeMappings).ThenTry(
				__ => SubType2.IsSubType(aSupType, __)
			).ModifyError(
				__ => ExtendError(__, aSubType, aSupType)
			);
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
					: mResult.Fail(ExtendError("", aSubType, aSupType));
			}
			case tKind.Abstract:
			case tKind.SigHead: {
				return mResult.Fail(ExtendError("different bound types", aSubType, aSupType));
			}
			case tKind.TypeApply: {
				if (SubBaseType.Kind is not tKind.TypeApply) {
					return mResult.Fail(ExtendError("different type constructors", aSubType, aSupType));
				}
				static mResult.tResult<mStream.tStream<(tType Free, tType Ref)>, tText>
				MatchApplication(
					tType aLeft,
					tType aRight,
					mStream.tStream<(tType Free, tType Ref)> aMappings
				) {
					if (aLeft.SameType(aRight)) {
						return aMappings;
					}
					if (aLeft.Kind is not tKind.TypeApply || aRight.Kind is not tKind.TypeApply) {
						return mResult.Fail("different type function bindings");
					}
					return MatchApplication(aLeft.Refs[0], aRight.Refs[0], aMappings).ThenTry(
						__ => aLeft.Refs[1].IsSubType(aRight.Refs[1], __)
					).ThenTry(
						__ => aRight.Refs[1].IsSubType(aLeft.Refs[1], __)
					);
				}
				return MatchApplication(SubBaseType, aSupType, aTypeMappings);
			}
			case tKind.Sig: {
				if (!SubBaseType.IsSig(out var SubHead, out var SubBody)) {
					return mResult.Fail(ExtendError("expected SIG", aSubType, aSupType));
				}
				var SupHead = aSupType.Refs[0];
				if (!SubHead.KindType().SameType(SupHead.KindType())) {
					return mResult.Fail(ExtendError("different SIG head kinds", aSubType, aSupType));
				}
				if (SupHead.Kind is not tKind.SigHead && !SubHead.SameType(SupHead)) {
					return mResult.Fail(ExtendError("different SIG heads", aSubType, aSupType));
				}
				var Witness = SubHead.Kind is tKind.SigHead
					? Abstract(SubHead.Id!, SubHead.KindType())
					: SubHead;
				return SubBody.Substitute(SubHead, Witness).IsSubType(
					aSupType.Refs[1].Substitute(SupHead, Witness),
					aTypeMappings
				);
			}
			case tKind.Pair: {
				var TailSubType = SubBaseType;
				var TailSupType = aSupType;
				var Error = "";
				if (
					TailSubType.IsPair(out TailSubType, out var HeadSubType) &&
					TailSupType.IsPair(out TailSupType, out var HeadSupType) &&
					HeadSubType.IsSubType(HeadSupType, aTypeMappings).Match(out aTypeMappings, out Error) &&
					TailSubType.IsSubType(TailSupType, aTypeMappings).Match(out aTypeMappings, out Error)
				) {
					return aTypeMappings;
				} else {
					return mResult.Fail(ExtendError(Error, aSubType, aSupType));
				}
			}
			case tKind.Prefix: {
				if (
					SubBaseType.IsPrefix(out var SubPrefix, out var Sub) &&
					aSupType.IsPrefix(out var SupPrefix, out var Sup) &&
					SubPrefix == SupPrefix
				) {
					return Sub.IsSubType(
						Sup,
						aTypeMappings
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
				}
				
				if (!aSubType.IsRecord(out var SubFields)) {
					return mResult.Fail(
						ExtendError(
							$"Expected Record but is {aSubType}",
							aSubType,
							aSupType
						)
					);
				}
				
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
				}
				
				return SubObj.IsSubType(SupObj, aTypeMappings)
				.ThenTry(__ => SupObj.IsSubType(SubObj, __))
				.ThenTry(__ => SubArg.IsSubType(SupArg, __))
				.ThenTry(__ => SubRes.IsSubType(SupRes, __))
				.ElseTry(__ => mResult.Fail(ExtendError(__, aSubType, aSupType)));
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
					if (!ReferenceEquals(SubHead, SupHead)) {
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
				var SupHead = aSupType.Refs[0];
				var SupBody = aSupType.Refs[1];
				if (aSubType.IsGeneric(out var SubHead, out var SubBody)) {
					// Signature parameters are inferred from arguments; their declaration order may differ.
					return SubBody.Substitute(SubHead, SupHead).IsSubType(SupBody, aTypeMappings).ModifyError(
						__ => ExtendError(__, aSubType, aSupType)
					);
				}
				// A monomorphic function must work for an arbitrary parameter, not just one inferred type.
				return aSubType.IsSubType(
					SupBody.Substitute(SupHead, Abstract(SupHead.Id!, Type())),
					aTypeMappings
				).ModifyError(__ => ExtendError(__, aSubType, aSupType));
			}
			case tKind.Interface: {
				mAssert.IsTrue(aSupType.IsInterface(out var SupHead, out var SupBody));
				if (aSubType.IsInterface(out var SubHead, out var SubBody)) {
					if (!ReferenceEquals(SubHead, SupHead)) {
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
			
			return ReferenceEquals(Expanded, aType)
			? mStd.cEmpty
			: Expanded.SubSet(aSelect);
		}
		
		if (aType.IsSet(out var Type1, out var Type2)) {
			var Selected1 = Type1.SubSet(aSelect);
			var Selected2 = Type2.SubSet(aSelect);
			
			return !Selected1.IsSome(out var SelectedType1) ? Selected2
			: !Selected2.IsSome(out var SelectedType2) ? Selected1
			: SelectedType1 == SelectedType2 ? Selected1
			: Set(SelectedType1, SelectedType2);
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
		
		if (!aArg.IsSubType(ArgType, mStd.cEmpty).Match(out var TypeMappings, out var Error)) {
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
		Type1 == Type2 ? aType1 :
		mMaybe.Some(Set(Type1, Type2))
	);
	
	public static mMaybe.tMaybe<tType>
	Subtract(
		this tType aType,
		tType aRemoved
	) {
		if (aType.IsSubType(aRemoved, mStd.cEmpty).Match(out _, out _)) {
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
			tKind.Free => "?" + aType.Id,
			tKind.Abstract => "^" + aType.Id,
			tKind.SigHead => aType.Id!,
			tKind.TypeApply => $"[.{aType.Refs[0].ToText(____)} {aType.Refs[1].ToText(____)}]",
			tKind.Sig => $"[§SIG_WITH {aType.Refs[0]} € {aType.Refs[0].KindType()} IN {aType.Refs[1].ToText(____)}]",
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
			tKind.Recursive => $"[{____}§RECURSIVE {aType.Refs[0]} = {aType.Refs[1].ToText(____)}{__}]",
			tKind.Generic => $"[{____}§ALL {aType.Refs[0]} => {aType.Refs[1].ToText(____)}{__}]",
			tKind.Interface => $"[{____}§LET {aType.Refs[0]} IN {aType.Refs[1].ToText(____)}{__}]",
			tKind.Cond => $"[{____ + aType.Refs[0].ToText(____)} ? ...{__}]", // TODO
			_ => throw mError.Error("impossible")
		};
	}
}
