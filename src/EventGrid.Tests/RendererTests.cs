using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Devlooped;

public class RendererTests
{
    [Fact]
    public async Task IgnorePropertiesAsync()
    {
        var e = new PathEventGridEvent(Guid.NewGuid().ToString(), "Subject", "{ \"Value\": 42 }", "EventType", DateTime.UtcNow, "1.0", "Topic");

        var renderer = Renderer.Parse("-id", "-eventtime", "-DataVersion");

        var json = await renderer.RenderAsync(e);

        Assert.False(json.Contains("Id", StringComparison.OrdinalIgnoreCase));
        Assert.False(json.Contains("EventTime", StringComparison.OrdinalIgnoreCase));
        Assert.False(json.Contains("DataVersion", StringComparison.OrdinalIgnoreCase));
    }

    [MemberData(nameof(DefaultExcludedProperties))]
    [Theory]
    public async Task DefaultIgnoredPropertyAsync(string property)
    {
        var e = new PathEventGridEvent(Guid.NewGuid().ToString(), "UserId", "{ \"Value\": 42 }", "LoginEvent", DateTime.UtcNow, "1.0", "ShoppingCart", "1.0");

        var renderer = Renderer.Parse();

        var json = await renderer.RenderAsync(e);

        Assert.False(json.Contains(property, StringComparison.OrdinalIgnoreCase));
    }

    [MemberData(nameof(DefaultExcludedProperties))]
    [Theory]
    public async Task IncludeAllIgnoredPropertyAsync(string property)
    {
        var e = new PathEventGridEvent(Guid.NewGuid().ToString(), "UserId", "{ \"Value\": 42 }", "LoginEvent", DateTime.UtcNow, "1.0", "ShoppingCart", "1.0");

        var renderer = Renderer.Parse("+all");

        var json = await renderer.RenderAsync(e);

        Assert.True(json.Contains(property, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<object[]> DefaultExcludedProperties
        => Renderer.DefaultExcluded.Select(name => new object[] { name });
}
