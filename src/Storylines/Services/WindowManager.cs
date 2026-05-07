using Microsoft.Extensions.DependencyInjection;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using WinUIEx;

namespace Storylines.Services
{
    internal sealed class WindowManager : IWindowManager
    {
        private readonly IServiceProvider _rootServices;
        private readonly List<WindowContext> _windows = new List<WindowContext>();
        private readonly AsyncLocal<WindowContext> _ambientContext = new AsyncLocal<WindowContext>();
        private WindowContext _current;

        public WindowManager(IServiceProvider rootServices)
        {
            _rootServices = rootServices;
        }

        public WindowContext Current => _ambientContext.Value ?? _current ?? PrimaryWindow;

        public WindowContext PrimaryWindow => _windows.FirstOrDefault();

        public WindowContext CreateDocumentWindow(IStorageItem pendingActivatedItem = null, string activationSource = null)
        {
            var scope = _rootServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WindowContext>();
            context.AttachScope(scope);
            context.PendingActivatedItem = pendingActivatedItem;

            using (Enter(context))
            {
                var window = new ShellWindow(context);
                window.Closed += (_, e) => App.Current?.OnWindowClosed(context, e);
                window.Activated += (_, _) => SetCurrent(context);
                window.Initialize();
                _windows.Add(context);
                SetCurrent(context);
                window.Activate();
                window.CenterOnScreen();
            }

            return context;
        }

        public WindowContext GetContext(Guid id) => _windows.FirstOrDefault(window => window.Id == id);

        public IDisposable Enter(WindowContext context)
        {
            var previous = _ambientContext.Value;
            if (context is not null)
                SetCurrent(context);
            _ambientContext.Value = context;
            return new ContextScope(this, previous);
        }

        public async Task RunAsync(WindowContext context, Func<Task> action)
        {
            if (action is null)
                return;

            using (Enter(context))
            {
                await action();
            }
        }

        public void SetCurrent(WindowContext context)
        {
            if (context is not null)
                _current = context;
        }

        public void Close(WindowContext context)
        {
            if (context?.Window is null)
                return;

            using (Enter(context))
            {
                context.Window.Close();
            }
        }

        internal void Remove(WindowContext context)
        {
            if (context is null)
                return;

            _windows.Remove(context);
            if (_current == context)
                _current = PrimaryWindow;

            context.DisposeScope();

            if (_windows.Count == 0)
                App.Current?.Exit();
        }

        private sealed class ContextScope : IDisposable
        {
            private readonly WindowManager _owner;
            private readonly WindowContext _previous;

            public ContextScope(WindowManager owner, WindowContext previous)
            {
                _owner = owner;
                _previous = previous;
            }

            public void Dispose()
            {
                _owner._ambientContext.Value = _previous;
            }
        }
    }
}
