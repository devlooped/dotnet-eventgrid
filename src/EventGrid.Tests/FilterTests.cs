using System;
using System.Linq;
using Xunit;

namespace Devlooped;

public class FilterTests
{

    [Theory]
    [InlineData("+path=company/*/123/*Exception", "123", "/subscriptions/4490a56e/resourceGroups/company/providers/Microsoft.EventGrid/domains/company/topics/app", "System.Exception")]
    [InlineData("", "", "Topic", "System.Exception")]
    [InlineData("+topic:Some* +topic:*Topic +eventType:*System*", "", "Topic", "mscorlib::System.Exception")]
    [InlineData("+topic:*Topic +eventType:*system*", "", "topic", "mscorlib::System.Exception")]
    [InlineData("+topic:Some* +EventType:*Event", "SomeSubject", "SomeTopic", "SomeEvent")]
    [InlineData("+topic:Some*", "SomeSubject", "SomeTopic", "SomeEvent")]
    [InlineData("+topic:SomeTopic", "SomeSubject", "SomeTopic", "SomeEvent")]
    [InlineData("+eventType:*", "", "FooBar", "AnyEvent")]
    [InlineData("+topic:**/Bar", "Subject", "Topic/Foo/Bar", "Event")]
    [InlineData("+topic:**/Bar/**", "Subject", "Topic/Foo/Bar/Baz/Suffix", "Event")]
    [InlineData("+topic:** +eventType:**", "Subject", "Topic/Foo/Bar/Baz/Suffix", "My.Event")]
    public void Matches(string args, string subject, string topic, string eventType)
    {
        var e = new PathEventGridEvent("", subject, "", eventType, DateTime.UtcNow, "1.0", topic);
        var filter = Filter.Parse(args.Split(' ').ToArray());

        Assert.True(filter.ShouldInclude(e));
    }

    [InlineData("+topic:FooTopic", "", "Topic", "")]
    [InlineData("+eventType:Bar", "", "FooBar", "")]
    [InlineData("+eventType:Bar +eventType:Baz", "", "", "Foo")]
    [InlineData("+topic:*Bar*", "Subject", "Topic/Foo/Bar/Baz", "Event")]
    [InlineData("+topic:**/Bar/*", "Subject", "Topic/Foo/Bar/Baz/Suffix", "Event")]
    [Theory]
    public void NonMatches(string args, string subject, string topic, string eventType)
    {
        var e = new PathEventGridEvent("", subject, "", eventType, DateTime.UtcNow, "1.0", topic);
        var filter = Filter.Parse(args.Split(' ').ToArray());

        Assert.False(filter.ShouldInclude(e));
    }
}
