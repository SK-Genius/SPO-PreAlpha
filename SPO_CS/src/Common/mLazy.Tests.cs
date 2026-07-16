#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mTest.cs
#:ref mAssert.cs
#:ref mLazy.cs

public static class
mLazy_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mLazy),
		[
			mTest.Test("NonLazy stores value immediately",
				aStreamOut => {
					var x = 5;
					var lazy = mLazy.NonLazy(x);
					x = 10;
					mAssert.AreEquals(lazy.Value, 5);
				}
			),
			mTest.Test("Lazy defers execution until first Value access and caches result",
				aStreamOut => {
					var calls = 0;
					var lazy = mLazy.Lazy(() => {
						calls++;
						return 7;
					});
					mAssert.AreEquals(calls, 0);
					mAssert.AreEquals(lazy.Value, 7);
					mAssert.AreEquals(calls, 1);
					mAssert.AreEquals(lazy.Value, 7);
					mAssert.AreEquals(calls, 1);
				}
			),
			mTest.Test("Implicit conversion from value behaves like NonLazy",
				aStreamOut => {
					var x = 3;
					mLazy.tLazy<tInt32> lazy = x;
					x = 4;
					mAssert.AreEquals(lazy.Value, 3);
				}
			),
			mTest.Test("Implicit conversion from tFunc behaves like Lazy",
				aStreamOut => {
					var calls = 0;
					mStd.tFunc<tInt32> func = () => {
						calls++;
						return 9;
					};
					mLazy.tLazy<tInt32> lazy = func;
					mAssert.AreEquals(calls, 0);
					mAssert.AreEquals(lazy.Value, 9);
					mAssert.AreEquals(calls, 1);
					mAssert.AreEquals(lazy.Value, 9);
					mAssert.AreEquals(calls, 1);
				}
			)
		]
	);
}
