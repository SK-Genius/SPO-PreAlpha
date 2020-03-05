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

public static class mStd {
	
	#region tFunc & tAction
	
	public delegate tRes tFunc<out tRes>();
	public delegate tRes tFunc<out tRes, in tArg>(tArg a);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2>(tArg1 a1, tArg2 a2);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3>(tArg1 a1, tArg2 a2, tArg3 a3);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4, in tArg5>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4, tArg5 a5);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4, in tArg5, in tArg6>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4, tArg5 a5, tArg6 a6);
	
	public static tFunc<tRes> Func<tRes>(tFunc<tRes> a) => a;
	public static tFunc<tRes, tArg> Func<tRes, tArg>(tFunc<tRes, tArg> a) => a;
	public static tFunc<tRes, tArg1, tArg2> Func<tRes, tArg1, tArg2>(tFunc<tRes, tArg1, tArg2> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3> Func<tRes, tArg1, tArg2, tArg3>(tFunc<tRes, tArg1, tArg2, tArg3> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4> Func<tRes, tArg1, tArg2, tArg3, tArg4>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5> Func<tRes, tArg1, tArg2, tArg3, tArg4, tArg5>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5, tArg6> Func<tRes, tArg1, tArg2, tArg3, tArg4, tArg5, tArg6>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5, tArg6> a) => a;
	
	public delegate void tAction();
	public delegate void tAction<in tArg>(tArg a);
	public delegate void tAction<in tArg1, in tArg2>(tArg1 a1, tArg2 a2);
	public delegate void tAction<in tArg1, in tArg2, in tArg3>(tArg1 a1, tArg2 a2, tArg3 a3);
	public delegate void tAction<in tArg1, in tArg2, in tArg3, in tArg4>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4);
	
	public static tAction Action(tAction a) => a;
	public static tAction<tArg> Action<tArg>(tAction<tArg> a) => a;
	public static tAction<tArg1, tArg2> Action<tArg1, tArg2>(tAction<tArg1, tArg2> a) => a;
	public static tAction<tArg1, tArg2, tArg3> Action<tArg1, tArg2, tArg3>(tAction<tArg1, tArg2, tArg3> a) => a;
	public static tAction<tArg1, tArg2, tArg3, tArg4> Action<tArg1, tArg2, tArg3, tArg4>(tAction<tArg1, tArg2, tArg3, tArg4> a) => a;
	
	#endregion
	
	public readonly struct
	tEmpty {
	}
	
	public static readonly tEmpty cEmpty = new tEmpty();
	
	public static tBool
	IsNull(
		this object a
	) => a is null;
	
	public struct tLazy<t> {
		private t _Value;
		public mStd.tFunc<t> Func { private get; set; }
		public t Value {
			get {
				if (this.Func is null) {
					return this._Value;
				} else {
					this.Value = this.Func();
					return this.Value;
				}
			}
			set {
				this._Value = value;
				this.Func = null;
			}
		}
	}
}
