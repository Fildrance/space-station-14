using Content.Shared.Chat;
using Content.Shared.Chat.V2;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared;

public sealed class AuditoryCommunicationSystem : EntitySystem
{
    private static readonly ProtoId<CommunicationMediumPrototype> SpeechMedium = "Auditory";
    private static readonly ProtoId<CommunicationChannelPrototype> SpeechChannel = "ICSpeech";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AuditoryReceiverComponent, GetRefinedReceiverChatMessageEvent>(OnRefineReceiverChatMessage);
        SubscribeLocalEvent<AuditoryReceiverComponent, GetRefinedProducedChatMessageEvent>(OnRefineProducedChatMessage);
        SubscribeLocalEvent<SpeechComponent, GetPotentialRecipientsChatMessageEvent>(OnGetPotentialRecipients);
        SubscribeLocalEvent<SpeechComponent, AttemptSendChatMessageEvent>(OnAttemptSendChatMessage);
    }

    private void OnRefineProducedChatMessage(Entity<AuditoryReceiverComponent> ent, ref GetRefinedProducedChatMessageEvent args)
    {
        if (args.CommunicationChannel != SpeechChannel)
            return;

        var nameEv = new TransformSpeakerNameEvent(ent, MetaData(ent).EntityName);
        RaiseLocalEvent(ent, nameEv);
        args.MessageContext.EntityName = nameEv.VoiceName;
        // get owner accents?
        // hook into other stuff?
    }

    private void OnAttemptSendChatMessage(Entity<SpeechComponent> ent, ref AttemptSendChatMessageEvent args)
    {
        if (args.CommunicationChannel != SpeechChannel)
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

        var query = EntityQueryEnumerator<AuditoryReceiverComponent>();
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
            {
                args.Recipients.Add(uid);
                data.DistanceByRecipient.Add(GetNetEntity(uid), distance);
            }
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
            if (node.Name != null || node.Value.StringValue == null)
                continue;

            foreach (var text in node.Value.StringValue)
            {
                const char exclamationChar = '!';
                if (text != exclamationChar)
                    continue;

                exclamationCount++;
                if (exclamationCount == 3)
                    return exclamationCount;
            }
        }

        return exclamationCount;
    }

    private void OnRefineReceiverChatMessage(Entity<AuditoryReceiverComponent> ent, ref GetRefinedReceiverChatMessageEvent args)
    {
        if (args.Sender == ent.Owner)
            return;

        if (!args.MessageContext.TryGet<AudialCommunicationContextData>(out var data) || !data.IsWhispering)
            return;

        // chance for part of text to be obfuscated starts at WhisperClearlyRange and grows as log
        float distanceMod = 1;
        if (data.DistanceByRecipient.TryGetValue(GetNetEntity(ent.Owner), out var distance)
            && TryComp<SpeechComponent>(args.Sender, out var speech))
        {
            distanceMod = distance < ent.Comp.WhisperClearlyRange
                ? 0
                : MathF.Log10(distance / (ent.Comp.RangeChange + speech.WhisperRange)) + 1;
        }

        var obfuscationChance = ent.Comp.WhisperObfuscationMaxChance * distanceMod;
        if (obfuscationChance > 0.05)
        {
            var obfuscated = ProcessChatModifier(obfuscationChance, args.Message, args.MessageContext);
            args = new GetRefinedReceiverChatMessageEvent(args, obfuscated);
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
