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
}

public sealed class MetricStore<T>: IEnumerable<T?> where T: class,new()
{
    public T?[] Metrics { get; private set; } = ArrayPool<T?>.Shared.Rent(16);
    
    public int Count { get; set; }
    
    private void ResizeControllers()
    {
        var newMetrics = ArrayPool<T?>.Shared.Rent(Metrics.Length+16);
        Array.Copy(Metrics, newMetrics, Metrics.Length);
        ArrayPool<T?>.Shared.Return(Metrics);
        Metrics = newMetrics;
    }
    
    public T this[int index]
    {
        get
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException("Index is out of range.");
            }
            if (index >= Metrics.Length)
            {
                ResizeControllers();
            }

            return Metrics[index] ??= new();
        }
        set
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException("Index is out of range.");
            }
            
            if (index >= Metrics.Length)
            {
                ResizeControllers();
            }

            Metrics[index] = value;
        }
    }

    public IEnumerator<T?> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return Metrics[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}