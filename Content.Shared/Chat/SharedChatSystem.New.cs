using Content.Shared.Chat.V2;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public void InitializeNew()
    {
        SubscribeAllEvent<SendChatMessageEvent>(OnSendChat);
    }

    private void OnSendChat(SendChatMessageEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var targetChannel = Prototype.Index(args.CommunicationChannel);
        var formattedMessage = args.Message;
        // This section handles setting up the parameters and any other business that should happen before validation starts.

        // block if message was already sent by same entity and into same channel.
        var currentMessage = args;
        var sender = GetEntity(args.Sender);
        while (currentMessage.Parent != null)
        {
            if (currentMessage.Parent.CommunicationChannel == args.CommunicationChannel
                && currentMessage.Sender == args.Sender)
            {
                return;
            }

            currentMessage = currentMessage.Parent;
        }

        var context = PrepareContext(sender, args, targetChannel);


        // This section handles validating the publisher based on ChatConditions, and passing on the message should the validation fail.


        var attemptEvent = new AttemptSendChatMessageEvent(context, targetChannel, formattedMessage);
        RaiseLocalEvent(sender, ref attemptEvent);

        // If the sender failed the publishing conditions, this attempt a back-up channel.
        // Useful for e.g. making ghosts trying to send LOOC messages fall back to Deadchat instead.
        if (!attemptEvent.CanHandle || attemptEvent.Cancelled)
        {
            AlsoSendTo(args, context, targetChannel.FallbackChannels, sender);

            // we failed publishing, no reason to proceed.
            return;
        }

        // This section handles sending out the message to consumers, whether that be sessions or entities.
        // This is done via consume conditions. Conditional modifiers may also be applied here for a subset of consumers.

        // Evaluate what clients should consume this message.
        var getRecipientsEvent = new GetPotentialRecipientsChatMessageEvent(context, targetChannel, formattedMessage);
        RaiseLocalEvent(sender, ref getRecipientsEvent);

        var targets = getRecipientsEvent.Recipients;
        if (targets.Count == 0)
            return;

        RefineContext(formattedMessage, targetChannel, context, sender);

        foreach (var target in targets)
        {
            var attemptReceiveEvent = new AttemptReceiveChatMessageEvent(context, formattedMessage);
            RaiseLocalEvent(target, ref attemptReceiveEvent);

            if (attemptReceiveEvent.Cancelled)
                continue;

            var receiveEvent = new ReceiveChatMessageEvent(formattedMessage, context, targetChannel);
            RaiseLocalEvent(target, ref receiveEvent);

            SendChatMessageReceivedCommand(sender, target, formattedMessage, context, targetChannel);
        }

        // We also pass it on to any child channels that should be included.
        AlsoSendTo(args, context, targetChannel.AlwaysRelayedToChannels, sender);
    }

    protected virtual void SendChatMessageReceivedCommand(
        EntityUid sender,
        EntityUid target,
        FormattedMessage formattedMessage,
        ChatMessageContext context,
        CommunicationChannelPrototype targetChannel
    )
    {
        // no-op
    }


    private void RefineContext(FormattedMessage input, CommunicationChannelPrototype channel, ChatMessageContext context, EntityUid sender)
    {
        var metaData = MetaData(sender);

        var nameEv = new TransformSpeakerNameEvent(sender, metaData.EntityName);
        RaiseLocalEvent(sender, nameEv);
        context.Set(MessageParts.EntityName, nameEv.VoiceName);

        var hash = SharedRandomExtensions.HashCodeCombine(new() { (int)GetNetEntity(sender) });
        var random = new System.Random(hash);

        // get owner accents?
        // hook into other stuff?
    }

    public enum MessageData
    {
        SpeechBubbleType
    }

    private void AlsoSendTo(
        SendChatMessageEvent @event,
        ChatMessageContext messageContext,
        IEnumerable<ProtoId<CommunicationChannelPrototype>> otherChannels,
        EntityUid sender
    )
    {
        foreach (var childChannel in otherChannels)
        {
            var newMessage = new SendChatMessageEvent(1, childChannel, @event.Sender, @event.Message, messageContext, @event);
            RaiseLocalEvent(sender, newMessage);
        }
    }

    private static ChatMessageContext PrepareContext(
        EntityUid sender,
        SendChatMessageEvent @event,
        CommunicationChannelPrototype channelPrototype
    )
    {
        // Set the channel parameters, and supply any custom ones if necessary.

        // Include a random seed based on the message's hashcode.
        // Since the message has yet to be formatted by anything, any child channels should get the same random seed.
        var messageContext = new ChatMessageContext(
            channelPrototype.ChannelParameters,
            @event.Context,
            @event.GetHashCode()
        );

        return messageContext;
    }

    private SpeechVerbPrototype GetSpeechVerbProto(FormattedMessage message, ProtoId<SpeechVerbPrototype>? speechVerb, EntityUid sender)
    {
        // This if/else tree can probably be cleaned up at some point
        if (speechVerb != null && Prototype.TryIndex(speechVerb, out var eventProto))
        {
            return eventProto;
        }

        if (!TryComp<SpeechComponent>(sender, out var speech))
        {
            return Prototype.Index(DefaultSpeechVerb);
        }

        SpeechVerbPrototype? current = null;
        // check for a suffix-applicable speech verb
        foreach (var (str, id) in speech.SuffixSpeechVerbs)
        {
            var proto = Prototype.Index(id);
            if (message.ToString().EndsWith(Loc.GetString(str)) &&
                proto.Priority >= (current?.Priority ?? 0))
            {
                current = proto;
            }
        }

        // if no applicable suffix verb return the normal one used by the entity
        current ??= Prototype.Index(speech.SpeechVerb);

        return current;
    }
}
