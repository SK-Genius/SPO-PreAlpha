﻿//IMPORT mTest.cs
//IMPORT mSPO2IL.cs
//IMPORT mSPO_Parser.cs

#nullable enable

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
			Ident = "",
			Row = aStart.Row,
			Col = aStart.Col
		},
		new tPos {
			Ident = "",
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO2IL),
		mTest.Test(
			"MapExpresion",
			aStreamOut => {
				var ExpressionNode = mSPO_Parser.Expression.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"2 .< (4 .+ 3) < 3",
					"",
					a => aStreamOut(a())
				);
				
				var Def = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge).NewDefConstructor();
				mAssert.AreEquals(Def.MapExpresion(ExpressionNode), mSPO2IL.TempReg(11));
				
				mAssert.AreEquals(
					Def.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 17), (1, 17)), mSPO2IL.TempReg(1), "3"),
						mIL_AST.CreatePair(Span((1, 17), (1, 17)), mSPO2IL.TempReg(2), mSPO2IL.TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((1, 12), (1, 12)), mSPO2IL.TempReg(3), "3"),
						mIL_AST.CreatePair(Span((1, 12), (1, 12)), mSPO2IL.TempReg(4), mSPO2IL.TempReg(3), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((1, 7), (1, 7)), mSPO2IL.TempReg(5), "4"),
						mIL_AST.CreatePair(Span((1, 7), (1, 7)), mSPO2IL.TempReg(6), mSPO2IL.TempReg(5), mSPO2IL.TempReg(4)),
						mIL_AST.CallFunc(Span((1, 7), (1, 12)), mSPO2IL.TempReg(7), mSPO2IL.Ident("...+..."), mSPO2IL.TempReg(6)),
						mIL_AST.CreatePair(Span((1, 7), (1, 12)), mSPO2IL.TempReg(8), mSPO2IL.TempReg(7), mSPO2IL.TempReg(2)),
						mIL_AST.CreateInt(Span((1, 1), (1, 1)), mSPO2IL.TempReg(9), "2"),
						mIL_AST.CreatePair(Span((1, 1), (1, 1)), mSPO2IL.TempReg(10), mSPO2IL.TempReg(9), mSPO2IL.TempReg(8)),
						mIL_AST.CallFunc(Span((1, 1), (1, 17)), mSPO2IL.TempReg(11), mSPO2IL.Ident("...<...<..."), mSPO2IL.TempReg(10))
					)
				);
			}
		),
		mTest.Test(
			"MapDef1",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"§DEF a = (1, 2)",
					"",
					a => aStreamOut(a())
				);
				
				var DefConstructor = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge).NewDefConstructor();
				DefConstructor.MapDef(DefNode);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 14), (1, 14)), mSPO2IL.TempReg(1), "2"),
						mIL_AST.CreatePair(Span((1, 14), (1, 14)), mSPO2IL.TempReg(2), mSPO2IL.TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((1, 11), (1, 11)), mSPO2IL.TempReg(3), "1"),
						mIL_AST.CreatePair(Span((1, 11), (1, 11)), mSPO2IL.TempReg(4), mSPO2IL.TempReg(3), mSPO2IL.TempReg(2)),
						
						mIL_AST.Alias(Span((1, 1), (1, 6)), mSPO2IL.Ident("a"), mSPO2IL.TempReg(4))
					)
				);
			}
		),
		mTest.Test(
			"MapDefMatch",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"(§DEF a, (§DEF b, §DEF c)) = (1, (2, 3))",
					"",
					a => aStreamOut(a())
				);
				
				var DefConstructor = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge).NewDefConstructor();
				DefConstructor.MapDef(DefNode);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 38), (1, 38)), mSPO2IL.TempReg(1), "3"),
						mIL_AST.CreatePair(Span((1, 38), (1, 38)), mSPO2IL.TempReg(2), mSPO2IL.TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((1, 35), (1, 35)), mSPO2IL.TempReg(3), "2"),
						mIL_AST.CreatePair(Span((1, 35), (1, 35)), mSPO2IL.TempReg(4), mSPO2IL.TempReg(3), mSPO2IL.TempReg(2)),
						mIL_AST.CreatePair(Span((1, 34), (1, 39)), mSPO2IL.TempReg(5), mSPO2IL.TempReg(4), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((1, 31), (1, 31)), mSPO2IL.TempReg(6), "1"),
						mIL_AST.CreatePair(Span((1, 31), (1, 31)), mSPO2IL.TempReg(7), mSPO2IL.TempReg(6), mSPO2IL.TempReg(5)),
						
						mIL_AST.GetFirst(Span((1, 2), (1, 7)), mSPO2IL.TempReg(8), mSPO2IL.TempReg(7)),
						mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.Ident("a"), mSPO2IL.TempReg(8)),
						mIL_AST.GetSecond(Span((1, 2), (1, 7)), mSPO2IL.TempReg(9), mSPO2IL.TempReg(7)),
						mIL_AST.GetFirst(Span((1, 10), (1, 25)), mSPO2IL.TempReg(10), mSPO2IL.TempReg(9)),
						mIL_AST.GetFirst(Span((1, 11), (1, 16)), mSPO2IL.TempReg(11), mSPO2IL.TempReg(10)),
						mIL_AST.Alias(Span((1, 11), (1, 16)), mSPO2IL.Ident("b"), mSPO2IL.TempReg(11)),
						mIL_AST.GetSecond(Span((1, 11), (1, 16)), mSPO2IL.TempReg(12), mSPO2IL.TempReg(10)),
						mIL_AST.GetFirst(Span((1, 19), (1, 24)), mSPO2IL.TempReg(13), mSPO2IL.TempReg(12)),
						mIL_AST.Alias(Span((1, 19), (1, 24)), mSPO2IL.Ident("c"), mSPO2IL.TempReg(13)),
						
						mIL_AST.GetSecond(Span((1, 19), (1, 24)), mSPO2IL.TempReg(14), mSPO2IL.TempReg(12)),
						mIL_AST.GetSecond(Span((1, 10), (1, 25)), mSPO2IL.TempReg(15), mSPO2IL.TempReg(9))
					)
				);
			}
		),
		mTest.Test(
			"MatchTuple",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"(§DEF a, §DEF b, §DEF c) = (1, 2, 3)",
					"",
					a => aStreamOut(a())
				);
				
				var Module = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge).NewDefConstructor();
				Module.MapDef(DefNode);
				
				mAssert.AreEquals(
					Module.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 35), (1, 35)), mSPO2IL.TempReg(1), "3"),
						mIL_AST.CreatePair(Span((1, 35), (1, 35)), mSPO2IL.TempReg(2), mSPO2IL.TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((1, 32), (1, 32)), mSPO2IL.TempReg(3), "2"),
						mIL_AST.CreatePair(Span((1, 32), (1, 32)), mSPO2IL.TempReg(4), mSPO2IL.TempReg(3), mSPO2IL.TempReg(2)),
						mIL_AST.CreateInt(Span((1, 29), (1, 29)), mSPO2IL.TempReg(5), "1"),
						mIL_AST.CreatePair(Span((1, 29), (1, 29)), mSPO2IL.TempReg(6), mSPO2IL.TempReg(5), mSPO2IL.TempReg(4)),
						
						mIL_AST.GetFirst(Span((1, 2), (1, 7)), mSPO2IL.TempReg(7), mSPO2IL.TempReg(6)),
						mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.Ident("a"), mSPO2IL.TempReg(7)),
						mIL_AST.GetSecond(Span((1, 2), (1, 7)), mSPO2IL.TempReg(8), mSPO2IL.TempReg(6)),
						mIL_AST.GetFirst(Span((1, 10), (1, 15)), mSPO2IL.TempReg(9), mSPO2IL.TempReg(8)),
						mIL_AST.Alias(Span((1, 10), (1, 15)), mSPO2IL.Ident("b"), mSPO2IL.TempReg(9)),
						mIL_AST.GetSecond(Span((1, 10), (1, 15)), mSPO2IL.TempReg(10), mSPO2IL.TempReg(8)),
						mIL_AST.GetFirst(Span((1, 18), (1, 23)), mSPO2IL.TempReg(11), mSPO2IL.TempReg(10)),
						mIL_AST.Alias(Span((1, 18), (1, 23)), mSPO2IL.Ident("c"), mSPO2IL.TempReg(11)),
						mIL_AST.GetSecond(Span((1, 18), (1, 23)), mSPO2IL.TempReg(12), mSPO2IL.TempReg(10))
					)
				);
			}
		),
		mTest.Test(
			"MapMatchPrefix",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"(§DEF a, §DEF b, (#bla (§DEF c , §DEF d))) = (1, 2, (#bla (3, 4)))",
					"",
					a => aStreamOut(a())
				);
				
				var DefConstructor = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge).NewDefConstructor();
				DefConstructor.MapDef(DefNode);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreateInt(Span((1, 63), (1, 63)), mSPO2IL.TempReg(1), "4"),
						mIL_AST.CreatePair(Span((1, 63), (1, 63)), mSPO2IL.TempReg(2), mSPO2IL.TempReg(1), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((1, 60), (1, 60)), mSPO2IL.TempReg(3), "3"),
						mIL_AST.CreatePair(Span((1, 60), (1, 60)), mSPO2IL.TempReg(4), mSPO2IL.TempReg(3), mSPO2IL.TempReg(2)),
						mIL_AST.AddPrefix(Span((1, 54), (1, 64)), mSPO2IL.TempReg(5), mSPO2IL.Ident("bla..."), mSPO2IL.TempReg(4)),
						mIL_AST.CreatePair(Span((1, 54), (1, 64)), mSPO2IL.TempReg(6), mSPO2IL.TempReg(5), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((1, 50), (1, 50)), mSPO2IL.TempReg(7), "2"),
						mIL_AST.CreatePair(Span((1, 50), (1, 50)), mSPO2IL.TempReg(8), mSPO2IL.TempReg(7), mSPO2IL.TempReg(6)),
						mIL_AST.CreateInt(Span((1, 47), (1, 47)), mSPO2IL.TempReg(9), "1"),
						mIL_AST.CreatePair(Span((1, 47), (1, 47)), mSPO2IL.TempReg(10), mSPO2IL.TempReg(9), mSPO2IL.TempReg(8)),
						
						mIL_AST.GetFirst(Span((1, 2), (1, 7)), mSPO2IL.TempReg(11), mSPO2IL.TempReg(10)),
						mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.Ident("a"), mSPO2IL.TempReg(11)),
						mIL_AST.GetSecond(Span((1, 2), (1, 7)), mSPO2IL.TempReg(12), mSPO2IL.TempReg(10)),
						mIL_AST.GetFirst(Span((1, 10), (1, 15)), mSPO2IL.TempReg(13), mSPO2IL.TempReg(12)),
						mIL_AST.Alias(Span((1, 10), (1, 15)), mSPO2IL.Ident("b"), mSPO2IL.TempReg(13)),
						mIL_AST.GetSecond(Span((1, 10), (1, 15)), mSPO2IL.TempReg(14), mSPO2IL.TempReg(12)),
						mIL_AST.GetFirst(Span((1, 18), (1, 41)), mSPO2IL.TempReg(15), mSPO2IL.TempReg(14)),
						mIL_AST.SubPrefix(Span((1, 18), (1, 41)), mSPO2IL.TempReg(16), mSPO2IL.Ident("bla..."), mSPO2IL.TempReg(15)),
						mIL_AST.GetFirst(Span((1, 25), (1, 30)), mSPO2IL.TempReg(17), mSPO2IL.TempReg(16)),
						mIL_AST.Alias(Span((1, 25), (1, 30)), mSPO2IL.Ident("c"), mSPO2IL.TempReg(17)),
						mIL_AST.GetSecond(Span((1, 25), (1, 30)), mSPO2IL.TempReg(18), mSPO2IL.TempReg(16)),
						mIL_AST.GetFirst(Span((1, 34), (1, 39)), mSPO2IL.TempReg(19), mSPO2IL.TempReg(18)),
						mIL_AST.Alias(Span((1, 34), (1, 39)), mSPO2IL.Ident("d"), mSPO2IL.TempReg(19)),
						
						mIL_AST.GetSecond(Span((1, 34), (1, 39)), mSPO2IL.TempReg(20), mSPO2IL.TempReg(18)),
						mIL_AST.GetSecond(Span((1, 18), (1, 41)), mSPO2IL.TempReg(21), mSPO2IL.TempReg(14))
					)
				);
			}
		),
		mTest.Test(
			"MapLambda1",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"§DEF x = §DEF a => 2 .* a",
					"",
					a => aStreamOut(a())
				);
				
				var DefConstructor = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge).NewDefConstructor();
				DefConstructor.MapDef(DefNode);
				
				mAssert.AreEquals(DefConstructor.ModuleConstructor.Defs.Size(), 2);
				mAssert.AreEquals(
					DefConstructor.ModuleConstructor.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.Alias(Span((1, 10), (1, 25)), mSPO2IL.Ident("...*..."), mIL_AST.cEnv),
						
						mIL_AST.Alias(Span((1, 10), (1, 15)), mSPO2IL.Ident("a"), mIL_AST.cArg),
						
						mIL_AST.CreatePair(Span((1, 25), (1, 25)), mSPO2IL.TempReg(1), mSPO2IL.Ident("a"), mIL_AST.cEmpty),
						mIL_AST.CreateInt(Span((1, 20), (1, 20)), mSPO2IL.TempReg(2), "2"),
						mIL_AST.CreatePair(Span((1, 20), (1, 20)), mSPO2IL.TempReg(3), mSPO2IL.TempReg(2), mSPO2IL.TempReg(1)),
						mIL_AST.CallFunc(Span((1, 20), (1, 25)), mSPO2IL.TempReg(4), mSPO2IL.Ident("...*..."), mSPO2IL.TempReg(3)),
						mIL_AST.ReturnIf(Span((1, 20), (1, 25)), mSPO2IL.TempReg(4), mIL_AST.cTrue)
					)
				);
				
				mAssert.AreEquals(DefConstructor.Index, 0);
				
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CallFunc(Span((1, 10), (1, 25)), mSPO2IL.TempReg(1), mSPO2IL.TempDef(1), mSPO2IL.Ident("...*...")),
						mIL_AST.Alias(Span((1, 1), (1, 6)), mSPO2IL.Ident("x"), mSPO2IL.TempReg(1))
					)
				);
				
				mAssert.AreEquals(
					DefConstructor.UnsolvedSymbols.ToStream(),
					mStream.Stream((mSPO2IL.Ident("...*..."), Span((1, 20), (1, 25))), (mSPO2IL.TempDef(1), Span((1, 10), (1, 25))))
				);
			}
		),
		mTest.Test(
			"MapLambda2",
			aStreamOut => {
				var DefNode = mSPO_Parser.Def.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"§DEF ...*...+... = (§DEF a, §DEF b, §DEF c) => (a .* b) .+ c",
					"",
					a => aStreamOut(a())
				);
				
				var DefConstructor = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge).NewDefConstructor();
				DefConstructor.MapDef(DefNode);
				
				mAssert.AreEquals(DefConstructor.ModuleConstructor.Defs.Size(), 2);
				mAssert.AreEquals(
					DefConstructor.ModuleConstructor.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.GetFirst(Span((1, 20), (1, 60)), mSPO2IL.Ident("...+..."), mIL_AST.cEnv),
						mIL_AST.GetSecond(Span((1, 20), (1, 60)), mSPO2IL.TempReg(13), mIL_AST.cEnv),
						mIL_AST.GetFirst(Span((1, 20), (1, 60)), mSPO2IL.Ident("...*..."), mSPO2IL.TempReg(13)), // TODO
						mIL_AST.GetSecond(Span((1, 20), (1, 60)), mSPO2IL.TempReg(14), mSPO2IL.TempReg(13)),
						
						mIL_AST.GetFirst(Span((1, 21), (1, 26)), mSPO2IL.TempReg(1), mIL_AST.cArg),
						mIL_AST.Alias(Span((1, 21), (1, 26)), mSPO2IL.Ident("a"), mSPO2IL.TempReg(1)),
						mIL_AST.GetSecond(Span((1, 21), (1, 26)), mSPO2IL.TempReg(2), mIL_AST.cArg),
						mIL_AST.GetFirst(Span((1, 29), (1, 34)), mSPO2IL.TempReg(3), mSPO2IL.TempReg(2)),
						mIL_AST.Alias(Span((1, 29), (1, 34)), mSPO2IL.Ident("b"), mSPO2IL.TempReg(3)),
						mIL_AST.GetSecond(Span((1, 29), (1, 34)), mSPO2IL.TempReg(4), mSPO2IL.TempReg(2)),
						mIL_AST.GetFirst(Span((1, 37), (1, 42)), mSPO2IL.TempReg(5), mSPO2IL.TempReg(4)),
						mIL_AST.Alias(Span((1, 37), (1, 42)), mSPO2IL.Ident("c"), mSPO2IL.TempReg(5)),
						mIL_AST.GetSecond(Span((1, 37), (1, 42)), mSPO2IL.TempReg(6), mSPO2IL.TempReg(4)),
						
						mIL_AST.CreatePair(Span((1, 60), (1, 60)), mSPO2IL.TempReg(7), mSPO2IL.Ident("c"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((1, 54), (1, 54)), mSPO2IL.TempReg(8), mSPO2IL.Ident("b"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((1, 49), (1, 49)), mSPO2IL.TempReg(9), mSPO2IL.Ident("a"), mSPO2IL.TempReg(8)),
						mIL_AST.CallFunc(Span((1, 49), (1, 54)), mSPO2IL.TempReg(10), mSPO2IL.Ident("...*..."), mSPO2IL.TempReg(9)),
						mIL_AST.CreatePair(Span((1, 49), (1, 54)), mSPO2IL.TempReg(11), mSPO2IL.TempReg(10), mSPO2IL.TempReg(7)),
						mIL_AST.CallFunc(Span((1, 48), (1, 60)), mSPO2IL.TempReg(12), mSPO2IL.Ident("...+..."), mSPO2IL.TempReg(11)),
						mIL_AST.ReturnIf(Span((1, 48), (1, 60)), mSPO2IL.TempReg(12), mIL_AST.cTrue)
					)
				);
				
				mAssert.AreEquals(DefConstructor.Index, 0);
				mAssert.AreEquals(
					DefConstructor.Commands.ToStream(),
					mStream.Stream(
						mIL_AST.CreatePair(Span((1, 20), (1, 60)), mSPO2IL.TempReg(1), mSPO2IL.Ident("...*..."), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((1, 20), (1, 60)), mSPO2IL.TempReg(2), mSPO2IL.Ident("...+..."), mSPO2IL.TempReg(1)),
						mIL_AST.CallFunc(Span((1, 20), (1, 60)), mSPO2IL.TempReg(3), mSPO2IL.TempDef(1), mSPO2IL.TempReg(2)),
						mIL_AST.Alias(Span((1, 1), (1, 16)), mSPO2IL.Ident("...*...+..."), mSPO2IL.TempReg(3))
					)
				);
				
				mAssert.AreEquals(
					DefConstructor.UnsolvedSymbols.ToStream(),
					mStream.Stream(
						(mSPO2IL.Ident("...+..."), Span((1, 48), (1, 60))),
						(mSPO2IL.Ident("...*..."), Span((1, 49), (1, 54))),
						(mSPO2IL.TempDef(1), Span((1, 20), (1, 60)))
					)
				);
			}
		),
		mTest.Test(
			"MapNestedMatch",
			aStreamOut => {
				var LambdaNode = mSPO_Parser.Lambda.ParseText(
					//        1         2         3         4         5         6         7         8
					//2345678901234567890123456789012345678901234567890123456789012345678901234567890
					"(§DEF a, §DEF b, (§DEF x, §DEF y, §DEF z)) => a .* z",
					"",
					a => aStreamOut(a())
				);
				
				var ModuleConstructor = mSPO2IL.NewModuleConstructor<tSpan>(mSpan.Merge);
				var (DefIndex, UnsolvedSymbols) = ModuleConstructor.MapLambda(LambdaNode);
				
				mAssert.AreEquals(ModuleConstructor.Defs.Size(), 1);
				mAssert.AreEquals(DefIndex, 0);
				mAssert.AreEquals(
					ModuleConstructor.Defs.Get(DefIndex).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.Alias(Span((1, 1), (1, 52)), mSPO2IL.Ident("...*..."), mIL_AST.cEnv),
						
						mIL_AST.GetFirst(Span((1, 2), (1, 7)), mSPO2IL.TempReg(1), mIL_AST.cArg),
						mIL_AST.Alias(Span((1, 2), (1, 7)), mSPO2IL.Ident("a"), mSPO2IL.TempReg(1)),
						mIL_AST.GetSecond(Span((1, 2), (1, 7)), mSPO2IL.TempReg(2), mIL_AST.cArg),
						mIL_AST.GetFirst(Span((1, 10), (1, 15)), mSPO2IL.TempReg(3), mSPO2IL.TempReg(2)),
						mIL_AST.Alias(Span((1, 10), (1, 15)), mSPO2IL.Ident("b"), mSPO2IL.TempReg(3)),
						mIL_AST.GetSecond(Span((1, 10), (1, 15)), mSPO2IL.TempReg(4), mSPO2IL.TempReg(2)),
						mIL_AST.GetFirst(Span((1, 18), (1, 41)), mSPO2IL.TempReg(5), mSPO2IL.TempReg(4)),
						mIL_AST.GetFirst(Span((1, 19), (1, 24)), mSPO2IL.TempReg(6), mSPO2IL.TempReg(5)),
						mIL_AST.Alias(Span((1, 19), (1, 24)), mSPO2IL.Ident("x"), mSPO2IL.TempReg(6)),
						mIL_AST.GetSecond(Span((1, 19), (1, 24)), mSPO2IL.TempReg(7), mSPO2IL.TempReg(5)),
						mIL_AST.GetFirst(Span((1, 27), (1, 32)), mSPO2IL.TempReg(8), mSPO2IL.TempReg(7)),
						mIL_AST.Alias(Span((1, 27), (1, 32)), mSPO2IL.Ident("y"), mSPO2IL.TempReg(8)),
						mIL_AST.GetSecond(Span((1, 27), (1, 32)), mSPO2IL.TempReg(9), mSPO2IL.TempReg(7)),
						mIL_AST.GetFirst(Span((1, 35), (1, 40)), mSPO2IL.TempReg(10), mSPO2IL.TempReg(9)),
						mIL_AST.Alias(Span((1, 35), (1, 40)), mSPO2IL.Ident("z"), mSPO2IL.TempReg(10)),
						mIL_AST.GetSecond(Span((1, 35), (1, 40)), mSPO2IL.TempReg(11), mSPO2IL.TempReg(9)),
						mIL_AST.GetSecond(Span((1, 18), (1, 41)), mSPO2IL.TempReg(12), mSPO2IL.TempReg(4)),
						
						mIL_AST.CreatePair(Span((1, 52), (1, 52)), mSPO2IL.TempReg(13), mSPO2IL.Ident("z"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((1, 47), (1, 47)), mSPO2IL.TempReg(14), mSPO2IL.Ident("a"), mSPO2IL.TempReg(13)),
						mIL_AST.CallFunc(Span((1, 47), (1, 52)), mSPO2IL.TempReg(15), mSPO2IL.Ident("...*..."), mSPO2IL.TempReg(14)),
						mIL_AST.ReturnIf(Span((1, 47), (1, 52)), mSPO2IL.TempReg(15), mIL_AST.cTrue)
					)
				);
				
				mAssert.AreEquals(
					UnsolvedSymbols.ToStream(),
					mStream.Stream((mSPO2IL.Ident("...*..."), Span((1, 47), (1, 52))))
				);
			}
		),
		mTest.Test(
			"MapModule",
			aStreamOut => {
				var ModuleNode = mSPO_Parser.Module.ParseText(
					mStream.Stream(
						//        1         2         3         4         5         6         7         8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"§IMPORT (",
						"	§DEF T € [[]]",
						"	§DEF ...*... € [[T, T] => T]",
						"	§DEF k € T",
						")",
						"",
						"§DEF x... = (§DEF a € T) => k .* a",
						"§DEF y = .x k",
						"",
						"§EXPORT y"
					).Join((a1, a2) => a1 + "\n" + a2, ""),
					"",
					a => aStreamOut(a())
				);
				
				var ModuleConstructor = mSPO2IL.MapModule(ModuleNode, mSpan.Merge).ElseThrow();
				
				mAssert.AreEquals(ModuleConstructor.Defs.Size(), 2);
				
				mAssert.AreEquals(
					ModuleConstructor.Defs.Get(0).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.Alias(Span((1, 1), (10, 9)), mSPO2IL.TempDef(1), mIL_AST.cEnv),
						
						mIL_AST.GetFirst(Span((2, 2), (2, 14)), mSPO2IL.TempReg(1), mIL_AST.cArg),
						mIL_AST.Alias(Span((2, 2), (2, 7)), mSPO2IL.Ident("T"), mSPO2IL.TempReg(1)),
						mIL_AST.GetSecond(Span((2, 2), (2, 14)), mSPO2IL.TempReg(2), mIL_AST.cArg),
						mIL_AST.GetFirst(Span((3, 2), (3, 29)), mSPO2IL.TempReg(3), mSPO2IL.TempReg(2)),
						mIL_AST.Alias(Span((3, 2), (3, 13)), mSPO2IL.Ident("...*..."), mSPO2IL.TempReg(3)),
						mIL_AST.GetSecond(Span((3, 2), (3, 29)), mSPO2IL.TempReg(4), mSPO2IL.TempReg(2)),
						mIL_AST.GetFirst(Span((4, 2), (4, 11)), mSPO2IL.TempReg(5), mSPO2IL.TempReg(4)),
						mIL_AST.Alias(Span((4, 2), (4, 7)), mSPO2IL.Ident("k"), mSPO2IL.TempReg(5)),
						mIL_AST.GetSecond(Span((4, 2), (4, 11)), mSPO2IL.TempReg(6), mSPO2IL.TempReg(4)),
						
						mIL_AST.CreatePair(Span((7, 13), (7, 34)), mSPO2IL.TempReg(7), mSPO2IL.Ident("k"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((7, 13), (7, 34)), mSPO2IL.TempReg(8), mSPO2IL.Ident("...*..."), mSPO2IL.TempReg(7)),
						mIL_AST.CallFunc(Span((7, 13), (7, 34)), mSPO2IL.TempReg(9), mSPO2IL.TempDef(1), mSPO2IL.TempReg(8)),
						mIL_AST.Alias(Span((7, 1), (7, 9)), mSPO2IL.Ident("x..."), mSPO2IL.TempReg(9)),
						mIL_AST.CallFunc(Span((8, 10), (8, 13)), mSPO2IL.TempReg(10), mSPO2IL.Ident("x..."), mSPO2IL.Ident("k")),
						mIL_AST.Alias(Span((8, 1), (8, 6)), mSPO2IL.Ident("y"), mSPO2IL.TempReg(10)),
						mIL_AST.ReturnIf(Span((10, 1), (10, 9)), mSPO2IL.Ident("y"), mIL_AST.cTrue)
					)
				);
				
				mAssert.AreEquals(
					ModuleConstructor.Defs.Get(1).Commands.ToStream(),
					mStream.Stream(
						mIL_AST.GetFirst(Span((7, 13), (7, 34)), mSPO2IL.Ident("...*..."), mIL_AST.cEnv),
						mIL_AST.GetSecond(Span((7, 13), (7, 34)), mSPO2IL.TempReg(4), mIL_AST.cEnv),
						mIL_AST.GetFirst(Span((7, 13), (7, 34)), mSPO2IL.Ident("k"), mSPO2IL.TempReg(4)),
						mIL_AST.GetSecond(Span((7, 13), (7, 34)), mSPO2IL.TempReg(5), mSPO2IL.TempReg(4)),
						
						mIL_AST.Alias(Span((7, 14), (7, 19)), mSPO2IL.Ident("a"), mIL_AST.cArg),
						
						mIL_AST.CreatePair(Span((7, 34), (7, 34)), mSPO2IL.TempReg(1), mSPO2IL.Ident("a"), mIL_AST.cEmpty),
						mIL_AST.CreatePair(Span((7, 29), (7, 29)), mSPO2IL.TempReg(2), mSPO2IL.Ident("k"), mSPO2IL.TempReg(1)),
						mIL_AST.CallFunc(Span((7, 29), (7, 34)), mSPO2IL.TempReg(3), mSPO2IL.Ident("...*..."), mSPO2IL.TempReg(2)),
						mIL_AST.ReturnIf(Span((7, 29), (7, 34)), mSPO2IL.TempReg(3), mIL_AST.cTrue)
					)
				);
			}
		)
	);
}
