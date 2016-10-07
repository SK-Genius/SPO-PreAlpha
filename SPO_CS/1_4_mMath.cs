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

public static class mMath {
	//================================================================================
	public static tInt32
	Max(
		tInt32 a1,
		tInt32 a2
	//================================================================================
	) {
		return (a1 > a2) ? a1 : a2;
	}
}
