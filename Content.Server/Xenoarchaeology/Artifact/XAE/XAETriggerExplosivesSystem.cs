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
    protected override void OnActivated(Entity<XAETriggerExplosivesComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!TryComp<ExplosiveComponent>(ent, out var explosiveComp))
            return;

        var maxIntensity = explosiveComp.MaxIntensity;

        if (args.Modifications.TryGetValue(XenoArtifactExplosionEffectModifier.TotalIntensity,
                out var totalIntensityChange))
        {
            maxIntensity = Math.Max(maxIntensity / 4, maxIntensity + totalIntensityChange);
        }

        var totalIntensity = explosiveComp.TotalIntensity;
        if (args.Modifications.TryGetValue(XenoArtifactExplosionEffectModifier.MaxIntensity, out var maxIntensityChange))
        {
            totalIntensity += Math.Max(totalIntensity / 4, totalIntensity + maxIntensityChange);
        }

        _explosion.TriggerExplosive(ent, explosiveComp, totalIntensity: totalIntensity, maxIntensity: maxIntensity);
    }
}

public enum XenoArtifactExplosionEffectModifier
{
    TotalIntensity,
    MaxIntensity
}
