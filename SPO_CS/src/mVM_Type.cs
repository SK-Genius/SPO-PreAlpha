// IMPORT Common/mStd
// IMPORT Common/mResult
// IMPORT Common/mStream
// IMPORT Common/mError
// IMPORT Common/mAssert
// IMPORT Common/mMaybe

public static class
mVM_Type {
	
	public enum
	tKind {
		Free,
		Any,
		Empty,
		Bool,
		Int,
		Type,
		Pair,
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
			if (ReferenceEquals(a1, a2)) {
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
		tText aFreeId,
		tType aReplacement
	) {
		switch (aType.Kind) {
			case tKind.Free: {
				return aType.Id == aFreeId
				? aReplacement
				: aType;
			}
			case tKind.Empty:
			case tKind.Bool:
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
					Refs = System.Array.ConvertAll(aType.Refs, _ => _.Substitute(aFreeId, aReplacement))
				};
			}
			case tKind.Record: {
				mAssert.IsTrue(aType.IsRecord(out var Fields));
				return Record(
					Fields.ToStream(
					).Map(
						_ => (_.Key, _.Value.Substitute(aFreeId, aReplacement))
					).ToArrayList(
					).ToArray(
					)
				);
			}
			case tKind.Recursive: {
				mAssert.IsTrue(aType.IsRecursive(out var Head, out var Body));
				return Head.Id == aFreeId
					? aType
					: Recursive(Head, Body.Substitute(aFreeId, aReplacement));
			}
			default: {
				throw new System.NotImplementedException($"aType.Kind '{aType.Kind}'"); // TODO
			}
		}
	}
	
	public static tType
	Free(
		tText aId
	) {
		var Type = new tType {
			Kind = tKind.Free,
			Id = aId
		};
		Type.Refs = [Type]; // needed for unification
		return Type;
	}
	
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
	Bool(
	) => new() { Kind = tKind.Bool };
	
	public static tBool
	IsBool(
		this tType aType
	) {
		if (aType.Kind is tKind.Free) {
			aType = aType.Refs[0];
		}
		
		return aType.Kind is tKind.Bool;
	}
	
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
		_ => Recursive(
			_,
			Set(
				Pair(_, Char()),
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
	Value(
		this tType aType
	) {
		mAssert.IsTrue(aType.Kind is tKind.Type or tKind.Free);
		return aType.Refs[0];
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
			Unknown field '{aKey}' in record [{Fields.ToStream().Map(_ => _.Key).Reduce("", (a1, a2) => a1 + "\n  " + a2)}
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
		if (aTailType.IsEmpty()) {;
			Fields = mTreeMap.Tree<tText, tType>((a1, a2) => a1.CompareTo(a2).Sign(), []);
		} else {
			mAssert.IsTrue(aTailType.IsRecord(out Fields));
		}
		mAssert.IsTrue(Fields.TryGet(Prefix).IsNone(),
			$"Field '{Prefix}' already exists in record [{Fields.ToStream().Map(_ => _.Key).Reduce("", (a1, a2) => a1 + "\n  " + a2)}]"
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
	) => new() {
		Kind = tKind.Generic,
		Refs = [aTypeHead, aTypeBody],
	};
	
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
		if (aSubType.Kind is tKind.Free) {
			aSubType = aSubType.Refs[0];
		}
		
		if (aSupType.Kind is tKind.Free) {
			aSupType = aSupType.Refs[0];
		}
		
		if (aSubType == aSupType) {
			return aTypeMappings;
		}
		
		var SubBaseType = aSubType.BaseType();
		
		if (aSupType.Kind is tKind.Free) {
			return mStream.Stream(
				(Free: aSupType, Ref: aSubType),
				aTypeMappings
			);
		}
		
		if (aSubType.Kind is tKind.Free) {
			return mStream.Stream(
				(Free: aSubType, Ref: aSupType),
				aTypeMappings
			);
		}
		
		if (SubBaseType.IsSet(out var SubType1, out var SubType2)) {
			return SubType1.IsSubType(aSupType, aTypeMappings).ThenTry(
				_ => SubType2.IsSubType(aSupType, _)
			).ModifyError(
				_ => ExtendError(_, aSubType, aSupType)
			);
		}
		
		// TODO: implement
		switch (aSupType.Kind) {
			case tKind.Free: {
				throw new System.NotImplementedException();
			}
			case tKind.Any: {
				throw new System.NotImplementedException();
			}
			case tKind.Empty:
			case tKind.Bool:
			case tKind.Int:
			case tKind.Type: {
				return SubBaseType.Kind == aSupType.Kind
					? aTypeMappings
					: mResult.Fail(ExtendError("", aSubType, aSupType));
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
						_ => mResult.Fail(ExtendError(_, aSubType, aSupType))
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
				.ThenTry(_ => SupObj.IsSubType(SubObj, _))
				.ThenTry(_ => SubArg.IsSubType(SupArg, _))
				.ThenTry(_ => SubRes.IsSubType(SupRes, _))
				.ElseTry(_ => mResult.Fail(ExtendError(_, aSubType, aSupType)));
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
					if (SubHead.Id != SupHead.Id) {
						SubBody = SubBody.Substitute(SubHead.Id, Free(SupHead.Id));
					}
					return SubBody.IsSubType(SupBody, aTypeMappings).ModifyError(
						_ => ExtendError(_, aSubType, aSupType)
					);
				} else {
					return aSubType.IsSubType(
						SupBody.Substitute(SupHead.Id, aSupType),
						aTypeMappings
					).ModifyError(
						_ => ExtendError(_, aSubType, aSupType)
					);
				}
			}
			case tKind.Generic: {
				mAssert.IsTrue(aSupType.IsGeneric(out var SupHead, out var SupBody));
				if (aSubType.IsGeneric(out var SubHead, out var SubBody)) {
					if (SubHead.Id != SupHead.Id) {
						SubBody = SubBody.Substitute(SubHead.Id, Free(SupHead.Id));
					}
					return SubBody.IsSubType(SupBody, aTypeMappings).ModifyError(
						_ => ExtendError(_, aSubType, aSupType)
					);
				} else {
					return aSubType.IsSubType(SupBody, aTypeMappings).ModifyError(
						_ => ExtendError(_, aSubType, aSupType)
					);
				}
			}
			case tKind.Interface: {
				mAssert.IsTrue(aSupType.IsInterface(out var SupHead, out var SupBody));
				if (aSubType.IsInterface(out var SubHead, out var SubBody)) {
					if (SubHead.Id != SupHead.Id) {
						SubBody = SubBody.Substitute(SubHead.Id, Free(SupHead.Id));
					}
					return SubBody.IsSubType(SupBody, aTypeMappings).ModifyError(
						_ => ExtendError(_, aSubType, aSupType)
					);
				} else {
					return aSubType.IsSubType(SupBody, aTypeMappings).ModifyError(
						_ => ExtendError(_, aSubType, aSupType)
					);
				}
			}
			default: {
				mAssert.Impossible();
				return default;
			}
		}
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
		
		foreach (var Mapping in TypeMappings) {
			mAssert.IsTrue(Mapping.Free.IsFree(out var Id, out _));
			ResType = ResType.Substitute(Id, Mapping.Ref);
		}
		
		return ResType;
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
			tKind.Bool => "§BOOL",
			tKind.Int => "§INT",
			tKind.Any => "§ANY",
			tKind.Type => "§TYPE",
			tKind.Free => "?" + aType.Id,
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
					while (Temp.Kind is tKind.Pair) {
						Result = Temp.Refs[1].ToText(____) + "," + ____ + Result;
						Temp = Temp.Refs[0];
					}
					if (Temp.Kind is not tKind.Empty) {
						Result = Temp.ToText(____) + ";" + ____ + Result;
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
			tKind.Set => $"[{____}{mStream.Stream(System.MemoryExtensions.AsSpan(aType.Refs)).Map(_ => _.ToText(____)).Join((a1, a2) => a1 + " |" + ____ + a2, "")}{__}]",
			tKind.Var => $"[{____}§VAR {aType.Refs[0].ToText(____)}{__}]",
			tKind.Recursive => $"[{____}§RECURSIVE {aType.Refs[0]} = {aType.Refs[1].ToText(____)}{__}]",
			tKind.Generic => $"[{____}{aType.Refs[0]} => {aType.Refs[1].ToText(____)}{__}]",
			tKind.Interface => $"[{____}§LET {aType.Refs[0]} IN {aType.Refs[1].ToText(____)}{__}]",
			tKind.Cond => $"[{____ + aType.Refs[0].ToText(____)} ? ...{__}]", // TODO
			_ => throw mError.Error("impossible")
		};
	}
}
