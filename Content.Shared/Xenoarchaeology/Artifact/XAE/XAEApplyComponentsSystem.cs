using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for applying component-registry when artifact effect is activated.
/// </summary>
public sealed class XAEApplyComponentsSystem : BaseXAESystem<XAEApplyComponentsComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAEApplyComponentsComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAEApplyComponentsComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        var components = ent.Comp.Components;
        components.TryGetComponent()
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEApplyComponentsComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var artifact = args.Artifact;

        foreach (var registry in ent.Comp.Components)
        {
            var componentType = registry.Value.Component.GetType();
            if (!ent.Comp.ApplyIfAlreadyHave && HasComp(artifact, componentType))
            {
                continue;
            }

            if (ent.Comp.RefreshOnReactivate)
            {
                RemComp(artifact, componentType);
            }

            var clone = EntityManager.ComponentFactory.GetComponent(registry.Value);
            AddComp(artifact, clone);
        }
    }
}
