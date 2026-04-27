using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Storylines.Views.Controls
{
    public sealed partial class BranchingDialogueCanvasControl : UserControl
    {
        private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();

        private const double NodeCardWidth = 200;
        private const double NodeCardHeight = 90;

        private readonly Dictionary<string, Border> _nodeCards = new Dictionary<string, Border>();
        private Border _draggingCard;
        private BranchingDialogueNodeData _draggingNode;
        private Point _dragOffset;

        // Connection mode state
        private bool _isConnecting;
        private BranchingDialogueNodeData _connectSourceNode;
        private Line _connectPreviewLine;

        public BranchingDialogueViewModel ViewModel { get; set; }

        public event Action<BranchingDialogueNodeData> NodeSelected;
        public event Action<BranchingDialogueNodeData> NodeDoubleTapped;
        public event Action<double, double> CanvasDoubleTapped;
        public event Action<BranchingDialogueNodeData, BranchingDialogueNodeData> ConnectionRequested;

        public BranchingDialogueCanvasControl()
        {
            InitializeComponent();
        }

        public void RedrawCanvas()
        {
            mapCanvas.Children.Clear();
            _nodeCards.Clear();

            if (ViewModel == null)
                return;

            var nodes = ViewModel.AllNodeTargets.ToList();
            var characters = App.TryGetService<ProjectState>()?.Characters;

            emptyState.Visibility = nodes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var node in nodes)
                EnsurePosition(node);

            DrawConnections(nodes);

            foreach (var node in nodes)
            {
                var card = CreateNodeCard(node, characters);
                _nodeCards[node.Id] = card;
                Canvas.SetLeft(card, (node.PositionX ?? 0) - 7);
                Canvas.SetTop(card, node.PositionY ?? 0);
                mapCanvas.Children.Add(card);
            }
        }

        public void ScrollToNode(BranchingDialogueNodeData node)
        {
            if (node?.PositionX == null || node?.PositionY == null)
                return;

            var zoom = canvasScrollViewer.ZoomFactor;
            var targetX = (node.PositionX ?? 0) * zoom - canvasScrollViewer.ActualWidth / 2 + NodeCardWidth * zoom / 2;
            var targetY = (node.PositionY ?? 0) * zoom - canvasScrollViewer.ActualHeight / 2 + NodeCardHeight * zoom / 2;

            canvasScrollViewer.ChangeView(
                Math.Max(0, targetX),
                Math.Max(0, targetY),
                null);

            RedrawCanvas();
        }

        public void AutoArrangeNodes()
        {
            if (ViewModel == null)
                return;

            var nodes = ViewModel.AllNodeTargets.ToList();
            if (nodes.Count == 0) return;

            var cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(nodes.Count)));
            var spacingX = NodeCardWidth + 80;
            var spacingY = NodeCardHeight + 60;

            for (int i = 0; i < nodes.Count; i++)
            {
                var col = i % cols;
                var row = i / cols;
                nodes[i].PositionX = 60 + col * spacingX;
                nodes[i].PositionY = 60 + row * spacingY;
            }

            var service = App.TryGetService<IBranchingDialogueService>();
            if (ViewModel.SelectedChapter != null)
                service?.NotifyGraphChanged(ViewModel.SelectedChapter.Token);

            RedrawCanvas();
        }

        public void ZoomIn()
        {
            var currentZoom = canvasScrollViewer.ZoomFactor;
            var newZoom = Math.Min(currentZoom * 1.25f, 2.0f);
            canvasScrollViewer.ChangeView(null, null, newZoom);
        }

        public void ZoomOut()
        {
            var currentZoom = canvasScrollViewer.ZoomFactor;
            var newZoom = Math.Max(currentZoom * 0.8f, 0.25f);
            canvasScrollViewer.ChangeView(null, null, newZoom);
        }

        #region Event Handlers

        private void OnCanvas_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (_isConnecting) { CancelConnection(); return; }
            var position = e.GetPosition(mapCanvas);
            CanvasDoubleTapped?.Invoke(position.X - NodeCardWidth / 2, position.Y - NodeCardHeight / 2);
        }

        private void OnCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isConnecting || _connectPreviewLine == null || _connectSourceNode == null)
                return;

            var pos = e.GetCurrentPoint(mapCanvas).Position;
            _connectPreviewLine.X2 = pos.X;
            _connectPreviewLine.Y2 = pos.Y;
        }

        private void OnCanvas_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (_isConnecting) { CancelConnection(); e.Handled = true; }
        }

        private void OnAddNode_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.CreateNodeCommand.Execute(null);
        }

        private void OnAutoArrange_Click(object sender, RoutedEventArgs e)
        {
            AutoArrangeNodes();
        }

        private void OnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        private void OnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }

        private void OnCancelConnection_Click(object sender, RoutedEventArgs e)
        {
            CancelConnection();
        }

        #endregion

        #region Connection Mode

        private void StartConnection(BranchingDialogueNodeData sourceNode)
        {
            _isConnecting = true;
            _connectSourceNode = sourceNode;

            var startX = (sourceNode.PositionX ?? 0) + NodeCardWidth;
            var startY = (sourceNode.PositionY ?? 0) + NodeCardHeight / 2;

            _connectPreviewLine = new Line
            {
                X1 = startX, Y1 = startY,
                X2 = startX, Y2 = startY,
                Stroke = new SolidColorBrush(Color.FromArgb(180, 0, 120, 215)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 6, 3 },
                IsHitTestVisible = false
            };
            mapCanvas.Children.Add(_connectPreviewLine);

            connectionModeBanner.Visibility = Visibility.Visible;
            connectionModeText.Text = string.Format(
                _resources.GetString("branchingConnectionModeText") ?? "Click a target passage to connect from \"{0}\"",
                sourceNode.Title ?? sourceNode.Id);
        }

        private void CancelConnection()
        {
            if (_connectPreviewLine != null)
                mapCanvas.Children.Remove(_connectPreviewLine);

            _isConnecting = false;
            _connectSourceNode = null;
            _connectPreviewLine = null;
            connectionModeBanner.Visibility = Visibility.Collapsed;
        }

        private void CompleteConnection(BranchingDialogueNodeData targetNode)
        {
            var source = _connectSourceNode;
            CancelConnection();

            if (source == null || targetNode == null || source.Id == targetNode.Id)
                return;

            ConnectionRequested?.Invoke(source, targetNode);
        }

        #endregion

        #region Canvas Drawing

        private static void EnsurePosition(BranchingDialogueNodeData node)
        {
            if (node.PositionX.HasValue && node.PositionY.HasValue)
                return;

            var seed = Math.Abs((node.Id ?? string.Empty).GetHashCode());
            var col = seed % 6;
            var row = (seed / 6) % 6;
            node.PositionX = 60 + col * 260;
            node.PositionY = 60 + row * 140;
        }

        private void DrawConnections(List<BranchingDialogueNodeData> nodes)
        {
            var byId = nodes.ToDictionary(n => n.Id, n => n);
            foreach (var from in nodes)
            {
                foreach (var choice in from.Choices ?? Enumerable.Empty<BranchingDialogueChoiceData>())
                {
                    if (string.IsNullOrWhiteSpace(choice.TargetNodeId) || !byId.TryGetValue(choice.TargetNodeId, out var target))
                        continue;

                    var startX = (from.PositionX ?? 0) + NodeCardWidth;
                    var startY = (from.PositionY ?? 0) + NodeCardHeight / 2;
                    var endX = (target.PositionX ?? 0);
                    var endY = (target.PositionY ?? 0) + NodeCardHeight / 2;

                    var speakerColor = BranchingDialogueViewModel.GetSpeakerColor(from.Speaker);
                    var connectionColor = Color.FromArgb(200, speakerColor.R, speakerColor.G, speakerColor.B);

                    // Conditional choice indicator — dashed line
                    bool hasConditions = choice.Conditions != null && choice.Conditions.Count > 0;

                    var controlDistance = Math.Max(80, Math.Abs(endX - startX) * 0.4);
                    var pathFigure = new PathFigure { StartPoint = new Point(startX, startY) };
                    pathFigure.Segments.Add(new BezierSegment
                    {
                        Point1 = new Point(startX + controlDistance, startY),
                        Point2 = new Point(endX - controlDistance, endY),
                        Point3 = new Point(endX, endY)
                    });

                    var pathGeometry = new PathGeometry();
                    pathGeometry.Figures.Add(pathFigure);

                    var path = new Path
                    {
                        Data = pathGeometry,
                        Stroke = new SolidColorBrush(connectionColor),
                        StrokeThickness = 2,
                        Opacity = 0.8
                    };

                    if (hasConditions)
                    {
                        path.StrokeDashArray = new DoubleCollection { 4, 2 };
                    }

                    mapCanvas.Children.Add(path);

                    DrawArrowHead(endX, endY, connectionColor);

                    if (!string.IsNullOrWhiteSpace(choice.Text) && choice.Text != "→")
                    {
                        var labelX = (startX + endX) / 2;
                        var labelY = (startY + endY) / 2 - 12;
                        var label = new TextBlock
                        {
                            Text = choice.Text,
                            FontSize = 10,
                            Opacity = 0.7,
                            Foreground = new SolidColorBrush(connectionColor),
                            MaxWidth = 120,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        };
                        Canvas.SetLeft(label, labelX - 30);
                        Canvas.SetTop(label, labelY);
                        mapCanvas.Children.Add(label);
                    }
                }
            }
        }

        private void DrawArrowHead(double tipX, double tipY, Color color)
        {
            var size = 8.0;
            var polygon = new Polygon
            {
                Points = {
                    new Point(tipX, tipY),
                    new Point(tipX - size, tipY - size / 2),
                    new Point(tipX - size, tipY + size / 2)
                },
                Fill = new SolidColorBrush(color),
                Opacity = 0.8
            };
            mapCanvas.Children.Add(polygon);
        }

        private Border CreateNodeCard(BranchingDialogueNodeData node,
            System.Collections.ObjectModel.ObservableCollection<Character> characters)
        {
            var isStart = ViewModel?.AllNodeTargets?.FirstOrDefault()?.Id == node.Id;
            var isDeadEnd = node.Choices == null || node.Choices.Count == 0;
            var choiceCount = node.Choices?.Count ?? 0;
            var speakerColor = BranchingDialogueViewModel.GetSpeakerColor(node.Speaker);
            var character = characters != null ? SpeakerResolver.Resolve(node, characters) : null;

            var contentPanel = new StackPanel { Spacing = 2 };

            // Title row with badges
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            titleRow.Children.Add(new TextBlock
            {
                Text = node.Title ?? "",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 130
            });

            if (isStart)
            {
                titleRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(180, 76, 175, 80)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 1, 4, 1),
                    Child = new TextBlock
                    {
                        Text = "★",
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Colors.White)
                    }
                });
            }
            contentPanel.Children.Add(titleRow);

            // Speaker row with character portrait
            if (!string.IsNullOrWhiteSpace(node.Speaker))
            {
                var speakerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };

                // Character portrait (small circle)
                if (character?.Picture?.Image != null)
                {
                    var portrait = new Ellipse
                    {
                        Width = 16,
                        Height = 16,
                        Fill = new ImageBrush { ImageSource = character.Picture.Image, Stretch = Stretch.UniformToFill }
                    };
                    speakerRow.Children.Add(portrait);
                }

                var speakerText = new TextBlock
                {
                    Text = node.Speaker,
                    FontSize = 10,
                    Opacity = 0.8,
                    Foreground = new SolidColorBrush(speakerColor)
                };
                speakerRow.Children.Add(speakerText);

                // Character tooltip
                if (character != null)
                {
                    var tooltipText = character.DetailsLine;
                    if (!string.IsNullOrWhiteSpace(tooltipText))
                        ToolTipService.SetToolTip(speakerRow, tooltipText);
                }

                contentPanel.Children.Add(speakerRow);
            }

            // Text preview
            contentPanel.Children.Add(new TextBlock
            {
                Text = node.Text ?? "",
                FontSize = 10,
                Opacity = 0.6,
                MaxLines = 2,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap
            });

            // Footer: tags + choice count + dead-end
            var footer = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };

            // Tag pills
            if (node.Tags != null && node.Tags.Count > 0)
            {
                foreach (var tag in node.Tags.Take(2))
                {
                    footer.Children.Add(new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(60, 128, 128, 255)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(4, 1, 4, 1),
                        Child = new TextBlock
                        {
                            Text = tag,
                            FontSize = 8,
                            Opacity = 0.7
                        }
                    });
                }
                if (node.Tags.Count > 2)
                {
                    footer.Children.Add(new TextBlock { Text = $"+{node.Tags.Count - 2}", FontSize = 8, Opacity = 0.4 });
                }
            }

            if (choiceCount > 0)
            {
                var choiceLabel = string.Format(_resources.GetString("branchingChoiceCountFormat") ?? "{0} choices", choiceCount);
                footer.Children.Add(new TextBlock
                {
                    Text = choiceLabel,
                    FontSize = 9,
                    Opacity = 0.5
                });
            }
            if (isDeadEnd)
            {
                footer.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(180, 244, 67, 54)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 1, 4, 1),
                    Child = new TextBlock
                    {
                        Text = _resources.GetString("branchingDeadEndBadge") ?? "Dead-end",
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.White)
                    }
                });
            }

            // Actions indicator
            if (node.Actions != null && node.Actions.Count > 0)
            {
                footer.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(180, 33, 150, 243)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 1, 4, 1),
                    Child = new TextBlock
                    {
                        Text = $"⚡{node.Actions.Count}",
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.White)
                    }
                });
            }

            if (footer.Children.Count > 0)
                contentPanel.Children.Add(footer);

            // Card layout: speaker color stripe + content
            var cardContent = new Grid();
            cardContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            cardContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var stripe = new Rectangle
            {
                Fill = new SolidColorBrush(speakerColor),
                RadiusX = 2,
                RadiusY = 2,
                Margin = new Thickness(0, 4, 0, 4)
            };
            Grid.SetColumn(stripe, 0);
            cardContent.Children.Add(stripe);

            var contentBorder = new Border
            {
                Padding = new Thickness(8, 6, 8, 6),
                Child = contentPanel
            };
            Grid.SetColumn(contentBorder, 1);
            cardContent.Children.Add(contentBorder);

            var isSelected = ViewModel?.SelectedNode?.Id == node.Id;
            var selectedBrush = new SolidColorBrush(speakerColor);
            try
            {
                if (Application.Current.Resources["SystemAccentColor"] is Color accent)
                    selectedBrush = new SolidColorBrush(accent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Accent color lookup failed: {ex.Message}");
            }

            var card = new Border
            {
                Width = NodeCardWidth,
                MinHeight = NodeCardHeight,
                CornerRadius = new CornerRadius(8),
                BorderThickness = isSelected ? new Thickness(2) : new Thickness(1),
                BorderBrush = isSelected
                    ? selectedBrush
                    : (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
                Tag = node.Id,
                Child = cardContent
            };

            card.PointerPressed += Card_PointerPressed;
            card.PointerMoved += Card_PointerMoved;
            card.PointerReleased += Card_PointerReleased;
            card.DoubleTapped += Card_DoubleTapped;

            // Wrap card in a Grid so we can overlay an output port
            var wrapper = new Grid();
            wrapper.Children.Add(card);

            // Output port (right edge circle) — click to start connection
            var outputPort = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush(Color.FromArgb(200, speakerColor.R, speakerColor.G, speakerColor.B)),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, -7, 0),
                Tag = node.Id
            };
            ToolTipService.SetToolTip(outputPort,
                _resources.GetString("branchingConnectPortTooltip") ?? "Drag to connect");
            outputPort.PointerPressed += OutputPort_PointerPressed;
            wrapper.Children.Add(outputPort);

            // Input port (left edge circle) — visual indicator
            var inputPort = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Color.FromArgb(120, 128, 128, 128)),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1.5,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(-5, 0, 0, 0),
                IsHitTestVisible = false
            };
            wrapper.Children.Add(inputPort);

            var wrapperBorder = new Border
            {
                Width = NodeCardWidth + 14,
                Child = wrapper,
                Tag = node.Id
            };
            return wrapperBorder;
        }

        #endregion

        #region Card Interactions

        private void OutputPort_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is Ellipse port) || !(port.Tag is string nodeId))
                return;

            var node = ViewModel?.AllNodeTargets?.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                StartConnection(node);
                e.Handled = true;
            }
        }

        private void Card_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is Border card) || !(card.Tag is string nodeId))
                return;

            // If we're in connection mode, complete the connection to this target
            if (_isConnecting)
            {
                var targetNode = ViewModel?.AllNodeTargets?.FirstOrDefault(n => n.Id == nodeId);
                if (targetNode != null)
                    CompleteConnection(targetNode);
                e.Handled = true;
                return;
            }

            _draggingNode = ViewModel?.AllNodeTargets?.FirstOrDefault(n => n.Id == nodeId);
            if (_draggingNode == null)
                return;

            _draggingCard = FindParentWrapper(card);
            var pos = e.GetCurrentPoint(mapCanvas).Position;
            _dragOffset = new Point(pos.X - Canvas.GetLeft(_draggingCard), pos.Y - Canvas.GetTop(_draggingCard));
            _draggingCard.CapturePointer(e.Pointer);

            NodeSelected?.Invoke(_draggingNode);
            e.Handled = true;
        }

        private void Card_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_draggingCard == null || _draggingNode == null)
                return;

            var pos = e.GetCurrentPoint(mapCanvas).Position;
            var left = Math.Max(-7, pos.X - _dragOffset.X);
            var top = Math.Max(0, pos.Y - _dragOffset.Y);

            Canvas.SetLeft(_draggingCard, left);
            Canvas.SetTop(_draggingCard, top);

            _draggingNode.PositionX = left + 7;
            _draggingNode.PositionY = top;

            e.Handled = true;
        }

        private void Card_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_draggingCard != null)
                _draggingCard.ReleasePointerCapture(e.Pointer);

            if (_draggingNode != null && ViewModel?.SelectedChapter != null)
            {
                var service = App.TryGetService<IBranchingDialogueService>();
                service?.NotifyGraphChanged(ViewModel.SelectedChapter.Token);
            }

            _draggingCard = null;
            _draggingNode = null;

            RedrawCanvas();
            e.Handled = true;
        }

        private void Card_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (!(sender is Border card) || !(card.Tag is string nodeId))
                return;

            var node = ViewModel?.AllNodeTargets?.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
                NodeDoubleTapped?.Invoke(node);

            e.Handled = true;
        }

        private static Border FindParentWrapper(FrameworkElement element)
        {
            var parent = element.Parent as FrameworkElement;
            while (parent != null)
            {
                if (parent is Border b && b.Tag is string)
                    return b;
                parent = parent.Parent as FrameworkElement;
            }
            return element as Border;
        }

        #endregion
    }
}
