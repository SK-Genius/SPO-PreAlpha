//IMPORT mError.cs
//IMPORT mStd.cs

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
mResult {
	public struct
	tResultFail<t> {
		internal t _Error;
	}
	
	public struct
	tResultOK<t> {
		internal t _Value;
	}
	
	public struct
	tResult<t, tFail> {
		
		internal tBool _IsOK;
		internal t _Value;
		internal tFail _Error;
		
		public static
		implicit operator tResult<t, tFail>(
			tResultOK<t> aOK
		) => new tResult<t, tFail> {
			_IsOK = true,
			_Value = aOK._Value
		};
		
		public static
		implicit operator tResult<t, tFail>(
			tResultFail<tFail> aFail
		) => new tResult<t, tFail> {
			_IsOK = false,
			_Error = aFail._Error
		};
	}
	
	public static tResultOK<t>
	OK<t>(
		t a
	) => new tResultOK<t> {
		_Value = a
	};

	public static tResultFail<tFail>
	Fail<tFail>(
		tFail aError
	) => new tResultFail<tFail> {
		_Error = aError
	};
	
	public static tResultFail<mStd.tEmpty>
	Fail(
	) => new tResultFail<mStd.tEmpty>();

	public static tBool
	Match<t, tFail>(
		this tResult<t, tFail> a,
		out t aValue,
		out tFail aError
	) {
		aValue = a._Value;
		aError = a._Error;
		return a._IsOK;
	}
	
	public static tBool
	Match<t>(
		this tResult<t, mStd.tEmpty> a,
		out t aValue
	) {
		aValue = a._Value;
		return a._IsOK;
	}
	
	public static tResult<tOut, tError>
	ThenTry<tIn, tOut, tError>(
		this tResult<tIn, tError> a,
		mStd.tFunc<tResult<tOut, tError>, tIn> aMod
	) {
		if (a.Match(out var Value, out var Error)) {
			return aMod(Value);
		} else {
			return Fail(Error);
		}
	}
	
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> a,
		mStd.tFunc<tOut, tIn> aMod
	) {
		if (a.Match(out var Value, out var Error)) {
			return OK(aMod(Value));
		} else {
			return Fail(Error);
		}
	}
	
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> a,
		mStd.tFunc<tResultFail<tError>, tIn> aMod
	) {
		if (a.Match(out var Value, out var Error)) {
			return aMod(Value);
		} else {
			return Fail(Error);
		}
	}
	
	public static t
	Else<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<t, tError> aOnError
	) {
		return a.Match(out var Value, out var Error)
			? Value
			: aOnError(Error);
	}
	
	public static t
	ElseThrow<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<tText, tError> aModifyError
	) {
		if (a.Match(out var Value, out var Error)) {
			return Value;
		} else {
			throw mError.Error(aModifyError(Error));
		}
	}
	
	public static tResult<t, tError>
	Assert<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<tBool, t> aCond,
		tError aError
	) {
		return a.ThenTry<t, t, tError>(
			_ => {
				if (aCond(_)) {
					return OK(_);
				} else {
					return Fail(aError);
				}
			}
		);
	}
}
