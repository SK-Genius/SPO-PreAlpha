public static partial class mIL_Specialize {
	public sealed class tFlow<tPos> {
		public readonly tNat64 Id = mStd.NewDebugId();
		public tFlow<tPos>? First;
		public tFlow<tPos>? Second;
		public readonly mArrayList.tArrayList<(mVM_Type.tType Type, tFlow<tPos> Value)> Sealed = mArrayList.List<(mVM_Type.tType, tFlow<tPos>)>();
		public mTreeMap.tTree<tNat32, tFlow<tPos>> Fields = mTreeMap.Tree<tNat32, tFlow<tPos>>((a1, a2) => a1.CompareTo(a2), []);
		public readonly mArrayList.tArrayList<mVM_Data.tProcDef<tPos>> Definitions = mArrayList.List<mVM_Data.tProcDef<tPos>>();
		public readonly mArrayList.tArrayList<(mVM_Data.tProcDef<tPos> Code, tFlow<tPos> Environment)> Functions = mArrayList.List<(mVM_Data.tProcDef<tPos>, tFlow<tPos>)>();
		public readonly mArrayList.tArrayList<(mVM_Type.tType Type, tFlow<tPos> Value)> Dynamic = mArrayList.List<(mVM_Type.tType, tFlow<tPos>)>();
		public readonly mArrayList.tArrayList<tCell<tPos>> Cells = mArrayList.List<tCell<tPos>>();
		public readonly System.Collections.Generic.List<tNativeInput<tPos>> Native = [];
		public readonly System.Collections.Generic.List<mVM_Data.tNativeCode> NativeFunctions = [];
		public readonly System.Collections.Generic.List<mVM_Data.tNativeCode> NativeDefinitions = [];
	}
	public sealed class tNativeInput<tPos>(tContext<tPos> aContext, mVM_Data.tData aData) {
		public readonly tContext<tPos> Context = aContext;
		public readonly mVM_Data.tData Data = aData;
		public readonly System.Collections.Generic.List<(mVM_Type.tType Type, tFlow<tPos> Flow)> Resolved = [];
	}
	
	public sealed class tCell<tPos>(tFlow<tPos> aSlot, mVM_Type.tType aType) {
		public readonly tFlow<tPos> Slot = aSlot;
		public readonly mVM_Type.tType Type = aType;
		public readonly tFlow<tPos> Value = new();
		public mArrayList.tArrayList<tCell<tPos>> Views = mArrayList.List<tCell<tPos>>();
		public readonly mArrayList.tArrayList<(tCell<tPos> Cell, mVM_Data.tConversion Conversion)> Writes = mArrayList.List<(tCell<tPos>, mVM_Data.tConversion)>();
	}
	
	public sealed class tFrame<tPos>(mVM_Data.tProcDef<tPos> aCode) {
		public readonly mVM_Data.tProcDef<tPos> Code = aCode;
		public tFlow<tPos> Environment = new();
		public tFlow<tPos> Object = new();
		public tFlow<tPos> Argument = new();
		public readonly tFlow<tPos> Result = new();
		public readonly tFlow<tPos>[] Registers = new tFlow<tPos>[aCode._LastReg + 1];
		public readonly tFlow<tPos>?[] Refined = new tFlow<tPos>?[aCode.Commands.Size];
		public tBool Active;
	}
	
	private static tBool Join<tPos>(tFlow<tPos> aTarget, tFlow<tPos> aSource, System.Collections.Generic.HashSet<(tNat64, tNat64)>? aSeen = null) {
		if (ReferenceEquals(aTarget, aSource)) { return false; }
		aSeen ??= [];
		if (!aSeen.Add((aTarget.Id, aSource.Id))) { return false; }
		var Changed = false;
		
		void Part(ref tFlow<tPos>? aLeft, tFlow<tPos>? aRight) {
			if (aRight is null) { return; }
			
			if (aLeft is null) { aLeft = aRight; Changed = true; }
			else { Changed |= Join(aLeft, aRight, aSeen); }
		}
		
		Part(ref aTarget.First, aSource.First);
		Part(ref aTarget.Second, aSource.Second);
		
		foreach (var Origin in aSource.Sealed.ToStream()) {
			if (aTarget.Sealed.ToStream().Where(__ => __.Type.SameType(Origin.Type)).TryFirst().IsSome(out var Existing)) { Changed |= Join(Existing.Value, Origin.Value, aSeen); }
			else { aTarget.Sealed.Push(Origin); Changed = true; }
		}
		
		foreach (var Field in aSource.Fields.ToStream()) {
			if (aTarget.Fields.TryGet(Field.Key).IsSome(out var Existing)) { Changed |= Join(Existing, Field.Value, aSeen); }
			else { aTarget.Fields = aTarget.Fields.Set(Field.Key, Field.Value); Changed = true; }
		}
		
		foreach (var Definition in aSource.Definitions.ToStream()) {
			if (!aTarget.Definitions.ToStream().Any(__ => ReferenceEquals(__, Definition))) { aTarget.Definitions.Push(Definition); Changed = true; }
		}
		
		foreach (var Function in aSource.Functions.ToStream()) {
			if (aTarget.Functions.ToStream().Where(__ => ReferenceEquals(__.Code, Function.Code)).TryFirst().IsSome(out var Existing)) { Changed |= Join(Existing.Environment, Function.Environment, aSeen); }
			else { aTarget.Functions.Push(Function); Changed = true; }
		}
		
		foreach (var Alternative in aSource.Dynamic.ToStream()) {
			if (aTarget.Dynamic.ToStream().Where(__ => __.Type.SameType(Alternative.Type)).TryFirst().IsSome(out var Existing)) { Changed |= Join(Existing.Value, Alternative.Value, aSeen); }
			else { aTarget.Dynamic.Push(Alternative); Changed = true; }
		}
		
		foreach (var Cell in aSource.Cells.ToStream()) {
			if (!aTarget.Cells.ToStream().Any(__ => ReferenceEquals(__, Cell))) { aTarget.Cells.Push(Cell); Changed = true; }
		}
		
		foreach (var Input in aSource.Native) {
			if (!aTarget.Native.Contains(Input)) { aTarget.Native.Add(Input); Input.Context.NativeInputs.Add(aTarget); Changed = true; }
		}
		
		foreach (var Native in aSource.NativeFunctions) { if (!aTarget.NativeFunctions.Contains(Native)) { aTarget.NativeFunctions.Add(Native); Changed = true; } }
		foreach (var Native in aSource.NativeDefinitions) { if (!aTarget.NativeDefinitions.Contains(Native)) { aTarget.NativeDefinitions.Add(Native); Changed = true; } }
		
		return Changed;
	}
	
	public static tTemplate<tPos> Register<tPos>(this tContext<tPos> aContext, mVM_Data.tProcDef<tPos> aCode) {
		if (aContext.Templates.ToStream().Where(__ => ReferenceEquals(__.Token, aCode)).TryFirst().IsSome(out var Existing)) { return Existing; }
		
		if (aCode.Compilation is tContext<tPos> Owner && Owner.Templates.ToStream().Where(__ => ReferenceEquals(__.Token, aCode)).TryFirst().IsSome(out Existing)) {
			aContext.Templates.Push(Existing);
			return Existing;
		}
		
		var Template = new tTemplate<tPos> {Token = aCode, Source = aCode, Captures = Mappings()};
		aCode.Compilation ??= aContext;
		aContext.Templates.Push(Template);
		return Template;
	}
	
	public static tFlow<tPos> FromData<tPos>(this tContext<tPos> aContext, (mVM_Data.tData Data, mVM_Type.tType Type) aValue) {
		var Seen = new System.Collections.Generic.Dictionary<(tNat64, tNat64), tFlow<tPos>>();
		var Pending = new System.Collections.Generic.List<(mVM_Data.tData Data, mVM_Type.tType Type, tFlow<tPos> Flow)>();
		var Expanded = new System.Collections.Generic.Dictionary<tNat64, mVM_Type.tType>();
		var Schemas = new System.Collections.Generic.HashSet<tNat64>();
		
		void RegisterSchema(mVM_Type.tType aType) {
			if (!Schemas.Add(aType.DebugId)) { return; }
			
			if (aType.IsRecursive(out var Head, out var Body)) {
				if (!Expanded.TryGetValue(aType.DebugId, out var Shape)) { Shape = Body.Substitute(Head, aType); Expanded.Add(aType.DebugId, Shape); }
				RegisterSchema(Shape);
				return;
			}
			
			if (aType.IsSet(out _, out _)) {
				foreach (var Part in aType.Refs) {
					if (!Part.IsSet(out _, out _) && !aContext.ActualTypes.ToStream().Any(Part.SameType)) { aContext.ActualTypes.Push(Part); aContext.Progress = true; }
				}
			}
			
			if (aType.IsRecord(out var Fields)) { foreach (var Field in Fields.ToStream()) { RegisterSchema(Field.Value); } }
			else { foreach (var Child in aType.Refs) { RegisterSchema(Child); } }
		}
		
		tFlow<tPos> Read(mVM_Data.tData aData, mVM_Type.tType aType) {
			while (aType.IsRecursive(out var Head, out var Body)) {
				if (!Expanded.TryGetValue(aType.DebugId, out var Shape)) { Shape = Body.Substitute(Head, aType); Expanded.Add(aType.DebugId, Shape); }
				aType = Shape;
			}
			
			if (Seen.TryGetValue((aData._DebugId, aType.DebugId), out var Previous)) { return Previous; }
			var Flow = new tFlow<tPos>();
			Seen.Add((aData._DebugId, aType.DebugId), Flow);
			if (!aType.CarriesCode()) { RegisterSchema(aType); return Flow; }
			Pending.Add((aData, aType, Flow));
			return Flow;
		}
		
		var Result = Read(aValue.Data, aValue.Type);
		
		for (var Index = 0; Index < Pending.Count; Index += 1) {
			var (aData, aType, Flow) = Pending[Index];
			
			if (aType.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) {
				foreach (var Alternative in aData._Value.To<mVM_Data.tDynamicValue>().Alternatives()) {
					if (!aContext.ActualTypes.ToStream().Any(Alternative.Type.SameType)) { aContext.ActualTypes.Push(Alternative.Type); aContext.Progress = true; }
					Flow.Dynamic.Push((Alternative.Type, Read(Alternative.Data, Alternative.Type)));
				}
			} else if (aType.IsRecord(out var Fields)) {
				foreach (var Field in Fields.ToStream()) { Flow.Fields = Flow.Fields.Set(Field.Key.PrefixHash(), Read(aData.GetField(Field.Key.PrefixHash()), Field.Value)); }
			} else if (aType.IsPair(out var First, out var Second)) {
				var Pair = aData._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
				Flow.First = Read(Pair.Item1, First);
				Flow.Second = Read(Pair.Item2, Second);
			} else if (aType.IsPrefix(out _, out var Inner)) {
				Flow.Second = Read(aData._Value.To<(tNat32, mVM_Data.tData)>().Item2, Inner);
			} else if (aType.IsSig(out _, out var Body)) {
				Flow.Second = Read(aData._Value.To<mVM_Data.tSig>().Body, Body);
			} else if (aType.IsInterface(out _, out Body)) {
				var Interface = aData._Value.To<mVM_Data.tInterface>();
				Flow.First = Read(Interface.Object, aType.Refs[0]);
				Flow.Second = Read(Interface.Methods, Body);
			} else if (aType.Kind is mVM_Type.tKind.Var or mVM_Type.tKind.Ref) {
				var Slot = Read(aData.ReadCell(), aType.Refs[0]);
				var Cell = new tCell<tPos>(Slot, aType);
				Cell.Views.Push(Cell);
				Join(Cell.Value, Slot);
				Flow.Cells.Push(Cell);
			} else if (aType.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic) {
				if (aData.IsDef<tPos>(out var Definition)) { aContext.Register(Definition); Flow.Definitions.Push(Definition); }
				else if (aData.IsProc<tPos>(out Definition, out var Environment)) {
					aContext.Register(Definition);
					Flow.Functions.Push((Definition, Read(Environment, Definition.DefType.Refs[1])));
				}
				else if (aData.IsExternProc(out var Native, out _)) { Flow.NativeFunctions.Add(Native); }
				else if (aData.IsExternDef(out Native)) { Flow.NativeDefinitions.Add(Native); }
			} else if (aType.Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply or mVM_Type.tKind.Opaque) {
				Flow.Native.Add(new(aContext, aData));
				aContext.NativeInputs.Add(Flow);
			}
		}
		
		return Result;
	}
	
	private static mVM_Data.tProcDef<tPos> ViewOwner<tPos>(this tContext<tPos> aContext) {
		var Code = new mVM_Data.tProcDef<tPos>(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Empty())));
		aContext.Configure(Code);
		return Code;
	}
	
	private static tFlow<tPos> ConvertFlow<tPos>(this tContext<tPos> aContext, mVM_Data.tConversion aPlan, tFlow<tPos> aValue) {
		if (!aPlan.Target.CarriesCode()) { return new(); }
		if (aPlan.Before is not null) { return aContext.ConvertFlow(aPlan.After!, aContext.ConvertFlow(aPlan.Before, aValue)); }
		
		if (aPlan.Projection != -1) {
			var Result = new tFlow<tPos>();
			
			if (aPlan.Dynamic) {
				foreach (var Alternative in aValue.Dynamic.ToStream()) { Join(Result, aContext.ConvertFlow(aContext.ViewOwner().Project(Alternative.Type, aPlan.Target, aPlan.Projection), Alternative.Value)); }
				return Result;
			}
			
			return aContext.ConvertFlow(aPlan.Inner!, (aPlan.Projection == 0 ? aValue.First : aValue.Second) ?? Result);
		}
		
		var Seen = mArrayList.List<(mVM_Data.tConversion Plan, tFlow<tPos> Input, tBool PreserveCode, tFlow<tPos> Output)>();
		
		tFlow<tPos> Convert(mVM_Type.tType aSource, mVM_Type.tType aTarget, tFlow<tPos> aInput, tBool aReveal, mVM_Type.tWitnessMap? aProof, tBool aPreserveCode = false) {
			if (ReferenceEquals(aSource, aTarget)) { return aInput; }
			if (aPreserveCode && aSource.Kind == aTarget.Kind && aTarget.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic) { return aInput; }
			var Owner = aContext.ViewOwner();
			var Plan = Owner.Compile(aSource, aTarget, aReveal, aProof);
			if (Plan.Before is not null) { return aContext.ConvertFlow(Plan, aInput); }
			if (Plan.Apply is null) { return aInput; }
			if (Seen.ToStream().Where(__ => ReferenceEquals(__.Input, aInput) && ReferenceEquals(__.Plan, Plan) && __.PreserveCode == aPreserveCode).TryFirst().IsSome(out var Previous)) { return Previous.Output; }
			var Result = new tFlow<tPos>();
			Seen.Push((Plan, aInput, aPreserveCode, Result));
			var SourceOpaque = aSource.Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply or mVM_Type.tKind.Opaque;
			var TargetOpaque = aTarget.Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply or mVM_Type.tKind.Opaque;
			if (TargetOpaque && (aReveal || aSource.Kind is not (mVM_Type.tKind.Any or mVM_Type.tKind.Set))) { if (SourceOpaque) { return aInput; } Result.Sealed.Push((aSource, aInput)); return Result; }
			
			if (SourceOpaque && aReveal && aTarget.IsAny() && aProof is not null) {
				var Reified = aSource.ResolveWitnesses(aProof, false);
				if (!Reified.IsAny()) { Result.Dynamic.Push((Reified, Convert(aSource, Reified, aInput, true, aProof))); return Result; }
			}
			
			if (SourceOpaque && (aReveal || aTarget.Kind is not (mVM_Type.tKind.Any or mVM_Type.tKind.Set))) {
				var Inner = new tFlow<tPos>();
				
				foreach (var Origin in aInput.Sealed.ToStream()) {
					if (!mVM_Type.CanMatch(Origin.Type.ResolveWitnesses(aProof, false), aTarget.ResolveWitnesses(aProof, true))) { continue; }
					Join(Inner, aTarget.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic ? Origin.Value : Convert(Origin.Type, aTarget, Origin.Value, true, aProof, true));
				}
				
				foreach (var Input in aInput.Native) {
					var Resolved = Input.Resolved.Find(__ => __.Type.SameType(aTarget));
					
					if (Resolved.Flow is null) {
						var Data = Input.Data._Value.To<mVM_Data.tOpaqueValue>().Data.Normalize(aTarget, "Argument", true);
						Resolved = (aTarget, aContext.FromData((Data, aTarget)));
						Input.Resolved.Add(Resolved);
					}
					
					Join(Inner, Resolved.Flow);
				}
				
				if (Plan.Adapter is mVM_Data.tProcDef<tPos> Proxy) { Result.Functions.Push((Proxy, Proxy.TypeCaptures.Length == 0 ? Inner : new() {Second = Inner})); return Result; }
				Join(Result, Inner);
				return Result;
			}
			
			if (aSource.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) {
				if (aTarget.IsAny()) { return aInput; }
				
				foreach (var Alternative in aInput.Dynamic.ToStream()) {
					var Reified = aSource.IsAny() ? Alternative.Type : Alternative.Type.ResolveWitnesses(aProof, false);
					if (!mVM_Type.CanMatch(Reified, aTarget.ResolveWitnesses(aProof, true))) { continue; }
					var Value = Alternative.Type.SameType(Reified) ? Alternative.Value : Convert(Alternative.Type, Reified, Alternative.Value, true, aProof);
					Join(Result, Convert(Reified, aTarget, Value, aReveal, aProof, aPreserveCode));
				}
				
				return Result;
			}
			
			if (aTarget.IsAny()) { Result.Dynamic.Push((aSource, aInput)); return Result; }
			
			if (aTarget.IsSet(out var A, out var B)) {
				void Branch(mVM_Type.tType aType) {
					if (aType.IsSet(out var Left, out var Right)) { Branch(Left); Branch(Right); return; }
					if (!mVM_Type.CanMatch(aSource, aType.ResolveWitnesses(aProof, true))) { return; }
					var Actual = aSource.SealVisibility(aType, aProof);
					Result.Dynamic.Push((Actual, Convert(aSource, Actual, aInput, aReveal, aProof)));
				}
				
				Branch(aTarget);
				return Result;
			}
			
			if (aSource.IsRecursive(out var Head, out var Body)) { Join(Result, Convert(Body.Substitute(Head, aSource), aTarget, aInput, aReveal, aProof, aPreserveCode)); return Result; }
			if (aTarget.IsRecursive(out Head, out Body)) { Join(Result, Convert(aSource, Body.Substitute(Head, aTarget), aInput, aReveal, aProof, aPreserveCode)); return Result; }
			if (Plan.Adapter is mVM_Data.tProcDef<tPos> Adapter) { Result.Functions.Push((Adapter, Adapter.TypeCaptures.Length == 0 ? aInput : new() {Second = aInput})); return Result; }
			
			if (aSource.IsRecord(out var SourceFields) && aTarget.IsRecord(out var TargetFields)) {
				Result.Fields = aInput.Fields;
				
				foreach (var Field in TargetFields.ToStream()) {
					var Value = aInput.Fields.TryGet(Field.Key.PrefixHash()).ElseUse(new tFlow<tPos>());
					Result.Fields = Result.Fields.Set(Field.Key.PrefixHash(), Convert(SourceFields.TryGet(Field.Key).AssertNotEmpty(), Field.Value, Value, aReveal, aProof, aPreserveCode));
				}
				
				return Result;
			}
			
			if (aSource.IsPair(out var LeftType, out var RightType) && aTarget.IsPair(out var LeftTarget, out var RightTarget)) {
				Result.First = Convert(LeftType, LeftTarget, aInput.First ?? new(), aReveal, aProof, aPreserveCode);
				Result.Second = Convert(RightType, RightTarget, aInput.Second ?? new(), aReveal, aProof, aPreserveCode);
				return Result;
			}
			
			if (aSource.IsPrefix(out _, out var SourceInner) && aTarget.IsPrefix(out _, out var TargetInner)) {
				Result.Second = Convert(SourceInner, TargetInner, aInput.Second ?? new(), aReveal, aProof, aPreserveCode);
				return Result;
			}
			
			if (aSource.Kind is mVM_Type.tKind.Sig or mVM_Type.tKind.Interface) {
				Result.First = aInput.First;
				Result.Second = Convert(Plan.Inner!.Source, Plan.Inner.Target, aInput.Second ?? new(), Plan.Inner.RevealOpaque, Plan.Inner.Proof, aPreserveCode);
				if (Plan.After is not null) { Result.Second = aContext.ConvertFlow(Plan.After, Result.Second); }
				return Result;
			}
			
			if (aSource.Kind is mVM_Type.tKind.Var or mVM_Type.tKind.Ref) {
				foreach (var Cell in aInput.Cells.ToStream()) {
					if (!Cell.Views.ToStream().Where(__ => __.Type.SameType(aTarget)).TryFirst().IsSome(out var View)) {
						View = new tCell<tPos>(Cell.Slot, aTarget) {Views = Cell.Views};
						Cell.Views.Push(View);
					}
					
					aContext.Progress |= Join(View.Value, Convert(aSource.Refs[0], aTarget.Refs[0], Cell.Value, aReveal, aProof));
					if (aTarget.Kind is mVM_Type.tKind.Var && !View.Writes.ToStream().Any(__ => ReferenceEquals(__.Cell, Cell))) { View.Writes.Push((Cell, Owner.Compile(aTarget.Refs[0], aSource.Refs[0], aReveal, aProof))); }
					Result.Cells.Push(View);
				}
				
				return Result;
			}
			
			Join(Result, aInput);
			return Result;
		}
		
		return Convert(aPlan.Source, aPlan.Target, aValue, aPlan.RevealOpaque, aPlan.Proof);
	}
	
	private static mVM_Data.tPreparedClose<tPos> LinkClose<tPos>(this tContext<tPos> aContext, mVM_Data.tCloseSite<tPos> aSite, mVM_Data.tProcDef<tPos> aDefinition) {
		if (aSite.Targets.TryGet(aDefinition.CodeId).IsSome(out var Previous)) { return Previous; }
		var Template = aContext.Register(aDefinition);
		var Maps = Template.Captures;
		var Seen = new System.Collections.Generic.HashSet<(tNat64, tNat64)>();
		
		void Bind(mVM_Type.tType aFormal, mVM_Type.tType aActual, mStream.tStream<mVM_Type.tType> aBound = default) {
			if (ReferenceEquals(aFormal, aActual)) { return; }
			
			if (aFormal.IsFree()) {
				if (!aBound.Any(__ => ReferenceEquals(__, aFormal)) && !Maps.TryGet(aFormal).IsSome(out _)) { Maps = Maps.Set(aFormal, aActual); }
				return;
			}
			
			if (aFormal.Kind != aActual.Kind || !Seen.Add((aFormal.DebugId, aActual.DebugId))) { return; }
			
			if (aFormal.Kind is mVM_Type.tKind.Generic or mVM_Type.tKind.Recursive or mVM_Type.tKind.Sig or mVM_Type.tKind.Interface) {
				Bind(aFormal.Refs[1].Substitute(aFormal.Refs[0], aActual.Refs[0]), aActual.Refs[1], mStream.Stream(aActual.Refs[0], aBound));
				return;
			}
			
			if (aFormal.IsRecord(out var Fields)) {
				foreach (var Field in Fields.ToStream()) { if (aActual.Fields.TryGet(Field.Key).IsSome(out var Actual)) { Bind(Field.Value, Actual, aBound); } }
			} else {
				for (var Index = 0; Index < aFormal.Refs.Length; Index += 1) { Bind(aFormal.Refs[Index], aActual.Refs[Index], aBound); }
			}
		}
		
		Bind(aDefinition.DefType.Refs[2], aSite.Signature.Refs[2]);
		Bind(aDefinition.DefType.Refs[1], aSite.Environment);
		var Variables = aDefinition.DefType.FreeParameters();
		
		if (aDefinition.DefType.Refs[2].IsSubType(aSite.Signature.Refs[2], Maps, false).Match(out var ResultMaps, out _)) {
			foreach (var Mapping in ResultMaps.ToStream()) { if (Variables.TryGet(Mapping.Key.DebugId).IsSome(out _)) { Maps = Maps.Set(Mapping.Key, Mapping.Value); } }
		}
		
		var Bound = mVM_Type.BindCall(aDefinition.DefType.ApplyMappings(Maps), mVM_Type.Empty(), aSite.Environment, _ => {}).ElseThrow();
		foreach (var Mapping in Bound.Mappings.ToStream()) { if (Variables.TryGet(Mapping.Key.DebugId).IsSome(out _)) { Maps = Maps.Set(Mapping.Key, Mapping.Value); } }
		var Type = Template.Source.DefType.ApplyMappings(Maps);
		var TypeCaptures = RuntimeCaptures(aContext, mStream.Stream(Type), Template.Source.TypeBindings.ToStream().Map(__ => __.Symbol.ApplyMappings(Maps)));
		if (TypeCaptures.Length != 0) { Type = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Pair(mStream.Stream(TypeCaptures).Map(__ => __.KindType()).Reduce(mVM_Type.Empty(), mVM_Type.Pair), Type.Refs[1]), Type.Refs[2]); }
		
		if (!aContext.Templates.ToStream().Where(__ => ReferenceEquals(__.Source, Template.Source) && __.Token.DefType.SameType(Type)).TryFirst().IsSome(out var Captured)) {
			var Token = new mVM_Data.tProcDef<tPos>(Type) {IsTemplate = true, Compilation = aContext, TypeCaptures = TypeCaptures, SignatureProof = Template.Token.SignatureProof};
			Captured = new() {Token = Token, Source = Template.Source, Captures = Maps, Body = Template.Body};
			aContext.Templates.Push(Captured);
		}
		
		var Target = new mVM_Data.tPreparedClose<tPos> {Code = Captured.Token, Environment = aContext.ViewOwner().Compile(aSite.Environment, Bound.Argument)};
		aSite.Targets = aSite.Targets.Set(aDefinition.CodeId, Target);
		aContext.Progress = true;
		return Target;
	}
	
	private static mVM_Data.tPreparedCall<tPos>? LinkCall<tPos>(this tContext<tPos> aContext, mVM_Data.tCallSite<tPos> aSite, mVM_Data.tProcDef<tPos> aDefinition) {
		if (aSite.Targets.TryGet(aDefinition.CodeId).IsSome(out var Previous)) { return Previous; }
		var Template = aContext.Register(aDefinition);
		var Function = aDefinition.Function();
		var Expected = aSite.SourceSignature;
		var Maps = Template.Captures;
		var Bound = mVM_Type.BindCall(Expected, aSite.ObjectType, aSite.ArgumentType, _ => {}, false).Match(out var Binding, out _) ? Binding.Mappings : Mappings();
		var Inference = Function.ApplyMappings(Maps);
		var Parameters = mArrayList.List<(mVM_Type.tType Original, mVM_Type.tType Fresh)>();
		
		while (Inference.IsGeneric(out var Parameter, out var Inner)) {
			var Fresh = mVM_Type.Free(Parameter.Id!, Parameter.KindType());
			Parameters.Push((Parameter, Fresh));
			Inference = Inner.Substitute(Parameter, Fresh);
		}
		
		foreach (var Parameter in Inference.FreeParameters().ToStream()) {
			if (Parameters.ToStream().Any(__ => ReferenceEquals(__.Fresh, Parameter.Value))) { continue; }
			var Fresh = mVM_Type.Free(Parameter.Value.Id!, Parameter.Value.KindType());
			Parameters.Push((Parameter.Value, Fresh));
			Inference = Inference.Substitute(Parameter.Value, Fresh);
		}
		
		// Package adapters rename existential witnesses. Relate their ABI schemas only
		// for the implementation already proven reachable at this static call site.
		var Seen = new System.Collections.Generic.HashSet<(tNat64, tNat64)>();
		
		void Witnesses(mVM_Type.tType aFormal, mVM_Type.tType aActual) {
			if (!Seen.Add((aFormal.DebugId, aActual.DebugId))) { return; }
			
			if (aFormal.Kind is mVM_Type.tKind.Abstract && aActual.Kind is mVM_Type.tKind.Abstract) {
				Inference = Inference.Substitute(aFormal, aActual);
				return;
			}
			
			if (aFormal.Kind != aActual.Kind) { return; }
			
			if (aFormal.IsRecord(out var Fields)) {
				foreach (var Field in Fields.ToStream()) { if (aActual.Fields.TryGet(Field.Key).IsSome(out var Actual)) { Witnesses(Field.Value, Actual); } }
			} else {
				for (var Index = 0; Index < aFormal.Refs.Length; Index += 1) { Witnesses(aFormal.Refs[Index], aActual.Refs[Index]); }
			}
		}
		
		if (!Expected.IsGeneric(out _, out _)) { Witnesses(Inference, aSite.Signature); }
		
		if (mVM_Type.BindCall(Inference, aSite.Signature.Refs[0], aSite.Signature.Refs[1], _ => {}, false).Match(out var Direct, out _)) {
			foreach (var Parameter in Parameters.ToStream()) { Maps = Maps.Set(Parameter.Original, Parameter.Fresh.ApplyMappings(Direct.Mappings)); }
			while (Function.IsGeneric(out _, out var Body)) { Function = Body; }
			Expected = aSite.Signature;
		}
		
		while (Function.IsGeneric(out var Parameter, out var Inner)) {
			if (!Expected.IsGeneric(out var Other, out var OtherBody)) { return null; }
			Maps = Maps.Set(Parameter, Other.ApplyMappings(Bound));
			Function = Inner;
			Expected = OtherBody;
		}
		
		Function = Function.ApplyMappings(Maps);
		
		var Variables = Function.FreeParameters();
		
		if (Variables.Deep() != 0 && mVM_Type.Proc(mVM_Type.Empty(), Function.Refs[1], Function.Refs[2]).IsSubType(mVM_Type.Proc(mVM_Type.Empty(), aSite.Signature.Refs[1], aSite.Signature.Refs[2]), Maps, false).Match(out var Completed, out _)) {
			foreach (var Mapping in Completed.ToStream()) { if (Variables.TryGet(Mapping.Key.DebugId).IsSome(out _)) { Maps = Maps.Set(Mapping.Key, Mapping.Value); } }
			Function = Function.ApplyMappings(Maps);
		}
		
		if (Expected.IsGeneric(out _, out _) || !Compatible(aSite.Signature.Refs[1], Function.Refs[1]) || !Compatible(Function.Refs[2], aSite.Signature.Refs[2])) { return null; }
		mAssert.IsFalse(mStream.Stream(Function.Refs).Any(__ => HasUnboundData(__)), () => "Unbound data in a reachable specialization:\n" + Function.ToText());
		var Code = aContext.Body(Template, Function, Maps);
		var Target = new mVM_Data.tPreparedCall<tPos> {Code = Code, Environment = Code.Compile(aDefinition.DefType.Refs[1], Code.DefType.Refs[1], true), Object = Code.Compile(aSite.PhysicalObjectType ?? aSite.ObjectType, Function.Refs[0]), Argument = Code.Compile(aSite.ArgumentType, Function.Refs[1]), Result = Code.Compile(Function.Refs[2], aSite.Signature.Refs[2])};
		aSite.Targets = aSite.Targets.Set(aDefinition.CodeId, Target);
		aContext.Progress = true;
		return Target;
	}
	
	private static tFlow<tPos> Observe<tPos>(this tContext<tPos> aContext, mVM_Data.tCallSite<tPos> aSite, tFlow<tPos> aFunction, tFlow<tPos> aObject, tFlow<tPos> aArgument) {
		var Result = new tFlow<tPos>();
		
		foreach (var Definition in aFunction.Definitions.ToStream()) {
			var Closed = aContext.LinkClose(aSite.Closure!, Definition);
			var Environment = aContext.ConvertFlow(Closed.Environment, aArgument);
			Result.Functions.Push((Closed.Code, Closed.Code.TypeCaptures.Length == 0 ? Environment : new() {Second = Environment}));
		}
		
		foreach (var Native in aFunction.NativeDefinitions) { Result.NativeFunctions.Add(Native); }
		
		foreach (var Function in aFunction.Functions.ToStream()) {
			var Target = aContext.LinkCall(aSite, Function.Code);
			if (Target is null) { continue; }
			var Frame = aContext.Frames.ToStream().Where(__ => ReferenceEquals(__.Code, Target.Code)).TryFirst().AssertNotEmpty();
			if (!Frame.Active) { Frame.Active = true; aContext.Progress = true; }
			if (Target.Code.DefType.Refs[1].CarriesCode()) { aContext.Progress |= Join(Frame.Environment, aContext.ConvertFlow(Target.Environment, Function.Environment)); }
			if (Target.Code.DefType.Refs[2].Refs[0].CarriesCode()) { aContext.Progress |= Join(Frame.Object, aContext.ConvertFlow(Target.Object, aObject)); }
			if (Target.Code.DefType.Refs[2].Refs[1].CarriesCode()) { aContext.Progress |= Join(Frame.Argument, aContext.ConvertFlow(Target.Argument, aArgument)); }
			Join(Result, aContext.ConvertFlow(Target.Result, Frame.Result));
		}
		
		var ResultType = aSite.Signature.Refs[2];
		
		foreach (var Native in aFunction.NativeFunctions) {
			if (!aSite.NativeTargets.TryGetValue(Native, out var Target)) {
				var Function = Native.Signature;
				while (Function.IsGeneric(out _, out var Body)) { Function = Body; }
				var Signature = mVM_Type.InferCall(Native.Signature, Function.Refs[0].HasUnboundParameter() ? aSite.ObjectType : Function.Refs[0], aSite.Signature.Refs[1], _ => {}).ElseThrow();
				mAssert.IsTrue(Signature.Result.IsSubType(ResultType, aDescribeError: false).Match(out var Mappings, out _) && Mappings.Deep() == 0, "A native implementation must satisfy its visible result contract.");
				var Owner = aContext.ViewOwner();
				var Object = Signature.Object.FirstObjectType().IsInterface(out _, out _) ? aSite.PhysicalObjectType ?? aSite.ObjectType : Signature.Object;
				Target = (Owner.Compile(aSite.PhysicalObjectType ?? aSite.ObjectType, Object, true), Owner.Compile(aSite.ArgumentType, Signature.Argument), Owner.Compile(Signature.Result, ResultType));
				aSite.NativeTargets.Add(Native, Target);
				aContext.Progress = true;
			}
			
			var NativeResult = Target.Result.Source;
			
			if (NativeResult.CarriesCode()) {
				var Known = aContext.NativeResults.Find(__ => ReferenceEquals(__.Code, Native) && __.Type.SameType(NativeResult));
				
				if (Known.Value is null) {
					Known = (Native, NativeResult, new());
					aContext.NativeResults.Add(Known);
					foreach (var Origin in Native.ResultOrigins) { Join(Known.Value, aContext.FromData((Origin.Normalize(NativeResult, "Ergebnisursprung"), NativeResult))); }
				}
				
				Join(Result, aContext.ConvertFlow(Target.Result, Known.Value));
			}
		}
		
		return Result;
	}
	
	private static void Analyze<tPos>(this tContext<tPos> aContext, tFrame<tPos> aFrame) {
		var Code = aFrame.Code;
		
		if (Code.Entry is not null) {
			var Original = Code.TypeCaptures.Length == 0 ? aFrame.Environment : aFrame.Environment.Second ?? new();
			var Value = aContext.Observe(Code.Calls[0]!, Original, aContext.ConvertFlow(Code.ObjectViews[0]!, aFrame.Object), aContext.ConvertFlow(Code.Views[0]!, aFrame.Argument));
			aContext.Progress |= Join(aFrame.Result, aContext.ConvertFlow(Code.Views[1]!, Value));
			return;
		}
		
		var Regs = aFrame.Registers;
		for (tNat32 Index = 0; Index < Regs.Length; Index += 1) { Regs[Index] ??= new(); }
		aContext.Progress |= Join(Regs[mVM_Data.cEnvReg], Code.TypeCaptures.Length == 0 ? aFrame.Environment : aFrame.Environment.Second ?? new());
		aContext.Progress |= Join(Regs[mVM_Data.cObjReg], aFrame.Object);
		aContext.Progress |= Join(Regs[mVM_Data.cArgReg], Code.EntryArgument is null ? aFrame.Argument : aContext.ConvertFlow(Code.EntryArgument, aFrame.Argument));
		Regs = (tFlow<tPos>[])Regs.Clone();
		
		void Write(tCell<tPos> aCell, tFlow<tPos> aValue, mStream.tStream<tCell<tPos>> aSeen = default) {
			if (aSeen.Any(__ => ReferenceEquals(__, aCell))) { return; }
			aSeen = mStream.Stream(aCell, aSeen);
			if (aCell.Writes.IsEmpty) { aContext.Progress |= Join(aCell.Slot, aValue); aContext.Progress |= Join(aCell.Value, aCell.Slot); }
			foreach (var Edge in aCell.Writes.ToStream()) { Write(Edge.Cell, aContext.ConvertFlow(Edge.Conversion, aValue), aSeen); }
		}
		
		tNat32 Next = mVM_Data.cResReg + 1;
		
		for (tNat32 Index = 0; Index < Code.Commands.Size; Index += 1) {
			var (Operation, A, B, _) = Code.Commands.Get(Index);
			var (FirstType, SecondType, ResultType) = Code.OperandTypes.Get(Index);
			var First = A < Regs.Length ? Regs[A] : new tFlow<tPos>();
			var Second = B < Regs.Length ? Regs[B] : new tFlow<tPos>();
			var Value = new tFlow<tPos>();
			
			switch (Operation) {
				case mVM_Data.tOpCode.NewPair: Value.First = First;
				Value.Second = Second;
				break;
				case mVM_Data.tOpCode.First:
				case mVM_Data.tOpCode.Second:
				Value = Code.Views[Index] is not null ? aContext.ConvertFlow(Code.Views[Index]!, First) : (Operation is mVM_Data.tOpCode.First ? First.First : First.Second) ?? Value;
				break;
				case mVM_Data.tOpCode.AddPrefix: Value.Second = Second;
				break;
				case mVM_Data.tOpCode.DelPrefix: Value = Second.Second ?? Value;
				break;
				case mVM_Data.tOpCode.AddField:
				case mVM_Data.tOpCode.SetField: Value.Fields = First.Fields.Set(SecondType.Prefix!.PrefixHash(), Second.Second ?? new());
				break;
				case mVM_Data.tOpCode.GetField: Value = First.Fields.TryGet(B).ElseUse(Value);
				break;
				case mVM_Data.tOpCode.VarDef:
				if (Regs[Next].Cells.IsEmpty) { var Cell = new tCell<tPos>(First, ResultType!); Cell.Views.Push(Cell); Join(Cell.Value, First); Regs[Next].Cells.Push(Cell); aContext.Progress = true; }
				break;
				case mVM_Data.tOpCode.VarGet:
				foreach (var Cell in First.Cells.ToStream()) { aContext.Progress |= Join(Cell.Value, Cell.Slot); Join(Value, Cell.Value); }
				break;
				case mVM_Data.tOpCode.VarSet:
				foreach (var Cell in First.Cells.ToStream()) { Write(Cell, aContext.ConvertFlow(Code.Views[Index]!, Second)); }
				break;
				case mVM_Data.tOpCode.WithType: Value = aContext.ConvertFlow(Code.Views[Index]!, Second);
				break;
				case mVM_Data.tOpCode.Narrow:
				aFrame.Refined[Index] ??= new();
				aContext.Progress |= Join(aFrame.Refined[Index]!, aContext.ConvertFlow(Code.Views[Index]!, First));
				Regs[A] = aFrame.Refined[Index]!;
				break;
				case mVM_Data.tOpCode.CallFunc:
				case mVM_Data.tOpCode.CallProc:
				case mVM_Data.tOpCode.DefRecProcs: {
					var Function = First;
					var Object = new tFlow<tPos>();
					
					if (Operation is mVM_Data.tOpCode.CallProc) {
						Object = First.First ?? new();
						Function = First.Second ?? new();
						
						if (FirstType.Refs[1].IsPrefix(out var Name, out var Marker) && Marker.IsEmpty()) {
							static (tFlow<tPos>, tFlow<tPos>) Method(tFlow<tPos> aValue, mVM_Type.tType aType, tNat32 aName) {
								if (aType.IsInterface(out _, out _)) { return (aValue.First ?? new(), aValue.Second?.Fields.TryGet(aName).ElseUse(new tFlow<tPos>()) ?? new()); }
								var Singleton = aType.Refs[0].IsEmpty();
								var Bound = Method((Singleton ? aValue.Second : aValue.First) ?? new(), Singleton ? aType.Refs[1] : aType.Refs[0], aName);
								return (Singleton ? new() {First = aValue.First, Second = Bound.Item1} : new() {First = Bound.Item1, Second = aValue.Second}, Bound.Item2);
							}
							
							(Object, Function) = Method(Object, FirstType.Refs[0], Name.PrefixHash());
						}
					}
					
					Join(Value, aContext.Observe(Code.Calls[Index]!, Function, Object, Second));
					
					if (Operation is mVM_Data.tOpCode.DefRecProcs) {
						Join(Regs[Next], aContext.Observe(Code.RecursiveCalls[Index]!, Value, new(), Regs[Next]));
						Value = Regs[Next];
					}
					
					break;
				}
				case mVM_Data.tOpCode.TryReturn: {
					var Function = FirstType.IsPair(out _, out _) ? First.First ?? new() : First;
					if (Code.Guards[Index] is not null) { aContext.Observe(Code.Guards[Index]!, First.Second ?? new(), aFrame.Object, Second); }
					Value = aContext.ConvertFlow(Code.Views[Index]!, aContext.Observe(Code.Calls[Index]!, Function, aFrame.Object, Second));
					break;
				}
				case mVM_Data.tOpCode.ReturnIf:
				case mVM_Data.tOpCode.ReturnIfNotEmpty:
				if (Code.Views[Index] is not null) { aContext.Progress |= Join(aFrame.Result, aContext.ConvertFlow(Code.Views[Index]!, Operation is mVM_Data.tOpCode.ReturnIf ? Second : First)); }
				break;
				case mVM_Data.tOpCode.NewSig: Value.First = Second.First;
				Value.Second = aContext.ConvertFlow(Code.Views[Index]!, Second.Second ?? new());
				break;
				case mVM_Data.tOpCode.NewInterface: Value.First = aContext.ConvertFlow(Code.ObjectViews[Index]!, Second.First ?? new());
				Value.Second = aContext.ConvertFlow(Code.Views[Index]!, Second.Second ?? new());
				break;
				case mVM_Data.tOpCode.SigBody: Value = aContext.ConvertFlow(Code.Views[Index]!, First.Second ?? new());
				break;
				case mVM_Data.tOpCode.TryRemovePrefixFrom: Value = aContext.ConvertFlow(Code.Views[Index]!, Second).Second ?? new();
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
				case mVM_Data.tOpCode.TryHasHeadType:
				if (Code.Views[Index] is null) { return; }
				Value = aContext.ConvertFlow(Code.Views[Index]!, First);
				break;
			}
			
			if (ResultType is not null) { if (ResultType.CarriesCode()) { aContext.Progress |= Join(Regs[Next], Value); } Next += 1; }
			if (Operation is mVM_Data.tOpCode.ReturnIf && FirstType.Kind is mVM_Type.tKind.True) { break; }
		}
	}
	
	public static void Complete<tPos>(this tContext<tPos> aContext) {
		mAssert.IsFalse(aContext.Started, "Compilation must finish before VM execution.");
		var Prepared = new System.Collections.Generic.Dictionary<mVM_Data.tConversion, tNat32>();
		
		do {
			aContext.Progress = false;
			for (tNat32 Index = 0; Index < aContext.Frames.Size; Index += 1) { var Frame = aContext.Frames.Get(Index); if (Frame.Active) { aContext.Analyze(Frame); } }
			
			for (tNat32 Index = 0; Index < aContext.Calls.Size; Index += 1) {
				var Site = aContext.Calls.Get(Index);
				
				if (Site.NativeArgument is null) {
					var Owner = aContext.ViewOwner();
					Site.NativeArgument = Owner.Compile(Site.ArgumentType, Site.Signature.Refs[1]);
					Site.NativeObject = Owner.Compile(Site.PhysicalObjectType ?? Site.ObjectType, Site.PhysicalObjectType ?? Site.Signature.Refs[0]);
					aContext.Progress = true;
				}
			}
			
			for (tNat32 Index = 0; Index < aContext.Views.Size; Index += 1) {
				var View = aContext.Views.Get(Index);
				if (!View.Dynamic) { continue; }
				var Target = View.Target.ResolveWitnesses(View.Proof, true);
				Prepared.TryGetValue(View, out var FirstType);
				
				for (var TypeIndex = FirstType; TypeIndex < aContext.ActualTypes.Size; TypeIndex += 1) {
					var Type = aContext.ActualTypes.Get(TypeIndex);
					if (View.Alternatives.ToStream().Any(__ => __.Type.SameType(Type)) || !mVM_Type.CanMatch(Type, View.Source)) { continue; }
					
					var Reified = View.Source.IsAny() ? Type : Type.ResolveWitnesses(View.Proof, false);
					
					var Compatible = View.Projection == -1
					? mVM_Type.CanMatch(Reified, Target)
					: Type.TryProjectPair(out var First, out var Second) && mVM_Type.CanMatch(View.Projection == 0 ? First : Second, View.Target);
					
					if (Compatible) {
						var Conversion = View.Projection == -1 ? aContext.ViewOwner().Compile(Reified, View.Target, View.RevealOpaque, View.Proof) : aContext.ViewOwner().Project(Type, View.Target, View.Projection);
						if (View.Projection == -1 && !Type.SameType(Reified)) { Conversion = mVM_View.Sequence(aContext.ViewOwner().Compile(Type, Reified, true, View.Proof), Conversion); }
						View.Alternatives.Push((Type, Conversion));
						aContext.Progress = true;
					}
				}
				
				Prepared[View] = aContext.ActualTypes.Size;
			}
		} while (aContext.Progress);
	}
	
	public static void ReleaseNativeInputs<tPos>(this tContext<tPos> aContext) {
		foreach (var Flow in aContext.NativeInputs) { Flow.Native.Clear(); }
		aContext.NativeInputs.Clear();
	}
	
	public static void PrepareInvocation<tPos>(this tContext<tPos> aContext, mVM_Data.tCallSite<tPos> aSite, mVM_Data.tData aFunction, mVM_Data.tData aObject, mVM_Data.tData aArgument) {
		mAssert.IsFalse(aContext.Started, "Native call signatures must be linked before VM execution.");
		aContext.Progress = false;
		aContext.Observe(aSite, aContext.FromData((aFunction, aSite.SourceSignature)), aContext.FromData((aObject, aSite.ObjectType)), aContext.FromData((aArgument, aSite.ArgumentType)));
		
		if (aContext.Progress) { aContext.Complete(); }
		aContext.ReleaseNativeInputs();
	}
	
	public static void PrepareNativeCall<tPos>(
		(mVM_Data.tData Data, mVM_Type.tType Type) aFunction,
		(mVM_Data.tData Data, mVM_Type.tType Type) aObject,
		(mVM_Data.tData Data, mVM_Type.tType Type) aArgument
	) {
		mAssert.IsTrue(aFunction.Data.IsProc<tPos>(out var Definition, out _), "A native entry requires a declared SPO implementation.");
		var Compiler = Definition.Compilation as tContext<tPos> ?? new();
		mAssert.IsFalse(Compiler.Started, "Native call signatures must be linked before VM execution.");
		Compiler.Register(Definition);
		var Site = Compiler.Call(aFunction.Type, aObject.Type, aArgument.Type);
		Compiler.PrepareInvocation(Site, aFunction.Data, aObject.Data.Normalize(aObject.Type, "Empfänger"), aArgument.Data.Normalize(aArgument.Type));
	}
	
}