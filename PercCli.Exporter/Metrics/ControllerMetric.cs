using System.Text.Json;
using PercCli.Exporter.Stores;

namespace PercCli.Exporter.Metrics;

public sealed class ControllerMetric
{
    public int Ctl { get; set; }
    public StringToken Model { get; set; }
    public int Ports { get; set; }
    public int PDs { get; set; }
    public int DGs { get; set; }
    public int DnOpt { get; set; }
    public int VDs { get; set; }
    public int VnOpt { get; set; }
    public sbyte Bbu { get; set; }
    public byte Spr { get; set; }
    public StringToken Ds { get; set; }
    public byte Ehs { get; set; }
    public int AsOs { get; set; }
    public byte Hlth { get; set; }

    public sbyte SetBbu(Utf8JsonReader reader)
    {
        reader.Read();
        return Bbu = reader.ValueSpan switch
        {
            _ when reader.ValueSpan.StartsWith("Opt"u8 ) => 1,
            _ when reader.ValueTextEquals("Charging"u8) || reader.ValueTextEquals("Recycle"u8) || reader.ValueTextEquals("Relearning"u8) => 2,
            _ when reader.ValueTextEquals("Missing"u8) || reader.ValueTextEquals("None"u8) || reader.ValueTextEquals("N/A"u8) => -1,
            _ => 0
        };
    }
    
    public byte SetSpr(Utf8JsonReader reader)
    {
        reader.Read();
        return Spr = reader.ValueSpan switch
        {
            _ when reader.ValueTextEquals("On"u8 ) => 1,
            _ when reader.ValueTextEquals("Active"u8) => 2,
            _ when reader.ValueTextEquals("Off"u8) => 0,
            _ when reader.ValueTextEquals("Suspended"u8) => 3,
            _ => 0
        };
    }
    
    public byte SetEhs(Utf8JsonReader reader)
    {
        reader.Read();
        return Ehs = reader.ValueSpan switch
        {
            _ when reader.ValueSpan.StartsWith("Y"u8) => 1,
            _ when reader.ValueSpan.StartsWith("N"u8) => 0,
            _ => 0
        };
    }

    public byte SetHlth(Utf8JsonReader reader)
    {
        reader.Read();
        return Hlth = reader.ValueSpan switch
        {
            _ when reader.ValueSpan.StartsWith("Opt"u8) => 1,
            _ => 0
        };
    }
}