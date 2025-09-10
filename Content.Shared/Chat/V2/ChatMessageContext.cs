using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class CommunicationContextData
{

}

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class AudialCommunicationContextData : CommunicationContextData
{
    [DataField]
    public bool IsWhispering = false;
}

[NetSerializable, Serializable]
public sealed partial class ChatMessageContext
{
    [DataField]
    public List<CommunicationContextData> Data = new();

    [DataField]
    public string? EntityName;

    [DataField]
    public required int Seed;

    [DataField]
    public string TextColor { get; set; }
    
    public void Set(CommunicationContextData data)
    {
        Data.Add(data);
    }

    public bool TryGet<T>([NotNullWhen(true)]out T? result) where T : CommunicationContextData
    {
        result = null;
        foreach (var data in Data)
        {
            if (data is T casted)
            {
                result = casted;
                return true;
            }
        }

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
