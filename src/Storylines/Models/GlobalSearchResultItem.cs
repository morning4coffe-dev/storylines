namespace Storylines.Models;

public sealed class GlobalSearchResultItem(string title, string description, Action execute)
{
    public string Title { get; } = title;
    public string Description { get; } = description;
    public Action Execute { get; } = execute;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public Visibility DescriptionVisibility => string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;
    public double RowOpacity => Execute is null ? 0.68 : 1.0;

    public bool Matches(string query)
    {
        return Title?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0
            || Description?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
}
