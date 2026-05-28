using System.Diagnostics;
using System.Text.Json;
using Metrics.EF;
using Metrics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Metrics.Store;

/// <summary>
/// FilePersistenceService is a service that persists metrics in memory and writes them to a file.
/// It uses an in-memory database context to store metrics and provides methods to create pull requests with metrics,
/// retrieve the last run date and time, and update the last run status.
/// This service is designed to work with a file-based persistence mechanism, allowing for quick access to
/// metrics data while also ensuring that the data is stored persistently in a file.
/// It uses an asynchronous reader-writer lock to ensure thread-safe access to the file when reading
/// and writing metrics data.
/// The service initializes by reading existing metrics from the file and storing them in the in-memory database.
/// It also provides methods to ensure the database is created, create pull requests with metrics,
/// get the last run date and time, and update the last run status.
/// The last run status is stored in a separate JSON file, allowing for tracking of the last run date and time,
/// as well as whether the last run was successful or encountered an error.
/// </summary>
public sealed class FilePersistenceService : MetricsPersistenceService<DevExMetricDbContext>
{
    readonly IConfiguration _configuration;
    readonly string _filePath, _lastRunStatusFilePath;
    readonly Task _initTask;
    readonly AsyncReaderWriterLock _rwLock = new AsyncReaderWriterLock();

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePersistenceService"/> class.
    /// This constructor initializes the service with the provided database context factory, logger, and configuration.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="configuration"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public FilePersistenceService(IDbContextFactory<DevExMetricDbContext> context,
        ILogger<DevExMetricDbContext> logger,
        SprintCalendar sprintCalendar,
        IConfiguration configuration)
        : base(context, logger, sprintCalendar)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        var filePath = _configuration.GetConnectionString("File");
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("Connection string 'File' is not configured");

        _filePath = filePath;
        _lastRunStatusFilePath = Path.Combine(Path.GetDirectoryName(filePath), "LastRunStatus.json");
        _initTask = Task.Run(async () => await LoadToInMemoryDbAsync());
    }

    /// <summary>
    /// Loads metrics from the file to the in-memory database.
    /// This method reads metrics from a JSON file and loads them into the in-memory database.
    /// It uses a stopwatch to measure the time taken for the operation and logs the elapsed time
    /// and the total number of records loaded.
    /// The method ensures that the metrics are read asynchronously and stored in a hash set to avoid
    /// duplicates. It then calls the internal method to create pull requests with the metrics,
    /// which persists the metrics in the in-memory database.
    /// </summary>
    /// <returns></returns>
    async Task LoadToInMemoryDbAsync()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var metrics = new HashSet<PRMetrics>();
        await _filePath.ReadFromFileAsync(metrics);

        int totalRecords = 0;
        if (metrics is not null && metrics.Any())
        {
            totalRecords = await InternalCreatePRWithMetricsAsync(metrics, true);
        }

        stopwatch.Stop();
        _logger.LogInformation("Initialized in {ElapsedMilliseconds} ms for total records {TotalRecords}", stopwatch.ElapsedMilliseconds, totalRecords);
    }

    /// <summary>
    /// Ensures that the database is created.
    /// This method is called to ensure that the database schema is created before performing any operations.
    /// </summary>
    /// <param name="context"></param>
    protected override async Task EnsureCreatedAsync(DevExMetricDbContext context, bool initialLoad = false)
    {
        await base.EnsureCreatedAsync(context);
        if (!initialLoad && !_initTask.IsCompleted)
        {
            _initTask.Wait();
        }
    }

    /// <summary>
    /// Creates a pull request with metrics and writes to the file.
    /// This method is called to persist the metrics in memory and also write them to a file
    /// </summary>
    /// <param name="metrics"></param>
    /// <returns></returns>
    protected override async Task<int> CreatePRWithMetricsAsync(IEnumerable<PRMetrics> metrics)
    {
        var recordsAffected = await base.CreatePRWithMetricsAsync(metrics);
        using (await _rwLock.WriterLockAsync())
        {
            await metrics.WriteOrAppendToJsonFile(_filePath);
        }
        return recordsAffected;
    }

    /// <summary>
    /// Gets the last run date and time from the file.
    /// This method reads the last run status from a JSON file and returns the last successful run
    /// </summary>
    /// <returns></returns>
    public override async Task<(DateTime?,int?)> GetLastRunAt(string repoWithOwner)
    {
        if (File.Exists(_lastRunStatusFilePath))
        {
            using var stream = File.OpenRead(_lastRunStatusFilePath);
            var lastRunStatuses = await JsonSerializer.DeserializeAsync<IList<RunStatus>>(stream, RunStatusJsonContext.Default.IListRunStatus);
            if (lastRunStatuses != null && lastRunStatuses.Count > 0)
            {
                var lastSuccessfulRun = lastRunStatuses.OrderByDescending(l => l.LastRunAt)
                        .FirstOrDefault(l => l.Repository == repoWithOwner && l.LastError == null);

                if (lastSuccessfulRun != null)
                {
                    return (lastSuccessfulRun.LastRunAt, lastSuccessfulRun.PRNumber);
                }
            }
        }
        return (null,null);
    }

    /// <summary>
    /// Updates the last run date and time in the file.
    /// This method appends the last run status to a JSON file, including the date,
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override async Task UpdateLastRunAt(DateTime dateTime, string repoWithOwner, int? prNumber, Exception? exception = null)
    {
        IList<RunStatus> lastRunStatuses = new List<RunStatus>();
        if (File.Exists(_lastRunStatusFilePath))
        {
            using var fileStream = new FileStream(_lastRunStatusFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            lastRunStatuses = await JsonSerializer.DeserializeAsync<IList<RunStatus>>(fileStream, RunStatusJsonContext.Default.IListRunStatus);
        }
        try
        {
            lastRunStatuses.Add(new RunStatus
            {
                LastRunAt = dateTime,
                LastError = exception?.GetAllMessages(),
                Repository = repoWithOwner,
                PRNumber = prNumber
            });
            using var fileStream = new FileStream(_lastRunStatusFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fileStream, lastRunStatuses, RunStatusJsonContext.Default.IListRunStatus);
        }
        catch (Exception ex)
        {
            // Log the exception if needed
            throw new InvalidOperationException("Failed to update last run status.", ex);
        }
    }
}
