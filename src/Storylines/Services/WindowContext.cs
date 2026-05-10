using Microsoft.Extensions.DependencyInjection;
using Storylines.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Storylines.Views.Controls;
using Storylines.Views.Pages;
using System;
using Windows.Storage;

namespace Storylines.Services
{
    public sealed class WindowContext
    {
        private IServiceScope _scope;

        public Guid Id { get; } = Guid.NewGuid();
        public ShellWindow Window { get; internal set; }
        public FrameworkElement RootElement { get; internal set; }
        public IServiceProvider Services { get; internal set; }
        public IStorageItem PendingActivatedItem { get; set; }
        public ContentDialog CurrentDialog { get; set; }
        public AppView AppView { get; internal set; }
        public MainPage MainPage { get; internal set; }
        public CharactersPage CharactersPage { get; internal set; }
        public ChaptersList ChapterList { get; internal set; }
        public MainCommandBar CommandBar { get; internal set; }
        public Storylines.Views.Controls.DialogueEditor.BranchingDialogueEditor ChapterText { get; internal set; }
        public TextHighlighter Highlighter { get; } = new TextHighlighter();
        internal bool IsInitialized { get; set; }

        public XamlRoot XamlRoot => RootElement?.XamlRoot ?? AppView?.XamlRoot;

        public IntPtr Hwnd => Window is null
            ? IntPtr.Zero
            : WinRT.Interop.WindowNative.GetWindowHandle(Window);

        internal void AttachScope(IServiceScope scope)
        {
            _scope = scope;
            Services = scope.ServiceProvider;
        }

        internal void DisposeScope()
        {
            _scope?.Dispose();
            _scope = null;
        }
    }
}
