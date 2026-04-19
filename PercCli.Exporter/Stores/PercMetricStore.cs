using System.Buffers;
using System.Collections;
using PercCli.Exporter.Metrics;

namespace PercCli.Exporter.Stores;

public sealed class PercMetricStore
{
    private volatile PercCliMetricSnapshot current = new();

    public PercCliMetricSnapshot Current => current;
    
    public PercCliMetricSnapshot Snapshot { get; private set; } = new();

    public void UpdateSnapshot()
    {
        var old = Interlocked.Exchange(ref current, Snapshot);
        Snapshot = old;
    }
}

public sealed class PercCliMetricSnapshot
{
    public MetricStore<ControllerMetric> ControllerMetricStore { get; } = new();
    
    public MetricStore<VirtualDriveMetric> VirtualDriveMetricStore { get; } = new();

    public MetricStore<PhysicalDriveMetric> PhysicalDriveMetricStore { get; } = new();
}

public sealed class MetricStore<T>: IEnumerable<T?> where T: class,new()
{
    private T?[] metrics = ArrayPool<T?>.Shared.Rent(16);
    
    public int Count { get; set; }
    
    private void ResizeControllers()
    {
        var newMetrics = ArrayPool<T?>.Shared.Rent(metrics.Length+16);
        Array.Copy(metrics, newMetrics, metrics.Length);
        ArrayPool<T?>.Shared.Return(metrics);
        metrics = newMetrics;
    }
    
    public T this[int index]
    {
        get
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException("Index is out of range.");
            }
            if (index >= metrics.Length)
            {
                ResizeControllers();
            }

            return metrics[index] ??= new();
        }
        set
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException("Index is out of range.");
            }
            
            if (index >= metrics.Length)
            {
                ResizeControllers();
            }

            metrics[index] = value;
        }
    }

    public IEnumerator<T?> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return metrics[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}