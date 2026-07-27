using Content.Shared.Chat.V2;
using Content.Shared.Random.Helpers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected ISharedChatManager _chatManager = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    [ViewVariables]
    protected readonly Dictionary<NetUserId, EntityUid> ChatExchangerEntitiesByUserNetId = new();

    private readonly EntProtoId _exchangeProto = "ChatMessageExchangerBase";

    private void InitializeNew()
    {
        SubscribeAllEvent<ProducePlayerChatMessageEvent>(OnPlayerSendChat);
        SubscribeLocalEvent<ProduceEntityChatMessageEvent>(OnEntitySendChat);
        SubscribeLocalEvent<ActorComponent, ReceiveChatMessageEvent>(OnChatMessageReceive);
        SubscribeLocalEvent<ChatMessageExchangerComponent, ComponentGetState>(OnGetState);

    }

    private void OnGetState(EntityUid uid, ChatMessageExchangerComponent component, ref ComponentGetState args)
    {
        //if (!args.ReplayState)
        //    return;

        // Should this be a full component state or a delta-state?
        if (args.FromTick <= component.CreationTick)
        {
            args.State = new ChatMessageExchangerComponent.ChatMessageExchangerState(component.Messages);
            return;
        }

        var data = new Dictionary<Guid, ChatMessageDataForExchange>();
        foreach (var (messageId, message) in component.Messages)
        {
            if (message.PushedOnTick >= args.FromTick)
                data[messageId] = message;
        }

        args.State = new ChatMessageExchangerComponent.ChatMessageExchangerDeltaState(data, new(component.Messages.Keys));
    }

    protected virtual void OnChatMessageReceive(Entity<ActorComponent> ent, ref ReceiveChatMessageEvent args)
    {
        TryAddMessage(ent.Comp.PlayerSession.UserId, args.CommunicationChannel, args.Message, args.MessageContext, args.Sender, args.PublishedOnTick);
    }

    private void OnPlayerSendChat(ProducePlayerChatMessageEvent msgEvent, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue)
            return; // log error? we have been violated! >:(

        if (!_chatManager.TryProcessChatMessage(msgEvent, args))
            return;

        var evt = new ProduceEntityChatMessageEvent(
            msgEvent.PlayerMessageId,
            msgEvent.CommunicationChannel,
            args.SenderSession.AttachedEntity.Value,
            msgEvent.Message,
            msgEvent.AdditionalData
        );
        RaiseLocalEvent(ref evt);
    }

    private void OnEntitySendChat(ref ProduceEntityChatMessageEvent msgEvent)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var sender = msgEvent.Sender;
        var targetChannel = ProtoMan.Index(msgEvent.CommunicationChannel);
        var formattedMessage = msgEvent.Message;

        // This section handles setting up the parameters and any other business that should happen before validation starts.

        if (IsRecursive(msgEvent))
            return;

        var context = PrepareContext(sender, msgEvent.AdditionalData, targetChannel, formattedMessage);

        // This section handles validating the publisher and passing on the message should the validation fail.


        var attemptEvent = new AttemptSendChatMessageEvent(context, targetChannel, formattedMessage);
        RaiseLocalEvent(sender, ref attemptEvent);

        // If the sender failed the publishing conditions, this attempt a back-up channel.
        // Useful for e.g. making ghosts trying to send LOOC messages fall back to Deadchat instead.
        if (!attemptEvent.CanHandle || attemptEvent.Cancelled)
        {
            AlsoSendTo(msgEvent, context, targetChannel.FallbackChannels);

            // we failed publishing, no reason to proceed.
            return;
        }

        var getRefined = new GetRefinedProducedChatMessageEvent(context, targetChannel, formattedMessage);
        RaiseLocalEvent(sender, ref getRefined);

        formattedMessage = getRefined.Message;
        context = getRefined.MessageContext;

        // This section handles sending out the message to consumers

        // Evaluate what clients should consume this message.
        var getRecipientsEvent = new GetPotentialRecipientsChatMessageEvent(context, targetChannel, formattedMessage);
        RaiseLocalEvent(sender, ref getRecipientsEvent);

        var targets = getRecipientsEvent.Recipients;
        if (targets.Count == 0)
            return;

        var tick = Timing.CurTick;
        foreach (var target in targets)
        {
            var attemptReceiveEvent = new AttemptReceiveChatMessageEvent(sender, context, formattedMessage);
            RaiseLocalEvent(target, ref attemptReceiveEvent);

            if (!attemptReceiveEvent.CanHandle || attemptReceiveEvent.Cancelled)
                continue;

            var getRefinedReceiverMsg = new GetRefinedReceiverChatMessageEvent(sender, context, formattedMessage);
            RaiseLocalEvent(target, ref getRefinedReceiverMsg);

            var receiverSpecifiedMessage = getRefinedReceiverMsg.Message;
            var receiverSpecifiedContext = getRefinedReceiverMsg.MessageContext;
            
            var receiveEvent = new ReceiveChatMessageEvent(sender, receiverSpecifiedMessage, receiverSpecifiedContext, targetChannel, tick);
            RaiseLocalEvent(target, ref receiveEvent);
        }

        // We also pass it on to any child channels that should be included.
        AlsoSendTo(msgEvent, context, targetChannel.AlwaysRelayedToChannels);
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

    private void AlsoSendTo(
        ProduceEntityChatMessageEvent @event,
        ChatMessageContext messageContext,
        IEnumerable<ProtoId<CommunicationChannelPrototype>> otherChannels
    )
    {
        foreach (var childChannel in otherChannels)
        {
            var newMessage = new ProduceEntityChatMessageEvent(@event.OriginalPlayerMessageId, childChannel, @event.Sender, @event.Message, messageContext.Data, @event);
            RaiseLocalEvent(@event.Sender, ref newMessage);
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

        var seed = SharedRandomExtensions.HashCodeCombine((int)GetNetEntity(sender), (int)Timing.CurTick.Value, GetDeterministicHashCode(channelPrototype.ID), GetDeterministicHashCode(formattedMessage.ToString()));
        var messageContext = new ChatMessageContext(seed, additionalData);

        return messageContext;
    }

    public bool TryGetExchanger(NetUserId user, [NotNullWhen(true)] out EntityUid? exchangerId, [NotNullWhen(true)] out ChatMessageExchangerComponent? exchanger)
    {
        if (ChatExchangerEntitiesByUserNetId.TryGetValue(user, out var exchangerValue)
            && TryComp(exchangerValue, out exchanger))
        {
            DebugTools.Assert(exchanger.UserId == user);

            exchangerId = exchangerValue;
            return true;
        }

        exchangerId = null;
        exchanger = null;
        return false;
    }

    private bool TryAddMessage(NetUserId userId,
        CommunicationChannelPrototype channel,
        FormattedMessage message,
        ChatMessageContext context,
        EntityUid? sender,
        GameTick tick)
    {
        if (!TryGetExchanger(userId, out var exchangerEnt, out var exchanger))
            return false;

        var messageId = Generate(context.Seed);
        if (exchanger.Messages.TryAdd(messageId, new(channel, message, context, GetNetEntity(sender), tick)))
        {
            exchanger.LastModified = Timing.CurTick;
            Dirty(exchangerEnt.Value, exchanger);
            return true;
        }

        return false;
    }

    protected static Guid Generate(int seed)
    {
        var random = new RobustRandom();
        random.SetSeed(seed);
        var bytes = new byte[16];
        random.NextBytes(bytes); //

        // Enforce Valid RFC 4122 Version 4 (Random) GUID bits
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40); // Set Version to 4
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // Set Variant to RFC 4122

        return new Guid(bytes);
    }

    public Entity<ChatMessageExchangerComponent> EnsureExchangerFor(ICommonSession session)
    {
        var exchangerId = Spawn(_exchangeProto, MapCoordinates.Nullspace);
        string? name = null;
        if (session.AttachedEntity.HasValue)
            name = Name(session.AttachedEntity.Value);

        _metadata.SetEntityName(exchangerId, name == null ? "message exchanger" : $"message exchanger ({name})");
        var exchanger = EnsureComp<ChatMessageExchangerComponent>(exchangerId);
        exchanger.UserId = session.UserId;
        ChatExchangerEntitiesByUserNetId.Add(session.UserId, exchangerId);

        return (exchangerId, exchanger);
    }

    private static int GetDeterministicHashCode(string str)
    {
        unchecked
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            for (int i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i + 1 == str.Length)
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}
