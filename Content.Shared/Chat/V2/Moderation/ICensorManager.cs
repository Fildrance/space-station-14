using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Chat.V2.Moderation;

public interface ICensorManager
{
    void Initialize();

    bool Censor(string input, [NotNullWhen(true)] out string? output);
}
