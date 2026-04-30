using System;

namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Drives the right-side contextual inspector panel. Whatever the user has currently
    /// selected — a chapter, character, pinboard card or dialogue node — is set here and the
    /// panel binds the matching DataTemplate. Implementations should accept any object so the
    /// inspector remains pluggable across features.
    /// </summary>
    public interface IInspectorViewModel
    {
        /// <summary>
        /// The currently inspected target. <c>null</c> when nothing is selected.
        /// </summary>
        object Target { get; }

        /// <summary>
        /// Raised whenever <see cref="Target"/> changes so the view can swap templates.
        /// </summary>
        event Action<object> TargetChanged;

        /// <summary>
        /// Set the inspected target. Pass <c>null</c> to clear the panel.
        /// </summary>
        void Inspect(object target);
    }
}
