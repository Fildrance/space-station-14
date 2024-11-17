using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Random.Helpers;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAECreatePuddleSystem: BaseXAESystem<XAECreatePuddleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAECreatePuddleComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;

        if (component.SelectedChemicals == null)
        {
            var chemicalList = new List<ProtoId<ReagentPrototype>>();
            for (var i = 0; i < component.ChemAmount; i++)
            {
                var chemProto = _random.Pick(component.PossibleChemicals);
                chemicalList.Add(chemProto);
            }

            component.SelectedChemicals = chemicalList;
        }

        var amountPerChem = component.ChemicalSolution.MaxVolume / component.ChemAmount;
        foreach (var reagent in component.SelectedChemicals)
        {
            component.ChemicalSolution.AddReagent(reagent, amountPerChem);
        }

        _puddle.TrySpillAt(ent, component.ChemicalSolution, out _);
    }
}