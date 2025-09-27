using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Repository;
using Robust.Shared.Network;

namespace Content.Server.Chat.V2.Repository;

public interface IChatRepositoryManager
{
    void Initialize();

    /// <summary>
    /// Adds an <see cref="IChatEvent"/> to the repo and raises it with a UID for consumption elsewhere.
    /// </summary>
    /// <param name="ev">The event to store and raise</param>
    /// <returns>If storing and raising succeeded.</returns>
    bool TryAdd(ProducePlayerChatMessageEvent ev);

    /// <summary>
    /// Edits a specific message and issues a <see cref="MessagePatchedEvent"/> that says this happened both locally and
    /// on the network. Note that this doesn't replay the message (yet), so translators and mutators won't act on it.
    /// </summary>
    /// <param name="id">The ID to edit</param>
    /// <param name="message">The new message to send</param>
    /// <returns>If patching did anything did anything</returns>
    /// <remarks>Should be used for admining and admemeing only.</remarks>
    bool Patch(string id, string message);

    /// <summary>
    /// Deletes a message from the repository and issues a <see cref="MessageDeletedEvent"/> that says this has happened
    /// both locally and on the network.
    /// </summary>
    /// <param name="id">The ID to delete</param>
    /// <returns>If deletion did anything</returns>
    /// <remarks>Should only be used for adminning</remarks>
    bool Delete(string id);

    /// <summary>
    /// Nukes a user's entire chat history from the repo and issues a <see cref="MessageDeletedEvent"/> saying this has
    /// happened.
    /// </summary>
    /// <param name="userName">The user ID to nuke.</param>
    /// <param name="reason">Why nuking failed, if it did.</param>
    /// <returns>If nuking did anything.</returns>
    /// <remarks>Note that this could be a <b>very large</b> event, as we send every single event ID over the wire.
    /// By necessity we can't leak the player-source of chat messages (or if they even have the same origin) because of
    /// client modders who could use that information to cheat/metagrudge/etc >:(</remarks>
    bool NukeForUsername(string userName, [NotNullWhen(false)] out string? reason);

    /// <summary>
    /// Nukes a user's entire chat history from the repo and issues a <see cref="MessageDeletedEvent"/> saying this has
    /// happened.
    /// </summary>
    /// <param name="userId">The user ID to nuke.</param>
    /// <param name="reason">Why nuking failed, if it did.</param>
    /// <returns>If nuking did anything.</returns>
    /// <remarks>Note that this could be a <b>very large</b> event, as we send every single event ID over the wire.
    /// By necessity we can't leak the player-source of chat messages (or if they even have the same origin) because of
    /// client modders who could use that information to cheat/metagrudge/etc >:(</remarks>
    bool NukeForUserId(NetUserId userId, [NotNullWhen(false)] out string? reason);

    /// <summary>
    /// Dumps held chat storage data and refreshes the repo.
    /// </summary>
    void Refresh();
}
