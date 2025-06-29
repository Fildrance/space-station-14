using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Atmos;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that changes atmospheric temperature on adjacent tiles.
/// </summary>
public sealed class XAETemperatureSystem : BaseXAESystem<XAETemperatureComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAETemperatureComponent, XenoArtifactAmplifyApplyEvent>(OnAmplify);
    }

    private void OnAmplify(Entity<XAETemperatureComponent> ent, ref XenoArtifactAmplifyApplyEvent args)
    {
        var dirty = false;
        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyTemperatureEffect.AdjacentTileChance, out var chanceChange))
        {
            ent.Comp.AdjacentTileEffectProbability = Math.Max(0.1f, ent.Comp.AdjacentTileEffectProbability + chanceChange);
            dirty = true;
        }

        if (args.CurrentAmplification.TryGetValue<int>(XenoArtifactAmplifyEffect.Amount, out var amountChange))
        {
            ent.Comp.TargetTemperature = Math.Max(ent.Comp.TargetTemperature / 8, ent.Comp.TargetTemperature + amountChange);
            dirty = true;
        }

        if (dirty)
            Dirty(ent);
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAETemperatureComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;
        var transform = Transform(ent);

        var center = _atmosphereSystem.GetContainingMixture(ent.Owner, false, true);
        if (center == null)
            return;

        UpdateTileTemperature(component, center);

        if (component.AdjacentTileEffectProbability > 0 && transform.GridUid != null)
        {
            var position = _transformSystem.GetGridOrMapTilePosition(ent, transform);
            var enumerator = _atmosphereSystem.GetAdjacentTileMixtures(transform.GridUid.Value, position, excite: true);

            while (enumerator.MoveNext(out var mixture))
            {
                if(_random.Prob(component.AdjacentTileEffectProbability))
                    UpdateTileTemperature(component, mixture);
            }
        }
    }

    private void UpdateTileTemperature(XAETemperatureComponent component, GasMixture environment)
    {
        var dif = component.TargetTemperature - environment.Temperature;
        var absDif = Math.Abs(dif);
        var step = Math.Min(absDif, component.SpawnTemperature);
        environment.Temperature += dif > 0 ? step : -step;
    }
}

public enum XenoArtifactAmplifyTemperatureEffect
{
    AdjacentTileChance
}
