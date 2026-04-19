using PercCli.Exporter.Stores;

namespace PercCli.Exporter.Collectors;

public sealed class FilePercCollector(PercMetricStore metricStore): PercCollector(metricStore)
{
    public override async Task CollectControllerMetrics(CancellationToken stoppingToken)
    {
        try
        {
            var ctlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "control.json");
            var stream = new FileStream(ctlPath, FileMode.Open, FileAccess.Read);

            await HandingControllers(stream, stoppingToken);
        }
        catch (Exception ex)
        {
            
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
                var ctlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "vd-list-info.json");
                await using var stream = new FileStream(ctlPath, FileMode.Open, FileAccess.Read);

                await HandingVirtualDrives(stream, ctl.Ctl, stoppingToken);
            }
            catch (Exception ex)
            {
            
            }
        }
    }
}