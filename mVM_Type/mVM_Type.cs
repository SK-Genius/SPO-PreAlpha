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
	
	//================================================================================
	public static tText
	AsText(
		this tType aType
	//================================================================================
	) => aType.Kind.Switch(
		aKind => "" + aKind,
		(tKind.Free, _ => $@"[?{aType.Id}]"),
		(tKind.Empty, _ => $@"[]"),
		(tKind.Bool, _ => $@"[{_}]"),
		(tKind.Int, _ => $@"[{_}]"),
		(tKind.Type, _ => $@"[§]"),
		(tKind.Pair, _ => $@"[{aType.Refs[0]}, {aType.Refs[1]}]"),
		(tKind.Prefix, _ => $@"[#{aType.Prefix}:{aType.Refs[0]}]"),
		(
			tKind.Proc,
			_ => (
				aType.Refs[0].Kind == tKind.Empty
				? $@"[{aType.Refs[1]}=>{aType.Refs[2]}]"
				: $@"[{aType.Refs[0]}:{aType.Refs[1]}=>{aType.Refs[2]}]"
			)
		),
		(tKind.Var, _ => $@"[§VAR {aType.Refs[0]}]"),
		(tKind.Ref, _ => $@"[§REF {aType.Refs[0]}]"),
		(tKind.Set, _ => $@"[{aType.Refs[0]} | {aType.Refs[1]}]"),
		(tKind.Cond, _ => $@"[{aType.Refs[0]} & ...]"),
		(tKind.Recursiv, _ => $@"[§REC {aType.Refs[0]} {aType.Refs[1]}]"),
		(tKind.Interface, _ => $@"[§ANY {aType.Refs[0]} {aType.Refs[1]}]"),
		(tKind.Generic, _ => $@"[§ALL {aType.Refs[0]} {aType.Refs[1]}]")
	);
		
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
		mStd.tAction<tText> aTrace
	//================================================================================
	) {
		if (a1 == a2) {
			return;
		}
		
		if (
			a1.Kind== tKind.Free &&
			a2.Kind== tKind.Free
		) {
			if (ReferenceEquals(a1.Refs, a2.Refs)) {
				return;
			}
			var Aliases = mList.Concat(mList.List(a1.Refs), mList.List(a2.Refs));
			var NewRefs = Aliases.ToArrayList().ToArray();
			while (Aliases.Match(out var Alias, out Aliases)) {
				Alias.Id = a1.Id;
				Alias.Refs = NewRefs;
			}
			return;
		}
		
		if (a1.Kind == tKind.Free) {
			// TODO: check aginst cycles (a1 in a2)
			var Aliases = mList.List(a1.Refs);
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
		
		mDebug.AssertEq(a1.Kind, a2.Kind);
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
		switch (aType.Kind) {
			case tKind.Empty: {
				return "[]";
			}
			case tKind.Bool: {
				return "§BOOL";
			}
			case tKind.Int: {
				return "§INT";
			}
			case tKind.Type: {
				return $"[[]]";
			}
			case tKind.Free: {
				return "?"+aType.Id;
			}
			case tKind.Prefix: {
				return $"[#{aType.Prefix} {aType.Refs[0].ToText(aLimit - 1)}]";
			}
			case tKind.Pair: {
				return $"[{aType.Refs[0].ToText(aLimit - 1)}, {aType.Refs[1].ToText(aLimit - 1)}]";
			}
			case tKind.Proc: {
				return $"[{aType.Refs[0].ToText(aLimit - 1)} : {aType.Refs[1].ToText(aLimit - 1)} -> {aType.Refs[2].ToText(aLimit - 1)}]";
			}
			case tKind.Ref: {
				return $"[§REF {aType.Refs[0].ToText(aLimit - 1)}]";
			}
			case tKind.Set: {
				return $"[{aType.Refs[0].ToText(aLimit - 1)} | {aType.Refs[1].ToText(aLimit - 1)}]";
			}
			case tKind.Var: {
				return $"[§VAR {aType.Refs[0].ToText(aLimit - 1)}]";
			}
			case tKind.Recursiv: {
				return $"[§RECURSIVE {aType.Id} -> {aType.Refs[0].ToText(aLimit - 1)}]";
			}
			case tKind.Interface: {
				return $"[§INTERFACE {aType.Id} -> {aType.Refs[0].ToText(aLimit - 1)}]";
			}
			case tKind.Generic: {
				return $"[§ALL {aType.Id} -> {aType.Refs[0].ToText(aLimit - 1)}]";
			}
			case tKind.Cond: {
				return $"[{aType.Refs[0].ToText(aLimit - 1)} ? ...]"; // TODO
			}
		}
		throw mStd.Error("");
	}
}
