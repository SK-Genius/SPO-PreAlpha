//IMPORT mStd.cs
//IMPORT mArrayList.cs
//IMPORT mDebug.cs

using tBool = System.Boolean;

using tNat8 = System.Byte;
using tNat16 = System.UInt16;
using tNat32 = System.UInt32;
using tNat64 = System.UInt64;

using tInt8 = System.SByte;
using tInt16 = System.Int16;
using tInt32 = System.Int32;
using tInt64 = System.Int64;

using tChar = System.Char;
using tText = System.String;

public static class mVM_Type {
	
	public enum
	tKind {
		Free,
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
		Recursiv,
		Interface,
		Generic
	}
	
	public class
	tType {
		public tKind Kind;
		public tText Id;
		public tText Prefix;
		public tType[] Refs = new tType[0];
		
		public override bool Equals(
			object a
		) {
			var Other = (tType)a;
			
			if (
				this.Kind != Other.Kind ||
				this.Id != Other.Id ||
				this.Prefix != Other.Prefix ||
				this.Refs.Length != Other.Refs.Length
			) {
				return false;
			}
			
			if (this.Kind == tKind.Free) {
				return true;
			}
			
			for (var I = this.Refs.Length; I --> 0;) {
				if (!this.Refs[I].Equals(Other.Refs[I])) {
					return false;
				}
			}
			
			return true;
		}
	}
		
	public static readonly tText cUnknownPrefix = null; // TODO
	
	private static int NextPlaceholderId = 1; // TODO: remove static var
	
	//================================================================================
	public static tType
	Free(
		tText aId
	//================================================================================
	) {
		var Type = new tType {
			Kind = tKind.Free,
			Id = aId
		};
		Type.Refs = new[]{ Type }; // needed for unification
		return Type;
	}
	
	//================================================================================
	public static tType
	Free(
	//================================================================================
	) {
		var Id = ""+NextPlaceholderId;
		NextPlaceholderId += 1;
		return Free(Id);
	}
	
	//================================================================================
	public static tType
	Empty(
	//================================================================================
	) => new tType { Kind = tKind.Empty };
	
	//================================================================================
	public static tType
	Bool(
	//================================================================================
	) => new tType { Kind = tKind.Bool };
	
	//================================================================================
	public static tType
	Int(
	//================================================================================
	) => new tType { Kind = tKind.Int };
	
	//================================================================================
	public static tType
	Type(
	//================================================================================
	) => new tType { Kind = tKind.Type };
	
	//================================================================================
	public static tType
	Type(
		tType aType
	//================================================================================
	) => new tType {
		Kind = tKind.Type,
		Refs = new []{ aType }
	};
	
	//================================================================================
	public static tType
	Value(
		this tType aType
	//================================================================================
	) {
		mDebug.Assert(aType.Kind == tKind.Type ||aType.Kind == tKind.Free);
		return aType.Refs[0];
	}
	
	//================================================================================
	public static tType
	Pair(
		tType aType1,
		tType aType2
	//================================================================================
	) => new tType {
		Kind = tKind.Pair,
		Refs = new []{ aType1, aType2 }
	};
	
	//================================================================================
	public static tType
	Prefix(
		tText aPrefix,
		tType aType
	//================================================================================
	) => new tType {
		Kind = tKind.Prefix,
		Prefix = aPrefix,
		Refs = new []{ aType }
	};
	
	//================================================================================
	public static tType
	Record(
		tType aOldRecordType,
		tType aNewElementType
	//================================================================================
	) {
		mStd.AssertIsIn(aOldRecordType.Kind, tKind.Record, tKind.Empty);
		mStd.AssertEq(aNewElementType.Kind, tKind.Prefix);
		AssertNotIn(aNewElementType.Prefix, aOldRecordType);
		
		return new tType {
			Kind = tKind.Record,
			Refs = new[]{aOldRecordType, aNewElementType}
		};
		
		void AssertNotIn(tText aPrefix, tType aRecord) {
			if (aRecord.Kind == tKind.Empty) {
				return;
			}
			mStd.AssertNotEq(aPrefix, aRecord.Prefix);
			AssertNotIn(aPrefix, aRecord.Refs[0]);
		}
	}
	
	//================================================================================
	public static tType
	Proc(
		tType aObjType,
		tType aArgType,
		tType aResType
	//================================================================================
	) => new tType {
		Kind = tKind.Proc,
		Refs = new []{ aObjType, aArgType, aResType }
	};
	
	//================================================================================
	public static tType
	Prefix(
		tText aId
	//================================================================================
	) => new tType {
		Kind = tKind.Var,
		Id = aId
	};
	
	//================================================================================
	public static tType
	Ref(
		tType aType
	//================================================================================
	) => new tType {
		Kind = tKind.Ref,
		Refs = new []{ aType }
	};
	
	//================================================================================
	public static tType
	Var(
		tType aType
	//================================================================================
	) => new tType {
		Kind = tKind.Var,
		Refs = new []{ aType }
	};
	
	//================================================================================
	public static tType
	Set(
		tType aType1,
		tType aType2
	//================================================================================
	) => new tType {
		Kind = tKind.Set,
		Refs = new []{ aType1, aType2 }
	};
	
	//================================================================================
	public static tType
	Cond(
		tType aType
	//================================================================================
	) {
		mDebug.Assert(false); // TODO
		return new tType {
			Kind = tKind.Cond,
			Refs = new [] { aType },
		};
	}
	
	//================================================================================
	public static tType
	Recursive(
		tType aTypeBody
	//================================================================================
	) => new tType {
		Kind = tKind.Recursiv,
		Refs = new [] { aTypeBody },
	};
	
	//================================================================================
	public static tType
	Interface(
		tType aTypeHead,
		tType aTypeBody
	//================================================================================
	) => new tType {
		Kind = tKind.Interface,
		Refs = new [] { aTypeHead, aTypeBody },
	};
	
	//================================================================================
	public static tType
	Generic(
		tType aTypeHead,
		tType aTypeBody
	//================================================================================
	) => new tType {
		Kind = tKind.Generic,
		Refs = new [] { aTypeHead, aTypeBody },
	};
	
	//================================================================================
	public static void
	Unify(
		tType a1,
		tType a2,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		if (a1 == a2) {
			return;
		}
		
		if (
			a1.Kind == tKind.Free &&
			a2.Kind == tKind.Free
		) {
			if (ReferenceEquals(a1.Refs, a2.Refs)) {
				return;
			}
			var Aliases = mStream.Concat(mStream.Stream(a1.Refs), mStream.Stream(a2.Refs));
			var NewRefs = Aliases.ToArrayList().ToArray();
			while (Aliases.Match(out var Alias, out Aliases)) {
				Alias.Id = a1.Id;
				Alias.Refs = NewRefs;
			}
			return;
		}
		
		if (a1.Kind == tKind.Free) {
			// TODO: check aginst cycles (a1 in a2)
			var Aliases = mStream.Stream(a1.Refs);
			while (Aliases.Match(out var Alias, out Aliases)) {
				Alias.Kind = a2.Kind;
				Alias.Id = a2.Id;
				Alias.Prefix = a2.Prefix;
				Alias.Refs = a2.Refs;
			}
			return;
		}
		
		if (a2.Kind == tKind.Free) {
			Unify(a2, a1, aTrace);
			return;
		}
		
		if (a1.Kind != a2.Kind) {
			var Type1 = new tType {
				Id = a1.Id,
				Kind = a1.Kind,
				Prefix = a1.Prefix,
				Refs = a1.Refs,
			};
			var Type2 = new tType {
				Id = a2.Id,
				Kind = a2.Kind,
				Prefix = a2.Prefix,
				Refs = a2.Refs,
			};
			
			a1.Kind = tKind.Set;
			a1.Id = ""+NextPlaceholderId;
			NextPlaceholderId += 1;
			a1.Prefix = null;
			
			if (a1.Kind == tKind.Set) {
				a1.Refs = mStream.Stream(Type2, mStream.Stream(a1.Refs)).ToArrayList().ToArray();
			} else if (a2.Kind == tKind.Set) {
				a1.Refs = mStream.Stream(Type1, mStream.Stream(a2.Refs)).ToArrayList().ToArray();
			} else {
				a1.Refs = new[]{Type1, Type2};
			}
			
			a2.Kind = a1.Kind;
			a2.Id = a1.Id;
			a2.Prefix = a1.Prefix;
			a2.Refs = a1.Refs;
			return;
		}
		mDebug.AssertEq(a1.Id, a2.Id);
		mDebug.AssertEq(a1.Prefix, a2.Prefix);
		var RefCount = a1.Refs.Length;
		mDebug.AssertEq(a2.Refs.Length, RefCount);
		while (RefCount --> 0) {
			Unify(a1.Refs[RefCount], a2.Refs[RefCount], aTrace);
		}
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
		return aType.Kind.Switch(
			(tKind.Empty, _ => "[]"),
			(tKind.Bool, _ => "§BOOL"),
			(tKind.Int, _ => "§INT"),
			(tKind.Type, _ => $"[[]]"),
			(tKind.Free, _ => "?"+aType.Id),
			(tKind.Prefix, _ => $"[#{aType.Prefix} {aType.Refs[0].ToText(NextLimit)}]"),
			(tKind.Record, _ => $"[{{{aType.Refs[0].ToText(NextLimit)}, {aType.Refs[1].ToText(NextLimit)}}}]"),
			(tKind.Pair, _ => $"[{aType.Refs[0].ToText(NextLimit)}, {aType.Refs[1].ToText(NextLimit)}]"),
			(tKind.Proc, _ => $"[{aType.Refs[0].ToText(NextLimit)} : {aType.Refs[1].ToText(NextLimit)} -> {aType.Refs[2].ToText(NextLimit)}]"),
			(tKind.Ref, _ => $"[§REF {aType.Refs[0].ToText(NextLimit)}]"),
			(tKind.Set, _ => $"[{mStream.Stream(aType.Refs).Map(a => a.ToText(NextLimit)).Join((a1, a2) => a1 + " | " + a2)}]"),
			(tKind.Var, _ => $"[§VAR {aType.Refs[0].ToText(NextLimit)}]"),
			(tKind.Recursiv, _ => $"[§RECURSIVE {aType.Id} -> {aType.Refs[0].ToText(NextLimit)}]"),
			(tKind.Interface, _ => $"[§INTERFACE {aType.Id} -> {aType.Refs[0].ToText(NextLimit)}]"),
			(tKind.Generic, _ => $"[§ALL {aType.Id} -> {aType.Refs[0].ToText(NextLimit)}]"),
			(tKind.Cond, _ => $"[{aType.Refs[0].ToText(NextLimit)} ? ...]") // TODO
		);
	}
}
