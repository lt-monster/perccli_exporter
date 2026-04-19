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
            
            sw.Stop();
            Console.WriteLine($"Query took {sw.Elapsed}.");
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}