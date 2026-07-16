#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs

public static class
mError {
	public sealed class
	tError : System.Exception {
		internal
		tError(
			tText aMsg
		) : base(aMsg) {
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tError
	Error(
		tText aMsg
	) => new(aMsg);
}
