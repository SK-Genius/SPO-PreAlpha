//IMPORT mStd.cs
//IMPORT mArrayList.cs
//IMPORT mDebug.cs

#nullable enable

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
	
	public class
	tType {
		public tKind Kind;
		public tText? Id;
		public tText? Prefix;
		public tType[] Refs = System.Array.Empty<tType>();
		
		public override bool
		Equals(
			object? a
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
			
			if (a1.Kind == tKind.Free) {
				return true;
			}
			
			for (var I = a1.Refs.Length; I --> 0;) {
				if (!a1.Refs[I].Equals(a2.Refs[I])) {
					return false;
				}
			}
			
			return true;
		}
		
		public override string
		ToString(
		) => this.ToText(10);
	}
	
	public static readonly tText? cUnknownPrefix = null; // TODO
	
	private static int NextPlaceholderId = 1; // TODO: remove static var
	
	public static tType
	Free(
		tText aId
	) {
		var Type = new tType {
			Kind = tKind.Free,
			Id = aId
		};
		Type.Refs = new[]{ Type }; // needed for unification
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
	MatchFree(
		this tType aType,
		[MaybeNullWhen(false)]out tText aId,
		[MaybeNullWhen(false)]out tType aRef
	) {
		if (aType.Kind == tKind.Free) {
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
	MatchAny(
		this tType aType
	) => aType.Kind == tKind.Any;
	
	public static tType
	Empty(
	) => new() { Kind = tKind.Empty };
	
	public static tBool
	MatchEmpty(
		this tType aType
	) => aType.Kind == tKind.Empty;
	
	public static tType
	Bool(
	) => new() { Kind = tKind.Bool };
	
	public static tBool
	MatchBool(
		this tType aType
	) => aType.Kind == tKind.Bool;
	
	public static tType
	Int(
	) => new() { Kind = tKind.Int };
	
	public static tBool
	MatchInt(
		this tType aType
	) => aType.Kind == tKind.Int;
	
	public static tType
	Type(
	) => new() { Kind = tKind.Type };
	
	public static tBool
	MatchType(
		this tType aType,
		out mMaybe.tMaybe<tType> aOfType
	) { 
		if (aType.Kind != tKind.Type) {
			aOfType = mStd.cEmpty;
			return false;
		}
		
		aOfType = aType.Refs.Length == 0 ? mStd.cEmpty : aType.Refs[0];
		return true;
	}
	
	public static tType
	Type(
		tType? aType
	) => new() {
		Kind = tKind.Type,
		Refs = aType is null ? new tType[0] : new []{ aType }
	};
	
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
		Refs = new []{ aType1, aType2 }
	};
	
	public static tBool
	MatchPair(
		this tType aType,
		[MaybeNullWhen(false)]out tType aType1,
		[MaybeNullWhen(false)]out tType aType2
	) {
		switch (aType.Kind) {
			case tKind.Pair: {
				aType1 = aType.Refs[0];
				aType2 = aType.Refs[1];
				return true;
			}
			case tKind.Empty: {
				aType1 = default!;
				aType2 = default!;
				return false;
			}
			default: {
				aType1 = aType;
				aType2 = Empty();
				return true;
			}
		}
	}
	
	public static tType
	Tuple(
		mStream.tStream<tType>? aTypes
	) => aTypes.Reduce(Empty(), Pair);
	
	public static tType
	Tuple(
		params tType[] aTypes
	) => Tuple(mStream.Stream(aTypes));
	
	public static tType
	Prefix(
		tText aPrefix,
		tType aType
	) => new() {
		Kind = tKind.Prefix,
		Prefix = mAssert.IsNotNull(aPrefix),
		Refs = new []{ aType }
	};
	
	public static tBool
	MatchPrefix(
		this tType aType,
		[MaybeNullWhen(false)] out tText aPrefix,
		[MaybeNullWhen(false)] out tType aTypeOut
	) {
		if (aType.Kind == tKind.Prefix) {
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
	MatchPrefix(
		this tType aType,
		tText aPrefix,
		[MaybeNullWhen(false)] out tType aTypeOut
	) => aType.MatchPrefix(out var Prefix, out aTypeOut!) && Prefix == aPrefix;
	
	public static tType
	Record(
		tType aHeadType,
		tType aTailType
	) {
		mAssert.IsIn(aTailType.Kind, tKind.Record, tKind.Empty);
		mAssert.AreEquals(aHeadType.Kind, tKind.Prefix);
		AssertNotIn(aHeadType.Prefix!, aTailType);
		
		return new tType {
			Kind = tKind.Record,
			Refs = new[]{aTailType, aHeadType}
		};
		
		static void
		AssertNotIn(tText aPrefix, tType aRecord) {
			if (aRecord.Kind == tKind.Empty) {
				return;
			}
			mAssert.AreNotEquals(aPrefix, aRecord.Prefix);
			AssertNotIn(aPrefix, aRecord.Refs[0]);
		}
	}
	
	public static tBool
	MatchRecord(
		this tType aType,
		[MaybeNullWhen(false)] out tText aHeadKey,
		[MaybeNullWhen(false)] out tType aHeadType,
		[MaybeNullWhen(false)] out tType aTailRecord
	) {
		if (aType.Kind == tKind.Record) {
			mAssert.IsTrue(aType.Refs[1].MatchPrefix(out aHeadKey!, out aHeadType!));
			aTailRecord = aType.Refs[0];
			return true;
		} else {
			aHeadKey = default!;
			aHeadType = default!;
			aTailRecord = default!;
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
		Refs = new []{ aObjType, aArgType, aResType }
	};
	
	public static tBool
	MatchProc(
		this tType aType,
		[MaybeNullWhen(false)] out tType aObjType,
		[MaybeNullWhen(false)] out tType aArgType,
		[MaybeNullWhen(false)] out tType aResType
	) {
		if (aType.Kind == tKind.Proc) {
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
		Refs = new[]{Empty()},
	};
	
	public static tBool
	MatchPrefix(
		this tType aType,
		tText aPrefix
	) => aType.Prefix == aPrefix && aType.Refs[0].MatchEmpty();
	
	public static tType
	Ref(
		tType aType
	) => new() {
		Kind = tKind.Ref,
		Refs = new []{ aType }
	};
	
	public static tBool
	MatchRef(
		this tType aType,
		[MaybeNullWhen(false)] out tType aOutType
	) {
		if (aType.Kind == tKind.Ref) {
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
		Refs = new []{ aType }
	};
	
	public static tBool
	MatchVar(
		this tType aType,
		[MaybeNullWhen(false)] out tType aOutType
	) {
		if (aType.Kind == tKind.Var) {
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
			Refs = new []{ aType1, aType2 }
		};
	}
	
	public static tBool
	MatchSet(
		this tType aType,
		[MaybeNullWhen(false)] out tType aType1,
		[MaybeNullWhen(false)] out tType aType2
	) {
		if (aType.Kind == tKind.Set) {
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
			Refs = new [] { aType },
		};
	}
	
	public static tBool
	MatchCond(
		this tType aType,
		out tType aSuperType
		// aCond
	) {
		mAssert.IsTrue(false); // TODO
		if (aType.Kind == tKind.Cond) {
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
		Refs = new [] { aTypeHead, aTypeBody },
	};
	
	public static tBool
	MatchRecursive(
		this tType aType,
		out tType aOutType
		// aCond
	) {
		mAssert.IsTrue(false); // TODO
		if (aType.Kind == tKind.Recursive) {
			aOutType = aType.Refs[0];
			return true;
		} else {
			aOutType = default!;
			return false;
		}
	}
	
	public static tType
	Interface(
		tType aTypeHead,
		tType aTypeBody
	) => new() {
		Kind = tKind.Interface,
		Refs = new [] { aTypeHead, aTypeBody },
	};
	
	public static tBool
	MatchInterface(
		this tType aType,
		[MaybeNullWhen(false)] out tType aHeadType,
		[MaybeNullWhen(false)] out tType aBodyType
		// aCond
	) {
		if (aType.Kind == tKind.Interface) {
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
		Kind = tKind.Generic ,
		Refs = new [] { aTypeHead, aTypeBody },
	};
	
	public static tBool
	MatchGeneric(
		this tType aType,
		[MaybeNullWhen(false)] out tType aHeadType,
		[MaybeNullWhen(false)] out tType aBodyType
		// aCond
	) {
		if (aType.Kind == tKind.Generic ) {
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
	Normalize(
		this tType a
	) => (
		a.Kind == tKind.Pair && a.Refs[1].MatchEmpty()
		? a.Refs[0].Normalize()
		: a
	);
	
	public static mResult.tResult<mStream.tStream<(tType Free, tType Ref)>?, tText>
	IsSubType(
		this tType aSubType,
		tType aSupType,
		mStream.tStream<(tType Free, tType Ref)>? aTypeMappings
	) {
		aSubType = aSubType.Normalize();
		aSupType = aSupType.Normalize();
		
		if (aSubType == aSupType) {
			return mResult.OK(aTypeMappings);
		}
		
		var SubBaseType = aSubType.BaseType();
		
		if (aSupType.Kind is tKind.Free) {
			return mResult.OK(
				mStream.Stream(
					(Free: aSupType, Ref: aSubType),
					aTypeMappings
				)
			);
		}
		
		if (aSubType.Kind is tKind.Free) {
			return mResult.OK(
				mStream.Stream(
					(Free: aSubType, Ref: aSupType),
					aTypeMappings
				)
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
				if (SubBaseType.Kind == aSupType.Kind) {
					return mResult.OK(aTypeMappings);
				} else {
					return mResult.Fail($"{SubBaseType.Kind} != {aSupType.Kind}");
				}
			}
			case tKind.Pair: {
				if (
					!SubBaseType.MatchPair(out var Sub1, out var Sub2) ||
					!aSupType.MatchPair(out var Sup1, out var Sup2)
				) {
					return mResult.Fail(mStd.FileLine());
				}
				
				return Sub1.IsSubType(
					Sup1,
					aTypeMappings
				).ThenTry(
					a => Sub2.IsSubType(
						Sup2,
						a
					)
				);
			}
			case tKind.Prefix: {
				if (
					SubBaseType.MatchPrefix(out var SubPrefix, out var Sub) &&
					aSupType.MatchPrefix(out var SupPrefix, out var Sup) &&
					SubPrefix == SupPrefix
				) {
					return Sub.IsSubType(Sup, aTypeMappings);
				} else {
					return mResult.Fail(mStd.FileLine());
				}
			}
			case tKind.Record: {
				throw new System.NotImplementedException();
			}
			case tKind.Proc: {
				if (
					!aSubType.MatchProc(out var SubObj, out var SubArg, out var SubRes) ||
					!aSupType.MatchProc(out var SupObj, out var SupArg, out var SupRes)
				) {
					return mResult.Fail(mStd.FileLine());
				}
				
				return SubObj.IsSubType(SupObj, aTypeMappings)
				.ThenTry(_ => SupObj.IsSubType(SubObj, _))
				.ThenTry(_ => SubArg.IsSubType(SupArg, _))
				.ThenTry(_ => SubRes.IsSubType(SupRes, _));
			}
			case tKind.Var: {
				throw new System.NotImplementedException();
			}
			case tKind.Ref: {
				throw new System.NotImplementedException();
			}
			case tKind.Set: {
				if (SubBaseType.Kind == tKind.Set) {
					throw new System.NotImplementedException();
				} else {
					throw new System.NotImplementedException();
				}
			}
			case tKind.Cond: {
				throw new System.NotImplementedException();
			}
			case tKind.Recursive: {
				throw new System.NotImplementedException();
			}
			case tKind.Generic: {
				throw new System.NotImplementedException();
			}
			case tKind.Interface: {
				throw new System.NotImplementedException();
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
		if (a.Kind == tKind.Cond) {
			if (a.MatchCond(out var Sup)) {
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
		if (aProc.MatchGeneric(out var FreeType, out var InnerType)) {
			return Infer(InnerType, aObj, aArg, aTrace);
		}
		
		mAssert.IsTrue(aProc.MatchProc(out var ObjType, out var ArgType, out var ResType));
		mAssert.IsTrue(
			aArg.IsSubType(ArgType, mStd.cEmpty).Match(out _, out var Error),
			() => $"""
			{Error}
				{aArg}
				is not a subtype of
				{ArgType}
			"""
		);
		mAssert.AreEquals(aObj, ObjType);
		
		return mResult.OK(ResType);
	}
	
	public static tText
	ToText(
		this tType aType,
		tInt32 aLimit
	) {
		if (aLimit <= 0) {
			return "...";
		}
		var NextLimit = aLimit - 1;
		return aType.Kind switch {
			tKind.Empty => "[]",
			tKind.Bool => "§BOOL",
			tKind.Int => "§INT",
			tKind.Any => "§ANY",
			tKind.Type => "[[]]",
			tKind.Free => "?"+aType.Id,
			tKind.Prefix => $"[#{aType.Prefix} {aType.Refs[0].ToText(NextLimit)}]",
			tKind.Record => mStd.Call(
				() => {
					var Text = "[{";
					var Type = aType;
					for (var Limit = aLimit; Type.Kind == tKind.Record; Limit -= 1) {
						if (Limit <= 0) {
							Text += ", ...";
							break;
						}
						Text += ", " + Type.Refs[1].ToText(aLimit);
						Type = Type.Refs[0];
					}
					if (Type.Kind != tKind.Empty) {
						Text += "; " + Type.ToText(aLimit);
					}
					return Text + "}]";
				}
			),
			tKind.Pair => mStd.Call(
				() => {
					var Result = "[";
					var Temp = aType;
					Result += Temp.Refs[0].ToText(NextLimit);
					Temp = Temp.Refs[1];
					
					while (Temp.Kind == tKind.Pair) {
						Result += ", " + Temp.Refs[0].ToText(NextLimit);
						Temp = Temp.Refs[1];
					}
					if (Temp.Kind != tKind.Empty) {
						Result += "; " + Temp.ToText(NextLimit);
					}
					return Result + "]";
				}
			),
			tKind.Proc => mStd.Call(
				() => {
					var Result = "[";
					if (aType.Refs[0].Kind != tKind.Empty) {
						Result += aType.Refs[0].ToText(NextLimit) + " : ";
					}
					if (aType.Refs[1].Kind != tKind.Empty) {
						Result += aType.Refs[1].ToText(NextLimit);
					}
					if (aType.Refs[2].Kind != tKind.Empty) {
						Result += " -> " + aType.Refs[2].ToText(NextLimit);
					}
					return Result + "]";
				}
			),
			tKind.Ref => $"[§REF {aType.Refs[0].ToText(NextLimit)}]",
			tKind.Set => $"[{mStream.Stream(aType.Refs).Map(a => a.ToText(NextLimit)).Join((a1, a2) => a1 + " | " + a2, "")}]",
			tKind.Var => $"[§VAR {aType.Refs[0].ToText(NextLimit)}]",
			tKind.Recursive => $"[§RECURSIVE {aType.Id} = {aType.Refs[0].ToText(NextLimit)}]",
			tKind.Generic => $"[{aType.Id} => {aType.Refs[0].ToText(NextLimit)}]",
			tKind.Interface => $"[§LET {aType.Id} IN {aType.Refs[0].ToText(NextLimit)}]",
			tKind.Cond => $"[{aType.Refs[0].ToText(NextLimit)} ? ...]", // TODO
			_ => throw mError.Error("impossible")
		};
	}
}
