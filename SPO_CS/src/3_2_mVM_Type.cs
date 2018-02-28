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
	tTypeType {
		Unknown,
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
	}
	
	public class
	tType {
		public tTypeType Type;
		public tText Id;
		public tText Prefix;
		public tType[] Refs = new tType[0];
		
		public override tText ToString(
		) => Type.Switch(
			_ => _.ToString(),
			(tTypeType.Unknown, _ => $@"[?{Id}]"),
			(tTypeType.Empty,  _ => $@"[]"),
			(tTypeType.Bool,  _ => $@"[{_}]"),
			(tTypeType.Int,  _ => $@"[{_}]"),
			(tTypeType.Type,  _ => $@"[§]"),
			(tTypeType.Pair, _ => $@"[{Refs[0]}, {Refs[1]}]"),
			(tTypeType.Prefix, _ => $@"[#{Prefix}:{Refs[0]}]"),
			(
				tTypeType.Proc,
				_ => (
					Refs[0].Type == tTypeType.Empty
					? $@"[{Refs[1]}=>{Refs[2]}]"
					: $@"[{Refs[0]}:{Refs[1]}=>{Refs[2]}]"
				)
			),
			(tTypeType.Var, _ => $@"[§VAR {Refs[0]}]"),
			(tTypeType.Ref, _ => $@"[§REF {Refs[0]}]"),
			(tTypeType.Set, _ => $@"[{Refs[0]} | {Refs[1]}]"),
			(tTypeType.Cond, _ => $@"[{Refs[0]} & ...]")
		);
	}
	
	public static readonly tText cUnknownPrefix = null; // TODO
	
	private static int NextPlaceholderId = 1;
	//================================================================================
	public static tType
	Unknown(
	//================================================================================
	) {
		var Id = ""+NextPlaceholderId;
		NextPlaceholderId += 1;
		var Res = new tType {
			Type = tTypeType.Unknown,
			Id = Id
		};
		Res.Refs = new [] { Res };
		return Res;
	}
	
	//================================================================================
	public static tType
	Empty(
	//================================================================================
	) => new tType { Type = tTypeType.Empty };
	
	//================================================================================
	public static tType
	Bool(
	//================================================================================
	) => new tType { Type = tTypeType.Bool };
	
	//================================================================================
	public static tType
	Int(
	//================================================================================
	) => new tType { Type = tTypeType.Int };
	
	//================================================================================
	public static tType
	Type(
	//================================================================================
	) => new tType { Type = tTypeType.Type };
	
	//================================================================================
	public static tType
	Pair(
		tType aType1,
		tType aType2
	//================================================================================
	) => new tType {
		Type = tTypeType.Pair,
		Refs = new []{ aType1, aType2 }
	};
	
	//================================================================================
	public static tType
	Prefix(
		tText aPrefix,
		tType aType
	//================================================================================
	) => new tType {
		Type = tTypeType.Prefix,
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
		Type = tTypeType.Proc,
		Refs = new []{ aObjType, aArgType, aResType }
	};
	
	//================================================================================
	public static tType
	Prefix(
		tText aId
	//================================================================================
	) => new tType {
		Type = tTypeType.Var,
		Id = aId
	};
	
	//================================================================================
	public static tType
	Ref(
		tType aType
	//================================================================================
	) => new tType {
		Type = tTypeType.Ref,
		Refs = new []{ aType }
	};
	
	//================================================================================
	public static tType
	Var(
		tType aType
	//================================================================================
	) => new tType {
		Type = tTypeType.Var,
		Refs = new []{ aType }
	};
	
	//================================================================================
	public static tType
	Set(
		tType aType1,
		tType aType2
	//================================================================================
	) => new tType {
		Type = tTypeType.Set,
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
			Type = tTypeType.Cond,
			Refs = new [] { aType },
		};
	}
	
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
			a1.Type== tTypeType.Unknown &&
			a2.Type== tTypeType.Unknown
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
		
		if (a1.Type == tTypeType.Unknown) {
			// TODO: check aginst cycles (a1 in a2)
			var Aliases = mList.List(a1.Refs);
			while (Aliases.Match(out var Alias, out Aliases)) {
				Alias.Type = a2.Type;
				Alias.Id = a2.Id;
				Alias.Prefix = a2.Prefix;
				Alias.Refs = a2.Refs;
			}
			return;
		}
		
		if (a2.Type == tTypeType.Unknown) {
			Unify(a2, a1, aTrace);
			return;
		}
		
		#if !true
		
		if (!MyUnificator.Unify(a1, a2, out var Error)) {
			aTrace($"TypeError: '{Error.Item1}' dosn't match '{Error.Item2}'");
		}
		
		#else
		
		mDebug.AssertEq(a1.Type, a2.Type);
		mDebug.AssertEq(a1.Id, a2.Id);
		mDebug.AssertEq(a1.Prefix, a2.Prefix);
		var RefCount = a1.Refs.Length;
		mDebug.AssertEq(a2.Refs.Length, RefCount);
		while (RefCount --> 0) {
			Unify(a1.Refs[RefCount], a2.Refs[RefCount], aTrace);
		}
		
		#endif
	}
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mVM_Type),
		mTest.Test(
			"BoolBool",
			aDebugStream => {
				Unify(Bool(), Bool(), aDebugStream);
			}
		),
		mTest.Test(
			"BoolInt",
			aDebugStream => {
				mTest.AssertError(() => Unify(Bool(), Int(), aDebugStream));
			}
		)
	);
	
	#endregion
}