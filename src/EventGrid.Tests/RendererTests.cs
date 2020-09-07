using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Devlooped
{
    public class RendererTests
    {
        [Fact]
        public void IgnoreProperties()
        {
            var e = new PathEventGridEvent(Guid.NewGuid().ToString(), "Subject", "{ \"Value\": 42 }", "EventType", DateTime.UtcNow, "1.0", "Topic");

            var renderer = Renderer.Parse("-id", "-eventtime", "-DataVersion");

            var json = renderer.Render(e);

            Assert.False(json.Contains("Id", StringComparison.OrdinalIgnoreCase));
            Assert.False(json.Contains("EventTime", StringComparison.OrdinalIgnoreCase));
            Assert.False(json.Contains("DataVersion", StringComparison.OrdinalIgnoreCase));
        }

        [MemberData(nameof(DefaultExcludedProperties))]
        [Theory]
        public void DefaultIgnoredProperty(string property)
        {
            var e = new PathEventGridEvent(Guid.NewGuid().ToString(), "UserId", "{ \"Value\": 42 }", "LoginEvent", DateTime.UtcNow, "1.0", "ShoppingCart", "1.0");

            var renderer = Renderer.Parse();

            var json = renderer.Render(e);

            Assert.False(json.Contains(property, StringComparison.OrdinalIgnoreCase));
        }

        [MemberData(nameof(DefaultExcludedProperties))]
        [Theory]
        public void IncludeAllIgnoredProperty(string property)
        {
            var e = new PathEventGridEvent(Guid.NewGuid().ToString(), "UserId", "{ \"Value\": 42 }", "LoginEvent", DateTime.UtcNow, "1.0", "ShoppingCart", "1.0");

            var renderer = Renderer.Parse("+all");

            var json = renderer.Render(e);

            Assert.True(json.Contains(property, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<object[]> DefaultExcludedProperties
            => Renderer.DefaultExcluded.Select(name => new object[] { name });
    }
}
