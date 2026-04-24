using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Storylines.Views.Pages
{
    public sealed partial class BranchingDialoguePage : Page
    {
        private readonly IBranchingDialogueService _service;
        public BranchingDialogueViewModel ViewModel { get; }

        public bool unappliedChanges { get; set; }
        public static BranchingDialoguePage current { get; private set; }

        private readonly Dictionary<string, Border> _nodeCards = new Dictionary<string, Border>();
        private Border _draggingCard;
        private BranchingDialogueNodeData _draggingNode;
        private Point _dragOffset;

        public BranchingDialoguePage()
        {
            InitializeComponent();

            _service = App.GetService<IBranchingDialogueService>();
            ViewModel = App.GetService<BranchingDialogueViewModel>();
            DataContext = ViewModel;

            current = this;
            AppView.current.page = AppView.Pages.BranchingDialogue;

            ViewModel.GraphRefreshed += RedrawMap;
            RedrawMap();
        }

        private void OnBackButton_Click(object sender, RoutedEventArgs e)
        {
            AppView.current.GoBack();
        }

        private void OnMapToggle_Toggled(object sender, RoutedEventArgs e)
        {
            mapScrollViewer.Visibility = ViewModel.IsMapModeEnabled ? Visibility.Visible : Visibility.Collapsed;
            if (ViewModel.IsMapModeEnabled)
                RedrawMap();
        }

        private void RedrawMap()
        {
            if (!ViewModel.IsMapModeEnabled)
                return;

            mapCanvas.Children.Clear();
            _nodeCards.Clear();

            var nodes = ViewModel.AllNodeTargets.ToList();

            foreach (var node in nodes)
            {
                EnsurePosition(node);
            }

            DrawConnections(nodes);

            foreach (var node in nodes)
            {
                var card = CreateNodeCard(node);
                _nodeCards[node.Id] = card;

                Canvas.SetLeft(card, node.PositionX ?? 0);
                Canvas.SetTop(card, node.PositionY ?? 0);
                mapCanvas.Children.Add(card);
            }
        }

        private static void EnsurePosition(BranchingDialogueNodeData node)
        {
            if (node.PositionX.HasValue && node.PositionY.HasValue)
                return;

            var seed = Math.Abs((node.Id ?? string.Empty).GetHashCode());
            var col = seed % 6;
            var row = (seed / 6) % 6;

            node.PositionX = 40 + col * 250;
            node.PositionY = 40 + row * 130;
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

                    var line = new Line
                    {
                        X1 = (from.PositionX ?? 0) + 90,
                        Y1 = (from.PositionY ?? 0) + 35,
                        X2 = (target.PositionX ?? 0) + 90,
                        Y2 = (target.PositionY ?? 0) + 35,
                        Stroke = new SolidColorBrush(Color.FromArgb(180, 90, 140, 220)),
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 3 }
                    };

                    mapCanvas.Children.Add(line);
                }
            }
        }

        private Border CreateNodeCard(BranchingDialogueNodeData node)
        {
            var card = new Border
            {
                Width = 180,
                Height = 70,
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
                Tag = node.Id,
                Child = new StackPanel
                {
                    Margin = new Thickness(8, 6, 8, 6),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = node.Title,
                            FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = node.Speaker,
                            Opacity = 0.7,
                            FontSize = 11,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    }
                }
            };

            card.PointerPressed += Card_PointerPressed;
            card.PointerMoved += Card_PointerMoved;
            card.PointerReleased += Card_PointerReleased;

            return card;
        }

        private void Card_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is Border card) || !(card.Tag is string nodeId))
                return;

            _draggingNode = ViewModel.AllNodeTargets.FirstOrDefault(n => n.Id == nodeId);
            if (_draggingNode == null)
                return;

            _draggingCard = card;
            var pos = e.GetCurrentPoint(mapCanvas).Position;
            _dragOffset = new Point(pos.X - Canvas.GetLeft(card), pos.Y - Canvas.GetTop(card));
            card.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void Card_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_draggingCard == null || _draggingNode == null)
                return;

            var pos = e.GetCurrentPoint(mapCanvas).Position;
            var left = Math.Max(0, pos.X - _dragOffset.X);
            var top = Math.Max(0, pos.Y - _dragOffset.Y);

            Canvas.SetLeft(_draggingCard, left);
            Canvas.SetTop(_draggingCard, top);

            _draggingNode.PositionX = left;
            _draggingNode.PositionY = top;

            e.Handled = true;
        }

        private void Card_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_draggingCard != null)
                _draggingCard.ReleasePointerCapture(e.Pointer);

            if (_draggingNode != null && ViewModel.SelectedChapter != null)
                _service.NotifyGraphChanged(ViewModel.SelectedChapter.Token);

            _draggingCard = null;
            _draggingNode = null;

            RedrawMap();
            e.Handled = true;
        }
    }
}
