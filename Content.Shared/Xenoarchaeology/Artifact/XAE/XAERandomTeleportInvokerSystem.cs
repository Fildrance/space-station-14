using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAERandomTeleportInvokerSystem : BaseXAESystem<XAERandomTeleportInvokerComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAERandomTeleportInvokerComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAERandomTeleportInvokerComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyEffect.Range, out var rangeChange))
        {
            ent.Comp.MaxRange += rangeChange;
            if (ent.Comp.MaxRange <= 4f)
                ent.Comp.MaxRange = 4f;

            ent.Comp.MinRange+= rangeChange;
            if (ent.Comp.MinRange <= 4f)
                ent.Comp.MinRange = 4f;

            Dirty(ent);
        }
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAERandomTeleportInvokerComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        // todo: teleport person who activated artifact with artifact itself
        var component = ent.Comp;

        var xform = Transform(ent.Owner);
        _popup.PopupCoordinates(Loc.GetString("blink-artifact-popup"), xform.Coordinates, PopupType.Medium);

        var offsetTo = _random.NextVector2(component.MinRange, component.MaxRange);
        _xform.SetCoordinates(ent.Owner, xform, xform.Coordinates.Offset(offsetTo));
    }
}
