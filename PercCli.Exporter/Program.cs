using System.Diagnostics;
using System.Runtime.InteropServices;
using PercCli.Exporter;
using PercCli.Exporter.Collectors;
using PercCli.Exporter.Stores;

if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    Console.WriteLine("perccli_exporter is only supported on Windows and Linux.");
    return;
}

var stopwatch = Stopwatch.StartNew();

static bool HasUrlsOverride(string[] args)
{
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))) return true;

    for (var i = 0; i < args.Length; i++)
    {
        var a = args[i];
        if (a.Equals("--urls", StringComparison.OrdinalIgnoreCase)) return true;
        if (a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase)) return true;
    }

    return false;
}

static int? GetPortOverride(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        var a = args[i];

        if (a.StartsWith("--port=", StringComparison.OrdinalIgnoreCase))
        {
            var v = a["--port=".Length..];
            if (int.TryParse(v, out var p) && p is > 0 and < 65536) return p;
        }

        if (a.Equals("--port", StringComparison.OrdinalIgnoreCase) || a.Equals("-p", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out var p) && p is > 0 and < 65536) return p;
        }
    }

    var env = Environment.GetEnvironmentVariable("PERC_EXPORTER_PORT") ?? Environment.GetEnvironmentVariable("PORT");
    if (int.TryParse(env, out var envPort) && envPort is > 0 and < 65536) return envPort;

    return null;
}

var builder = WebApplication.CreateSlimBuilder(args);

if (!HasUrlsOverride(args))
{
    var port = GetPortOverride(args);
    if (port is not null)
    {
        builder.WebHost.UseUrls($"http://*:{port.Value}");
    }
}

var percOptions = builder.Configuration.GetSection("PercOption").Get<PercCollectOptions>() ?? new();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    try {
        var startInfo = new ProcessStartInfo {
            FileName = "id",
            Arguments = "-u",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var process = Process.Start(startInfo);
        var output = process?.StandardOutput.ReadToEnd().Trim();
        percOptions.IsRoot = output == "0";
    } catch { }
}
builder.Services.AddSingleton(percOptions);

builder.Services.AddSingleton<PercMetricStore>();
builder.Services.AddSingleton<PercMetricWriter>();
builder.Services.AddSingleton<PercCollector, LocalPercCollector>();
builder.Services.AddHostedService<PercCollectService>();

var app = builder.Build();

app.MapGet("/metrics", async (HttpContext context, PercMetricWriter percMetricWriter) =>
{
    context.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";

    percMetricWriter.WriteControllerMetrics(context.Response.BodyWriter);
    percMetricWriter.WriteVirtualDriveMetrics(context.Response.BodyWriter);
    percMetricWriter.WritePhysicalDriveMetrics(context.Response.BodyWriter);

    await context.Response.BodyWriter.FlushAsync();
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine($"perccli_exporter has been launched within {stopwatch.ElapsedMilliseconds} milliseconds.");
});

app.Run();
