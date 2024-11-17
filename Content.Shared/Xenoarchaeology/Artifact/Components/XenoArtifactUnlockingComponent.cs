using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// This is used for tracking the nodes which have been triggered during a particular unlocking state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class XenoArtifactUnlockingComponent : Component
{
    /// <summary>
    /// Indexes corresponding to all of the nodes that have been triggered
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<int> TriggeredNodeIndexes = new();

    /// <summary>
    /// The time at which the unlocking state ends.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// The sound that plays when an artifact is activated
    /// </summary>
    [DataField]
    public SoundSpecifier ActivationSound = new SoundCollectionSpecifier("ArtifactActivation")
    {
        Params = new()
        {
            Variation = 0.1f,
            Volume = 3f
        }
    };
}