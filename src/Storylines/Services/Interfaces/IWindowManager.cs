
namespace Storylines.Services.Interfaces;

public interface IWindowManager
{
    WindowContext Current { get; }
    WindowContext PrimaryWindow { get; }

    WindowContext CreateDocumentWindow(IStorageItem pendingActivatedItem = null, string activationSource = null);
    WindowContext GetContext(Guid id);
    IDisposable Enter(WindowContext context);
    Task RunAsync(WindowContext context, Func<Task> action);
    void SetCurrent(WindowContext context);
    void Close(WindowContext context);
}
