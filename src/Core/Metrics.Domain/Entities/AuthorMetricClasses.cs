namespace Metrics.Domain;

/// <summary>
/// Author level metrics
/// </summary>
public sealed record AuthorMetrics
{
    public string Author { get; set; }
    public string Team { get; set; }
    public int PrsAuthored { get; set; }
    public int PrsClosed { get; set; }
    public float AvgPrSize { get; set; }
    public float AvgCommentCount { get; set; }
    public float AvgCycleTime { get; set; }
    public float AvgReviewComments { get; set; }
    public float AvgPRMaturity { get; set; }

}
