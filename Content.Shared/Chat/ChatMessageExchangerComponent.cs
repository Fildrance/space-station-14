using Content.Shared.Chat.V2;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

[Serializable, NetSerializable]
public sealed class ChatMessageDataForExchange(
    ProtoId<CommunicationChannelPrototype> channel,
    FormattedMessage message,
    ChatMessageContext context,
    NetEntity? sender,
    GameTick pushedOnTick
)
{
    public ProtoId<CommunicationChannelPrototype> Channel = channel;
    public FormattedMessage Message = message;
    public ChatMessageContext Context = context;
    public NetEntity? Sender = sender;
    public GameTick PushedOnTick = pushedOnTick;
}

[RegisterComponent]
[Access(typeof(SharedChatSystem))]
[NetworkedComponent]
public sealed partial class ChatMessageExchangerComponent : Component
{
    public readonly Dictionary<Guid, ChatMessageDataForExchange> Messages = new();

    public NetUserId UserId;
    public GameTick LastModified;

    [Serializable, NetSerializable]
    public sealed class ChatMessageExchangerState(Dictionary<Guid, ChatMessageDataForExchange> messages) : ComponentState
    {
        public Dictionary<Guid, ChatMessageDataForExchange> Messages = messages;
    }

    [Serializable, NetSerializable]
    public sealed class ChatMessageExchangerDeltaState(Dictionary<Guid, ChatMessageDataForExchange> modifiedMessages, HashSet<Guid> allChunks)
        : ComponentState, IComponentDeltaState<ChatMessageExchangerState>
    {
        public Dictionary<Guid, ChatMessageDataForExchange> ModifiedMessages = modifiedMessages;
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
            var chunks = new Dictionary<Guid, ChatMessageDataForExchange>(state.Messages.Count);

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
