namespace Storylines.Services;

/// <summary>
/// Single source of truth for the BCP-47 language tags Storylines ships translations for.
/// Consumers (settings UI, language detection, manifest validation) must reference this list
/// rather than redeclaring it locally.
/// </summary>
public static class SupportedLanguages
{
    /// <summary>
    /// Default fallback language tag when no user preference matches a shipped translation.
    /// </summary>
    public const string DefaultTag = "en";

    /// <summary>
    /// Ordered list of supported BCP-47 language tags. Order is meaningful for UI presentation.
    /// </summary>
    public static IReadOnlyList<string> Tags { get; } = new[]
    {
        "en",
        "zh-CN",
        "ru",
        "it",
        "cs",
        "hi-IN",
        "pl",
    };
}
