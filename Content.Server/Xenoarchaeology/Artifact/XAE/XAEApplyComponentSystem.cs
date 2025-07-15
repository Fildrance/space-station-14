using Content.Server.Power.Components;
using Content.Server.Spawners.Components;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.EntityTable.ValueSelector;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;

public sealed class XAEApplyComponentSystem : SharedXAEApplyComponentsSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

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
        if (modifications.TryGetValue(XAEApplyComponentsComponent.XenoArtifactEntityTableSpawnerEffectModifier.SpawnCountChange,
                out var spawnCountChange))
        {
            if (tableSpawner.Table is EntSelector entSelector)
            {
                var newValue = entSelector.Amount.Get(_random.GetRandom()) + spawnCountChange;
                entSelector.Amount = new ConstantNumberSelector((int)newValue);
            }

        }

        return false;
    }

    private bool TryApplyModifiersFor(PowerSupplierComponent powerSupplier, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue(XAEApplyComponentsComponent.XenoArtifactPowerSupplierEffectModifier.Effectiveness, out var effectiveness))
        {
            powerSupplier.MaxSupply *= effectiveness;
            return true;
        }

        return false;
    }
}
