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

public static class mDebug {
	
	//================================================================================
	public static void
	Assert(
		tBool a
	//================================================================================
	) {
		#if DEBUG
		mStd.Assert(a);
		#endif
	}
	
	//================================================================================
	public static void
	Assert(
		tBool a,
		tText aMsg
	//================================================================================
	) {
		#if DEBUG
		mStd.Assert(a, aMsg);
		#endif
	}
	
	//================================================================================
	public static void
	Assert(
		tBool a,
		mStd.tFunc<tText> aMsg
	//================================================================================
	) {
		#if DEBUG
		mStd.Assert(a, aMsg);
		#endif
	}
	
	//================================================================================
	public static void
	AssertNot(
		tBool a
	//================================================================================
	) {
		#if DEBUG
		mStd.AssertNot(a);
		#endif
	}
	
	//================================================================================
	public static void
	AssertEq<t>(
		t a1,
		t a2
	//================================================================================
	) {
		#if DEBUG
		mStd.AssertEq(a1, a2);
		#endif
	}
	
	//================================================================================
	public static void
	AssertNotEq<t>(
		t a1,
		t a2
	//================================================================================
	) {
		#if DEBUG
		mStd.AssertNotEq(a1, a2);
		#endif
	}
	
	//================================================================================
	public static t
	AssertNull<t>(
		this t a
	//================================================================================
	) {
		Assert(a.IsNull());
		return a;
	}
	
	//================================================================================
	public static t
	AssertNotNull<t>(
		this t a
	//================================================================================
	) {
		Assert(!a.IsNull());
		return a;
	}
}
