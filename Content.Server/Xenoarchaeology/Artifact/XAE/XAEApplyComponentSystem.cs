using Content.Server.Power.Components;
using Content.Server.Spawners.Components;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.EntityTable.ValueSelector;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
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
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier) && tableSpawner.Table is EntSelector entSelector)
        {
            var originalAmount = entSelector.Amount.Get(_random.GetRandom());
            var newAmount = modifier.Modify(originalAmount);
            entSelector.Amount = new ConstantNumberSelector((int)newAmount);
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(PowerSupplierComponent powerSupplier, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier))
        {
            powerSupplier.MaxSupply = modifier.Modify(powerSupplier.MaxSupply);
            return true;
        }

        return false;
    }
}
