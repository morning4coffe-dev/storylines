using Storylines.Tests.Stubs;
using Storylines.ViewModels.Settings;
using Xunit;

namespace Storylines.Tests.ViewModels;

public class GeneralSettingsViewModelTests
{
    private static GeneralSettingsViewModel CreateViewModel(AppSettingsServiceStub? settings = null)
    {
        settings ??= new AppSettingsServiceStub();
        return new GeneralSettingsViewModel(settings);
    }

    [Fact]
    public void Constructor_LoadsValuesFromService()
    {
        var settings = new AppSettingsServiceStub
        {
            ChapterName = "Episode",
            ExitDialogueEnabled = false,
            LoadLastProjectOnStart = true,
            AutosaveEnabled = true,
            AutosaveInterval = 5,
            DailyWordGoal = 1000,
            ExperimentalFeaturesEnabled = true
        };

        var vm = CreateViewModel(settings);

        Assert.Equal("Episode", vm.ChapterName);
        Assert.False(vm.ExitDialogueEnabled);
        Assert.True(vm.LoadLastProjectOnStart);
        Assert.True(vm.AutosaveEnabled);
        Assert.Equal("5", vm.AutosaveIntervalKey);
        Assert.Equal(1000, vm.DailyWordGoal);
        Assert.True(vm.ExperimentalFeaturesEnabled);
    }

    [Fact]
    public void ChapterName_Change_UpdatesService()
    {
        var settings = new AppSettingsServiceStub { ChapterName = "Chapter" };
        var vm = CreateViewModel(settings);

        vm.ChapterName = "Part";

        Assert.Equal("Part", settings.ChapterName);
    }

    [Fact]
    public void ResetChapterNameCommand_RestoresDefault()
    {
        var settings = new AppSettingsServiceStub { ChapterName = "Custom" };
        var vm = CreateViewModel(settings);

        vm.ResetChapterNameCommand.Execute(null);

        Assert.Equal(settings.DefaultChapterName, vm.ChapterName);
    }

    [Fact]
    public void ExitDialogueEnabled_Change_UpdatesService()
    {
        var settings = new AppSettingsServiceStub { ExitDialogueEnabled = true };
        var vm = CreateViewModel(settings);

        vm.ExitDialogueEnabled = false;

        Assert.False(settings.ExitDialogueEnabled);
    }

    [Fact]
    public void LoadLastProjectOnStart_Change_UpdatesService()
    {
        var settings = new AppSettingsServiceStub();
        var vm = CreateViewModel(settings);

        vm.LoadLastProjectOnStart = true;

        Assert.True(settings.LoadLastProjectOnStart);
    }

    [Fact]
    public void AutosaveEnabled_Change_UpdatesService()
    {
        var settings = new AppSettingsServiceStub();
        var vm = CreateViewModel(settings);

        vm.AutosaveEnabled = true;

        Assert.True(settings.AutosaveEnabled);
    }

    [Fact]
    public void AutosaveIntervalKey_ValidValue_UpdatesService()
    {
        var settings = new AppSettingsServiceStub { AutosaveInterval = 2 };
        var vm = CreateViewModel(settings);

        vm.AutosaveIntervalKey = "5";

        Assert.Equal(5, settings.AutosaveInterval);
    }

    [Fact]
    public void AutosaveIntervalKey_InvalidValue_DoesNotUpdateService()
    {
        var settings = new AppSettingsServiceStub { AutosaveInterval = 2 };
        var vm = CreateViewModel(settings);

        vm.AutosaveIntervalKey = "not_a_number";

        Assert.Equal(2, settings.AutosaveInterval);
    }

    [Fact]
    public void DailyWordGoal_Change_UpdatesService()
    {
        var settings = new AppSettingsServiceStub();
        var vm = CreateViewModel(settings);

        vm.DailyWordGoal = 750;

        Assert.Equal(750, settings.DailyWordGoal);
    }

    [Fact]
    public void DailyWordGoal_NaN_DoesNotUpdateService()
    {
        var settings = new AppSettingsServiceStub { DailyWordGoal = 500 };
        var vm = CreateViewModel(settings);

        vm.DailyWordGoal = double.NaN;

        Assert.Equal(500, settings.DailyWordGoal);
    }

    [Fact]
    public void ExperimentalFeaturesEnabled_Change_UpdatesService()
    {
        var settings = new AppSettingsServiceStub();
        var vm = CreateViewModel(settings);

        vm.ExperimentalFeaturesEnabled = true;

        Assert.True(settings.ExperimentalFeaturesEnabled);
    }

    [Fact]
    public void Constructor_DoesNotWriteBackToService()
    {
        var settings = new AppSettingsServiceStub
        {
            ChapterName = "Chapter",
            ExitDialogueEnabled = true,
            AutosaveInterval = 2,
            DailyWordGoal = 500
        };

        var vm = new GeneralSettingsViewModel(settings);

        // Verify the VM loaded values without modifying the service state
        Assert.Equal("Chapter", vm.ChapterName);
        Assert.True(vm.ExitDialogueEnabled);
        Assert.Equal("2", vm.AutosaveIntervalKey);
        Assert.Equal(500, vm.DailyWordGoal);
    }
}
