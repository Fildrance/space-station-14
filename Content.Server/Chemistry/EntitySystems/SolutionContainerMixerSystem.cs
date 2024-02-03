using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed class SolutionContainerMixerSystem : SharedSolutionContainerMixerSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionContainerMixerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SolutionContainerMixerComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
        SubscribeLocalEvent<SolutionContainerMixerComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<SolutionContainerMixerComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<SolutionContainerMixerComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);
        SubscribeLocalEvent<SolutionContainerMixerComponent, CentrifugeReagentButtonMessage>(OnReagentButtonMessage);
    }

    private void OnReagentButtonMessage(Entity<SolutionContainerMixerComponent> mixerComponent, ref CentrifugeReagentButtonMessage message)
    {
        if (message.IsTransfer)
        {
            TransferReagents(mixerComponent, message.ReagentId, FixedPoint2.MaxValue, true);
        }
        else
        {
            DiscardReagents(mixerComponent, message.ReagentId, FixedPoint2.MaxValue, true);
        }

        ClickSound(mixerComponent);
    }

    private void TransferReagents(Entity<SolutionContainerMixerComponent> mixerComponent, ReagentId id, FixedPoint2 amount, bool fromBuffer)
    {
        var container = _itemSlotsSystem.GetItemOrNull(mixerComponent, CentrifugeSlotNames.Input);
        if (container is null ||
            !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
            !_solutionContainerSystem.TryGetSolution(mixerComponent.Owner, CentrifugeSlotNames.Buffer, out _, out var bufferSolution))
        {
            return;
        }

        if (fromBuffer) // Buffer to container
        {
            amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
            amount = bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
            _solutionContainerSystem.TryAddReagent(containerSoln.Value, id, amount, out var _);
        }

        UpdateUiState(mixerComponent);
    }

    private void DiscardReagents(Entity<SolutionContainerMixerComponent> mixerComponent, ReagentId id, FixedPoint2 amount, bool fromBuffer)
    {
        if (fromBuffer)
        {
            if (_solutionContainerSystem.TryGetSolution(mixerComponent.Owner, CentrifugeSlotNames.Buffer, out _, out var bufferSolution))
                bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
            else
                return;
        }

        UpdateUiState(mixerComponent);
    }


    private void SubscribeUpdateUiState<T>(Entity<SolutionContainerMixerComponent> ent, ref T ev)
    {
        UpdateUiState(ent);
    }

    protected override void UpdateUiState(Entity<SolutionContainerMixerComponent> ent)
    {
        var (owner, mixerComponent) = ent;
        if (!_solutionContainerSystem.TryGetSolution(owner, CentrifugeSlotNames.Buffer, out _, out var bufferSolution))
            return;
        var inputContainer = _itemSlotsSystem.GetItemOrNull(owner, CentrifugeSlotNames.Input);

        var bufferReagents = bufferSolution.Contents;
        var bufferCurrentVolume = bufferSolution.Volume;

        var state = new CentrifugeBoundUserInterfaceState(
            BuildInputContainerInfo(inputContainer),
            bufferReagents,
            bufferCurrentVolume,
            mixerComponent.Mixing
        );

        _userInterfaceSystem.TrySetUiState(owner, CentrifugeUiKey.Key, state);
    }


    private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
    {
        if (container is not { Valid: true })
            return null;

        if (!TryComp(container, out FitsInDispenserComponent? fits)
            || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
        {
            return null;
        }

        return BuildContainerInfo(Name(container.Value), solution);
    }

    private static ContainerInfo BuildContainerInfo(string name, Solution solution)
    {
        return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
        {
            Reagents = solution.Contents
        };
    }
    private void OnPowerChanged(Entity<SolutionContainerMixerComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            StopMix(ent);
    }

    protected override bool HasPower(Entity<SolutionContainerMixerComponent> entity)
    {
        return this.IsPowered(entity, EntityManager);
    }
}
