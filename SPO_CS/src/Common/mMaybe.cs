﻿//IMPORT mStd.cs
//IMPORT mError.cs

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

public static class
mMaybe {
	public readonly struct
	tMaybe<t> {
		internal readonly tBool _HasValue;
		internal readonly t _Value;
		
		internal tMaybe(
			tBool aHasValue,
			t aValue
		) {
			_HasValue = aHasValue;
			_Value = aValue;
		}
		
		public static
		implicit operator tMaybe<t>(
			mStd.tEmpty _
		) => new tMaybe<t>(false, default);
		
		public static
		implicit operator tMaybe<t>(
			t aValue
		) => new tMaybe<t>(true, aValue);
	}
	
	public static tMaybe<t>
	Some<t>(
		t a
	) => a;
	
	public static tBool
	Match<t>(
		this tMaybe<t> a,
		out t aValue
	) {
		if (a._HasValue) {
			aValue = a._Value;
			return true;
		} else {
			aValue = default;
			return false;
		}
	}
	
	public static tMaybe<tOut>
	ThenTry<tIn, tOut>(
		this tMaybe<tIn> a,
		mStd.tFunc<tMaybe<tOut>, tIn> aMap
	) => (
		a.Match(out var Value)
		? aMap(Value)
		: mStd.cEmpty
	);
	
	public static t
	Else<t>(
		this tMaybe<t> a,
		mStd.tFunc<t> aDefault
	) => (
		a.Match(out var Value)
		? Value
		: aDefault()
	);
	
	public static t
	ElseThrow<t>(
		this tMaybe<t> a,
		tText aError
	) => (
		a.Match(out var Value)
		? Value
		: throw mError.Error(aError)
	);
	
	public static t
	Else<t>(
		this tMaybe<t> a,
		mStd.tLazy<t> aDefault
	) => (
		a.Match(out var Value)
		?	Value
		: aDefault.Value
	);
	
	public static tMaybe<t>
	Assert<t>(
		this tMaybe<t> a,
		mStd.tFunc<tBool, t> aCond
	) => a.ThenTry<t, t>(
		_ => {
			if (aCond(_)) {
				return _;
			} else {
				return mStd.cEmpty;
			}
		}
	);
}
