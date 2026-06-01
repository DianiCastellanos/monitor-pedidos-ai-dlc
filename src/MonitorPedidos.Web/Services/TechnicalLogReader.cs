using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MonitorPedidos.Domain.Dashboard;

namespace MonitorPedidos.Web.Services;

public sealed class TechnicalLogReader : ITechnicalLogReader
{
    // ADR-U6-03: Regex compilada — amortizada entre llamadas
    private static readonly Regex _pattern = new(
        @"^\[(\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}[^\]]*)\] \[?(INF|WRN|ERR|CRT|DBG)\]? (.+)$",
        RegexOptions.Compiled);

    private readonly IConfiguration _config;

    public TechnicalLogReader(IConfiguration config)
    {
        _config = config;
    }

    public Task<IReadOnlyList<LogLine>> GetRecentAsync(int maxLines, CancellationToken ct = default)
    {
        var filePath = ResolveFilePath(_config["Logging:FilePath"] ?? "logs/log-.txt");

        if (filePath is null)
            return Task.FromResult<IReadOnlyList<LogLine>>(Array.Empty<LogLine>());

        // FileShare.ReadWrite: permite leer mientras Serilog mantiene el archivo abierto para escritura
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var rawLines     = ReadAllLines(reader).TakeLast(maxLines);
        var result   = new List<LogLine>();
        LogLine?    current = null;

        foreach (var raw in rawLines)
        {
            var m = _pattern.Match(raw);
            if (m.Success)
            {
                current = new LogLine(
                    DateTime.TryParse(m.Groups[1].Value, out var ts) ? ts : DateTime.UtcNow,
                    m.Groups[2].Value,
                    m.Groups[3].Value,
                    Exception: null);
                result.Add(current);
            }
            else if (current is not null)
            {
                // Línea de stack trace — concatenar a Exception de la línea anterior
                var updated = current with { Exception = (current.Exception ?? "") + "\n" + raw };
                result[^1] = updated;
                current    = updated;
            }
        }

        return Task.FromResult<IReadOnlyList<LogLine>>(result);
    }

    public string ExportCsv(IReadOnlyList<LogLine> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Level,Message,Exception");
        foreach (var l in lines)
        {
            sb.AppendLine(string.Join(",",
                Escape(l.Timestamp.ToString("O")),
                Escape(l.Level),
                Escape(l.Message),
                Escape(l.Exception ?? "")));
        }
        return sb.ToString();
    }

    public string ExportJson(IReadOnlyList<LogLine> lines)
        => JsonSerializer.Serialize(lines, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static IEnumerable<string> ReadAllLines(StreamReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) is not null)
            yield return line;
    }

    private static string? ResolveFilePath(string configured)
    {
        if (File.Exists(configured)) return configured;

        var dir  = Path.GetDirectoryName(configured) ?? ".";
        var stem = Path.GetFileNameWithoutExtension(configured);
        var ext  = Path.GetExtension(configured);

        if (!Directory.Exists(dir)) return null;

        return Directory.GetFiles(dir, $"{stem}*{ext}")
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
    }

    private static string Escape(string v)
    {
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
