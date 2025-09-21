using Content.Shared.Chat.V2;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Random;

namespace Content.Shared.Speech;

public sealed class SpeechSystem : EntitySystem
{
    private static readonly ProtoId<CommunicationMediumPrototype> SpeechMedium = "Auditory";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<SpeechReceiverComponent, AttemptReceiveChatMessageEvent>(OnAttemptReceive);
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

        var data = args.MessageContext.Ensure<AudialCommunicationContextData>(() => new());
        var isWhispering = data.IsWhispering;
        var exclamationsCount = CountExclamation(args.Message);
        data.ExclamationCount = exclamationsCount;

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
                : GetRange(ent.Comp, exclamationsCount);

            var inRange = distance <= range + comp.RangeChange;
            if (inRange)
                args.DistanceByRecipient.Add(uid, distance);
        }
    }

    private static float GetRange(SpeechComponent component, int exclamationCount)
    {
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

    private void OnAttemptReceive(Entity<SpeechReceiverComponent> ent, ref AttemptReceiveChatMessageEvent args)
    {
        if(args.Sender == ent.Owner)
            return;

        if(!args.MessageContext.TryGet<AudialCommunicationContextData>(out var data) || !data.IsWhispering)
            return;

        // chance for part of text to be obfuscated starts at WhisperClearlyRange and grows as log
        float distanceMod = 1;
        var distance = args.MessageContext.Distance;
        if (distance.HasValue && TryComp<SpeechComponent>(args.Sender, out var speech))
        {
            distanceMod = distance < ent.Comp.WhisperClearlyRange
                ? 0
                : MathF.Log10(distance.Value / (ent.Comp.RangeChange + speech.WhisperRange)) + 1;
        }

        var obfuscationChance = ent.Comp.WhisperObfuscationMaxChance * distanceMod;
        if (obfuscationChance > 0.05)
        {
            var obfuscated = ProcessChatModifier(obfuscationChance, args.Message, args.MessageContext);
            args = new AttemptReceiveChatMessageEvent(args.Sender, args.MessageContext, obfuscated);
        }
    }

    private static FormattedMessage ProcessChatModifier(float obfuscationChance, FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        var newMessage = new FormattedMessage(message);

        var random = new System.Random(chatMessageContext.Seed);

        for (int i = 0; i < newMessage.Count; i++)
        {
            var node = newMessage.Nodes[i];
            if (node.Name == null && node.Value.TryGetString(out var text))
            {
                var obfuscated = ObfuscateMessageReadability(random, text, obfuscationChance);
                newMessage.ReplaceTextNode(node, new MarkupNode(obfuscated));
            }
        }

        return newMessage;
    }

    private static string ObfuscateMessageReadability(System.Random random, string message, float chance)
    {
        var charArray = message.ToCharArray();
        for (var i = 0; i < charArray.Length; i++)
        {
            if (char.IsWhiteSpace((charArray[i])))
            {
                continue;
            }

            if (random.Prob(chance))
            {
                charArray[i] = '~';
            }
        }

        return new string(charArray);
    }
}
