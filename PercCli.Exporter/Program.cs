using System.Diagnostics;
using PercCli.Exporter;
using PercCli.Exporter.Collectors;
using PercCli.Exporter.Stores;

var stopwatch = Stopwatch.StartNew();

var builder = WebApplication.CreateSlimBuilder(args);

var percOptions = builder.Configuration.GetSection("PercOption").Get<PercCollectOptions>() ?? new();
builder.Services.AddSingleton(percOptions);

builder.Services.AddSingleton<PercMetricStore>();
builder.Services.AddSingleton<PercMetricWriter>();

switch (percOptions.StartMode)
{
    case StartMode.Process:
        // builder.Services.AddSingleton<PercDataCollector, LocalPercDataCollector>();
        break;
    case StartMode.Ssh:
        if (string.IsNullOrWhiteSpace(percOptions.SshConfig?.Host) ||
            string.IsNullOrWhiteSpace(percOptions.SshConfig?.Username) ||
            string.IsNullOrWhiteSpace(percOptions.SshConfig?.Password))
        {
            Console.WriteLine("The mode used is SSH. \"Host\", \"Username\" and \"Password\" cannot be left blank.");
            return;
        }
        builder.Services.AddSingleton<PercCollector, SshPercCollector>();
        break;
    case StartMode.File:
        builder.Services.AddSingleton<PercCollector, FilePercCollector>();
        break;
    default: 
        Console.WriteLine($"Unknown StartMode: {percOptions.StartMode}");
        return;
}

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