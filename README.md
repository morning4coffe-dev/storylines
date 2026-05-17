# Storylines

<img src="./Logo and Screenshots/Storylines-icon.png" width="100" alt="Storylines app icon" />

Storylines is an open-source Windows writing app for building stories chapter by chapter. It combines a clean editor with tools for characters, dialogue-heavy scenes, story analysis, focused drafting, and accessibility-minded workflows.

[![Microsoft Store](https://img.shields.io/static/v1?label=Microsoft&message=Download&color=0078D4&style=for-the-badge&logo=microsoft)](https://www.microsoft.com/store/apps/9PN77P9WJ3CX)
[![License: MIT](https://img.shields.io/badge/License-MIT-black?style=for-the-badge)](./LICENSE.md)

Storylines is designed for writers who want more structure than a plain text file without jumping into a heavyweight tool. Draft chapters, keep track of characters, work with dialogue-focused scenes, review story information, and stay in flow with Focus Mode, keyboard shortcuts, and recovery features.

The app is built as a WinUI 3 desktop application on Windows App SDK with DI-backed window scoping for multi-window workflows.

<img src="./Logo and Screenshots/0-6/Storylines-0-6-image-1.png" width="760" alt="Storylines editor screenshot" />

## Highlights

- Chapter-based writing workspace for longer projects
- Character and dialogue tools for story-driven drafting
- Story Analysis view for quick project insight
- Focus Mode, read aloud, voice dictation, and keyboard shortcuts
- Autosave, recovery, undo/redo, export, and Microsoft Store updates
- Localized UI in English, Czech, Hindi, Italian, Polish, Russian, and Simplified Chinese
- Built for Windows 10 and Windows 11 with accessibility in mind

## Get Storylines

- Install from the [Microsoft Store](https://www.microsoft.com/store/apps/9PN77P9WJ3CX)
- Build it yourself from source

## Build From Source

### Requirements

- Windows 10 version 1903 or later, or Windows 11
- Visual Studio 2022 or later with the .NET desktop development and Windows application development workloads
- .NET 9 SDK if you want to use the command line

### Command line

```powershell
msbuild src/Storylines.sln /p:Platform=x64 /p:Configuration=Debug /t:Restore
msbuild src/Storylines.sln /p:Platform=x64 /p:Configuration=Debug
dotnet test src/Storylines.Tests/Storylines.Tests.csproj --platform x64
```

## Running Tests

### Unit tests

```powershell
dotnet test src/Storylines.Tests/Storylines.Tests.csproj --platform x64
```

### UI / end-to-end tests

UI tests in `src/Storylines.UITests` drive the real packaged app via [Appium](https://appium.io) and the [appium-windows-driver](https://github.com/appium/appium-windows-driver).

#### Prerequisites

1. Install [Node.js](https://nodejs.org) (LTS).
2. Install Appium and the Windows driver:
   ```powershell
   npm install -g appium
   appium driver install windows
   ```
3. Build and **deploy** the app so it is installed as an MSIX package:
   - Open `src/Storylines.sln` in Visual Studio, set configuration to **Debug | x64**, then press **F5** (or **Deploy** from the Build menu).
4. Find the app's **AUMID** (Application User Model ID):
   ```powershell
   Get-AppxPackage *Storylines* | Select-Object PackageFamilyName
   # Example output: 3597CaffeStudios.Storylines_abc123xyz
   # AUMID = <PackageFamilyName>!App
   ```
5. Set the required environment variable:
   ```powershell
   $env:STORYLINES_TEST_AUMID = "3597CaffeStudios.Storylines_abc123xyz!App"
   ```

#### Running the tests

Start the Appium server in one terminal, then run the tests in another:

```powershell
# Terminal 1 — keep running while tests execute
appium

# Terminal 2
dotnet test src/Storylines.UITests/Storylines.UITests.csproj
```

#### Environment variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `STORYLINES_TEST_AUMID` | Yes | — | Package AUMID (see step 4 above) |
| `STORYLINES_APPIUM_URL` | No | `http://127.0.0.1:4723` | Appium server URL |

#### Writing new UI tests

- Add `AutomationProperties.AutomationId="YourId"` to any XAML element you want to target.
- Locate it in tests with `driver.FindElement(MobileBy.AccessibilityId("YourId"))`.
- Add the test class to `src/Storylines.UITests/Tests/` and use `IClassFixture<AppFixture>` to share the driver session.

### Visual Studio

1. Open `src/Storylines.sln`.
2. Select the `Debug` configuration and `x64` platform.
3. Restore NuGet packages if Visual Studio prompts you.
4. Build and run the `Storylines` project.

## Project Links

- Release notes: [change-log.md](./change-log.md)
- Contribution guide: [CONTRIBUTING.md](./CONTRIBUTING.md)
- Privacy policy: [privacy-policy.md](./privacy-policy.md)
- Microsoft Store release guide: [docs/microsoft-store-release.md](./docs/microsoft-store-release.md)

## Contributing

Bug reports, feature requests, translations, and pull requests are all welcome. If you want to help, start with [CONTRIBUTING.md](./CONTRIBUTING.md), or leave a review on the [Microsoft Store](https://www.microsoft.com/store/apps/9PN77P9WJ3CX).

## License

Storylines is released under the [MIT License](./LICENSE.md).
