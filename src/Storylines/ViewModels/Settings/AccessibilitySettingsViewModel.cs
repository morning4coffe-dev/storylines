using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.AppLifecycle;
using Windows.Globalization;
using Windows.Media.SpeechSynthesis;
using Microsoft.UI.Xaml;

namespace Storylines.ViewModels.Settings
{
    public partial class AccessibilitySettingsViewModel : ObservableObject
    {

        private readonly IAppSettingsService _settings;
        private readonly string _loadedLanguageTag;
        private bool _isInitializing;

        [ObservableProperty]
        private string _selectedLanguageTag;

        [ObservableProperty]
        private Visibility _languageRestartBannerVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private bool _languageRestartFailed;

        [ObservableProperty]
        private bool _textBoxSolidBackground;

        [ObservableProperty]
        private double _readAloudVolume;

        [ObservableProperty]
        private string _selectedVoiceId;

        public ObservableCollection<VoiceOption> Voices { get; } = new ObservableCollection<VoiceOption>();

        public string ReadAloudVolumeText => Math.Round(ReadAloudVolume).ToString(CultureInfo.CurrentCulture);

        public string ReadAloudVolumeGlyph => GetVolumeGlyph(ReadAloudVolume);

        public AccessibilitySettingsViewModel(IAppSettingsService settings)
        {
            _settings = settings;

            _isInitializing = true;
            _loadedLanguageTag = ResolveSupportedLanguageTag(GetCurrentLanguageTag());
            _selectedLanguageTag = _loadedLanguageTag;
            _textBoxSolidBackground = _settings.TextBoxSolidBackground;
            _readAloudVolume = _settings.ReadAloudVolume;

            foreach (var voice in SpeechSynthesizer.AllVoices)
                Voices.Add(new VoiceOption(voice.DisplayName, voice.Id));

            _selectedVoiceId = Voices.Any(v => v.Id == _settings.ReadAloudVoiceId)
                ? _settings.ReadAloudVoiceId
                : SpeechSynthesizer.DefaultVoice.Id;
            _isInitializing = false;
        }

        [RelayCommand]
        private Task RestartNowAsync()
        {
            try
            {
                var result = AppInstance.Restart(string.Empty);
                LanguageRestartFailed = result != AppRestartFailureReason.RestartPending;
            }
            catch (Exception)
            {
                LanguageRestartFailed = true;
            }
            return Task.CompletedTask;
        }

        [RelayCommand]
        private void DismissRestartBanner()
        {
            LanguageRestartBannerVisibility = Visibility.Collapsed;
            LanguageRestartFailed = false;
        }

        [RelayCommand]
        private void ToggleReadAloudMute()
        {
            ReadAloudVolume = ReadAloudVolume > 0 ? 0 : 100;
        }

        partial void OnSelectedLanguageTagChanged(string value)
        {
            if (_isInitializing || string.IsNullOrWhiteSpace(value))
                return;

            _settings.UserLanguage = value;
            LanguageRestartBannerVisibility = _settings.LanguageTagsMatch(_loadedLanguageTag, value)
                ? Visibility.Collapsed
                : Visibility.Visible;
            LanguageRestartFailed = false;
        }

        partial void OnTextBoxSolidBackgroundChanged(bool value)
        {
            if (_isInitializing)
                return;

            _settings.TextBoxSolidBackground = value;
        }

        partial void OnReadAloudVolumeChanged(double value)
        {
            if (_isInitializing || double.IsNaN(value))
                return;

            _settings.ReadAloudVolume = value;
            OnPropertyChanged(nameof(ReadAloudVolumeText));
            OnPropertyChanged(nameof(ReadAloudVolumeGlyph));
        }

        partial void OnSelectedVoiceIdChanged(string value)
        {
            if (_isInitializing || string.IsNullOrWhiteSpace(value))
                return;

            _settings.ReadAloudVoiceId = value;
        }

        private string GetCurrentLanguageTag()
        {
            if (!_settings.IsUserLanguageSupported())
                return "en";

            return string.IsNullOrEmpty(ApplicationLanguages.PrimaryLanguageOverride)
                ? ApplicationLanguages.Languages[0]
                : ApplicationLanguages.PrimaryLanguageOverride;
        }

        private string ResolveSupportedLanguageTag(string languageTag)
        {
            foreach (var supportedLanguageTag in SupportedLanguages.Tags)
            {
                if (_settings.LanguageTagsMatch(languageTag, supportedLanguageTag))
                    return supportedLanguageTag;
            }

            return SupportedLanguages.DefaultTag;
        }

        private static string GetVolumeGlyph(double value)
        {
            if (value <= 0)
                return "";

            if (value < 50)
                return "";

            if (value < 100)
                return "";

            return "";
        }

        public sealed class VoiceOption
        {
            public VoiceOption(string displayName, string id)
            {
                DisplayName = displayName;
                Id = id;
            }

            public string DisplayName { get; }

            public string Id { get; }
        }
    }
}