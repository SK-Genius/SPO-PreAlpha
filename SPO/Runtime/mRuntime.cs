#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
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
	) {
		mAssert.IsTrue(aRecord.IsRecord(out var Fields));
		return Fields.TryGet(("_" + aName).PrefixHash()).AssertNotEmpty();
	}
	
	public static mVM_Data.tData
	CallMethod(
		this mVM_Data.tData aCapability,
		tText aName,
		mVM_Data.tData aArgument
	) {
		mAssert.IsTrue(aCapability.IsInterface(out _, out var Receiver, out var Methods));
		return Methods.Field(aName).Call(aArgument, Receiver);
	}
	
	public static mVM_Data.tData
	Call(
		this mVM_Data.tData aFunction,
		mVM_Data.tData aArgument,
		mVM_Data.tData? aObject = null
	) {
		var Result = mVM_Data.Empty();
		mVM.Run<mSpan.tSpan<mTextStream.tPos>>(
			aFunction,
			aObject ?? mVM_Data.Empty(),
			aArgument,
			Result,
			a => $"{a.Start.Id}({a.Start.Row}:{a.Start.Col})",
			_ => {}
		);
		return Result;
	}
}