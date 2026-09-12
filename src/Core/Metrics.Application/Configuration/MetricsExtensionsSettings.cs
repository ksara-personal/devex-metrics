using System;

namespace Metrics.Application;

/// <summary>
/// Represents the settings for metrics extensions.
/// </summary>
/// <value></value>
public sealed record MetricsExtensionsSettings
{
    /// <summary>
    /// Gets or sets the list of metrics extensions.
    /// </summary>
    /// <value></value>
    public IReadOnlyList<MetricsExtension> Extensions { get; set; }
}
