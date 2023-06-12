global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Diagnostics.Contracts;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;

#if DISABLE_DEBUGGER_HIDDEN // disable [DebuggerHidden] attribute
	global using DebuggerHiddenAttribute = MyFakeAttribute;
	
	[System.AttributeUsage(
		System.AttributeTargets.Constructor |
		System.AttributeTargets.Method |
		System.AttributeTargets.Property
	)]
	internal class MyFakeAttribute : System.Attribute { } 
#endif
