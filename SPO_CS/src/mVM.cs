#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include _GlobalUsings.cs
#:ref mVM_Data.cs
#:ref mVM_View.cs
#:ref mVM_Visible.cs
#:ref mIL_AST.cs
#:ref mIL_GenerateOpcodes.cs

public static class
mVM {
	const tText cEnterMarker = ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>";
	const tText cExitMarker = "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<";
	public static readonly mStd.tAction<mStd.tFunc<tText>> cNoTrace = _ => {};
	
	private sealed class tExecution<tPos> {
		public mVM_Data.tExecute<tPos> Invoke = default!;
		public readonly mVM_Data.tConversionMemo Views = new();
	}
	
	private sealed class tCallStack<tPos> {
		internal mMaybe.tMaybe<tCallStack<tPos>> _Parent;
		internal mArrayList.tArrayList<mVM_Data.tData> _Regs = default!;
		internal mVM_Data.tProcDef<tPos> _ProcDef = default!;
		internal tNat32 _CodePointer;
		internal mStd.tAction<mStd.tFunc<tText>> _TraceOut = default!;
		internal tExecution<tPos> _Execution = default!;
		internal tBool _Succeeded;
		internal mVM_Type.tTypeResolver? _Types;
	}
	
	private static void Copy(mVM_Data.tData aTarget, mVM_Data.tData aSource) {
		aTarget._Value = aSource._Value;
		aTarget._Fields = aSource._Fields;

	}
	
	private static mVM_Data.tData Payload(mVM_Data.tData aData, mVM_Type.tType aType) {
		while (aType.IsRecursive(out var Head, out var Body)) { aType = Body.Substitute(Head, aType); }
		return aType.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set ? aData._Value.To<mVM_Data.tDynamicValue>().Data : aData;
	}
	
	private static tBool Flag(mVM_Data.tData aData, mVM_Type.tType aType) {
		if (aType.Kind is mVM_Type.tKind.True) { return true; }
		if (aType.Kind is mVM_Type.tKind.False) { return false; }
		return Payload(aData, aType)._Value.To<tBool>();
	}
	
	private static mMaybe.tMaybe<tCallStack<tPos>> NewCallStack<tPos>(
		mMaybe.tMaybe<tCallStack<tPos>> aParent,
		mVM_Data.tProcDef<tPos> aCode,
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tAction<mStd.tFunc<tText>> aTrace,
		tExecution<tPos> aExecution,
		mVM_Type.tTypeResolver? aTypes
	) {
		mAssert.IsFalse(aCode.IsTemplate, "A compiler template cannot enter the VM.");
		mAssert.IsTrue(aCode.DefType.Refs[2].Kind is mVM_Type.tKind.Proc, "Only specialized functions can enter the VM.");
		var ReturnType = aCode.DefType.Refs[2].Refs[2];
		
		if (ReturnType.Kind is mVM_Type.tKind.Set or mVM_Type.tKind.Any) {
			Copy(aRes, mVM_Data.Dynamic(mVM_Data.Empty(), mVM_Type.Empty()));
		}
		
		var Registers = mArrayList.List<mVM_Data.tData>();
		(aEnv, aTypes) = aEnv.OpenTypeCaptures(aCode.TypeCaptures, aTypes);
		Registers._Items = new mVM_Data.tData[System.Math.Max((tInt32)aCode._LastReg + 1, 11)];
		Registers.Push(mVM_Data.Empty(), mVM_Data.Int(1), mVM_Data.Bool(false), mVM_Data.Bool(true), mVM_Data.TypeEmpty(), mVM_Data.TypeInt(), mVM_Data.TypeType(), aEnv, aObj, aArg, aRes);
		var Frame = new tCallStack<tPos> {_Parent = aParent, _Regs = Registers, _ProcDef = aCode, _TraceOut = aTrace, _Execution = aExecution, _Types = aTypes};
		
		if (aCode.TypeBindings.Size != 0) {
			var Cache = new System.Collections.Generic.Dictionary<tNat64, mVM_Type.tType>();
			var Bindings = aCode.TypeBindings.ToStream().ToArrayList().ToArray();
			mVM_Type.tType[]? Values = null;
			
			Frame._Types = aType => {
				if (Values is null && Bindings.All(__ => __.Register < Registers.Size)) { Values = Bindings.Select(__ => mVM_Type.Opaque(Registers.Get(__.Register).TypeExpressionValue())).ToArray(); }
				if (Values is not null && Cache.TryGetValue(aType.DebugId, out var Previous)) { return Previous; }
				var Original = aType;
				
				for (var Index = 0; Index < Bindings.Length; Index += 1) {
					var Binding = Bindings[Index];
					if (Binding.Register < Registers.Size) { aType = aType.Substitute(Binding.Symbol, Values is null ? mVM_Type.Opaque(Registers.Get(Binding.Register).TypeExpressionValue()) : Values[Index]); }
				}
				
				aType = aType.Resolve(aTypes);
				if (Values is not null) { Cache.Add(Original.DebugId, aType); }
				return aType;
			};
		}
		
		if (!ReferenceEquals(aTrace, cNoTrace)) {
			aTrace(() => cEnterMarker);
			aTrace(() => "ENV " + (aEnv, aCode.Types.Get(mVM_Data.cEnvReg)).ToVisibleText());
			aTrace(() => "OBJ " + (aObj, aCode.DefType.Refs[2].Refs[0]).ToVisibleText());
			aTrace(() => "ARG " + (aArg, aCode.DefType.Refs[2].Refs[1]).ToVisibleText());
		}
		
		return Frame;
	}
	
	private static mMaybe.tMaybe<mVM_Data.tData> RunCode<tPos>(tExecution<tPos> aExecution, mVM_Data.tProcDef<tPos> aCode, mVM_Data.tData aEnv, mVM_Data.tData aObject, mVM_Data.tData aArgument, mStd.tAction<mStd.tFunc<tText>> aTrace, mVM_Type.tTypeResolver? aTypes) {
		mAssert.IsFalse(aCode.IsTemplate, "A compiler template cannot execute.");
		
		if (aCode.Compilation is mIL_Specialize.tContext<tPos> Compiler && !Compiler.Started) {
			Compiler.Started = true;
			Compiler.Frames = mArrayList.List<mIL_Specialize.tFrame<tPos>>();
			Compiler.ActualTypes = mArrayList.List<mVM_Type.tType>();
			Compiler.NativeInputs.Clear();
			Compiler.NativeResults.Clear();
		}
		
		if (aCode.Entry is not null) { return aCode.Entry(aTypes, aEnv, aObject, aArgument, aTrace, aExecution.Invoke, aExecution.Views); }
		var Result = mVM_Data.Empty();
		var Root = NewCallStack(mMaybe.None<tCallStack<tPos>>(), aCode, aEnv, aObject, aArgument, Result, aTrace, aExecution, aTypes).AssertNotEmpty();
		mMaybe.tMaybe<tCallStack<tPos>> Frame = Root;
		while (Frame.IsSome(out var Current)) { Frame = Current.Step(); }
		return Root._Succeeded ? mMaybe.Some(Result) : mStd.cEmpty;
	}
	
	private static mMaybe.tMaybe<mVM_Data.tData> Execute<tPos>(tExecution<tPos> aExecution, mVM_Data.tCallSite<tPos> aSite, mVM_Data.tData aFunction, mVM_Data.tData aObject, mVM_Data.tData aArgument, mStd.tAction<mStd.tFunc<tText>> aTrace, mVM_Type.tTypeResolver? aTypes) {
		if (aFunction.IsDef<tPos>(out var Definition)) {
			var Target = aSite.Closure!.Targets.TryGet(Definition.CodeId).AssertNotEmpty();
			return mVM_Data.Closure(Target.Code, Target.Environment.Convert(aArgument, aTypes, aExecution.Views).CaptureTypes(Target.Code.TypeCaptures, aTypes));
		}
		
		if (aFunction.IsExternDef(out var NativeDefinition)) { return mVM_Data.NativeClosure(NativeDefinition, aSite.NativeArgument!.Convert(aArgument, aTypes, aExecution.Views)); }
		
		if (aFunction.IsProc<tPos>(out var Token, out var Environment)) {
			if (!aSite.Targets.TryGet(Token.CodeId).IsSome(out var Target)) {
				mAssert.Fail("Unlinked static call at " + Token.PosList.ToStream().TryFirst().Match(aPos => "" + aPos, () => "adapter") + " (code " + Token.CodeId + "). Linked codes: " + aSite.Targets.ToStream().Map(__ => "" + __.Key).Join((aLeft, aRight) => aLeft + ", " + aRight, "") + ".\nImplementation: " + Token.DefType.Refs[2].ToText() + "\nSource: " + aSite.SourceSignature.ToText() + "\nCall: " + aSite.Signature.ToText());
			}
			
			var Object = Target.Object.Convert(aObject, aTypes, aExecution.Views);
			var Result = RunCode(aExecution, Target.Code, Target.Environment.Convert(Environment, aTypes, aExecution.Views), Object, Target.Argument.Convert(aArgument, aTypes, aExecution.Views), aTrace, aTypes);
			return Result.IsSome(out var Value) ? mMaybe.Some(Target.Result.Convert(Value, aTypes, aExecution.Views)) : mMaybe.None<mVM_Data.tData>();
		}
		
		if (aFunction.IsExternProc(out var Native, out var NativeEnvironment)) {
			var Prepared = aSite.NativeTargets.TryGetValue(Native, out var Target);
			mAssert.IsTrue(Prepared || Native.Signature.SameType(aSite.SourceSignature), "A native implementation signature must be linked before execution.");
			var ObjectView = Prepared ? Target.Object : aSite.NativeObject!;
			var Argument = (Prepared ? Target.Argument : aSite.NativeArgument!).Convert(aArgument, aTypes, aExecution.Views);
			var Object = ObjectView.Convert(aObject, aTypes, aExecution.Views);
			
			mVM_Data.tData Receiver(mVM_Data.tData aValue, mVM_Type.tType aType) {
				if (!aType.IsPair(out var Tail, out var Head)) { return aValue._Value.Is(out mVM_Data.tOpaqueValue Opaque) ? Opaque.Data : aValue; }
				var Pair = aValue._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
				return Tail.IsEmpty() ? mVM_Data.Pair(Pair.Item1, Receiver(Pair.Item2, Head)) : mVM_Data.Pair(Receiver(Pair.Item1, Tail), Pair.Item2);
			}
			
			var Result = Native.Execute(NativeEnvironment, Receiver(Object, ObjectView.Target.Resolve(aTypes)), Argument, aTrace).Normalize((Prepared ? Target.Result.Source : aSite.Signature.Refs[2]).Resolve(aTypes), "Ergebnis");
			return Prepared ? Target.Result.Convert(Result, aTypes, aExecution.Views) : Result;
		}
		
		throw mError.Error("Expected a compiled callable.");
	}
	
	private static (mVM_Data.tData Object, mVM_Data.tData Method) ResolveInterfaceCall(mVM_Data.tData aObject, mVM_Type.tType aType, tNat32 aName) {
		if (aType.Kind is mVM_Type.tKind.Interface) {
			var Interface = aObject._Value.To<mVM_Data.tInterface>();
			return (Interface.Object, Interface.Methods.GetField(aName));
		}
		
		var Pair = aObject._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
		var Singleton = aType.Refs[0].IsEmpty();
		var First = ResolveInterfaceCall(Singleton ? Pair.Item2 : Pair.Item1, Singleton ? aType.Refs[1] : aType.Refs[0], aName);
		return (Singleton ? mVM_Data.Pair(Pair.Item1, First.Object) : mVM_Data.Pair(First.Object, Pair.Item2), First.Method);
	}
	
	private static mMaybe.tMaybe<tCallStack<tPos>> Step<tPos>(this tCallStack<tPos> aCallStack) {
		var Code = aCallStack._ProcDef;
		var Index = aCallStack._CodePointer++;
		var (OpCode, Arg1, Arg2, DebugId) = Code.Commands.Get(Index);
		var (FirstType, SecondType, ResultType) = Code.OperandTypes.Get(Index);
		var Regs = aCallStack._Regs;
		var Trace = aCallStack._TraceOut;
		var Types = aCallStack._Types;
		if (!ReferenceEquals(Trace, cNoTrace)) { Trace(() => $">>> {OpCode} {Arg1} {Arg2} // CommandDebugId:{DebugId} // {Code.PosList.Get(Index)}"); }
		
		switch (OpCode) {
			case mVM_Data.tOpCode.NewInt: Regs.Push(mVM_Data.Int((tInt32)Arg1));
			break;
			case mVM_Data.tOpCode.And:
			case mVM_Data.tOpCode.Or:
			case mVM_Data.tOpCode.XOr:
			case mVM_Data.tOpCode.IntsAreEq: {
				var Result = OpCode switch {
					mVM_Data.tOpCode.And => Flag(Regs.Get(Arg1), FirstType) && Flag(Regs.Get(Arg2), SecondType),
					mVM_Data.tOpCode.Or => Flag(Regs.Get(Arg1), FirstType) || Flag(Regs.Get(Arg2), SecondType),
					mVM_Data.tOpCode.XOr => Flag(Regs.Get(Arg1), FirstType) != Flag(Regs.Get(Arg2), SecondType),
					_ => Regs.Get(Arg1)._Value.To<tInt32>() == Regs.Get(Arg2)._Value.To<tInt32>()
				};
				
				Regs.Push(mVM_Data.Dynamic(mVM_Data.Bool(Result), Result ? mVM_Type.True() : mVM_Type.False()));
				break;
			}
			case mVM_Data.tOpCode.IntsComp:
			case mVM_Data.tOpCode.IntsAdd:
			case mVM_Data.tOpCode.IntsSub:
			case mVM_Data.tOpCode.IntsMul:
			case mVM_Data.tOpCode.IntsDiv: {
				var A = Regs.Get(Arg1)._Value.To<tInt32>();
				var B = Regs.Get(Arg2)._Value.To<tInt32>();
				
				Regs.Push(
					OpCode is mVM_Data.tOpCode.IntsDiv ? mVM_Data.Pair(mVM_Data.Int(A / B), mVM_Data.Int(A % B)) : mVM_Data.Int(
						OpCode switch {
							mVM_Data.tOpCode.IntsAdd => A + B,
							mVM_Data.tOpCode.IntsSub => A - B,
							mVM_Data.tOpCode.IntsMul => A * B,
							_ => (A - B).Sign()
						}
					)
				);
				
				break;
			}
			case mVM_Data.tOpCode.NewPair: Regs.Push(mVM_Data.Pair(Regs.Get(Arg1), Regs.Get(Arg2)));
			break;
			case mVM_Data.tOpCode.First:
			case mVM_Data.tOpCode.Second: {
				if (Code.Views[Index] is not null) { Regs.Push(Code.Views[Index]!.Convert(Regs.Get(Arg1), Types, aCallStack._Execution.Views)); break; }
				var Pair = Regs.Get(Arg1)._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
				Regs.Push(OpCode is mVM_Data.tOpCode.First ? Pair.Item1 : Pair.Item2);
				break;
			}
			case mVM_Data.tOpCode.AddPrefix: Regs.Push(mVM_Data.Prefix(Arg1, Regs.Get(Arg2)));
			break;
			case mVM_Data.tOpCode.DelPrefix: Regs.Push(Regs.Get(Arg2)._Value.To<(tNat32, mVM_Data.tData)>().Item2);
			break;
			case mVM_Data.tOpCode.AddField:
			case mVM_Data.tOpCode.SetField: {
				var Prefix = Regs.Get(Arg2)._Value.To<(tNat32, mVM_Data.tData)>();
				var Fields = FirstType.IsEmpty() ? mTreeMap.Tree<tNat32, mVM_Data.tData>((a1, a2) => a1.CompareTo(a2), []) : Regs.Get(Arg1)._Fields;
				Regs.Push(new mVM_Data.tData { _Fields = Fields.Set(Prefix.Item1, Prefix.Item2)});
				break;
			}
			case mVM_Data.tOpCode.GetField: Regs.Push(Regs.Get(Arg1).GetField(Arg2));
			break;
			case mVM_Data.tOpCode.VarDef: Regs.Push(mVM_Data.Var(Regs.Get(Arg1)));
			break;
			case mVM_Data.tOpCode.VarGet: Regs.Push(Regs.Get(Arg1).ReadCell());
			break;
			case mVM_Data.tOpCode.VarSet: Regs.Get(Arg1).WriteCell(Code.Views[Index]!.Convert(Regs.Get(Arg2), Types, aCallStack._Execution.Views));
			break;
			case mVM_Data.tOpCode.WithType: Regs.Push(Code.Views[Index]!.Convert(Regs.Get(Arg2), Types, aCallStack._Execution.Views));
			break;
			case mVM_Data.tOpCode.Narrow: Regs.Set(Arg1, Code.Views[Index]!.Convert(Regs.Get(Arg1), Types, aCallStack._Execution.Views));
			break;
			case mVM_Data.tOpCode.Assert:
			if (Flag(Regs.Get(Arg1), FirstType)) { mAssert.IsTrue(Flag(Regs.Get(Arg2), SecondType)); }
			break;
			case mVM_Data.tOpCode.DefRecProcs: {
				var Function = Regs.Get(Arg1);
				var Environment = Regs.Get(Arg2);
				mVM_Data.tData Closed;
				
				if (Function.IsDef<tPos>(out var Definition)) {
					var Target = Code.Closures[Index]!.Targets.TryGet(Definition.CodeId).AssertNotEmpty();
					Closed = mVM_Data.Closure(Target.Code, Target.Environment.Convert(Environment, Types, aCallStack._Execution.Views).CaptureTypes(Target.Code.TypeCaptures, Types));
				} else if (Function.IsExternDef(out var Native)) { Closed = mVM_Data.NativeClosure(Native, Environment); }
				else { Closed = aCallStack._Execution.Invoke(Code.Calls[Index]!, Function, mVM_Data.Empty(), Environment, Trace, Types).AssertNotEmpty(); }
				
				var Placeholders = mVM_Data.Empty();
				var Shape = ResultType!;
				while (Shape.IsPair(out Shape, out _)) { Placeholders = mVM_Data.Pair(Placeholders, mVM_Data.Empty()); }
				Regs.Push(Placeholders);
				var Value = aCallStack._Execution.Invoke(Code.RecursiveCalls[Index]!, Closed, mVM_Data.Empty(), Placeholders, Trace, Types).AssertNotEmpty();
				var Pending = Placeholders;
				Shape = ResultType!;
				
				while (Shape.IsPair(out Shape, out _)) {
					var Left = Pending._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
					var Right = Value._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
					Copy(Left.Item2, Right.Item2);
					Pending = Left.Item1;
					Value = Right.Item1;
				}
				
				Copy(Pending, Value);
				break;
			}
			case mVM_Data.tOpCode.CallFunc:
			case mVM_Data.tOpCode.CallProc: {
				var Function = Regs.Get(Arg1);
				var Object = mVM_Data.Empty();
				
				if (OpCode is mVM_Data.tOpCode.CallProc) {
					var Pair = Function._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
					Object = Pair.Item1;
					Function = Pair.Item2;
					
					if (FirstType.Refs[1].IsPrefix(out var Name, out var Marker) && Marker.IsEmpty()) {
						(Object, Function) = ResolveInterfaceCall(Object, FirstType.Refs[0], Name.PrefixHash());
					}
				}
				
				var Argument = Regs.Get(Arg2);
				
				if (Function._Value.Is(out mVM_Type.tType Constructor)) {
					Regs.Push(mVM_Data.TypeExpression(Constructor.ApplyType(Argument.TypeValue())));
				} else if (Function.IsDef<tPos>(out var Definition)) {
					var Closed = Code.Closures[Index]!.Targets.TryGet(Definition.CodeId).AssertNotEmpty();
					Regs.Push(mVM_Data.Closure(Closed.Code, Closed.Environment.Convert(Argument, Types, aCallStack._Execution.Views).CaptureTypes(Closed.Code.TypeCaptures, Types)));
				} else if (Function.IsExternDef(out var Native)) {
					Regs.Push(mVM_Data.NativeClosure(Native, Argument));
				} else {
					Regs.Push(aCallStack._Execution.Invoke(Code.Calls[Index]!, Function, Object, Argument, Trace, Types).AssertNotEmpty());
				}
				
				break;
			}
			case mVM_Data.tOpCode.ReturnIf:
			case mVM_Data.tOpCode.ReturnIfNotEmpty: {
				var Value = Regs.Get(OpCode is mVM_Data.tOpCode.ReturnIf ? Arg2 : Arg1);
				var ShouldReturn = OpCode is mVM_Data.tOpCode.ReturnIf ? Flag(Regs.Get(Arg1), FirstType) : !Value.Matches(FirstType.Resolve(Types), mVM_Type.Empty());
				
				if (ShouldReturn) {
					if (Code.Views[Index] is not null) {
						Copy(Regs.Get(mVM_Data.cResReg), Code.Views[Index]!.Convert(Value, Types, aCallStack._Execution.Views));
						aCallStack._Succeeded = true;
					}
					
					if (!ReferenceEquals(Trace, cNoTrace)) { Trace(() => cExitMarker); }
					return aCallStack._Parent;
				}
				
				break;
			}
			case mVM_Data.tOpCode.TryReturn: {
				var Function = Regs.Get(Arg1);
				var FunctionType = FirstType;
				mVM_Data.tData? Guard = null;
				
				if (FirstType.IsPair(out FunctionType, out _)) {
					var Pair = Function._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
					Function = Pair.Item1;
					Guard = Pair.Item2;
				}
				
				var Argument = Regs.Get(Arg2);
				var Site = Code.Calls[Index]!;
				var Match = Code.TryReturnTypes.TryGet(Index).ElseUse(Site.Signature.Refs[1]);
				
				if (Argument.Matches(SecondType.Resolve(Types), Match.Resolve(Types))) {
					var Object = Regs.Get(mVM_Data.cObjReg);
					
					if (Guard is not null && (!aCallStack._Execution.Invoke(Code.Guards[Index]!, Guard, Object, Argument, Trace, Types).IsSome(out var GuardValue) || !Flag(GuardValue, Code.Guards[Index]!.Signature.Refs[2]))) {
						Regs.Push(mVM_Data.Dynamic(mVM_Data.Empty(), mVM_Type.Empty()));
						break;
					}
					
					var Value = aCallStack._Execution.Invoke(Site, Function, Object, Argument, Trace, Types);
					Regs.Push(Value.IsSome(out var Success) ? Code.Views[Index]!.Convert(Success, Types, aCallStack._Execution.Views) : mVM_Data.Dynamic(mVM_Data.Empty(), mVM_Type.Empty()));
				} else { Regs.Push(mVM_Data.Dynamic(mVM_Data.Empty(), mVM_Type.Empty())); }
				
				break;
			}
			case mVM_Data.tOpCode.TryAsEmpty:
			case mVM_Data.tOpCode.TryAsInt:
			case mVM_Data.tOpCode.TryAsBool:
			case mVM_Data.tOpCode.TryAsType:
			case mVM_Data.tOpCode.TryAsPair:
			case mVM_Data.tOpCode.TryAsRecord:
			case mVM_Data.tOpCode.TryAsVar:
			case mVM_Data.tOpCode.TryAsRef:
			case mVM_Data.tOpCode.TryAsSig:
			case mVM_Data.tOpCode.TryRemovePrefixFrom: {
				var Prefix = OpCode is mVM_Data.tOpCode.TryRemovePrefixFrom;
				var Source = Prefix ? SecondType : FirstType;
				var Value = Regs.Get(Prefix ? Arg2 : Arg1);
				var Target = Prefix ? mVM_Type.Prefix(mVM_Data.gHashToPrefix.TryGet(Arg1).AssertNotEmpty(), ResultType!) : ResultType!;
				if (OpCode is mVM_Data.tOpCode.TryAsSig) { Target = Code.Constants[Arg2].TypeExpressionValue(); }
				
				if (!Value.Matches(Source.Resolve(Types), Target.Resolve(Types))) {
					if (!ReferenceEquals(Trace, cNoTrace)) { Trace(() => cExitMarker); }
					return aCallStack._Parent;
				}
				
				var Converted = Code.Views[Index]!.Convert(Value, Types, aCallStack._Execution.Views);
				Regs.Push(Prefix ? Converted._Value.To<(tNat32, mVM_Data.tData)>().Item2 : Converted);
				break;
			}
			case mVM_Data.tOpCode.NewSig:
			case mVM_Data.tOpCode.NewInterface: {
				var Contract = Regs.Get(Arg1).TypeExpressionValue();
				var Pair = Regs.Get(Arg2)._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
				
				if (OpCode is mVM_Data.tOpCode.NewSig) {
					var Bound = Types.WithWitness(ResultType!.Refs[0].ErasedHead(), Pair.Item1.TypeExpressionValue());
					Regs.Push(mVM_Data.Sig(Pair.Item1, Code.Views[Index]!.Convert(Pair.Item2, Bound, aCallStack._Execution.Views)));
				} else {
					Regs.Push(mVM_Data.Interface(Code.ObjectViews[Index]!.Convert(Pair.Item1, Types, aCallStack._Execution.Views), Code.Views[Index]!.Convert(Pair.Item2, Types, aCallStack._Execution.Views)));
				}
				
				break;
			}
			case mVM_Data.tOpCode.SigHead:
			case mVM_Data.tOpCode.SigBody:
			case mVM_Data.tOpCode.TryHasHeadType: {
				var Value = Regs.Get(Arg1);
				var Sig = Value._Value.To<mVM_Data.tSig>();
				
				if (OpCode is mVM_Data.tOpCode.TryHasHeadType) {
					if (!Sig.Head.TypeExpressionValue().SameType(Regs.Get(Arg2).TypeExpressionValue())) { return aCallStack._Parent; }
					Regs.Push(Code.Views[Index]!.Convert(Value, Types, aCallStack._Execution.Views));
				} else if (OpCode is mVM_Data.tOpCode.SigHead) { Regs.Push(Sig.Head); }
				else {
					Regs.Push(Code.Views[Index]!.Convert(Sig.Body, Types, aCallStack._Execution.Views));
				}
				
				break;
			}
			case mVM_Data.tOpCode.LoadConstant: {
				var Constant = Code.Constants[Arg1];
				Regs.Push(Types is not null && Constant._Value.Is(out mVM_Type.tType Expression) ? mVM_Data.TypeExpression(Expression.Resolve(Types)) : Constant);
				break;
			}
			case mVM_Data.tOpCode.TypeSig:
			case mVM_Data.tOpCode.TypeGenericApply:
			case mVM_Data.tOpCode.TypeFunc:
			case mVM_Data.tOpCode.TypeMeth:
			case mVM_Data.tOpCode.TypeRecord:
			case mVM_Data.tOpCode.TypePrefix:
			case mVM_Data.tOpCode.TypeVar: {
				var A = OpCode switch {
					mVM_Data.tOpCode.TypePrefix => mVM_Type.Empty(),
					mVM_Data.tOpCode.TypeSig or mVM_Data.tOpCode.TypeGenericApply => (
						aCallStack._Regs.Get(Arg1).TypeExpressionValue()
					),
					_ => aCallStack._Regs.Get(Arg1).SignatureValue(),
				};
				
				var B = OpCode switch {
					mVM_Data.tOpCode.TypeVar => mVM_Type.Empty(),
					mVM_Data.tOpCode.TypeGenericApply => aCallStack._Regs.Get(Arg2).TypeExpressionValue(),
					_ => aCallStack._Regs.Get(Arg2).SignatureValue(),
				};
				
				aCallStack._Regs.Push(
					mVM_Data.TypeExpression(
						OpCode switch {
							mVM_Data.tOpCode.TypeSig => mVM_Type.Sig(A, B),
							mVM_Data.tOpCode.TypeGenericApply => A.ApplyType(B),
							mVM_Data.tOpCode.TypeFunc => mVM_Type.Proc(mVM_Type.Empty(), A, B),
							mVM_Data.tOpCode.TypeRecord => mVM_Type.Record(A, B),
							mVM_Data.tOpCode.TypePrefix => mVM_Type.Prefix(
								aCallStack._ProcDef.TypePrefixes.Get(Arg1),
								B
							),
							mVM_Data.tOpCode.TypeMeth => mVM_Type.Proc(A, B.Refs[1], B.Refs[2]),
							_ => mVM_Type.Var(A),
						}
					)
				);
				
				break;
			}
			case mVM_Data.tOpCode.TypeAbstract:
			case mVM_Data.tOpCode.TypeFree: {
				var Kind = aCallStack._Regs.Get(Arg1).TypeValue();
				
				aCallStack._Regs.Push(
					mVM_Data.TypeExpression(
						OpCode is mVM_Data.tOpCode.TypeFree
						? mVM_Type.Free("runtime", Kind)
						: mVM_Type.Abstract("runtime", Kind)
					)
				);
				
				break;
			}
			case mVM_Data.tOpCode.TypePair: {
				var Type1 = aCallStack._Regs.Get(Arg1);
				var Type2 = aCallStack._Regs.Get(Arg2);
				var T1 = Type1.SignatureValue();
				var T2 = Type2.SignatureValue();
				
				aCallStack._Regs.Push(
					new mVM_Data.tData {
						_Value = mAny.Any(mVM_Type.Pair(T1, T2))
					}
				);
				
				break;
			}
			case mVM_Data.tOpCode.TypeSet: {
				var Type1 = aCallStack._Regs.Get(Arg1);
				var Type2 = aCallStack._Regs.Get(Arg2);
				var T1 = Type1.SignatureValue();
				var T2 = Type2.SignatureValue();
				
				aCallStack._Regs.Push(
					new mVM_Data.tData {
						_Value = mAny.Any(mVM_Type.Set(T1, T2))
					}
				);
				
				break;
			}
			case mVM_Data.tOpCode.TypeInterface: {
				aCallStack._Regs.Push(
					mVM_Data.TypeExpression(
						mVM_Type.Interface(
							aCallStack._Regs.Get(Arg1).TypeValue(),
							aCallStack._Regs.Get(Arg2).SignatureValue()
						)
					)
				);
				
				break;
			}
			case mVM_Data.tOpCode.TypeRecursive: {
				aCallStack._Regs.Push(
					mVM_Data.TypeExpression(
						mVM_Type.Recursive(
							aCallStack._Regs.Get(Arg1).TypeValue(),
							aCallStack._Regs.Get(Arg2).SignatureValue()
						)
					)
				);
				
				break;
			}
			case mVM_Data.tOpCode.TypeGeneric: {
				aCallStack._Regs.Push(
					mVM_Data.TypeExpression(
						mVM_Type.Generic(
							aCallStack._Regs.Get(Arg1).TypeExpressionValue(),
							aCallStack._Regs.Get(Arg2).TypeExpressionValue()
						)
					)
				);
				
				break;
			}
			default: throw mError.Error("Unsupported VM instruction: " + OpCode);
		}
		
		if (!ReferenceEquals(Trace, cNoTrace) && ResultType is not null) {
			Trace(() => "    = " + (Regs.Get(Regs.Size - 1), ResultType).ToVisibleText());
		}
		
		return aCallStack;
	}
	
	public static mVM_Data.tData
	GetModuleFactory<tPos>(
		mStream.tStream<mVM_Data.tProcDef<tPos>> aDefs
	) {
		var Env = mVM_Data.Empty();
		mAssert.IsTrue(aDefs.Is(out var LastDef, out aDefs));
		
		foreach (var Def in aDefs) {
			Env = mVM_Data.Pair(Env, mVM_Data.Def(LastDef));
			LastDef = Def;
		}
		
		return mVM_Data.Closure(LastDef, Env);
	}
	
	public static (mVM_Data.tData Data, mVM_Type.tType Type)
	OpenSig<tPos>(this (mVM_Data.tData Data, mVM_Type.tType Type) aPackage) {
		mAssert.IsTrue(aPackage.Type.IsSig(out var Head, out var Body));
		var Sig = aPackage.Data._Value.To<mVM_Data.tSig>();
		var SourceHead = Head.ErasedHead();
		return (Sig.Body, Body.Substitute(Head, SourceHead));
	}
	
	public static mVM_Data.tData
	Call<tPos>(
		this mVM_Data.tData aFunction,
		mVM_Data.tData aObject,
		mVM_Data.tData aArgument,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut,
		mVM_Type.tType? aArgumentType = null
	) {
		var Result = mVM_Data.Empty();
		Run(aFunction, aObject, aArgument, Result, aPosToText, aTraceOut, aArgumentType);
		return Result;
	}
	
	public static (mVM_Data.tData Data, mVM_Type.tType Type)
	Call<tPos>(this (mVM_Data.tData Data, mVM_Type.tType Type) aFunction, (mVM_Data.tData Data, mVM_Type.tType Type) aObject, (mVM_Data.tData Data, mVM_Type.tType Type) aArgument, mStd.tFunc<tPos, tText> aPosToText, mStd.tAction<mStd.tFunc<tText>> aTraceOut) {
		var Signature = mVM_Type.InferCall(aFunction.Type, aObject.Type, aArgument.Type, _ => {}).ElseThrow();
		var Result = mVM_Data.Empty();
		Run(aFunction.Data, aObject.Data, aArgument.Data, Result, aPosToText, aTraceOut, aArgument.Type, aFunction.Type, aObject.Type);
		return (Result, Signature.Result);
	}
	
	public static mVM_Data.tData
	CallMethod<tPos>(
		this mVM_Data.tData aCapability,
		tText aName,
		mVM_Data.tData aArgument,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		mAssert.IsTrue(aCapability.IsInterface(out _, out var Receiver, out var Methods));
		var Contract = aCapability._Value.To<mVM_Data.tDynamicValue>().ActualType;
		var Signature = Contract.TryGetInterfaceMethod(aName).AssertNotEmpty();
		mAssert.IsTrue(Signature.Kind is mVM_Type.tKind.Proc, "A native generic method call requires explicit static argument types.");
		var Argument = aArgument.Normalize(Signature.Refs[1]);
		mAssert.IsTrue(Methods.IsRecord(out var Implementations));
		var Method = Implementations.TryGet(aName.PrefixHash()).AssertNotEmpty();
		
		if (Method.IsExternProc(out var Native, out var Environment)) {
			var Function = Native.Signature;
			while (Function.IsGeneric(out _, out var Body)) { Function = Body; }
			var Bound = mVM_Type.InferCall(Native.Signature, Function.Refs[0].HasUnboundParameter() ? Contract : Function.Refs[0], Signature.Refs[1], _ => {}).ElseThrow();
			mAssert.IsTrue(Bound.Result.IsSubType(Signature.Refs[2], aDescribeError: false).Match(out var Mappings, out _) && Mappings.Deep() == 0, "A native implementation must satisfy its visible result contract.");
			var Object = Bound.Object.IsInterface(out _, out _) ? Receiver._Value.To<mVM_Data.tOpaqueValue>().Data : Receiver.Normalize(Bound.Object, "Empfänger", true);
			var Views = new mVM_Data.tConversionMemo();
			Argument = mVM_View.Structural(Argument, Signature.Refs[1], Bound.Argument, Views);
			var NativeResult = Native.Execute(Environment, Object, Argument, aTraceOut).Normalize(Bound.Result, "Ergebnis");
			return mVM_View.Structural(NativeResult, Bound.Result, Signature.Refs[2], Views);
		}
		
		mAssert.IsTrue(Method.IsProc<tPos>(out var Definition, out _));
		var Implementation = Definition.DefType.Refs[2];
		while (Implementation.IsGeneric(out _, out var Body)) { Implementation = Body; }
		Receiver = Receiver.Normalize(Implementation.Refs[0], "Empfänger", true);
		var Result = mVM_Data.Empty();
		Run(Method, Receiver, Argument, Result, aPosToText, aTraceOut, Signature.Refs[1], mVM_Type.Proc(Implementation.Refs[0], Signature.Refs[1], Signature.Refs[2]));
		return Result;
	}
	
	private static void
	Run<tPos>(
		mVM_Data.tData aProc,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut,
		mVM_Type.tType? aArgumentType = null,
		mVM_Type.tType? aSignature = null,
		mVM_Type.tType? aObjectType = null
	) {
		mAssert.IsTrue(aProc.IsProc<tPos>(out var Definition, out var Environment), "A public call requires a declared compiled signature.");
		var Execution = new tExecution<tPos>();
		Execution.Invoke = (aSite, aFunction, aObject, aArgument, aTrace, aTypes) => Execute(Execution, aSite, aFunction, aObject, aArgument, aTrace, aTypes);
		var Signature = aSignature ?? Definition.DefType.Refs[2];
		var Function = Signature;
		while (Function.IsGeneric(out _, out var Body)) { Function = Body; }
		mAssert.IsTrue(aArgumentType is not null || !Function.Refs[1].HasUnboundParameter(), "Native generic calls require explicit static argument types.");
		var ArgumentType = aArgumentType ?? Function.Refs[1];
		var Bound = mVM_Type.InferCall(Signature, aObjectType ?? Function.Refs[0], ArgumentType, _ => {}).ElseThrow();
		var ObjectType = aObjectType ?? Bound.Object;
		var Types = Environment.OpenTypeCaptures(Definition.TypeCaptures, null).Types.WithProof(Definition.SignatureProof);
		var Object = aObj.Normalize(ObjectType.Resolve(Types), "Empfänger");
		var Argument = aArg.Normalize(ArgumentType.Resolve(Types));
		mAssert.IsTrue(Definition.Compilation is mIL_Specialize.tContext<tPos>, "A native entry must be linked before VM execution.");
		var Compiler = (mIL_Specialize.tContext<tPos>)Definition.Compilation!;
		var Site = Compiler.Calls.ToStream().Where(__ => __.SourceSignature.SameType(Signature) && __.ObjectType.SameType(ObjectType) && __.ArgumentType.SameType(ArgumentType) && __.Targets.TryGet(Definition.CodeId).IsSome(out _)).TryFirst().AssertNotEmpty(() => "The static native call signature was not linked before VM execution.");
		var Outcome = Execution.Invoke(Site, aProc, Object, Argument, aTraceOut);
		
		if (Outcome.IsSome(out var Value)) { Copy(aRes, Value); }
		else { mAssert.IsTrue(Bound.Result.IsEmpty(), "The public call failed to return its declared result."); Copy(aRes, mVM_Data.Empty()); }
	}
	
	public static mResult.tResult<(mVM_Data.tData Data, mVM_Type.tType Type), tText>
	TryRun<tPos>(
		mIL_AST.tModule<tPos> aModule,
		(mVM_Data.tData Data, mVM_Type.tType Type) aImport,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		if (!mIL_TypeChecker.AreModuleTypesValid(aModule, out var Error)) {
			return mResult.Fail(mIL_TypeChecker.ErrorText(Error));
		}
		
		return RunValidModule(aModule, aImport, aPosToText, aTrace);
	}
	
	public static mResult.tResult<(mVM_Data.tData Data, mVM_Type.tType Type), tText>
	RunValidModule<tPos>(
		mIL_AST.tModule<tPos> aModule,
		(mVM_Data.tData Data, mVM_Type.tType Type) aImport,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTrace,
		mStream.tStream<(mVM_Type.tType Function, mVM_Type.tType Object, mVM_Type.tType Argument)> aNativeCalls = default
	) {
		if (!mIL_TypeChecker.AreImportTypesValid(aModule, aImport.Type, out var Error)) {
			return mResult.Fail(mIL_TypeChecker.ErrorText(Error));
		}
		
		var Compiled = mIL_GenerateOpcodes.GenerateOpcodes(aModule, aImport, aNativeCalls);
		return RunModule(Compiled.Defs, aImport.Data, aPosToText, aTrace);
	}
	
	public static mResult.tResult<(mVM_Data.tData Data, mVM_Type.tType Type), tText>
	RunModule<tPos>(
		mStream.tStream<mVM_Data.tProcDef<tPos>> aModule,
		mVM_Data.tData aImport,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		var Res = mVM_Data.Empty();
		var Defs = aModule.Reverse().Skip(1).Reverse();
		
		var DefTuple = Defs.Take(2).Count() switch {
			0 => mVM_Data.Empty(),
			1 => mVM_Data.Def(Defs.TryFirst().AssertNotEmpty()),
			_ => Defs.Reduce(
				mVM_Data.Empty(),
				(aTuple, aDef) => mVM_Data.Pair(
					aTuple,
					mVM_Data.Def(aDef)
				)
			),
		};
		
		var InitProc = aModule.TryLast().AssertNotEmpty();
		
		#if MY_TRACE_VM
			var TraceOut = aTrace;
		#else
		var TraceOut = cNoTrace;
		#endif
		
		var Execution = new tExecution<tPos>();
		Execution.Invoke = (aSite, aFunction, aObject, aArgument, aTrace, aTypes) => Execute(Execution, aSite, aFunction, aObject, aArgument, aTrace, aTypes);
		var Outcome = RunCode(Execution, InitProc, DefTuple, mVM_Data.Empty(), InitProc.EntryArgument?.Convert(aImport, null, Execution.Views) ?? aImport, TraceOut, null);
		if (Outcome.IsSome(out var Value)) { Copy(Res, Value); }
		
		mAssert.IsTrue(
			aModule.TryLast(
			).AssertNotEmpty(
			).DefType.IsProc(
				out _,
				out _,
				out var FuncType
			)
		);
		
		mAssert.IsTrue(
			FuncType.IsProc(out _, out _, out var ResType),
			() => $"{mStd.FileLine()}: {FuncType.ToText()}"
		);
		
		if (Outcome.IsNone()) {
			mAssert.IsTrue(mVM_Type.Empty().IsSubType(ResType).Match(out _, out _), "The module failed to return its declared result.");
			if (ResType.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) { Copy(Res, mVM_Data.Dynamic(mVM_Data.Empty(), mVM_Type.Empty())); }
		}
		
		return (Res, ResType);
	}
	
}