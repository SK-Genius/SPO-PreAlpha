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

public static class mParserGen {
	
	#region Helper
	// TODO: seperate file ???
	
	//================================================================================
	public static tParser<t, tError>
	Modify<t, tR, tError>(
		this tParser<t, tError> aParser,
		mStd.tFunc<tR> aModifyFunc
	//================================================================================
	) => aParser.ModifyList(
		a => ResultList(aModifyFunc())
	);
	
	//================================================================================
	public static tParser<t, tError>
	Modify<t, tR, t1, tError>(
		this tParser<t, tError> aParser,
		mStd.tFunc<tR, t1> aModifyFunc
	//================================================================================
	) => aParser.ModifyList(
		a => {
			mDebug.Assert(
				a.Match(out t1 A1),
				aParser.DebugName ?? aParser.DebugDef
			);
			return ResultList(aModifyFunc(A1));
		}
	);
	
	//================================================================================
	public static tParser<t, tError>
	Assert<t, t1, tError>(
		this tParser<t, tError> aParser,
		mStd.tFunc<tBool, t1> aIsValid
	//================================================================================
	) {
		var Parser = new tParser<t, tError>();
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
	public static tParser<t, tError>
	Modify<t, tR, t1, t2, tError>(
		this tParser<t, tError> aParser,
		mStd.tFunc<tR, t1, t2> aModifyFunc
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
			return ResultList(aModifyFunc(A1, A2));
		}
	);
	
	//================================================================================
	public static tParser<t, tError>
	Modify<t, tR, t1, t2, t3, tError>(
		this tParser<t, tError> aParser,
		mStd.tFunc<tR, t1, t2, t3> aModifyFunc
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
			return ResultList(aModifyFunc(A1, A2, A3));
		}
	);
	
	//================================================================================
	public static tParser<t, tError>
	ModifyErrors<t, tError>(
		this tParser<t, tError> aParser,
		mStd.tFunc<mList.tList<tError>, mList.tList<tError>, t> aModifyFunc
	//================================================================================
	) {
		aParser._ModifyErrorsFunc = aModifyFunc;
		return aParser;
	}
	
	//================================================================================
	public static tParser<t, tError>
	AddError<t, tError>(
		this tParser<t, tError> aParser,
		mStd.tFunc<tError, t> aCreateError
	//================================================================================
	) =>aParser.ModifyErrors(
		(aErrors, a) => mList.Concat(aErrors, mList.List(aCreateError(a)))
	);
	
	#endregion
	
	#region tResult
	
	public struct tResultList {
		public mList.tList<mStd.tAny> Value;
		
		override public tText ToString() => Value?.ToString() ?? "";
	}
	
	//================================================================================
	public static tResultList
	ResultList(
	//================================================================================
	) => new tResultList{
		Value = mList.List<mStd.tAny>()
	};
	
	//================================================================================
	public static tResultList
	ResultList<t>(
		t a
	//================================================================================
	) => new tResultList{
		Value = mList.List(
			mStd.Any(a)
		)
	};
	
	//================================================================================
	public static tResultList
	ResultList<t1, t2>(
		t1 a1,
		t2 a2
	//================================================================================
	) => new tResultList{
		Value = mList.List(
			mStd.Any(a1),
			mStd.Any(a2)
		)
	};
	
	//================================================================================
	public static tResultList
	ResultList<t1, t2, t3>(
		t1 a1,
		t2 a2,
		t3 a3
	//================================================================================
	) => new tResultList{
		Value = mList.List(
			mStd.Any(a1),
			mStd.Any(a2),
			mStd.Any(a3)
		)
	};
	
	//================================================================================
	public static tResultList
	Concat(
		tResultList a1,
		tResultList a2
	//================================================================================
	) => new tResultList{Value = mList.Concat(a1.Value, a2.Value)};
	
	//================================================================================
	public static tBool
	Match<t, tError>(
		this mStd.tMaybe<(tResultList, mList.tList<t>), mList.tList<tError>> a,
		out tResultList aResultList,
		out mList.tList<t> aRestStream,
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
	public static mStd._tOK<(tResultList, mList.tList<t>)>
	OK<t>(
		tResultList aResultList,
		mList.tList<t> aRestStream
	//================================================================================
	) => mStd.OK((aResultList, aRestStream));
	
	//================================================================================
	public static mList.tList<t>
	Map<t>(
		this tResultList aArgs,
		mStd.tFunc<t, mStd.tAny> aFunc
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
	public static tResultList
	Reduce<tRes, tElem>(
		this tResultList aArgs,
		tRes aInitialAgregate,
		mStd.tFunc<tRes, tRes, tElem> aAgregatorFunc
	//================================================================================
	) {
		var RestArgs = aArgs.Value;
		var Agregate = aInitialAgregate;
		while (RestArgs.Match(out var Head, out RestArgs)) {
			Agregate = aAgregatorFunc(Agregate, Head.To<tElem>());
		}
		return ResultList(Agregate);
	}
	
	//================================================================================
	public static tBool
	GetHeadTail(
		this tResultList aList,
		out mStd.tAny aHead,
		out tResultList aTail
	//================================================================================
	) {
		if (
			aList.Value.Match(out aHead, out var Tail)
		) {
			aTail = new tResultList{Value = Tail};
			return true;
		} else {
			aTail = default;
			return false;
		}
	}
	
	//================================================================================
	public static tBool
	GetHeadTail<t>(
		this tResultList aList,
		out t aHead,
		out tResultList aTail
	//================================================================================
	) {
		if (
			aList.Value.Match(out var Head, out var Tail) &&
			Head.Match(out aHead)
		) {
			aTail = new tResultList{Value = Tail};
			return true;
		} else {
			aHead = default;
			aTail = default;
			return false;
		}
	}
	
	//================================================================================
	public static tBool
	Match<t>(
		this tResultList aList,
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
	Match<t1, t2>(
		this tResultList aList,
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
	Match<t1, t2, t3>(
		this tResultList aList,
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
	
	public sealed class tParser<t, tError> {
		internal mStd.tFunc<
			mStd.tMaybe<(tResultList, mList.tList<t>), mList.tList<tError>>,
			mList.tList<t>,
			mStd.tAction<tText>,
			mList.tList<tParser<t, tError>>
		> _ParseFunc;
		
		internal mStd.tFunc<mList.tList<tError>, mList.tList<tError>, t> _ModifyErrorsFunc;
		
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
		public tParser<t, tError>
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
		public tParser<t, tError>
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
		public static tParser<t, tError>
		operator+(
			tParser<t, tError> aP1,
			tParser<t, tError> aP2
		//================================================================================
		) {
			var Parser = new tParser<t, tError>();
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
		public static tParser<t, tError>
		operator-(
			tParser<t, tError> aP1,
			tParser<t, tError> aP2
		//================================================================================
		) => (aP1 + -aP2)
		.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") - (", aP2.DebugName??aP2.DebugDef, ")");
		
		//================================================================================
		public static tParser<t, tError>
		operator-(
			tParser<t, tError> aParser
		//================================================================================
		) => aParser.ModifyList(
			_ => ResultList()
		)
		.SetDebugDef("-(", aParser.DebugName??aParser.DebugDef, ")");
		
		//================================================================================
		public static tParser<t, tError>
		operator+(
			tParser<t, tError> aParser
		//================================================================================
		) => aParser;
		
		//================================================================================
		public static tParser<t, tError>
		operator*(
			tParser<t, tError> aParser,
			tInt32 aCount
		//================================================================================
		) => aCount * aParser;
		
		//================================================================================
		public static tParser<t, tError>
		operator*(
			tInt32 aCount,
			tParser<t, tError> aParser
		//================================================================================
		) => (
			(aCount == 0) ? EmptyParser<t, tError>() :
			(aCount < 0) ? -(-aCount * aParser) :
			aParser + (aCount-1)*aParser
		)
		.SetDebugDef(aCount.ToString(), " * (", aParser, ")");
		
		//================================================================================
		public static tParser<t, tError>
		operator|(
			tParser<t, tError> aP1,
			tParser<t, tError> aP2
		//================================================================================
		) {
			var Parser = new tParser<t, tError>();
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
		public tParser<t, tError>
		this[
			tNat32 aMin,
			tNat32? aMax
		//================================================================================
		] {
			get {
				if (aMin == 0) {
					if (aMax is null) {
						var Parser = new tParser<t, tError>();
						Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
							var Result = ResultList();
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
						var Parser = new tParser<t, tError>();
						Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
							var Result = ResultList();
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
		public static tParser<t, tError>
		operator ~(
			tParser<t, tError> aParser
		//================================================================================
		) {
			var Parser = new tParser<t, tError>();
			Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
				var Result = ResultList();
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
	public static tParser<t, tError>
	OrFail<t, tError>(
		this tParser<t, tError> aParser
	//================================================================================
	) {
		var Parser = new tParser<t, tError>();
		Parser._ParseFunc = (aStream, aDebugStream, aPath) => {
			if (
				aParser.Parse(aStream, aDebugStream, mList.List(Parser, aPath))
				.Match(out var Result, out var TempStream, out var ErrorList)
			) {
				return OK(Result, TempStream);
			} else {
				throw mStd.Error(ErrorList);
			}
		};
		return Parser;
	}
	
	//================================================================================
	public static tParser<t, tError>
	UndefParser<t, tError>(
	//================================================================================
	) => new tParser<t, tError>{_ParseFunc = null};
	
	//================================================================================
	public static void
	Def<t, tError>(
		this tParser<t, tError> a1,
		tParser<t, tError> a2
	//================================================================================
	) {
		mDebug.AssertNull(a1._ParseFunc);
		a1._ParseFunc = a2._ParseFunc;
		a1.SetDebugDef(a2.DebugDef);
	}
	
	//================================================================================
	public static mStd.tMaybe<(tResultList, mList.tList<t>), mList.tList<tError>>
	StartParse<t, tError>(
		this tParser<t, tError> aParser,
		mList.tList<t> aStream,
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
				mList.List<tParser<t, tError>>()
			);
		} catch (mStd.tError<mList.tList<tError>> e) {
			return mStd.Fail(e.Value);
		}
	}
	
	//================================================================================
	private static mStd.tMaybe<(tResultList, mList.tList<t>), mList.tList<tError>>
	Parse<t, tError>(
		this tParser<t, tError> aParser,
		mList.tList<t> aStream,
		mStd.tAction<tText> aDebugStream,
		mList.tList<tParser<t, tError>> aInfinitLoopDetectionSet
	//================================================================================
	) {
		if (!aInfinitLoopDetectionSet.Where(_ => ReferenceEquals(_, aParser)).IsEmpty()) {
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
		mStd.tMaybe<(tResultList, mList.tList<t>), mList.tList<tError>> Result;
		var Head = aStream is null ? default : aStream.First();
		try {
			Result = aParser._ParseFunc(aStream, aDebugStream, aInfinitLoopDetectionSet);
		} catch (mStd.tError<mList.tList<tError>> e) {
			throw mStd.Error(
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
			if (Result._IsOK) {
				aDebugStream($"}} -> OK{(tText.IsNullOrWhiteSpace(aParser._DebugName) ? "" : $" : {aParser._DebugName}")}");
			} else {
				aDebugStream("} -> FAIL");
			}
		#endif
		return Result;
	}
	
	//================================================================================
	public static tParser<t, tError>
	ModifyList<t, tError>(
		this tParser<t, tError> aParser,
		mStd.tFunc<tResultList, tResultList> aModifyFunc
	//================================================================================
	) {
		var Parser = new tParser<t, tError>();
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
	public static tParser<t, tError>
	Flat<t, tError>(
		this tParser<t, tError> aParser
	//================================================================================
	) => aParser.ModifyList(
		aResults => ResultList(
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
	public static tParser<t, tError>
	AtomParser<t, tError>(
		mStd.tFunc<tBool, t> aTest,
		mStd.tFunc<tError, t> aCreateErrorFunc
	//================================================================================
	) => new tParser<t, tError>{
		_ParseFunc = (aStream, aDebugStream, aPath) => {
			if (aStream.Match(out var Head, out var Tail) && aTest(Head)) {
				return OK(ResultList(Head), Tail);
			} else {
				return mStd.Fail(mList.List(aCreateErrorFunc(Head)));
			}
		},
		_ModifyErrorsFunc = (_, a) => mList.List(aCreateErrorFunc(a))
	};
	
	//================================================================================
	public static tParser<t, tError>
	EmptyParser<t, tError>(
	//================================================================================
	) => new tParser<t, tError>{
		_ParseFunc = (aStream, aDebugStream, aPath) => OK(ResultList(), aStream)
	};
	
	#region Test
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mParserGen),
		mTest.Test(
			"AtomParser",
			aDebugStream => {
				var A = AtomParser((tChar a) => a == 'A', _ => "miss A");
				mTest.Assert(A.StartParse(mList.List('A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList('A'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.AssertNot(A.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A.StartParse(mList.List('B', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...+...",
			aDebugStream => {
				var A = AtomParser((tChar a) => a == 'A', _ => "miss A");
				var B = AtomParser((tChar a) => a == 'B', _ => "miss B");
				var AB = A + B;
				mTest.Assert(AB.StartParse(mList.List('A', 'B', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'B'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.AssertNot(AB.Parse(mList.List<tChar>(), aDebugStream, null).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(AB.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(AB.StartParse(mList.List('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...-...",
			aDebugStream => {
				var A = AtomParser((tChar a) => a == 'A', _ => "miss A");
				var B = AtomParser((tChar a) => a == 'B', _ => "miss B");
				var AB = A - B;
				mTest.Assert(AB.StartParse(mList.List('A', 'B', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList('A'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.AssertNot(AB.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(AB.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(AB.StartParse(mList.List('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"-...",
			aDebugStream => {
				var A = -AtomParser((tChar a) => a == 'A', _ => "unexpected A");
				mTest.Assert(A.StartParse(mList.List('A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList());
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.AssertNot(A.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"n*...",
			aDebugStream => {
				var A3 = 3 * AtomParser((tChar a) => a == 'A', _ => "miss A");
				mTest.Assert(A3.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'A', 'A'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.Assert(A3.StartParse(mList.List('A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'A', 'A'));
				mTest.AssertEq(RestStream, mList.List('A', '_'));
				
				mTest.AssertNot(A3.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A3.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A3.StartParse(mList.List('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A3.StartParse(mList.List('A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"-n*...",
			aDebugStream => {
				var A3 = -3 * AtomParser((tChar a) => a == 'A', _ => "miss A");
				mTest.Assert(A3.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList());
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.Assert(A3.StartParse(mList.List('A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList());
				mTest.AssertEq(RestStream, mList.List('A', '_'));
				
				mTest.AssertNot(A3.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A3.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A3.StartParse(mList.List('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A3.StartParse(mList.List('A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...*n",
			aDebugStream => {
				var A3 = AtomParser((tChar a) => a == 'A', _ => "miss A") * 3;
				mTest.Assert(A3.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'A', 'A'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.AssertNot(A3.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A3.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A3.StartParse(mList.List('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A3.StartParse(mList.List('A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...|...",
			aDebugStream => {
				var A = AtomParser((tChar a) => a == 'A', _ => "miss A");
				var B = AtomParser((tChar a) => a == 'B', _ => "miss B");
				var AB = A | B;
				mTest.Assert(AB.StartParse(mList.List('A', 'B'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList('A'));
				mTest.AssertEq(RestStream, mList.List('B'));
				
				mTest.Assert(AB.StartParse(mList.List('B', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList('B'));
				mTest.AssertEq(RestStream, mList.List('A'));
				
				mTest.AssertNot(AB.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(AB.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(AB.StartParse(mList.List('_', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...[m, n]",
			aDebugStream => {
				var A2_3 = AtomParser((tChar a) => a == 'A', _ => "miss A")[2, 3];
				mTest.Assert(A2_3.StartParse(mList.List('A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'A'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.Assert(A2_3.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'A', 'A'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.Assert(A2_3.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'A', 'A'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.Assert(A2_3.StartParse(mList.List('A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'A', 'A'));
				mTest.AssertEq(RestStream, mList.List('A', '_'));
				
				mTest.AssertNot(A2_3.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_3.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_3.StartParse(mList.List('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_3.StartParse(mList.List('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_3.StartParse(mList.List('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...[n, null]",
			aDebugStream => {
				var A2_ = AtomParser((tChar a) => a == 'A', _ => "miss A")[2, null];
				mTest.Assert(A2_.StartParse(mList.List('A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'A'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.Assert(A2_.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList('A', 'A', 'A'));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.AssertNot(A2_.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"....ModifyList(...=>...)",
			aDebugStream => {
				var A2_ = AtomParser((tChar a) => a == 'A', _ => "miss A")[2, null]
				.ModifyList(
					a => {
						var Tail = a;
						var N = 0;
						while (Tail.GetHeadTail(out char Head, out Tail)) {
							N += 1;
						}
						return ResultList(N);
					}
				);
				mTest.Assert(A2_.StartParse(mList.List('A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList(2));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.Assert(A2_.StartParse(mList.List('A', 'A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList(5));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.AssertNot(A2_.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"....ModifyList(a => a.Reduce(...))",
			aDebugStream => {
				var A2_ = AtomParser((tChar a) => a == 'A', _ => "miss A")[2, null]
				.ModifyList(a => a.Reduce(0, (int aCount, tChar aElem) => aCount + 1));
				mTest.Assert(A2_.StartParse(mList.List('A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList(2));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.Assert(A2_.StartParse(mList.List('A', 'A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList(5));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.AssertNot(A2_.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(A2_.StartParse(mList.List('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"~...",
			aDebugStream => {
				var A = AtomParser((tChar a) => a == 'A', _ => "miss A");
				var B = AtomParser((tChar a) => a == 'B', _ => "miss B");
				var AB = (A + B).ModifyList(a => ResultList("AB"));
				var nAB = ~AB;
				mTest.Assert(nAB.StartParse(mList.List('A', 'B', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mTest.AssertEq(Result, ResultList("AB"));
				mTest.AssertEq(RestStream, mList.List('_'));
				
				mTest.Assert(nAB.StartParse(mList.List('B', 'A', 'A', 'B', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertEq(Result, ResultList('B', 'A', "AB"));
				mTest.AssertEq(RestStream, mList.List('A', '_'));
				
				mTest.AssertNot(nAB.StartParse(mList.List<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(nAB.StartParse(mList.List('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(nAB.StartParse(mList.List('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(nAB.StartParse(mList.List('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mTest.AssertNot(nAB.StartParse(mList.List('A', '_', 'B', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"Eval('MathExpr')",
			aDebugStream => {
				var CharIn = mStd.Func(
					(tText aChars) => AtomParser(
						(tChar aChar) => {
							foreach (var Char in aChars) {
								if (Char == aChar) { return true; }
							}
							return false;
						},
						a => $"miss one of [{aChars}]"
					)
				);
				
				var Token = mStd.Func(
					(tText aTocken) => {
						var Parser = EmptyParser<tChar, tText>();
						foreach (var Char in aTocken) {
							Parser += AtomParser<tChar, tText>(
								(tChar aChar) => aChar == Char,
								a => $"miss {Char}"
							);
						}
						return Parser;
					}
				);
				
				var _ = -CharIn(" \t");
				var __ = -_[0, null];
				
				var P = mStd.Func((tParser<tChar, tText> aParser) => (-Token("(") -__ +aParser -__ -Token(")")));
				
				var Digit = CharIn("0123456789")
				.Modify((tChar a) => (tInt32)a - (tInt32)'0');
				
				var Nat = Digit[1, null]
				.ModifyList(aDigits => aDigits.Reduce(0, (tInt32 aNat, tInt32 aDigit) => aNat*10 + aDigit));
				
				var PosSignum = AtomParser<tChar, tText>((tChar a) => a == '+', a => $"miss +")
				.Modify((tChar a) => +1);
				
				var NegSignum = AtomParser<tChar, tText>((tChar a) => a == '-', a => "miss -")
				.Modify((tChar a) => -1);
				
				var Signum = (PosSignum | NegSignum);
				
				var Int = (+Signum +Nat)
				.Modify((tInt32 aSignum, tInt32 aNat) => aSignum * aNat);
				
				var Number = Nat | Int;
				
				var OpAdd = (-Token("+"))
				.Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 + a2));
				
				var OpSub = (-Token("-"))
				.Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 - a2));
				
				var OpMul = (-Token("*"))
				.Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 * a2));
				
				var OpDiv = (-Token("/"))
				.Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 / a2));
				
				var Op = OpAdd | OpSub | OpMul | OpDiv;
				
				var Expression = UndefParser<tChar, tText>();
				
				Expression.Def(
					Number |
					P(+Expression -__ +Op -__ +Expression)
					.Modify((tInt32 a1, mStd.tFunc<tInt32, tInt32, tInt32> aOp, tInt32 a2) => aOp(a1, a2))
				);
				
				var Eval = mStd.Func(
					(tText aExpr) => {
						var X = Expression.StartParse(
							mList.List(aExpr.ToCharArray()),
							aDebugStream
						);
						mTest.Assert(X.Match(out var Tuple, out var _));
						var (ResultList, Rest) = Tuple;
						
						mTest.Assert(ResultList.Match(out tInt32 Result));
						
						return Result;
					}
				);
				
				mTest.AssertEq(Eval("1"), 1);
				mTest.AssertEq(Eval("-2"), -2);
				mTest.AssertEq(Eval("(3+4)"), 7);
				mTest.AssertEq(Eval("( 5 - 6)"), -1);
				mTest.AssertEq(Eval("( 5 --6)"), 11);
				mTest.AssertEq(Eval("( 7 * (8 / 4))"), 14);
				mTest.AssertEq(Eval("( 7 * (8 / (4  -6)))"), -28);
			}
		)
	);
	
	#endregion
}
