using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.WinUI.Services;
using System;
using Windows.Storage;

namespace Storylines.WinUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private bool _isChapterToolsEnabled;
        public bool IsChapterToolsEnabled
        {
            get => _isChapterToolsEnabled;
            set => SetProperty(ref _isChapterToolsEnabled, value);
        }

        private bool _isStorylinesDocument;
        public bool IsStorylinesDocument
        {
            get => _isStorylinesDocument;
            set => SetProperty(ref _isStorylinesDocument, value);
        }

        private double _textBoxZoomValue = 25;
        public double TextBoxZoomValue
        {
            get => _textBoxZoomValue;
            set => SetProperty(ref _textBoxZoomValue, value);
        }

        public MainViewModel()
        {
            LoadTextBoxZoom();
        }

        [RelayCommand]
        private void PageLoaded()
        {
            // TODO: Handle default launch
            //if (App.item != null)
            //{
            //    SaveSystem.DefaultLaunch(App.item);
            //    App.item = null;
            //}
        }

        public void LoadTextBoxZoom()
        {
            TextBoxZoomValue = Convert.ToDouble(ApplicationData.Current.LocalSettings.Values["TextBoxZoomValue"] ?? 25);
        }

        [RelayCommand]
        private void OpenProjectStats()
        {
            // TODO: Implement ProjectStatsDialogue
        }

        [RelayCommand]
        private void ResetZoom()
        {
            TextBoxZoomValue = 25;
        }
    }
}
