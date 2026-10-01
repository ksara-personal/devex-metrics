using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Metrics.Application;
public abstract record PullRequestData();
public abstract record PRSearchData();
public abstract record TeamMembersData();

/// <summary>
/// abstract class PRAnalyzer. This class cannot be instantiated.
/// </summary>
/// <typeparam name="TSource"></typeparam>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public abstract class PRAnalyzer<TSource> where TSource : PullRequestData
{
    protected IServiceProvider _serviceProvider;
    protected ILogger _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="logger"></param>
    public PRAnalyzer(IServiceProvider serviceProvider, ILogger<PRAnalyzer<TSource>> logger) =>
        (this._serviceProvider, _logger) = (serviceProvider, logger);

    /// <summary>
    /// Sanitizes the PR commits based on the state transition.
    /// </summary>
    /// <param name="source"></param>
    protected abstract TSource Sanitize(TSource source);

    /// <summary>
    /// Gets the total draft workflow transitions.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    protected abstract int GetTotalDraftWorkflowTransitions(TSource source);

    /// <summary>
    /// Creates the metrics object.
    /// </summary>
    /// <param name="prRoot"></param>
    /// <returns></returns>
    protected abstract PRMetrics CreateMetrics(TSource prRoot);

    /// <summary>
    /// Calculates the PR metrics for a PR.
    /// </summary>
    /// <param name="prRoot"></param>
    public PRMetrics CalcPRMetrics(TSource prRoot)
    {
        prRoot = Sanitize(prRoot);
        var metrics = CreateMetrics(prRoot);

        metrics.DraftTransitions = GetTotalDraftWorkflowTransitions(prRoot);

        var metricProviders = _serviceProvider.GetServices<MetricProvider<TSource>>();
        Parallel.ForEach(metricProviders, metricProvider => metricProvider.CalculateAndAddMetric(prRoot, metrics));
        return metrics;
    }
}
