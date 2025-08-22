using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.V2;

[ByRefEvent]
public struct AttemptReceiveChatMessage
{
    public bool Cancelled;
}

[ByRefEvent]
public sealed class SendChatMessageEvent(uint messageId, EntityUid sender, FormattedMessage message)
{
    public uint MessageId = messageId;

    public ProtoId<CommunicationChannelPrototype> CommunicationChannel;

    public SendChatMessageEvent? Parent;

    public EntityUid Sender = sender;

    public EntityUid? Target;

    public FormattedMessage Message = message;

    public ChatMessageContext? Context;
}

[ByRefEvent]
public struct AttemptSendChatMessageEvent
{
    public AttemptSendChatMessageEvent(AttemptSendChatMessageEvent other)
    {
        MessageContext = other.MessageContext;
        CommunicationChannel = other.CommunicationChannel;
        Message = other.Message;
    }

    public AttemptSendChatMessageEvent(
        ChatMessageContext messageContext,
        CommunicationChannelPrototype communicationChannel,
        FormattedMessage message
    )
    {
        MessageContext = messageContext;
        CommunicationChannel = communicationChannel;
        Message = message;
    }

    public bool CanHandle;
    public bool Cancelled;
    public readonly ChatMessageContext MessageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel;
    public readonly FormattedMessage Message;
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
public sealed class ReceiveChatMessageEvent : EntityEventArgs
{
    public ProtoId<CommunicationChannelPrototype> CommunicationChannelProtoId;
    public ChatMessageContext MessageContext = default!;
    public FormattedMessage Message = default!;
}

[ByRefEvent]
public struct AttemptReceiveChatMessageEvent(ChatMessageContext messageContext, FormattedMessage message)
{
    public FormattedMessage Message = message;
    public readonly ChatMessageContext MessageContext = messageContext;
    public bool Cancelled;
}

