using Content.Shared.Chat.V2;

namespace Content.Shared.Chat;

public interface ISharedChatManager
{
    void Initialize();
    void SendAdminAlert(string message);
    void SendAdminAlert(EntityUid player, string message);

    bool TryProcessChatMessage(ProducePlayerChatMessageEvent ev, EntitySessionEventArgs args);
}
