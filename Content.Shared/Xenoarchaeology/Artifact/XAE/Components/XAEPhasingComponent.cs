namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
///     Removes the masks/layers of hard fixtures from the artifact when added, allowing it to pass through walls
///     and such.
/// </summary>
[RegisterComponent, Access(typeof(XAEPhasingSystem))]
public sealed partial class XAEPhasingComponent : Component
{
}