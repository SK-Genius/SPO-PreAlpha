// IMPORT Common/mStd
// IMPORT Common/mMaybe
// IMPORT Common/mSpan
// IMPORT Common/mTextStream
// IMPORT Common/mParserGen
// IMPORT mIL_AST
// IMPORT mIL_Parser
// IMPORT mTokenizer
// IMPORT mVM_Data
// IMPORT mVM

public static class
mStdLib {
	
	private static mMaybe.tMaybe<(mVM_Data.tData Data, mVM_Type.tType Type)> _Import;
	
	public static (mVM_Data.tData Data, mVM_Type.tType Type)
	GetImportData(
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		if (_Import.IsSome(out var Data)) {
			return Data;
		}
		
		var Std_ILT_Path = System.IO.Path.Combine(System.IO.Directory.GetParent(mStd.File()).FullName, "Std.ILT");
		
		var ImportData = mVM.Run(
			mIL_Parser.Module.ParseText(
				System.IO.File.ReadAllText(Std_ILT_Path), // TODO Update to new pair structure
				Std_ILT_Path,
				aDebugStream
			),
			(mVM_Data.Empty(), mVM_Type.Empty()),
			mTextParser.ToText,
			aDebugStream
		);
		_Import = ImportData;
		return ImportData;
	}
}
