using System.Runtime.InteropServices;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2.Repository;

/// <summary>
/// The record associated with a specific chat event.
/// </summary>
public struct ChatRecord(
    string userName,
    NetUserId userId,
    string originalMessage,
    ProtoId<CommunicationChannelPrototype> communicationChannel
)
{
    public string UserName = userName;
    public NetUserId UserId = userId;
    public string OriginalMessage = originalMessage;
    public ProtoId<CommunicationChannelPrototype> CommunicationChannel = communicationChannel;
}

/// <summary>
/// Notifies that a chat message has been created.
/// </summary>
/// <param name="ev"></param>
[Serializable, NetSerializable]
public sealed class MessageCreatedEvent(IChatEvent ev) : EntityEventArgs
{
    public IChatEvent Event = ev;
}

/// <summary>
/// Notifies that a chat message has been changed.
/// </summary>
/// <param name="id"></param>
/// <param name="newMessage"></param>
[Serializable, NetSerializable]
public sealed class MessagePatchedEvent(string id, string newMessage) : EntityEventArgs
{
    public string MessageId = id;
    public string NewMessage = newMessage;
}

/// <summary>
/// Notifies that a chat message has been deleted.
/// </summary>
/// <param name="id"></param>
[Serializable, NetSerializable]
public sealed class MessageDeletedEvent(string id) : EntityEventArgs
{
    public string MessageId = id;
}

/// <summary>
/// Notifies that a player's messages have been nuked.
/// </summary>
/// <param name="set"></param>
[Serializable, NetSerializable]
public sealed class MessagesNukedEvent(List<string> set) : EntityEventArgs
{
    public string[] MessageIds = CollectionsMarshal.AsSpan(set).ToArray();
}

