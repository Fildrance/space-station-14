using Content.Shared.Chat.V2;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly ISharedChatManager _chatManager = default!;

    private void InitializeNew()
    {
        SubscribeAllEvent<ProducePlayerChatMessageEvent>(OnPlayerSendChat);
        SubscribeLocalEvent<ProduceEntityChatMessageEvent>(OnEntitySendChat);
    }

    private void OnPlayerSendChat(ProducePlayerChatMessageEvent msgEvent, EntitySessionEventArgs args)
    {
        var sender = GetEntity(msgEvent.Sender);

        if (args.SenderSession.AttachedEntity != sender)
            return; // log error? we have been violated! >:(

        if (!_chatManager.TryProcessChatMessage(msgEvent, args))
            return;

        var evt = new ProduceEntityChatMessageEvent(
            msgEvent.PlayerMessageId,
            msgEvent.CommunicationChannel,
            sender,
            msgEvent.Message,
            msgEvent.AdditionalData,
            target: GetEntity(msgEvent.Target)
        );
        RaiseLocalEvent(ref evt);
    }

    private void OnEntitySendChat(ref ProduceEntityChatMessageEvent msgEvent)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var sender = msgEvent.Sender;
        var targetChannel = Prototype.Index(msgEvent.CommunicationChannel);
        var formattedMessage = msgEvent.Message;

        // This section handles setting up the parameters and any other business that should happen before validation starts.

        if (IsRecursive(msgEvent))
            return;

        var context = PrepareContext(sender, msgEvent.AdditionalData, targetChannel, formattedMessage);

        // This section handles validating the publisher based on ChatConditions, and passing on the message should the validation fail.


        var attemptEvent = new AttemptSendChatMessageEvent(context, targetChannel, formattedMessage);
        RaiseLocalEvent(sender, ref attemptEvent);

        // If the sender failed the publishing conditions, this attempt a back-up channel.
        // Useful for e.g. making ghosts trying to send LOOC messages fall back to Deadchat instead.
        if (!attemptEvent.CanHandle || attemptEvent.Cancelled)
        {
            AlsoSendTo(msgEvent, context, targetChannel.FallbackChannels, sender);

            // we failed publishing, no reason to proceed.
            return;
        }

        // This section handles sending out the message to consumers, whether that be sessions or entities.
        // This is done via consume conditions. Conditional modifiers may also be applied here for a subset of consumers.

        // Evaluate what clients should consume this message.
        var getRecipientsEvent = new GetPotentialRecipientsChatMessageEvent(context, targetChannel, formattedMessage);
        RaiseLocalEvent(sender, ref getRecipientsEvent);

        var targets = getRecipientsEvent.DistanceByRecipient;
        if (targets.Count == 0)
            return;

        RefineContext(formattedMessage, targetChannel, context, sender);

        foreach (var (target, distance) in targets)
        {
            context.Distance = distance;

            var attemptReceiveEvent = new AttemptReceiveChatMessageEvent(sender, context, formattedMessage);
            RaiseLocalEvent(target, ref attemptReceiveEvent);

            var receiverSpecifiedMessage = attemptReceiveEvent.Message;
            var receiverSpecifiedContext = attemptReceiveEvent.MessageContext;

            if (attemptReceiveEvent.Cancelled)
                continue;

            var receiveEvent = new ReceiveChatMessageEvent(sender, receiverSpecifiedMessage, receiverSpecifiedContext, targetChannel);
            RaiseLocalEvent(target, ref receiveEvent);
        }

        // We also pass it on to any child channels that should be included.
        AlsoSendTo(msgEvent, context, targetChannel.AlwaysRelayedToChannels, sender);
    }

    private static bool IsRecursive(ProduceEntityChatMessageEvent args)
    {
        // block if message was already sent by same entity and into same channel.
        var currentMessage = args;
        while (currentMessage.Parent != null)
        {
            if (currentMessage.Parent.CommunicationChannel == args.CommunicationChannel
                && currentMessage.Sender == args.Sender)
            {
                return true;
            }

            currentMessage = currentMessage.Parent;
        }

        return false;
    }

    private void RefineContext(FormattedMessage input, CommunicationChannelPrototype channel, ChatMessageContext context, EntityUid sender)
    {
        var metaData = MetaData(sender);

        var nameEv = new TransformSpeakerNameEvent(sender, metaData.EntityName);
        RaiseLocalEvent(sender, nameEv);
        context.EntityName = nameEv.VoiceName;
        context.Set(new AudialCommunicationContextData
        {
            
        });
        // get owner accents?
        // hook into other stuff?
    }

    private void AlsoSendTo(
        ProduceEntityChatMessageEvent @event,
        ChatMessageContext messageContext,
        IEnumerable<ProtoId<CommunicationChannelPrototype>> otherChannels,
        EntityUid sender
    )
    {
        foreach (var childChannel in otherChannels)
        {
            var newMessage = new ProduceEntityChatMessageEvent(@event.OriginalPlayerMessageId, childChannel, @event.Sender, @event.Message, messageContext.Data, @event);
            RaiseLocalEvent(sender, ref newMessage);
        }
    }

    private ChatMessageContext PrepareContext(
        EntityUid sender,
        List<CommunicationContextData>? additionalData,
        CommunicationChannelPrototype channelPrototype,
        FormattedMessage formattedMessage
    )
    {
        // Set the channel parameters, and supply any custom ones if necessary.

        // Include a random seed based on the message's hashcode.
        // Since the message has yet to be formatted by anything, any child channels should get the same random seed.

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)GetNetEntity(sender), (int)Timing.CurTick.Value, channelPrototype.ID.GetHashCode(), formattedMessage.GetHashCode() });

        var messageContext = new ChatMessageContext(seed, additionalData);

        return messageContext;
    }
}
