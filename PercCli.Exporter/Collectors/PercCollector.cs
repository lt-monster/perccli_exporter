using System.Buffers.Text;
using System.Collections.Frozen;
using System.IO.Pipelines;
using System.Text.Json;
using PercCli.Exporter.Metrics;
using PercCli.Exporter.Stores;

namespace PercCli.Exporter.Collectors;

//采集器
public abstract class PercCollector(PercMetricStore metricStore)
{
    private const double KB = 1024.0;
    private const double MB = 1024.0 * KB;
    private const double GB = 1024.0 * MB;
    private const double TB = 1024.0 * GB;
    private const double PB = 1024.0 * TB;
    
    protected const string CMD_CONTROLLERS = "sudo perccli64 show J";

    protected FrozenDictionary<int, string> virtualDriveQueryCmds = FrozenDictionary<int, string>.Empty;

    public PercMetricStore MetricStore => metricStore;

    protected string GetVirtualDriveQueryCmd(int ctlId)
    {
        if (!virtualDriveQueryCmds.TryGetValue(ctlId, out var cmd))
        {
            cmd = $"sudo perccli64 /c{ctlId}/vall show J";
            var dic = virtualDriveQueryCmds.ToDictionary();
            dic.Add(ctlId, cmd);
            virtualDriveQueryCmds = dic.ToFrozenDictionary();
        }
        return cmd;
    }

    protected static int GetIntMetric(Utf8JsonReader reader)
    {
        reader.Read();
        return reader.GetInt32();
    }
    
    protected static int GetStringMetric(Utf8JsonReader reader)
    {
        reader.Read();
        return StringStore.GetOrAdd(reader.ValueSpan);
    }

    protected static double ParseSize(Utf8JsonReader reader)
    {
        reader.Read();
        var span = reader.ValueSpan;
        if (Utf8Parser.TryParse(span, out double num, out var bytesConsumed))
        {
            ReadOnlySpan<byte> unitSpan = span[bytesConsumed..].Trim(" "u8);

            if (unitSpan.SequenceEqual("KB"u8)) return num * KB;
            if (unitSpan.SequenceEqual("MB"u8)) return num * MB;
            if (unitSpan.SequenceEqual("GB"u8)) return num * GB;
            if (unitSpan.SequenceEqual("TB"u8)) return num * TB;
            if (unitSpan.SequenceEqual("PB"u8)) return num * PB;

            return num;
        }

        return 0;
    }

    protected async Task HandingControllers(Stream stream, CancellationToken stoppingToken)
    {
        var pipeReader = PipeReader.Create(stream);
        JsonReaderState state = default;
        var index = -1;
        while (true)
        {
            var result = await pipeReader.ReadAsync(stoppingToken);
            var buffer = result.Buffer;
        
            var jsonReader = new Utf8JsonReader(buffer, isFinalBlock: result.IsCompleted, state: state);

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType != JsonTokenType.PropertyName || !jsonReader.ValueTextEquals("System Overview"u8)) continue;
                
                ControllerMetric? metric = null;
                while (jsonReader.Read())
                {
                    if(jsonReader.TokenType == JsonTokenType.EndArray) break;
                    
                    if (jsonReader.TokenType == JsonTokenType.StartObject)
                    {
                        index++;
                        metric = MetricStore.Snapshot.ControllerMetricStore[index] ??= new();
                    }
                    else
                    {
                        if (jsonReader.TokenType != JsonTokenType.PropertyName) continue;
                        
                        var propertyName = jsonReader.ValueSpan;
                        int? _ = propertyName switch
                        {
                            _ when propertyName.SequenceEqual("Ctl"u8) => metric?.Ctl = GetIntMetric(jsonReader),
                            _ when propertyName.SequenceEqual("Model"u8) => metric?.Model= GetStringMetric(jsonReader),
                            _ when propertyName.SequenceEqual("Ports"u8) => metric?.Ports = GetIntMetric(jsonReader),
                            _ when propertyName.SequenceEqual("PDs"u8) => metric?.PDs = GetIntMetric(jsonReader),
                            _ when propertyName.SequenceEqual("DGs"u8) => metric?.DGs = GetIntMetric(jsonReader),
                            _ when propertyName.SequenceEqual("DNOpt"u8) => metric?.DnOpt = GetIntMetric(jsonReader),
                            _ when propertyName.SequenceEqual("VDs"u8) => metric?.VDs = GetIntMetric(jsonReader),
                            _ when propertyName.SequenceEqual("VNOpt"u8) => metric?.VnOpt = GetIntMetric(jsonReader),
                            _ when propertyName.SequenceEqual("BBU"u8) => metric?.SetBbu(jsonReader),
                            _ when propertyName.SequenceEqual("sPR"u8) => metric?.SetSpr(jsonReader),
                            _ when propertyName.SequenceEqual("DS"u8) => metric?.Ds = GetStringMetric(jsonReader),
                            _ when propertyName.SequenceEqual("EHS"u8) => metric?.SetEhs(jsonReader),
                            _ when propertyName.SequenceEqual("ASOs"u8) => metric?.AsOs = GetIntMetric(jsonReader),
                            _ when propertyName.SequenceEqual("Hlth"u8) => metric?.SetHlth(jsonReader),
                            _ => 0
                        };
                    }
                }
            }

            state = jsonReader.CurrentState;
            // 告诉 Pipe 我们读到了哪里
            pipeReader.AdvanceTo(jsonReader.Position, buffer.End);

            if (result.IsCompleted) break;
        }
        await pipeReader.CompleteAsync();

        MetricStore.Snapshot.ControllerMetricStore.Count = index + 1;
    }

    protected async Task HandingVirtualDrives(Stream stream, int ctlId, CancellationToken stoppingToken)
    {
        VirtualDriveMetric? metric = null;
        var pipeReader = PipeReader.Create(stream);
        JsonReaderState state = default;
        var vdBasicInfoReading = false; // 类似/c0/v0的对象读取
        while (true)
        {
            var result = await pipeReader.ReadAsync(stoppingToken);
            var buffer = result.Buffer;
    
            var jsonReader = new Utf8JsonReader(buffer, isFinalBlock: result.IsCompleted, state: state);

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonTokenType.PropertyName && jsonReader.ValueTextEquals("DG/VD"))
                {
                    vdBasicInfoReading = true;
                    MetricStore.Snapshot.VirtualDriveMetricStore.Count++;
                    metric = MetricStore.Snapshot.VirtualDriveMetricStore[MetricStore.Snapshot.VirtualDriveMetricStore.Count - 1];
                    metric.CtlId = ctlId;
                    
                    jsonReader.Read();
                    var dgvdSpan = jsonReader.ValueSpan;
                    if (Utf8Parser.TryParse(dgvdSpan, out int dg, out int bytesConsumed))
                    {
                        metric.Dg = dg;

                        if (dgvdSpan.Length > bytesConsumed)
                        {
                            if (Utf8Parser.TryParse(dgvdSpan[(bytesConsumed+1)..], out int vd, out var _))
                            {
                                metric.Vd = vd;
                            }
                        }
                    }
                    continue;
                }
                
                if (vdBasicInfoReading)
                {
                    if (jsonReader.TokenType == JsonTokenType.EndObject)
                    {
                        vdBasicInfoReading = false;
                        continue;
                    }

                    if (jsonReader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = jsonReader.ValueSpan;
                        _ = propertyName switch
                        {
                            _ when jsonReader.ValueTextEquals("TYPE"u8) => metric?.Type = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("State"u8) => metric?.SetState(jsonReader),
                            _ when jsonReader.ValueTextEquals("Access"u8) => metric?.Access = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("Consist"u8) => metric?.SetConsist(jsonReader),
                            _ when jsonReader.ValueTextEquals("Cache"u8) => metric?.Cache = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("Cac"u8) => metric?.Cac = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("sCC"u8) => metric?.SetScc(jsonReader),
                            _ when jsonReader.ValueTextEquals("Size"u8) => metric?.SizeBytes = ParseSize(jsonReader),
                            _ when jsonReader.ValueTextEquals("Name"u8) => metric?.Name = GetStringMetric(jsonReader),
                            _ => 0
                        };
                    }
                }
            }

            state = jsonReader.CurrentState;
            // 告诉 Pipe 我们读到了哪里
            pipeReader.AdvanceTo(jsonReader.Position, buffer.End);

            if (result.IsCompleted) break;
        }
        await pipeReader.CompleteAsync();
    }
    
    public abstract Task CollectControllerMetrics(CancellationToken stoppingToken);

    public abstract Task CollectVirtualDriveMetrics(CancellationToken stoppingToken);

    public void Update() => metricStore.UpdateSnapshot();
}