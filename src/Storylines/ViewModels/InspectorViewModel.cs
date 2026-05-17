
namespace Storylines.ViewModels;

/// <summary>
/// Default <see cref="IInspectorViewModel"/> implementation. Stores the inspected target
/// and raises <see cref="IInspectorViewModel.TargetChanged"/> on every transition; views
/// pick the right DataTemplate from a TemplateSelector keyed off <c>Target.GetType()</c>.
/// </summary>
public partial class InspectorViewModel : ObservableObject, IInspectorViewModel
{
    [ObservableProperty]
    private object _target;

    public event Action<object> TargetChanged;

    public void Inspect(object target)
    {
        Target = target;
        TargetChanged?.Invoke(target);
    }
}
