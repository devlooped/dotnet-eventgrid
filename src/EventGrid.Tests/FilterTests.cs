using System;
using System.Linq;
using Microsoft.Azure.EventGrid.Models;
using Xunit;

namespace Devlooped
{
    public class FilterTests
    {
        [InlineData("", "", "Topic", "System.Exception")]
        [InlineData("+topic:Some* +topic:*Topic +event:*System*", "", "Topic", "mscorlib::System.Exception")]
        [InlineData("+topic:Some* +event:*Event", "SomeSubject", "SomeTopic", "SomeEvent")]
        [InlineData("+topic:Some*", "SomeSubject", "SomeTopic", "SomeEvent")]
        [InlineData("+topic:SomeTopic", "SomeSubject", "SomeTopic", "SomeEvent")]
        [InlineData("+eventType:*", "", "FooBar", "AnyEvent")]
        [InlineData("+topic:**/Bar", "Subject", "Topic/Foo/Bar", "Event")]
        [InlineData("+topic:**/Bar/**", "Subject", "Topic/Foo/Bar/Baz/Suffix", "Event")]
        [InlineData("+topic:** +event:**", "Subject", "Topic/Foo/Bar/Baz/Suffix", "My.Event")]
        [Theory]
        public void Matches(string args, string subject, string topic, string eventType)
        {
            var e = new EventGridEvent("", subject, "", eventType, DateTime.UtcNow, "1.0", topic);
            var filter = Filter.Parse(args.Split(' ').ToArray());

            Assert.True(filter.ShouldInclude(e));
        }

        [InlineData("+topic:FooTopic", "", "Topic", "")]
        [InlineData("+eventType:Bar", "", "FooBar", "")]
        [InlineData("+event:Bar +event:Baz", "", "", "Foo")]
        [InlineData("+topic:*Bar*", "Subject", "Topic/Foo/Bar/Baz", "Event")]
        [InlineData("+topic:**/Bar/*", "Subject", "Topic/Foo/Bar/Baz/Suffix", "Event")]
        [Theory]
        public void NonMatches(string args, string subject, string topic, string eventType)
        {
            var e = new EventGridEvent("", subject, "", eventType, DateTime.UtcNow, "1.0", topic);
            var filter = Filter.Parse(args.Split(' ').ToArray());

            Assert.False(filter.ShouldInclude(e));
        }

    }
}
