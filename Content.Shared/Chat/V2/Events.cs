using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.V2;

[Serializable, NetSerializable]
public sealed class ProducePlayerChatMessageEvent(
    string playerMessageId,
    ProtoId<CommunicationChannelPrototype> communicationChannel,
    NetEntity sender,
    FormattedMessage message,
    List<CommunicationContextData>? additionalData = null,
    NetEntity? target = null
) : EntityEventArgs
{
    public readonly string PlayerMessageId = playerMessageId;

    public readonly ProtoId<CommunicationChannelPrototype> CommunicationChannel = communicationChannel;

    public readonly NetEntity Sender = sender;

    public readonly NetEntity? Target = target;

    public FormattedMessage Message = message;

    public readonly List<CommunicationContextData> AdditionalData = additionalData ?? new();
}

[ByRefEvent]
public sealed class ProduceEntityChatMessageEvent(
    string? originalPlayerMessageId,
    ProtoId<CommunicationChannelPrototype> communicationChannel,
    EntityUid sender,
    FormattedMessage message,
    List<CommunicationContextData>? additionalData = null,
    ProduceEntityChatMessageEvent? parent = null,
    EntityUid? target = null
)
{
    public readonly string? OriginalPlayerMessageId = originalPlayerMessageId;

    public readonly ProtoId<CommunicationChannelPrototype> CommunicationChannel = communicationChannel;

    public readonly ProduceEntityChatMessageEvent? Parent = parent;

    public readonly EntityUid Sender = sender;

    public readonly EntityUid? Target = target;

    public readonly FormattedMessage Message = message;

    public readonly List<CommunicationContextData> AdditionalData = additionalData ?? new();
}

[ByRefEvent]
public struct AttemptSendChatMessageEvent(
    ChatMessageContext messageContext,
    CommunicationChannelPrototype communicationChannel,
    FormattedMessage message
)
{
    public bool CanHandle;
    public bool Cancelled;
    public readonly ChatMessageContext MessageContext = messageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel = communicationChannel;
    public readonly FormattedMessage Message = message;
}


[ByRefEvent]
public struct GetRefinedProducedChatMessageEvent(
    ChatMessageContext messageContext,
    CommunicationChannelPrototype communicationChannel,
    FormattedMessage message
)
{
    public readonly ChatMessageContext MessageContext = messageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel = communicationChannel;
    public readonly FormattedMessage Message = message;
}

[ByRefEvent]
public struct GetPotentialRecipientsChatMessageEvent(
    ChatMessageContext messageContext,
    CommunicationChannelPrototype communicationChannel,
    FormattedMessage message
)
{
    public readonly Dictionary<EntityUid, float?> DistanceByRecipient = new();
    public readonly ChatMessageContext MessageContext = messageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel = communicationChannel;
    public readonly FormattedMessage Message = message;
}

[Serializable, NetSerializable]
public sealed partial class ReceiveChatMessageNetworkMessage(
    NetEntity sender,
    FormattedMessage message,
    ChatMessageContext context,
    ProtoId<CommunicationChannelPrototype> communicationChannel
) : EntityEventArgs
{
    public readonly NetEntity Sender = sender;
    public readonly FormattedMessage Message = message;
    public readonly ChatMessageContext Context = context;
    public readonly ProtoId<CommunicationChannelPrototype> CommunicationChannel = communicationChannel;
}

[ByRefEvent]
public record struct ReceiveChatMessageEvent(
    EntityUid? Sender,
    FormattedMessage Message,
    ChatMessageContext MessageContext,
    CommunicationChannelPrototype CommunicationChannel
);

[ByRefEvent]
public record struct AttemptReceiveChatMessageEvent(
    EntityUid? Sender,
    ChatMessageContext MessageContext,
    FormattedMessage Message,
    bool CanHandle = true,
    bool Cancelled = false
);

[ByRefEvent]
public record struct GetRefinedReceiverChatMessageEvent(
    EntityUid? Sender,
    ChatMessageContext MessageContext,
    FormattedMessage Message
);
