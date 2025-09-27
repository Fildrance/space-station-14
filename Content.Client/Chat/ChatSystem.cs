using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Chat.V2;
using Content.Shared.Decals;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Chat;

public sealed class ChatSystem : SharedChatSystem
{
    [Dependency] private readonly IUserInterfaceManager _interfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    private ChatUIController _chatController = default!;

    public override void Initialize()
    {
        base.Initialize();

        _chatController = _interfaceManager.GetUIController<ChatUIController>();

        SubscribeNetworkEvent<ReceiveChatMessageNetworkMessage>(OnReceiveChatMessage);
        SubscribeLocalEvent<PrepareReceivedChatMessageEvent>(OnPrepareReceivedChatMessage);
    }

    public void SendMessage(
        ProtoId<CommunicationChannelPrototype> channelProtoId,
        EntityUid? entity,
        string str,
        List<CommunicationContextData>? additionalData = null
    )
    {
        if (!entity.HasValue)
            return;

        var netEntity = GetNetEntity(entity);
        if (!netEntity.HasValue)
            return;

        var markup = FormattedMessage.FromMarkupPermissive(str);
        var sender = GetNetEntity(entity.Value);
        var messageId = Guid.NewGuid().ToString();
        var @event = new ProducePlayerChatMessageEvent(messageId, channelProtoId, sender, markup, additionalData);
        RaisePredictiveEvent(@event);
    }

    private void OnReceiveChatMessage(ReceiveChatMessageNetworkMessage msg, EntitySessionEventArgs args)
    {
        if (_playerManager.LocalEntity == null || args.SenderSession.AttachedEntity != _playerManager.LocalEntity)
            return;

        var formattedMessage = msg.Message;
        var context = msg.Context;
        var targetChannel = Prototype.Index(msg.CommunicationChannel);
        var sender = GetEntity(msg.Sender);

        var renderSettings = new ChatMessageRenderSettings();
        var prepareEvent = new PrepareReceivedChatMessageEvent(sender, formattedMessage, renderSettings, context, targetChannel);
        RaiseLocalEvent(ref prepareEvent);

        var templateId = targetChannel.MessageFormatLayout;

        var entityName = context.EntityName ?? string.Empty;

        var verbPrototype = GetSpeechVerb(sender, formattedMessage.ToString());
        var verbs = verbPrototype.SpeechVerbStrings;
        var random = new Random(context.Seed);
        var verb = Loc.GetString(random.Pick(verbs));

        var message = Loc.GetString(templateId, ("entityName", entityName), ("verb", verb), ("sourceMessage", formattedMessage.ToMarkup()));
        var markup = FormattedMessage.FromMarkupPermissive(message);

        Apply(markup, renderSettings.Content, ChatConstants.BubbleBodyTagName);
        Apply(markup, renderSettings.Header, ChatConstants.BubbleHeaderTagName);
        Apply(markup, renderSettings.All);

        if (!formattedMessage.TryGetMessageInsideTag(ChatConstants.BubbleBodyTagName, out var body) || string.IsNullOrWhiteSpace(body.ToString()))
        {
            body = FormattedMessage.Empty;
        }

        var chatMessage = new ChatMessage(
            ChatChannel.Local,
            body.ToString(),
            markup.ToMarkup(),
            msg.Sender,
            null,
            targetChannel.HideChat
        );

        _chatController.AddMessage(chatMessage);
    }

    private static void Apply(FormattedMessage formattedMessage, ChatTextRenderSettings settings, string? intoTag = null)
    {
        if (settings.IsBold)
        {
            var markupNode = new MarkupNode("bold", null, null);
            InsertTag(formattedMessage, markupNode, intoTag);
        }

        if (settings.IsItalic)
        {
            var markupNode = new MarkupNode("italic", null, null);
            InsertTag(formattedMessage, markupNode, intoTag);
        }

        if (settings.Color.HasValue)
        {
            var markupNode = new MarkupNode("color", new MarkupParameter(settings.Color), null);
            InsertTag(formattedMessage, markupNode, intoTag);
        }

        if (settings.FontSize.HasValue || settings.FontName != null)
        {
            Dictionary<string, MarkupParameter>? markupParameters = null;
            if (settings.FontSize.HasValue)
            {
                markupParameters = new Dictionary<string, MarkupParameter> { ["size"] = new MarkupParameter(settings.FontSize) };
            }

            MarkupParameter? markupNode = null;
            if (settings.FontName != null)
            {
                markupNode = new MarkupParameter(settings.Color);
            }

            InsertTag(formattedMessage, new MarkupNode("font", markupNode, markupParameters), intoTag);
        }
    }

    private static void InsertTag(FormattedMessage formattedMessage, MarkupNode markupNode, string? intoTag)
    {
        if (intoTag == null)
        {
            formattedMessage.InsertAroundMessage(markupNode);
        }
        else
        {
            formattedMessage.InsertInsideTag(markupNode, intoTag);
        }
    }

    private void OnPrepareReceivedChatMessage(ref PrepareReceivedChatMessageEvent ev)
    {
        if (ev.MessageContext.EntityName != null && _config.GetCVar(CCVars.ChatEnableColorName))
        {
            ev.RenderSettings.Header.Color = GetNameColor(ev.MessageContext.EntityName);
        }

        if (!ev.MessageContext.TryGet<AudialCommunicationContextData>(out var data))
            return;

        if (data.IsWhispering)
        {
            ev.RenderSettings.All.IsItalic = true;
            ev.RenderSettings.Content.IsItalic = true;
        }

        if (data.IsExclaiming)
        {
            ev.RenderSettings.All.IsBold= true;
            ev.RenderSettings.Content.IsBold= true;
        }
    }

    private static readonly ProtoId<ColorPalettePrototype> ChatNamePalette = "ChatNames";

    private Color GetNameColor(string name)
    {
        var nameColors = Prototype.Index(ChatNamePalette).Colors.Values;
        var colorIdx = Math.Abs(name.GetHashCode() % nameColors.Count);
        var i = 0;
        foreach (var nameColor in nameColors)
        {
            if (i == colorIdx)
                return nameColor;

            i++;
        }

        return default;
    }
}

[ByRefEvent]
public record struct PrepareReceivedChatMessageEvent(
    EntityUid? Sender,
    FormattedMessage Message,
    ChatMessageRenderSettings RenderSettings,
    ChatMessageContext MessageContext,
    CommunicationChannelPrototype CommunicationChannel
);

public sealed class ChatMessageRenderSettings
{
    public ChatTextRenderSettings Header = new();
    public ChatTextRenderSettings Content = new();
    public ChatTextRenderSettings All = new();
}

public sealed class ChatTextRenderSettings
{
    public int? FontSize;
    public bool IsBold;
    public bool IsItalic;
    public Color? Color;
    public string? FontName;
}

/// <summary>
/// Constants, used by chat systems.
/// </summary>
public static class ChatConstants
{
    /// <summary>
    /// Tag name for speech bubble header tag.
    /// </summary>
    public const string BubbleHeaderTagName = "BubbleHeader";

    /// <summary>
    /// Tag name for speech bubble body tag.
    /// </summary>
    public const string BubbleBodyTagName = "BubbleMessage";
}

