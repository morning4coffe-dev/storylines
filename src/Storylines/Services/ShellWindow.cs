using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUIEx;
using System;
using System.IO;

namespace Storylines.Services
{
    public sealed class ShellWindow : WindowEx
    {
        private readonly WindowContext _context;

        public ShellWindow(WindowContext context)
        {
            _context = context;
            _context.Window = this;

            Title = "Storylines";
            Width = Constants.LayoutConstants.DefaultWindowWidth;
            Height = Constants.LayoutConstants.DefaultWindowHeight;
            MinWidth = Constants.LayoutConstants.MinWindowWidth;
            MinHeight = Constants.LayoutConstants.MinWindowHeight;
            SystemBackdrop = new MicaBackdrop();
            ExtendsContentIntoTitleBar = true;
        }

        public void Initialize()
        {
            var rootFrame = new Frame();
            _context.RootElement = rootFrame;
            Content = rootFrame;

            rootFrame.NavigationFailed += (_, e) =>
                throw new Exception("Failed to load Page " + e.SourcePageType.FullName);

            rootFrame.Navigate(typeof(AppView));

            try
            {
                string iconFileName = "Storylines-icon.ico";
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", iconFileName);
                if (File.Exists(iconPath))
                {
                    this.AppWindow?.SetIcon(iconPath);
                }
            }
            catch (Exception ex)
            {
                App.GetService<Interfaces.ILogger>()?.Warning($"Failed to set window icon: {ex.Message}");
            }
        }
    }
}
