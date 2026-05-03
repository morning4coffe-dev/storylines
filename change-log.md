# Changelog

All notable changes to Storylines are documented in this file.

## Versioning

- Starting with the next release, Storylines uses semantic versioning for user-facing releases: `MAJOR.MINOR.PATCH`.
- Microsoft Store packages and assembly metadata keep the required four-part format: `MAJOR.MINOR.PATCH.REVISION`.
- Keep `REVISION` at `0` for normal releases. Only bump it when resubmitting the same release to the Store or rebuilding packaging without user-facing changes.
- Bump `MINOR` for new features or meaningful UX updates, `PATCH` for fixes, localization, and maintenance, and `MAJOR` when compatibility or expectations change in a significant way.
- Add new notes under `## [Unreleased]`, then rename that section to the final version when shipping.

## [Unreleased]

- No unreleased changes yet.

The entries below are kept from the previous changelog style. They may describe legacy release trains rather than every individual Store submission.

## [0.7] (legacy)

- The Information Dialog is now Story Analysis with a design overhaul and new features (View > Story Analysis).
- Focus Mode received new features and fixes (View > Focus Mode).
- The Characters page was updated as well (View > Characters).
- Updated localization.
- Unified design across the app.
- Improved stability.
- Additional bug fixes.

## [0.6.9] (legacy)

- All keyboard shortcuts are now visible in the new dialog, which you can access from Help > Keyboard shortcuts.
- Chinese, Russian, and Italian translations were added by the community (some strings may still be untranslated in this build).
- Continued dialog updates.
- Fixed a bug where changing the translation and then restarting the app would not display the selected language.
- Underlying code improvements.
- Minor bug fixes.

## [0.6.8] (legacy)

- All keyboard shortcuts are now visible in the new dialog, which you can access from Help > Keyboard shortcuts.
- Chinese and Russian translations were added by the community.
- Continued dialog updates.
- Fixed a bug where changing the translation and then restarting the app would not display the selected language.
- Underlying code improvements.

## [0.6.4] (legacy)

- Storylines now warns you when there are no characters in the current project and you try to add a dialogue.
- Experimented with a new dialog design, currently visible in the Export dialog.
- Minor bug fixes.

## [0.6.2] (legacy)

- The voice dictation button has been enabled in this version.
- Zoom in on text with Ctrl + Mouse Wheel Scroll.
- Chapters and characters can now be renamed with a double-click.
- Updated labels and other strings.
- Minor bug fixes.

## [0.6.1] (legacy)

- Fixed progress loss when switching between chapters.
- Fixed an issue in the Export dialog where "This file already exists..." sometimes appeared even when the file did not exist.
- Updated strings across the app.

## [0.6] (legacy)

- New design across the whole app.
- Characters now have a new home.
- New Settings experience inspired by Windows 11.
- Redesigned menus such as Save and Export with many new features.
- Focus Mode for staying on what matters most.
- Read aloud for narrating the content of the currently selected chapter.
- Voice dictation for typing with your voice.
- Better pen support.
- “Complex dialogues” with more features, easier export, and future support for dialogue branching.
- A proper Undo and Redo system (still a little buggy in special cases).
- New notification system.
- Czech localization.
- Custom chapter names, for example `Part 1: The new beginning`.
- Custom accent color support.
- More story information.
- Animations across the whole app.
- Many other improvements and bug fixes.