using System.Text.RegularExpressions;

namespace Content.Shared.Chat.V2.Moderation;

public sealed class RegexCensor(Regex censorInstruction) : IChatCensor
{
    public bool Censor(string input, out string output, char replaceWith = '*')
    {
        output = censorInstruction.Replace(input, replaceWith.ToString());

        return !string.Equals(input, output);
    }
}
