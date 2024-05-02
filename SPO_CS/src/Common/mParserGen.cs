//#define MY_TRACE
//#define INF_LOOP_DETECTION

using System;

public static class
mParserGen {
	#region Helper
	// TODO: separate file ???
	
	public readonly struct
	tParserResult<tPos, tIn, tOut, tError> {
		public readonly (mSpan.tSpan<tPos> Span, tOut Value) Result;
		public readonly mStream.tStream<(mSpan.tSpan<tPos> Span, tIn Value)>? RemainingStream;
		public readonly mStream.tStream<(tPos Pos, tError Message)>? MaybeError;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tParserResult(
			(mSpan.tSpan<tPos> Span, tOut Value) aResult,
			mStream.tStream<(mSpan.tSpan<tPos> Span, tIn Value)>? aRemainingStream,
			mStream.tStream<(tPos Pos, tError Message)>? aMaybeError
		) {
			this.Result = aResult;
			this.RemainingStream = aRemainingStream;
			this.MaybeError = aMaybeError;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tParserResult<tPos, tIn, tOut, tError>
	ParserResult<tPos, tIn, tOut, tError>(
		(mSpan.tSpan<tPos> Span, tOut Value) aResult,
		mStream.tStream<(mSpan.tSpan<tPos> Span, tIn Value)>? aRemainingStream,
		mStream.tStream<(tPos Pos, tError Message)>? aMaybeError
	) => new(
		aResult,
		aRemainingStream,
		aMaybeError
	);
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tOut, tError>
	Assert<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<tBool, tOut> aIsValid,
		mStd.tFunc<(tPos Pos, tError Message), (mSpan.tSpan<tPos> Span, tOut Value)> aErrorMessage
	) {
		var Parser = new tParser<tPos, tIn, tOut, tError>(aParser._ComparePos);
		Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => aParser._ParseFunc(
			aStream,
			aDebugStream,
			mStream.Stream(Parser, aPath)
		).ThenTry<tParserResult<tPos, tIn, tOut, tError>, tParserResult<tPos, tIn, tOut, tError>, mStream.tStream<(tPos, tError)>?>(
			[DebuggerHidden](aResult) => (
				aIsValid(aResult.Result.Value)
				? mResult.OK(aResult)
				: mResult.Fail(mStream.Stream(aErrorMessage(aResult.Result)))
			)
		);
		return Parser;
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, tOut, tNewOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<tNewOut, mSpan.tSpan<tPos>> aModifyFunc
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>(aParser._ComparePos);
		Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => aParser._ParseFunc(
			aStream,
			aDebugStream,
			mStream.Stream(Parser, aPath)
		).Then(
			[DebuggerHidden](aResult) => ParserResult(
				(
					aResult.Result.Span,
					aModifyFunc(aResult.Result.Span)
				),
				aResult.RemainingStream,
				mStream.Stream<(tPos Pos, tError Message)>()
			)
		);
		return Parser.SetDebugDef("{", aParser.DebugName ?? aParser.DebugDef, "}");
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, tOut, tNewOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<tNewOut, mSpan.tSpan<tPos>, tOut> aModifyFunc
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>(aParser._ComparePos);
		Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => aParser._ParseFunc(
			aStream,
			aDebugStream,
			mStream.Stream(Parser, aPath)
		).Then(
			[DebuggerHidden](aResult) => ParserResult(
				(
					aResult.Result.Span,
					aModifyFunc(
						aResult.Result.Span,
						aResult.Result.Value
					)
				),
				aResult.RemainingStream,
				mStream.Stream<(tPos Pos, tError Message)>()
			)
		);
		return Parser.SetDebugDef("{", aParser.DebugName ?? aParser.DebugDef, "}");
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, t1, t2, tNewOut, tError>(
		this tParser<tPos, tIn, (t1 _1, t2 _2), tError> aParser,
		mStd.tFunc<tNewOut, mSpan.tSpan<tPos>, t1, t2> aModifyFunc
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>(aParser._ComparePos);
		Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) =>aParser._ParseFunc(
			aStream,
			aDebugStream,
			mStream.Stream(Parser, aPath)
		).Then(
			[DebuggerHidden](aResult) => ParserResult(
				(
					aResult.Result.Span,
					aModifyFunc(
						aResult.Result.Span,
						aResult.Result.Value._1,
						aResult.Result.Value._2
					)
				),
				aResult.RemainingStream,
				mStream.Stream<(tPos Pos, tError Message)>()
			)
		);
		return Parser.SetDebugDef("{", aParser.DebugName ?? aParser.DebugDef, "}");
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, t1, t2, t3, tNewOut, tError>(
		this tParser<tPos, tIn, (t1 _1, t2 _2, t3 _3), tError> aParser,
		mStd.tFunc<tNewOut, mSpan.tSpan<tPos>, t1, t2, t3> aModifyFunc
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>(aParser._ComparePos);
		Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => aParser._ParseFunc(
			aStream,
			aDebugStream,
			mStream.Stream(Parser, aPath)
		).Then(
			[DebuggerHidden](aResult) => ParserResult(
				(
					aResult.Result.Span,
					aModifyFunc(
						aResult.Result.Span,
						aResult.Result.Value._1,
						aResult.Result.Value._2,
						aResult.Result.Value._3
					)
				),
				aResult.RemainingStream,
				mStream.Stream<(tPos Pos, tError Message)>()
			)
		);
		return Parser.SetDebugDef("{", aParser.DebugName ?? aParser.DebugDef, "}");
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, t1, t2, t3, t4, tNewOut, tError>(
		this tParser<tPos, tIn, (t1 _1, t2 _2, t3 _3, t4 _4), tError> aParser,
		mStd.tFunc<tNewOut, mSpan.tSpan<tPos>, t1, t2, t3, t4> aModifyFunc
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>(aParser._ComparePos);
		Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => aParser._ParseFunc(
			aStream,
			aDebugStream,
			mStream.Stream(Parser, aPath)
		).Then(
			[DebuggerHidden](aResult) => ParserResult(
				(
					aResult.Result.Span,
					aModifyFunc(
						aResult.Result.Span,
						aResult.Result.Value._1,
						aResult.Result.Value._2,
						aResult.Result.Value._3,
						aResult.Result.Value._4
					)
				),
				aResult.RemainingStream,
				mStream.Stream<(tPos Pos, tError Message)>()
			)
		);
		return Parser.SetDebugDef("{", aParser.DebugName ?? aParser.DebugDef, "}");
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, t1, t2, t3, t4, t5, tNewOut, tError>(
		this tParser<tPos, tIn, (t1 _1, t2 _2, t3 _3, t4 _4, t5 _5), tError> aParser,
		mStd.tFunc<tNewOut, mSpan.tSpan<tPos>, t1, t2, t3, t4, t5> aModifyFunc
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>(aParser._ComparePos);
		Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => aParser._ParseFunc(
			aStream,
			aDebugStream,
			mStream.Stream(Parser, aPath)
		).Then(
			[DebuggerHidden](aResult) => ParserResult(
				(
					aResult.Result.Span,
					aModifyFunc(
						aResult.Result.Span,
						aResult.Result.Value._1,
						aResult.Result.Value._2,
						aResult.Result.Value._3,
						aResult.Result.Value._4,
						aResult.Result.Value._5
					)
				),
				aResult.RemainingStream,
				mStream.Stream<(tPos Pos, tError Message)>()
			)
		);
		return Parser.SetDebugDef("{", aParser.DebugName ?? aParser.DebugDef, "}");
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, tNewOut, tError>(
		this tParser<tPos, tIn, mStd.tEmpty, tError> aParser,
		mStd.tFunc<tNewOut> aModifyFunc
	) => aParser.ModifyS([DebuggerHidden](aSpan) => aModifyFunc());
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t, tNewOut, tError>(
		this tParser<tPos, tIn, t, tError> aParser,
		mStd.tFunc<tNewOut, t> aModifyFunc
	) => aParser.ModifyS([DebuggerHidden](aSpan, a) => aModifyFunc(a));
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t1, t2, tNewOut, tError>(
		this tParser<tPos, tIn, (t1, t2), tError> aParser,
		mStd.tFunc<tNewOut, t1, t2> aModifyFunc
	) => aParser.ModifyS([DebuggerHidden](aSpan, a1, a2) => aModifyFunc(a1, a2));
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t1, t2, t3, tNewOut, tError>(
		this tParser<tPos, tIn, (t1, t2, t3), tError> aParser,
		mStd.tFunc<tNewOut, t1, t2, t3> aModifyFunc
	) => aParser.ModifyS([DebuggerHidden](aSpan, a1, a2, a3) => aModifyFunc(a1, a2, a3));
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t1, t2, t3, t4, tNewOut, tError>(
		this tParser<tPos, tIn, (t1, t2, t3, t4), tError> aParser,
		mStd.tFunc<tNewOut, t1, t2, t3, t4> aModifyFunc
	) => aParser.ModifyS([DebuggerHidden](aSpan, a1, a2, a3, a4) => aModifyFunc(a1, a2, a3, a4));
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t1, t2, t3, t4, t5, tNewOut, tError>(
		this tParser<tPos, tIn, (t1, t2, t3, t4, t5), tError> aParser,
		mStd.tFunc<tNewOut, t1, t2, t3, t4, t5> aModifyFunc
	) => aParser.ModifyS([DebuggerHidden](aSpan, a1, a2, a3, a4, a5) => aModifyFunc(a1, a2, a3, a4, a5));
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tOut, tError>
	ModifyErrors<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<mStream.tStream<(tPos Pos, tError Message)>?, mStream.tStream<(tPos Pos, tError Message)>?, (mSpan.tSpan<tPos> Span, tIn Input)> aModifyFunc
	) {
		aParser._ModifyErrorsFunc = aModifyFunc;
		
		return aParser;
	}
	
	public static tParser<tPos, tIn, tOut, tError>
	AddError<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<(tPos Pos, tError Message), (mSpan.tSpan<tPos> Span, tIn Value)> aCreateError
	) => aParser.ModifyErrors(
		[DebuggerHidden](aErrors, a) => mStream.Concat(aErrors, mStream.Stream(aCreateError(a)))
	);
	
	#endregion
	
	public sealed class
	tParser<tPos, tIn, tOut, tError> {
		internal mStream.tStream<(mSpan.tSpan<tPos> Span, tIn Value)>? _LastInput;
		internal mResult.tResult<tParserResult<tPos, tIn, tOut, tError>, mStream.tStream<(tPos Pos, tError Message)>?> _LastOutput;

		#if MY_TRACE
		internal mStream.tStream<tText>? _LastTrace;
		#endif
		
		internal mStd.tFunc<
			mResult.tResult<tParserResult<tPos, tIn, tOut, tError>, mStream.tStream<(tPos Pos, tError Message)>?>,
			mStream.tStream<(mSpan.tSpan<tPos> Span, tIn Value)>?,
			mStd.tAction<tText>,
			mStream.tStream<object>?
		> _ParseFunc;
		
		internal mStd.tFunc<mStream.tStream<(tPos Pos, tError Message)>?, mStream.tStream<(tPos Pos, tError Message)>?, (mSpan.tSpan<tPos>, tIn)>? _ModifyErrorsFunc;
		internal readonly mStd.tFunc<tInt32, tPos, tPos> _ComparePos;
		
		#if DEBUG || MY_TRACE
			public tText? _DebugName = null;
			public tText _DebugDef = "";
		#endif
		
		public tText? DebugName
		#if DEBUG || MY_TRACE
			=> this._DebugName;
		#else
			=> null;
		#endif
		
		public tText DebugDef
		#if DEBUG || MY_TRACE
			=> this._DebugDef;
		#else
			=> null;
		#endif
		
		[Pure, DebuggerHidden]
		internal
		tParser(
			mStd.tFunc<tInt32, tPos, tPos> aComparePos
		) {
			this._ComparePos = aComparePos;
			this._ParseFunc = null!;
		}
		
		[Pure, DebuggerHidden]
		public static tParser<tPos, tIn, mStd.tEmpty, tError>
		operator-(
			tParser<tPos, tIn, tOut, tError> aParser
		) => aParser
			.ModifyS(aSpan => mStd.cEmpty)
			.SetDebugDef("-(", aParser.DebugName??aParser.DebugDef, ")");
		
		[Pure, DebuggerHidden]
		public static tParser<tPos, tIn, tOut, tError>
		operator+(
			tParser<tPos, tIn, tOut, tError> aP1,
			tParser<tPos, tIn, mStd.tEmpty, tError> aP2
		) => aP1.__(aP2).Modify((a1, a2) => a1);
		
		[Pure, DebuggerHidden]
		public static tParser<tPos, tIn, tOut, tError>
		operator+(
			tParser<tPos, tIn, mStd.tEmpty, tError> aP1,
			tParser<tPos, tIn, tOut, tError> aP2
		) => aP1.__(aP2).Modify((a1, a2) => a2);
		
		[Pure, DebuggerHidden]
		public static tParser<tPos, tIn, (tOut, tOut), tError>
		operator+(
			tParser<tPos, tIn, tOut, tError> aP1,
			tParser<tPos, tIn, tOut, tError> aP2
		) => aP1.__(aP2);
		
		[Pure, DebuggerHidden]
		public static tParser<tPos, tIn, mStd.tEmpty, tError>
		operator-(
			tParser<tPos, tIn, mStd.tEmpty, tError> aP1,
			tParser<tPos, tIn, tOut, tError> aP2
		) => -(aP1 + aP2);
		
		[Pure, DebuggerHidden]
		public static tParser<tPos, tIn, tOut, tError>
		operator|(
			tParser<tPos, tIn, tOut, tError> aP1,
			tParser<tPos, tIn, tOut, tError> aP2
		) {
			var Parser = new tParser<tPos, tIn, tOut, tError>(aP1._ComparePos);
			Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => aP1.Parse(
				aStream,
				aDebugStream,
				mStream.Stream(Parser, aPath)
			).ElseTry(
				[DebuggerHidden](aErrorList1) => aP2.Parse(
					aStream,
					aDebugStream,
					mStream.Stream(Parser, aPath)
				).Then(
					[DebuggerHidden](aResult) =>ParserResult(
						aResult.Result,
						aResult.RemainingStream,
						Merge(aErrorList1, aResult.MaybeError, aP1._ComparePos)
					)
				).ElseTry(
					aErrorList2 =>mResult.Fail(Merge(aErrorList1, aErrorList2, aP1._ComparePos))
				)
			);
			return Parser.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") | (", aP2.DebugName??aP2.DebugDef, ")");
		}
		
		[Pure, DebuggerHidden]
		public tParser<tPos, tIn, mStream.tStream<tOut>?, tError>
		this[
			Range aRange
		] {
			get {
				mAssert.IsFalse(aRange.Start.IsFromEnd);
				
				var Min = aRange.Start.Value;
				var Max = aRange.End.IsFromEnd ? (int?)null : aRange.End.Value;
				var Parser = new tParser<tPos, tIn, mStream.tStream<tOut>?, tError>(this._ComparePos);
				Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => {
					var Result = mStream.Stream<tOut>();
					var RemainingStream = aStream;
					var Max_ = Max ?? int.MaxValue;
					var Span = default(mSpan.tSpan<tPos>);
					var I = 0;
					
					var LastError = mStream.Stream<(tPos Pos, tError Message)>();
					while (
						I < Max_ &&
						this.Parse(
							RemainingStream,
							aDebugStream,
							mStream.Stream(Parser, aPath)
						).Match(
							out var TempResult,
							out LastError
						)
					) {
						Result = mStream.Stream(TempResult.Result.Value, Result);
						if (Span.Equals(default(mSpan.tSpan<tPos>))) {
							Span = TempResult.Result.Span;
						}
						Span = mSpan.Merge(Span, TempResult.Result.Span);
						RemainingStream = TempResult.RemainingStream;
						I += 1;
					}
					return (
						(I < Min)
						? mResult.Fail(LastError)
						: mResult.OK(ParserResult((Span, Result.Reverse()), RemainingStream, LastError))
					);
				};
				return Parser.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", Min, "..", Max, "]");
			}
		}
		
		[Pure, DebuggerHidden]
		public static tParser<tPos, tIn, (mStream.tStream<tIn>?, tOut), tError>
		operator~(
			tParser<tPos, tIn, tOut, tError> aParser
		) {
			var Parser = new tParser<tPos, tIn, (mStream.tStream<tIn>?, tOut),tError>(aParser._ComparePos);
			Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => {
				var List = mStream.Stream<tIn>();
				var RestStream = aStream;
				var Span = default(mSpan.tSpan<tPos>);
				while (true) {
					if (
						aParser.Parse(
							RestStream,
							aDebugStream,
							mStream.Stream(Parser, aPath)
						).Match(
							out var TempResult,
							out _
						)
					) {
						return mResult.OK(
							ParserResult(
								(
									mSpan.Merge(Span, TempResult.Result.Span),
									(List, TempResult.Result.Value)
								),
								TempResult.RemainingStream,
								mStream.Stream<(tPos Pos, tError Message)>()
							)
						);
					} else if (!RestStream.Is(out var Head, out RestStream)) {
						return mResult.Fail(mStream.Stream<(tPos Pos, tError Massege)>()); // TODO
					} else {
						Span = mSpan.Merge(Span, Head.Span);
						List = mStream.Concat(List, mStream.Stream(Head.Value));
					}
				}
			};
			return Parser.SetDebugDef("~(", aParser.DebugName??aParser.DebugDef, ")");
		}
		
		[Pure, DebuggerHidden]
		public tParser<tPos, tIn, tNewOut, tError>
		Cast<tNewOut>(
		) {
			mAssert.IsTrue(typeof(tNewOut).IsAssignableFrom(typeof(tOut)));
			return this.Modify(_ => (tNewOut)(object)_);
		}
	}
	
	[DebuggerHidden]
	public static tParser<tPos, tIn, tOut, tError>
	SetDebugDef<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		params object?[] aDebugNameParts
	) {
		#if DEBUG || MY_TRACE
			var Def = "";
			foreach (var Part in mStream.Stream(aDebugNameParts)) {
				Def += Part?.ToString() ?? "";
			}
			aParser._DebugDef = Def;
		#endif
		return aParser;
	}
	
	[DebuggerHidden]
	public static tParser<tPos, tIn, tOut, tError>
	SetDebugName<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		params object[] aDebugNameParts
	) {
		#if DEBUG || MY_TRACE
			var Name = "";
			foreach (var Part in mStream.Stream(aDebugNameParts)) {
				Name += Part?.ToString() ?? "";
			}
			aParser._DebugName = Name.Replace("\n", @"\n").Replace("\r", @"\r").Replace("\t", @"\t");
		#endif
		return aParser;
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, (tOut1, tOut2), tError>
	Seq<tPos, tIn, tOut1, tOut2, tError>(
		tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2
	) => aP1.__(aP2);
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, (tOut1, tOut2, tOut3), tError>
	Seq<tPos, tIn, tOut1, tOut2, tOut3, tError>(
		tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2,
		tParser<tPos, tIn, tOut3, tError> aP3
	) => aP1.__(aP2.__(aP3)).Modify((a1, a2) => (a1, a2.Item1, a2.Item2));
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, (tOut1, tOut2, tOut3, tOut4), tError>
	Seq<tPos, tIn, tOut1, tOut2, tOut3, tOut4, tError>(
		tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2,
		tParser<tPos, tIn, tOut3, tError> aP3,
		tParser<tPos, tIn, tOut4, tError> aP4
	) => aP1.__(aP2.__(aP3.__(aP4))).Modify((a1, a2) => (a1, a2.Item1, a2.Item2.Item1, a2.Item2.Item2));
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, (tOut1, tOut2, tOut3, tOut4, tOut5), tError>
	Seq<tPos, tIn, tOut1, tOut2, tOut3, tOut4, tOut5, tError>(
		tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2,
		tParser<tPos, tIn, tOut3, tError> aP3,
		tParser<tPos, tIn, tOut4, tError> aP4,
		tParser<tPos, tIn, tOut5, tError> aP5
	) => aP1.__(aP2.__(aP3.__(aP4.__(aP5)))).Modify((a1, a2) => (a1, a2.Item1, a2.Item2.Item1, a2.Item2.Item2.Item1, a2.Item2.Item2.Item2));
	
	[Pure, DebuggerHidden]
	internal static tParser<tPos, tIn, (tOut1, tOut2), tError>
	__<tPos, tIn, tOut1, tOut2, tError>(
		this tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2
	) {
		var Parser = new tParser<tPos, tIn, (tOut1, tOut2), tError>(aP1._ComparePos);		
		Parser._ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => aP1.Parse(
			aStream,
			aDebugStream,
			mStream.Stream(Parser, aPath)
		).ThenTry(
			[DebuggerHidden](aResult1) => aP2.Parse(
				aResult1.RemainingStream,
				aDebugStream,
				ReferenceEquals(aStream, aResult1.RemainingStream)
				? mStream.Stream(Parser, aPath)
				: null
			).Then(
				[DebuggerHidden](aResult2) => ParserResult(
					(
						mSpan.Merge(aResult1.Result.Span, aResult2.Result.Span),
						(aResult1.Result.Value, aResult2.Result.Value)
					),
					aResult2.RemainingStream,
					Merge(aResult1.MaybeError, aResult2.MaybeError, aP1._ComparePos)
				)
			).ElseTry(
				[DebuggerHidden](aErrorList) => mResult.Fail(
					Merge(
						Merge(
							aResult1.MaybeError,
							aErrorList,
							aP1._ComparePos
						),
						aErrorList,
						aP1._ComparePos
					)
				)
			)
		);
		return Parser.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") + (", aP2.DebugName??aP2.DebugDef, ")");
	}
	
	[Pure, DebuggerHidden]
	public static mStream.tStream<(tPos Pos, tError Message)>?
	Merge<tPos, tError>(
		mStream.tStream<(tPos Pos, tError Message)>? a1,
		mStream.tStream<(tPos Pos, tError Message)>? a2,
		mStd.tFunc<tInt32, tPos, tPos> aComparePos
	) {
		if (a1 is null) { return a2; }
		if (a2 is null) { return a1; }
		
		// TODO: review (First vs Last ???)
		return a1.TryFirst().Match(
			aOnNone: () => a2,
			aOnSome: a1_ => a2.TryFirst().Match(
				aOnNone: () => a1,
				aOnSome: a2_ => aComparePos(a1_.Pos, a2_.Pos) switch {
					> 0 => a1,
					< 0 => a2,
					_ => a1.TryLast().Match(
						aOnNone: () => a2,
						aOnSome: _ => mStream.Stream(_, a2)
					)
				}
			)
		);
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tOut, tError>
	OneOf<tPos, tIn, tOut, tError>(
		tParser<tPos, tIn, tOut, tError> aP1,
		params tParser<tPos, tIn, tOut, tError>[] aPs
	) {
		var I = aPs.Length - 1;
		var P = aPs[I];
		while (I --> 0) {
			P = aPs[I] | P;
		}
		return aP1 | P;
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, tOut, tError>
	UndefParser<tPos, tIn, tOut, tError>(
		mStd.tFunc<tInt32, tPos, tPos> aComparePos
	) => new(aComparePos);
	
	[Pure, DebuggerHidden]
	public static void
	Def<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> a1,
		tParser<tPos, tIn, tOut, tError> a2
	) {
		mAssert.IsNull(a1._ParseFunc);
		a1._ParseFunc = a2._ParseFunc;
		a1.SetDebugDef(a2.DebugDef);
	}
	
	[Pure, DebuggerHidden]
	public static mResult.tResult<
		tParserResult<tPos, tIn, tOut, tError>,
		mStream.tStream<(tPos Pos, tError Message)>?
	>
	StartParse<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStream.tStream<(mSpan.tSpan<tPos> Span, tIn Value)>? aStream,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		using var _ = mPerf.Measure();
		var Level = (tInt32?)0;
		return aParser.Parse(
			aStream,
			aDebugText => {
				if (aDebugText.StartsWith('}')) { Level -= 1; }
				aDebugStream(() => new tText(' ', mMath.Max(Level.Value, 0)) + aDebugText);
				if (aDebugText.EndsWith('{')) { Level += 1; }
			},
			mStream.Stream<object>()
		);
	}
	
	[Pure, DebuggerHidden]
	internal static mResult.tResult<
		tParserResult<tPos, tIn, tOut, tError>,
		mStream.tStream<(tPos Pos, tError Message)>?
	>
	Parse<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStream.tStream<(mSpan.tSpan<tPos>, tIn)>? aStream,
		mStd.tAction<tText> aDebugStream,
		mStream.tStream<object>? aInfiniteLoopDetectionSet
	) {
		const bool HasToLogIfFailed = !true;
		var HasToLog = false;
		var Trace = mStream.Stream<tText>();
		void AppendToTrace(tText a) {
			#if true
			Trace = mStream.Stream(a, Trace);
			#endif
		}

#if INF_LOOP_DETECTION
		if (!aInfiniteLoopDetectionSet.All(_ => !ReferenceEquals(_, aParser))) {
#if MY_TRACE
				aDebugStream($"!!! INFINITE LOOP !!! ({aParser._DebugName??aParser._DebugDef})");
#endif
			return mResult.Fail(mList.List<tError>());
		}
#endif

#if MY_TRACE
			
			if (aParser._DebugName is not null) {
				AppendToTrace(aParser._DebugName+" = "+aParser._DebugDef+" -> {");
			} else if (aParser._DebugDef != "") {
				AppendToTrace(aParser._DebugDef+" -> {");
			} else {
				AppendToTrace("??? -> {");
			}
#endif

		if (ReferenceEquals(aParser._LastInput, aStream) && aStream is not null) {
			var Result_ = aParser._LastOutput;
			#if MY_TRACE
				if (Result_.Match(out var _, out var __)) {
					AppendToTrace($"}} -> Cached OK{(tText.IsNullOrWhiteSpace(aParser._DebugName) ? "" : $" : {aParser._DebugName}")}");
					Trace = aParser._LastTrace;
					HasToLog = true;
				} else {
					AppendToTrace("} -> Cached FAIL");
					HasToLog = HasToLogIfFailed;
				}
				if (HasToLog) {
					foreach (var Line in Trace.Reverse()) {
						aDebugStream(Line);
					}
				}
			#endif
			return Result_;
		}
		
		var Result = aParser._ParseFunc(aStream, AppendToTrace, aInfiniteLoopDetectionSet);
		if (
			!Result.Match(out var Value, out var Error) &&
			aParser._ModifyErrorsFunc is not null
		) {
			Result = mResult.Fail(aParser._ModifyErrorsFunc(Error, aStream.TryFirst().Else(default)));
		}
		
		#if MY_TRACE
			if (Result.Match(out var PResult, out var Error_)) {
				AppendToTrace($"}} -> OK{tText.IsNullOrWhiteSpace(aParser._DebugName) ? "" : $" : {aParser._DebugName}"} ({PResult.Result.Span})");
				aParser._LastTrace = Trace;
				HasToLog = true;
			} else {
				AppendToTrace($"}} -> FAIL ({Error_.Match(aOnNone: () => "", aOnAny: (_) => "")})");
				HasToLog = HasToLogIfFailed;
			}
		#endif
		
		aParser._LastInput = aStream;
		aParser._LastOutput = Result;
		
		if (HasToLog) {
			foreach (var Line in Trace.Reverse()) {
				aDebugStream(Line);
			}
		}
		return Result;
	}
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, t, t, tError>
	AtomParser<tPos, t, tError>(
		mStd.tFunc<tBool, t> aTest,
		mStd.tFunc<(tPos Pos, tError Message), (mSpan.tSpan<tPos> Span, t Value)> aCreateErrorFunc,
		mStd.tFunc<tInt32, tPos, tPos> aComparePos
	) => new(aComparePos){
		_ParseFunc = [DebuggerHidden](aStream, aDebugStream, aPath) => (
			aStream.Is(out var Head, out var Tail) && aTest(Head.Value)
			? mResult.OK(ParserResult(Head, Tail, mStream.Stream<(tPos Pos, tError Message)>()))
			: mResult.Fail(mStream.Stream(aCreateErrorFunc(Head)))
		),
		_ModifyErrorsFunc = [DebuggerHidden](_, a) => mStream.Stream(aCreateErrorFunc(a)),
	};
	
	[Pure, DebuggerHidden]
	public static tParser<tPos, tIn, mStd.tEmpty, tError>
	EmptyParser<tPos, tIn, tError>(
		mStd.tFunc<tInt32, tPos, tPos> aComparePos
	) => new(aComparePos){
		_ParseFunc = (aStream, aDebugStream, aPath) => mResult.OK(
			ParserResult(
				(
					default(mSpan.tSpan<tPos>),
					mStd.cEmpty
				),
				aStream,
				mStream.Stream<(tPos Pos, tError Message)>()
			)
		),
	};
}
