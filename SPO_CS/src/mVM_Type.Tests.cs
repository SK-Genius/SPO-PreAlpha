// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mStream
// IMPORT Common/mSpan
// IMPORT Common/mResult
// IMPORT Common/mTextStream
// IMPORT Common/mArrayList
// IMPORT Common/mParserGen
// IMPORT mTokenizer
// IMPORT mVM_Type
// IMPORT mSPO_AST_Types
// IMPORT mSPO_AST
// IMPORT mSPO_Parser

public static class
mVM_Type_Tests {
	private static tText
	Id(
		tText aId
	) => "_" + aId;
	
	private static readonly mStream.tStream<mSPO_AST_Types.tScopeItem> cTestScope = mStream.Stream(
		mSPO_AST_Types.ScopeItem(
			"_...+...",
			mVM_Type.Proc(
				mVM_Type.Empty(),
				mVM_Type.Tuple(
					[mVM_Type.Int(), mVM_Type.Int()]
				),
				mVM_Type.Int()
			)
		)
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mVM_Type),
		mStream.Stream<(tText File, tInt32 LineNr, tText Expr, tText Type)>(
			[
				(mStd.File(), mStd.LineNr(), "()", "[]"),
				(mStd.File(), mStd.LineNr(), "§TRUE", "§BOOL"),
				(mStd.File(), mStd.LineNr(), "§FALSE", "§BOOL"),
				(mStd.File(), mStd.LineNr(), "1", "§INT"),
				(mStd.File(), mStd.LineNr(), "...+...", "[[§INT, §INT] => §INT]"),
				(mStd.File(), mStd.LineNr(), "1 .+ 1", "§INT"),
				(mStd.File(), mStd.LineNr(), "a € §INT => a .+ a", "[§INT => §INT]"),
				(mStd.File(), mStd.LineNr(), ".((a1 € §INT, a2 € §INT, a3 € §INT) => (a1 .+ a2) .+ a3)(1, 2, 3)", "§INT"),
				(
					mStd.File(),
					mStd.LineNr(),
					"""
					§IF (1, 2) MATCH {
						(1, 1) : 1
						(1, _) : 2
						(2, §DEF a) : a
					}
					""",
					"§INT"
				),
				(
					mStd.File(),
					mStd.LineNr(),
					"""
					§IF 1 MATCH {
						1 : 1
						_ : ()
					}
					""",
					"[§INT | []]"
				),
				(
					mStd.File(),
					mStd.LineNr(),
					"""
					§IF 1 MATCH {
						1 : 1
						_ : ()
					}
					""",
					"[[] | §INT]"
				)
			]
		).Map(
			a => mTest.Test(a.Expr + " => " + a.Type,
				aStreamOut => {
					var AST =  mSPO_Parser.Expression.ParseText(
						a.Expr,
						"",
						_ => aStreamOut(_())
					);
					
					var Type = AST.UpdateTypes(
						cTestScope
					).AssertNotError(__ => __.ToText());
					
					var Type_ = mSPO_Parser.Type.ParseText(
						a.Type,
						"",
						_ => { aStreamOut(_()); }
					).AsVM_Type(
						cTestScope
					).AssertNotError(__ => __.ToText());
					
					Type.IsSubType(Type_, mStd.cEmpty)
					.AssertNotError(_ => Type.ToText() + " != " + Type_.ToText());
					
					Type_.IsSubType(Type, mStd.cEmpty)
					.AssertNotError(_ => Type.ToText() + " != " + Type_.ToText());
				},
				a.File,
				a.LineNr
			)
		).ToArrayList(
		).ToArray(
		)
	);
}
