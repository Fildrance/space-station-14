using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Chat.V2;

public sealed class ChatMessageContext(IDictionary<Enum, object> dictionary, EntityUid sender, uint id)
{
    private readonly IDictionary<Enum, object> _genericParameters = dictionary;

    public ChatMessageContext(
        Dictionary<Enum, object> dictionary,
        EntityUid sender,
        uint id,
        ChatMessageContext? otherContext
    ) : this(dictionary, sender, id)
    {
        if (otherContext == null)
            return;

        foreach (var (key, value) in otherContext._genericParameters)
        {
            _genericParameters[key] = value;
        }
    }

    public EntityUid Sender => sender;

    public uint MessageId => id;

    public object this[Enum key] { set => _genericParameters[key] = value; }

    public bool TryGet<T>(Enum key, [NotNullWhen(true)] out T? value)
    {
        if (_genericParameters.TryGetValue(key, out var val))
        {
            value = (T)val;
            return true;
        }

        value = default;
        return false;
    }
}
