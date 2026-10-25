#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mAssert.cs
#:ref Common/mError.cs
#:ref Common/mPerf.cs
#:ref Common/mMaybe.cs
#:ref Common/mRef.cs
#:ref Common/mMath.cs
#:ref Common/mStream.cs
#:ref Common/mAny.cs
#:ref Common/mArrayList.cs
#:ref Common/mTreeMap.cs
#:ref mVM_Data.cs
#:ref mIL_AST.cs
#:ref mIL_GenerateOpcodes.cs

public static class
mVM {
	public sealed class
	tCallStack<tPos> {
		internal mMaybe.tMaybe<tCallStack<tPos>> _Parent;
		internal mArrayList.tArrayList<mVM_Data.tData> _Regs = default!;
		internal mVM_Data.tProcDef<tPos> _ProcDef = default!;
		internal mStream.tStream<mVM_Type.tType> _ReturnTypes;
		internal tNat32 _CodePointer = 0;
		internal mStd.tAction<mStd.tFunc<tText>> _TraceOut = default!;
	}
	
	public static mMaybe.tMaybe<tCallStack<tPos>>
	NewCallStack<tPos>(
		mMaybe.tMaybe<tCallStack<tPos>> aParent,
		mVM_Data.tProcDef<tPos> aProcDef,
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut,
		mStream.tStream<mVM_Type.tType> aReturnTypes = default
	) {
		mAssert.IsTrue(aProcDef.DefType.IsProc(out _, out var EnvType, out var ProcType));
		while (ProcType.IsGeneric(out _, out var Body)) {
			ProcType = Body;
		}
		mAssert.IsTrue(ProcType.IsProc(out var ObjectType, out var ArgumentType, out _));
		aEnv = ViewInterfaces(aEnv, EnvType);
		aObj = ViewInterfaces(aObj, ObjectType);
		aArg = ViewInterfaces(aArg, ArgumentType);
		var Result = new tCallStack<tPos> {
			_TraceOut = aTraceOut,
			_Parent = aParent,
			_ProcDef = aProcDef,
			_ReturnTypes = aReturnTypes,
			_Regs = mArrayList.List(
				mVM_Data.Empty(),
				mVM_Data.Int(1),
				mVM_Data.Bool(false),
				mVM_Data.Bool(true),
				//mVM_Data.Proc(aProcDef, aEnv),
				mVM_Data.TypeEmpty(),
				mVM_Data.TypeInt(),
				mVM_Data.TypeType(),
				aEnv,
				aObj,
				aArg,
				aRes
			),
		};
		aTraceOut(() => "______________________________________");
		aTraceOut(() => " 0 := EMPTY");
		aTraceOut(() => " 1 := 1");
		aTraceOut(() => " 2 := FALSE");
		aTraceOut(() => " 3 := TRUE");
		//aTraceOut(() => " 4 := SELF");
		aTraceOut(() => " 4 := EMPTY_TYPE");
		aTraceOut(() => " 5 := INT_TYPE");
		aTraceOut(() => " 6 := TYPE_TYPE");
		aTraceOut(() => " 7 := ENV  |  " + aEnv.ToText(20));
		aTraceOut(() => " 8 := OBJ  |  " + aObj.ToText(20));
		aTraceOut(() => " 9 := ARG  |  " + aArg.ToText(20));
		aTraceOut(() => "10 := RES");
		
		return Result;
	}
	
	private static mMaybe.tMaybe<mVM_Type.tType>
	TryGetProcArgType(
		mVM_Data.tData aProc
	) {
		if (aProc.IsProcView(out var ViewType, out _)) {
			while (ViewType.IsGeneric(out _, out var Body)) {
				ViewType = Body;
			}
			return ViewType.IsProc(out _, out var Argument, out _) ? Argument : mStd.cEmpty;
		}
		if (
			!aProc.IsProc(out var Def, out _) ||
			!Def.DefType.IsProc(out _, out _, out var FuncType)
		) {
			return mStd.cEmpty;
		}
		
		while (FuncType.IsGeneric(out _, out var BodyType)) {
			FuncType = BodyType;
		}
		
		return (
			FuncType.IsProc(out _, out var ArgType, out _)
			? ArgType
			: mStd.cEmpty
		);
	}
	
	private static tBool
	Matches(
		this mVM_Data.tData aData,
		mVM_Type.tType aType,
		mStream.tStream<(tNat64 Data, tNat64 Type)> aVisited = default
	) {
		if (aData._DataType is mVM_Data.tDataType.Type &&
			aType.Kind is mVM_Type.tKind.Type or mVM_Type.tKind.Proc) {
			return aData.TypeExpressionValue().KindType().SameType(aType);
		}
		if (aType.IsFree() || aType.IsAny()) {
			return true;
		}
		
		if (aVisited.Any(__ => __.Data == aData._DebugId && __.Type == aType.DebugId)) {
			return true;
		}
		if (aData.IsDef(out var Definition) && aType.Kind is mVM_Type.tKind.Proc) {
			return Definition.DefType.IsSubType(aType).Match(out _, out _);
		}
		if (aData.IsProcView(out var ViewType, out _) && aType.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic) {
			return ViewType.IsSubType(aType).Match(out _, out _);
		}
		aVisited = mStream.Stream((aData._DebugId, aType.DebugId), aVisited);
		if (
			aData.IsProc(out var Def, out _) &&
			Def.DefType.IsProc(out _, out _, out var FuncType) &&
			aType.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic
		) {
			while (aType.Kind is mVM_Type.tKind.Proc && FuncType.IsGeneric(out _, out var FuncBody)) {
				FuncType = FuncBody;
			}
			return FuncType.IsSubType(aType).Match(out _, out _);
		}
		if (aType.IsSig(out var Binder, out var SigBody)) {
			return (
				aData.IsSig(out var Contract, out var Head, out var Body) &&
				Contract.IsSubType(aType).Match(out _, out _) &&
				Head.TypeExpressionValue().KindType().SameType(Binder.KindType()) &&
				Body.Matches(SigBody.Substitute(Binder, Head.TypeExpressionValue()), aVisited)
			);
		} else if (aType.IsSet(out var Type1, out var Type2)) {
			return aData.Matches(Type1, aVisited) || aData.Matches(Type2, aVisited);
		} else if (aType.IsRecursive(out var HeadType, out var BodyType)) {
			return aData.Matches(BodyType.Substitute(HeadType, aType), aVisited);
		} else if (aType.IsInterface(out _, out _)) {
			return aData.IsInterface(out var Contract, out _, out _) && Contract.IsSubType(aType).Match(out _, out _);
		} else if (aType.IsGeneric(out _, out BodyType)) {
			return aData.Matches(BodyType, aVisited);
		} else {
			return aType.Kind switch {
				mVM_Type.tKind.Empty => aData.IsEmpty(),
				mVM_Type.tKind.True => aData.IsBool(out var Bool) && Bool,
				mVM_Type.tKind.False => aData.IsBool(out var Bool) && !Bool,
				mVM_Type.tKind.Int => aData.IsInt(out _),

				// An opaque application has no known representation here. The IL checks its
				// binding and arguments; SIG matching substitutes the concrete head first.
				mVM_Type.tKind.TypeApply => true,
				mVM_Type.tKind.Pair => (
					aData.IsPair(out var First, out var Second) &&
					aType.IsPair(out var FirstType, out var SecondType) &&
					First.Matches(FirstType, aVisited) &&
					Second.Matches(SecondType, aVisited)
				),
				mVM_Type.tKind.Prefix => (
					aType.IsPrefix(out var Prefix, out var InnerType) &&
					aData.IsPrefix(Prefix, out var Inner) &&
					Inner.Matches(InnerType, aVisited)
				),
				mVM_Type.tKind.Record => (
					aType.IsRecord(out var FieldTypes) &&
					aData.IsRecord(out var Fields) &&
					FieldTypes.ToStream().All(
						__ => (
							Fields.TryGet(__.Key.PrefixHash()).IsSome(out var Field) &&
							Field.Matches(__.Value, aVisited)
						)
					)
				),
				mVM_Type.tKind.Var => (
					aType.IsVar(out var ValueType) &&
					aData.IsVar(out var Value) &&
					Value.Matches(ValueType, aVisited)
				),
				_ => false,
			};
		}
	}
	
	private static mVM_Data.tData
	ViewInterfaces(
		mVM_Data.tData aData,
		mVM_Type.tType aType,
		mStream.tStream<(tNat64 Data, tNat64 Type)> aVisited = default
	) {
		if (!aType.HasInterfaceValues() || aVisited.Any(__ => __.Data == aData._DebugId && __.Type == aType.DebugId)) {
			return aData;
		}
		aVisited = mStream.Stream((aData._DebugId, aType.DebugId), aVisited);
		if (aType.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic) {
			// Keep a callable's exposed signature and its capability restrictions together.
			return aData.IsProcView(out var ViewType, out _) && ViewType.SameType(aType) ? aData : mVM_Data.ProcView(aType, aData);
		}
		if (aType.IsInterface(out _, out var Body)) {
			mAssert.IsTrue(aData.IsInterface(out var Contract, out var Object, out var Methods));
			mAssert.IsTrue(Contract.IsSubType(aType).Match(out _, out _), "interface view cannot add endpoints");
			if (Contract.SameType(aType)) {
				return aData;
			}
			var Restricted = mVM_Data.Empty();
			if (Body.IsRecord(out var Required)) {
				mAssert.IsTrue(Methods.IsRecord(out var Fields));
				foreach (var Field in Required.ToStream()) {
					Restricted = mVM_Data.Record(Restricted, mVM_Data.Prefix(Field.Key, Fields.TryGet(Field.Key.PrefixHash()).AssertNotEmpty()));
				}
			}
			return mVM_Data.Interface(aType, Object, Restricted);
		}
		if (aType.IsPair(out var FirstType, out var SecondType) && aData.IsPair(out var First, out var Second)) {
			var FirstView = ViewInterfaces(First, FirstType, aVisited);
			var SecondView = ViewInterfaces(Second, SecondType, aVisited);
			return mStd.RefEq(First, FirstView) && mStd.RefEq(Second, SecondView) ? aData : mVM_Data.Pair(FirstView, SecondView);
		}
		if (aType.IsPrefix(out var Prefix, out var InnerType) && aData.IsPrefix(Prefix.PrefixHash(), out var Inner)) {
			var View = ViewInterfaces(Inner, InnerType, aVisited);
			return mStd.RefEq(Inner, View) ? aData : mVM_Data.Prefix(Prefix, View);
		}
		if (aType.IsRecord(out var FieldTypes) && aData.IsRecord(out var Record)) {
			var Fields = Record;
			var Changed = false;
			foreach (var Field in FieldTypes.ToStream()) {
				var Key = Field.Key.PrefixHash();
				var Value = Fields.TryGet(Key).AssertNotEmpty();
				var View = ViewInterfaces(Value, Field.Value, aVisited);
				if (!mStd.RefEq(Value, View)) {
					Fields = Fields.Set(Key, View);
					Changed = true;
				}
			}
			return !Changed ? aData : new mVM_Data.tData {
				_DataType = mVM_Data.tDataType.Record,
				_Fields = Fields,
				_IsMutable = aData._IsMutable
			};
		}
		if (aType.IsSet(out var Type1, out var Type2)) {
			return ViewInterfaces(aData, aData.Matches(Type1) ? Type1 : Type2, aVisited);
		}
		if (aType.IsRecursive(out var Head, out var RecursiveBody)) {
			return ViewInterfaces(aData, RecursiveBody.Substitute(Head, aType), aVisited);
		}
		if (aType.IsSig(out var Binder, out var SigBody) && aData.IsSig(out _, out var Witness, out var ValueBody)) {
			var View = ViewInterfaces(ValueBody, SigBody.Substitute(Binder, Witness.TypeExpressionValue()), aVisited);
			return mStd.RefEq(ValueBody, View) ? aData : mVM_Data.Sig(aType, Witness, View);
		}
		return aData;
	}

	private static mVM_Data.tData
	ReturnValue<tPos>(tCallStack<tPos> aStack, mVM_Data.tData aValue) {
		mAssert.IsTrue(aStack._ProcDef.DefType.IsProc(out _, out _, out var Proc));
		while (Proc.IsGeneric(out _, out var Body)) {
			Proc = Body;
		}
		mAssert.IsTrue(Proc.IsProc(out _, out _, out var Result));
		aValue = ViewInterfaces(aValue, Result);
		foreach (var Type in aStack._ReturnTypes) {
			aValue = ViewInterfaces(aValue, Type);
		}
		return aValue;
	}

	private static mVM_Data.tData
	UnwrapProcViews(
		mVM_Data.tData aProc,
		ref mVM_Data.tData aObject,
		ref mVM_Data.tData aArgument,
		ref mStream.tStream<mVM_Type.tType> aReturnTypes
	) {
		while (aProc.IsProcView(out var Type, out var Inner)) {
			while (Type.IsGeneric(out _, out var Body)) {
				Type = Body;
			}
			mAssert.IsTrue(Type.IsProc(out var Object, out var Argument, out var Result));
			aObject = ViewInterfaces(aObject, Object);
			aArgument = ViewInterfaces(aArgument, Argument);
			aReturnTypes = mStream.Stream(Result, aReturnTypes);
			aProc = Inner;
		}
		return aProc;
	}

	private static (mVM_Data.tData Object, mVM_Data.tData Method)
	ResolveInterfaceCall(mVM_Data.tData aObject, tNat32 aName) {
		if (aObject.IsInterface(out _, out var Hidden, out var Methods)) {
			mAssert.IsTrue(Methods.IsRecord(out var Fields));
			return (Hidden, Fields.TryGet(aName).AssertNotEmpty());
		}
		mAssert.IsTrue(aObject.IsPair(out var Tail, out var Head), "expected interface as first object argument");
		var First = ResolveInterfaceCall(Tail.IsEmpty() ? Head : Tail, aName);
		return (
			Tail.IsEmpty() ? mVM_Data.Pair(Tail, First.Object) : mVM_Data.Pair(First.Object, Head),
			First.Method
		);
	}

	private static mVM_Data.tData
	RunProc<tPos>(
		mVM_Data.tData aProc,
		mVM_Data.tData aObject,
		mVM_Data.tData aArg,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		var ReturnTypes = mStream.Stream<mVM_Type.tType>();
		aProc = UnwrapProcViews(aProc, ref aObject, ref aArg, ref ReturnTypes);
		mVM_Data.tData Result;
		if (aProc.IsProc<tPos>(out var Def, out var Env)) {
			mAssert.IsTrue(Def.DefType.IsProc(out _, out _, out var ProcType));
			while (ProcType.IsGeneric(out _, out var Body)) {
				ProcType = Body;
			}
			mAssert.IsTrue(ProcType.IsProc(out var ObjectType, out _, out _));
			var Res = mVM_Data.Empty();
			Run(
				mVM_Data.Proc(Def, Env),
				ObjectType.IsEmpty() ? mVM_Data.Empty() : aObject,
				aArg,
				Res,
				aPosToText,
				aTraceOut
			);
			Result = Res;
		} else if (aProc.IsExternProc(out var ExternDef, out var ExternEnv)) {
			Result = ExternDef(ExternEnv, aObject, aArg, aTraceOut);
		} else {
			throw mError.Error("expected proc but is: " + aProc._DataType);
		}
		foreach (var Type in ReturnTypes) {
			Result = ViewInterfaces(Result, Type);
		}
		return Result;
	}
	
	public static mMaybe.tMaybe<tCallStack<tPos>>
	Step<tPos>(
		this tCallStack<tPos> aCallStack,
		mStd.tFunc<tPos, tText> aPosToText
	) {
		var (OpCode, Arg1, Arg2, DebugId) = aCallStack._ProcDef.Commands.Get(aCallStack._CodePointer);
		var Pos = aCallStack._ProcDef.PosList.Get(aCallStack._CodePointer);
		var CallType = aCallStack._ProcDef.CallTypes.TryGet(aCallStack._CodePointer);
		var ReturnTypes = CallType.Match(__ => mStream.Stream(__.Refs[2]), () => mStream.Stream<mVM_Type.tType>());
		tText CommandLine() => $">>>   {aCallStack._Regs.Size:#0} := {OpCode} {Arg1} {Arg2} // CommandDebugId:{DebugId} // {Pos}";
		aCallStack._TraceOut(CommandLine);
		aCallStack._TraceOut(() => $"{mStd.NewDebugId()}");
		aCallStack._CodePointer += 1;
		
		switch (OpCode) {
			case mVM_Data.tOpCode.NewInt: {
				aCallStack._Regs.Push(mVM_Data.Int((tInt32)Arg1));
				break;
			}
			case mVM_Data.tOpCode.And: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(BoolData1.IsBool(out var Bool1));
				mAssert.IsTrue(BoolData2.IsBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 && Bool2));
				break;
			}
			case mVM_Data.tOpCode.Or: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(BoolData1.IsBool(out var Bool1));
				mAssert.IsTrue(BoolData2.IsBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 || Bool2));
				break;
			}
			case mVM_Data.tOpCode.XOr: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(BoolData1.IsBool(out var Bool1));
				mAssert.IsTrue(BoolData2.IsBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 ^ Bool2));
				break;
			}
			case mVM_Data.tOpCode.IntsAreEq: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1), () => $"{IntData1.ToText(100)}//DebugId:{IntData1._DebugId}");
				mAssert.IsTrue(IntData2.IsInt(out var Int2), () => $"{IntData2.ToText(100)}//DebugId:{IntData2._DebugId}");
				aCallStack._Regs.Push(mVM_Data.Bool(Int1 == Int2));
				break;
			}
			case mVM_Data.tOpCode.IntsComp: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				var Diff = Int1 - Int2;
				aCallStack._Regs.Push(mVM_Data.Int(Diff.Sign()));
				break;
			}
			case mVM_Data.tOpCode.IntsAdd: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 + Int2));
				break;
			}
			case mVM_Data.tOpCode.IntsSub: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 - Int2));
				break;
			}
			case mVM_Data.tOpCode.IntsMul: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 * Int2));
				break;
			}
			case mVM_Data.tOpCode.IntsDiv: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				aCallStack._Regs.Push(
					mVM_Data.Pair(
						mVM_Data.Int(Int1 / Int2),
						mVM_Data.Int(Int1 % Int2)
					)
				);
				break;
			}
			case mVM_Data.tOpCode.NewPair: {
				aCallStack._Regs.Push(
					mVM_Data.Pair(
						aCallStack._Regs.Get(Arg1),
						aCallStack._Regs.Get(Arg2)
					)
				);
				break;
			}
			case mVM_Data.tOpCode.First: {
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg1).IsPair(
						out var Var1,
						out var Var2
					),
					() => $"{CommandLine()} # expect pair but is {aCallStack._Regs.Get(Arg1).ToText(20)}"
				);
				aCallStack._Regs.Push(Var1);
				break;
			}
			case mVM_Data.tOpCode.Second: {
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg1).IsPair(
						out var Var1,
						out var Var2
					)
				);
				aCallStack._Regs.Push(Var2);
				break;
			}
			case mVM_Data.tOpCode.AddPrefix: {
				aCallStack._Regs.Push(mVM_Data.Prefix(Arg1, aCallStack._Regs.Get(Arg2)));
				break;
			}
			case mVM_Data.tOpCode.DelPrefix: {
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg2).IsPrefix(out var Prefix, out var Data_)
				);
				mAssert.AreEquals(Prefix, Arg1, (a1, a2) => a1 == a2);
				aCallStack._Regs.Push(Data_);
				break;
			}
			case mVM_Data.tOpCode.TryRemovePrefixFrom: {
				var Data = aCallStack._Regs.Get(Arg2);
				if (Data.IsPrefix(out var PrefixId, out var Inner) && PrefixId == Arg1) {
					aCallStack._Regs.Push(Inner);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.AddField: {
				aCallStack._Regs.Push(
					mVM_Data.Record(
						aCallStack._Regs.Get(Arg1),
						aCallStack._Regs.Get(Arg2)
					)
				);
				break;
			}
			case mVM_Data.tOpCode.GetField: {
				var Arg = aCallStack._Regs.Get(Arg1);
				mAssert.IsTrue(Arg.IsRecord(out var Fields));
				aCallStack._Regs.Push(
					Fields.TryGet(Arg2).ElseFail(() => $"{aCallStack._CodePointer}: {OpCode} {Arg2} {Arg.ToText(3)}").ElseThrow()
				);
				break;
			}
			case mVM_Data.tOpCode.Assert: {
				if (aCallStack._Regs.Get(Arg1).IsBool(out var Bool) && Bool) {
					mAssert.IsTrue(aCallStack._Regs.Get(Arg2).IsBool(out Bool) && Bool);
				}
				break;
			}
			case mVM_Data.tOpCode.VarDef: {
				aCallStack._Regs.Push(mVM_Data.Var(aCallStack._Regs.Get(Arg1)));
				break;
			}
			case mVM_Data.tOpCode.VarSet: {
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1)._Value.Is(out mRef.tRefBox<mVM_Data.tData> Cell));
				Cell._Value = aCallStack._Regs.Get(Arg2);
				break;
			}
			case mVM_Data.tOpCode.VarGet: {
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).IsVar(out var X));
				aCallStack._Regs.Push(X);
				break;
			}
			case mVM_Data.tOpCode.DefRecProcs: {
				var Func = aCallStack._Regs.Get(Arg1);
				var Env = aCallStack._Regs.Get(Arg2);
				var Object = mVM_Data.Empty();
				var FactoryReturnTypes = mStream.Stream<mVM_Type.tType>();
				Func = UnwrapProcViews(Func, ref Object, ref Env, ref FactoryReturnTypes);
				
				var RecProcs = mVM_Data.Empty();
				{
					// Build the result's tuple shape before the closures capture its placeholders.
					var RecType = aCallStack._ProcDef.Types.Get(aCallStack._Regs.Size);
					while (RecType.IsGeneric(out _, out var BodyType)) {
						RecType = BodyType;
					}
					while (RecType.IsPair(out RecType, out _)) {
						RecProcs = mVM_Data.Pair(RecProcs, mVM_Data.Empty());
					}
				}
				aCallStack._Regs.Push(RecProcs);
				
				mVM_Data.tData Closed;
				switch (0) {
					case 0 when Func.IsExternDef(out var ExternDef): {
						Closed = mVM_Data.ExternProc(ExternDef, Env);
						break;
					}
					case 0 when Func.IsExternProc(out var ExternDef, out var Env_): {
						Closed = ExternDef(
							Env_,
							mVM_Data.Empty(),
							Env,
							aTraceLine => {
								aCallStack._TraceOut(() => "\t" + aTraceLine());
							}
						);
						break;
					}
					case 0 when Func.IsDef<tPos>(out var Def): {
						Closed = mVM_Data.Proc(Def, Env);
						break;
					}
					case 0 when Func.IsProc<tPos>(out var Def_, out var Env_): {
						throw mError.Error("need Env as Argument");
					}
					default: {
						throw mError.Error("impossible: " + Func._DataType);
					}
				}
				foreach (var Type in FactoryReturnTypes) {
					Closed = ViewInterfaces(Closed, Type);
				}
				var Res = mVM_Data.Empty();
				Run(Closed, mVM_Data.Empty(), RecProcs, Res, aPosToText, aCallStack._TraceOut);
				
				// Preserve the captured objects' identities when closing the recursive references.
				static void
				InitializePlaceholder(
					mVM_Data.tData aPlaceholder,
					mVM_Data.tData aResult
				) {
					aPlaceholder._DataType = aResult._DataType;
					aPlaceholder._Value = aResult._Value;
					aPlaceholder._Fields = aResult._Fields;
					aPlaceholder._IsMutable = aResult._IsMutable;
				}
				if (RecProcs.IsPair(out _, out _)) {
					while (RecProcs.IsPair(out RecProcs, out var Placeholder)) {
						mAssert.IsTrue(Res.IsPair(out Res, out var Result));
						InitializePlaceholder(Placeholder, Result);
					}
					mAssert.IsTrue(RecProcs.IsEmpty() && Res.IsEmpty());
				} else {
					InitializePlaceholder(RecProcs, Res);
				}
				break;
			}
			case mVM_Data.tOpCode.CallFunc: {
				var Proc = aCallStack._Regs.Get(Arg1);
				var Arg = aCallStack._Regs.Get(Arg2);
				if (CallType.IsSome(out var Signature)) {
					Arg = ViewInterfaces(Arg, Signature.Refs[1]);
				}
				var Object = mVM_Data.Empty();
				Proc = UnwrapProcViews(Proc, ref Object, ref Arg, ref ReturnTypes);
				
				switch (0) {
					case 0 when (
						Proc._DataType is mVM_Data.tDataType.Type &&
						Proc.TypeExpressionValue().KindType().IsTypeFunctionKind()
					): {
						aCallStack._Regs.Push(
							mVM_Data.TypeExpression(
								Proc.TypeExpressionValue().ApplyType(Arg.TypeValue())
							)
						);
						break;
					}
					case 0 when Proc.IsExternDef(out var ExternDef): {
						aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
						break;
					}
					case 0 when Proc.IsExternProc(out var ExternDef, out var Env): {
						aCallStack._Regs.Push(
							ExternDef(
								Env,
								mVM_Data.Empty(),
								Arg,
								aTraceLine => {
									aCallStack._TraceOut(() => "\t" + aTraceLine());
								}
							)
						);
						break;
					}
					case 0 when Proc.IsDef<tPos>(out var Def): {
						aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
						break;
					}
					case 0 when Proc.IsProc<tPos>(out var Def_, out var Env): {
						var Res = mVM_Data.Empty();
						aCallStack._Regs.Push(Res);
						return NewCallStack(
							aCallStack,
							Def_,
							Env,
							mVM_Data.Empty(),
							Arg,
							Res,
							aTraceLine => {
								aCallStack._TraceOut(() => "\t" + aTraceLine());
							},
							ReturnTypes
						);
					}
					default: {
						throw mError.Error("expected proc or def but is: " + Proc._DataType);
					}
				}
				break;
			}
			case mVM_Data.tOpCode.CallProc: {
				var Proc_ = aCallStack._Regs.Get(Arg1);
				var Arg = aCallStack._Regs.Get(Arg2);
				
				mAssert.IsTrue(Proc_.IsPair(out var Obj, out var Proc));
				if (CallType.IsSome(out var Signature)) {
					Obj = ViewInterfaces(Obj, Signature.Refs[0]);
					Arg = ViewInterfaces(Arg, Signature.Refs[1]);
				}
				if (Proc.IsPrefix(out var Name, out var Marker) && Marker.IsEmpty()) {
					(Obj, Proc) = ResolveInterfaceCall(Obj, Name);
				}
				Proc = UnwrapProcViews(Proc, ref Obj, ref Arg, ref ReturnTypes);
				
				switch (0) {
					case 0 when Proc.IsExternDef(out var ExternDef): {
						aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
						break;
					}
					case 0 when Proc.IsExternProc(out var ExternDef, out var Env): {
						aCallStack._Regs.Push(
							ExternDef(
								Env,
								Obj,
								Arg,
								aTraceLine => {
									aCallStack._TraceOut(() => "\t" + aTraceLine());
								}
							)
						);
						break;
					}
					case 0 when Proc.IsDef<tPos>(out var Def): {
						aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
						break;
					}
					case 0 when Proc.IsProc<tPos>(out var Def, out var Env): {
						var Res = mVM_Data.Empty();
						aCallStack._Regs.Push(Res);
						return NewCallStack(
							aCallStack,
							Def,
							Env,
							Obj,
							Arg,
							Res,
							aTraceLine => {
								aCallStack._TraceOut(() => "\t" + aTraceLine());
							},
							ReturnTypes
						);
					}
					default: {
						throw mError.Error("impossible: " + Proc._DataType);
					}
				}
				break;
			}
			case mVM_Data.tOpCode.ReturnIf: {
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).IsBool(out var Cond), () => CommandLine());
				if (Cond) {
					var Res = ReturnValue(aCallStack, aCallStack._Regs.Get(Arg2));
					var Des = aCallStack._Regs.Get(mVM_Data.cResReg);
					Des._DataType = Res._DataType;
					Des._Value = Res._Value;
					Des._Fields = Res._Fields;
					Des._IsMutable = Res._IsMutable;
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.ReturnIfNotEmpty: {
				var Res = aCallStack._Regs.Get(Arg1);
				if (Res._DataType is not mVM_Data.tDataType.Empty) {
					Res = ReturnValue(aCallStack, Res);
					var Des = aCallStack._Regs.Get(mVM_Data.cResReg);
					Des._DataType = Res._DataType;
					Des._Value = Res._Value;
					Des._Fields = Res._Fields;
					Des._IsMutable = Res._IsMutable;
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryReturn: {
				var ProcOrPair = aCallStack._Regs.Get(Arg1);
				
				var (Proc, Guard) = (
					ProcOrPair.IsPair(out var Proc_, out var Guard_)
					? (Proc_, Guard_)
					: (ProcOrPair, mMaybe.None<mVM_Data.tData>())
				);
				
				var Arg = aCallStack._Regs.Get(Arg2);
				var Object = aCallStack._Regs.Get(mVM_Data.cObjReg);
				if (
					TryGetProcArgType(Proc).IsSome(out var ArgType) &&
					Arg.Matches(ArgType) &&
					(
						!Guard.IsSome(out var GuardProc) ||
						(
							RunProc(GuardProc, Object, Arg, aPosToText, aCallStack._TraceOut).IsBool(out var GuardResult) &&
							GuardResult
						)
					)
				) {
					aCallStack._Regs.Push(RunProc(Proc, Object, Arg, aPosToText, aCallStack._TraceOut));
				} else {
					aCallStack._Regs.Push(mVM_Data.Empty());
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsPair: {
				var Arg = aCallStack._Regs.Get(Arg1);
				if (Arg.IsPair(out _, out _)) {
					aCallStack._Regs.Push(Arg);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsRecord: {
				var Arg = aCallStack._Regs.Get(Arg1);
				if (Arg.IsRecord(out var _)) {
					aCallStack._Regs.Push(Arg);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsVar: {
				var Arg = aCallStack._Regs.Get(Arg1);
				if (Arg.IsVar(out var _)) {
					aCallStack._Regs.Push(Arg);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.LoadType: {
				var Type = aCallStack._ProcDef.TypeDefinitions.TryGet(Arg1).AssertNotEmpty();
				aCallStack._Regs.Push(mVM_Data.TypeExpression(Type));
				break;
			}
			case mVM_Data.tOpCode.NewInterface: {
				var Contract = aCallStack._Regs.Get(Arg1).TypeValue();
				mAssert.IsTrue(Contract.IsInterface(out _, out _));
				mAssert.IsTrue(aCallStack._Regs.Get(Arg2).IsPair(out var Object, out var Methods));
				aCallStack._Regs.Push(mVM_Data.Interface(Contract, Object, Methods));
				break;
			}
			case mVM_Data.tOpCode.InterfaceView: {
				aCallStack._Regs.Push(ViewInterfaces(aCallStack._Regs.Get(Arg2), aCallStack._Regs.Get(Arg1).TypeExpressionValue()));
				break;
			}
			case mVM_Data.tOpCode.NewSig: {
				var Contract = aCallStack._Regs.Get(Arg1).TypeValue();
				mAssert.IsTrue(aCallStack._Regs.Get(Arg2).IsPair(out var Head, out var Body));
				mAssert.IsTrue(Contract.IsSig(out var Binder, out var BodyType));
				mAssert.IsTrue(Head.TypeExpressionValue().KindType().SameType(Binder.KindType()));
				mAssert.IsTrue(Body.Matches(BodyType.Substitute(Binder, Head.TypeExpressionValue())));
				aCallStack._Regs.Push(mVM_Data.Sig(Contract, Head, Body));
				break;
			}
			case mVM_Data.tOpCode.TryAsSig: {
				var Arg = aCallStack._Regs.Get(Arg1);
				var Contract = aCallStack._ProcDef.TypeDefinitions.TryGet(Arg2).AssertNotEmpty();
				if (!Arg.Matches(Contract)) {
					return aCallStack._Parent;
				}
				aCallStack._Regs.Push(Arg);
				break;
			}
			case mVM_Data.tOpCode.TryHasHeadType: {
				var Arg = aCallStack._Regs.Get(Arg1);
				mAssert.IsTrue(Arg.IsSig(out _, out var Head, out _));
				var Expected = aCallStack._Regs.Get(Arg2).TypeExpressionValue();
				if (!Head.TypeExpressionValue().SameType(Expected)) {
					return aCallStack._Parent;
				}
				aCallStack._Regs.Push(Arg);
				break;
			}
			case mVM_Data.tOpCode.SigHead:
			case mVM_Data.tOpCode.SigBody: {
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).IsSig(out _, out var Head, out var Body));
				aCallStack._Regs.Push(OpCode is mVM_Data.tOpCode.SigHead ? Head : Body);
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
						_DataType = mVM_Data.tDataType.Type,
						_IsMutable = false,
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
						_DataType = mVM_Data.tDataType.Type,
						_IsMutable = false,
						_Value = mAny.Any(mVM_Type.Set(T1, T2))
					}
				);
				break;
			}
			case mVM_Data.tOpCode.TypeInterface:
			case mVM_Data.tOpCode.TypeRecursive: {
				var Parameter = aCallStack._Regs.Get(Arg1).TypeValue();
				var Body = aCallStack._Regs.Get(Arg2).SignatureValue();
				aCallStack._Regs.Push(mVM_Data.TypeExpression(
					OpCode is mVM_Data.tOpCode.TypeInterface ? mVM_Type.Interface(Parameter, Body) : mVM_Type.Recursive(Parameter, Body)
				));
				break;
			}
			case mVM_Data.tOpCode.TryAsEmpty: {
				var Data = aCallStack._Regs.Get(Arg1);
				if (Data.IsEmpty()) {
					aCallStack._Regs.Push(Data);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsBool: {
				var Data = aCallStack._Regs.Get(Arg1);
				if (Data.IsBool(out _)) {
					aCallStack._Regs.Push(Data);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsInt: {
				var Data = aCallStack._Regs.Get(Arg1);
				if (Data.IsInt(out _)) {
					aCallStack._Regs.Push(Data);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
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
			// TODO: missing IL Command
			// - Create Process
			// - Send Message
			
			default: {
				throw mError.Error("TODO " + OpCode);
			}
		}
		var ResultIndex = aCallStack._Regs.Size - 1;
		foreach (var Type in ReturnTypes) {
			aCallStack._Regs.Set(ResultIndex, ViewInterfaces(aCallStack._Regs.Get(ResultIndex), Type));
		}
		aCallStack._TraceOut(
			() => $@"    \ {aCallStack._Regs.Size - 1} = {aCallStack._Regs.Get(aCallStack._Regs.Size - 1).ToText(20)}"
		);
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
		
		return mVM_Data.Proc(LastDef, Env);
	}
	
	public static void
	Run<tPos>(
		mVM_Data.tData aProc,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		using var _ = mPerf.Measure();
		var ReturnTypes = mStream.Stream<mVM_Type.tType>();
		aProc = UnwrapProcViews(aProc, ref aObj, ref aArg, ref ReturnTypes);
		switch (0) {
			case 0 when aProc.IsProc<tPos>(out var Def, out var Env): {
				var CallStack = NewCallStack(mStd.cEmpty, Def, Env, aObj, aArg, aRes, aTraceOut, ReturnTypes);
				while (CallStack.IsSome(out var CallStack_)) {
					CallStack = CallStack_.Step(aPosToText);
				}
				break;
			}
			case 0 when aProc.IsExternProc(out var ExternDef, out var Env): {
				var Res = ExternDef(Env, aObj, aArg, aTraceOut);
				foreach (var Type in ReturnTypes) {
					Res = ViewInterfaces(Res, Type);
				}
				aRes._DataType = Res._DataType;
				aRes._Value = Res._Value;
				aRes._Fields = Res._Fields;
				aRes._IsMutable = Res._IsMutable;
				break;
			}
			default: {
				throw mError.Error($"tPos ({typeof(tPos)}) does not match with the tPos of aProc");
			}
		}
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
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		if (!mIL_TypeChecker.AreImportTypesValid(aModule, aImport.Type, out var Error)) {
			return mResult.Fail(mIL_TypeChecker.ErrorText(Error));
		}
		if (aImport.Data._IsMutable) {
			return mResult.Fail("mutable module import is not allowed");
		}
		var Compiled = mIL_GenerateOpcodes.GenerateOpcodes(aModule, aImport);
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
			var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => { });
		#endif
		
		Run(
			mVM_Data.Proc(InitProc, DefTuple),
			mVM_Data.Empty(),
			aImport,
			Res,
			aPosToText,
			TraceOut
		);
		
		mAssert.IsFalse(
			Res._IsMutable,
			"mutable module export is not allowed"
		);
		
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
		
		return (Res, ResType);
	}
	
}
