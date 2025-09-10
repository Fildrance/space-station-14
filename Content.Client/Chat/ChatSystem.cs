using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Chat;
using Content.Shared.Chat.V2;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Chat;

public sealed class ChatSystem : SharedChatSystem
{
    [Dependency] private readonly IUserInterfaceManager _interfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ChatUIController _chatController = default!;

    public override void Initialize()
    {
        base.Initialize();

        _chatController = _interfaceManager.GetUIController<ChatUIController>();

        SubscribeNetworkEvent<ReceiveChatMessageNetworkMessage>(OnReceiveChatMessage);
        SubscribeLocalEvent<PrepareReceivedChatMessageEvent>(OnPrepReceivedChatMessage);
    }

    public void SendMessage(ProtoId<CommunicationChannelPrototype> channelProtoId, EntityUid? entity, string str)
    {
        if (!entity.HasValue)
            return;

        var netEntity = GetNetEntity(entity);
        if (!netEntity.HasValue)
            return;

        var markup = FormattedMessage.FromMarkupPermissive(str);
        var sender = GetNetEntity(entity.Value);
        var @event = new ProduceChatMessageEvent(channelProtoId, sender, markup);
        RaisePredictiveEvent(@event);
    }

    private void OnReceiveChatMessage(ReceiveChatMessageNetworkMessage msg, EntitySessionEventArgs args)
    {
        if (_playerManager.LocalEntity == null || args.SenderSession.AttachedEntity != _playerManager.LocalEntity)
            return;

        var formattedMessage = msg.Message;
        var context = msg.Context;
        var targetChannel = msg.CommunicationChannel;
        var sender = GetEntity(msg.Sender);

        var prepareEvent = new PrepareReceivedChatMessageEvent(sender, formattedMessage, context, targetChannel);
        RaiseLocalEvent(_playerManager.LocalEntity.Value, ref prepareEvent);

        if (!formattedMessage.TryGetMessageInsideTag("BubbleContent", out var text))
        {
            text = FormattedMessage.Empty;
        }

        var templateId = targetChannel.MessageFormatLayout;

        string entityName = context.EntityName ?? "";

        var color = context.TextColor;
        formattedMessage.AddMarkupPermissive(color);

        var verbPrototype = GetSpeechVerb(sender, formattedMessage.ToString());
        var verbs = verbPrototype.SpeechVerbStrings;
        var random = new Random(context.Seed);
        var verb = Loc.GetString(random.Pick(verbs));

        var message = Loc.GetString(templateId, ("entityName", entityName), ("verb", verb), ("sourceMessage", formattedMessage.ToMarkup()));
        var markup = FormattedMessage.FromMarkupPermissive(message);

        var chatMessage = new ChatMessage(
            ChatChannel.Local,
            text.ToString(),
            markup.ToMarkup(),
            msg.Sender,
            null,
            targetChannel.HideChat
        );

        _chatController.AddMessage(chatMessage);
    }

    private void OnPrepReceivedChatMessage(ref PrepareReceivedChatMessageEvent ev)
    {
        if(!ev.MessageContext.TryGet<AudialCommunicationContextData>(out var data))
            return;

        if (data.IsWhispering)
        {
            ev.Message.InsertAroundMessage(new MarkupNode("italic"));
        }

        if (data.IsExclaiming)
        {
            ev.Message.InsertAroundMessage(new MarkupNode("bold"));
        }
    }
}


[ByRefEvent]
public record struct PrepareReceivedChatMessageEvent(
    EntityUid? Sender,
    FormattedMessage Message,
    ChatMessageContext MessageContext,
    CommunicationChannelPrototype CommunicationChannel
);

