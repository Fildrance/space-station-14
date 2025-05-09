using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that starts Foam chemical reaction with random-ish reagents inside.
/// </summary>
public sealed class XAEFoamSystem : BaseXAESystem<XAEFoamComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager= default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAEFoamComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XAEFoamComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAEFoamComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyEffect.Amount, out var amountChange))
        {
            var currentAmount = ent.Comp.FoamAmount;
            var amountMin = Math.Min(currentAmount.Min / 4, currentAmount.Min + amountChange);
            var amountMax = Math.Min(currentAmount.Min, currentAmount.Max + amountChange);

            ent.Comp.FoamAmount = new MinMax(amountMin, amountMax);
            Dirty(ent);
        }
    }

    private void OnMapInit(EntityUid uid, XAEFoamComponent component, MapInitEvent args)
    {
        if (component.SelectedReagent != null)
            return;

        if (component.Reagents.Count == 0)
            return;

        component.SelectedReagent = _random.Pick(component.Reagents);

        if (component.ReplaceDescription)
        {
            var reagent = _prototypeManager.Index<ReagentPrototype>(component.SelectedReagent);
            var newEntityDescription = Loc.GetString("xenoarch-effect-foam", ("reagent", reagent.LocalizedName));
            _metaData.SetEntityDescription(uid, newEntityDescription);
        }
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEFoamComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;
        if (component.SelectedReagent == null)
            return;

        var sol = new Solution();
        var range = (int)MathF.Round(MathHelper.Lerp(component.FoamAmount.Min, component.FoamAmount.Max, _random.NextFloat(0, 1f)));
        sol.AddReagent(component.SelectedReagent, component.ReagentAmount);
        var foamEnt = Spawn(ChemicalReactionSystem.FoamReaction, args.Coordinates);
        var spreadAmount = range * 4;
        _smoke.StartSmoke(foamEnt, sol, component.Duration, spreadAmount);
    }
}
