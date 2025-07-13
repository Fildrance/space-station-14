using Content.Server.Power.Components;
using Content.Server.Spawners.Components;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEApplyComponentSystem : SharedXAEApplyComponentsSystem
{
    protected override bool TryApplyModifiers(IComponent component, XenoArtifactEffectsModifications modifications)
    {
        return component switch
        {
            PowerSupplierComponent powerSupplier => TryApplyModifiersFor(powerSupplier, modifications),
            EntityTableSpawnerComponent spawner => TryApplyModifiersFor(spawner, modifications),
            _ => base.TryApplyModifiers(component, modifications)
        };
    }

    private bool TryApplyModifiersFor(EntityTableSpawnerComponent tableSpawner, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue<int>(XenoArtifactEntityTableSpawnerEffectModifier.SpawnCountChange, out var spawnCountChange))
        {
            switch (tableSpawner.Table)
            {
                case AllSelector allSelector:

                    return true;
                case EntSelector entSelector:

                    return true;
                case GroupSelector groupSelector:
                    groupSelector.;
                    return true;
                case NestedSelector nestedSelector:
                    nestedSelector.
                    return true;
                case NoneSelector noneSelector:
                    return true;
                default:
                    return false;
            }

            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(PowerSupplierComponent powerSupplier, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue<float>(XenoArtifactPowerSupplierEffectModifier.Effectiveness, out var effectiveness))
        {
            powerSupplier.MaxSupply *= effectiveness;
            return true;
        }

        return false;
    }
}

public enum XenoArtifactEntityTableSpawnerEffectModifier
{
    SpawnCountChange
}

public enum XenoArtifactPowerSupplierEffectModifier
{
    Effectiveness
}
