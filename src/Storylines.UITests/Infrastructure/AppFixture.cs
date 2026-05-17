using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using Xunit;

namespace Storylines.UITests.Infrastructure;

/// <summary>
/// xUnit class fixture that manages a single Appium <see cref="WindowsDriver"/> session
/// shared across all tests in a test class.
///
/// Prerequisites:
///   1. Install Node.js (https://nodejs.org)
///   2. npm install -g appium
///   3. appium driver install windows
///   4. Build and deploy the Storylines app (F5 or via MSIX)
///   5. Set the STORYLINES_TEST_AUMID env var (see below)
///   6. Start the Appium server: appium
///   7. dotnet test src/Storylines.UITests
///
/// Finding the AUMID:
///   Run in PowerShell: Get-AppxPackage *Storylines* | Select-Object PackageFamilyName
///   AUMID = {PackageFamilyName}!App
///   Example: 3597CaffeStudios.Storylines_abc123xyz!App
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    private const string AppiumServerEnvVar = "STORYLINES_APPIUM_URL";
    private const string AumidEnvVar = "STORYLINES_TEST_AUMID";
    private const string DefaultAppiumUrl = "http://127.0.0.1:4723";

    public WindowsDriver Driver { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var aumid = Environment.GetEnvironmentVariable(AumidEnvVar)
            ?? throw new InvalidOperationException(
                $"Set the '{AumidEnvVar}' environment variable to the app's AUMID before running UI tests. " +
                "Run 'Get-AppxPackage *Storylines* | Select-Object PackageFamilyName' in PowerShell, " +
                "then append '!App' to the PackageFamilyName.");

        var serverUrl = Environment.GetEnvironmentVariable(AppiumServerEnvVar)
            ?? DefaultAppiumUrl;

        var options = new AppiumOptions
        {
            App = aumid,
            DeviceName = "WindowsPC",
            AutomationName = "Windows"
        };

        Driver = new WindowsDriver(new Uri(serverUrl), options, TimeSpan.FromSeconds(60));
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Driver?.Quit();
        return Task.CompletedTask;
    }
}
