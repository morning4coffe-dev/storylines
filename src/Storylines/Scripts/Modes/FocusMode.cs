using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Variables;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Scripts.Modes
{
    public class FocusMode
    {
        private Visibility chaptersViewEnabled;
        private CommandBar CommandBar;
        private Grid commandBarGrid;

        private ApplicationView view = ApplicationView.GetForCurrentView();

        public enum ToMeasure { Characters, Words, Paragraphs };
        private ToMeasure toMeasure;

        private DispatcherTimer timer;
        private TimeSpan timerTime;
        private long timerStartTime;

        public bool fullScreen;

        private int measureValue;
        private int measureValueStart;

        private string downBarTime;
        private string downBarMeasure;

        private bool timeFinal;
        private bool measureFinal;
        public bool final;

        #region Switch
        public static void Switch(bool fullScreen, TimeSpan time, int measureValue, ToMeasure toMeasure)
        {
            MainPage.FocusMode = new FocusMode();

            MainPage.FocusMode.PrivateSwitch(fullScreen, time, measureValue, toMeasure);
        }

        private void PrivateSwitch(bool fullScreen, TimeSpan time, int measureValue, ToMeasure toMeasure)
        {
            MainPage.Current.OpenOrCloseChapterList(false, true);

            NewCommandBar();

            chaptersViewEnabled = MainPage.ChapterList.Visibility;

            MainPage.Current.storyInfo.Visibility = Visibility.Visible;
            MainPage.Current.storyInfoDetailed.Visibility = Visibility.Collapsed;
            MainPage.Current.downBarFocusText.Visibility = Visibility.Visible;

            MainPage.Current.line.Visibility = Visibility.Collapsed;

            AppView.current.BackButtonCheck();

            ModesShared.RemoveChapterTextCommandBar();

            if (time != new TimeSpan(0, 0, 0))
            {
                timer = new DispatcherTimer()
                {
                    Interval = new TimeSpan(0, 1, 0),
                };
                timerTime = time;

                timer.Tick += Timer_Tick;
                timer.Start();

                timerTime = timerTime.Add(new TimeSpan(0, 1, 0));
                Timer_Tick(timerTime, new object());

                timerStartTime = time.Ticks;
            }
            else
            {
                timeFinal = true;
            }

            if (fullScreen)
            {
                this.fullScreen = fullScreen;
                view.TryEnterFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            }

            MainPage.FocusMode.measureValue = measureValue;
            MainPage.FocusMode.toMeasure = toMeasure;

            if (measureValue > 0)
                switch (toMeasure)
                {
                    case ToMeasure.Characters:
                        measureValueStart = ProjectStatsDialogue.GetTextFromAllChapters().Length - 1;
                        break;
                    case ToMeasure.Words:
                        measureValueStart = ProjectStatsDialogue.GetTextFromAllChapters().Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;
                        break;
                    case ToMeasure.Paragraphs:
                        measureValueStart = System.Text.RegularExpressions.Regex.Matches(ProjectStatsDialogue.GetTextFromAllChapters(), "[^\r\n]+((\r|\n|\r\n)[^\r\n]+)*").Count;
                        break;
                }
            else
                measureFinal = true;

            Finalized(timeFinal, measureFinal);

            TextChanged();
            NotificationManager.DisplayMainProgressBar(false);
        }

        private void NewCommandBar()
        {
            commandBarGrid = ModesShared.NewCommandBarBackground();

            MainPage.Current.mainGrid.Children.Add(commandBarGrid);
            Grid.SetColumnSpan(commandBarGrid, 2);

            MainCommandBar mainCommandBarInstance = new MainCommandBar();
            CommandBar = ModesShared.NewCommandBar();

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.undoButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.undoButton);

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.redoButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.redoButton);

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.saveButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.saveButton);

            mainCommandBarInstance.saveButton.IsEnabled = true;

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.autosaveToggleButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.autosaveToggleButton);

            CommandBar.PrimaryCommands.Add(new AppBarSeparator());

            _ = mainCommandBarInstance.commandBarInsert.PrimaryCommands.Remove(mainCommandBarInstance.dialoguesEnableButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.dialoguesEnableButton);

            mainCommandBarInstance.dialoguesEnableButton.IsEnabled = Character.characters.Count > 0;

            _ = mainCommandBarInstance.commandBarInsert.PrimaryCommands.Remove(mainCommandBarInstance.dialoguesAddButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.dialoguesAddButton);

            mainCommandBarInstance.dialoguesAddButton.IsEnabled = Character.characters.Count > 0;

            _ = mainCommandBarInstance.commandBarInsert.PrimaryCommands.Remove(MainPage.CommandBar.dictationButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.dictationButton);

            CommandBar.PrimaryCommands.Add(new AppBarSeparator());

            _ = mainCommandBarInstance.commandBarHelp.PrimaryCommands.Remove(mainCommandBarInstance.readAloudButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.readAloudButton);

            _ = mainCommandBarInstance.commandBarHelp.PrimaryCommands.Remove(mainCommandBarInstance.readAloudControllHolder);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.readAloudControllHolder);

            MainPage.Current.mainGrid.Children.Remove(MainPage.CommandBar);

            MainPage.Current.mainGrid.Children.Add(CommandBar);
            Grid.SetColumnSpan(CommandBar, 2);
        }
        #endregion

        private void Timer_Tick(object sender, object e)
        {
            if (timerTime.Ticks > 0)
            {
                timerTime = timerTime.Subtract(new TimeSpan(0, 1, 0));
                downBarTime = $"{timerTime.Hours}:{timerTime.Minutes}";

                MeasureValuesChanged(timerTime.Ticks, dbm);
            }
            else
            {
                timer.Stop();
                Finalized(true, measureFinal);
                downBarTime = ResourceLoader.GetForCurrentView().GetString("done");
            }

            MainPage.Current.downBarFocusText.Text = $"{downBarMeasure}   {downBarTime}";
        }

        private int dbm;
        public void TextChanged()
        {
            if (MainPage.FocusMode.measureValue > 0)
            {
                switch (toMeasure)
                {
                    case ToMeasure.Characters:
                        int ch = ProjectStatsDialogue.GetTextFromAllChapters().Length - 1;
                        downBarMeasure = $"{ch - measureValueStart} / {measureValue}";
                        dbm = ch - measureValueStart;
                        break;
                    case ToMeasure.Words:
                        int w = ProjectStatsDialogue.GetTextFromAllChapters().Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;
                        downBarMeasure = $"{w - measureValueStart} / {measureValue}";
                        dbm = w - measureValueStart;
                        break;
                    case ToMeasure.Paragraphs:
                        int p = System.Text.RegularExpressions.Regex.Matches(ProjectStatsDialogue.GetTextFromAllChapters(), "[^\r\n]+((\r|\n|\r\n)[^\r\n]+)*").Count;
                        downBarMeasure = $"{p - measureValueStart} / {measureValue}";
                        dbm = p - measureValueStart;
                        break;
                }
                if (Math.Abs(dbm) >= measureValue)
                    Finalized(timeFinal, true);
                else
                    Finalized(timeFinal, false);

                MeasureValuesChanged(timerTime.Ticks, dbm);
                MainPage.Current.downBarFocusText.Text = $"{downBarMeasure}   {downBarTime}";
            }
        }

        private void Finalized(bool time, bool measure)
        {
            timeFinal = time;
            measureFinal = measure;

            if (timeFinal && measureFinal)
                final = true;
            else
                final = false;
        }

        public void MeasureValuesChanged(long currentTime, int currentMeasure)
        {
            var percentage = 0;
            var multiplier = 50;
            if (timerStartTime < 1 || measureValue < 1)
                multiplier = 100;

            if (measureValue > 0)
                if (currentMeasure < measureValue)
                    percentage += (int)Math.Round((double)(multiplier * currentMeasure) / measureValue);
                else
                    percentage += multiplier;
            if (timerStartTime > currentTime)
                percentage += (int)((timerStartTime - currentTime) * multiplier / timerStartTime);

            NotificationManager.UpdateMainProgressBar(percentage, NotificationManager.ProgressState.Normal);
        }

        #region Leave
        public void Leave()
        {
            if (MainPage.FocusMode.timer != null)
                MainPage.FocusMode.timer.Stop();

            MainPage.ChapterList.listView.Visibility = chaptersViewEnabled;

            MainPage.Current.mainGrid.Children.Remove(commandBarGrid);

            MainPage.Current.mainGrid.Children.Remove(MainPage.FocusMode.CommandBar);
            MainPage.Current.mainGrid.Children.Add(MainPage.CommandBar);
            Grid.SetColumnSpan(MainPage.CommandBar, 2);

            MainPage.Current.line.Visibility = Visibility.Visible;

            MainPage.ChapterText.gridHolder.RowDefinitions.Insert(0, new RowDefinition() { Height = new GridLength(48, GridUnitType.Pixel) });
            MainPage.ChapterText.gridHolder.RowDefinitions.Insert(1, new RowDefinition() { Height = new GridLength(4.5, GridUnitType.Pixel) });
            MainPage.ChapterText.gridCommandBarHolder.Visibility = Visibility.Visible;

            MainPage.Current.storyInfo.Visibility = Visibility.Collapsed;
            MainPage.Current.storyInfoDetailed.Visibility = Visibility.Visible;
            MainPage.Current.downBarFocusText.Visibility = Visibility.Collapsed;

            if (fullScreen)
            {
                view.ExitFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            }

            NotificationManager.HideMainProgressBar();

            MainPage.FocusMode = null;

            AppView.current.BackButtonCheck();
        }
        #endregion
    }
}
