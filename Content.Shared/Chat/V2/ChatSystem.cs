using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.V2;

public sealed class ChatSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<SendChatMessageEvent>(OnSendChat);
    }

    private void OnSendChat(SendChatMessageEvent args)
    {
        var targetChannel = _prototype.Index(args.CommunicationChannel);

        // This section handles setting up the parameters and any other business that should happen before validation starts.

        // block if message was already sent by same entity and into same channel.
        var currentMessage = args;
        while (currentMessage.Parent != null)
        {
            if (currentMessage.Parent.CommunicationChannel == args.CommunicationChannel
                && currentMessage.Sender == args.Sender)
            {
                return;
            }

            currentMessage = currentMessage.Parent;
        }

        var messageContext = PrepareContext(args, targetChannel);


        // This section handles validating the publisher based on ChatConditions, and passing on the message should the validation fail.

        // We also pass it on to any child channels that should be included.
        AlsoSendTo(args, messageContext, targetChannel.AlwaysRelayedToChannels);

        var attemptEvent = new AttemptSendChatMessageEvent(messageContext, targetChannel);
        RaiseLocalEvent(args.Sender, ref attemptEvent);

        // If the sender failed the publishing conditions, this attempt a back-up channel.
        // Useful for e.g. making ghosts trying to send LOOC messages fall back to Deadchat instead.
        if (!attemptEvent.CanHandle)
        {
            AlsoSendTo(args, messageContext, targetChannel.FallbackChannels);

            // we failed publishing, no reason to proceed.
            return;
        }

        // This section handles sending out the message to consumers, whether that be sessions or entities.
        // This is done via consume conditions. Conditional modifiers may also be applied here for a subset of consumers.

        // Evaluate what clients should consume this message.
        var getRecipientsEvent = new GetPotentialRecipientsChatMessageEvent(messageContext, targetChannel);
        RaiseLocalEvent(args.Sender, ref getRecipientsEvent);

        var recipientsFilteredList = new ValueList<EntityUid>();
        var targets = getRecipientsEvent.Recipients;
        foreach (var target in targets)
        {
            var attemptReceiveEvent = new AttemptReceiveChatMessageEvent(messageContext);
            RaiseLocalEvent(target, ref attemptReceiveEvent);
            if (!attemptReceiveEvent.Cancelled)
            {
                recipientsFilteredList.Add(target);
            }
        }

        foreach (var target in recipientsFilteredList)
        {
            var receiveEvent = new ReceiveChatMessageEvent(messageContext);
            RaiseLocalEvent(target, ref receiveEvent);
        }
    }

    private void AlsoSendTo(
        SendChatMessageEvent message,
        ChatMessageContext messageContext,
        IEnumerable<ProtoId<CommunicationChannelPrototype>> otherChannels
    )
    {
        foreach (var childChannel in otherChannels)
        {
            var channelPrototype = _prototype.Index(childChannel);
            var newMessage = new SendChatMessageEvent
            {
                CommunicationChannel = channelPrototype,
                Parent = message,
                Sender = message.Sender,
                Context = messageContext,
            };
            RaiseLocalEvent(message.Sender, ref newMessage);
        }
    }

    private static ChatMessageContext PrepareContext(
        SendChatMessageEvent @event,
        CommunicationChannelPrototype channelPrototype
    )
    {
        // Set the channel parameters, and supply any custom ones if necessary.
        var messageContext = new ChatMessageContext(channelPrototype.ChannelParameters, @event.Sender, @event.MessageId, @event.Context)
        {
            // Include a random seed based on the message's hashcode.
            // Since the message has yet to be formatted by anything, any child channels should get the same random seed.
            [DefaultChannelParameters.RandomSeed] = @event.GetHashCode(),
        };

        return messageContext;
    }
}
