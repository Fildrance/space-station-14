using JetBrains.Annotations;

namespace Content.Shared.Xenoarchaeology.Artifact.Modifiers;

public sealed partial class BudgetDependantLogEffectivenessModifierProvider : ModifierProviderBase, IBudgetPlacementAwareModifier
{
    [DataField]
    public int LogBase = 2;

    [DataField]
    public float IncrementToOffsetZero = 2;

    [UsedImplicitly]
    public float? PlacementInBudget;

    /// <inheritdoc />
    public override float Modify(float originalValue)
    {
        if (!PlacementInBudget.HasValue)
            return originalValue;

        PlacementInBudget += IncrementToOffsetZero;
        return MathF.Log(PlacementInBudget.Value, LogBase) * originalValue;
    }

    /// <inheritdoc />
    public void SetPlacementInBudget(float placementInBudget)
    {
        PlacementInBudget = placementInBudget;
    }
}
