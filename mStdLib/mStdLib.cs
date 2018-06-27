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

public static class mStdLib {
	
	private static mVM_Data.tData _ImportData;
	
	//================================================================================
	public static mVM_Data.tData
	GetImportData(
	) {
		if (!(_ImportData is null)) {
			return _ImportData;
		}
		_ImportData = mIL_Interpreter.Run(
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
	_51 := _50, EMPTY
	_52 := . ...<... EMPTY
	_53 := _52, _51
	_54 := . ...>=... EMPTY
	_55 := _54, _53
	_56 := . ...>... EMPTY
	_57 := _56, _55
	_58 := . ...!=... EMPTY
	_59 := _58, _57
	_60 := . ...==... EMPTY
	_61 := _60, _59
	_62 := . ...%... EMPTY
	_63 := _62, _61
	_64 := . .../... EMPTY
	_65 := _64, _63
	_66 := . ...*... EMPTY
	_67 := _66, _65
	_68 := . ...-... EMPTY
	_69 := _68, _67
	_70 := . ...+... EMPTY
	_71 := _70, _69
	_72 := . -... EMPTY
	_73 := _72, _71
	_74 := . ...^... EMPTY
	_75 := _74, _73
	_76 := . ...|... EMPTY
	_77 := _76, _75
	_78 := . ...&... EMPTY
	_79 := _78, _77
	_80 := . !... EMPTY
	_81 := _80, _79
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
			mVM_Data.Empty(),
			aLine => { }
		);
		return _ImportData;
	}
	
	public static readonly tText cImportTuple = (
		@"("+
			"!..., ...&..., ...|..., ...^..., "+
			"-..., ...+..., ...-..., ...*..., .../..., ...%..., "+
			"...==..., ...!=..., ...>..., ...>=..., ...<..., ...<=..."+
		")"
	);
}
