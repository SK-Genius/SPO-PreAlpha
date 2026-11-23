#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include _GlobalUsings.cs
#:ref mVM_Data.cs

public static class
mVM_Visible {
	public static tText
	ToVisibleText(
		this (mVM_Data.tData Data, mVM_Type.tType Type) aValue,
		tInt32 aLimit = 20
	) {
		static tText
		Format(
			(mVM_Data.tData Data, mVM_Type.tType Type) aCurrent,
			tInt32 aRemaining,
			tBool aAlternatives = false
		) {
			if (aRemaining <= 0) {
				return "...";
			}
			
			if (aCurrent.Type.IsRecursive(out var Head, out var Body)) {
				return Format((aCurrent.Data, Body.Substitute(Head, aCurrent.Type)), aRemaining - 1);
			}
			
			if (!aAlternatives && aCurrent.Type.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) {
				var Dynamic = aCurrent.Data._Value.To<mVM_Data.tDynamicValue>();
				return Format((Dynamic.Data, Dynamic.ActualType), aRemaining, true);
			}
			
			switch (aCurrent.Type.Kind) {
				case mVM_Type.tKind.Empty: return "()";
				case mVM_Type.tKind.True: return "§TRUE";
				case mVM_Type.tKind.False: return "§FALSE";
				case mVM_Type.tKind.Int: return aCurrent.Data._Value.To<tInt32>().ToString();
				case mVM_Type.tKind.Type: return aCurrent.Data._Value.To<mVM_Type.tType>().ToText();
				case mVM_Type.tKind.Abstract:
				case mVM_Type.tKind.Opaque:
				case mVM_Type.tKind.TypeApply:
				case mVM_Type.tKind.Free:
				return "<opaque>";
				case mVM_Type.tKind.Proc:
				case mVM_Type.tKind.Generic:
				return "<function>";
				case mVM_Type.tKind.Interface: return "<interface>";
				case mVM_Type.tKind.Record: {
					var Text = "{";
					var Separator = "";
					
					foreach (var Field in aCurrent.Type.Fields.ToStream()) {
						Text += Separator + Field.Key + ": " + Format(
							(aCurrent.Data.GetField(Field.Key.PrefixHash()), Field.Value),
							aRemaining - 1
						);
						
						Separator = ", ";
					}
					
					return Text + "}";
				}
				case mVM_Type.tKind.Pair: {
					var Pair = aCurrent.Data._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
					
					return "(" + Format((Pair.Item1, aCurrent.Type.Refs[0]), aRemaining - 1) + "; " +
					Format((Pair.Item2, aCurrent.Type.Refs[1]), aRemaining - 1) + ")";
				}
				case mVM_Type.tKind.Prefix: {
					var Prefix = aCurrent.Data._Value.To<(tNat32, mVM_Data.tData)>();
					
					return "(#" + aCurrent.Type.Prefix + " " +
					Format((Prefix.Item2, aCurrent.Type.Refs[0]), aRemaining - 1) + ")";
				}
				case mVM_Type.tKind.Set: {
					return "[" + new mVM_Data.tDynamicValue(aCurrent.Type, aCurrent.Data).Alternatives().Map(
						__ => Format(__, aRemaining - 1, true)
					).Join((a1, a2) => a1 + " | " + a2, "") + "]";
				}
				case mVM_Type.tKind.Sig: {
					var Sig = aCurrent.Data._Value.To<mVM_Data.tSig>();
					var Binder = aCurrent.Type.Refs[0];
					var Witness = Binder.IsFree() ? mVM_Type.Abstract("head", Binder.KindType()) : Binder;
					
					return "(§SIG " + Sig.Head.TypeExpressionValue().ToText() + " IN " +
					Format((Sig.Body, aCurrent.Type.Refs[1].Substitute(Binder, Witness)), aRemaining - 1) + ")";
				}
				case mVM_Type.tKind.Var: {
					return "(§VAR " + Format((aCurrent.Data.ReadCell(), aCurrent.Type.Refs[0]), aRemaining - 1) + ")";
				}
				default:
				throw mError.Error($"Value formatting is not defined for {aCurrent.Type.Kind}.");
			}
		}
		
		return Format(aValue, aLimit);
	}
	
	public static tBool
	SameVisibleValue(
		this (mVM_Data.tData Data, mVM_Type.tType Type) aLeft,
		(mVM_Data.tData Data, mVM_Type.tType Type) aRight
	) => Compare(aLeft, aRight, mTreeMap.Tree<(tNat64, tNat64, tNat64, tNat64), tBool>((a1, a2) => a1.CompareTo(a2), []), new());
	
	private static tBool Compare(
		(mVM_Data.tData Data, mVM_Type.tType Type) aLeft,
		(mVM_Data.tData Data, mVM_Type.tType Type) aRight,
		mTreeMap.tTree<(tNat64, tNat64, tNat64, tNat64), tBool> aVisited,
		System.Collections.Generic.Dictionary<tNat64, mVM_Type.tType> aExpanded
	) {
		var Pending = mArrayList.List((Left: aLeft, Right: aRight, Alternatives: false));
		var Visited = aVisited;
		
		mVM_Type.tType Expand(mVM_Type.tType aType) {
			if (!aType.IsRecursive(out var Head, out var Body)) { return aType; }
			if (!aExpanded.TryGetValue(aType.DebugId, out var Shape)) { Shape = Body.Substitute(Head, aType); aExpanded.Add(aType.DebugId, Shape); }
			return Shape;
		}
		
		while (!Pending.IsEmpty) {
			var (Left, Right, Alternatives) = Pending.Pop();
			
			Left.Type = Expand(Left.Type);
			Right.Type = Expand(Right.Type);
			
			if (!Alternatives) {
				if (Left.Type.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) {
					var Dynamic = Left.Data._Value.To<mVM_Data.tDynamicValue>();
					Left = (Dynamic.Data, Dynamic.ActualType);
				}
				
				if (Right.Type.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) {
					var Dynamic = Right.Data._Value.To<mVM_Data.tDynamicValue>();
					Right = (Dynamic.Data, Dynamic.ActualType);
				}
			}
			
			if (Left.Type.Kind is mVM_Type.tKind.Set || Right.Type.Kind is mVM_Type.tKind.Set) {
				var LeftViews = new mVM_Data.tDynamicValue(Left.Type, Left.Data).Alternatives();
				var RightViews = new mVM_Data.tDynamicValue(Right.Type, Right.Data).Alternatives();
				
				if (
					!LeftViews.All(aView => RightViews.Any(aOther => Compare(aView, aOther, Visited, aExpanded))) ||
					!RightViews.All(aView => LeftViews.Any(aOther => Compare(aOther, aView, Visited, aExpanded)))
				) { return false; }
				
				continue;
			}
			
			if (Left.Type.Kind != Right.Type.Kind) {
				return false;
			}
			
			if (Left.Type.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply or mVM_Type.tKind.Free or mVM_Type.tKind.Opaque) {
				if (!Left.Type.SameType(Right.Type)) {
					return false;
				}
				
				// The representation of an opaque type is not observable, including its identity.
				continue;
			}
			
			var Key = (Left.Data._DebugId, Left.Type.DebugId, Right.Data._DebugId, Right.Type.DebugId);
			
			if (Visited.TryGet(Key).IsSome(out _)) {
				continue;
			}
			
			Visited = Visited.Set(Key, true);
			
			switch (Left.Type.Kind) {
				case mVM_Type.tKind.Empty:
				case mVM_Type.tKind.True:
				case mVM_Type.tKind.False:
				break;
				case mVM_Type.tKind.Int: {
					if (Left.Data._Value.To<tInt32>() != Right.Data._Value.To<tInt32>()) {
						return false;
					}
					
					break;
				}
				case mVM_Type.tKind.Type: {
					if (!Left.Data._Value.To<mVM_Type.tType>().SameType(Right.Data._Value.To<mVM_Type.tType>())) {
						return false;
					}
					
					break;
				}
				case mVM_Type.tKind.Record: {
					var LeftFields = Left.Type.Fields;
					var RightFields = Right.Type.Fields;
					
					if (LeftFields.ToStream().Count() != RightFields.ToStream().Count()) {
						return false;
					}
					
					foreach (var Field in LeftFields.ToStream()) {
						if (!RightFields.TryGet(Field.Key).IsSome(out var RightType)) {
							return false;
						}
						
						Pending.Push(
							(
								(Left.Data.GetField(Field.Key.PrefixHash()), Field.Value),
								(Right.Data.GetField(Field.Key.PrefixHash()), RightType),
								false
							)
						);
					}
					
					break;
				}
				case mVM_Type.tKind.Pair: {
					var LeftPair = Left.Data._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
					var RightPair = Right.Data._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
					Pending.Push(((LeftPair.Item1, Left.Type.Refs[0]), (RightPair.Item1, Right.Type.Refs[0]), false));
					Pending.Push(((LeftPair.Item2, Left.Type.Refs[1]), (RightPair.Item2, Right.Type.Refs[1]), false));
					break;
				}
				case mVM_Type.tKind.Prefix: {
					if (Left.Type.Prefix != Right.Type.Prefix) {
						return false;
					}
					
					var LeftPrefix = Left.Data._Value.To<(tNat32, mVM_Data.tData)>();
					var RightPrefix = Right.Data._Value.To<(tNat32, mVM_Data.tData)>();
					Pending.Push(((LeftPrefix.Item2, Left.Type.Refs[0]), (RightPrefix.Item2, Right.Type.Refs[0]), false));
					break;
				}
				case mVM_Type.tKind.Proc:
				case mVM_Type.tKind.Generic:
				case mVM_Type.tKind.Interface:
				if (!Left.Type.SameType(Right.Type)) { return false; }
				break;
				case mVM_Type.tKind.Sig: {
					var LeftSig = Left.Data._Value.To<mVM_Data.tSig>();
					var RightSig = Right.Data._Value.To<mVM_Data.tSig>();
					if (!LeftSig.Head.TypeExpressionValue().SameType(RightSig.Head.TypeExpressionValue())) { return false; }
					var Witness = mVM_Type.Abstract("head", Left.Type.Refs[0].KindType());
					Pending.Push(((LeftSig.Body, Left.Type.Refs[1].Substitute(Left.Type.Refs[0], Witness)), (RightSig.Body, Right.Type.Refs[1].Substitute(Right.Type.Refs[0], Witness)), false));
					break;
				}
				case mVM_Type.tKind.Var:
				case mVM_Type.tKind.Ref: {
					Pending.Push(((Left.Data.ReadCell(), Left.Type.Refs[0]), (Right.Data.ReadCell(), Right.Type.Refs[0]), false));
					break;
				}
				default:
				throw mError.Error($"Value comparison is not defined for {Left.Type.Kind}.");
			}
		}
		
		return true;
	}
}