using Robust.Shared.Utility;
using Content.Shared.CCVar;
using Content.Shared.Decals;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Access.Systems;
using Robust.Shared.Configuration;

namespace Content.Shared.Chat.V2;

public partial class ChatSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAccessSystem _accent = default!;
    [Dependency] private IConfigurationManager _config = default!;

    public FormattedMessage AccentChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        var accents = _accent.GetAccentList(chatMessageContext.Sender);
        foreach (var accentsDicts in accents)
        {
            message.InsertInsideTag(new MarkupNode("Accent", new MarkupParameter(accentsDicts.Key), accentsDicts.Value, false), "MainMessage");
        }

        return message;
    }

    [DataField]
    public string DefaultColorKey = "Base";

    public FormattedMessage ColorFulltextChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        var colorKey = DefaultColorKey;
        if (chatMessageContext.TryGet<string>(ColorFulltextMarkupParameter.Color, out var color))
            colorKey = color;

        message.InsertAroundMessage(new MarkupNode("ColorValue", new MarkupParameter(colorKey), null, false));
        return message;
    }

    public enum ColorFulltextMarkupParameter
    {
        Color,
    }


    private static ProtoId<ColorPalettePrototype> _chatNamePalette = "ChatNames";

    public FormattedMessage ColorEntityNameHeaderChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        var colorName = _config.GetCVar(CCVars.ChatEnableColorName);
        if (!colorName || !message.TryFirstOrDefault(x => x.Name == "EntityNameHeader", out var nameHeader))
            return message;

        var name = nameHeader.Value.StringValue;
        if (name == null)
            return message;

        var color = GetNameColor(name);
        if (color != default)
        {
            message.InsertOutsideTag(new MarkupNode("color", new MarkupParameter(color), null),
                "EntityNameHeader");
        }

        return message;
    }

    public Color GetNameColor(string name)
    {
        var nameColors = _prototype.Index<ColorPalettePrototype>(_chatNamePalette).Colors.Values;
        var colorIdx = Math.Abs(name.GetHashCode() % nameColors.Count);
        // simplified ElementAt, required to find element from ICollection without any allocations
        var i = 0;
        foreach (var nameColor in nameColors)
        {
            if (i == colorIdx)
                return nameColor;
            i++;
        }

        return default;
    }

    public FormattedMessage MainMessageChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        message.InsertAroundText(new MarkupNode("MainMessage", null, null));
        return message;
    }

    public FormattedMessage ItalicsFulltextChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        message.InsertAroundMessage(new MarkupNode("italic", null, null));
        return message;
    }

    public  FormattedMessage BoldFulltextChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        message.InsertAroundMessage(new MarkupNode("bold", null, null));
        return message;
    }

    public FormattedMessage BoldTagChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        if (TargetNode != null)
            message.InsertOutsideTag(new MarkupNode("bold", null, null), TargetNode);

        return message;
    }

    [DataField]
    public SpeechType SpeechType = SpeechType.Say;

    public FormattedMessage BubbleProviderChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        message.InsertOutsideTag(new MarkupNode(ChatConstants.BubbleHeaderTagName, new MarkupParameter((int)SpeechType), null), "EntityNameHeader");
        message.InsertOutsideTag(new MarkupNode(ChatConstants.BubbleBodyTagName, new MarkupParameter((int)SpeechType), null), "MainMessage");
        return message;
    }
}

// CHAT-TODO: This enum needs to be merged with the one in SpeechBubble.cs
public enum SpeechType : byte
{
    Emote,
    Say,
    Whisper,
    Looc
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
