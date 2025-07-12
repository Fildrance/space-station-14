using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Radiation.Components;
using Content.Shared.Stealth.Components;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public partial class SharedXAEApplyComponentsSystem
{
    [Dependency] private readonly HeldSpeedModifierSystem _heldSpeedModifier = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionsContainer = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    protected virtual bool TryApplyModifiers(IComponent component, XenoArtifactEffectsModifications modifications)
    {
        return component switch
        {
            StorageComponent storage => TryApplyModifiersFor(storage, modifications),
            SolutionContainerManagerComponent solutionContainer => TryApplyModifiersFor(solutionContainer, modifications),
            HeldSpeedModifierComponent speedModifier => TryApplyModifiersFor(speedModifier, modifications),
            MeleeWeaponComponent meleeWeapon => TryApplyModifiersFor(meleeWeapon, modifications),
            RevolverAmmoProviderComponent revolverAmmo => TryApplyModifiersFor(revolverAmmo, modifications),
            ToolComponent tool => TryApplyModifiersFor(tool, modifications),
            RadiationSourceComponent radiation => TryApplyModifiersFor(radiation, modifications),
            StealthOnMoveComponent stealthOnMove => TryApplyModifiersFor(stealthOnMove, modifications),
            _ => false
        };
    }

    private bool TryApplyModifiersFor(StealthOnMoveComponent stealthOnMove, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue<float>(XenoArtifactStealthEffectModifier.Effectiveness, out var effectiveness))
        {
            stealthOnMove.PassiveVisibilityRate *= effectiveness;
            stealthOnMove.MovementVisibilityRate *= effectiveness;
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(RadiationSourceComponent radiationSource, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue<float>(XenoArtifactRadiationSourceEffectModifier.Effectiveness,
                out var effectiveness))
        {
            radiationSource.Intensity *= effectiveness;
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(ToolComponent tool, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue<float>(XenoArtifactToolEffectModifier.Effectiveness, out var effectiveness))
        {
            _tool.ChangeSpeedModifier(tool, tool.SpeedModifier * effectiveness);
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(RevolverAmmoProviderComponent revolverAmmoProvider, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue<int>(XenoArtifactAmmoSourceEffectModifier.CapacityChange, out var capacityChange))
        {
            revolverAmmoProvider.Capacity += capacityChange;
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(MeleeWeaponComponent meleeWeapon, XenoArtifactEffectsModifications modifications)
    {
        var changed = false;
        if (modifications.TryGetValue<float>(XenoArtifactMeleeWeaponEffectModifier.Damage, out var damageChange))
        {
            foreach (var (key, value) in meleeWeapon.Damage.DamageDict)
            {
                meleeWeapon.Damage.DamageDict[key] = Math.Max(1, value.Value + damageChange);
            }

            changed = true;
        }

        if (modifications.TryGetValue<float>(XenoArtifactMeleeWeaponEffectModifier.AttackRate, out var attackRateChange))
        {
            meleeWeapon.AttackRate *= attackRateChange;
            changed = true;
        }

        return changed;
    }

    private bool TryApplyModifiersFor(HeldSpeedModifierComponent speedModifier, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue<float>(XenoArtifactHeldSpeedModifierEffectModifier.Multiplier, out var multiplier))
        {
            _heldSpeedModifier.ChangeModifiers(
                speedModifier,
                speedModifier.SprintModifier * multiplier,
                speedModifier.WalkModifier * multiplier
            );
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(
        SolutionContainerManagerComponent solutionStorage,
        XenoArtifactEffectsModifications modifications
    )
    {
        if (modifications.TryGetValue<int>(XenoArtifactSolutionStorageEffectModifier.VolumeChange, out var volumeChange)
            && _solutionsContainer.TryGetSolution(solutionStorage, "beaker", out var sol))
        {
            sol.MaxVolume = MathF.Max(5, sol.MaxVolume.Value + volumeChange);
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(StorageComponent storage, XenoArtifactEffectsModifications modifications)
    {
        if (storage.Grid.Count <= 0)
            return false;

        var storageToModify = storage.Grid[0];
        var height = storageToModify.Height;
        var width = storageToModify.Width;
        var changed = false;
        if (modifications.TryGetValue<int>(XenoArtifactStorageEffectModifier.HeightChange, out var heightChange))
        {
            height = Math.Max(1, height + heightChange);
            changed = true;
        }

        if (modifications.TryGetValue<int>(XenoArtifactStorageEffectModifier.WidthChange, out var widthChange))
        {
            width = Math.Max(1, width + widthChange);
            changed = true;
        }

        if(changed)
            storage.Grid[0] = new Box2i(0, 0, width, height);

        return changed;
    }
}

public enum XenoArtifactMeleeWeaponEffectModifier
{
    Damage,
    AttackRate
}

public enum XenoArtifactAmmoSourceEffectModifier
{
    CapacityChange
}

public enum XenoArtifactRadiationSourceEffectModifier
{
    Effectiveness
}

public enum XenoArtifactToolEffectModifier
{
    Effectiveness
}

public enum XenoArtifactStealthEffectModifier
{
    Effectiveness
}

public enum XenoArtifactStorageEffectModifier
{
    WidthChange,
    HeightChange,
}

public enum XenoArtifactHeldSpeedModifierEffectModifier
{
    Multiplier
}

public enum XenoArtifactSolutionStorageEffectModifier
{
    VolumeChange
}
