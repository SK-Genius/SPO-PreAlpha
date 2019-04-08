//IMPORT mStd.cs
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
	public struct
	tMaybe<t> {
		internal tBool _HasValue;
		internal t _Value;

		public static
		implicit operator tMaybe<t>(
			mStd.tEmpty _
		) {
			return new tMaybe<t> {
				_HasValue = false,
				_Value = default,
			};
		}

		public static
		implicit operator tMaybe<t>(
			t aValue
		) {
			return new tMaybe<t> {
				_HasValue = true,
				_Value = aValue,
			};
		}
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
	Then<tIn, tOut>(
		this tMaybe<tIn> a,
		mStd.tFunc<tMaybe<tOut>, tIn> aMap
	) {
		if (a.Match(out var Value)) {
			return aMap(Value);
		} else {
			return mStd.cEmpty;
		}
	}

	public static t
	Else<t>(
		this tMaybe<t> a,
		t aDefault
	) {
		if (a.Match(out var Value)) {
			return Value;
		} else {
			return aDefault;
		}
	}

	public static t
	ElseThrow<t>(
		this tMaybe<t> a,
		tText aError
	) {
		if (a.Match(out var Value)) {
			return Value;
		} else {
			throw mError.Error(aError);
		}
	}

	public static t
	Else<t>(
		this tMaybe<t> a,
		mStd.tLazy<t> aDefault
	) {
		if (a.Match(out var Value)) {
			return Value;
		} else {
			return aDefault.Value;
		}
	}

	public static tMaybe<t>
	Assert<t>(
		this tMaybe<t> a,
		mStd.tFunc<tBool, t> aCond
	) {
		return a.Then<t, t>(
			_ => {
				if (aCond(_)) {
					return _;
				} else {
					return mStd.cEmpty;
				}
			}
		);
	}
}
