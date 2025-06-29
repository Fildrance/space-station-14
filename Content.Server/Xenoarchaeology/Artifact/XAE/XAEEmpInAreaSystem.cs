using Content.Server.Emp;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that creates EMP on use.
/// </summary>
public sealed class XAEEmpInAreaSystem : BaseXAESystem<XAEEmpInAreaComponent>
{
    [Dependency] private readonly EmpSystem _emp = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAEEmpInAreaComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAEEmpInAreaComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        var dirtied = false;
        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyEffect.Range, out var rangeChange))
        {
            ent.Comp.Range += rangeChange;
            if (ent.Comp.Range <= 4f)
                ent.Comp.Range = 4f;

            dirtied = true;
        }

        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyEffect.Duration, out var durationChange))
        {
            ent.Comp.DisableDuration += durationChange;
            if (ent.Comp.DisableDuration <= 0)
                ent.Comp.DisableDuration = 1;

            dirtied = true;
        }

        if (dirtied)
            Dirty(ent);
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEEmpInAreaComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        _emp.EmpPulse(args.Coordinates, ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration);
    }
}
