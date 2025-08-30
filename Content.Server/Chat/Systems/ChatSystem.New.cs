using Content.Shared.Chat.V2;
using Robust.Shared.Player;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private void InitializeSendMessage()
    {
        SubscribeLocalEvent<ActorComponent, ReceiveChatMessageEvent>(Handler);
    }

    private void Handler(Entity<ActorComponent> ent, ref ReceiveChatMessageEvent args)
    {
        var senderNetEntity = GetNetEntity(args.Sender);
        if(!senderNetEntity.HasValue)
            return;

        var chatMessageWrapper = new ReceiveChatMessage(senderNetEntity.Value, args.Message, args.MessageContext, args.CommunicationChannel);
        RaiseNetworkEvent(chatMessageWrapper, ent);
    }
}
