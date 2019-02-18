//IMPORT mVM.cs
//IMPORT mSPO_Parser.cs
//IMPORT mIL_Parser.cs
//IMPORT mIL_Interpreter.cs

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

public static class
mStdLib {
	
	private static mVM_Data.tData _ImportData;
	
	public static mVM_Data.tData
	GetImportData(
	) {
		if (!(_ImportData is null)) {
			return _ImportData;
		}
		_ImportData = mIL_Interpreter<mStd.tSpan<mTextStream.tPos>>.Run(
			mIL_Parser.Module.ParseText(
@"§DEF Init
	_01 := ENV
	!... := §1ST _01
	_02 := §2ND _01
	...&... := §1ST _02
	_03 := §2ND _02
	...|... := §1ST _03
	_04 := §2ND _03
	...^... := §1ST _04
	_05 := §2ND _04
	-... := §1ST _05
	_06 := §2ND _05
	...+... := §1ST _06
	_07 := §2ND _06
	...-... := §1ST _07
	_08 := §2ND _07
	...*... := §1ST _08
	_09 := §2ND _08
	.../... := §1ST _09
	_10 := §2ND _09
	...%... := §1ST _10
	_11 := §2ND _10
	...==... := §1ST _11
	_12 := §2ND _11
	...!=... := §1ST _12
	_13 := §2ND _12
	...>... := §1ST _13
	_14 := §2ND _13
	...>=... := §1ST _14
	_15 := §2ND _14
	...<... := §1ST _15
	_16 := §2ND _15
	...<=... := §1ST _16
	_50 := . ...<=... EMPTY
	_50_ := +#_...<=... _50
	_51 := {EMPTY} + _50_
	_52 := . ...<... EMPTY
	_52_ := +#_...<... _52
	_53 := {_51} + _52_
	_54 := . ...>=... EMPTY
	_54_ := +#_...>=... _54
	_55 := {_53} + _54_
	_56 := . ...>... EMPTY
	_56_ := +#_...>... _56
	_57 := {_55} + _56_
	_58 := . ...!=... EMPTY
	_58_ := +#_...!=... _58
	_59 := {_57} + _58_
	_60 := . ...==... EMPTY
	_60_ := +#_...==... _60
	_61 := {_59} + _60_
	_62 := . ...%... EMPTY
	_62_ := +#_...%... _62
	_63 := {_61} + _62_
	_64 := . .../... EMPTY
	_64_ := +#_.../... _64
	_65 := {_63} + _64_
	_66 := . ...*... EMPTY
	_66_ := +#_...*... _66
	_67 := {_65} + _66_
	_68 := . ...-... EMPTY
	_68_ := +#_...-... _68
	_69 := {_67} + _68_
	_70 := . ...+... EMPTY
	_70_ := +#_...+... _70
	_71 := {_69} + _70_
	_72 := . -... EMPTY
	_72_ := +#_-... _72
	_73 := {_71} + _72_
	_74 := . ...^... EMPTY
	_74_ := +#_...^... _74
	_75 := {_73} + _74_
	_76 := . ...|... EMPTY
	_76_ := +#_...|... _76
	_77 := {_75} + _76_
	_78 := . ...&... EMPTY
	_78_ := +#_...&... _78
	_79 := {_77} + _78_
	_80 := . !... EMPTY
	_80_ := +#_!... _80
	_81 := {_79} + _80_
	§RETURN _81 IF TRUE
§DEF !...
	Res := §BOOL ARG ^ TRUE
	§RETURN Res IF TRUE
§DEF ...&...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §BOOL A & B
	§RETURN Res IF TRUE
§DEF ...|...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §BOOL A | B
	§RETURN Res IF TRUE
§DEF ...^...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §BOOL A ^ B
	§RETURN Res IF TRUE
§DEF -...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	_1 := ONE
	_0 := §INT _1 - _1
	Res := §INT _0 - B
	§RETURN Res IF TRUE
§DEF ...+...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A + B
	§RETURN Res IF TRUE
§DEF ...-...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A - B
	§RETURN Res IF TRUE
§DEF ...*...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A * B
	§RETURN Res IF TRUE
§DEF .../...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A * B
	§RETURN Res IF TRUE
§DEF ...%...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A * B
	§RETURN Res IF TRUE
§DEF ...==...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A == B
	§RETURN Res IF TRUE
§DEF ...!=...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Comp := §INT A == B
	Res := §BOOL Comp ^ TRUE
	§RETURN Res IF TRUE
§DEF ...>...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	_1 := ONE
	Comp := §INT A <=> B
	Res := §INT Comp == _1
	§RETURN Res IF TRUE
§DEF ...>=...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	_1 := ONE
	_0 := §INT _1 - _1
	Comp := §INT A <=> B
	>? := §INT Comp == _1
	=? := §INT Comp == _0
	Res := §BOOL >? | =?
	§RETURN Res IF TRUE
§DEF ...<...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	_1 := ONE
	_0 := §INT _1 - _1
	_-1 := §INT _0 - _1
	Comp := §INT A <=> B
	Res := §INT Comp == _-1
	§RETURN Res IF TRUE
§DEF ...<=...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	_1 := ONE
	_0 := §INT _1 - _1
	_-1 := §INT _0 - _1
	Comp := §INT A <=> B
	<? := §INT Comp == _-1
	=? := §INT Comp == _0
	Res := §BOOL <? | =?
	§RETURN Res IF TRUE
",
				"mStdLib.cs",
				aLine => { }
			),
			mVM_Data.Empty(),
			a => $"{a.Start.Ident}({a.Start.Row}:{a.Start.Col} .. {a.End.Row}:{a.End.Col})",
			aLine => { }
		);
		return _ImportData;
	}
}
