﻿using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Chat.Prototypes;

[Serializable]
[Prototype("communicationChannel")]
public sealed partial class CommunicationChannelPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The prototype we inherit from.
    /// </summary>
    [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<CommunicationChannelPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// The conditions required for a session (i.e. client) to be allowed to publish via this communication channel.
    /// </summary>
    [DataField(serverOnly: true)]
    [AlwaysPushInheritance]
    public List<SessionChatCondition> PublishSessionChatConditions = new();

    /// <summary>
    /// The conditions required for an entity (non-client) to be allowed to publish via this communication channel.
    /// </summary>
    [DataField(serverOnly: true)]
    [AlwaysPushInheritance]
    public List<EntityChatCondition> PublishEntityChatConditions = new();

    /// <summary>
    /// A collection of consumer conditions and applicable markup tags.
    /// Clients/entities prioritize the ConsumeCollections in order; if a consumer is active in a collection it will not be included in any subsequent collections in the list.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ConsumeCollection> ConsumeCollections = new();

    /// <summary>
    /// The kind of communication types this channel utilizes (e.g. speech, OOC, etc.)
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ChatChannel ChatChannels = ChatChannel.None;

    /// <summary>
    /// If true, the server may publish to this channel.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool AllowServerMessages = true;

    /// <summary>
    /// If true, sessions (i.e. players) may publish to this channel.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool AllowSessionMessages = true;

    /// <summary>
    /// If true, an entity does not need to be attached to publish to this channel.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool AllowEntitylessMessages = true;

    /// <summary>
    /// If true, the same message may not be published twice on the same channel.
    /// Useful to be true for radio channels but false for whispers, to allow relaying via intercoms without looping.
    /// EXTREME ATTENTION TO AVOID LOOPING must be paid if this value is set to false, as improper message relay can cause an infinite loop.
    /// If someone knows how to make a good check to avoid that, please implement it!
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool NonRepeatable = true;

    /// <summary>
    /// If set, a message published on the current channel will also try to publish to these communication channels.
    /// Channels are evaluated separately.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ProtoId<CommunicationChannelPrototype>>? AlwaysChildCommunicationChannels;

    /// <summary>
    /// If set, a message that fails the conditions to publish on the current channel will try to publish to these communication channels instead.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ProtoId<CommunicationChannelPrototype>>? BackupChildCommunicationChannels;

    /// <summary>
    /// Contains markup node suppliers that are applied and processed on the client.
    /// Clientsided ChatModifiers may provide text nodes and include text formatting;
    /// because of this, attention should be paid to the list order to ensure the correct order of operations.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ChatModifier> ClientChatModifiers = new();

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

[Serializable]
[DataDefinition]
public partial struct ConsumeCollection
{
    /// <summary>
    /// The conditions required for a session (i.e. client) to be allowed to consume this communication channel.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<SessionChatCondition> ConsumeSessionChatConditions = new();

    /// <summary>
    /// The conditions required for an entity (non-client) to be allowed to consume this communication channel.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<EntityChatCondition> ConsumeEntityChatConditions = new();

    /// <summary>
    /// Contains markup node suppliers that are applied and processed on the server.
    /// Serversided node suppliers should avoid adding text nodes or formatting tags where possible.
    /// If such nodes are needed, it's better to supply a tag that gets converted appropriately on the client instead.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<ChatModifier> CollectionChatModifiers = new();

    public ConsumeCollection(
        List<SessionChatCondition> consumeSessionChatConditions,
        List<EntityChatCondition> consumeEntityChatConditions,
        List<ChatModifier> collectionChatModifiers)
    {
        ConsumeSessionChatConditions = consumeSessionChatConditions;
        ConsumeEntityChatConditions = consumeEntityChatConditions;
        CollectionChatModifiers = collectionChatModifiers;
    }
}

/// <summary>
/// Default channel parameters utilized by various conditions and suppliers. Not guaranteed to be set.
/// </summary>
[Serializable]
public enum DefaultChannelParameters
{
    SenderEntity,
    SenderSession,
    RandomSeed,
    RadioChannel,
}
