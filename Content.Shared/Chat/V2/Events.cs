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
    public readonly ChatMessageContext MessageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel;
}

[ByRefEvent]
public struct GetPotentialRecipientsChatMessageEvent(ChatMessageContext messageContext, CommunicationChannelPrototype communicationChannel)
{
    public readonly List<EntityUid> Recipients = new();
    public readonly ChatMessageContext MessageContext = messageContext;
    public readonly CommunicationChannelPrototype CommunicationChannel = communicationChannel;
}

[ByRefEvent]
public struct ReceiveChatMessageEvent(ChatMessageContext messageContext)
{
    public readonly ChatMessageContext MessageContext = messageContext;
}

[ByRefEvent]
public struct AttemptReceiveChatMessageEvent(ChatMessageContext messageContext)
{
    public readonly ChatMessageContext MessageContext = messageContext;
    public bool Cancelled;
}

