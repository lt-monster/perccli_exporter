using System.Diagnostics;
using PercCli.Exporter.Stores;

namespace PercCli.Exporter.Collectors;

public sealed class LocalPercCollector(PercMetricStore metricStore) : PercCollector(metricStore)
{
    private static async Task RunCommand(string arguments, Func<Stream, Task> handler, CancellationToken stoppingToken)
    {
        var parts = arguments.Split(' ', 2);
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = parts[0],
            Arguments = parts.Length > 1 ? parts[1] : string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = true,
            CreateNoWindow = false,
        };
        process.Start();
        try
        {
            await handler(process.StandardOutput.BaseStream);
        }
        finally
        {
            await process.WaitForExitAsync(stoppingToken);
        }
    }

    public override async Task CollectControllerMetrics(CancellationToken stoppingToken)
    {
        try
        {
            await RunCommand(CMD_CONTROLLERS, s => HandingControllers(s, stoppingToken), stoppingToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Controller query failed: {ex.Message}");
        }
    }

    public override async Task CollectVirtualDriveMetrics(CancellationToken stoppingToken)
    {
        MetricStore.Snapshot.VirtualDriveMetricStore.Count = 0;
        MetricStore.Snapshot.PhysicalDriveMetricStore.Count = 0;

        foreach (var ctl in MetricStore.Snapshot.ControllerMetricStore)
        {
            if (ctl is null) continue;

            try
            {
                var cmd = GetVirtualDriveQueryCmd(ctl.Ctl);
                await RunCommand(cmd, s => HandingVirtualDrives(s, ctl.Ctl, stoppingToken), stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VirtualDrive query failed: {ex.Message}");
            }
        }
    }
}
