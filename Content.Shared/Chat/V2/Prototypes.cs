using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Chat.V2;


[Serializable]
[Prototype("communicationChannel")]
public sealed partial class CommunicationChannelPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The prototype we inherit from.
    /// </summary>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CommunicationChannelPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// The way the message is conveyed in the game; audio, visual, OOC or such.
    /// </summary>
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public ProtoId<CommunicationMediumPrototype> ChatMedium;

    /// <summary>
    /// If set, a message published on the current channel will also try to publish to these communication channels.
    /// Channels are evaluated separately.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ProtoId<CommunicationChannelPrototype>> AlwaysRelayedToChannels = [];

    /// <summary>
    /// If set, a message that fails the conditions to publish on the current channel will try to publish to these communication channels instead.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ProtoId<CommunicationChannelPrototype>> FallbackChannels = [];

    /// <summary>
    /// If true, any message published to this channel won't show up in the chatbox.
    /// Useful for vending machines, bots and other speech bubble pop-ups.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool HideChat = false;

    /// <summary>
    /// Used as input for certain conditions and node suppliers, for when they need more customizability than what is defined in the channel prototype.
    /// An example of this would be overriding ColorFulltextMarkupChatModifier's color key when doing communication console alert levels, or defining radio channels.
    /// The parameters are *not* communicated between server and client, but may be shared via the prototype.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<Enum, object> ChannelParameters = new();
}

/// <summary>
/// Default channel parameters utilized by various conditions and suppliers. Not guaranteed to be set.
/// </summary>
[Serializable]
public enum DefaultChannelParameters
{
    SenderSession,
    RandomSeed,
    RadioChannel,
    GlobalAudioPath,
    GlobalAudioVolume,
    RangeToEntities,
}

public class CommunicationMediumPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;
}
