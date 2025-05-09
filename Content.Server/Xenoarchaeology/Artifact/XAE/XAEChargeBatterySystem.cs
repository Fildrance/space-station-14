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
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAEChargeBatteryComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAEChargeBatteryComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        var dirty = false;
        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyEffect.Range, out var rangeChange))
        {
            ent.Comp.Radius = Math.Max(1f, ent.Comp.Radius + rangeChange);
            dirty = true;
        }

        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyEffect.Amount, out var amountChange))
        {
            ent.Comp.Radius = Math.Max(ent.Comp.AddChargeAmount / 8, ent.Comp.AddChargeAmount + amountChange);
            dirty = true;
        }

        if (dirty)
            Dirty(ent);
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEChargeBatteryComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var chargeBatteryComponent = ent.Comp;
        _batteryEntities.Clear();
        _lookup.GetEntitiesInRange(args.Coordinates, chargeBatteryComponent.Radius, _batteryEntities);
        foreach (var battery in _batteryEntities)
        {
            var chargeToSet = Math.Max(battery.Comp.CurrentCharge + ent.Comp.AddChargeAmount, battery.Comp.MaxCharge);
            _battery.SetCharge(battery, chargeToSet, battery);
        }
    }
}
