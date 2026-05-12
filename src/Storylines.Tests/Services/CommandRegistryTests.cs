using System.Windows.Input;
using Xunit;

namespace Storylines.Tests.Services;

public class CommandRegistryTests
{
    [Fact]
    public void Register_AddsCommand()
    {
        var registry = new CommandRegistry();
        registry.Register(NewCommand("editor.toggleFocus", "Toggle focus mode", "Editor"));

        Assert.Single(registry.Commands);
    }

    [Fact]
    public void Register_DuplicateId_ReplacesPrevious()
    {
        var registry = new CommandRegistry();
        registry.Register(NewCommand("editor.save", "Save", "Editor"));
        registry.Register(NewCommand("editor.save", "Save project", "Project"));

        Assert.Single(registry.Commands);
        Assert.Equal("Project", registry.Commands[0].Category);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        var registry = new CommandRegistry();
        registry.Register(NewCommand("a", "Alpha", "X"));
        registry.Register(NewCommand("b", "Bravo", "X"));

        Assert.Equal(2, registry.Search(string.Empty).Count);
    }

    [Fact]
    public void Search_ByDisplayName_FindsCommand()
    {
        var registry = new CommandRegistry();
        registry.Register(NewCommand("editor.toggleFocus", "Toggle focus mode", "Editor"));
        registry.Register(NewCommand("project.save", "Save project", "Project"));

        var hits = registry.Search("focus");

        Assert.Single(hits);
        Assert.Equal("editor.toggleFocus", hits[0].Id);
    }

    [Fact]
    public void Search_MultipleTokens_AllMustMatch()
    {
        var registry = new CommandRegistry();
        registry.Register(NewCommand("editor.toggleFocus", "Toggle focus mode", "Editor"));

        Assert.Single(registry.Search("toggle focus"));
        Assert.Empty(registry.Search("toggle nope"));
    }

    [Fact]
    public void Unregister_ById_RemovesCommand()
    {
        var registry = new CommandRegistry();
        registry.Register(NewCommand("a", "Alpha", "X"));

        Assert.True(registry.Unregister("a"));
        Assert.Empty(registry.Commands);
    }

    private static AppCommand NewCommand(string id, string name, string category)
        => new(id, name, category, new RelayShim(_ => { }));

    private sealed class RelayShim : ICommand
    {
        private readonly Action<object> _exec;
        public RelayShim(Action<object> exec) { _exec = exec; }
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _exec(parameter);
        public event EventHandler CanExecuteChanged;
    }
}
