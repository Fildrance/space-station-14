using Content.Shared.Chat;
using Content.Shared.Chat.V2;
using Robust.Shared.Utility;

namespace Content.Server.Chat;

public sealed class ChatSystemNew : SharedChatSystemNew
{
    /// <inheritdoc />
    protected override void SendChatMessageReceivedCommand(
        EntityUid target,
        FormattedMessage formattedMessage,
        ChatMessageContext context,
        CommunicationChannelPrototype targetChannel
    )
    {
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

        var messageToNetwork = new ChatMessage(
            ChatChannel.Local,
            text.ToString(),
            markup.ToMarkup(),
            context.Sender,
            null,
            targetChannel.HideChat
        );
        var chatMessageWrapper = new ChatMessageWrapper(messageToNetwork);
        RaiseNetworkEvent(chatMessageWrapper, target);
    }
}
