using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEPhasingSystem : BaseXAESystem<XAEPhasingComponent>
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEPhasingComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!TryComp<FixturesComponent>(ent.Owner, out var fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            _physics.SetHard(ent.Owner, fixture, false, fixtures);
        }
    }
}