using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that is fully charging batteries in certain range.
/// </summary>
public sealed class XAEChargeBatterySystem : BaseXAESystem<XAEChargeBatteryComponent>
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<Entity<BatteryComponent>> _batteryEntities = new();

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEChargeBatteryComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var radius = ent.Comp.Radius;
        if (args.Modifications.TryGetValue<int>(XenoArtifactEffectModifier.Range, out var rangeChange))
        {
            radius = Math.Max(1f, radius + rangeChange);
        }

        var charge = ent.Comp.AddChargeAmount;
        if (args.Modifications.TryGetValue<int>(XenoArtifactEffectModifier.Amount, out var amountChange))
        {
            charge = Math.Max(charge / 8, charge + amountChange);
        }

        _batteryEntities.Clear();
        _lookup.GetEntitiesInRange(args.Coordinates, radius, _batteryEntities);
        foreach (var battery in _batteryEntities)
        {
            var chargeToSet = Math.Max(battery.Comp.CurrentCharge + charge, battery.Comp.MaxCharge);
            _battery.SetCharge(battery, chargeToSet, battery);
        }
    }
}
