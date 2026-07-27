using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.V2;

[Serializable, NetSerializable]
public sealed class ProducePlayerChatMessageEvent(
    string playerMessageId,
    ProtoId<CommunicationChannelPrototype> communicationChannel,
    FormattedMessage message,
    List<CommunicationContextData>? additionalData = null
) : EntityEventArgs
{
    public readonly string PlayerMessageId = playerMessageId;

    public readonly ProtoId<CommunicationChannelPrototype> CommunicationChannel = communicationChannel;

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
    ProduceEntityChatMessageEvent? parent = null
)
{
    public readonly string? OriginalPlayerMessageId = originalPlayerMessageId;

    public readonly ProtoId<CommunicationChannelPrototype> CommunicationChannel = communicationChannel;

    public readonly ProduceEntityChatMessageEvent? Parent = parent;

    public readonly EntityUid Sender = sender;

    public readonly FormattedMessage Message = message;

    public readonly List<CommunicationContextData> AdditionalData = additionalData ?? new();
}

[ByRefEvent]
public record struct AttemptSendChatMessageEvent(
    ChatMessageContext MessageContext,
    ProtoId<CommunicationChannelPrototype> CommunicationChannel,
    FormattedMessage Message
)
{
    public bool CanHandle;
    public bool Cancelled;
    public readonly ChatMessageContext MessageContext = MessageContext;
    public readonly ProtoId<CommunicationChannelPrototype> CommunicationChannel = CommunicationChannel;
    public readonly FormattedMessage Message = Message;
}


[ByRefEvent]
public record struct GetRefinedProducedChatMessageEvent(
    ChatMessageContext MessageContext,
    CommunicationChannelPrototype CommunicationChannel,
    FormattedMessage Message
)
{
    public readonly ChatMessageContext MessageContext = MessageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel = CommunicationChannel;
    public readonly FormattedMessage Message = Message;
}

[ByRefEvent]
public record struct GetPotentialRecipientsChatMessageEvent(
    ChatMessageContext MessageContext,
    CommunicationChannelPrototype CommunicationChannel,
    FormattedMessage Message
)
{
    public readonly List<EntityUid> Recipients = new();
    public readonly ChatMessageContext MessageContext = MessageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel = CommunicationChannel;
    public readonly FormattedMessage Message = Message;
}

[ByRefEvent]
public record struct ReceiveChatMessageEvent(
    EntityUid Sender,
    FormattedMessage Message,
    ChatMessageContext MessageContext,
    CommunicationChannelPrototype CommunicationChannel,
    GameTick PublishedOnTick
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
)
{
    public GetRefinedReceiverChatMessageEvent(GetRefinedReceiverChatMessageEvent ev, FormattedMessage message):this(ev.Sender, ev.MessageContext, message)
    {

    }
}
