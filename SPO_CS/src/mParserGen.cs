﻿using tBool = System.Boolean;

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
	public static tParser<tPos, t, tError>
	Assert<tPos, t, t1, tError>(
		this tParser<tPos, t, tError> aParser,
		mStd.tFunc<tBool, t1> aIsValid
	//================================================================================
	) {
		var Parser = new tParser<tPos, t, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser._ParseFunc(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var ResultList_,
					out var RestStream,
					out var ErrorList
				)
			) {
				mDebug.Assert(ResultList_.Match(out t1 Item));
				if (aIsValid(Item)) {
					return mStd.OK((ResultList_, RestStream));
				} else {
					return mStd.Fail(mList.List<tError>());
				}
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser;
	}
	
	//================================================================================
	public static tParser<tPos, t, tError>
	Modify<tPos, t, tR, tError>(
		this tParser<tPos, t, tError> aParser,
		mStd.tFunc<tR, mStd.tSpan<tPos>> aModifyFunc
	//================================================================================
	) => aParser.ModifyList(
		a => ResultList(a.Span, aModifyFunc(a.Span))
	);
	
	//================================================================================
	public static tParser<tPos, t, tError>
	Modify<tPos, t, tR, t1, tError>(
		this tParser<tPos, t, tError> aParser,
		mStd.tFunc<tR, mStd.tSpan<tPos>, t1> aModifyFunc
	//================================================================================
	) => aParser.ModifyList(
		a => {
			mDebug.Assert(
				a.Match(out t1 A1),
				aParser.DebugName ?? aParser.DebugDef
			);
			return ResultList(a.Span, aModifyFunc(a.Span, A1));
		}
	);
	
	//================================================================================
	public static tParser<tPos, t, tError>
	Modify<tPos, t, tR, t1, t2, tError>(
		this tParser<tPos, t, tError> aParser,
		mStd.tFunc<tR, mStd.tSpan<tPos>, t1, t2> aModifyFunc
	//================================================================================
	) => aParser.ModifyList(
		a => {
			mDebug.Assert(
				a.Match(out t1 A1, out t2 A2),
				() => (
					$"{nameof(Modify)}: " +
					$"({typeof(t1).Name}, {typeof(t2).Name}) <- {a}    " +
					$"in parser {aParser.DebugName ?? aParser.DebugDef}"
				)
			);
			return ResultList(a.Span, aModifyFunc(a.Span, A1, A2));
		}
	);
	
	//================================================================================
	public static tParser<tPos, t, tError>
	Modify<tPos, t, tR, t1, t2, t3, tError>(
		this tParser<tPos, t, tError> aParser,
		mStd.tFunc<tR, mStd.tSpan<tPos>, t1, t2, t3> aModifyFunc
	//================================================================================
	) => aParser.ModifyList(
		a => {
			mDebug.Assert(
				a.Match(out t1 A1, out t2 A2, out t3 A3),
				() => (
					$"{nameof(Modify)}: " +
					$"({typeof(t1).Name}, {typeof(t2).Name}, {typeof(t3).Name}) <- {a}    " +
					$"in parser {aParser.DebugName ?? aParser.DebugDef}"
				)
			);
			return ResultList(a.Span, aModifyFunc(a.Span, A1, A2, A3));
		}
	);
	
	//================================================================================
	public static tParser<tPos, t, tError>
	ModifyErrors<tPos, t, tError>(
		this tParser<tPos, t, tError> aParser,
		mStd.tFunc<mList.tList<tError>, mList.tList<tError>, (mStd.tSpan<tPos>, t)> aModifyFunc
	//================================================================================
	) {
		aParser._ModifyErrorsFunc = aModifyFunc;
		return aParser;
	}
	
	//================================================================================
	public static tParser<tPos, t, tError>
	AddError<tPos, t, tError>(
		this tParser<tPos, t, tError> aParser,
		mStd.tFunc<tError, (mStd.tSpan<tPos>, t)> aCreateError
	//================================================================================
	) =>aParser.ModifyErrors(
		(aErrors, a) => mList.Concat(aErrors, mList.List(aCreateError(a)))
	);
	
	#endregion
	
	#region tResult
	
	public struct tResultList<tPos> {
		public mStd.tSpan<tPos> Span;
		public mList.tList<mStd.tAny> Value;
	}
	
	//================================================================================
	public static tResultList<tPos>
	ResultList<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => new tResultList<tPos>{
		Span = aSpan,
		Value = mList.List<mStd.tAny>()
	};
	
	//================================================================================
	public static tResultList<tPos>
	ResultList<t, tPos>(
		(mStd.tSpan<tPos>, t) a
	//================================================================================
	) => new tResultList<tPos>{
		Span = a.Item1,
		Value = mList.List(
			mStd.Any(a.Item2)
		)
	};
	
	//================================================================================
	public static tResultList<tPos>
	ResultList<t, tPos>(
		mStd.tSpan<tPos> aSpan,
		t a
	//================================================================================
	) => new tResultList<tPos>{
		Span = aSpan,
		Value = mList.List(
			mStd.Any(a)
		)
	};
	
	//================================================================================
	public static tResultList<tPos>
	ResultList<tPos, t1, t2>(
		mStd.tSpan<tPos> aSpan,
		t1 a1,
		t2 a2
	//================================================================================
	) => new tResultList<tPos>{
		Span = aSpan,
		Value = mList.List(
			mStd.Any(a1),
			mStd.Any(a2)
		)
	};
	
	//================================================================================
	public static tResultList<tPos>
	ResultList<tPos, t1, t2, t3>(
		mStd.tSpan<tPos> aSpan,
		t1 a1,
		t2 a2,
		t3 a3
	//================================================================================
	) => new tResultList<tPos>{
		Span = aSpan,
		Value = mList.List(
			mStd.Any(a1),
			mStd.Any(a2),
			mStd.Any(a3)
		)
	};
	
	//================================================================================
	public static tResultList<tPos>
	Concat<tPos>(
		tResultList<tPos> a1,
		tResultList<tPos> a2
	//================================================================================
	) => new tResultList<tPos>{Span = mStd.Merge(a1.Span, a2.Span), Value = mList.Concat(a1.Value, a2.Value)};
	
	//================================================================================
	public static tBool
	Match<tPos, t, tError>(
		this mStd.tMaybe<(tResultList<tPos>, mList.tList<(mStd.tSpan<tPos>, t)>), mList.tList<tError>> a,
		out tResultList<tPos> aResultList,
		out mList.tList<(mStd.tSpan<tPos>, t)> aRestStream,
		out mList.tList<tError> aErrorList
	//================================================================================
	) {
		if (a.Match(out var Temp, out aErrorList)) {
			(aResultList, aRestStream) = Temp;
			return true;
		}
		aResultList = default;
		aRestStream = default;
		return false;
	}
	
	//================================================================================
	public static mStd._tOK<(tResultList<tPos>, mList.tList<(mStd.tSpan<tPos>, t)>)>
	OK<tPos, t>(
		tResultList<tPos> aResultList,
		mList.tList<(mStd.tSpan<tPos>, t)> aRestStream
	//================================================================================
	) => mStd.OK((aResultList, aRestStream));
	
	//================================================================================
	public static mList.tList<t>
	Map<tPos, t>(
		this tResultList<tPos> aArgs,
		mStd.tFunc<t,  mStd.tAny> aFunc
	//================================================================================
	) {
		var List = mList.List<t>();
		var RestArgs = aArgs;
		while (RestArgs.GetHeadTail(out var Head, out RestArgs)) {
			List = mList.Concat(List, mList.List(aFunc(Head)));
		}
		return List;
	}
	
	//================================================================================
	public static tResultList<tPos>
	Reduce<tPos, tRes, tElem>(
		this tResultList<tPos> aArgs,
		tRes aInitialAgregate,
		mStd.tFunc<tRes, tRes, tElem> aAgregatorFunc
	//================================================================================
	) {
		var RestArgs = aArgs.Value;
		var Agregate = aInitialAgregate;
		while (RestArgs.Match(out var Head, out RestArgs)) {
			Agregate = aAgregatorFunc(Agregate, Head.To<tElem>());
		}
		return ResultList(aArgs.Span, Agregate);
	}
	
	//================================================================================
	public static tResultList<tPos>
	Reduce<tPos, t>(
		this tResultList<tPos> aArgs,
		mStd.tFunc<t, t, t> aAgregatorFunc
	//================================================================================
	) {
		mDebug.Assert(aArgs.Value.Match(out var Head, out var Tail));
		return ResultList(aArgs.Span, Tail.Map(mStd.To<t>).Reduce(Head.To<t>(), aAgregatorFunc));
	}
	
	//================================================================================
	public static tBool
	GetHeadTail<tPos>(
		this tResultList<tPos> aList,
		out mStd.tAny aHead,
		out tResultList<tPos> aTail
	//================================================================================
	) {
		if (
			aList.Value.Match(out aHead, out var Tail)
		) {
			aTail = new tResultList<tPos>{Span = aList.Span, Value = Tail};
			return true;
		} else {
			aTail = default;
			return false;
		}
	}
	
	//================================================================================
	public static tBool
	GetHeadTail<tPos, t>(
		this tResultList<tPos> aList,
		out t aHead,
		out tResultList<tPos> aTail
	//================================================================================
	) {
		if (
			aList.Value.Match(out var Head, out var Tail) &&
			Head.Match(out aHead)
		) {
			aTail = new tResultList<tPos>{Value = Tail};
			return true;
		} else {
			aHead = default;
			aTail = default;
			return false;
		}
	}
	
	//================================================================================
	public static tBool
	Match<tPos, t>(
		this tResultList<tPos> aList,
		out t a
	//================================================================================
	) {
		mDebug.AssertNotEq(typeof(t), typeof(mStd.tAny));
		a = default;
		var RestList = aList;
		return (
			RestList.GetHeadTail(out a, out RestList) &&
			RestList.Value is null
		);
	}
	
	//================================================================================
	public static tBool
	Match<tPos, t1, t2>(
		this tResultList<tPos> aList,
		out t1 a1,
		out t2 a2
	//================================================================================
	) {
		a1 = default;
		a2 = default;
		var RestList = aList;
		return (
			RestList.GetHeadTail(out a1, out RestList) &&
			RestList.GetHeadTail(out a2, out RestList) &&
			RestList.Value is null
		);
	}
	
	//================================================================================
	public static tBool
	Match<tPos, t1, t2, t3>(
		this tResultList<tPos> aList,
		out t1 a1,
		out t2 a2,
		out t3 a3
	//================================================================================
	) {
		a1 = default;
		a2 = default;
		a3 = default;
		var RestList = aList;
		return (
			RestList.GetHeadTail(out a1, out RestList) &&
			RestList.GetHeadTail(out a2, out RestList) &&
			RestList.GetHeadTail(out a3, out RestList) &&
			RestList.Value is null
		);
	}
	
	#endregion
	
	public sealed class tParser<tPos, t, tError> {
		internal mStd.tFunc<
			mStd.tMaybe<(tResultList<tPos>, mList.tList<(mStd.tSpan<tPos>, t)>), mList.tList<tError>>,
			mList.tList<(mStd.tSpan<tPos>, t)>,
			mStd.tAction<tText>,
			mList.tList<tParser<tPos, t, tError>>
		> _ParseFunc;
		
		internal mStd.tFunc<mList.tList<tError>, mList.tList<tError>, (mStd.tSpan<tPos>, t)> _ModifyErrorsFunc;
		
		#if DEBUG || TRACE
			public tText _DebugName = null;
			public tText _DebugDef = "";
		#endif
		
		public tText DebugName {
			get {
				#if DEBUG || TRACE
					return _DebugName;
				#else
					return null;
				#endif
			}
		}
		
		public tText DebugDef {
			get {
				#if DEBUG || TRACE
					return _DebugDef;
				#else
					return null;
				#endif
			}
		}
		
		//================================================================================
		public tParser<tPos, t, tError>
		SetDebugDef(
			params object[] aDebugNameParts
		//================================================================================
		) {
			#if DEBUG || TRACE
				var Def = "";
				var Parts = mList.List(aDebugNameParts);
				while (Parts.Match(out var Part, out Parts)) {
					Def += Part?.ToString() ?? "";
				}
				_DebugDef = Def;
			#endif
			return this;
		}
		
		//================================================================================
		public tParser<tPos, t, tError>
		SetDebugName(
			params object[] aDebugNameParts
		//================================================================================
		) {
			#if DEBUG || TRACE
				var Name = "";
				var Parts = mList.List(aDebugNameParts);
				while (Parts.Match(out object Part, out Parts)) {
					Name += Part?.ToString() ?? "";
				}
				_DebugName = Name.Replace("\n", @"\n").Replace("\r", @"\r").Replace("\t", @"\t");
			#endif
			return this;
		}
		
		//================================================================================
		public static tParser<tPos, t, tError>
		operator+(
			tParser<tPos, t, tError> aP1,
			tParser<tPos, t, tError> aP2
		//================================================================================
		) {
			var Parser = new tParser<tPos, t, tError>();
			Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
				if (
					aP1.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
					.Match(
						out var Result1,
						out var TempStream,
						out var ErrorList
					) &&
					aP2.Parse(TempStream, aDebugStream, ReferenceEquals(aStream, TempStream) ? mList.List(Parser, aPath) : null)
					.Match(
						out var Result2,
						out TempStream,
						out ErrorList
					)
				) {
					return OK(Concat(Result1, Result2), TempStream);
				} else {
					return mStd.Fail(ErrorList);
				}
			};
			return Parser.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") + (", aP2.DebugName??aP2.DebugDef, ")");
		}
		
		//================================================================================
		public static tParser<tPos, t, tError>
		operator-(
			tParser<tPos, t, tError> aP1,
			tParser<tPos, t, tError> aP2
		//================================================================================
		) => (aP1 + -aP2)
		.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") - (", aP2.DebugName??aP2.DebugDef, ")");
		
		//================================================================================
		public static tParser<tPos, t, tError>
		operator-(
			tParser<tPos, t, tError> aParser
		//================================================================================
		) => aParser.ModifyList(
			_ => ResultList<tPos>(_.Span)
		)
		.SetDebugDef("-(", aParser.DebugName??aParser.DebugDef, ")");
		
		//================================================================================
		public static tParser<tPos, t, tError>
		operator+(
			tParser<tPos, t, tError> aParser
		//================================================================================
		) => aParser;
		
		//================================================================================
		public static tParser<tPos, t, tError>
		operator*(
			tParser<tPos, t, tError> aParser,
			tInt32 aCount
		//================================================================================
		) => aCount * aParser;
		
		//================================================================================
		public static tParser<tPos, t, tError>
		operator*(
			tInt32 aCount,
			tParser<tPos, t, tError> aParser
		//================================================================================
		) => (
			(aCount == 0) ? EmptyParser<tPos, t, tError>() :
			(aCount < 0) ? -(-aCount * aParser) :
			aParser + (aCount-1)*aParser
		)
		.SetDebugDef(aCount.ToString(), " * (", aParser, ")");
		
		//================================================================================
		public static tParser<tPos, t, tError>
		operator|(
			tParser<tPos, t, tError> aP1,
			tParser<tPos, t, tError> aP2
		//================================================================================
		) {
			var Parser = new tParser<tPos, t, tError>();
			Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
				if (
					aP1.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
					.Match(
						out var Result,
						out var TempStream,
						out var ErrorList1
					) ||
					aP2.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
					.Match(
						out Result,
						out TempStream,
						out var ErrorList2
					)
				) {
					return OK(Result, TempStream);
				} else {
					return mStd.Fail(mList.Concat(ErrorList1, ErrorList2));
				}
			};
			return Parser.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") | (", aP2.DebugName??aP2.DebugDef, ")");
		}
		
		//================================================================================
		public tParser<tPos, t, tError>
		this[
			tNat32 aMin,
			tNat32? aMax
		//================================================================================
		] {
			get {
				if (aMin == 0) {
					if (aMax is null) {
						var Parser = new tParser<tPos, t, tError>();
						Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
							var Result = ResultList<tPos>(default);
							var RestStream = aStream;
							while (
								this.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
								.Match(
									out var TempResult,
									out aStream,
									out var _
								)
							) {
								Result = Concat(Result, TempResult);
								RestStream = aStream;
							}
							return OK(Result, RestStream);
						};
						return Parser.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", aMin, "..", aMax, "]");
					} else {
						var Parser = new tParser<tPos, t, tError>();
						Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
							var Result = ResultList<tPos>(default);
							var RestStream = aStream;
							var Max = aMax.Value;
							while (
								Max != 0 &&
								this.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
								.Match(
									out var TempResult,
									out aStream,
									out var _
								)
							) {
								Result = Concat(Result, TempResult);
								RestStream = aStream;
								Max -= 1;
							}
							return OK(Result, RestStream);
						};
						return Parser.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", aMin, "..", aMax, "]");
					}
				}
				if (aMax is null) {
					return ((int)aMin*this + this[0, null])
					.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", aMin, "..", aMax, "]");
				}
				if (aMin <= aMax) {
					return (int)aMin*this + this[0, aMax - aMin]
					.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", aMin, "..", aMax, "]");
				}
				throw mStd.Error("impossible");
			}
		}
		
		//================================================================================
		public static tParser<tPos, t, tError>
		operator~(
			tParser<tPos, t, tError> aParser
		//================================================================================
		) {
			var Parser = new tParser<tPos, t, tError>();
			Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
				var Result = ResultList<tPos>(default);
				var RestStream = aStream;
				while (true) {
					if (
						aParser.Parse(
							RestStream,
							aDebugStream,
							mList.List(Parser, aPath)
						).Match(
							out var TempResult,
							out var NewRestStream,
							out var ErrorList
						)
					) {
						Result = Concat(Result, TempResult);
						RestStream = NewRestStream;
						break;
					}
					if (!RestStream.Match(out var Head, out RestStream)) {
						return mStd.Fail(mList.List<tError>()); // TODO
					}
					Result = Concat(Result, ResultList(Head));
				}
				return OK(Result, RestStream);
			};
			return Parser.SetDebugDef("~(", aParser.DebugName??aParser.DebugDef, ")");
		}
	}
	
	//================================================================================
	public static tParser<tPos, t, tError>
	OrFail<tPos, t, tError>(
		this tParser<tPos, t, tError> aParser
	//================================================================================
	) {
		var Parser = new tParser<tPos, t, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(out var Result, out var TempStream, out var ErrorList)
			) {
				return OK(Result, TempStream);
			} else {
				throw mStd.Error("", ErrorList);
			}
		};
		return Parser;
	}
	
	//================================================================================
	public static tParser<tPos, t, tError>
	UndefParser<tPos, t, tError>(
	//================================================================================
	) => new tParser<tPos, t, tError>{_ParseFunc = null};
	
	//================================================================================
	public static void
	Def<tPos, t, tError>(
		this tParser<tPos, t, tError> a1,
		tParser<tPos, t, tError> a2
	//================================================================================
	) {
		mDebug.AssertNull(a1._ParseFunc);
		a1._ParseFunc = a2._ParseFunc;
		a1.SetDebugDef(a2.DebugDef);
	}
	
	//================================================================================
	public static mStd.tMaybe<(tResultList<tPos>, mList.tList<(mStd.tSpan<tPos>, t)>), mList.tList<tError>>
	StartParse<tPos, t, tError>(
		this tParser<tPos, t, tError> aParser,
		mList.tList<(mStd.tSpan<tPos>, t)> aStream,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		try {
			var Level = (tInt32?)0;
			return aParser.Parse(
				aStream,
				aDebugText => {
					if (aDebugText.StartsWith("}", System.StringComparison.Ordinal)) { Level -= 1; }
					aDebugStream(new tText(' ', mMath.Max(Level.Value, 0)) + aDebugText);
					if (aDebugText.EndsWith("{", System.StringComparison.Ordinal)) { Level += 1; }
				},
				mList.List<tParser<tPos, t, tError>>()
			);
		} catch (mStd.tError<mList.tList<tError>> e) {
			return mStd.Fail(e.Value);
		}
	}
	
	//================================================================================
	internal static mStd.tMaybe<(tResultList<tPos>, mList.tList<(mStd.tSpan<tPos>, t)>), mList.tList<tError>>
	Parse<tPos, t, tError>(
		this tParser<tPos, t, tError> aParser,
		mList.tList<(mStd.tSpan<tPos>, t)> aStream,
		mStd.tAction<tText> aDebugStream,
		mList.tList<tParser<tPos, t, tError>> aInfinitLoopDetectionSet
	//================================================================================
	) {
		if (!aInfinitLoopDetectionSet.All(_ => !ReferenceEquals(_, aParser))) {
			#if TRACE
				aDebugStream($"!!! INFINIT LOOP !!! ({aParser._DebugName??aParser._DebugDef})");
			#endif
			return mStd.Fail(mList.List<tError>());
		}
		
		#if TRACE
			if (aParser._DebugName != null) {
				aDebugStream(aParser._DebugName+" = "+aParser._DebugDef+" -> {");
			}else if (aParser._DebugDef != "") {
				aDebugStream(aParser._DebugDef+" -> {");
			} else {
				aDebugStream("??? -> {");
			}
		#endif
		mStd.tMaybe<(tResultList<tPos>, mList.tList<(mStd.tSpan<tPos>, t)>), mList.tList<tError>> Result;
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
	public static tParser<tPos, t, tError>
	ModifyList<tPos, t, tError>(
		this tParser<tPos, t, tError> aParser,
		mStd.tFunc<tResultList<tPos>, tResultList<tPos>> aModifyFunc
	//================================================================================
	) {
		var Parser = new tParser<tPos, t, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser._ParseFunc(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(
					out var ResultList_,
					out var RestStream,
					out var ErrorList
				)
			) {
				return OK(aModifyFunc(ResultList_), RestStream);
			} else {
				return mStd.Fail(ErrorList);
			}
		};
		return Parser.SetDebugDef("{", aParser?.DebugName ?? aParser.DebugDef, "}");
	}
	
	//================================================================================
	public static tParser<tPos, t, tError>
	Flat<tPos, t, tError>(
		this tParser<tPos, t, tError> aParser
	//================================================================================
	) => aParser.ModifyList(
		aResults => ResultList(
			aResults.Span,
			aResults.Value.Reduce(
				mList.List<mStd.tAny>(),
				(a, b) => {
					if (b.Match(out mList.tList<mStd.tAny> List)) {
						return mList.Concat(a, List);
					} else {
						return mList.Concat(a, mList.List(b));
					}
				}
			)
		)
	);
	
	//================================================================================
	public static tParser<tPos, t, tError>
	AtomParser<tPos, t, tError>(
		mStd.tFunc<tBool, t> aTest,
		mStd.tFunc<tError, (mStd.tSpan<tPos>, t)> aCreateErrorFunc
	//================================================================================
	) => new tParser<tPos, t, tError>{
		_ParseFunc = (aStream, aDebugStream, aPath) => {
			if (aStream.Match(out var Head, out var Tail) && aTest(Head.Item2)) {
				return OK(ResultList(Head.Item1, Head.Item2), Tail);
			} else {
				return mStd.Fail(mList.List(aCreateErrorFunc(Head)));
			}
		},
		_ModifyErrorsFunc = (_, a) => mList.List(aCreateErrorFunc(a))
	};
	
	//================================================================================
	public static tParser<tPos, t, tError>
	EmptyParser<tPos, t, tError>(
	//================================================================================
	) => new tParser<tPos, t, tError>{
		_ParseFunc = (aStream, aDebugStream, aPath) => OK(ResultList<tPos>(default), aStream)
	};
}
