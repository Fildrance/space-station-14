using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Chat;
using Content.Shared.Chat.V2;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Chat.V2;

public sealed class ChatSystemNew : SharedChatSystemNew
{
    [Dependency] private readonly IUserInterfaceManager _interfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager= default!;

    private ChatUIController _chatController = default!;

    public override void Initialize()
    {
        base.Initialize();

        _chatController = _interfaceManager.GetUIController<ChatUIController>();
        
        SubscribeNetworkEvent<ReceiveChatMessage>(OnReceiveChatMessage);
    }

    private void OnReceiveChatMessage(ReceiveChatMessage msg, EntitySessionEventArgs args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        if (args.SenderSession.AttachedEntity != _playerManager.LocalEntity)
            return;

        var formattedMessage = msg.Message;
        var context = msg.Context;
        var targetChannel = msg.CommunicationChannel;
        var sender = msg.Sender;

        if (!formattedMessage.TryGetMessageInsideTag("BubbleContent", out var text))
        {
            text = FormattedMessage.Empty;
        }

        var templateId = targetChannel.MessageFormatLayout;

        if (!context.TryGetString(MessageParts.EntityName, out var entityName))
        {
            entityName = "";
        }

        var message = Loc.GetString(templateId, ("entityName", entityName), ("verb", "lmao"), ("sourceMessage", formattedMessage.ToMarkup()));
        var markup = FormattedMessage.FromMarkupPermissive(message);

        var chatMessage = new ChatMessage(
            ChatChannel.Local,
            text.ToString(),
            markup.ToMarkup(),
            sender,
            null,
            targetChannel.HideChat
        );

        _chatController.AddMessage(chatMessage);
    }

    public void SendMessage(ProtoId<CommunicationChannelPrototype> channelProtoId, EntityUid? entity, string str)
    {
        if (!entity.HasValue)
            return;

        var netEntity = GetNetEntity(entity);
        if(!netEntity.HasValue)
            return;

        const uint messageId = 1u;
        var markup = FormattedMessage.FromMarkupPermissive(str);
        var sender = GetNetEntity(entity.Value);
        var context = new ChatMessageContext(messageId);
        var @event = new SendChatMessageEvent(messageId, channelProtoId, sender, markup, context);
        RaisePredictiveEvent(@event);
    }
}
