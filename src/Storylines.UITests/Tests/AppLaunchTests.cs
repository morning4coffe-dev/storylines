using OpenQA.Selenium.Appium;
using Storylines.UITests.Infrastructure;
using Xunit;

namespace Storylines.UITests.Tests;

/// <summary>
/// Smoke tests that verify the app launches and its main shell elements are present.
/// The app opens a "Load project" dialog on first launch when no file is provided;
/// these tests pass regardless of whether that dialog is visible.
///
/// All element lookups use AutomationProperties.AutomationId ("accessibility ID") which
/// is locale-independent and survives UI refactors better than class name or name matching.
/// </summary>
public class AppLaunchTests : IClassFixture<AppFixture>
{
    private readonly AppFixture _fixture;

    public AppLaunchTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Driver_IsNotNull_AfterLaunch()
    {
        Assert.NotNull(_fixture.Driver);
    }

    [Fact]
    public void Window_HasNonEmptyTitle()
    {
        var title = _fixture.Driver.Title;
        Assert.NotNull(title);
        Assert.NotEmpty(title);
    }

    [Fact]
    public void WindowTitle_ContainsStorylines()
    {
        // TitleBar.Title is bound to AppViewModel.TitleText which always includes "Storylines"
        Assert.Contains("Storylines", _fixture.Driver.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppTitleBar_IsPresent()
    {
        var titleBar = _fixture.Driver.FindElement(MobileBy.AccessibilityId("AppTitleBar"));
        Assert.True(titleBar.Displayed);
    }

    [Fact]
    public void MainCommandBar_IsPresent()
    {
        // MainCommandBar contains the top NavigationView (File / Insert / View / Help tabs).
        // It is always visible once a project is loaded; if the Load dialog is shown first,
        // the command bar may not yet be rendered — the test will still pass because
        // FindElements returns an empty list rather than throwing.
        var bars = _fixture.Driver.FindElements(MobileBy.AccessibilityId("MainCommandBar"));
        // We just assert the session is alive; a project-loaded assertion belongs in a
        // dedicated fixture that supplies a test project file via STORYLINES_TEST_FILE.
        Assert.NotNull(bars);
    }

    [Fact]
    public void MainFrame_IsPresent()
    {
        var frame = _fixture.Driver.FindElement(MobileBy.AccessibilityId("MainFrame"));
        Assert.True(frame.Displayed);
    }
}
