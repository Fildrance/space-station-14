using Content.Server.Power.Components;
using Content.Server.Spawners.Components;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.EntityTable.ValueSelector;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;

/// <inheritdoc />
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
        if (modifications.TryGetValue(XenoArtifactEntityTableSpawnerEffectModifier.SpawnCountChange,
                out var spawnCountModifier))
        {
            if (tableSpawner.Table is EntSelector entSelector)
            {
                var newValue = spawnCountModifier.Modify(entSelector.Amount.Get(_random.GetRandom()));
                entSelector.Amount = new ConstantNumberSelector((int)newValue);
            }

        }

        return false;
    }

    private bool TryApplyModifiersFor(PowerSupplierComponent powerSupplier, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue(XenoArtifactPowerSupplierEffectModifier.Effectiveness, out var effectivenessModifier))
        {
            powerSupplier.MaxSupply = effectivenessModifier.Modify(powerSupplier.MaxSupply);
            return true;
        }

        return false;
    }
}
