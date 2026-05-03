using Storylines.Services;
using System.Collections.Generic;
using Xunit;

namespace Storylines.Tests.Services;

public class TelemetryEventPropertyBuilderTests
{
    [Fact]
    public void Build_IgnoresInvalidEntries_AndLetsSpecificPropertiesOverrideBaseline()
    {
        var baseline = new Dictionary<string, string>
        {
            ["event"] = "start",
            ["theme"] = "light",
        };

        var specific = TelemetryEventPropertyBuilder.Create(
            ("theme", "dark"),
            ("mode", "focus"),
            (" ", "ignored"),
            ("empty", " "));

        var properties = TelemetryEventPropertyBuilder.Build(baseline, specific);

        Assert.Equal("start", properties["event"]);
        Assert.Equal("dark", properties["theme"]);
        Assert.Equal("focus", properties["mode"]);
        Assert.False(properties.ContainsKey("empty"));
        Assert.Equal(3, properties.Count);
    }
}