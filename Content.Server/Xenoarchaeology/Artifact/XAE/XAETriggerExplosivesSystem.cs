using Content.Server.Explosion.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect of triggering explosion.
/// </summary>
public sealed class XAETriggerExplosivesSystem : BaseXAESystem<XAETriggerExplosivesComponent>
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAETriggerExplosivesComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAETriggerExplosivesComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        if (!TryComp<ExplosiveComponent>(ent, out var explosive))
            return;

        var dirty = false;
        if (args.CurrentAmplification.TryGetValue<float>(XenoArtifactAmplifyExplosionEffect.TotalIntensity,
                out var totalIntensityChange))
        {
            explosive.MaxIntensity = Math.Max(explosive.MaxIntensity / 4, explosive.MaxIntensity + totalIntensityChange);
            dirty = true;
        }

        if (args.CurrentAmplification.TryGetValue<float>(XenoArtifactAmplifyExplosionEffect.MaxIntensity, out var maxIntensityChange))
        {
            explosive.TotalIntensity += Math.Max(explosive.TotalIntensity / 4, explosive.TotalIntensity + maxIntensityChange);
            dirty = true;
        }

        if(dirty)
            Dirty(ent);
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAETriggerExplosivesComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if(!TryComp<ExplosiveComponent>(ent, out var explosiveComp))
            return;

        _explosion.TriggerExplosive(ent, explosiveComp);
    }
}

public enum XenoArtifactAmplifyExplosionEffect
{
    TotalIntensity,
    MaxIntensity
}
