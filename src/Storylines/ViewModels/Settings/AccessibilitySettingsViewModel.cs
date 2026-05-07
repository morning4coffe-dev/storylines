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
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;

namespace Storylines.ViewModels.Settings
{
    public partial class AccessibilitySettingsViewModel : ObservableObject
    {
        private const string FollowAppLanguageTag = "";

        private readonly IAppSettingsService _settings;
        private readonly ISpeechService _speech;
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

        [ObservableProperty]
        private double _readAloudRate;

        [ObservableProperty]
        private double _readAloudPitch;

        [ObservableProperty]
        private string _selectedDictationLanguageTag;

        public ObservableCollection<VoiceOption> Voices { get; } = new ObservableCollection<VoiceOption>();

        public ObservableCollection<DictationLanguageOption> DictationLanguages { get; } = new ObservableCollection<DictationLanguageOption>();

        public string ReadAloudVolumeText => Math.Round(ReadAloudVolume).ToString(CultureInfo.CurrentCulture);

        public string ReadAloudVolumeGlyph => GetVolumeGlyph(ReadAloudVolume);

        public string ReadAloudRateText => $"{ReadAloudRate:0.0}x";

        public string ReadAloudPitchText => ReadAloudPitch.ToString("0.0", CultureInfo.CurrentCulture);

        public AccessibilitySettingsViewModel(IAppSettingsService settings, ISpeechService speech)
        {
            _settings = settings;
            _speech = speech;

            _isInitializing = true;
            _loadedLanguageTag = ResolveSupportedLanguageTag(GetCurrentLanguageTag());
            _selectedLanguageTag = _loadedLanguageTag;
            _textBoxSolidBackground = _settings.TextBoxSolidBackground;
            _readAloudVolume = _settings.ReadAloudVolume;
            _readAloudRate = _settings.ReadAloudRate;
            _readAloudPitch = _settings.ReadAloudPitch;

            foreach (var voice in SpeechSynthesizer.AllVoices)
                Voices.Add(new VoiceOption(voice.DisplayName, voice.Id));

            _selectedVoiceId = Voices.Any(v => v.Id == _settings.ReadAloudVoiceId)
                ? _settings.ReadAloudVoiceId
                : SpeechSynthesizer.DefaultVoice.Id;

            PopulateDictationLanguages();
            _selectedDictationLanguageTag = string.IsNullOrWhiteSpace(_settings.DictationLanguage)
                ? FollowAppLanguageTag
                : _settings.DictationLanguage;

            _isInitializing = false;
        }

        [RelayCommand]
        private Task RestartNowAsync()
        {
            try
            {
                AppInstance.Restart(string.Empty);
                LanguageRestartFailed = false;
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

        [RelayCommand]
        private Task TestVoiceAsync() => _speech.ReadAloud.SpeakSampleAsync();

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

        partial void OnReadAloudRateChanged(double value)
        {
            if (_isInitializing || double.IsNaN(value))
                return;

            _settings.ReadAloudRate = value;
            OnPropertyChanged(nameof(ReadAloudRateText));
        }

        partial void OnReadAloudPitchChanged(double value)
        {
            if (_isInitializing || double.IsNaN(value))
                return;

            _settings.ReadAloudPitch = value;
            OnPropertyChanged(nameof(ReadAloudPitchText));
        }

        partial void OnSelectedDictationLanguageTagChanged(string value)
        {
            if (_isInitializing) return;
            _settings.DictationLanguage = value ?? string.Empty;
        }

        private void PopulateDictationLanguages()
        {
            DictationLanguages.Add(new DictationLanguageOption(FollowAppLanguageTag, "Follow app language"));

            try
            {
                foreach (var language in SpeechRecognizer.SupportedTopicLanguages
                             .Select(l => new DictationLanguageOption(l.LanguageTag, l.DisplayName))
                             .OrderBy(o => o.DisplayName, StringComparer.CurrentCulture))
                {
                    DictationLanguages.Add(language);
                }
            }
            catch
            {
                // SpeechRecognizer may be unavailable on some SKUs; "follow app language" stays as the fallback.
            }
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

        public sealed class DictationLanguageOption
        {
            public DictationLanguageOption(string tag, string displayName)
            {
                Tag = tag;
                DisplayName = displayName;
            }

            public string Tag { get; }

            public string DisplayName { get; }
        }
    }
}
