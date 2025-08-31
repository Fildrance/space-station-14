using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

[NetSerializable, Serializable]
public sealed class ChatMessageContext
{
    public readonly Dictionary<string, string> GenericParameters;

    public ChatMessageContext(
        Dictionary<string, string> dictionary,
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

    public ChatMessageContext(Dictionary<string, string> dictionary)
    {
        GenericParameters = dictionary;
    }

    public ChatMessageContext() : this(new Dictionary<string, string>())
    {
    }

    public int Count => GenericParameters.Count;

    public void Set(string key, bool value)
    {
        GenericParameters[key] = value.ToString();
    }

    public void Set(string key, float value)
    {
        GenericParameters[key] = value.ToString(CultureInfo.InvariantCulture);
    }

    public void Set(string key, string value)
    {
        GenericParameters[key] = value;
    }

    public void Set(string key, int value)
    {
        GenericParameters[key] = value.ToString();
    }


    public bool TryGetFloat(string key, [NotNullWhen(true)] out float? value)
    {
        if (GenericParameters.TryGetValue(key, out var val) && float.TryParse(val, out var result))
        {
            value = result;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetInt(string key, [NotNullWhen(true)] out int? value)
    {
        if (GenericParameters.TryGetValue(key, out var val) && int.TryParse(val, out var result))
        {
            value = result;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetBool(string key, [NotNullWhen(true)] out bool? value)
    {
        if (GenericParameters.TryGetValue(key, out var val) && bool.TryParse(val, out var result))
        {
            value = result;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetString(string key, [NotNullWhen(true)] out string? value)
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

public static class MessageParts
{
    public const string EntityName = "entityName";
    public const string RadioChannel = "RadioChannel";
    public const string GlobalAudioPath = "GlobalAudioPath";
    public const string GlobalAudioVolume = "GlobalAudioVolume";
    public const string ColorFulltext = "ColorFulltext";
    public const string IsWhispering = "IsWhispering";
}
