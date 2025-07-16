using Content.Shared.Magic.Events;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that opens doors in some area around.
/// </summary>
public sealed class XAEKnockSystem : BaseXAESystem<XAEKnockComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEKnockComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var range = ent.Comp.KnockRange;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Range, out var rangeChange))
            range = Math.Max(1f, range + rangeChange);

        var ev = new KnockSpellEvent
        {
            Performer = ent.Owner,
            Range = range
        };
        RaiseLocalEvent(ev);
    }
}
