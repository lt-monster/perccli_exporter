using System.Text.Json;
using PercCli.Exporter.Stores;

namespace PercCli.Exporter.Metrics;

public sealed class PhysicalDriveMetric
{
    public int CtlId { get; set; }
    public int Vd { get; set; }
    public int Eid { get; set; }
    public int Slt { get; set; }
    public int Did { get; set; }
    public byte State { get; set; }
    public int Dg { get; set; }
    public double SizeBytes { get; set; }
    public StringToken Intf { get; set; }
    public StringToken Med { get; set; }
    public int Sed { get; set; }
    public int Pi { get; set; }
    public double SeSz { get; set; }
    public StringToken Model { get; set; }
    public byte Sp { get; set; }
    public StringToken Type { get; set; }

    public byte SetState(Utf8JsonReader reader)
    {
        reader.Read();
        return State = reader.ValueSpan switch
        {
            _ when reader.ValueTextEquals("Onln"u8) || 
                   reader.ValueTextEquals("UG"u8) || 
                   reader.ValueTextEquals("HS"u8) || 
                   reader.ValueTextEquals("JBOD"u8) => 1,
            _ when reader.ValueTextEquals("Pdg"u8) 
                   || reader.ValueTextEquals("Rbld"u8) => 2,
            _ => 0
        };
    }

    public byte SetSp(Utf8JsonReader reader)
    {
        reader.Read();
        //0=未配置热备，1=热备，2=全局热备
        return Sp = reader.ValueSpan switch
        {
            _ when reader.ValueTextEquals("HS"u8) => 1,
            _ when reader.ValueTextEquals("PS"u8) => 2,
            _ => 0
        };
    }
}
