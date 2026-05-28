namespace Metrics.Extensions;

/// <summary>
/// Provides extension methods for safely extracting and converting values from a dictionary of work item fields.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Safely retrieves and converts a value from the dictionary for the specified field name.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="fields">The dictionary of work item fields.</param>
    /// <param name="field">The field name to retrieve.</param>
    /// <returns>The value converted to type <typeparamref name="T"/>, or default if not found or null.</returns>
    public static T? GetValue<T>(this IDictionary<string, object> fields, string field, T defaultValue = default) =>
        fields.TryGetValue(field, out var value) && value != null ? ConvertTo<T>(value) : defaultValue;

    /// <summary>
    /// Converts an input object to the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="input">The input object to convert.</param>
    /// <returns>The converted value, or default if input is null or conversion fails.</returns>
    static T? ConvertTo<T>(object input)
    {
        if (input == null || input is DBNull)
            return default;

        try
        {
            return typeof(T).IsPrimitive || typeof(string) == typeof(T) ? (T)Convert.ChangeType(input, typeof(T)) : (T)input;
        }
        catch
        {
            return default;
        }
    }
}
