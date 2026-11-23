#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../SPO_CS/src/mVM.cs

public static class
mRuntime {
	public static tInt32
	WithCompilerStack(
		mStd.tFunc<tInt32> aRun
	) {
		var Result = 1;
		System.Runtime.ExceptionServices.ExceptionDispatchInfo? Error = null;
		
		// The unoptimized recursive compiler needs more than the Windows main-thread stack.
		var Thread = new System.Threading.Thread(
			() => {
				try {
					Result = aRun();
				} catch (System.Exception Exception) {
					Error = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(Exception);
				}
			},
			maxStackSize: 32 * 1024 * 1024
		);
		
		Thread.Start();
		Thread.Join();
		Error?.Throw();
		return Result;
	}
	
	public static mVM_Data.tData
	Field(
		this mVM_Data.tData aRecord,
		tText aName
	) => aRecord.GetField(("_" + aName).PrefixHash());
	
	public static mVM_Data.tData
	CallMethod(
		this mVM_Data.tData aCapability,
		tText aName,
		mVM_Data.tData aArgument
	) => mVM.CallMethod<mSpan.tSpan<mTextStream.tPos>>(
		aCapability,
		"_" + aName,
		aArgument,
		a => $"{a.Start.Id}({a.Start.Row}:{a.Start.Col})",
		_ => {}
	);
	
	public static mVM_Data.tData
	Call(
		this mVM_Data.tData aFunction,
		mVM_Data.tData aArgument,
		mVM_Data.tData? aObject = null
	) => mVM.Call<mSpan.tSpan<mTextStream.tPos>>(
		aFunction,
		aObject ?? mVM_Data.Empty(),
		aArgument,
		a => $"{a.Start.Id}({a.Start.Row}:{a.Start.Col})",
		_ => {}
	);
}