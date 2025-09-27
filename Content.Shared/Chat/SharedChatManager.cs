using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Moderation;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract class SharedChatManager : ISharedChatManager
{
    [Dependency] protected readonly ICensorManager Censor = default!;
    
    public bool TryProcessChatMessage(ProducePlayerChatMessageEvent ev, EntitySessionEventArgs args)
    {
        var formattedMessage = ev.Message;

        if(!IsFittingRateLimit(ev, args))
            return false;

        // check message-rate
        if(!TryAddToRepository(ev))
            return false;

        var asMarkup = formattedMessage.ToMarkup();

        if (Censor.Censor(asMarkup, out var censored))
        {
            ev.Message = FormattedMessage.FromMarkupPermissive(censored);
        }

        return true;
    }

    protected abstract bool TryAddToRepository(ProducePlayerChatMessageEvent ev);

    protected abstract bool IsFittingRateLimit(ProducePlayerChatMessageEvent ev, EntitySessionEventArgs args);

    public abstract void Initialize();

    /// <inheritdoc />
    public abstract void SendAdminAlert(string message);

    /// <inheritdoc />
    public abstract void SendAdminAlert(EntityUid player, string message);
}
