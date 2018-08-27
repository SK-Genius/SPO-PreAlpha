//IMPORT mList.cs
//IMPORT mDebug.cs
//IMPORT mPerf.cs
//IMPORT mMath.cs


//#define TRACE
//#defind INF_LOOP_DETECTIOM

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

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(nameof(mParserGen) + "_Test")]

public static class mParserGen {
	
	#region Helper
	// TODO: seperate file ???
	
	//================================================================================
	public static tParser<tPos, tIn, tOut, tError>
	Assert<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<tBool, tOut> aIsValid,
		mStd.tFunc<tError, (mStd.tSpan<tPos> Span, tOut Value)> aErrorMessage
	//================================================================================
	) {
		var Parser = new tParser<tPos, tIn, tOut, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser._ParseFunc(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var Result,
					out var ErrorList
				)
			) {
				if (aIsValid(Result.Result.Value)) {
					return mStd.OK(Result);
				} else {
					return mStd.Fail(mList.List(aErrorMessage(Result.Result)));
				}
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser;
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, tOut, tNewOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<tNewOut, mStd.tSpan<tPos>> aModifyFunc
	//================================================================================
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser._ParseFunc(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var Result,
					out var ErrorList
				)
			) {
				return mStd.OK(((Result.Result.Span, aModifyFunc(Result.Result.Span)), Result.RestStream));
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser.SetDebugDef("{", aParser?.DebugName ?? aParser.DebugDef, "}");
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, tOut, tNewOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<tNewOut, mStd.tSpan<tPos>, tOut> aModifyFunc
	//================================================================================
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser._ParseFunc(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var Result,
					out var ErrorList
				)
			) {
				return mStd.OK(
					(
						(
							Result.Result.Span,
							aModifyFunc(
								Result.Result.Span,
								Result.Result.Value
							)
						),
						Result.RestStream
					)
				);
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser.SetDebugDef("{", aParser?.DebugName ?? aParser.DebugDef, "}");
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, t1, t2, tNewOut, tError>(
		this tParser<tPos, tIn, (t1 _1, t2 _2), tError> aParser,
		mStd.tFunc<tNewOut, mStd.tSpan<tPos>, t1, t2> aModifyFunc
	//================================================================================
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser._ParseFunc(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var Result,
					out var ErrorList
				)
			) {
				return mStd.OK(
					(
						(
							Result.Result.Span,
							aModifyFunc(
								Result.Result.Span,
								Result.Result.Value._1,
								Result.Result.Value._2
							)
						),
						Result.RestStream
					)
				);
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser.SetDebugDef("{", aParser?.DebugName ?? aParser.DebugDef, "}");
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, t1, t2, t3, tNewOut, tError>(
		this tParser<tPos, tIn, (t1 _1, t2 _2, t3 _3), tError> aParser,
		mStd.tFunc<tNewOut, mStd.tSpan<tPos>, t1, t2, t3> aModifyFunc
	//================================================================================
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser._ParseFunc(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var Result,
					out var ErrorList
				)
			) {
				return mStd.OK(
					(
						(
							Result.Result.Span,
							aModifyFunc(
								Result.Result.Span,
								Result.Result.Value._1,
								Result.Result.Value._2,
								Result.Result.Value._3
							)
						),
						Result.RestStream
					)
				);
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser.SetDebugDef("{", aParser?.DebugName ?? aParser.DebugDef, "}");
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, t1, t2, t3, t4, tNewOut, tError>(
		this tParser<tPos, tIn, (t1 _1, t2 _2, t3 _3, t4 _4), tError> aParser,
		mStd.tFunc<tNewOut, mStd.tSpan<tPos>, t1, t2, t3, t4> aModifyFunc
	//================================================================================
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser._ParseFunc(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var Result,
					out var ErrorList
				)
			) {
				return mStd.OK(
					(
						(
							Result.Result.Span,
							aModifyFunc(
								Result.Result.Span,
								Result.Result.Value._1,
								Result.Result.Value._2,
								Result.Result.Value._3,
								Result.Result.Value._4
							)
						),
						Result.RestStream
					)
				);
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser.SetDebugDef("{", aParser?.DebugName ?? aParser.DebugDef, "}");
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	ModifyS<tPos, tIn, t1, t2, t3, t4, t5, tNewOut, tError>(
		this tParser<tPos, tIn, (t1 _1, t2 _2, t3 _3, t4 _4, t5 _5), tError> aParser,
		mStd.tFunc<tNewOut, mStd.tSpan<tPos>, t1, t2, t3, t4, t5> aModifyFunc
	//================================================================================
	) {
		var Parser = new tParser<tPos, tIn, tNewOut, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser._ParseFunc(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var Result,
					out var ErrorList
				)
			) {
				return mStd.OK(
					(
						(
							Result.Result.Span,
							aModifyFunc(
								Result.Result.Span,
								Result.Result.Value._1,
								Result.Result.Value._2,
								Result.Result.Value._3,
								Result.Result.Value._4,
								Result.Result.Value._5
							)
						),
						Result.RestStream
					)
				);
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser.SetDebugDef("{", aParser?.DebugName ?? aParser.DebugDef, "}");
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, tNewOut, tError>(
		this tParser<tPos, tIn, mStd.tEmpty, tError> aParser,
		mStd.tFunc<tNewOut> aModifyFunc
	//================================================================================
	) => aParser.ModifyS(aSpan => aModifyFunc());
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t, tNewOut, tError>(
		this tParser<tPos, tIn, t, tError> aParser,
		mStd.tFunc<tNewOut, t> aModifyFunc
	//================================================================================
	) => aParser.ModifyS((aSpan, a) => aModifyFunc(a));
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t1, t2, tNewOut, tError>(
		this tParser<tPos, tIn, (t1, t2), tError> aParser,
		mStd.tFunc<tNewOut, t1, t2> aModifyFunc
	//================================================================================
	) => aParser.ModifyS((aSpan, a1, a2) => aModifyFunc(a1, a2));
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t1, t2, t3, tNewOut, tError>(
		this tParser<tPos, tIn, (t1, t2, t3), tError> aParser,
		mStd.tFunc<tNewOut, t1, t2, t3> aModifyFunc
	//================================================================================
	) => aParser.ModifyS((aSpan, a1, a2, a3) => aModifyFunc(a1, a2, a3));
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t1, t2, t3, t4, tNewOut, tError>(
		this tParser<tPos, tIn, (t1, t2, t3, t4), tError> aParser,
		mStd.tFunc<tNewOut, t1, t2, t3, t4> aModifyFunc
	//================================================================================
	) => aParser.ModifyS((aSpan, a1, a2, a3, a4) => aModifyFunc(a1, a2, a3, a4));
	
	//================================================================================
	public static tParser<tPos, tIn, tNewOut, tError>
	Modify<tPos, tIn, t1, t2, t3, t4, t5, tNewOut, tError>(
		this tParser<tPos, tIn, (t1, t2, t3, t4, t5), tError> aParser,
		mStd.tFunc<tNewOut, t1, t2, t3, t4, t5> aModifyFunc
	//================================================================================
	) => aParser.ModifyS((aSpan, a1, a2, a3, a4, a5) => aModifyFunc(a1, a2, a3, a4, a5));
	
	//================================================================================
	public static tParser<tPos, tIn, tOut, tError>
	ModifyErrors<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<mList.tList<tError>, mList.tList<tError>, (mStd.tSpan<tPos> Span, tIn Input)> aModifyFunc
	//================================================================================
	) {
		aParser._ModifyErrorsFunc = aModifyFunc;
		return aParser;
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tOut, tError>
	AddError<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mStd.tFunc<tError, (mStd.tSpan<tPos> Span, tIn Value)> aCreateError
	//================================================================================
	) => aParser.ModifyErrors(
		(aErrors, a) => mList.Concat(aErrors, mList.List(aCreateError(a)))
	);
	
	#endregion
	
	public sealed class tParser<tPos, tIn, tOut, tError> {
		internal mStd.tFunc<
			mStd.tMaybe<((mStd.tSpan<tPos> Span, tOut Value) Result, mList.tList<(mStd.tSpan<tPos> Span, tIn Value)> RestStream), mList.tList<tError>>,
			mList.tList<(mStd.tSpan<tPos> Span, tIn Value)>,
			mStd.tAction<tText>,
			mList.tList<object> 
		> _ParseFunc;
		
		internal mStd.tFunc<mList.tList<tError>, mList.tList<tError>, (mStd.tSpan<tPos>, tIn)> _ModifyErrorsFunc;
		
		#if DEBUG || TRACE
			public tText _DebugName = null;
			public tText _DebugDef = "";
		#endif
		
		public tText DebugName
		#if DEBUG || TRACE
			=> this._DebugName;
		#else
			=> null;
		#endif
		
		public tText DebugDef
		#if DEBUG || TRACE
			=> this._DebugDef;
		#else
			=> null;
		#endif
		
		//================================================================================
		public static tParser<tPos, tIn, mStd.tEmpty, tError>
		operator-(
			tParser<tPos, tIn, tOut, tError> aParser
		//================================================================================
		) => aParser
			.ModifyS(aSpan => mStd.cEmpty)
			.SetDebugDef("-(", aParser.DebugName??aParser.DebugDef, ")");
		
		//================================================================================
		public static tParser<tPos, tIn, tOut, tError>
		operator+(
			tParser<tPos, tIn, tOut, tError> aP1,
			tParser<tPos, tIn, mStd.tEmpty, tError> aP2
		//================================================================================
		) => aP1.__(aP2).Modify((a1, a2) => a1);
		
		//================================================================================
		public static tParser<tPos, tIn, tOut, tError>
		operator+(
			tParser<tPos, tIn, mStd.tEmpty, tError> aP1,
			tParser<tPos, tIn, tOut, tError> aP2
		//================================================================================
		) => aP1.__(aP2).Modify((a1, a2) => a2);
		
		//================================================================================
		public static tParser<tPos, tIn, (tOut, tOut), tError>
		operator+(
			tParser<tPos, tIn, tOut, tError> aP1,
			tParser<tPos, tIn, tOut, tError> aP2
		//================================================================================
		) => (aP1).__(aP2);
		
		//================================================================================
		public static tParser<tPos, tIn, mStd.tEmpty, tError>
		operator-(
			tParser<tPos, tIn, mStd.tEmpty, tError> aP1,
			tParser<tPos, tIn, tOut, tError> aP2
		//================================================================================
		) => -(aP1 + aP2);
		
		//================================================================================
		public static tParser<tPos, tIn, tOut, tError>
		operator|(
			tParser<tPos, tIn, tOut, tError> aP1,
			tParser<tPos, tIn, tOut, tError> aP2
		//================================================================================
		) {
			var Parser = new tParser<tPos, tIn, tOut, tError>();
			Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
				if (
					aP1.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
					.Match(
						out var Result,
						out var ErrorList1
					)
				) {
					return mStd.OK(Result);
				}
				
				if (
					aP2.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
					.Match(
						out Result,
						out var ErrorList2
					)
				) {
					return mStd.OK(Result);
				}
				
				return mStd.Fail(mList.Concat(ErrorList1, ErrorList2));
			};
			return Parser.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") | (", aP2.DebugName??aP2.DebugDef, ")");
		}
		
		//================================================================================
		public tParser<tPos, tIn, mList.tList<tOut>, tError>
		this[
			tNat32 aMin,
			tNat32? aMax
		//================================================================================
		] {
			get {
				var Parser = new tParser<tPos, tIn, mList.tList<tOut>, tError>();
				Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
					var Result = mList.List<tOut>();
					var RestStream = aStream;
					var Max = aMax ?? int.MaxValue;
					var Span = default(mStd.tSpan<tPos>);
					var I = 0;
					
					var LastError = mList.List<tError>();
					while (
						I < Max &&
						this.Parse(RestStream, aDebugStream, mList.List(Parser, aPath))
						.Match(
							out var TempResult,
							out LastError
						)
					) {
						Result = mList.Concat(Result, mList.List(TempResult.Result.Value));
						if (Span.Equals(default(mStd.tSpan<tPos>))) {
							Span = TempResult.Result.Span;
						}
						Span = mStd.Merge(Span, TempResult.Result.Span);
						RestStream = TempResult.RestStream;
						I += 1;
					}
					if (I < aMin) {
						return mStd.Fail(LastError);
					} else {
						return mStd.OK(((Span, Result), RestStream));
					}
				};
				return Parser.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", aMin, "..", aMax, "]");
			}
		}
		
		//================================================================================
		public static tParser<tPos, tIn, (mList.tList<tIn>, tOut), tError>
		operator~(
			tParser<tPos, tIn, tOut, tError> aParser
		//================================================================================
		) {
			var Parser = new tParser<tPos, tIn, (mList.tList<tIn>, tOut),tError>();
			Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
				var List = mList.List<tIn>();
				var RestStream = aStream;
				var Span = default(mStd.tSpan<tPos>);
				while (true) {
					if (
						aParser.Parse(
							RestStream,
							aDebugStream,
							mList.List(Parser, aPath)
						).Match(
							out var TempResult,
							out var ErrorList
						)
					) {
						return mStd.OK(((mStd.Merge(Span, TempResult.Result.Span), (List, TempResult.Result.Value)), TempResult.RestStream));
					}
					if (!RestStream.Match(out var Head, out RestStream)) {
						return mStd.Fail(mList.List<tError>()); // TODO
					}
					Span = mStd.Merge(Span, Head.Span);
					List = mList.Concat(List, mList.List(Head.Value));
				}
			};
			return Parser.SetDebugDef("~(", aParser.DebugName??aParser.DebugDef, ")");
		}
		
		//================================================================================
		public tParser<tPos, tIn, tNewOut, tError>
		Cast<tNewOut>(
		//================================================================================
		) {
			mStd.Assert(typeof(tNewOut).IsAssignableFrom(typeof(tOut)));
			return this.Modify(_ => (tNewOut)(object)_);
		}
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tOut, tError>
	SetDebugDef<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		params object[] aDebugNameParts
	//================================================================================
	) {
		#if DEBUG || TRACE
			var Def = "";
			var Parts = mList.List(aDebugNameParts);
			while (Parts.Match(out var Part, out Parts)) {
				Def += Part?.ToString() ?? "";
			}
			aParser._DebugDef = Def;
		#endif
		return aParser;
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tOut, tError>
	SetDebugName<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		params object[] aDebugNameParts
	//================================================================================
	) {
		#if DEBUG || TRACE
			var Name = "";
			var Parts = mList.List(aDebugNameParts);
			while (Parts.Match(out var Part, out Parts)) {
				Name += Part?.ToString() ?? "";
			}
			aParser._DebugName = Name.Replace("\n", @"\n").Replace("\r", @"\r").Replace("\t", @"\t");
		#endif
		return aParser;
	}
	
	//================================================================================
	public static tParser<tPos, tIn, (tOut1, tOut2), tError>
	Seq<tPos, tIn, tOut1, tOut2, tError>(
		tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2
	//================================================================================
	) => aP1.__(aP2);
	
	//================================================================================
	public static tParser<tPos, tIn, (tOut1, tOut2, tOut3), tError>
	Seq<tPos, tIn, tOut1, tOut2, tOut3, tError>(
		tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2,
		tParser<tPos, tIn, tOut3, tError> aP3
	//================================================================================
	) => aP1.__(aP2.__(aP3)).Modify((a1, a2) => (a1, a2.Item1, a2.Item2));
	
	//================================================================================
	public static tParser<tPos, tIn, (tOut1, tOut2, tOut3, tOut4), tError>
	Seq<tPos, tIn, tOut1, tOut2, tOut3, tOut4, tError>(
		tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2,
		tParser<tPos, tIn, tOut3, tError> aP3,
		tParser<tPos, tIn, tOut4, tError> aP4
	//================================================================================
	) => aP1.__(aP2.__(aP3.__(aP4))).Modify((a1, a2) => (a1, a2.Item1, a2.Item2.Item1, a2.Item2.Item2));
	
	//================================================================================
	public static tParser<tPos, tIn, (tOut1, tOut2, tOut3, tOut4, tOut5), tError>
	Seq<tPos, tIn, tOut1, tOut2, tOut3, tOut4, tOut5, tError>(
		tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2,
		tParser<tPos, tIn, tOut3, tError> aP3,
		tParser<tPos, tIn, tOut4, tError> aP4,
		tParser<tPos, tIn, tOut5, tError> aP5
	//================================================================================
	) => aP1.__(aP2.__(aP3.__(aP4.__(aP5)))).Modify((a1, a2) => (a1, a2.Item1, a2.Item2.Item1, a2.Item2.Item2.Item1, a2.Item2.Item2.Item2));
	
	//================================================================================
	internal static tParser<tPos, tIn, (tOut1, tOut2), tError>
	__<tPos, tIn, tOut1, tOut2, tError>(
		this tParser<tPos, tIn, tOut1, tError> aP1,
		tParser<tPos, tIn, tOut2, tError> aP2
	//================================================================================
	) {
		var Parser = new tParser<tPos, tIn, (tOut1, tOut2), tError>();
		
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aP1.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var Result1,
					out var ErrorList
				) &&
				aP2.Parse(Result1.RestStream, aDebugStream, ReferenceEquals(aStream, Result1.RestStream) ? mList.List(Parser, aPath) : null)
				.Match(
					out var Result2,
					out ErrorList
				)
			) {
				var ((Span1, Value1), RestStream1) = Result1;
				var ((Span2, Value2), RestStream2) = Result2;
				return mStd.OK(((mStd.Merge(Span1, Span2), (Value1, Value2)), RestStream2));
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") + (", aP2.DebugName??aP2.DebugDef, ")");
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tOut, tError>
	OneOf<tPos, tIn, tOut, tError>(
		tParser<tPos, tIn, tOut, tError> aP1,
		params tParser<tPos, tIn, tOut, tError>[] aPs
	//================================================================================
	) {
		var I = aPs.Length - 1;
		var P = aPs[I];
		while (I --> 0) {
			P = aPs[I] | P;
		}
		return aP1 | P;
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tOut, tError>
	OrFail<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser
	//================================================================================
	) {
		var Parser = new tParser<tPos, tIn, tOut, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(out var Result, out var ErrorList)
			) {
				return mStd.OK(Result);
			} else {
				throw mStd.Error("", ErrorList);
			}
		};
		return Parser;
	}
	
	//================================================================================
	public static tParser<tPos, tIn, tOut, tError>
	UndefParser<tPos, tIn, tOut, tError>(
	//================================================================================
	) => new tParser<tPos, tIn, tOut, tError>{
		_ParseFunc = null,
	};
	
	//================================================================================
	public static void
	Def<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> a1,
		tParser<tPos, tIn, tOut, tError> a2
	//================================================================================
	) {
		mDebug.AssertNull(a1._ParseFunc);
		a1._ParseFunc = a2._ParseFunc;
		a1.SetDebugDef(a2.DebugDef);
	}
	
	//================================================================================
	public static mStd.tMaybe<((mStd.tSpan<tPos> Span, tOut Value) Result, mList.tList<(mStd.tSpan<tPos> Span, tIn Value)> RestStream), mList.tList<tError>>
	StartParse<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mList.tList<(mStd.tSpan<tPos> Span, tIn Value)> aStream,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		using (mPerf.Measure()) {
			try {
				var Level = (tInt32?)0;
				return aParser.Parse(
					aStream,
					aDebugText => {
						if (aDebugText.StartsWith("}", System.StringComparison.Ordinal)) { Level -= 1; }
						aDebugStream(new tText(' ', mMath.Max(Level.Value, 0)) + aDebugText);
						if (aDebugText.EndsWith("{", System.StringComparison.Ordinal)) { Level += 1; }
					},
					mList.List<object>()
				);
			} catch (mStd.tError<mList.tList<tError>> e) {
				return mStd.Fail(e.Value);
			}
		}
	}
	
	//================================================================================
	internal static mStd.tMaybe<((mStd.tSpan<tPos> Span, tOut Value) Result, mList.tList<(mStd.tSpan<tPos>, tIn)> RestStream), mList.tList<tError>>
	Parse<tPos, tIn, tOut, tError>(
		this tParser<tPos, tIn, tOut, tError> aParser,
		mList.tList<(mStd.tSpan<tPos>, tIn)> aStream,
		mStd.tAction<tText> aDebugStream,
		mList.tList<object> aInfinitLoopDetectionSet
	//================================================================================
	) {
		#if INF_LOOP_DETECTIOM
		if (!aInfinitLoopDetectionSet.All(_ => !ReferenceEquals(_, aParser))) {
			#if TRACE
				aDebugStream($"!!! INFINIT LOOP !!! ({aParser._DebugName??aParser._DebugDef})");
			#endif
			return mStd.Fail(mList.List<tError>());
		}
		#endif
		
		#if TRACE
			if (aParser._DebugName != null) {
				aDebugStream(aParser._DebugName+" = "+aParser._DebugDef+" -> {");
			}else if (aParser._DebugDef != "") {
				aDebugStream(aParser._DebugDef+" -> {");
			} else {
				aDebugStream("??? -> {");
			}
		#endif
		mStd.tMaybe<((mStd.tSpan<tPos>, tOut), mList.tList<(mStd.tSpan<tPos>, tIn)>), mList.tList<tError>> Result;
		var Head = aStream is null ? default : aStream.First();
		try {
			Result = aParser._ParseFunc(aStream, aDebugStream, aInfinitLoopDetectionSet);
		} catch (mStd.tError<mList.tList<tError>> e) {
			throw mStd.Error(
				"",
				aParser._ModifyErrorsFunc?.Invoke(e.Value, Head) ?? e.Value
			);
		}
		if (
			!Result.Match(out var Value, out var Error) &&
			!(aParser._ModifyErrorsFunc is null)
		) {
			Result = mStd.Fail(aParser._ModifyErrorsFunc(Error, Head));
		}
		#if TRACE
			if (Result.Match(out var Results, out var _)) {
				aDebugStream($"}} -> OK{(tText.IsNullOrWhiteSpace(aParser._DebugName) ? "" : $" : {aParser._DebugName}")}");
			} else {
				aDebugStream("} -> FAIL");
			}
		#endif
		return Result;
	}
	
	//================================================================================
	public static tParser<tPos, t, t, tError>
	AtomParser<tPos, t, tError>(
		mStd.tFunc<tBool, t> aTest,
		mStd.tFunc<tError, (mStd.tSpan<tPos> Span, t Value)> aCreateErrorFunc
	//================================================================================
	) => new tParser<tPos, t, t, tError>{
		_ParseFunc = (aStream, aDebugStream, aPath) => {
			if (aStream.Match(out var Head, out var Tail) && aTest(Head.Value)) {
				return mStd.OK((Head, Tail));
			} else {
				return mStd.Fail(mList.List(aCreateErrorFunc(Head)));
			}
		},
		_ModifyErrorsFunc = (_, a) => mList.List(aCreateErrorFunc(a)),
	};
	
	//================================================================================
	public static tParser<tPos, tIn, mStd.tEmpty, tError>
	EmptyParser<tPos, tIn, tError>(
	//================================================================================
	) => new tParser<tPos, tIn, mStd.tEmpty, tError>{
		_ParseFunc = (aStream, aDebugStream, aPath) => mStd.OK(((default(mStd.tSpan<tPos>), mStd.cEmpty), aStream)),
	};
}
