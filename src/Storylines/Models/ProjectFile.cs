using Storylines.Views.Dialogs;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;

namespace Storylines.Models;

public partial class ProjectFile : ObservableObject
{
    private static readonly SemaphoreSlim _futureAccessListLock = new SemaphoreSlim(1, 1);

    public string Name { get; set; }
    public string Token { get; private set; }
    public string Path { get; set; }

    // PascalCase property with backward-compatible alias
    public StorageFile File { get; set; }
    public StorageFile file { get => File; set => File = value; }

    public string ProjectName { get; set; }
    public string projectName { get => ProjectName; set => ProjectName = value; }

    public string ProjectVersion { get; set; }
    public string projectVersion { get => ProjectVersion; set => ProjectVersion = value; }

    public Uri Icon { get; set; }
    public string ShortPath { get; set; }
    public string LastEditedFormatted { get; private set; }
    public DateTimeOffset LastEdited { get; private set; }

    public Microsoft.UI.Xaml.Thickness osMargin { get; private set; } = LoadProjectDialogue.osMargin;
    public double osWidth { get; private set; } = LoadProjectDialogue.osWidth;

    public static ObservableCollection<ProjectFile> projectFiles = new ObservableCollection<ProjectFile>();

    public static void New(StorageFile file)
    {
        _ = RememberSafelyAsync(file);
    }

    public static async Task<ProjectFile> LoadExistingAsync(StorageFile file, string token)
    {
        BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
        return new ProjectFile()
        {
            Name = file.Name,
            Path = file.Path,
            Token = token,
            File = file,
            Icon = new Uri(file.FileType == ".txt" ? "ms-appx:/Assets/Icons/Text-document-icon.png" : "ms-appx:/Assets/Icons/Storylines-document-icon.png"),
            ShortPath = file.Path.Replace(@"\" + file.Name, string.Empty).Replace(@"\", "/"),
            LastEditedFormatted = basicProperties.DateModified.ToString("g", System.Globalization.CultureInfo.CurrentCulture),
            LastEdited = basicProperties.DateModified
        };
    }

    private static async Task RememberSafelyAsync(StorageFile file)
    {
        try
        {
            await RememberAsync(file);
        }
        catch
        {
        }
    }

    private static async Task<string> RememberAsync(StorageFile file)
    {
        if (file is null)
            throw new ArgumentNullException(nameof(file));

        await _futureAccessListLock.WaitAsync();

        try
        {
            var futureAccessList = StorageApplicationPermissions.FutureAccessList;
            var existingToken = RecentProjectDeduplicator.FindExistingToken(
                await LoadRememberedProjectReferencesAsync(),
                file.Path);

            if (!string.IsNullOrWhiteSpace(existingToken))
            {
                futureAccessList.AddOrReplace(existingToken, file);
                return existingToken;
            }

            string token = Guid.NewGuid().ToString();
            if (futureAccessList.Entries.Count >= futureAccessList.MaximumItemsAllowed)
                futureAccessList.Remove(futureAccessList.Entries[0].Token);

            futureAccessList.AddOrReplace(token, file);
            return token;
        }
        finally
        {
            _futureAccessListLock.Release();
        }
    }

    private static async Task<List<RecentProjectReference>> LoadRememberedProjectReferencesAsync()
    {
        var rememberedProjects = new List<RecentProjectReference>();

        foreach (var entry in StorageApplicationPermissions.FutureAccessList.Entries.ToList())
        {
            try
            {
                using var timeout = new CancellationTokenSource(LayoutConstants.ProjectFileLoadTimeoutMs);
                var existingFile = await GetProjectFromTokenAsync(entry.Token, timeout.Token);
                if (existingFile is not null)
                    rememberedProjects.Add(new RecentProjectReference(entry.Token, existingFile.Path));
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
        }

        return rememberedProjects;
    }

    public static void Remove(string token)
    {
        for (int i = 0; i < projectFiles.Count; i++)
        {
            if (projectFiles[i].Token == token)
            {
                projectFiles.RemoveAt(i);
                StorageApplicationPermissions.FutureAccessList.Remove(token);
                return;
            }
        }
    }

    public static async Task LoadAllAsync()
    {
        projectFiles.Clear();
        var loadedProjects = new List<ProjectFile>();

        foreach (var token in StorageApplicationPermissions.FutureAccessList.Entries.ToList())
        {
            try
            {
                using var timeout = new CancellationTokenSource(LayoutConstants.ProjectFileLoadTimeoutMs);
                var file = await GetProjectFromTokenAsync(token.Token, timeout.Token);
                if (file is not null)
                    loadedProjects.Add(await LoadExistingAsync(file, token.Token));
            }
            catch (OperationCanceledException)
            {
                StorageApplicationPermissions.FutureAccessList.Remove(token.Token);
            }
            catch
            {
                StorageApplicationPermissions.FutureAccessList.Remove(token.Token);
            }
        }

        foreach (var projectFile in RecentProjectDeduplicator.DistinctByPath(loadedProjects, currentProject => currentProject.Path))
            projectFiles.Add(projectFile);
    }

    public static async Task<StorageFile> GetProjectFromTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
            return null;

        return await StorageApplicationPermissions.FutureAccessList.GetFileAsync(token).AsTask(cancellationToken);
    }

    public static bool CheckIfProjectExists(StorageFile file)
    {
        for (int i = 0; i < projectFiles.Count; i++)
        {
            if (RecentProjectDeduplicator.PathsMatch(projectFiles[i].Path, file.Path))
            {
                return true;
            }
        }
        return false;
    }
}
