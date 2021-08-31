using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Functions;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Storylines.Scripts.Modes.FocusMode;

namespace Storylines.Scripts.Modes
{
    public class FocusMode
    {
        private Visibility chaptersViewEnabled;
        private CommandBar commandBar;
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
            MainPage.focusMode = new FocusMode();

            MainPage.focusMode.PrivateSwitch(fullScreen, time, measureValue, toMeasure);
        }

        private void PrivateSwitch(bool fullScreen, TimeSpan time, int measureValue, ToMeasure toMeasure)
        {
            MainPage.current.OpenOrCloseChapterList(false, true);

            NewCommandBar();

            chaptersViewEnabled = MainPage.chapterList.Visibility;

            MainPage.current.storyInfo.Visibility = Visibility.Visible;
            MainPage.current.storyInfoDetailed.Visibility = Visibility.Collapsed;
            MainPage.current.downBarFocusText.Visibility = Visibility.Visible;

            MainPage.current.line.Visibility = Visibility.Collapsed;

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

            MainPage.focusMode.measureValue = measureValue;
            MainPage.focusMode.toMeasure = toMeasure;

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

            MainPage.current.mainGrid.Children.Add(commandBarGrid);
            Grid.SetColumnSpan(commandBarGrid, 2);

            MainCommandBar mainCommandBarInstance = new MainCommandBar();
            commandBar = ModesShared.NewCommandBar();

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.undoButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.undoButton);

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.redoButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.redoButton);

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.saveButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.saveButton);

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.autosaveToggleButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.autosaveToggleButton);

            commandBar.PrimaryCommands.Add(new AppBarSeparator());

            _ = mainCommandBarInstance.commandBarInsert.PrimaryCommands.Remove(mainCommandBarInstance.dialoguesEnableButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.dialoguesEnableButton);

            _ = mainCommandBarInstance.commandBarInsert.PrimaryCommands.Remove(mainCommandBarInstance.dialoguesAddButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.dialoguesAddButton);

            _ = mainCommandBarInstance.commandBarInsert.PrimaryCommands.Remove(MainPage.commandBar.dictationButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.dictationButton);

            commandBar.PrimaryCommands.Add(new AppBarSeparator());

            _ = mainCommandBarInstance.commandBarHelp.PrimaryCommands.Remove(mainCommandBarInstance.readAloudButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.readAloudButton);

            _ = mainCommandBarInstance.commandBarHelp.PrimaryCommands.Remove(mainCommandBarInstance.readAloudControllHolder);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.readAloudControllHolder);

            MainPage.current.mainGrid.Children.Remove(MainPage.commandBar);

            MainPage.current.mainGrid.Children.Add(commandBar);
            Grid.SetColumnSpan(commandBar, 2);
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

            MainPage.current.downBarFocusText.Text = $"{downBarMeasure}   {downBarTime}";
        }

        private int dbm;
        public void TextChanged()
        {
            if (MainPage.focusMode.measureValue > 0)
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
                MainPage.current.downBarFocusText.Text = $"{downBarMeasure}   {downBarTime}";
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
                percentage += (int)((timerStartTime - currentTime) * multiplier / timerStartTime);//nefunguje
            //else
            //    percentage += (int)Math.Round((double)(multiplier * Math.Abs(currentTime)) / timerStartTime);

            NotificationManager.UpdateMainProgressBar(percentage, NotificationManager.ProgressState.Normal);
        }

        #region Leave
        public void Leave()
        {
            if(MainPage.focusMode.timer != null)
                MainPage.focusMode.timer.Stop();

            MainPage.chapterList.listView.Visibility = chaptersViewEnabled;

            MainPage.current.mainGrid.Children.Remove(commandBarGrid);

            MainPage.current.mainGrid.Children.Remove(MainPage.focusMode.commandBar);
            MainPage.current.mainGrid.Children.Add(MainPage.commandBar);
            Grid.SetColumnSpan(MainPage.commandBar, 2);

            MainPage.current.line.Visibility = Visibility.Visible;

            MainPage.chapterText.gridHolder.RowDefinitions.Insert(0, new RowDefinition() { Height = new GridLength(48, GridUnitType.Pixel) });
            MainPage.chapterText.gridHolder.RowDefinitions.Insert(1, new RowDefinition() { Height = new GridLength(4.5, GridUnitType.Pixel) });
            MainPage.chapterText.gridCommandBarHolder.Visibility = Visibility.Visible;

            MainPage.current.storyInfo.Visibility = Visibility.Collapsed;
            MainPage.current.storyInfoDetailed.Visibility = Visibility.Visible;
            MainPage.current.downBarFocusText.Visibility = Visibility.Collapsed;

            if (fullScreen)
            {
                view.ExitFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            }

            NotificationManager.HideMainProgressBar();

            MainPage.focusMode = null;

            AppView.current.BackButtonCheck();
        }
        #endregion
    }
}
