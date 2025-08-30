using Content.Shared.Chat.V2;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Speech;

public sealed class SpeechSystem : EntitySystem
{
    private static readonly ProtoId<CommunicationMediumPrototype> SpeechMedium = "Auditory";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<SpeechComponent, GetPotentialRecipientsChatMessageEvent>(OnGetPotentialRecipients);
        SubscribeLocalEvent<SpeechComponent, AttemptSendChatMessageEvent>(OnAttemptSendChatMessage);
    }

    private void OnAttemptSendChatMessage(Entity<SpeechComponent> ent, ref AttemptSendChatMessageEvent args)
    {
        if (args.CommunicationChannel.ChatMedium != SpeechMedium)
            return;

        if (!ent.Comp.Enabled)
            return;

        args.CanHandle = true;
    }

    private void OnGetPotentialRecipients(Entity<SpeechComponent> ent, ref GetPotentialRecipientsChatMessageEvent args)
    {
        if (args.CommunicationChannel.ChatMedium != SpeechMedium)
            return;

        var isWhispering = args.MessageContext.TryGetBool(MessageParts.IsWhispering, out var result) && result.Value;

        var rangeByRecipient = new Dictionary<EntityUid, float>();
        var query = EntityQueryEnumerator<SpeechReceiverComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var sourceTransform = Transform(ent);
            var targetTransform = Transform(uid);

            if (targetTransform.MapID != sourceTransform.MapID)
                continue;

            // If you wanted to do something like a hard-of-hearing trait, our hearing extension component,
            // this is probably where you'd check for it.
            // Even if they are a ghost hearer, in some situations we still need the range
            var targetCoordinates = targetTransform.Coordinates;

            if (!sourceTransform.Coordinates.TryDistance(EntityManager, targetCoordinates, out var distance))
                continue;

            var range = isWhispering
                ? ent.Comp.WhisperRange
                : GetRange(ent.Comp, args.Message);

            var inRange = distance <= range + comp.RangeChange;
            if (inRange)
                rangeByRecipient.Add(uid, distance);
        }

        args.Recipients.AddRange(rangeByRecipient.Keys);
    }

    private static float GetRange(SpeechComponent component, FormattedMessage message)
    {
        var exclamationCount = CountExclamation(message);
        var additionalRange = component.YellingAdditionalRange * exclamationCount;
        return component.Range + additionalRange;
    }

    private static int CountExclamation(FormattedMessage message)
    {
        var exclamationCount = 0;
        foreach (var node in message)
        {
            if (node.Name == null && node.Value.StringValue != null)
            {
                foreach (var text in node.Value.StringValue)
                {
                    if (text == '!')
                    {
                        exclamationCount++;
                        if (exclamationCount == 3)
                            return exclamationCount;
                    }
                }
            }
        }

        return exclamationCount;
    }

    public void SetSpeech(EntityUid uid, bool value, SpeechComponent? component = null)
    {
        if (value && !Resolve(uid, ref component))
            return;

        component = EnsureComp<SpeechComponent>(uid);

        if (component.Enabled == value)
            return;

        component.Enabled = value;
                
        Dirty(uid, component);
    }

    private void OnSpeakAttempt(SpeakAttemptEvent args)
    {
        if (!TryComp(args.Uid, out SpeechComponent? speech) || !speech.Enabled)
            args.Cancel();
    }
}
