using Metrics.GitHub;

namespace Metrics.Tests;

public sealed class WorkItemIdExtractorTests
{
    static readonly WorkItemIdPattern[] _patterns =
    [
        WorkItemIdExtractor.DefaultPattern,
        new() { Pattern = @"\bPROD[-\s](?<WorkItemId>\d+)", Format = "PROD-${WorkItemId}" }
    ];

    [Theory]
    [InlineData("PROD-148932 - Configuration Cache Miss issue Fix- #3999", new[] { "PROD-148932" })]
    [InlineData("Prod 135856 ace unsupported field enablement", new[] { "PROD-135856" })]
    [InlineData("Prod pentest 2026 fixes | PROD-146427 | PROD-146439", new[] { "PROD-146427", "PROD-146439" })]
    [InlineData("PROD-2 then AB#1", new[] { "PROD-2", "AB#1" })]
    [InlineData("AB#5 and again ab#5", new[] { "AB#5" })]
    [InlineData("no ids here #3999", new string[0])]
    public void Extract_ReturnsFormattedIdsInOrder(string text, string[] expected)
    {
        var extractor = new WorkItemIdExtractor(_patterns);
        Assert.Equal(expected, extractor.Extract(text));
    }

    [Fact]
    public void Extract_DefaultsToAzureBoards()
    {
        var extractor = new WorkItemIdExtractor(null);
        Assert.Equal(["AB#123"], extractor.Extract("Fixes ab#123, PROD-9"));
    }

    [Fact]
    public void Extract_WithoutFormat_ReturnsGroupValue()
    {
        var extractor = new WorkItemIdExtractor([new() { Pattern = @"PROD-(?<WorkItemId>\d+)" }]);
        Assert.Equal(["42"], extractor.Extract("PROD-42"));
    }

    [Fact]
    public void Ctor_ThrowsWhenGroupMissing() =>
        Assert.Throws<ArgumentException>(() => new WorkItemIdExtractor([new() { Pattern = @"PROD-\d+" }]));
}
