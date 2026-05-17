
namespace Storylines.Models;

public enum ChapterStatus
{
    Draft,
    Writing,
    Revision,
    Final
}

public partial class Chapter : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private string _notes;

    [ObservableProperty]
    private string _synopsis;

    [ObservableProperty]
    private int? _wordCountGoal;

    [ObservableProperty]
    private double _pinboardX;

    [ObservableProperty]
    private double _pinboardY;

    [ObservableProperty]
    private ChapterStatus _status = ChapterStatus.Draft;

    [ObservableProperty]
    private string _location;

    [ObservableProperty]
    private int _lastCaretPosition;

    [ObservableProperty]
    private double _lastVerticalOffset;

    private List<string> _plotThreads = new List<string>();
    public List<string> PlotThreads
    {
        get => _plotThreads;
        set => SetProperty(ref _plotThreads, value ?? new List<string>());
    }

    private List<string> _tags = new List<string>();
    public List<string> Tags
    {
        get => _tags;
        set
        {
            if (SetProperty(ref _tags, value ?? new List<string>()))
                OnPropertyChanged(nameof(TagsText));
        }
    }

    public string TagsText
    {
        get => _tags is null || _tags.Count == 0 ? string.Empty : string.Join(", ", _tags);
        set
        {
            Tags = (value ?? string.Empty)
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(System.StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            OnPropertyChanged();
        }
    }

    public string Token { get; private set; }

    public void SetToken(string token)
    {
        Token = token;
    }
}
