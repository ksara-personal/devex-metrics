using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Services.Common;

namespace Metrics.ADO;

/// <summary>
/// Custom JSON converter for <see cref="WorkItemDto"/> to handle serialization and deserialization
/// of work item fields as a flexible dictionary structure.
/// </summary>
sealed class WorkItemDtoConverter : JsonConverter<WorkItemDto>
{
    /// <summary>
    /// Reads and deserializes a <see cref="WorkItemDto"/> from JSON, mapping fields into a dictionary.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert (should be <see cref="WorkItemDto"/>).</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>A deserialized <see cref="WorkItemDto"/> instance.</returns>
    public override WorkItemDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize into a dictionary first
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);

        var wrapper = new WorkItemDto
        {
            Fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        };

        foreach (var kvp in dict)
        {
            if (kvp.Key == "Id")
            {
                wrapper.Id = kvp.Value.GetInt32();
            }
            else
            {
                wrapper.Fields[kvp.Key] = kvp.Value.ToString();
            }
        }

        return wrapper;
    }

    /// <summary>
    /// Writes a <see cref="WorkItemDto"/> to JSON, serializing the Id and all fields in the dictionary.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The <see cref="WorkItemDto"/> to serialize.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, WorkItemDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("Id", value.Id.Value);

        if (value.Fields != null)
        {
            foreach (var kvp in value.Fields)
            {
                var val = kvp.Value;
                if (val is int i)
                {
                    writer.WriteNumber(kvp.Key, i);
                }
                else if (val is bool b)
                {
                    writer.WriteBoolean(kvp.Key, b);
                }
                else if (val is DateTime dt)
                {
                    writer.WriteString(kvp.Key, dt);
                }
                else
                {
                    writer.WriteString(kvp.Key, kvp.Value?.ToString());
                }
            }
        }
        writer.WriteEndObject();
    }
}
