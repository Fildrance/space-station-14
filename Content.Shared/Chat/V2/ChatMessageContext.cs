using System.Diagnostics.CodeAnalysis;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

[NetSerializable, Serializable, DataDefinition]
public sealed partial class ChatMessageContext
{
    public ChatMessageContext(int seed) 
    {
        Seed = seed;
        Data = new();
    }

    public ChatMessageContext(int seed, Dictionary<string, CommunicationContextData>? additionalData = null) : this(seed)
    {
        if (additionalData == null)
            return;

        foreach (var data in additionalData)
        {
            Data.Add(data.Key, data.Value);
        }
    }

    public ChatMessageContext(int seed, List<CommunicationContextData>? additionalData = null) : this(seed)
    {
        if (additionalData == null)
            return;

        foreach (var data in additionalData)
        {
            Data.Add(data.GetType().FullName!, data);
        }
    }

    [DataField]
    public Dictionary<string, CommunicationContextData> Data;

    [DataField]
    public string? EntityName;

    [DataField]
    public int Seed;

    [DataField]
    public float? Distance;

    public void Set<T>(T data) where T : CommunicationContextData
    {
        Data.Add(typeof(T).Name, data);
    }

    public void Set(CommunicationContextData data)
    {
        Data.Add(data.GetType().FullName!, data);
    }

    public T Ensure<T>(Func<T> factory) where T : CommunicationContextData, new()
    {
        var key = typeof(T).FullName!;
        if (Data.TryGetValue(key, out var val) && val is T casted)
        {
            return casted;
        }

        var communicationContextData = factory();
        Set(communicationContextData);
        return communicationContextData;
    }

    public bool TryGet<T>([NotNullWhen(true)]out T? result) where T : CommunicationContextData
    {
        result = null;
        var key = typeof(T).FullName!;
        if (Data.TryGetValue(key, out var val) && val is T casted)
        {
            result = casted;
            return true;
        }

        return false;
    }

    public bool Contains<T>()
    {
        var key = typeof(T).FullName!;
        if (Data.TryGetValue(key, out var val) && val is T)
        {
            return true;
        }

        return false;
    }

    public List<CommunicationContextData> GetRetainedData(ProtoId<CommunicationChannelPrototype> forCommunicationChannel)
    {
        var list = new List<CommunicationContextData>();
        foreach (var (_, value) in Data)
        {
            value.IsRetainedFor(forCommunicationChannel);
        }

        return list;
    }
}

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class CommunicationContextData
{
    public abstract bool IsRetainedFor(ProtoId<CommunicationChannelPrototype> forCommunicationChannel);
}

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class LanguageCommunicationContextData:CommunicationContextData
{
    public override bool IsRetainedFor(ProtoId<CommunicationChannelPrototype> forCommunicationChannel)
    {
        return true;
    }
}

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class AudialCommunicationContextData : CommunicationContextData
{
    [DataField]
    public bool IsWhispering = false;

    [DataField]
    public int ExclamationCount;

    public bool IsExclaiming => ExclamationCount > 0;

    public Dictionary<NetEntity, float> DistanceByRecipient = new();

    public override bool IsRetainedFor(ProtoId<CommunicationChannelPrototype> forCommunicationChannel)
    {
        return false;
    }
}

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class RadioCommunicationContextData : CommunicationContextData
{
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel;

    public override bool IsRetainedFor(ProtoId<CommunicationChannelPrototype> forCommunicationChannel)
    {
        return false; // retain only for other radio com channel?
    }
}
