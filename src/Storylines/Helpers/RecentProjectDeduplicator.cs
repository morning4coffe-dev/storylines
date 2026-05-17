
namespace Storylines.Helpers;

internal sealed class RecentProjectReference
{
    public RecentProjectReference(string token, string path)
    {
        Token = token;
        Path = path;
    }

    public string Token { get; }

    public string Path { get; }
}

internal static class RecentProjectDeduplicator
{
    public static IEnumerable<T> DistinctByPath<T>(IEnumerable<T> items, Func<T, string> pathSelector)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (pathSelector is null)
            throw new ArgumentNullException(nameof(pathSelector));

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var normalizedPath = NormalizePath(pathSelector(item));
            if (string.IsNullOrWhiteSpace(normalizedPath) || seenPaths.Add(normalizedPath))
                yield return item;
        }
    }

    public static string FindExistingToken(IEnumerable<RecentProjectReference> references, string path)
    {
        if (references is null)
            throw new ArgumentNullException(nameof(references));

        var normalizedPath = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
            return null;

        foreach (var reference in references)
        {
            if (string.Equals(normalizedPath, NormalizePath(reference.Path), StringComparison.OrdinalIgnoreCase))
                return reference.Token;
        }

        return null;
    }

    public static bool PathsMatch(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : path.Trim().Replace('/', '\\');
    }
}