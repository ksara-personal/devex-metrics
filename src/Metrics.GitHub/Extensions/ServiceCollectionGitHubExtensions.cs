using Metrics.DataMigrations;
using Metrics.Models;
using Metrics.GitHub.DataMigrations;
using Metrics.MultiTenant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Metrics.GitHub.Extensions;

static class ServiceCollectionGitHubExtensions
{
    /// <summary>
    /// Adds all the metric providers for GitHub to the service collection.
    /// This method registers various metric providers that analyze GitHub pull requests (PRs) and
    /// their associated data. Each provider is responsible for calculating specific metrics related to PRs,
    /// such as approval time, review comments, contributor activity, and more.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    static IServiceCollection AddGitHubMetricProviders(this IServiceCollection services)
    {
        return services.AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRApproveTimeMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRCopilotReviewMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRContributorMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRChangesRequestedMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRCodingTimeMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRCommitMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRCycleTimeMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRLeadTimeMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRMaturityMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRMergeTimeMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRPickupTimeMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRReviewCommentsAfterApprovalMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRReviewTimeMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRReviewerMetricProvider>()
            .AddScoped<MetricProvider<GitHubPRRoot>, Metrics.GitHub.Providers.PRSizeMetricProvider>();
    }

    /// <summary>
    /// Registers github specific services.
    /// Settings are resolved per-tenant at runtime via TenantConfigurationService.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddGitHubServices(this IServiceCollection services) =>
            services
                .AddKeyedScoped<DataSynchronizer, GitHubPRMetricsSynchronizer>(typeof(DevExMetricDbContext))
                .AddScoped<GitHubPRMetricsSynchronizer>() // does reuse the same instance
                .AddKeyedScoped<DataSynchronizer, GitHubReviewerMetricsSynchronizer>(typeof(DevExMetricDbContext))
                .AddScoped<GitHubReviewerMetricsSynchronizer>() // does reuse the same instance
                .AddScoped<MultiOrgUserService<GitHubOrganization>>()
                .AddScoped<DataSyncService<DevExMetricDbContext>>()
                .AddScoped<OrgUserService<GitHubOrganization>, GitHubOrgUserService>()
                .AddScoped<PRAnalyzer<GitHubPRRoot>, GitHubPRAnalyzer>()
                .AddScoped<UserMembershipService<GitHubOrganization>, DefaultUserMembershipService<GitHubOrganization>>()
                .AddTransient<OrgUserService<GitHubOrganization>, GitHubOrgUserService>()
                .AddScoped<IMetricsScheduler, GitHubMetricsScheduler>()
                .AddGitHubMetricProviders()
                .RegisterHttpClient<GitHubApiClient>(sp =>
                {
                    var tenantConfig = sp.GetRequiredService<TenantConfigurationService>();
                    var tenantSettings = tenantConfig.GetSettings<SourceControlSettings<GitHubOrganization>>(GitHubServiceConfigurator.GitHubSectionName)
                        ?? throw new InvalidOperationException("GitHub settings not configured for tenant");

                    if (string.IsNullOrEmpty(tenantSettings.PAT))
                    throw new ArgumentNullException(nameof(tenantSettings.PAT), $"PAT not found for {nameof(GitHubServiceConfigurator.GitHubSectionName)}");


                    return (tenantSettings.PAT, GitHubConstants.GitHubGraphQLEndpoint);
                })
                .AddMigrations();

    /// <summary>
    /// Adds migration services.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    static IServiceCollection AddMigrations(this IServiceCollection services)
    {
        var contextType = typeof(DevExMetricDbContext);
        return services
            .AddMigrationRunners<DevExMetricDbContext>()
            // register migrations.
            .AddKeyedTransient<IDataMigration<DevExMetricDbContext>, WorkItemMigration>(contextType)
            .AddKeyedTransient<IDataMigration<DevExMetricDbContext>, WorkItemIdFallbackMigration>(contextType)
            .AddKeyedTransient<IDataMigration<DevExMetricDbContext>, DevExMetricsMigration>(contextType)
            .AddKeyedTransient<IDataMigration<DevExMetricDbContext>, CodingAgentTeamMigration>(contextType)
            .AddKeyedTransient<IDataMigration<DevExMetricDbContext>, ReviewerMetricsMigration>(contextType)
            .AddKeyedTransient<IDataMigration<DevExMetricDbContext>, ChangesRequestedMetricsMigration>(contextType);
    }
}
