using System.Text.Json;
using PercCli.Exporter.Stores;

namespace PercCli.Exporter.Metrics;

public sealed class VirtualDriveMetric
{
    public int CtlId { get; set; }
    public int Dg { get; set; }
    public int Vd { get; set; }
    public StringToken Type { get; set; }
    public byte State { get; set; }
    public StringToken Access { get; set; }
    public byte Consist { get; set; }
    public StringToken Cache { get; set; }
    public StringToken Cac { get; set; }
    public byte Scc { get; set; }
    public double SizeBytes { get; set; }
    public StringToken Name { get; set; }

    public byte SetState(Utf8JsonReader reader)
    {
        reader.Read();
        return State = reader.ValueSpan switch
        {
            _ when reader.ValueSpan.StartsWith("Opt"u8 ) => 1,
            _ when reader.ValueTextEquals("Degraded"u8) => 2,
            _ when reader.ValueTextEquals("Offline"u8) => 3,
            _ => 0
        };
    }
    
    public byte SetConsist(Utf8JsonReader reader)
    {
        reader.Read();
        return Consist = reader.ValueSpan switch
        {
            _ when reader.ValueTextEquals("Yes"u8 ) => 1,
            _ => 0
        };
    }
    
    public byte SetScc(Utf8JsonReader reader)
    {
        reader.Read();
        return Scc = reader.ValueSpan switch
        {
            _ when reader.ValueTextEquals("ON"u8 ) => 1,
            _ => 0
        };
    }
}