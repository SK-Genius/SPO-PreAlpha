//IMPORT mError.cs
//IMPORT mStd.cs

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
mResult {
	public readonly struct
	tResultFail<t> {
		internal readonly t _Error;
		
		internal tResultFail(
			t aError
		) {
			_Error = aError;
		}
	}
	
	public readonly struct
	tResultOK<t> {
		internal readonly t _Value;
		
		internal tResultOK(
			t aValue
		) {
			_Value = aValue;
		}
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
	) => new tResultOK<t>(a);
	
	public static tResultFail<tFail>
	Fail<tFail>(
		tFail aError
	) => new tResultFail<tFail>(aError);
	
	public static tResultFail<mStd.tEmpty>
	Fail(
	) => new tResultFail<mStd.tEmpty>(mStd.cEmpty);
	
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
	) => (
		a.Match(out var Value, out var Error)
		? aMod(Value)
		: Fail(Error)
	);
	
	public static tResult<tOut, tError>
	WhenAllThen<tIn, tOut, tError>(
		this mStream.tStream<tResult<tIn, tError>>? a,
		mStd.tFunc<tOut, mStream.tStream<tIn>?> aOnSucceed
	) {
		var List = mStream.Stream<tIn>();
		while (a.Match(out var Head, out a)) {
			if (Head.Match(out var Value, out var Error)) {
				List = mStream.Stream(Value, List);
			} else {
				return Fail(Error);
			}
		}
		return OK(aOnSucceed(List.Reverse()));
	}
	
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> a,
		mStd.tFunc<tOut, tIn> aMod
	) => (
		a.Match(out var Value, out var Error)
		? (tResult<tOut, tError>)OK(aMod(Value))
		: Fail(Error)
	);
	
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> a,
		mStd.tFunc<tResultFail<tError>, tIn> aMod
	) => (
		a.Match(out var Value, out var Error)
		? (tResult<tOut, tError>)aMod(Value)
		: Fail(Error)
	);
	
	public static tResult<t, tError>
	ThenDo<t, tError>(
		this tResult<t, tError> a,
		mStd.tAction<t> aAction
	) => a.Then(
		_ => {
			aAction(_);
			return _;
		}
	);
	
	public static tResult<t, tError>
	ThenAssert<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<tBool, t> aCond,
		mStd.tFunc<tError, t> aOnFail
	) => a.ThenTry(
		_ => aCond(_) ? (tResult<t, tError>)OK(_) : Fail(aOnFail(_))
	);
	
	public static t
	Else<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<t, tError> aOnError
	) => a.Match(out var Value, out var Error)
		? Value
		: aOnError(Error);
	
	public static tResult<t, tError>
	ElseFail<t, tError>(
		this mMaybe.tMaybe<t> a,
		mStd.tFunc<tError> aOnFail
	) {
		if (a.Match(out var Value)) {
			return OK(Value);
		} else {
			return Fail(aOnFail());
		}
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
	) => a.ThenTry<t, t, tError>(
		_ => {
			if (aCond(_)) {
				return OK(_);
			} else {
				return Fail(aError);
			}
		}
	);
}
