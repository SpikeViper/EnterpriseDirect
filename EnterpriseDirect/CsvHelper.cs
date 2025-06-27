namespace EnterpriseDirect;

/// <summary>
/// A helper class for CSV operations.
/// Blazor razor compilation does not build properly with complex escaping logic in Razor files.
/// </summary>
public class CsvHelper
{
    // Helper method to escape a string for CSV
    public static string EscapeCsvField(string? field)
    {
        // Handle null fields
        if (field == null) return "";

        // Escape double quotes by doubling them, and then enclose the whole field in quotes
        // if it contains commas, double quotes, or newlines (or starts/ends with spaces)
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r') || field.StartsWith(" ") || field.EndsWith(" "))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field; // No escaping needed
    }
}