﻿//IMPORT mSPO_Parser.cs
//IMPORT mIL_Interpreter.cs
//IMPORT mSPO2IL.cs
//IMPORT mSpan.cs

#nullable enable

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

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mSPO_Interpreter {
	
	public static mVM_Data.tData
	Run(
		tText aCode,
		tText aIdent,
		mVM_Data.tData aImport,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) => mIL_GenerateOpcodes.Run(
		mSPO2IL.MapModule(
			mSPO_Parser.Module.ParseText(aCode, aIdent, aDebugStream),
			mSpan.Merge
		).ElseThrow(
			a => a
		).Defs.ToStream(
		).MapWithIndex(
			(aIndex, aDef) => (mSPO2IL.TempDef(aIndex), aDef.Type, aDef.Commands.ToStream())
		),
		aImport,
		a => $"{a.Start.Ident}({a.Start.Row}:{a.Start.Col} .. {a.Start.Row}:{a.Start.Col})",
		aDebugStream
	);
}
