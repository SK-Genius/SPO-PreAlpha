#:property LangVersion = preview
#:property TargetFramework = net11.0

#:property DefineConstants = noMY_TRACE;noDISABLE_DEBUGGER_HIDDEN
#:property InvariantGlobalization = true
#:property Nullable = enable
#:property Deterministic = true
#:property AnalysisLevel = preview-all
#:property NoWarn = 659,661,8604,8618
#:property EnableSourceControlManagerQueries = false
#:property Optimize = false

global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Diagnostics.Contracts;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;

#pragma warning disable 8019

global using tUnknown = System.Object;

global using tBool = System.Boolean;

global using tNat8 = System.Byte;
global using tNat16 = System.UInt16;
global using tNat32 = System.UInt32;
global using tNat64 = System.UInt64;

global using tInt8 = System.SByte;
global using tInt16 = System.Int16;
global using tInt32 = System.Int32;
global using tInt64 = System.Int64;

global using tChar = System.Char;
global using tText = System.String;

global using tCPtr = System.IntPtr;

#pragma warning restore 8019

#if DISABLE_DEBUGGER_HIDDEN // disable [DebuggerHidden] attribute
	global using DebuggerHiddenAttribute = MyFakeAttribute;
	
	[System.AttributeUsage(
		System.AttributeTargets.Constructor |
		System.AttributeTargets.Method |
		System.AttributeTargets.Property
	)]
	internal sealed class MyFakeAttribute : System.Attribute { } 
#endif
