// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT Common/mMaybe
// IMPORT Common/mResult
// IMPORT Common/mSpan
// IMPORT Common/mArrayList
// IMPORT Common/mStream
// IMPORT Common/mTextStream
// IMPORT Common/mParserGen
// IMPORT mTokenizer
// IMPORT mIL_AST
// IMPORT mIL_Parser
// IMPORT mVM_Type
// IMPORT mSPO_AST
// IMPORT mSPO_AST_Types
// IMPORT mSPO_Parser
// IMPORT mSPO2IL


using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mSPO2IL_Tests {
	private static tSpan
	Span(
		(tNat32 Row, tNat32 Col) aStart,
		(tNat32 Row, tNat32 Col) aEnd
	) => mSpan.Span(
		new tPos {
			Id = "",
			Row = aStart.Row,
			Col = aStart.Col
		},
		new tPos {
			Id = "",
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	);
	
	public static tBool
	EqSpan(
		this tSpan a1,
		tSpan a2
	) => (
		a1.Start.Row == a2.Start.Row &&
		a1.Start.Col == a2.Start.Col &&
		a1.End.Row == a2.End.Row &&
		a1.End.Col == a2.End.Col
	);
	
	private static void AssertCommandsAre<tPos>(
		mStream.tStream<mIL_AST.tCommandNode<tPos>> aSPO_Commands,
		tText aIL_Commands
	) {
		mAssert.AreEquals(
			aSPO_Commands.Map(
				mIL_AST.ToText
			).Join(
				(a1, a2) => a1 + "\r\n" + a2,
				""
			),
			aIL_Commands
		);
	}
	
	private static void AssertDefsAre<tPos>(
		mStd.tAction<tText> aStreamOut,
		mStream.tStream<(tText? Type, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands)> aSPO_Defs,
		System.Span<tText> aIL_Defs
	) {
		aStreamOut($"Def Count: {aSPO_Defs.Count()}");
		var DefIndex = 0u;
		foreach (var (SPO_Def, IL_Def) in mStream.ZipShort(aSPO_Defs, mStream.Stream(aIL_Defs))) {
			aStreamOut(mSPO2IL.GetDefId(DefIndex));
			AssertCommandsAre(SPO_Def.Commands.ToStream(), IL_Def);
			aStreamOut(" OK");
			DefIndex += 1;
		}
		
		if (aSPO_Defs.Count() > aIL_Defs.Length) {
			aStreamOut("miss additional defs:");
			foreach (var (_, Commands) in aSPO_Defs.Skip((tNat32)aIL_Defs.Length)) {
				foreach (var command in Commands.ToStream()) {
					aStreamOut(command.ToText());
				}
				aStreamOut("");
			}
			aStreamOut("");
			mAssert.Fail();
		}
		
		mAssert.AreEquals(aSPO_Defs.Count(), (tNat32)aIL_Defs.Length);
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(nameof(mSPO2IL),
		[
			mTest.Test("MapExpression",
				aStreamOut => {
					var ExpressionNode = mSPO_Parser.Expression.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"0 .< (1 .+ 2) < 4",
						"",
						_ => aStreamOut(_())
					);
					
					var Scope = mStream.Stream(
						[
							(Id: "_...+...", Type: mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]), mVM_Type.Int())),
							(Id: "_...<...<...", Type: mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int()]), mVM_Type.Bool()))
						]
					);
					
					mSPO_AST_Types.UpdateExpressionTypes(ExpressionNode, Scope);
					
					var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var Def = mSPO2IL.NewDefConstructor<tSpan>();
					mAssert.AreEquals(Def.MapExpression(Module, ExpressionNode), mSPO2IL.GetRegId(11));
					
					mAssert.AreEquals(
						Def.Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.CreateInt(Span((1, 1), (1, 1)), mSPO2IL.GetRegId(1), "0"),
								mIL_AST.CreatePair(Span((1, 1), (1, 17)), mSPO2IL.GetRegId(2), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(1)),
								mIL_AST.CreateInt(Span((1, 7), (1, 7)), mSPO2IL.GetRegId(3), "1"),
								mIL_AST.CreatePair(Span((1, 7), (1, 12)), mSPO2IL.GetRegId(4), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(3)),
								mIL_AST.CreateInt(Span((1, 12), (1, 12)), mSPO2IL.GetRegId(5), "2"),
								mIL_AST.CreatePair(Span((1, 7), (1, 12)), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(5)),
								mIL_AST.CallFunc(Span((1, 7), (1, 12)), mSPO2IL.GetRegId(7), mSPO2IL.GetId("...+..."), mSPO2IL.GetRegId(6)),
								mIL_AST.CreatePair(Span((1, 1), (1, 17)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(7)),
								mIL_AST.CreateInt(Span((1, 17), (1, 17)), mSPO2IL.GetRegId(9), "4"),
								mIL_AST.CreatePair(Span((1, 1), (1, 17)), mSPO2IL.GetRegId(10), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(9)),
								mIL_AST.CallFunc(Span((1, 1), (1, 17)), mSPO2IL.GetRegId(11), mSPO2IL.GetId("...<...<..."), mSPO2IL.GetRegId(10))
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
				}
			),
			mTest.Test("MapDef1",
				aStreamOut => {
					var DefNode = mSPO_Parser.Def.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"§DEF a = (1, 2)",
						"",
						_ => aStreamOut(_())
					);
					
					mSPO_AST_Types.UpdateCommandTypes(DefNode, mStd.cEmpty);
					
					var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var DefConstructor = mSPO2IL.NewDefConstructor<tSpan>();
					DefConstructor.MapDef(Module, DefNode);
					
					mAssert.AreEquals(
						DefConstructor.Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.CreateInt(Span((1, 11), (1, 11)), mSPO2IL.GetRegId(1), "1"),
								mIL_AST.CreatePair(Span((1, 10), (1, 15)), mSPO2IL.GetRegId(2), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(1)),
								mIL_AST.CreateInt(Span((1, 14), (1, 14)), mSPO2IL.GetRegId(3), "2"),
								mIL_AST.CreatePair(Span((1, 10), (1, 15)), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(3)),
								
								mIL_AST.Alias(Span((1, 1), (1, 6)), mSPO2IL.GetId("a"), mSPO2IL.GetRegId(4))
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
				}
			),
			mTest.Test("MapDefMatch",
				aStreamOut => {
					var DefNode = mSPO_Parser.Def.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"(§DEF a, (§DEF b, §DEF c)) = (1, (2, 3))",
						"",
						_ => aStreamOut(_())
					);
					
					mSPO_AST_Types.UpdateCommandTypes(DefNode, mStd.cEmpty);
					
					var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var DefConstructor = mSPO2IL.NewDefConstructor<tSpan>();
					DefConstructor.MapDef(Module, DefNode);
					
					mAssert.AreEquals(
						DefConstructor.Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.CreateInt(Span((1, 31), (1, 31)), mSPO2IL.GetRegId(1), "1"), // [1]:1
								mIL_AST.CreatePair(Span((1, 30), (1, 40)), mSPO2IL.GetRegId(2), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(1)), // (); [1]:1 => [2]:(1)
								mIL_AST.CreateInt(Span((1, 35), (1, 35)), mSPO2IL.GetRegId(3), "2"), // [3]:2
								mIL_AST.CreatePair(Span((1, 34), (1, 39)), mSPO2IL.GetRegId(4), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(3)), // (); [4]:2 => [3]:(2)
								mIL_AST.CreateInt(Span((1, 38), (1, 38)), mSPO2IL.GetRegId(5), "3"), // [5]:3
								mIL_AST.CreatePair(Span((1, 34), (1, 39)), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(5)), // [4]:(2); [5]:3 => (2, 3)
								mIL_AST.CreatePair(Span((1, 30), (1, 40)), mSPO2IL.GetRegId(7), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(6)), // [2]:(1); [6]:(2, 3) => [7]:(1, (2, 3))
								
								mIL_AST.GetSecond(Span((1, 1), (1, 26)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(7)), // [7]:(1, (2, 3)) => [8]:(2, 3)
								mIL_AST.GetSecond(Span((1, 10), (1, 25)), mSPO2IL.GetRegId(9), mSPO2IL.GetRegId(8)), // [8]:(2, 3) => [9]:3
								mIL_AST.Alias(Span((1, 19), (1, 24)), mSPO2IL.GetId("c"), mSPO2IL.GetRegId(9)), // [9]:3 == c
								mIL_AST.GetFirst(Span((1, 10), (1, 25)), mSPO2IL.GetRegId(10), mSPO2IL.GetRegId(8)), // [8]:(2, 3) => [10]:(2)
								mIL_AST.GetSecond(Span((1, 10), (1, 25)), mSPO2IL.GetRegId(11), mSPO2IL.GetRegId(10)), // [10]:(2) => [11]:2
								mIL_AST.Alias(Span((1, 11), (1, 16)), mSPO2IL.GetId("b"), mSPO2IL.GetRegId(11)), // [11]:2 == b
								mIL_AST.GetFirst(Span((1, 10), (1, 25)), mSPO2IL.GetRegId(12), mSPO2IL.GetRegId(10)), // [10]:(2) => [12]:()
								mIL_AST.GetFirst(Span((1, 1), (1, 26)), mSPO2IL.GetRegId(13), mSPO2IL.GetRegId(7)), // [7]:(1, (2, 3)) => [13]:(1)
								mIL_AST.GetSecond(Span((1, 1), (1, 26)), mSPO2IL.GetRegId(14), mSPO2IL.GetRegId(13)), // [13]:(1) => [14]:1
								mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.GetId("a"), mSPO2IL.GetRegId(14)), // [14]:1 == a
								
								mIL_AST.GetFirst(Span((1, 1), (1, 26)), mSPO2IL.GetRegId(15), mSPO2IL.GetRegId(13)) // [13]:(1) => [14]:()
							]
						),
						aAreEqual: mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
				}
			),
			mTest.Test("MatchTuple",
				aStreamOut => {
					var DefNode = mSPO_Parser.Def.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"(§DEF a, §DEF b, §DEF c) = (1, 2, 3)",
						"",
						_ => aStreamOut(_())
					);
					
					mSPO_AST_Types.UpdateCommandTypes(DefNode, mStd.cEmpty);
					
					var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var DefConstructor = mSPO2IL.NewDefConstructor<tSpan>();
					DefConstructor.MapDef(Module, DefNode);
					
					mAssert.AreEquals(
						DefConstructor.Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.CreateInt(Span((1, 29), (1, 29)), mSPO2IL.GetRegId(1), "1"), // [1]:1
								mIL_AST.CreatePair(Span((1, 28), (1, 36)), mSPO2IL.GetRegId(2), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(1)), // (); [1]1 => [2]:(1)
								mIL_AST.CreateInt(Span((1, 32), (1, 32)), mSPO2IL.GetRegId(3), "2"), // [3]:2
								mIL_AST.CreatePair(Span((1, 28), (1, 36)), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(3)), // [2]:(1); [3]:2 => [4]:(1, 2)
								mIL_AST.CreateInt(Span((1, 35), (1, 35)), mSPO2IL.GetRegId(5), "3"), // [5]:3
								mIL_AST.CreatePair(Span((1, 28), (1, 36)), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(5)), // [4]:(1, 2); [5]:3 => [6]:(1, 2, 3)
								
								mIL_AST.GetSecond(Span((1, 1), (1, 24)), mSPO2IL.GetRegId(7), mSPO2IL.GetRegId(6)), // [6]:(1, 2, 3) => [7]:3
								mIL_AST.Alias(Span((1, 18), (1, 23)), mSPO2IL.GetId("c"), mSPO2IL.GetRegId(7)), // [7]:3 == c
								mIL_AST.GetFirst(Span((1, 1), (1, 24)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(6)), // [6]:(1, 2, 3) => [8]:(1, 2)
								mIL_AST.GetSecond(Span((1, 1), (1, 24)), mSPO2IL.GetRegId(9), mSPO2IL.GetRegId(8)), // [8]:(1, 2) => [9]:2
								mIL_AST.Alias(Span((1, 10), (1, 15)), mSPO2IL.GetId("b"), mSPO2IL.GetRegId(9)), // [9]:2 == b
								mIL_AST.GetFirst(Span((1, 1), (1, 24)), mSPO2IL.GetRegId(10), mSPO2IL.GetRegId(8)), // [8]:(1, 2) => [10]:(1)
								mIL_AST.GetSecond(Span((1, 1), (1, 24)), mSPO2IL.GetRegId(11), mSPO2IL.GetRegId(10)), // [10]:(1) => [11]:1
								mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.GetId("a"), mSPO2IL.GetRegId(11)), // [11]:1 == a
								mIL_AST.GetFirst(Span((1, 1), (1, 24)), mSPO2IL.GetRegId(12), mSPO2IL.GetRegId(10)) // [10]:(1) => [12]:()
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
				}
			),
			mTest.Test("MapMatchPrefix",
				aStreamOut => {
					var DefNode = mSPO_Parser.Def.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"(§DEF a, §DEF b, (#bla (§DEF c , §DEF d))) = (1, 2, (#bla (3, 4)))",
						"",
						_ => aStreamOut(_())
					);
					
					mSPO_AST_Types.UpdateCommandTypes(DefNode, mStd.cEmpty);
					
					var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var DefConstructor = mSPO2IL.NewDefConstructor<tSpan>();
					
					DefConstructor.MapDef(Module, DefNode);
					
					mAssert.AreEquals(
						DefConstructor.Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.CreateInt(Span((1, 47), (1, 47)), mSPO2IL.GetRegId(1), "1"), // 1 => [1]:1
								mIL_AST.CreatePair(Span((1, 46), (1, 66)), mSPO2IL.GetRegId(2), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(1)), // (); [1]:1 => [2]:(1)
								mIL_AST.CreateInt(Span((1, 50), (1, 50)), mSPO2IL.GetRegId(3), "2"), // 2 => [3]:2
								mIL_AST.CreatePair(Span((1, 46), (1, 66)), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(3)), // [2]:(1); [3]:2 => [4]:(1, 2)
								mIL_AST.CreateInt(Span((1, 60), (1, 60)), mSPO2IL.GetRegId(5), "3"), // 3 => [5]:3
								mIL_AST.CreatePair(Span((1, 59), (1, 64)), mSPO2IL.GetRegId(6), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(5)), // (); [5]:3 => [6]:(3)
								mIL_AST.CreateInt(Span((1, 63), (1, 63)), mSPO2IL.GetRegId(7), "4"), // 4 => [7]:4
								mIL_AST.CreatePair(Span((1, 59), (1, 64)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(7)), // [6]:(3); [7]:4 => [8]:(3, 4)
								mIL_AST.AddPrefix(Span((1, 54), (1, 64)), mSPO2IL.GetRegId(9), mSPO2IL.GetId("bla..."), mSPO2IL.GetRegId(8)), // #bla[8]:(3, 4) => [9]:#bla(3, 4)
								mIL_AST.CreatePair(Span((1, 46), (1, 66)), mSPO2IL.GetRegId(10), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(9)), // [4]:(1, 2); [8]:(#bla(3, 4)) => [10]:(1, 2, #bla(3, 4))
								
								mIL_AST.GetSecond(Span((1, 1), (1, 42)), mSPO2IL.GetRegId(11), mSPO2IL.GetRegId(10)), // [10]:(1, 2, #bla(3, 4)) => [11]:#bla(3, 4)
								mIL_AST.SubPrefix(Span((1, 18), (1, 41)), mSPO2IL.GetRegId(12), mSPO2IL.GetId("bla..."), mSPO2IL.GetRegId(11)), // [11]:#bla(3, 4) => [12]:(3, 4)
								mIL_AST.GetSecond(Span((1, 24), (1, 40)), mSPO2IL.GetRegId(13), mSPO2IL.GetRegId(12)), // [12]:(3, 4) => [13]:4
								mIL_AST.Alias(Span((1, 34), (1, 39)), mSPO2IL.GetId("d"), mSPO2IL.GetRegId(13)), // [13]:4 == d
								mIL_AST.GetFirst(Span((1, 24), (1, 40)), mSPO2IL.GetRegId(14), mSPO2IL.GetRegId(12)), // [12]:(3, 4) => [14]:(3)
								mIL_AST.GetSecond(Span((1, 24), (1, 40)), mSPO2IL.GetRegId(15), mSPO2IL.GetRegId(14)), // [14]:(3) => [15]:3
								mIL_AST.Alias(Span((1, 25), (1, 30)), mSPO2IL.GetId("c"), mSPO2IL.GetRegId(15)), // [15]:3 == c
								mIL_AST.GetFirst(Span((1, 24), (1, 40)), mSPO2IL.GetRegId(16), mSPO2IL.GetRegId(14)), // [14]:(3) => [16]:()
								mIL_AST.GetFirst(Span((1, 1), (1, 42)), mSPO2IL.GetRegId(17), mSPO2IL.GetRegId(10)), // [10]:(1, 2, #bla(3, 4)) => [17]:(1, 2)
								mIL_AST.GetSecond(Span((1, 1), (1, 42)), mSPO2IL.GetRegId(18), mSPO2IL.GetRegId(17)), // [17]:(1, 2) => [18]:2
								mIL_AST.Alias(Span((1, 10), (1, 15)), mSPO2IL.GetId("b"), mSPO2IL.GetRegId(18)), // [18]:2 == b
								mIL_AST.GetFirst(Span((1, 1), (1, 42)), mSPO2IL.GetRegId(19), mSPO2IL.GetRegId(17)), // [17]:(1, 2) => [19]:(1)
								mIL_AST.GetSecond(Span((1, 1), (1, 42)), mSPO2IL.GetRegId(20), mSPO2IL.GetRegId(19)), // [19]:(1) => [20]:1
								mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.GetId("a"), mSPO2IL.GetRegId(20)), // [20]:1 == a
								mIL_AST.GetFirst(Span((1, 1), (1, 42)), mSPO2IL.GetRegId(21), mSPO2IL.GetRegId(19)) // [19]:(1) => [21]:()
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
				}
			),
			mTest.Test("MapLambda1",
				aStreamOut => {
					var DefNode = mSPO_Parser.Def.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"§DEF x = §DEF a € §INT => 2 .* a",
						"",
						_ => aStreamOut(_())
					);
					
					var InitScope = mStream.Stream(
						[
							(
								Id: mSPO2IL.GetId("...*..."),
								Type: mVM_Type.Proc(
									mVM_Type.Empty(),
									mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
									mVM_Type.Int()
								)
							)
						]
					);
					
					var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, InitScope).ElseThrow();
					
					mAssert.AreEquals(
						Scope,
						mStream.Stream(
							(
								Id: mSPO2IL.GetId("x"),
								Type: mVM_Type.Proc(
									mVM_Type.Empty(),
									mVM_Type.Int(),
									mVM_Type.Int()
								)
							),
							InitScope
						)
					);
					
					var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var DefConstructor = mSPO2IL.NewDefConstructor<tSpan>();
					DefConstructor.MapDef(Module, DefNode);
					
					DefConstructor.FinishMapProc(
						default,
						Module,
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Pair(
								mVM_Type.Proc( // _...*... € [§INT, §INT] => §INT
									mVM_Type.Empty(),
									mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
									mVM_Type.Int()
								),
								mVM_Type.Proc( // d_0 € [[[§INT, §INT] => §INT] => [§INT => §INT]]
									mVM_Type.Empty(),
									mVM_Type.Proc( // _...*... € [§INT, §INT] => §INT
										mVM_Type.Empty(),
										mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
										mVM_Type.Int()
									),
									mVM_Type.Proc( // _x € §INT => §INT
										mVM_Type.Empty(),
										mVM_Type.Int(), // _a
										mVM_Type.Int()
									)
								)
							),
							mVM_Type.Proc( // not defied now
								mVM_Type.Empty(),
								mVM_Type.Empty(),
								mVM_Type.Empty()
							)
						)
					);
					
					foreach (var (DefTypeId, DefCommands) in Module.Defs.ToStream()) {
						aStreamOut("==================");
						aStreamOut(DefTypeId);
						aStreamOut("------------------");
						foreach (var Command in DefCommands.ToStream()) {
							aStreamOut(Command.ToText());
						}
					}
					
					mAssert.AreEquals(Module.Defs.Size(), 2u);
					mAssert.AreEquals(
						Module.Defs.Get(0).Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.Alias(Span((1, 10), (1, 32)), mSPO2IL.GetId("...*..."), mIL_AST.cEnv), // ENV == ...*...
								
								mIL_AST.Alias(Span((1, 10), (1, 22)), mSPO2IL.GetId("a"), mIL_AST.cArg), // ARG == a
								
								mIL_AST.CreateInt(Span((1, 27), (1, 27)), mSPO2IL.GetRegId(1), "2"), // 2 => [1]:2
								mIL_AST.CreatePair(Span((1, 27), (1, 32)), mSPO2IL.GetRegId(2), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(1)), // (); [1]:2 => [2]:(2)
								mIL_AST.CreatePair(Span((1, 27), (1, 32)), mSPO2IL.GetRegId(3), mSPO2IL.GetRegId(2), mSPO2IL.GetId("a")), // [2]:(2); a => (2, a)
								
								mIL_AST.CallFunc(Span((1, 27), (1, 32)), mSPO2IL.GetRegId(4), mSPO2IL.GetId("...*..."), mSPO2IL.GetRegId(3)), // ...*... (2, a) => [4]
								mIL_AST.ReturnIf(Span((1, 27), (1, 32)), mIL_AST.cTrue, mSPO2IL.GetRegId(4)), // [4] TRUE
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
					
					mAssert.AreEquals(
						Module.Defs.Get(1).Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.GetSecond(Span((0, 0), (0, 0)), mSPO2IL.GetDefId(0), mIL_AST.cEnv),
								mIL_AST.GetFirst(Span((0, 0), (0, 0)), mSPO2IL.GetRegId(2), mIL_AST.cEnv),
								mIL_AST.GetSecond(Span((0, 0), (0, 0)), mSPO2IL.GetId("...*..."), mSPO2IL.GetRegId(2)),
								mIL_AST.GetFirst(Span((0, 0), (0, 0)), mSPO2IL.GetRegId(3), mSPO2IL.GetRegId(2)),
								
								mIL_AST.CallFunc(Span((1, 10), (1, 32)), mSPO2IL.GetRegId(1), mSPO2IL.GetDefId(0), mSPO2IL.GetId("...*...")),
								mIL_AST.Alias(Span((1, 1), (1, 6)), mSPO2IL.GetId("x"), mSPO2IL.GetRegId(1)),
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
					
					mAssert.AreEquals(
						DefConstructor.EnvIds.ToStream(),
						mStream.Stream(
							[
								mSPO2IL.GetId("...*..."),
								mSPO2IL.GetDefId(0)
							]
						)
					);
				}
			),
			mTest.Test("MapLambda2",
				aStreamOut => {
					var DefNode = mSPO_Parser.Def.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"§DEF ...*...+... = (§DEF a € §INT, §DEF b € §INT, §DEF c € §INT) => (a .* b) .+ c",
						"",
						_ => aStreamOut(_())
					);
					
					var InitScope = mStream.Stream(
						[
							(
								Id: mSPO2IL.GetId("...*..."),
								Type: mVM_Type.Proc(
									mVM_Type.Empty(),
									mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
									mVM_Type.Int()
								)
							),
							(
								Id: mSPO2IL.GetId("...+..."),
								Type: mVM_Type.Proc(
									mVM_Type.Empty(),
									mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
									mVM_Type.Int()
								)
							)
						]
					);
					
					var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, InitScope).ElseThrow();
					var ExpScope = mStream.Stream(
						(
							Id: mSPO2IL.GetId("...*...+..."),
							Type: mVM_Type.Proc(
								mVM_Type.Empty(),
								mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int()]),
								mVM_Type.Int()
							)
						),
						InitScope
					);
					mAssert.AreEquals(Scope, ExpScope);
					
					var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var DefConstructor = mSPO2IL.NewDefConstructor<tSpan>();
					DefConstructor.MapDef(Module, DefNode);
					DefConstructor.FinishMapProc(
						default,
						Module,
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Pair(
								mVM_Type.Proc( // _...+... € [§INT, §INT] => §INT
									mVM_Type.Empty(),
									mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
									mVM_Type.Int()
								),
								mVM_Type.Pair(
									mVM_Type.Proc( // _...*... € [§INT, §INT] => §INT
										mVM_Type.Empty(),
										mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
										mVM_Type.Int()
									),
									mVM_Type.Proc( // d_0 € [[[[§INT, §INT] => §INT]; [[§INT, §INT] => §INT]] => [§INT => §INT]]
										mVM_Type.Empty(),
										mVM_Type.Pair(
											mVM_Type.Proc( // _...+... € [§INT, §INT] => §INT
												mVM_Type.Empty(),
												mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
												mVM_Type.Int()
											),
											mVM_Type.Proc( // _...*... € [§INT, §INT] => §INT
												mVM_Type.Empty(),
												mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
												mVM_Type.Int()
											)
										),
										mVM_Type.Proc( // _...*...+... € [§INT, §INT, §INT] => §INT
											mVM_Type.Empty(),
											mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int()]), // (_a, _b, _c)
											mVM_Type.Int()
										)
									)
								)
							),
							mVM_Type.Proc( // not defied now
								mVM_Type.Empty(),
								mVM_Type.Empty(),
								mVM_Type.Empty()
							)
						)
					);
					
					foreach (var (DefTypeId, DefCommands) in Module.Defs.ToStream()) {
						aStreamOut("------------------");
						aStreamOut(DefTypeId);
						aStreamOut("------------------");
						foreach (var Command in DefCommands.ToStream()) {
							aStreamOut(Command.ToText());
						}
					}
					
					mAssert.AreEquals(Module.Defs.Size(), 2u);
					mAssert.AreEquals(
						Module.Defs.Get(0).Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.GetSecond(Span((1, 20), (1, 81)), mSPO2IL.GetId("...*..."), mIL_AST.cEnv),
								mIL_AST.GetFirst(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(13), mIL_AST.cEnv),
								mIL_AST.GetSecond(Span((1, 20), (1, 81)), mSPO2IL.GetId("...+..."), mSPO2IL.GetRegId(13)), // TODO
								mIL_AST.GetFirst(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(14), mSPO2IL.GetRegId(13)),
								
								mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(1), mIL_AST.cArg),
								mIL_AST.Alias(Span((1, 51), (1, 63)), mSPO2IL.GetId("c"), mSPO2IL.GetRegId(1)),
								mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(2), mIL_AST.cArg),
								mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(3), mSPO2IL.GetRegId(2)),
								mIL_AST.Alias(Span((1, 36), (1, 48)), mSPO2IL.GetId("b"), mSPO2IL.GetRegId(3)),
								mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(2)),
								mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(5), mSPO2IL.GetRegId(4)),
								mIL_AST.Alias(Span((1, 21), (1, 33)), mSPO2IL.GetId("a"), mSPO2IL.GetRegId(5)),
								mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(4)),
								
								mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(7), mIL_AST.cEmptyValue, mSPO2IL.GetId("a")),
								mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(7), mSPO2IL.GetId("b")),
								mIL_AST.CallFunc(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(9), mSPO2IL.GetId("...*..."), mSPO2IL.GetRegId(8)),
								mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(10), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(9)),
								mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(11), mSPO2IL.GetRegId(10), mSPO2IL.GetId("c")),
								mIL_AST.CallFunc(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(12), mSPO2IL.GetId("...+..."), mSPO2IL.GetRegId(11)),
								mIL_AST.ReturnIf(Span((1, 69), (1, 81)), mIL_AST.cTrue, mSPO2IL.GetRegId(12))
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
					
					mAssert.AreEquals(
						Module.Defs.Get(1).Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.GetSecond(Span((0, 0), (0, 0)), mSPO2IL.GetDefId(0), mIL_AST.cEnv),
								mIL_AST.GetFirst(Span((0, 0), (0, 0)), mSPO2IL.GetRegId(4), mIL_AST.cEnv),
								mIL_AST.GetSecond(Span((0, 0), (0, 0)), mSPO2IL.GetId("...*..."), mSPO2IL.GetRegId(4)),
								mIL_AST.GetFirst(Span((0, 0), (0, 0)), mSPO2IL.GetRegId(5), mSPO2IL.GetRegId(4)),
								mIL_AST.GetSecond(Span((0, 0), (0, 0)), mSPO2IL.GetId("...+..."), mSPO2IL.GetRegId(5)),
								mIL_AST.GetFirst(Span((0, 0), (0, 0)), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(5)),
								
								mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(1), mIL_AST.cEmptyValue, mSPO2IL.GetId("...+...")),
								mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(1), mSPO2IL.GetId("...*...")),
								mIL_AST.CallFunc(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(3), mSPO2IL.GetDefId(0), mSPO2IL.GetRegId(2)),
								mIL_AST.Alias(Span((1, 1), (1, 16)), mSPO2IL.GetId("...*...+..."), mSPO2IL.GetRegId(3))
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
					
					mAssert.AreEquals(
						DefConstructor.EnvIds.ToStream(),
						mStream.Stream(
							[
								mSPO2IL.GetId("...+..."),
								mSPO2IL.GetId("...*..."),
								mSPO2IL.GetDefId(0)
							]
						)
					);
				}
			),
			mTest.Test("MapLambda3",
				aStreamOut => {
					var DefNode = mSPO_Parser.Def.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"§DEF TestTest... = (§DEF a € §INT, §DEF b € §INT, §DEF c € §INT) => (a .* b) .> c",
						"",
						_ => aStreamOut(_())
					);
					
					var InitScope = mStream.Stream(
						[
							(
								Id: mSPO2IL.GetId("...*..."),
								Type: mVM_Type.Proc(
									mVM_Type.Empty(),
									mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
									mVM_Type.Int()
								)
							),
							(
								Id: mSPO2IL.GetId("...>..."),
								Type: mVM_Type.Proc(
									mVM_Type.Empty(),
									mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
									mVM_Type.Bool()
								)
							)]
					);
					
					var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, InitScope).ElseThrow();
					var ExpScope = mStream.Stream(
						(
							Id: mSPO2IL.GetId("TestTest..."),
							Type: mVM_Type.Proc(
								mVM_Type.Empty(),
								mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int()]),
								mVM_Type.Bool()
							)
						),
						InitScope
					);
					mAssert.AreEquals(Scope, ExpScope);
					
					var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var DefConstructor = mSPO2IL.NewDefConstructor<tSpan>();
					DefConstructor.MapDef(Module, DefNode);
					DefConstructor.FinishMapProc(
						default,
						Module,
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Pair(
								mVM_Type.Proc( // _...*... € [§INT, §INT] => §INT
									mVM_Type.Empty(),
									mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
									mVM_Type.Int()
								),
								mVM_Type.Pair(
									mVM_Type.Proc( // _...>... € [§INT, §INT] => §BOOL
										mVM_Type.Empty(),
										mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
										mVM_Type.Bool()
									),
									mVM_Type.Proc( // d_0 € [[[[§INT, §INT] => §INT]; [[§INT, §INT] => §BOOL]] => [§INT => §INT]]
										mVM_Type.Empty(),
										mVM_Type.Pair(
											mVM_Type.Proc( // _...*... € [§INT, §INT] => §INT
												mVM_Type.Empty(),
												mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
												mVM_Type.Int()
											),
											mVM_Type.Proc( // _...>... € [§INT, §INT] => §BOOL
												mVM_Type.Empty(),
												mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
												mVM_Type.Bool()
											)
										),
										mVM_Type.Proc( // TestTest... € [§INT, §INT, §INT] => §BOOL
											mVM_Type.Empty(),
											mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int()]), // (_a, _b, _c)
											mVM_Type.Int()
										)
									)
								)
							),
							mVM_Type.Proc( // not defied now
								mVM_Type.Empty(),
								mVM_Type.Empty(),
								mVM_Type.Empty()
							)
						)
					);
					
					mAssert.AreEquals(Module.Defs.Size(), 2u);
					mAssert.AreEquals(
						Module.Defs.Get(0).Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.GetSecond(Span((1, 20), (1, 81)), mSPO2IL.GetId("...*..."), mIL_AST.cEnv),
								mIL_AST.GetFirst(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(13), mIL_AST.cEnv),
								mIL_AST.GetSecond(Span((1, 20), (1, 81)), mSPO2IL.GetId("...>..."), mSPO2IL.GetRegId(13)), // TODO
								mIL_AST.GetFirst(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(14), mSPO2IL.GetRegId(13)),
								
								mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(1), mIL_AST.cArg),
								mIL_AST.Alias(Span((1, 51), (1, 63)), mSPO2IL.GetId("c"), mSPO2IL.GetRegId(1)),
								mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(2), mIL_AST.cArg),
								mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(3), mSPO2IL.GetRegId(2)),
								mIL_AST.Alias(Span((1, 36), (1, 48)), mSPO2IL.GetId("b"), mSPO2IL.GetRegId(3)),
								mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(2)),
								mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(5), mSPO2IL.GetRegId(4)),
								mIL_AST.Alias(Span((1, 21), (1, 33)), mSPO2IL.GetId("a"), mSPO2IL.GetRegId(5)),
								mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(4)),
								
								mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(7), mIL_AST.cEmptyValue, mSPO2IL.GetId("a")),
								mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(7), mSPO2IL.GetId("b")),
								mIL_AST.CallFunc(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(9), mSPO2IL.GetId("...*..."), mSPO2IL.GetRegId(8)),
								mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(10), mIL_AST.cEmptyValue, mSPO2IL.GetRegId(9)),
								mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(11), mSPO2IL.GetRegId(10), mSPO2IL.GetId("c")),
								mIL_AST.CallFunc(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(12), mSPO2IL.GetId("...>..."), mSPO2IL.GetRegId(11)),
								mIL_AST.ReturnIf(Span((1, 69), (1, 81)), mIL_AST.cTrue, mSPO2IL.GetRegId(12))
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
					
					mAssert.AreEquals(
						Module.Defs.Get(1).Commands.ToStream(),
						mStream.Stream(
							[
								mIL_AST.GetSecond(Span((0, 0), (0, 0)), mSPO2IL.GetDefId(0), mIL_AST.cEnv),
								mIL_AST.GetFirst(Span((0, 0), (0, 0)), mSPO2IL.GetRegId(4), mIL_AST.cEnv),
								mIL_AST.GetSecond(Span((0, 0), (0, 0)), mSPO2IL.GetId("...*..."), mSPO2IL.GetRegId(4)),
								mIL_AST.GetFirst(Span((0, 0), (0, 0)), mSPO2IL.GetRegId(5), mSPO2IL.GetRegId(4)),
								mIL_AST.GetSecond(Span((0, 0), (0, 0)), mSPO2IL.GetId("...>..."), mSPO2IL.GetRegId(5)),
								mIL_AST.GetFirst(Span((0, 0), (0, 0)), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(5)),
								
								mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(1), mIL_AST.cEmptyValue, mSPO2IL.GetId("...>...")),
								mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(1), mSPO2IL.GetId("...*...")),
								mIL_AST.CallFunc(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(3), mSPO2IL.GetDefId(0), mSPO2IL.GetRegId(2)),
								mIL_AST.Alias(Span((1, 1), (1, 16)), mSPO2IL.GetId("TestTest..."), mSPO2IL.GetRegId(3))
							]
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
					
					mAssert.AreEquals(
						DefConstructor.EnvIds.ToStream(),
						mStream.Stream(
							[
								mSPO2IL.GetId("...>..."),
								mSPO2IL.GetId("...*..."),
								mSPO2IL.GetDefId(0)
							]
						)
					);
				}
			),
			mTest.Test("MapIfMatch_1",
				aStreamOut => {
					var DefNode = mSPO_Parser.Def.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"""
						§DEF x = §IF 1 MATCH {
							0 => 2
							1 => 4
							_ => 6
						}
						""",
						"",
						_ => aStreamOut(_())
					);
					
					var InitScope = mStream.Stream<(tText Id, mVM_Type.tType Type)>([]);
					var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, InitScope).ElseThrow();
					mAssert.AreEquals(
						Scope,
						mStream.Stream(
							(
								Id: mSPO2IL.GetId("x"),
								Type: mVM_Type.Int()
							),
							InitScope
						)
					);
					
					var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var DefConstructor = mSPO2IL.NewDefConstructor<tSpan>();
					
					DefConstructor.MapDef(Module, DefNode);
					DefConstructor.FinishMapProc(
						default,
						Module,
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Proc(
								mVM_Type.Empty(),
								mVM_Type.Empty(),
								mVM_Type.Empty()
							)
						)
					);
					
					PrintModuleDefs(Module.Defs.ToStream(), aStreamOut);
					
					AssertDefsAre(
						aStreamOut,
						Module.Defs.ToStream(),
						[
							"""
							r_1 := 2
							§RETURN r_1 IF TRUE
							""",
							"""
							d_0 := ENV
							r_1 := .d_0 EMPTY
							r_2 := §TRY ARG AS_INT
							r_3 := 0
							r_4 := §INT r_2 == r_3
							r_5 := §BOOL r_4 ^ TRUE
							§RETURN EMPTY IF r_5
							r_6 := .r_1 r_2
							§RETURN r_6 IF TRUE
							""",
							"""
							r_1 := 4
							§RETURN r_1 IF TRUE
							""",
							"""
							d_2 := ENV
							r_1 := .d_2 EMPTY
							r_2 := §TRY ARG AS_INT
							r_3 := 1
							r_4 := §INT r_2 == r_3
							r_5 := §BOOL r_4 ^ TRUE
							§RETURN EMPTY IF r_5
							r_6 := .r_1 r_2
							§RETURN r_6 IF TRUE
							""",
							"""
							r_1 := 6
							§RETURN r_1 IF TRUE
							""",
							"""
							d_4 := §2ND ENV
							r_7 := §1ST ENV
							d_3 := §2ND r_7
							r_8 := §1ST r_7
							d_2 := §2ND r_8
							r_9 := §1ST r_8
							d_1 := §2ND r_9
							r_10 := §1ST r_9
							d_0 := §2ND r_10
							r_11 := §1ST r_10
							r_1 := .d_1 d_0
							r_2 := .r_1 ARG
							§RETURN r_2 IF_NOT_EMPTY
							r_3 := .d_3 d_2
							r_4 := .r_3 ARG
							§RETURN r_4 IF_NOT_EMPTY
							r_5 := .d_4 EMPTY
							r_6 := .r_5 ARG
							§RETURN r_6 IF_NOT_EMPTY
							§RETURN EMPTY IF TRUE
							""",
							"""
							d_5 := §2ND ENV
							r_9 := §1ST ENV
							d_4 := §2ND r_9
							r_10 := §1ST r_9
							d_3 := §2ND r_10
							r_11 := §1ST r_10
							d_2 := §2ND r_11
							r_12 := §1ST r_11
							d_1 := §2ND r_12
							r_13 := §1ST r_12
							d_0 := §2ND r_13
							r_14 := §1ST r_13
							r_1 := 1
							r_2 := EMPTY, d_0
							r_3 := r_2, d_1
							r_4 := r_3, d_2
							r_5 := r_4, d_3
							r_6 := r_5, d_4
							r_7 := .d_5 r_6
							r_8 := .r_7 r_1
							_x := r_8
							""",
						]
					);
				}
			),
			mTest.Test("MapIfMatch_2_WithSet",
				aStreamOut => {
					var ModuleNode = mSPO_Parser.Module.ParseText(
						//    1     2     3     4     5     6     7     8
						//345678901234567890123456789012345678901234567890123456789012345678901234567890
						"""
						§IMPORT ()
						
						§DEF X € [[#Bla []] | [#Blub []]] = #Bla ()
						
						§EXPORT §IF X MATCH {
							(#Blub ()) => 1
							(#Bla ()) => 2
							_ => 3
						}
						""",
						"",
						_ => aStreamOut(_())
					);
					
					var InitScope = mSPO_AST_Types.UpdateMatchTypes(
						ModuleNode.Import.Match,
						mStd.cEmpty,
						mSPO_AST_Types.tTypeRelation.Sub,
						mStd.cEmpty
					).Then(
						_ => _.Scope
					).ElseThrow(
					);
					
					var Scope = ModuleNode.Commands.Reduce(
						mResult.OK(InitScope).AsResult<tText>(),
						(aResultScope, aCommand) => aResultScope.ThenTry(
							aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope)
						)
					).ElseThrow(
					);
					
					var Module = mSPO2IL.MapModule(ModuleNode, mSpan.Merge, InitScope);
					
					PrintModuleDefs(Module.Defs.ToStream(), aStreamOut);
					
					AssertDefsAre(
						aStreamOut,
						Module.Defs.ToStream(),
						[
							"""
							r_1 := 1
							§RETURN r_1 IF TRUE
							""",
							"""
							d_0 := ENV
							r_1 := .d_0 EMPTY
							r_2 := §TRY_REMOVE #_Blub... FROM ARG
							r_3 := .r_1 r_2
							§RETURN r_3 IF TRUE
							""",
							"""
							r_1 := 2
							§RETURN r_1 IF TRUE
							""",
							"""
							d_2 := ENV
							r_1 := .d_2 EMPTY
							r_2 := §TRY_REMOVE #_Bla... FROM ARG
							r_3 := .r_1 r_2
							§RETURN r_3 IF TRUE
							""",
							"""
							r_1 := 3
							§RETURN r_1 IF TRUE
							""",
							"""
							d_4 := §2ND ENV
							r_7 := §1ST ENV
							d_3 := §2ND r_7
							r_8 := §1ST r_7
							d_2 := §2ND r_8
							r_9 := §1ST r_8
							d_1 := §2ND r_9
							r_10 := §1ST r_9
							d_0 := §2ND r_10
							r_11 := §1ST r_10
							r_1 := .d_1 d_0
							r_2 := .r_1 ARG
							§RETURN r_2 IF_NOT_EMPTY
							r_3 := .d_3 d_2
							r_4 := .r_3 ARG
							§RETURN r_4 IF_NOT_EMPTY
							r_5 := .d_4 EMPTY
							r_6 := .r_5 ARG
							§RETURN r_6 IF_NOT_EMPTY
							§RETURN EMPTY IF TRUE
							""",
							"""
							d_5 := §2ND ENV
							r_9 := §1ST ENV
							d_4 := §2ND r_9
							r_10 := §1ST r_9
							d_3 := §2ND r_10
							r_11 := §1ST r_10
							d_2 := §2ND r_11
							r_12 := §1ST r_11
							d_1 := §2ND r_12
							r_13 := §1ST r_12
							d_0 := §2ND r_13
							r_14 := §1ST r_13
							r_1 := +#_Bla... EMPTY
							_X := r_1
							r_2 := EMPTY, d_0
							r_3 := r_2, d_1
							r_4 := r_3, d_2
							r_5 := r_4, d_3
							r_6 := r_5, d_4
							r_7 := .d_5 r_6
							r_8 := .r_7 _X
							§RETURN r_8 IF TRUE
							""",
						]
					);
				}
			),
			mTest.Test("MapNestedMatch",
				aStreamOut => {
					var LambdaNode = mSPO_Parser.Lambda.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"(§DEF a € §INT, §DEF b € §INT, (§DEF x € §INT, §DEF y € §INT, §DEF z € §INT)) => a .* z",
						"",
						_ => aStreamOut(_())
					);
					
					var InitScope = mStream.Stream(
						[
							(
								Id: mSPO2IL.GetId("...*..."),
								Type: mVM_Type.Proc(
									mVM_Type.Empty(),
									mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
									mVM_Type.Int()
								)
							)
						]
					);
					
					var ModuleConstructor = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
					var Type = mSPO_AST_Types.UpdateExpressionTypes(LambdaNode, InitScope);
					if (Type.Match(out var Type_, out var Error)) {
						mAssert.AreEquals(
							Type_,
							mVM_Type.Proc(
								mVM_Type.Empty(),
								mVM_Type.Tuple(
									[
										mVM_Type.Int(),
										mVM_Type.Int(),
										mVM_Type.Tuple(
											[
												mVM_Type.Int(),
												mVM_Type.Int(),
												mVM_Type.Int()
											]
										)
									]
								),
								mVM_Type.Int()
							),
							(a1, a2) => a1 == a2
						);
					} else {
						mAssert.Fail(Error);
					}
					
					var DefConstructor = mSPO2IL.NewDefConstructor<tSpan>();
					
					var (DefIndex, DefType) = DefConstructor.MapLambda(
						ModuleConstructor,
						LambdaNode
					);
					
					mAssert.AreEquals(ModuleConstructor.Defs.Size(), 1u);
					mAssert.AreEquals(DefIndex, 0u);
					mAssert.AreEquals(
						ModuleConstructor.Defs.Get(DefIndex).Commands.ToStream(),
						mStream.Stream<(tText Command, (tNat32, tNat32) Start, (tNat32, tNat32) End)>(
							[
								("_...*... := ENV", (1, 1), (1, 87)),
								("r_1 := §2ND ARG", (1, 1), (1, 77)),
								("r_2 := §2ND r_1", (1, 32), (1, 76)),
								("_z := r_2", (1, 63), (1, 75)),
								("r_3 := §1ST r_1", (1, 32), (1, 76)),
								("r_4 := §2ND r_3", (1, 32), (1, 76)),
								("_y := r_4", (1, 48), (1, 60)),
								("r_5 := §1ST r_3", (1, 32), (1, 76)),
								("r_6 := §2ND r_5", (1, 32), (1, 76)),
								("_x := r_6", (1, 33), (1, 45)),
								("r_7 := §1ST r_5", (1, 32), (1, 76)),
								("r_8 := §1ST ARG", (1, 1), (1, 77)),
								("r_9 := §2ND r_8", (1, 1), (1, 77)),
								("_b := r_9", (1, 17), (1, 29)),
								("r_10 := §1ST r_8", (1, 1), (1, 77)),
								("r_11 := §2ND r_10", (1, 1), (1, 77)),
								("_a := r_11", (1, 2), (1, 14)),
								("r_12 := §1ST r_10", (1, 1), (1, 77)),
								("r_13 := EMPTY, _a", (1, 82), (1, 87)),
								("r_14 := r_13, _z", (1, 82), (1, 87)),
								("r_15 := ._...*... r_14", (1, 82), (1, 87)),
								("§RETURN r_15 IF TRUE", (1, 82), (1, 87))
							]
						).Map(
							_ => ParseCommand(_.Command, Span(_.Start, _.End), aStreamOut)
						),
						mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
					);
					
					mAssert.AreEquals(
						DefConstructor.EnvIds.ToStream(),
						mStream.Stream([mSPO2IL.GetId("...*...")])
					);
				}
			),
			mTest.Test("MapModule",
				aStreamOut => {
					var ModuleNode = mSPO_Parser.Module.ParseText(
						//    1     2     3     4     5     6     7     8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"""
						§IMPORT (
							§DEF T € [[]]
							§DEF ...*... € [[T, T] => T]
							§DEF k € T
						)
						
						§DEF x... = (§DEF a € T) => k .* a
						§DEF y = .x k
						
						§EXPORT y
						""",
						"",
						_ => aStreamOut(_())
					);
					
					var InitScope = mSPO_AST_Types.UpdateMatchTypes(
						ModuleNode.Import.Match,
						mStd.cEmpty,
						mSPO_AST_Types.tTypeRelation.Sub,
						mStd.cEmpty
					).Then(
						_ => _.Scope
					).ElseThrow(
					);
					
					var ModuleConstructor = mSPO2IL.MapModule(ModuleNode, mSpan.Merge, mStd.cEmpty);
					
					AssertModuleDefs(
						aStreamOut,
						ModuleConstructor.Defs,
						[
							[
								("_k := §2ND ENV", (7, 13), (7, 34)),
								("r_4 := §1ST ENV", (7, 13), (7, 34)),
								("_...*... := §2ND r_4", (7, 13), (7, 34)),
								("r_5 := §1ST r_4", (7, 13), (7, 34)),
								("_a := ARG", (7, 14), (7, 23)),
								("r_1 := EMPTY, _k", (7, 29), (7, 34)),
								("r_2 := r_1, _a", (7, 29), (7, 34)),
								("r_3 := ._...*... r_2", (7, 29), (7, 34)),
								("§RETURN r_3 IF TRUE", (7, 29), (7, 34)),
							],
							[
								("d_0 := ENV", (1, 1), (10, 9)),
								("r_1 := §2ND ARG", (1, 9), (5, 1)),
								("_k := r_1", (4, 2), (4, 11)),
								("r_2 := §1ST ARG", (1, 9), (5, 1)),
								("r_3 := §2ND r_2", (1, 9), (5, 1)),
								("_...*... := r_3", (3, 2), (3, 29)),
								("r_4 := §1ST r_2", (1, 9), (5, 1)),
								("r_5 := §2ND r_4", (1, 9), (5, 1)),
								("_T := r_5", (2, 2), (2, 14)),
								("r_6 := §1ST r_4", (1, 9), (5, 1)),
								("r_7 := EMPTY, _...*...", (7, 13), (7, 34)),
								("r_8 := r_7, _k", (7, 13), (7, 34)),
								("r_9 := .d_0 r_8", (7, 13), (7, 34)),
								("_x... := r_9", (7, 1), (7, 9)),
								("r_10 := ._x... _k", (8, 10), (8, 13)),
								("_y := r_10", (8, 1), (8, 6)),
								("§RETURN _y IF TRUE", (10, 1), (10, 9)),
							],
						]
					);
				}
			),
			mTest.Test("MapRecursion",
				aStreamOut => {
					var ModuleNode = mSPO_Parser.Module.ParseText(
						//        1         2         3         4         5         6         7         8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"""
						§IMPORT (
							§DEF ...+... € [[§INT, §INT] => §INT]
							§DEF ...-... € [[§INT, §INT] => §INT]
						)
						
						§RECURSIVE {
							§DEF Fib... = (§DEF a € §INT) => §IF a MATCH {
								0 => 0
								§DEF b => (.Fib(b .- 2)) .+ (.Fib(b .- 1))
							}
						}
						
						§EXPORT Fib...
						""",
						"",
						_ => aStreamOut(_())
					);
					
					var InitScope = mSPO_AST_Types.UpdateMatchTypes(
						ModuleNode.Import.Match,
						mStd.cEmpty,
						mSPO_AST_Types.tTypeRelation.Sub,
						mStd.cEmpty
					).Then(
						_ => _.Scope
					).ElseThrow(
					);
					
					var ModuleConstructor = mSPO2IL.MapModule(ModuleNode, mSpan.Merge, mStd.cEmpty);
					
					AssertModuleDefs(
						aStreamOut,
						ModuleConstructor.Defs,
						[
							[ // Def 0   => 0
								("r_1 := 0", (8, 8), (8, 8)),
								("§RETURN r_1 IF TRUE", (8, 3), (8, 8)),
							],
							[ // Def 1   0 =>
								("d_0 := ENV", (7, 35), (10, 2)),
								("r_1 := .d_0 EMPTY", (8, 3), (8, 3)),
								("r_2 := §TRY ARG AS_INT", (8, 3), (8, 8)),
								("r_3 := 0", (8, 3), (8, 8)),
								("r_4 := §INT r_2 == r_3", (8, 3), (8, 8)),
								("r_5 := §BOOL r_4 ^ TRUE", (8, 3), (8, 8)),
								("§RETURN EMPTY IF r_5", (8, 3), (8, 8)),
								("r_6 := .r_1 r_2", (8, 3), (8, 8)),
								("§RETURN r_6 IF TRUE", (8, 3), (8, 8)),
							],
							[ // Def 2   => (.Fib(b .- 2)) .+ (.Fib(b .- 1))
								("_...-... := §2ND ENV", (7, 35), (10, 2)),
								("r_14 := §1ST ENV", (7, 35), (10, 2)),
								("_Fib... := §2ND r_14", (7, 35), (10, 2)),
								("r_15 := §1ST r_14", (7, 35), (10, 2)),
								("_...+... := §2ND r_15", (7, 35), (10, 2)),
								("r_16 := §1ST r_15", (7, 35), (10, 2)),
								("_b := ARG", (9, 3), (9, 44)),
								("r_1 := EMPTY, _b", (9, 19), (9, 24)),
								("r_2 := 2", (9, 24), (9, 24)),
								("r_3 := r_1, r_2", (9, 19), (9, 24)),
								("r_4 := ._...-... r_3", (9, 19), (9, 24)),
								("r_5 := ._Fib... r_4", (9, 14), (9, 25)),
								("r_6 := EMPTY, r_5", (9, 13), (9, 44)),
								("r_7 := EMPTY, _b", (9, 37), (9, 42)),
								("r_8 := 1", (9, 42), (9, 42)),
								("r_9 := r_7, r_8", (9, 37), (9, 42)),
								("r_10 := ._...-... r_9", (9, 37), (9, 42)),
								("r_11 := ._Fib... r_10", (9, 32), (9, 43)),
								("r_12 := r_6, r_11", (9, 13), (9, 44)),
								("r_13 := ._...+... r_12", (9, 13), (9, 44)),
								("§RETURN r_13 IF TRUE", (9, 3), (9, 44)),
							],
							[ // Def 3   §IF a MATCH {
								("d_2 := §2ND ENV", (7, 35), (10, 2)),
								("r_8 := §1ST ENV", (7, 35), (10, 2)),
								("_...-... := §2ND r_8", (7, 35), (10, 2)),
								("r_9 := §1ST r_8", (7, 35), (10, 2)),
								("_Fib... := §2ND r_9", (7, 35), (10, 2)),
								("r_10 := §1ST r_9", (7, 35), (10, 2)),
								("_...+... := §2ND r_10", (7, 35), (10, 2)),
								("r_11 := §1ST r_10", (7, 35), (10, 2)),
								("d_1 := §2ND r_11", (7, 35), (10, 2)),
								("r_12 := §1ST r_11", (7, 35), (10, 2)),
								("d_0 := §2ND r_12", (7, 35), (10, 2)),
								("r_13 := §1ST r_12", (7, 35), (10, 2)),
								("r_1 := .d_1 d_0", (7, 35), (10, 2)),
								("r_2 := .r_1 ARG", (8, 3), (8, 8)),
								("§RETURN r_2 IF_NOT_EMPTY", (8, 3), (8, 8)),
								("r_3 := EMPTY, _...+...", (7, 35), (10, 2)),
								("r_4 := r_3, _Fib...", (7, 35), (10, 2)),
								("r_5 := r_4, _...-...", (7, 35), (10, 2)),
								("r_6 := .d_2 r_5", (7, 35), (10, 2)),
								("r_7 := .r_6 ARG", (9, 3), (9, 44)),
								("§RETURN r_7 IF_NOT_EMPTY", (9, 3), (9, 44)),
								("§RETURN EMPTY IF TRUE", (7, 35), (10, 2)),
							],
							[ // Def 4    Def_Fib...
								("d_3 := §2ND ENV", (7, 16), (10, 2)),
								("r_9 := §1ST ENV", (7, 16), (10, 2)),
								("d_2 := §2ND r_9", (7, 16), (10, 2)),
								("r_10 := §1ST r_9", (7, 16), (10, 2)),
								("_...-... := §2ND r_10", (7, 16), (10, 2)),
								("r_11 := §1ST r_10", (7, 16), (10, 2)),
								("_Fib... := §2ND r_11", (7, 16), (10, 2)),
								("r_12 := §1ST r_11", (7, 16), (10, 2)),
								("_...+... := §2ND r_12", (7, 16), (10, 2)),
								("r_13 := §1ST r_12", (7, 16), (10, 2)),
								("d_1 := §2ND r_13", (7, 16), (10, 2)),
								("r_14 := §1ST r_13", (7, 16), (10, 2)),
								("d_0 := §2ND r_14", (7, 16), (10, 2)),
								("r_15 := §1ST r_14", (7, 16), (10, 2)),
								("_a := ARG", (7, 17), (7, 29)),
								("r_1 := EMPTY, d_0", (7, 35), (10, 2)),
								("r_2 := r_1, d_1", (7, 35), (10, 2)),
								("r_3 := r_2, _...+...", (7, 35), (10, 2)),
								("r_4 := r_3, _Fib...", (7, 35), (10, 2)),
								("r_5 := r_4, _...-...", (7, 35), (10, 2)),
								("r_6 := r_5, d_2", (7, 35), (10, 2)),
								("r_7 := .d_3 r_6", (7, 35), (10, 2)),
								("r_8 := .r_7 _a", (7, 35), (10, 2)),
								("§RETURN r_8 IF TRUE", (7, 35), (10, 2)),
							],
							[ // Def 5    RecFactory
								("d_4 := §2ND ENV", (6, 1), (11, 1)),
								("r_9 := §1ST ENV", (6, 1), (11, 1)),
								("d_3 := §2ND r_9", (6, 1), (11, 1)),
								("r_10 := §1ST r_9", (6, 1), (11, 1)),
								("d_2 := §2ND r_10", (6, 1), (11, 1)),
								("r_11 := §1ST r_10", (6, 1), (11, 1)),
								("_...-... := §2ND r_11", (6, 1), (11, 1)),
								("r_12 := §1ST r_11", (6, 1), (11, 1)),
								("_...+... := §2ND r_12", (6, 1), (11, 1)),
								("r_13 := §1ST r_12", (6, 1), (11, 1)),
								("d_1 := §2ND r_13", (6, 1), (11, 1)),
								("r_14 := §1ST r_13", (6, 1), (11, 1)),
								("d_0 := §2ND r_14", (6, 1), (11, 1)),
								("r_15 := §1ST r_14", (6, 1), (11, 1)),
								("_Fib... := ARG", (7, 2), (10, 2)),
								("r_1 := EMPTY, d_0", (7, 2), (10, 2)),
								("r_2 := r_1, d_1", (7, 2), (10, 2)),
								("r_3 := r_2, _...+...", (7, 2), (10, 2)),
								("r_4 := r_3, _Fib...", (7, 2), (10, 2)),
								("r_5 := r_4, _...-...", (7, 2), (10, 2)),
								("r_6 := r_5, d_2", (7, 2), (10, 2)),
								("r_7 := r_6, d_3", (7, 2), (10, 2)),
								("r_8 := .d_4 r_7", (7, 2), (10, 2)),
								("§RETURN r_8 IF TRUE", (6, 1), (11, 1))
							],
							[ // Def 6    Init
								("d_5 := §2ND ENV", (1, 1), (13, 14)),
								("r_13 := §1ST ENV", (1, 1), (13, 14)),
								("d_4 := §2ND r_13", (1, 1), (13, 14)),
								("r_14 := §1ST r_13", (1, 1), (13, 14)),
								("d_3 := §2ND r_14", (1, 1), (13, 14)),
								("r_15 := §1ST r_14", (1, 1), (13, 14)),
								("d_2 := §2ND r_15", (1, 1), (13, 14)),
								("r_16 := §1ST r_15", (1, 1), (13, 14)),
								("d_1 := §2ND r_16", (1, 1), (13, 14)),
								("r_17 := §1ST r_16", (1, 1), (13, 14)),
								("d_0 := §2ND r_17", (1, 1), (13, 14)),
								("r_18 := §1ST r_17", (1, 1), (13, 14)),
								("r_1 := §2ND ARG", (1, 9), (4, 1)),
								("_...-... := r_1", (3, 2), (3, 38)),
								("r_2 := §1ST ARG", (1, 9), (4, 1)),
								("r_3 := §2ND r_2", (1, 9), (4, 1)),
								("_...+... := r_3", (2, 2), (2, 38)),
								("r_4 := §1ST r_2", (1, 9), (4, 1)),
								("r_5 := EMPTY, d_0", (6, 1), (11, 1)),
								("r_6 := r_5, d_1", (6, 1), (11, 1)),
								("r_7 := r_6, _...+...", (6, 1), (11, 1)),
								("r_8 := r_7, _...-...", (6, 1), (11, 1)),
								("r_9 := r_8, d_2", (6, 1), (11, 1)),
								("r_10 := r_9, d_3", (6, 1), (11, 1)),
								("r_11 := r_10, d_4", (6, 1), (11, 1)),
								("§REC r_12 := .d_5 r_11", (6, 1), (11, 1)),
								("_Fib... := r_12", (7, 2), (10, 2)),
								("§RETURN _Fib... IF TRUE", (13, 1), (13, 14)),
							]
						]
					);
				}
			),
			mTest.Test("MapRecursion2",
				aStreamOut => {
					var ModuleNode = mSPO_Parser.Module.ParseText(
						//        1         2         3         4         5         6         7         8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"""
						§IMPORT (
							§DEF ...+... € [[§INT, §INT] => §INT]
							§DEF ...-... € [[§INT, §INT] => §INT]
						)
						
						§RECURSIVE {
							§DEF Fib1... = (§DEF a € §INT) => §IF a MATCH {
								0 => 0
								§DEF b => (.Fib2(b .- 2)) .+ (.Fib2(b .- 1))
							}
							
							§DEF Fib2... = (§DEF a € §INT) => §IF a MATCH {
								0 => 0
								§DEF b => (.Fib1(b .- 2)) .+ (.Fib1(b .- 1))
							}
						}
						
						§EXPORT Fib1...
						""",
						"",
						_ => aStreamOut(_())
					);
					
					var InitScope = mSPO_AST_Types.UpdateMatchTypes(
						ModuleNode.Import.Match,
						mStd.cEmpty,
						mSPO_AST_Types.tTypeRelation.Sub,
						mStd.cEmpty
					).Then(
						_ => _.Scope
					).ElseThrow(
					);
					
					var ModuleConstructor = mSPO2IL.MapModule(ModuleNode, mSpan.Merge, mStd.cEmpty);
				}
			)
		]
	);
	
	private static mIL_AST.tCommandNode<tSpan>
	ParseCommand(
		tText aCommand,
		tSpan aSpan,
		mStd.tAction<tText> aDebugStream
	) {
		var Command = mIL_Parser.Command.ParseText(aCommand, "", _ => { aDebugStream(_()); });
		Command.Pos = aSpan;
		return Command;
	}
	
	private static void
	AssertModuleDefs(
		mStd.tAction<tText> aDebugStream,
		mArrayList.tArrayList<(tText? TypeId, mArrayList.tArrayList<mIL_AST.tCommandNode<tSpan>> Commands)> aDefs1,
		(tText Command, (tNat32, tNat32) From, (tNat32, tNat32) To)[][] aDefs2
	) {
		var Count = (tNat32)aDefs2.Length;
		mAssert.AreEquals(aDefs1.Size(), Count);
		for (var I = 0u; I < Count; I += 1) {
			aDebugStream($"Def {I}:");
			mAssert.AreEquals(
				aDefs1.Get(I).Commands.ToStream(),
				mStream.Stream(System.MemoryExtensions.AsSpan(aDefs2[I])).Map(_ => ParseCommand(_.Command, Span(_.From, _.To), aDebugStream)),
				mStream.Eq(mIL_AST.Eq_<tSpan>(EqSpan))
			);
		}
	}
	
	private static void
	PrintModuleDefs<tPos>(
		mStream.tStream<(tText TypeId, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands)> aDefs,
		mStd.tAction<tText> aStreamOut
	) {
		var DefIndex = 0u;
		foreach (var (_, Commands) in aDefs) {
			aStreamOut(mSPO2IL.GetDefId(DefIndex));
			aStreamOut("---");
			foreach (var command in Commands.ToStream()) {
				aStreamOut(command.ToText());
			}
			DefIndex += 1;
			aStreamOut("");
		}
		aStreamOut("");
	}
}
