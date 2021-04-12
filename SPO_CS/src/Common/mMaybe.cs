//IMPORT mStd.cs
//IMPORT mError.cs
//IMPORT mLazy.cs

#nullable enable

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
			this._HasValue = aHasValue;
			this._Value = aValue;
		}
		
		public static
		implicit operator tMaybe<t>(
			mStd.tEmpty _
		) => new tMaybe<t>(false, default!);
	}
	
	public static tMaybe<t>
	Some<t>(
		t a
	) => new tMaybe<t>(true, a);
	
	public static tMaybe<t>
	None<t>(
	) => mStd.cEmpty;
	
	public static tBool
	IsSome<t>(
		this tMaybe<t> a,
		out t aValue
	) {
		if (a._HasValue) {
			aValue = a._Value;
			return true;
		} else {
			aValue = default!;
			return false;
		}
	}
	
	public static tBool
	IsNone<t>(
		this tMaybe<t> a
	) => !a._HasValue;
	
	public static tOut
	Match<tIn, tOut>( // nice but slow
		this tMaybe<tIn> a,
		mStd.tFunc<tOut, tIn> Some,
		mStd.tFunc<tOut> None
	) => (
		a.IsSome(out var Value)
		? Some(Value)
		: None()
	);
	
	public static tMaybe<tOut>
	Then<tIn, tOut>(
		this tMaybe<tIn> a,
		mStd.tFunc<tOut, tIn> aMap
	) => a.IsSome(out var Value) ? Some(aMap(Value)) : mStd.cEmpty;
	
	public static tMaybe<tOut>
	ThenTry<tIn, tOut>(
		this tMaybe<tIn> a,
		mStd.tFunc<tMaybe<tOut>, tIn> aMap
	) => a.IsSome(out var Value) ? aMap(Value) : mStd.cEmpty;
	
	public static tOut
	Else<tOut>(
		this tMaybe<tOut> a,
		mStd.tFunc<tOut> aElse
	) => a.IsSome(out var Value) ? Value : aElse();
	
	public static tOut
	Else<tOut>(
		this tMaybe<tOut> a,
		mLazy.tLazy<tOut> aElse
	) => a.IsSome(out var Value) ? Value : aElse.Value;
	
	public static tMaybe<tOut>
	ElseTry<tOut>(
		this tMaybe<tOut> a,
		mLazy.tLazy<tMaybe<tOut>> aElse
	) => a.IsSome(out var _) ? a : aElse.Value;
	
	public static t
	ElseThrow<t>(
		this tMaybe<t> a,
		tText aError
	) => a.IsSome(out var Value) ? Value : throw mError.Error(aError);
}
