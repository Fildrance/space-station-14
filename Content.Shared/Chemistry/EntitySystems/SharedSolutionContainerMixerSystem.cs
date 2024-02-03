using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// This handles <see cref="SolutionContainerMixerComponent"/>
/// </summary>
public abstract class SharedSolutionContainerMixerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionContainerMixerComponent, ContainerIsRemovingAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<SolutionContainerMixerComponent, CentrifugeStartUnmixMessage>(OnStartUnmix);
    }

    protected void ClickSound(Entity<SolutionContainerMixerComponent> mixerComponent)
    {
        _audio.PlayPvs(mixerComponent.Comp.ClickSound, mixerComponent, AudioParams.Default.WithVolume(-2f));
    }

    private void OnStartUnmix(Entity<SolutionContainerMixerComponent> entity, ref CentrifugeStartUnmixMessage args)
    {
        TryStartMix(entity, null);
    }

    private void OnRemoveAttempt(Entity<SolutionContainerMixerComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (args.Container.ID == ent.Comp.ContainerId && ent.Comp.Mixing)
            args.Cancel();
    }

    protected virtual bool HasPower(Entity<SolutionContainerMixerComponent> entity)
    {
        return true;
    }

    public void TryStartMix(Entity<SolutionContainerMixerComponent> entity, EntityUid? user)
    {
        var (uid, comp) = entity;
        if (comp.Mixing)
            return;

        if (!HasPower(entity))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-no-power"), entity, user.Value);
            return;
        }

        var container = _itemSlotsSystem.GetItemOrNull(entity, CentrifugeSlotNames.Input);
        if (container == null
            || !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution)
            || containerSolution.Volume == FixedPoint2.Zero)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-popup-nothing-to-mix"), entity, user.Value);
            return;
        }

        comp.Mixing = true;
        if (_net.IsServer)
            comp.MixingSoundEntity = _audio.PlayPvs(comp.MixingSound, entity, comp.MixingSound?.Params.WithLoop(true));
        
        comp.MixTimeEnd = _timing.CurTime + comp.MixDuration;
        _appearance.SetData(entity, SolutionContainerMixerVisuals.Mixing, true);
        Dirty(uid, comp);
        UpdateUiState(entity);
    }

    public void StopMix(Entity<SolutionContainerMixerComponent> entity)
    {
        var (uid, comp) = entity;
        if (!comp.Mixing)
            return;
        _audio.Stop(comp.MixingSoundEntity);
        _appearance.SetData(entity, SolutionContainerMixerVisuals.Mixing, false);
        comp.Mixing = false;
        comp.MixingSoundEntity = null;
        Dirty(uid, comp);
        UpdateUiState(entity);
    }

    public void FinishMix(Entity<SolutionContainerMixerComponent> mixerComponent)
    {
        var (uid, comp) = mixerComponent;
        if (!comp.Mixing)
            return;
        StopMix(mixerComponent);

        var container = _itemSlotsSystem.GetItemOrNull(mixerComponent, CentrifugeSlotNames.Input);
        if (container is null
            || !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution)
            || !_solutionContainerSystem.TryGetSolution(mixerComponent.Owner, CentrifugeSlotNames.Buffer, out _, out var bufferSolution)
            || !TryComp<ReactionMixerComponent>(mixerComponent, out var reactionMixer))
            return;

        _solutionContainerSystem.UpdateChemicals(containerSoln.Value, true, reactionMixer);

        bufferSolution.AddSolution(containerSolution, _prototypeManager);
        _solutionContainerSystem.RemoveAllSolution(containerSoln.Value);

        UpdateUiState(mixerComponent);
    }

    protected virtual void UpdateUiState(Entity<SolutionContainerMixerComponent> ent)
    {

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionContainerMixerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Mixing)
                continue;

            if (_timing.CurTime < comp.MixTimeEnd)
                continue;

            FinishMix((uid, comp));
        }
    }
}

[Serializable, NetSerializable]
public sealed class CentrifugeBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly ContainerInfo? InputContainerInfo;

    /// <summary>
    /// A list of the reagents and their amounts within the buffer, if applicable.
    /// </summary>
    public readonly IReadOnlyList<ReagentQuantity> BufferReagents;

    public readonly FixedPoint2? BufferCurrentVolume;
    public readonly bool IsBusy;

    public CentrifugeBoundUserInterfaceState(
        ContainerInfo? inputContainerInfo,
        IReadOnlyList<ReagentQuantity> bufferReagents,
        FixedPoint2 bufferCurrentVolume,
        bool isBusy
    )
    {
        InputContainerInfo = inputContainerInfo;
        BufferReagents = bufferReagents;
        BufferCurrentVolume = bufferCurrentVolume;
        IsBusy = isBusy;
    }

}
[Serializable, NetSerializable]
public sealed class CentrifugeStartUnmixMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CentrifugeReagentButtonMessage : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId;
    public readonly bool IsTransfer;

    public CentrifugeReagentButtonMessage(ReagentId reagentId, bool isTransfer)
    {
        ReagentId = reagentId;
        IsTransfer = isTransfer;
    }
}

public static class CentrifugeSlotNames
{
    public const string Buffer = "buffer";
    public const string Input = "input";
}

[Serializable, NetSerializable]
public enum CentrifugeUiKey
{
    Key
}
