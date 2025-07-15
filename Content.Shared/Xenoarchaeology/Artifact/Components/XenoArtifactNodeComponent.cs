using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// Component for holding artifact info that is related to amplification:
/// <para/> - budget range (min and max budget for which this node can fit
/// <para/> - list of amplification effects which are applicable to node
/// </summary>
[RegisterComponent, Access(typeof(SharedXenoArtifactSystem))]
public sealed partial class XenoArtifactNodeBudgetComponent : Component
{
    [DataField(required: true)]
    public MinMax BudgetRange;

    [DataField]
    public XenoArtifactEffectsModifications ModifyBy = new ();
}

[DataDefinition]
public sealed partial class XenoArtifactEffectsModifications
{
    [DataField]
    public Dictionary<Enum, float> Dictionary = new();
     
    public bool IsEmpty => Dictionary.Count <= 0;

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
                result.Dictionary[modKey] = modValue + otherValue;
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

    /// <summary>
    /// Creates new dictionary, keys and values are copied from original,
    /// but they are divided provided float when possible.
    /// </summary>
    public static XenoArtifactEffectsModifications operator /(XenoArtifactEffectsModifications original, float c)
    {
        var newEffects = new XenoArtifactEffectsModifications();
        var newDict = newEffects.Dictionary;

        foreach (var (key, value) in original.Dictionary)
        {
            newDict[key] = value / c;
        }

        return newEffects;
    }

    /// <summary>
    /// Creates new dictionary, keys and values are copied from original,
    /// but they are divided provided float when possible.
    /// </summary>
    public static XenoArtifactEffectsModifications operator *(XenoArtifactEffectsModifications original, float c)
    {
        var newEffects = new XenoArtifactEffectsModifications();
        var newDict = newEffects.Dictionary;

        foreach (var (key, value) in original.Dictionary)
        {
            newDict[key] = value * c;
        }

        return newEffects;
    }

    /// <inheritdoc cref="Dictionary{TKey,TValue}.TryGetValue"/>>
    public bool TryGetValue(Enum key, out float value)
    {
        return Dictionary.TryGetValue(key, out value);
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

    [DataField, AutoNetworkedField]
    public int? EffectMultiplier;

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

    /// <summary>
    /// The variance from MaxDurability present when a node is created.
    /// </summary>
    [DataField]
    public MinMax MaxDurabilityCanDecreaseBy = new(0, 2);
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
