using System.Data.Common;
using System.Text.Json;
using ChoETL;

namespace Metrics.Infrastructure;

/// <summary>
/// Extension methods for JSON file operations.
/// These methods provide functionality to write or append data to a JSON file and read from it.
/// </summary>
public static class JsonFileExtensions
{
    /// <summary>
    /// Extension method to write/append json into a file.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="outfile"></param>
    /// <param name="typeInfo"></param>
    public static async Task WriteOrAppendToJsonFile(this IEnumerable<PRMetrics> data, string outfile)
    {
        HashSet<PRMetrics> metrics = default;
        if (File.Exists(outfile))
        {
            using var stream = File.OpenRead(outfile);
            metrics = await JsonSerializer.DeserializeAsync<HashSet<PRMetrics>>(stream, PRMetricsContext.Default.HashSetPRMetrics);
        }
        metrics = metrics ?? new HashSet<PRMetrics>();
        foreach (var item in data)
        {
            metrics.Add(item);
        }
        using var fileStream = File.OpenWrite(outfile);
        await JsonSerializer.SerializeAsync<HashSet<PRMetrics>>(fileStream, metrics, PRMetricsContext.Default.HashSetPRMetrics);
    }

    /// <summary>
    /// Writes or appends data to a JSON file.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="outfile"></param>
    /// <param name="append"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task WriteOrAppendToJsonFile<T>(this IEnumerable<T> data, string outfile, bool append = false)
    {
        HashSet<T> metrics = default;
        if (append && File.Exists(outfile))
        {
            using var stream = File.OpenRead(outfile);
            metrics = await JsonSerializer.DeserializeAsync<HashSet<T>>(stream);
        }
        metrics = metrics ?? new HashSet<T>();
        foreach (var item in data)
        {
            metrics.Add(item);
        }
        using var fileStream = File.OpenWrite(outfile);
        await JsonSerializer.SerializeAsync<HashSet<T>>(fileStream, metrics, new JsonSerializerOptions { WriteIndented = true });
    }

    public static async Task<HashSet<T>> ReadFromFileAsync<T>(this string fileName)
    {
        HashSet<T> metrics = default;
        if (File.Exists(fileName))
        {
            using var stream = File.OpenRead(fileName);
            metrics = await JsonSerializer.DeserializeAsync<HashSet<T>>(stream);
        }
        return metrics;
    }

    /// <summary>
    /// Reads the json file and adds the content to the metrics.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fileName"></param>
    /// <param name="metrics"></param>
    /// <returns></returns>
    public static async Task ReadFromFileAsync(this string fileName, HashSet<PRMetrics> metrics)
    {
        if (File.Exists(fileName))
        {
            using var stream = File.OpenRead(fileName);
            var sizeInMB = stream.Length / 1024 / 1024;
            if (sizeInMB > 1)
            {
                // If the file is larger than 100 MB, read it in chunks.
                await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<PRMetrics>(stream, PRMetricsContext.Default.PRMetrics))
                {
                    metrics.Add(item);
                }
            }
            else
            {
                var metrics2 = await JsonSerializer.DeserializeAsync<HashSet<PRMetrics>>(stream, PRMetricsContext.Default.HashSetPRMetrics);
                foreach (var item in metrics2)
                {
                    metrics.Add(item);
                }
            }
        }
    }

    /// <summary>
    /// Writes the data to a CSV file and returns the CSV content as a string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="outfile"></param>
    /// <returns></returns> <summary>
    public static async Task<string> WriteToCsvData<T>(this IEnumerable<T> data)
    {
        var sb = new System.Text.StringBuilder();
        using var writer = new StringWriter(sb);
        using var jsonReader = ChoJSONReader.LoadText(JsonSerializer.Serialize(data))
            .Configure(c => c.FlattenNode = true)
            .JsonSerializationSettings(s => s.DateParseHandling = Newtonsoft.Json.DateParseHandling.DateTime);

        using var csvWriter = new ChoCSVWriter(writer)
            .WithDelimiter(",")
            .WithFirstLineHeader()
            .Configure(c => c.IgnoreDictionaryFieldPrefix = true);

        csvWriter.Write(jsonReader);
        await writer.FlushAsync();
        var csvText = sb.ToString();
        return csvText;
    }
}
