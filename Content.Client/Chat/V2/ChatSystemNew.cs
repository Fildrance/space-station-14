using Content.Client.UserInterface.Systems.Chat;
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
        
        SubscribeNetworkEvent<ChatMessageWrapper>(OnReceiveChatMessage);
    }

    private void OnReceiveChatMessage(ChatMessageWrapper msg, EntitySessionEventArgs args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        if (args.SenderSession.AttachedEntity != _playerManager.LocalEntity)
            return;

        _chatController.AddMessage(msg.Wrapped);
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
        var sendChatMessageEvent = new SendChatMessageEvent(messageId, channelProtoId, sender, markup, context);
        RaisePredictiveEvent(sendChatMessageEvent);
    }
}
