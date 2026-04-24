using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Storylines.Pages
{
    public sealed partial class StoryPinboardPage : Page
    {
        private const double CardWidth = 200;
        private const double CardHeight = 180;

        private ObservableCollection<Chapter> _allChapters;
        private List<Chapter> _filteredView;
        private string _activeTagFilter;

        // Card UI elements keyed by chapter token
        private readonly Dictionary<string, Grid> _cardElements = new Dictionary<string, Grid>();

        // Drag state
        private Grid _draggingCard;
        private Chapter _draggingChapter;
        private Point _dragOffset;

        // Connect mode state
        private Chapter _connectSource;
        private readonly List<Path> _connectionPaths = new List<Path>();
        private readonly List<FrameworkElement> _connectionLabels = new List<FrameworkElement>();

        public StoryPinboardPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            AppView.current.page = AppView.Pages.MainPage;

            _allChapters = ServiceLocator.ProjectState.Chapters;
            _activeTagFilter = null;

            PopulateTagFilter();
            ApplyFilter();
        }

        // ─── Tag filtering ───────────────────────────────────────────

        private void PopulateTagFilter()
        {
            var allTags = _allChapters
                .Where(c => c.Tags != null)
                .SelectMany(c => c.Tags)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            tagFilterComboBox.Items.Clear();
            tagFilterComboBox.Items.Add("All chapters");
            foreach (var tag in allTags)
                tagFilterComboBox.Items.Add(tag);

            tagFilterComboBox.SelectedIndex = 0;
            tagFilterComboBox.Visibility = allTags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTagFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tagFilterComboBox.SelectedIndex <= 0)
                _activeTagFilter = null;
            else
                _activeTagFilter = tagFilterComboBox.SelectedItem as string;

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<Chapter> source = _allChapters;

            if (!string.IsNullOrEmpty(_activeTagFilter))
            {
                source = _allChapters.Where(c =>
                    c.Tags != null &&
                    c.Tags.Any(t => string.Equals(t, _activeTagFilter, StringComparison.CurrentCultureIgnoreCase)));
            }

            _filteredView = source.ToList();
            emptyState.Visibility = _filteredView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateSubtitle();
            RebuildCanvas();
        }

        private void UpdateSubtitle()
        {
            int total = _allChapters.Count;
            int shown = _filteredView.Count;

            if (string.IsNullOrEmpty(_activeTagFilter))
                subtitleText.Text = $"{total} chapter{(total == 1 ? "" : "s")}";
            else
                subtitleText.Text = $"{shown} of {total} chapter{(total == 1 ? "" : "s")} tagged \"{_activeTagFilter}\"";
        }

        // ─── Canvas rendering ─────────────────────────────────────────

        private void RebuildCanvas()
        {
            pinboardCanvas.Children.Clear();
            _cardElements.Clear();
            _connectionPaths.Clear();
            _connectionLabels.Clear();
            _connectSource = null;

            // Auto-assign positions for chapters that have none
            AssignDefaultPositions();

            // Draw connection lines first (behind cards)
            DrawConnections();

            // Create chapter cards
            for (int i = 0; i < _filteredView.Count; i++)
            {
                var chapter = _filteredView[i];
                var card = CreateChapterCard(chapter, i);
                _cardElements[chapter.Token] = card;

                Canvas.SetLeft(card, chapter.PinboardX);
                Canvas.SetTop(card, chapter.PinboardY);
                pinboardCanvas.Children.Add(card);
            }
        }

        private void AssignDefaultPositions()
        {
            const double startX = 40;
            const double startY = 40;
            const double spacingX = 240;
            const double spacingY = 220;
            const int columns = 5;

            int unpositioned = 0;
            foreach (var chapter in _filteredView)
            {
                if (chapter.PinboardX == 0 && chapter.PinboardY == 0)
                {
                    int idx = _allChapters.IndexOf(chapter);
                    int col = idx % columns;
                    int row = idx / columns;
                    chapter.PinboardX = startX + col * spacingX;
                    chapter.PinboardY = startY + row * spacingY;
                    unpositioned++;
                }
            }

            if (unpositioned > 0)
                Scripts.Functions.TimeTravelSystem.SomethingChanged();
        }

        private Grid CreateChapterCard(Chapter chapter, int displayIndex)
        {
            int chapterNumber = _allChapters.IndexOf(chapter) + 1;
            var accentBrush = (SolidColorBrush)Application.Current.Resources["SystemControlHighlightAccentBrush"];

            // Status color
            Color statusColor;
            switch (chapter.Status)
            {
                case ChapterStatus.Writing: statusColor = Color.FromArgb(255, 66, 165, 245); break;
                case ChapterStatus.Revision: statusColor = Color.FromArgb(255, 255, 167, 38); break;
                case ChapterStatus.Final: statusColor = Color.FromArgb(255, 102, 187, 106); break;
                default: statusColor = Color.FromArgb(255, 158, 158, 158); break;
            }

            // Card container
            var card = new Grid
            {
                Width = CardWidth,
                Padding = new Thickness(14),
                CornerRadius = new CornerRadius(8),
                Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Tag = chapter.Token
            };

            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: status + number + name
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: separator
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: location
            card.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 3: synopsis
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: character initials
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5: tags + plot threads

            // Row 0: Status dot + chapter number badge + name
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var statusDot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(statusColor),
                Margin = new Thickness(0, 7, 6, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            ToolTipService.SetToolTip(statusDot, chapter.Status.ToString());
            Grid.SetColumn(statusDot, 0);
            headerGrid.Children.Add(statusDot);

            var badge = new Border
            {
                CornerRadius = new CornerRadius(10),
                Width = 22,
                Height = 22,
                Background = accentBrush,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = chapterNumber.ToString(),
                    FontSize = 11,
                    FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(badge, 1);
            headerGrid.Children.Add(badge);

            var nameText = new TextBlock
            {
                Text = chapter.Name ?? "",
                FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 2);
            headerGrid.Children.Add(nameText);

            Grid.SetRow(headerGrid, 0);
            card.Children.Add(headerGrid);

            // Row 1: Separator
            var separator = new Rectangle
            {
                Height = 1,
                Fill = (Brush)Application.Current.Resources["SystemControlForegroundBaseLowBrush"],
                Opacity = 0.15,
                Margin = new Thickness(0, 6, 0, 4)
            };
            Grid.SetRow(separator, 1);
            card.Children.Add(separator);

            // Row 2: Location (if set)
            if (!string.IsNullOrWhiteSpace(chapter.Location))
            {
                var locationPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 0, 0, 4) };
                locationPanel.Children.Add(new FontIcon { Glyph = "\uE707", FontSize = 10, Opacity = 0.5 });
                locationPanel.Children.Add(new TextBlock
                {
                    Text = chapter.Location,
                    FontSize = 10,
                    Opacity = 0.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxLines = 1
                });
                Grid.SetRow(locationPanel, 2);
                card.Children.Add(locationPanel);
            }

            // Row 3: Synopsis
            var synopsisText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(chapter.Synopsis) ? "No synopsis" : chapter.Synopsis,
                FontSize = 12,
                Opacity = string.IsNullOrWhiteSpace(chapter.Synopsis) ? 0.3 : 0.65,
                FontStyle = string.IsNullOrWhiteSpace(chapter.Synopsis) ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 3,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(synopsisText, 3);
            card.Children.Add(synopsisText);

            // Row 4: Character initials found in this chapter's dialogue
            var characterNames = DetectCharactersInChapter(chapter);
            if (characterNames.Count > 0)
            {
                var charPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = -4, Margin = new Thickness(0, 6, 0, 0) };
                foreach (var charName in characterNames.Take(5))
                {
                    string initials = charName.Length >= 2 ? charName.Substring(0, 2).ToUpper() : charName.ToUpper();
                    var charBubble = new Border
                    {
                        Width = 22,
                        Height = 22,
                        CornerRadius = new CornerRadius(11),
                        Background = new SolidColorBrush(Color.FromArgb(180, 100, 100, 180)),
                        Child = new TextBlock
                        {
                            Text = initials,
                            FontSize = 8,
                            FontWeight = Windows.UI.Text.FontWeights.Bold,
                            Foreground = new SolidColorBrush(Colors.White),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    ToolTipService.SetToolTip(charBubble, charName);
                    charPanel.Children.Add(charBubble);
                }
                if (characterNames.Count > 5)
                {
                    charPanel.Children.Add(new TextBlock { Text = $"+{characterNames.Count - 5}", FontSize = 9, Opacity = 0.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) });
                }
                Grid.SetRow(charPanel, 4);
                card.Children.Add(charPanel);
            }

            // Row 5: Tags + plot threads
            var bottomPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, Margin = new Thickness(0, 6, 0, 0) };
            bool hasBottom = false;

            if (chapter.Tags != null && chapter.Tags.Count > 0)
            {
                foreach (var tag in chapter.Tags.Take(2))
                {
                    bottomPanel.Children.Add(new Border
                    {
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(5, 1, 5, 1),
                        Background = accentBrush,
                        Opacity = 0.85,
                        Child = new TextBlock { Text = tag, FontSize = 9, Foreground = new SolidColorBrush(Colors.White) }
                    });
                }
                hasBottom = true;
            }

            if (chapter.PlotThreads != null && chapter.PlotThreads.Count > 0)
            {
                foreach (var thread in chapter.PlotThreads.Take(2))
                {
                    bottomPanel.Children.Add(new Border
                    {
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(5, 1, 5, 1),
                        Background = new SolidColorBrush(Color.FromArgb(200, 156, 39, 176)),
                        Child = new TextBlock { Text = thread, FontSize = 9, Foreground = new SolidColorBrush(Colors.White) }
                    });
                }
                hasBottom = true;
            }

            if (hasBottom)
            {
                Grid.SetRow(bottomPanel, 5);
                card.Children.Add(bottomPanel);
            }

            // Pointer events for drag and click
            card.PointerPressed += Card_PointerPressed;
            card.PointerMoved += Card_PointerMoved;
            card.PointerReleased += Card_PointerReleased;
            card.PointerCanceled += Card_PointerCanceled;

            return card;
        }

        private List<string> DetectCharactersInChapter(Chapter chapter)
        {
            var characters = ServiceLocator.ProjectState.Characters;
            if (characters == null || characters.Count == 0 || string.IsNullOrEmpty(chapter.Text))
                return new List<string>();

            var found = new List<string>();
            string textLower = (chapter.Text ?? "").ToLowerInvariant();
            foreach (var c in characters)
            {
                if (!string.IsNullOrWhiteSpace(c.Name) && textLower.Contains(c.Name.ToLowerInvariant()))
                    found.Add(c.Name);
            }
            return found;
        }

        // ─── Connection drawing ───────────────────────────────────────

        private void DrawConnections()
        {
            var connections = ServiceLocator.ProjectState.PinboardConnections;
            if (connections == null) return;

            // Build a set of visible chapter indices for filtering
            var visibleTokens = new HashSet<string>(_filteredView.Select(c => c.Token));

            foreach (var conn in connections)
            {
                if (conn.FromIndex < 0 || conn.FromIndex >= _allChapters.Count ||
                    conn.ToIndex < 0 || conn.ToIndex >= _allChapters.Count)
                    continue;

                var fromChapter = _allChapters[conn.FromIndex];
                var toChapter = _allChapters[conn.ToIndex];

                if (!visibleTokens.Contains(fromChapter.Token) || !visibleTokens.Contains(toChapter.Token))
                    continue;

                var path = CreateConnectionPath(fromChapter, toChapter);
                _connectionPaths.Add(path);
                pinboardCanvas.Children.Add(path);

                // Add label if present
                if (!string.IsNullOrWhiteSpace(conn.Label))
                {
                    double midX = (fromChapter.PinboardX + toChapter.PinboardX) / 2 + CardWidth / 2;
                    double midY = (fromChapter.PinboardY + toChapter.PinboardY) / 2 + CardHeight / 2;

                    var label = new Border
                    {
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
                        BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                        BorderThickness = new Thickness(1),
                        Tag = $"label|{fromChapter.Token}|{toChapter.Token}",
                        Child = new TextBlock
                        {
                            Text = conn.Label,
                            FontSize = 10,
                            Opacity = 0.7,
                            TextAlignment = TextAlignment.Center
                        }
                    };
                    label.RightTapped += ConnectionLabel_RightTapped;
                    Canvas.SetLeft(label, midX - 30);
                    Canvas.SetTop(label, midY - 10);
                    _connectionLabels.Add(label);
                    pinboardCanvas.Children.Add(label);
                }
            }
        }

        private Path CreateConnectionPath(Chapter from, Chapter to)
        {
            double fromX = from.PinboardX + CardWidth / 2;
            double fromY = from.PinboardY + CardHeight / 2;
            double toX = to.PinboardX + CardWidth / 2;
            double toY = to.PinboardY + CardHeight / 2;

            // Determine if the connection is more horizontal or vertical
            double dx = toX - fromX;
            double dy = toY - fromY;

            double cp1X, cp1Y, cp2X, cp2Y;
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                // Horizontal bias: control points push horizontally
                cp1X = fromX + dx * 0.4;
                cp1Y = fromY;
                cp2X = toX - dx * 0.4;
                cp2Y = toY;
            }
            else
            {
                // Vertical bias: control points push vertically
                cp1X = fromX;
                cp1Y = fromY + dy * 0.4;
                cp2X = toX;
                cp2Y = toY - dy * 0.4;
            }

            var pathFigure = new PathFigure
            {
                StartPoint = new Point(fromX, fromY),
                IsClosed = false
            };
            pathFigure.Segments.Add(new BezierSegment
            {
                Point1 = new Point(cp1X, cp1Y),
                Point2 = new Point(cp2X, cp2Y),
                Point3 = new Point(toX, toY)
            });

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            var accentColor = ((SolidColorBrush)Application.Current.Resources["SystemControlHighlightAccentBrush"]).Color;

            var path = new Path
            {
                Data = pathGeometry,
                Stroke = new SolidColorBrush(accentColor) { Opacity = 0.55 },
                StrokeThickness = 2.5,
                StrokeDashArray = new DoubleCollection { 6, 3 },
                Tag = $"{from.Token}|{to.Token}"
            };

            // Right-click to delete connection
            path.RightTapped += Connection_RightTapped;

            return path;
        }

        private void Connection_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is Path path && path.Tag is string tagStr)
            {
                var tokens = tagStr.Split('|');
                if (tokens.Length == 2)
                {
                    int fromIdx = FindChapterIndex(tokens[0]);
                    int toIdx = FindChapterIndex(tokens[1]);
                    RemoveConnection(fromIdx, toIdx);
                }
            }
        }

        private void ConnectionLabel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string tagStr)
            {
                var tokens = tagStr.Split('|');
                if (tokens.Length == 3)
                {
                    int fromIdx = FindChapterIndex(tokens[1]);
                    int toIdx = FindChapterIndex(tokens[2]);
                    RemoveConnection(fromIdx, toIdx);
                }
            }
        }

        private void RedrawConnections()
        {
            // Remove old connection paths and labels
            foreach (var p in _connectionPaths)
                pinboardCanvas.Children.Remove(p);
            _connectionPaths.Clear();
            foreach (var l in _connectionLabels)
                pinboardCanvas.Children.Remove(l);
            _connectionLabels.Clear();

            DrawConnections();
        }

        // ─── Card drag-to-move ────────────────────────────────────────

        private bool _isDragging;
        private bool _dragMoved;

        private void Card_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid card)
            {
                var token = card.Tag as string;
                _draggingChapter = _filteredView.FirstOrDefault(c => c.Token == token);
                if (_draggingChapter == null) return;

                _draggingCard = card;
                _isDragging = true;
                _dragMoved = false;

                var pos = e.GetCurrentPoint(pinboardCanvas).Position;
                _dragOffset = new Point(pos.X - Canvas.GetLeft(card), pos.Y - Canvas.GetTop(card));

                card.CapturePointer(e.Pointer);

                // Bring card to front
                Canvas.SetZIndex(card, 100);
                card.Opacity = 0.85;

                e.Handled = true;
            }
        }

        private void Card_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || _draggingCard == null) return;

            // In connect mode, don't drag
            if (connectModeToggle.IsChecked == true) return;

            var pos = e.GetCurrentPoint(pinboardCanvas).Position;
            double newX = Math.Max(0, pos.X - _dragOffset.X);
            double newY = Math.Max(0, pos.Y - _dragOffset.Y);

            Canvas.SetLeft(_draggingCard, newX);
            Canvas.SetTop(_draggingCard, newY);

            _draggingChapter.PinboardX = newX;
            _draggingChapter.PinboardY = newY;

            _dragMoved = true;

            // Redraw connections live
            RedrawConnections();

            e.Handled = true;
        }

        private void Card_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_draggingCard != null)
            {
                _draggingCard.ReleasePointerCapture(e.Pointer);
                Canvas.SetZIndex(_draggingCard, 0);
                _draggingCard.Opacity = 1.0;
            }

            if (_isDragging && _dragMoved)
            {
                Scripts.Functions.TimeTravelSystem.SomethingChanged();
            }
            else if (_isDragging && !_dragMoved)
            {
                // This was a click, not a drag
                HandleCardClick(_draggingChapter);
            }

            _isDragging = false;
            _dragMoved = false;
            _draggingCard = null;
            _draggingChapter = null;

            e.Handled = true;
        }

        private void Card_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            if (_draggingCard != null)
            {
                Canvas.SetZIndex(_draggingCard, 0);
                _draggingCard.Opacity = 1.0;
            }

            _isDragging = false;
            _dragMoved = false;
            _draggingCard = null;
            _draggingChapter = null;
        }

        // ─── Card click handling ──────────────────────────────────────

        private void HandleCardClick(Chapter chapter)
        {
            if (chapter == null) return;

            if (connectModeToggle.IsChecked == true)
            {
                HandleConnectClick(chapter);
                return;
            }

            // Navigate to chapter
            int index = _allChapters.IndexOf(chapter);
            if (index < 0) return;

            ServiceLocator.Navigation.GoBack();
            ServiceLocator.TextEditor.SelectedChapterIndex = index;

            if (MainPage.ChapterList?.listView != null)
                MainPage.ChapterList.listView.SelectedIndex = index;
        }

        // ─── Connect mode ─────────────────────────────────────────────

        private void HandleConnectClick(Chapter chapter)
        {
            if (_connectSource == null)
            {
                // First click: highlight source
                _connectSource = chapter;
                HighlightCard(chapter.Token, true);
            }
            else
            {
                if (_connectSource.Token == chapter.Token)
                {
                    // Clicked same card: deselect
                    HighlightCard(_connectSource.Token, false);
                    _connectSource = null;
                    return;
                }

                // Second click: create connection
                int fromIdx = _allChapters.IndexOf(_connectSource);
                int toIdx = _allChapters.IndexOf(chapter);

                if (fromIdx >= 0 && toIdx >= 0)
                {
                    _ = AddConnectionAsync(fromIdx, toIdx);
                }

                HighlightCard(_connectSource.Token, false);
                _connectSource = null;
            }
        }

        private void HighlightCard(string token, bool highlight)
        {
            if (_cardElements.TryGetValue(token, out var card))
            {
                card.BorderThickness = highlight ? new Thickness(2.5) : new Thickness(1);
                if (highlight)
                    card.BorderBrush = (Brush)Application.Current.Resources["SystemControlHighlightAccentBrush"];
                else
                    card.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
            }
        }

        private async System.Threading.Tasks.Task AddConnectionAsync(int fromIndex, int toIndex)
        {
            var connections = ServiceLocator.ProjectState.PinboardConnections;

            // Don't add duplicates
            bool exists = connections.Any(c =>
                (c.FromIndex == fromIndex && c.ToIndex == toIndex) ||
                (c.FromIndex == toIndex && c.ToIndex == fromIndex));

            if (!exists)
            {
                // Prompt for an optional label
                var inputBox = new TextBox { PlaceholderText = "e.g. leads to, flashback of…", AcceptsReturn = false };
                var dialog = new ContentDialog
                {
                    Title = "Connection Label (optional)",
                    Content = inputBox,
                    PrimaryButtonText = "Create",
                    CloseButtonText = "Skip",
                    DefaultButton = ContentDialogButton.Primary
                };

                string label = null;
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputBox.Text))
                    label = inputBox.Text.Trim();

                connections.Add(new PinboardConnectionData { FromIndex = fromIndex, ToIndex = toIndex, Label = label });
                Scripts.Functions.TimeTravelSystem.SomethingChanged();
                RedrawConnections();
            }
        }

        private void RemoveConnection(int fromIndex, int toIndex)
        {
            var connections = ServiceLocator.ProjectState.PinboardConnections;
            int removed = connections.RemoveAll(c =>
                (c.FromIndex == fromIndex && c.ToIndex == toIndex) ||
                (c.FromIndex == toIndex && c.ToIndex == fromIndex));

            if (removed > 0)
            {
                Scripts.Functions.TimeTravelSystem.SomethingChanged();
                RedrawConnections();
            }
        }

        private int FindChapterIndex(string token)
        {
            for (int i = 0; i < _allChapters.Count; i++)
                if (_allChapters[i].Token == token) return i;
            return -1;
        }

        // ─── Auto-arrange ─────────────────────────────────────────────

        private void OnAutoArrange_Click(object sender, RoutedEventArgs e)
        {
            const double startX = 40;
            const double startY = 40;
            const double spacingX = 240;
            const double spacingY = 220;
            const int columns = 5;

            for (int i = 0; i < _filteredView.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                _filteredView[i].PinboardX = startX + col * spacingX;
                _filteredView[i].PinboardY = startY + row * spacingY;
            }

            Scripts.Functions.TimeTravelSystem.SomethingChanged();
            RebuildCanvas();
        }

        // ─── Back navigation ──────────────────────────────────────────

        private void OnBackButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceLocator.Navigation.GoBack();
        }
    }
}
