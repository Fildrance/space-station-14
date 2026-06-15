using Content.Shared.Chat.V2;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

[RegisterComponent]
[Access(typeof(SharedChatSystem))]
[NetworkedComponent]
public sealed partial class ChatMessageExchangerComponent : Component
{
    public readonly Dictionary<Guid, (ProtoId<CommunicationChannelPrototype> channel, FormattedMessage message, ChatMessageContext context, NetEntity? sender)> Messages = new();

    public NetUserId UserId;
    public GameTick LastModified;

    [Serializable, NetSerializable]
    public sealed class ChatMessageExchangerState(Dictionary<Guid, (ProtoId<CommunicationChannelPrototype> channel, FormattedMessage message, ChatMessageContext context, NetEntity? sender)> messages) : ComponentState
    {
        public Dictionary<Guid, (ProtoId<CommunicationChannelPrototype> channel, FormattedMessage message, ChatMessageContext context, NetEntity? sender)> Messages = messages;
    }

    [Serializable, NetSerializable]
    public sealed class ChatMessageExchangerDeltaState(Dictionary<Guid, (ProtoId<CommunicationChannelPrototype> channel, FormattedMessage message, ChatMessageContext context, NetEntity? sender)> modifiedMessages, HashSet<Guid> allChunks)
        : ComponentState, IComponentDeltaState<ChatMessageExchangerState>
    {
        public Dictionary<Guid, (ProtoId<CommunicationChannelPrototype> channel, FormattedMessage message, ChatMessageContext context, NetEntity? sender)> ModifiedMessages = modifiedMessages;
        public HashSet<Guid> AllChunks = allChunks;

        public void ApplyToFullState(ChatMessageExchangerState state)
        {
            foreach (var key in state.Messages.Keys)
            {
                if (!AllChunks!.Contains(key))
                    state.Messages.Remove(key);
            }

            foreach (var (chunk, data) in ModifiedMessages)
            {
                state.Messages[chunk] = data;
            }
        }

        public ChatMessageExchangerState CreateNewFullState(ChatMessageExchangerState state)
        {
            var chunks = new Dictionary<Guid, (ProtoId<CommunicationChannelPrototype> channel, FormattedMessage message, ChatMessageContext context, NetEntity? sender)>(state.Messages.Count);

            foreach (var (chunk, data) in ModifiedMessages)
            {
                chunks[chunk] = data;
            }

            foreach (var (chunk, data) in state.Messages)
            {
                if (AllChunks!.Contains(chunk))
                    chunks.TryAdd(chunk, data);
            }
            return new ChatMessageExchangerState(chunks);
        }
    }
}
