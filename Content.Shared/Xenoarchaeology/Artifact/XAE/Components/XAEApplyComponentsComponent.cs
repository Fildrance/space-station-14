using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Applies components when effect is activated.
/// </summary>
[RegisterComponent, Access(typeof(SharedXAEApplyComponentsSystem))]
public sealed partial class XAEApplyComponentsComponent : Component
{
    /// <summary>
    /// Components that are permanently added to an entity when the effect's node is entered.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Does adding components need to be done only on first activation.
    /// </summary>
    [DataField]
    public bool ApplyIfAlreadyHave { get; set; }

    /// <summary>
    /// Does component need to be restored when activated 2nd or more times.
    /// </summary>
    [DataField]
    public bool RefreshOnReactivate { get; set; }
}

public enum XenoArtifactMeleeWeaponEffectModifier
{
    Damage,
    AttackRate
}

public enum XenoArtifactAmmoSourceEffectModifier
{
    CapacityChange
}

public enum XenoArtifactRadiationSourceEffectModifier
{
    Effectiveness
}

public enum XenoArtifactToolEffectModifier
{
    Effectiveness
}

public enum XenoArtifactStealthEffectModifier
{
    Effectiveness
}

public enum XenoArtifactStorageEffectModifier
{
    WidthChange,
    HeightChange,
}

public enum XenoArtifactHeldSpeedModifierEffectModifier
{
    Multiplier
}

public enum XenoArtifactSolutionStorageEffectModifier
{
    VolumeChange
}


[Serializable, NetSerializable]
public enum XenoArtifactEntityTableSpawnerEffectModifier
{
    SpawnCountChange
}

public enum XenoArtifactPowerSupplierEffectModifier
{
    Effectiveness
}
