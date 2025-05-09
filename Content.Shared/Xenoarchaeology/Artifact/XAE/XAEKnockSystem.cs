using Content.Shared.Magic.Events;
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
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAEKnockComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAEKnockComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyEffect.Range, out var rangeChange))
        {
            ent.Comp.KnockRange = Math.Max(1f, ent.Comp.KnockRange + rangeChange);
            Dirty(ent);
        }
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEKnockComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var ev = new KnockSpellEvent
        {
            Performer = ent.Owner,
            Range = ent.Comp.KnockRange
        };
        RaiseLocalEvent(ev);
    }
}
