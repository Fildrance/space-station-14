using System.Diagnostics.CodeAnalysis;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
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

    [DataField]
    public int ExclamationCount;

    public bool IsExclaiming => ExclamationCount > 0;
}

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class RadioCommunicationContextData : CommunicationContextData
{
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel;
}

[NetSerializable, Serializable]
public sealed partial class ChatMessageContext
{
    private readonly IDynamicTypeFactory _dtf;

    public ChatMessageContext(IDynamicTypeFactory dtf, int seed) : this(dtf, seed, null)
    {

    }

    public ChatMessageContext(IDynamicTypeFactory dtf, int seed, IReadOnlyCollection<CommunicationContextData>? additionalData = null)
    {
        _dtf = dtf;
        Seed = seed;
        Data = additionalData == null
            ? new()
            : new(additionalData);
    }

    [DataField]
    public List<CommunicationContextData> Data;

    [DataField]
    public string? EntityName;

    [DataField]
    public readonly int Seed;

    [DataField]
    public float? Distance;

    public void Set(CommunicationContextData data)
    {
        Data.Add(data);
    }

    public T Ensure<T>() where T : CommunicationContextData, new()
    {
        if (TryGet<T>(out var value))
        {
            return value;
        }

        var communicationContextData = _dtf.CreateInstance<T>();
        Set(communicationContextData);
        return communicationContextData;
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
