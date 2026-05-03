using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Storylines.Helpers;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ProjectFileInfoDialogue : ContentDialog
    {
        private static IProjectPersistenceService ProjectPersistence => App.TryGetService<IProjectPersistenceService>();

        public static ProjectFileInfoDialogue fileInfoDialogue;

        public ProjectFileInfoDialogue()
        {
            InitializeComponent();
            DialogHelper.EnsureXamlRoot(this);
            fileInfoDialogue = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = fileInfoDialogue;
            fileInfoDialogue.RequestedTheme = App.GetService<WindowContext>().AppView.ActualTheme;
        }

        public static void Open()
        {
            var dialog = new ProjectFileInfoDialogue();

            _ = dialog.ShowAsync();
            _ = dialog.DisplayFileInfoAsync();
        }

        public async Task DisplayFileInfoAsync()
        {
            var resourceLoader = ResourceLoader.GetForViewIndependentUse();
            var project = ProjectPersistence?.CurrentProject;
            StorageFile file = project?.file;

            string unavailable = resourceLoader.GetString("projectFileUnavailable");
            string notSaved = resourceLoader.GetString("projectFileNotSaved");

            string fileName = file?.Name;
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = !string.IsNullOrWhiteSpace(project?.ProjectName) ? project.ProjectName : !string.IsNullOrWhiteSpace(project?.Name) ? project.Name : unavailable;

            string location = file?.Path;
            if (string.IsNullOrWhiteSpace(location))
                location = !string.IsNullOrWhiteSpace(project?.Path) ? project.Path : project == null ? unavailable : notSaved;

            string created = unavailable;
            string modified = unavailable;
            string size = unavailable;
            string type = unavailable;

            if (file != null)
            {
                try
                {
                    var basicProperties = await file.GetBasicPropertiesAsync();

                    created = FormatDate(file.DateCreated, unavailable);
                    modified = FormatDate(basicProperties.DateModified, unavailable);
                    size = FormatFileSize(basicProperties.Size);
                    type = string.IsNullOrWhiteSpace(file.FileType) ? unavailable : file.FileType;
                }
                catch (Exception ex)
                {
                    App.TryGetService<ILogger>()?.Warning($"Failed to load project file metadata: {ex.Message}");
                }
            }

            projectFileInfoRun.Text =
                $"{resourceLoader.GetString("projectFileNameLabel")} {fileName}\n" +
                $"{resourceLoader.GetString("projectFileLocationLabel")} {location}\n" +
                $"{resourceLoader.GetString("projectFileCreatedLabel")} {created}\n" +
                $"{resourceLoader.GetString("projectFileModifiedLabel")} {modified}\n" +
                $"{resourceLoader.GetString("projectFileSizeLabel")} {size}\n" +
                $"{resourceLoader.GetString("projectFileTypeLabel")} {type}";
        }

        private static string FormatDate(DateTimeOffset date, string fallback)
        {
            return date == default
                ? fallback
                : date.ToString("g", System.Globalization.CultureInfo.CurrentCulture);
        }

        private static string FormatFileSize(ulong size)
        {
            if (size == 0)
                return "0 B";

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = size;
            int unitIndex = 0;

            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            string formattedValue = value >= 10 || unitIndex == 0
                ? value.ToString("0", System.Globalization.CultureInfo.CurrentCulture)
                : value.ToString("0.#", System.Globalization.CultureInfo.CurrentCulture);

            return $"{formattedValue} {units[unitIndex]}";
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            App.GetService<WindowContext>().RootElement.PointerPressed -= OnWindowPointerPressed;
            AppView.currentlyOpenedDialogue = null;
        }

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private bool isHide = true;

        private void InitializeClickOutToClose()
        {
            App.GetService<WindowContext>().RootElement.PointerPressed += OnWindowPointerPressed;

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }

        private void OnWindowPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (isHide)
                Hide();
        }
    }
}
