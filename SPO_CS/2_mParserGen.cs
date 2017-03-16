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

public static class mParserGen {
	
	#region Helper
	// TODO: seperate file ???
	
	//================================================================================
	public static tParser<t>
	Modify<t, tR>(
		this tParser<t> aParser,
		mStd.tFunc<tR> aModifyFunc
	//================================================================================
	) => aParser.Modify_(
		a => ResultList(aModifyFunc())
	);
	
	//================================================================================
	public static tParser<t>
	Modify<t, tR, t1>(
		this tParser<t> aParser,
		mStd.tFunc<tR, t1> aModifyFunc
	//================================================================================
	) => aParser.Modify_(
		a => {
			mStd.Assert(a.MATCH(out t1 A1));
			return ResultList(aModifyFunc(A1));
		}
	);
	
	//================================================================================
	public static tParser<t>
	Modify<t, tR, t1, t2>(
		this tParser<t> aParser,
		mStd.tFunc<tR, t1, t2> aModifyFunc
	//================================================================================
	) => aParser.Modify_(
		a => {
			mStd.Assert(a.MATCH(out t1 A1, out t2 A2));
			return ResultList(aModifyFunc(A1, A2));
		}
	);
	
	//================================================================================
	public static tParser<t>
	Modify<t, tR, t1, t2, t3>(
		this tParser<t> aParser,
		mStd.tFunc<tR, t1, t2, t3> aModifyFunc
	//================================================================================
	) => aParser.Modify_(
		a => {
			mStd.Assert(a.MATCH(out t1 A1, out t2 A2, out t3 A3));
			return ResultList(aModifyFunc(A1, A2, A3));
		}
	);
	
	//================================================================================
	public static tParser<t>
	Modify<t, tR, t1, t2, t3, t4>(
		this tParser<t> aParser,
		mStd.tFunc<tR, t1, t2, t3, t4> aModifyFunc
	//================================================================================
	) => aParser.Modify_(
		a => {
			mStd.Assert(a.MATCH(out t1 A1, out t2 A2, out t3 A3, out t4 A4));
			return ResultList(aModifyFunc(A1, A2, A3, A4));
		}
	);
	
	#endregion
	
	#region tResult
	
	public class tResultList {
		internal mList.tList<mStd.tAny> _Value;
		
		//================================================================================
		public tBool
		Equals(
			tResultList a
		//================================================================================
		) => (
			!a.IsNull() &&
			(_Value.IsNull() ? a._Value.IsNull() : _Value.Equals(a._Value))
		);
		
		override public tBool Equals(object a) => Equals(a as tResultList);
		override public tText ToString() => _Value.ToString();
	}
	
	//================================================================================
	public static tResultList
	ResultList(
	//================================================================================
	) => new tResultList{_Value = mList.List<mStd.tAny>() };
	
	//================================================================================
	public static tResultList
	ResultList<t>(
		t a
	//================================================================================
	) => new tResultList{_Value = mList.List(mStd.Any(a)) };
	
	//================================================================================
	public static tResultList
	ResultList<t1, t2>(
		t1 a1,
		t2 a2
	//================================================================================
	) => new tResultList{_Value = mList.List(mStd.Any(a1), mStd.Any(a2)) };
	
	//================================================================================
	public static tResultList
	ResultList<t1, t2, t3>(
		t1 a1,
		t2 a2,
		t3 a3
	//================================================================================
	) => new tResultList{_Value = mList.List(mStd.Any(a1), mStd.Any(a2), mStd.Any(a3)) };
	
	//================================================================================
	public static tResultList
	ResultList<t1, t2, t3, t4>(
		t1 a1,
		t2 a2,
		t3 a3,
		t4 a4
	//================================================================================
	) => new tResultList{_Value = mList.List(mStd.Any(a1), mStd.Any(a2), mStd.Any(a3), mStd.Any(a4)) };
	
	//================================================================================
	public static tResultList
	Concat(
		tResultList a1,
		tResultList a2
	//================================================================================
	) => new tResultList{_Value = mList.Concat(a1._Value, a2._Value)};
	
	//================================================================================
	public static tBool MATCH<t>(
		this mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>> a,
		out tResultList aResultList,
		out mList.tList<t> aRestStream
	//================================================================================
	) {
		mStd.tTuple<tResultList, mList.tList<t>> Temp;
		if (a.MATCH(out Temp)) {
			Temp.MATCH(out aResultList, out aRestStream);
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
	) => mStd.OK(mStd.Tuple(aResultList, aRestStream));
	
	//================================================================================
	public static mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>>
	Fail<t>(
	//================================================================================
	) => mStd.Fail<mStd.tTuple<tResultList, mList.tList<t>>>();
	
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
		internal mStd.tFunc<mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>>, mList.tList<t>, mStd.tAction<tText>> _ParseFunc;
		#if DEBUG || TRACE
			public tText _DebugName = null;
			public tText _DebugDef = "";
		#endif
		
		public tText DebugName {
			get {
				#if DEBUG
					return _DebugName;
				#else
					return null;
				#endif
			}
		}
		
		public tText DebugDef {
			get {
				#if DEBUG
					return _DebugDef;
				#else
					return null;
				#endif
			}
		}
		
		//================================================================================
		public tParser<t>
		SetDebugDef(
			params object[] aDebugNameParts
		//================================================================================
		) {
			#if DEBUG || TRACE
				var Def = "";
				var Parts = mList.List(aDebugNameParts);
				object Part;
				while (Parts.MATCH(out Part, out Parts)) {
					Def += Part?.ToString() ?? "";
				}
				_DebugDef = Def;
			#endif
			return this;
		}
		
		//================================================================================
		public tParser<t>
		SetDebugName(
			params object[] aDebugNameParts
		//================================================================================
		) {
			#if DEBUG || TRACE
				var Name = "";
				var Parts = mList.List(aDebugNameParts);
				object Part;
				while (Parts.MATCH(out Part, out Parts)) {
					Name += Part?.ToString() ?? "";
				}
				_DebugName = Name.Replace("\n", @"\n").Replace("\r", @"\r").Replace("\t", @"\t");
			#endif
			return this;
		}
		
		//================================================================================
		public static tParser<t>
		operator+(
			tParser<t> aP1,
			tParser<t> aP2
		//================================================================================
		) => new tParser<t> {
			_ParseFunc = (aStream, aDebugStream) => {
				return (
					aP1.Parse(aStream, aDebugStream).MATCH(out var Result1, out var TempStream) &&
					aP2.Parse(TempStream, aDebugStream).MATCH(out var Result2, out TempStream)
				) ? (
					OK(Concat(Result1, Result2), TempStream)
				) : (
					Fail<t>()
				);
			}
		}
		.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") + (", aP2.DebugName??aP2.DebugDef, ")");
		
		//================================================================================
		public static tParser<t>
		operator-(
			tParser<t> aP1,
			tParser<t> aP2
		//================================================================================
		) => (aP1 + -aP2).SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") - (", aP2.DebugName??aP2.DebugDef, ")");
		
		//================================================================================
		public static tParser<t>
		operator-(
			tParser<t> aParser
		//================================================================================
		) => aParser.Modify_(_ => ResultList()).SetDebugDef(" - (", aParser.DebugName??aParser.DebugDef, ")");
		
		//================================================================================
		public static tParser<t>
		operator+(
			tParser<t> aParser
		//================================================================================
		) => aParser;
		
		//================================================================================
		public static tParser<t>
		operator*(
			tParser<t> aParser,
			tInt32 aCount
		//================================================================================
		) => aCount * aParser;

		
		//================================================================================
		public static tParser<t>
		operator*(
			tInt32 aCount,
			tParser<t> aParser
		//================================================================================
		) => (
			(aCount == 0) ? EmptyParser<t>() :
			(aCount < 0) ? -(-aCount * aParser) :
			aParser + (aCount-1)*aParser
		)
		.SetDebugDef(aCount.ToString(), " * (", aParser, ")");
		
		//================================================================================
		public static tParser<t>
		operator|(
			tParser<t> aP1,
			tParser<t> aP2
		//================================================================================
		) => new tParser<t> {
			_ParseFunc = (aStream, aDebugStream) => {
				return (
					aP1.Parse(aStream, aDebugStream).MATCH(out var Result, out var TempStream) ||
					aP2.Parse(aStream, aDebugStream).MATCH(out Result, out TempStream)
				) ? (
					OK(Result, TempStream)
				) : (
					Fail<t>()
				);
			}
		}
		.SetDebugDef("(", aP1.DebugName??aP1.DebugDef, ") | (", aP2.DebugName??aP2.DebugDef, ")");
		
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
							_ParseFunc = (aStream, aDebugStream) => {
								var Result = ResultList();
								var RestStream = aStream;
								while (this.Parse(aStream, aDebugStream).MATCH(out var TempResult, out aStream)) {
									Result = Concat(Result, TempResult);
									RestStream = aStream;
								}
								return OK(Result, RestStream);
							}
						}
						.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", aMin, "..", aMax, "]");
					} else {
						return new tParser<t> {
							_ParseFunc = (aStream, aDebugStream) => {
								var Result = ResultList();
								var RestStream = aStream;
								var Max = aMax.Value;
								while (Max != 0 && this.Parse(aStream, aDebugStream).MATCH(out var TempResult, out aStream)) {
									Result = Concat(Result, TempResult);
									RestStream = aStream;
									Max -= 1;
								}
								return OK(Result, RestStream);
							}
						}
						.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", aMin, "..", aMax, "]");
					}
				}
				if (aMax.IsNull()) {
					return ((int)aMin*this + this[0, null])
					.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", aMin, "..", aMax, "]");
				}
				if (aMin <= aMax) {
					return (int)aMin*this + this[0, aMax - aMin]
					.SetDebugDef("(", this.DebugName??this.DebugDef, ")[", aMin, "..", aMax, "]");
				}
				throw null;
			}
		}
		
		//================================================================================
		public static tParser<t>
		operator ~(
			tParser<t> aParser
		//================================================================================
		) => new tParser<t> {
			_ParseFunc = (aStream, aDebugStream) => {
				var Result = ResultList();
				var RestStream = aStream;
				while (true) {
					tResultList TempResult;
					mList.tList<t> NewRestStream;
					if (aParser.Parse(RestStream, aDebugStream).MATCH(out TempResult, out NewRestStream)) {
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
		}
		.SetDebugDef("~(", aParser.DebugName??aParser.DebugDef, ")");
	}
	
	//================================================================================
	public static tParser<t>
	UndefParser<t>(
	//================================================================================
	) => new tParser<t>{_ParseFunc = null};
	
	//================================================================================
	public static void
	Def<t>(
		this tParser<t> a1,
		tParser<t> a2
	//================================================================================
	) {
		mStd.Assert(a1._ParseFunc.IsNull());
		a1._ParseFunc = a2._ParseFunc;
		a1.SetDebugDef(a2.DebugDef);
	}
	
	//================================================================================
	public static mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>>
	StartParse<t>(
		this tParser<t> aParser,
		mList.tList<t> aStream,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var Level = (tInt32?)0;
		return aParser.Parse(
			aStream,
			(tText aDebugText) => {
				if (aDebugText.StartsWith("}")) { Level -= 1; }
				aDebugStream(new tText(' ', mMath.Max(Level.Value, 0)) + aDebugText);
				if (aDebugText.EndsWith("{")) { Level += 1; }
			}
		);
	}
	
	//================================================================================
	private static mStd.tMaybe<mStd.tTuple<tResultList, mList.tList<t>>>
	Parse<t>(
		this tParser<t> aParser,
		mList.tList<t> aStream,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		#if TRACE
			if (aParser._DebugName != null) {
				aDebugStream(aParser._DebugName+" = "+aParser._DebugDef+" -> {");
			}else if (aParser._DebugDef != "") {
				aDebugStream(aParser._DebugDef+" -> {");
			} else {
				aDebugStream("??? -> {");
			}
		#endif
		var Result = aParser._ParseFunc(aStream, aDebugStream);
		#if TRACE
		if (Result._IsOK) {
			aDebugStream("} -> OK");
		} else {
			aDebugStream("} -> FAIL");
		}
		#endif
		return Result;
	}
	
	//================================================================================
	public static tParser<t>
	Modify_<t>(
		this tParser<t> aParser,
		mStd.tFunc<tResultList, tResultList> aModifyFunc
	//================================================================================
	) => new tParser<t>{
		_ParseFunc = (aStream, aDebugStream) => {
			return (
				aParser._ParseFunc(aStream, aDebugStream).MATCH(out tResultList ResultList_, out mList.tList<t> RestStream) ?
				OK(aModifyFunc(ResultList_), RestStream) :
				Fail<t>()
			);
		}
	}.SetDebugDef("{", aParser?.DebugName ?? aParser.DebugDef, "}");
	
	//================================================================================
	public static tParser<t>
	AtomParser<t>(
		mStd.tFunc<tBool, t> aTest
	//================================================================================
	) => new tParser<t>{
		_ParseFunc = (aStream, aDebugStream) => {
			return (
				(aStream.MATCH(out var Head, out var Tail) && aTest(Head)) ?
				OK(ResultList(Head), Tail) :
				Fail<t>()
			);
		}
	};
	
	//================================================================================
	public static tParser<t>
	EmptyParser<t>(
	//================================================================================
	) => new tParser<t>{
		_ParseFunc = (aStream, aDebugStream) => OK(ResultList(), aStream)
	};
	
	#region Test
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"AtomParser",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A.StartParse(mList.List('B', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...+...",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					var B = AtomParser((tChar a) => a == 'B');
					var AB = A + B;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(AB.StartParse(mList.List('A', 'B', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'B'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(AB.Parse (mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...-...",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					var B = AtomParser((tChar a) => a == 'B');
					var AB = A - B;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(AB.StartParse(mList.List('A', 'B', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(AB.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"-...",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A = -AtomParser((tChar a) => a == 'A');
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList());
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"n*...",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A3 = 3 * AtomParser((tChar a) => a == 'A');
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A3.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A3.StartParse(mList.List('A', 'A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('A', '_'));
					
					mStd.AssertNot(A3.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.StartParse(mList.List('A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"-n*...",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A3 = -3 * AtomParser((tChar a) => a == 'A');
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A3.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList());
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A3.StartParse(mList.List('A', 'A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList());
					mStd.AssertEq(RestStream, mList.List('A', '_'));
					
					mStd.AssertNot(A3.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.StartParse(mList.List('A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...*n",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A3 = AtomParser((tChar a) => a == 'A') * 3;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A3.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A3.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A3.StartParse(mList.List('A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...|...",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					var B = AtomParser((tChar a) => a == 'B');
					var AB = A | B;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(AB.StartParse(mList.List('A', 'B'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A'));
					mStd.AssertEq(RestStream, mList.List('B'));
					
					mStd.Assert(AB.StartParse(mList.List('B', 'A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('B'));
					mStd.AssertEq(RestStream, mList.List('A'));
					
					mStd.AssertNot(AB.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(AB.StartParse(mList.List('_', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...[m, n]",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A2_4 = AtomParser((tChar a) => a == 'A')[2, 4];
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A2_4.StartParse(mList.List('A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_4.StartParse(mList.List('A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_4.StartParse(mList.List('A', 'A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_4.StartParse(mList.List('A', 'A', 'A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('A', '_'));
					
					mStd.AssertNot(A2_4.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_4.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_4.StartParse(mList.List('_', 'A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_4.StartParse(mList.List('A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_4.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"...[n, null]",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A2_ = AtomParser((tChar a) => a == 'A')[2, null];
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A2_.StartParse(mList.List('A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_.StartParse(mList.List('A', 'A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('A', 'A', 'A', 'A'));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A2_.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('_', 'A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"....Modify(...=>...)",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
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
					mStd.Assert(A2_.StartParse(mList.List('A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList(2));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_.StartParse(mList.List('A', 'A', 'A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList(5));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A2_.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('_', 'A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"....Modify(a => a.Reduce(...))",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A2_ = AtomParser((tChar a) => a == 'A')[2, null]
						.Modify_(a => a.Reduce(0, (int aCount, tChar aElem) => aCount + 1));
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(A2_.StartParse(mList.List('A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList(2));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(A2_.StartParse(mList.List('A', 'A', 'A', 'A', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList(5));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.AssertNot(A2_.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('_', 'A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(A2_.StartParse(mList.List('A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"~...",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					var A = AtomParser((tChar a) => a == 'A');
					var B = AtomParser((tChar a) => a == 'B');
					var AB = (A + B).Modify_(a => ResultList("AB"));
					var nAB = ~AB;
					
					tResultList Result;
					mList.tList<tChar> RestStream;
					mStd.Assert(nAB.StartParse(mList.List('A', 'B', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList("AB"));
					mStd.AssertEq(RestStream, mList.List('_'));
					
					mStd.Assert(nAB.StartParse(mList.List('B', 'A', 'A', 'B', 'A', '_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertEq(Result, ResultList('B', 'A', "AB"));
					mStd.AssertEq(RestStream, mList.List('A', '_'));
					
					mStd.AssertNot(nAB.StartParse(mList.List<tChar>(), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(nAB.StartParse(mList.List('_'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(nAB.StartParse(mList.List('_', 'A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(nAB.StartParse(mList.List('A'), aDebugStream).MATCH(out Result, out RestStream));
					mStd.AssertNot(nAB.StartParse(mList.List('A', '_', 'B', 'A'), aDebugStream).MATCH(out Result, out RestStream));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"Eval('MathExpr')",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
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
							
							var X = EXPR.StartParse(
								mList.LasyList(
									() => {
										if (I.Value < aExpr.Length) {
											I += 1;
											return mStd.OK(aExpr[I.Value - 1]);
										}
										return mStd.Fail<tChar>();
									}
								),
								aDebugStream
							);
							mStd.Assert(X.MATCH(out Tuple));
							
							tResultList ResultList;
							mList.tList<tChar> Rest;
							Tuple.MATCH(out ResultList, out Rest);
							
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
