using CommunityToolkit.Mvvm.ComponentModel;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;

namespace Storylines.ViewModels
{
    public partial class AppViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _titleText;

        [ObservableProperty]
        private string _unsavedIndicatorText;

        [ObservableProperty]
        private Visibility _unsavedIndicatorVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _backButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private AppPages _currentPage;

        public enum AppPages { Settings, Characters, MainPage }

        private readonly string _editedString;
        private readonly IProjectPersistenceService _persistence;
        private readonly IUndoRedoService _undoRedo;

        public AppViewModel(EventAggregator events, IProjectPersistenceService persistence, IUndoRedoService undoRedo)
        {
            _persistence = persistence;
            _undoRedo = undoRedo;
            _editedString = ResourceLoader.GetForViewIndependentUse().GetString("appHeaderEdited");
            events.Subscribe<TitleBarUpdateEvent>(_ => UpdateTitleBar());
            UpdateTitleBar();
        }

        public void UpdateTitleBar()
        {
            if (CurrentPage == AppPages.Settings)
            {
                var version = Package.Current.Id.Version;
                TitleText =
                    $"{Package.Current.DisplayName}" +
                    $" {version.Major}.{version.Minor}{(version.Build.ToString().Equals("0") ? string.Empty : $".{version.Build}")}{(version.Revision.ToString().Equals("0") ? string.Empty : $".{version.Revision}")}" +
                    $"{(Package.Current.IsDevelopmentMode ? " Dev" : " Preview")}";
            }
            else
            {
                var name = GetProjectName();
                TitleText = name ?? Package.Current.DisplayName;
            }

            UnsavedIndicatorText = $" {_editedString}";
            UnsavedIndicatorVisibility = _undoRedo.IsDirty ? Visibility.Visible : Visibility.Collapsed;
        }

        public string GetProjectName()
        {
            if (_persistence?.CurrentProject != null)
            {
                if (!string.IsNullOrEmpty(_persistence.CurrentProject.projectName))
                    return _persistence.CurrentProject.projectName;
                else
                    return _persistence.CurrentProject.Name;
            }
            return null;
        }

        public void UpdateBackButtonVisibility(bool canGoBack, bool hasModeActive)
        {
            BackButtonVisibility = (canGoBack || hasModeActive) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
