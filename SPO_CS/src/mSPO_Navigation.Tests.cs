// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT Common/mMaybe
// IMPORT Common/mSpan
// IMPORT Common/mStream
// IMPORT Common/mTextStream
// IMPORT mSPO_Navigation

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mSPO_Navigation_Tests {
	const tText cTestId = "test.spo";
	
	static t[]
	ToArray<t>(
		mStream.tStream<t> aStream
	) => aStream.ToArrayList().ToArray();
	
	static tSpan
	Span(
		(tNat32 Row, tNat32 Col) aStart,
		(tNat32 Row, tNat32 Col) aEnd
	) => mSpan.Span(
		mTextStream.Pos(cTestId, aStart.Row, aStart.Col),
		mTextStream.Pos(cTestId, aEnd.Row, aEnd.Col)
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO_Navigation),
		[
			mTest.Test("Definition resolves top-level definitions",
				_ => {
					var Definition = mSPO_Navigation.GetDefinition(
						"""
						§DEF Foo = 1
						§DEF Bar = Foo
						§EXPORT Bar
						
						""",
						"test.spo",
						mTextStream.Pos("test.spo", 2, 13),
						_ => {}
					).AssertNotEmpty(
						() => "expect definition"
					);
					
					mAssert.AreEquals(Definition.Uri, "test.spo");
					mAssert.AreEquals(
						Definition.Range,
						Span((1, 1), (1, 8)),
						mSpan.Eq_<tPos>(mTextStream.Eq)
					);
				}
			),
			mTest.Test("Definition resolves lambda parameters",
				_ => {
					var Definition = mSPO_Navigation.GetDefinition(
						"""
						§DEF Id = §DEF x => x
						§EXPORT Id
						
						""",
						"test.spo",
						mTextStream.Pos("test.spo", 1, 21),
						_ => {}
					).AssertNotEmpty(
						() => "expect definition"
					);
					
					mAssert.AreEquals(
						Definition.Range,
						Span((1, 11), (1, 16)),
						mSpan.Eq_<tPos>(mTextStream.Eq)
					);
				}
			),
			mTest.Test("Definition resolves local block bindings",
				_ => {
					var Definition = mSPO_Navigation.GetDefinition(
						"""
						§DEF Make = §DEF a => {
							§DEF y = a
							§RETURN y IF §TRUE
						}
						§EXPORT Make
						
						""",
						"test.spo",
						mTextStream.Pos("test.spo", 3, 10),
						_ => {}
					).AssertNotEmpty(
						() => "expect definition"
					);
					
					mAssert.AreEquals(
						Definition.Range,
						Span((2, 2), (2, 7)),
						mSpan.Eq_<tPos>(mTextStream.Eq)
					);
				}
			),
			mTest.Test("Definition resolves generic parameters inside prefixed types",
				_ => {
					var Definition = mSPO_Navigation.GetDefinition(
						"""
						§DEF TypeAlias = [§GENERIC t [#Box t]]
						§EXPORT TypeAlias
						
						""",
						"test.spo",
						mTextStream.Pos("test.spo", 1, 36),
						_ => {}
					).AssertNotEmpty(
						() => "expect definition"
					);
					
					mAssert.AreEquals(
						Definition.Range,
						Span((1, 28), (1, 28)),
						mSpan.Eq_<tPos>(mTextStream.Eq)
					);
				}
			),
			mTest.Test("Outline includes nested function symbols",
				_ => {
					var Symbols = ToArray(
						mSPO_Navigation.GetDocumentSymbols(
							"""
							§DEF Input = 1
							§DEF Map = §DEF a => {
								§DEF y = a
								§RETURN y IF §TRUE
							}
							§EXPORT Map
							
							""",
							"test.spo",
							_ => {}
						)
					);
					
					mAssert.AreEquals(Symbols.Length, 2);
					
					mAssert.AreEquals(Symbols[0].Name, "Input");
					mAssert.AreEquals(Symbols[0].Kind, mSPO_Navigation.tSymbolKind.Variable);
					
					mAssert.AreEquals(Symbols[1].Name, "Map");
					mAssert.AreEquals(Symbols[1].Kind, mSPO_Navigation.tSymbolKind.Function);
					
					var MapChildren = ToArray(Symbols[1].Children);
					mAssert.AreEquals(MapChildren.Length, 2);
					mAssert.AreEquals(MapChildren[0].Name, "a");
					mAssert.AreEquals(MapChildren[1].Name, "y");
				}
			),
			mTest.Test("Outline includes method-call result bindings",
				_ => {
					var Symbols = ToArray(
						mSPO_Navigation.GetDocumentSymbols(
							"""
							§DEF Main = {
								§VAR Source := Value, Read => §DEF Line .
								Line : Trim => §DEF Trimmed .
								§RETURN Trimmed IF §TRUE
							}
							§EXPORT Main
							
							""",
							"test.spo",
							_ => {}
						)
					);
					
					mAssert.AreEquals(Symbols.Length, 1);
					mAssert.AreEquals(Symbols[0].Name, "Main");
					
					var MainChildren = ToArray(Symbols[0].Children);
					mAssert.AreEquals(MainChildren.Length, 2);
					mAssert.AreEquals(MainChildren[0].Name, "Source");
					mAssert.AreEquals(MainChildren[1].Name, "Trimmed");
					
					var SourceChildren = ToArray(MainChildren[0].Children);
					mAssert.AreEquals(SourceChildren.Length, 1);
					mAssert.AreEquals(SourceChildren[0].Name, "Line");
				}
			),
		]
	);
}
