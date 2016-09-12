using tBool = System.Boolean;

using tNat8 = System.Byte;
using tNat16 = System.UInt16;
using tNat32 = System.UInt32;
using tNat64 = System.UInt64;

using tInt8 = System.SByte;
using tInt16 = System.Int16;
using tInt32 = System.Int32;
using tInt64 = System.Int64;

using tChar = System.Char;
using tText = System.String;

using tResults = mList.tList<mStd.tAny>;

using tIL_Parser = mParserGen.tParser<mStd.tTuple<char, mStd.tAction<string>>>;

public static class mSPO2IL {
#if !true
	public static mList.tList<mStd.tTuple<tText, mIL_AST.tCommandNode>>
	Create_IL_Module(
		mList.tList<mSPO_AST.tAssignmantNode> aSPO_Module
	) {
		var ResultDefs = mList.List<mList.tList<mStd.tTuple<tText, mIL_AST.tCommandNode>>>();
		
		var RestList = aSPO_Module;
		mSPO_AST.tAssignmantNode SPO_Assignment;
		while (RestList.MATCH(out SPO_Assignment, out RestList)) {
			if (SPO_Assignment._Src is mSPO_AST.tLambdaNode) {
				
			}
		}
		return null;
	}
	
	#region TEST
	
	public static mStd.tFunc<tBool, mStd.tAction<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"",
			mStd.Func(
				(mStd.tAction<tText> aStreamOut) => {
					var ParserResult = mSPO_Parser.MODUL.Parse<tChar>(
						mList.LasyList(
							mTextParser.TextToStream(
								tText.Join(
									"\n",
									"§IMPORT (",
									"	tText :: []",
									"	Print :: tText ->",
									")",
									"",
									"§DEF Main = () => {",
									"	Print(\"Hello World\")",
									""
								)
							)
						)
					);
					
					mParserGen.tResultList ResultList;
					mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>> RestStream;
					ParserResult.MATCH(out ResultList, out RestStream);
					
					mStd.Assert(RestStream.IsNull());
					
					mList.tList<mStd.tTuple<tText, mList.tList<mSPO_AST.tCommandNode>>> Defs;
					ResultList.MATCH(out Defs);
					
					var X = mSPO2IL.Create_IL_Module(ModuleAST);
					
					mList.tList<mIL_VM.tProcDef> Module;
					mMap.tMap<tText, tInt32> ModuleMap;
					mStd.Assert(X.MATCH(out Module, out ModuleMap));
					
					return mIL_Interpreter;
					
					
					
					return true;
				}
			)
		)
	);
	
	#endregion
#endif
}