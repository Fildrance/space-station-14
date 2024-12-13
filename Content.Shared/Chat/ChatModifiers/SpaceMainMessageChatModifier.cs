﻿using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Adds a space in front of the [MainMessage] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SpaceMainMessageChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        return InsertBeforeTag(message, new MarkupNode(" "), "MainMessage");
    }
}
