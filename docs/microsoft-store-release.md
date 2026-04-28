# Microsoft Store Release Guide

This document is the maintainer checklist for shipping a new Storylines version to the Microsoft Store.

## Quick Reference

- Store listing: https://www.microsoft.com/store/apps/9PN77P9WJ3CX
- Package identity: `3597CaffeStudios.Storylines`
- Publisher display name: `Morning4coffe`
- Manifest version source: `src/Storylines/Package.appxmanifest`
- Current manifest version at the time this guide was written: `0.7.5.0`
- Minimum supported Windows version: `10.0.18362.0`
- Preferred development platform: `x64`

## Pre-Release Checklist

- Confirm the release scope is complete and merged.
- Update [change-log.md](../change-log.md) with the final user-facing release notes.
- Review [README.md](../README.md) if the release materially changes user-visible features.
- Review [privacy-policy.md](../privacy-policy.md) if telemetry, crash reporting, or data collection changed.
- Verify any new user-facing strings were localized in the `.resw` files and translation assets.
- Refresh Store screenshots if the UI changed materially. The screenshots currently in `Logo and Screenshots/0-6` are still useful as references, but they should not be treated as always-current Store assets.

## Versioning

1. Open `src/Storylines/Package.appxmanifest`.
2. Update the `Identity Version` value.
3. Keep the four-part Store version format: `Major.Minor.Build.Revision`.
4. Make sure the version in release notes matches the manifest version you are submitting.

## Build And Test

Run the standard validation commands before creating the Store package:

```powershell
msbuild src/Storylines.sln /p:Platform=x64 /p:Configuration=Debug /t:Restore
msbuild src/Storylines.sln /p:Platform=x64 /p:Configuration=Debug
dotnet test src/Storylines.Tests/Storylines.Tests.csproj --platform x64
```

For a release candidate, also do a clean Visual Studio build with `Release` selected for the architectures you plan to publish.

## Package Creation

Use Visual Studio's Store packaging flow for the submission artifact.

1. Open `src/Storylines.sln` in Visual Studio.
2. Switch the `Storylines` project to the `Release` configuration.
3. Right-click the `Storylines` project.
4. Select `Store` > `Create App Packages...`.
5. Choose the Microsoft Store option for creating packages tied to the existing Store listing.
6. Select the architectures you want to publish. The project is already configured for `x86`, `x64`, and `ARM64`.
7. Let Visual Studio generate the upload package and symbol files.

Notes:

- Local package output currently shows debug `.msix` artifacts under `src/Storylines/bin/AppPackages`, but those are not the Store submission artifact you should upload.
- The project currently generates architecture-specific packages and resource packs. Rely on the Visual Studio Store packaging wizard to produce the correct upload artifact for Partner Center.

## Submission Metadata

Prepare these items before opening Partner Center:

- Final release title and version number
- Final release notes for the `What's new in this version` field
- Updated screenshots that match the current UI
- Privacy policy URL
- Support URL
- Category, markets, age rating, pricing, and availability review

Suggested public URLs:

- Support URL: https://github.com/morning4coffe-dev/Storylines/issues
- Source repository: https://github.com/morning4coffe-dev/Storylines
- Privacy policy URL: publish `privacy-policy.md` at a stable public address before submission, for example through the repository or a project website

## Draft Store Copy

### Short description

Storylines is an open-source Windows writing app for chapter-based stories, characters, dialogues, and focused drafting.

### Full description

Storylines is a modern writing app for Windows built around long-form storytelling. Instead of treating a manuscript like one endless document, it helps you organize work into chapters, keep track of characters, shape dialogue-heavy scenes, and review story structure while staying in a straightforward editor.

Use Storylines to draft, revise, and export your work with the support of Focus Mode, keyboard shortcuts, autosave, recovery, and accessibility-minded design. The app is open source, available through the Microsoft Store, and localized into multiple languages.

### Release notes starter

Use [change-log.md](../change-log.md) as the source of truth, then trim it into concise Store-facing bullets such as:

- Improved story editing and workflow polish
- Updated character and dialogue tools
- Focus Mode and Story Analysis improvements
- Localization updates
- Stability fixes and bug fixes

## Partner Center Submission Pass

Before clicking submit:

- Confirm the uploaded package version matches `Package.appxmanifest`.
- Confirm the listing text does not mention removed or unreleased features.
- Confirm screenshots, descriptions, and privacy policy link are current.
- Review crash or telemetry changes one more time if the active telemetry provider behavior changed.
- Double-check the availability date and pricing settings.

## Post-Release Verification

After the Store publishes the update:

- Install or update Storylines from the Store on a test machine.
- Verify the app launches and opens existing projects.
- Confirm the reported version matches the submitted package.
- Smoke-test saving, loading, export, Focus Mode, and Story Analysis.
- Review early crash telemetry and user reports for regression signals.