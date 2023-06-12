//IMPORT mTest.cs
//IMPORT mSPO2IL.cs
//IMPORT mSPO_Parser.cs

#nullable enable

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mSPO2IL_Tests {
	
	private static tSpan
	Span(
		(tInt32 Row, tInt32 Col) aStart,
		(tInt32 Row, tInt32 Col) aEnd
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
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(nameof(mSPO2IL),
		mTest.Test("MapExpresion",
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
				var Def = Module.NewDefConstructor();
				mAssert.AreEquals(Def.MapExpresion(ExpressionNode, Scope), mSPO2IL.RegId(11));
				
				mAssert.AreEquals(
					Def.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 1), (1, 1)), mSPO2IL.RegId(1), "0"),
						mIL_AST.CreatePair(Span((1, 1), (1, 17)), mSPO2IL.RegId(2), mIL_AST.cEmpty, mSPO2IL.RegId(1)),
						mIL_AST.CreateInt(Span((1, 7), (1, 7)), mSPO2IL.RegId(3), "1"),
						mIL_AST.CreatePair(Span((1, 7), (1, 12)), mSPO2IL.RegId(4), mIL_AST.cEmpty, mSPO2IL.RegId(3)),
						mIL_AST.CreateInt(Span((1, 12), (1, 12)), mSPO2IL.RegId(5), "2"),
						mIL_AST.CreatePair(Span((1, 7), (1, 12)), mSPO2IL.RegId(6), mSPO2IL.RegId(4), mSPO2IL.RegId(5)),
						mIL_AST.CallFunc(Span((1, 7), (1, 12)), mSPO2IL.RegId(7), mSPO2IL.Id("...+..."), mSPO2IL.RegId(6)),
						mIL_AST.CreatePair(Span((1, 1), (1, 17)), mSPO2IL.RegId(8), mSPO2IL.RegId(2), mSPO2IL.RegId(7)),
						mIL_AST.CreateInt(Span((1, 17), (1, 17)), mSPO2IL.RegId(9), "4"),
						mIL_AST.CreatePair(Span((1, 1), (1, 17)), mSPO2IL.RegId(10), mSPO2IL.RegId(8), mSPO2IL.RegId(9)),
						mIL_AST.CallFunc(Span((1, 1), (1, 17)), mSPO2IL.RegId(11), mSPO2IL.Id("...<...<..."), mSPO2IL.RegId(10))
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
				var DefConstructor = Module.NewDefConstructor();
				DefConstructor.MapDef(DefNode, Scope);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 11), (1, 11)), mSPO2IL.RegId(1), "1"),
						mIL_AST.CreatePair(Span((1, 10), (1, 15)), mSPO2IL.RegId(2), mIL_AST.cEmpty, mSPO2IL.RegId(1)),
						mIL_AST.CreateInt(Span((1, 14), (1, 14)), mSPO2IL.RegId(3), "2"),
						mIL_AST.CreatePair(Span((1, 10), (1, 15)), mSPO2IL.RegId(4), mSPO2IL.RegId(2), mSPO2IL.RegId(3)),
						
						mIL_AST.Alias(Span((1, 1), (1, 6)), mSPO2IL.Id("a"), mSPO2IL.RegId(4))
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
				var DefConstructor = Module.NewDefConstructor();
				DefConstructor.MapDef(DefNode, Scope);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 31), (1, 31)), mSPO2IL.RegId(1), "1"), // [1]:1
						mIL_AST.CreatePair(Span((1, 30), (1, 40)), mSPO2IL.RegId(2), mIL_AST.cEmpty, mSPO2IL.RegId(1)), // (); [1]:1 => [2]:(1)
						mIL_AST.CreateInt(Span((1, 35), (1, 35)), mSPO2IL.RegId(3), "2"), // [3]:2
						mIL_AST.CreatePair(Span((1, 34), (1, 39)), mSPO2IL.RegId(4), mIL_AST.cEmpty, mSPO2IL.RegId(3)), // (); [4]:2 => [3]:(2)
						mIL_AST.CreateInt(Span((1, 38), (1, 38)), mSPO2IL.RegId(5), "3"), // [5]:3
						mIL_AST.CreatePair(Span((1, 34), (1, 39)), mSPO2IL.RegId(6), mSPO2IL.RegId(4), mSPO2IL.RegId(5)), // [4]:(2); [5]:3 => (2, 3)
						mIL_AST.CreatePair(Span((1, 30), (1, 40)), mSPO2IL.RegId(7), mSPO2IL.RegId(2), mSPO2IL.RegId(6)), // [2]:(1); [6]:(2, 3) => [7]:(1, (2, 3))
						
						mIL_AST.GetSecond(Span((1, 1), (1, 26)), mSPO2IL.RegId(8), mSPO2IL.RegId(7)), // [7]:(1, (2, 3)) => [8]:(2, 3)
						mIL_AST.GetSecond(Span((1, 10), (1, 25)), mSPO2IL.RegId(9), mSPO2IL.RegId(8)), // [8]:(2, 3) => [9]:3
						mIL_AST.Alias(Span((1, 19), (1, 24)), mSPO2IL.Id("c"), mSPO2IL.RegId(9)), // [9]:3 == c
						mIL_AST.GetFirst(Span((1, 10), (1, 25)), mSPO2IL.RegId(10), mSPO2IL.RegId(8)),  // [8]:(2, 3) => [10]:(2)
						mIL_AST.GetSecond(Span((1, 10), (1, 25)), mSPO2IL.RegId(11), mSPO2IL.RegId(10)), // [10]:(2) => [11]:2
						mIL_AST.Alias(Span((1, 11), (1, 16)), mSPO2IL.Id("b"), mSPO2IL.RegId(11)), // [11]:2 == b
						mIL_AST.GetFirst(Span((1, 10), (1, 25)), mSPO2IL.RegId(12), mSPO2IL.RegId(10)), // [10]:(2) => [12]:()
						mIL_AST.GetFirst(Span((1, 1), (1, 26)), mSPO2IL.RegId(13), mSPO2IL.RegId(7)), // [7]:(1, (2, 3)) => [13]:(1)
						mIL_AST.GetSecond(Span((1, 1), (1, 26)), mSPO2IL.RegId(14), mSPO2IL.RegId(13)), // [13]:(1) => [14]:1
						mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.Id("a"), mSPO2IL.RegId(14)), // [14]:1 == a
						
						mIL_AST.GetFirst(Span((1, 1), (1, 26)), mSPO2IL.RegId(15), mSPO2IL.RegId(13)) // [13]:(1) => [14]:()
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
				var DefConstructor = Module.NewDefConstructor();
				DefConstructor.MapDef(DefNode, Scope);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 29), (1, 29)), mSPO2IL.RegId(1), "1"), // [1]:1
						mIL_AST.CreatePair(Span((1, 28), (1, 36)), mSPO2IL.RegId(2), mIL_AST.cEmpty, mSPO2IL.RegId(1)), // (); [1]1 => [2]:(1)
						mIL_AST.CreateInt(Span((1, 32), (1, 32)), mSPO2IL.RegId(3), "2"), // [3]:2
						mIL_AST.CreatePair(Span((1, 28), (1, 36)), mSPO2IL.RegId(4), mSPO2IL.RegId(2), mSPO2IL.RegId(3)), // [2]:(1); [3]:2 => [4]:(1, 2)
						mIL_AST.CreateInt(Span((1, 35), (1, 35)), mSPO2IL.RegId(5), "3"), // [5]:3
						mIL_AST.CreatePair(Span((1, 28), (1, 36)), mSPO2IL.RegId(6), mSPO2IL.RegId(4), mSPO2IL.RegId(5)), // [4]:(1, 2); [5]:3 => [6]:(1, 2, 3)
						
						mIL_AST.GetSecond(Span((1, 1), (1, 24)), mSPO2IL.RegId(7), mSPO2IL.RegId(6)), // [6]:(1, 2, 3) => [7]:3
						mIL_AST.Alias(Span((1, 18), (1, 23)), mSPO2IL.Id("c"), mSPO2IL.RegId(7)), // [7]:3 == c
						mIL_AST.GetFirst(Span((1, 1), (1, 24)), mSPO2IL.RegId(8), mSPO2IL.RegId(6)), // [6]:(1, 2, 3) => [8]:(1, 2)
						mIL_AST.GetSecond(Span((1, 1), (1, 24)), mSPO2IL.RegId(9), mSPO2IL.RegId(8)), // [8]:(1, 2) => [9]:2
						mIL_AST.Alias(Span((1, 10), (1, 15)), mSPO2IL.Id("b"), mSPO2IL.RegId(9)), // [9]:2 == b
						mIL_AST.GetFirst(Span((1, 1), (1, 24)), mSPO2IL.RegId(10), mSPO2IL.RegId(8)), // [8]:(1, 2) => [10]:(1)
						mIL_AST.GetSecond(Span((1, 1), (1, 24)), mSPO2IL.RegId(11), mSPO2IL.RegId(10)), // [10]:(1) => [11]:1
						mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.Id("a"), mSPO2IL.RegId(11)), // [11]:1 == a
						mIL_AST.GetFirst(Span((1, 1), (1, 24)), mSPO2IL.RegId(12), mSPO2IL.RegId(10)) // [10]:(1) => [12]:()
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
				var DefConstructor = Module.NewDefConstructor();
				DefConstructor.MapDef(DefNode, Scope);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 47), (1, 47)), mSPO2IL.RegId(1), "1"), // 1 => [1]:1
						mIL_AST.CreatePair(Span((1, 46), (1, 66)), mSPO2IL.RegId(2), mIL_AST.cEmpty, mSPO2IL.RegId(1)), // (); [1]:1 => [2]:(1)
						mIL_AST.CreateInt(Span((1, 50), (1, 50)), mSPO2IL.RegId(3), "2"), // 2 => [3]:2
						mIL_AST.CreatePair(Span((1, 46), (1, 66)), mSPO2IL.RegId(4), mSPO2IL.RegId(2), mSPO2IL.RegId(3)), // [2]:(1); [3]:2 => [4]:(1, 2)
						mIL_AST.CreateInt(Span((1, 60), (1, 60)), mSPO2IL.RegId(5), "3"), // 3 => [5]:3
						mIL_AST.CreatePair(Span((1, 59), (1, 64)), mSPO2IL.RegId(6), mIL_AST.cEmpty, mSPO2IL.RegId(5)), // (); [5]:3 => [6]:(3)
						mIL_AST.CreateInt(Span((1, 63), (1, 63)), mSPO2IL.RegId(7), "4"), // 4 => [7]:4
						mIL_AST.CreatePair(Span((1, 59), (1, 64)), mSPO2IL.RegId(8), mSPO2IL.RegId(6), mSPO2IL.RegId(7)), // [6]:(3); [7]:4 => [8]:(3, 4)
						mIL_AST.AddPrefix(Span((1, 54), (1, 64)), mSPO2IL.RegId(9), mSPO2IL.Id("bla..."), mSPO2IL.RegId(8)), // #bla[8]:(3, 4) => [9]:#bla(3, 4)
						mIL_AST.CreatePair(Span((1, 46), (1, 66)), mSPO2IL.RegId(10), mSPO2IL.RegId(4), mSPO2IL.RegId(9)), // [4]:(1, 2); [8]:(#bla(3, 4)) => [10]:(1, 2, #bla(3, 4))
						
						mIL_AST.GetSecond(Span((1, 1), (1, 42)), mSPO2IL.RegId(11), mSPO2IL.RegId(10)), // [10]:(1, 2, #bla(3, 4)) => [11]:#bla(3, 4)
						mIL_AST.SubPrefix(Span((1, 18), (1, 41)), mSPO2IL.RegId(12), mSPO2IL.Id("bla..."), mSPO2IL.RegId(11)), // [11]:#bla(3, 4) => [12]:(3, 4)
						mIL_AST.GetSecond(Span((1, 24), (1, 40)), mSPO2IL.RegId(13), mSPO2IL.RegId(12)), // [12]:(3, 4) => [13]:4
						mIL_AST.Alias(Span((1, 34), (1, 39)), mSPO2IL.Id("d"), mSPO2IL.RegId(13)), // [13]:4 == d
						mIL_AST.GetFirst(Span((1, 24), (1, 40)), mSPO2IL.RegId(14), mSPO2IL.RegId(12)), // [12]:(3, 4) => [14]:(3)
						mIL_AST.GetSecond(Span((1, 24), (1, 40)), mSPO2IL.RegId(15), mSPO2IL.RegId(14)), // [14]:(3) => [15]:3
						mIL_AST.Alias(Span((1, 25), (1, 30)), mSPO2IL.Id("c"), mSPO2IL.RegId(15)), // [15]:3 == c
						mIL_AST.GetFirst(Span((1, 24), (1, 40)), mSPO2IL.RegId(16), mSPO2IL.RegId(14)), // [14]:(3) => [16]:()
						mIL_AST.GetFirst(Span((1, 1), (1, 42)), mSPO2IL.RegId(17), mSPO2IL.RegId(10)), // [10]:(1, 2, #bla(3, 4)) => [17]:(1, 2)
						mIL_AST.GetSecond(Span((1, 1), (1, 42)), mSPO2IL.RegId(18), mSPO2IL.RegId(17)), // [17]:(1, 2) => [18]:2
						mIL_AST.Alias(Span((1, 10), (1, 15)), mSPO2IL.Id("b"), mSPO2IL.RegId(18)), // [18]:2 == b
						mIL_AST.GetFirst(Span((1, 1), (1, 42)), mSPO2IL.RegId(19), mSPO2IL.RegId(17)), // [17]:(1, 2) => [19]:(1)
						mIL_AST.GetSecond(Span((1, 1), (1, 42)), mSPO2IL.RegId(20), mSPO2IL.RegId(19)), // [19]:(1) => [20]:1
						mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.Id("a"), mSPO2IL.RegId(20)), // [20]:1 == a
						mIL_AST.GetFirst(Span((1, 1), (1, 42)), mSPO2IL.RegId(21), mSPO2IL.RegId(19)) // [19]:(1) => [21]:()
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
						Id: mSPO2IL.Id("...*..."),
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
						Id: mSPO2IL.Id("x"),
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
				var DefConstructor = Module.NewDefConstructor();
				DefConstructor.MapDef(DefNode, Scope);
				
				mAssert.AreEquals(DefConstructor.ModuleConstructor.Defs.Size(), 2);
				mAssert.AreEquals(
					DefConstructor.ModuleConstructor.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.Alias(Span((1, 10), (1, 32)), mSPO2IL.Id("...*..."), mIL_AST.cEnv), // [ENV]:(...*...) => ...*...
						
						mIL_AST.Alias(Span((1, 10), (1, 22)), mSPO2IL.Id("a"), mIL_AST.cArg), // ARG == a
						
						mIL_AST.CreateInt(Span((1, 27), (1, 27)), mSPO2IL.RegId(1), "2"), // 2 => [1]:2
						mIL_AST.CreatePair(Span((1, 27), (1, 32)), mSPO2IL.RegId(2), mIL_AST.cEmpty, mSPO2IL.RegId(1)), // (); [1]:2 => [2]:(2)
						mIL_AST.CreatePair(Span((1, 27), (1, 32)), mSPO2IL.RegId(3), mSPO2IL.RegId(2), mSPO2IL.Id("a")), // [2]:(2); a => (2, a)
						
						mIL_AST.CallFunc(Span((1, 27), (1, 32)), mSPO2IL.RegId(4), mSPO2IL.Id("...*..."), mSPO2IL.RegId(3)), // ...*... (2, a) => [4]
						mIL_AST.ReturnIf(Span((1, 27), (1, 32)), mSPO2IL.RegId(4), mIL_AST.cTrue) // [4] TRUE
					)
				);
				
				mAssert.AreEquals(DefConstructor.Index, 0);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CallFunc(Span((1, 10), (1, 32)), mSPO2IL.RegId(1), mSPO2IL.DefId(1), mSPO2IL.Id("...*...")),
						mIL_AST.Alias(Span((1, 1), (1, 6)), mSPO2IL.Id("x"), mSPO2IL.RegId(1))
					)
				);
				
				mAssert.AreEquals(
					DefConstructor.EnvIds.ToStream(),
					mStream.Stream(
						(mSPO2IL.Id("...*..."), Span((1, 27), (1, 32))),
						(mSPO2IL.DefId(1), Span((1, 10), (1, 32)))
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
						Id: mSPO2IL.Id("...*..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						)
					),
					(
						Id: mSPO2IL.Id("...+..."),
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
						Id: mSPO2IL.Id("...*...+..."),
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
				var DefConstructor = Module.NewDefConstructor();
				DefConstructor.MapDef(DefNode, Scope);
				
				mAssert.AreEquals(DefConstructor.ModuleConstructor.Defs.Size(), 2);
				mAssert.AreEquals(
					DefConstructor.ModuleConstructor.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.GetSecond(Span((1, 20), (1, 81)), mSPO2IL.Id("...*..."), mIL_AST.cEnv),
						mIL_AST.GetFirst(Span((1, 20), (1, 81)), mSPO2IL.RegId(13), mIL_AST.cEnv),
						mIL_AST.GetSecond(Span((1, 20), (1, 81)), mSPO2IL.Id("...+..."), mSPO2IL.RegId(13)), // TODO
						mIL_AST.GetFirst(Span((1, 20), (1, 81)), mSPO2IL.RegId(14), mSPO2IL.RegId(13)),
						
						mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.RegId(1), mIL_AST.cArg),
						mIL_AST.Alias(Span((1, 51), (1, 63)), mSPO2IL.Id("c"), mSPO2IL.RegId(1)),
						mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.RegId(2), mIL_AST.cArg),
						mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.RegId(3), mSPO2IL.RegId(2)),
						mIL_AST.Alias(Span((1, 36), (1, 48)), mSPO2IL.Id("b"), mSPO2IL.RegId(3)),
						mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.RegId(4), mSPO2IL.RegId(2)),
						mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.RegId(5), mSPO2IL.RegId(4)),
						mIL_AST.Alias(Span((1, 21), (1, 33)), mSPO2IL.Id("a"), mSPO2IL.RegId(5)),
						mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.RegId(6), mSPO2IL.RegId(4)),
						
						mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.RegId(7), mIL_AST.cEmpty, mSPO2IL.Id("a")),
						mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.RegId(8), mSPO2IL.RegId(7), mSPO2IL.Id("b")),
						mIL_AST.CallFunc(Span((1, 70), (1, 75)), mSPO2IL.RegId(9), mSPO2IL.Id("...*..."), mSPO2IL.RegId(8)),
						mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.RegId(10), mIL_AST.cEmpty, mSPO2IL.RegId(9)),
						mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.RegId(11), mSPO2IL.RegId(10), mSPO2IL.Id("c")),
						mIL_AST.CallFunc(Span((1, 69), (1, 81)), mSPO2IL.RegId(12), mSPO2IL.Id("...+..."), mSPO2IL.RegId(11)),
						mIL_AST.ReturnIf(Span((1, 69), (1, 81)), mSPO2IL.RegId(12), mIL_AST.cTrue)
					)
				);
				
				mAssert.AreEquals(DefConstructor.Index, 0);
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.RegId(1), mIL_AST.cEmpty, mSPO2IL.Id("...+...")),
						mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.RegId(2), mSPO2IL.RegId(1), mSPO2IL.Id("...*...")),
						mIL_AST.CallFunc(Span((1, 20), (1, 81)), mSPO2IL.RegId(3), mSPO2IL.DefId(1), mSPO2IL.RegId(2)),
						mIL_AST.Alias(Span((1, 1), (1, 16)), mSPO2IL.Id("...*...+..."), mSPO2IL.RegId(3))
					)
				);
				
				mAssert.AreEquals(
					DefConstructor.EnvIds.ToStream(),
					mStream.Stream(
						(mSPO2IL.Id("...+..."), Span((1, 69), (1, 81))),
						(mSPO2IL.Id("...*..."), Span((1, 70), (1, 75))),
						(mSPO2IL.DefId(1), Span((1, 20), (1, 81)))
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
						Id: mSPO2IL.Id("...*..."),
						Type: mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						)
					),
					(
						Id: mSPO2IL.Id("...>..."),
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
						Id: mSPO2IL.Id("TestTest..."),
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
				var DefConstructor = Module.NewDefConstructor();
				DefConstructor.MapDef(DefNode, Scope);
				
				mAssert.AreEquals(DefConstructor.ModuleConstructor.Defs.Size(), 2);
				mAssert.AreEquals(
					DefConstructor.ModuleConstructor.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.GetSecond(Span((1, 20), (1, 81)), mSPO2IL.Id("...*..."), mIL_AST.cEnv),
						mIL_AST.GetFirst(Span((1, 20), (1, 81)), mSPO2IL.RegId(13), mIL_AST.cEnv),
						mIL_AST.GetSecond(Span((1, 20), (1, 81)), mSPO2IL.Id("...>..."), mSPO2IL.RegId(13)), // TODO
						mIL_AST.GetFirst(Span((1, 20), (1, 81)), mSPO2IL.RegId(14), mSPO2IL.RegId(13)),
						
						mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.RegId(1), mIL_AST.cArg),
						mIL_AST.Alias(Span((1, 51), (1, 63)), mSPO2IL.Id("c"), mSPO2IL.RegId(1)),
						mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.RegId(2), mIL_AST.cArg),
						mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.RegId(3), mSPO2IL.RegId(2)),
						mIL_AST.Alias(Span((1, 36), (1, 48)), mSPO2IL.Id("b"), mSPO2IL.RegId(3)),
						mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.RegId(4), mSPO2IL.RegId(2)),
						mIL_AST.GetSecond(Span((1, 20), (1, 64)), mSPO2IL.RegId(5), mSPO2IL.RegId(4)),
						mIL_AST.Alias(Span((1, 21), (1, 33)), mSPO2IL.Id("a"), mSPO2IL.RegId(5)),
						mIL_AST.GetFirst(Span((1, 20), (1, 64)), mSPO2IL.RegId(6), mSPO2IL.RegId(4)),
						
						mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.RegId(7), mIL_AST.cEmpty, mSPO2IL.Id("a")),
						mIL_AST.CreatePair(Span((1, 70), (1, 75)), mSPO2IL.RegId(8), mSPO2IL.RegId(7), mSPO2IL.Id("b")),
						mIL_AST.CallFunc(Span((1, 70), (1, 75)), mSPO2IL.RegId(9), mSPO2IL.Id("...*..."), mSPO2IL.RegId(8)),
						mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.RegId(10), mIL_AST.cEmpty, mSPO2IL.RegId(9)),
						mIL_AST.CreatePair(Span((1, 69), (1, 81)), mSPO2IL.RegId(11), mSPO2IL.RegId(10), mSPO2IL.Id("c")),
						mIL_AST.CallFunc(Span((1, 69), (1, 81)), mSPO2IL.RegId(12), mSPO2IL.Id("...>..."), mSPO2IL.RegId(11)),
						mIL_AST.ReturnIf(Span((1, 69), (1, 81)), mSPO2IL.RegId(12), mIL_AST.cTrue)
					)
				);
				
				mAssert.AreEquals(DefConstructor.Index, 0);
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.RegId(1), mIL_AST.cEmpty, mSPO2IL.Id("...>...")),
						mIL_AST.CreatePair(Span((1, 20), (1, 81)), mSPO2IL.RegId(2), mSPO2IL.RegId(1), mSPO2IL.Id("...*...")),
						mIL_AST.CallFunc(Span((1, 20), (1, 81)), mSPO2IL.RegId(3), mSPO2IL.DefId(1), mSPO2IL.RegId(2)),
						mIL_AST.Alias(Span((1, 1), (1, 16)), mSPO2IL.Id("TestTest..."), mSPO2IL.RegId(3))
					)
				);
				
				mAssert.AreEquals(
					DefConstructor.EnvIds.ToStream(),
					mStream.Stream(
						(mSPO2IL.Id("...>..."), Span((1, 69), (1, 81))),
						(mSPO2IL.Id("...*..."), Span((1, 70), (1, 75))),
						(mSPO2IL.DefId(1), Span((1, 20), (1, 81)))
					)
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
						Id: mSPO2IL.Id("...*..."),
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
				
				var (DefId, EnvIds) = ModuleConstructor.MapLambda(LambdaNode, InitScope);
				
				var DefIndex = 0;
				
				mAssert.AreEquals(ModuleConstructor.Defs.Size(), 1);
				mAssert.AreEquals(DefId, mSPO2IL.DefId(DefIndex));
				mAssert.AreEquals(
					ModuleConstructor.Defs.Get(DefIndex).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.Alias(Span((1, 1), (1, 87)), mSPO2IL.Id("...*..."), mIL_AST.cEnv),
						
						mIL_AST.GetSecond(Span((1, 1), (1, 77)), mSPO2IL.RegId(1), mIL_AST.cArg),
						mIL_AST.GetSecond(Span((1, 32), (1, 76)), mSPO2IL.RegId(2), mSPO2IL.RegId(1)),
						mIL_AST.Alias(Span((1, 63), (1, 75)), mSPO2IL.Id("z"), mSPO2IL.RegId(2)),
						mIL_AST.GetFirst(Span((1, 32), (1, 76)), mSPO2IL.RegId(3), mSPO2IL.RegId(1)),
						mIL_AST.GetSecond(Span((1, 32), (1, 76)), mSPO2IL.RegId(4), mSPO2IL.RegId(3)),
						mIL_AST.Alias(Span((1, 48), (1, 60)), mSPO2IL.Id("y"), mSPO2IL.RegId(4)),
						mIL_AST.GetFirst(Span((1, 32), (1, 76)), mSPO2IL.RegId(5), mSPO2IL.RegId(3)),
						mIL_AST.GetSecond(Span((1, 32), (1, 76)), mSPO2IL.RegId(6), mSPO2IL.RegId(5)),
						mIL_AST.Alias(Span((1, 33), (1, 45)), mSPO2IL.Id("x"), mSPO2IL.RegId(6)),
						mIL_AST.GetFirst(Span((1, 32), (1, 76)), mSPO2IL.RegId(7), mSPO2IL.RegId(5)),
						mIL_AST.GetFirst(Span((1, 1), (1, 77)), mSPO2IL.RegId(8), mIL_AST.cArg),
						mIL_AST.GetSecond(Span((1, 1), (1, 77)), mSPO2IL.RegId(9), mSPO2IL.RegId(8)),
						mIL_AST.Alias(Span((1, 17), (1, 29)), mSPO2IL.Id("b"), mSPO2IL.RegId(9)),
						mIL_AST.GetFirst(Span((1, 1), (1, 77)), mSPO2IL.RegId(10), mSPO2IL.RegId(8)),
						mIL_AST.GetSecond(Span((1, 1), (1, 77)), mSPO2IL.RegId(11), mSPO2IL.RegId(10)),
						mIL_AST.Alias(Span((1, 2), (1, 14)), mSPO2IL.Id("a"), mSPO2IL.RegId(11)),
						mIL_AST.GetFirst(Span((1, 1), (1, 77)), mSPO2IL.RegId(12), mSPO2IL.RegId(10)),
						
						mIL_AST.CreatePair(Span((1, 82), (1, 87)), mSPO2IL.RegId(13), mIL_AST.cEmpty, mSPO2IL.Id("a")),
						mIL_AST.CreatePair(Span((1, 82), (1, 87)), mSPO2IL.RegId(14), mSPO2IL.RegId(13), mSPO2IL.Id("z")),
						mIL_AST.CallFunc(Span((1, 82), (1, 87)), mSPO2IL.RegId(15), mSPO2IL.Id("...*..."), mSPO2IL.RegId(14)),
						mIL_AST.ReturnIf(Span((1, 82), (1, 87)), mSPO2IL.RegId(15), mIL_AST.cTrue)
					)
				);
				
				mAssert.AreEquals(
					EnvIds.ToStream(),
					mStream.Stream((mSPO2IL.Id("...*..."), Span((1, 82), (1, 87))))
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
				
				mAssert.AreEquals(ModuleConstructor.Defs.Size(), 2);
				
				mAssert.AreEquals(
					ModuleConstructor.Defs.Get(0).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.Alias(Span((1, 1), (10, 9)), mSPO2IL.DefId(1), mIL_AST.cEnv),
						
						mIL_AST.GetSecond(Span((1, 9), (5, 1)), mSPO2IL.RegId(1), mIL_AST.cArg),
						mIL_AST.Alias(Span((4, 2), (4, 11)), mSPO2IL.Id("k"), mSPO2IL.RegId(1)),
						mIL_AST.GetFirst(Span((1, 9), (5, 1)), mSPO2IL.RegId(2), mIL_AST.cArg),
						mIL_AST.GetSecond(Span((1, 9), (5, 1)), mSPO2IL.RegId(3), mSPO2IL.RegId(2)),
						mIL_AST.Alias(Span((3, 2), (3, 29)), mSPO2IL.Id("...*..."), mSPO2IL.RegId(3)),
						mIL_AST.GetFirst(Span((1, 9), (5, 1)), mSPO2IL.RegId(4), mSPO2IL.RegId(2)),
						mIL_AST.GetSecond(Span((1, 9), (5, 1)), mSPO2IL.RegId(5), mSPO2IL.RegId(4)),
						mIL_AST.Alias(Span((2, 2), (2, 14)), mSPO2IL.Id("T"), mSPO2IL.RegId(5)),
						mIL_AST.GetFirst(Span((1, 9), (5, 1)), mSPO2IL.RegId(6), mSPO2IL.RegId(4)),
						
						mIL_AST.CreatePair(Span((7, 13), (7, 34)), mSPO2IL.RegId(7), mIL_AST.cEmpty, mSPO2IL.Id("...*...")),
						mIL_AST.CreatePair(Span((7, 13), (7, 34)), mSPO2IL.RegId(8), mSPO2IL.RegId(7), mSPO2IL.Id("k")),
						mIL_AST.CallFunc(Span((7, 13), (7, 34)), mSPO2IL.RegId(9), mSPO2IL.DefId(1), mSPO2IL.RegId(8)),
						mIL_AST.Alias(Span((7, 1), (7, 9)), mSPO2IL.Id("x..."), mSPO2IL.RegId(9)),
						mIL_AST.CallFunc(Span((8, 10), (8, 13)), mSPO2IL.RegId(10), mSPO2IL.Id("x..."), mSPO2IL.Id("k")),
						mIL_AST.Alias(Span((8, 1), (8, 6)), mSPO2IL.Id("y"), mSPO2IL.RegId(10)),
						mIL_AST.ReturnIf(Span((10, 1), (10, 9)), mSPO2IL.Id("y"), mIL_AST.cTrue)
					)
				);
				
				mAssert.AreEquals(
					ModuleConstructor.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.GetSecond(Span((7, 13), (7, 34)), mSPO2IL.Id("k"), mIL_AST.cEnv),
						mIL_AST.GetFirst(Span((7, 13), (7, 34)), mSPO2IL.RegId(4), mIL_AST.cEnv),
						mIL_AST.GetSecond(Span((7, 13), (7, 34)), mSPO2IL.Id("...*..."), mSPO2IL.RegId(4)),
						mIL_AST.GetFirst(Span((7, 13), (7, 34)), mSPO2IL.RegId(5), mSPO2IL.RegId(4)),
						
						mIL_AST.Alias(Span((7, 14), (7, 23)), mSPO2IL.Id("a"), mIL_AST.cArg),
						
						mIL_AST.CreatePair(Span((7, 29), (7, 34)), mSPO2IL.RegId(1), mIL_AST.cEmpty, mSPO2IL.Id("k")),
						mIL_AST.CreatePair(Span((7, 29), (7, 34)), mSPO2IL.RegId(2), mSPO2IL.RegId(1), mSPO2IL.Id("a")),
						mIL_AST.CallFunc(Span((7, 29), (7, 34)), mSPO2IL.RegId(3), mSPO2IL.Id("...*..."), mSPO2IL.RegId(2)),
						mIL_AST.ReturnIf(Span((7, 29), (7, 34)), mSPO2IL.RegId(3), mIL_AST.cTrue)
					)
				);
			}
		)
	);
}
