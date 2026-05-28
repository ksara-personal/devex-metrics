using Metrics.ADO.Models;

namespace Metrics.ADO;

/// <summary>
/// ADO Extension OData Configuration Support
/// </summary>
sealed class ADOODataConfigurator : IODataConfigurator
{
    /// <summary>
    /// Configures the OData model for the ADO extension.
    /// </summary>
    /// <param name="odataBuilder"></param>
    public void ConfigureODataModel(Microsoft.OData.ModelBuilder.ODataModelBuilder odataBuilder)
    {
        var workItemMetricsEntitySet = odataBuilder.EntitySet<WorkItemMetrics>("WorkItemMetrics");
        workItemMetricsEntitySet.EntityType.HasKey(w => w.WorkItemId);
    }
}
