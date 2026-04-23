using System.Diagnostics;
using PercCli.Exporter.Collectors;

namespace PercCli.Exporter;

public sealed class PercCollectService(PercCollectOptions collOpts, PercCollector collector): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(collOpts.PollingInterval));
        var sw = Stopwatch.StartNew();

        do
        {
            sw.Restart();

            await collector.CollectControllerMetrics(stoppingToken);
            await collector.CollectVirtualDriveMetrics(stoppingToken);
            
            collector.Update();

            var (ctlCount, vdCount, pdCount) = collector.GetCount();
            
            sw.Stop();
            Console.WriteLine($"Query {sw.ElapsedMilliseconds}ms | controllers={ctlCount}, virtual_drives={vdCount}, physical_drives={pdCount}");
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}