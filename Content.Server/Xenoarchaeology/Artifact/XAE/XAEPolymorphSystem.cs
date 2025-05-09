using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that is polymorphing all humanoid entities in range.
/// </summary>
public sealed class XAEPolymorphSystem : BaseXAESystem<XAEPolymorphComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly PolymorphSystem _poly = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<Entity<HumanoidAppearanceComponent>> _humanoids = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAEPolymorphComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAEPolymorphComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        if (args.CurrentAmplification.TryGetValue<float>(XenoArtifactAmplifyEffect.Duration, out var duration))
        {
            ent.Comp.Duration += duration;
            if (ent.Comp.Duration < 1)
                ent.Comp.Duration = 1;

            Dirty(ent);
        }
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEPolymorphComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        _humanoids.Clear();
        _lookup.GetEntitiesInRange(args.Coordinates, ent.Comp.Range, _humanoids);
        foreach (var comp in _humanoids)
        {
            var target = comp.Owner;
            if (!_mob.IsAlive(target))
                continue;

            var uidAfter = _poly.PolymorphEntity(target, ent.Comp.PolymorphPrototypeName);
            if (uidAfter.HasValue && TryComp<PolymorphedEntityComponent>(uidAfter, out var polyComp))
                polyComp.Configuration.Duration += ent.Comp.Duration;

            _audio.PlayPvs(ent.Comp.PolySound, ent);
        }
    }
}
