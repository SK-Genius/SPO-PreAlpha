// IMPORT mStd
// IMPORT mStream
// IMPORT mMaybe
// IMPORT mError

public static class
mResult {
	public readonly struct
	tResultFail<tError> {
		internal readonly tError _Error;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tResultFail(
			tError aError
		) {
			this._Error = aError;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<tOK, tError>
		AsResult<tOK>(
		) => this;
	}
	
	public readonly struct
	tResultOK<tOK> {
		internal readonly tOK _Value;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tResultOK(
			tOK aValue
		) {
			this._Value = aValue;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<tOK, tError>
		WithErrorType<tError>(
		) => this;
	}
	
	public struct
	tResult<tOK, tError> {
		
		internal tBool _IsOK;
		internal tOK _Value;
		internal tError _Error;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static
		implicit operator tResult<tOK, tError>(
			tResultOK<tOK> aOK
		) => new() {
			_IsOK = true,
			_Value = aOK._Value
		};
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static
		implicit operator tResult<tOK, tError>(
			tOK aOK
		) => new() {
			_IsOK = true,
			_Value = aOK
		};
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static
		implicit operator tResult<tOK, tError>(
			tResultFail<tError> aFail
		) => new() {
			_IsOK = false,
			_Error = aFail._Error
		};
		
		public override readonly tText
		ToString(
		) => this.Then(
			_ => "" + _
		).Else(
			_ => $"Error: {_}"
		);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResultOK<tOK>
	OK<tOK>(
		tOK a
	) => new(a);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResultFail<tError>
	Fail<tError>(
		tError aError
	) => new(aError);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResultFail<mStd.tEmpty>
	Fail(
	) => new(mStd.cEmpty);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	IsFail<t, tError>(
		this tResult<t, tError> aRes,
		out tResultFail<tError> aError,
		out t aValue
	) {
		if (aRes._IsOK) {
			aError = default!;
			aValue = aRes._Value;
			return false;
		} else {
			aError = new(aRes._Error);
			aValue = default!;
			return true;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	Match<t, tFail>(
		this tResult<t, tFail> aRes,
		out t aValue,
		out tFail aError
	) {
		aValue = aRes._Value;
		aError = aRes._Error;
		return aRes._IsOK;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	[System.Obsolete]
	public static tBool
	Is<t>(
		this tResult<t, mStd.tEmpty> aRes,
		out t aValue
	) {
		aValue = aRes._Value;
		return aRes._IsOK;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	[System.Obsolete]
	public static tRes
	Match<t, tFail, tRes>(
		this tResult<t, tFail> aRes,
		mStd.tFunc<t, tRes> aOnSuccess,
		mStd.tFunc<tFail, tRes> aOnFail
	) => (
		aRes._IsOK
		? aOnSuccess(aRes._Value)
		: aOnFail(aRes._Error)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<tOut, tError>
	ThenTry<tIn, tOut, tError>(
		this tResult<tIn, tError> aRes,
		mStd.tFunc<tIn, tResult<tOut, tError>> aMod
	) => (
		aRes.Match(out var Value, out var Error)
		? aMod(Value)
		: Fail(Error)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<tOut, tError>
	WhenAllThen<tIn, tOut, tError>(
		this mStream.tStream<tResult<tIn, tError>> aResults,
		mStd.tFunc<mStream.tStream<tIn>, tOut> aOnSucceed
	) {
		var List = mStream.Stream<tIn>([]);
		foreach (var Result in aResults) {
			if (Result.Match(out var Value, out var Error)) {
				List = mStream.Stream(Value, List);
			} else {
				return Fail(Error);
			}
		}
		return aOnSucceed(List.Reverse());
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> aRes,
		mStd.tFunc<tIn, tOut> aMod
	) => (
		aRes.Match(out var Value, out var Error)
		? aMod(Value)
		: Fail(Error)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<(t Value, tContext Context), (tError Error, tContext Context)>
	AddContext<t, tError, tContext>(
		this tResult<t, tError> a,
		tContext aContext
	) => a.Match(out var Value, out var Error)
	? OK((Value: Value, Context: aContext))
	: Fail((Error: Error, Context: aContext));
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> aRes,
		mStd.tFunc<tIn, tResultFail<tError>> aMod
	) => (
		aRes.Match(out var Value, out var Error)
		? aMod(Value)
		: Fail(Error)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<t, tError>
	ThenDo<t, tError>(
		this tResult<t, tError> aRes,
		mStd.tAction<t> aAction
	) {
		if (aRes._IsOK) {
			aAction(aRes._Value);
		}
		return aRes;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<t, tError>
	ThenAssert<t, tError>(
		this tResult<t, tError> aRes,
		mStd.tFunc<t, tBool> aCond,
		mStd.tFunc<t, tError> aOnFail
	) => aRes.ThenTry(
		[DebuggerHidden] (a) => (
			aCond(a)
			? OK(a).WithErrorType<tError>()
			: Fail(aOnFail(a))
		)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	Else<t, tError>(
		this tResult<t, tError> aRes,
		mStd.tFunc<tError, t> aOnError
	) => (
		aRes.Match(out var Value, out var Error)
		? Value
		: aOnError(Error)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<t, tError>
	ElseTry<t, tError>(
		this tResult<t, tError> aRes,
		mStd.tFunc<tError, tResult<t, tError>> aOnError
	) => (
		aRes.Match(out var Value, out var Error)
		? Value
		: aOnError(Error)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<t, tError>
	ElseFail<t, tError>(
		this mMaybe.tMaybe<t> aRes,
		mStd.tFunc<tError> aOnFail
	) => aRes.Match(
		[DebuggerHidden] (aValue) => OK(aValue).WithErrorType<tError>(),
		[DebuggerHidden] () => Fail(aOnFail())
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	ElseThrow<t, tError>(
		this tResult<t, tError> aRes,
		mStd.tFunc<tError, tText> aModifyError
	) => (
		aRes.Match(out var Value, out var Error)
		? Value
		: throw mError.Error(aModifyError(Error))
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	ElseThrow<t, tError>(
		this tResult<t, tError> aRes,
		tText aErrorMsg
	) => aRes.ElseThrow(_ => aErrorMsg);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	ElseThrow<t>(
		this tResult<t, tText> aRes
	) => aRes.ElseThrow(_ => _);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<t, tErrorOut>
	ModifyError<t, tErrorIn, tErrorOut>(
		this tResult<t, tErrorIn> aRes,
		mStd.tFunc<tErrorIn, tErrorOut> aModError
	) => aRes.Match(out var Value, out var Error)
	? OK(Value)
	: Fail(aModError(Error));
}
