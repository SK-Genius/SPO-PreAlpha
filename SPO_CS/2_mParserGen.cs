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
	public static tParser<t>
	Modify<t, tR>(
		this tParser<t> aParser,
		mStd.tFunc<tR> aModifyFunc
	//================================================================================
	) {
		return aParser.Modify_(
			a => {
				return ResultList(aModifyFunc());
			}
		);
	}
	
	//================================================================================
	public static tParser<t>
	Modify<t, tR, t1>(
		this tParser<t> aParser,
		mStd.tFunc<tR, t1> aModifyFunc
	//================================================================================
	) {
		return aParser.Modify_(
			a => {
				t1 A1;
				a.MATCH(out A1);
				return ResultList(aModifyFunc(A1));
			}
		);
	}
	
	//================================================================================
	public static tParser<t>
	Modify<t, tR, t1, t2>(
		this tParser<t> aParser,
		mStd.tFunc<tR, t1, t2> aModifyFunc
	//================================================================================
	) {
		return aParser.Modify_(
			a => {
				t1 A1;
				t2 A2;
				a.MATCH(out A1, out A2);
				return ResultList(aModifyFunc(A1, A2));
			}
		);
	}
	
	//================================================================================
	public static tParser<t>
	Modify<t, tR, t1, t2, t3>(
		this tParser<t> aParser,
		mStd.tFunc<tR, t1, t2, t3> aModifyFunc
	//================================================================================
	) {
		return aParser.Modify_(
			a => {
				t1 A1;
				t2 A2;
				t3 A3;
				a.MATCH(out A1, out A2, out A3);
				return ResultList(aModifyFunc(A1, A2, A3));
			}
		);
	}
	
	//================================================================================
	public static tParser<t>
	Modify<t, tR, t1, t2, t3, t4>(
		this tParser<t> aParser,
		mStd.tFunc<tR, t1, t2, t3, t4> aModifyFunc
	//================================================================================
	) {
		return aParser.Modify_(
			a => {
				t1 A1;
				t2 A2;
				t3 A3;
				t4 A4;
				a.MATCH(out A1, out A2, out A3, out A4);
				return ResultList(aModifyFunc(A1, A2, A3, A4));
			}
		);
	}
	
	#endregion
	
	#region tResult
	
	public class tResultList {
		internal mList.tList<mStd.tAny> _Value;
		
		//================================================================================
		public tBool
		Equals(
			tResultList a
		//================================================================================
		) {
			return (
				!a.IsNull() &&
				(_Value.IsNull() ? a._Value.IsNull() : _Value.Equals(a._Value))
			);
		}
		
		override public tBool Equals(object a) { return Equals(a as tResultList); }
		override public tText ToString() { return _Value.ToString(); }
	}
	
	//================================================================================
	public static tResultList
	ResultList(
	//================================================================================
	) {
		return new tResultList{_Value = mList.List<mStd.tAny>() };
	}
	
	//================================================================================
	public static tResultList
	ResultList<t>(
		t a
	//================================================================================
	) {
		return new tResultList{_Value = mList.List(mStd.Any(a)) };
	}
	
	//================================================================================
	public static tResultList
	ResultList<t1, t2>(
		t1 a1,
		t2 a2
	//================================================================================
	) {
		return new tResultList{_Value = mList.List(mStd.Any(a1), mStd.Any(a2)) };
	}
	
	//================================================================================
	public static tResultList
	ResultList<t1, t2, t3>(
		t1 a1,
		t2 a2,
		t3 a3
	//================================================================================
	) {
		return new tResultList{_Value = mList.List(mStd.Any(a1), mStd.Any(a2), mStd.Any(a3)) };
	}
	
	//================================================================================
	public static tResultList
	ResultList<t1, t2, t3, t4>(
		t1 a1,
		t2 a2,
		t3 a3,
		t4 a4
	//================================================================================
	) {
		return new tResultList{_Value = mList.List(mStd.Any(a1), mStd.Any(a2), mStd.Any(a3), mStd.Any(a4)) };
	}
	
	//================================================================================
	public static tResultList
	Concat(
		tResultList a1,
		tResultList a2
	//================================================================================
	) {
		return new tResultList{_Value = mList.Concat(a1._Value, a2._Value)};
	}
	
	//================================================================================
	public static tBool MATCH<t>(
		this mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>> a,
		out tResultList aResultList,
		out mList.tList<t> aRestStream
	//================================================================================
	) {
		mStd.tTuple<tResultList, mList.tList<t>> Temp;
		if (a.MATCH(out Temp)) {
			mStd.Assert(Temp.MATCH(out aResultList, out aRestStream));
			return true;
		}
		aResultList = default(tResultList);
		aRestStream = default(mList.tList<t>);
		return false;
	}
	
	//================================================================================
	public static mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>>
	OK<t>(
		tResultList aResultList,
		mList.tList<t> aRestStream
	//================================================================================
	) {
		return mStd.OK(mStd.Tuple(aResultList, aRestStream));
	}
	
	//================================================================================
	public static mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>>
	Fail<t>(
	//================================================================================
	) {
		return mStd.Fail<mStd.tTuple<tResultList, mList.tList<t>>>();
	}
	
	//================================================================================
	public static mList.tList<t>
	Map<t>(
		this tResultList aArgs,
		mStd.tFunc<t, mStd.tAny> aFunc
	//================================================================================
	) {
		var List = mList.List<t>();
		mStd.tAny Head;
		var RestArgs = aArgs;
		while (RestArgs.GetHeadTail(out Head, out RestArgs)) {
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
		mStd.tAny Head;
		var RestArgs = aArgs._Value;
		while (RestArgs.MATCH(out Head, out RestArgs)) {
			aInitialAgregate = aAgregatorFunc(aInitialAgregate, Head.To<tElem>());
		}
		return ResultList(aInitialAgregate);
	}
	
	//================================================================================
	public static tBool
	GetHeadTail(
		this tResultList aList,
		out mStd.tAny aHead,
		out tResultList aTail
	//================================================================================
	) {
		mList.tList<mStd.tAny> Tail;
		if (
			aList._Value.MATCH(out aHead, out Tail)
		) {
			aTail = new tResultList{_Value = Tail};
			return true;
		} else {
			aTail = default(tResultList);
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
		mStd.tAny Head;
		mList.tList<mStd.tAny> Tail;
		if (
			aList._Value.MATCH(out Head, out Tail) &&
			Head.MATCH(out aHead)
		) {
			aTail = new tResultList{_Value = Tail};
			return true;
		} else {
			aHead = default(t);
			aTail = default(tResultList);
			return false;
		}
	}
	
	//================================================================================
	public static tBool
	MATCH<t>(
		this tResultList aList,
		out t a
	//================================================================================
	) {
		mStd.AssertNotEq(typeof(t), typeof(mStd.tAny));
		a = default(t);
		var RestList = aList;
		return (
			RestList.GetHeadTail(out a, out RestList) &&
			RestList._Value.IsNull()
		);
	}
	
	//================================================================================
	public static tBool
	MATCH<t1, t2>(
		this tResultList aList,
		out t1 a1,
		out t2 a2
	//================================================================================
	) {
		a1 = default(t1);
		a2 = default(t2);
		var RestList = aList;
		return (
			RestList.GetHeadTail(out a1, out RestList) &&
			RestList.GetHeadTail(out a2, out RestList) &&
			RestList._Value.IsNull()
		);
	}
	
	//================================================================================
	public static tBool
	MATCH<t1, t2, t3>(
		this tResultList aList,
		out t1 a1,
		out t2 a2,
		out t3 a3
	//================================================================================
	) {
		a1 = default(t1);
		a2 = default(t2);
		a3 = default(t3);
		var RestList = aList;
		return (
			RestList.GetHeadTail(out a1, out RestList) &&
			RestList.GetHeadTail(out a2, out RestList) &&
			RestList.GetHeadTail(out a3, out RestList) &&
			RestList._Value.IsNull()
		);
	}
	
	//================================================================================
	public static tBool
	MATCH<t1, t2, t3, t4>(
		this tResultList aList,
		out t1 a1,
		out t2 a2,
		out t3 a3,
		out t4 a4
	//================================================================================
	) {
		a1 = default(t1);
		a2 = default(t2);
		a3 = default(t3);
		a4 = default(t4);
		var RestList = aList;
		return (
			RestList.GetHeadTail(out a1, out RestList) &&
			RestList.GetHeadTail(out a2, out RestList) &&
			RestList.GetHeadTail(out a3, out RestList) &&
			RestList.GetHeadTail(out a4, out RestList) &&
			RestList._Value.IsNull()
		);
	}
	
	#endregion
	
	public class tParser<t> {
		internal mStd.tFunc<mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>>, mList.tList<t>> _ParseFunc;
		
		//================================================================================
		public static tParser<t>
		operator+(
			tParser<t> aP1,
			tParser<t> aP2
		//================================================================================
		) {
			return new tParser<t> {
				_ParseFunc = aStream => {
					mList.tList<t> TempStream;
					tResultList Result1;
					tResultList Result2;
					return (
						aP1.Parse(aStream).MATCH(out Result1, out TempStream) &&
						aP2.Parse(TempStream).MATCH(out Result2, out TempStream)
					) ? (
						OK(Concat(Result1, Result2), TempStream)
					) : (
						Fail<t>()
					);
				}
			};
		}
		
		//================================================================================
		public static tParser<t>
		operator-(
			tParser<t> aP1,
			tParser<t> aP2
		//================================================================================
		) {
			return aP1 + -aP2;
		}
		
		//================================================================================
		public static tParser<t>
		operator-(
			tParser<t> aParser
		//================================================================================
		) {
			return aParser.Modify_(_ => ResultList());
		}
		
		//================================================================================
		public static tParser<t>
		operator+(
			tParser<t> aParser
		//================================================================================
		) {
			return aParser;
		}
		
		//================================================================================
		public static tParser<t>
		operator*(
			tParser<t> aParser,
			tInt32 aCount
		//================================================================================
		) {
			return aCount * aParser;
		}
		
		//================================================================================
		public static tParser<t>
		operator*(
			tInt32 aCount,
			tParser<t> aParser
		//================================================================================
		) {
			return (
				(aCount == 0) ? EmptyParser<t>() :
				(aCount < 0) ? -(-aCount * aParser) :
				aParser + (aCount-1)*aParser
			);
		}
		
		//================================================================================
		public static tParser<t>
		operator|(
			tParser<t> aP1,
			tParser<t> aP2
		//================================================================================
		) {
			return new tParser<t> {
				_ParseFunc = aStream => {
					mList.tList<t> TempStream;
					tResultList Result;
					return (
						aP1.Parse(aStream).MATCH(out Result, out TempStream) ||
						aP2.Parse(aStream).MATCH(out Result, out TempStream)
					) ? (
						OK(Result, TempStream)
					) : (
						Fail<t>()
					);
				}
			};
		}
		
		//================================================================================
		public tParser<t>
		this[
			tNat32 aMin,
			tNat32? aMax
		//================================================================================
		] {
			get {
				if (aMin == 0) {
					if (aMax.IsNull()) {
						return new tParser<t> {
							_ParseFunc = aStream => {
								var Result = ResultList();
								var RestStream = aStream;
								tResultList TempResult;
								while (this.Parse(aStream).MATCH(out TempResult, out aStream)) {
									Result = Concat(Result, TempResult);
									RestStream = aStream;
								}
								return OK(Result, RestStream);
							}
						};
					} else {
						return new tParser<t> {
							_ParseFunc = aStream => {
								var Result = ResultList();
								var RestStream = aStream;
								var Max = aMax.Value;
								tResultList TempResult;
								while (Max != 0 && this.Parse(aStream).MATCH(out TempResult, out aStream)) {
									Result = Concat(Result, TempResult);
									RestStream = aStream;
									Max -= 1;
								}
								return OK(Result, RestStream);
							}
						};
					}
				}
				if (aMax.IsNull()) { return (int)aMin*this + this[0, null]; }
				if (aMin <= aMax) { return (int)aMin*this + this[0, aMax - aMin]; }
				throw null;
			}
		}
		
		//================================================================================
		public static tParser<t>
		operator ~(
			tParser<t> aParser
		//================================================================================
		) {
			return new tParser<t> {
				_ParseFunc = aStream => {
					var Result = ResultList();
					var RestStream = aStream;
					while (true) {
						tResultList TempResult;
						mList.tList<t> NewRestStream;
						if (aParser.Parse(RestStream).MATCH(out TempResult, out NewRestStream)) {
							Result = Concat(Result, TempResult);
							RestStream = NewRestStream;
							break;
						}
						t Head;
						if (!RestStream.MATCH(out Head, out RestStream)) {
							return Fail<t>();
						}
						Result = Concat(Result, ResultList(Head));
					}
					return OK(Result, RestStream);
				}
			};
		}
	}
	
	//================================================================================
	public static tParser<t>
	UndefParser<t>(
	//================================================================================
	) {
		return new tParser<t>{_ParseFunc = null};
	}
	
	//================================================================================
	public static void
	Def<t>(
		this tParser<t> a1,
		tParser<t> a2
	//================================================================================
	) {
		mStd.Assert(a1._ParseFunc.IsNull());
		a1._ParseFunc = a2._ParseFunc;
	}
	
	//================================================================================
	public static mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>>
	Parse<t>(
		this tParser<t> aParser,
		mList.tList<t> aStream
	//================================================================================
	) {
		return aParser._ParseFunc(aStream);
	}
	
	//================================================================================
	public static tParser<t>
	Modify_<t>(
		this tParser<t> aParser,
		mStd.tFunc<tResultList, tResultList> aModifyFunc
	//================================================================================
	) {
		return new tParser<t>{
			_ParseFunc = (mList.tList<t> aStream) => {
				tResultList ResultList_;
				mList.tList<t> RestStream;
				if (aParser._ParseFunc(aStream).MATCH(out ResultList_, out RestStream)) {
					return OK(aModifyFunc(ResultList_), RestStream);
				} else {
					return Fail<t>();
				}
			}
		};
	}
	
	//================================================================================
	public static tParser<t>
	AtomParser<t>(
		mStd.tFunc<tBool, t> aTest
	//================================================================================
	) {
		return new tParser<t>{
			_ParseFunc = Stream => {
				t Head;
				mList.tList<t> Tail;
				if (Stream.MATCH(out Head, out Tail) && aTest(Head)) {
					return OK(ResultList(Head), Tail);
				}
				return Fail<t>();
			}
		};
	}
	
	//================================================================================
	public static tParser<mStd.tTuple<t1, t2>>
	AtomParser<t1, t2>(
		mStd.tFunc<tBool, t1, t2> aTest
	//================================================================================
	) {
		return new tParser<mStd.tTuple<t1, t2>>{
			_ParseFunc = Stream => {
				mStd.tTuple<t1, t2> Head;
				mList.tList<mStd.tTuple<t1, t2>> Tail;
				t1 Head1;
				t2 Head2;
				if (Stream.MATCH(out Head, out Tail) && Head.MATCH(out Head1, out Head2) && aTest(Head1, Head2)) {
					return OK(ResultList(Head), Tail);
				}
				return Fail<mStd.tTuple<t1, t2>>();
			}
		};
	}
	
	//================================================================================
	public static tParser<t>
	EmptyParser<t>(
	//================================================================================
	) {
		return new tParser<t>{
			_ParseFunc = Stream => OK(ResultList(), Stream)
		};
	}
	
	#region Test
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"AtomParser",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(A.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A.Parse(mList.List('B', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...+...",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					var B = AtomParser((tChar a) => a == 'B');
					var AB = A + B;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(AB.Parse(mList.List('A', 'B', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'B'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(AB.Parse (mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...-...",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					var B = AtomParser((tChar a) => a == 'B');
					var AB = A - B;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(AB.Parse(mList.List('A', 'B', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(AB.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"-...",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A = -AtomParser((tChar a) => a == 'A');
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList());
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(A.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"n*...",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A3 = 3 * AtomParser((tChar a) => a == 'A');
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A3.Parse(mList.List('A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A3.Parse(mList.List('A', 'A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('A', '_'));
					
					mStd.AssertNot(A3.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.Parse(mList.List('A', 'A', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"-n*...",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A3 = -3 * AtomParser((tChar a) => a == 'A');
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A3.Parse(mList.List('A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList());
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A3.Parse(mList.List('A', 'A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList());
					mStd.AssertEq(RestStream, mList.List('A', '_'));
					
					mStd.AssertNot(A3.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.Parse(mList.List('A', 'A', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...*n",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A3 = AtomParser((tChar a) => a == 'A') * 3;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A3.Parse(mList.List('A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A3.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.Parse(mList.List('A', 'A', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...|...",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					var B = AtomParser((tChar a) => a == 'B');
					var AB = A | B;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(AB.Parse(mList.List('A', 'B')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A'));
					mStd.AssertEq(RestStream, mList.List('B'));
					
					mStd.Assert(AB.Parse(mList.List('B', 'A')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('B'));
					mStd.AssertEq(RestStream, mList.List('A'));
					
					mStd.AssertNot(AB.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.Parse(mList.List('_', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...[m, n]",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A2_4 = AtomParser((tChar a) => a == 'A')[2, 4];
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A2_4.Parse(mList.List('A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_4.Parse(mList.List('A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_4.Parse(mList.List('A', 'A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_4.Parse(mList.List('A', 'A', 'A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('A', '_'));
					
					mStd.AssertNot(A2_4.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_4.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_4.Parse(mList.List('_', 'A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_4.Parse(mList.List('A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_4.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...[n, null]",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A2_ = AtomParser((tChar a) => a == 'A')[2, null];
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A2_.Parse(mList.List('A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_.Parse(mList.List('A', 'A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A2_.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('_', 'A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"....Modify(...=>...)",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A2_ = AtomParser((tChar a) => a == 'A')[2, null]
						.Modify_(
							a => {
								char Head;
								var Tail = a;
								var N = 0;
								while (Tail.GetHeadTail(out Head, out Tail)) {
									N += 1;
								}
								return ResultList(N);
							}
						);
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A2_.Parse(mList.List('A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList(2));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_.Parse(mList.List('A', 'A', 'A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList(5));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A2_.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('_', 'A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"....Modify(a => a.Reduce(...))",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A2_ = AtomParser((tChar a) => a == 'A')[2, null]
						.Modify_(a => a.Reduce(0, (int aCount, tChar aElem) => aCount + 1));
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A2_.Parse(mList.List('A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList(2));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_.Parse(mList.List('A', 'A', 'A', 'A', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList(5));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A2_.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('_', 'A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.Parse(mList.List('A', '_')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"~...",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					var B = AtomParser((tChar a) => a == 'B');
					var AB = (A + B).Modify_(a => ResultList("AB"));
					var nAB = ~AB;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(nAB.Parse(mList.List('A', 'B', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList("AB"));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(nAB.Parse(mList.List('B', 'A', 'A', 'B', 'A', '_')).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('B', 'A', "AB"));
					mStd.AssertEq(RestStream, mList.List('A', '_'));
					
					mStd.AssertNot(nAB.Parse(mList.List<tChar>()).MATCH(out Result, out RestStream));
					mStd.AssertNot(nAB.Parse(mList.List('_')).MATCH(out Result, out RestStream));
					mStd.AssertNot(nAB.Parse(mList.List('_', 'A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(nAB.Parse(mList.List('A')).MATCH(out Result, out RestStream));
					mStd.AssertNot(nAB.Parse(mList.List('A', '_', 'B', 'A')).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"Eval('MathExpr')",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					var CharIn = mStd.Func(
						(tText aChars) => AtomParser(
							mStd.Func(
								(tChar aChar) => {
									foreach (var Char in aChars) {
										if (Char == aChar) { return true; }
									}
									return false;
								}
							)
						)
					);
					
					var Token = mStd.Func(
						(tText aTocken) => {
							var Parser = EmptyParser<tChar>();
							foreach (var Char in aTocken) {
								Parser += AtomParser((tChar aChar) => aChar == Char);
							}
							return Parser;
						}
					);
					
					var _ = -CharIn(" \t");
					var __ = -_[0, null];
					
					var P = mStd.Func((tParser<tChar> aParser) => (-Token("(") -__ +aParser -__ -Token(")")));
					
					var DIGIT    = CharIn("0123456789")                  .Modify((tChar a) => (tInt32)a - (tInt32)'0');
					var NAT      = DIGIT[1, null]                        .Modify_(aDigits => aDigits.Reduce(0, (tInt32 aNat, tInt32 aDigit) => aNat*10 + aDigit));
					var PLUS     = AtomParser((tChar a) => a == '+')     .Modify((tChar a) => +1);
					var MINUS    = AtomParser((tChar a) => a == '-')     .Modify((tChar a) => -1);
					var SIGNUM   = (PLUS | MINUS);
					var INT      = (+SIGNUM +NAT)                        .Modify((tInt32 aSignum, tInt32 aNat) => aSignum * aNat);
					var NUMBER   = NAT | INT;
					var OP_SUM   = (-Token("+"))                         .Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 + a2));
					var OP_DIF   = (-Token("-"))                         .Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 - a2));
					var OP_PROD  = (-Token("*"))                         .Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 * a2));
					var OP_QUOT  = (-Token("/"))                         .Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 / a2));
					var OP       = OP_SUM | OP_DIF | OP_PROD | OP_QUOT;
					var EXPR     = UndefParser<tChar>();
					EXPR.Def(
						NUMBER |
						P(+EXPR -__ +OP -__ +EXPR)                       .Modify((tInt32 a1, mStd.tFunc<tInt32, tInt32, tInt32> aOp, tInt32 a2) => aOp(a1, a2))
					);
					
					var Eval = mStd.Func(
						(tText aExpr) => {
							var I = (tInt32?)0;
							mStd.tTuple<tResultList, mList.tList<tChar>> Tuple;
							
							var X = EXPR.Parse(
								mList.LasyList(
									() => {
										if (I.Value < aExpr.Length) {
											I += 1;
											return mStd.OK(aExpr[I.Value - 1]);
										}
										return mStd.Fail<tChar>();
									}
								)
							);
							mStd.Assert(X.MATCH(out Tuple));
							
							tResultList ResultList;
							mList.tList<tChar> Rest;
							mStd.Assert(Tuple.MATCH(out ResultList, out Rest));
							
							tInt32 Result;
							mStd.Assert(ResultList.MATCH(out Result));
							
							return Result;
						}
					);
					
					mStd.AssertEq(Eval("1"), 1);
					mStd.AssertEq(Eval("-2"), -2);
					mStd.AssertEq(Eval("(3+4)"), 7);
					mStd.AssertEq(Eval("( 5 - 6)"), -1);
					mStd.AssertEq(Eval("( 5 --6)"), 11);
					mStd.AssertEq(Eval("( 7 * (8 / 4))"), 14);
					mStd.AssertEq(Eval("( 7 * (8 / (4  -6)))"), -28);
					
					return true;
				}
			)
		)
	);
	
	#endregion
}
