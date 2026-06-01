using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitorPedidos.Domain.Monitoring;

namespace MonitorPedidos.Infrastructure.JobMonitoring;

public sealed class SchtasksJobStatusSource : IJobStatusSource
{
    private readonly JobsMonitorOptions _options;
    private readonly ILogger<SchtasksJobStatusSource> _logger;

    public SchtasksJobStatusSource(IOptions<JobsMonitorOptions> options, ILogger<SchtasksJobStatusSource> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    public async Task<IReadOnlyList<JobStatusSnapshot>> GetCurrentStatusAsync(CancellationToken ct = default)
    {
        var results = new List<JobStatusSnapshot>(1);
        JobStatusSnapshot snapshot;

        try
        {
            var psi = new ProcessStartInfo("schtasks")
            {
                ArgumentList =
                {
                    "/QUERY",
                    "/S", _options.Server,
                    "/TN", _options.TaskName,
                    "/FO", "CSV",
                    "/NH"
                },
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_options.CommandTimeoutMs);

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

            await process.WaitForExitAsync(cts.Token);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                _logger.LogWarning(
                    "schtasks /QUERY para {Task} en {Server} falló (exit={Exit}): {Error}",
                    _options.TaskName, _options.Server, process.ExitCode, stderr);

                snapshot = CreateFailedSnapshot();
            }
            else
            {
                snapshot = ParseOutput(stdout, stderr);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "schtasks /QUERY para {Task} en {Server} excedió el timeout de {T}ms",
                _options.TaskName, _options.Server, _options.CommandTimeoutMs);

            snapshot = CreateFailedSnapshot($"Sin conexión a {_options.Server} (timeout {_options.CommandTimeoutMs}ms)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error ejecutando schtasks /QUERY para {Task} en {Server}",
                _options.TaskName, _options.Server);

            snapshot = CreateFailedSnapshot($"Error al consultar {_options.Server}: {ex.Message[..Math.Min(50, ex.Message.Length)]}");
        }

        results.Add(snapshot);
        return results;
    }

    private JobStatusSnapshot CreateFailedSnapshot(string reason = "Sin acceso al servidor")
        => new(_options.TaskName, _options.TaskName, false, null, false, FailureReason: reason);

    private JobStatusSnapshot ParseOutput(string stdout, string stderr)
    {
        var line = stdout?.Trim();

        if (string.IsNullOrEmpty(line))
        {
            _logger.LogWarning("schtasks /QUERY devolvió salida vacía para {Task}", _options.TaskName);
            return CreateFailedSnapshot();
        }

        var parts = SplitCsvLine(line);
        var status = parts.Length >= 3 ? parts[2].Trim('"').Trim() : string.Empty;

        _logger.LogInformation(
            "schtasks /QUERY {Task} en {Server}: status={Status}",
            _options.TaskName, _options.Server, status);

        var isReady      = string.Equals(status, "Ready", StringComparison.OrdinalIgnoreCase);
        var failureReason = isReady ? null : $"Job {status} — no está en estado Ready";

        return new JobStatusSnapshot(
            _options.TaskName,
            _options.TaskName,
            IsRunning:              isReady,
            LastExecutedAt:         null,
            LastExecutionSucceeded: isReady,
            FailureReason:          failureReason);
    }

    private static string[] SplitCsvLine(string line)
    {
        var parts = new List<string>();
        var inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (var ch in line)
        {
            if (ch == '"')
                inQuotes = !inQuotes;
            else if (ch == ',' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
                current.Append(ch);
        }

        parts.Add(current.ToString());
        return parts.ToArray();
    }
}
