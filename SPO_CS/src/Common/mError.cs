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
			tText aMsg,
			tNat64 aDebugId
		) : base($"[DebugId: {(aDebugId == 0 ? mStd.NewDebugId() : aDebugId)}] {aMsg}") {
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tError
	Error(
		tText aMsg,
		tNat64 aDebugId = 0
	) => new(aMsg, aDebugId);
}
