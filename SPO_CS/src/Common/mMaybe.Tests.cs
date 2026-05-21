#:include mStd.cs
#:include mTest.cs
#:include mAssert.cs
#:include mMaybe.cs

public static class
mMaybe_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mMaybe),
		[
			mTest.Test("Creating a Maybe with Some and check the value",
				aStreamOut => {
					var SomeInt = mMaybe.Some(1);
					
					mAssert.IsTrue(SomeInt.IsSome(out var Int) && Int == 1);
					
					mAssert.IsFalse(SomeInt.IsNone());
				}
			),
			mTest.Test("Creating a Maybe with None and check the value",
				aStreamOut => {
					var NoneInt = mMaybe.None<tInt32>();
					
					mAssert.IsTrue(NoneInt.IsNone());
					
					mAssert.IsFalse(NoneInt.IsSome(out _));
				}
			),
			mTest.Test("Implicit cast from Empty to None Maybe",
				aStreamOut => {
					mAssert.AreEquals(
						mStd.cEmpty,
						mMaybe.None<tInt32>()
					);
				}
			),
			mTest.Test("Implicit cast from value to Some Maybe",
				aStreamOut => {
					mAssert.AreEquals(
						1,
						mMaybe.Some(1)
					);
				}
			),
			mTest.Test("Using Else with a None Maybe to get a default value",
				aStreamOut => {
					mAssert.AreEquals(
						mMaybe.None<tInt32>().ElseUse(5),
						5
					);
				}
			),
			mTest.Test("Using Else with a Some Maybe returns the contained value",
				aStreamOut => {
					mAssert.AreEquals(
						mMaybe.Some(1).ElseUse(5),
						1
					);
				}
			),
			mTest.Test("Applying a function using Then on a Some Maybe",
				aStreamOut => {
					mAssert.AreEquals(
						mMaybe.Some(1).Then(
							i => i.ToString()
						),
						"1"
					);
				}
			),
			mTest.Test("Applying a function using Then on a None Maybe",
				aStreamOut => {
					mAssert.AreEquals(
						mMaybe.None<tInt32>().Then(
							i => i.ToString()
						),
						mStd.cEmpty
					);
				}
			),
			mTest.Test("Applying a function using ThenTry on a Some Maybe",
				aStreamOut => {
					mAssert.AreEquals(
						mMaybe.Some(1).ThenTry(
							i => mMaybe.Some(i.ToString())
						),
						"1"
					);
				}
			),
			mTest.Test("Applying a function using ThenTry on a None Maybe",
				aStreamOut => {
					mAssert.AreEquals(
						mMaybe.None<tInt32>().ThenTry(
							i => mMaybe.Some(i.ToString())
						),
						mStd.cEmpty
					);
				}
			),
			mTest.Test("Using Match to handle both Some and None cases",
				aStreamOut => {
					mAssert.AreEquals(
						mMaybe.Some(1).Match(
							() => "No value",
							i => i.ToString()
						),
						"1"
					);
					
					mAssert.AreEquals(
						mMaybe.None<tInt32>().Match(
							() => "No value",
							i => i.ToString()
						),
						"No value"
					);
				}
			)
		]
	);
}
