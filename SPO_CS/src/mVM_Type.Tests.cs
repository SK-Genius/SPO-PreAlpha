﻿//IMPORT mTest.cs
//IMPORT mVM_Type.cs

#nullable enable

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mVM_Type_Tests {
	private static tText
	Id(
		tText aId
	) => "_" + aId;
	
	private static readonly mStream.tStream<(tText Id, mVM_Type.tType)>? cTestScope = mStream.Stream(
		(
			Id: "_...+...",
			Type: mVM_Type.Proc(
				mVM_Type.Empty(),
				mVM_Type.Tuple(
					mVM_Type.Int(),
					mVM_Type.Int()
				),
				mVM_Type.Int()
			)
		)
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mVM_Type),
		mStream.Stream<(tText Expr, tText Type)>(
			("1", "§INT"),
			("...+...", "[[§INT, §INT] => §INT]"),
			("1 .+ 1", "§INT"),
			("a € §INT => a .+ a", "[§INT => §INT]"),
			(".((a1 € §INT, a2 € §INT, a3 € §INT) => (a1 .+ a2) .+ a3)(1, 2, 3)", "§INT"),
			// TODO: (".((a1, a2, a3) => ((a1 .+ a2) .+ a3))(1, 2, 3)", "§INT"),
			(
				"""
				§IF (1, 2) MATCH {
					( 1, 1 ) => 1
					(1, _) => 2
					(2, §DEF a) => a
				}
				""",
				"§INT"
			)
		).Map(
			a => mTest.Test(
				a.Expr + " € " + a.Type,
				aStreamOut => {
					var AST =  mSPO_Parser.Expression.ParseText(
						a.Expr,
						"",
						_ => aStreamOut(_())
					);
					
					mAssert.AreEquals(
						mSPO_AST_Types.UpdateExpressionTypes(
							AST,
							cTestScope
						).ElseThrow(),
						mSPO_AST_Types.ResolveTypeExpression(
							mSPO_Parser.Type.ParseText(
								a.Type,
								"",
								a => aStreamOut(a())
							),
							cTestScope
						).ElseThrow()
					);
				}
			)
		).ToArrayList(
		).ToArray(
		)
	);
}
