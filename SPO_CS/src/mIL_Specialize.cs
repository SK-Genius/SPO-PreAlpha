#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include _GlobalUsings.cs
#:include mIL_Specialize.Flow.cs
#:ref mVM_View.cs

public static partial class
mIL_Specialize {
	public sealed class
	tTemplate<tPos> {
		public required mVM_Data.tProcDef<tPos> Token;
		public required mVM_Data.tProcDef<tPos> Source;
		public mTreeMap.tTree<mVM_Type.tType, mVM_Type.tType> Captures;
		public mStd.tFunc<tContext<tPos>, mVM_Type.tType, mTreeMap.tTree<mVM_Type.tType, mVM_Type.tType>, mVM_Data.tProcDef<tPos>>? Body;
	}
	
	public sealed class
	tContext<tPos> {
		public tBool Started;
		public mArrayList.tArrayList<tFrame<tPos>> Frames = mArrayList.List<tFrame<tPos>>();
		public tBool Progress;
		public readonly System.Collections.Generic.HashSet<tFlow<tPos>> NativeInputs = [];
		public readonly System.Collections.Generic.List<(mVM_Data.tNativeCode Code, mVM_Type.tType Type, tFlow<tPos> Value)> NativeResults = [];
		public readonly System.Collections.Generic.HashSet<mVM_Type.tType> TypeWitnesses = [];
		public readonly mArrayList.tArrayList<mVM_Data.tConversion> Views = mArrayList.List<mVM_Data.tConversion>();
		public readonly System.Collections.Generic.Dictionary<(tInt32, tInt32, tInt32, tBool), System.Collections.Generic.List<mVM_Data.tConversion>> ConversionIndex = [];
		public mArrayList.tArrayList<mVM_Type.tType> ActualTypes = mArrayList.List(mVM_Type.Empty(), mVM_Type.True(), mVM_Type.False());
		public readonly mArrayList.tArrayList<tTemplate<tPos>> Templates = mArrayList.List<tTemplate<tPos>>();
		public readonly mArrayList.tArrayList<mVM_Data.tCallSite<tPos>> Calls = mArrayList.List<mVM_Data.tCallSite<tPos>>();
		public readonly mArrayList.tArrayList<mVM_Data.tCloseSite<tPos>> Closures = mArrayList.List<mVM_Data.tCloseSite<tPos>>();
		
		public readonly mArrayList.tArrayList<(mVM_Data.tProcDef<tPos> Template, mVM_Type.tType Function, mVM_Data.tProcDef<tPos> Code)>
		Bodies = mArrayList.List<(mVM_Data.tProcDef<tPos>, mVM_Type.tType, mVM_Data.tProcDef<tPos>)>();
		
		public readonly mArrayList.tArrayList<(mVM_Type.tType Source, mVM_Type.tType Target, tBool RevealOpaque, mVM_Type.tWitnessMap? Proof, mVM_Data.tProcDef<tPos> Template)>
		Adapters = mArrayList.List<(mVM_Type.tType, mVM_Type.tType, tBool, mVM_Type.tWitnessMap?, mVM_Data.tProcDef<tPos>)>();
	}
	
	public static void
	Configure<tPos>(this tContext<tPos> aContext, mVM_Data.tProcDef<tPos> aCode) {
		aCode.AdapterFactory = (a1, a2, a3, a4) => aContext.Adapter(a1, a2, a3, a4);
		aCode.Conversions = aContext.Views;
		aCode.ConversionIndex = aContext.ConversionIndex;
		
		aCode.RegisterActual = aType => {
			if (!aContext.ActualTypes.ToStream().Any(aType.SameType)) { aContext.ActualTypes.Push(aType); }
		};
		
		aCode.RegisterWitness = aType => aContext.TypeWitnesses.Add(aType);
	}
	
	private static mTreeMap.tTree<mVM_Type.tType, mVM_Type.tType>
	Mappings() => mTreeMap.Tree<mVM_Type.tType, mVM_Type.tType>((a1, a2) => a1.DebugId.CompareTo(a2.DebugId), []);
	
	private static mVM_Type.tType
	Function(
		this mVM_Data.iProcDef aCode
	) {
		mAssert.IsTrue(aCode.DefType.IsProc(out _, out _, out var Function));
		return Function;
	}
	
	private static tBool
	HasUnboundData(mVM_Type.tType aType, mStream.tStream<mVM_Type.tType> aBound = default) {
		if (aType.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic or mVM_Type.tKind.Interface or mVM_Type.tKind.Sig) { return false; }
		if (aType.IsFree()) { return !aBound.Any(__ => ReferenceEquals(__, aType)); }
		if (aType.IsRecursive(out var Head, out var Body)) { return HasUnboundData(Body, mStream.Stream(Head, aBound)); }
		return aType.IsRecord(out var Fields) ? Fields.ToStream().Any(__ => HasUnboundData(__.Value, aBound)) : mStream.Stream(aType.Refs).Any(__ => HasUnboundData(__, aBound));
	}
	
	private static tBool
	Compatible(
		mVM_Type.tType aSource,
		mVM_Type.tType aTarget,
		mStream.tStream<(tNat64, tNat64)> aSeen = default
	) {
		if (aSource.SameType(aTarget)) { return true; }
		if (aSource.Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply) { return true; }
		if (aTarget.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Free or mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply) { return true; }
		if (aSeen.Any(__ => __ == (aSource.DebugId, aTarget.DebugId))) { return true; }
		aSeen = mStream.Stream((aSource.DebugId, aTarget.DebugId), aSeen);
		if (aSource.IsSet(out var Left, out var Right)) { return Compatible(Left, aTarget, aSeen) && Compatible(Right, aTarget, aSeen); }
		if (aTarget.IsSet(out Left, out Right)) { return Compatible(aSource, Left, aSeen) || Compatible(aSource, Right, aSeen); }
		
		if (aSource.IsGeneric(out var SourceParameter, out var SourceBody) && aTarget.IsGeneric(out var TargetParameter, out var TargetBody)) {
			return Compatible(SourceBody.Substitute(SourceParameter, TargetParameter), TargetBody, aSeen);
		}
		
		if (aSource.IsRecursive(out var Head, out var Body)) { return Compatible(Body.Substitute(Head, aSource), aTarget, aSeen); }
		if (aTarget.IsRecursive(out Head, out Body)) { return Compatible(aSource, Body.Substitute(Head, aTarget), aSeen); }
		if (aSource.Kind != aTarget.Kind) { return false; }
		
		if (aSource.Kind is mVM_Type.tKind.Proc) {
			return Compatible(aTarget.Refs[1], aSource.Refs[1], aSeen) && Compatible(aSource.Refs[2], aTarget.Refs[2], aSeen);
		}
		
		if (aSource.Kind is mVM_Type.tKind.Interface or mVM_Type.tKind.Sig) {
			var Witness = mVM_Type.Abstract("bound", aSource.Refs[0].KindType());
			return Compatible(aSource.Refs[1].Substitute(aSource.Refs[0], Witness), aTarget.Refs[1].Substitute(aTarget.Refs[0], Witness), aSeen);
		}
		
		if (aTarget.IsRecord(out var Fields)) {
			return Fields.ToStream().All(aField => aSource.Fields.TryGet(aField.Key).IsSome(out var Source) && Compatible(Source, aField.Value, aSeen));
		}
		
		if (aTarget.Kind is mVM_Type.tKind.Pair or mVM_Type.tKind.Prefix) {
			return aSource.Prefix == aTarget.Prefix && mStream.ZipShort(mStream.Stream(aSource.Refs), mStream.Stream(aTarget.Refs)).All(__ => Compatible(__.Item1, __.Item2, aSeen));
		}
		
		return true;
	}
	
	public static mVM_Data.tCallSite<tPos>
	Call<tPos>(
		this tContext<tPos> aContext,
		mVM_Type.tType aSource,
		mVM_Type.tType aObject,
		mVM_Type.tType aArgument,
		tBool aPattern = false,
		mVM_Type.tType? aPhysicalObject = null
	) {
		var Signature = aSource;
		
		if (aPattern) {
			while (Signature.IsGeneric(out var Parameter, out var Body)) { Signature = Body.Substitute(Parameter, mVM_Type.Any()); }
		} else {
			var Bound = mVM_Type.InferCall(aSource, aObject, aArgument, _ => {}).ElseThrow();
			Signature = mVM_Type.Proc(Bound.Object, Bound.Argument, Bound.Result);
		}
		
		foreach (var Existing in aContext.Calls.ToStream()) {
			if (
				Existing.SourceSignature.SameType(aSource) && Existing.Signature.SameType(Signature) &&
				Existing.ObjectType.SameType(aObject) && Existing.ArgumentType.SameType(aArgument) && (Existing.PhysicalObjectType ?? Existing.ObjectType).SameType(aPhysicalObject ?? aObject)
			) { return Existing; }
		}
		
		var Site = new mVM_Data.tCallSite<tPos> {SourceSignature = aSource, Signature = Signature, ObjectType = aObject, ArgumentType = aArgument};
		Site.PhysicalObjectType = aPhysicalObject;
		Site.Closure = aContext.Close(aSource, aArgument);
		aContext.Calls.Push(Site);
		return Site;
	}
	
	public static mVM_Data.tCloseSite<tPos>
	Close<tPos>(
		this tContext<tPos> aContext,
		mVM_Type.tType aSignature,
		mVM_Type.tType aEnvironment
	) {
		foreach (var Existing in aContext.Closures.ToStream()) {
			if (Existing.Signature.SameType(aSignature) && Existing.Environment.SameType(aEnvironment)) { return Existing; }
		}
		
		var Site = new mVM_Data.tCloseSite<tPos> {Signature = aSignature, Environment = aEnvironment};
		aContext.Closures.Push(Site);
		return Site;
	}
	
	private static mVM_Type.tType[] RuntimeCaptures<tPos>(tContext<tPos> aContext, mStream.tStream<mVM_Type.tType> aTypes, mStream.tStream<mVM_Type.tType> aLocal = default) {
		var Captures = new System.Collections.Generic.List<mVM_Type.tType>();
		
		void Capture(mVM_Type.tType aType, mStream.tStream<mVM_Type.tType> aBound = default) {
			if (aBound.Any(__ => ReferenceEquals(__, aType))) { return; }
			
			if (aType.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.Free) {
				if (!Captures.Any(__ => ReferenceEquals(__, aType)) && !aLocal.Any(__ => ReferenceEquals(__, aType)) && (aContext.TypeWitnesses.Contains(aType) || aContext.Templates.ToStream().Any(__ => __.Source.TypeBindings.ToStream().Any(aBinding => ReferenceEquals(aBinding.Symbol, aType))))) { Captures.Add(aType); }
				return;
			}
			
			if (aType.Kind is mVM_Type.tKind.Generic or mVM_Type.tKind.Sig or mVM_Type.tKind.Interface or mVM_Type.tKind.Recursive) {
				Capture(aType.Refs[0].KindType(), aBound);
				Capture(aType.Refs[1], mStream.Stream(aType.Refs[0], aBound));
			} else if (aType.IsRecord(out var Fields)) {
				foreach (var Field in Fields.ToStream()) { Capture(Field.Value, aBound); }
			} else { foreach (var Child in aType.Refs) { Capture(Child, aBound); } }
		}
		
		foreach (var Type in aTypes) { Capture(Type); }
		return Captures.ToArray();
	}
	
	private static mVM_Data.tProcDef<tPos>
	Adapter<tPos>(
		this tContext<tPos> aContext,
		mVM_Type.tType aSource,
		mVM_Type.tType aTarget,
		tBool aRevealOpaque,
		mVM_Type.tWitnessMap? aProof
	) {
		foreach (var Existing in aContext.Adapters.ToStream()) {
			if (Existing.Source.SameType(aSource) && Existing.Target.SameType(aTarget) && Existing.RevealOpaque == aRevealOpaque && Existing.Proof.SameWitnesses(aProof)) { return Existing.Template; }
		}
		
		var Types = mStream.Stream(aSource, aTarget);
		for (var Proof = aProof; Proof is not null; Proof = Proof.Parent) { Types = mStream.Stream(Proof.Opaque, mStream.Stream(Proof.Known, Types)); }
		var TypeCaptures = RuntimeCaptures(aContext, Types);
		var Environment = TypeCaptures.Length == 0 ? aSource : mVM_Type.Pair(mStream.Stream(TypeCaptures).Map(__ => __.KindType()).Reduce(mVM_Type.Empty(), mVM_Type.Pair), aSource);
		var Template = new mVM_Data.tProcDef<tPos>(mVM_Type.Proc(mVM_Type.Empty(), Environment, aTarget)) {IsTemplate = true, Compilation = aContext, TypeCaptures = TypeCaptures, SignatureProof = aProof};
		aContext.Adapters.Push((aSource, aTarget, aRevealOpaque, aProof, Template));
		
		aContext.Templates.Push(
			new tTemplate<tPos>() {
				Token = Template,
				Source = Template,
				Captures = Mappings(),
				Body = (aCompiler, aFunction, aBindings) => {
					var Source = aSource;
					var Target = aTarget;
					var Bindings = aBindings;
					
					while (Source.IsGeneric(out var Parameter, out var Body) && Target.IsGeneric(out var TargetParameter, out var TargetBody)) {
						Bindings = Bindings.Set(Parameter, TargetParameter.ApplyMappings(aBindings));
						Source = Body;
						Target = TargetBody;
					}
					
					if (Source.IsGeneric(out _, out _)) {
						var Parameters = mArrayList.List<(mVM_Type.tType Original, mVM_Type.tType Fresh)>();
						
						while (Source.IsGeneric(out var Parameter, out var Body)) {
							var Fresh = mVM_Type.Free(Parameter.Id!, Parameter.KindType());
							Parameters.Push((Parameter, Fresh));
							Source = Body.Substitute(Parameter, Fresh);
						}
						
						var Bound = mVM_Type.BindCall(Source, aFunction.Refs[0], aFunction.Refs[1], _ => {}).ElseThrow();
						foreach (var Mapping in Bound.Mappings.ToStream()) { Bindings = Bindings.Set(Mapping.Key, Mapping.Value); }
						foreach (var Parameter in Parameters.ToStream()) { Bindings = Bindings.Set(Parameter.Original, Parameter.Fresh.ApplyMappings(Bound.Mappings)); }
					}
					
					Source = Source.ApplyMappings(Bindings);
					
					if (mVM_Type.Proc(mVM_Type.Empty(), Source.Refs[1], Source.Refs[2]).IsSubType(mVM_Type.Proc(mVM_Type.Empty(), aFunction.Refs[1], aFunction.Refs[2]), Bindings, false).Match(out var Completed, out _)) {
						Source = Source.ApplyMappings(Completed);
					}
					
					var Site = aCompiler.Call(Source, Source.Refs[0], Source.Refs[1]);
					mVM_Type.tWitnessMap? Specialize(mVM_Type.tWitnessMap? aMap) => aMap is null ? null : new(aMap.Opaque, aMap.Known.ApplyMappings(Bindings), aMap.Seal, Specialize(aMap.Parent));
					var Proof = Specialize(aProof);
					var Code = new mVM_Data.tProcDef<tPos>(mVM_Type.Proc(mVM_Type.Empty(), Environment, aFunction)) {Compilation = aCompiler, TypeCaptures = TypeCaptures, SignatureProof = Proof};
					aCompiler.Configure(Code);
					var Argument = Code.Compile(aFunction.Refs[1], Source.Refs[1], aRevealOpaque, Proof.ReverseWitnesses());
					var Object = Code.Compile(aFunction.Refs[0], Source.Refs[0], aRevealOpaque, Proof.ReverseWitnesses());
					var Result = Code.Compile(Source.Refs[2], aFunction.Refs[2], aRevealOpaque, Proof);
					
					Code.Entry = (aTypes, aEnvironment, aObject, aArgument, aTrace, aExecute, aMemo) => {
						var (Original, Types) = aEnvironment.OpenTypeCaptures(TypeCaptures, aTypes);
						var Outcome = aExecute(Site, Original, Object.Convert(aObject, Types, aMemo), Argument.Convert(aArgument, Types, aMemo), aTrace, Types);
						return Outcome.IsSome(out var Value) ? mMaybe.Some(Result.Convert(Value, Types, aMemo)) : mMaybe.None<mVM_Data.tData>();
					};
					
					Code.Calls = [Site];
					Code.Views = [Argument, Result];
					Code.ObjectViews = [Object];
					return Code;
				}
			}
		);
		
		return Template;
	}
	
	private static mVM_Data.tProcDef<tPos>
	Body<tPos>(
		this tContext<tPos> aContext,
		tTemplate<tPos> aTemplate,
		mVM_Type.tType aFunction,
		mTreeMap.tTree<mVM_Type.tType, mVM_Type.tType> aMappings
	) {
		foreach (var Existing in aContext.Bodies.ToStream()) {
			if (ReferenceEquals(Existing.Template, aTemplate.Token) && Existing.Function.SameType(aFunction)) { return Existing.Code; }
		}
		
		mAssert.IsFalse(aContext.Started, "Executable specializations must be linked before VM execution.");
		
		if (aTemplate.Body is not null) {
			var Native = aTemplate.Body(aContext, aFunction, aMappings);
			aContext.Bodies.Push((aTemplate.Token, aFunction, Native));
			aContext.Frames.Push(new tFrame<tPos>(Native));
			return Native;
		}
		
		var Source = aTemplate.Source;
		var Changed = true;
		
		while (Changed) {
			Changed = false;
			
			void Infer(mVM_Type.tType aFrom, mVM_Type.tType aTo) {
				var From = aFrom.ApplyMappings(aMappings);
				var To = aTo.ApplyMappings(aMappings);
				if (From.HasUnboundParameter()) { return; }
				var Variables = To.FreeParameters();
				if (Variables.Deep() == 0 || !From.IsSubType(To, aMappings, false).Match(out var Found, out _)) { return; }
				
				foreach (var Mapping in Found.ToStream()) {
					if (Variables.TryGet(Mapping.Key.DebugId).IsNone()) { continue; }
					if (aMappings.TryGet(Mapping.Key).IsSome(out var Existing) && Existing.SameType(Mapping.Value)) { continue; }
					aMappings = aMappings.Set(Mapping.Key, Mapping.Value);
					Changed = true;
				}
			}
			
			for (tNat32 Index = 0; Index < Source.Commands.Size; Index += 1) {
				var Operation = Source.Commands.Get(Index).Item1;
				var (First, Second, Result) = Source.OperandTypes.Get(Index);
				if (Operation is mVM_Data.tOpCode.WithType) { Infer(Second, Result!); }
			}
		}
		
		var Declared = Source.Function();
		
		while (Declared.IsGeneric(out var Parameter, out var Inner)) {
			if (Parameter.ApplyMappings(aMappings).HasUnboundParameter()) {
				foreach (var (Operation, Operand, _, _) in Source.Commands.ToStream()) {
					if (Operation is not mVM_Data.tOpCode.LoadConstant) { continue; }
					var Expression = Source.Constants[Operand].TypeExpressionValue();
					mAssert.IsFalse(Expression.FreeParameters().TryGet(Parameter.DebugId).IsSome(out _), "A generic TYPE value requires a statically determined instantiation.");
				}
			}
			
			Declared = Inner;
		}
		
		aFunction = aFunction.ApplyMappings(aMappings);
		var Environment = Source.DefType.Refs[1].ApplyMappings(aMappings);
		var TypeCaptures = aTemplate.Token.TypeCaptures;
		var StoredEnvironment = TypeCaptures.Length == 0 ? Environment : mVM_Type.Pair(mStream.Stream(TypeCaptures).Map(__ => __.KindType()).Reduce(mVM_Type.Empty(), mVM_Type.Pair), Environment);
		var Code = new mVM_Data.tProcDef<tPos>(mVM_Type.Proc(mVM_Type.Empty(), StoredEnvironment, aFunction)) {Compilation = aContext, TypeCaptures = TypeCaptures};
		aContext.Configure(Code);
		aContext.Bodies.Push((aTemplate.Token, aFunction, Code));
		
		foreach (var Type in Source.Types.ToStream()) { Code.Types.Push(Type.ApplyMappings(aMappings)); }
		Code.Types.Set(mVM_Data.cEnvReg, Environment);
		Code.Types.Set(mVM_Data.cObjReg, aFunction.Refs[0]);
		Code.Types.Set(mVM_Data.cArgReg, aFunction.Refs[1]);
		Code.Types.Set(mVM_Data.cResReg, aFunction.Refs[2]);
		foreach (var Position in Source.PosList.ToStream()) { Code.PosList.Push(Position); }
		foreach (var Command in Source.Commands.ToStream()) { Code.Commands.Push(Command); }
		
		foreach (var Types in Source.OperandTypes.ToStream()) {
			Code.OperandTypes.Push((Types.First.ApplyMappings(aMappings), Types.Second.ApplyMappings(aMappings), Types.Result?.ApplyMappings(aMappings)));
		}
		
		Code.Constants = Source.Constants.AsStream().Map(aData => aData._Value.Is(out mVM_Type.tType Type) ? mVM_Data.TypeExpression(Type.ApplyMappings(aMappings)) : aData).ToArrayList().ToArray();
		foreach (var Prefix in Source.TypePrefixes.ToStream()) { Code.TypePrefixes.Push(Prefix); }
		foreach (var Match in Source.TryReturnTypes.ToStream()) { Code.TryReturnTypes = Code.TryReturnTypes.Set(Match.Key, Match.Value.ApplyMappings(aMappings)); }
		foreach (var Head in Source.SigHeads.ToStream()) { Code.SigHeads = Code.SigHeads.Set(Head.Key, Head.Value.ApplyMappings(aMappings)); }
		foreach (var Binding in Source.TypeBindings.ToStream()) { Code.TypeBindings.Push((Binding.Symbol.ApplyMappings(aMappings), Binding.Register)); }
		Code._LastReg = Source._LastReg;
		aContext.Frames.Push(new tFrame<tPos>(Code));
		Code.Calls = new mVM_Data.tCallSite<tPos>?[Code.Commands.Size];
		Code.Guards = new mVM_Data.tCallSite<tPos>?[Code.Commands.Size];
		Code.RecursiveCalls = new mVM_Data.tCallSite<tPos>?[Code.Commands.Size];
		Code.Closures = new mVM_Data.tCloseSite<tPos>?[Code.Commands.Size];
		Code.Views = new mVM_Data.tConversion?[Code.Commands.Size];
		Code.ObjectViews = new mVM_Data.tConversion?[Code.Commands.Size];
		
		for (tNat32 Index = 0; Index < Code.Commands.Size; Index += 1) {
			var (Operation, _, _, _) = Code.Commands.Get(Index);
			var (First, Second, Result) = Code.OperandTypes.Get(Index);
			
			switch (Operation) {
				case mVM_Data.tOpCode.First:
				case mVM_Data.tOpCode.Second: {
					var Projection = Code.Project(First, Result!, Operation is mVM_Data.tOpCode.First ? 0 : 1);
					if (Projection.Dynamic || Projection.Inner!.Apply is not null) { Code.Views[Index] = Projection; }
					break;
				}
				case mVM_Data.tOpCode.DefRecProcs:
				Code.RecursiveCalls[Index] = aContext.Call(mVM_Type.Infer(First, mVM_Type.Empty(), Second, _ => {}).ElseThrow(), mVM_Type.Empty(), Result!);
				goto case mVM_Data.tOpCode.CallFunc;
				case mVM_Data.tOpCode.CallFunc:
				Code.Calls[Index] = aContext.Call(First, mVM_Type.Empty(), Second);
				Code.Closures[Index] = aContext.Close(First, Second);
				break;
				case mVM_Data.tOpCode.CallProc: {
					var Method = First.Refs[1];
					mVM_Type.tType? PhysicalObject = null;
					
					if (Method.IsPrefix(out var Name, out var Marker) && Marker.IsEmpty()) {
						var Interface = First.Refs[0].FirstObjectType();
						Method = Interface.TryGetInterfaceMethod(Name).AssertNotEmpty();
						
						mVM_Type.tType Physical(mVM_Type.tType aObject) {
							if (!aObject.IsPair(out var Tail, out var Head)) { return Interface.Refs[0].ErasedHead(); }
							return Tail.IsEmpty() ? mVM_Type.Pair(Tail, Physical(Head)) : mVM_Type.Pair(Physical(Tail), Head);
						}
						
						PhysicalObject = Physical(First.Refs[0]);
					}
					
					Code.Calls[Index] = aContext.Call(Method, First.Refs[0], Second, false, PhysicalObject);
					Code.Closures[Index] = aContext.Close(Method, Second);
					break;
				}
				case mVM_Data.tOpCode.TryReturn:
				Code.Calls[Index] = aContext.Call(First.IsPair(out var Function, out _) ? Function : First, aFunction.Refs[0], Second, true);
				if (First.IsPair(out _, out var Guard)) { Code.Guards[Index] = aContext.Call(Guard, aFunction.Refs[0], Second, true); }
				Code.Views[Index] = Code.Compile(Code.Calls[Index]!.Signature.Refs[2], Result!);
				break;
				case mVM_Data.tOpCode.TryRemovePrefixFrom:
				Code.Views[Index] = Code.Compile(Second, mVM_Type.Prefix(mVM_Data.gHashToPrefix.TryGet(Code.Commands.Get(Index).Item2).AssertNotEmpty(), Result!));
				break;
				case mVM_Data.tOpCode.TryAsEmpty:
				case mVM_Data.tOpCode.TryAsBool:
				case mVM_Data.tOpCode.TryAsInt:
				case mVM_Data.tOpCode.TryAsType:
				case mVM_Data.tOpCode.TryAsPair:
				case mVM_Data.tOpCode.TryAsRecord:
				case mVM_Data.tOpCode.TryAsVar:
				case mVM_Data.tOpCode.TryAsRef:
				case mVM_Data.tOpCode.TryAsSig:
				if (mVM_Type.CanMatch(First, Result!)) { Code.Views[Index] = Code.Compile(First, Result!); }
				break;
				case mVM_Data.tOpCode.TryHasHeadType:
				Code.Views[Index] = Code.Compile(First, Result!, true);
				break;
				case mVM_Data.tOpCode.NewSig:
				var Witness = Result!.Refs[0].ErasedHead();
				var KnownHead = Code.SigHeads.TryGet(Index).AssertNotEmpty();
				var VisibleBody = Result.Refs[1].Substitute(Result.Refs[0], KnownHead);
				Code.Views[Index] = mVM_View.Sequence(Code.Compile(Second.Refs[1], VisibleBody), Code.Compile(VisibleBody, Result.Refs[1].Substitute(Result.Refs[0], Witness), true, new(Witness, KnownHead, true)));
				break;
				case mVM_Data.tOpCode.NewInterface:
				var Receiver = Result!.Refs[0].ErasedHead();
				var VisibleMethods = Result.Refs[1].Substitute(Result.Refs[0], Second.Refs[0]);
				Code.Views[Index] = mVM_View.Sequence(Code.Compile(Second.Refs[1], VisibleMethods), Code.Compile(VisibleMethods, Result.Refs[1].Substitute(Result.Refs[0], Receiver), true, new(Receiver, Second.Refs[0], true)));
				Code.ObjectViews[Index] = Code.Compile(Second.Refs[0], Receiver, true);
				break;
				case mVM_Data.tOpCode.SigBody:
				Code.Views[Index] = Code.Compile(First.Refs[1], Result!);
				break;
				case mVM_Data.tOpCode.WithType:
				Code.Views[Index] = Code.Compile(Second, Result!);
				break;
				case mVM_Data.tOpCode.VarSet:
				Code.Views[Index] = Code.Compile(Second, First.Refs[0]);
				break;
				case mVM_Data.tOpCode.Narrow:
				Code.Views[Index] = Code.Compile(First, Second);
				break;
				case mVM_Data.tOpCode.ReturnIf:
				if (!Second.IsEmpty() || Second.IsSubType(aFunction.Refs[2]).Match(out _, out _)) { Code.Views[Index] = Code.Compile(Second, aFunction.Refs[2]); }
				break;
				case mVM_Data.tOpCode.ReturnIfNotEmpty:
				if (!First.IsEmpty()) { Code.Views[Index] = Code.Compile(First, aFunction.Refs[2]); }
				break;
			}
		}
		
		return Code;
	}
	
	public static mVM_Data.tProcDef<tPos>
	Prepare<tPos>(
		mStream.tStream<mVM_Data.tProcDef<tPos>> aDefinitions,
		(mVM_Data.tData Data, mVM_Type.tType Type) aImport,
		mStream.tStream<(mVM_Type.tType Function, mVM_Type.tType Object, mVM_Type.tType Argument)> aNativeCalls = default
	) {
		var Context = new tContext<tPos>();
		var Frame = Context.PrepareModule(aDefinitions, aImport.Type, Context.FromData((aImport.Data.Normalize(aImport.Type), aImport.Type)));
		Context.PrepareExports(mStream.Stream((Frame.Code.Function().Refs[2], Frame.Result)), aNativeCalls);
		return Frame.Code;
	}
	
	public static tFrame<tPos> PrepareModule<tPos>(
		this tContext<tPos> aContext,
		mStream.tStream<mVM_Data.tProcDef<tPos>> aDefinitions,
		mVM_Type.tType aImportType,
		tFlow<tPos> aImport
	) {
		var Context = aContext;
		mAssert.IsFalse(Context.Started, "Modules must be linked before VM execution.");
		
		foreach (var Definition in aDefinitions) {
			Definition.IsTemplate = true;
			Definition.Compilation = Context;
			Context.Templates.Push(new tTemplate<tPos>() {Token = Definition, Source = Definition, Captures = Mappings()});
		}
		
		var Entry = aDefinitions.TryLast().AssertNotEmpty();
		var Root = Context.Templates.ToStream().Where(__ => ReferenceEquals(__.Token, Entry)).TryFirst().AssertNotEmpty();
		var Function = Entry.Function();
		mAssert.IsTrue(Function.Kind is mVM_Type.tKind.Proc && !Function.HasUnboundParameter(), "Module entry must be statically specialized.");
		var Code = Context.Body(Root, Function, Mappings());
		Code.EntryArgument = Code.Compile(aImportType, Function.Refs[1]);
		var Frame = Context.Frames.ToStream().Where(__ => ReferenceEquals(__.Code, Code)).TryFirst().AssertNotEmpty();
		var Environment = new tFlow<tPos>();
		var Definitions = aDefinitions.Reverse().Skip(1).Reverse();
		
		foreach (var Definition in Definitions) {
			var Flow = new tFlow<tPos>();
			Flow.Definitions.Push(Definition);
			
			if (Definitions.Take(2).Count() == 1) { Environment = Flow; }
			else { Environment = new() {First = Environment, Second = Flow}; }
		}
		
		Frame.Environment = Environment;
		Frame.Argument = aImport;
		Frame.Active = true;
		return Frame;
	}
	
	public static void PrepareExports<tPos>(
		this tContext<tPos> aContext,
		mStream.tStream<(mVM_Type.tType Type, tFlow<tPos> Value)> aExports,
		mStream.tStream<(mVM_Type.tType Function, mVM_Type.tType Object, mVM_Type.tType Argument)> aNativeCalls = default
	) {
		var Context = aContext;
		Context.Complete();
		
		// Public monomorphic entry points are roots of the same static analysis as
		// internal calls. Their captured implementations must be linked before execution.
		var Public = mArrayList.List<(mVM_Type.tType Type, mVM_Type.tType Object, mVM_Type.tType Argument, tFlow<tPos> Function)>();
		var Shapes = new System.Collections.Generic.Dictionary<tNat64, mVM_Type.tType>();
		
		void Publish(mVM_Type.tType aType, mVM_Type.tType aObject, mVM_Type.tType aArgument, tFlow<tPos> aValue) {
			if (Public.ToStream().Where(__ => __.Type.SameType(aType) && __.Object.SameType(aObject) && __.Argument.SameType(aArgument)).TryFirst().IsSome(out var Existing)) { Context.Progress |= Join(Existing.Function, aValue); }
			else { Public.Push((aType, aObject, aArgument, aValue)); Context.Progress = true; }
		}
		
		void Expose(mVM_Type.tType aType, tFlow<tPos> aValue, System.Collections.Generic.HashSet<(tNat64, tNat64)> aSeen) {
			if (!aType.CarriesCode() || !aSeen.Add((aType.DebugId, aValue.Id))) { return; }
			
			if (aType.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic) {
				if (aType.IsProc(out var Object, out var Argument, out _) && !aType.HasUnboundParameter()) { Publish(aType, Object, Argument, aValue); }
				foreach (var Native in aNativeCalls.Where(__ => __.Function.SameType(aType))) { Publish(aType, Native.Object, Native.Argument, aValue); }
				return;
			}
			
			if (aType.IsRecursive(out var Head, out var Body)) {
				if (!Shapes.TryGetValue(aType.DebugId, out var Shape)) { Shape = Body.Substitute(Head, aType); Shapes.Add(aType.DebugId, Shape); }
				Expose(Shape, aValue, aSeen);
			} else if (aType.IsRecord(out var Fields)) {
				foreach (var Field in Fields.ToStream()) { if (aValue.Fields.TryGet(Field.Key.PrefixHash()).IsSome(out var Value)) { Expose(Field.Value, Value, aSeen); } }
			} else if (aType.IsPair(out var First, out var Second)) {
				if (aValue.First is not null) { Expose(First, aValue.First, aSeen); }
				if (aValue.Second is not null) { Expose(Second, aValue.Second, aSeen); }
			} else if (aType.IsPrefix(out _, out var Inner)) {
				if (aValue.Second is not null) { Expose(Inner, aValue.Second, aSeen); }
			} else if (aType.IsSig(out Head, out Body)) {
				if (aValue.Second is not null) { Expose(Body.Substitute(Head, Head.ErasedHead()), aValue.Second, aSeen); }
			} else if (aType.IsInterface(out Head, out Body)) {
				if (aValue.Second is not null) { Expose(Body.Substitute(Head, Head.ErasedHead()), aValue.Second, aSeen); }
			} else if (aType.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) {
				foreach (var Alternative in aValue.Dynamic.ToStream()) { Expose(Alternative.Type, Alternative.Value, aSeen); }
			}
		}
		
		tFlow<tPos> Input(mVM_Type.tType aType, System.Collections.Generic.Dictionary<tNat64, tFlow<tPos>> aSeen) {
			if (!aType.CarriesCode()) { return new(); }
			if (aSeen.TryGetValue(aType.DebugId, out var Previous)) { return Previous; }
			var Value = new tFlow<tPos>();
			aSeen.Add(aType.DebugId, Value);
			
			foreach (var Active in Context.Frames.ToStream()) {
				if (!Active.Active) { continue; }
				
				for (tNat32 Register = 0; Register < Active.Code.Types.Size; Register += 1) {
					if (Active.Registers[Register] is { } Known && Active.Code.Types.Get(Register).SameType(aType)) { Join(Value, Known); }
				}
			}
			
			if (aType.IsPair(out var First, out var Second)) { Join(Value, new() {First = Input(First, aSeen), Second = Input(Second, aSeen)}); }
			else if (aType.IsRecord(out var Fields)) {
				foreach (var Field in Fields.ToStream()) {
					var Child = Input(Field.Value, aSeen);
					
					if (Value.Fields.TryGet(Field.Key.PrefixHash()).IsSome(out var Existing)) { Join(Existing, Child); }
					else { Value.Fields = Value.Fields.Set(Field.Key.PrefixHash(), Child); }
				}
			} else if (aType.IsPrefix(out _, out var Inner)) { Join(Value, new() {Second = Input(Inner, aSeen)}); }
			
			return Value;
		}
		
		do {
			Context.Progress = false;
			foreach (var Export in aExports) { Expose(Export.Type, Export.Value, []); }
			
			for (tNat32 Index = 0; Index < Public.Size; Index += 1) {
				var RootCall = Public.Get(Index);
				var Site = Context.Call(RootCall.Type, RootCall.Object, RootCall.Argument);
				var Inputs = new System.Collections.Generic.Dictionary<tNat64, tFlow<tPos>>();
				var Result = Context.Observe(Site, RootCall.Function, Input(Site.ObjectType, Inputs), Input(Site.ArgumentType, Inputs));
				Expose(Site.Signature.Refs[2], Result, []);
			}
			
			if (!Context.Progress) { break; }
			Context.Complete();
		} while (true);
		
		Context.ReleaseNativeInputs();
		foreach (var Native in aNativeCalls) { mAssert.IsTrue(Public.ToStream().Any(__ => __.Type.SameType(Native.Function) && __.Object.SameType(Native.Object) && __.Argument.SameType(Native.Argument)), "A native call declaration must refer to an exported function."); }
	}
}