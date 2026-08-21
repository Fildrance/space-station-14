using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Chat.V2.Moderation;

public sealed class CensorManager : ICensorManager
{
    private readonly ChatCensorFactory _factory = new ChatCensorFactory();

    private IChatCensor? _censor;

    public void Initialize()
    {
        _factory.Reset();

        // todo: read config and set censor up. ask FSP.

        _censor = _factory.Build();
    }

    public bool Censor(string input, [NotNullWhen(true)] out string output)
    {
        if (_censor == null)
        {
            output = input;
            return false;
        }

        return _censor.Censor(input, out output);
    }
}
