using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

[NetSerializable, Serializable]
public sealed class ChatMessageContext
{
    public readonly Dictionary<MessageParts, string> GenericParameters;

    public ChatMessageContext(
        Dictionary<MessageParts, string> dictionary,
        ChatMessageContext? otherContext
    ) : this(dictionary)
    {
        if (otherContext == null)
            return;

        foreach (var (key, value) in otherContext.GenericParameters)
        {
            GenericParameters[key] = value;
        }
    }

    public ChatMessageContext(Dictionary<MessageParts, string> dictionary)
    {
        GenericParameters = dictionary;
    }

    public ChatMessageContext() : this(new Dictionary<MessageParts, string>())
    {
    }

    public int Count => GenericParameters.Count;

    public void Set(MessageParts key, bool value)
    {
        GenericParameters[key] = value.ToString();
    }

    public void Set(MessageParts key, float value)
    {
        GenericParameters[key] = value.ToString(CultureInfo.InvariantCulture);
    }

    public void Set(MessageParts key, string value)
    {
        GenericParameters[key] = value;
    }

    public void Set(MessageParts key, int value)
    {
        GenericParameters[key] = value.ToString();
    }


    public bool TryGetFloat(MessageParts key, [NotNullWhen(true)] out float? value)
    {
        if (GenericParameters.TryGetValue(key, out var val) && float.TryParse(val, out var result))
        {
            value = result;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetInt(MessageParts key, [NotNullWhen(true)] out int? value)
    {
        if (GenericParameters.TryGetValue(key, out var val) && int.TryParse(val, out var result))
        {
            value = result;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetBool(MessageParts key, [NotNullWhen(true)] out bool? value)
    {
        if (GenericParameters.TryGetValue(key, out var val) && bool.TryParse(val, out var result))
        {
            value = result;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetString(MessageParts key, [NotNullWhen(true)] out string? value)
    {
        if (GenericParameters.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }

        value = null;
        return false;
    }
}

[Serializable, NetSerializable]
public enum MessageParts
{
    EntityName,
    SenderSession,
    RandomSeed,
    RadioChannel,
    GlobalAudioPath,
    GlobalAudioVolume,
    ColorFulltext,
    IsWhispering,
}
