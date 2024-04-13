using System;

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
	
	private static void AssertCommandsAre<tPos>(
		mStream.tStream<mIL_AST.tCommandNode<tPos>>? aSPO_Commands,
		string aIL_Commands
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
		mStream.tStream<(tText? Type, mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands)>? aSPO_Defs,
		params string[] aIL_Defs
	) {
		aStreamOut($"Def Count: {aSPO_Defs.Count()}");
		var DefIndex = 0u;
		foreach (var (SPO_Def, IL_Def) in mStream.ZipShort(aSPO_Defs, mStream.Stream(aIL_Defs))) {
			aStreamOut(mSPO2IL.GetDefId(DefIndex));
			AssertCommandsAre(SPO_Def.Commands.ToStream(), IL_Def);
			aStreamOut("  OK");
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
		mTest.Test("MapExpression",
			aStreamOut => {
				var ExpressionNode = mSPO_Parser.Expression.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"0 .< (1 .+ 2) < 4",
					"",
					a => aStreamOut(a())
				);
				
				var Scope = mStream.Stream(
					(Id: "_...+...", Type: mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()), mVM_Type.Int())),
					(Id: "_...<...<...", Type: mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int()), mVM_Type.Bool()))
				);
				
				var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var Type = mSPO_AST_Types.UpdateExpressionTypes(ExpressionNode, Scope).ElseThrow();
				var Def = Module.NewDefConstructor(
					Module.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Int()
						)
					)
				);
				mAssert.AreEquals(Def.MapExpression(Module, ExpressionNode, Scope), mSPO2IL.GetRegId(11));
				
				mAssert.AreEquals(
					Def.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 1), (1, 1)), mSPO2IL.GetRegId(1), "0"),
						mIL_AST.CreatePair(Span((1, 1), (1, 17)), mSPO2IL.GetRegId(2), mIL_AST.cEmpty, mSPO2IL.GetRegId(1)),
						mIL_AST.CreateInt(Span((1, 7), (1, 7)), mSPO2IL.GetRegId(3), "1"),
						mIL_AST.CreatePair(Span((1, 7), (1, 12)), mSPO2IL.GetRegId(4), mIL_AST.cEmpty, mSPO2IL.GetRegId(3)),
						mIL_AST.CreateInt(Span((1, 12), (1, 12)), mSPO2IL.GetRegId(5), "2"),
						mIL_AST.CreatePair(Span((1, 7), (1, 12)), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(5)),
						mIL_AST.CallFunc(Span((1, 7), (1, 12)), mSPO2IL.GetRegId(7), mSPO2IL.GetId("...+..."), mSPO2IL.GetRegId(6)),
						mIL_AST.CreatePair(Span((1, 1), (1, 17)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(7)),
						mIL_AST.CreateInt(Span((1, 17), (1, 17)), mSPO2IL.GetRegId(9), "4"),
						mIL_AST.CreatePair(Span((1, 1), (1, 17)), mSPO2IL.GetRegId(10), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(9)),
						mIL_AST.CallFunc(Span((1, 1), (1, 17)), mSPO2IL.GetRegId(11), mSPO2IL.GetId("...<...<..."), mSPO2IL.GetRegId(10))
					)
				);
			}
		),
		mTest.Test("MapDef1",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"§DEF a = (1, 2)",
					"",
					a => aStreamOut(a())
				);
				
				var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, null).ElseThrow();
				var DefConstructor = Module.NewDefConstructor(
					Module.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int())
						)
					)
				);
				DefConstructor.MapDef(Module, DefNode, Scope);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 11), (1, 11)), mSPO2IL.GetRegId(1), "1"),
						mIL_AST.CreatePair(Span((1, 10), (1, 15)), mSPO2IL.GetRegId(2), mIL_AST.cEmpty, mSPO2IL.GetRegId(1)),
						mIL_AST.CreateInt(Span((1, 14), (1, 14)), mSPO2IL.GetRegId(3), "2"),
						mIL_AST.CreatePair(Span((1, 10), (1, 15)), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(3)),
						
						mIL_AST.Alias(Span((1, 1), (1, 6)), mSPO2IL.GetId("a"), mSPO2IL.GetRegId(4))
					)
				);
			}
		),
		mTest.Test("MapDefMatch",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"(§DEF a, (§DEF b, §DEF c)) = (1, (2, 3))",
					"",
					a => aStreamOut(a())
				);
				
				var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, null).ElseThrow();
				var DefConstructor = Module.NewDefConstructor(
					Module.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Empty()
						)
					)
				);
				DefConstructor.MapDef(Module, DefNode, Scope);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 31), (1, 31)), mSPO2IL.GetRegId(1), "1"), // [1]:1
						mIL_AST.CreatePair(Span((1, 30), (1, 40)), mSPO2IL.GetRegId(2), mIL_AST.cEmpty, mSPO2IL.GetRegId(1)), // (); [1]:1 => [2]:(1)
						mIL_AST.CreateInt(Span((1, 35), (1, 35)), mSPO2IL.GetRegId(3), "2"), // [3]:2
						mIL_AST.CreatePair(Span((1, 34), (1, 39)), mSPO2IL.GetRegId(4), mIL_AST.cEmpty, mSPO2IL.GetRegId(3)), // (); [4]:2 => [3]:(2)
						mIL_AST.CreateInt(Span((1, 38), (1, 38)), mSPO2IL.GetRegId(5), "3"), // [5]:3
						mIL_AST.CreatePair(Span((1, 34), (1, 39)), mSPO2IL.GetRegId(6), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(5)), // [4]:(2); [5]:3 => (2, 3)
						mIL_AST.CreatePair(Span((1, 30), (1, 40)), mSPO2IL.GetRegId(7), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(6)), // [2]:(1); [6]:(2, 3) => [7]:(1, (2, 3))
						
						mIL_AST.GetSecond(Span((1, 1), (1, 26)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(7)), // [7]:(1, (2, 3)) => [8]:(2, 3)
						mIL_AST.GetSecond(Span((1, 10), (1, 25)), mSPO2IL.GetRegId(9), mSPO2IL.GetRegId(8)), // [8]:(2, 3) => [9]:3
						mIL_AST.Alias(Span((1, 19), (1, 24)), mSPO2IL.GetId("c"), mSPO2IL.GetRegId(9)), // [9]:3 == c
						mIL_AST.GetFirst(Span((1, 10), (1, 25)), mSPO2IL.GetRegId(10), mSPO2IL.GetRegId(8)),  // [8]:(2, 3) => [10]:(2)
						mIL_AST.GetSecond(Span((1, 10), (1, 25)), mSPO2IL.GetRegId(11), mSPO2IL.GetRegId(10)), // [10]:(2) => [11]:2
						mIL_AST.Alias(Span((1, 11), (1, 16)), mSPO2IL.GetId("b"), mSPO2IL.GetRegId(11)), // [11]:2 == b
						mIL_AST.GetFirst(Span((1, 10), (1, 25)), mSPO2IL.GetRegId(12), mSPO2IL.GetRegId(10)), // [10]:(2) => [12]:()
						mIL_AST.GetFirst(Span((1, 1), (1, 26)), mSPO2IL.GetRegId(13), mSPO2IL.GetRegId(7)), // [7]:(1, (2, 3)) => [13]:(1)
						mIL_AST.GetSecond(Span((1, 1), (1, 26)), mSPO2IL.GetRegId(14), mSPO2IL.GetRegId(13)), // [13]:(1) => [14]:1
						mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.GetId("a"), mSPO2IL.GetRegId(14)), // [14]:1 == a
						
						mIL_AST.GetFirst(Span((1, 1), (1, 26)), mSPO2IL.GetRegId(15), mSPO2IL.GetRegId(13)) // [13]:(1) => [14]:()
					)
				);
			}
		),
		mTest.Test("MatchTuple",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"(§DEF a, §DEF b, §DEF c) = (1, 2, 3)",
					"",
					a => aStreamOut(a())
				);
				
				var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, null).ElseThrow();
				var DefConstructor = Module.NewDefConstructor(
					Module.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Empty()
						)
					)

				);
				DefConstructor.MapDef(Module, DefNode, Scope);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 29), (1, 29)), mSPO2IL.GetRegId(1), "1"), // [1]:1
						mIL_AST.CreatePair(Span((1, 28), (1, 36)), mSPO2IL.GetRegId(2), mIL_AST.cEmpty, mSPO2IL.GetRegId(1)), // (); [1]1 => [2]:(1)
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
					)
				);
			}
		),
		mTest.Test("MapMatchPrefix",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"(§DEF a, §DEF b, (#bla (§DEF c , §DEF d))) = (1, 2, (#bla (3, 4)))",
					"",
					a => aStreamOut(a())
				);
				
				var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, null).ElseThrow();
				var DefConstructor = Module.NewDefConstructor(
					Module.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Empty()
						)
					)
				);
				
				DefConstructor.MapDef(Module, DefNode, Scope);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 47), (1, 47)), mSPO2IL.GetRegId(1), "1"), // 1 => [1]:1
						mIL_AST.CreatePair(Span((1, 46), (1, 66)), mSPO2IL.GetRegId(2), mIL_AST.cEmpty, mSPO2IL.GetRegId(1)), // (); [1]:1 => [2]:(1)
						mIL_AST.CreateInt(Span((1, 50), (1, 50)), mSPO2IL.GetRegId(3), "2"), // 2 => [3]:2
						mIL_AST.CreatePair(Span((1, 46), (1, 66)), mSPO2IL.GetRegId(4), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(3)), // [2]:(1); [3]:2 => [4]:(1, 2)
						mIL_AST.CreateInt(Span((1, 60), (1, 60)), mSPO2IL.GetRegId(5), "3"), // 3 => [5]:3
						mIL_AST.CreatePair(Span((1, 59), (1, 64)), mSPO2IL.GetRegId(6), mIL_AST.cEmpty, mSPO2IL.GetRegId(5)), // (); [5]:3 => [6]:(3)
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
					)
				);
			}
		),
		mTest.Test("MapLambda1",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"§DEF x = §DEF a € §INT => 2 .* a",
					"",
					a => aStreamOut(a())
				);
				
				var InitScope = mStream.Stream(
					(
						Id: mSPO2IL.GetId("...*..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						)
					)
				);
				
				var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, InitScope).ElseThrow();
				var ExpScope = mStream.Stream(
					(
						Id: mSPO2IL.GetId("x"),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Int(),
							mVM_Type.Int()
						)
					),
					InitScope
				);
				mAssert.AreEquals(Scope, ExpScope);
				
				var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var DefConstructor = Module.NewDefConstructor(
					Module.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Empty()
						)
					)
				);
				DefConstructor.MapDef(Module, DefNode, Scope);
				
				mAssert.AreEquals(Module.Defs.Size(), 2u);
				mAssert.AreEquals(
					Module.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.Alias(Span((1, 10), (1, 32)), mSPO2IL.GetId("...*..."), mIL_AST.cEnv), // ENV == ...*...
						
						mIL_AST.Alias(Span((1, 10), (1, 22)), mSPO2IL.GetId("a"), mIL_AST.cArg), // ARG == a
						
						mIL_AST.CreateInt(Span((1, 27), (1, 27)), mSPO2IL.GetRegId(1), "2"), // 2 => [1]:2
						mIL_AST.CreatePair(Span((1, 27), (1, 32)), mSPO2IL.GetRegId(2), mIL_AST.cEmpty, mSPO2IL.GetRegId(1)), // (); [1]:2 => [2]:(2)
						mIL_AST.CreatePair(Span((1, 27), (1, 32)), mSPO2IL.GetRegId(3), mSPO2IL.GetRegId(2), mSPO2IL.GetId("a")), // [2]:(2); a => (2, a)
						
						mIL_AST.CallFunc(Span((1, 27), (1, 32)), mSPO2IL.GetRegId(4), mSPO2IL.GetId("...*..."), mSPO2IL.GetRegId(3)), // ...*... (2, a) => [4]
						mIL_AST.ReturnIf(Span((1, 27), (1, 32)), mIL_AST.cTrue, mSPO2IL.GetRegId(4)) // [4] TRUE
					)
				);
				
				mAssert.AreEquals(DefConstructor.Index, 0u);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CallFunc(Span((1, 10), (1, 32)), mSPO2IL.GetRegId(1), mSPO2IL.GetDefId(1), mSPO2IL.GetId("...*...")),
						mIL_AST.Alias(Span((1, 1), (1, 6)), mSPO2IL.GetId("x"), mSPO2IL.GetRegId(1))
					)
				);
				
				mAssert.AreEquals(
					DefConstructor.EnvIds.ToStream(),
					mStream.Stream(
						(mSPO2IL.GetId("...*..."), Span((1, 27), (1, 32))),
						(mSPO2IL.GetDefId(1), Span((1, 10), (1, 32)))
					)
				);
			}
		),
		mTest.Test("MapLambda2",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"§DEF ...*...+... = (§DEF a € §INT, §DEF b € §INT, §DEF c € §INT) => (a .* b) .+ c",
					"",
					a => aStreamOut(a())
				);
				
				var InitScope = mStream.Stream(
					(
						Id: mSPO2IL.GetId("...*..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						)
					),
					(
						Id: mSPO2IL.GetId("...+..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						)
					)
				);
				
				var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, InitScope).ElseThrow();
				var ExpScope = mStream.Stream(
					(
						Id: mSPO2IL.GetId("...*...+..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						)
					),
					InitScope
				);
				mAssert.AreEquals(Scope, ExpScope);
				
				var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var DefConstructor = Module.NewDefConstructor(
					Module.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Empty()
						)
					)
				);
				DefConstructor.MapDef(Module, DefNode, Scope);
				
				mAssert.AreEquals(Module.Defs.Size(), 2u);
				mAssert.AreEquals(
					Module.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
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
						
						mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(7), mIL_AST.cEmpty, mSPO2IL.GetId("a")),
						mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(7), mSPO2IL.GetId("b")),
						mIL_AST.CallFunc(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(9), mSPO2IL.GetId("...*..."), mSPO2IL.GetRegId(8)),
						mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(10), mIL_AST.cEmpty, mSPO2IL.GetRegId(9)),
						mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(11), mSPO2IL.GetRegId(10), mSPO2IL.GetId("c")),
						mIL_AST.CallFunc(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(12), mSPO2IL.GetId("...+..."), mSPO2IL.GetRegId(11)),
						mIL_AST.ReturnIf(Span((1, 69), (1, 81)), mIL_AST.cTrue, mSPO2IL.GetRegId(12))
					)
				);
				
				mAssert.AreEquals(DefConstructor.Index, 0u);
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(1), mIL_AST.cEmpty, mSPO2IL.GetId("...+...")),
						mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(1), mSPO2IL.GetId("...*...")),
						mIL_AST.CallFunc(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(3), mSPO2IL.GetDefId(1), mSPO2IL.GetRegId(2)),
						mIL_AST.Alias(Span((1, 1), (1, 16)), mSPO2IL.GetId("...*...+..."), mSPO2IL.GetRegId(3))
					)
				);
				
				mAssert.AreEquals(
					DefConstructor.EnvIds.ToStream(),
					mStream.Stream(
						(mSPO2IL.GetId("...+..."), Span((1, 69), (1, 81))),
						(mSPO2IL.GetId("...*..."), Span((1, 70), (1, 75))),
						(mSPO2IL.GetDefId(1), Span((1, 20), (1, 81)))
					)
				);
			}
		),
		mTest.Test("MapLambda3",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"§DEF TestTest... = (§DEF a € §INT, §DEF b € §INT, §DEF c € §INT) => (a .* b) .> c",
					"",
					a => aStreamOut(a())
				);
				
				var InitScope = mStream.Stream(
					(
						Id: mSPO2IL.GetId("...*..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						)
					),
					(
						Id: mSPO2IL.GetId("...>..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Bool()
						)
					)
				);
				
				var Scope = mSPO_AST_Types.UpdateCommandTypes(DefNode, InitScope).ElseThrow();
				var ExpScope = mStream.Stream(
					(
						Id: mSPO2IL.GetId("TestTest..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Bool()
						)
					),
					InitScope
				);
				mAssert.AreEquals(Scope, ExpScope);
				
				var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var DefConstructor = Module.NewDefConstructor(
					Module.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Empty()
						)
					)
				);
				DefConstructor.MapDef(Module, DefNode, Scope);
				
				mAssert.AreEquals(Module.Defs.Size(), 2u);
				mAssert.AreEquals(
					Module.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
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
						
						mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(7), mIL_AST.cEmpty, mSPO2IL.GetId("a")),
						mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(8), mSPO2IL.GetRegId(7), mSPO2IL.GetId("b")),
						mIL_AST.CallFunc(Span((1, 70), (1, 75)), mSPO2IL.GetRegId(9), mSPO2IL.GetId("...*..."), mSPO2IL.GetRegId(8)),
						mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(10), mIL_AST.cEmpty, mSPO2IL.GetRegId(9)),
						mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(11), mSPO2IL.GetRegId(10), mSPO2IL.GetId("c")),
						mIL_AST.CallFunc(Span((1, 69), (1, 81)), mSPO2IL.GetRegId(12), mSPO2IL.GetId("...>..."), mSPO2IL.GetRegId(11)),
						mIL_AST.ReturnIf(Span((1, 69), (1, 81)), mIL_AST.cTrue, mSPO2IL.GetRegId(12))
					)
				);
				
				mAssert.AreEquals(DefConstructor.Index, 0u);
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(1), mIL_AST.cEmpty, mSPO2IL.GetId("...>...")),
						mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(2), mSPO2IL.GetRegId(1), mSPO2IL.GetId("...*...")),
						mIL_AST.CallFunc(Span((1, 20), (1, 81)), mSPO2IL.GetRegId(3), mSPO2IL.GetDefId(1), mSPO2IL.GetRegId(2)),
						mIL_AST.Alias(Span((1, 1), (1, 16)), mSPO2IL.GetId("TestTest..."), mSPO2IL.GetRegId(3))
					)
				);
				
				mAssert.AreEquals(
					DefConstructor.EnvIds.ToStream(),
					mStream.Stream(
						(mSPO2IL.GetId("...>..."), Span((1, 69), (1, 81))),
						(mSPO2IL.GetId("...*..."), Span((1, 70), (1, 75))),
						(mSPO2IL.GetDefId(1), Span((1, 20), (1, 81)))
					)
				);
			}
		),
		mTest.Test("MapIfMatch_1",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"""
					§DEF x = §IF 1 MATCH {
						0 => 2
						1 => 4
						_ => 6
					}
					""",
					"",
					a => aStreamOut(a())
				);
				
				var InitScope = mStream.Stream<(tText Id, mVM_Type.tType Type)>();
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
				var DefConstructor = Module.NewDefConstructor(
					Module.MapType(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Empty()
						)
					)
				);
				
				DefConstructor.MapDef(Module, DefNode, Scope);
				
				PrintModuleDefs(Module.Defs.ToStream(), aStreamOut);
				
				AssertDefsAre(
					aStreamOut,
					Module.Defs.ToStream(),
					"""
					t_1 := 1
					t_2 := EMPTY, d_3
					t_3 := t_2, d_2
					t_4 := t_3, d_5
					t_5 := t_4, d_4
					t_6 := t_5, d_6
					t_7 := .d_1 t_6
					t_8 := .t_7 t_1
					_x := t_8
					""",
					"""
					d_6 := §2ND ENV
					t_7 := §1ST ENV
					d_4 := §2ND t_7
					t_8 := §1ST t_7
					d_5 := §2ND t_8
					t_9 := §1ST t_8
					d_2 := §2ND t_9
					t_10 := §1ST t_9
					d_3 := §2ND t_10
					t_11 := §1ST t_10
					t_1 := .d_2 d_3
					t_2 := .t_1 ARG
					§RETURN t_2 IF_NOT_EMPTY
					t_3 := .d_4 d_5
					t_4 := .t_3 ARG
					§RETURN t_4 IF_NOT_EMPTY
					t_5 := .d_6 EMPTY
					t_6 := .t_5 ARG
					§RETURN t_6 IF_NOT_EMPTY
					§RETURN EMPTY IF TRUE
					""",
					"""
					d_3 := ENV
					t_1 := .d_3 EMPTY
					t_2 := §TRY ARG AS_INT
					t_3 := 0
					t_4 := §INT t_2 == t_3
					t_5 := §BOOL t_4 ^ TRUE
					§RETURN EMPTY IF t_5
					t_6 := .t_1 t_2
					§RETURN t_6 IF TRUE
					""",
					"""
					t_1 := 2
					§RETURN t_1 IF TRUE
					""",
					"""
					d_5 := ENV
					t_1 := .d_5 EMPTY
					t_2 := §TRY ARG AS_INT
					t_3 := 1
					t_4 := §INT t_2 == t_3
					t_5 := §BOOL t_4 ^ TRUE
					§RETURN EMPTY IF t_5
					t_6 := .t_1 t_2
					§RETURN t_6 IF TRUE
					""",
					"""
					t_1 := 4
					§RETURN t_1 IF TRUE
					""",
					"""
					t_1 := 6
					§RETURN t_1 IF TRUE
					"""
				);
			}
		),
		mTest.Test("MapIfMatch_2_WithSet",
			aStreamOut => {
				var ModuleNode = mSPO_Parser.Module.ParseText(
					//       1         2         3         4         5         6         7         8
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
					a => aStreamOut(a())
				);
				
				var InitScope = mSPO_AST_Types.UpdateMatchTypes(
					ModuleNode.Import.Match,
					mStd.cEmpty,
					mSPO_AST_Types.tTypeRelation.Sub,
					mStd.cEmpty
				).Then(
					a => a.Scope
				).ElseThrow();
				
				var Scope = ModuleNode.Commands.Reduce(
					mResult.OK(InitScope).AsResult<tText>(),
					(aResultScope, aCommand) => aResultScope.ThenTry(
						aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope)
					)
				).ElseThrow();
				
				var Module = mSPO2IL.MapModule(ModuleNode, mSpan.Merge, Scope);
				
				PrintModuleDefs(Module.Defs.ToStream(), aStreamOut);
				
				AssertDefsAre(
					aStreamOut,
					Module.Defs.ToStream(),
					// d_0
					"""
					d_1 := §2ND ENV
					t_9 := §1ST ENV
					d_2 := §2ND t_9
					t_10 := §1ST t_9
					d_3 := §2ND t_10
					t_11 := §1ST t_10
					d_4 := §2ND t_11
					t_12 := §1ST t_11
					d_5 := §2ND t_12
					t_13 := §1ST t_12
					d_6 := §2ND t_13
					t_14 := §1ST t_13
					t_1 := +#_Bla... EMPTY
					_X := t_1
					t_2 := EMPTY, d_3
					t_3 := t_2, d_2
					t_4 := t_3, d_5
					t_5 := t_4, d_4
					t_6 := t_5, d_6
					t_7 := .d_1 t_6
					t_8 := .t_7 _X
					§RETURN t_8 IF TRUE
					""",
					// d_1
					"""
					d_6 := §2ND ENV
					t_7 := §1ST ENV
					d_4 := §2ND t_7
					t_8 := §1ST t_7
					d_5 := §2ND t_8
					t_9 := §1ST t_8
					d_2 := §2ND t_9
					t_10 := §1ST t_9
					d_3 := §2ND t_10
					t_11 := §1ST t_10
					t_1 := .d_2 d_3
					t_2 := .t_1 ARG
					§RETURN t_2 IF_NOT_EMPTY
					t_3 := .d_4 d_5
					t_4 := .t_3 ARG
					§RETURN t_4 IF_NOT_EMPTY
					t_5 := .d_6 EMPTY
					t_6 := .t_5 ARG
					§RETURN t_6 IF_NOT_EMPTY
					§RETURN EMPTY IF TRUE
					""",
					// d_2
					"""
					d_3 := ENV
					t_1 := .d_3 EMPTY
					t_2 := §TRY_REMOVE #_Blub... FROM ARG
					t_3 := .t_1 t_2
					§RETURN t_3 IF TRUE
					""",
					// d_3
					"""
					t_1 := 1
					§RETURN t_1 IF TRUE
					""",
					// d_4
					"""
					d_5 := ENV
					t_1 := .d_5 EMPTY
					t_2 := §TRY_REMOVE #_Bla... FROM ARG
					t_3 := .t_1 t_2
					§RETURN t_3 IF TRUE
					""",
					// d_5
					"""
					t_1 := 2
					§RETURN t_1 IF TRUE
					""",
					// d_6
					"""
					t_1 := 3
					§RETURN t_1 IF TRUE
					"""
				);
			}
		),
		mTest.Test("MapNestedMatch",
			aStreamOut => {
				var LambdaNode = mSPO_Parser.Lambda.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"(§DEF a € §INT, §DEF b € §INT, (§DEF x € §INT, §DEF y € §INT, §DEF z € §INT)) => a .* z",
					"",
					a => aStreamOut(a())
				);
				
				var InitScope = mStream.Stream(
					(
						Id: mSPO2IL.GetId("...*..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						)
					)
				);
				
				var ModuleConstructor = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var Type = mSPO_AST_Types.UpdateExpressionTypes(LambdaNode, InitScope);
				if (Type.Match(out var Type_, out var Error)) {
					mAssert.AreEquals(
						Type_,
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(
								mVM_Type.Int(),
								mVM_Type.Int(),
								mVM_Type.Tuple(
									mVM_Type.Int(),
									mVM_Type.Int(),
									mVM_Type.Int()
								)
							),
							mVM_Type.Int()
						)
					);
				} else {
					mAssert.Fail(Error);
				}
				
				var (DefIndex, EnvIds) = ModuleConstructor.MapLambda(LambdaNode, InitScope);
				
				mAssert.AreEquals(ModuleConstructor.Defs.Size(), 1u);
				mAssert.AreEquals(DefIndex, 0u);
				mAssert.AreEquals(
					ModuleConstructor.Defs.Get(DefIndex).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.Alias	(Span((1,  1), (1, 87)),	mSPO2IL.GetId("...*..."),	mIL_AST.cEnv),
									
						mIL_AST.GetSecond	(Span((1,  1), (1, 77)),	mSPO2IL.GetRegId(1),	mIL_AST.cArg),
						mIL_AST.GetSecond	(Span((1, 32), (1, 76)),	mSPO2IL.GetRegId(2),	mSPO2IL.GetRegId(1)),
						mIL_AST.Alias	(Span((1, 63), (1, 75)),	mSPO2IL.GetId("z"),	mSPO2IL.GetRegId(2)),
						mIL_AST.GetFirst	(Span((1, 32), (1, 76)),	mSPO2IL.GetRegId(3),	mSPO2IL.GetRegId(1)),
						mIL_AST.GetSecond	(Span((1, 32), (1, 76)),	mSPO2IL.GetRegId(4),	mSPO2IL.GetRegId(3)),
						mIL_AST.Alias	(Span((1, 48), (1, 60)),	mSPO2IL.GetId("y"),	mSPO2IL.GetRegId(4)),
						mIL_AST.GetFirst	(Span((1, 32), (1, 76)),	mSPO2IL.GetRegId(5),	mSPO2IL.GetRegId(3)),
						mIL_AST.GetSecond	(Span((1, 32), (1, 76)),	mSPO2IL.GetRegId(6),	mSPO2IL.GetRegId(5)),
						mIL_AST.Alias	(Span((1, 33), (1, 45)),	mSPO2IL.GetId("x"),	mSPO2IL.GetRegId(6)),
						mIL_AST.GetFirst	(Span((1, 32), (1, 76)),	mSPO2IL.GetRegId(7),	mSPO2IL.GetRegId(5)),
						mIL_AST.GetFirst	(Span((1,  1), (1, 77)),	mSPO2IL.GetRegId(8),	mIL_AST.cArg),
						mIL_AST.GetSecond	(Span((1,  1), (1, 77)),	mSPO2IL.GetRegId(9),	mSPO2IL.GetRegId(8)),
						mIL_AST.Alias	(Span((1, 17), (1, 29)),	mSPO2IL.GetId("b"),	mSPO2IL.GetRegId(9)),
						mIL_AST.GetFirst	(Span((1,  1), (1, 77)),	mSPO2IL.GetRegId(10),	mSPO2IL.GetRegId(8)),
						mIL_AST.GetSecond	(Span((1,  1), (1, 77)),	mSPO2IL.GetRegId(11),	mSPO2IL.GetRegId(10)),
						mIL_AST.Alias	(Span((1,  2), (1, 14)),	mSPO2IL.GetId("a"),	mSPO2IL.GetRegId(11)),
						mIL_AST.GetFirst	(Span((1,  1), (1, 77)),	mSPO2IL.GetRegId(12),	mSPO2IL.GetRegId(10)),
											
						mIL_AST.CreatePair	(Span((1, 82), (1, 87)),	mSPO2IL.GetRegId(13),	mIL_AST.cEmpty,	mSPO2IL.GetId("a")),
						mIL_AST.CreatePair	(Span((1, 82), (1, 87)),	mSPO2IL.GetRegId(14),	mSPO2IL.GetRegId(13),	mSPO2IL.GetId("z")),
						mIL_AST.CallFunc	(Span((1, 82), (1, 87)),	mSPO2IL.GetRegId(15),	mSPO2IL.GetId("...*..."),	mSPO2IL.GetRegId(14)),
						mIL_AST.ReturnIf	(Span((1, 82), (1, 87)),	mIL_AST.cTrue,	mSPO2IL.GetRegId(15))
					)
				);
				
				mAssert.AreEquals(
					EnvIds.ToStream(),
					mStream.Stream((mSPO2IL.GetId("...*..."), Span((1, 82), (1, 87))))
				);
			}
		),
		mTest.Test("MapModule",
			aStreamOut => {
				var ModuleNode = mSPO_Parser.Module.ParseText(
					//        1         2         3         4         5         6         7         8
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
					a => aStreamOut(a())
				);
				
				var InitScope = mSPO_AST_Types.UpdateMatchTypes(
					ModuleNode.Import.Match,
					mStd.cEmpty,
					mSPO_AST_Types.tTypeRelation.Sub,
					null
				).Then(
					a => a.Scope
				).ElseThrow();
				
				var ModuleConstructor = mSPO2IL.MapModule(ModuleNode, mSpan.Merge, mStd.cEmpty);
				
				mAssert.AreEquals(ModuleConstructor.Defs.Size(), 2u);
				
				mAssert.AreEquals(
					ModuleConstructor.Defs.Get(0).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.Alias(Span((1, 1), (10, 9)), mSPO2IL.GetDefId(1), mIL_AST.cEnv),
						
						mIL_AST.GetSecond	(Span((1, 9), (5,  1)),	mSPO2IL.GetRegId(1),	mIL_AST.cArg),
						mIL_AST.Alias	(Span((4, 2), (4, 11)),	mSPO2IL.GetId("k"),	mSPO2IL.GetRegId(1)),
						mIL_AST.GetFirst	(Span((1, 9), (5,  1)),	mSPO2IL.GetRegId(2),	mIL_AST.cArg),
						mIL_AST.GetSecond	(Span((1, 9), (5,  1)),	mSPO2IL.GetRegId(3),	mSPO2IL.GetRegId(2)),
						mIL_AST.Alias	(Span((3, 2), (3, 29)),	mSPO2IL.GetId("...*..."),	mSPO2IL.GetRegId(3)),
						mIL_AST.GetFirst	(Span((1, 9), (5,  1)),	mSPO2IL.GetRegId(4),	mSPO2IL.GetRegId(2)),
						mIL_AST.GetSecond	(Span((1, 9), (5,  1)),	mSPO2IL.GetRegId(5),	mSPO2IL.GetRegId(4)),
						mIL_AST.Alias	(Span((2, 2), (2, 14)),	mSPO2IL.GetId("T"),	mSPO2IL.GetRegId(5)),
						mIL_AST.GetFirst	(Span((1, 9), (5,  1)),	mSPO2IL.GetRegId(6),	mSPO2IL.GetRegId(4)),
									
						mIL_AST.CreatePair	(Span(( 7, 13), ( 7, 34)),	mSPO2IL.GetRegId(7),	mIL_AST.cEmpty,	mSPO2IL.GetId("...*...")),
						mIL_AST.CreatePair	(Span(( 7, 13), ( 7, 34)),	mSPO2IL.GetRegId(8),	mSPO2IL.GetRegId(7),	mSPO2IL.GetId("k")),
						mIL_AST.CallFunc	(Span(( 7, 13), ( 7, 34)),	mSPO2IL.GetRegId(9),	mSPO2IL.GetDefId(1),	mSPO2IL.GetRegId(8)),
						mIL_AST.Alias	(Span(( 7,  1), ( 7,  9)),	mSPO2IL.GetId("x..."),	mSPO2IL.GetRegId(9)),
						mIL_AST.CallFunc	(Span(( 8, 10), ( 8, 13)),	mSPO2IL.GetRegId(10),	mSPO2IL.GetId("x..."),	mSPO2IL.GetId("k")),
						mIL_AST.Alias	(Span(( 8,  1), ( 8,  6)),	mSPO2IL.GetId("y"),	mSPO2IL.GetRegId(10)),
						mIL_AST.ReturnIf	(Span((10,  1), (10,  9)),	mIL_AST.cTrue,	mSPO2IL.GetId("y"))
					)
				);
				
				mAssert.AreEquals(
					ModuleConstructor.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.GetSecond	(Span((7, 13), (7, 34)),	mSPO2IL.GetId("k"),	mIL_AST.cEnv),
						mIL_AST.GetFirst	(Span((7, 13), (7, 34)),	mSPO2IL.GetRegId(4),	mIL_AST.cEnv),
						mIL_AST.GetSecond	(Span((7, 13), (7, 34)),	mSPO2IL.GetId("...*..."),	mSPO2IL.GetRegId(4)),
						mIL_AST.GetFirst	(Span((7, 13), (7, 34)),	mSPO2IL.GetRegId(5),	mSPO2IL.GetRegId(4)),
									
						mIL_AST.Alias	(Span((7, 14), (7, 23)),	mSPO2IL.GetId("a"),	mIL_AST.cArg),
									
						mIL_AST.CreatePair	(Span((7, 29), (7, 34)),	mSPO2IL.GetRegId(1),	mIL_AST.cEmpty,	mSPO2IL.GetId("k")),
						mIL_AST.CreatePair	(Span((7, 29), (7, 34)),	mSPO2IL.GetRegId(2),	mSPO2IL.GetRegId(1),	mSPO2IL.GetId("a")),
						mIL_AST.CallFunc	(Span((7, 29), (7, 34)),	mSPO2IL.GetRegId(3),	mSPO2IL.GetId("...*..."),	mSPO2IL.GetRegId(2)),
						mIL_AST.ReturnIf	(Span((7, 29), (7, 34)),	mIL_AST.cTrue,	mSPO2IL.GetRegId(3))
					)
				);
			}
		)
	);
	
	private static void PrintModuleDefs<tPos>(
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
