#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mAny.cs
#:ref Common/mArrayList.cs
#:ref Common/mAssert.cs
#:ref Common/mMaybe.cs
#:ref Common/mRef.cs
#:ref Common/mTreeMap.cs
#:ref Common/mStream.cs
#:ref mVM_Type.cs

public static class
mVM_Data {
	public enum
	tOpCode {
		// BOOL
		And,
		Or,
		XOr,
		
		// INT
		NewInt,
		IntsAreEq,
		IntsComp,
		IntsAdd,
		IntsSub,
		IntsMul,
		IntsDiv,
		
		// PAIR
		NewPair,
		First,
		Second,
		
		// SIG
		NewSig,
		NewInterface,
		WithType,
		Narrow,
		SigHead,
		SigBody,
		
		// PREFIX
		AddPrefix,
		DelPrefix,
		
		// RECORD
		AddField,
		SetField,
		GetField,
		
		// VAR
		VarDef,
		VarSet,
		VarGet,
		
		// JUMP
		CallFunc,
		CallProc,
		DefRecProcs,
		ReturnIf,
		ReturnIfNotEmpty,
		TryReturn,
		TryAsEmpty,
		TryAsBool,
		TryAsInt,
		TryAsType,
		TryAsPair,
		TryAsSig,
		TryHasHeadType,
		TryAsVar,
		TryAsRef,
		TryAsRecord,
		TryRemovePrefixFrom,
		
		// ASSERT
		Assert,
		
		// CONSTANTS
		LoadConstant,
		
		// TYPE
		TypeEmpty,
		TypeAny,
		TypeInt,
		TypeFree,
		TypeAbstract,
		TypePair,
		TypeSig,
		TypePrefix,
		TypeRecord,
		TypeVar,
		TypeSet,
		TypeCond,
		TypeFunc,
		TypeMeth,
		TypeRecursive,
		TypeInterface,
		TypeGeneric,
		TypeGenericApply,
	}
	
	public interface
	iProcDef {
		tText FirstPosText { get; }
		mVM_Type.tType DefType { get; }
		mVM_Type.tType[] TypeCaptures { get; }
		mVM_Type.tWitnessMap? SignatureProof { get; }
	}
	
	public sealed class tNativeCode {
		public required mVM_Type.tType Signature { get; init; }
		public required mStd.tFunc<tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>, tData> Execute { get; init; }
		// Representative results supply code origins to the static linker. Their
		// ordinary payload values do not determine types or execute during linking.
		public tData[] ResultOrigins { get; init; } = [];
	}
	
	public delegate mMaybe.tMaybe<tData>
	tExecute<tPos>(tCallSite<tPos> aSite, tData aFunction, tData aObject, tData aArgument, mStd.tAction<mStd.tFunc<tText>> aTrace, mVM_Type.tTypeResolver? aTypes = null);
	
	public sealed class
	tPreparedCall<tPos> {
		public required tProcDef<tPos> Code;
		public required tConversion Environment;
		public required tConversion Object;
		public required tConversion Argument;
		public required tConversion Result;
	}
	
	public sealed class
	tCallSite<tPos> {
		public required mVM_Type.tType SourceSignature;
		public required mVM_Type.tType Signature;
		public required mVM_Type.tType ArgumentType;
		public required mVM_Type.tType ObjectType;
		public mVM_Type.tType? PhysicalObjectType;
		public tConversion? NativeArgument;
		public tConversion? NativeObject;
		public readonly System.Collections.Generic.Dictionary<tNativeCode, (tConversion Object, tConversion Argument, tConversion Result)> NativeTargets = [];
		public tCloseSite<tPos>? Closure;
		public mTreeMap.tTree<tNat64, tPreparedCall<tPos>> Targets = mTreeMap.Tree<tNat64, tPreparedCall<tPos>>((a1, a2) => a1.CompareTo(a2), []);
	}
	
	public sealed class
	tPreparedClose<tPos> {
		public required tProcDef<tPos> Code;
		public required tConversion Environment;
	}
	
	public sealed class
	tCloseSite<tPos> {
		public required mVM_Type.tType Signature;
		public required mVM_Type.tType Environment;
		public mTreeMap.tTree<tNat64, tPreparedClose<tPos>> Targets = mTreeMap.Tree<tNat64, tPreparedClose<tPos>>((a1, a2) => a1.CompareTo(a2), []);
	}
	
	public sealed class
	tConversionMemo {
		public readonly System.Collections.Generic.Dictionary<(tConversion Plan, tData Data), tData> Values = [];
		public System.Collections.Generic.Dictionary<(tData Data, mVM_Type.tType Source, mVM_Type.tType Target), tData>? Structural;
	}
	
	public sealed class
	tConversion {
		public required mVM_Type.tType Source;
		public required mVM_Type.tType Target;
		public mStd.tFunc<tData, mVM_Type.tTypeResolver?, tConversionMemo, tData>? Apply;
		public tBool Memoize;
		public tBool Dynamic;
		public tBool RevealOpaque;
		public mVM_Type.tWitnessMap? Proof;
		public iProcDef? Adapter;
		public tConversion? Inner;
		public tConversion? After;
		public tConversion? Before;
		public tInt32 Projection = -1;
		public readonly mArrayList.tArrayList<(mVM_Type.tType Type, tConversion Plan)> Alternatives = mArrayList.List<(mVM_Type.tType, tConversion)>();
		public ConditionalWeakTable<mVM_Type.tType, System.Tuple<tNat32, (mVM_Type.tType Type, tConversion Plan)[]>>? CandidateSchemas;
	}
	
	// standard stack indexes
	public static readonly tNat32 cEmptyReg = 0;
	public static readonly tNat32 cOneReg = 1;
	public static readonly tNat32 cFalseReg = 2;
	public static readonly tNat32 cTrueReg = 3;
	//public static readonly tNat32 cSelfFuncReg = 4;
	public static readonly tNat32 cEmptyTypeReg = 4;
	public static readonly tNat32 cIntTypeReg = 5;
	public static readonly tNat32 cTypeTypeReg = 6;
	public static readonly tNat32 cEnvReg = 7;
	public static readonly tNat32 cObjReg = 8;
	public static readonly tNat32 cArgReg = 9;
	public static readonly tNat32 cResReg = 10;
	
	[DebuggerDisplay("{this.DefType.ToText()}")]
	public sealed class
	tProcDef<tPos> : iProcDef {
		public readonly tNat64 CodeId = mStd.NewDebugId();
		public tBool IsTemplate;
		public tUnknown? Compilation;
		public tCallSite<tPos>?[] Calls = [];
		public tCallSite<tPos>?[] Guards = [];
		public tCallSite<tPos>?[] RecursiveCalls = [];
		public tCloseSite<tPos>?[] Closures = [];
		public tConversion?[] Views = [];
		public tConversion?[] ObjectViews = [];
		public tConversion? EntryArgument;
		public readonly mArrayList.tArrayList<(mVM_Type.tType Symbol, tNat32 Register)> TypeBindings = mArrayList.List<(mVM_Type.tType, tNat32)>();
		// Static symbols whose explicit TYPE witnesses are stored in the environment.
		public mVM_Type.tType[] TypeCaptures { get; init; } = [];
		public mVM_Type.tWitnessMap? SignatureProof { get; init; }
		public mStd.tFunc<mVM_Type.tType, mVM_Type.tType, tBool, mVM_Type.tWitnessMap?, tProcDef<tPos>>? AdapterFactory;
		
		public readonly mArrayList.tArrayList<(mVM_Type.tType First, mVM_Type.tType Second, mVM_Type.tType? Result)>
		OperandTypes = mArrayList.List<(mVM_Type.tType, mVM_Type.tType, mVM_Type.tType?)>();
		
		public mArrayList.tArrayList<tConversion>
		Conversions = mArrayList.List<tConversion>();
		
		public System.Collections.Generic.Dictionary<(tInt32 Source, tInt32 Target, tInt32 Projection, tBool Reveal), System.Collections.Generic.List<tConversion>> ConversionIndex = [];
		
		public mStd.tAction<mVM_Type.tType>? RegisterActual;
		public mStd.tAction<mVM_Type.tType>? RegisterWitness;
		
		public System.Func<mVM_Type.tTypeResolver?, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>, tExecute<tPos>, tConversionMemo, mMaybe.tMaybe<tData>>?
		Entry;
		
		public readonly mArrayList.tArrayList<(tOpCode, tNat32, tNat32, tNat64)>
		Commands = mArrayList.List<(tOpCode, tNat32, tNat32, tNat64)>();
		
		public readonly mArrayList.tArrayList<tPos>
		PosList = mArrayList.List<tPos>();
		
		public mVM_Type.tType
		DefType { get; }
		
		public readonly mArrayList.tArrayList<mVM_Type.tType>
		Types = mArrayList.List<mVM_Type.tType>();
		
		public mTreeMap.tTree<tNat32, mVM_Type.tType>
		TryReturnTypes = mTreeMap.Tree<tNat32, mVM_Type.tType>((a1, a2) => a1.CompareTo(a2), []);
		
		public mTreeMap.tTree<tNat32, mVM_Type.tType> SigHeads = mTreeMap.Tree<tNat32, mVM_Type.tType>((a1, a2) => a1.CompareTo(a2), []);
		
		public tData[]
		Constants = [];
		
		public readonly mArrayList.tArrayList<tText>
		TypePrefixes = mArrayList.List<tText>();
		
		public tNat32
		_LastReg = cResReg;
		
		public tText
		FirstPosText => "" + this.PosList.ToStream().TryFirst().AssertNotEmpty();
		
		public tProcDef(
			mVM_Type.tType aDefType
		) {
			mAssert.IsTrue(aDefType.IsProc(out var Empty, out var Env, out var Proc));
			mAssert.AreEquals(Empty.Kind, mVM_Type.tKind.Empty);
			
			while (Proc.IsGeneric(out _, out var Body)) {
				Proc = Body;
			}
			
			if (Proc.Kind is not mVM_Type.tKind.Proc) {
				mAssert.Fail($"expected proc type but is:\n{Proc.ToText()}");
			}
			
			this.DefType = aDefType;
		}
	}
	
	internal static void
	_AddCommand<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tNat32 aReg1
	) => aDef._AddCommand(aPos, aCommand, aReg1, 0);
	
	internal static void
	_AddCommand<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tNat32 aReg1,
		tNat32 aReg2
	) { 
		if (aDef.Types.IsEmpty) {
			mAssert.IsTrue(aDef.DefType.IsProc(out _, out var Environment, out var Function));
			while (Function.IsGeneric(out _, out var Body)) { Function = Body; }
			mAssert.IsTrue(Function.IsProc(out var Object, out var Argument, out var Result));
			
			aDef.Types.Push(
				mVM_Type.Empty(),
				mVM_Type.Int(),
				mVM_Type.False(),
				mVM_Type.True(),
				mVM_Type.Type(),
				mVM_Type.Type(),
				mVM_Type.Type(),
				Environment,
				Object,
				Argument,
				Result
			);
		}
		
		aDef.PosList.Push(aPos);
		aDef.Commands.Push((aCommand, aReg1, aReg2, mStd.NewDebugId()));
		
		aDef.OperandTypes.Push(
			(
				aReg1 < aDef.Types.Size ? aDef.Types.Get(aReg1) : mVM_Type.Empty(),
				aReg2 < aDef.Types.Size ? aDef.Types.Get(aReg2) : mVM_Type.Empty(),
				null
			)
		);
	}
	
	internal static tNat32
	_AddReg<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand
	) => aDef._AddReg(aPos, aCommand, 0, 0);
	
	internal static tNat32
	_AddReg<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tNat32 aReg1
	) => aDef._AddReg(aPos, aCommand, aReg1, 0);
	
	internal static tNat32
	_AddReg<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tNat32 aReg1,
		tNat32 aReg2
	) {
		aDef._AddCommand(aPos, aCommand, aReg1, aReg2);
		aDef._LastReg += 1;
		var (First, Second, _) = aDef.OperandTypes.Get(aDef.Commands.Size - 1);
		
		var Result = aCommand switch {
			tOpCode.NewInt or tOpCode.IntsComp or tOpCode.IntsAdd or tOpCode.IntsSub or tOpCode.IntsMul => mVM_Type.Int(),
			tOpCode.And or tOpCode.Or or tOpCode.XOr or tOpCode.IntsAreEq => mVM_Type.Bool(),
			tOpCode.IntsDiv => mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Int()),
			tOpCode.NewPair => mVM_Type.Pair(First, Second),
			tOpCode.First => First.TryProjectPair(out var Left, out _) ? Left : mVM_Type.Any(),
			tOpCode.Second => First.TryProjectPair(out _, out var Right) ? Right : mVM_Type.Any(),
			tOpCode.AddPrefix => mVM_Type.Prefix(gHashToPrefix.TryGet(aReg1).AssertNotEmpty(), Second),
			tOpCode.DelPrefix => Second.Refs[0],
			tOpCode.AddField => mVM_Type.Record(First, Second),
			tOpCode.GetField => First.GetFieldType(gHashToPrefix.TryGet(aReg2).AssertNotEmpty()),
			tOpCode.VarDef => mVM_Type.Var(First),
			tOpCode.VarGet => First.Refs[0],
			tOpCode.CallFunc => mVM_Type.Infer(First, mVM_Type.Empty(), Second, _ => {}).ElseThrow(),
			tOpCode.CallProc => mVM_Type.Infer(First.Refs[1], First.Refs[0], Second, _ => {}).ElseThrow(),
			tOpCode.DefRecProcs => First.Refs[2].Refs[2],
			tOpCode.LoadConstant => aDef.Constants[aReg1].TypeExpressionValue().KindType(),
			tOpCode.TryAsEmpty => mVM_Type.Empty(),
			tOpCode.TryAsInt => mVM_Type.Int(),
			tOpCode.TryAsBool => mVM_Type.Bool(),
			tOpCode.TryAsPair => First.SubSet(__ => __.IsPair(out _, out _) ? __ : mStd.cEmpty).ElseUse(mVM_Type.Pair(mVM_Type.Any(), mVM_Type.Any())),
			tOpCode.TryAsVar => First.SubSet(__ => __.IsVar(out _) ? __ : mStd.cEmpty).ElseUse(mVM_Type.Var(mVM_Type.Any())),
			tOpCode.TryAsRecord => First,
			tOpCode.TryAsSig => aDef.Constants[aReg2].TypeExpressionValue(),
			tOpCode.TryHasHeadType => First,
			tOpCode.SigHead => mVM_Type.Type(),
			tOpCode.SigBody => First.Refs[1],
			_ => mVM_Type.Type(),
		};
		
		aDef.Types.Push(Result);
		aDef.OperandTypes.Set(aDef.Commands.Size - 1, (First, Second, Result));
		return aDef._LastReg;
	}
	
	public static tNat32
	And<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aBoolReg1,
		tNat32 aBoolReg2
	) => aDef._AddReg(aPos, tOpCode.And, aBoolReg1, aBoolReg2);
	
	public static tNat32
	Or<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aBoolReg1,
		tNat32 aBoolReg2
	) => aDef._AddReg(aPos, tOpCode.Or, aBoolReg1, aBoolReg2);
	
	public static tNat32
	XOr<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aBoolReg1,
		tNat32 aBoolReg2
	) => aDef._AddReg(aPos, tOpCode.XOr, aBoolReg1, aBoolReg2);
	
	public static tNat32
	Int<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aIntValue
	) => aDef._AddReg(aPos, tOpCode.NewInt, (tNat32)aIntValue);
	
	public static tNat32
	IntsAreEq<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsAreEq, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsComp<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsComp, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsAdd<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsAdd, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsSub<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsSub, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsMul<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsMul, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsDiv<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsDiv, aIntReg1, aIntReg2);
	
	public static tNat32
	Pair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aDataReg1,
		tNat32 aDataReg2
	) => aDef._AddReg(aPos, tOpCode.NewPair, aDataReg1, aDataReg2);
	
	public static tNat32
	First<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPairReg
	) => aDef._AddReg(aPos, tOpCode.First, aPairReg);
	
	public static tNat32
	Second<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPairReg
	) => aDef._AddReg(aPos, tOpCode.Second, aPairReg);

	public static tNat32
	Sig<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aContractReg,
		tNat32 aPayloadReg
	) => aDef._AddReg(aPos, tOpCode.NewSig, aContractReg, aPayloadReg);

	public static tNat32
	SigHead<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aSigReg
	) => aDef._AddReg(aPos, tOpCode.SigHead, aSigReg);

	public static tNat32
	SigBody<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aSigReg
	) => aDef._AddReg(aPos, tOpCode.SigBody, aSigReg);
	
	public static tNat32
	AddPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPrefixId,
		tNat32 aDataReg
	) => aDef._AddReg(aPos, tOpCode.AddPrefix, aPrefixId, aDataReg);
	
	public static tNat32
	DelPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPrefixId,
		tNat32 aReg
	) => aDef._AddReg(aPos, tOpCode.DelPrefix, aPrefixId, aReg);
	
	public static tNat32
	ExtendRec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecReg,
		tNat32 aPrefixReg
	) => aDef._AddReg(aPos, tOpCode.AddField, aRecReg, aPrefixReg);
	
	public static tNat32
	GetField<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecReg,
		tNat32 aFieldId
	) => aDef._AddReg(aPos, tOpCode.GetField, aRecReg, aFieldId);
	
	public static tNat32
	AddField<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecReg
	) => aDef._AddReg(aPos, tOpCode.AddField, aRecReg);
	
	public static tNat32
	VarDef<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aValueReg
	) => aDef._AddReg(aPos, tOpCode.VarDef, aValueReg);
	
	public static void
	VarSet<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aVarReg,
		tNat32 aValueReg
	) {
		aDef._AddCommand(aPos, tOpCode.VarSet, aVarReg, aValueReg);
	}
	
	public static tNat32
	VarGet<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aVarReg
	) => aDef._AddReg(aPos, tOpCode.VarGet, aVarReg);
	
	public static tNat32
	DefRecProcs<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aFuncReg,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.DefRecProcs, aFuncReg, aArgReg);
	
	public static tNat32
	Call<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aProcReg,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.CallFunc, aProcReg, aArgReg);
	
	public static tNat32
	Exec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aProcReg,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.CallProc, aProcReg, aArgReg);
	
	public static void
	ReturnIf<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aCondReg,
		tNat32 aResReg
	) {
		aDef._AddCommand(aPos, tOpCode.ReturnIf, aCondReg, aResReg);
	}
	
	public static void
	ReturnIfNotEmpty<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aResReg
	) {
		aDef._AddCommand(aPos, tOpCode.ReturnIfNotEmpty, aResReg);
	}
	
	public static tNat32
	TryReturn<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aProcOrProcGuardPairReg,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryReturn, aProcOrProcGuardPairReg, aArgReg);
	
	public static tNat32
	TryAsEmpty<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsEmpty, aArgReg);
	
	public static tNat32
	TryAsBool<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsBool, aArgReg);
	
	public static tNat32
	TryAsInt<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsInt, aArgReg);
	
	public static tNat32
	TryAsType<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsType, aArgReg);
	
	public static tNat32
	TryRemovePrefixFrom<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPrefixId,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryRemovePrefixFrom, aPrefixId, aArgReg);
	
	public static tNat32
	TryAsRecord<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsRecord, aArgReg);
	
	public static tNat32
	TryAsPair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsPair, aArgReg);
	
	public static tNat32
	TryAsSig<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg,
		tNat32 aContractTypeIndex
	) => aDef._AddReg(aPos, tOpCode.TryAsSig, aArgReg, aContractTypeIndex);
	
	public static tNat32
	TryHasHeadType<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aSigReg,
		tNat32 aHeadReg
	) => aDef._AddReg(aPos, tOpCode.TryHasHeadType, aSigReg, aHeadReg);
	
	public static tNat32
	TryAsVar<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsVar, aArgReg);
	
	public static tNat32
	TryAsRef<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsRef, aArgReg);
	
	public static void
	Assert<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPreCondReg,
		tNat32 aPostCondReg
	) {
		aDef._AddCommand(aPos, tOpCode.Assert, aPreCondReg, aPostCondReg);
	}
	
	public static tNat32
	LoadConstant<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aConstantIndex
	) => aDef._AddReg(aPos, tOpCode.LoadConstant, aConstantIndex);
	
	public static tNat32
	TypeEmpty<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeEmpty);
	
	public static tNat32
	TypeAny<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeAny);
	
	public static tNat32
	TypeInt<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeInt);
	
	public static tNat32
	TypePair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aTypeReg1,
		tNat32 aTypeReg2
	) => aDef._AddReg(aPos, tOpCode.TypePair, aTypeReg1, aTypeReg2);
	
	public static tNat32
	TypeSig<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aBinderKindReg,
		tNat32 aBodyTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeSig, aBinderKindReg, aBodyTypeReg);
	
	public static tNat32
	TypePrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tText aPrefix,
		tNat32 aTypeReg
	) {
		var Index = aDef.TypePrefixes.Size;
		aDef.TypePrefixes.Push(aPrefix);
		return aDef._AddReg(aPos, tOpCode.TypePrefix, Index, aTypeReg);
	}
	
	public static tNat32
	TypeRecord<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecordTypeReg,
		tNat32 aPrefixTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeRecord, aRecordTypeReg, aPrefixTypeReg);
	
	public static tNat32
	TypeVar<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeVar, aTypeReg);
	
	public static tNat32
	TypeSet<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aTypeReg1,
		tNat32 aTypeReg2
	) => aDef._AddReg(aPos, tOpCode.TypeSet, aTypeReg1, aTypeReg2);
	
	public static tNat32
	TypeFunc<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgTypeReg,
		tNat32 aResTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeFunc, aArgTypeReg, aResTypeReg);
	
	public static tNat32
	TypeMeth<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aObjTypeReg,
		tNat32 aFuncTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeMeth, aObjTypeReg, aFuncTypeReg);
	
	public static tNat32
	TypeFree<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aKindReg
	) => aDef._AddReg(aPos, tOpCode.TypeFree, aKindReg);
	
	public static tNat32
	TypeAbstract<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aKindReg
	) => aDef._AddReg(aPos, tOpCode.TypeAbstract, aKindReg);
	
	public static tNat32
	TypeRecursive<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aHeadTypeReg,
		tNat32 aBodyTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeRecursive, aHeadTypeReg, aBodyTypeReg);
	
	public static tNat32
	TypeInterface<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aHeadTypeReg,
		tNat32 aBodyTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeInterface, aHeadTypeReg, aBodyTypeReg);
	
	public static tNat32
	TypeGeneric<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aHeadTypeReg,
		tNat32 aBodyTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeGeneric, aHeadTypeReg, aBodyTypeReg);
	
	public static tNat32
	TypeGenericApply<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aGenericTypeReg,
		tNat32 aArgTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeGenericApply, aGenericTypeReg, aArgTypeReg);
	
	// TODO: Pattern Types
	
	public sealed class
	tDynamicValue(mVM_Type.tType aActualType, tData aData) {
		public readonly mVM_Type.tType ActualType = aActualType;
		public readonly tData Data = aData;
	}
	
	public sealed class
	tOpaqueValue(tData aData) {
		public readonly tData Data = aData;
	}
	
	public sealed class
	tCellView(tData aCell, mStd.tFunc<tData, tData> aRead, mStd.tFunc<tData, tData>? aWrite) {
		public readonly tData Cell = aCell;
		public readonly mStd.tFunc<tData, tData> Read = aRead;
		public readonly mStd.tFunc<tData, tData>? Write = aWrite;
	}
	
	public static tData
	ReadCell(this tData aCell) => aCell._Value.Is(out tCellView View) ? View.Read(View.Cell.ReadCell()) : aCell._Value.To<mRef.tRefBox<tData>>()._Value;
	
	public static void
	WriteCell(this tData aCell, tData aValue) {
		if (aCell._Value.Is(out tCellView View)) { View.Cell.WriteCell(View.Write!(aValue)); }
		else { aCell._Value.To<mRef.tRefBox<tData>>()._Value = aValue; }
	}
	
	public static tData
	Dynamic(
		tData aData,
		mVM_Type.tType aActualType
	) => new() {_Value = mAny.Any(new tDynamicValue(aActualType, aData))};
	
	public static tData
	JoinDynamic(tData aLeft, tData aRight) {
		var Left = aLeft._Value.To<tDynamicValue>();
		var Right = aRight._Value.To<tDynamicValue>();
		var Views = mArrayList.List<(tData Data, mVM_Type.tType Type)>();
		
		foreach (var View in mStream.Concat(Left.Alternatives(), Right.Alternatives())) {
			if (!Views.ToStream().Any(__ => __.Type.SameType(View.Type))) { Views.Push(View); }
		}
		
		var Result = Dynamic(Views.Get(0).Data, Views.Get(0).Type);
		
		foreach (var View in Views.ToStream().Skip(1)) {
			var Previous = Result._Value.To<tDynamicValue>();
			Result = Dynamic(Pair(Previous.Data, View.Data), mVM_Type.Set(Previous.ActualType, View.Type));
		}
		
		return Result;
	}
	
	public static mStream.tStream<(tData Data, mVM_Type.tType Type)>
	Alternatives(this tDynamicValue aValue) {
		if (aValue.ActualType.IsSet(out var Left, out var Right)) {
			var Pair = aValue.Data._Value.To<(tData, tData)>();
			return mStream.Concat(new tDynamicValue(Left, Pair.Item1).Alternatives(), new tDynamicValue(Right, Pair.Item2).Alternatives());
		}
		
		return mStream.Stream((aValue.Data, aValue.ActualType));
	}
	
	public sealed class
	tData {
		public mAny.tAny
		_Value = mAny.Any(mStd.cEmpty);
		
		public mTreeMap.tTree<tNat32, tData>
		_Fields = mTreeMap.Tree<tNat32, tData>((a1, a2) => a1.CompareTo(a2), []);
		
		public tNat64
		_DebugId = mStd.NewDebugId();
		
	}
	
	private static tData
	Data<t>(
		t aValue
	) => new() {
		_Value = mAny.Any(aValue)
	};
	
	private static tData
	Data(
		mTreeMap.tTree<tNat32, tData> aFields
	) => new() {
		_Fields = aFields,
	};
	
	private static tData
	Data<t1, t2>(
		t1 aValue1,
		t2 aValue2
	) => new() {
		_Value = mAny.Any((aValue1, aValue2))
	};
	
	private static tBool
	Is<t>(
		this tData aData,
		out t aValue
	) {
		aValue = default!;
		
		return (
			aData._Value.Is(out aValue)
		);
	}
	
	private static tBool
	Is<t1, t2>(
		this tData aData,
		out t1 aValue1,
		out t2 aValue2
	) {
		if (
			aData._Value.Is(out (t1, t2) Tuple)
		) {
			(aValue1, aValue2) = Tuple;
			return true;
		} else {
			aValue1 = default!;
			aValue2 = default!;
			return false;
		}
	}
	
	public static tData
	Empty(
	) => new();
	
	public static tBool
	IsEmpty(
		this tData aData
	) => aData._Value.Is(out tDynamicValue Dynamic) ? Dynamic.Alternatives().Any(__ => __.Type.IsEmpty()) : aData._Fields.Deep() == 0 && aData._Value.Is(out mStd.tEmpty _);
	
	public static tData
	Bool(
		tBool aValue
	) => Data(aValue);
	
	public static tBool
	IsBool(
		this tData aData,
		out tBool aValue
	) {
		if (aData._Value.Is(out tDynamicValue Dynamic) && Dynamic.ActualType.Kind is mVM_Type.tKind.True or mVM_Type.tKind.False) { aData = Dynamic.Data; }
		return aData.Is(out aValue);
	}
	
	public static tData
	Int(
		tInt32 aValue
	) => Data(aValue);
	
	public static tBool
	IsInt(
		this tData aData,
		out tInt32 aValue
	) {
		if (aData._Value.Is(out tDynamicValue Dynamic) && Dynamic.ActualType.IsInt()) { aData = Dynamic.Data; }
		return aData.Is(out aValue);
	}
	
	public static tData
	Pair(
		tData aFirst,
		tData aSecond
	) => Data(aFirst, aSecond);
	
	public static tBool
	IsPair(
		this tData aData,
		out tData aFirst,
		out tData aSecond
	) {
		if (aData._Value.Is(out tDynamicValue Dynamic)) {
			if (Dynamic.Alternatives().Where(__ => __.Type.Kind is mVM_Type.tKind.Pair).TryFirst().IsSome(out var View)) { aData = View.Data; }
			else { aFirst = aData; aSecond = Empty(); return false; }
		}
		
		if (!aData.Is(out aFirst, out aSecond)) {
			aFirst = aData;
			aSecond = Empty();
			return false;
		}
		
		return true;
	}
	
	public readonly struct tSig(tData aHead, tData aBody) {
		public readonly tData Head = aHead;
		public readonly tData Body = aBody;
	}
	public readonly struct tInterface(tData aObject, tData aMethods) {
		public readonly tData Object = aObject;
		public readonly tData Methods = aMethods;
	}
	
	public static tData Sig(tData aHead, tData aBody) => Data(new tSig(aHead, aBody));
	public static tData Interface(tData aObject, tData aMethods) => Data(new tInterface(aObject, aMethods));
	public static tData DynamicSig(mVM_Type.tType aContract, tData aHead, tData aBody) => Dynamic(Sig(aHead, aBody), aContract);
	public static tData DynamicInterface(mVM_Type.tType aContract, tData aObject, tData aMethods) => Dynamic(Interface(aObject._Value.Is(out tOpaqueValue _) ? aObject : new tData {_Value = mAny.Any(new tOpaqueValue(aObject))}, aMethods), aContract);
	
	public static tBool
	IsSig(
		this tData aData,
		out mVM_Type.tType aContract,
		out tData aHead,
		out tData aBody
	) {
		if (!aData._Value.Is(out tDynamicValue Dynamic) || !Dynamic.ActualType.IsSig(out _, out _)) {
			aContract = default!;
			aHead = default!;
			aBody = default!;
			return false;
		}
		
		var Sig = Dynamic.Data._Value.To<tSig>();
		(aContract, aHead, aBody) = (Dynamic.ActualType, Sig.Head, Sig.Body);
		return true;
	}
	
	public static tBool
	IsSig(this (tData Data, mVM_Type.tType Type) aValue, out mVM_Type.tType aContract, out tData aHead, out tData aBody) {
		aContract = aValue.Type;
		aHead = default!;
		aBody = default!;
		if (!aContract.IsSig(out _, out _) || !aValue.Data._Value.Is(out tSig Sig)) { return false; }
		(aHead, aBody) = (Sig.Head, Sig.Body);
		return true;
	}

	public static tBool
	IsInterface(this tData aData, out mVM_Type.tType aContract, out tData aObject, out tData aMethods) {
		aContract = default!;
		aObject = default!;
		aMethods = default!;
		
		if (!aData._Value.Is(out tDynamicValue Dynamic) || !Dynamic.ActualType.IsInterface(out _, out _)) {
			return false;
		}
		
		var Value = Dynamic.Data._Value.To<tInterface>();
		(aContract, aObject, aMethods) = (Dynamic.ActualType, Value.Object, Value.Methods);
		return true;
	}
	
	public static tBool
	IsInterface(this (tData Data, mVM_Type.tType Type) aValue, out mVM_Type.tType aContract, out tData aObject, out tData aMethods) {
		aContract = aValue.Type;
		aObject = default!;
		aMethods = default!;
		if (!aContract.IsInterface(out _, out _) || !aValue.Data._Value.Is(out tInterface Value)) { return false; }
		(aObject, aMethods) = (Value.Object, Value.Methods);
		return true;
	}
	
	public static tData
	Type(
		mVM_Type.tType aValue
	) {
		mAssert.IsTrue(aValue.KindType().IsType(), "expected type value");
		return Data(aValue);
	}
	
	// Type expressions may also denote constructors or type parameters.
	public static tData
	TypeExpression(
		mVM_Type.tType aValue
	) => Data(
		aValue
	);
	
	public static mVM_Type.tType
	TypeExpressionValue(
		this tData aValue
	) {
		if (aValue.IsBool(out var Bool)) {
			return Bool ? mVM_Type.True() : mVM_Type.False();
		}
		
		mAssert.IsTrue(aValue._Value.Is(out mVM_Type.tType Expression));
		return Expression;
	}
	
	public static mVM_Type.tType
	SignatureValue(
		this tData aValue
	) {
		var Value = aValue.TypeExpressionValue();
		mAssert.IsTrue(Value.IsSignature(), "expected type or generic signature");
		return Value;
	}
	
	public static mVM_Type.tType
	TypeValue(
		this tData aValue
	) {
		var Value = aValue.TypeExpressionValue();
		mAssert.IsTrue(Value.KindType().IsType(), "expected type value");
		return Value;
	}
	
	public static tData
	Tuple(
		System.Span<tData> a
	) => mStream.Stream(a).Reduce(Empty(), Pair);
	
	public static tData CaptureTypes(this tData aEnvironment, mVM_Type.tType[] aSymbols, mVM_Type.tTypeResolver? aTypes) => aSymbols.Length == 0 ? aEnvironment : Pair(Tuple(aSymbols.Select(__ => TypeExpression(__.Resolve(aTypes))).ToArray()), aEnvironment);
	
	public static (tData Environment, mVM_Type.tTypeResolver? Types) OpenTypeCaptures(this tData aEnvironment, mVM_Type.tType[] aSymbols, mVM_Type.tTypeResolver? aTypes) {
		if (aSymbols.Length == 0) { return (aEnvironment, aTypes); }
		var Pair = aEnvironment._Value.To<(tData, tData)>();
		var Values = new mVM_Type.tType[aSymbols.Length];
		var Tail = Pair.Item1;
		
		for (var Index = Values.Length - 1; Index >= 0; Index -= 1) {
			var Item = Tail._Value.To<(tData, tData)>();
			Values[Index] = Item.Item2.TypeExpressionValue();
			Tail = Item.Item1;
		}
		
		var Cache = new System.Collections.Generic.Dictionary<tNat64, mVM_Type.tType>();
		
		mVM_Type.tType Resolve(mVM_Type.tType aType) {
			if (Cache.TryGetValue(aType.DebugId, out var Previous)) { return Previous; }
			var Original = aType;
			for (var Index = 0; Index < Values.Length; Index += 1) { aType = aType.Substitute(aSymbols[Index], Values[Index]); }
			aType = aType.Resolve(aTypes);
			Cache.Add(Original.DebugId, aType);
			return aType;
		}
		
		return (Pair.Item2, Resolve);
	}
	
	public static tBool
	IsTuple(
		this tData aData,
		out tData a1,
		out tData a2
	) {
		a1 = default!;
		
		return (
			aData.IsPair(out var Rest2, out a2) &&
			Rest2.IsPair(out var Rest_, out a1) &&
			Rest_.IsEmpty()
		);
	}
	
	public static tBool
	IsTuple(
		this tData aData,
		out tData a1,
		out tData a2,
		out tData a3
	) {
		a1 = default!;
		a2 = default!;
		
		return (
			aData.IsPair(out var Rest23, out a3) &&
			Rest23.IsPair(out var Rest3, out a2) &&
			Rest3.IsPair(out var Rest_, out a1) &&
			Rest_.IsEmpty()
		);
	}
	
	public static tBool
	IsTuple(
		this tData aData,
		out tData a1,
		out tData a2,
		out tData a3,
		out tData a4
	) {
		a1 = default!;
		a2 = default!;
		a3 = default!;
		
		return (
			aData.IsPair(out var Rest234, out a4) &&
			Rest234.IsPair(out var Rest34, out a3) &&
			Rest34.IsPair(out var Rest4, out a2) &&
			Rest4.IsPair(out var Rest_, out a1) &&
			Rest_.IsEmpty()
		);
	}
	
	public static tData
	Prefix(
		tNat32 aPrefixId,
		tData aData
	) => Data(aPrefixId, aData);
	
	public static tBool
	IsPrefix(
		this tData aData,
		out tNat32 aPrefixId,
		out tData aValue
	) {
		if (aData._Value.Is(out tDynamicValue Dynamic)) {
			if (Dynamic.Alternatives().Where(__ => __.Type.Kind is mVM_Type.tKind.Prefix).TryFirst().IsSome(out var View)) { aData = View.Data; }
			else { aPrefixId = 0; aValue = Empty(); return false; }
		}
		
		return aData.Is(out aPrefixId, out aValue);
	}
	
	public static tBool
	IsPrefix(
		this tData aData,
		tNat32 aPrefixId,
		out tData aValue
	) => aData.IsPrefix(out var PrefixId, out aValue) && PrefixId == aPrefixId;
	
	public static tData
	Prefix(
		tText aPrefix,
		tData aData
	) => Data(aPrefix.PrefixHash(), aData);
	
	public static tBool
	IsPrefix(
		this tData aData,
		tText aPrefix,
		out tData aValue
	) => aData.IsPrefix(aPrefix.PrefixHash(), out aValue);
	
	public static tData
	Record(
		tData aRecord,
		tData aPrefix
	) {
		mAssert.IsTrue(aPrefix.IsPrefix(out var PrefixHash, out var Data_));
		mAssert.IsTrue(aRecord.IsEmpty() || aRecord.IsRecord(out _));
		var Fields = aRecord._Fields;
		mAssert.IsTrue(Fields.TryGet(PrefixHash).IsNone());
		
		return Data(
			Fields.Set(PrefixHash, Data_)
		);
	}
	
	public static tData
	Record(
		(tText Key, tData Value)[] aFields
	) => aFields.AsStream(
	).Map(
		__ => Prefix(__.Key, __.Value)
	).Reduce(
		Empty(),
		Record
	);
	
	public static tBool
	IsRecord(
		this tData aData,
		[NotNullWhen(true)]out mTreeMap.tTree<tNat32, tData> aFields
	) {
		if (aData._Value.Is(out tDynamicValue Dynamic) && Dynamic.ActualType.IsRecord(out _)) { aData = Dynamic.Data; }
		
		if (aData._Fields.Deep() != 0) {
			aFields = aData._Fields;
			return true;
		} else {
			aFields = default;
			return false;
		}
	}
	
	public static tData
	GetField(
		this tData aRecord,
		tNat32 aFieldName
	) => aRecord._Fields.TryGet(aFieldName).AssertNotEmpty();
	
	public static tData
	Proc<tPos>(
		tProcDef<tPos> aDef,
		tData aEnv
	) {
		var Environment = aDef.DefType.Refs[1];
		mAssert.IsTrue(Environment.IsImmutable, "A closure environment must be statically immutable.");
		return Closure(aDef, aEnv.Normalize(Environment, "Umgebung"));
	}
	
	public static tData
	Closure<tPos>(tProcDef<tPos> aDef, tData aEnv) => Data(aDef, aEnv);
	
	public static tBool
	IsProc(
		this tData aData,
		[MaybeNullWhen(false)]out iProcDef aDef,
		[MaybeNullWhen(false)]out tData aEnv
	) {
		if (aData._Value.Is(out ITuple Data) && Data.Length == 2 && Data[0] is iProcDef Definition) {
			
			aDef = Definition;
			aEnv = (tData)Data[1]!;
			
			return true;
		} else {
			aDef = default!;
			aEnv = default!;
			
			return false;
		}
	}
	
	public static tBool
	IsProc<tPos>(
		this tData aData,
		out tProcDef<tPos> aDef,
		out tData aEnv
	) => aData.Is(out aDef, out aEnv);
	
	public static tData
	ExternProc(
		mVM_Type.tType aSignature,
		mStd.tFunc<tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>, tData> aExternDef,
		tData aEnv,
		mVM_Type.tType? aEnvironmentType = null,
		tData[]? aResultOrigins = null
	) {
		var Type = aEnvironmentType ?? mVM_Type.Empty();
		mAssert.IsTrue(Type.IsImmutable, "A native closure environment must be statically immutable.");
		return NativeClosure(new tNativeCode {Signature = aSignature, Execute = aExternDef, ResultOrigins = aResultOrigins ?? []}, aEnv.Normalize(Type, "Umgebung"));
	}
	
	public static tData
	NativeClosure(tNativeCode aCode, tData aEnvironment) => Data(aCode, aEnvironment);
	
	public static tBool
	IsExternProc(
		this tData aData,
		out tNativeCode aExternDef,
		out tData aEnv
	) => aData.Is(out aExternDef, out aEnv);
	
	public static tData
	Def<tPos>(
		tProcDef<tPos> aDef
	) => Data(aDef);
	
	public static tBool
	IsDef(
		this tData aData,
		out iProcDef aDef
	) {
		return aData._Value.Is(out aDef);
	}
	
	public static tBool
	IsDef<tPos>(
		this tData aData,
		out tProcDef<tPos> aDef
	) => aData.Is(out aDef);
	
	public static tData
	Var(
		tData aValue
	) => Data(new mRef.tRefBox<tData> { _Value = aValue });
	
	public static tBool
	IsVar(
		this tData aData,
		out tData aValue
	) {
		if (aData._Value.Is(out tCellView _)) { aValue = aData.ReadCell(); return true; }
		
		if (aData.Is(out mRef.tRefBox<tData> Cell)) {
			aValue = Cell._Value;
			return true;
		}
		
		aValue = default!;
		return false;
	}
	
	public static tData
	ExternDef(
		mVM_Type.tType aSignature,
		mStd.tFunc<tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>, tData> a,
		tData[]? aResultOrigins = null
	) => Data(new tNativeCode {Signature = aSignature, Execute = a, ResultOrigins = aResultOrigins ?? []});
	
	public static tBool
	IsExternDef(
		this tData aData,
		out tNativeCode a
	) => aData.Is(out a);
	
	public static tData
	TypeType(
	) => Data(mVM_Type.Type());
	
	// TODO: Matches for Types ???
	
	public static tData
	TypeEmpty(
	) => Data(mVM_Type.Empty());
	
	public static tData
	TypeInt(
	) => Data(mVM_Type.Int());
	
	public static tData
	TypePair(
		mVM_Type.tType aType1,
		mVM_Type.tType aType2
	) => Data(mVM_Type.Pair(aType1, aType2));
	
	
	public static mTreeMap.tTree<tNat32, tText> gHashToPrefix = mTreeMap.Tree<tNat32, tText>(
		(aLeft, aRight) => aLeft.CompareTo(aRight),
		[]
	);
	
	public static tNat32
	PrefixHash(
		this tText aPrefix
	) {
		if (aPrefix is null) {
			return 0;
		}
		
		// FNV-1a over UTF-16 code units; IDs stay deterministic across runs.
		var Hash = 2166136261u;
		
		unchecked {
			foreach (var Character in aPrefix) {
				Hash = (Hash ^ Character) * 16777619u;
			}
		}
		
		if (!gHashToPrefix.TryGet(Hash).IsSome(out var Existing)) {
			gHashToPrefix = gHashToPrefix.Set(Hash, aPrefix);
		} else {
			mAssert.IsTrue(Existing == aPrefix, "prefix hash collision");
		}
		
		return Hash;
	}
}

public static class
mVM_Native {
	private sealed class tNode(mVM_Data.tData aData, mVM_Type.tType aType) {
		public readonly mVM_Data.tData Data = aData;
		public readonly mVM_Type.tType Type = aType;
		public mVM_Type.tTypeResolver? Types;
		public mVM_Type.tType? Function;
		public mVM_Data.tData Output = aData;
		public readonly System.Collections.Generic.List<tNode> Children = [];
		public readonly System.Collections.Generic.List<tNode> Parents = [];
		public tBool Valid = true;
		public tBool Choice;
		public tBool Alias;
		public tBool Changed;
		public tBool RequireIdentity;
		public mStd.tAction? Build;
	}
	
	public static mVM_Data.tData
	Normalize(this mVM_Data.tData aData, mVM_Type.tType aType, tText aRole = "Argument", tBool aRevealOpaque = false) {
		var Nodes = new System.Collections.Generic.List<tNode>();
		var Cache = new System.Collections.Generic.Dictionary<(tNat64 Data, mVM_Type.tType Type, mVM_Type.tType? Function), tNode>();
		var Expanded = new System.Collections.Generic.Dictionary<tNat64, mVM_Type.tType>();
		
		tNode Get(mVM_Data.tData aValue, mVM_Type.tType aExpected, mVM_Type.tTypeResolver? aTypes = null) {
			var Original = aValue;
			aExpected = aExpected.Resolve(aTypes);
			
			if (aRevealOpaque && aExpected.Kind is not (mVM_Type.tKind.Any or mVM_Type.tKind.Abstract or mVM_Type.tKind.Free or mVM_Type.tKind.TypeApply or mVM_Type.tKind.Opaque)) {
				while (aValue._Value.Is(out mVM_Data.tOpaqueValue Opaque)) { aValue = Opaque.Data; }
			}
			
			mVM_Type.tType? Function = null;
			
			if (aValue.IsProc(out var Definition, out var Environment)) {
				aTypes = Environment.OpenTypeCaptures(Definition.TypeCaptures, aTypes).Types.WithProof(Definition.SignatureProof);
				aExpected = aExpected.Resolve(aTypes);
				Function = Definition.DefType.Refs[2].Resolve(aTypes);
			}
			
			var Key = (Original._DebugId, aExpected, Function);
			if (Cache.TryGetValue(Key, out var Previous)) { return Previous; }
			var Node = new tNode(aValue, aExpected) {Changed = !ReferenceEquals(Original, aValue), Types = aTypes, Function = Function};
			Cache.Add(Key, Node);
			Nodes.Add(Node);
			return Node;
		}
		
		static void Depend(tNode aParent, tNode aChild) { aParent.Children.Add(aChild); aChild.Parents.Add(aParent); }
		
		static tBool Implements(mVM_Type.tType aSource, mVM_Type.tType aTarget) {
			if (aSource.IsGeneric(out _, out _) && aTarget.IsProc(out var Object, out var Argument, out _)) {
				if (!mVM_Type.InferCall(aSource, Object, Argument, _ => {}).Match(out var Bound, out _)) { return false; }
				aSource = mVM_Type.Proc(Bound.Object, Bound.Argument, Bound.Result);
			}
			
			return aSource.IsSubType(aTarget, aDescribeError: false).Match(out var Mappings, out _) && Mappings.Deep() == 0;
		}
		
		static tBool NativeImplements(mVM_Type.tType aSource, mVM_Type.tType aTarget) {
			if (Implements(aSource, aTarget)) { return true; }
			var Function = aTarget;
			var Parameters = new System.Collections.Generic.HashSet<tNat64>();
			while (Function.IsGeneric(out var Parameter, out var Body)) { Parameters.Add(Parameter.DebugId); Function = Body; }
			var Receiver = Function.Refs[0].FirstObjectType();
			foreach (var Parameter in Parameters) { if (Receiver.FreeParameters().TryGet(Parameter).IsSome(out _)) { return false; } }
			
			// Only an existential receiver may hide the native representation.
			// Ordinary receiver contracts remain invariant, including ANY and ().
			var Identity = Receiver.Kind is mVM_Type.tKind.Opaque ? Receiver.Refs[0] : Receiver;
			var Hidden = Identity.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.Free or mVM_Type.tKind.TypeApply;
			if (!Hidden && Receiver.Kind is not mVM_Type.tKind.Opaque) { return false; }
			
			static mVM_Type.tType ReplaceReceiver(mVM_Type.tType aObject, mVM_Type.tType aReceiver) {
				if (!aObject.IsPair(out var Tail, out var Head)) { return aReceiver; }
				return Tail.IsEmpty() ? mVM_Type.Pair(Tail, aReceiver) : mVM_Type.Pair(ReplaceReceiver(Tail, aReceiver), Head);
			}
			
			mVM_Type.tType Contract(mVM_Type.tType aFunction, tBool aSource) => aFunction.IsGeneric(out var Parameter, out var Body)
			? mVM_Type.Generic(Parameter, Contract(Body, aSource))
			: mVM_Type.Proc(ReplaceReceiver(aFunction.Refs[0], Hidden ? mVM_Type.Empty() : aSource ? aFunction.Refs[0].FirstObjectType() : Identity), aFunction.Refs[1], aFunction.Refs[2]);
			
			return Implements(Contract(aSource, true), Contract(aTarget, false));
		}
		
		var Root = Get(aData, aType);
		
		for (var Index = 0; Index < Nodes.Count; Index += 1) {
			var Node = Nodes[Index];
			var Value = Node.Data;
			var Expected = Node.Type;
			
			if (Expected.IsRecursive(out var Head, out var Body)) {
				if (!Expanded.TryGetValue(Expected.DebugId, out var Shape)) { Shape = Body.Substitute(Head, Expected); Expanded.Add(Expected.DebugId, Shape); }
				Node.Alias = true;
				Depend(Node, Get(Value, Shape, Node.Types));
				continue;
			}
			
			if (Expected.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) {
				var Alternatives = new System.Collections.Generic.List<(mVM_Type.tType Type, tNode Value)>();
				
				if (Value._Value.Is(out mVM_Data.tDynamicValue Dynamic)) {
					foreach (var Alternative in Dynamic.Alternatives()) {
						if (Expected.IsAny()) {
							var Child = Get(Alternative.Data, Alternative.Type, Node.Types);
							Depend(Node, Child);
							Alternatives.Add((Alternative.Type, Child));
						} else {
							Node.Choice = true;
							var Types = new System.Collections.Generic.Stack<mVM_Type.tType>();
							Types.Push(Expected);
							
							while (Types.TryPop(out var Type)) {
								if (Type.IsSet(out var Left, out var Right)) { Types.Push(Right); Types.Push(Left); continue; }
								if (!mVM_Type.CanMatch(Alternative.Type, Type)) { continue; }
								var Visible = Alternative.Type.LimitVisibility(Type);
								var Child = Get(Alternative.Data, Visible, Node.Types);
								Depend(Node, Child);
								Alternatives.Add((Visible, Child));
							}
						}
					}
					
					var Original = Dynamic.Alternatives();
					Node.Changed |= Alternatives.Count != Original.Count() || Alternatives.Exists(aView => !Original.Any(__ => __.Type.SameType(aView.Type)));
				} else if (Expected.IsSet(out _, out _)) {
					Node.Choice = true;
					Node.Changed = true;
					var Types = new System.Collections.Generic.Stack<mVM_Type.tType>();
					Types.Push(Expected);
					
					while (Types.TryPop(out var Type)) {
						if (Type.IsSet(out var Left, out var Right)) { Types.Push(Right); Types.Push(Left); }
						else { var Child = Get(Value, Type, Node.Types); Depend(Node, Child); Alternatives.Add((Type, Child)); }
					}
				} else { Node.Valid = false; }
				
				Node.Valid &= Node.Children.Count != 0;
				
				Node.Build = () => {
					mVM_Data.tData? Result = null;
					
					foreach (var Alternative in Alternatives) {
						if (!Alternative.Value.Valid) { continue; }
						var Box = mVM_Data.Dynamic(Alternative.Value.Output, Alternative.Type);
						Result = Result is null ? Box : mVM_Data.JoinDynamic(Result, Box);
					}
					
					Node.Output._Value = Result!._Value;
				};
				
				continue;
			}
			
			if (Value._Value.Is(out mVM_Data.tDynamicValue Boxed)) {
				Node.Alias = true;
				Node.Choice = true;
				Node.Changed = true;
				
				foreach (var Alternative in Boxed.Alternatives()) {
					if (Alternative.Type.IsSubType(Expected).Match(out _, out _)) { Depend(Node, Get(Alternative.Data, Expected, Node.Types)); }
				}
				
				Node.Valid = Node.Children.Count != 0;
				continue;
			}
			
			switch (Expected.Kind) {
				case mVM_Type.tKind.Empty: Node.Valid = Value.IsEmpty();
				break;
				case mVM_Type.tKind.Int: Node.Valid = Value._Value.Is(out tInt32 _);
				break;
				case mVM_Type.tKind.True:
				case mVM_Type.tKind.False: Node.Valid = Value._Value.Is(out tBool Flag) && Flag == (Expected.Kind is mVM_Type.tKind.True);
				break;
				case mVM_Type.tKind.Type: Node.Valid = Value._Value.Is(out mVM_Type.tType Type) && Type.KindType().IsType();
				break;
				case mVM_Type.tKind.Pair:
				if (Value._Value.Is(out (mVM_Data.tData, mVM_Data.tData) Pair)) {
					var Left = Get(Pair.Item1, Expected.Refs[0], Node.Types);
					var Right = Get(Pair.Item2, Expected.Refs[1], Node.Types);
					Depend(Node, Left);
					Depend(Node, Right);
					Node.Build = () => Node.Output._Value = mVM_Data.Pair(Left.Output, Right.Output)._Value;
				} else { Node.Valid = false; }
				
				break;
				case mVM_Type.tKind.Prefix:
				if (Value._Value.Is(out (tNat32, mVM_Data.tData) Prefix) && Prefix.Item1 == Expected.Prefix!.PrefixHash()) {
					var Child = Get(Prefix.Item2, Expected.Refs[0], Node.Types);
					Depend(Node, Child);
					Node.Build = () => Node.Output._Value = mVM_Data.Prefix(Prefix.Item1, Child.Output)._Value;
				} else { Node.Valid = false; }
				
				break;
				case mVM_Type.tKind.Record: {
					var Fields = new System.Collections.Generic.List<(tNat32 Key, tNode Value)>();
					
					foreach (var Field in Expected.Fields.ToStream()) {
						if (!Value._Fields.TryGet(Field.Key.PrefixHash()).IsSome(out var Data)) { Node.Valid = false; break; }
						var Child = Get(Data, Field.Value, Node.Types);
						Depend(Node, Child);
						Fields.Add((Field.Key.PrefixHash(), Child));
					}
					
					Node.Build = () => {
						var Values = Value._Fields;
						foreach (var Field in Fields) { Values = Values.Set(Field.Key, Field.Value.Output); }
						Node.Output._Fields = Values;
					};
					
					break;
				}
				case mVM_Type.tKind.Var:
				case mVM_Type.tKind.Ref:
				if (Value._Value.Is(out mVM_Data.tCellView _) || Value._Value.Is(out mRef.tRefBox<mVM_Data.tData> _)) { Depend(Node, Get(Value.ReadCell(), Expected.Refs[0], Node.Types)); Node.RequireIdentity = true; }
				else { Node.Valid = false; }
				
				break;
				case mVM_Type.tKind.Generic:
				if (Expected.Refs[1].Kind is not (mVM_Type.tKind.Proc or mVM_Type.tKind.Generic)) { Node.Alias = true; Depend(Node, Get(Value, Expected.Refs[1], Node.Types)); break; }
				goto case mVM_Type.tKind.Proc;
				case mVM_Type.tKind.Proc:
				if (Value.IsProc(out var Definition, out var Environment)) {
					Node.Valid = Definition.DefType.Refs[1].IsImmutable && Implements(Node.Function!, Expected);
					Node.RequireIdentity = true;
					Depend(Node, Get(Environment, Definition.DefType.Refs[1], Node.Types));
				} else if (Value._Value.Is(out mVM_Type.tType Constructor)) { Node.Valid = Constructor.KindType().IsSubType(Expected).Match(out _, out _); }
				else if (Value.IsExternProc(out var Native, out _)) { Node.Valid = NativeImplements(Native.Signature, Expected); }
				else if (Value.IsExternDef(out Native)) { Node.Valid = Expected.IsProc(out _, out _, out var Closed) && Closed.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic && NativeImplements(Native.Signature, Closed); }
				else { Node.Valid = Value.IsDef(out _); }
				
				break;
				case mVM_Type.tKind.Sig:
				if (Value._Value.Is(out mVM_Data.tSig Sig) && Sig.Head._Value.Is(out mVM_Type.tType Witness)) {
					var Binder = Expected.Refs[0];
					Node.Valid = Binder.IsFree() ? Witness.KindType().SameType(Binder.KindType()) : Witness.SameType(Binder);
					var Child = Get(Sig.Body, Binder.IsFree() ? Expected.Refs[1].Substitute(Binder, mVM_Type.Opaque(Witness)) : Expected.Refs[1], Node.Types.WithWitness(Binder.ErasedHead(), Witness));
					Depend(Node, Child);
					Node.Build = () => Node.Output._Value = mVM_Data.Sig(Sig.Head, Child.Output)._Value;
				} else { Node.Valid = false; }
				
				break;
				case mVM_Type.tKind.Interface:
				if (Value._Value.Is(out mVM_Data.tInterface Interface)) {
					var Child = Get(Interface.Methods, Expected.Refs[1].Substitute(Expected.Refs[0], Expected.Refs[0].ErasedHead()), Node.Types);
					Depend(Node, Child);
					Node.Build = () => Node.Output._Value = mVM_Data.Interface(Interface.Object, Child.Output)._Value;
				} else { Node.Valid = false; }
				
				break;
				case mVM_Type.tKind.Abstract:
				case mVM_Type.tKind.Free:
				case mVM_Type.tKind.TypeApply:
				if (!Value._Value.Is(out mVM_Data.tOpaqueValue _)) { Node.Changed = true; Node.Build = () => Node.Output._Value = mAny.Any(new mVM_Data.tOpaqueValue(Value)); }
				break;
				case mVM_Type.tKind.Opaque: {
					var Wrapped = Value._Value.Is(out mVM_Data.tOpaqueValue Opaque);
					Node.Changed |= !Wrapped;
					var Identity = Expected.Refs[0];
					
					if (Identity.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.Free or mVM_Type.tKind.TypeApply) {
						if (!Wrapped) { Node.Build = () => Node.Output._Value = mAny.Any(new mVM_Data.tOpaqueValue(Value)); }
						break;
					}
					
					var Child = Get(Wrapped ? Opaque!.Data : Value, Identity, Node.Types);
					Depend(Node, Child);
					Node.Build = () => Node.Output._Value = mAny.Any(new mVM_Data.tOpaqueValue(Child.Output));
					break;
				}
				default: Node.Valid = false;
				break;
			}
		}
		
		var Pending = new System.Collections.Generic.Queue<tNode>();
		foreach (var Node in Nodes) { if (Node.Changed) { Pending.Enqueue(Node); } }
		
		while (Pending.TryDequeue(out var Child)) {
			foreach (var Parent in Child.Parents) { if (!Parent.Changed && !Parent.RequireIdentity) { Parent.Changed = true; Pending.Enqueue(Parent); } }
		}
		
		foreach (var Node in Nodes) {
			if (Node.RequireIdentity && Node.Children.Exists(__ => __.Changed)) { Node.Valid = false; }
			if (!Node.Valid) { Pending.Enqueue(Node); }
		}
		
		while (Pending.TryDequeue(out var Child)) {
			foreach (var Parent in Child.Parents) {
				if (Parent.Valid && (Parent.Choice ? Parent.Children.TrueForAll(__ => !__.Valid) : true)) { Parent.Valid = false; Pending.Enqueue(Parent); }
			}
		}
		
		if (!Root.Valid) {
			var Actual = aData._Value.Is(out tBool Flag) ? Flag ? "§TRUE" : "§FALSE" : "<native value; Diagnose gekürzt>";
			throw mError.Error($"{aRole}: Native value does not implement its declared contract.\nErwarteter Typ:\n  {aType.ToText()}\nAktueller Typ:\n  {Actual}");
		}
		
		foreach (var Node in Nodes) { if (Node.Valid && Node.Changed && !Node.Alias && Node.Build is not null) { Node.Output = mVM_Data.Empty(); } }
		
		foreach (var Node in Nodes) {
			if (!Node.Valid || !Node.Alias || !Node.Changed) { continue; }
			var Target = Node;
			var Seen = new System.Collections.Generic.HashSet<tNode>();
			while (Target.Alias) { mAssert.IsTrue(Seen.Add(Target), "An unproductive recursive type has no native representation."); Target = Target.Children.Find(__ => __.Valid)!; }
			Node.Output = Target.Output;
		}
		
		foreach (var Node in Nodes) { if (Node.Valid && Node.Changed && !Node.Alias) { Node.Build?.Invoke(); } }
		return Root.Output;
	}
}