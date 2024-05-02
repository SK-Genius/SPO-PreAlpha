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
		AsResult<tError>(
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
			tResultFail<tError> aFail
		) => new() {
			_IsOK = false,
			_Error = aFail._Error
		};
		
		public override readonly string
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
	[System.Obsolete]
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
		mStd.tFunc<tRes, t> aOnSuccess,
		mStd.tFunc<tRes, tFail> aOnFail
	) {
		if (aRes._IsOK) {
			return aOnSuccess(aRes._Value);
		} else {
			return aOnFail(aRes._Error);
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	[System.Obsolete]
	public static tRes
	Match<t, tRes>(
		this tResult<t, mStd.tEmpty> aRes,
		mStd.tFunc<tRes, t> aOnSuccess,
		mStd.tFunc<tRes> aOnFail
	) {
		if (aRes._IsOK) {
			return aOnSuccess(aRes._Value);
		} else {
			return aOnFail();
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<tOut, tError>
	ThenTry<tIn, tOut, tError>(
		this tResult<tIn, tError> aRes,
		mStd.tFunc<tResult<tOut, tError>, tIn> aMod
	) => aRes.Match(out var Value, out var Error) ? aMod(Value) : Fail(Error);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> aRes,
		mStd.tFunc<tOut, tIn> aMod
	) => (
		aRes.Match(out var Value, out var Error)
		? OK(aMod(Value))
		: Fail(Error)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<tOut, tError>
	Then<tIn, tOut, tError>(
		this tResult<tIn, tError> aRes,
		mStd.tFunc<tResultFail<tError>, tIn> aMod
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
		mStd.tFunc<tBool, t> aCond,
		mStd.tFunc<tError, t> aOnFail
	) => aRes.ThenTry(
		[DebuggerHidden](a) => aCond(a) ? (tResult<t, tError>)OK(a) : Fail(aOnFail(a))
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	Else<t, tError>(
		this tResult<t, tError> aRes,
		mStd.tFunc<t, tError> aOnError
	) => (
		aRes.Match(out var Value, out var Error)
		? Value
		: aOnError(Error)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<t, tError>
	ElseTry<t, tError>(
		this tResult<t, tError> aRes,
		mStd.tFunc<tResult<t, tError>, tError> aOnError
	) => (
		aRes.Match(out var Value, out var Error)
		? OK(Value)
		: aOnError(Error)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<t, tError>
	ElseFail<t, tError>(
		this mMaybe.tMaybe<t> aRes,
		mStd.tFunc<tError> aOnFail
	) => aRes.Match(
		aOnSome: [DebuggerHidden](aValue) => (tResult<t, tError>)OK(aValue),
		aOnNone: [DebuggerHidden]() => Fail(aOnFail())
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	ElseThrow<t, tError>(
		this tResult<t, tError> aRes,
		mStd.tFunc<tText, tError> aModifyError
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
}
