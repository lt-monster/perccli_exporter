using System.Diagnostics;
using System.Runtime.InteropServices;
using PercCli.Exporter.Stores;

namespace PercCli.Exporter.Collectors;

public sealed class LocalPercCollector(PercMetricStore metricStore, PercCollectOptions percOptions) : PercCollector(metricStore, percOptions)
{
    private static async Task RunCommand(string filename, string arguments, Func<Stream, Task> handler, CancellationToken stoppingToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await RunCommand("perccli64", "show J", stream => HandingControllers(stream, stoppingToken), stoppingToken);
            }
            else
            {
                if (PercOptions.IsRoot)
                {
                    await RunCommand("perccli64", "show J", stream => HandingControllers(stream, stoppingToken), stoppingToken);
                }
                else
                {
                    await RunCommand("sudo", "perccli64 show J", stream => HandingControllers(stream, stoppingToken), stoppingToken);
                }
            }
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
                var (filename, args) = GetVirtualDriveQueryCmd(ctl.Ctl);
                await RunCommand(filename, args, s => HandingVirtualDrives(s, ctl.Ctl, stoppingToken), stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VirtualDrive query failed: {ex.Message}");
            }
        }
    }
}
