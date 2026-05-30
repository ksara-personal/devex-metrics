using Metrics.Extensions;
using Metrics.Models;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Metrics.MCP.StreamableHTTP;

static class ODataEdmBuilderUtils
{
    /// <summary>
    /// Builds the Entity Data Model (EDM) that defines the structure and relationships 
    /// of entities exposed through OData endpoints.
    /// DevExMetrics collection is excluded and DevExEffciencyMetrics dictionary properties are flattened.
    /// </summary>
    /// <returns>An EDM model containing all configured entity sets and their metadata</returns>
    public static IEdmModel GetEdmModel(IServiceCollection services)
    {
        var builder = new ODataConventionModelBuilder();

        var prMetricsEntitySet = builder.EntitySet<PRMetrics>("PRMetrics");
        prMetricsEntitySet.EntityType
            .HasKey(p => p.Id);

        var prMetricsExEntitySet = builder.EntitySet<PRMetricsEx>("PRMetricsEx");
        var prMetricsExType = prMetricsExEntitySet.EntityType;
        prMetricsExType
            .HasKey(p => p.Id);

        prMetricsExType.HasRequired(p => p.Team);
        prMetricsExType.HasMany(p => p.Contributors);
        prMetricsExType.HasMany(p => p.ReviewerMetrics);
        prMetricsExType.HasOptional(p => p.CopilotReviewMetrics);

        builder.EntitySet<Sprint>("Sprints").EntityType.HasKey(t => t.Id);
        builder.EntitySet<Team>("Teams").EntityType.HasKey(t => t.Id).HasMany(t => t.Metrics);
        builder.EntitySet<PRContributor>("Contributors").EntityType.HasKey(c => c.Id).HasRequired(c => c.PRMetric);
        builder.EntitySet<ReviewerSprintMetrics>("ReviewerSprintMetrics").EntityType.HasKey(r => r.Id);
        builder.EntitySet<ReviewerMonthlyMetrics>("ReviewerMonthlyMetrics").EntityType.HasKey(r => r.Id);
        builder.EntitySet<CopilotReviewMetrics>("CopilotReviewMetrics").EntityType.HasKey(c => c.Id).HasRequired(c => c.PRMetric);
        builder.EntitySet<PRReviewerMetrics>("PRReviewerMetrics").EntityType.HasKey(c => c.Id).HasRequired(c => c.PRMetric);

        // Register OData functions for AuthorMetrics
        builder.ComplexType<AuthorMetrics>();

        var getByAuthorSprintFunc = builder.Function("GetByAuthorSprint").Returns<AuthorMetrics>();
        getByAuthorSprintFunc.Parameter<string>("Author");
        getByAuthorSprintFunc.Parameter<int>("Year");
        getByAuthorSprintFunc.Parameter<int>("SprintNumber");

        var getByAuthorMonthFunc = builder.Function("GetByAuthorMonth").Returns<AuthorMetrics>();
        getByAuthorMonthFunc.Parameter<string>("author");
        getByAuthorMonthFunc.Parameter<int>("year");
        getByAuthorMonthFunc.Parameter<int>("month");

        var getByTeamSprintFunc = builder.Function("GetByTeamSprint").ReturnsCollection<AuthorMetrics>();
        getByTeamSprintFunc.Parameter<string>("team");
        getByTeamSprintFunc.Parameter<int>("year");
        getByTeamSprintFunc.Parameter<int>("sprintNumber");

        var getByTeamMonthFunc = builder.Function("GetByTeamMonth").ReturnsCollection<AuthorMetrics>();
        getByTeamMonthFunc.Parameter<string>("team");
        getByTeamMonthFunc.Parameter<int>("year");
        getByTeamMonthFunc.Parameter<int>("month");

        var extensions = services.BuildServiceProvider().GetServices<IMetricsExtensionProvider>();
        extensions.RegisterODataModel(builder);

        return builder.GetEdmModel();
    }
}
