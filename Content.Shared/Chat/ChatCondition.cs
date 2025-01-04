using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Chat;

public class AllChatCondition : IChatCondition
{
    /// <inheritdoc />
    public AllChatCondition()
    {
    }

    /// <inheritdoc />
    public AllChatCondition(List<ChatCondition> subconditions)
    {
        Subconditions = subconditions;
    }
    
    public List<ChatCondition> Subconditions = new();

    /// <inheritdoc />
    public bool Check(ChatMessageConditionSubject subject, ChatMessageContext channelParameters)
    {
        foreach (var chatCondition in Subconditions)
        {
            if (!chatCondition.Check(subject, channelParameters))
            {
                return false;
            }
        }

        return true;
    }
}

public class AnyChatCondition : IChatCondition
{
    /// <inheritdoc />
    public AnyChatCondition()
    {
    }

    /// <inheritdoc />
    public AnyChatCondition(List<IChatCondition> subconditions)
    {
        Subconditions = subconditions;
    }

    public List<IChatCondition> Subconditions = new();

    /// <inheritdoc />
    public bool Check(ChatMessageConditionSubject subject, ChatMessageContext channelParameters)
    {
        foreach (var chatCondition in Subconditions)
        {
            if (chatCondition.Check(subject, channelParameters))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class ChatMessageContext : Dictionary<Enum, object>
{
    /// <inheritdoc />
    public ChatMessageContext(IDictionary<Enum, object> dictionary) : base(dictionary)
    {
    }
}

public abstract partial class SessionChatConditionBase : ChatCondition
{
    /// <inheritdoc />
    protected override bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters)
    {
        return false;
    }
}

public abstract partial class EntityChatConditionBase : ChatCondition
{
    /// <inheritdoc />
    protected override bool Check(ICommonSession subjectEntity, ChatMessageContext channelParameters)
    {
        return false;
    }
}


public interface IChatCondition
{
    bool Check(
        ChatMessageConditionSubject subject,
        ChatMessageContext channelParameters
    );
}

[Serializable]
[DataDefinition]
[Virtual]
public abstract partial class ChatCondition : IChatCondition
{
    // If true, invert the result of the condition.
    [DataField]
    public bool Inverted = false;

    public virtual bool Check(
        ChatMessageConditionSubject subject,
        ChatMessageContext channelParameters
    )
    {
        if (subject.Entity.HasValue && Check(subject.Entity.Value, channelParameters))
        {
            return true;
        }

        if (subject.Session != null && Check(subject.Session, channelParameters))
        {
            return true;
        }

        return false;
    }

    protected abstract bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters);
    protected abstract bool Check(ICommonSession subjectEntity, ChatMessageContext channelParameters);

    ///// <summary>
    ///// Iterate over all the subconditions and process them.
    ///// </summary>
    ///// <param name="consumers">The hashset of consumers that should be evaluated against.</param>
    ///// <returns>Hashset of consumers processed by the subconditions.</returns>
    //private HashSet<T> IterateSubconditions<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    //{
    //    var changedConsumers = new HashSet<T>();
    //    foreach (var condition in Subconditions)
    //    {
    //        // No more consumers, no point in continuing further.
    //        if (changedConsumers.Count == consumers.Count)
    //            return changedConsumers;

    //        // If the condition doesn't do anything specific with the type, or if it's the same type as the input, just run it normally
    //        if (condition.ConsumerType == null || typeof(T) == condition.ConsumerType)
    //        {
    //            changedConsumers.UnionWith(condition.ProcessCondition(consumers, channelParameters));
    //        }
    //        // Converts the hashset of ICommonSessions to EntityUid, processes the condition, and then converts the result back to ICommonSessions
    //        else if (condition.ConsumerType == typeof(EntityUid) && consumers is HashSet<ICommonSession> sessionConsumers)
    //        {
    //            var sessionEntities = sessionConsumers.Where(z => z.AttachedEntity != null).Select(z => z.AttachedEntity!.Value).ToHashSet();
    //            var filteredEntities = condition.ProcessCondition<EntityUid>(sessionEntities, channelParameters);
    //            var filteredSessions = sessionConsumers.Where(z => filteredEntities.Contains(z.AttachedEntity ?? EntityUid.Invalid)).ToHashSet();
    //            changedConsumers.UnionWith(filteredSessions as HashSet<T> ?? new HashSet<T>());
    //        }
    //        // Converts the hashset of EntityUid to ICommonSessions, processes the condition, and then converts the result back to EntityUid
    //        else if (condition.ConsumerType == typeof(ICommonSession) && consumers is HashSet<EntityUid> entityConsumers)
    //        {
    //            var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
    //            if (entitySystemManager.TryGetEntitySystem<ActorSystem>(out var actorSystem))
    //            {

    //                HashSet<ICommonSession> entitySessions = new();
    //                foreach (var entity in entityConsumers)
    //                {
    //                    if (actorSystem.TryGetSession(entity, out var session) && session != null)
    //                        entitySessions.Add(session);
    //                }

    //                var filteredSessions =
    //                    condition.ProcessCondition<ICommonSession>(entitySessions, channelParameters);
    //                var filteredEntities = filteredSessions.Where(z => z.AttachedEntity != null)
    //                    .Select(z => z.AttachedEntity!.Value)
    //                    .ToHashSet();

    //                changedConsumers.UnionWith(filteredEntities as HashSet<T> ?? new HashSet<T>());
    //            }
    //        }
    //    }
    //    return changedConsumers;
    //}
}

public sealed class ChatMessageConditionSubject
{
    public ChatMessageConditionSubject(EntityUid entity)
    {
        Entity = entity;
    }

    public ChatMessageConditionSubject(ICommonSession? session)
    {
        Session = session;
    }

    public EntityUid? Entity { get; }
    public ICommonSession? Session { get; }
}
