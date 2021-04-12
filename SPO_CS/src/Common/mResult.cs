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
	tResultFail<tError> {
		internal readonly tError _Error;
		
		internal tResultFail(
			tError aError
		) {
			this._Error = aError;
		}
		
		public tResult<tOK, tError>
		AsResult<tOK>(
		) => this;
	}
	
	public readonly struct
	tResultOK<tOK> {
		internal readonly tOK _Value;
		
		internal tResultOK(
			tOK aValue
		) {
			this._Value = aValue;
		}
		
		public tResult<tOK, tError>
		AsResult<tError>(
		) => this;
	}
	
	public struct
	tResult<tOK, tError> {
		
		internal tBool _IsOK;
		internal tOK _Value;
		internal tError _Error;
		
		public static
		implicit operator tResult<tOK, tError>(
			tResultOK<tOK> aOK
		) => new tResult<tOK, tError> {
			_IsOK = true,
			_Value = aOK._Value
		};
		
		public static
		implicit operator tResult<tOK, tError>(
			tResultFail<tError> aFail
		) => new tResult<tOK, tError> {
			_IsOK = false,
			_Error = aFail._Error
		};
	}
	
	public static tResultOK<tOK>
	OK<tOK>(
		tOK a
	) => new tResultOK<tOK>(a);
	
	public static tResultFail<tError>
	Fail<tError>(
		tError aError
	) => new tResultFail<tError>(aError);
	
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
	) => a.Match(out var Value, out var Error) ? aMod(Value) : Fail(Error);
	
	public static tResult<tOut, tError>
	WhenAllThen<tIn, tOut, tError>(
		this mStream.tStream<tResult<tIn, tError>>? aResults,
		mStd.tFunc<tOut, mStream.tStream<tIn>?> aOnSucceed
	) {
		var List = mStream.Stream<tIn>();
		foreach (var Result in aResults) {
			if (Result.Match(out var Value, out var Error)) {
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
		? OK(aMod(Value))
		: Fail(Error)
	);
	
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> a,
		mStd.tFunc<tResultFail<tError>, tIn> aMod
	) => (
		a.Match(out var Value, out var Error)
		? aMod(Value)
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
	) => (
		a.Match(out var Value, out var Error)
		? Value
		: aOnError(Error)
	);
	
	public static tResult<t, tError>
	ElseTry<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<tResult<t, tError>, tError> aOnError
	) => (
		a.Match(out var Value, out var Error)
		? OK(Value)
		: aOnError(Error)
	);
	
	public static tResult<t, tError>
	ElseFail<t, tError>(
		this mMaybe.tMaybe<t> a,
		mStd.tFunc<tError> aOnFail
	) => a.Match(
		Some: _ => (tResult<t, tError>)OK(_),
		None: () => Fail(aOnFail())
	);
	
	public static t
	ElseThrow<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<tText, tError> aModifyError
	) => (
		a.Match(out var Value, out var Error)
		? Value
		: throw mError.Error(aModifyError(Error))
	);
	
	public static t
	ElseThrow<t, tError>(
		this tResult<t, tError> a,
		tText aErrorMsg
	) => a.ElseThrow(_ => aErrorMsg);
	
	public static t
	ElseThrow<t>(
		this tResult<t, tText> a
	) => a.ElseThrow(_ => _);
}
