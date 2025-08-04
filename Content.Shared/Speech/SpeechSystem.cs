using Content.Shared.Chat.V2;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech;

public sealed class SpeechSystem : EntitySystem
{
    private static readonly ProtoId<CommunicationMediumPrototype> SpeechMedium = "Speech";

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

        if (ent.Comp.Enabled)
            return;

        args.CanHandle = true;
    }

    private void OnGetPotentialRecipients(Entity<SpeechComponent> ent, ref GetPotentialRecipientsChatMessageEvent args)
    {
        if (args.CommunicationChannel.ChatMedium != SpeechMedium)
            return;

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


            var inRange = distance <= ent.Comp.Range + comp.RangeChange;
            if (inRange)
                rangeByRecipient.Add(uid, distance);
        }

        args.Recipients.AddRange(rangeByRecipient.Keys);

        args.MessageContext[DefaultChannelParameters.RangeToEntities] = rangeByRecipient;

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
