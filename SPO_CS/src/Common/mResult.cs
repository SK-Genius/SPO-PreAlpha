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
	tResult<tOK, tFail> {
		
		internal tBool _IsOK;
		internal tOK _Value;
		internal tFail _Error;
		
		public static
		implicit operator tResult<tOK, tFail>(
			tResultOK<tOK> aOK
		) => new tResult<tOK, tFail> {
			_IsOK = true,
			_Value = aOK._Value
		};
		
		public static
		implicit operator tResult<tOK, tFail>(
			tResultFail<tFail> aFail
		) => new tResult<tOK, tFail> {
			_IsOK = false,
			_Error = aFail._Error
		};
	}
	
	public static tResultOK<tOK>
	OK<tOK>(
		tOK a
	) => new tResultOK<tOK> {
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
	Match<tOK, tFail>(
		this tResult<tOK, tFail> a,
		out tOK aValue,
		out tFail aError
	) {
		aValue = a._Value;
		aError = a._Error;
		return a._IsOK;
	}
	
	public static tBool
	Match<tOK>(
		this tResult<tOK, mStd.tEmpty> a,
		out tOK aValue
	) {
		aValue = a._Value;
		return a._IsOK;
	}
	
	public static tResult<tOK_Out, tError>
	Then<tOK_In, tOK_Out, tError>(
		this tResult<tOK_In, tError> a,
		mStd.tFunc<tResult<tOK_Out, tError>, tOK_In> aMod
	) {
		if (a.Match(out var Value, out var Error)) {
			return aMod(Value);
		} else {
			return Fail(Error);
		}
	}
	
	public static tResult<tOK_Out, tError>
	Then<tOK_In, tOK_Out, tError>(
		this tResult<tOK_In, tError> a,
		mStd.tFunc<tResultOK<tOK_Out>, tOK_In> aMod
	) {
		if (a.Match(out var Value, out var Error)) {
			return aMod(Value);
		} else {
			return Fail(Error);
		}
	}
	
	public static tResult<tOK_Out, tError>
	Then<tOK_In, tOK_Out, tError>(
		this tResult<tOK_In, tError> a,
		mStd.tFunc<tResultFail<tError>, tOK_In> aMod
	) {
		if (a.Match(out var Value, out var Error)) {
			return aMod(Value);
		} else {
			return Fail(Error);
		}
	}
	
	public static tOK
	Else<tOK, tError>(
		this tResult<tOK, tError> a,
		mStd.tFunc<tOK, tError> aOnError
	) {
		return a.Match(out var Value, out var Error)
			? Value
			: aOnError(Error);
	}
	
	public static tOK
	ElseThrow<tOK, tError>(
		this tResult<tOK, tError> a,
		mStd.tFunc<tText, tError> aModifyError
	) {
		if (a.Match(out var Value, out var Error)) {
			return Value;
		} else {
			throw mError.Error(aModifyError(Error));
		}
	}
	
	public static tResult<tOK, tError>
	Assert<tOK, tError>(
		this tResult<tOK, tError> a,
		mStd.tFunc<tBool, tOK> aCond,
		tError aError
	) {
		return a.Then<tOK, tOK, tError>(
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
