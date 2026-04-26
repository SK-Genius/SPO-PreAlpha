// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT Common/mMaybe
// IMPORT mSPO_Diagnostics

public static class
mSPO_Diagnostics_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO_Diagnostics),
		[
			mTest.Test("Parser diagnostics use parser positions",
				_ => {
					var Diagnostic = mSPO_Diagnostics.GetModuleDiagnostics(
						"""
						X =
						§EXPORT X
						
						""",
						"test.spo",
						_ => {}
					).TryFirst(
					).AssertNotEmpty(
						() => "expect diagnostic"
					);
					
					mAssert.AreEquals(Diagnostic.Source, "parser");
					mAssert.AreEquals(
						Diagnostic.Message,
						"""
						expect '§TYPE'
						"""
					);
					mAssert.AreEquals(Diagnostic.Pos.Start.Row, (tNat32)1);
					mAssert.AreEquals(Diagnostic.Pos.Start.Col, (tNat32)4);
				}
			),
			mTest.Test("Type diagnostics use semantic positions",
				_ => {
					var Diagnostic = mSPO_Diagnostics.GetModuleDiagnostics(
						"""
						§DEF X € [#Bla §INT] = 1
						§EXPORT X
						
						""",
						"test.spo",
						_ => {}
					).TryFirst(
					).AssertNotEmpty(
						() => "expect diagnostic"
					);
					
					mAssert.AreEquals(Diagnostic.Source, "types");
					mAssert.AreEquals(
						Diagnostic.Message,
						"""
						
						in:
						  §INT
						!<
						  [ #_Bla... §INT ]
						
						"""
					);
					mAssert.AreEquals(Diagnostic.Pos.Start.Row, (tNat32)1);
					mAssert.AreEquals(Diagnostic.Pos.Start.Col, (tNat32)24);
					mAssert.AreEquals(Diagnostic.Pos.End.Row, (tNat32)1);
					mAssert.AreEquals(Diagnostic.Pos.End.Col, (tNat32)24);
				}
			),
		]
	);
}
