using System.Diagnostics.CodeAnalysis;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

[RegisterComponent, Access(typeof(SharedXenoArtifactSystem))]
public sealed partial class XenoArtifactNodeBudgetComponent : Component
{
    [DataField(required: true)]
    public MinMax BudgetRange;

    [DataField]
    public XenoArtifactAmplificationEffects AmplifyBy = new ();
}

public sealed class XenoArtifactAmplificationEffects : Dictionary<Enum, object>
{
    public static XenoArtifactAmplificationEffects operator /(XenoArtifactAmplificationEffects original, float c)
    {
        var newOne = new XenoArtifactAmplificationEffects();

        foreach (var (key, value) in original)
        {
            if(value == null)
                continue;

            newOne[key] = value switch
            {
                int intVal => (int)intVal / c,
                float floatVal => floatVal / c,
                double doubleVal => (double)doubleVal / c,
                Vector2d vec2dVal => new Vector2d(vec2dVal.X / c, vec2dVal.Y / c),
                Vector2i vec2iVal => new Vector2i((int)(vec2iVal.X / c), (int)(vec2iVal.Y / c)),
                _ => newOne[key]
            };
        }

        return newOne;
    }

    public static XenoArtifactAmplificationEffects operator *(XenoArtifactAmplificationEffects original, float c)
    {
        var newOne = new XenoArtifactAmplificationEffects();

        foreach (var (key, value) in original)
        {
            if (value == null)
                continue;

            newOne[key] = value switch
            {
                int intVal => (int)intVal * c,
                float floatVal => floatVal * c,
                double doubleVal => (double)doubleVal * c,
                Vector2d vec2dVal => new Vector2d(vec2dVal.X * c, vec2dVal.Y * c),
                Vector2i vec2iVal => new Vector2i((int)(vec2iVal.X * c), (int)(vec2iVal.Y * c)),
                _ => newOne[key]
            };
        }

        return newOne;
    }

    public bool TryGetValue<T>(Enum key,  [NotNullWhen(true)] out T? value)
    {
        if (TryGetValue(key, out var val) && val is T returnValue)
        {
            value = returnValue;
            return true;
        }

        value = default;
        return false;
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
