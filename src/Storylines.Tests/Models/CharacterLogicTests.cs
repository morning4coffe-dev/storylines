using Xunit;

namespace Storylines.Tests.Models;

/// <summary>
/// Tests for the pure business logic in Character: traitsText parsing/formatting
/// and the detailsLine computed property.
///
/// Character.cs has Windows/WinUI dependencies (BitmapImage, ApplicationData, etc.)
/// so these helpers mirror only the portable logic from that class, keeping these
/// tests free of any platform dependencies.
/// </summary>
public class CharacterLogicTests
{
    #region Helpers mirroring Character logic

    // Mirrors Character.traitsText setter
    private static List<string> ParseTraitsText(string? value) =>
        (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    // Mirrors Character.TraitsText getter
    private static string FormatTraitsText(List<string> traits) =>
        traits == null || traits.Count == 0 ? string.Empty : string.Join(", ", traits);

    // Mirrors Character.DetailsLine getter
    private static string BuildDetailsLine(string? role, string? age, List<string>? traits, string? description)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(role))
            details.Add(role!);
        if (!string.IsNullOrWhiteSpace(age))
            details.Add(age!);
        if (traits != null && traits.Count > 0)
            details.Add(string.Join(", ", traits.Take(2)) + (traits.Count > 2 ? "\u2026" : string.Empty));

        return details.Count > 0
            ? string.Join(" \u00b7 ", details)
            : description ?? string.Empty;
    }

    #endregion

    #region traitsText — parsing (setter logic)

    [Fact]
    public void ParseTraitsText_CommaSeparatedValues_ProducesCorrectList()
    {
        var traits = ParseTraitsText("brave, kind, clever");
        Assert.Equal(new[] { "brave", "kind", "clever" }, traits);
    }

    [Fact]
    public void ParseTraitsText_TrimsWhitespaceFromEachTrait()
    {
        var traits = ParseTraitsText("  brave  ,  kind  ");
        Assert.Equal(new[] { "brave", "kind" }, traits);
    }

    [Fact]
    public void ParseTraitsText_RemovesDuplicatesCaseInsensitively()
    {
        var traits = ParseTraitsText("Brave, brave, BRAVE");
        Assert.Single(traits);
    }

    [Fact]
    public void ParseTraitsText_EmptyString_ReturnsEmptyList()
    {
        var traits = ParseTraitsText(string.Empty);
        Assert.Empty(traits);
    }

    [Fact]
    public void ParseTraitsText_Null_ReturnsEmptyList()
    {
        var traits = ParseTraitsText(null);
        Assert.Empty(traits);
    }

    [Fact]
    public void ParseTraitsText_WhitespaceOnly_ReturnsEmptyList()
    {
        var traits = ParseTraitsText("   ,   ,  ");
        Assert.Empty(traits);
    }

    [Fact]
    public void ParseTraitsText_SingleTrait_ReturnsSingleElement()
    {
        var traits = ParseTraitsText("hero");
        Assert.Single(traits);
        Assert.Equal("hero", traits[0]);
    }

    #endregion

    #region traitsText — formatting (getter logic)

    [Fact]
    public void FormatTraitsText_EmptyList_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, FormatTraitsText(new List<string>()));
    }

    [Fact]
    public void FormatTraitsText_SingleTrait_ReturnsJustThatTrait()
    {
        Assert.Equal("brave", FormatTraitsText(new List<string> { "brave" }));
    }

    [Fact]
    public void FormatTraitsText_MultipleTraits_JoinsWithCommaSpace()
    {
        Assert.Equal("brave, kind, clever", FormatTraitsText(new List<string> { "brave", "kind", "clever" }));
    }

    #endregion

    #region detailsLine

    [Fact]
    public void DetailsLine_WithRoleAgeAndTwoTraits_FormatsCorrectly()
    {
        var result = BuildDetailsLine("Hero", "30", new List<string> { "brave", "kind" }, "A hero.");
        Assert.Equal("Hero · 30 · brave, kind", result);
    }

    [Fact]
    public void DetailsLine_OnlyRole_ShowsJustRole()
    {
        var result = BuildDetailsLine("Villain", null, new List<string>(), "Desc");
        Assert.Equal("Villain", result);
    }

    [Fact]
    public void DetailsLine_OnlyAge_ShowsJustAge()
    {
        var result = BuildDetailsLine(null, "25", new List<string>(), "Desc");
        Assert.Equal("25", result);
    }

    [Fact]
    public void DetailsLine_NoRoleNoAgeNoTraits_FallsBackToDescription()
    {
        var result = BuildDetailsLine(null, null, new List<string>(), "Just a description");
        Assert.Equal("Just a description", result);
    }

    [Fact]
    public void DetailsLine_MoreThanTwoTraits_ShowsEllipsis()
    {
        var result = BuildDetailsLine(null, null, new List<string> { "a", "b", "c", "d" }, "Desc");
        Assert.Equal("a, b\u2026", result);
    }

    [Fact]
    public void DetailsLine_ExactlyTwoTraits_NoEllipsis()
    {
        var result = BuildDetailsLine(null, null, new List<string> { "a", "b" }, "Desc");
        Assert.Equal("a, b", result);
    }

    [Fact]
    public void DetailsLine_ExactlyThreeTraits_ShowsEllipsisAfterSecond()
    {
        var result = BuildDetailsLine(null, null, new List<string> { "a", "b", "c" }, "Desc");
        Assert.Equal("a, b\u2026", result);
    }

    [Fact]
    public void DetailsLine_WhitespaceRole_TreatedAsAbsent_FallsBackToDescription()
    {
        var result = BuildDetailsLine("   ", null, new List<string>(), "description");
        Assert.Equal("description", result);
    }

    #endregion
}
