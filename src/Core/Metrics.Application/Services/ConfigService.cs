using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Metrics.Application;

/// <summary>
/// Class for config service
/// </summary>
public sealed class ConfigService
{
    readonly IConfiguration _configuration;
    DataStoreType _storeType;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="configuration"></param>
    public ConfigService(IConfiguration configuration, ILogger<ConfigService> logger)
    {
        _configuration = configuration;
        var storeType = configuration.GetValue<string>("DataStoreType", "InMemory");
        _storeType = Enum.Parse<DataStoreType>(storeType, true);
        logger.LogInformation("ConfigService initialized with DataStoreType: {DataStoreType}", _storeType);
    }

    /// <summary>
    /// Gets the datastore type.
    /// </summary>
    public DataStoreType DataStoreType
    {
        get => _storeType;
        internal set => _storeType = value;
    }
}
