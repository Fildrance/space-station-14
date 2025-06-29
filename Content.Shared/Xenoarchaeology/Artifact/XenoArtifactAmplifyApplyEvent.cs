using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.Xenoarchaeology.Artifact;

[ByRefEvent]
public record struct XenoArtifactAmplifyApplyEvent(XenoArtifactAmplificationEffects CurrentAmplification);
