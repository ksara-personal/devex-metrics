using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Metrics;
using System.CommandLine;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Metrics.EF;
using System.Data;
using System.Text.Json.Serialization.Metadata;
using Metrics.Models;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Metrics.MultiTenant;

namespace MetricsConsoleApp;

public class Program
{
    /// <summary>
    /// Main method where DI injects the services and executes the program.
    /// </summary>
    /// <returns></returns>
    public static async Task Main(string[] args)
    {
        var host = CreateServices(args);
        var sp = host.Services;

        var writeCommand = AddWriteToFileCommand(sp);
        var writeAllCommand = AddWriteToDatabaseCommand(sp);
        var exportCommand = AddExportCommand(sp);
        var exportTeamReportsCommand = AddExportTeamReportsToFileCommand(sp);
        var authorCommand = AddPRByAuthorCommand(sp);
        var teamCommand = AddPRByTeamCommand(sp);
        var prCommand = AddPRByIdCommand(sp);

        var rootCommand = new RootCommand("Metrics CLI Application")
        {
            writeCommand, writeAllCommand,exportCommand,exportTeamReportsCommand, authorCommand, teamCommand, prCommand
        };

        await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Adds a command to export team reports.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    static Command AddExportCommand(IServiceProvider serviceProvider)
    {
        var sourceOption = new Option<DataStoreType>("--source", "Source data store") { IsRequired = true };
        sourceOption.AddAlias("-s");

        var targetOption = new Option<DataStoreType>("--target", "Target data store") { IsRequired = true };
        targetOption.AddAlias("-t");

        var startOption = new Option<DateTime?>("--start", "Start date") { IsRequired = false };
        startOption.AddAlias("-sd");

        var endOption = new Option<DateTime?>("--end", "End date") { IsRequired = false };
        endOption.AddAlias("-ed");

        var teamOption = new Option<string>("--team", "Team name") { IsRequired = false };
        teamOption.AddAlias("-tn");

        var tenantIdOption = new Option<string>("--tenant-id", "Tenant identifier (e.g., tenant-1, tenant-2)") { IsRequired = true };
        tenantIdOption.AddAlias("-tid");

        var exportCommand = new Command("export", "Exports from the source data store to the target data store")
        {
            sourceOption, targetOption, startOption, endOption, teamOption, tenantIdOption
        };

        exportCommand.SetHandler(async (DataStoreType srcType, DataStoreType targetType, string? team, DateTime? start, DateTime? end, string tenantId) =>
        {
            SetTenantContext(serviceProvider, tenantId);
            var exportService = serviceProvider.GetRequiredService<MetricsExportService>();
            await exportService.ExportMetricsAsync(srcType, targetType, team, start, end);
        }, sourceOption, targetOption, teamOption, startOption, endOption, tenantIdOption);

        return exportCommand;
    }
    
    /// <summary>
    /// Adds a command to export team reports to a file.
    /// This command exports metrics for each team defined in the settings to a file.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    static Command AddExportTeamReportsToFileCommand(IServiceProvider serviceProvider)
    {
        var sourceOption = new Option<DataStoreType>("--source", "Source data store") { IsRequired = true };
        sourceOption.AddAlias("-s");

        var startOption = new Option<DateTime?>("--start", "Start date") { IsRequired = false };
        startOption.AddAlias("-sd");

        var endOption = new Option<DateTime?>("--end", "End date") { IsRequired = false };
        endOption.AddAlias("-ed");

        var tenantIdOption = new Option<string>("--tenant-id", "Tenant identifier (e.g., tenant-1, tenant-2)") { IsRequired = true };
        tenantIdOption.AddAlias("-tid");

        var exportCommand = new Command("export-team-metrics", "Exports teams specific metrics to the file")
        {
            sourceOption, startOption, endOption, tenantIdOption
        };

        exportCommand.SetHandler(async (DataStoreType srcType, DateTime? start, DateTime? end, string tenantId) =>
        {
            SetTenantContext(serviceProvider, tenantId);
            var exportService = serviceProvider.GetRequiredService<MetricsExportService>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var filePath = configuration.GetConnectionString("File");
            var orgDir = Path.GetDirectoryName(filePath);
            var dir = Path.Combine(orgDir, "Teams");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var userMembershipService = serviceProvider.GetRequiredService<UserMembershipService<GitHubOrganization>>();
            foreach (var team in userMembershipService.ProductTeams.SelectMany(p => p.Teams.Select(t => t.Name)))
            {
                var targetFile = Path.Combine(dir, $"{team}.json");
                Console.WriteLine($"Exporting metrics for team: {team}, src file: {filePath}, target file: {targetFile}");
                await exportService.ExportMetricsAsync(srcType, DataStoreType.File, team, start, end, false);
                if (File.Exists(filePath))
                {
                    // move the file.
                    File.Move(filePath, targetFile, true);
                }
            }
        }, sourceOption, startOption, endOption, tenantIdOption);

        return exportCommand;
    }

    /// <summary>
    /// Adds a file write command.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    static Command AddWriteToFileCommand(IServiceProvider serviceProvider)
    {
        var startOption = new Option<DateTime>("--start", "Start date") { IsRequired = true };
        startOption.AddAlias("-s");

        var endOption = new Option<DateTime>("--end", "End date") { IsRequired = true };
        endOption.AddAlias("-e");

        var tenantIdOption = new Option<string>("--tenant-id", "Tenant identifier (e.g., tenant-1, tenant-2)") { IsRequired = true };
        tenantIdOption.AddAlias("-tid");

        var writeCommand = new Command("writetofile", "Write metrics to json file")
        {
            startOption, endOption, tenantIdOption
        };

        writeCommand.SetHandler(async (DateTime start, DateTime end, string tenantId) =>
        {
            SetTenantContext(serviceProvider, tenantId);
            var dataSynchronizer = serviceProvider.GetRequiredService<DataSyncService<DevExMetricDbContext>>();
            await dataSynchronizer.WriteMetricsAsync(start, end);
        }, startOption, endOption, tenantIdOption);

        return writeCommand;
    }

    /// <summary>
    /// Writes all data from a predefined start to until now.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    static Command AddWriteToDatabaseCommand(IServiceProvider serviceProvider)
    {
        var startOption = new Option<DateTime?>("--start", "Start date"){ IsRequired = false };
        startOption.AddAlias("-s");

        var endOption = new Option<DateTime?>("--end", "End date"){ IsRequired = false };
        endOption.AddAlias("-e");

        var tenantIdOption = new Option<string>("--tenant-id", "Tenant identifier (e.g., tenant-1, tenant-2)") { IsRequired = true };
        tenantIdOption.AddAlias("-tid");

        var writeCommand = new Command("writetodb", "Write all metrics to database since a predefined date to until now")
        {
            startOption, endOption, tenantIdOption
        };
        writeCommand.SetHandler(async (DateTime? start, DateTime? end, string tenantId) =>
        {
            SetTenantContext(serviceProvider, tenantId);
            var dataSynchronizer = serviceProvider.GetRequiredService<DataSyncService<DevExMetricDbContext>>();
            await dataSynchronizer.WriteMetricsAsync(start,end);
        }, startOption, endOption, tenantIdOption);

        return writeCommand;
    }

    /// <summary>
    /// Add a command to fetch by author.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="startOption"></param>
    /// <param name="endOption"></param>
    /// <returns></returns>
    static Command AddPRByAuthorCommand(IServiceProvider serviceProvider)
    {
        var authorOption = new Option<string>("--author", "Author of pull requests") { IsRequired = true };
        authorOption.AddAlias("-a");

        var startOption = new Option<DateTime>("--start", "Start date"){ IsRequired = true };
        startOption.AddAlias("-s");

        var endOption = new Option<DateTime>("--end", "End date"){ IsRequired = true };
        endOption.AddAlias("-e");

        var tenantIdOption = new Option<string>("--tenant-id", "Tenant identifier (e.g., tenant-1, tenant-2)") { IsRequired = true };
        tenantIdOption.AddAlias("-tid");

        var getCommand = new Command("getbyauthor", "Gets the metrics of the PR author")
        {
            authorOption, startOption, endOption, tenantIdOption
        };

        getCommand.SetHandler(async (string author, DateTime start, DateTime end, string tenantId) =>
        {
            SetTenantContext(serviceProvider, tenantId);
            await Execute(serviceProvider, (connector) => connector.QueryMetricsByAuthorAsync(author,start,end), PRMetricsContext.Default.IEnumerablePRMetrics);
        }, authorOption, startOption, endOption, tenantIdOption);

        return getCommand;
    }

    /// <summary>
    /// Adds a command to fetch by PR id.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    static Command AddPRByIdCommand(IServiceProvider serviceProvider)
    {
        var repoOption = new Option<string>("--repo", "Repository where the pull request can be found"){ IsRequired = true };
        repoOption.AddAlias("-r");

        var idOption = new Option<int>("--id", "Pull request id"){ IsRequired = true };
        idOption.AddAlias("-id");

        var tenantIdOption = new Option<string>("--tenant-id", "Tenant identifier (e.g., tenant-1, tenant-2)") { IsRequired = true };
        tenantIdOption.AddAlias("-tid");

        var getCommand = new Command("getbyid", "Gets the metrics of the PR for the given id")
        {
            repoOption, idOption, tenantIdOption
        };

        getCommand.SetHandler(async (string repo, int prId, string tenantId) => 
        {
            SetTenantContext(serviceProvider, tenantId);
            await Execute(serviceProvider, (connector) => connector.QueryMetricsByIdAsync(repo,prId), PRMetricsContext.Default.PRMetrics);
        }, repoOption, idOption, tenantIdOption);

        return getCommand;
    }

    /// <summary>
    /// Adds a command to fetch by team.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    static Command AddPRByTeamCommand(IServiceProvider serviceProvider)
    {
        var authorOption = new Option<string>("--team", "Team name"){ IsRequired = true };
        authorOption.AddAlias("-t");

        var startOption = new Option<DateTime>("--start", "Start date"){ IsRequired = true };
        startOption.AddAlias("-s");

        var endOption = new Option<DateTime>("--end", "End date"){ IsRequired = true };
        endOption.AddAlias("-e");

        var tenantIdOption = new Option<string>("--tenant-id", "Tenant identifier (e.g., tenant-1, tenant-2)") { IsRequired = true };
        tenantIdOption.AddAlias("-tid");

        var getCommand = new Command("getbyteam", "Gets the metrics for a team")
        {
            authorOption, startOption, endOption, tenantIdOption
        };

        getCommand.SetHandler(async (string team, DateTime start,DateTime end, string tenantId) =>
        {
            SetTenantContext(serviceProvider, tenantId);
            await Execute(serviceProvider, (connector) => connector.QueryMetricsByTeamAsync(team, start, end), PRMetricsContext.Default.IEnumerablePRMetrics);
        }, authorOption, startOption, endOption, tenantIdOption);

        return getCommand;
    }

    /// <summary>
    /// Executes the connector command and measures the time consumed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serviceProvider"></param>
    /// <param name="callback"></param>
    /// <param name="typeInfo"></param>
    /// <returns></returns>
    static async Task Execute<T>(IServiceProvider serviceProvider, Func<DataClient, Task<T>> callback, JsonTypeInfo typeInfo)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var connector = serviceProvider.GetRequiredService<DataClient>();
        var met = await callback(connector);
        sw.Stop();

        Console.WriteLine(JsonSerializer.Serialize(met, typeInfo));
        Console.WriteLine($"Metrics took {sw.Elapsed}, {sw.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Sets the tenant context for the command execution.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="tenantId">The tenant identifier (e.g., "tenant-1", "tenant-2")</param>
    static void SetTenantContext(IServiceProvider serviceProvider, string tenantId)
    {
        var contextSetter = serviceProvider.GetRequiredService<IMultiTenantContextSetter>();
        var tenant = new AppTenantInfo
        {
            Id = tenantId,
            Identifier = tenantId,
            Name = tenantId,
            ConfigurationFile = $"appsettings.{tenantId}.json"
        };
        var multiTenantContext = new MultiTenantContext<AppTenantInfo> { TenantInfo = tenant };
        contextSetter.MultiTenantContext = multiTenantContext;
        Console.WriteLine($"Tenant context set to: {tenantId}");
    }

    /// <summary>
    /// Create services and adds them to DI.
    /// </summary>
    /// <returns></returns>
    static IHost CreateServices(string[] args)
    {
        IConfigurationBuilder configurationBuilder = null;
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => configurationBuilder = config);

        var host = hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddHttpClient();
                services.AddLogging(l => l.AddSimpleConsole(c => c.IncludeScopes = true))
                .AddDIServices(configurationBuilder);
            })
            .Build();
        return host;
    }
}
