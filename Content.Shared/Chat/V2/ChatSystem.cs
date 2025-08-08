using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.V2;

public sealed partial class ChatSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IChatRepository _repository = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<SendChatMessageEvent>(OnSendChat);
    }

    private void OnSendChat(SendChatMessageEvent args)
    {
        var targetChannel = _prototype.Index(args.CommunicationChannel);
        var formattedMessage = args.Message;
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

        var context = PrepareContext(args, targetChannel);


        // This section handles validating the publisher based on ChatConditions, and passing on the message should the validation fail.

        // We also pass it on to any child channels that should be included.
        AlsoSendTo(args, context, targetChannel.AlwaysRelayedToChannels);

        var attemptEvent = new AttemptSendChatMessageEvent(context, targetChannel, formattedMessage);
        RaiseLocalEvent(args.Sender, ref attemptEvent);

        // If the sender failed the publishing conditions, this attempt a back-up channel.
        // Useful for e.g. making ghosts trying to send LOOC messages fall back to Deadchat instead.
        if (!attemptEvent.CanHandle || attemptEvent.Cancelled)
        {
            AlsoSendTo(args, context, targetChannel.FallbackChannels);

            // we failed publishing, no reason to proceed.
            return;
        }

        // This section handles sending out the message to consumers, whether that be sessions or entities.
        // This is done via consume conditions. Conditional modifiers may also be applied here for a subset of consumers.

        // Evaluate what clients should consume this message.
        var getRecipientsEvent = new GetPotentialRecipientsChatMessageEvent(context, targetChannel, formattedMessage);
        RaiseLocalEvent(args.Sender, ref getRecipientsEvent);

        var recipientsFilteredList = new ValueList<EntityUid>();
        var targets = getRecipientsEvent.Recipients;
        foreach (var target in targets)
        {
            var attemptReceiveEvent = new AttemptReceiveChatMessageEvent(context, formattedMessage);
            RaiseLocalEvent(target, ref attemptReceiveEvent);
            if (!attemptReceiveEvent.Cancelled)
            {
                recipientsFilteredList.Add(target);
            }
        }

        var modifiedMessage = Modify(formattedMessage, targetChannel, context);

        foreach (var target in recipientsFilteredList)
        {
            var receiveEvent = new ReceiveChatMessageEvent(context, modifiedMessage);
            RaiseLocalEvent(target, ref receiveEvent);
        }
    }

    private FormattedMessage Modify(FormattedMessage input, CommunicationChannelPrototype channel, ChatMessageContext context)
    {
        var message = input;

        var (entityName, verbId) = PrepareData(message, context);
        // get speech verb
        // get owner accents?
        // get color for output!
        // hook into other stuff?

        LocId layout = channel.MessageFormatLayout;

        var transformed = Loc.GetString(
            layout,
            ("sourceMessage", message.ToString()),
            ("entityName", entityName), //"EntityNameHeader"
            ("verbId", verbId) // [SpeechVerb id='']
        );
        message = FormattedMessage.FromMarkupOrThrow(transformed);
        return message;
    }

    private (string VoiceName, int VerbId) PrepareData(FormattedMessage message, ChatMessageContext context)
    {
        var sender = context.Sender;
        var metaData = MetaData(sender);

        var nameEv = new TransformSpeakerNameEvent(sender, metaData.EntityName);
        RaiseLocalEvent(sender, nameEv);
        var voiceName = nameEv.VoiceName;

        var hash = SharedRandomExtensions.HashCodeCombine([(int)GetNetEntity(context.Sender)]);
        var random = new System.Random(hash);

        var current = GetSpeechVerbProto(message, nameEv.SpeechVerb, sender);

        var count = current.SpeechVerbStrings.Count;

        var verbId = random.Next(count);

        return (voiceName, verbId);
    }

    private void AlsoSendTo(
        SendChatMessageEvent @event,
        ChatMessageContext messageContext,
        IEnumerable<ProtoId<CommunicationChannelPrototype>> otherChannels
    )
    {
        foreach (var childChannel in otherChannels)
        {
            var channelPrototype = _prototype.Index(childChannel);
            var id = _repository.Save(); // todo: move real repository code.
            var newMessage = new SendChatMessageEvent(id, @event.Sender, @event.Message)
            {
                CommunicationChannel = channelPrototype,
                Parent = @event,
                Context = messageContext,
            };
            RaiseLocalEvent(@event.Sender, ref newMessage);
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

    public ProtoId<SpeechVerbPrototype> DefaultSpeechVerb = "Default";

    private SpeechVerbPrototype GetSpeechVerbProto(FormattedMessage message, ProtoId<SpeechVerbPrototype>? speechVerb, EntityUid sender)
    {
        // This if/else tree can probably be cleaned up at some point
        if (speechVerb != null && _prototype.TryIndex(speechVerb, out var eventProto))
        {
            return eventProto;
        }

        if (!TryComp<SpeechComponent>(sender, out var speech))
        {
            return _prototype.Index(DefaultSpeechVerb);
        }

        SpeechVerbPrototype? current = null;
        // check for a suffix-applicable speech verb
        foreach (var (str, id) in speech.SuffixSpeechVerbs)
        {
            var proto = _prototype.Index(id);
            if (message.ToString().EndsWith(Loc.GetString(str)) &&
                proto.Priority >= (current?.Priority ?? 0))
            {
                current = proto;
            }
        }

        // if no applicable suffix verb return the normal one used by the entity
        current ??= _prototype.Index(speech.SpeechVerb);

        return current;
    }
}

public interface IChatRepository
{
    uint Save();
}
