using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that creates temporary portal between places on station.
/// </summary>
public sealed class XAEPortalSystem : BaseXAESystem<XAEPortalComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto= default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAEPortalComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAEPortalComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        if (args.CurrentAmplification.TryGetValue<float>(XenoArtifactAmplifyEffect.Duration, out var duration))
        {
            ent.Comp.Lifetime += duration;
            if (ent.Comp.Lifetime < 1)
                ent.Comp.Lifetime = 1;

            Dirty(ent);
        }
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEPortalComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var map = Transform(ent).MapID;
        var validMinds = new ValueList<EntityUid>();
        var mindQuery = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, TransformComponent, MetaDataComponent>();
        while (mindQuery.MoveNext(out var uid, out var mc, out _, out var xform, out var meta))
        {
            // check if the MindContainer has a Mind and if the entity is not in a container (this also auto excludes AI) and if they are on the same map
            if (mc.HasMind && !_container.IsEntityOrParentInContainer(uid, meta: meta, xform: xform) && xform.MapID == map)
            {
                validMinds.Add(uid);
            }
        }
        // this would only be 0 if there were a station full of AIs and no one else, in that case just stop this function
        if (validMinds.Count == 0)
            return;

        if(!TrySpawnNextTo(ent.Comp.PortalProto, args.Artifact, out var firstPortal))
            return;

        if (ent.Comp.Lifetime.HasValue)
        {
            if (TryComp<TimedDespawnComponent>(firstPortal.Value, out var timedDespawn))
            {
                timedDespawn.Lifetime = ent.Comp.Lifetime.Value;
            }

        }


        var target = _random.Pick(validMinds);
        if(!TrySpawnNextTo(ent.Comp.PortalProto, target, out var secondPortal))
            return;

        // Manual position swapping, because the portal that opens doesn't trigger a collision, and doesn't teleport targets the first time.
        _transform.SwapPositions(target, args.Artifact.Owner);

        _link.TryLink(firstPortal.Value, secondPortal.Value, true);
    }
}
