using JetBrains.Annotations;

namespace Content.Shared.Xenoarchaeology.Artifact.Modifiers;

[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class ModifierProviderBase
{
    public abstract float Modify(float originalValue);
}
