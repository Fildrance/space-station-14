using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<XenoArtifactUnlockingComponent> _unlockingQuery;

    private void InitializeUnlock()
    {
        _unlockingQuery = GetEntityQuery<XenoArtifactUnlockingComponent>();
    }

    /// <summary> Finish unlocking phase when the time is up. </summary>
    private void UpdateUnlock(float _)
    {
        var query = EntityQueryEnumerator<XenoArtifactUnlockingComponent, XenoArtifactComponent>();
        while (query.MoveNext(out var uid, out var unlock, out var comp))
        {
            if (_timing.CurTime < unlock.EndTime)
                continue;

            FinishUnlockingState((uid, unlock, comp));
        }
    }

    /// <summary>
    /// Checks if node can be unlocked.
    /// Only those nodes, that have no predecessors, or have all
    /// predecessors unlocked can be unlocked themselves.
    /// Artifact being suppressed also prevents unlocking.
    /// </summary>
    public bool CanUnlockNode(Entity<XenoArtifactNodeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var artifact = GetEntity(ent.Comp.Attached);
        if (!TryComp<XenoArtifactComponent>(artifact, out var artiComp))
            return false;

        if (artiComp.Suppressed)
            return false;

        if (!HasUnlockedPredecessor((artifact.Value, artiComp), ent))
            return false;

        return true;
    }

    /// <summary>
    /// Finishes unlocking phase, removing related component, and sums up what nodes were triggered,
    /// that could be unlocked. Marks such nodes as unlocked, and pushes their node activation event.
    /// </summary>
    public void FinishUnlockingState(Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent)
    {
        string unlockAttemptResultMsg;
        var artifactComponent = ent.Comp2;
        if (TryGetNodeFromUnlockState(ent, out var node))
        {
            SetNodeUnlocked((ent, artifactComponent), node.Value);
            unlockAttemptResultMsg = "artifact-unlock-state-end-success";
            var activated = ActivateNode((ent, artifactComponent), node.Value, null, null, Transform(ent).Coordinates, false);

            if (activated)
            {
                _audio.PlayPvs(ent.Comp1.ActivationSound, ent.Owner);

                var unlockingFinishedEvent = new ArtifactActivatedEvent();
                RaiseLocalEvent(ent.Owner, ref unlockingFinishedEvent);
            }
        }
        else
        {
            unlockAttemptResultMsg = "artifact-unlock-state-end-failure";
        }

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString(unlockAttemptResultMsg), ent);

        var unlockingComponent = ent.Comp1;
        RemComp(ent, unlockingComponent);
        artifactComponent.NextUnlockTime = _timing.CurTime + artifactComponent.UnlockStateRefractory;
    }

    public void CancelUnlockingState(Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent)
    {
        RemComp(ent, ent.Comp1);
    }

    /// <summary>
    /// Gets first locked node that can be unlocked (it is locked and all predecessor are unlocked).
    /// </summary>
    public bool TryGetNodeFromUnlockState(
        Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent,
        [NotNullWhen(true)] out Entity<XenoArtifactNodeComponent>? node
    )
    {
        node = null;

        var artifactUnlockingComponent = ent.Comp1;
        foreach (var nodeIndex in artifactUnlockingComponent.TriggeredNodeIndexes)
        {
            var artifactComponent = ent.Comp2;
            var curNode = GetNode((ent, artifactComponent), nodeIndex);
            if (!curNode.Comp.Locked || !CanUnlockNode((curNode, curNode)))
                continue;

            var requiredIndices = GetPredecessorNodes((ent, artifactComponent), nodeIndex);
            requiredIndices.Add(nodeIndex);

            // Make sure the two sets are identical
            if (requiredIndices.Count != artifactUnlockingComponent.TriggeredNodeIndexes.Count
                || !artifactUnlockingComponent.TriggeredNodeIndexes.All(requiredIndices.Contains))
                continue;

            node = curNode;
            return true;
        }

        return node != null;
    }
}

/// <summary>
/// Event of artifact node finishing unlocking and getting 1 or more node activated.
/// </summary>
[ByRefEvent]
public record struct ArtifactActivatedEvent(EntityUid ArtifactUid);
