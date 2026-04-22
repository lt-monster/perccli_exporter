using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using PercCli.Exporter.Metrics;
using PercCli.Exporter.Stores;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PercCli.Exporter.Tests;

public sealed class UnitTest1
{
    private static async Task<string> ReadPipeAsStringAsync(PipeReader reader)
    {
        var sb = new StringBuilder();

        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;

            if (buffer.Length > 0)
            {
                foreach (var segment in buffer)
                {
                    sb.Append(Encoding.UTF8.GetString(segment.Span));
                }
            }

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted) break;
        }

        await reader.CompleteAsync();
        return sb.ToString();
    }

    [Fact]
    public async Task MetricWriter_Writes_Controller_Count_And_Info()
    {
        var store = new PercMetricStore();

        var ctl = store.Snapshot.ControllerMetricStore[0];
        ctl.Ctl = 0;
        ctl.Model = StringStore.GetOrAdd("PERC H730"u8);
        ctl.Hlth = 1;
        ctl.Bbu = 1;
        ctl.Spr = 1;
        ctl.Ehs = 1;
        ctl.Ports = 8;
        ctl.PDs = 12;
        ctl.DGs = 1;
        ctl.DnOpt = 0;
        ctl.VDs = 2;
        ctl.VnOpt = 0;
        ctl.Ds = StringStore.GetOrAdd("On"u8);
        ctl.AsOs = 0;

        store.Snapshot.ControllerMetricStore.Count = 1;
        store.UpdateSnapshot();

        var writer = new PercMetricWriter(store);
        var pipe = new Pipe();

        writer.WriteControllerMetrics(pipe.Writer);
        await pipe.Writer.FlushAsync();
        await pipe.Writer.CompleteAsync();

        var text = await ReadPipeAsStringAsync(pipe.Reader);

        Assert.Contains("perc_controller_count 1\n", text, StringComparison.Ordinal);
        Assert.Contains("perc_controller_info{ctl=\"0\",model=\"PERC H730\"} 1\n", text, StringComparison.Ordinal);
        Assert.Contains("perccli_controller_health_status{ctl=\"0\"} 1\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MetricWriter_Writes_VirtualDrive_ActiveOperation()
    {
        var store = new PercMetricStore();

        var ctl = store.Snapshot.ControllerMetricStore[0];
        ctl.Ctl = 0;
        ctl.Model = StringStore.GetOrAdd("PERC H730"u8);
        store.Snapshot.ControllerMetricStore.Count = 1;

        var vd = store.Snapshot.VirtualDriveMetricStore[0];
        vd.CtlId = 0;
        vd.Dg = 0;
        vd.Vd = 0;
        vd.Type = StringStore.GetOrAdd("RAID1"u8);
        vd.Access = StringStore.GetOrAdd("RW"u8);
        vd.Cache = StringStore.GetOrAdd("RWBD"u8);
        vd.Cac = StringStore.GetOrAdd("RWBD"u8);
        vd.Name = StringStore.GetOrAdd("os"u8);
        vd.OsDevice = StringStore.GetOrAdd("/dev/sda"u8);
        vd.NaaId = StringStore.GetOrAdd("naa.6000"u8);
        vd.State = 1;
        vd.Consist = 1;
        vd.Scc = 1;
        vd.SizeBytes = 1024;
        vd.ActiveOperations = 1;
        store.Snapshot.VirtualDriveMetricStore.Count = 1;

        store.UpdateSnapshot();

        var writer = new PercMetricWriter(store);
        var pipe = new Pipe();

        writer.WriteVirtualDriveMetrics(pipe.Writer);
        await pipe.Writer.FlushAsync();
        await pipe.Writer.CompleteAsync();

        var text = await ReadPipeAsStringAsync(pipe.Reader);

        Assert.Contains("perccli_virtual_drive_active_operation{ctl=\"0\",dg=\"0\",vd=\"0\"} 1\n", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{\"BBU\":\"Optimal\"}", 1)]
    [InlineData("{\"BBU\":\"Charging\"}", 2)]
    [InlineData("{\"BBU\":\"Missing\"}", -1)]
    [InlineData("{\"BBU\":\"Failure\"}", 0)]
    public void ControllerMetric_BBU_Mapping_Works(string json, sbyte expected)
    {
        var metric = new ControllerMetric();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        reader.Read();
        reader.Read();
        Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);
        Assert.Equal("BBU", reader.GetString());

        var actual = metric.SetBbu(reader);
        Assert.Equal(expected, actual);
        Assert.Equal(expected, metric.Bbu);
    }

    [Theory]
    [InlineData("{\"Active Operations\":\"None\"}", (byte)0)]
    [InlineData("{\"Active Operations\":\"Rebuild\"}", (byte)1)]
    [InlineData("{\"Active Operations\":\"Patrol Read\"}", (byte)1)]
    [InlineData("{\"Active Operations\":\"SomethingElse\"}", (byte)2)]
    public void VirtualDriveMetric_ActiveOperation_Mapping_Works(string json, byte expected)
    {
        var metric = new VirtualDriveMetric();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        reader.Read();
        reader.Read();
        Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);
        Assert.Equal("Active Operations", reader.GetString());

        var actual = metric.SetActiveOperation(reader);
        Assert.Equal(expected, actual);
        Assert.Equal(expected, metric.ActiveOperations);
    }

    [Theory]
    [InlineData("{\"State\":\"Onln\"}", (byte)1)]
    [InlineData("{\"State\":\"Rbld\"}", (byte)2)]
    [InlineData("{\"State\":\"Offln\"}", (byte)0)]
    public void PhysicalDriveMetric_State_Mapping_Works(string json, byte expected)
    {
        var metric = new PhysicalDriveMetric();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        reader.Read();
        reader.Read();
        Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);
        Assert.Equal("State", reader.GetString());

        var actual = metric.SetState(reader);
        Assert.Equal(expected, actual);
        Assert.Equal(expected, metric.State);
    }
}
