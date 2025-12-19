// IMPORT mStd
// IMPORT mStream
// IMPORT mMaybe
// IMPORT mError

public static class
mResult {
	public readonly struct
	tResultFail<tError> {
		internal readonly tError _Error;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	public static tResult<t, tError>
	ElseFail<t, tError>(
		this mMaybe.tMaybe<t> aRes,
		mStd.tFunc<tError> aOnFail
	) => aRes.Match(
		[DebuggerHidden] (aValue) => OK(aValue).WithErrorType<tError>(),
		[DebuggerHidden] () => Fail(aOnFail())
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
		return OK(aOnSucceed(List.Reverse()));
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tResult<tOut, tError>
	WhenAllThenTry<tIn, tOut, tError>(
		this mStream.tStream<tResult<tIn, tError>> aResults,
		mStd.tFunc<mStream.tStream<tIn>, tResult<tOut, tError>> aOnSucceed
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
	public static t
	ElseThrow<t>(
		this tResult<t, tText> aRes
	) => aRes.AssertNotError(__ => __);
	
	extension<t, tError> (tResult<t, tError> aRes) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		IsFail(
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
		public tBool
		Match(
			[MaybeNullWhen(false)]out t aValue,
			[MaybeNullWhen(true)]out tError aError
		) {
			aValue = aRes._Value;
			aError = aRes._Error;
			return aRes._IsOK;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		[System.Obsolete]
		public tRes
		Match<tRes>(
			mStd.tFunc<t, tRes> aOnSuccess,
			mStd.tFunc<tError, tRes> aOnFail
		) => (
			aRes._IsOK
			? aOnSuccess(aRes._Value)
			: aOnFail(aRes._Error)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<tOut, tError>
		ThenTry<tOut>(
			mStd.tFunc<t, tResult<tOut, tError>> aMod
		) => (
			aRes.Match(out var Value, out var Error)
			? aMod(Value)
			: Fail(Error)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<tOut, tError>
		Then<tOut>(
			mStd.tFunc<t, tOut> aMod
		) => (
			aRes.Match(out var Value, out var Error)
			? aMod(Value)
			: Fail(Error)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<(t Value, tContext Context), (tError Error, tContext Context)>
		AddContext<tContext>(
			tContext aContext
		) => aRes.Match(out var Value, out var Error)
		? OK((Value: Value, Context: aContext))
		: Fail((Error: Error, Context: aContext));
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<tOut, tError>
		Then<tOut>(
			mStd.tFunc<t, tResultFail<tError>> aMod
		) => (
			aRes.Match(out var Value, out var Error)
			? aMod(Value)
			: Fail(Error)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<t, tError>
		ThenDo(
			mStd.tAction<t> aAction
		) {
			if (aRes._IsOK) {
				aAction(aRes._Value);
			}
			return aRes;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<t, tError>
		FailIfNot(
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
		public t
		Else(
			mStd.tFunc<tError, t> aOnError
		) => (
			aRes.Match(out var Value, out var Error)
			? Value
			: aOnError(Error)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<t, tError>
		ElseTry(
			mStd.tFunc<tError, tResult<t, tError>> aOnError
		) => (
			aRes.Match(out var Value, out var Error)
			? Value
			: aOnError(Error)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public t
		AssertNotError(
			mStd.tFunc<tError, tText> aModifyError
		) => (
			aRes.Match(out var Value, out var Error)
			? Value
			: throw mError.Error(aModifyError(Error))
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public t
		AssertNotError(
			tText aErrorMsg
		) => aRes.AssertNotError(_ => aErrorMsg);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tResult<t, tErrorOut>
		ModifyError<tErrorOut>(
			mStd.tFunc<tError, tErrorOut> aModifyError
		) => aRes.Match(out var Value, out var Error)
		? OK(Value)
		: Fail(aModifyError(Error));
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tText
		ToText(
			mStd.tFunc<t, tText> aOnOK,
			mStd.tFunc<tError, tText> aOnFail
		) => aRes.Match(out var Value, out var Error)
		? aOnOK(Value)
		: aOnFail(Error);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mStd.tFunc<tResult<t, tError>, tText>
	ToText_<t, tError>(
		mStd.tFunc<t, tText> aOnOK,
		mStd.tFunc<tError, tText> aOnFail
	) => [DebuggerHidden](
		a
	) => a.Match(out var Value, out var Error)
	? aOnOK(Value)
	: aOnFail(Error);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	Eq<t, tError>(
		tResult<t, tError> a1,
		tResult<t, tError> a2,
		mStd.tFunc<t, t, tBool> aValueEq,
		mStd.tFunc<tError, tError, tBool> aErrorEq
	) => a1.Match(out var V1, out var E1)
	? (a2.Match(out var V2, out var E2) && aValueEq(V1, V2))
	: (!a2.Match(out V2, out E2) && aErrorEq(E1, E2));
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mStd.tFunc<tResult<t, tError>, tResult<t, tError>, tBool>
	Eq_<t, tError>(
		mStd.tFunc<t, t, tBool> aValueEq,
		mStd.tFunc<tError, tError, tBool> aErrorEq
	) => (a1, a2) => Eq(a1, a2, aValueEq, aErrorEq);
	
}
