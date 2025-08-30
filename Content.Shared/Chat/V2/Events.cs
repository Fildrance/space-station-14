using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.V2;

[Serializable, NetSerializable]
public sealed class SendChatMessageEvent(
    uint messageId,
    ProtoId<CommunicationChannelPrototype> communicationChannel,
    NetEntity sender,
    FormattedMessage message,
    ChatMessageContext context,
    SendChatMessageEvent? parent = null,
    NetEntity? target = null
) : EntityEventArgs
{
    public readonly uint MessageId = messageId;

    public readonly ProtoId<CommunicationChannelPrototype> CommunicationChannel = communicationChannel;

    public readonly SendChatMessageEvent? Parent = parent;

    public readonly NetEntity Sender = sender;

    public readonly NetEntity? Target = target;

    public readonly FormattedMessage Message = message;

    public readonly ChatMessageContext? Context = context;
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
public sealed partial class ChatMessageWrapper(ChatMessage wrapped) : EntityEventArgs
{
    public ChatMessage Wrapped = wrapped;
}

[ByRefEvent]
public struct ReceiveChatMessageEvent(FormattedMessage message, ChatMessageContext messageContext, CommunicationChannelPrototype communicationChannel)
{
    public readonly FormattedMessage Message = message;
    public readonly ChatMessageContext MessageContext = messageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel = communicationChannel;
}

[ByRefEvent]
public struct AttemptReceiveChatMessageEvent(ChatMessageContext messageContext, FormattedMessage message)
{
    public FormattedMessage Message = message;
    public readonly ChatMessageContext MessageContext = messageContext;
    public bool Cancelled;
}

