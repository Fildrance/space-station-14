using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that ignites any flammable entity in range.
/// </summary>
public sealed class XAEIgniteSystem : BaseXAESystem<XAEIgniteComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    private EntityQuery<FlammableComponent> _flammables;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<EntityUid> _entities = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _flammables = GetEntityQuery<FlammableComponent>();
        SubscribeLocalEvent<XAEIgniteComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAEIgniteComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        var dirty = false;
        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyEffect.Range, out var rangeChange))
        {
            ent.Comp.Range += rangeChange;
            if (ent.Comp.Range <= 2f)
                ent.Comp.Range = 2f;

            dirty = true;
        }

        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyIgniteEffect.Effectiveness, out var effectiveness))
        {
            var stacks = ent.Comp.FireStack;
            var stacksMin = Math.Max(1, stacks.Min + effectiveness);
            var stacksMax = Math.Max(stacksMin, stacks.Max + effectiveness);
            ent.Comp.FireStack = new MinMax(stacksMin, stacksMax);

            dirty = true;
        }

        if (dirty)
            Dirty(ent);
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEIgniteComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;
        _entities.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, component.Range, _entities);
        foreach (var target in _entities)
        {
            if (!_flammables.TryGetComponent(target, out var fl))
                continue;

            fl.FireStacks += component.FireStack.Next(_random);
            _flammable.Ignite(target, ent.Owner, fl);
        }
    }

    public enum XenoArtifactAmplifyIgniteEffect
    {
        Effectiveness
    }
}
