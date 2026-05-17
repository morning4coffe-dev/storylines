using Windows.UI;
using Microsoft.UI.Xaml.Shapes;

namespace Storylines.Views.Pages;

/// <summary>
/// Story Pinboard page for visualizing and managing chapter connections.
/// Canvas rendering and pointer events stay in code-behind;
/// all data logic is delegated to StoryPinboardViewModel.
/// </summary>
public sealed partial class StoryPinboardPage : Page
{
    private readonly WindowContext _windowContext;
    private readonly StoryPinboardViewModel _viewModel;

    private const double CardWidth = 200;
    private const double CardHeight = 180;

    private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();

    // UI state for drag/drop (inherently view-layer)
    private Grid _draggingCard;
    private Chapter _draggingChapter;
    private Point _dragOffset;
    private Point _dragStartPoint;

    // Visual elements for connections
    private readonly Dictionary<string, Grid> _cardElements = new Dictionary<string, Grid>();
    private readonly List<Path> _connectionPaths = new List<Path>();
    private readonly List<FrameworkElement> _connectionLabels = new List<FrameworkElement>();

    public StoryPinboardViewModel ViewModel => _viewModel;

    public StoryPinboardPage()
    {
        _windowContext = App.GetService<WindowContext>();
        _viewModel = App.GetService<StoryPinboardViewModel>();

        InitializeComponent();

        _viewModel.CanvasRebuildRequested += RebuildCanvas;
        _viewModel.ConnectionsChangedForView += RedrawConnections;
        _viewModel.ChapterNavigated += OnChapterNavigated;
    }

    private void OnChapterNavigated(int index)
    {
        if (_windowContext?.ChapterList?.listView is not null)
            _windowContext.ChapterList.listView.SelectedIndex = index;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (_windowContext?.AppView is not null)
            _windowContext.AppView.page = AppView.Pages.MainPage;

        _viewModel.Initialize();
    }

    // ─── Tag filtering (delegated to VM) ─────────────────────────

    private void OnTagFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.SelectedTagIndex = tagFilterComboBox.SelectedIndex;
    }

    // ─── Canvas rendering ─────────────────────────────────────────

    private void RebuildCanvas()
    {
        ClearPendingConnectionSelection();

        pinboardCanvas.Children.Clear();
        _cardElements.Clear();
        _connectionPaths.Clear();
        _connectionLabels.Clear();

        _viewModel.AssignDefaultPositions();

        var filteredView = _viewModel.FilteredChapters;

        // Create chapter cards
        for (int i = 0; i < filteredView.Count; i++)
        {
            var chapter = filteredView[i];
            var card = CreateChapterCard(chapter, i);
            _cardElements[chapter.Token] = card;

            Canvas.SetLeft(card, chapter.PinboardX);
            Canvas.SetTop(card, chapter.PinboardY);
            Canvas.SetZIndex(card, 10);
            pinboardCanvas.Children.Add(card);
        }

        // Draw connection lines behind cards using measured bounds.
        DrawConnections();
    }

    private Grid CreateChapterCard(Chapter chapter, int displayIndex)
    {
        int chapterNumber = _viewModel.AllChapters.IndexOf(chapter) + 1;
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
            MinHeight = CardHeight,
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
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

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
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
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
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
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
        var characterNames = _viewModel.DetectCharactersInChapter(chapter);
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
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
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

        if (chapter.Tags is not null && chapter.Tags.Count > 0)
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

        if (chapter.PlotThreads is not null && chapter.PlotThreads.Count > 0)
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

        card.Measure(new Size(CardWidth, double.PositiveInfinity));

        return card;
    }

    // ─── Connection drawing ───────────────────────────────────────

    private void DrawConnections()
    {
        var connections = _viewModel.AllChapters.Count > 0 ? App.GetService<ProjectState>().PinboardConnections : null;
        if (connections is null) return;

        var allChapters = _viewModel.AllChapters;
        var filteredView = _viewModel.FilteredChapters;

        // Build a set of visible chapter indices for filtering
        var visibleTokens = new HashSet<string>(filteredView.Select(c => c.Token));

        foreach (var conn in connections)
        {
            if (conn.FromIndex < 0 || conn.FromIndex >= allChapters.Count ||
                conn.ToIndex < 0 || conn.ToIndex >= allChapters.Count)
                continue;

            var fromChapter = allChapters[conn.FromIndex];
            var toChapter = allChapters[conn.ToIndex];

            if (!visibleTokens.Contains(fromChapter.Token) || !visibleTokens.Contains(toChapter.Token))
                continue;

            var connection = CanvasConnectionGeometry.CreatePinboardConnection(
                GetChapterBounds(fromChapter),
                GetChapterBounds(toChapter));
            var path = CreateConnectionPath(fromChapter, toChapter, connection);
            _connectionPaths.Add(path);
            Canvas.SetZIndex(path, 0);
            pinboardCanvas.Children.Add(path);

            // Add label if present
            if (!string.IsNullOrWhiteSpace(conn.Label))
            {
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
                Canvas.SetLeft(label, connection.Label.X - 30);
                Canvas.SetTop(label, connection.Label.Y - 10);
                Canvas.SetZIndex(label, 1);
                _connectionLabels.Add(label);
                pinboardCanvas.Children.Add(label);
            }
        }
    }

    private CanvasConnectionRect GetChapterBounds(Chapter chapter)
    {
        if (chapter is not null && !string.IsNullOrWhiteSpace(chapter.Token) && _cardElements.TryGetValue(chapter.Token, out var card))
        {
            var height = Math.Max(card.ActualHeight, card.DesiredSize.Height);
            if (height > 0)
            {
                return new CanvasConnectionRect(
                    chapter.PinboardX,
                    chapter.PinboardY,
                    CardWidth,
                    Math.Max(CardHeight, height));
            }
        }

        return new CanvasConnectionRect(chapter.PinboardX, chapter.PinboardY, CardWidth, CardHeight);
    }

    private Path CreateConnectionPath(Chapter from, Chapter to, CanvasBezierConnection connection)
    {
        var pathFigure = new PathFigure
        {
            StartPoint = new Point(connection.Start.X, connection.Start.Y),
            IsClosed = false
        };
        pathFigure.Segments.Add(new BezierSegment
        {
            Point1 = new Point(connection.Control1.X, connection.Control1.Y),
            Point2 = new Point(connection.Control2.X, connection.Control2.Y),
            Point3 = new Point(connection.End.X, connection.End.Y)
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
                int fromIdx = _viewModel.FindChapterIndex(tokens[0]);
                int toIdx = _viewModel.FindChapterIndex(tokens[1]);
                _viewModel.RemoveConnection(fromIdx, toIdx);
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
                int fromIdx = _viewModel.FindChapterIndex(tokens[1]);
                int toIdx = _viewModel.FindChapterIndex(tokens[2]);
                _viewModel.RemoveConnection(fromIdx, toIdx);
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
            _draggingChapter = _viewModel.FilteredChapters.FirstOrDefault(c => c.Token == token);
            if (_draggingChapter is null) return;

            _draggingCard = card;
            _dragMoved = false;

            var pos = e.GetCurrentPoint(pinboardCanvas).Position;
            _dragStartPoint = pos;
            _dragOffset = new Point(pos.X - Canvas.GetLeft(card), pos.Y - Canvas.GetTop(card));

            if (connectModeToggle.IsChecked == true)
            {
                e.Handled = true;
                return;
            }

            _isDragging = true;

            card.CapturePointer(e.Pointer);

            // Bring card to front
            Canvas.SetZIndex(card, 100);
            card.Opacity = 0.85;

            e.Handled = true;
        }
    }

    private void Card_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging || _draggingCard is null) return;

        var pos = e.GetCurrentPoint(pinboardCanvas).Position;

        if (!_dragMoved && !CanvasConnectionGeometry.HasMovedBeyondThreshold(
            new CanvasConnectionPoint(_dragStartPoint.X, _dragStartPoint.Y),
            new CanvasConnectionPoint(pos.X, pos.Y)))
        {
            return;
        }

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
        if (_isDragging && _draggingCard is not null)
        {
            _draggingCard.ReleasePointerCapture(e.Pointer);
            Canvas.SetZIndex(_draggingCard, 0);
            _draggingCard.Opacity = 1.0;
        }

        if (connectModeToggle.IsChecked == true && _draggingChapter is not null)
        {
            HandleCardClick(_draggingChapter);
        }
        else if (_isDragging && _dragMoved)
        {
            _viewModel.OnDragCompleted();
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
        if (_isDragging && _draggingCard is not null)
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
        if (chapter is null) return;

        if (connectModeToggle.IsChecked == true)
        {
            HandleConnectClick(chapter);
            return;
        }

        _viewModel.NavigateToChapter(chapter);
    }

    // ─── Connect mode ─────────────────────────────────────────────

    private void HandleConnectClick(Chapter chapter)
    {
        var (action, fromIdx, toIdx) = _viewModel.HandleConnectClick(chapter);

        switch (action)
        {
            case ConnectAction.HighlightSource:
                HighlightCard(chapter.Token, true);
                break;
            case ConnectAction.ClearHighlight:
                // Clear all highlights
                foreach (var kvp in _cardElements)
                    HighlightCard(kvp.Key, false);
                break;
            case ConnectAction.CreateConnection:
                foreach (var kvp in _cardElements)
                    HighlightCard(kvp.Key, false);
                _ = _viewModel.AddConnectionAsync(fromIdx, toIdx);
                break;
        }
    }

    private void ClearPendingConnectionSelection()
    {
        var connectSource = _viewModel.ConnectSource;
        if (connectSource is not null)
            HighlightCard(connectSource.Token, false);

        _viewModel.ClearPendingConnection();
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

    private void OnCanvas_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (!(e.OriginalSource is Canvas))
            return;

        if (connectModeToggle.IsChecked == true)
            ClearPendingConnectionSelection();
    }

    private void OnConnectModeToggle_Checked(object sender, RoutedEventArgs e)
    {
        ClearPendingConnectionSelection();
    }

    private void OnConnectModeToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        ClearPendingConnectionSelection();
    }

    // ─── Auto-arrange ─────────────────────────────────────────────

    private void OnAutoArrange_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AutoArrangeChaptersCommand.Execute(null);
    }
}
