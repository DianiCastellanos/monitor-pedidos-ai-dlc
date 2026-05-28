namespace MonitorPedidos.Domain.Dashboard;

public record LogLine(
    DateTime Timestamp,
    string   Level,
    string   Message,
    string?  Exception);

public interface ITechnicalLogReader
{
    Task<IReadOnlyList<LogLine>> GetRecentAsync(int maxLines, CancellationToken ct = default);
    string ExportCsv(IReadOnlyList<LogLine> lines);
    string ExportJson(IReadOnlyList<LogLine> lines);
}
