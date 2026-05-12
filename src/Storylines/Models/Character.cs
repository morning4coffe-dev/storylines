using Microsoft.UI.Xaml.Media.Imaging;

namespace Storylines.Models;

public partial class Character : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    [ObservableProperty]
    private CharacterPicture _picture;

    [ObservableProperty]
    private string _role;

    [ObservableProperty]
    private string _age;

    [ObservableProperty]
    private string _appearance;

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(DetailsLine));
    partial void OnDescriptionChanged(string value) => OnPropertyChanged(nameof(DetailsLine));
    partial void OnRoleChanged(string value) => OnPropertyChanged(nameof(DetailsLine));
    partial void OnAgeChanged(string value) => OnPropertyChanged(nameof(DetailsLine));

    private List<string> _traits = new List<string>();
    public List<string> Traits
    {
        get => _traits;
        set
        {
            if (SetProperty(ref _traits, value ?? new List<string>()))
            {
                OnPropertyChanged(nameof(TraitsText));
                OnPropertyChanged(nameof(DetailsLine));
            }
        }
    }

    public string TraitsText
    {
        get => Traits is null || Traits.Count == 0 ? string.Empty : string.Join(", ", Traits);
        set
        {
            Traits = (value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            OnPropertyChanged();
        }
    }

    public string DetailsLine
    {
        get
        {
            var details = new List<string>();

            if (!string.IsNullOrWhiteSpace(Role))
                details.Add(Role);

            if (!string.IsNullOrWhiteSpace(Age))
                details.Add(Age);

            if (Traits is not null && Traits.Count > 0)
                details.Add(string.Join(", ", Traits.Take(2)) + (Traits.Count > 2 ? "…" : string.Empty));

            if (details.Count > 0)
                return string.Join(" · ", details);

            return Description;
        }
    }

    private List<CharacterRelationship> _relationships = new List<CharacterRelationship>();
    public List<CharacterRelationship> Relationships
    {
        get => _relationships;
        set => SetProperty(ref _relationships, value ?? new List<CharacterRelationship>());
    }

    public string Token { get; private set; }

    public void SetToken(string token)
    {
        Token = token;
    }

    public static async Task<BitmapImage> LoadProfilePictureAsync(CharacterPicture cp)
    {
        try
        {
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);
            var file = await folder.TryGetItemAsync(cp.FileName);

            if (file is not null)
                return new BitmapImage(new Uri(file.Path)) { DecodePixelHeight = LayoutConstants.ProfilePictureDecodeSize, DecodePixelWidth = LayoutConstants.ProfilePictureDecodeSize };
        }
        catch (Exception ex)
        {
            App.GetService<Services.Interfaces.ILogger>().Error($"Failed to load profile picture: {cp.FileName}", ex);
        }

        return null;
    }
}

public class CharacterPicture
{
    public string LocalFilePath { set; get; }
    public string FileName { set; get; }
    public BitmapImage Image { set; get; }
}

public class CharacterRelationship
{
    public string TargetCharacterToken { get; set; }
    public string Type { get; set; }
}
