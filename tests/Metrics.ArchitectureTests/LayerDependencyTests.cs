using System.Reflection;

namespace Metrics.ArchitectureTests;

/// <summary>
/// Guards the clean-architecture dependency rule: references point inwards only.
/// </summary>
/// <remarks>
/// The checks read each layer's assembly references, so a project file that adds a
/// forbidden package or project reference fails here rather than at review time.
/// </remarks>
public class LayerDependencyTests
{
    static readonly Assembly Domain = typeof(Metrics.Domain.PRMetrics).Assembly;
    static readonly Assembly Application = typeof(Metrics.Application.IMetricsPersistenceService).Assembly;
    static readonly Assembly Infrastructure = typeof(Metrics.Infrastructure.DevExMetricDbContext).Assembly;

    /// <summary>Assembly name prefixes that may never appear inside the two inner layers.</summary>
    static readonly string[] InfrastructureOnlyPrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
        "SQLitePCLRaw",
        "EFCore.NamingConventions",
        "ChoETL",
        "Cronos",
        "Nito.AsyncEx",
        "Polly",
    ];

    static string[] ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();

    [Fact]
    public void Domain_references_no_other_layer()
    {
        var referenced = ReferencedNames(Domain);

        Assert.DoesNotContain("Metrics.Application", referenced);
        Assert.DoesNotContain("Metrics.Infrastructure", referenced);
        Assert.DoesNotContain("Metrics.GitHub", referenced);
        Assert.DoesNotContain("Metrics.ADO", referenced);
    }

    [Fact]
    public void Domain_references_no_infrastructure_package()
    {
        var offenders = ReferencedNames(Domain)
            .Where(name => InfrastructureOnlyPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Application_references_only_the_domain()
    {
        var referenced = ReferencedNames(Application);

        Assert.Contains("Metrics.Domain", referenced);
        Assert.DoesNotContain("Metrics.Infrastructure", referenced);
        Assert.DoesNotContain("Metrics.GitHub", referenced);
        Assert.DoesNotContain("Metrics.ADO", referenced);
    }

    [Fact]
    public void Application_references_no_infrastructure_package()
    {
        var offenders = ReferencedNames(Application)
            .Where(name => InfrastructureOnlyPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Infrastructure_does_not_reference_a_provider_adapter()
    {
        // Adapters plug into the shared infrastructure, never the other way round.
        var referenced = ReferencedNames(Infrastructure);

        Assert.DoesNotContain("Metrics.GitHub", referenced);
        Assert.DoesNotContain("Metrics.ADO", referenced);
        Assert.DoesNotContain("Metrics.MCP.StreamableHTTP", referenced);
    }

    [Theory]
    [InlineData("Metrics.Domain")]
    [InlineData("Metrics.Application")]
    [InlineData("Metrics.Infrastructure")]
    public void Layer_types_live_in_the_matching_namespace(string layer)
    {
        var assembly = layer switch
        {
            "Metrics.Domain" => Domain,
            "Metrics.Application" => Application,
            _ => Infrastructure,
        };

        var strays = assembly.GetExportedTypes()
            .Where(t => t.Namespace is not null)
            .Where(t => !t.Namespace!.StartsWith(layer, StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .ToArray();

        Assert.Empty(strays);
    }
}
