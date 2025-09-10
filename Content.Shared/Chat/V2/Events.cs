using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.V2;

[Serializable, NetSerializable]
public sealed class ProduceChatMessageEvent(
    ProtoId<CommunicationChannelPrototype> communicationChannel,
    NetEntity sender,
    FormattedMessage message,
    ChatMessageContext? context = null,
    ProduceChatMessageEvent? parent = null,
    NetEntity? target = null
) : EntityEventArgs
{
    public readonly ProtoId<CommunicationChannelPrototype> CommunicationChannel = communicationChannel;

    public readonly ProduceChatMessageEvent? Parent = parent;

    public readonly NetEntity Sender = sender;

    public readonly NetEntity? Target = target;

    public readonly FormattedMessage Message = message;

    public readonly ChatMessageContext? Context = context;
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
public struct GetPotentialRecipientsChatMessageEvent(
    ChatMessageContext messageContext,
    CommunicationChannelPrototype communicationChannel,
    FormattedMessage message
)
{
    public readonly List<EntityUid> Recipients = new();
    public readonly ChatMessageContext MessageContext = messageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel = communicationChannel;
    public readonly FormattedMessage Message = message;
}

[Serializable, NetSerializable]
public sealed partial class ReceiveChatMessageNetworkMessage(
    NetEntity sender,
    FormattedMessage message,
    ChatMessageContext context,
    CommunicationChannelPrototype communicationChannel
) : EntityEventArgs
{
    public readonly NetEntity Sender = sender;
    public readonly FormattedMessage Message = message;
    public readonly ChatMessageContext Context = context;
    public readonly CommunicationChannelPrototype CommunicationChannel = communicationChannel;
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
    bool Cancelled = false
);
