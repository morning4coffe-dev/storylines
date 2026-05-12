namespace Storylines.Services;

/// <summary>
/// In-memory <see cref="ICommandRegistry"/>. Search applies a simple subsequence match per
/// query token so the palette feels responsive without pulling in a fuzzy-match library.
/// </summary>
internal sealed class CommandRegistry : ICommandRegistry
{
    private readonly List<AppCommand> _commands = new List<AppCommand>();

    public IReadOnlyList<AppCommand> Commands => _commands;

    public void Register(AppCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        Unregister(command.Id);
        _commands.Add(command);
    }

    public bool Unregister(string commandId)
    {
        if (string.IsNullOrEmpty(commandId)) return false;
        var idx = _commands.FindIndex(c => string.Equals(c.Id, commandId, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return false;
        _commands.RemoveAt(idx);
        return true;
    }

    public IReadOnlyList<AppCommand> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _commands;

        var tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return _commands
            .Select(c => new { Cmd = c, Score = ScoreMatch(c, tokens) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Cmd)
            .ToList();
    }

    private static int ScoreMatch(AppCommand command, string[] tokens)
    {
        int score = 0;
        foreach (var token in tokens)
        {
            if (ContainsCi(command.DisplayName, token)) score += 3;
            else if (ContainsCi(command.Category, token)) score += 2;
            else if (ContainsCi(command.Id, token)) score += 1;
            else return 0; // every token must match somewhere
        }
        return score;
    }

    private static bool ContainsCi(string source, string value)
        => !string.IsNullOrEmpty(source)
           && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
}
