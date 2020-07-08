using System;
using Microsoft.Azure.EventGrid.Models;
using Xunit;

namespace Devlooped
{
    public class RendererTests
    {
        [Fact]
        public void IgnoreProperties()
        {
            var e = new EventGridEvent(Guid.NewGuid().ToString(), "Subject", "{ \"Value\": 42 }", "EventType", DateTime.UtcNow, "1.0", "Topic");

            var renderer = Renderer.Parse("-id", "-eventtime", "-DataVersion");

            var json = renderer.Render(e);

            Assert.False(json.Contains("Id", StringComparison.Ordinal));
            Assert.False(json.Contains("EventTime", StringComparison.Ordinal));
            Assert.False(json.Contains("DataVersion", StringComparison.Ordinal));
        }
    }
}
