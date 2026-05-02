using Storylines.ViewModels;
using Xunit;

namespace Storylines.Tests.ViewModels;

public class InspectorViewModelTests
{
    [Fact]
    public void InitialTarget_IsNull()
    {
        var vm = new InspectorViewModel();
        Assert.Null(vm.Target);
    }

    [Fact]
    public void Inspect_SetsTarget()
    {
        var vm = new InspectorViewModel();
        var payload = new object();

        vm.Inspect(payload);

        Assert.Same(payload, vm.Target);
    }

    [Fact]
    public void Inspect_RaisesTargetChangedEvent()
    {
        var vm = new InspectorViewModel();
        object received = null;
        vm.TargetChanged += t => received = t;

        var payload = new object();
        vm.Inspect(payload);

        Assert.Same(payload, received);
    }

    [Fact]
    public void Inspect_Null_ClearsTarget()
    {
        var vm = new InspectorViewModel();
        vm.Inspect(new object());

        vm.Inspect(null);

        Assert.Null(vm.Target);
    }
}
