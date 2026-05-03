# Storylines — Agent & Contributor Guidelines

## Architecture

**Platform**: UWP (Universal Windows Platform) — `TargetPlatformVersion` 10.0.26100.0, min 10.0.18362.0  
**Pattern**: MVVM with `CommunityToolkit.Mvvm` (`ObservableObject`, `RelayCommand`)  
**Inter-component communication**: `EventAggregator` (publish/subscribe, not direct references)  
**Service wiring**: Static `ServiceLocator` (see `Services/ServiceLocator.cs`)  
**Serialization**: JSON (primary) with legacy `.srl` fallback via `ISaveSerializer`

### Key folders

| Folder | Purpose |
|--------|---------|
| `Models/` | Data models — `Chapter`, `Character`, `ProjectData`, `ProjectState`, `ProjectFile` |
| `ViewModels/` | MVVM ViewModels bound to pages/controls |
| `Views/Pages/` | Top-level navigation pages |
| `Views/Pages/Settings/` | Settings sub-pages |
| `Views/Controls/` | Reusable XAML UserControls |
| `Views/Dialogs/` | ContentDialog windows |
| `Services/` | Services — `SaveSystem`, `EventAggregator`, `FileService`, `NavigationService`, `ExportSystem` |
| `Services/Interfaces/` | Service interfaces |
| `Services/Serializers/` | Save file serializers (`JsonSaveSerializer`, `LegacySrlSerializer`) |
| `Helpers/` | Stateless helpers — `TimeTravelSystem`, `TextHighlighter`, `ShortcutManager` |
| `Helpers/Modes/` | UI modes — `FocusMode`, `ReadMode` |
| `DataStructures/` | Generic data structures — `PartialStack<T>` |
| `Constants/` | Named constants (`LayoutConstants`) |
| `Converters/` | XAML value converters |
| `Resources/` | Localization `.resw` files, `ResourceDictionaries/` |

## Build & Test

```powershell
# Restore + build (use x64 for development)
msbuild src/Storylines.sln /p:Platform=x64 /p:Configuration=Debug /t:Restore
msbuild src/Storylines.sln /p:Platform=x64 /p:Configuration=Debug

# Run tests
dotnet test src/Storylines.Tests/Storylines.Tests.csproj --platform x64
```

Requires **Visual Studio 2019+** with the **Universal Windows Platform development** workload.

## Code Style

- C# 9+ features (pattern matching, target-typed new, etc.)
- `var` preferred for obvious types
- `private readonly` fields — `_camelCase` prefix
- Constants in `LayoutConstants` or as `const` members — never magic numbers in XAML code-behind
- ViewModels inherit `ObservableObject`; use `[ObservableProperty]` or `SetProperty()`
- No `async void` except event handlers
- Log errors via `ServiceLocator.Logger.Error(message, exception)`

## Localization

All user-facing strings **must** be localized. Never hardcode text in XAML or C#.

### XAML strings
Use `x:Uid` bindings. The Uid maps to a key in `Resources/<lang>/Resources.resw`:

```xml
<!-- XAML -->
<TextBlock x:Uid="myTextBlock" />

<!-- en/Resources.resw -->
<!-- Name: myTextBlock.Text  |  Value: Hello World -->
```

For properties other than `Text`, append the property name: `myButton.Content`, `myBox.PlaceholderText`, `myBox.Header`.

### C# strings
Use the existing wrapper pattern in `Resources/Strings.cs`:

```csharp
private static readonly ResourceLoader _resources =
    ResourceLoader.GetForViewIndependentUse("Resources");
// Then: _resources.GetString("KeyName");
```

### Adding/updating translations
1. Add the English key + value to `Resources/en/Resources.resw` (or the appropriate `.resw`)
2. Add corresponding entries to **all** language folders: `cs/`, `hi-IN/`, `it/`, `pl/`, `ru/`, `zh-CN/`
3. Update the `.xlf` files in `MultilingualResources/` if using the Multilingual App Toolkit
4. See `CONTRIBUTING.md` for full workflow with the Multilingual App Toolkit extension

### Supported languages
`en` (English, reference), `cs` (Czech), `hi-IN` (Hindi), `it` (Italian), `pl` (Polish), `ru` (Russian), `zh-CN` (Chinese Simplified)

## Design System (Fluent Design)

Follow the [WinUI / Fluent Design System](https://learn.microsoft.com/en-us/windows/apps/design/) guidelines.

### Sizing & spacing (defined in `MainStyleDictionary.xaml`)

| Token | Value | Use for |
|-------|-------|---------|
| Compact button height | 28px | Icon-only toolbar buttons |
| Default button height | 32–36px | Standard buttons, combo boxes |
| Touch-friendly height | 44px | Primary action buttons on touch surfaces |
| Standard corner radius | 4px | Buttons, inputs, cards |
| Dialog corner radius | 8px | ContentDialog, flyout panels |
| Pill/badge corner radius | 12px | Tags, badges, chips |

### Accessibility requirements
- **Every** interactive element must have `AutomationProperties.Name` (or inherit from `x:Uid`)
- Icon-only buttons must have `ToolTipService.ToolTip`
- Use `x:Uid` for automation names when localization applies: set `myButton.AutomationProperties.Name` in `.resw`
- Support keyboard navigation — avoid `TabIndex` hacks; rely on natural tab order
- Test with Windows Narrator and Accessibility Insights

### Responsive design
- Use `VisualStateManager` with `AdaptiveTrigger` for breakpoints
- Key breakpoints defined in `LayoutConstants.cs`: `CompactBreakpoint = 800`, `CharactersCompactBreakpoint = 700`
- Settings pages: use `MaxWidth` (not `Width`) for content areas
- Dialogs: use `MinWidth`/`MaxWidth` instead of fixed `Width`

## Adding a New Feature

1. **Model** → Add/modify classes in `Models/`
2. **Service** → Add interface in `Services/Interfaces/`, implement in `Services/`, register in `ServiceLocator.Initialize()`
3. **ViewModel** → Add/modify in `ViewModels/`, register in `ServiceLocator` if shared
4. **View** → XAML in `Views/Pages/`, `Views/Controls/`, or `Views/Dialogs/`, bind to ViewModel via `DataContext`
5. **Localize** → Add all strings via `x:Uid` + `.resw` entries (see Localization section)
6. **Test** → Add unit tests in `Storylines.Tests/`
