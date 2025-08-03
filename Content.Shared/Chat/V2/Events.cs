using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.V2;

[ByRefEvent]
public struct AttemptReceiveChatMessage
{
    public bool Cancelled;
}

[ByRefEvent]
public sealed class SendChatMessageEvent
{
    public uint MessageId { get; set; }

    public ProtoId<CommunicationChannelPrototype> CommunicationChannel;

    public SendChatMessageEvent? Parent;

    public EntityUid Sender;

    public EntityUid? Target;

    public ChatMessageContext? Context { get; set; }
}

[ByRefEvent]
public struct AttemptSendChatMessageEvent
{
    public AttemptSendChatMessageEvent(AttemptSendChatMessageEvent other)
    {
        MessageContext = other.MessageContext;
        CommunicationChannel = other.CommunicationChannel;
    }

    public AttemptSendChatMessageEvent(
        ChatMessageContext messageContext,
        CommunicationChannelPrototype communicationChannel
    )
    {
        MessageContext = messageContext;
        CommunicationChannel = communicationChannel;
    }

    public bool CanHandle;
    public ChatMessageContext MessageContext;
    public CommunicationChannelPrototype CommunicationChannel;
}

[ByRefEvent]
public struct GetPotentialRecipientsChatMessageEvent(ChatMessageContext messageContext, CommunicationChannelPrototype communicationChannel)
{
    public List<EntityUid> Recipients = new();
    public ChatMessageContext MessageContext = messageContext;
    public CommunicationChannelPrototype CommunicationChannel = communicationChannel;
}

[ByRefEvent]
public struct ReceiveChatMessageEvent(ChatMessageContext messageContext)
{
    public ChatMessageContext MessageContext = messageContext;
}

[ByRefEvent]
public struct AttemptReceiveChatMessageEvent(ChatMessageContext messageContext)
{
    public ChatMessageContext MessageContext = messageContext;
    public bool Cancelled;
}

