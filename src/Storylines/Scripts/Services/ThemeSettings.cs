using Microsoft.Toolkit.Uwp.UI.Helpers;
using Storylines.Pages;
using System;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using static Storylines.Scripts.Services.SettingsValues;

namespace Storylines.Scripts.Services
{
    public static class ThemeSettings
    {
        private static readonly UISettings uiSettings = new UISettings();
        public static readonly ThemeListener themeListener = new ThemeListener();

        public static ElementTheme RootTheme
        {
            get
            {
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    return rootElement.RequestedTheme;
                }

                return ElementTheme.Default;
            }
            set
            {
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = value;
                }
            }
        }

        public static void Initialize()
        {
            InitializeThemeMode();

            InitializeAppAccentColor();
        }

        private static void InitializeThemeMode()
        {
            themeListener.ThemeChanged += ThemeListener_ThemeChanged;

            switch (selectedTheme)
            {
                case SelectedTheme.Light:
                    ChangeTheme(0, ElementTheme.Default);
                    break;
                case SelectedTheme.Dark:
                    ChangeTheme(1, ElementTheme.Default);
                    break;
                case SelectedTheme.System:
                    ChangeTheme(2, themeListener.CurrentTheme.ToElementTheme());
                    break;
            }
        }

        public static void InitializeAppAccentColor()
        {
            uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;

            UpdateAccentColor(GetCurrentAccentColor());
        }

        public static Color GetCurrentAccentColor()
        {
            switch (selectedAccent)
            {
                case SelectedAccent.System:
                    return uiSettings.GetColorValue(UIColorType.Accent);

                case SelectedAccent.App:
                    return appAccentColor;

                case SelectedAccent.Custom:
                    return customAccentColor;

                default:
                    return appAccentColor;
            }
        }

        private static void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {
            if (selectedAccent == SelectedAccent.System)
            {
                UpdateAccentColor(sender.GetColorValue(UIColorType.Accent));
            }
        }

        private static void ThemeListener_ThemeChanged(ThemeListener sender)
        {
            if (selectedTheme == SelectedTheme.System)
            {
                ChangeTheme(2, sender.CurrentTheme.ToElementTheme());
            }
        }

        public static void ChangeTheme(int id, ElementTheme currentSystemTheme)
        {
            switch (id)
            {
                case 0:
                    RootTheme = ElementTheme.Light;
                    AppView.current.RequestedTheme = ElementTheme.Light;
                    selectedTheme = SelectedTheme.Light;
                    break;
                case 1:
                    RootTheme = ElementTheme.Dark;
                    AppView.current.RequestedTheme = ElementTheme.Dark;
                    selectedTheme = SelectedTheme.Dark;
                    break;
                case 2:
                    selectedTheme = SelectedTheme.System;

                    switch (currentSystemTheme)
                    {
                        case ElementTheme.Light:
                            RootTheme = ElementTheme.Light;
                            AppView.current.RequestedTheme = ElementTheme.Light;
                            break;
                        case ElementTheme.Dark:
                            RootTheme = ElementTheme.Dark;
                            AppView.current.RequestedTheme = ElementTheme.Dark;
                            break;
                    }
                    break;
            }
            try
            {
                MainPage.chapterText.TextBoxWhiteBackground(Convert.ToBoolean(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.TextBoxSolidBackground] ?? false));
            }
            catch { }

            UpdateAccentColor((Color)Application.Current.Resources["SystemAccentColor"]);

            ApplicationData.Current.LocalSettings.Values["AppTheme"] = id;
        }

        public static ElementTheme ToElementTheme(this ApplicationTheme theme)
        {
            switch (theme)
            {
                case ApplicationTheme.Light:
                    return ElementTheme.Light;
                case ApplicationTheme.Dark:
                    return ElementTheme.Dark;
                default:
                    return ElementTheme.Default;
            }
        }

        private static void ApplyThemeForTitleBar(ApplicationViewTitleBar titleBar, Color color, ElementTheme theme)
        {
            if (theme == ElementTheme.Dark)
            {
                // Set active window colors
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 90, 90, 90);
                titleBar.ButtonPressedForegroundColor = Colors.White;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 120, 120, 120);

                // Set inactive window colors
                titleBar.InactiveForegroundColor = Colors.Gray;
                titleBar.InactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                titleBar.BackgroundColor = Color.FromArgb(255, 45, 45, 45);

                titleBar.ButtonForegroundColor = ChangeColorBrightness(color, 0.15f);
                Application.Current.Resources["AppTitleColor"] = ChangeColorBrightness(color, 0.15f);
            }
            else if (theme == ElementTheme.Light)
            {
                // Set active window colors
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 180, 180, 180);
                titleBar.ButtonPressedForegroundColor = Colors.Black;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 150, 150, 150);

                // Set inactive window colors
                titleBar.InactiveForegroundColor = Colors.DimGray;
                titleBar.InactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveForegroundColor = Colors.DimGray;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                titleBar.BackgroundColor = Color.FromArgb(255, 210, 210, 210);

                titleBar.ButtonForegroundColor = ChangeColorBrightness(color, -0.10f);
                Application.Current.Resources["AppTitleColor"] = ChangeColorBrightness(color, -0.10f);
            }
        }

        private static void UpdateAccentColor(Color color)
        {
            _ = AppView.current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
              {
                  Application.Current.Resources["SystemAccentColor"] = color; 
                  Application.Current.Resources["SystemAccentColorDark1"] = ChangeColorBrightness(color, -0.05f);
                  Application.Current.Resources["SystemAccentColorDark2"] = ChangeColorBrightness(color, -0.10f);
                  Application.Current.Resources["SystemAccentColorDark3"] = ChangeColorBrightness(color, -0.15f);
                  Application.Current.Resources["SystemAccentColorLight1"] = ChangeColorBrightness(color, 0.05f);
                  Application.Current.Resources["SystemAccentColorLight2"] = ChangeColorBrightness(color, 0.10f);
                  Application.Current.Resources["SystemAccentColorLight3"] = ChangeColorBrightness(color, 0.15f);

                  ApplyThemeForTitleBar(ApplicationView.GetForCurrentView().TitleBar, color, AppView.current.RequestedTheme);

                  AppView.current.RequestedTheme = AppView.current.RequestedTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                  AppView.current.RequestedTheme = AppView.current.RequestedTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
              });
        }

        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = color.R;
            float green = color.G;
            float blue = color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, (byte)(int)red, (byte)(int)green, (byte)(int)blue);
        }
    }
}