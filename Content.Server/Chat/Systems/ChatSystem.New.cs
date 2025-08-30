using Content.Shared.Chat.V2;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    /// <inheritdoc />
    protected override void SendChatMessageReceivedCommand(
        EntityUid sender,
        EntityUid target,
        FormattedMessage formattedMessage,
        ChatMessageContext context,
        CommunicationChannelPrototype targetChannel
    )
    {
        var senderNetEntity = GetNetEntity(sender);
        var chatMessageWrapper = new ReceiveChatMessage(senderNetEntity, formattedMessage, context, targetChannel);
        RaiseNetworkEvent(chatMessageWrapper, target);
    }
}
