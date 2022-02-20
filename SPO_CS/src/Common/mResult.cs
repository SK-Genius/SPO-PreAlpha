//IMPORT mError.cs
//IMPORT mStd.cs

#nullable enable

[DebuggerStepThrough]
public static class
mResult {
	[DebuggerStepThrough]
	public readonly struct
	tResultFail<tError> {
		internal readonly tError _Error;
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal tResultFail(
			tError aError
		) {
			this._Error = aError;
		}
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public tResult<tOK, tError>
		AsResult<tOK>(
		) => this;
	}
	
	[DebuggerStepThrough]
	public readonly struct
	tResultOK<tOK> {
		internal readonly tOK _Value;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal tResultOK(
			tOK aValue
		) {
			this._Value = aValue;
		}
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public tResult<tOK, tError>
		AsResult<tError>(
		) => this;
	}
	
	[DebuggerStepThrough]
	[DebuggerDisplay("{_IsOK ? \"Value: \"+_Value : \"Error: \"+_Error}")]
	public struct
	tResult<tOK, tError> {
		
		internal tBool _IsOK;
		internal tOK _Value;
		internal tError _Error;
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static
		implicit operator tResult<tOK, tError>(
			tResultOK<tOK> aOK
		) => new() {
			_IsOK = true,
			_Value = aOK._Value
		};
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static
		implicit operator tResult<tOK, tError>(
			tResultFail<tError> aFail
		) => new() {
			_IsOK = false,
			_Error = aFail._Error
		};
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tResultOK<tOK>
	OK<tOK>(
		tOK a
	) => new(a);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tResultFail<tError>
	Fail<tError>(
		tError aError
	) => new(aError);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tResultFail<mStd.tEmpty>
	Fail(
	) => new(mStd.cEmpty);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tBool
	Match<t>(
		this tResult<t, mStd.tEmpty> a,
		out t aValue
	) {
		aValue = a._Value;
		return a._IsOK;
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tResult<tOut, tError>
	ThenTry<tIn, tOut, tError>(
		this tResult<tIn, tError> a,
		mStd.tFunc<tResult<tOut, tError>, tIn> aMod
	) => a.Match(out var Value, out var Error) ? aMod(Value) : Fail(Error);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> a,
		mStd.tFunc<tOut, tIn> aMod
	) => (
		a.Match(out var Value, out var Error)
		? OK(aMod(Value))
		: Fail(Error)
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> a,
		mStd.tFunc<tResultFail<tError>, tIn> aMod
	) => (
		a.Match(out var Value, out var Error)
		? aMod(Value)
		: Fail(Error)
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[DebuggerStepThrough]
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tResult<t, tError>
	ThenAssert<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<tBool, t> aCond,
		mStd.tFunc<tError, t> aOnFail
	) => a.ThenTry(
		_ => aCond(_) ? (tResult<t, tError>)OK(_) : Fail(aOnFail(_))
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	Else<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<t, tError> aOnError
	) => (
		a.Match(out var Value, out var Error)
		? Value
		: aOnError(Error)
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tResult<t, tError>
	ElseTry<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<tResult<t, tError>, tError> aOnError
	) => (
		a.Match(out var Value, out var Error)
		? OK(Value)
		: aOnError(Error)
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tResult<t, tError>
	ElseFail<t, tError>(
		this mMaybe.tMaybe<t> a,
		mStd.tFunc<tError> aOnFail
	) => a.Match(
		Some: _ => (tResult<t, tError>)OK(_),
		None: () => Fail(aOnFail())
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	ElseThrow<t, tError>(
		this tResult<t, tError> a,
		mStd.tFunc<tText, tError> aModifyError
	) => (
		a.Match(out var Value, out var Error)
		? Value
		: throw mError.Error(aModifyError(Error))
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	ElseThrow<t, tError>(
		this tResult<t, tError> a,
		tText aErrorMsg
	) => a.ElseThrow(_ => aErrorMsg);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	ElseThrow<t>(
		this tResult<t, tText> a
	) => a.ElseThrow(_ => _);
}
