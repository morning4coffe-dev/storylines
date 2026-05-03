using Storylines.Services.Interfaces;
using System;
using Windows.UI;
using Windows.UI.Xaml;

namespace Storylines.Services
{
    /// <summary>
    /// Façade over the existing static <see cref="ThemeSettings"/> helper. Lets ViewModels and
    /// services consume theming through DI today, while leaving the UWP-specific resource-mutation
    /// implementation in <see cref="ThemeSettings"/> until WinUI 3 migration replaces it.
    /// </summary>
    internal sealed class ThemeService : IThemeService
    {
        private readonly IAppSettingsService _settings;

        public ThemeService(IAppSettingsService settings)
        {
            _settings = settings;
            AccentColor = ThemeSettings.GetCurrentAccentColor();
            RequestedTheme = ThemeSettings.RootTheme;
        }

        public Color AccentColor { get; private set; }

        public ElementTheme RequestedTheme { get; private set; }

        public event Action<Color> AccentChanged;
        public event Action<ElementTheme> ThemeChanged;

        public void ApplyAccent(Color color)
        {
            _settings.SelectedAccent = SettingsValues.SelectedAccent.Custom;
            _settings.CustomAccentColor = color;
            AccentColor = color;
            AccentChanged?.Invoke(color);
        }

        public void ApplyTheme(ElementTheme theme)
        {
            switch (theme)
            {
                case ElementTheme.Light:
                    _settings.SelectedTheme = SettingsValues.SelectedTheme.Light;
                    break;
                case ElementTheme.Dark:
                    _settings.SelectedTheme = SettingsValues.SelectedTheme.Dark;
                    break;
                default:
                    _settings.SelectedTheme = SettingsValues.SelectedTheme.System;
                    break;
            }
            RequestedTheme = theme;
            ThemeChanged?.Invoke(theme);
        }
    }
}
