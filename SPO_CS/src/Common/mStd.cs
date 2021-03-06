﻿#nullable enable

using System.Diagnostics.CodeAnalysis;

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
	
	public static t Call<t>(tFunc<t> a) => a();
	
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
		[NotNullWhen(false)] this object? a
	) => a is null;
	
	public static t?
	MaybeNull<t>(
		this t a
	) where t : class => a;
	
	public sealed class
	tLazy<t> {
		private mStd.tFunc<t>? _Func;
		private t _Value;
		
		public t Value {
			get {
				if (this._Func is null) {
					return this._Value;
				} else {
					this._Value = this._Func();
					this._Func = null;
					return this._Value;
				}
			}
		}
		
		internal
		tLazy(
			t a
		) {
			this._Value = a;
			this._Func = null;
		}
		
		internal
		tLazy(
			mStd.tFunc<t> a
		) {
			this._Func = a;
			this._Value = default!;
		}
	}
	
	public static tLazy<t>
	NonLazy<t>(
		t a
	) => new tLazy<t>(a);
	
	public static tLazy<t>
	Lazy<t>(
		mStd.tFunc<t> a
	) => new tLazy<t>(a);
}
