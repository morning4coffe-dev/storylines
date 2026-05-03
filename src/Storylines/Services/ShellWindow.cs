using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

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
                throw new System.Exception("Failed to load Page " + e.SourcePageType.FullName);

            rootFrame.Navigate(typeof(AppView));
        }
    }
}
