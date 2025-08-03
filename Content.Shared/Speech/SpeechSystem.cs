using Content.Shared.Chat.V2;
using Robust.Shared.Collections;
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

        args = new AttemptSendChatMessageEvent(args.MessageContext, args.CommunicationChannel)
        {
            CanHandle = true
        };
    }

    private void OnGetPotentialRecipients(Entity<SpeechComponent> ent, ref GetPotentialRecipientsChatMessageEvent args)
    {
        if (args.CommunicationChannel.ChatMedium != SpeechMedium)
            return;

        ValueList<EntityUid> recipients = new();
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
            var inRange = sourceTransform.Coordinates.TryDistance(EntityManager, targetTransform.Coordinates, out var distance)
                          && distance < ent.Comp.MaximumRange + comp.MaxRangeChange
                          && distance >= ent.Comp.MinimumRange + comp.MinRangeChange;
            if (inRange)
                recipients.Add(uid);
        }
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
