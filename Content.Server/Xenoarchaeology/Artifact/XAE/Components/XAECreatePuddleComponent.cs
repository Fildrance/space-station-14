using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// This is used for an artifact that creates a puddle of
/// random chemicals upon being triggered.
/// </summary>
[RegisterComponent, Access(typeof(XAECreatePuddleSystem))]
public sealed partial class XAECreatePuddleComponent : Component
{
    /// <summary>
    /// The solution where all the chemicals are stored
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public Solution ChemicalSolution = default!;

    /// <summary>
    /// The different chemicals that can be spawned by this effect
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> PossibleChemicals = default!;

    /// <summary>
    /// The number of chemicals in the puddle
    /// </summary>
    [DataField]
    public int ChemAmount = 3;

    /// <summary>
    /// List of reagents selected for this node. Selected ones are chosen on first activation
    /// and are picked from <see cref="PossibleChemicals"/>.
    /// </summary>
    public List<ProtoId<ReagentPrototype>>? SelectedChemicals;
}