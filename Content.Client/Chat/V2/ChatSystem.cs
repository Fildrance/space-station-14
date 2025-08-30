using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Chat.V2;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Chat.V2;

public sealed class ChatSystem : SharedChatSystemNew
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

    public void SendMessage(
        ProtoId<CommunicationChannelPrototype> channelProtoId,
        string str,
        EntityUid? entity
    )
    {
        if (!entity.HasValue)
            return;

        var netEntity = GetNetEntity(entity);
        if(!netEntity.HasValue)
            return;

        var markup = FormattedMessage.FromMarkupPermissive(str);
        var messageId = 1u;
        var sendChatMessageEvent = new SendChatMessageEvent(messageId, channelProtoId, GetNetEntity(entity.Value), markup, new ChatMessageContext(netEntity.Value, messageId));
        RaisePredictiveEvent(sendChatMessageEvent);
    }
}
