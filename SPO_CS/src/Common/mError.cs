//IMPORT mStd.cs

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
mError {
	public sealed class tError<t> : System.Exception {
		public tError(tText a) : base(a) { }
		public t Value;
	}
	
	public static tError<t>
	Error<t>(
		tText aMsg,
		t aData
	) => new tError<t>(aMsg) { Value = aData };
	
	public static tError<mStd.tEmpty>
	Error(
		tText aMsg
	) => Error(aMsg, mStd.cEmpty);
}
