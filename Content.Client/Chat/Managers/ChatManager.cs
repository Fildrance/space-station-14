using Content.Client.Administration.Managers;
using Content.Client.Ghost;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Chat.V2;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Chat.Managers;

internal sealed class ChatManager : IChatManager
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly IClientAdminManager _adminMgr = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IPlayerManager _playerManager= default!;

    private static readonly ProtoId<CommunicationChannelPrototype> SpeechChannel = "ICSpeech";

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("chat");
        _sawmill.Level = LogLevel.Info;
    }

    public void SendAdminAlert(string message)
    {
        // See server-side manager. This just exists for shared code.
    }

    public void SendAdminAlert(EntityUid player, string message)
    {
        // See server-side manager. This just exists for shared code.
    }

    public void SendMessage(string text, ChatSelectChannel channel)
    {
        if (channel is ChatSelectChannel.Whisper or ChatSelectChannel.Local)
        {
            List<CommunicationContextData>? data = null;
            if (channel == ChatSelectChannel.Whisper)
            {
                data = new List<CommunicationContextData>
                {
                    new AudialCommunicationContextData { IsWhispering = true }
                };
            }
            _systems.GetEntitySystem<ChatSystem>()
                    .SendMessage(SpeechChannel, _playerManager.LocalEntity, text, data);
            return;
        }

        switch (channel)
        {
            case ChatSelectChannel.Console:
                // run locally
                _consoleHost.ExecuteCommand(text);
                break;

            case ChatSelectChannel.LOOC:
                _consoleHost.ExecuteCommand($"looc \"{CommandParsing.Escape(text)}\"");
                break;

            case ChatSelectChannel.OOC:
                _consoleHost.ExecuteCommand($"ooc \"{CommandParsing.Escape(text)}\"");
                break;

            case ChatSelectChannel.Admin:
                _consoleHost.ExecuteCommand($"asay \"{CommandParsing.Escape(text)}\"");
                break;

            case ChatSelectChannel.Emotes:
                _consoleHost.ExecuteCommand($"me \"{CommandParsing.Escape(text)}\"");
                break;

            case ChatSelectChannel.Dead:
                if (_systems.GetEntitySystemOrNull<GhostSystem>() is {IsGhost: true})
                    goto case ChatSelectChannel.Radio;

                if (_adminMgr.HasFlag(AdminFlags.Admin))
                    _consoleHost.ExecuteCommand($"dsay \"{CommandParsing.Escape(text)}\"");
                else
                    _sawmill.Warning("Tried to speak on deadchat without being ghost or admin.");
                break;

            // TODO sepearate radio and say into separate commands.
            case ChatSelectChannel.Radio:
                _consoleHost.ExecuteCommand($"say \"{CommandParsing.Escape(text)}\"");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
        }
    }
}
