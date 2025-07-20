using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
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
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEIgniteComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var range = ent.Comp.Range;

        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Range, out var rangeModifier))
        {
            range = Math.Max(2f, rangeModifier.Modify(range));
        }

        var stacks = ent.Comp.FireStack;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var effectivenessModifier))
        {
            var stacksMin = Math.Max(1, (int)effectivenessModifier.Modify(stacks.Min));
            var stacksMax = Math.Max(stacksMin, (int)effectivenessModifier.Modify(stacks.Max));
            stacks = new MinMax(stacksMin, stacksMax);
        }

        _entities.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, range, _entities);
        foreach (var target in _entities)
        {
            if (!_flammables.TryGetComponent(target, out var fl))
                continue;

            fl.FireStacks += stacks.Next(_random);
            _flammable.Ignite(target, ent.Owner, fl);
        }
    }
}
