# Storylines

<img src="./Logo and Screenshots/Storylines-icon.png" width="100" alt="Storylines app icon" />

Storylines is an open-source Windows writing app for building stories chapter by chapter. It combines a clean editor with tools for characters, dialogue-heavy scenes, story analysis, focused drafting, and accessibility-minded workflows.

[![Microsoft Store](https://img.shields.io/static/v1?label=Microsoft&message=Download&color=0078D4&style=for-the-badge&logo=microsoft)](https://www.microsoft.com/store/apps/9PN77P9WJ3CX)
[![License: MIT](https://img.shields.io/badge/License-MIT-black?style=for-the-badge)](./LICENSE.md)

Storylines is designed for writers who want more structure than a plain text file without jumping into a heavyweight tool. Draft chapters, keep track of characters, work with dialogue-focused scenes, review story information, and stay in flow with Focus Mode, keyboard shortcuts, and recovery features.

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
- Visual Studio 2019 or later with the Universal Windows Platform development workload

### Command line

```powershell
msbuild src/Storylines.sln /p:Platform=x64 /p:Configuration=Debug /t:Restore
msbuild src/Storylines.sln /p:Platform=x64 /p:Configuration=Debug
dotnet test src/Storylines.Tests/Storylines.Tests.csproj --platform x64
```

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
