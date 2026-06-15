using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Chat.V2;
using Content.Shared.Decals;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Chat;

public sealed class ChatSystem : SharedChatSystem
{
    [Dependency] private IUserInterfaceManager _interfaceManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _config = default!;

    private readonly List<Guid> _removedMessages = new();
    private ChatUIController _chatController = default!;

    public override void Initialize()
    {
        base.Initialize();

        _chatController = _interfaceManager.GetUIController<ChatUIController>();

        SubscribeLocalEvent<PrepareReceivedChatMessageEvent>(OnPrepareReceivedChatMessage);
        SubscribeLocalEvent<ChatMessageExchangerComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid gridUid, ChatMessageExchangerComponent exchangerComp, ref ComponentHandleState args)
    {
        _removedMessages.Clear();
        Dictionary<Guid, (ProtoId<CommunicationChannelPrototype> channel, FormattedMessage message, ChatMessageContext context, NetEntity? sender)> modifiedMessages;

        switch (args.Current)
        {
            case ChatMessageExchangerComponent.ChatMessageExchangerDeltaState delta:
            {
                modifiedMessages = delta.ModifiedMessages;
                foreach (var key in exchangerComp.Messages.Keys)
                {
                    if (!delta.AllChunks.Contains(key))
                        _removedMessages.Add(key);
                }

                break;
            }
            case ChatMessageExchangerComponent.ChatMessageExchangerState state:
            {
                modifiedMessages = state.Messages;
                foreach (var key in exchangerComp.Messages.Keys)
                {
                    if (!state.Messages.ContainsKey(key))
                        _removedMessages.Add(key);
                }

                break;
            }
            default:
                return;
        }

        if (_removedMessages.Count > 0)
            RemoveMessages(exchangerComp);

        if (modifiedMessages.Count > 0)
            ModifyMessages(exchangerComp, modifiedMessages);
    }

    protected override void OnChatMessageReceive(Entity<ActorComponent> ent, ref ReceiveChatMessageEvent args)
    {
        base.OnChatMessageReceive(ent, ref args);

        var localSession = _playerManager.LocalSession;
        if (ent.Owner == localSession?.AttachedEntity)
        {
            OnReceiveChatMessage(localSession.AttachedEntity.Value, args);
        }
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
        var messageId = Guid.NewGuid().ToString();
        var @event = new ProducePlayerChatMessageEvent(messageId, channelProtoId, markup, additionalData);
        RaisePredictiveEvent(@event);
    }

    private void OnReceiveChatMessage(EntityUid target, ReceiveChatMessageEvent msg)
    {
        if (_playerManager.LocalEntity == null || target != _playerManager.LocalEntity)
            return;

        var chatMessage = PrepareMessage(msg.Message, msg.MessageContext, msg.CommunicationChannel, msg.Sender);

        _chatController.AddMessage(chatMessage);
    }

    private ChatMessage PrepareMessage(
        FormattedMessage formattedMessage,
        ChatMessageContext context,
        CommunicationChannelPrototype targetChannel,
        EntityUid sender
    )
    {
        var renderSettings = new ChatMessageRenderSettings();
        var prepareEvent = new PrepareReceivedChatMessageEvent(sender, formattedMessage, renderSettings, context, targetChannel);
        RaiseLocalEvent(ref prepareEvent);

        var templateId = targetChannel.MessageFormatLayout;

        var entityName = context.EntityName ?? string.Empty;

        var verbPrototype = GetSpeechVerb(sender, formattedMessage.ToString());
        var verbs = verbPrototype.SpeechVerbStrings;
        var random = new RobustRandom();
        random.SetSeed(context.Seed);
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
            Map(targetChannel),
            body.ToString(),
            markup.ToMarkup(),
            GetNetEntity(sender),
            null,
            targetChannel.HideChat,
            id : Generate(context.Seed)
        );
        return chatMessage;
    }

    private static ChatChannel Map(ProtoId<CommunicationChannelPrototype> communicationChannel)
    {
        return communicationChannel.Id switch
        {
            "ICSpeech" => ChatChannel.Local,
            _ => ChatChannel.Local
        };
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
        var nameColors = _prototype.Index(ChatNamePalette).Colors.Values;
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

    private void ModifyMessages(ChatMessageExchangerComponent exchanger, Dictionary<Guid, (ProtoId<CommunicationChannelPrototype> channel, FormattedMessage message, ChatMessageContext context, NetEntity? sender)> modifiedMessages)
    {
        foreach (var (key, value) in modifiedMessages)
        {
            exchanger.Messages[key] = value;
            if(value.sender == null)
                return;

            var data = PrepareMessage(value.message, value.context, _prototype.Index(value.channel), GetEntity(value.sender.Value));
            _chatController.ModifyMessage(key, data);
        }
    }

    private void RemoveMessages(ChatMessageExchangerComponent exchanger)
    {
        foreach (var removedMessage in _removedMessages)
        {
            exchanger.Messages.Remove(removedMessage);
            _chatController.RemoveMessage(removedMessage);
        }
    }
}

[ByRefEvent]
public record struct PrepareReceivedChatMessageEvent(
    EntityUid? Sender,
    FormattedMessage Message,
    ChatMessageRenderSettings RenderSettings,
    ChatMessageContext MessageContext,
    ProtoId<CommunicationChannelPrototype> CommunicationChannel
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
