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
        VirtualDriveMetric? vdMetric = null;
        PhysicalDriveMetric? pdMetric = null;
        var pipeReader = PipeReader.Create(stream);
        JsonReaderState state = default;
        var vdBasicInfoReading = false; // 类似/c0/v0的对象读取
        var pdBasicInfoReading = false;
        while (true)
        {
            var result = await pipeReader.ReadAsync(stoppingToken);
            var buffer = result.Buffer;

            var jsonReader = new Utf8JsonReader(buffer, isFinalBlock: result.IsCompleted, state: state);

            while (jsonReader.Read())
            {
                // VD 入口：DG/VD 属性
                if (jsonReader.TokenType == JsonTokenType.PropertyName && jsonReader.ValueTextEquals("DG/VD"u8))
                {
                    vdBasicInfoReading = true;
                    MetricStore.Snapshot.VirtualDriveMetricStore.Count++;
                    vdMetric = MetricStore.Snapshot.VirtualDriveMetricStore[MetricStore.Snapshot.VirtualDriveMetricStore.Count - 1];
                    vdMetric.CtlId = ctlId;

                    jsonReader.Read();
                    var dgvdSpan = jsonReader.ValueSpan;
                    if (Utf8Parser.TryParse(dgvdSpan, out int dg, out int bytesConsumed))
                    {
                        vdMetric.Dg = dg;

                        if (dgvdSpan.Length > bytesConsumed)
                        {
                            if (Utf8Parser.TryParse(dgvdSpan[(bytesConsumed+1)..], out int vd, out var _))
                            {
                                vdMetric.Vd = vd;
                            }
                        }
                    }
                    continue;
                }

                // PD 入口：EID:Slt 属性，每个 PD 对象的第一个字段
                if (jsonReader.TokenType == JsonTokenType.PropertyName && jsonReader.ValueTextEquals("EID:Slt"u8))
                {
                    pdBasicInfoReading = true;
                    MetricStore.Snapshot.PhysicalDriveMetricStore.Count++;
                    pdMetric = MetricStore.Snapshot.PhysicalDriveMetricStore[MetricStore.Snapshot.PhysicalDriveMetricStore.Count - 1];
                    pdMetric.CtlId = ctlId;
                    pdMetric.Vd = vdMetric?.Vd ?? 0;

                    jsonReader.Read();
                    var esSpan = jsonReader.ValueSpan;
                    if (Utf8Parser.TryParse(esSpan, out int eid, out int bytesConsumed))
                    {
                        pdMetric.Eid = eid;

                        if (esSpan.Length > bytesConsumed)
                        {
                            if (Utf8Parser.TryParse(esSpan[(bytesConsumed+1)..], out int slotId, out var _))
                            {
                                pdMetric.Slt = slotId;
                            }
                        }
                    }
                    continue;
                }
                
                if (jsonReader.TokenType == JsonTokenType.PropertyName && jsonReader.ValueTextEquals("OS Drive Name"u8))
                {
                    vdMetric?.OsDevice = GetStringMetric(jsonReader);
                    continue;
                }
                
                if (jsonReader.TokenType == JsonTokenType.PropertyName && jsonReader.ValueTextEquals("SCSI NAA Id"u8))
                {
                    vdMetric?.NaaId = GetStringMetric(jsonReader);
                    continue;
                }
                
                if (jsonReader.TokenType == JsonTokenType.PropertyName && jsonReader.ValueTextEquals("Active Operations"u8))
                {
                    vdMetric?.SetActiveOperation(jsonReader);
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
                        _ = jsonReader.ValueSpan switch
                        {
                            _ when jsonReader.ValueTextEquals("TYPE"u8)    => vdMetric?.Type = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("State"u8)   => vdMetric?.SetState(jsonReader),
                            _ when jsonReader.ValueTextEquals("Access"u8)  => vdMetric?.Access = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("Consist"u8) => vdMetric?.SetConsist(jsonReader),
                            _ when jsonReader.ValueTextEquals("Cache"u8)   => vdMetric?.Cache = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("Cac"u8)     => vdMetric?.Cac = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("sCC"u8)     => vdMetric?.SetScc(jsonReader),
                            _ when jsonReader.ValueTextEquals("Size"u8)    => vdMetric?.SizeBytes = ParseSize(jsonReader),
                            _ when jsonReader.ValueTextEquals("Name"u8)    => vdMetric?.Name = GetStringMetric(jsonReader),
                            _ => 0
                        };
                    }
                }

                if (pdBasicInfoReading)
                {
                    if (jsonReader.TokenType == JsonTokenType.EndObject)
                    {
                        pdBasicInfoReading = false;
                        continue;
                    }

                    if (jsonReader.TokenType == JsonTokenType.PropertyName)
                    {
                        _ = jsonReader.ValueSpan switch
                        {
                            _ when jsonReader.ValueTextEquals("DID"u8)   => pdMetric?.Did = GetIntMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("State"u8) => pdMetric?.SetState(jsonReader),
                            _ when jsonReader.ValueTextEquals("DG"u8)    => pdMetric?.Dg = GetIntMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("Size"u8)  => pdMetric?.SizeBytes = ParseSize(jsonReader),
                            _ when jsonReader.ValueTextEquals("Intf"u8)  => pdMetric?.Intf = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("Med"u8)   => pdMetric?.Med = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("SED"u8)   => pdMetric?.Sed = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("PI"u8)    => pdMetric?.Pi = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("SeSz"u8)  => pdMetric?.SeSz = ParseSectorSize(jsonReader),
                            _ when jsonReader.ValueTextEquals("Model"u8) => pdMetric?.Model = GetStringMetric(jsonReader),
                            _ when jsonReader.ValueTextEquals("Sp"u8)    => pdMetric?.SetSp(jsonReader),
                            _ when jsonReader.ValueTextEquals("Type"u8)  => pdMetric?.Type = GetStringMetric(jsonReader),
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

    protected static double ParseSectorSize(Utf8JsonReader reader)
    {
        reader.Read();
        var span = reader.ValueSpan;
        // 格式如 "512B" 或 "4kB"
        if (Utf8Parser.TryParse(span, out double num, out var consumed))
        {
            var unit = span[consumed..];
            if (unit.StartsWith("k"u8) || unit.StartsWith("K"u8)) return num * 1024;
            return num;
        }
        return 0;
    }
    
    public abstract Task CollectControllerMetrics(CancellationToken stoppingToken);

    public abstract Task CollectVirtualDriveMetrics(CancellationToken stoppingToken);

    public void Update() => metricStore.UpdateSnapshot();
}