using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Storylines.Services
{
    internal sealed class DeveloperDiagnosticsService : IDeveloperDiagnosticsService
    {
        private const int MaxReportedItems = 120;

        private readonly WindowContext _windowContext;
        private readonly IDialogService _dialogService;
        private readonly ILogger _logger;

        public DeveloperDiagnosticsService(WindowContext windowContext, IDialogService dialogService, ILogger logger)
        {
            _windowContext = windowContext;
            _dialogService = dialogService;
            _logger = logger;
        }

        public DeveloperDiagnosticsSnapshot CaptureSnapshot()
        {
            var invisibleControls = new List<DeveloperDiagnosticItem>();
            var attachedBehaviors = new List<DeveloperDiagnosticItem>();
            var visited = new HashSet<DependencyObject>();
            int invisibleControlCount = 0;
            int attachedBehaviorCount = 0;

            CollectDiagnostics(
                _windowContext.RootElement ?? _windowContext.AppView,
                invisibleControls,
                attachedBehaviors,
                visited,
                ref invisibleControlCount,
                ref attachedBehaviorCount);

            if (_dialogService.CurrentDialog is not null)
            {
                CollectDiagnostics(
                    _dialogService.CurrentDialog,
                    invisibleControls,
                    attachedBehaviors,
                    visited,
                    ref invisibleControlCount,
                    ref attachedBehaviorCount);
            }

            return new DeveloperDiagnosticsSnapshot
            {
                CurrentPage = _windowContext.AppView?.page.ToString() ?? string.Empty,
                CurrentTheme = (_windowContext.AppView?.ActualTheme ?? ElementTheme.Default).ToString(),
                WindowSize = GetWindowSize(),
                CurrentDialog = _dialogService.CurrentDialog?.GetType().Name ?? "None",
                InvisibleControlCount = invisibleControlCount,
                InvisibleControls = invisibleControls.ToArray(),
                AttachedBehaviorCount = attachedBehaviorCount,
                AttachedBehaviors = attachedBehaviors.ToArray(),
            };
        }

        public IReadOnlyList<string> GetRecentLogEntries()
        {
            if (_logger is DebugLogger debugLogger)
                return debugLogger.GetRecentEntries().ToArray();

            return Array.Empty<string>();
        }

        private static void CollectDiagnostics(
            DependencyObject root,
            List<DeveloperDiagnosticItem> invisibleControls,
            List<DeveloperDiagnosticItem> attachedBehaviors,
            HashSet<DependencyObject> visited,
            ref int invisibleControlCount,
            ref int attachedBehaviorCount)
        {
            if (root is null || !visited.Add(root))
                return;

            if (root is FrameworkElement frameworkElement &&
                frameworkElement.Visibility != Visibility.Visible &&
                ShouldReportInvisibleElement(frameworkElement))
            {
                invisibleControlCount++;
                if (invisibleControls.Count < MaxReportedItems)
                {
                    invisibleControls.Add(new DeveloperDiagnosticItem
                    {
                        Title = GetElementLabel(frameworkElement),
                        Detail = $"{frameworkElement.GetType().Name} • {frameworkElement.Visibility} • Parent: {GetParentLabel(frameworkElement)}",
                    });
                }
            }

            var behaviors = Interaction.GetBehaviors(root);
            if (behaviors?.Count > 0)
            {
                string elementLabel = root is FrameworkElement behaviorElement
                    ? GetElementLabel(behaviorElement)
                    : root.GetType().Name;

                foreach (var behavior in behaviors)
                {
                    attachedBehaviorCount++;
                    if (attachedBehaviors.Count >= MaxReportedItems)
                        continue;

                    attachedBehaviors.Add(new DeveloperDiagnosticItem
                    {
                        Title = $"{behavior.GetType().Name} on {elementLabel}",
                        Detail = $"{root.GetType().Name} • Parent: {GetParentLabel(root)}",
                    });
                }
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                CollectDiagnostics(
                    VisualTreeHelper.GetChild(root, i),
                    invisibleControls,
                    attachedBehaviors,
                    visited,
                    ref invisibleControlCount,
                    ref attachedBehaviorCount);
            }
        }

        private string GetWindowSize()
        {
            var root = _windowContext.RootElement as FrameworkElement ?? _windowContext.AppView;
            if (root is not null && root.ActualWidth > 0 && root.ActualHeight > 0)
                return $"{Math.Round(root.ActualWidth)} x {Math.Round(root.ActualHeight)}";

            return string.Empty;
        }

        private static bool ShouldReportInvisibleElement(FrameworkElement element)
        {
            string elementNamespace = element.GetType().Namespace ?? string.Empty;
            return !string.IsNullOrWhiteSpace(element.Name)
                || !string.IsNullOrWhiteSpace(AutomationProperties.GetName(element))
                || elementNamespace.StartsWith("Storylines", StringComparison.Ordinal);
        }

        private static string GetElementLabel(FrameworkElement element)
        {
            if (!string.IsNullOrWhiteSpace(element.Name))
                return element.Name;

            string automationName = AutomationProperties.GetName(element);
            if (!string.IsNullOrWhiteSpace(automationName))
                return automationName;

            return element.GetType().Name;
        }

        private static string GetParentLabel(DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            return parent switch
            {
                FrameworkElement frameworkElement => GetElementLabel(frameworkElement),
                null => "Root",
                _ => parent.GetType().Name,
            };
        }
    }
}
