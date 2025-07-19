using System.Diagnostics.CodeAnalysis;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Xenoarchaeology.Artifact.Modifiers;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// Component for holding artifact info that is related to amplification:
/// <para/> - budget range (min and max budget for which this node can fit
/// <para/> - list of amplification effects which are applicable to node
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedXenoArtifactSystem))]
public sealed partial class XenoArtifactNodeBudgetComponent : Component
{
    [DataField(required: true)]
    public MinMax BudgetRange;

    [DataField]
    public float PlacementInBudgetRange;

    [DataField, AutoNetworkedField]
    public XenoArtifactEffectsModifications ModifyBy = new ();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class XenoArtifactEffectsModifications
{
    private static readonly PlacementBudgetDistributionStrategyBase[] BudgetDistributionStrategies =
    [
        new AllInOnePlacementBudgetDistributionStrategy(),
        new NormalPlacementBudgetDistributionStrategy(),
        new OffsettingPlacementBudgetDistributionStrategy()
    ];

    [DataField]
    public Dictionary<Enum, ModifierProviderBase> Dictionary = new();

    public bool IsEmpty => Dictionary.Count <= 0;

    /// <inheritdoc cref="Dictionary{TKey,TValue}.TryGetValue"/>>
    public bool TryGetValue(Enum key, [NotNullWhen(true)] out ModifierProviderBase? value)
    {
        return Dictionary.TryGetValue(key, out value);
    }

    /// <summary>
    /// Produces new dictionary with values from both original and other, using sum of both
    /// when keys exists in both dictionaries and values are compatible.
    /// </summary>
    public static XenoArtifactEffectsModifications operator +(
        XenoArtifactEffectsModifications original,
        XenoArtifactEffectsModifications other
    )
    {
        var result = new XenoArtifactEffectsModifications();
        ValueList<Enum> alreadyMatched = new();
        foreach (var (modKey, modValue) in original.Dictionary)
        {
            if (other.Dictionary.TryGetValue(modKey, out var otherValue))
            {
                result.Dictionary[modKey] = new Modifiers.WrapperModifierProvider([modValue, otherValue]);
                alreadyMatched.Add(modKey);
            }
            else
            {
                result.Dictionary[modKey] = modValue;
            }
        }

        foreach (var (modKey, modValue) in other.Dictionary)
        {
            if (alreadyMatched.Contains(modKey))
                continue;

            result.Dictionary[modKey] = modValue;
        }

        return result;
    }

    public void ApplyActualBudgetPlacement(float placementInBudgetRange, IRobustRandom random)
    {
        var keys = Dictionary.Keys;

        var pickedStrategy = random.Pick(BudgetDistributionStrategies);
        var distribution = pickedStrategy.Distribute(placementInBudgetRange, keys, random);

        foreach (var (key, provider) in Dictionary)
        {
            if (provider is IBudgetPlacementAwareModifier budgetPlacementAware && distribution.TryGetValue(key, out var share))
            {
                budgetPlacementAware.PlacementInBudget = share;
            }
        }
    }
}

public abstract class PlacementBudgetDistributionStrategyBase
{
    public abstract Dictionary<Enum, float> Distribute(float placementInBudget, IReadOnlyCollection<Enum> modifiers, IRobustRandom random);
}

public sealed class AllInOnePlacementBudgetDistributionStrategy : PlacementBudgetDistributionStrategyBase
{
    /// <inheritdoc />
    public override Dictionary<Enum, float> Distribute(float placementInBudget, IReadOnlyCollection<Enum> modifiers, IRobustRandom random)
    {
        var selected = random.Pick(modifiers);
        var result = new Dictionary<Enum, float>();
        foreach (var mod in modifiers)
        {
            var share = Equals(mod, selected)
                ? placementInBudget
                : 0;

            result.Add(mod, share);
        }

        return result;
    }
}

public sealed class NormalPlacementBudgetDistributionStrategy : PlacementBudgetDistributionStrategyBase
{
    /// <inheritdoc />
    public override Dictionary<Enum, float> Distribute(float placementInBudget, IReadOnlyCollection<Enum> modifiers, IRobustRandom random)
    {
        var fairShare = placementInBudget / modifiers.Count;
        var result = new Dictionary<Enum, float>();
        foreach (var mod in modifiers)
        {
            result.Add(mod, fairShare);
        }

        return result;
    }
}

public sealed class OffsettingPlacementBudgetDistributionStrategy : PlacementBudgetDistributionStrategyBase
{
    /// <inheritdoc />
    public override Dictionary<Enum, float> Distribute(float placementInBudget, IReadOnlyCollection<Enum> modifiers, IRobustRandom random)
    {
        var selected = random.Pick(modifiers);
        var mostPoints = placementInBudget * 0.7f;
        var othersShare = (placementInBudget * 0.3f) / (modifiers.Count - 1);
        var result = new Dictionary<Enum, float>();
        foreach (var mod in modifiers)
        {
            var share = Equals(mod, selected)
                ? mostPoints
                : othersShare;
            result.Add(mod, share);
        }

        return result;
    }
}


/// <summary>
/// Stores metadata about a particular artifact node
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedXenoArtifactSystem)), AutoGenerateComponentState]
public sealed partial class XenoArtifactNodeComponent : Component
{
    /// <summary>
    /// Depth within the graph generation.
    /// Used for sorting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Depth;

    /// <summary>
    /// Denotes whether an artifact node has been activated at least once (through the required triggers).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked = true;

    /// <summary>
    /// List of trigger descriptions that this node require for activation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? TriggerTip;

    /// <summary>
    /// The entity whose graph this node is a part of.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? Attached;

    [DataField, AutoNetworkedField]
    public int Budget;
    
    #region Durability
    /// <summary>
    /// Marker, is durability of node degraded or not.
    /// </summary>
    public bool Degraded => Durability <= 0;

    /// <summary>
    /// The amount of generic activations a node has left before becoming fully degraded and useless.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Durability;

    /// <summary>
    /// The maximum amount of times a node can be generically activated before becoming useless
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxDurability = 5;
    #endregion

    #region Research
    /// <summary>
    /// The amount of points a node is worth with no scaling
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BasePointValue = 4000;

    /// <summary>
    /// Amount of points available currently for extracting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ResearchValue;

    /// <summary>
    /// Amount of points already extracted from node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ConsumedResearchValue;
    #endregion
}
