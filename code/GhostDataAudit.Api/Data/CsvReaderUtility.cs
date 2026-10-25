using System.Text;

namespace GhostDataAudit.Api.Data;

/// <summary>
/// Robust standard RFC 4180 CSV parser supporting quotes, escaped quotes (""),
/// commas within quotes, trailing line endings, and custom record mappings.
/// </summary>
public static class CsvReaderUtility
{
    public static List<List<string>> Parse(string content)
    {
        var records = new List<List<string>>();
        if (string.IsNullOrEmpty(content))
            return records;

        var currentRecord = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check if it's an escaped quote ("")
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (c == '\r')
                {
                    if (i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++; // Skip \n in CRLF
                    }
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();
                    records.Add(currentRecord);
                    currentRecord = new List<string>();
                }
                else if (c == '\n')
                {
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();
                    records.Add(currentRecord);
                    currentRecord = new List<string>();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        // Add trailing field and record if present
        if (currentField.Length > 0 || currentRecord.Count > 0)
        {
            currentRecord.Add(currentField.ToString());
            records.Add(currentRecord);
        }

        return records;
    }

    public static async Task<List<List<string>>> ParseFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}", filePath);

        var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
        return Parse(content);
    }
}

