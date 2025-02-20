using System.IO;

public static class
mStdLib {
	
	private static mMaybe.tMaybe<mVM_Data.tData> _ImportData;
	
	public static mVM_Data.tData
	GetImportData(
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		if (_ImportData.IsSome(out var Data)) {
			return Data;
		}
		
		var ImportData = mVM.Run(
			mIL_Parser.Module.ParseText(
				File.ReadAllText("src/Std.ILT"), // TODO Update to new pair structure
				Path.Combine(Directory.GetParent(mStd.File()).FullName, "Std.ILT"),
				aDebugStream
			),
			mVM_Data.Empty(),
			_ => $"{_.Start.Id}({_.Start.Row}:{_.Start.Col} .. {_.End.Row}:{_.End.Col})",
			aDebugStream
		);
		_ImportData = ImportData;
		return ImportData;
	}
}
